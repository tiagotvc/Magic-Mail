# Folhas e cores das árvores — v10

As árvores da entrada usam uma cor por copa: redonda violeta e larga verde-água, com degradê e brilho dentro dessa cor. Mantêm o desenho das variantes redonda/larga. As cartas continuam penduradas, sem emissão de folhas nessas duas variantes.

Nas árvores laranja da trilha, as folhas:
- voam somente na horizontal, sempre para o lado do caminho;
- saem à direita nas árvores à esquerda e à esquerda nas árvores à direita;
- têm tamanho relativo de 0,13 da altura da árvore, antes 0,105 (+24%);
- usam cinco objetos reutilizados por árvore, com cerca de 2,45 folhas visíveis em média, aproximadamente o dobro da revisão anterior;
- mantêm giro, variação de altura entre folhas e desaparecimento suave, sem queda vertical.

A entrada também bloqueia a emissão pela posição antes da fase 1, mesmo quando outra árvore é colocada ali. Os layouts manuais, posições e tamanhos existentes foram preservados.

## Arquivos

- `Assets/RoyalAves/Art/MapKitV3/SimpleBase/entrance-trees-v10.png` — violeta e verde-água.
- `Tools/level-map/entrance-tree-v10-assets.json` — prompt final, referência e origem; ImageGen integrado.
- `Assets/RoyalAves/Scripts/Meta/WorldMap/WorldMapTreeDecor.cs` — direção, frequência, tamanho e giro.
- `Tools/level-map/previews/v10/review.html` — mapa e prova animada de direção dos dois lados.
- `Tools/level-map/previews/v10/validation.txt` — verificações Unity.
- `Tools/level-map/previews/v10/sync-validation.json` — referências sincronizadas e hashes dos layouts.

Unity 6000.6.0f1, método `PolishPreviewBatch.RunTreeV10`, saída 0. Conferidos os dois sentidos de emissão, ausência de queda vertical, giro, densidade, ausência de emissão na entrada em cinco tempos diferentes, cartas preservadas e migração das árvores da entrada mantendo fase/lateral/tamanho. Salvas duas capturas do mapa e 32 quadros nativos de animação. Desempenho em aparelho físico não medido.

Para repetir: `node Tools/level-map/prepare-polish-preview.cjs`, execute o método acima no projeto isolado `Library/MapValidation/PreviewProject`, depois `node Tools/level-map/sync-tree-v10.cjs`.
