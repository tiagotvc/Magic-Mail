# Fonte a partir da folha de alfabeto

Transforma uma folha PNG de glifos (feita no gerador de imagem) num `.ttf` de verdade — o mesmo caminho
da `CorreioMagico.ttf`.

```
pip install fonttools pillow numpy
python build_font.py "<folha>.png" ..\..\Assets\RoyalAves\Fonts\MagicMail.ttf "Magic Mail"
```

Depois, no Unity, o menu de fontes recria `MagicMail SDF` e os dois materiais (`MagicMail Numeros`,
`MagicMail Textos`) — apague os `.asset`/`.mat` antigos para forçar.

## Como funciona

1. `sheet.py` corta a folha em glifos. **O gerador de imagem entrega os glifos desalinhados na vertical**
   (até 35 px numa caixa-alta de 135), por isso cada um é reposicionado pela regra tipográfica da sua
   classe — pé na linha de base, topo na altura-x para `g p q y`, etc. As *alturas* vieram certas; só a
   posição variava.
2. `trace.py` extrai os contornos (com furos), simplifica com Douglas-Peucker, separa cantos de curvas
   pelo ângulo e ajusta Béziers quadráticas por encontro de tangentes.
3. `build_font.py` monta o `.ttf`: caixa-alta em 700 unidades (como a CorreioMagico), dígitos todos com a
   mesma largura (para o contador do HUD não dançar) e o ponto final criado a partir do ponto alto da
   folha, pousado na base.

## Aferição

O construtor foi afinado contra a própria folha: renderiza-se o `.ttf` e compara-se glifo a glifo com o
bitmap de origem (IoU). Valores actuais: **médio 0,973 · mínimo 0,940**, contra um piso de ruído da
medição de 0,988 — ou seja, perto do máximo que a medição consegue distinguir.

As folhas de "números" e de "textos" são as **mesmas letras**: a de números é só a versão com contorno
(IoU entre elas ≈ 0,91, contra 0,75 se comparada com o contorno incluído). Por isso há uma só fonte, e os
dois visuais são materiais do TextMeshPro em `RoyalAvesFonts`.
