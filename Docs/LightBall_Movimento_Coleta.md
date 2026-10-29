# Movimento da multicolorida e coleta — V4

Referência fornecida em 29/10/2026: vídeo local
`YTDown.com_Shorts_Media_3nHGI5YwlNE_Level-5-Introducing-the-light-ball-Royal-Match_001_1080p.mp4`.
Vídeo de 41,05 s, aproximadamente 23,98 fps. Análise quadro a quadro do giro/ativação
em 8,34–10,67 s e da partida das peças a partir de 10,67 s. Quadros para análise em
`Logs/lightball-reference/`; nenhuma imagem do Royal Match foi importada como arte do jogo.

## Multicolorida

A referência mostra preparação breve, aumento de volume, suspensão e giro contínuo
durante a marcação; as peças saem depois que essa etapa termina. A adaptação conserva
a arte e os quadros existentes. A peça faz uma compressão de 90 ms, cresce até 1,16×
na horizontal, fica levemente achatada e sobe visualmente cerca de 0,09 célula.
A posição lógica do tabuleiro não muda.

O giro usa deltaTime e índice de quadro acumulado, acelerando no início; não depende
de esperar 18 ms por quadro e perder velocidade quando o frame rate varia. A revolução
em andamento termina antes da detonação. O rastro acompanha a mesma fase; os feixes
saem da posição visual da peça. O Animator fica suspenso durante esse movimento,
para não sobrescrever escala/posição, e é restaurado junto da pose no encerramento.
Cancelamento e reutilização restauram a pose. Seleção, reservas, dano, conversões e
simultaneidade das detonações continuam no fluxo existente. Raios amarelos V3 preservados.

## Peças coletadas

Somente `AnimateItems.target` usa o novo voo. Outros usos de queda dessa classe mantêm
o trajeto anterior. A peça sai com o tamanho visual real, dá um salto curto de 100 ms
com expansão até 1,12× e inclinação pequena; em seguida acelera numa curva até a meta.
O trecho de aproximação dura aproximadamente 0,38–0,58 s conforme a distância na tela.
Encolhimento concentrado na aproximação final; removida a rotação antiga de 1000°/s.

O voo resolve o centro do ícone pela câmera do Canvas e a câmera do jogo. Reutiliza
`MailDeliveryOverlay` para desenhar acima do HUD, sem o rastro de cartas nessa variante.
Na chegada, o ícone recebe um pulso curto de 160 ms, sem acumular escala em chegadas
simultâneas. A apresentação de cartas/pergaminhos existente não é redesenhada.

Peças comuns que são metas saem intactas também nos matches normais, sem gerar lascas
ao mesmo tempo. Peças que não são metas mantêm a quebra local. A contagem continua no
callback existente, somente ao chegar; cancelar o voo remove overlay e barreira sem
chamar a coleta. Reiniciar/trocar o nível invalida o callback do nível anterior.

## Verificação

Compilação runtime/editor: `Logs/lightball-motion-v4/*-build.log`.
Auditoria ampliada: rota normal de destruição de peça coletável, alvos pendentes até
chegar, um callback por chegada, inclinação limitada e cancelamento de um voo.
Os oito cenários de multicolorida existentes continuam no conjunto.

Resultados: `Logs/multicolor-audit.result` e `Logs/multicolor-audit-timeline.csv`;
captura de voo em `Logs/lightball-motion-v4/native-collection.png`.
Os testes usam Play nativo e chamadas/trocas programáticas, não ponteiro físico.
Sem alegação de playthrough completo, desempenho em aparelho ou equivalência exata
com a animação original. Aceite visual do usuário continua separado da auditoria.

Campanha, cenas, economia, recompensas e prefabs não foram reconstruídos. Backups das
fontes alteradas em `Logs/lightball-motion-v4/`.

Resultado final: PASS em nove cenarios nativos, incluindo os oito combos/limites
anteriores e a rota de coleta comum. Na rota comum foi verificado contador 999 antes
da chegada e 998 depois, com exatamente um voo. Nos casos de muitos alvos e dupla,
auditoria aguardou todos os voos e comparou os callbacks com a quantidade esperada.
Cancelar/destruir um voo nao chamou callback nem deixou barreira em animateItems.
A inclinacao dos sprites durante voo ficou abaixo de 26 graus na auditoria.

A montagem final usa diretamente o grid ja gerado e cancela a limpeza de matches
iniciais da bancada; duas tentativas anteriores ficaram esperando essa preparacao.
Essa massa isolada nao valida o carregamento normal de uma fase nem o fluxo completo
de menus/vitoria. Capturas finais sao da bancada, com enquadramento proprio do auditor.
Copias dos resultados: `Logs/lightball-motion-v4/native-audit.result` e
`native-timeline.csv`; ativacao em `native-activation.png`.
