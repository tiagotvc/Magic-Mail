# -*- coding: utf-8 -*-
"""Monta um .ttf a partir da folha de alfabeto do Magic Mail."""
import sys, math
import numpy as np
sys.path.insert(0, sys.path[0] or ".")
from sheet import load, ROWS, DESCENDERS
from trace import contours, simplify, smooth, area, curves
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen

UPEM = 1000
CAP_UNITS = 700.0          # mesma convencao da CorreioMagico.ttf
SIDE_BEARING = 0.055       # em fracao da altura de caixa alta
DP1, DP2 = 0.35, 0.12       # tolerancias de simplificacao, em pixels da folha
CORNER, ROUNDS = 0.75, 0   # deteccao de canto e passadas de suavizacao (o ajuste de curvas ja alisa)
CURVE_TOL = 0.30           # erro maximo do ajuste de curvas, em pixels da folha
CORNER_DEG = 60.0          # a partir daqui um vertice conta como canto e nao como curva

def inside(point, poly):
    x, y = point; hit = False; n = len(poly)
    for i in range(n):
        x1, y1 = poly[i]; x2, y2 = poly[(i + 1) % n]
        if (y1 > y) != (y2 > y):
            if x < x1 + (y - y1) / (y2 - y1) * (x2 - x1): hit = not hit
    return hit

def outline(mask, tol1=None, tol2=None):
    """Contornos limpos do glifo, em pixels locais (y para baixo)."""
    tol1 = DP1 if tol1 is None else tol1
    tol2 = DP2 if tol2 is None else tol2
    loops = []
    for loop in contours(mask):
        pts = simplify([(float(x), float(y)) for x, y in loop], tol1)
        pts = smooth(pts, CORNER, ROUNDS)
        pts = simplify(pts, tol2)
        if len(pts) >= 3 and abs(area(pts)) > 1.5:
            loops.append(pts)
    return loops

