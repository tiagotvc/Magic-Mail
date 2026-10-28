# Incubadora em camadas — produção V63

Montagem autorizada pelo usuário após aprovação do mock V62. Gerado com ImageGen integrado. Botões e barra de tempo são placeholders do Unity para substituição pelo usuário, incluindo a patinha: nenhum botão novo foi pintado.

## Arquivos

- Prefab montado: [Pets.prefab](../../Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab).
- [PNGs e sprites em camadas](../../Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsLayeredV63).
- [Capturas reais e verificação](production/review/pets-layered-v63/index.html).
- [Manifesto com todos os prompts e originais](production/pets-layered-v63-assets.json).
- Backup do prefab anterior: production/review/pets-layered-v63/before.prefab.

## Peças de arte

Nove PNGs originais preservados, sem alterações de pixels: cenário, bancada, prateleira vermelha, divisória com centro vermelho sem texto, quatro máquinas sem botão/ovo/efeito incorporado e brilho de partícula. Ovos e ampulheta existentes reutilizados.

Cada máquina possui sprites nativos independentes RearFloor, GlassFront, Lid e BaseFront, criados com retângulos do mesmo PNG1024x1536, preservando alinhamento e pixels. Os recortes não reamostram nem repintam a fonte. Os PNGs de máquina têm centro realmente transparente (alpha0 conferido). Não contêm a patinha ou cavidade circular incorporada. O vidro não deve ser pintado novamente junto do ovo ou dos efeitos.

## Editar no Editor de telas

Cada raridade fica em Body/Workshop/Incubator0 a Incubator3.

- Machine/RearFloor: piso interior e parte traseira.
- Machine/ParticlesBehind: efeitos atrás do ovo, sob máscara.
- Machine/Egg: ovo independente.
- Machine/ParticlesFront: poucos brilhos de frente, sob outra máscara.
- Machine/GlassFront: reflexos transparentes.
- Machine/Lid e Machine/BaseFront: tampa e base/lábio frontal.
- PawButton_PLACEHOLDER: botão independente da máquina. Substituir Image e/ou adicionar borda/centro do usuário, preservando componente Button e ligação useItem.
- Accelerate_PLACEHOLDER: atalho contextual durante incubação, com ícone existente.
- Available/TimeBar_PLACEHOLDER: imagem da barra do usuário; Fill_PLACEHOLDER e TimeRemaining são filhos, de modo que texto e preenchimento acompanham a geometria.

Header/Back e Header/Close são placeholders. CurrentTab/IncubatorTab trocam entre Mascote e Incubadora. Inventory contém ovos e estoque de ampulhetas; a quantidade de tempo acompanha seu ícone ao mover o conjunto. Todo desenho de controles novos fica sob autoria do usuário; os arquivos anteriores do usuário continuam no projeto e no backup, sem apagar artes.

## Comportamento

O círculo da patinha inicia incubação quando há estoque e o slot está liberado/vazio. Durante execução ele fica inativo; Acelerar aparece apenas enquanto há tempo restante. Estoque e preço em gold mantêm as regras atuais, com custo visível quando não há item. Não foram criadas novas regras de eclosão/recompensa. Vazio mostra duração total; incubando mostra tempo restante; ao finalizar mostra Pronto. Formato suporta48:00:00 sem voltar a zero após24h.

Tempos preservados:1h/3h/12h/48h. Travas preservadas do prefab atual: Comum0, Raro30, Épico100, Lendário0. Nenhuma alteração de balanceamento.

RoyalPostmanIncubatorParticles anima14 sprites UI por máquina ativa, divididos em frente/fundo, com máscara e sem raycast. Usa textura compartilhada e tintas por raridade; não gera partículas fora do vidro. Efeitos desligam no estado vazio, bloqueado ou concluído.

## Instalação e verificação

Construção/testes em Unity separado e oculto; a sessão do usuário não foi operada/reiniciada. O instalador faz merge auditado dos componentes, preserva IDs e blocos inalterados, confere que o prefab principal não mudou desde o início e exige PASS nativo.485 componentes de CurrentProfile foram mantidos exatamente, com mudança apenas da visibilidade inicial do painel para abrir na Incubadora. Home/Navigation devem manter os hashes originais.

