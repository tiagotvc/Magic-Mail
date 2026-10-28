# Regra vigente: estrelas por pontuação — 28/10/2026

Campanha ampliada: fases 11-30 aplicadas. Fontes atuais e verificacoes em [Campanha_Fases_11_a_30.md](Campanha_Fases_11_a_30.md). Armario estreia na 11; franqueadora na 21; ambos com apresentacao. Desafios 15/20/25/30. Bloco aprovado 1-10 preservado; estrelas 11-30 iniciais, pendentes de playtest. Os registros abaixo de fases 11-100 ainda legadas sao historicos.

Decisão do usuário: adotar o padrão de avaliação do Sweet Sugar, com 1, 2 ou 3 estrelas por nível conforme os pontos acumulados. Substitui a regra antiga de uma estrela fixa, inclusive referências históricas na documentação.

- Usar os limites `star1`, `star2` e `star3` definidos em cada LevelData. Não estabelecer um limite único para a campanha.
- Resolver o bônus dos movimentos restantes antes de avaliar, salvar e apresentar o resultado. Seus pontos também contam para a segunda e terceira estrelas.
- Manter pelo menos uma estrela quando as metas forem concluídas. O melhor resultado salvo nunca diminui por uma repetição pior.
- Exibir pontuação e barra de estrelas do Sweet Sugar durante a partida. O resultado informa a avaliação de 1–3 estrelas, os pontos e as estrelas novas creditadas.
- Na primeira vitória, creditar a avaliação obtida no saldo. Ao melhorar uma fase de 1 para 3 estrelas, creditar somente as duas adicionais. Repetir ou piorar a avaliação não concede estrelas duplicadas. O evento de vitória e a tela podem registrar a mesma vitória sem crédito duplicado.
- Saves antigos sem registro de estrelas creditadas por fase consideram a recompensa fixa antiga para níveis já vencidos. O saldo existente não é resetado; melhorias futuras creditam a diferença.

Implementação: LevelManager (avaliação após bônus), MetaProgress (crédito e compatibilidade), LobbyController (HUD e resultado), VictoryScreen (textos e avaliação). O campo antigo `starsPerFirstWin` é mantido para compatibilidade com saves.

## Validação

### Revisão do bloco completo 1–10

O log recente da fase 7 registrou 23 dos 28 movimentos restantes, 1.670 pontos antes do bônus, 1.010 no bônus e 2.680 no total. O limite herdado de 3.200 resultava em duas estrelas mesmo nessa vitória eficiente.

Tabela vigente, mantida em `Tools/level-map/campaign-star-thresholds.csv`:

| Fase | 1 estrela | 2 estrelas | 3 estrelas | Base da revisão |
| --- | --- | --- | --- | --- |
| 1 | 100 | 600 | 1.200 | Preservada; falta amostra recente |
| 2 | 200 | 900 | 1.430 | Preservada; falta amostra recente |
| 3 | 100 | 500 | 1.100 | Preservada; falta amostra recente |
| 4 | 300 | 900 | 1.500 | Estimativa inicial para ensino de grama em 32 células |
| 5 | 350 | 1.000 | 1.700 | Estimativa inicial para prática em 36 células |
| 6 | 500 | 1.600 | 2.400 | Preservado ajuste; vitórias recentes de 2.650 e 3.110 |
| 7 | 500 | 1.500 | 2.400 | Vitória recente de 2.680 com 23 movimentos restantes |
| 8 | 400 | 1.100 | 1.900 | Estimativa inicial para cruz com grama de duas camadas |
| 9 | 550 | 1.700 | 2.700 | Estimativa inicial para o desafio com oito caixas |
| 10 | 550 | 1.700 | 2.800 | Estimativa inicial para o tabuleiro maior de consolidação |

Esta revisão substitui o limite idêntico copiado do template. Apenas 6 e 7 têm telemetria recente com bônus e movimentos; os demais ajustes são hipóteses de design para testar, não uma taxa de três estrelas comprovada. Geometria, movimentos, metas, valores por peça e o bônus não foram alterados. Resultados antigos salvos não são recalculados automaticamente.

Auditoria de todos os 100 assets em `Campanha_Auditoria_Estrelas.csv`: todas as faixas existentes são positivas e crescentes. Os assets 11–100 continuam legados, sem as receitas da nova campanha aplicadas, e precisam de calibração junto da montagem e do playtest. Não foram reajustados com uma fórmula genérica.

Ferramenta: `python Tools/level-map/calibrate_campaign_stars.py --apply`; sem `--apply`, apenas audita. Backup dos assets alterados em `Backups/Campaign-stars-before-block-review`. O gerador de layouts consome a mesma tabela, evitando voltar aos limites herdados. Verificadas preservação dos demais dados serializados, pontuações recentes da fase 6/7 e fronteiras exatas da terceira estrela.

### Primeira calibração da fase 6

Feedback do usuário: vitória com muitos movimentos restantes rendeu apenas duas estrelas. Em `Logs/Editor.log`, foi localizada uma vitória da fase 6 com 2.670 pontos e duas estrelas; o registro não informa quantos movimentos sobraram, portanto não comprova que seja exatamente a partida relatada. As fases 4–10 herdaram os mesmos limites 500/1.600/3.200 do template, sem calibração individual.

Fase 6 ajustada para **500/1.600/2.400**. Mantida a pontuação do motor e a avaliação após o bônus. O registro de 2.670 passa a obter três estrelas. A mudança é inicial e baseada no feedback e no registro disponível; não garante três estrelas por qualquer número de movimentos restantes e ainda requer novos playtests. As demais fases não foram alteradas sem registros de desempenho. Melhores resultados salvos não são recalculados retroativamente nesta entrega.

O gerador preserva os limites ajustados da fase 6 ao reconstruir o lote. Conferidos os limites exatos de segunda/terceira estrela e o caso de 2.670 pontos; runtime compilado. O log `[Campaign bonus]` passa a registrar movimentos restantes, pontos antes/depois do bônus e limites da fase para orientar a calibração seguinte.

Runtime compilado via MSBuild. Conferência em Play ainda pendente: vitória em cada faixa de pontos, bônus promovendo de 1 para 2/3, repetição com melhora e sem melhora, crédito único apesar das duas chamadas de registro, persistência após reiniciar e carregamento de save antigo. Os limites existentes de cada fase não foram rebalanceados nesta correção.
