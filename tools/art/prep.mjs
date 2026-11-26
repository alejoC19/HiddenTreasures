// Pipeline de arte: toma un .glb (Sketchfab / Poly Haven) y deja un asset listo para Unity:
//  - geometria fusionada por material, centrada en XZ, apoyada en y=0, escalada a una altura real (metros),
//  - LODs decimados (<nombre>_lod0.glb, _lod1.glb...), SIN texturas embebidas,
//  - texturas exportadas aparte (<mat>_diff, _nor, _mr) + <nombre>.json con la descripcion de cada material.
// Uso: node prep.mjs <entrada.glb> <salida_dir> <nombre> <altura_m> <ratio_lod1,ratio_lod2> [soloNodos_regex] [excluirNodos_regex]
import { NodeIO, Document } from '@gltf-transform/core';
import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
import { MeshoptDecoder, MeshoptSimplifier } from 'meshoptimizer';
import fs from 'fs';
import path from 'path';

const [input, outDir, name, heightArg, ratiosArg, onlyRe, skipRe] = process.argv.slice(2);
const targetHeight = parseFloat(heightArg);
const ratios = (ratiosArg || '').split(',').filter(Boolean).map(Number);
await MeshoptDecoder.ready; await MeshoptSimplifier.ready;
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS).registerDependencies({ 'meshopt.decoder': MeshoptDecoder });
const doc = await io.read(input);
fs.mkdirSync(outDir, { recursive: true });

function mulMat(m, v) { // m: column-major 4x4, v: [x,y,z,1]
  return [m[0]*v[0]+m[4]*v[1]+m[8]*v[2]+m[12], m[1]*v[0]+m[5]*v[1]+m[9]*v[2]+m[13], m[2]*v[0]+m[6]*v[1]+m[10]*v[2]+m[14]];
}
function normalMat(m, n) { const r = [m[0]*n[0]+m[4]*n[1]+m[8]*n[2], m[1]*n[0]+m[5]*n[1]+m[9]*n[2], m[2]*n[0]+m[6]*n[1]+m[10]*n[2]]; const l = Math.hypot(...r) || 1; return r.map(x => x / l); }

// 1) Recolectar geometria en mundo, agrupada por material.
const groups = new Map(); // matName -> {mat, pos:[], nor:[], uv:[], idx:[]}
for (const node of doc.getRoot().listNodes()) {
  const mesh = node.getMesh(); if (!mesh) continue;
  if (onlyRe && !new RegExp(onlyRe).test(node.getName())) continue;
  if (skipRe && new RegExp(skipRe).test(node.getName())) continue;
  const w = node.getWorldMatrix();
  for (const prim of mesh.listPrimitives()) {
    const mat = prim.getMaterial(); const key = mat ? mat.getName() || 'mat' : 'mat';
    let g = groups.get(key); if (!g) { g = { mat, pos: [], nor: [], uv: [], idx: [] }; groups.set(key, g); }
    const P = prim.getAttribute('POSITION'), N = prim.getAttribute('NORMAL'), U = prim.getAttribute('TEXCOORD_0'), I = prim.getIndices();
    const base = g.pos.length / 3; const v = [0, 0, 0];
    for (let i = 0; i < P.getCount(); i++) {
      P.getElement(i, v); g.pos.push(...mulMat(w, v));
      if (N) { N.getElement(i, v); g.nor.push(...normalMat(w, v)); } else g.nor.push(0, 1, 0);
      if (U) { const t = [0, 0]; U.getElement(i, t); g.uv.push(t[0], t[1]); } else g.uv.push(0, 0);
    }
    const count = I ? I.getCount() : P.getCount();
    for (let i = 0; i < count; i++) g.idx.push(base + (I ? I.getScalar(i) : i));
  }
}
if (!groups.size) { console.error('sin geometria'); process.exit(2); }

