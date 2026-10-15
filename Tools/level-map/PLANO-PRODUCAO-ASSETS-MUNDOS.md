# Plano de produção — novos assets, animação e Natal
Data: 13/10/2026. **Todos os itens deste backlog estão planejados; ainda não gerados.**
Catálogo base já existente: MapKitV2, seleção `recommended:true`.
Fonte desta lista: `asset-production-backlog.json`.

## Como ler o backlog

P0: primeiro lote Green + Natal/comum. P1: expansão Green e produção Lava + Natal Lava. P2: reserva pré-histórica.
Uma “variante” pode ser PNG, peça articulada, receita de malha, material ou configuração de partículas. Não confundir a soma com quantidade de imagens. Exportar preferencialmente PNG separado por peça, com nomes estáveis; evitar atlas apertado que corte poses.

Destino proposto para os novos arquivos: `Assets/RoyalAves/Art/MapKitV3/{Shared,GreenLand,LavaLand,Christmas,Prehistoric}`. **Essas pastas/arquivos são proposta; não foram criados nesta atualização.** Reutilizar bases MapKitV2 pelos seus caminhos reais.

## Comum aos mundos

| Família / ID | Prioridade | Formato | Variantes / peças |
|---|---|---|---|
| shared-clouds | P0 | transparent-png | puff-small, puff-wide, cloud-strip |
| shared-mail-floaters | P0 | transparent-png | envelope-cream, envelope-purple, envelope-teal, postcard, parcel-small |
| shared-particle-masks | P0 | transparent-png | smoke-soft-a, smoke-soft-b, steam-soft, spark-dot, snowflake-soft |
| shared-route-passage-kit | P0 | mesh-recipe | narrow-plank-bridge, arched-postal-bridge, stepping-platforms, wide-gate |
| shared-river-surface | P0 | material-recipe | water-flow, magical-letter-current |

**shared-clouds:** Nuvens de massas grandes e simples, claras com sombra suave. Movimento: `slow-drift-loop`. Camadas de céu; deslocamento e parallax discretos, loop fora da área visível. Não comprar uma folha de frames para cada nuvem.

**shared-mail-floaters:** Cartas e pequenas encomendas visíveis em rios, alinhadas à paleta Magic Mail. Movimento: `flow-path-bob`. Fluxo sobre água ou magia; separar da interface e limitar densidade perto dos níveis. Encaixes: giftRibbon, snowPatch.

**shared-particle-masks:** Máscaras cartoon limpas para fumaça, vapor, brasas e neve. Movimento: `particle-system`. Texturas simples, animação no motor; não gerar vídeo. Glows somente em partículas luminosas.

**shared-route-passage-kit:** Passagens com espessura, laterais e encaixes no terreno. Movimento: `static`. Modelos/cartões de acabamento a combinar com cores por mundo; número do nível sobre superfície real. Encaixes: edgeSnow, garland, poleFlag.

**shared-river-surface:** Superfície separada do chão, duas ou três cores largas e linhas de fluxo. Movimento: `flow-offset`. Materiais do mundo, sem atlas de texturas rejeitado; cartas flutuantes são objetos independentes.

## Green Land — acréscimos

| Família / ID | Prioridade | Formato | Variantes / peças |
|---|---|---|---|
| green-letter-factory | P0 | transparent-png-parts | main-building, chimney, side-letter-chute |
| green-sorter-machine | P0 | transparent-png-parts | machine-body, stamp-arm, conveyor-wheel |
| green-postal-fountain | P0 | transparent-png | envelope-fountain, seal-fountain |
| green-river-details | P0 | transparent-png | mail-reed, seal-islet, postal-dock |
| green-route-landmarks | P1 | transparent-png | postal-gate, mail-bag-arch, covered-postal-stop |
| green-dog-animation-parts | P0 | transparent-png-parts | dog-body-master, dog-tail, dog-envelope |
| green-flags-and-banners | P1 | transparent-png | envelope-flag, postal-bunting |
| green-garden-props | P1 | transparent-png | envelope-bench, seal-topiary, parcel-cart |
| green-winter-accents | P1 | transparent-png | postal-snowman, gift-sleigh |