Validação inclui quatro raridades; estoque e gold; estados vazio/incubando/concluído; navegação; clique/raycast; máscaras e ordem de camadas; contagem temporal e48h; safe areas720x1280,390x844 e360x640 com/sem recorte; redimensionamento e Save/reload no editor. Capturas são renders nativos, não mock do ImageGen. Quantidades e tempo das capturas são fixtures de teste, não o estoque do usuário.

## Prompts completos

### machine-common

Arquivo: Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsLayeredV63/machine-common.png

Original: C:/Users/Tiago Carvalho/.codex/generated_images/01a15af4-fedb-77d3-b750-1054318794bd/exec-26f2b0d9-a05d-461a-9055-06555894d674.png

```text
Production TRANSPARENT PNG for Unity layered sprites. Reference1 is approved four-rarity family; reference2 is approved screen. Extract/redraw ONLY ONE COMUM teal body and simple rounded teal dome lid with one small gold ball. Plain smooth teal front base. incubator, the 1th in reference1. Premium clean rounded cartoon mobile game painting, matching the approved reference exactly: sunny yellow gold with orange shadows, vivid teal/red accents, warm colored contact shadows, smooth broad highlights, crisp clean edges. No stains, grain, noisy texture, speckles, realistic metal, faceted shading, excessive glow or ornaments. Production art only: NO text, NO labels, NO UI controls, NO placeholders, NO buttons or medallions, NO egg, NO particles, NO watermark. Front orthographic perspective exactly like reference, isolated on TRUE TRANSPARENT alpha with generous small transparent margins, portrait canvas approximately1024x1536. Full complete bell-jar outer frame visible: lid, glass side reflections, rear oval inner floor, base and gold trim. GLASS INTERIOR AND ALL SPACE BETWEEN THE SIDE REFLECTIONS MUST BE GENUINELY TRANSPARENT, not solid white/gray/blue painted fill, no scene seen through it, no checkerboard baked in. Very subtle narrow translucent cyan/cream reflections along left/right edges only, center fully alpha0 so a separate egg remains fully visible. Lid occupies roughly upper25%, clear glass middle50%, broad base lower25%; consistent vertical architecture across rarities. One continuous unbroken PLAIN FRONT BASE behind the future separate user button: absolutely NO circle, paw, medallion, button-shaped socket or gray placeholder. Narrow gold horizontal trim and clean elliptical floor at top of base. Symmetric aligned silhouette, flat horizontal resting base. Do not include table/counter, ground plane or backdrop. This PNG will be split by native Unity Sprite rectangles into lid, transparent glass and base sections without repainting pixels, so use clean horizontal natural part boundaries.
```

### machine-rare

Arquivo: Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsLayeredV63/machine-rare.png

Original: C:/Users/Tiago Carvalho/.codex/generated_images/01a15af4-fedb-77d3-b750-1054318794bd/exec-927a3ff6-ca36-4ea7-98a0-736f599805c7.png

```text
Production TRANSPARENT PNG for Unity layered sprites. Reference1 is approved four-rarity family; reference2 is approved screen. Extract/redraw ONLY ONE RARO rich PURPLE body and rounded purple dome with two modest soft rounded shoulder lobes and one gold ball. Plain purple front base. incubator, the 2th in reference1. Premium clean rounded cartoon mobile game painting, matching the approved reference exactly: sunny yellow gold with orange shadows, vivid teal/red accents, warm colored contact shadows, smooth broad highlights, crisp clean edges. No stains, grain, noisy texture, speckles, realistic metal, faceted shading, excessive glow or ornaments. Production art only: NO text, NO labels, NO UI controls, NO placeholders, NO buttons or medallions, NO egg, NO particles, NO watermark. Front orthographic perspective exactly like reference, isolated on TRUE TRANSPARENT alpha with generous small transparent margins, portrait canvas approximately1024x1536. Full complete bell-jar outer frame visible: lid, glass side reflections, rear oval inner floor, base and gold trim. GLASS INTERIOR AND ALL SPACE BETWEEN THE SIDE REFLECTIONS MUST BE GENUINELY TRANSPARENT, not solid white/gray/blue painted fill, no scene seen through it, no checkerboard baked in. Very subtle narrow translucent cyan/cream reflections along left/right edges only, center fully alpha0 so a separate egg remains fully visible. Lid occupies roughly upper25%, clear glass middle50%, broad base lower25%; consistent vertical architecture across rarities. One continuous unbroken PLAIN FRONT BASE behind the future separate user button: absolutely NO circle, paw, medallion, button-shaped socket or gray placeholder. Narrow gold horizontal trim and clean elliptical floor at top of base. Symmetric aligned silhouette, flat horizontal resting base. Do not include table/counter, ground plane or backdrop. This PNG will be split by native Unity Sprite rectangles into lid, transparent glass and base sections without repainting pixels, so use clean horizontal natural part boundaries.
```

