# Neve mais leve, luzes existentes e gelo nos galhos

Revisão do acabamento v33 a partir da imagem do usuário. O chão conserva a maior parte da grama e a trilha verde. A neve fica em áreas menores, com borda fria e alguns montinhos arredondados de frente sombreada, topo claro e sombra de contato.

## Assets

As coberturas genéricas por categoria foram retiradas: não há mais uma mesma touca aplicada a árvores, arbustos, postes ou caixas de correio de formatos diferentes. Assets sem camada pintada específica recebem uma cobertura fina pelo próprio contorno alpha do sprite, respeitando inclusive os recortes dos atlas. O shader mantém esse tratamento dentro da silhueta. As camadas desenhadas para o arbusto coral e o pinheiro clássico continuam separadas e registradas ao original.

As árvores recebem fios curvos com as **luzes douradas, vermelhas e turquesa existentes** de `Assets/RoyalAves/Art/MapKitV3/Christmas/christmas-lights-cartoon.png`. A nova lâmpada branca tentada durante o trabalho foi descartada e não foi instalada. As luzes têm uma variação suave de brilho e o preview acompanha a animação enquanto estiverem visíveis.

Há dois pequenos grupos de gelo nos galhos inferiores. Os pontos de apoio foram calculados pela transparência real das imagens de 18 sprites de árvore; a leitura não altera PNGs. Isso evita depender da malha simplificada, que pode atravessar o espaço entre copa e tronco. As peças acompanham o mesmo espelhamento, profundidade e movimento do objeto.

O comportamento de **Adicionar tema → Da temporada / Gelo / desligado** continua válido para neve, luzes e gelo. O preview **Natal / neve sobreposta** não muda a temporada ativa do jogo. Coberturas explicitamente configuradas pelo usuário em uma temporada continuam disponíveis.

## Arquivos e reprodução

Nova arte: `Assets/RoyalAves/Art/MapKitV3/Christmas/hanging-icicles-v34.png`, gerada com **image_gen integrado** e copiada sem editar os pixels. Prompt e origem: [winter-v34.json](winter-v34.json).

1. `node Tools/level-map/install-winter-v34.cjs` instala o gelo e vincula as três luzes existentes.
2. `node Tools/level-map/bake-winter-anchors-v34.cjs` atualiza os encaixes por árvore após adicionar novas artes à biblioteca.

`WorldMapKit.treeDecorationProfiles` contém os apoios normalizados, e `WorldMapChristmasDecor` monta fios, luzes e pingentes. O relatório dos apoios está em [tree-anchors.json](previews/winter-v34/tree-anchors.json).

## Verificação

Compilação do runtime e capturas nativas do Unity em projeto isolado. `WinterV34Validation.Run` confere as cenas com a disposição salva pelo usuário, dois formatos de tela, ausência das toucas genéricas, referência ao atlas de luzes existente, decoração nos galhos, brilho animado, espelhamento, remoção do tema e reutilização dos slots. Também executa a suíte anterior de coberturas registradas e quantidade de neve. As cenas, posições e temporada ativa do projeto principal não são salvas pelos testes. Desempenho em aparelho físico não foi medido.

[Captura 1080×1920](previews/winter-v34/map-1080x1920.png) · [Captura 1080×2280](previews/winter-v34/map-1080x2280.png) · [Resultado](previews/winter-v34/validation.txt)