**green-letter-factory:** Fábrica postal simples: corpo creme, telhado roxo, grande envelope dourado e tubo teal. Movimento: `building-static-chimney-smoke`. Construção de poucas massas, sem tijolos miúdos; reaproveitar fábrica anterior como referência e diferenciar fachada. Encaixes: roofSnow, chimneySnow, garland, doorWreath.

**green-sorter-machine:** Máquina de carimbar correspondências com rosto/forma simpática de poucos elementos. Movimento: `stamp-cycle-and-wheel`. Braço e roda independentes; cartas chegam por caminho próprio; sem animação frenética. Encaixes: machineSnow, redBow.

**green-postal-fountain:** Fontes estáticas de bacia arredondada e envelope/selo central, água ilustrada. Movimento: `static`. Não usar partículas, jatos animados, ondulação nem pulse nesta fonte. Encaixes: rimSnow, redBow.

**green-river-details:** Juncos com cartas, ilhota-selo e pequeno píer; complementam o rio de cartas. Movimento: `static`. Píer precisa de geometria simples para mudança de vista; estas imagens são fachada/decoração. Encaixes: edgeSnow, garland.

**green-route-landmarks:** Portão, arco de sacola de correio e parada coberta; marcam transições de região. Movimento: `static`. Não substituir geometria navegável pela pintura do arco. Encaixes: roofSnow, archGarland.

**green-dog-animation-parts:** Peças derivadas do cachorro existente para gesto de cauda sem pular entre corpos. Movimento: `tail-sway-occasional`. Preservar identidade exata; não redesenhar cachorro diferente. As quatro poses existentes continuam para gestos. Encaixes: headHat, neckScarf.

**green-flags-and-banners:** Bandeiras e bandeirolas com ícones postais, sem texto pequeno. Movimento: `gentle-sway`. Separar tecido e suporte ou deformação pequena com pivô preso. Encaixes: poleSnow, seasonRibbon.

**green-garden-props:** Banco-envelope, topiária de selo e carrinho de pacotes. Movimento: `static`. Poucos contornos; bancos/cartinhos fora da faixa interativa. Encaixes: topSnow, redBow.

**green-winter-accents:** Boneco de neve carteiro e trenó de presentes como objetos exclusivos do Natal. Movimento: `static`. São additions sazonais; não substituir cachorro/agência permanentes. Encaixes: headHat.

## Natal — sobreposições e adaptadores

| Família / ID | Prioridade | Formato | Variantes / peças |
|---|---|---|---|
| christmas-snow-roofs | P0 | transparent-png-overlay | gable-left, gable-right, flat-wide, factory-step |
| christmas-snow-foliage | P0 | transparent-png-overlay | pine-upper, pine-middle, pine-lower, round-crown |
| christmas-snow-small-props | P0 | transparent-png-overlay | mailbox-arch, lamp-cap, parcel-top, sign-top |
| christmas-snow-ground-edges | P0 | mesh-or-overlay-recipe | bank-lip, bridge-edge, fountain-rim, basalt-cold-ledge |
| christmas-hats | P0 | transparent-png-overlay | hat-front, hat-three-quarter, hat-side |
| christmas-garlands | P0 | transparent-png-overlay | garland-straight, garland-arch, wreath |
| christmas-gift-ribbons | P0 | transparent-png-overlay | bow-red, ribbon-cross, sealed-letter-gold |
| christmas-lights | P1 | transparent-png-parts | bulb-gold, bulb-red, bulb-teal |
| christmas-scarf-and-flags | P1 | transparent-png-overlay | scarf-red, flag-envelope-holiday |
| lava-christmas-adapters | P1 | transparent-png-overlay | basalt-snow-cap, volcano-cold-ledge-snow, forge-garland, geyser-outer-rim-snow |

