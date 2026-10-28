# Incubadora — estudo de camadas e placeholders V62

Direção de22/10/2026: usuário escolheu a última V61 (03.png, abas laterais). Antes de produzir os assets, pediu novo mock com todos os botões e a barra deixados sob sua autoria, usando placeholders; patinha separada da máquina; barra mostrando tempo; ovo realmente dentro; partículas interiores; e estudo de como cada incubadora adota o estilo.

## Entregas visuais

- [Mock da tela](previews/incubator-v62/screen.png): estado incubando, botões e barra cinza, ovo visível dentro do vidro, partículas, tempo restante00:42:18 ilustrativo.
- [Família de quatro incubadoras](previews/incubator-v62/incubator-family.png): Comum turquesa, Raro roxo, Épico vermelho, Lendário dourado; posição do botão indicada pelo mesmo círculo cinza independente em cada base.
- [Visualizar juntos](previews/incubator-v62/index.html).

Gerados com ImageGen integrado, copiados sem alterações de pixels, hashes verificados. São imagens de aprovação, ainda não exports de PNGs individuais em camadas. Nenhum prefab, regra de jogo, botão ou barra de produção foi alterado.

## Tempo

Valores conferidos no prefab atual e em PetsIncubationRules: Comum3600s (1h), Raro10800s (3h), Épico43200s (12h), Lendário172800s (48h). Permanecem inalterados. A futura barra representa o andamento temporal e deve exibir tempo restante, sem o texto0/100. A apresentação precisa suportar horas acima de24 para o Lendário; não usar um campo de horas que volte a zero a cada dia. Os valores no mock são ilustrativos. No estado vazio pode mostrar a duração total; durante incubação o restante; ao finalizar, Pronto. A aceleração contextual só aparece durante a incubação em andamento.

## Construção proposta para a produção

As quatro máquinas usam o mesmo princípio de camadas; manter cor e identidade da tampa/base de cada raridade. Não pintar ovo, efeito ou botão no corpo. Cada conjunto precisa de um canvas e pivô comuns, sem recortes independentes que percam o alinhamento; encaixes devem ser derivados do master aprovado e conferidos com/sem ovo.

Ordem de trás para frente:

1. Cenário, bancada e sombra de contato, separados da máquina.
2. Corpo traseiro: interior e piso de apoio do ovo; eventuais contornos traseiros do vidro.
3. Partículas de fundo dentro da máscara da cavidade.
4. Sprite independente do ovo, com âncora e escala adequadas ao interior.
5. Poucas partículas de frente, sem cobrir a patinha do ovo; mesma máscara interior.
6. Vidro frontal transparente e seus reflexos discretos. O centro deve preservar a leitura do ovo e do cenário.
7. Base/lábio frontal que encobre corretamente a borda inferior do ovo; tampa em sprite separado.
8. Botão de patinha criado pelo usuário, sobre a base. A pintura da base deve ser contínua por baixo: sem medalhão, aro circular, patinha ou cavidade circular incorporada.
9. Demais controles/textos, com botões e barra fornecidos pelo usuário.

Partículas: criar sprites de ponto de luz, pequeno brilho e halo suave em alpha real; usar os mesmos sprites com tintas por raridade. Camadas de efeito de trás e frente independentes, limitadas ao interior do vidro e desativáveis. O mock mostra uma aparência de efeito; não constitui animação ou efeito implementado. Preferir renderização de sprites UI sob máscara compatível com o Canvas para que nenhum ponto ultrapasse o vidro. Não assumir que um ParticleSystem comum respeitará o stencil de um UI Mask sem integração própria.

Ovos, cenário, prateleira, reflexos, tampa, base e efeitos são peças distintas; textos permanecem dinâmicos. Placeholder cinza não é proposta final de cor/formato do botão. A barra final e todos os botões são de autoria do usuário.

## Prompts completos e referências

### Mock com tempo, partículas e placeholders

