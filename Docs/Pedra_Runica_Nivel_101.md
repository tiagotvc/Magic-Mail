# Estreia da pedra rúnica — nível 101

30/10/2026. Referência visual: arquivo local Owl_Statue_Tutorail_Final.webp fornecido pelo usuário, 50 quadros. A referência mantém o grid destacado, escurece o restante da tela, apresenta instrução central por alguns segundos e libera a partida. Não mostra uma animação de quebra nessa sequência.

Fase real em Resources/Levels/Level_101: 9 colunas × 10 linhas, 85 pedras, quatro foguetes verticais, uma dinamite e 30 jogadas. Meta única: destruir as 85 pedras. A composição adapta o princípio da referência com a arte própria da runa/pata, sem importar elementos do Royal Match.

A estreia dispara após PrePlay: faixa creme/cereja/dourada no centro do grid, “Use powerups para quebrar as pedras rúnicas!”, máscara externa preservando o tabuleiro, bloqueio de entrada e pausa durante cinco segundos; saída com fade e retomada do jogo. Outras estreias mantêm sua apresentação existente.

Hits comuns não alteram cor, posição ou estado da pedra. Powerups usam a fragmentação aprovada. O prefab de produção reutiliza o sprite e a quebra do laboratório. A pedra possui agora o tipo independente `SquareTypes.EnchantedStone` (15), prefab Resources/Blocks/EnchantedStone e meta EnchantedStone (?ndice 14 acrescentado ao cat?logo). N?o depende do n?mero da fase e pode coexistir com caixas. O n?vel 101 foi migrado preservando layout, foguetes verticais, dinamite, jogadas e 85 pedras. Os tipos/?ndices anteriores foram preservados.

Menu: Royal Aves > Elementos > Pedra rúnica > Abrir fase 101 da campanha. A ferramenta abre os assets reais e não reconstrói a fase ao abrir, preservando edições futuras. Durante a prévia, OpenLevel é guardado e restaurado ao sair de Play. Não altera ReachedLevel, vitórias, moedas ou recompensas.

Compilação runtime/editor sem erros: Logs/stone-101-runtime-check.log e Logs/stone-101-editor-check.log. Validação nativa e capturas: Logs/stone-101-preview.result, stone-101-intro.png e stone-101-board.png quando executadas. Não inferir teste físico por ponteiro nem fluxo completo de vitória a partir da ferramenta.

Validacao nativa: introducao pausou e retomou a fase real; meta/arte corretas; hit comum sem resposta; foguete real quebrou sete pedras e reduziu a meta de 85 para 78. Captura em Game View livre evidenciou proporcao diferente do display do projeto; a previa agora reutiliza o fitter de resolucao do laboratorio (1440x2960), restaurando a selecao anterior ao sair. A limpeza inicial de matches passou a aguardar o proximo update em vez de WaitForEndOfFrame, que bloqueava o preparo quando o Game View nao estava renderizando. Sem teste fisico de ponteiro, audio, dispositivo ou vitoria completa.

Conferencia visual final em 1440x2960: PASS, tabuleiro inteiro enquadrado e destacado, mascara externa alinhada, faixa central legivel sem texto cortado. Captura final stone-101-intro.png conferida. Previa reaberta limpa para teste manual; balanceamento/vitoria completa continuam pendentes.

## Detector de movimentos com powerups — 30/10/2026

O detector considera como jogada disponível um foguete horizontal/vertical, dinamite, hélice ou Light Ball ativo na própria célula, sem gelo/prisão, sem queda, destruição ou reserva pela Light Ball. Não exige vizinho nem combinação de três peças. Uma peça comum ou powerup bloqueado não impede o aviso legítimo.

A busca AI.CheckPossibleCombines consulta HasUsablePowerUp antes de declarar ausência de jogadas; LevelManager.NoMatches também verifica a condição antes de iniciar som, popup, embaralhamento ou derrota por falta de jogadas. Não há exceção por número de fase.

Compilações runtime/editor sem erros: Logs/no-moves-powerup-build.log e no-moves-powerup-editor-build.log. Teste nativo: Logs/no-moves-powerup-native.result. A ferramenta da fase 101 verifica os cinco tipos, casos bloqueado/em queda/peça comum, chamada direta de NoMatches e seis segundos de partida estável antes de testar o foguete.

