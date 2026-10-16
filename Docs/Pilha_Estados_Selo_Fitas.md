# Encomenda fechada — selo, fitas e papel pardo

Direção atual definida pelo usuário: o pacote permanece fechado. A resistência é indicada pela retirada de acessórios, terminando na embalagem de papel pardo intacta. Essa revisão substitui as propostas anteriores de rasgar o papel e expor uma caixa interna.

## Estados ativos

| Vida | Aparência | Próximo impacto |
| --- | --- | --- |
| 4 | Selo postal vermelho sobre duas fitas claras | Remove o selo |
| 3 | Duas fitas claras, sem selo | Remove a fita vertical |
| 2 | Uma fita horizontal, sem selo | Remove a fita horizontal |
| 1 | Somente o pacote fechado em papel pardo | Conclui a destruição |

Os quatro PNGs foram gerados com image_gen integrada a partir da referência cartoon aprovada. Estão em `Assets/RoyalAves/Art/Obstacles/ParcelStack/CM_pilha_estado01_00.png` até `CM_pilha_estado04_00.png`. Nomes e GUIDs foram preservados, mantendo as referências existentes do prefab e dos objetivos. Os arquivos são RGBA de 1254×1254; o corpo continua centralizado e normalizado para 0,7224×0,7224 dentro da célula de 0,82. Os PNGs foram copiados sem editar pixels. O conjunto anterior está em `Backups/ParcelStack-cartoon-aberto-v3`.

## Animações e contagem

Os quatro níveis e o laboratório foram validados pelo usuário. A rodada atual trabalha no feedback visual: reação curta de compressão e retorno do pacote a cada dano, brilhos discretos no desprendimento e uma pequena expansão de brilhos na conclusão. O selo ganha pulso e saída; as fitas mantêm o descolamento por segmentos. A embalagem permanece fechada.

`ParcelAccessoryEffect` anima o selo com um pequeno pulso e saída em aproximadamente 0,30 segundo. Cada fita descola em aproximadamente 0,38 segundo: uma malha com 12 segmentos leva a retirada da ponta até o restante, com uma curva suave. A opacidade é mantida no início e cai ao fim. O estado seguinte aparece como pacote fechado. Essas animações não coletam objetivos.

## Laboratório e painel de teste

O laboratório tem quatro pacotes: vida 1 em (coluna 1, linha 2), vida 2 em (3,2), vida 3 em (1,4) e vida 4 em (3,4), usando coordenadas iniciadas em zero. Meta: quatro pacotes concluídos. A configuração dos assets foi conferida contra as posições do painel.

A ativação para a próxima fase foi solicitada em `Logs/parcel-stack-prepare.request` com conteúdo `activate`. Após o Unity compilar, o preparador ativa a substituição da fase pelo laboratório e abre a janela **Teste dos pacotes**. Entre em Play e abra uma fase pelo lobby. Se a fase já estiver carregada, reinicie o Play para carregar a configuração de laboratório.

Na janela, cada linha mostra resistência inicial e restante e tem um botão **Acertar**. Os botões usam o dano normal do pacote sem consumir jogadas ou boosters. Isso permite observar separadamente retirada do selo, fita vertical, fita horizontal e desaparecimento final. O painel também fica no menu `Royal Aves > Elementos > Pilha de Encomendas > Painel de teste dos efeitos`.

Compilação de runtime e editor passou após os ajustes. O comportamento visual do descolamento ainda precisa ser conferido em Play.

## Ajuste dos efeitos em Play

O painel **Teste dos pacotes** agora oferece **Velocidade do efeito** (0,5–1,5) e **Intensidade** (0,5–1,25), além de **Restaurar efeitos padrão**. O padrão é 1 para ambos. Os ajustes valem para a sessão de teste, sem alterar velocidade da partida, regras de dano ou quantidade de objetivos.

A velocidade controla somente os relógios das animações; a conclusão continua aguardando o fim do efeito antes de contabilizar o pacote. A intensidade altera reação, deslocamento e brilhos. A reação do pacote restaura sua escala e rotação de referência ao fim; acertos seguidos não acumulam deformação. Reinicie o Play após recompilar e use os botões **Acertar** para comparar cada estágio no Game View. Essa nova rodada compilou em runtime e editor; a leitura e o ritmo visual ainda precisam de conferência em Play.

No último impacto, o pacote de papel pardo recebe um breve pulso e desaparece inteiro, com movimento, redução de escala e fade. A classe histórica `ParcelTearEffect` mantém seu nome para compatibilidade, mas não divide nem abre o pacote. A animação final dura aproximadamente 0,62 segundo e continua ligada à fila de coleta do jogo. Somente ao final a célula é liberada e a meta diminui uma unidade. O bloqueio de impactos repetidos e o cancelamento ao reiniciar a fase foram mantidos.

Compilação de runtime e editor passou. Resolução, alpha, encaixe dos quatro estados e referências do prefab foram conferidos. Conferência visual das animações em Play ainda pendente. A aplicação pelo editor foi solicitada em `Logs/parcel-stack-prepare.request`, que permanece disponível entre reinícios do Unity.

## Prompts

+## Estado 4