**christmas-snow-roofs:** Neve em telhados: quatro perfis coerentes com os prédios atuais. Movimento: `attached-static`. Gerar sobre bases reais para preservar contorno/perspectiva; não uma faixa de neve universal.

**christmas-snow-foliage:** Camadas de neve grandes para pinheiro e copas arredondadas. Movimento: `attached-static`. Montar por encaixes e evitar cobrir toda a cor da árvore.

**christmas-snow-small-props:** Neve ajustada à caixa, luminária, encomenda e placa. Movimento: `attached-static`. Caixa aberta/fechada compartilha neve de topo; peças móveis recebem próprio encaixe se necessário.

**christmas-snow-ground-edges:** Neve nas bordas de chão, ponte, fonte e basalto frio. Movimento: `attached-static`. No 3D, malha/cor por superfície; no cartão, PNG de forma correspondente. Nada de colar imagem de tela sobre o mundo.

**christmas-hats:** Gorros vermelhos com borda creme, sombra suave e pompom grande. Movimento: `attached-to-pose`. Encaixe por cabeça/pose. Frente e lado são variantes; não esticar um gorro em todo personagem.

**christmas-garlands:** Guirlandas e coroas simples com detalhe de carta dourada. Movimento: `attached-static`. Cordão/curva posicionados por sockets; não tapar portas ou envelope principal.

**christmas-gift-ribbons:** Laços, fita cruzada e selo postal natalino. Movimento: `attached-static`. Serve a caixas, encomendas, postes e cartas flutuantes. Encaixes: none.

**christmas-lights:** Lâmpadas pequenas e arredondadas em fio criado no motor. Movimento: `slow-low-contrast-pulse`. Sem piscar sincronizado em toda a cena; emissividade opcional com limites de exposição.

**christmas-scarf-and-flags:** Cachecol e bandeira sazonal com ícone postal. Movimento: `attached-static-or-sway`. Mapa continua sendo Green/Lava; Natal é decoração, não outro mundo.

**lava-christmas-adapters:** Encaixes natalinos específicos para rocha fria, forja e margem externa do gêiser. Movimento: `attached-static`. Neve só em zonas frias/edifícios; lava e boca ativa do gêiser continuam visíveis.

## Lava Land — mundo de 250 fases

| Família / ID | Prioridade | Formato | Variantes / peças |
|---|---|---|---|
| lava-sky | P1 | background-raster | peach-orange, coral-dusk, ember-night |
| lava-horizon-volcanoes | P1 | transparent-png | round-single, twin-cones, wide-caldera |
| lava-basalt-props | P1 | transparent-png | basalt-cluster, split-boulder, stone-mail-pedestal |
| lava-geyser-holes | P1 | mesh-or-sprite-parts | round-rim, cracked-rim, mineral-basin |
| lava-geyser-fx | P1 | particle-recipe | steam-pulse, ember-jet, soft-fumarole |
| lava-surface-materials | P1 | material-recipe | river-lava, quiet-lava-pool, obsidian, mineral-ochre, ash-lilac, purple-crystal-rock |
| lava-passages | P1 | mesh-recipe | basalt-arch-bridge, thermal-metal-bridge, rope-over-lava, stepped-stone-islands, tunnel-entrance |
| lava-mailbox | P1 | transparent-png | basalt-mailbox-closed, basalt-mailbox-open |
| lava-postal-lamp | P1 | transparent-png | ember-postal-lamp |
| lava-forge-factory | P1 | transparent-png-parts | forge-building, forge-chimney, hot-letter-chute |
| lava-stamp-machine | P1 | transparent-png-parts | stamp-base, stamp-arm |
| lava-crystals | P1 | transparent-png | teal-crystal, purple-cluster, seal-crystal |
| lava-thermal-flora | P1 | transparent-png | ember-tree, thermal-flower, ash-shrub |
| lava-postal-boat | P1 | transparent-png-parts | metal-letter-boat, boat-flag |
| lava-letter-floaters | P1 | transparent-png | enchanted-envelope, metal-sealed-parcel |
| lava-cave-kit | P1 | transparent-png | cave-back-layer, large-stalactite, glowing-letter-lantern |
| lava-landmark-set | P1 | transparent-png | thermal-post-office, postal-observatory, anchored-mail-balloon, caldera-grand-agency |
| lava-courier-dog-accessories | P1 | transparent-png-overlay | courier-goggles, heat-mail-bag |

