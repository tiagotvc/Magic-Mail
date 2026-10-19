# Mapa curvo do Correio Mágico

## Próxima implementação pelo Claude

Comece por [CLAUDE-HANDOFF.md](CLAUDE-HANDOFF.md). Ele reúne o plano híbrido 2.5D, kit de assets revisado, correção de profundidade e brainstorm das 200 posições. A seção abaixo descreve o protótipo 2D atual, ainda preservado como referência. A arquitetura nova não foi implementada nesta entrega.

A expansão mais recente está em [PLANO-MUNDOS-SEASONS.md](PLANO-MUNDOS-SEASONS.md): Green Land com 200 posições, Lava Land com 250, pré-histórico futuro e primeira season Natal como sobreposições. [PLANO-PRODUCAO-ASSETS-MUNDOS.md](PLANO-PRODUCAO-ASSETS-MUNDOS.md) lista os novos assets/animações ainda a produzir.

[PLANO-CLIMA-E-BACKGROUNDS.md](PLANO-CLIMA-E-BACKGROUNDS.md) define backgrounds específicos por área e clima animado: sol/raios, chuva, ventania, tempestade e outros ambientes, com vinte perfis Green/Lava e transições graduais no scroll.

O estado `Map` agora abre o mundo de níveis em camadas dentro do canvas do lobby. Selecionar uma fase usa o mesmo pré-nível e carregamento do Sweet Sugar. O progresso existente de estrelas e da agência continua sendo usado. A agência permanece acessível pelo botão **Agência** e pelo menu inferior; nela, **Mapa** retorna ao mundo.

## Configuração

- Asset: `Assets/RoyalAves/Resources/RoyalAvesLevelMap.asset`.
- Menu: **Royal Aves > Mapa de níveis > Selecionar configuração**.
- `enabledByDefault`: desmarcar retorna à agência como tela inicial.
- `levelsPerRegion`: 25 fases por etapa, com quatro cenários iniciais: Jardim Postal, Vila das Cartas, Bosque Encantado e Fábrica de Encomendas. Se forem adicionadas mais fases, os cenários se repetem em sequência; novas regiões podem ser acrescentadas ao array `regions`.
- Cada região define terreno, faixa central, tonalidade do céu, sprites de árvore/construção/personagem e proporção de construções.
- `curveDepth`: distância em fases até a tangente do mundo (11 por padrão). O chão dobra para trás a partir desse ponto.
- `crestCurvature`: queda lateral do horizonte (0,025 por padrão). O horizonte fica quase horizontal; a curva principal está no sentido do avanço.
- `worldRadius`: raio do arco circular (14 por padrão); valores menores intensificam a dobra longitudinal.
- `cameraHeight`: altura da visão sobre o chão (2,4 por padrão). A visão padrão é mais baixa, olhando para a frente.
- `horizonObjectTilt`: inclinação dos objetos na curva (70 graus por padrão). Sprites distantes ficam baixos e com o topo recuado; vão se erguendo suavemente ao se aproximar, com a base presa ao chão e a sombra independente.
- `horizon`, `bend`, `perspective`, `nodeSize` e `visibleLevels` ajustam o enquadramento e a projeção.

O céu fica no fundo. O terreno usa a projeção em perspectiva de um arco circular, com uma tangente finita e uma curvatura lateral convexa. O chão sobe até a tangente e desce atrás dela. Estrada, marcadores e objetos compartilham essa geometria. Os marcadores achatam e os objetos acompanham a inclinação da superfície. Árvores e construções são sprites separados, com sombras próprias; árvores oscilam e personagens se movimentam discretamente. Os objetos distantes desenham atrás do terreno, que oculta suas bases: aparecem primeiro como silhuetas e emergem da curva ao avançar. São os mesmos objetos que depois chegam ao primeiro plano. O conjunto fixo de instâncias é reutilizado.

## Prévia e validação

**Royal Aves > Mapa de níveis > Pré-visualizar mundo curvo** abre uma cena de preview independente da cena editada. O slider percorre todas as etapas e **Exportar PNG** salva uma captura. Não altera fases, cenas, prefabs ou progresso.

`prepare-preview.cjs` prepara um projeto pequeno em `Library/MapValidation/PreviewProject` para renderizar enquanto o jogo já está aberto em outro Unity. Ele copia os mesmos componentes de mapa e a arte real. Apenas as fachadas de save desse projeto isolado são substituídas, para a prévia não depender do estado do jogo; a compilação do jogo é verificada com os scripts reais e as referências do Unity. `RoyalAvesLevelMapPreview.ValidateAndRender` verifica a tangente, a descida atrás da curva, a convexidade lateral, a seleção de níveis e o arraste. Gera imagens em seis posições, duas proporções de tela e frames de avanço pela transição de cenário.

## Arte gerada por IA

Ferramenta: `image_gen` integrada. Assets finais, separados:

- `Assets/RoyalAves/Art/Maps/postal-sky.png`
- `Assets/RoyalAves/Art/Maps/cartoon-pine.png`
- `Assets/RoyalAves/Art/Maps/cartoon-post-office.png`
- `Assets/RoyalAves/Art/Maps/cartoon-sorting-factory.png`

Árvore, agência e fábrica têm fundo transparente. Os primeiros conceitos com muitos detalhes foram descartados. Personagem, avatar, HUD e botões de navegação reutilizam a arte do jogo. Os marcadores de fase são gráficos de UI simples, sem uma imagem de botão brilhante.

Os prompts finais estão em `prompts.json`, junto com a ferramenta e os caminhos dos assets.
