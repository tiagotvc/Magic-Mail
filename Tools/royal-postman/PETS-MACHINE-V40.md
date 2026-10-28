# Mascotes — máquina integrada V40 / centro V42

Status: instalada no prefab real `Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab`, após aprovação explícita do mock V39. O usuário em seguida especificou o desenho dos botões pelo recorte do mock e pediu arte neutra com borda separada; pediu ainda ampliar um único chanfro, rejeitando V41 por duplicar o relevo.

## Composição

Carcaça vermelha com painel creme integrada à base da incubadora; sua região inferior encosta na seção original dos ovos. O corpo ocupa toda a largura física da tela, inclusive fora da área segura; apenas os ombros superiores afunilam. Sem laterais externas, moldura extra ao redor dos ovos, luzes de compartimentos ou glow adicionado.

Os ovos, pedestais, rail, brilho já existente, incubadoras, header e perfil Atual foram preservados por comparação semântica. Home e Navigation mantiveram os hashes originais. Níveis, estoque, preços, ações e Lendário48h continuam iguais.

Incubar e Acelerar agora têm470×230 unidades, com texto e ícones separados. A borda permanece fixa na frente; centro, texto, ícone e quantidade encolhem ao pressionar. O material `RoyalPostman/UI/NeutralPalette` colore o cinza usando o tint de cada Image e preserva os realces. Não usa emissão/bloom. `RoyalPostmanActionVisual.enabledTint` e `disabledTint` guardam as cores de cada estado.

## Arte

- Carcaça: `Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsMachineV40/pets-machine-housing-v40.png`
- Borda neutra: `Assets/RoyalAves/Art/UI/Button/RoyalPostman/NeutralV40/button-reference-rim-v40.png`
- Centro ativo: `Assets/RoyalAves/Art/UI/Button/RoyalPostman/NeutralV40/button-reference-center-v42.png`
- Centro original V40 mantido para histórico; V41 duplicado foi rejeitado e permanece apenas na pasta de prévias.

PNG originais copiados byte a byte da geração integrada image_gen. A região Sprite da carcaça exclui os98px superiores fora da silhueta; os pixels não foram editados. As camadas do botão usam regiões equivalentes centralizadas1796×580 e Sprite borders250/220/250/220, com Image Sliced e pixelsPerUnitMultiplier3.8 para preservar os detalhes ao aumentar a altura. A borda e o centro têm canvas nativos ligeiramente diferentes (1942/1944px); suas regiões centrais compensam1px de origem.

## Verificação

Renderizações e testes em `production/review/pets-machine-v40/`:360×640,390×844,720×1280,1080×1920, com/sem notch; carcaça cobre a largura e encosta no inventário; oito ações neutras em quatro páginas; mesmo tamanho; área de toque; borda fixa ao pressionar; incubar consome um ovo e acelerar consome um item; tempo ativo não sobrepõe botões; Lendário48h; salvar no editor restaura largura canônica1080 da carcaça. Centro V42 e palette foram novamente renderizados em720/390 nos estados pronto, vazio e pressionado. Testes usam recursos injetados em memória, sem escrever PlayerPrefs.

O instalador mantém todos os blocos do prefab fora do painel/ações e do novo flag `integratedMachineLayout`; ele não executa os builders históricos sobre a tela autoral.

## Prompt da carcaça

```
Production game UI sprite extracted from the approved attached mock. Create ONLY the RED UPPER MACHINE HOUSING WITH ITS EMPTY CREAM CONTROL PANEL. This is one independent asset to go behind separate incubator/button/progress sprites.

Use the approved mock as the exact art/style/material authority. Isolate the red sloping shoulders under the jar and the red front body around the cream action panel. Remove the jar, paw emblem, Comum plaque, progress track/text, both buttons, all icons/labels, egg section, Ovos rail, garden and header. Reconstruct the vacated regions cleanly.

Canvas about 1800 x 1250, wide landscape 1.44:1. The whole machine is cropped very close: below the shoulders its body extends past BOTH left/right image edges, with no visible outer vertical sidewall, gold perimeter rail, feet or outer bottom corners. At the top only, a narrower red mounting platform (roughly central 60% of canvas width) smoothly widens via simple sloping red shoulders to both canvas edges by 25% down. Transparent ABOVE that top silhouette. The full lower 75% is solid red machine front edge-to-edge. Straight red bottom edge flush to image bottom.

A single large EMPTY warm-cream control-panel face is integrated into this red body, occupying about y=32% to y=94% of the canvas, x=2% to x=98%, softly rounded corners and a neat simple red/gold contact trim like the mock. No inset cavities inside it, no button outlines, no progress groove, no text. Enough uninterrupted cream space for a separate progress bar above two large buttons.

Preserve our clean polished cartoon red/plum/cream/sunny-gold palette, simple broad rounded molded shapes and restrained bevel highlights. No wood, machine lights, glow, bloom, metal microtexture, particles, screws, extra ornaments or grime. Not a whole UI mock or a grey placeholder. Actual transparent background outside only the upper shoulder silhouette; otherwise the housing fills the canvas. Crisp full-resolution independent raster sprite, no external cast shadow.
```

## Prompt da borda neutra nova

