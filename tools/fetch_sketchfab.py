#!/usr/bin/env python3
"""Descarga modelos de Sketchfab (glb) y guarda su licencia/autor. Uso: SKETCHFAB_TOKEN=... fetch_sketchfab.py salida_dir nombre=uid ...
El token se lee solo del entorno; nunca se guarda ni se imprime."""
import json, os, sys, urllib.request
tok = os.environ["SKETCHFAB_TOKEN"]
out = sys.argv[1]; os.makedirs(out, exist_ok=True)
H = {"User-Agent": "Mozilla/5.0", "Authorization": "Token " + tok}
def get(u, auth=True):
    h = dict(H) if auth else {"User-Agent": "Mozilla/5.0"}
    return urllib.request.urlopen(urllib.request.Request(u, headers=h), timeout=120).read()
meta = {}
for arg in sys.argv[2:]:
    name, uid = arg.split("=")
    try:
        m = json.loads(get("https://api.sketchfab.com/v3/models/" + uid))
        dl = json.loads(get("https://api.sketchfab.com/v3/models/%s/download" % uid))
        data = get(dl["glb"]["url"], auth=False)
        open(os.path.join(out, name + ".glb"), "wb").write(data)
        meta[name] = {"title": m["name"], "author": m["user"]["displayName"], "license": m["license"]["label"], "url": m["viewerUrl"], "faces": m["faceCount"], "uid": uid}
        print("ok", name, len(data) // 1024, "KB", m["faceCount"], "faces")
    except Exception as e:
        print("FALLO", name, e)
p = os.path.join(out, "sources.json")
old = json.load(open(p)) if os.path.exists(p) else {}
old.update(meta); json.dump(old, open(p, "w"), indent=1)
