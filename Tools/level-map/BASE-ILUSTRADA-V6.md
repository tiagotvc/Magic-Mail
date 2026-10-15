# Base ilustrada v6

A direção ativa agora é um único cenário verde postal, com um conjunto pequeno de desenhos 2D simples e vivos. As ilhas continuam organizando a edição e as travessias; não alternam automaticamente entre deserto, lava, corais e outros mundos. As seasons continuam podendo sobrepor decoração e trocar o chão.

## Coleção ativa

Doze sprites de cenário: árvore redonda, pinheiro, arbusto irregular, flores, pedrinhas, cerca, casa postal, oficina postal, poste pequeno, carteiro, coruja e caixa de correio. Há também uma ponte de troncos, duas nuvens e quatro emblemas de nível (coroa, estrela e duas corujas de dificuldade). Todos os novos desenhos têm formas amplas, degradês suaves e poucos reflexos; o contorno dos objetos é fino e claro. As nuvens são planas e suaves, sem contorno.

Arquivos finais em `Assets/RoyalAves/Art/MapKitV3/SimpleBase/`: `simple-nature-v6.png`, `simple-postal-v6.png`, `simple-bridge-v6.png`, `simple-emblems-v6.png`, `simple-clouds-v6.png`. Foram criados com **ImageGen integrado**, com alpha verdadeiro. Prompts completos, origem e nomes dos sub-sprites: [simple-v6-assets.json](simple-v6-assets.json). Os pixels gerados não foram modificados.

O chão e a água usam os shaders existentes, ajustados diretamente: degradês largos, faixas de valor claro/escuro, margem baixa colorida e reflexos discretos. A trilha oferece areia dourada ou verde menta. Cores e materiais manuais continuam respeitados. Os botões mantêm a geometria contínua com aro e lateral creme; a face ganhou degradê mais vivo e reflexo suave, com os novos emblemas.

## Distribuição e movimento

As árvores e os arbustos formam a maior parte do cenário. Há trechos só com vegetação e poucos postes; casas e oficina aparecem com intervalos maiores. A abertura usa os dois personagens. A receita inicial tem até vinte peças por vinte fases, sem gêiseres, presentes, selos soltos ou cristais gigantes. Pequenos detalhes de grama seguem o chão. Foram retirados os buracos escuros da trilha base.

Árvores, arbustos e construções pulsam suavemente a partir da base. Os personagens pulsam e se deslocam um pouco para os lados. O movimento é calculado pelo mesmo relógio no jogo e no preview. A inclinação distante é mais suave para manter as silhuetas legíveis perto do horizonte. Água e nuvens também têm movimento discreto.

## Editor e dados preservados

A janela mostra **Base ilustrada v6**, com configurações à esquerda e preview à direita. O seletor para adicionar objetos apresenta os doze assets da coleção. O tamanho e o tipo vêm preenchidos conforme a peça. Cada ilha conserva fundo/clima editáveis, e a season continua configurada no editor de temporadas → **Mapa de níveis**.

A composição ativa usa `RoyalAvesWorldMapSimpleLayout.asset`, compartilhada pelo editor e pelo jogo. A disposição antiga permanece intacta em `RoyalAvesWorldMapLayout.asset`. A configuração anterior dos dez biomas foi guardada em [backups/islands-before-simple-v6.asset](backups/islands-before-simple-v6.asset). As 100 fases criadas e as posições das travessias foram preservadas.

## Verificação

[Galeria nativa do Unity](previews/v6/review.html) · [Relatório](previews/v6/validation.txt).

A validação usa uma cópia isolada do projeto: coleção e densidade, cliques nos níveis, âncoras de arraste na superfície curva, pulsação, deslocamento dos personagens, material de água, chão personalizado e neve sobre os mesmos objetos. Inclui capturas da abertura, percurso, ponte, bosque, trecho posterior, movimento e season. Gestos reais do mouse e desempenho em aparelho físico não foram medidos automaticamente.
