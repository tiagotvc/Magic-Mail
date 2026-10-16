# Árvores postais, entrada e acabamento do mapa — v9

## Resultado

- Três árvores na mesma paleta amarelo/laranja/coral da árvore aprovada: redonda, alta e larga. As copas não contêm a folha decorativa fixa.
- Duas cartas por árvore, presas por cordões e articuladas pelo nó superior.
- Folhas douradas e corais em camadas independentes: saem da copa em intervalos distintos, caem, giram e desaparecem suavemente. O tronco permanece apoiado. O efeito pausa na versão com neve.
- Entrada verde com duas árvores no primeiro plano. A trilha começa com uma ponta arredondada antes da fase 1; a faixa central não atravessa esse gramado.
- O jogo enquadra as primeiras fases mais acima para exibir a entrada, aproximando gradualmente o enquadramento normal até a fase 4.
- Degradê verde mais vivo nas margens de toda a base unificada, com faixas claras/escuras suaves e um ponto de luz no gramado inicial.
- Medalhões com degradê e reflexão curva suaves: violeta para fases seguintes, turquesa para a fase atual e dourado para concluídas; a borda bege e o volume permanecem em geometria.

## Editor e jogo

As três árvores novas estão na coleção do editor de objetos. Selecionar **Tipo / animação: Tree** aplica o balanço e as camadas de cartas/folhas. O editor e o jogo usam o mesmo `WorldMapView`, a mesma rota e os mesmos shaders.

As árvores antigas já posicionadas no layout passam a usar a copa nova sem alterar lateral, fase ou tamanho. As duas árvores da entrada podem ser movidas ou removidas no editor e a decisão fica salva como os outros objetos. Os dois arquivos de disposição já existentes não foram regravados nesta alteração.

## Arquivos

- `Assets/RoyalAves/Art/MapKitV3/SimpleBase/postal-trees-v9.png` — três bases.
- `Assets/RoyalAves/Art/MapKitV3/SimpleBase/postal-tree-details-v9.png` — duas folhas e duas cartas com cordões.
- `Assets/RoyalAves/Scripts/Meta/WorldMap/WorldMapTreeDecor.cs` — articulação e ciclos das camadas.
- `Tools/level-map/postal-tree-v9-assets.json` — prompts finais, referência e origens; imagens geradas com **ImageGen integrado**.
- `Tools/level-map/previews/v9/review.html` — mapa com abas e prova animada da árvore.
- `Tools/level-map/previews/v9/validation.txt` — verificação no Unity.
- `Tools/level-map/previews/v9/sync-validation.json` — referências persistidas e hashes dos layouts preservados.

## Validação

Unity 6000.6.0f1, projeto isolado, método `PolishPreviewBatch.RunTreeV9`, saída 0. Conferidos: sete sprites novos e shader; migração visual das árvores antigas mantendo âncoras/tamanhos; duas árvores de entrada efetivamente visíveis; gramado e começo arredondado; posição da primeira fase; ponto de suspensão das cartas fixo enquanto giram; queda e desaparecimento intermitente das folhas; tempos distintos por árvore; reutilização de objetos; clique nas fases próximas e distantes. Foram salvas cinco capturas do mapa e 32 quadros da árvore pelo renderizador nativo do Unity.

O desempenho em aparelho físico não foi medido. Os ciclos usam poucos objetos reutilizados por árvore e materiais compartilhados.

Para repetir: `node Tools/level-map/prepare-polish-preview.cjs`, execute o método acima no projeto `Library/MapValidation/PreviewProject`, depois `node Tools/level-map/sync-tree-v9.cjs` para copiar somente as referências e importações desta revisão.
