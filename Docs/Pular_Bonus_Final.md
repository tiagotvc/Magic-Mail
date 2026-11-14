# Pular a apresentação do bônus após a vitória

30/10/2026. Integrado ao ramo de campanha de PreWinAnimationsCor. O botão **Pular** aparece após três segundos reais de bônus; não aparece durante a partida nem se a sequência terminar antes. Fica na área segura, com alvo mínimo de 44 px, fonte existente e o mesmo sprite azul V161 usado nas configurações (referência bonusSkipButton em RoyalAvesFeatures; sem nova arte ou textura).

O clique é idempotente: cobre as animações com “Finalizando bônus...”, oculta/desabilita o botão e acelera a simulação existente em 16×. O cálculo continua em CampaignWinBonus, inclusive consumo das jogadas restantes e cascatas; não há prêmio fixo inventado nem chamada direta ao pagamento de vitória. A música e os PlayerPrefs de áudio não são alterados; apenas o AudioSource de efeitos é temporariamente silenciado. Velocidade e mute anteriores são restaurados na conclusão, mudança de estado/fase ou destruição da interface. A interface é desativada antes de persistir o resultado e abrir a vitória.

O encerramento do bônus exige também quatro atualizações de estabilidade, além do intervalo de quietude já existente, para não ultrapassar callbacks de cascata ao acelerar. Guards impedem resolução concorrente/repetida da mesma vitória e repetição da entrada no estado Win. PrepareGame reinicia esses guards para uma nova partida.

Implementação: Assets/RoyalAves/Scripts/Meta/CampaignBonusSkip.cs; integração em Assets/SweetSugar/Scripts/Core/LevelManager.cs. Nenhum prefab de lobby/vitória, cena, economia, mapa ou sprite existente foi reconstruído.

Verificação final: compilações runtime/editor sem erros (Logs/bonus-skip-runtime-build.log e bonus-skip-editor-build.log). Teste nativo PASS (Logs/bonus-skip-native.result): atraso/ausência antes do tempo, GraphicRaycaster e evento de ponteiro do botão, sprite compartilhado, área segura/44 px, repetição de pedido ignorada, CampaignWinBonus real consumindo quatro jogadas e calculando pontos, limpeza de sequência curta, restauração de velocidade/áudio na conclusão e interrupção. Captura Logs/bonus-skip-button.png inspecionada.

A fixture bloqueia o caminho de persistência/pagamento e não executa uma vitória real: moedas, estrelas, níveis vencidos e score persistido foram comparados antes/depois e permaneceram iguais. Entrada física em dispositivo, áudio audível e fluxo completo vitória -> Continue -> home não foram testados nesta entrega. A revisão do caminho final é de código; não confundir a execução do bônus isolado com aceite completo desse fluxo. A fase 101 foi reaberta limpa ao terminar.