**lava-sky:** Somente céu simples em camadas, sem chão, prédios ou fases embutidos. Movimento: `slow-layer-parallax`. Laranja dominante; sombras frias/lilás preservam contraste. Não colorir todos os objetos de vermelho.

**lava-horizon-volcanoes:** Silhuetas grandes de vulcões ao fundo, duas ou três massas de cor. Movimento: `static-with-separate-smoke`. Fumaça é partícula/camada separada; não gerar vulcão com fumaça fixa no mesmo PNG. Encaixes: coldLedgeSnow.

**lava-basalt-props:** Pedras arredondadas de basalto e pedestal postal. Movimento: `static`. Faces largas, brilho suave nas bordas e fissuras pequenas com luz interna. Encaixes: coldLedgeSnow, redBow.

**lava-geyser-holes:** Bocas de gêiser e bacias minerais apoiadas no chão, com depressão visível. Movimento: `rim-static-plume-animated`. Boca não gira/estica; jato separado sobe e dissipa fora do caminho dos níveis. Encaixes: coolOuterRimSnow, redBow.

**lava-geyser-fx:** Vapor, jato estilizado de brasa e fumaça baixa. Movimento: `seeded-staggered-cycle`. Usar máscaras compartilhadas; ciclos diferentes, preview reproduzível, pausa quando fora da vista.

**lava-surface-materials:** Materiais/cartoon colors do mundo, faixas largas e bordas frias/quentes. Movimento: `lava-flow-and-gentle-pulse`. Água comum não substitui lava recolorida sem composição; geometrias de cortes e margens próprias.

**lava-passages:** Cinco famílias de passagem sobre lava e entre plataformas. Movimento: `static`. Mesmo sistema de conexão do Green; silhouette e suporte próprios. Encaixes: coldEdgeSnow, garland, redBow.

**lava-mailbox:** Caixa de correio resistente ao calor, roxo/basalto, borda dourada e bandeira teal. Movimento: `two-state-occasional`. Tem identidade postal; não simples pedra com buraco. Encaixes: topSnow, flagRibbon.

**lava-postal-lamp:** Luminária postal com base de basalto e selo de brasa brilhante. Movimento: `lamp-static-optional-glow`. Comparar com luminária aprovada; no máximo uma camada de halo suave. Encaixes: capSnow, redBow.

**lava-forge-factory:** Fábrica/forja de selos com chaminé grossa e envelope dourado central. Movimento: `static-with-smoke`. Não virar fábrica industrial realista; teto separado para variar e receber Natal. Encaixes: roofSnowColdZone, garland, doorWreath.

**lava-stamp-machine:** Carimbador de selo quente com braço simplificado. Movimento: `stamp-cycle`. Compartilhar controlador da máquina Green; desenhos e paleta distintos. Encaixes: redBow.

**lava-crystals:** Cristais grandes teal/roxo com planos e luz interior. Movimento: `static-or-gentle-glow`. Variação de contraste frio sem sair do bioma lava. Encaixes: snowPatch, ribbon.

**lava-thermal-flora:** Árvore de brasa, flor termal e arbusto de cinza cartoon. Movimento: `subtle-sway`. Aparência térmica própria; não reutilizar tudo verde com filtro vermelho. Encaixes: branchSnowCold, redBow.

**lava-postal-boat:** Barquinho postal de metal mágico entre ilhas de lava. Movimento: `local-bob-and-flag-sway`. Casco apoia na superfície do rio; não cruzar faixa de clique. Encaixes: holidayFlag, giftRibbon.