Resultado final: PASS no teste nativo acima. Os cinco tipos foram reconhecidos; powerups bloqueados/em queda e pecas comuns rejeitados; nivel 101 permaneceu em Playing sem popup/embaralhamento, e o foguete continuou quebrando sete pedras com meta 85 -> 78. Sem teste auditivo fisico.

Corre??o da refer?ncia: os quatro foguetes iniciais s?o verticais. Atualizados o ItemType e o prefab de cada c?lula, al?m do gerador da fase; posi??es das c?lulas e dinamite preservadas.

## Pedra no Level Editor

Na paleta de obst?culos, selecionar **Pedra r?nica**, pintar as c?lulas e salvar normalmente. O pincel solicita a meta pr?pria automaticamente; a quantidade ? calculada pelo tabuleiro. Caixas usam seu pr?prio tipo e ?cone. O laborat?rio antigo continua isolado; o elemento de produ??o tem seu pr?prio cadastro. A arte e a fragmenta??o aprovadas foram reutilizadas.

Verificacao deste cadastro: compilacao externa runtime/editor sem erros em Logs/rune-editor-*-build.log; referencias e indices verificados em Logs/rune-editor-asset-check.result. Conferencia interativa do novo pincel ainda pendente: a instancia Unity manteve a compilacao anterior em cache, apesar de Assets > Refresh. O teste isolado esta disponivel pelo comando verify-editor da ferramenta, mas NAO foi registrado como executado. Nao houve nova validacao completa da campanha.

## Correção de visibilidade e reposição — 30/10/2026

Reprodução nativa confirmou 85 componentes de pedra ativos, mas invisíveis na captura: a camada da pedra havia sido salva antes do fundo EmptySquare, e o sprite serializado possuía quatro coordenadas UV iguais a zero, amostrando o canto transparente da textura.

Correções: BindLayer próprio no prefab de produção, normalização das 85 pilhas para fundo → pedra e restauração dos quatro UVs a partir dos vértices/uvTransform e da textura original de 1254×1254. Arte PNG, recorte, geometria, escala, GUID do sprite e quebra preservados. A checagem do editor passa a rejeitar sprite com UVs colapsados ou sem BindLayer.

A fase tinha 81 entradas locais nas linhas inferiores e nenhuma na primeira linha. Corrigida para nove entradas apenas no topo e direção para baixo, preservando layout, metas, jogadas e especiais. O gerador da fase agora define essas entradas explicitamente. Novas peças usam a queda já existente desde o topo; bolsões bloqueados aguardam abertura de caminho em vez de gerar peças localmente.

Teste nativo do Level Editor PASS: ícones distintos, pincel real em cópia isolada do nível 102, caixa e pedra independentes após serialização, meta solicitada pelo pincel e arte correta (Logs/rune-editor-native.result). Não houve alteração de uma fase 102 de produção nem teste físico de ponteiro.

Verificação final desta correção: PASS nativo com pedras visíveis na captura, camadas acima do fundo, UVs distintos e pincel independente no editor. Após foguete vertical, nove pedras quebraram (85 → 76); peças novas foram observadas acima da entrada, movendo-se para baixo em vários quadros e entrando no interior do tabuleiro. A coluna ficou preenchida na captura final.

Encontrado ainda um bloqueio na interpolação: Item.FallingCor aguardava WaitForEndOfFrame, dependente da renderização do Game View. No editor/Simulator, podia ficar suspenso e depois saltar pela diferença acumulada de tempo. A interpolação agora aguarda o próximo update (yield null), sem depender desse evento de renderização; tempos/curva de queda preservados. A verificação exige descida real dentro do grid, além do pequeno deslocamento acima da entrada que não bastava na checagem inicial.

Evidências finais: Logs/rune-refill-native.result, stone-101-intro.png, rune-refill-falling.png e rune-refill-settled.png; capturas inspecionadas. Compilações runtime/editor sem erros em rune-refill-*-build.log. Partida completa, dispositivo físico e áudio não verificados; sem vitória ou premiação executada pela ferramenta. A fase foi reaberta limpa, preservando a cena já aberta sem salvá-la/recarregá-la.

Sequenciamento posterior do foguete corrigido e verificado em seis cenarios nativos: impactos acompanham a passagem em XY; geracao e queda aguardam voo e pausa das explosoes; ajuste final de aterrissagem precede falling=false. Ver Foguete_Impactos_Sequenciais.md e Logs/rocket-pass-native.result.
