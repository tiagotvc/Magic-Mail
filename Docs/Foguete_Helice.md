# Foguete + helice - 11/11/2026

Referencia anexada: Rocket%2BPropeller.webp (85 quadros, 388x690). Inspecionados os quadros de preparacao, voo, chegada e varredura da coluna. Sem importacao de arte externa para o jogo.

A combinacao consome o foguete parceiro sem dispara-lo na origem. Uma helice leva a arte existente do foguete como carga visivel abaixo do corpo; impacto nos vizinhos ortogonais da decolagem. Na chegada, o foguete dispara no eixo original: H limpa a linha, V limpa a coluna. O efeito existente de duas metades e rastro e usado; cada celula recebe o impacto quando a metade passa por ela. O caminho tambem atende destinos de obstaculo sem peca, sem depender de conversao de peca em foguete. Barreiras/resistencia existentes preservadas. Helice e carga continuam na camada Item mask acima do cofre.

Menu de laboratorio existente: Royal Aves > Efeitos > Pacote explosivo > Painel de testes. Novos cenarios foguete horizontal + helice e foguete vertical + helice. Sem placements em campanha, alteracoes de cofre, recompensas ou economia.

Runtime/editor compilam com zero erros. Validacao nativa em copia isolada: PASS nas duas orientacoes (um disparo entregue, nove celulas distintas no eixo correto, carga visivel, gravidade liberada), com regressao dinamite + helice PASS. Rotacao vertical tambem conferida em nova captura e novo PASS. Nenhuma afirmacao de ponteiro fisico, dispositivo, audio audivel ou fluxo completo de vitoria.

Previas: Tools/royal-postman/previews/rocket-propeller-v1/index.html (MP4/GIF), comparativo.png. Evidencia: Tools/royal-postman/production/review/rocket-propeller-v1/. Manifesto ROCKET-PROPELLER-V1.json. Capturas reais com camera dedicada ao campo, sem HUD/audio. Avaliacao visual do usuario pendente.

## Laboratorio aberto no Unity principal - 11/11/2026

Atalhos Royal Aves > Efeitos > Foguete + helice > Abrir laboratorio horizontal/vertical. Painel existente com troca direta H/V, reinicio e velocidade. Instalado e aberto no editor principal existente, sem segundo editor no mesmo projeto. READY Playing vertical confirmado, captura real da janela inspecionada: foguete na linha 5 coluna 5 e helice na coluna 6, 99 movimentos. Arrastar a helice uma casa para a esquerda. Sem troca fisica executada nesta abertura; verificacao de combinacao previa permanece na copia isolada. Evidencia review/rocket-propeller-lab-v1/. Runtime/editor compilam sem erros.

## V2 - coreografia orbital (11/11/2026)

Pedido do usuario: aproximar a animacao do WebP, pois V1 so carregava o foguete. Revisao quadro a quadro mostra os dois girando lado a lado. V2 substitui a carga pendurada por dois sprites em orbita eliptica, com profundidade alternada, impulso na preparacao e arco de voo baixo. Rotacao H/V do foguete preservada. Ambos ficam em Item mask, ordens 101/103, acima do cofre. Corpo original fica oculto apenas durante esta combinacao e e restaurado na limpeza. Dinamite nao usa esta animacao.

Runtime/editor zero erros. Play isolado PASS H/V: oito trocas de lado, um disparo entregue, nove celulas unicas no eixo correto, gravidade liberada. Regressao dinamite + helice PASS. Novos MP4/GIF em Tools/royal-postman/previews/rocket-propeller-v2/index.html. Evidencias review/rocket-propeller-v2/, manifesto ROCKET-PROPELLER-V2.json. Avaliacao visual do usuario pendente; V1 fica como historico.

Abertura final de V2 no editor principal nao confirmada: o processo principal foi fechado durante esta entrega. DLL principal ja contem o codigo orbital. Pedido automatico de reabertura removido para nao iniciar Play inesperadamente no proximo uso. Acesso manual pelos atalhos de laboratorio existentes.

Reabertura solicitada pelo usuario: Unity principal iniciado e READY do laboratorio vertical renovado em 11/11/2026, 14:04. Janela em segundo plano; Playing confirmado pelo controlador. Nenhuma troca fisica executada nesta abertura.

Ajuste visual solicitado em 11/11/2026: itens das combinacoes giratorias ampliados em 25%. Multiplicador comum CombinationSpinScale=1.25 na dupla orbital e nas tres helices (preparacao e voo). Tamanho de repouso restaurado pelo ciclo existente de desativacao. Nao muda raio, alvos ou resistencia. Verificacao desta alteracao: compilacao runtime/editor, sem nova captura Play.

Correcao solicitada em 11/11/2026: aumento de escala mais evidente (multiplicador 1.60) limitado ao giro. Dupla orbital reduz o bonus ao fechar a orbita na chegada; grupo de tres aplica o bonus apenas na formacao e restaura a escala padrao de voo ao partir. Removido multiplicador adicional do tween de voo. Runtime/editor zero erros; sem nova captura Play nesta alteracao. Previas anteriores nao representam este ajuste de escala.

Regra de 12/11/2026: helice com carga segue navegacao/alvo normais; removidos arco baixo especial e alvo central forcado da dinamite. Ver Docs/Helice_Voo_Compartilhado.md e NORMAL-HELIX-DELIVERY-V1.json.
