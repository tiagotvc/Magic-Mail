# Caixas como barreiras de impacto

Cada ativa??o captura as posi??es das caixas antes de atingir os alvos. O impacto pode acertar a primeira caixa, mas n?o as pe?as ou gramas atr?s dela. A mesma ativa??o continua bloqueada mesmo se destruir a ?ltima camada; uma ativa??o posterior volta a calcular o caminho.

A regra vale para linhas e colunas dos foguetes, combina??es com bombas, ?rea e rea??o visual das explos?es e sele??o/convers?o da bomba multicolorida. O canh?o considera a origem do tiro na base da coluna. As metades do foguete param na caixa, sem o avan?o usado para sair pela borda do tabuleiro. Diagonais n?o podem cortar o canto de uma caixa.

Valida??o: `Tools > Magic Mail > Validate Crate Impact Barriers` verifica os caminhos nas duas dire??es, o primeiro obst?culo, cantos diagonais, caminhos livres e a persist?ncia da barreira durante uma ativa??o. Compila??o de runtime/editor e execu??o dos mesmos cen?rios em harness gerenciado aprovadas. A confer?ncia visual em Play Mode permanece pendente.
