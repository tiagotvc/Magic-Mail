# Caixas e propaga??o de impactos

Revis?o de 20/10/2026: todas as caixas dentro do alcance do power-up recebem um impacto, inclusive caixas atr?s de outras caixas. A caixa n?o protege outra caixa.

Pe?as comuns atr?s das caixas s? recebem o impacto quando todas as caixas que interceptam aquele caminho est?o no ?ltimo n?vel e ser?o quebradas por esse dano. Uma caixa que ainda ter? resist?ncia depois do impacto continua protegendo as pe?as. Exemplo: foguete ? caixa com duas resist?ncias ? caixa com uma resist?ncia ? pe?a: as duas caixas s?o atingidas, mas a pe?a fica protegida pela primeira. Se as duas caixas tiverem uma resist?ncia, ambas quebram e a pe?a recebe o mesmo impacto.

`ImpactBarrierSnapshot` captura as posi??es e a resist?ncia exposta antes da ativa??o, distinguindo caixas ating?veis de caixas que sobreviver?o ao impacto. Isso permite selecionar as pe?as do mesmo impacto sem esperar pela anima??o de remo??o da caixa. A previs?o fica est?vel durante a ativa??o. Alcance, raio, resist?ncia removida por hit e prote??o contra impactos repetidos continuam regidos pelos fluxos existentes.

A regra compartilhada ? usada por foguetes, dinamite, combina??es, canh?o e filtros de alvos da bomba multicolorida. A prote??o diagonal considera tamb?m caixas que interceptam os cantos. A ?rea de efeito n?o aumenta para alcan?ar caixas al?m do raio.

Valida??o nativa executada com sucesso em `Tools > Magic Mail > Validate Crate Impact Barriers`: caminhos horizontais, verticais e diagonais; v?rias caixas consecutivas; resist?ncias mistas; penetra??o no ?ltimo hit; captura das camadas reais do tabuleiro; previs?o est?vel durante a ativa??o; limite de alcance. Resultado em `Logs/crate-impact.result`. Runtime e editor compilaram sem erros. Os testes validam sele??o de alvos e resist?ncias; uma partida completa em Play n?o foi executada nesta revis?o.
