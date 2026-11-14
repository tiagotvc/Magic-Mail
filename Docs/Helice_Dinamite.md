# Helice + dinamite V2 - 11/11/2026

Referencia do usuario: dinamite+helice.webp, 67 quadros, 388x690; quadros de decolagem, voo orbital e chegada inspecionados. Nenhuma arte da referencia importada no jogo.

A dinamite e a helice usam a mesma familia orbital da combinacao com foguete: aproximacao, impulso, alternancia entre frente/fundo e voo baixo. A dinamite nao fica pendurada. Na chegada, a dupla converge enquanto o pavio queima por 0,32s; uma explosao normal de raio 2 aplica o impacto no destino. Sem explosao da dinamite na origem. Dano, barreiras e resistencia existentes preservados. Ambas as artes permanecem na camada Item mask, acima do cofre. Master PNG da dinamite e sprites existentes preservados.

Laboratorio: Royal Aves > Efeitos > Helice + dinamite > Abrir laboratorio. Tambem ha botao Helice + dinamite no painel de combinacoes. Grade temporaria 9x9, dupla na linha 5 colunas 5/6, helice a direita. Arrastar a helice para a esquerda. Alvo guiado na linha 7 coluna 6 deixa o alcance 5x5 inteiro visivel. Reinicio e controle de velocidade no painel. Sem campanha, recompensas ou boosts pagos.

Validacao nativa isolada: PASS, sete trocas de lado, carga visivel, uma explosao entregue, 25 celulas selecionadas e nenhuma explosao prematura. Regressoes foguete + helice (orbita e nove impactos no eixo) e duas helices (grupo de tres e tres chegadas distintas) PASS. Gravidade liberada em todos; runtime/editor zero erros. Sem teste de ponteiro fisico, audio audivel, dispositivo ou fluxo completo de vitoria.

Preview MP4/GIF: Tools/royal-postman/previews/dynamite-propeller-v2/index.html. Evidencia: review/dynamite-propeller-v2/. Manifesto: DYNAMITE-PROPELLER-V2.json. Avaliacao visual do usuario pendente.

Editor principal atualizado e cenario Dinamite + helice aberto em Playing apos o pedido. Confirmacao nativa main-open.result; sem captura de desktop nesta abertura.

Ajuste visual solicitado em 11/11/2026: itens das combinacoes giratorias ampliados em 25%. Multiplicador comum CombinationSpinScale=1.25 na dupla orbital e nas tres helices (preparacao e voo). Tamanho de repouso restaurado pelo ciclo existente de desativacao. Nao muda raio, alvos ou resistencia. Verificacao desta alteracao: compilacao runtime/editor, sem nova captura Play.

Correcao solicitada em 11/11/2026: aumento de escala mais evidente (multiplicador 1.60) limitado ao giro. Dupla orbital reduz o bonus ao fechar a orbita na chegada; grupo de tres aplica o bonus apenas na formacao e restaura a escala padrao de voo ao partir. Removido multiplicador adicional do tween de voo. Runtime/editor zero erros; sem nova captura Play nesta alteracao. Previas anteriores nao representam este ajuste de escala.

Regra de 12/11/2026: helice com carga segue navegacao/alvo normais; removidos arco baixo especial e alvo central forcado da dinamite. Ver Docs/Helice_Voo_Compartilhado.md e NORMAL-HELIX-DELIVERY-V1.json.
