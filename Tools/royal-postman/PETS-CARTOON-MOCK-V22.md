# Incubadora cartoon — revisão V22

Mock revisado em 21 de outubro de 2026 com a ferramenta integrada `image_gen`. Alvo inicial: V21; referência de construção: botão verde Jogar com marcações do usuário. O close-up dos nossos botões ilustra o cavado indevido atrás do texto.

## Construção exigida para produção individual

1. Cavidade na própria superfície bege, com borda creme e sombra interna discreta.
2. Borda dourada fixa acomodada nessa cavidade.
3. Folga visível entre borda e botão móvel.
4. Uma única face colorida convexa, com chanfro largo, relevo nítido e contraste de luz/sombra. Não criar placa ou cavado atrás do texto.
5. Texto e ícone acompanham a face móvel. No clique, reduzir a face/texto/ícone dentro da borda fixa, preservando a cavidade.
6. Dois botões com largura reduzida igualmente para acomodar cavidades, espaçamento e margens; manter alturas iguais e área de toque generosa.
7. Ícone à direita do texto, com altura igual ou menor que as letras. Quantidade menor no alto à direita do ícone.

O mock reduz a largura das ações, adiciona os dois encaixes no painel e ajusta os contadores e ícones. A fabricação individual deve conferir rigorosamente escala dos ícones, continuidade da face e relevo, pois o mock gerado é uma referência visual, não geometria final.

Entrega: `previews/pets-incubator-cartoon-v22.png`. PNG original, sem pós-processamento. V21 preservado. Sem integração no Unity ou implementação do clique nesta revisão.

## Prompt inicial

```text
Use case: precise-object-edit / ui-mockup.
Edit ONLY the full portrait "Mascotes" Magic Mail screen shown in the supplied images. The close-up GREEN JOGAR button with drawn arrows is the reference for physical construction; the close-up of our teal/purple action buttons shows the error to REMOVE. Keep our screen's existing cartoon style, palette, roundness, composition and all content.
Change ONLY the Incubar and Acelerar button construction and their icon/counter placement.

THE CORRECT PHYSICAL LAYERING, from outside to inside:
A) The shared BEIGE PANEL has TWO shallow sculpted sockets/recesses cut into its surface, one per button, subtle dark warm inner shadow against the panel and a clean cream lip. Each socket follows the existing button's rounded rectangular silhouette. This recess is OUTSIDE and BEHIND the gold rim.
B) A FIXED SUNNY-GOLD RIM sits neatly fitted into each socket.
C) Just INSIDE the gold rim there is a NARROW CONTINUOUS DARK SEPARATION GAP, allowing the independent colored push button to move/shrink when pressed. Keep this visible all around, no clutter.
D) Inside this gap is ONE SOLID RAISED CONVEX TEAL button for Incubar and ONE SOLID RAISED CONVEX PURPLE button for Acelerar. Both have a substantial WIDE SAME-COLOR BEVEL, bright broad upper/left shoulders and distinctly darker lower/right sidewall. Strong clearly visible rounded volume like the green Jogar example, with vivid highlights and colored shadows. NOT washed out. The center is a single smooth continuous convex colored face, tapering smoothly to its bevel. 
REMOVE the extra smaller inset rounded rectangle/pill/patch BEHIND EACH WORD. NO inner hollow, no depressed plaque around the label, no bordered inner face, no rectangular contour behind text. The words rest directly on the uninterrupted convex teal/purple face. The ONLY cavity is the socket in the beige panel outside the gold rim; the only inner dark gap is directly adjacent to the rim.

Maintain the two action buttons' existing equal outer WIDTH and HEIGHT, same positions and rounded corners. Keep the large cream labels exactly "Incubar" and "Acelerar". To the RIGHT of each word place its SMALL egg/hourglass icon: the ENTIRE icon height must be at most the visible capital-letter height, about 70-85% of its current height. Each x0 must be SMALLER than the main label, a compact quantity placed at the UPPER RIGHT of its icon, vertically raised slightly above the icon center (like a superscript to its right), NOT underneath the icon. Keep these groups within the button with comfortable padding; no overlap with the label or bevel.

Preserve literally everything else: header and its round buttons, reduced vertically stacked right tabs, standalone upper-left hourglass x0, empty teal incubator, arrows, Comum badge, progress 0 / 100, four eggs and labels/counts, spacing, straight egg shelf top, gold base exactly flush to bottom with no wood under it. Do not invent new elements or change the approved scene. No Vazia, no stars, no egg in capsule, no bottom accelerators section. Deliver ONE full portrait mock, same 933:1686 aspect ratio, clean smooth cartoon finish.

```

