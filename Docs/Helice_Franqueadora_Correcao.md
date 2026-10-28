# Hélice e franqueadora

A busca de alvo lançava `ArgumentException: Requested value 'MachineLetters' was not found`, pois o nome da meta era convertido diretamente para `SquareTypes`. A exceção interrompia a busca sem encerrar a hélice.

A meta `MachineLetters` agora identifica as células com `FrankingMachineCell`, inclusive nos prefabs que conservam o tipo antigo de bloco. Ao chegar, a hélice chama `AcceptImpact(manual: true)`. Máquinas ocupadas ou sem cartas pendentes disponíveis não são elegíveis. Outras metas usam conversão segura e nomes que não representam blocos são ignorados nessa etapa.

Se o alvo deixar de estar disponível, a hélice libera sua reserva e procura outro. A busca termina após dois segundos sem alvo; o voo tem limite total de seis segundos. Ao encerrar, os tweens, o som e a reserva são liberados. A chegada também distingue peças comuns de blocos por `is Item`.

## Validação

- Compilação de `Assembly-CSharp-Editor.csproj` concluída com sucesso; registro em `Logs/helix-franking-target-build.log`.
- Verificação de whitespace dos dois arquivos C# sem erros.
- A tentativa de testar a conversão por reflexão fora do Unity não pôde executar por incompatibilidade com o carregamento de `netstandard 2.1` no Windows PowerShell. Isso não substitui teste em Play Mode.

## Conferência em Play Mode

1. Na fase 21, lançar uma hélice com a meta de cartas pendente e verificar o impacto na franqueadora e a emissão da carta.
2. Nas fases 25 e 30, lançar várias hélices simultâneas contra as máquinas do U, inclusive perto da última carta necessária.
3. Durante um voo, ativar a máquina com outro power-up: a hélice deve buscar outro alvo disponível.
4. Verificar que não há hélices presas quando faltam alvos elegíveis e que o som termina junto do voo.
5. Repetir com caixas, armários e metas de peças comuns para conferir os demais alvos.
