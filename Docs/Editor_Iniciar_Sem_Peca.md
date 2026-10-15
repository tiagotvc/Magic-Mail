# Iniciar sem peça — 15/10/2026

Na aba **Items** do criador de níveis, o botão **Iniciar sem peça** ativa um pincel: clique nas células jogáveis para marcar ou desmarcar o preenchimento inicial. A marca “Vazia” aparece sobre as células e a prévia omite a peça, preservando o piso e obstáculos. Escolher uma peça normal sai desse pincel; pintar uma peça remove a marca daquela célula.

**Gramas sem peça** marca todas as células com grama da subfase atual. **Limpar marcações** restaura o preenchimento inicial sem mexer nos pisos ou nas peças predefinidas. O Clear tradicional da aba Items limpa tanto as peças predefinidas quanto as marcações.

`SquareBlocks.startWithoutItem` é um bool serializado por célula, disponível para todas as fases e subfases e preservado nas cópias de dados. Padrão false mantém as fases existentes. `FieldBoard.GenerateNewItems(false)` ignora o preenchimento dessas células, inclusive se houver uma peça predefinida. Não bloqueia gravidade, geração normal ou reposição posterior.

Para a fase 6, marcar as gramas sob a barreira de caixas e manter entradas apenas acima da barreira na aba Directions. A região fica vazia enquanto não houver passagem; ao abrir o caminho, recebe as peças pelo fluxo normal. Uma célula marcada que também seja entrada poderá gerar peças imediatamente pelo fluxo normal: a marca não é um bloqueio permanente nem uma condição de ativação.

Leituras de peças predefinidas foram protegidas contra null para suportar células limpas no editor. Runtime e editor compilados sem erros. Conferência interativa de salvamento, início vazio e queda após liberar a barreira ainda pendente no Unity. O layout da fase 6 não foi alterado automaticamente; o atalho está disponível para configurá-la.
