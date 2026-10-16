# Moldura do tabuleiro

A moldura da tela de jogo é construída por `BoardBorderRenderer`, acompanhando o formato do tabuleiro, seus recortes e espaços vazios.

- Espessura reduzida pela metade: faixa opaca total de 0,1065 célula, antes 0,213.
- Mantidos o dourado, os reflexos e o filete azul interno.
- Cantos arredondados com 48 segmentos por curva, antes 12, e raio de 0,20 célula para cobrir o encontro com o piso.
- Transição de alpha de aproximadamente 0,75 pixel nas duas silhuetas externas para suavizar o serrilhado. As faixas douradas permanecem opacas e nítidas.
- A grama fica contida no perímetro do tabuleiro; a sobreposição orgânica dentro das células jogáveis continua fixa desde o início da fase.

A geometria acompanha a resolução de renderização, sem ampliar uma imagem pequena. O ajuste entra no próximo carregamento do tabuleiro. Prévia da conferência em `Logs/board-frame-thin-preview.png`.

Validado: scripts compilados sem erros; teste carregado na Unity; prévia do tabuleiro conferida com moldura fina, cantos sem pontas e grama contida no perímetro.
