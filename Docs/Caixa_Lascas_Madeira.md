# Lascas de madeira e percurso de matches — 15/10/2026

Revisão de quantidade e formato: 6 pedaços por tábua (antes 12) e 12 na quebra final (antes 22). Fragmentos mais curtos e encorpados, próximos de círculos levemente irregulares: raio de 7,5% a 11% da textura, proporção entre eixos de 0,85 a 1,1 e 12 a 16 vértices. Variação do contorno reduzida para evitar pontas longas. Mantidas tábua inteira desprendendo, velocidade e distância de queda. Fragmentos de matches preservados. Runtime compilado sem erros.

Revisão de desprendimento: ao remover uma proteção, a tábua inteira também se solta junto das lascas. Usa malha com extremidades arredondadas, amostrada do estado de uma única diagonal para evitar trazer o fundo da caixa ou outras tábuas no recorte. A orientação corresponde à proteção removida (horizontal, diagonal ascendente e descendente). Impulso lateral e para cima, giro de 280 a 380 graus por segundo de simulação e queda com a mesma aceleração de tempo 1,4× das lascas. Sem fade; some após sair da tela. Animações de outros detritos preservam sua rotação inicial. Runtime compilado sem erros; conferência visual no jogo pendente.

Revisão de velocidade: quebra final reduzida de 0,62 s para 0,42 s, com tremor de 0,08 s. Queda das lascas de caixas e tábuas a 1,4× a velocidade anterior, preservando o percurso até sair da tela. Tamanho das lascas e efeitos dos matches preservados.

Revisão de tamanho: lascas da caixa e das tábuas aumentadas em cerca de 55% no comprimento e 65% na largura. Sem alteração nos fragmentos de matches, na quantidade de lascas ou na queda.

Caixas e tábuas passam a usar `FallingBoardDebris.ShatterWood`: 12 lascas por tábua e 22 na quebra final. Polígonos alongados de 9 a 12 vértices, com raios variados e bordas irregulares, amostram pequenas regiões da textura. Não há mais corte em grade 4×3 nem recorte retangular de uma tábua inteira. Distribuição das lascas acompanha a orientação das tábuas; tamanhos, impulsos e rotações variam.

Lascas pertencem ao FieldBoard, sobrevivendo à remoção da caixa, e caem com gravidade de 9,5 unidades/s² até todas cruzarem o limite inferior da câmera, com margem de 1,5 unidade. Sem fade ou duração fixa. Reiniciar/destruir o campo limpa os efeitos. Velocidade e intensidade da caixa respeitam os controles existentes.

A conclusão mantém os 0,12 s de tremor e a barreira curta de 0,62 s. O callback da meta e liberação da célula ocorre sem esperar a queda completa das lascas decorativas; portas do armário preservam seu comportamento anterior.

Matches: velocidades iniciais e gravidade multiplicadas por 1,3; assim, o deslocamento da trajetória aumenta exatamente 30% no mesmo instante. Mantidos duração de 0,55 s e início do fade em 0,18 s. Não passam a cair até o fim da tela.

Validação: compilação runtime e editor via Roslyn do Unity sem erros. Revisão de referências e diff sem erros de whitespace. Conferência visual no jogo pendente da abertura do Unity (janela de licença/termos observada nas revisões anteriores).