def build(sheet_path, out_path, family):
    glyphs, bases = load(sheet_path)
    cap = glyphs["H"]["bottom"] - glyphs["H"]["top"] + 1
    xh = glyphs["n"]["bottom"] - glyphs["n"]["top"] + 1
    asc = glyphs["b"]["bottom"] - glyphs["b"]["top"] + 1
    desc = max(glyphs[c]["bottom"] - glyphs[c]["top"] + 1 for c in DESCENDERS if c in glyphs) - xh
    k = CAP_UNITS / cap
    print("px  caixa-alta=%d altura-x=%d ascendente=%d descendente=%d  ->  %.4f unidades/px" % (cap, xh, asc, desc, k))

    i_top_offset = glyphs["i"]["bottom"] - glyphs["i"]["top"]      # do pe ate o pingo

    def foot(ch, g):
        """Onde o pe do glifo deve cair, em pixels abaixo da base da linha."""
        h = g["bottom"] - g["top"] + 1
        if ch in DESCENDERS:      return h - xh          # topo na altura-x
        if ch == "j":             return h - i_top_offset - 1
        if ch in u",;":           return desc
        if ch == u"\u00b7":       return -(xh - h) / 2.0 - h + h  # centro na meia altura-x
        if ch == "*":             return h - cap
        if ch in u"()/@+-=":      return g["bottom"] - bases[g["row"]]   # a linha 7 veio no lugar
        return 0                                                         # pe na base

    pen_glyphs, metrics, cmap = {}, {}, {}
    order = [".notdef", "space"]
    pen = TTGlyphPen(None); pen_glyphs[".notdef"] = pen.glyph(); metrics[".notdef"] = (int(0.5 * UPEM), 0)
    pen = TTGlyphPen(None); pen_glyphs["space"] = pen.glyph(); metrics["space"] = (300, 0)
    cmap[0x20] = "space"

    extras = []
    for ch, g in sorted(glyphs.items(), key=lambda kv: ord(kv[0])):
        entry = make(ch, g, bases, foot, k, cap, xh)
        if entry is None: continue
        name, glyph, adv, lsb = entry
        order.append(name); pen_glyphs[name] = glyph; metrics[name] = (adv, lsb); cmap[ord(ch)] = name
        if ch == u"\u00b7": extras.append((".", g))

    # A folha traz um ponto alto (middle dot) e nenhum ponto final: o ponto e o mesmo desenho, pousado na base.
    for ch, g in extras:
        entry = make(ch, g, bases, lambda c, gg: 0, k, cap, xh)
        name, glyph, adv, lsb = entry
        order.append(name); pen_glyphs[name] = glyph; metrics[name] = (adv, lsb); cmap[ord(ch)] = name

    # Digitos com a mesma largura, para o contador do HUD nao dancar.
    digits = [cmap[ord(d)] for d in "0123456789" if ord(d) in cmap]
    if digits:
        wide = max(metrics[d][0] for d in digits)
        for d in digits:
            adv, lsb = metrics[d]
            metrics[d] = (wide, lsb + (wide - adv) // 2)

    ymax = max((max(y for c in g.coordinates for y in [c[1]]) for g in pen_glyphs.values() if g.numberOfContours), default=900)
    ymin = min((min(y for c in g.coordinates for y in [c[1]]) for g in pen_glyphs.values() if g.numberOfContours), default=-220)

    fb = FontBuilder(UPEM, isTTF=True)
    fb.setupGlyphOrder(order)
    fb.setupCharacterMap(cmap)
    fb.setupGlyf(pen_glyphs)
    fb.setupHorizontalMetrics(metrics)
    fb.setupHorizontalHeader(ascent=int(ymax), descent=int(ymin), lineGap=0)
    fb.setupNameTable({"familyName": family, "styleName": "Regular",
                       "uniqueFontIdentifier": family + " Regular",
                       "fullName": family, "psName": family.replace(" ", "") + "-Regular",
                       "version": "Version 1.000"})
    fb.setupOS2(sTypoAscender=int(ymax), sTypoDescender=int(ymin), sTypoLineGap=0,
                usWinAscent=int(ymax), usWinDescent=int(-ymin),
                sCapHeight=int(CAP_UNITS), sxHeight=int(round(xh * k)),
                achVendID="RAVS", fsType=0)
    fb.setupPost()
    fb.save(out_path)
    print("gravado", out_path, "-", len(cmap), "caracteres")
    return out_path

def make(ch, g, bases, foot, k, cap, xh):
    loops = outline(g["mask"])
    if not loops: return None
    drop = foot(ch, g)                       # pe, em pixels abaixo da base
    h = g["mask"].shape[0]
    pen = TTGlyphPen(None)
    ordered = []
    for pts in loops:
        depth = sum(1 for other in loops if other is not pts and inside(pts[0], other))
        ordered.append((pts, depth % 2))
    sb = SIDE_BEARING * cap
    for pts, hole in ordered:
        # y do bitmap (para baixo, 0 no topo) -> unidades da fonte (para cima, 0 na base)
        # y do bitmap (para baixo, 0 no topo) -> unidades da fonte (para cima, 0 na base)
        units = [((x + sb) * k, ((h - 1 - y) - drop) * k) for x, y in pts]
        if (area(units) > 0) != bool(hole):  # externo no sentido horario, furo no anti-horario
            units = units[::-1]
        start, segments = curves(units, CURVE_TOL * k, CORNER_DEG)
        pen.moveTo((round(units[start][0]), round(units[start][1])))
        for control, point in segments:
            if control is None: pen.lineTo((round(point[0]), round(point[1])))
            else: pen.qCurveTo((round(control[0]), round(control[1])), (round(point[0]), round(point[1])))
        pen.closePath()
    glyph = pen.glyph()
    adv = int(round((g["width"] + 2 * sb) * k))
    name = "uni%04X" % ord(ch)
    return name, glyph, adv, int(round(sb * k))

if __name__ == "__main__":
    build(sys.argv[1], sys.argv[2], sys.argv[3])