### machine-epic

Arquivo: Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsLayeredV63/machine-epic.png

Original: C:/Users/Tiago Carvalho/.codex/generated_images/01a15af4-fedb-77d3-b750-1054318794bd/exec-875803a1-de23-47bc-ae82-fb04bdf6d645.png

```text
Production TRANSPARENT PNG for Unity layered sprites. Reference1 is approved four-rarity family; reference2 is approved screen. Extract/redraw ONLY ONE ÉPICO saturated RED body and dome with three soft rounded cap lobes, small gold balls and modest rounded red feet at outer base corners. Plain central red base. incubator, the 3th in reference1. Premium clean rounded cartoon mobile game painting, matching the approved reference exactly: sunny yellow gold with orange shadows, vivid teal/red accents, warm colored contact shadows, smooth broad highlights, crisp clean edges. No stains, grain, noisy texture, speckles, realistic metal, faceted shading, excessive glow or ornaments. Production art only: NO text, NO labels, NO UI controls, NO placeholders, NO buttons or medallions, NO egg, NO particles, NO watermark. Front orthographic perspective exactly like reference, isolated on TRUE TRANSPARENT alpha with generous small transparent margins, portrait canvas approximately1024x1536. Full complete bell-jar outer frame visible: lid, glass side reflections, rear oval inner floor, base and gold trim. GLASS INTERIOR AND ALL SPACE BETWEEN THE SIDE REFLECTIONS MUST BE GENUINELY TRANSPARENT, not solid white/gray/blue painted fill, no scene seen through it, no checkerboard baked in. Very subtle narrow translucent cyan/cream reflections along left/right edges only, center fully alpha0 so a separate egg remains fully visible. Lid occupies roughly upper25%, clear glass middle50%, broad base lower25%; consistent vertical architecture across rarities. One continuous unbroken PLAIN FRONT BASE behind the future separate user button: absolutely NO circle, paw, medallion, button-shaped socket or gray placeholder. Narrow gold horizontal trim and clean elliptical floor at top of base. Symmetric aligned silhouette, flat horizontal resting base. Do not include table/counter, ground plane or backdrop. This PNG will be split by native Unity Sprite rectangles into lid, transparent glass and base sections without repainting pixels, so use clean horizontal natural part boundaries.
```

### machine-legendary

Arquivo: Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsLayeredV63/machine-legendary.png

Original: C:/Users/Tiago Carvalho/.codex/generated_images/01a15af4-fedb-77d3-b750-1054318794bd/exec-befc952e-15fe-4b49-8ed2-73c212a68cf7.png

