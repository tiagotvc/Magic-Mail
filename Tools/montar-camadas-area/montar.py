# -*- coding: utf-8 -*-
"""Monta as camadas de melhoria de uma area a partir das folhas de objectos.

    python montar.py "<pasta dos assets>" "<pasta de saida>"

Cada folha (01_..., 02_...) traz varios objectos soltos. O script separa-os, redimensiona e posiciona
cada um segundo posicoes.json, e grava UMA imagem do tamanho do fundo por melhoria - que e o que o
AreaTask.layer espera ("Imagem do tamanho do fundo, com o movel ja na posicao final").

Grava tambem previa-composta.png, com o fundo e todas as camadas por cima, para conferir a olho.
"""
import io, json, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from split import objects

SRC = sys.argv[1] if len(sys.argv) > 1 else r"C:\Users\Tiago Carvalho\Downloads\assets_background_magic_mail"
OUT = sys.argv[2] if len(sys.argv) > 2 else "../../Assets/RoyalAves/Art/Backgrounds/Agencia"
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, OUT)) if not os.path.isabs(OUT) else OUT
os.makedirs(OUT, exist_ok=True)

def icon(layer, path, side=256, margin=0.06):
    """Icone da melhoria: o que a camada desenhou, recortado e centrado num quadrado."""
    a = np.array(layer)[..., 3] > 25
    if not a.any(): return
    ys, xs = np.nonzero(a)
    cut = layer.crop((int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1))
    room = int(side * (1 - 2 * margin))
    k = min(room / cut.size[0], room / cut.size[1])
    cut = cut.resize((max(1, int(cut.size[0] * k)), max(1, int(cut.size[1] * k))), Image.LANCZOS)
    square = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    square.alpha_composite(cut, ((side - cut.size[0]) // 2, (side - cut.size[1]) // 2))
    square.save(path)

plan = json.load(io.open(os.path.join(HERE, "posicoes.json"), encoding="utf-8"))
bg = Image.open(os.path.join(SRC, plan["fundo"])).convert("RGBA")
W, H = bg.size
bg.save(os.path.join(OUT, "bg-agencia-sem-upgrades.png"))
print("fundo %dx%d" % (W, H))

preview = bg.copy()
for camada in plan["camadas"]:
    sheet = np.array(Image.open(os.path.join(SRC, camada["ficheiro"])).convert("RGBA"))
    found = objects(sheet, min_area=2500, gap=2)
    wanted = camada["objectos"]
    if len(found) != len(wanted):
        print("  AVISO %s: encontrei %d objecto(s), posicoes.json descreve %d" %
              (camada["ficheiro"], len(found), len(wanted)))
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for (piece, _, _), spot in zip(found, wanted):
        got = "%dx%d" % (piece.shape[1], piece.shape[0])
        if spot.get("esperado") and spot["esperado"] != got:
            print("  AVISO %s/%s: esperava %s e veio %s (a ordem pode ter mudado)" %
                  (camada["nome"], spot["o_que"], spot["esperado"], got))
        pw = max(1, int(round(spot["w"] * W)))
        ph = max(1, int(round(pw * piece.shape[0] / piece.shape[1])))
        img = Image.fromarray(piece).resize((pw, ph), Image.LANCZOS)
        x = int(round(spot["cx"] * W - pw / 2.0))
        y = int(round(spot["cy"] * H - ph / 2.0))
        layer.alpha_composite(img, (max(0, x), max(0, y)) if x >= 0 and y >= 0 else (x, y)) \
            if x >= 0 and y >= 0 else layer.paste(img, (x, y), img)
        print("  %-26s %-28s %4dx%-4d em (%4d,%4d)" % (camada["nome"], spot["o_que"], pw, ph, x, y))
    layer.save(os.path.join(OUT, camada["nome"] + ".png"))
    preview.alpha_composite(layer)
    icon(layer, os.path.join(OUT, "icone-" + camada["nome"] + ".png"))

preview.convert("RGB").save(os.path.join(HERE, "previa-composta.png"))
print("\ncamadas em %s\nprevia em %s" % (OUT, os.path.join(HERE, "previa-composta.png")))
