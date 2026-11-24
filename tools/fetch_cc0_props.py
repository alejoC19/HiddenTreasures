#!/usr/bin/env python3
"""Descarga modelos CC0 de Poly Haven (FBX 1k + texturas) a Assets/_Project/Resources/Art/Props/<id>/ y agrega los creditos.
Uso: python3 tools/fetch_cc0_props.py <repo_root>"""
import json, os, sys, urllib.request

ROOT = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else ".")
OUT = os.path.join(ROOT, "Assets/_Project/Resources/Art/Props")
UA = {"User-Agent": "HiddenTreasuresPrototype"}
PROPS = ["stone_fire_pit", "wooden_crate_01", "wooden_crate_02", "barrel_03", "wooden_bucket_01", "vintage_oil_lamp",
         "wooden_lantern_01", "wooden_ladder_02", "antique_ceramic_vase_01", "ceramic_vase_02", "brass_vase_03"]

def get(u): return json.load(urllib.request.urlopen(urllib.request.Request(u, headers=UA), timeout=60))
def dl(u, p):
    os.makedirs(os.path.dirname(p), exist_ok=True)
    if os.path.exists(p) and os.path.getsize(p) > 100: return os.path.getsize(p)
    d = urllib.request.urlopen(urllib.request.Request(u, headers=UA), timeout=180).read(); open(p, "wb").write(d); return len(d)

rows, total = [], 0
for pid in PROPS:
    files = get("https://api.polyhaven.com/files/" + pid); info = get("https://api.polyhaven.com/info/" + pid)
    fb = files["fbx"]["1k"]["fbx"]
    base = os.path.join(OUT, pid)
    total += dl(fb["url"], os.path.join(base, pid + "_1k.fbx"))
    for rel, meta in fb.get("include", {}).items():
        total += dl(meta["url"], os.path.join(base, rel))
    dims = info.get("dimensions") or [1000, 1000, 1000]
    json.dump({"id": pid, "dimensions_mm": dims, "polycount": info.get("polycount")}, open(os.path.join(base, "info.json"), "w"))
    rows.append((pid, ", ".join(info.get("authors", {}).keys()) or "Poly Haven", info.get("polycount")))
    print("ok", pid)

p = os.path.join(ROOT, "CREDITS_ASSETS.md")
txt = open(p, encoding="utf-8").read() if os.path.exists(p) else "# Créditos de assets de terceros\n"
if "## Modelos (Poly Haven, CC0)" not in txt:
    txt += "\n## Modelos (Poly Haven, CC0)\n\nDescarga reproducible: `tools/fetch_cc0_props.py`.\n\n| ID | Autores | Polígonos |\n|---|---|---|\n"
    for pid, a, pc in rows: txt += f"| [{pid}](https://polyhaven.com/a/{pid}) | {a} | {pc} |\n"
open(p, "w", encoding="utf-8").write(txt)
print("total %.1f MB" % (total / 1e6))
