# Escaninho de madeira V6

**Atualização V7:** o voo, a acomodação das pilhas e a centralização foram revistos após o feedback do usuário. Os tempos das cartas abaixo descrevem a instalação original V6; consulte [Escaninho_Voo_V7.md](Escaninho_Voo_V7.md) para o comportamento atual.

Substitui o visual anterior pelo modelo de madeira clara, portas turquesa e bordas finas escolhido na prévia V6. Corpo vazio e porta são PNGs transparentes separados; a porta direita espelha a esquerda. As 36 cartas continuam sendo sprites individuais do projeto.

O escaninho continua ocupando quatro células, com um controlador compartilhado. O primeiro impacto desprende as duas portas. Os quatro seguintes enviam nove cartas cada até o objetivo. A quebra final usa fragmentos procedurais do novo corpo e libera as quatro células. Os sons existentes continuam conectados.

Somente a queda das portas usa `DoorFallSpeedMultiplier = 1.25`: velocidade 25% maior, duração equivalente 20% menor. As cartas, a reação do corpo e a quebra mantêm seus tempos anteriores.

`MailCubbyTools.Build()` atualiza o prefab existente no lugar, preservando sua hierarquia, IDs e ajustes de animação. Tanto o prefab de produção quanto o laboratório continuam referenciando o mesmo visual. O catálogo de fases mostra a nova madeira.

Laboratório: **Royal Aves > Elementos > Escaninho > Teste no tabuleiro 2x2**. Os botões de impacto são diagnósticos; o tabuleiro também permite jogar normalmente.

Arte: `Assets/RoyalAves/Art/Obstacles/MailCubby/MM_cubby_wood_body_v6.png` e `MM_cubby_wood_door_v6.png`. PNGs gerados com ImageGen, preservados sem repintura por código. Prompts e evidências: `Tools/royal-postman/production/review/cubby-wood-v6/`. Backup anterior: `Backups/escaninho-madeira-2026-11-12/`.

Não houve alteração em fases da campanha, recompensas, metas cadastradas ou no cofre aprovado. A avaliação estética final permanece com o usuário.

Validação nativa em cópia isolada: PASS. Os 124 objetos/componentes serializados conservaram seus IDs. Conferidos estado inicial, quatro células compartilhadas, primeiro impacto sem coleta, restauração das portas e estoque, quatro lotes de nove, meta final zero, quebra e liberação das quatro células, sem resultado de campanha. Compilação runtime/editor: zero erros. Capturas e gravação mostram a arte real no Unity. Os impactos foram iniciados pelas APIs de diagnóstico; não se trata de validação de ponteiro físico, áudio audível, aparelho ou campanha completa.

Gravação: [ciclo-escaninho.mp4](../Tools/royal-postman/production/review/cubby-wood-v6/ciclo-escaninho.mp4). Se o editor principal estiver em Play com o modelo antigo, pare o Play, aguarde a importação e reabra o laboratório pelo menu acima.
