# Multicolorida — fluxo luminoso, 29/10/2026

Direção solicitada: aproximar o giro e a energia do vídeo enviado pelo usuário:
https://www.youtube.com/shorts/7-34U_-kAgU
Análise visual do trecho aproximadamente 14–15,6 s, a partir do vídeo acessado na sessão.
Quadros de análise: `Logs/video-reference/multicolor-active-frames.jpg`.
Nenhum quadro do vídeo foi importado como arte do jogo.

## Alteração visual

- Feixes quase retos e suaves, núcleo branco quente, halo dourado discreto e ponta arredondada.
- Removidos ruído perpendicular, zigue-zague e ramificações.
- Cada feixe leva os mesmos 45 ms para tocar o alvo; sua decoração desaparece em 210 ms.
  O intervalo de marcação continua em 80 ms, deixando normalmente dois ou três feixes visíveis.
- O feixe invisível mantém sua reserva no pool até EndSpin; não pode ser reciclado para
  outro dono e depois liberado indevidamente pela sequência antiga.
- Rastro elíptico luminoso acompanha o período dos quadros de giro existentes, com cauda
  azul/violeta e cabeça branca/dourada. Pool de 16 rastros, preparado no início da cena.
- Alvos recebem brilho quente mais contido e pequenas estrelas; menos partículas contínuas.
- Fim da marcação e cancelamento recolhem o rastro junto dos feixes e cargas.

Os quadros do globo, a animação de entrada, as regras de seleção, reservas, conversões,
tempo de giro e dano permanecem existentes. Todos os alvos continuam sendo marcados antes
da detonação simultânea. Não foram alterados campanha, preços, recompensas ou prefabs.

## Verificação

Compilação runtime/editor com as referências da Unity, em `Logs/multicolor-flow-v2/`.
Auditoria nativa existente ampliada para exigir também ausência de rastro na fronteira
de detonação/cancelamento. A massa temporária define paleta e uma meta de peças explícita,
para não depender da meta da bancada aberta. Nada dessa massa é salvo em assets.

Captura durante marcação: `Logs/multicolor-flow-v2/native-flow.png`.
Resultado dos oito cenários: `Logs/multicolor-audit.result`; sequência por quadro em
`Logs/multicolor-audit-timeline.csv`. Consulte o resultado para o estado da execução.
Auditoria usa chamadas de jogo e trocas programáticas; não substitui aceite por ponteiro,
aparelho físico, som ou partida completa. Sem medição de FPS em dispositivo.

Backups das três fontes visuais anteriores em `Logs/multicolor-flow-v2/*-before.*`.
O documento `Multicolor_Raios_Eletricos.md` descreve a sequência e o histórico anterior;
esta revisão substitui sua seção de aparência elétrica com ruído/ramificações.

Resultado final: PASS nos oito cenarios em Play (sem alvos; um alvo; muitos com dano
externo; dinamite; foguete; helice; dupla multicolorida; cancelamento e reutilizacao).
O giro concluiu antes das detonacoes; nenhum feixe ou rastro sobreviveu a essa fronteira.
Conferidas as coletas por voo e a mistura de pecas com/sem meta. Copias dos resultados
em `Logs/multicolor-flow-v2/native-audit.result` e `native-timeline.csv`.
A primeira execucao detectou que a bancada de pedra nao tinha metas de pecas coletaveis;
a massa de auditoria foi corrigida para definir sua propria meta temporaria antes da
execucao final. Nao houve alteracao da meta de pedra nem de assets de campanha.

Revisao final da auditoria: tambem corrigida a geracao da massa sem alvos, pois GenItem
normalizava as cores pela paleta. As cores da massa agora sao impostas apos gerar;
AllMarked exige explicitamente zero alvos nesse caso e um alvo no caso single.
Execucao final PASS: empty=0 destruicoes; single=1; many/dinamite/foguete/helice=13
cada; dupla=47; cancelamento=0. O arquivo de timeline arquivado corresponde a esta execucao.

## Revisao amarela V3

Correcao solicitada pelo usuario: os raios V2 ainda pareciam finos e brancos.
Largura nominal do corpo: 0,13 -> 0,28 celula; halo: 0,24 -> 0,52 celula.
Corpo amarelo-claro (1, 0.90, 0.38); halo dourado (1, 0.68, 0.04).
O corpo usa BeamBody.mat com alpha blend, para preservar o amarelo sobre o tabuleiro
claro em vez de soma aditiva saturando em branco. O halo continua aditivo. O shader
compartilhado expoe fatores de blend com os mesmos valores aditivos anteriores por
padrao; so BeamBody muda o destino para OneMinusSrcAlpha. Rastro, estrelas, anel,
tempos de marcacao/detonacao e regras mantidos. Fontes anteriores e compilacao em
`Logs/multicolor-yellow-v3/`.
`nV3: compilacao runtime sem erros; oito cenarios nativos PASS novamente. Captura visual amarela conferida em Logs/multicolor-yellow-v3/native-flow.png. Sem aceite visual do usuario inferido.

## Reversao do amarelo (29 outubro 2026)
A pedido do usuario, Lightning voltou exatamente aos parametros V2 anteriores ao amarelo: corpo branco luminoso aditivo Ribbon, largura 0,13 celula e halo 0,24. A classe Lightning foi comparada com o backup pre-V3 sem diferencas. Mantidos o giro sincronizado V4, coleta e brilho dos alvos V5. Compilacao runtime: zero erros (Logs/lightball-ray-revert-build.log).

## Raios irregulares (29 outubro 2026)
Trajetoria agora usa duas escalas de ruido continuo com curvaturas independentes por disparo e movimento no tempo. Desvio limitado a 0,48 celula e reduzido em raios curtos; extremidades permanecem presas a origem/alvo. Mantidos material branco, larguras, duracao, brilho dos alvos e regras. Compilacao runtime sem erros: Logs/lightball-irregular-rays-build.log.
