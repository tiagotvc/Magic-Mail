# Gelo

Pedido: novo elemento com dois níveis visuais, um grosso e branco e outro fino e transparente.

## Arte

### Correção de legibilidade — conferida na Unity

O usuário apontou que não conseguia identificar a peça para fazer o match. O grosso estava com alpha 1 e cobria completamente o conteúdo. Foram comparados, na Unity aberta, os sprites reais de seis peças, o material do jogo e o fundo de `BoardCellVisual`: sem gelo, fino antigo (0,55), grosso antigo (1), fino corrigido (0,35) e grosso corrigido (0,60).

Valores atuais: **fino 0,35; grosso 0,60**. Aplicados ao prefab, ao gerador e à inicialização de `IceLayer`. Assim, os dois níveis deixam cor e forma reconhecíveis, mantendo mais branco no grosso. A mecânica continua exigindo match com a peça congelada ou impacto de especial.

Prova visual: [Gelo_Legibilidade_Unity.png](Gelo_Legibilidade_Unity.png), renderizada pela Unity usando o prefab salvo. Colunas: peça sem gelo, fino e grosso. É uma bancada visual com assets do jogo; não é captura de uma fase em Play. Compilação nativa e renderização concluídas. Comparador reproduzível em `Royal Aves > Elementos > Gelo > Comparar legibilidade sobre as peças` (`IceVisualValidation.cs`).

### Revisão v4 — cartoon e névoa branca no fino

Pedido do usuário: o fino precisa ter uma superfície branca embaçada, além da borda; os dois estados devem parecer desenho, combinando com o projeto. Novos arquivos: `Assets/RoyalAves/Art/Obstacles/ice-thin-v4.png` e `ice-thick-v4.png`. Ambos substituem a v3 no prefab, no objetivo e no gerador de assets.

Referência de estilo: `ParcelStack/CM_pilha_estado01_00.png`. Ferramenta: imagegen integrada. Prompts: uma placa de gelo por PNG, desenho cartoon com formas grandes arredondadas, preenchimentos suaves, contorno azul e reflexos brancos largos; sem facetas de cristal, refração detalhada ou textura fotográfica. Fino com superfície completa de névoa branca, sem centro vazio. Grosso branco fosco, opaco e com borda mais volumosa. Fundo transparente e nenhuma peça desenhada dentro do gelo.

A arte do fino saiu com centro branco opaco; nesta revisão inicial, seu renderer usava alpha **0,55**, e o grosso usava alpha **1**. Esses valores foram substituídos pelos valores conferidos na seção de legibilidade acima. Os assets anteriores foram preservados.

Validação: compilação de editor sem erros; referências dos dois sprites e opacidades do prefab conferidas. Conferência visual dentro da Unity permanece pendente.

### Revisão v3 — referência de peças congeladas

Novos assets independentes: `Assets/RoyalAves/Art/Obstacles/ice-thin-v3.png` e `ice-thick-v3.png`. Substituem a prancha v2 no prefab e no objetivo. Referência enviada pelo usuário: peças coloridas visíveis sob uma placa quadrada com bordas ciano fortes, reflexos brancos e facetas nos cantos. O fino deixa o centro translúcido; o grosso tem mais branco, geada e volume.

Os dois renderers usam cor branca com alpha 1. A transparência vem dos pixels da arte, preservando o contraste das bordas. A redução uniforme para 28% foi removida porque apagava também o contorno no grid.

Ferramenta: imagegen integrada. Prompts: sprite individual de gelo fino, quadrado quase reto, bordas ciano opacas e cantos facetados brancos, sem peça interna e fundo transparente; sprite individual de gelo grosso da mesma família visual, bordas volumosas ciano e centro branco fosco. Prompt final do fino: moldura de gelo com perímetro ciano, quatro cantos cristalinos brancos, dois reflexos diagonais, centro totalmente transparente, espessura de borda de aproximadamente 8%, margem externa de 12%, sem neblina, partículas ou texto. Os arquivos anteriores continuam preservados.

Arte gerada com a ferramenta integrada imagegen, no estilo cartoon dos obstáculos do jogo. Arquivos em `Assets/RoyalAves/Art/Obstacles/ice-two-levels-v*.png`. Cada prancha contém os dois estados, grosso à esquerda e fino à direita. Os sprites antigos `ice.png` e `ice-cracked.png` foram preservados.

Prompt utilizado: duas placas quadradas arredondadas de gelo para match-3, com o mesmo tamanho e iluminação; estado grosso branco e fosco, com bordas volumosas; estado fino de gelo transparente, com borda delicada e reflexos pequenos; fundo transparente, sem texto ou símbolos. Revisão: placa fina inteira, sem abertura irregular, transparência uniforme e sem pixels soltos fora das placas.

