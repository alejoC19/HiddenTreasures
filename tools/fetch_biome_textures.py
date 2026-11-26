#!/usr/bin/env python3
"""Texturas PBR CC0 (Poly Haven) para los biomas nuevos: desierto, nieve y pantano.
Guarda Diffuse y Normal (OpenGL) a 1k en Assets/_Project/Resources/Art/Textures y escribe CREDITS_BIOMAS.md.
Uso: python3 tools/fetch_biome_textures.py <repo_root>"""
import json, os, sys, urllib.request

ROOT = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else ".")
OUT = os.path.join(ROOT, "Assets/_Project/Resources/Art/Textures")
UA = {"User-Agent": "HiddenTreasuresPrototype"}

TEXTURES = {
    # Cañon del desierto
    "red_sand": "desert_sand", "dry_ground_01": "desert_dry", "rocky_trail": "desert_road",
    "cracked_red_ground": "desert_earth", "rock_face": "desert_rock", "sandstone_blocks_04": "desert_ruin",
    # Pinar nevado
    "snow_02": "snow_a", "snow_03": "snow_b", "gravel_ground_01": "snow_road",
    "rock_ground_02": "snow_earth", "rock_face_03": "snow_rock", "broken_wall": "snow_ruin",
    # Pantano
    "mud_forest": "swamp_mud", "brown_mud_leaves_01": "swamp_moss", "brown_mud_02": "swamp_road",
    "muddy_tracks": "swamp_earth", "bark_willow": "bark_dead",
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
rows, total, missing = [], 0, []
for pid, local in TEXTURES.items():
    try:
        files = get("https://api.polyhaven.com/files/" + pid)
        info = get("https://api.polyhaven.com/info/" + pid)
        for kind, suffix in (("Diffuse", "diff"), ("nor_gl", "nor")):
            total += download(files[kind]["1k"]["jpg"]["url"], os.path.join(OUT, f"{local}_{suffix}.jpg"))
        rows.append((local, pid, ", ".join(info.get("authors", {}).keys()) or "Poly Haven"))
        print("ok", pid, "->", local)
    except Exception as e:
        missing.append((pid, str(e)[:80])); print("FALLO", pid, e)

with open(os.path.join(ROOT, "CREDITS_BIOMAS.md"), "w", encoding="utf-8") as f:
    f.write("# Texturas de los biomas nuevos\n\nTodas **CC0** (dominio público) de [Poly Haven](https://polyhaven.com). Descarga reproducible: `tools/fetch_biome_textures.py`.\n\n"
            "| Uso en el juego | ID en Poly Haven | Autores |\n|---|---|---|\n")
    for local, pid, authors in rows:
        f.write(f"| `{local}` | [{pid}](https://polyhaven.com/a/{pid}) | {authors} |\n")
print("total descargado: %.1f MB; faltan: %s" % (total / 1e6, missing))