```text
Production TRANSPARENT PNG for Unity layered sprites. Reference1 is approved four-rarity family; reference2 is approved screen. Extract/redraw ONLY ONE LENDÁRIO sunny GOLD/ORANGE body and compact gently three-bumped crown cap, broad simple golden shoulder surfaces. Plain central golden base, no filigree. incubator, the 4th in reference1. Premium clean rounded cartoon mobile game painting, matching the approved reference exactly: sunny yellow gold with orange shadows, vivid teal/red accents, warm colored contact shadows, smooth broad highlights, crisp clean edges. No stains, grain, noisy texture, speckles, realistic metal, faceted shading, excessive glow or ornaments. Production art only: NO text, NO labels, NO UI controls, NO placeholders, NO buttons or medallions, NO egg, NO particles, NO watermark. Front orthographic perspective exactly like reference, isolated on TRUE TRANSPARENT alpha with generous small transparent margins, portrait canvas approximately1024x1536. Full complete bell-jar outer frame visible: lid, glass side reflections, rear oval inner floor, base and gold trim. GLASS INTERIOR AND ALL SPACE BETWEEN THE SIDE REFLECTIONS MUST BE GENUINELY TRANSPARENT, not solid white/gray/blue painted fill, no scene seen through it, no checkerboard baked in. Very subtle narrow translucent cyan/cream reflections along left/right edges only, center fully alpha0 so a separate egg remains fully visible. Lid occupies roughly upper25%, clear glass middle50%, broad base lower25%; consistent vertical architecture across rarities. One continuous unbroken PLAIN FRONT BASE behind the future separate user button: absolutely NO circle, paw, medallion, button-shaped socket or gray placeholder. Narrow gold horizontal trim and clean elliptical floor at top of base. Symmetric aligned silhouette, flat horizontal resting base. Do not include table/counter, ground plane or backdrop. This PNG will be split by native Unity Sprite rectangles into lid, transparent glass and base sections without repainting pixels, so use clean horizontal natural part boundaries.
```

### environment

Arquivo: Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsLayeredV63/environment.png

Original: C:/Users/Tiago Carvalho/.codex/generated_images/01a15af4-fedb-77d3-b750-1054318794bd/exec-763127de-5cf8-446c-98b9-b32b44e3004d.png

```text
Production opaque background painting ONLY from the approved screen. Premium clean rounded cartoon mobile game painting, matching the approved reference exactly: sunny yellow gold with orange shadows, vivid teal/red accents, warm colored contact shadows, smooth broad highlights, crisp clean edges. No stains, grain, noisy texture, speckles, realistic metal, faceted shading, excessive glow or ornaments. Production art only: NO text, NO labels, NO UI controls, NO placeholders, NO buttons or medallions, NO egg, NO particles, NO watermark. Portrait9:16 full bleed. Remove the incubator COMPLETELY and reconstruct the empty area behind it. Also REMOVE all counter/pedestal, navigation, shelf, divider/title, progress, inventory eggs and hourglass. Upper55%: calm wide sunny tan stone garden arch with a few large green leaves at edges, low-contrast bright blue sky and soft distant royal postal garden/castle centered far away. No foreground props, parcels, lanterns, books, quills, satchels or characters. Lower45%: seamless warm orange-red TERRACOTTA BRICK WALL in a few large smooth clean cartoon blocks with restrained mortar joints, same color as approved mock. A simple horizontal stone sill transitions scene to lower wall at55% height; no gold rail or red label (separate asset). Center should be calm with no distinctive prop because machine is layered over it. Bottom all warm terracotta wall, no shelf or heavy floor. Beautiful crisp source painting with restrained background contrast, no UI or any text.
```

### counter

Arquivo: Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsLayeredV63/counter.png

Original: C:/Users/Tiago Carvalho/.codex/generated_images/01a15af4-fedb-77d3-b750-1054318794bd/exec-926c8b79-cac3-4b32-b42b-7304a51ae4bc.png

```text
Isolate/redraw ONLY the round warm TERRACOTTA stone counter/pedestal under the incubator in approved reference. Premium clean rounded cartoon mobile game painting, matching the approved reference exactly: sunny yellow gold with orange shadows, vivid teal/red accents, warm colored contact shadows, smooth broad highlights, crisp clean edges. No stains, grain, noisy texture, speckles, realistic metal, faceted shading, excessive glow or ornaments. Production art only: NO text, NO labels, NO UI controls, NO placeholders, NO buttons or medallions, NO egg, NO particles, NO watermark. Landscape approximately3:1 isolated TRUE TRANSPARENT PNG. One broad shallow elliptical countertop with a clean upper oval plane and short vertical front wall made from a few large smooth rounded clay stone segments. Straight symmetrical perspective front view, low thick platform like approved reference, no tall stand. Top plane is warm orange with broad soft lighting. NO machine, glass, buttons, plaque, label, text, plants, eggs, shelf, brick background or ground. No wide detached shadow cloud, only a tight subtle dark-orange underside contact edge. Full silhouette and small transparent margin. Canvas is filled mainly by this single low slab.
```