## Mecânica aprovada e implementada

A meta agora é inicializada diretamente pela quantidade de células pintadas, inclusive antes de criar o tabuleiro. O editor mostra a quantidade automática. Corrigida também a classe exigida pelo carregador quando gelo é a meta principal. Detalhes e validação em [Metas_Gelo_Caixa.md](Metas_Gelo_Caixa.md).

O usuário definiu que o gelo prende a peça por baixo. A sequência é grosso → fino → removido; também é possível iniciar com apenas o nível fino.

- As duas camadas ficam acima da peça. A transparência do fino é definida na arte, com bordas fortes, e o grosso usa mais branco fosco. Apenas o estado exposto é desenhado.
- A peça recebe preenchimento inicial, mas não pode ser trocada nem cair enquanto houver gelo. Matches que incluem a peça e impactos de especiais atingem o gelo, preservando a peça.
- Um impacto remove uma camada. O intervalo de proteção de 0,2 s acompanha a deduplicação já usada pelo tabuleiro e protege o conteúdo mesmo no impacto final.
- Power-ups congelados não são ativados pelo impacto que retira o gelo. A conversão em power-up e a atração visual não podem consumir uma peça congelada.
- Objetivo `IceBlock`, exibido como **Gelo**: uma unidade por célula totalmente liberada. A camada grossa não aumenta a meta; a coleta genérica não antecipa nem duplica a redução.
- `SquareTypes.IceBlock` foi acrescentado no final do enum (14), preservando os valores anteriores. O objetivo também foi acrescentado no final da configuração.

## Uso no editor

O prefab está em `Assets/SweetSugar/Resources/Blocks/IceBlock.prefab`. No editor de fases, pintar `IceBlock` em uma camada deixa gelo fino; repetir em uma segunda camada deixa gelo grosso. Usar a meta **Gelo** para exigir que todas as células sejam liberadas. Nenhuma fase da campanha foi alterada para inserir gelo.

Menu para reconstruir os assets: `Royal Aves > Elementos > Gelo > Preparar gelo de dois níveis`, fora do Play Mode. O importador usa dois arquivos de sprite individuais; os arquivos antigos de gelo continuam intactos.

## Validação

### Animação própria de quebra

`IceBreakEffect` substitui a quebra genérica usada pelos matches. O grosso se divide em **5 cacos maiores**, com brilho de impacto e queda de **0,64 s**, revelando o fino. O fino se divide em **7 cacos menores**, com dispersão mais rápida e duração de **0,48 s**, liberando a peça. As placas são divididas em polígonos irregulares por Voronoi, usando a textura cartoon do próprio gelo. Os fragmentos giram, caem e desaparecem progressivamente.

O efeito é independente da camada destruída e não bloqueia a resolução do tabuleiro. A remoção da camada e a meta mantêm a lógica existente; não se aguarda a queda dos cacos. Malha e material temporários são liberados ao terminar. O brilho dura 0,065 s, e os cacos têm mais opacidade que a placa intacta para ficarem legíveis durante o impacto.

Validação nativa reproduzível: `Royal Aves > Elementos > Gelo > Validar e capturar quebra dos dois níveis`. Foram conferidas 10 amostras dos efeitos, incluindo número de cacos, geometria finita e transparência zero ao concluir. Sequência renderizada pela Unity em [Gelo_Quebra_Unity.png](Gelo_Quebra_Unity.png): grosso na linha de cima, fino na de baixo; antes do impacto e em 0,04 / 0,12 / 0,24 / 0,42 / 0,68 s. É uma bancada visual com os sprites e o material do projeto, não uma gravação de uma fase jogada.

### Correção da prévia do editor

O editor usava `GetSpriteTexture`, que retorna a prancha inteira mesmo quando o sprite representa apenas um dos recortes. Isso mostrava os dois gelos juntos. `SquareUtils` agora renderiza apenas o retângulo do sprite selecionado, respeitando sua opacidade, e mostra somente o estado exposto do gelo. Um clique insere o nível fino; o segundo acrescenta a resistência que é representada pelo grosso. Em jogo, o primeiro dano no grosso revela o fino e o segundo remove o gelo. Dados antigos também respeitam `obstacleLayer` ao escolher a prévia. Runtime e editor recompilados sem erros após a correção; interação visual na Unity ainda pendente.

Compilação gerenciada de runtime e editor verificada com o compilador e referências da Unity 6000.6.0f1. A execução batch da Unity encontrou outra instância com este projeto aberto e não pôde iniciar. A conferência visual em Play e os cenários reais de match, especiais, cascata, reinício e contagem de metas permanecem pendentes.
