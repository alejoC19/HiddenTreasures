#!/usr/bin/env python3
"""Descarga modelos CC0 de Poly Haven (gltf + bin + texturas) a salida_dir/<id>/. Uso: fetch_polyhaven_models.py salida_dir res id [id...]"""
import json, os, sys, urllib.request
out, res = sys.argv[1], sys.argv[2]
H = {"User-Agent": "HiddenTreasures-art-pipeline"}
def get(u): return urllib.request.urlopen(urllib.request.Request(u, headers=H), timeout=180).read()
for mid in sys.argv[3:]:
    try:
        files = json.loads(get("https://api.polyhaven.com/files/" + mid))
        g = files["gltf"]; r = res if res in g else sorted(g, key=lambda k: abs(int(k[:-1]) - int(res[:-1])))[0]
        entry = g[r]["gltf"]
        d = os.path.join(out, mid); os.makedirs(d, exist_ok=True)
        open(os.path.join(d, os.path.basename(entry["url"])), "wb").write(get(entry["url"]))
        n = 0
        for rel, inc in entry.get("include", {}).items():
            p = os.path.join(d, rel); os.makedirs(os.path.dirname(p), exist_ok=True)
            open(p, "wb").write(get(inc["url"])); n += 1
        print("ok", mid, r, n, "archivos")
    except Exception as e:
        print("FALLO", mid, e)
