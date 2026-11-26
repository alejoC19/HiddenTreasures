#!/usr/bin/env python3
"""Presupuesto ESTATICO de arte (no es una medicion de rendimiento): estima la memoria de texturas en VRAM y los triangulos por asset
a partir de los archivos del proyecto. Uso: budget.py [raiz_del_repo] > ARTE_PRESUPUESTO.md"""
import json, os, sys
from PIL import Image
root = sys.argv[1] if len(sys.argv) > 1 else "."
res = os.path.join(root, "Assets", "_Project", "Resources", "Art")

def vram_mb(path):
    """Estimacion con compresion de bloques (DXT1/BC1 0,5 B/px sin alfa; DXT5/BC3 1 B/px con alfa o normales) y mipmaps (x1,33)."""
    try:
        im = Image.open(path); w, h = im.size
        name = os.path.basename(path).lower()
        bpp = 1.0 if (im.mode in ("RGBA", "LA", "P") or "_nor" in name or "_ms" in name) else 0.5
        return w * h * bpp * 1.33 / 1048576.0, (w, h)
    except Exception:
        return 0.0, (0, 0)

groups = {}
for dp, _, fns in os.walk(res):
    for f in fns:
        if f.lower().endswith((".png", ".jpg", ".jpeg")):
            rel = os.path.relpath(dp, res).split(os.sep)
            key = "/".join(rel[:2]) if rel[0] == "World" and len(rel) > 1 else rel[0]
            m, _ = vram_mb(os.path.join(dp, f))
            groups.setdefault(key, [0.0, 0]); groups[key][0] += m; groups[key][1] += 1
tris = []
world = os.path.join(res, "World")
if os.path.isdir(world):
    for n in sorted(os.listdir(world)):
        j = os.path.join(world, n, n + ".json")
        if os.path.exists(j):
            d = json.load(open(j))
            tris.append((n, d.get("height", 0), [l["tris"] for l in d.get("lods", [])], len(d.get("materials", []))))

total = sum(v[0] for v in groups.values())
print("# Presupuesto estático de arte\n")
print("> **Es una estimación calculada desde los archivos, no una medición.** Los FPS reales solo salen de F9 / el build de rendimiento en tu RX 580. Texturas estimadas con compresión de bloques y mipmaps; la memoria real depende de las opciones de importación y del nivel de calidad.\n")
print("## Texturas (VRAM estimada)\n\n| Grupo | Archivos | VRAM (MB) |\n|---|---|---|")
for k, (m, n) in sorted(groups.items(), key=lambda kv: -kv[1][0])[:40]:
    print(f"| {k} | {n} | {m:.1f} |")
print(f"\n**Total estimado de texturas del proyecto: {total:.0f} MB** (la RX 580 tiene 8192 MB; en juego solo están cargadas las del bioma actual, así que el pico real es menor).\n")
print("## Modelos (triángulos por LOD y materiales)\n\n| Asset | Alto (m) | LOD0 / LOD1 / LOD2 (tris) | Materiales |\n|---|---|---|---|")
for n, h, t, m in tris:
    print(f"| {n} | {h:.2f} | {' / '.join(str(x) for x in t)} | {m} |")
print("\n## Qué mirar al medir\nÁrboles cercanos (LOD0 ≤ 6 k tris) × cientos visibles dentro de 85 m, y ~50 k instancias de vegetación: son los candidatos a ajustar *si* el F9 muestra GPU limitada por triángulos o draw calls. Las palancas ya existen: `InstancedVegetation` (distancias por capa y de LOD), el nivel de calidad y los LOD de cada modelo.")
