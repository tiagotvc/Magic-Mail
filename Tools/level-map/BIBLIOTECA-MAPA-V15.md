# Biblioteca e chão do mapa — v15

O editor oferece cinco botões: **Adicionar objeto** (construções), **Adicionar árvore**, **Adicionar props**, **Adicionar efeitos** e **Adicionar personagens**. O seletor carrega apenas itens da categoria escolhida. Depois de escolher, use **Colocar no mapa**, ajuste posição e tamanho e **Salvar disposição**.

Os itens estão organizados em `Assets/RoyalAves/Resources/MapLibrary/Construcoes`, `Arvores`, `Props`, `Efeitos` e `Personagens`. Cada `.asset` guarda Sprite, categoria, tipo de animação e tamanho. As referências aos atlases existentes foram preservadas.

As artes novas estão nas pastas equivalentes de `Assets/RoyalAves/Art/MapKitV3/Library/`. Um PNG colocado ali é importado como Sprite e entra na biblioteca ao usar **Atualizar biblioteca**. Também é possível criar um **Royal Aves / Item da biblioteca do mapa** pelo menu Create, preencher seus campos e salvá-lo na pasta da categoria. Itens existentes mantêm alterações manuais.

Foram acrescentados: fábrica de cartas, depósito de encomendas e loja de selos, com fachadas predominantemente frontais, volumes assimétricos, cores alegres e detalhes reduzidos; arbustos coral e violeta com pulsação; graminha independente; reflexo em V como Sprite. Poça e abertura de lava também são Sprites separados. As graminhas procedurais foram retiradas do terreno; os pequenos pontos suaves são apenas textura do chão.

Em **Chão, fundo e clima desta ilha**, **Chão desta ilha** oferece verde, floresta lilás, vulcânico, lagoa turquesa, areia dourada, gelo azul, jardim rosado e cristal violeta. Há cores personalizadas e atlas opcional. Objetos, fundo e clima permanecem configurações independentes. As seasons continuam usando suas camadas existentes.

**Passagem para a próxima ilha** controla largura, profundidade, formato das margens, troncos/tábuas/pedras/água livre, pedras decorativas, movimento e cor da água. A largura move os trechos seguintes mantendo a fase dos objetos autorados. Os reflexos em V são Sprites que se deslocam lateralmente e reaparecem suavemente; podem ser selecionados e removidos como efeitos. A ponte segue a diagonal da referência sem herdar as ondulações da margem.

Validação em Unity isolado: compilação e renders nativos; persistência das configurações; materiais diferentes simultâneos por ilha; categorias filtradas; seleção, arraste e remoção de poça/lava; animação dos reflexos e dos arbustos. Foram conferidos início, oito chãos, telas alta/compacta e diferentes travessias. Interação manual do mouse e desempenho em celular não foram medidos.

As imagens foram feitas com o `image_gen` integrado, preservando seus pixels e alpha. Os prompts finais e caminhos estão em [map-library-v15-assets.json](map-library-v15-assets.json) e [ground-details-v14-assets.json](ground-details-v14-assets.json). As versões anteriores de geração não foram ligadas ao jogo. [Abrir prévia](previews/v15/review.html).
