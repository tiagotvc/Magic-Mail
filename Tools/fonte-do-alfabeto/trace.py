"""Traca os glifos de uma folha PNG e devolve contornos em coordenadas de fonte."""
import numpy as np

def contours(mask):
    """Todos os contornos fechados da mascara binaria, em coordenadas de pixel (y para baixo)."""
    h, w = mask.shape
    m = np.zeros((h + 2, w + 2), bool)
    m[1:-1, 1:-1] = mask
    edges = {}
    ys, xs = np.nonzero(m)
    for r, c in zip(ys, xs):
        if not m[r - 1, c]: edges.setdefault((c + 1, r), []).append((c, r))
        if not m[r + 1, c]: edges.setdefault((c, r + 1), []).append((c + 1, r + 1))
        if not m[r, c - 1]: edges.setdefault((c, r), []).append((c, r + 1))
        if not m[r, c + 1]: edges.setdefault((c + 1, r + 1), []).append((c + 1, r))
    loops = []
    while edges:
        start = next(iter(edges))
        loop = [start]
        node = start
        while True:
            outs = edges.get(node)
            if not outs:
                break
            nxt = outs.pop()
            if not outs: del edges[node]
            if nxt == start:
                break
            loop.append(nxt)
            node = nxt
        if len(loop) >= 4:
            loops.append([(x - 1, y - 1) for x, y in loop])
    return loops

def _perp(p, a, b):
    (px, py), (ax, ay), (bx, by) = p, a, b
    dx, dy = bx - ax, by - ay
    n = dx * dx + dy * dy
    if n == 0: return ((px - ax) ** 2 + (py - ay) ** 2) ** 0.5
    return abs(dy * (px - ax) - dx * (py - ay)) / n ** 0.5

def simplify(points, tol):
    """Douglas-Peucker num anel fechado."""
    if len(points) < 4: return points
    # comeca num canto real para nao cortar um vertice verdadeiro
    n = len(points)
    best, bi = -1, 0
    for i in range(n):
        d = _perp(points[i], points[i - 2], points[(i + 2) % n])
        if d > best: best, bi = d, i
    pts = points[bi:] + points[:bi] + [points[bi]]

    keep = [False] * len(pts)
    keep[0] = keep[-1] = True
    stack = [(0, len(pts) - 1)]
    while stack:
        a, b = stack.pop()
        if b <= a + 1: continue
        far, fi = -1, a
        for i in range(a + 1, b):
            d = _perp(pts[i], pts[a], pts[b])
            if d > far: far, fi = d, i
        if far > tol:
            keep[fi] = True
            stack.append((a, fi)); stack.append((fi, b))
    out = [p for p, k in zip(pts, keep) if k]
    return out[:-1] if out[0] == out[-1] else out

def smooth(points, corner_cos=0.55, rounds=2):
    """Media movel que preserva cantos: so mexe onde a curva e suave."""
    pts = points
    for _ in range(rounds):
        n = len(pts)
        if n < 5: return pts
        out = []
        for i in range(n):
            a, p, b = pts[i - 1], pts[i], pts[(i + 1) % n]
            v1 = (p[0] - a[0], p[1] - a[1]); v2 = (b[0] - p[0], b[1] - p[1])
            n1 = (v1[0] ** 2 + v1[1] ** 2) ** 0.5; n2 = (v2[0] ** 2 + v2[1] ** 2) ** 0.5
            if n1 == 0 or n2 == 0:
                out.append(p); continue
            cos = (v1[0] * v2[0] + v1[1] * v2[1]) / (n1 * n2)
            if cos < corner_cos or n1 > 6 or n2 > 6:
                out.append(p)                     # canto de verdade, ou trecho reto longo
            else:
                out.append(((a[0] + 2 * p[0] + b[0]) / 4.0, (a[1] + 2 * p[1] + b[1]) / 4.0))
        pts = out
    return pts

def area(points):
    s = 0.0
    for i in range(len(points)):
        x1, y1 = points[i]; x2, y2 = points[(i + 1) % len(points)]
        s += x1 * y2 - x2 * y1
    return s / 2.0


# ---------- ajuste de curvas ----------
import math

def _unit(a, b):
    dx, dy = b[0] - a[0], b[1] - a[1]
    n = math.hypot(dx, dy)
    return (dx / n, dy / n) if n else (0.0, 0.0)

def corners(points, limit_deg=60.0):
    """Indices dos vertices que sao canto de verdade (e nao amostragem de uma curva)."""
    n = len(points); out = set()
    for i in range(n):
        v1 = _unit(points[i - 1], points[i]); v2 = _unit(points[i], points[(i + 1) % n])
        if v1 == (0.0, 0.0) or v2 == (0.0, 0.0): continue
        cos = max(-1.0, min(1.0, v1[0] * v2[0] + v1[1] * v2[1]))
        if math.degrees(math.acos(cos)) > limit_deg: out.add(i)
    return out

def _control(p0, t0, p1, t1):
    """Encontro das tangentes: o ponto de controlo da quadratica."""
    den = t0[0] * t1[1] - t0[1] * t1[0]
    if abs(den) < 1e-9: return None
    s = ((p1[0] - p0[0]) * t1[1] - (p1[1] - p0[1]) * t1[0]) / den
    if s <= 0: return None
    c = (p0[0] + t0[0] * s, p0[1] + t0[1] * s)
    # t1 aponta de p1 para dentro do trecho: o controlo tem de estar desse lado
    if (c[0] - p1[0]) * t1[0] + (c[1] - p1[1]) * t1[1] <= 0: return None
    return c

def _error(run, p0, c, p1):
    worst, at = 0.0, None
    for k, q in enumerate(run[1:-1], start=1):
        best = 1e9
        for i in range(21):
            t = i / 20.0; u = 1 - t
            x = u * u * p0[0] + 2 * u * t * c[0] + t * t * p1[0]
            y = u * u * p0[1] + 2 * u * t * c[1] + t * t * p1[1]
            d = (x - q[0]) ** 2 + (y - q[1]) ** 2
            if d < best: best = d
        best = best ** 0.5
        if best > worst: worst, at = best, k
    return worst, at

def _fit(run, tol, out):
    """Uma quadratica pelo trecho; se nao couber na tolerancia, parte ao meio do pior ponto."""
    p0, p1 = run[0], run[-1]
    if len(run) == 2:
        out.append((None, p1)); return
    c = _control(p0, _unit(run[0], run[1]), p1, _unit(run[-1], run[-2]))
    if c is not None:
        err, at = _error(run, p0, c, p1)
        if err <= tol:
            out.append((c, p1)); return
    else:
        err, at = tol + 1, len(run) // 2
    if at is None or at <= 0 or at >= len(run) - 1: at = len(run) // 2
    _fit(run[:at + 1], tol, out)
    _fit(run[at:], tol, out)

def curves(points, tol=0.30, limit_deg=60.0):
    """Anel fechado -> [(controlo ou None, ponto final)], pronto para moveTo/lineTo/qCurveTo."""
    n = len(points)
    if n < 4: return 0, [(None, p) for p in points[1:]]
    marks = sorted(corners(points, limit_deg))
    if not marks: marks = [0, n // 3, 2 * n // 3]
    segments = []
    for a, b in zip(marks, marks[1:] + [marks[0] + n]):
        run = [points[i % n] for i in range(a, b + 1)]
        _fit(run, tol, segments)
    return marks[0], segments
