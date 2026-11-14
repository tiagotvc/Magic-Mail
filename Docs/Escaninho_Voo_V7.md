# Escaninho: voo, pilhas e centralização V7

Correção solicitada após a instalação do modelo de madeira V6. A arte permanece igual.

- Voo de 0,62 segundo por carta, com intervalo de 0,045 segundo entre saídas: curva contínua e inclinação suave, sem pirueta de 360 graus. As cartas continuam visíveis sobre o HUD até o objetivo correto do escaninho, sem as faixas amarelas grossas.
- Cada uma das seis pilhas se reorganiza assim que sua carta da frente parte. As cartas restantes avançam e descem para os lugares vagos; tamanho, iluminação e ordem de desenho acompanham a nova profundidade. O próximo lote só começa após o voo e a acomodação terminarem.
- O centro visível do armário coincide com o centro das quatro células. Ajuste proporcional ao espaço disponível, com margem de 3%; sem alinhamento pela base.
- Mantidos os cinco impactos, 36 cartas, lotes de nove, quebra final e liberação de quatro células. Portas continuam com velocidade 1,25×.

O comportamento novo é habilitado apenas no prefab do escaninho por `useCabinetDelivery`. A máquina de franquear, que compartilha a classe de animação, mantém seu voo anterior.

[Ver gravação do voo](../Tools/royal-postman/production/review/cubby-motion-v7/voo-cartas-v7.mp4).

Laboratório: **Royal Aves > Elementos > Escaninho > Teste no tabuleiro 2x2**. Pare o Play anterior e reabra após a importação dos scripts.

Backup: `Backups/escaninho-voo-v7-2026-11-12/`. Evidências: `Tools/royal-postman/production/review/cubby-motion-v7/`. Avaliação visual final depende do usuário.

Validação nativa isolada: PASS. Conferidos o centro opaco do armário contra o centro das células, os novos tempos, a habilitação do voo sobre o HUD, as posições e escalas das cartas remanescentes após cada meio-nicho, inclinação inferior a 26 graus durante o voo, reinício e ciclo completo de 36 entregas/quebra/liberação. Compilação runtime/editor: zero erros. Impactos iniciados pelas APIs de diagnóstico; sem alegação de teste de ponteiro físico, áudio audível, aparelho ou campanha completa. A prévia usa uma câmera adicional do tabuleiro, renderizando os mesmos sprites e transformações que guiam as cartas no HUD; não é uma captura da interface completa.