## Refinamento de largura, encaixes e face

Inclui a orientação posterior do usuário de reduzir a largura para dar espaço à cavidade.

```text
Use case: precise-object-edit.
Edit the attached full Mascotes screen, changing ONLY the two action buttons on the beige panel. All other pixels/content/composition should remain unchanged.

The user explicitly requires the buttons to be NARROWER so the panel has room for a real inset socket surrounding each button. Rebuild only this button row with the following clear physical construction:
- Reduce BOTH gold-rimmed buttons from about 410px wide to about 350px wide (about 15% narrower), keep their current height and roundness. Same width and height for both. Center the pair horizontally.
- Each narrower button sits inside a clearly visible recessed rounded-rectangle SOCKET SCULPTED INTO THE BEIGE PANEL: warm beige outer lip, darker warm inner cavity edge at top/left, light cream lower edge. There must be beige breathing room OUTSIDE each socket, including visible space between the two sockets. This is a recess in the beige surface, not a dark box behind text.
- Inside each socket, a sunny gold FIXED RIM. Just inside the gold is a narrow dark gap encircling the colored movable button.
- The colored push face is one continuous SOLID CONVEX button with strong broad colored bevel and clear depth. Left vivid teal, right vivid purple. Bright wide upper shoulder and dark lower sidewall.
- IMPORTANT REMOVE the currently visible EXTRA SMALLER rounded rectangle behind the labels. There must be NO smaller inset rectangle, NO recessed label area, NO nested colored plaque behind the words. A single uninterrupted raised curved face has the label printed directly on it. Think ONE convex colored cushion inside its rim, with a broad smooth gradient across the whole cushion. No interior contour line except the button's own outer bevel.

Label left exactly Incubar, right exactly Acelerar, heavy cream text. Slightly reduce the text size if needed for the narrower buttons.
Right-side egg/hourglass icons must be SMALL: entire icon height 42-45px compared to main letter height about 48px, NOT larger than the label. Quantity x0 is small (about 28px), at the icon's UPPER RIGHT, no quantity below the icon. Fit label+small icon+raised x0 comfortably inside the button. Both have matching layout.

Keep the empty incubator, background, header, tabs, progress track, eggs and all their counts exactly as they are. Gold egg-shelf base remains flush to bottom. No new UI. Produce the same full-screen portrait aspect ratio 933:1686. The real correction must be visible: narrower buttons, obvious beige inset sockets outside rims, clear rim-to-button gaps, strong convex relief, NO inset patch behind text, smaller icons.
```

## Refinamento dos ícones

```text
Use case: precise-object-edit. Make a TINY LOCAL edit to the attached Mascotes mock: shrink ONLY the egg icon inside Incubar and the hourglass icon inside Acelerar to 60% of their current size. They are currently still too tall. Each entire icon must be only about 38px tall on this 933px-wide screen, matching or slightly smaller than the height of the cream letters in Incubar/Acelerar. Center each icon vertically alongside its word. Move the existing small x0 next to the UPPER RIGHT of its respective small icon, a little raised, never underneath; retain x0 and fit comfortably. Do not change text size, button size, sockets, rims, colored relief, panel, or anything else. Preserve literally ALL other design and placement. Keep the clear beige inset sockets, fixed gold rims, inner separation gap and continuous convex colored button face. No inner recessed patch behind words. Same full-screen image and aspect ratio.
```

