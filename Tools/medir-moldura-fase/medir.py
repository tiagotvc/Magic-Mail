# -*- coding: utf-8 -*-
"""Mede os encaixes da moldura do HUD e imprime as constantes de RoyalAvesGameScreenBuilder.

    python medir.py "moldura-fase.png"

Encontra o furo redondo do retrato (o buraco transparente) e os dois paineis bege (movimentos e metas).
"""
import sys
import numpy as np
from PIL import Image

def groups(idx, gap):
    out = []; s = idx[0]; p = idx[0]
    for c in idx[1:]:
        if c - p > gap: out.append((int(s), int(p))); s = c
        p = c
    out.append((int(s), int(p))); return out

im = np.array(Image.open(sys.argv[1]).convert("RGBA"))
alpha = im[..., 3]; rgb = im[..., :3].astype(int)
h, w = alpha.shape
print("moldura: %dx%d  ->  proporcao %.2f:1   (o alvo e ~4.5:1, para a faixa ficar em ~12%% da tela)" % (w, h, w / float(h)))

# furo do retrato: o vazio transparente cercado por moldura, a meia altura
row = np.where(alpha[h // 2] > 10)[0]
gaps = [(int(row[i]), int(row[i + 1])) for i in range(len(row) - 1) if row[i + 1] - row[i] > 3]
if not gaps:
    print("nao encontrei o furo redondo do retrato"); sys.exit(1)
x0, x1 = max(gaps, key=lambda g: g[1] - g[0])
mid = (x0 + x1) // 2
col = np.where(alpha[:, mid] > 10)[0]
vgaps = [(int(col[i]), int(col[i + 1])) for i in range(len(col) - 1) if col[i + 1] - col[i] > 3]
y0, y1 = max(vgaps, key=lambda g: g[1] - g[0])
print("furo do retrato: %d..%d x %d..%d  (diametro ~%d = %.0f%% da altura)" % (x0, x1, y0, y1, x1 - x0, (x1 - x0) * 100.0 / h))

# paineis bege: a zona clara dentro do couro
beige = (rgb[..., 0] > 235) & (rgb[..., 1] > 180) & (rgb[..., 2] > 100) & (alpha > 200)
band = beige[int(h * 0.33):int(h * 0.66), :]
cols = np.where(band.sum(axis=0) > band.shape[0] * 0.6)[0]
panels = []
for a, b in groups(cols, 20):
    if b - a < w * 0.05: continue
    sub = beige[:, a + 20:b - 20]
    rr = np.where(sub.sum(axis=1) > (b - a - 40) * 0.6)[0]
    if len(rr): panels.append((a, int(rr.min()), b + 1, int(rr.max()) + 1))
panels.sort()
print()
print("        const float ArtW = %df, ArtH = %df;" % (w, h))
print("        static readonly Rect HoleArt = Rect.MinMaxRect(%d, %d, %d, %d);" % (x0, y0, x1 + 1, y1 + 1))
for name, p in zip(("MovesArt", "GoalsArt"), panels):
    print("        static readonly Rect %s = Rect.MinMaxRect(%d, %d, %d, %d);" % (name, p[0], p[1], p[2], p[3]))
if len(panels) != 2:
    print("\n  aviso: encontrei %d painel(eis) bege, esperava 2" % len(panels))
