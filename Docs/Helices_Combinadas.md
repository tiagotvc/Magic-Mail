# Helices combinadas - 11/11/2026

Referencia: helice-combinado.webp anexado pelo usuario, 65 quadros de 388x690. Quadros 46-54 mostram a fusao seguida de tres helices em triangulo e separacao. Nenhuma arte externa importada no jogo.

Implementacao: duas pecas consumidas uma vez; atracao de 0,22s; tres helices reais, com giro/expansao em triangulo por 0,58s e saida escalonada de 0,055s entre elas. Impacto ortogonal de decolagem aplicado uma vez. Cada voo reserva seu alvo e usa a logica existente de impacto. Camada Item mask, ordens acima do cofre. Terceira helice criada apenas se o dono ainda nao tem tres instancias; retorno ao estado comum e limpeza preservados. Nao altera campanha, economia, recompensas ou arte/prefab do cofre.

Menu: Royal Aves > Efeitos > Helices combinadas > Abrir laboratorio. Tambem ha botao Duas helices no painel de combinacoes existente. Cenario temporario 9x9, sem boosts pagos e sem resultado de campanha.

Verificacao isolada em Play: PASS, um grupo, tres helices ativas, expansao observada, tres chegadas a celulas distintas, limpeza e gravidade liberada. Regressoes foguete + helice orbital e dinamite + helice PASS. Runtime/editor zero erros. Previas reais MP4/GIF: Tools/royal-postman/previews/propeller-pair-v1/index.html. Evidencias review/propeller-pair-v1/, manifesto PROPELLER-PAIR-V1.json. Avaliacao visual do usuario pendente. Sem ponteiro fisico/audio audivel/dispositivo/fluxo completo de vitoria.

Editor principal atualizado e laboratorio Duas helices carregado em Playing, confirmado em Logs/explosive-package-lab-ready.result as 14:18 de 11/11/2026. Sem captura de desktop ou teste de ponteiro fisico nesta abertura.

Ajuste visual solicitado em 11/11/2026: itens das combinacoes giratorias ampliados em 25%. Multiplicador comum CombinationSpinScale=1.25 na dupla orbital e nas tres helices (preparacao e voo). Tamanho de repouso restaurado pelo ciclo existente de desativacao. Nao muda raio, alvos ou resistencia. Verificacao desta alteracao: compilacao runtime/editor, sem nova captura Play.

Correcao solicitada em 11/11/2026: aumento de escala mais evidente (multiplicador 1.60) limitado ao giro. Dupla orbital reduz o bonus ao fechar a orbita na chegada; grupo de tres aplica o bonus apenas na formacao e restaura a escala padrao de voo ao partir. Removido multiplicador adicional do tween de voo. Runtime/editor zero erros; sem nova captura Play nesta alteracao. Previas anteriores nao representam este ajuste de escala.

## V3 - fusao no ponto medio e saida continua (11/11/2026)

Usuario corrigiu a trajetoria: ambas as helices devem se juntar entre as celulas. Ambas as artes agora convergem para a media dos centros das duas casas; as tres nascem exatamente ali. Fusao de 0,12s, sem espera de 0,58s em triangulo parado; abertura ocorre durante o voo com entrada ease-out. Animacao e posicoes dos sprites sao restauradas em coordenadas locais na limpeza. Alvos, dano, contagem de tres e prioridade sobre o cofre mantidos.

Play isolado PASS: ponto medio verificado independentemente em cada uma das tres instancias; todas se deslocam mais de 0,12 unidades da propria posicao inicial em 0,083s apos a formacao. Um grupo, tres chegadas distintas, limpeza e gravidade liberada. Runtime/editor zero erros. Main Unity: novo READY Duas helices apos recompilacao. Preview MP4/GIF em Tools/royal-postman/previews/propeller-pair-v3/index.html, evidencia review/propeller-pair-v3/, manifesto PROPELLER-PAIR-V3.json. V1 fica como historico, V2 foi intermediaria durante o ajuste. Avaliacao visual do usuario pendente; sem ponteiro fisico/audio/dispositivo/vitoria completa.
