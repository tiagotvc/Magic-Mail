# -*- coding: utf-8 -*-
"""Le a folha de alfabeto e devolve cada glifo ja alinhado na linha de base."""
import numpy as np
from PIL import Image
from collections import Counter

ROWS = [
    u"ABCDEFGHIJKLM",
    u"NOPQRSTUVWXYZ",
    u"abcdefghijklm",
    u"nopqrstuvwxyz",
    u"0123456789\u00c1\u00c0\u00c2",
    u"\u00c3\u00c9\u00ca\u00cd\u00d3\u00d4\u00d5\u00da\u00dc\u00c7\u00e1\u00e0\u00e2",
    u"\u00e3\u00e9\u00ea\u00ed\u00f3\u00f4\u00f5\u00fa\u00fc\u00e7\u00b7,:",
    u";!?+-=/()%#@&",
    u"*",
]

# O gerador de imagem colocou varios glifos 15-35 px fora do lugar. Cada um e reposicionado pela regra
# tipografica da sua classe, sem mudar de tamanho: as alturas vieram consistentes, so a posicao variou.
DESCENDERS = set(u"gpqy")          # topo na altura-x, pe abaixo da base
BASELINE_EXCEPTIONS = set(u"j,;:\u00b7*()/@+-=")

def _groups(idx, gap):
    out = []; s = idx[0]; p = idx[0]
    for c in idx[1:]:
        if c - p > gap: out.append((int(s), int(p))); s = c
        p = c
    out.append((int(s), int(p))); return out

def load(path, body=(119, 29, 18), tol=90):
    a = np.array(Image.open(path).convert("RGBA"))
    ink = (a[..., 3] > 240) & (np.abs(a[..., :3].astype(int) - np.array(body)).sum(axis=2) < tol)
    rows = _groups(np.where(ink.any(axis=1))[0], 20)
    glyphs = {}
    for i, (r0, r1) in enumerate(rows):
        cols = _groups(np.where(ink[r0:r1 + 1].any(axis=0))[0], 12)
        for j, (c0, c1) in enumerate(cols):
            if j >= len(ROWS[i]): break
            block = ink[r0:r1 + 1, c0:c1 + 1]
            rr = np.where(block.any(axis=1))[0]; cc = np.where(block.any(axis=0))[0]
            glyphs[ROWS[i][j]] = dict(
                mask=block[rr.min():rr.max() + 1, cc.min():cc.max() + 1],
                row=i, top=r0 + int(rr.min()), bottom=r0 + int(rr.max()),
                left=c0 + int(cc.min()), width=int(cc.max() - cc.min() + 1))
    # base de cada linha: a moda dos pes dos glifos que de facto pousam nela
    bases = {}
    for i, chars in enumerate(ROWS):
        feet = [glyphs[c]["bottom"] for c in chars
                if c in glyphs and c not in DESCENDERS and c not in BASELINE_EXCEPTIONS]
        if feet: bases[i] = Counter(feet).most_common(1)[0][0]
    return glyphs, bases
