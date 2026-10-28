# Incubadora cartoon — revisão visual V20

Revisão do mock V19 conforme pedido de 21 de outubro de 2026. Header e desenho da incubadora foram aprovados; manter linguagem cartoon, paleta da Home e acabamento.

## Alterações

- Abas quadradas com ícones empilhadas na lateral direita.
- Contador de aceleradores no canto superior esquerdo, sem seção inferior.
- Remoção de “Vazia”; progresso permanece.
- Incubar e Acelerar com dimensões iguais, ícones menores à direita e quantidade junto aos ícones.
- Prateleira de ovos mais baixa e com borda superior reta.
- Quatro raridades preservadas. Este mock mostra somente Comum; produção futura precisa de um modelo distinto de incubadora para cada raridade.

Entrega: `previews/pets-incubator-cartoon-v20.png`. Edição com a ferramenta integrada `image_gen`, sem pós-processamento dos pixels. Mock visual, sem alterações no runtime ou em assets de produção. V19 preservado.

## Prompt da revisão inicial

```text
Use case: precise-object-edit / ui-mockup.
EDIT THIS EXACT APPROVED MOCKUP. Keep its polished cartoon painting, colors, chunky broad chamfers, materials, typography, lighting, scene and overall identity. This is a targeted UI layout revision, NOT a redesign. Same portrait canvas/aspect ratio, no phone frame. The RED HEADER is APPROVED: preserve the title "Mascotes", round back/help controls, dimensions and gold edge exactly. The simple large EMPTY TEAL INCUBATOR is APPROVED: preserve its dome, knob, clear bell jar, paw badge, base, large gold left/right arrows and "Comum" nameplate. Keep chamber empty, no egg, stars or particles. Keep the four egg rarity designs and the sunny courtyard/postal workshop artwork.

Make ONLY these requested changes:
1. REMOVE the two large horizontal text tabs "Atual" / "Incubadora" under the header. Put them back on the RIGHT SIDE as a compact vertical pair, immediately below the header: two generous rounded SQUARE buttons in the same painted beveled style, each approximately square. "Atual" has a large cream paw icon, a small readable caption "Atual" underneath; "Incubadora" has a large simple gold/cream egg icon with a small readable caption "Incubadora" underneath. Retain purple for Atual and TEAL for selected Incubadora, with the existing simple yellow-gold rounded rim. Do not make a new horizontal tab row. They must remain thumb-readable and not cover the incubator.
2. COMPLETELY REMOVE the full-width cream "Aceleradores x0" section at the very bottom, including its background and word label. Instead show ONLY a small standalone BLUE/GOLD HOURGLASS ICON with an attached readable cream quantity "x0" at the UPPER LEFT immediately below the header. No wide resource panel and no "Aceleradores" word there. This counter should use the same hourglass design as the approved mock.
3. REMOVE ONLY the word "Vazia" from the cream control panel beneath the incubator. Retain the "0 / 100" progress track. Do not replace Vazia with another status or heading.
4. Make the two side-by-side action buttons EXACTLY THE SAME WIDTH AND HEIGHT. Both must be large, tall, equally prominent rounded rectangles, with the same substantial bevel/frame proportions as the approved mock. Left remains TEAL with the text exactly "Incubar"; right remains PURPLE with the text exactly "Acelerar". Preserve the clean style, border thickness and strong cream lettering.
5. On BOTH action buttons, MOVE the icon to the RIGHT of the text and make it clearly SMALLER than it was. The left button has its small egg icon on the right; the right button has its small hourglass icon on the right. Attach a small but clearly readable quantity "x0" to the bottom-right of EACH icon, like a resource count. The x0 belongs visually to the icon; it is NOT part of the main "Incubar" / "Acelerar" word. Example arrangement within each equal-size button: [ large label | small icon with x0 ]. Icons should not dominate or crowd the label. Do not leave any large icon on the left of either button.
6. MOVE THE ENTIRE FOUR-EGG DISPLAY SHELF DOWN to occupy the space freed by deleting the bottom accelerator section. Keep all FOUR eggs, their pedestals, colors, rarity names "Comum", "Raro", "Épico", "Lendário", and each "x0". Keep the teal common selection. Keep the modest centered red "Ovos" label.
7. The TOP GOLD RAIL of the egg shelf must now be a STRAIGHT LEVEL HORIZONTAL EDGE across the entire screen. Remove the curved downward-drooping/flared ends at the left and right sides. It should look like a simple solid straight rectangular menu rail, not a decorative bowl or curtain edge. The shelf should finish near the bottom safe margin, with NO separate accelerator section underneath and no large empty bottom band.
8. Rebalance only the spacing needed for these changes so the layout stays clean and comfortably full. Do not add new controls, banners, ornaments, tabs, currencies, labels or decorative details. Do not change the approved incubator design or header. Preserve the original sunlight and teal/red/plum/cream/yellow-gold palette.
There are four incubator types, one for each rarity, but show only the selected common incubator in this screen, as before. The four egg choices communicate the four rarities; do not put four incubators on the screen.

Quality: exact same visual design, only requested changes. Crisp high-quality cartoon painted UI; large mobile-readable controls and text, equal action button geometry, smaller right-side icon counts. NO stains, noise, extra jewelry, tiny illegible captions, distorted words or additional star effects. Deliver ONE revised complete mockup only.
```

## Prompt do ajuste final

Alvo: primeira revisão V20. Corrige empilhamento lateral e posição da prateleira.

```text
Use case: precise-object-edit / ui-mockup.
Edit target: the attached Magic Mail Mascotes mock. This is a MINIMAL LAYOUT CORRECTION ONLY. Keep this exact approved cartoon design, red header, title/back/help, sunny workshop illustration, empty teal glass incubator, its scale and position, arrows, Comum plaque, cream/red control panel, progress track 0 / 100, equally sized teal Incubar and purple Acelerar action buttons, small right-side item icons with x0 counters, four eggs and their names/counts, simple yellow-orange gold bevels. Do not redesign or add anything.

Correct ONLY these two placements:
1. The two square icon tabs are currently SIDE BY SIDE. Put them in ONE VERTICAL STACK on the RIGHT EDGE under the header: Atual on top, Incubadora underneath. Each is a compact square about 150x150 pixels on the 933px-wide image, with a 12px gap. Right margin about 25px. First top about 165px, second top about 327px. Scale the existing buttons proportionally to fit, retaining the same paw and egg icons and small captions. They must remain clear of the central incubator and right carousel arrow. Clear their old horizontal locations naturally using the existing workshop backdrop. Keep the standalone hourglass x0 in the upper LEFT unchanged.
2. The complete purple egg shelf including its straight horizontal gold top rail, centered Ovos label, all FOUR eggs and their rarity labels/counts and bottom gold rail must sit LOWER. Move this shelf downward about 85px, so its bottom gold rail ends at the bottom screen margin (about 10-15px), eliminating the currently empty wooden band below it. Maintain exactly four eggs Comum, Raro, Épico, Lendário. Retain the perfectly STRAIGHT horizontal gold top edge all the way across, no curved-down ends. Redistribute the small vertical space above the shelf naturally by giving the cream action panel a little more vertical breathing room; the action buttons remain exactly equal width AND height, aligned on the same baseline, with unchanged wording and small right icons/counters. Keep the central incubator and header unchanged.

No Vazia text, no bottom accelerators section, no stars, no egg inside the empty capsule, no additional elements, no phone frame. Preserve all other painting, palette, typography, proportions, smooth clean finish and design choices. Return only the corrected single full-screen portrait mock at the same aspect ratio.
```

