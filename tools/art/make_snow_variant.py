#!/usr/bin/env python3
"""Crea una variante nevada de un asset procesado: copia <src> a <dst> y mezcla hacia blanco el color de las texturas con alfa (follaje).
Uso: make_snow_variant.py work_dir src dst [cantidad=0.7]"""
import json, os, shutil, sys
from PIL import Image
w, src, dst = sys.argv[1:4]; amt = float(sys.argv[4]) if len(sys.argv) > 4 else 0.7
sd, dd = os.path.join(w, src), os.path.join(w, dst)
if os.path.exists(dd): shutil.rmtree(dd)
os.makedirs(dd)
for f in os.listdir(sd):
    nf = f.replace(src, dst, 1)
    p = os.path.join(sd, f); q = os.path.join(dd, nf)
    if f.endswith(".json"):
        t = open(p).read().replace(src, dst); open(q, "w").write(t); continue
    if f.endswith("_diff.png"):
        im = Image.open(p)
        if im.mode == "RGBA":
            px = im.load()
            for y in range(im.height):
                for x in range(im.width):
                    r, g, b, a = px[x, y]
                    if a < 8: continue
                    k = min(1.0, amt * (0.6 + (r + g + b) / 765.0))
                    px[x, y] = (int(r + (238 - r) * k), int(g + (244 - g) * k), int(b + (252 - b) * k), a)
            im.save(q); continue
    shutil.copy(p, q)
