# Light Ball — brilho dos alvos V5

Correção solicitada pelo usuário: as peças marcadas pelos raios quase não brilhavam.
O efeito anterior desenhava sobretudo cópias atrás da arte, com borda de 2,5% e halo
fraco. A peça original escondia grande parte desse destaque.

## Alteração

- Luz aplicada também sobre a face da peça, por uma cópia com o mesmo Sprite e escala.
- Clarão no contato por aproximadamente 140 ms; luz sustentada pulsando enquanto
  o alvo aguarda a detonação. Passagem de luz suave na superfície.
- A contribuição luminosa usa a margem até branco de cada pixel, em vez de somar
  branco uniforme e apagar o desenho. Cores e sombras permanecem reconhecíveis.
- Borda de 6,5% e halo de 18%, mais fortes que os anteriores.
- Superfícies acima da camada superior da peça composta, incluindo especiais.
- Luz, borda e halo desligados antes da fronteira de detonação e no cancelamento;
  sprites das cópias são removidos imediatamente para não entrarem na coleta.

Arte original, raios amarelos, giro, reservas, conversões e momento da coleta não
foram redesenhados. Material compartilhado em
`Assets/RoyalAves/Resources/ElectricEffects/TargetCharge.mat`; shader próprio em
`Assets/RoyalAves/Shaders/MulticolorTargetCharge.shader`. Sem PNG novo.

## Verificação

Compilações em `Logs/lightball-target-glow-v5/`. Auditoria exige superfície luminosa
visível e acima da arte no momento da marcação, material correto e ausência de luz
na detonação/cancelamento. A massa também funciona com as duas metas do laboratório.

Resultados nativos em `Logs/multicolor-audit.result`; captura durante marcação em
`Logs/multicolor-flow-v2/native-flow.png`. O laboratório continua acessível em
**Royal Aves > Efeitos > Light Ball > Laboratório no jogo**.
Aceite visual do usuário é separado dos testes; não há alegação de FPS em dispositivo.

Resultado final: nove cenarios nativos PASS, incluindo alvos comuns, dinamite, foguete,
helice, dupla multicolorida, cancelamento/reutilizacao e coleta comum. Conferida luz
visivel sobre a arte, material correto e limpeza antes da detonacao. Smoke da bancada
interativa tambem PASS, com metas e voos; bancada reaberta limpa em Playing.

A primeira captura saturou os alvos em branco e foi corrigida para preservar a arte.
A auditoria tambem detectou a ordenacao de pecas compostas; as superficies agora usam
a camada superior de cada conjunto. Essas tentativas nao foram consideradas aprovadas.
Relatorios e capturas finais em `Logs/lightball-target-glow-v5/`.
