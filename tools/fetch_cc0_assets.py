#!/usr/bin/env python3
"""Descarga texturas PBR CC0 de Poly Haven (https://polyhaven.com, licencia CC0: sin atribucion obligatoria).
Guarda Diffuse y Normal (OpenGL) a 1k en Assets/_Project/Resources/Art/Textures y escribe CREDITS_ASSETS.md.
Uso: python3 tools/fetch_cc0_assets.py <repo_root>"""
import json, os, sys, urllib.request

ROOT = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else ".")
OUT = os.path.join(ROOT, "Assets/_Project/Resources/Art/Textures")
UA = {"User-Agent": "HiddenTreasuresPrototype"}

# id de Poly Haven -> nombre local (uso en el juego)
TEXTURES = {
    "forest_ground_04": "forest_floor",      # suelo de bosque (base)
    "leafy_grass": "grass",                  # claros con pasto
    "stony_dirt_path": "road",               # camino de tierra con piedras
    "red_laterite_soil_stones": "red_earth", # tierra roja de yacimiento (ruinas / excavacion)
    "mossy_rock": "rock",                    # roca con musgo (laderas)
    "pine_bark": "bark_pine",                # corteza de pino
    "bark_brown_02": "bark_broadleaf",       # corteza de latifoliado
    "mossy_stone_wall": "ruin_stone",        # muros de ruinas
    "medieval_blocks_02": "ruin_blocks",     # bloques de piedra labrada
    "weathered_planks": "wood_planks",       # madera de campamento
}

def get(u):
    return json.load(urllib.request.urlopen(urllib.request.Request(u, headers=UA), timeout=60))

def download(url, path):
    if os.path.exists(path) and os.path.getsize(path) > 1000:
        return os.path.getsize(path)
    data = urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=120).read()
    open(path, "wb").write(data)
    return len(data)

os.makedirs(OUT, exist_ok=True)
rows = []
total = 0
for pid, local in TEXTURES.items():
    files = get("https://api.polyhaven.com/files/" + pid)
    info = get("https://api.polyhaven.com/info/" + pid)
    for kind, suffix in (("Diffuse", "diff"), ("nor_gl", "nor")):
        url = files[kind]["1k"]["jpg"]["url"]
        n = download(url, os.path.join(OUT, f"{local}_{suffix}.jpg"))
        total += n
    authors = ", ".join(info.get("authors", {}).keys()) or "Poly Haven"
    rows.append((local, pid, authors))
    print("ok", pid, "->", local)

with open(os.path.join(ROOT, "CREDITS_ASSETS.md"), "w", encoding="utf-8") as f:
    f.write("# Créditos de assets de terceros\n\nTodas las texturas de esta lista son **CC0 (dominio público)**, de [Poly Haven](https://polyhaven.com). "
            "No exigen atribución; se acredita igualmente. Descarga reproducible: `tools/fetch_cc0_assets.py`.\n\n"
            "| Uso en el juego | ID en Poly Haven | Autores |\n|---|---|---|\n")
    for local, pid, authors in rows:
        f.write(f"| `{local}` | [{pid}](https://polyhaven.com/a/{pid}) | {authors} |\n")
print("total descargado: %.1f MB" % (total / 1e6))
