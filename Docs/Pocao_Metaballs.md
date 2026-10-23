# Estudo visual de poção com metaballs — 23/10/2026

## Teste no Game View

### Laboratório na fase jogável

Limiar atual do leque: começa após o corpo descer uma célula desde sua posição inicial. A altura é medida entre duas células verticalmente adjacentes e convertida para o espaço local do efeito; substitui a referência anterior à última linha da grade. Velocidade, abertura do leque e quebra do frasco mantidas.

Dispersão revisada: o início do leque usa agora a última linha real do `FieldBoard`, antecipado para a frente do líquido estar próxima da saída da grade. Substitui o limiar de 78% do caminho até a borda da câmera. Abertura lateral mais ampla e irregular, com entrada suave e domínio de renderização ampliado para não cortar o leque. Antes disso, corpo levemente afunilado e alongamento maior durante a descida. Mantidos o ritmo 2×, volume dos campos e animação do vidro. Compilação aprovada; conferir amplitude e momento no laboratório.

Revisão de continuidade: a troca para o sprite de quebra agora compensa também a posição horizontal do aro nos quatro frascos intactos, medida por quadrante, além dos pivôs entre quadros. O reset restaura a posição inicial. O líquido usa campos mais próximos numa descida conjunta, com deformação suave; a dispersão individual começa após percorrer 78% da distância entre a origem e a borda inferior da câmera. Mantidos relógio a 2× e tempo do vidro. Compilação aprovada e conferidos numericamente os quatro alinhamentos, conexão dos campos vizinhos antes da dispersão e limiar relativo à câmera. Aparência final ainda requer avaliação no laboratório. HTML e janela de estudo isolados mantêm a versão anterior.

Ritmo aprovado com ajuste: o relógio do efeito metaballs em jogo roda a 2×, reduzindo pela metade o tempo de escoamento, incluindo alongamento, separação e saída da tela. Percurso e quantidade de gotas preservados; duração e início dos quadros de quebra do frasco mantidos.

**Royal Aves → Elementos → Poções → Laboratório metaballs no jogo** (ou **Testar metaballs no jogo**) abre o laboratório real existente: tabuleiro 7×7, central 2×2, quatro frascos, meta de quatro poções e 35 jogadas, com HUD e enquadramento normais do jogo. O painel Poções inclui **Abrir / reiniciar laboratório metaballs**. A troca inicial preparada é (1,2) com (2,2), em coordenadas a partir de zero; há um foguete vertical em (3,5). Matches adjacentes da cor correspondente e impactos de especiais disparam vidro + escoamento metaballs.

A variante é configurada na criação de cada central do laboratório, antes do primeiro impacto. **Desativar laboratório** limpa a seleção; abrir o laboratório comum também restaura o efeito comum. A campanha não é editada. A prévia isolada descrita abaixo foi movida para **Prévia visual metaballs (sem jogadas)**. Compilação de runtime/editor aprovada; execução visual da fase ainda precisa ser conferida na Unity.

Origem inferior: no efeito em jogo, a massa inicial fica abaixo da origem de emissão, com margem para o raio das gotas. Essa origem está 0,65 unidade local abaixo do centro do frasco. Corrige a distribuição anterior, cujas gotas começavam até 0,7 unidade acima da origem, levando o líquido ao topo do frasco. A prévia HTML independente conserva seu enquadramento original.

Revisão: a variante mantém agora os 12 quadros de rachadura e ruptura do vidro. O líquido metaballs começa no quinto quadro, quando o vidro se abre, sobreposto ao respingo da arte e deslocado para a parte inferior do frasco. A sequência de vidro termina enquanto o líquido continua sua queda independente. Isso substitui o corte após o quarto quadro da primeira versão no jogo. A spritesheet ainda contém líquido desenhado junto com os cacos; a sobreposição visual precisa ser conferida no Game View.

Usar **Royal Aves → Elementos → Poções → Testar metaballs no jogo**. Sempre abre a cena existente `RouteCentralVisual` e inicia o teste; se já estiver em Play, encerra primeiro e aguarda a saída antes de abrir a cena. Isso evita herdar câmera e enquadramento de uma fase/mapa em andamento. Clicar nos frascos ou nos botões de cor dispara rachadura e líquido metaballs transparente sobre o tabuleiro. **Repetir teste** limpa o líquido e restaura os frascos. Ao sair do Play, a variante é desativada; não altera prefabs nem fases da campanha.

A queda usa uma malha retangular transparente, com domínio ajustado à borda inferior da câmera. Cada poção instancia seu próprio material e seus 28 campos; mesh e material são liberados ao concluir ou reiniciar. Ainda não inclui cacos independentes. Runtime e editor compilados sem erros; shader e execução no Game View aguardam conferência visual. As limitações de integração descritas abaixo se referem à primeira entrega.

Abrir `Pocao_Metaballs.html` no navegador para reproduzir, pausar, avançar manualmente, trocar de cor e ajustar velocidade e atraso do escoamento. Funciona localmente, sem dependências de rede. `Pocao_Metaballs.gif` mostra uma prévia das mesmas equações, renderizada em CPU; `Pocao_Metaballs_Estudo.png` compara 0 / 0,4 / 0,8 / 1,2 s. Essas imagens não são capturas nativas da Unity.

Na Unity: **Royal Aves → Elementos → Poções → Protótipo de poção metaballs**. A janela funciona fora do Play, sem abrir cenas nem alterar o prefab atual. Usa um shader próprio e RenderTexture, limitada a 720 × 1000 pixels. A versão HTML usa WebGL com a mesma distribuição e movimento das gotas.

São 28 campos gaussianos que se fundem numa superfície com borda iluminada. As gotas inferiores começam a cair primeiro e as superiores acompanham, formando ligações que se afinam e rompem. Cada campo conserva seu raio e intensidade; não há fade nem remoção de gotas no meio da queda. A massa finita escoa e sai pela borda inferior. O loop reinicia após 2,8 s.

Limitações: é um efeito visual, sem simulação de pressão, colisões ou conservação física exata de volume. A área aparente muda com a sobreposição dos campos. Ainda não inclui frasco, quebra, cacos nem integração com o tabuleiro; o fundo quadriculado dá referência de escala e movimento. O desempenho em mobile não foi medido.

Validação: o novo código C# foi incluído explicitamente em compilação de editor e compilou sem erros. A tentativa de captura nativa num projeto Unity isolado ficou bloqueada pela inicialização da licença e foi encerrada. A janela e o shader ainda precisam de conferência no editor do projeto; a prévia de navegador não foi validada automaticamente. Efeito existente preservado.