Use case: precise-object-edit. Revise an official Correio Mágico postal-parcel state according to the user's new rule: remove seal, then bands, ending with the SAME intact plain brown-paper package; the package NEVER opens. STATE 4: Keep the two cream packaging bands crossing at the front. REMOVE the little rectangular lower-right address label, restoring unbroken brown paper folds there. ADD a compact ROUND red postal wax seal at the center of the band crossing (about 16 percent of box width), simple beveled edge, small generic envelope mark without lettering. The seal is an attached small accessory on the package, never a separate giant icon. INVARIANTS: exact same square canvas, single rounded square package silhouette, width, height, center, baseline, folded-paper corners, camera, smooth cartoon 2.5D finish, restrained soft gradients, broad upper-left highlights and warm kraft color as reference. Preserve the closed outer paper wrap in all four states. Only remove/add specified ACCESSORIES, do not peel the paper or reveal contents. Real transparent alpha with same margin. No backdrop, external shadow, debris, captions, numerals, text, crown, rivets, extra packages or UI. Clean simple readable game sprite at 70px. No photographic grain or heavy ink outline.

## Estado 3

Use case: precise-object-edit. Revise an official Correio Mágico postal-parcel state according to the user's new rule: remove seal, then bands, ending with the SAME intact plain brown-paper package; the package NEVER opens. STATE 3: Keep BOTH cream packaging bands intact in EXACT same locations. REMOVE the small lower-right address label, restoring continuous brown paper folds beneath it. NO wax seal anywhere: this is the package after its seal has been removed. Two bands only, no tears or leftover tabs. INVARIANTS: exact same square canvas, single rounded square package silhouette, width, height, center, baseline, folded-paper corners, camera, smooth cartoon 2.5D finish, restrained soft gradients, broad upper-left highlights and warm kraft color as reference. Preserve the closed outer paper wrap in all four states. Only remove/add specified ACCESSORIES, do not peel the paper or reveal contents. Real transparent alpha with same margin. No backdrop, external shadow, debris, captions, numerals, text, crown, rivets, extra packages or UI. Clean simple readable game sprite at 70px. No photographic grain or heavy ink outline.

## Estado 2

Use case: precise-object-edit. Revise an official Correio Mágico postal-parcel state according to the user's new rule: remove seal, then bands, ending with the SAME intact plain brown-paper package; the package NEVER opens. STATE 2: REMOVE the small lower-right address label and REMOVE the ENTIRE VERTICAL cream packaging band. Keep ONLY the complete HORIZONTAL cream packaging band at its exact original height, as one uninterrupted strip. The exposed vertical region is smooth continuous kraft paper, not a lighter inner box. No wax seal. No torn ends, holes, seams from removed band, loose scraps or damage to the paper. INVARIANTS: exact same square canvas, single rounded square package silhouette, width, height, center, baseline, folded-paper corners, camera, smooth cartoon 2.5D finish, restrained soft gradients, broad upper-left highlights and warm kraft color as reference. Preserve the closed outer paper wrap in all four states. Only remove/add specified ACCESSORIES, do not peel the paper or reveal contents. Real transparent alpha with same margin. No backdrop, external shadow, debris, captions, numerals, text, crown, rivets, extra packages or UI. Clean simple readable game sprite at 70px. No photographic grain or heavy ink outline.

## Estado 1

Use case: precise-object-edit. Revise an official Correio Mágico postal-parcel state according to the user's new rule: remove seal, then bands, ending with the SAME intact plain brown-paper package; the package NEVER opens. STATE 1: REMOVE BOTH cream packaging bands and the small lower-right address label entirely. The result is ONLY THE SAME CLOSED PACKAGE WRAPPED IN BROWN KRAFT PAPER. Preserve all four simple folded-paper corner shapes and the continuous smooth honey-brown kraft-paper face. No wax seal, label, band, twine, ribbon, tearing, peeled paper, exposed inner cardboard, box lid seam, loose scraps, opening or change in box size. INVARIANTS: exact same square canvas, single rounded square package silhouette, width, height, center, baseline, folded-paper corners, camera, smooth cartoon 2.5D finish, restrained soft gradients, broad upper-left highlights and warm kraft color as reference. Preserve the closed outer paper wrap in all four states. Only remove/add specified ACCESSORIES, do not peel the paper or reveal contents. Real transparent alpha with same margin. No backdrop, external shadow, debris, captions, numerals, text, crown, rivets, extra packages or UI. Clean simple readable game sprite at 70px. No photographic grain or heavy ink outline.
# Áudio próprio dos pacotes

Quatro WAVs procedurais em `Assets/RoyalAves/Resources/Audio/Parcels`: estalo do selo, remoção de fita vertical, remoção de fita horizontal e conclusão com toque seco/brilho curto. Gerados por `Tools/GenerateParcelSfx.py`, sem download de assets. Os sons de acessórios iniciam na remoção; o final inicia junto ao brilho após a tensão inicial da animação. Usam o AudioSource/mixer existente e respeitam o controle de som do jogo. Sons iguais simultâneos são agrupados por 0,06 segundo. O painel de pacotes permite ajustar o volume para testar; essa regulagem é temporária.