Original: C:/Users/Tiago Carvalho/.codex/generated_images/01a15af4-fedb-77d3-b750-1054318794bd/exec-c86e7216-15a5-4907-9f33-6e1c6c93d769.png

Referências locais:
- C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/royal-postman/previews/incubator-v61/03.png

```text
Use case: ui-mockup. Create ONE portrait 9:16 full-screen mobile-game visual approval mock, refining the attached LAST SELECTED design (V61/3), not an asset export.
User owns ALL BUTTON ART and TIME BAR ART. For this mock ALL BUTTONS and the TIME BAR must be CLEAR SIMPLE NEUTRAL GRAY PLACEHOLDERS, not finished colorful artwork. Do not design/polish buttons. All other scenic/machine/shelf artwork retains the selected clean cartoon finish.
Preserve selected layout: calm sunlit stone arch/garden; large teal glass incubator front view on terracotta round counter; small side navigation on right; thin sunny gold divider with SMALL RED 'Incubadora' title tab just below machine; warm orange-red terracotta brick lower wall; four rarity eggs on a RED three-dimensional shelf, labels/counts; small hourglass stock x2 below, no card. Few background props, machine is focus. Professional clean bright rounded cartoon painting, smooth broad highlights, crisp shapes, vivid red, cream, sunny yellow/orange gold, turquoise and deep plum text. No realistic metal, stains/noise, faceted patches or excessive bloom.
IMPORTANT NEW LAYERING VISUAL:
Show active incubation, one white egg with cyan patches and golden paw INSIDE THE GLASS, resting above rear inner floor with its lower edge correctly hidden by the front lip. Show a restrained magical particle effect confined to jar interior: 8–12 softly glowing cyan/cream dots and 3–4 small four-point glints, mostly behind the egg and a few subtly in front, leaving egg face fully legible. No particles outside jar, no glitter cloud or huge halo. Egg should read inside transparent glass, with subtle front glass highlights over it and clear background behind. Keep glass center transparent, no opaque white fog.
The teal INCUBATOR PAINT must have a CONTINUOUS PLAIN FRONT BASE where paw control will be overlaid: NO BAKED PAW, NO BAKED GOLD MEDALLION, NO attached round gold socket. Put a clearly separate flat neutral GRAY CIRCLE PLACEHOLDER over that plain teal base at the previous large paw position, labelled simply 'Botão' in small dark text. This circle visually locates user-supplied paw control; it is not part of the incubator paint. No painted rim/paw icon. Its center and scale remain like selected large paw.
All other interactive controls: back at top left a simple gray circle with left arrow; close top right a gray circle with X; carousel chevrons simple gray circles; right side compact gray rectangle placeholders labelled 'Mascote' and 'Incubadora'; one SMALL gray round placeholder with hourglass glyph above/right of machine lid representing contextual accelerate while active. No colored/gold finished button treatment anywhere. Keep them sized/positioned for clear touch areas, not huge gray panels.
Under jar quiet small cream 'Comum · 1/4' caption; red/gold divider labelled 'Incubadora'. Below divider REPLACE the old value bar with one plain neutral GRAY TIME-BAR PLACEHOLDER, slight muted light-gray fill, centered readable text exactly 'Tempo restante · 00:42:18'. This is a Common incubation example within current1h duration, not a percentage or invented duration rule. NO '0 / 100', NO numeric score or percent. Plain small 'Incubando' below if needed, no instruction to select a new egg while occupied.
Inventory unchanged: exactly FOUR eggs on one red shelf, white/cyan Common 'Comum' 'x3', purple paw 'Raro' 'x1', red/orange paw 'Épico' 'x0', gold paw 'Lendário' 'x0'; small cream rarity tags along front shelf lip; no ornate individual pedestals. Footer hourglass stock 'x2' small and freestanding, no backplate.
No bottom rectangular action buttons. No new currency, rewards, locks, navbars, timer popups, layer diagrams, captions outside screen, watermark, hardware or extra panels. Output only the full screen mock. This is an intentional MIX of finished scenic illustration and neutral placeholders for user-supplied button/time-bar assets.
```

