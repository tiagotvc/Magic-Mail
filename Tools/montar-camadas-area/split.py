# -*- coding: utf-8 -*-
"""Separa uma folha de upgrades nos seus objectos, por componentes ligados do alfa."""
import numpy as np

def label(mask):
    """Componentes ligados (8-vizinhos) sem scipy: duas passagens com union-find."""
    h, w = mask.shape
    parent = {}
    def find(a):
        while parent[a] != a:
            parent[a] = parent[parent[a]]; a = parent[a]
        return a
    def union(a, b):
        ra, rb = find(a), find(b)
        if ra != rb: parent[max(ra, rb)] = min(ra, rb)
    lab = np.zeros((h, w), np.int32); nxt = 1
    for y in range(h):
        row = mask[y]
        for x in np.nonzero(row)[0]:
            near = []
            if y > 0:
                for dx in (-1, 0, 1):
                    xx = x + dx
                    if 0 <= xx < w and lab[y-1, xx]: near.append(lab[y-1, xx])
            if x > 0 and lab[y, x-1]: near.append(lab[y, x-1])
            if near:
                m = min(near); lab[y, x] = m
                for n in near: union(m, n)
            else:
                lab[y, x] = nxt; parent[nxt] = nxt; nxt += 1
    for y in range(h):
        for x in np.nonzero(lab[y])[0]:
            lab[y, x] = find(lab[y, x])
    return lab

def objects(rgba, min_area=1500, gap=6):
    """Devolve (recorte RGBA, x, y) por objecto. Dilata um pouco antes de ligar, para nao partir
    um objecto com contorno fino (a haste de um poste, o pe de uma mesa)."""
    a = rgba[..., 3] > 25
    d = a.copy()
    for _ in range(gap):
        d[1:, :] |= a[:-1, :]; d[:-1, :] |= a[1:, :]
        d[:, 1:] |= a[:, :-1]; d[:, :-1] |= a[:, 1:]
        a = d.copy()
    lab = label(d)
    out = []
    for i in range(1, lab.max() + 1):
        m = (lab == i) & (rgba[..., 3] > 25)
        if m.sum() < min_area: continue
        ys, xs = np.nonzero(m)
        y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
        piece = rgba[y0:y1, x0:x1].copy()
        piece[..., 3] = np.where(m[y0:y1, x0:x1], piece[..., 3], 0)
        out.append((piece, int(x0), int(y0)))
    out.sort(key=lambda t: -(t[0].shape[0] * t[0].shape[1]))
    return out
