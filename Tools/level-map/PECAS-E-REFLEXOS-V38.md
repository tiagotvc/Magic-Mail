# Peças individuais de neve e gelo

Em **Editar objetos**, selecione a árvore ou o arbusto. Abaixo de **Adicionar tema**, abra **Peças individuais de neve e gelo** e use **Selecionar peça**. Cada item tem posição, rotação, escala X/Y, ativação e restauração próprias. **Salvar disposição** grava os valores junto do objeto; Undo também funciona sobre esses dados.

Para escolher e mover diretamente na imagem, ative **Selecionar / arrastar peças no preview**. O contorno azul indica a peça selecionada. O arraste move somente essa peça no plano do asset, sem mudar a posição da árvore. Para voltar a arrastar a árvore ou o arbusto inteiro, desligue a opção. Os ajustes gerais antigos continuam em **Ajuste geral de neve / gelo (opcional)**.

As coberturas foram divididas em objetos e malhas individuais, com limites próprios. A árvore verde arredondada tem três coberturas; o arbusto coral tem quatro; o pinheiro clássico pintado tem três. Cada pingente de gelo também é um objeto separado, com três à esquerda e três à direita. Os controles gerais de luzes de Natal continuam disponíveis.

`WorldMapPiecePose[]` armazena os encaixes por ID estável, como `snow-0` ou `ice-left-0`. A conversão entre entradas do layout e peças do runtime clona esses dados. Valores antigos sem ajustes individuais usam as posições originais. O tamanho, o espelhamento e a profundidade do objeto continuam sendo herdados. Regiões do mesmo PNG viram malhas separadas; os pixels das artes existentes não são alterados.

`bake-snow-pieces-v38.cjs` lê a transparência das artes e gera `Resources/WorldMapSnowPieceProfiles.json`. O arquivo contém regiões para 19 fontes, incluindo as coberturas pintadas e as camadas pelo contorno. Depois de adicionar novas artes, a leitura pode ser executada novamente. Fontes sem perfil usam uma cobertura única até receberem regiões.

## Reflexos animados no chão

O brilho largo do gelo se desloca lentamente, com um reflexo mais estreito atravessando a superfície. O efeito também atua nas poças congeladas e nas passagens de água, enquanto as placas finas da trilha têm uma oscilação suave no reflexo. À noite, os reflexos de luar e das luzes próximas variam de intensidade e posição. A geometria do gelo e suas marcas internas permanecem fixas. O editor continua atualizando enquanto houver gelo animado visível ou inverno ativo.

São reflexos estilizados da iluminação, sem renderizar a geometria inteira da cena numa câmera de reflexão.

## Verificação

`PiecesV38Validation.Run` confere a quantidade de peças, posições, rotação, escala não uniforme, ativação individual, vizinhos preservados, ausência de acúmulo de transformações, projeção dos limites e persistência após salvar/reimportar um layout de teste. Também compara pixels da mesma região de uma poça em dois tempos para verificar mudança visível do reflexo renderizado. A validação usa um projeto Unity isolado, sem salvar a cena, o layout ou a temporada ativa do projeto principal. Interação manual do mouse e desempenho em aparelho físico não são medidos por ela.

[Peças originais](previews/pieces-v38/separate-default.png) · [Uma peça de gelo e uma de neve ajustadas](previews/pieces-v38/separate-edited.png) · [Reflexo em 1 s](previews/pieces-v38/reflection-1s.png) · [Reflexo em 5 s](previews/pieces-v38/reflection-5s.png) · [Resultado](previews/pieces-v38/validation.txt)