### Estudo das quatro incubadoras

Original: C:/Users/Tiago Carvalho/.codex/generated_images/01a15af4-fedb-77d3-b750-1054318794bd/exec-06208a4f-fe4b-4dd8-983a-e948b9582be6.png

Referências locais:
- C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/royal-postman/previews/incubator-v61/03.png
- C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsCartoon/pets-cartoon-v24-incubator-common.png
- C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsCartoon/pets-cartoon-v24-incubator-rare.png
- C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsCartoon/pets-cartoon-v24-incubator-epic.png
- C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsCartoon/pets-cartoon-v24-incubator-legendary.png

```text
Use case: ui-mockup / design study.
Create ONE clean LANDSCAPE 16:9 ART-DIRECTION BOARD showing how FOUR pet incubator rarity variants will adopt the approved NEW STYLE. This is a visual mock for approval, not production sprite exports.
Input1 (full Mascotes V61/3 mock) is the AUTHORITATIVE new rounded bell-jar silhouette, simple clean cartoon paint, large machine scale and calm finish. Input2 common teal, input3 rare purple, input4 epic red and input5 legendary gold are ONLY existing RARITY IDENTITY REFERENCES: preserve their distinct color and modest lid/profile cues, redraw them in the NEW style of input1. Do not copy old noisy edges, transparency artifacts or baked paw medallions.
Board: neutral warm light cream background, four evenly spaced FRONT-VIEW incubators in one horizontal row, full silhouettes visible with consistent perspective, equal overall body height and aligned bases, roomy gap. Names above exactly 'Comum', 'Raro', 'Épico', 'Lendário' in dark plum. Simple durations below each respectively '1h', '3h', '12h', '48h' (current configured total times, unchanged).
FOUR DESIGNS:
Comum: teal rounded lid and base, narrow sunny gold trims, simple golden ball cap.
Raro: rich purple lid/base, small soft rounded shoulder lobes like existing rare identity, same restrained gold trim.
Épico: saturated red lid/base, simple three rounded lobes at lid and modest rounded base feet, narrow gold trim, not a complicated crown.
Lendário: sunny gold/orange lid/base, a compact softly three-bumped crown-shaped cap, slight broad ornamental gold shoulder surfaces, no filigree/microdetails.
All use same clean bell-jar architecture and clear wide glass opening; compatible shared egg position, broad base front lip and aligned FRONT BUTTON ANCHOR. Smooth broad gradients, colored shadows, crisp rounded volumes, no stains, noisy texture, faceted paint, antique metal or excessive bloom.
INSIDE EACH transparent jar show its matching egg (white/cyan/gold paw, purple/gold paw, red/gold paw, gold/orange paw), unmistakably INSIDE: rear inner floor, egg, subtle front glass reflections, front lip occluding egg bottom slightly. A few restrained magical particle dots and glints INSIDE EACH ONLY: common cyan/cream, rare lavender/cream, epic warm coral/cream, legendary warm gold/cream. Effects never cover egg face or spill outside glass; keep center transparent, no fog/cloud.
VERY IMPORTANT USER ART OWNERSHIP: Do NOT DRAW ANY finished PAW BUTTON OR ANY OTHER BUTTON. Each machine's front base artwork remains completely plain, with NO BAKED PAW, NO gold medallion, NO attached round socket/housing. OVERLAY ONE IDENTICAL FLAT GRAY CIRCLE PLACEHOLDER at the SAME relative front-center position and same diameter on each base, small text 'Botão'. These gray discs only indicate the detached user-made paw button's location/fit. They must read as separate UI overlays, not material belonging to machine.
Small neutral footer line only: 'Vidro, ovo, partículas e botão em camadas separadas'. No top navigation, no screens, no time-bar artwork, no action buttons, no game props, no bottom inventory, no giant heading, no ornate pedestals, no phone hardware. Four designs must clearly belong to one family and work with the same layered construction. Output ONE board, not four separate files.
```

