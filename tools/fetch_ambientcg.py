#!/usr/bin/env python3
"""Descarga materiales CC0 de ambientCG (2K-JPG) y los descomprime en salida_dir/<id>/. Uso: fetch_ambientcg.py salida_dir id [id...]"""
import io, json, os, sys, urllib.request, zipfile
out = sys.argv[1]
H = {"User-Agent": "HiddenTreasures-art-pipeline"}
def get(u): return urllib.request.urlopen(urllib.request.Request(u, headers=H), timeout=180).read()
for aid in sys.argv[2:]:
    try:
        d = json.loads(get("https://ambientcg.com/api/v2/full_json?id=%s&include=downloadData" % aid))
        a = d["foundAssets"][0]
        dl = [x for x in a["downloadFolders"]["default"]["downloadFiletypeCategories"]["zip"]["downloads"] if x["attribute"] == "2K-JPG"][0]
        z = zipfile.ZipFile(io.BytesIO(get(dl["fullDownloadPath"])))
        p = os.path.join(out, aid); os.makedirs(p, exist_ok=True); z.extractall(p)
        print("ok", aid, [n for n in os.listdir(p)])
    except Exception as e:
        print("FALLO", aid, e)
