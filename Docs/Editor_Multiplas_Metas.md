# Várias metas por fase — 15/10/2026

O painel **Metas da fase** do criador de níveis mostra uma linha por tipo de objetivo. O botão **+** ao lado do seletor abre um menu com os tipos ainda não usados. O botão **−** remove somente aquela meta; a última meta visível é preservada. Cada seletor altera somente seu grupo, mantendo os demais objetivos, quantidades e variações de sprites.

Exemplo: adicionar **Cartas da franqueadora**, **Grama** e **Caixa (SolidBlock)** na mesma fase. A quantidade de cartas é manual; grama e caixas são calculadas pelo tabuleiro. Objetivo interno Stars continua oculto quando há outras metas, como já ocorre no jogo.

Grupos de variantes (cores/ingredientes) continuam reunidos por tipo, com uma quantidade manual por variante quando configurado. Novos grupos recebem cópias independentes dos sprites. Não são permitidos tipos duplicados. Alterações registram Undo e salvam o asset de metas e o nível pelo fluxo existente; o objetivo primário legado é sincronizado sem substituir a lista de metas. O runtime já exige completar todos os contadores.

Validação: runtime e editor compilados sem erros. Harness C# executado sobre os métodos reais de TargetLevel, sem inicializar objetos nativos do Unity: adição de três metas, preservação da quantidade de cartas, prevenção de duplicatas, troca de um grupo sem alterar os demais, remoção isolada e independência das cópias de sprites. Todos passaram. Harness temporário em `Logs/MultiTargetChecks.cs`. Conferência visual e reabertura interativa ainda pendentes no Unity.
