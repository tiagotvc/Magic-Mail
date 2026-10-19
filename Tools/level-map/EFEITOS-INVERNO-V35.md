# Encaixes de inverno por objeto

No editor do mapa, ative **Editar objetos**, selecione uma peça no preview e abra **Encaixe dos efeitos neste objeto**, abaixo de **Adicionar tema**. Os grupos **Neve**, **Luzes de Natal** e **Gelo** têm controles independentes de ativo, posição em porcentagem da altura da peça, rotação e escala X/Y. **Restaurar** devolve o encaixe automático daquele efeito. **Salvar disposição** grava os ajustes junto do objeto.

Use o preview de Natal ou o tema **Gelo** para visualizar. A posição é relativa ao objeto: o encaixe acompanha tamanho, espelhamento e profundidade. A rotação acontece em torno do grupo do efeito, sem mover o objeto original. Neve usa pivô a 55% da altura; luzes, a 60%; gelo, a 28%. A opção Ativo também permite habilitar luzes e gelo em um arbusto. A disposição antiga continua com escala unitária e encaixes automáticos. Alterações feitas pelo editor participam do Undo do layout.

A cobertura de neve pelo contorno alpha agora é uma camada separada, ajustável como as coberturas pintadas do arbusto coral e do pinheiro. Mover essa camada não altera o sprite original. Os efeitos mantêm a arte existente; não foram gerados novos PNGs nesta revisão.

As lâmpadas existentes alternam entre pouca luz e brilho forte, com fases diferentes. O gelo recebe um reflexo branco fino em movimento e um pequeno brilho em estrela. O preview continua atualizando enquanto esses efeitos estão visíveis.

O chão ganhou sombra azulada nas bordas dos montes de neve, crista branca e reflexo mais amplo para sugerir volume e glossy. Há pequenas porções espaçadas nas margens internas da trilha; a quantidade geral de neve permanece reduzida. As listras da grama e da trilha continuam independentes.

## Validação

`WinterV35Validation.Run` usa um projeto Unity isolado. Confere a persistência após salvar e reimportar um layout de teste, a conversão Entry/Placement, valores antigos, transformações independentes dos três grupos, posição original preservada, coberturas separadas, ativação em arbusto, reset, remoção do tema, amplitude do piscar e parâmetros do brilho animado. Captura a disposição salva em 1080×1920 e 1080×2280. Não salva a cena, os layouts ou a temporada ativa do projeto principal. Interação manual do mouse e desempenho em aparelho físico não são medidos por essa verificação.

[Captura 1080×1920](previews/winter-v35/map-1080x1920.png) · [Captura 1080×2280](previews/winter-v35/map-1080x2280.png) · [Encaixes independentes](previews/winter-v35/independent-effects.png) · [Resultado](previews/winter-v35/validation.txt)
