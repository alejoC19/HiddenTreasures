#!/usr/bin/env python3
"""Posproceso de texturas exportadas por prep.mjs: _mr (glTF G=rugosidad, B=metalico) -> _ms (R=0 (sin metal), A=suavidad, para URP/Lit)
y reduccion a un maximo de resolucion. Uso: post_textures.py dir [max_px=1024]"""
import os, sys
from PIL import Image
d = sys.argv[1]; mx = int(sys.argv[2]) if len(sys.argv) > 2 else 1024
for f in sorted(os.listdir(d)):
    p = os.path.join(d, f)
    if not f.lower().endswith((".png", ".jpg")): continue
    im = Image.open(p)
    if f.endswith("_mr.png") or f.endswith("_mr.jpg"):
        im = im.convert("RGB"); r, g, b = im.split()
        zero = b.point(lambda v: 0)   # el entorno (rocas, arboles, ruinas) no es metalico: sin esto algunos escaneos salen como espejo oscuro
        out = Image.merge("RGBA", (zero, zero, zero, g.point(lambda v: 255 - v)))
        if max(out.size) > mx: out = out.resize((min(mx, out.width), min(mx, out.height)), Image.LANCZOS)
        out.save(os.path.join(d, f.rsplit("_mr.", 1)[0] + "_ms.png")); os.remove(p); continue
    if max(im.size) > mx:
        k = mx / max(im.size); im = im.resize((int(im.width * k), int(im.height * k)), Image.LANCZOS)
        im.save(p, quality=90) if f.endswith(".jpg") else im.save(p)