### shelf

Arquivo: Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsLayeredV63/shelf.png

Original: C:/Users/Tiago Carvalho/.codex/generated_images/01a15af4-fedb-77d3-b750-1054318794bd/exec-e0f990d0-1d32-4a30-8257-6ee83d637167.png

```text
Production isolated RED THREE-DIMENSIONAL EGG SHELF ONLY matching approved reference. Premium clean rounded cartoon mobile game painting, matching the approved reference exactly: sunny yellow gold with orange shadows, vivid teal/red accents, warm colored contact shadows, smooth broad highlights, crisp clean edges. No stains, grain, noisy texture, speckles, realistic metal, faceted shading, excessive glow or ornaments. Production art only: NO text, NO labels, NO UI controls, NO placeholders, NO buttons or medallions, NO egg, NO particles, NO watermark. Landscape about4:1, true transparent alpha. Wide straight simple red painted shelf spanning horizontal screen, visible slim top plane and vertical red front lip; two very short understated supports below outer ends. Symmetric front view, horizontal parallel long edges, modest softly rounded corners. One broad clean red highlight on top and darker red underside, no tiny grain. Absolutely EMPTY: no eggs, rarity tags, cream capsules, counts, text, buttons, wall, floor or decor. Leave front lip continuous plain red, tags added separately. Whole shelf visible with small transparent margin, ready for native Sprite sizing.
```

### divider

Arquivo: Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsLayeredV63/divider.png

Original: C:/Users/Tiago Carvalho/.codex/generated_images/01a15af4-fedb-77d3-b750-1054318794bd/exec-2566d74a-844b-4231-a7d8-8f5c224d8a99.png

```text
Production PNG of ONLY the SIMPLE SUNNY GOLD HORIZONTAL DIVIDER with SMALL RED CENTRAL TAB from approved reference. Premium clean rounded cartoon mobile game painting, matching the approved reference exactly: sunny yellow gold with orange shadows, vivid teal/red accents, warm colored contact shadows, smooth broad highlights, crisp clean edges. No stains, grain, noisy texture, speckles, realistic metal, faceted shading, excessive glow or ornaments. Production art only: NO text, NO labels, NO UI controls, NO placeholders, NO buttons or medallions, NO egg, NO particles, NO watermark. Very WIDE landscape approximately5:1, transparent alpha outside artwork. One straight narrow yellow-gold rail spans canvas width. At center an integrated LOW RED tab with small gently rounded upper corners rises above rail, thin sunny gold edge, no curls, no scroll ends. Center tab about30% width, rail height about25% of tab total height. Straight continuous lower orange-gold edge. NO title lettering (text separate), no shadows floating far below, no icons, no buttons, no machine, scenery or bricks. Restrained clean mobile-game paint, preserve the compact space-efficient shape, not an ornate plaque. Full silhouette edge-to-edge with tiny transparent margins.
```

### particle-star

Arquivo: Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsLayeredV63/particle-star.png

Original: C:/Users/Tiago Carvalho/.codex/generated_images/01a15af4-fedb-77d3-b750-1054318794bd/exec-a83ccd6b-3535-4b98-a07a-87d14546bd1c.png

```text
Production small VFX sprite: ONE soft rounded four-point magical sparkle, white/cream center, short cyan-white gradient rays and small feathered falloff. True transparent alpha background. Square canvas512x512, sparkle centered, fills central60% with completely transparent outer margins. Clean smooth light sprite suitable for runtime tint to cyan/purple/red/gold. No surrounding particles, noise, rings, scene, icon border, text, hard rectangular haze or baked checkerboard. This is a luminous particle texture, not a button.
```

