# Botão neutro V37 — borda e centro separados

O usuário aprovou a construção V35, pediu a base neutra em preto/cinza para colorização e depois aprovou a separação em duas camadas V36. Pedido mais recente: limpar manchas nas duas peças preservando o relevo e o encaixe.

Assets:
- `Assets/RoyalAves/Art/UI/Button/RoyalPostman/NeutralV37/button-neutral-rim-v37.png`
- `Assets/RoyalAves/Art/UI/Button/RoyalPostman/NeutralV37/button-neutral-center-v37.png`

Dois PNGs originais da geração integrada image_gen, com alpha, importação Sprite Single, Full Rect, pivô central, sem mipmaps, sem compressão e limite8192. Não aplicados aos prefabs de telas.

Uso: mesmos RectTransform, posição e dimensões; centro atrás e borda na frente. A área do corpo ultrapassa discretamente a abertura do aro para fechar o encaixe. Tint de cada Image independente; tint usa os valores de cinza para preservar o volume. A borda pode ficar fixa enquanto o centro e seus futuros rótulos/ícones encolhem ao pressionar.

Apenas saturação não adiciona matiz a uma imagem cinza; usar cor/tint ou colorização. Não foi implementado shader ou controle novo neste trabalho.

Fontes da separação:
- Aro: exec-97e06075-b830-4bd7-8176-59ffe23a258d.png
- Centro: exec-b08d8eaa-72bb-4a94-ac08-1ad5aa2135d2.png

## Prompt da conversão neutra

```
Precise COLOR-ONLY edit of this exact approved button asset.

Convert the ENTIRE button to a completely neutral grayscale material for use as a reusable tintable game UI sprite. Remove ALL turquoise, green, cyan, yellow, gold, orange and brown color. Every visible part must be neutral grey, charcoal, silver-grey or neutral white, with absolutely no cool or warm color cast. This includes BOTH the central button body and the outer rim.

Preserve EXACTLY the approved shape, proportions, corner roundness, border thickness, raised center, broad bevel construction, lighting positions and clean cartoon finish. Do not redesign, reshape, flatten, add or remove details. Keep smooth simple shading and crisp boundaries.

Use a moderately LIGHT neutral grey for the main raised face so tint multiplication can create vivid colors in the game. Medium grey bevel, deep charcoal in the narrow separation gap and lower bevel shadow, light silver-grey rim, restrained near-white highlights. Keep the material volumes clearly legible. Do not make the main face black or very dark. No texture, mottling, noise, grain, brush marks or cloudy patches.

Same single EMPTY button with genuinely transparent background and a small even margin. No text, icons, new highlights, external shadow or additional elements. This is the SAME approved button, only changed into a neutral grayscale tint base.
```

## Prompt da separação do aro

```
Layer extraction from the attached approved neutral-grey game button. Output ONLY its OUTER SILVER/GREY RIM as a separate genuinely transparent PNG layer.

Keep the original 1944 x 809 canvas, original placement, exact outline, proportions, corner shape, thickness, neutral-grey shading and highlights. Do not crop, zoom, recenter the ring differently, redesign or redraw the finish. The ring must occupy exactly the same coordinates as in the source.

REMOVE the entire grey central button body, its raised plateau and colored-body bevel: every pixel INSIDE the inner dark separation line must be completely transparent. Keep the narrow dark separation line attached to the inner edge of the rim. Keep the complete outer rim including the lower grey thickness. All pixels outside the outer silhouette must also be completely transparent.

The result is ONE HOLLOW FRAME with a large genuinely transparent center, preserving the source's original neutral grayscale sculpted finish. No filled dark center, checkerboard, background, text, icon or added shadow.
```

## Prompt da separação do centro

```
Layer extraction from the attached approved neutral-grey game button. Output ONLY its INNER GREY BUTTON BODY as a separate genuinely transparent PNG layer.

Keep the original 1944 x 809 canvas, original placement, exact outline, proportions, corner shape, neutral-grey shading, smooth raised central plateau, highlight and broad grey bevel. Do not crop, zoom, stretch, recenter differently, redesign or change the finish. The body must occupy exactly the same coordinates as in the source so it aligns with the separate outer-rim layer.

REMOVE the entire outer silver/light-grey rim, its lower outer thickness and its narrow dark separation line. Keep the complete central grey button body INCLUDING the broad sloping bevel around the raised plateau, ending at the inside edge of the removed dark gap. Every pixel beyond that inner button-body silhouette must be fully transparent, including the area formerly occupied by the outer frame.

The result is ONE SOLID GREY RAISED BUTTON FACE with its bevel and no outer frame. Preserve the neutral grayscale lighting and clean finish; no extra outlines, background, checkerboard, text, icon or added shadow.
```

## Prompt da revisão do aro

```
Precise surface-cleanup edit of this exact approved neutral-grey game UI layer. Preserve its EXACT original 1944 x 809 canvas, position, shape, corner radii, silhouette and shading construction, so it still aligns with its companion layer. Do not crop, resize, zoom, shift or redesign.

Remove the visible mottled/cloudy patches, grey splotches, brush marks and grain. Repaint the existing material with a few perfectly smooth continuous NEUTRAL GREY color gradients, retaining the approved sculpted volume and existing broad highlights. No surface texture, no noise, no random local brightness fluctuations. Smooth controlled light-to-dark shading; crisp clean contour. Neutral R=G=B grey throughout, no warm/cool tint.

Keep all existing transparent areas fully transparent, clean the outer alpha fringe, and remove isolated pixel specks. No new outline, highlight, shadow, object, text, icon or background.
This layer is ONLY A HOLLOW RIM. Preserve the silver-grey front band, dark-grey lower thickness and narrow dark inner gap, but make each band pristine and uniformly smooth. Keep the entire large central hole transparent and keep the outside transparent. Do not fill the hole or change the band's thickness.
```

## Prompt da revisão do centro

```
Precise surface-cleanup edit of this exact approved neutral-grey game UI layer. Preserve its EXACT original 1944 x 809 canvas, position, shape, corner radii, silhouette and shading construction, so it still aligns with its companion layer. Do not crop, resize, zoom, shift or redesign.

Remove the visible mottled/cloudy patches, grey splotches, brush marks and grain. Repaint the existing material with a few perfectly smooth continuous NEUTRAL GREY color gradients, retaining the approved sculpted volume and existing broad highlights. No surface texture, no noise, no random local brightness fluctuations. Smooth controlled light-to-dark shading; crisp clean contour. Neutral R=G=B grey throughout, no warm/cool tint.

Keep all existing transparent areas fully transparent, clean the outer alpha fringe, and remove isolated pixel specks. No new outline, highlight, shadow, object, text, icon or background.
This layer is ONLY THE INNER BUTTON BODY. Preserve the broad raised light-grey central plateau and its broad darker sloping bevel. Make the plateau a smooth uniform light-to-mid-grey vertical ramp, with no cloudy areas across it. Keep the existing soft broad upper-shoulder highlight, smoothly transitioning into the face, and the darker lower bevel. No extra border: the outer rim is a separate asset. Keep every pixel beyond this body silhouette transparent.
```

São revisões de pintura, ainda sem aprovação final da limpeza. Não declarar ausência absoluta de artefatos apenas com base no prompt.

