# Sinalizador de triagem — laboratório de teste

09/11/2026. O usuário escolheu **a primeira opção do ícone**, com trevo de três folhas, por combinar mais com o jogo. A correção posterior de quatro folhas não foi a versão selecionada. O mestre original foi preservado em `Assets/RoyalAves/Art/Obstacles/SortingSignal/SignalMaster.png` e em `Tools/royal-postman/previews/sinalizador-icon-v1/primeira-opcao-selecionada.png`.

## Abrir e jogar

No Unity: **Royal Aves → Elementos → Sinalizador de triagem → Abrir laboratório no jogo**. O comando prepara os assets separados, usa `game.unity`, abre a tela real de partida em retrato e apresenta o elemento após as metas. A orientação existente pausa a partida por cinco segundos e sai automaticamente. O painel também fica disponível em **Painel de teste**.

Há três sinalizadores fixos de uma célula, meta de três e 40 movimentos. Peças do jogo: trevo verde, selo vermelho, pena lilás e envelope azul. Há um foguete horizontal na sexta linha para experimentar especiais. Os atalhos de boosters pagos são ocultos nesta bancada.

**Primeira troca guiada:** conte colunas a partir da esquerda e linhas a partir de cima, começando em 1. Na **linha 3**, troque o **trevo da coluna 2** com o **selo da coluna 3**, à direita. Isso forma uma coluna de três trevos ao lado do sinalizador superior. Ele recebe um acerto; os outros dois permanecem intactos. Ao terminar a jogada, todos passam a pedir vermelho.

O painel explica a regra, mostra cor e acertos de cada objeto e oferece reinício, captura e atalhos de diagnóstico. **Especial no primeiro sinalizador** e **Próxima cor** são diagnósticos: não gastam movimento e não equivalem a jogar pelo ponteiro. O botão **Abrir / reiniciar** restaura a bancada inteira; **Desativar e sair do Play** desliga o laboratório.

## Regra proposta para avaliação

- Cada sinalizador ocupa uma célula e bloqueia peças naquele espaço. Não cai nem troca.
- Começa verde. Uma combinação ortogonal adjacente da cor exigida causa um acerto. Cor errada e diagonais não causam dano.
- Especial que atinge diretamente o sinalizador causa dano independentemente da cor.
- São **três acertos**; os três pontos na base indicam a resistência restante. A mesma combinação não conta duas vezes, e há proteção de 0,2 s contra repetição imediata.
- Todos compartilham o ciclo **verde → vermelho → lilás → azul → verde**. O evento existente `LevelManager.OnTurnEnd`, já posterior à resolução de destruições/quedas/coletas, muda a cor uma vez por `moveID`; a preparação inicial não avança o ciclo. As cascatas da mesma jogada usam a cor anterior.
- A lente é material separado em execução. No estado verde, conserva o desenho da primeira imagem escolhida. Nas outras cores, somente a região de vidro muda e recebe o símbolo da peça correspondente; o corpo de cobre continua usando a mesma textura.
- O terceiro acerto usa os fragmentos sólidos existentes (`SafeDebrisEffect`), com giro e gravidade. A meta diminui uma unidade na remoção final. Não há moeda ou nova recompensa.
- Corpo, símbolo, pontos e fragmentos permanecem abaixo dos power-ups em voo.

## Isolamento

Esta entrega é **laboratório**, não conclusão/aceite definitivo da mecânica. Não adiciona o sinalizador à paleta de produção nem às fases da campanha. Como outros laboratórios existentes, troca o prefab `SolidBlock` somente quando a sessão deste laboratório está ativa; o recurso de produção das caixas é preservado.

Assets próprios: `Assets/RoyalAves/Laboratory/SortingSignalCell.prefab`, `SortingSignalLevel.asset`, `SortingSignalTargets.asset`; material e sprite em `Assets/RoyalAves/Art/Obstacles/SortingSignal/`; shader `SortingSignalLens`.

A meta `SortingSignals` foi acrescentada no fim do catálogo, sem deslocar os índices anteriores. Ela permanece oculta da autoria normal pelo catálogo explícito do editor. Não substituir outras metas/prefabs. A vitória/derrota de campanha fica bloqueada enquanto o laboratório estiver ativo, inclusive com a meta zerada; ele não concede recompensas nem altera o avanço da campanha. Não modifica `OpenLevel` ou `ReachedLevel` para abrir a bancada. Outros laboratórios cedem o lugar pelo controle de sessão compartilhado. A seleção de resolução é restaurada ao sair.

Backups: `Backups/sorting-signal-lab-2026-11-09/`. As alterações existentes dos arquivos tocados foram preservadas; a comparação do catálogo confirmou os registros anteriores idênticos, com apenas a nova meta acrescentada.

## Validação

Runtime/editor atuais compilados sem erros. Teste nativo em cópia isolada Unity 6000.6.0f1, saída 0: três objetos e meta inicial 3; rejeição de cor errada; troca real iniciada por API gerando match e um acerto; mudança de cor no encerramento da jogada; foguete real pelo ativador de toque iniciado por API atingindo dois alvos; três acertos e antirrepetição; quebra/meta unitária; meta zerada sem resultado de campanha; **reinício real** restaurando cor, resistência e meta. Capturas reais verde/vermelho/lilás e quebra inspecionadas.

O laboratório também foi instalado e aberto na instância principal existente, confirmado em `Playing`, sem iniciar outro editor nesse mesmo projeto. As capturas são nativas, não mockups gerados. Evidências: `Tools/royal-postman/production/review/sorting-signal-v1/`; manifesto `Tools/royal-postman/production/SORTING-SIGNAL-V1.json`.

Não afirmar teste físico de ponteiro, áudio, dispositivo ou campanha completa a partir dessas verificações. O usuário ainda avaliará a lógica, a leitura das cores e o equilíbrio dos três acertos.
