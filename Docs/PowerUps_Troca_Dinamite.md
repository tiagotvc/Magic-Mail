# Ativação por troca e alcance da dinamite

## Troca de power-ups

Foguetes, dinamite, multicolor e hélice devem ativar depois de uma troca válida do jogador, havendo match ou não. Vale tanto arrastar o power-up quanto mover uma peça comum para trocar de lugar com ele. Movimentos de queda por gravidade não são essa ativação.

O fluxo anterior só forçava a ativação quando `combines2` estava vazio. Um match formado pela outra peça podia deixar a hélice parada. A ativação também passava pelas proteções de destruição passiva (`dontDestroyOnThisMove`, `needFall`, `Explodable`).

`Item.ActivateFromSwap` dispara o power-up sem depender do match ou dessas proteções passivas. Bloqueio por gelo/fita e ativações já iniciadas continuam respeitados. O fluxo de combinação entre dois especiais é mantido. O pipeline não transforma em outro power-up um especial que já está sendo ativado.

## Dinamite

`ItemDestroyAnimation.BlastRadius = 3`: área quadrada de até **7×7**, incluindo a célula central, três casas à esquerda/direita e três acima/abaixo. O dano e a seleção usada pelas metas compartilham esse alcance. A área recorta nas bordas e mantém o snapshot das caixas: atinge a primeira caixa, mas não atravessa a mesma caixa nesse impacto.

O alcance padrão dos outros usos de vizinhança continua 1 (3×3); aumentar a dinamite não aumenta a vizinhança da hélice nem altera as regras dos outros especiais. A reação visual da explosão já abrangia os anéis até distância 3; o dano era limitado a 3×3.

## Verificação

Ferramenta: `PowerUpRulesValidation.cs`. Validação nativa de geometria: 49 casas no centro, 16 no canto, 28 numa lateral, nove no alcance padrão e bloqueio por caixa. Verifica também os cinco tipos de power-up ativáveis.

O teste de Play usa uma configuração temporária do tabuleiro, sem salvar o layout da fase: quatro trocas reais da hélice, com/sem match e arrastando a hélice/peça comum, exigindo um lançamento por caso. A proteção passiva é marcada para verificar que a ação explícita do jogador não fica bloqueada por ela. Resultado em `Logs/power-up-rules.result`.

Validação executada na Unity: PASS na geometria e PASS nos quatro casos de troca em Play, com exatamente um lançamento da hélice em cada caso.
