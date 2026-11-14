# Combinacoes da dinamite - 09/11/2026

Referencia do usuario: https://royalmatch.fandom.com/wiki/TNT_Combinations
Confirmacao primaria das regras: https://dreamgames.helpshift.com/hc/en/3-royal-match/faq/7-power-up-combinations/

## Regras e apresentacao

- Dinamite + foguete horizontal ou vertical: atracao do foguete, crescimento da dinamite e pavio queimando antes de liberar tres linhas e tres colunas. Dano acompanha a passagem dos foguetes. Uma celula na intersecao recebe apenas um impacto. Mantidas as barreiras locais e a resistencia dos obstaculos.
- Dinamite + helice: a helice carrega uma copia visual da dinamite. O parceiro original e consumido sem explodir na origem. Vizinhos ortogonais da decolagem recebem impacto; na chegada, pavio queima e ocorre explosao normal de raio 2, inclusive quando o destino e um obstaculo sem peca. Priorizacao de alvos considera concentracao de objetivos no alcance. Helice/carga ficam acima do cofre.
- Dinamite + bola de luz: a cor mais numerosa entre pecas comuns livres e transformada em dinamites, mantendo o fluxo existente de raios, reserva, marcacao e ativacao. Empates escolhem o menor ID de cor. A selecao dessa combinacao nao e bloqueada por caixas; cada explosao individual mantem suas barreiras. Outras combinacoes da bola de luz nao foram alteradas.
- Duas dinamites: preservado o pedido anterior do usuario de atingir o campo inteiro. Isso difere do raio 4 da referencia; nao houve regressao para o raio da referencia.

## Laboratorio

Menu Royal Aves > Efeitos > Pacote explosivo > Painel de testes.
Seis cenarios: simples, dupla, foguete horizontal, helice, bola de luz, foguete vertical. Arraste o especial da direita sobre a dinamite da esquerda. Os botoes diagnosticos usam APIs de jogo, nao ponteiro fisico. Sem escrita em niveis de campanha ou recompensas.

## Validacao

Runtime e editor compilam com zero erros via Tools/inkwell-compile.py. Verificacao nativa em copia isolada: PASS nos cinco cenarios por trocas reais iniciadas via API. Runner DynamiteCombinationsVerification somente aceita Logs/InkwellValidationProject, com company/product de preferencias separados. Sem nova arte raster, sem alteracoes no cofre aprovado ou na economia.

Resultados: foguete H/V = 43 pecas comuns cada; helice = carga visivel, um impacto entregue, zero explosoes de pacote na origem; bola de luz = 41 dinamites marcadas e ativadas; dupla = 79 pecas e um estouro. Gravidade liberada em todos. Nenhuma excecao de runtime ou erro de compilacao/shader no log final; avisos legados de Animator/IAP/servicos continuam presentes.

Evidencia consolidada: Tools/royal-postman/production/review/dynamite-combinations-v1/. Manifesto: DYNAMITE-COMBINATIONS-V1.json. Avaliacao visual final do usuario pendente. Sem teste de ponteiro fisico, audio audivel, dispositivo ou fluxo completo de vitoria/Continue.

## Previas de 10/11/2026

Galeria: Tools/royal-postman/previews/dynamite-combinations-v1/index.html. Cinco capturas reais em Play: dupla, foguete H/V, helice e bola de luz; MP4 640x640 a 24 fps e GIF 480x480 a 12 fps, com prancha comparativa. Camera dedicada ao tabuleiro, sem HUD/audio. A captura revelou carga encoberta pela helice: ajustadas a referencia do sprite e a posicao da dinamite abaixo dos limites dos frames de giro. Correcao visual, sem mudar regras. Nova captura confirma carga visivel; runtime/editor zero erros. Avaliacao visual do usuario continua pendente.

Atualizacao visual de 11/11/2026: helice + dinamite agora usa coreografia orbital, com novo laboratorio guiado. Ver Docs/Helice_Dinamite.md, DYNAMITE-PROPELLER-V2.json e previews/dynamite-propeller-v2/.
