# Chão e árvores — v20/v21

O chão ilustrado mantém a geometria e o rebaixo suave, com novos degradês, bandas levemente curvas que variam cor e valor, pintura discreta de solo e reflexo suave na trilha. A luz de entrada verde é amarelo-esverdeada. As outras sete áreas mantêm suas próprias cores: lavanda, vulcânico, lagoa, areia, gelo, rosa e cristais. Cores personalizadas continuam sendo respeitadas.

Poças, aberturas de lava, pedras e graminhas continuam como props independentes. As novas pinturas de material não incluem esses objetos. Não houve alteração nas disposições ou nas configurações salvas das ilhas.

Quatro novas árvores ficam em **Adicionar árvore**, como peças do tipo Tree, usando o balanço e a pulsação existentes. As ilustrações foram geradas e editadas com o **ImageGen integrado**, preservando os pixels e o alpha da última saída. O usuário pediu para manter a última edição de contato das árvores sem refazê-las. Regra para os próximos assets: retirar o branco apenas da aresta inferior em contato com o chão; preservar a borda nas laterais até a base.

Arquivos finais de árvores, relativos à raiz do projeto:

| Árvore | PNG |
|---|---|
| Árvore de copa inclinada | `Assets/RoyalAves/Art/MapKitV3/Library/Arvores/crooked-canopy-v20.png` |
| Árvore coral em leque | `Assets/RoyalAves/Art/MapKitV3/Library/Arvores/coral-fan-tree-v20.png` |
| Pinheiro de curvas | `Assets/RoyalAves/Art/MapKitV3/Library/Arvores/teal-wonky-pine-v20.png` |
| Palmeira de tronco curvo | `Assets/RoyalAves/Art/MapKitV3/Library/Arvores/crooked-mail-palm-v20.png` |

Os catálogos individuais ficam em `Assets/RoyalAves/Resources/MapLibrary/Arvores/arte-<nome-do-PNG>.asset`. Os [prompts completos, fontes e etapas de edição](map-library-v20-assets.json) foram registrados.

Pinturas opacas de material, também feitas com o ImageGen integrado e copiadas sem edição:

- `Assets/RoyalAves/Art/MapKitV3/GroundPaint/lawn-brush-v21.png`
- `Assets/RoyalAves/Art/MapKitV3/GroundPaint/path-brush-v21.png`

Os [prompts e arquivos de origem das pinturas](ground-art-v21-assets.json) foram salvos. O shader usa sua variação de luminosidade com intensidade baixa, sob as cores de cada ilha; o verde da textura não é imposto às paletas roxas ou alaranjadas. As coordenadas são fixas no mundo e espelhadas nas bordas para continuidade durante o avanço do mapa. Os materiais personalizados e a season continuam com sua seleção existente.

As alterações de execução ficam em `MapSurface.shader`, `WorldMapView.BindGround`, `WorldMapKit` e `WorldMapIslands.IllustratedPalette`; as referências são preenchidas por `RoyalAvesWorldMapSetup`. Não se alteraram a curvatura, altura de margem, ponte, medalhões ou posições dos objetos.

[Comparação das oito paletas](previews/v21/review.html) · [Validação Unity](previews/v21/validation.txt). O teste usa o código de produção em cópia isolada e não salva os dados de autoria no projeto principal. Desempenho em telefone e interação manual no editor não foram medidos.
