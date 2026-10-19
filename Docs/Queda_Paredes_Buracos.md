# Queda respeitando paredes e buracos — 15/10/2026

O preenchimento diagonal escolhia diretamente uma célula vizinha de destino, sem validar as paredes e os dois cantos da passagem. Uma peça na abertura da barreira podia entrar na diagonal em uma região isolada por caixa e célula NONE.

`Square.CanFallDiagonallyTo` exige origem/destino no mesmo campo, adjacência diagonal de uma célula, saída/entrada permitidas, ambos os vizinhos ortogonais livres de obstáculos e células ausentes, e ausência de restrições em todos os quatro lados do canto. As restrições são consultadas em ambos os lados de cada ligação. A validação ocorre antes de transferir a peça para a célula de destino em `Item.CheckNearSquare`.

Após remover uma caixa, a regra consulta os obstáculos atuais e permite novamente o movimento se a passagem estiver livre. O estado inicial vazio continua permitindo a queda normal. `FieldBoard.GetWaypoints` também deixa de incluir células NONE em trajetos normais. Teleportes explícitos mantêm suas ligações existentes.

Validação: runtime e editor compilados sem erros; diff conferido. Adicionado teste de integração de editor, menu **Royal Aves > Validação > Queda sem atravessar paredes**, com os quatro sentidos diagonais, caixas e buracos em cada canto, paredes nas duas faces de cada ligação e reabertura após remover bloqueios. Esse teste e a conferência visual da fase 6 ainda precisam ser executados no Unity; não foram executados nesta revisão.

## Revisão 19/10/2026 — a diagonal volta a contornar bloqueios

A regra de 15/10 exigia que **as duas** células do canto estivessem livres. Na prática isso travava a queda diagonal sempre que havia um obstáculo ao lado: na fase com franqueadoras, as células abaixo delas ficavam sem receber peças, porque a célula lateral era a própria máquina.

Pedido do usuário: a queda diagonal deve ser permitida; só é proibida quando há uma parede ou o fim do tabuleiro.

`Square.CanFallDiagonallyTo` passa a avaliar as **duas rotas em L** separadamente — contornando pela célula lateral ou pela célula de baixo. Basta uma estar aberta: célula existente, livre de buraco e obstáculo, e sem restrição de direção em nenhuma das duas pernas. A diagonal só é recusada quando as duas rotas estão fechadas, o que preserva a correção original: uma peça não entra numa região isolada espremendo-se pelo canto entre uma caixa e uma célula NONE.

O teste do menu **Royal Aves > Validação > Queda sem atravessar paredes** foi reescrito junto: um canto bloqueado (buraco, caixa ou mão única) agora **deve** permitir a queda; os dois cantos bloqueados devem travar; e uma parede em apenas uma das rotas não fecha a diagonal, enquanto paredes nas duas fecham. Compilação e execução do teste no Unity ainda pendentes nesta revisão.