// 2) Normalizar: centro XZ, base y=0, escala a altura objetivo.
let mn = [1e12, 1e12, 1e12], mx = [-1e12, -1e12, -1e12];
for (const g of groups.values()) for (let i = 0; i < g.pos.length; i += 3) for (let a = 0; a < 3; a++) { mn[a] = Math.min(mn[a], g.pos[i+a]); mx[a] = Math.max(mx[a], g.pos[i+a]); }
const srcH = mx[1] - mn[1]; const s = targetHeight > 0 ? targetHeight / srcH : 1;
const cx = (mn[0] + mx[0]) / 2, cz = (mn[2] + mx[2]) / 2;
for (const g of groups.values()) for (let i = 0; i < g.pos.length; i += 3) { g.pos[i] = (g.pos[i] - cx) * s; g.pos[i+1] = (g.pos[i+1] - mn[1]) * s; g.pos[i+2] = (g.pos[i+2] - cz) * s; }

// 3) Texturas y descripcion de materiales.
const manifest = { name, height: srcH * s, size: [(mx[0]-mn[0])*s, srcH * s, (mx[2]-mn[2])*s], materials: [], lods: [] };
function dump(tex, suffix, matName) {
  if (!tex) return null;
  const ext = tex.getMimeType() === 'image/jpeg' ? 'jpg' : 'png';
  const file = `${name}_${matName}_${suffix}.${ext}`.replace(/[^A-Za-z0-9_.\-]/g, '_');
  fs.writeFileSync(path.join(outDir, file), tex.getImage());
  return file;
}
for (const [key, g] of groups) {
  const m = g.mat; const k = key.replace(/[^A-Za-z0-9]/g, '_');
  manifest.materials.push({
    name: k,
    diff: m ? dump(m.getBaseColorTexture(), 'diff', k) : null,
    nor: m ? dump(m.getNormalTexture(), 'nor', k) : null,
    mr: m ? dump(m.getMetallicRoughnessTexture(), 'mr', k) : null,
    alphaMode: m ? m.getAlphaMode() : 'OPAQUE', cutoff: m ? m.getAlphaCutoff() : 0.5, doubleSided: m ? m.getDoubleSided() : false,
    color: m ? m.getBaseColorFactor() : [1, 1, 1, 1], metallic: m ? m.getMetallicFactor() : 0, roughness: m ? m.getRoughnessFactor() : 1,
  });
}

// 4) LODs: LOD0 completo, LOD1.. decimados. Un nodo por material.
async function writeLod(level, ratio) {
  const out = new Document(); const buf = out.createBuffer(); const scene = out.createScene('s'); let tris = 0;
  for (const [key, g] of groups) {
    let idx = Uint32Array.from(g.idx); const pos = Float32Array.from(g.pos);
    if (ratio < 1) {
      const target = Math.max(60, Math.floor(idx.length * ratio / 3) * 3);
      const [simp] = MeshoptSimplifier.simplify(idx, pos, 3, target, 0.08, ['Prune']);
      idx = simp.length >= 6 ? simp : idx;
    }
    tris += idx.length / 3;
    const k = key.replace(/[^A-Za-z0-9]/g, '_');
    const prim = out.createPrimitive()
      .setAttribute('POSITION', out.createAccessor().setType('VEC3').setArray(pos).setBuffer(buf))
      .setAttribute('NORMAL', out.createAccessor().setType('VEC3').setArray(Float32Array.from(g.nor)).setBuffer(buf))
      .setAttribute('TEXCOORD_0', out.createAccessor().setType('VEC2').setArray(Float32Array.from(g.uv)).setBuffer(buf))
      .setIndices(out.createAccessor().setType('SCALAR').setArray(idx).setBuffer(buf))
      .setMaterial(out.createMaterial(k));
    const node = out.createNode(k).setMesh(out.createMesh(k).addPrimitive(prim));
    scene.addChild(node);
  }
  await io.write(path.join(outDir, `${name}_lod${level}.glb`), out);
  manifest.lods.push({ level, tris: Math.round(tris), ratio });
}
// ratios = proporcion de triangulos respecto del original para cada LOD (el primero es LOD0).
for (let i = 0; i < (ratios.length || 1); i++) await writeLod(i, ratios.length ? ratios[i] : 1);
fs.writeFileSync(path.join(outDir, `${name}.json`), JSON.stringify(manifest, null, 1));
console.log(name, 'alto', srcH * s, 'm', manifest.lods.map(l => l.tris + ' tris').join(' / '), 'materiales', manifest.materials.length);
