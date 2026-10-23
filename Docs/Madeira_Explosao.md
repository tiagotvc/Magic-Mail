# Quebra de madeira — revisão 23/10/2026

Referência analisada: GIF de Royal Match enviado pelo usuário. Aproximadamente em 13,0–13,9 s, a estrutura vazia estoura em fragmentos dourados angulares de vários tamanhos, primeiro para fora e para cima, depois girando e caindo. Esse comportamento substitui as lascas antigas quase circulares.

`MaterialBurstEffect` cria pedaços de madeira por geometria própria, sem recortar o PNG em discos: pontas partidas, proporção comprimento/espessura de aproximadamente 3:1 a 6:1, faces com luz/sombra e dois veios longitudinais. Há uma mistura de pedaços maiores de tábua, lascas menores e resíduos muito pequenos. Só os resíduos curtos desaparecem por fade; os demais caem até sair da tela (limite defensivo de cinco segundos). A dispersão é radial, com impulso para cima e rotações independentes. Um mesh e material por explosão.

- Caixa: 18 pedaços na quebra final; 10 em impactos das tábuas, mais 10 resíduos pequenos em cada caso. Mantida a tábua inteira se desprendendo, os sons, o clarão e os tempos de resistência/coleta existentes.
- Escaninho: 28 pedaços, mais 10 resíduos, distribuídos nas laterais e nas regiões de prateleira. A quebra final informa explicitamente que é um armário ao `ParcelTearEffect`; as portas continuam se soltando inteiras no primeiro impacto. Mantidas cinco etapas de dano, emissão/coleta de cartas e liberação das quatro células no callback existente.
- Matches comuns, ovo e gelo conservam seus efeitos próprios. Os efeitos antigos de lascas arredondadas foram removidos do caminho da madeira.

Usar os laboratórios existentes **Royal Aves → Elementos → Escaninho → Teste no tabuleiro 2x2** e **Royal Aves → Elementos → Pilha de Encomendas → Teste das três caixas de madeira**. As mudanças já estão nos caminhos normais do jogo; não exigem regenerar arte.

Validação: runtime e editor compilados sem erros; contornos de líquido, vidro e madeira têm triangulação consistente e não degenerada. Conferidas 2000 combinações de escala, duração de queda e proporção das lascas. Aparência e intensidade ainda precisam de avaliação no jogo, especialmente a cor da madeira em relação às duas artes originais.