**lava-letter-floaters:** Cartas encantadas/seladas com aro dourado para o fluxo da lava. Movimento: `flow-path-bob`. Versão postal do rio; não papel real queimando com cinzas pretas. Encaixes: giftRibbon.

**lava-cave-kit:** Caverna aberta em corte com massas grandes e lanternas de correspondência. Movimento: `mostly-static`. A caverna conserva legibilidade/horizonte; teto não cobre a faixa interativa. Encaixes: garland, redBow.

**lava-landmark-set:** Marcos distintos para campo termal, altitude, crateras e final. Movimento: `static-balloon-local-bob`. Pinturas como decoração/fachada; volumes necessários recebem geometria. Encaixes: roofSnowColdZone, garland, doorWreath.

**lava-courier-dog-accessories:** Óculos e bolsa para o mesmo cachorro, com formas simples. Movimento: `attached-to-pose`. Não trocar a identidade do mascote; acessórios separados coexistem com gorro/cachecol sem cobrir olhos. Encaixes: none.

## Pré-histórico — reserva futura

| Família / ID | Prioridade | Formato | Variantes / peças |
|---|---|---|---|
| prehistoric-macro-kit | P2 | concept-and-parts | baby-triceratops, diplodocus-horizon, pterosaur-silhouette, dinosaur-egg, fern, giant-leaf, fossil-mailbox, wood-postal-hut, bone-arch, stamp-footprint |

**prehistoric-macro-kit:** Famílias reservadas ao próximo mundo: dinossauros, vegetação gigante e correio pré-histórico. Movimento: `future-plan`. Quantidade de fases não definida; primeiro conceituar o elenco e terreno depois dos pilotos Green/Lava. Encaixes: futureHeadHat, futureLeafSnow, futureRedBow.

## Ordem concreta dos lotes

1. **Green vivo:** três nuvens, cartas flutuantes, rio/passagem com profundidade, fábrica e fonte estática.
2. **Gestos:** corpo/cauda do cachorro e peças do carimbador; validar alinhamento/pivô e ciclos.
3. **Natal Green:** neve em luminária/caixa/árvore/casa, gorro do cachorro, guirlanda e laço; cena normal/Natal lado a lado.
4. **Lava piloto:** céu pêssego, um vulcão distante, fumaça separada, basalto/fenda/ponte, um gêiser, caixa e luminária vulcânicas.
5. **Natal Lava:** laços comuns + neve própria de borda fria/forja; comprovar reaproveitamento.
6. **Lava completo:** famílias de rio/forja/cristais/cinzas/crateras/caverna/altitude/caldeira, com autoria dos 50 momentos.
7. **Pré-histórico:** primeiro conceito de elenco e terreno; só depois fixar contagem e poses.

Uma família não exige exportar todas as variantes antes de validar a primeira no mapa. Reutilizar o mesmo master para derivadas. Não gerar 450 fundos completos.

## Contrato mínimo por asset produzido

`id`, `worldId`, `familyId`, `variantId`, `viewId`, caminho real, dimensões, transparência, bounds úteis, pixels por unidade, pivô, referência mestre, slots de encaixe, máscara/material, status e prompt/proveniência.

Para sobreposição: `compatibleBaseFamily`, poses/vistas aceitas, socket, transformação local, parte móvel dona, superfície, exclusões e prioridade **local**. Não aceitar escala ilimitada como substituto de ajuste de silhueta.

Para animação: partes/frames realmente entregues, pivôs, controlador, duração sugerida, seed, amplitudes, pausa/culling e estado inicial. Um PNG de nuvem é suficiente para deslocamento; um PNG de gêiser não entrega o jato por si só. A fonte é estática por solicitação.

Status sugeridos: planned → generated → inspected → imported → attached → verified-in-map. O arquivo de backlog atual é apenas planned.

## Prompt mestre de estilo — para a etapa futura de geração