```
The attached close-up shows two finished action buttons. Use ONLY the LEFT TEAL INCUBAR button as the exact shape, bevel and finish authority; the right button has the same construction. Produce a NEUTRAL GRAYSCALE EMPTY LAYER for this exact button design. Remove all label, egg, quantity and surrounding cream/red panel. No text or icons.

Keep the reference's crisp rounded-rectangle shape, same width top/bottom, identical corners, smooth broad raised face and clean colored-bevel construction. Convert all remaining color to neutral greys with no warm/cool cast. Pristine simple smooth shading, no mottling, cloudy stains, grain, noise, realistic metal detail or brush texture.

Canvas 1944 x 809. Place the complete original outer button footprint within x=104..1840 and y=49..761 on this canvas. Do not fill the whole canvas with the art or zoom it. The footprint has a moderate corner radius like the reference, NOT the previously over-rounded/tapered grey button. Straight parallel top and bottom sections and mirrored sides. This canvas/placement is required so the independent rim and center will align. Actual transparency outside the requested layer.
OUTPUT ONLY THE HOLLOW OUTER RIM: the reference's gold frame converted to light neutral silver-grey, including its restrained front highlight, darker lower thickness and narrow dark inner separation line. Every pixel inside its inner edge must be completely transparent. Preserve its reference thickness, not a heavier or ornate ring. Keep a crisp clean edge, no external shadow. No central button body.
```

## Prompt do centro original

```
The attached close-up shows two finished action buttons. Use ONLY the LEFT TEAL INCUBAR button as the exact shape, bevel and finish authority; the right button has the same construction. Produce a NEUTRAL GRAYSCALE EMPTY LAYER for this exact button design. Remove all label, egg, quantity and surrounding cream/red panel. No text or icons.

Keep the reference's crisp rounded-rectangle shape, same width top/bottom, identical corners, smooth broad raised face and clean colored-bevel construction. Convert all remaining color to neutral greys with no warm/cool cast. Pristine simple smooth shading, no mottling, cloudy stains, grain, noise, realistic metal detail or brush texture.

Canvas 1944 x 809. Place the complete original outer button footprint within x=104..1840 and y=49..761 on this canvas. Do not fill the whole canvas with the art or zoom it. The footprint has a moderate corner radius like the reference, NOT the previously over-rounded/tapered grey button. Straight parallel top and bottom sections and mirrored sides. This canvas/placement is required so the independent rim and center will align. Actual transparency outside the requested layer.
OUTPUT ONLY THE INNER BUTTON BODY: the reference's teal button converted to light/mid neutral grey, including its broad raised center and the entire surrounding bevel, as one solid button face. It must sit at its original position INSIDE the specified outer footprint, leaving the original frame area transparent. Remove the outer gold frame completely. Keep the center light enough for vivid tint coloring. Preserve the clean sculpted volume and bright upper bevel/darker lower bevel, without adding a separate little plaque or inset cavity. No rim or external shadow.
```

## V41 rejeitado

```
Edit this exact isolated neutral-grey button center to make a BEVEL-WIDER VARIANT.

Keep the outer silhouette, outer dimensions, corner shape, original position on the 1944×809 transparent canvas, neutral-grey material, clean shading and outer edge EXACTLY unchanged. It must still fit the existing separate rim.

Change ONLY the distance between that outer edge and the raised central plateau: increase the sloping bevel's width by about 75% on ALL FOUR SIDES. Reduce the size of the raised central plateau inward accordingly, leaving much more visible shoulder between the outside of the button body and the center face. The center remains one broad RAISED rounded-rectangle plateau, not a recessed cavity or small pill.

Use a single constant geometric bevel width around top, bottom, left and right. The bottom bevel must NOT be wider than the others. Concentric symmetric contours, identical four corners, no taper. A clear clean upper shoulder highlight and darker lower shoulder describe the stronger three-dimensional relief. Smooth controlled neutral-grey gradients, no mottling, grain, splotches, added shine marks or surface texture.

Only the button center, no outer rim, text, icon, drop shadow or background. Preserve all existing transparent areas and the original canvas/placement.
```

## Ajuste de tamanho V43

Após o usuário considerar os botões grandes demais, os oito controles Incubar/Acelerar receberam escala uniforme 0.85 no prefab real. O tamanho aparente passou de 470×230 para 399.5×195.5 unidades, preservando posições e proporções de borda, centro, texto e ícones. Somente essas oito escalas mudaram. Capturas nativas com estoque vazio e disponível: `production/review/pets-machine-v43/`.

## Centro V42: chanfro único

```
Edit this ORIGINAL neutral-grey button center. It currently has ONE narrow bevel around one broad raised central face.

Widen that SAME EXISTING bevel to exactly TWICE its current width, by moving its existing inner boundary inward a little. ERASE the old boundary completely. Do not retain the old highlight line and add another one. There must be exactly ONE sloping bevel and ONE broad raised face, with just ONE clean transition between them. No nested panel, no second chamfer, no separate central pill.

The outer body is approximately x157..1787 and y126..682 on the 1944x809 canvas. Use an approximately 85–90 pixel uniform inset from that outer edge to the new central plateau, rather than the current approximately45px. Thus the broad plateau should still span approximately x247..1697 and y216..592. This is a modest widening, not the oversized shoulder of the rejected variant.

Preserve the original outer silhouette, overall dimensions, corner character, canvas, position, neutral-grey shading and actual transparency. Uniform bevel width at top, bottom, left and right. One continuous smooth sloping shoulder, brighter above and darker below, meeting a single broad raised central face. No extra rim/outline or second highlight contour. No text, icons, shadow, grain or mottling.
```

