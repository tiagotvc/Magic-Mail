# Queda respeitando paredes e buracos — 15/10/2026

O preenchimento diagonal escolhia diretamente uma célula vizinha de destino, sem validar as paredes e os dois cantos da passagem. Uma peça na abertura da barreira podia entrar na diagonal em uma região isolada por caixa e célula NONE.

`Square.CanFallDiagonallyTo` exige origem/destino no mesmo campo, adjacência diagonal de uma célula, saída/entrada permitidas, ambos os vizinhos ortogonais livres de obstáculos e células ausentes, e ausência de restrições em todos os quatro lados do canto. As restrições são consultadas em ambos os lados de cada ligação. A validação ocorre antes de transferir a peça para a célula de destino em `Item.CheckNearSquare`.

Após remover uma caixa, a regra consulta os obstáculos atuais e permite novamente o movimento se a passagem estiver livre. O estado inicial vazio continua permitindo a queda normal. `FieldBoard.GetWaypoints` também deixa de incluir células NONE em trajetos normais. Teleportes explícitos mantêm suas ligações existentes.

Validação: runtime e editor compilados sem erros; diff conferido. Adicionado teste de integração de editor, menu **Royal Aves > Validação > Queda sem atravessar paredes**, com os quatro sentidos diagonais, caixas e buracos em cada canto, paredes nas duas faces de cada ligação e reabertura após remover bloqueios. Esse teste e a conferência visual da fase 6 ainda precisam ser executados no Unity; não foram executados nesta revisão.