> Arte original de mapa casual match-3 do Magic Mail. Usar postal-lamp.png e mailbox-poses.png aprovados como referências de acabamento. Poucas massas grandes, silhueta imediatamente legível, volume ilustrado arredondado, claros largos no alto à esquerda, sombras suaves e contorno creme. Reflexos pequenos em metal/tinta; folhagem mantém massas simples. Paleta postal roxo, creme, ouro e teal, adaptada ao bioma. Sem texturas realistas, microdetalhes, texto, HUD, números de fase, piso ou sombra de chão embutidos. Objeto completo com margem transparente e pivô de contato. Não incluir as referências no resultado.

Complementos:
- **Nuvem:** forma simples e clara, sem céu/piso; animação será transform no motor.
- **Fonte:** jato e água ilustrados parados; desenho simples de envelope/selo, sem chuva de partículas.
- **Fábrica:** corpo, chaminé e tubo como peças; sem fumaça no prédio; envelope grande sem letras.
- **Neve:** usar a base exata e a vista como referência; produzir apenas a camada separada, com borda coerente, sem redesenhar a base. Branco-creme, sombra lilás/azul, grandes massas.
- **Gorro:** variante frente/três quartos/lado, referência do mascote; mesma luz e porte; não gerar um novo cachorro.
- **Vulcão:** recorte simples distante com poucas cores, fumaça ausente; céu é outro asset.
- **Gêiser:** boca/bacia parada, emissão separada; não pintar o jato dentro do chão.
- **Lava:** não pedir fotografia de lava. Grandes faixas cartoon e poucas cores; topologia, bordas e movimento são do motor.

Usar geração de imagens para ilustração/recortes. Malhas, conectores, materiais e controladores são tarefas de implementação, não uma imagem fingindo geometria. Na geração futura, manter transparência real e salvar seleção final no projeto com prompt. Imagens de fundo não recebem todos os props combinados.

## QA antes de ampliar uma família

Conferir aparência em escala de celular, alpha sobre fundo claro/escuro, recorte com margem e baseline. Ajustar sockets sobre a base real. Comparar três posições de câmera/scroll; neve acompanha levantar/profundidade. Verificar separação da fumaça e pose normal/Natal. Conferir cada parte móvel isolada. Registrar correções e não marcar como pronto só por o arquivo existir.

## Background e clima — sete famílias acrescentadas

A especificação detalhada está em `PLANO-CLIMA-E-BACKGROUNDS.md` e `environment-weather-roadmap.json`. Backlog total atualizado: 50 famílias, 159 variantes/receitas propostas. Todas continuam planejadas, sem imagens novas geradas nesta etapa.

| Família | Prioridade | Peças / variantes |
|---|---|---|
| green-area-sky-bases | P0 | céu claro azul; pôr do sol; carregado azul/cinza; jardim lilás |
| weather-sun-rays | P0 | disco solar; facho largo; facho estreito |
| weather-storm-clouds | P0 | nuvem carregada grande; faixa carregada |
| weather-rain-kit | P0 | risco de gota; impacto pequeno; ondulação de rio |
| weather-wind-kit | P0 | arco de vento; folha solta; cartas existentes são reutilizadas |
| weather-lightning-kit | P1 | raio largo; raio curto; luz interna de nuvem |
| weather-mist-kit | P1 | névoa baixa; faixa lateral |

Céus são bases sem chão/prédios/níveis. Nuvens, sol, raios, fumaça e partículas são separados. Usar a mesma simplicidade cartoon e acabamento suave dos assets aprovados. Materiais/linhas procedurais podem resolver sol/raios/vento; não pedir imagem para substituir um efeito melhor construído no motor.

O cenário de cada uma das vinte regiões combina essas famílias com silhuetas e paletas próprias. Não produzir 450 imagens de fundo. O primeiro lote comprova dois fundos distintos + blend + nuvens/raios; depois chuva/vento/tempestade e variantes Natal. A chuva não anima fontes estáticas. Testar alpha, contraste dos números e transição ao voltar o scroll.


