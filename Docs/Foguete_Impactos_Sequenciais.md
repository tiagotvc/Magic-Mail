# Foguete: impacto sequencial e queda após explosões

30/10/2026. Escopo: foguetes horizontais/verticais, peças comuns, pedra rúnica, cruzamento de dois foguetes e foguete com dinamite. Sem nova arte.

A falha do impacto simultâneo na borda vinha do cálculo tridimensional: a peça estava em Z diferente das células. Uma ponta coincidente em XY adquiria um percurso artificial em Z, e a projeção desse percurso atribuía o mesmo tempo a várias células. O efeito agora mantém as pontas na profundidade da origem e calcula distâncias/projeções em XY. A metade voltada para fora da borda ainda tem uma saída visual própria.

Cada célula recebe o impacto pelo callback de passagem; peças/obstáculos não são destruídos antecipadamente em lote. As combinações também usam callbacks por linha/coluna e um conjunto de células já atingidas para não repetir dano nos cruzamentos. Metas de Jelly são verificadas no impacto, não antes do voo. Os itens são capturados antes do voo e a queda fica bloqueada durante toda a varredura, evitando que outra peça entre na trajetória.

Todas as orientações usam GameBlocker desde OnEnable (com fallback Start para cenas ainda inicializando), antes de disparar o efeito. O bloqueio permanece até o voo mais uma pausa de 0,25 s para leitura das explosões. Em cadeias/combos, os bloqueios se sobrepõem e a gravidade só volta quando todos forem liberados. Geradores aguardam esse bloqueio; a interpolação revalida StopFall antes de mover. O detector de ausência de jogadas também aguarda a liberação.

Efeitos originados em células não escondem o SpriteRenderer do piso. Callbacks tardios são descartados se a fase/célula original já tiver sido substituída. A quebra da pedra foi preservada.

Compilações runtime/editor: Logs/rocket-pass-runtime-build.log e rocket-pass-editor-build.log. Verificação nativa: RocketPassVerification, acionada por Logs/rocket-pass.request; inicia a fase 101 real e testa seis cenários sem editar assets de fases. Observa HitApplied/ActualExplosion e a posição real das metades, inclusive a posição final preservada durante a cauda do efeito. Remove da lista de sobreviventes os objetos destruídos, para não confundir reutilização do pool com movimento indevido. A observação do congelamento termina na primeira liberação de gravidade; cascatas posteriores ficam fora dessa janela.

Relatório/timeline/capturas em Logs/rocket-pass-native.result, rocket-pass-timeline.csv e rocket-pass-*.png. Não confundir diagnósticos por código em Play com entrada física por ponteiro, teste auditivo, dispositivo ou partida completa com vitória. A bancada mantém outras pedras na fase para não gerar vitória/recompensa e reabre o nível limpo após PASS.

Resultado final: PASS nos seis cenários nativos. Vertical/pedras: 9 impactos ao longo de 0,558 s; horizontal/misto: 8 em 0,503 s; vertical/peças: 9 em 0,567 s; horizontal/peças: 8 em 0,499 s; dois foguetes: 15 em 0,585 s; foguete/dinamite: 38 em 0,877 s. Todos os impactos foram comparados à posição real de uma metade que já alcançou a célula. Gravidade bloqueada até 0,270–0,323 s após o último impacto observado; nenhuma peça sobrevivente deslocou-se durante a janela de bloqueio.

A checagem revelou também um ajuste tardio de aterrissagem: falling=false era publicado antes do posicionamento final, que ocorria após a espera de pós-queda. O posicionamento agora termina antes de publicar a peça como parada e respeita StopFall, evitando o deslocamento residual no disparo seguinte. A reutilização do pool e cascatas posteriores à primeira liberação são tratadas separadamente pelo observador.

## Correção de trajetória diagonal — 03/11/2026

O disparo agora usa a posição da célula lógica, não a posição interpolada/animada da peça. Chamadas originadas em filhos procuram o `Item` com célula válida nos pais. A lista de dano é limitada à linha/coluna e ao tabuleiro dessa origem. O efeito projeta as pontas no eixo escolhido e mantém Z fixo; `BuildHalf` também aplica essa restrição defensivamente antes de criar o tween. Isso impede que deslocamentos de troca/queda ou offsets de filhos gerem uma trajetória diagonal.

O novo caso horizontal deslocado revelou uma corrida entre temporizadores e atualização dos tweens: o callback podia anteceder a passagem visível por um frame. `RocketAxisPassObserver` substitui a estimativa por tempo e observa as posições reais em LateUpdate, disparando cada célula uma vez ao ser alcançada. A pausa de impacto e o bloqueio de queda são preservados.

Runtime/editor compilados sem erros. Oito cenários nativos em cópia isolada passaram: seis casos anteriores mais horizontal/vertical com origem deslocada em `(0.43, 0.37, 2.1)`. O observador verificou ausência de movimento diagonal e em profundidade; danos comparados à posição das metades, sem queda durante a varredura. Relatório e timeline em `Tools/royal-postman/production/review/rocket-axis-v1/`. O editor de teste encerrou; sem mudança de fase ou recompensa no perfil principal. Input físico e fluxo completo até vitória/Continuar não foram testados nesta correção.

Compilações finais runtime/editor sem erros. Timeline CSV final usa cultura invariável. Nível 101 reaberto limpo após PASS, sem vitória/premiação. Os testes executam as rotas reais em Play por código; não validam ponteiro físico, áudio, dispositivo ou partida inteira.
