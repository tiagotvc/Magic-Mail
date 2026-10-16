# Escaninho — início da criação

## Direção definida em 14/10/2026

Um único recipiente fixo ocupa quatro células em um quadrado 2×2. Os quatro nichos pertencem ao mesmo objeto; não são quatro obstáculos independentes. A dimensão anteriormente estava pendente no plano e passa a seguir a indicação do usuário.

Visual simples, seguindo a caixa de madeira atual: madeira quente, moldura arredondada, divisória em cruz, quatro envelopes grandes e poucos veios. Sem brasão, ferragens decorativas ou ornamentos. Placa inferior reservada ao contador aplicado pelo jogo, sem número gravado na imagem.

Primeiro conceito com transparência: `Assets/RoyalAves/Art/Obstacles/MailCubby/CM_escaninho_conceito_cheio_v1.png`. Gerado com a ferramenta integrada imagegen usando `CM_pilha_estado04_00.png` como referência de estilo. É um protótipo visual, ainda sem prefab ou integração no tabuleiro.

## Mecânica a prototipar

O guia prevê emissão de cartas por matches adjacentes e especiais até esgotar o estoque. Proposta inicial: uma carta por impacto válido; estoque compartilhado entre as quatro células; contador representa cartas restantes, independentemente da quantidade de nichos visíveis.

Antes da implementação, definir estoque inicial, destino do recipiente vazio e deduplicação quando um especial atinge várias partes do mesmo objeto. Validar bloqueio de queda nas quatro células e adjacência ao perímetro completo. Não produzir todos os estados finais antes de estabilizar essas regras.

## Próxima entrega

Protótipo jogável isolado 2×2 com estoque compartilhado, emissão/coleta e contador. Depois validar o visual no tamanho do tabuleiro e produzir estados/animações com a mesma moldura e âncora.

## Animação criada — 14/10/2026

Base vazia e envelope independente produzidos com imagegen, com transparência. A madeira é fixa; quatro cartas sobrepostas compõem os estados 4, 3, 2, 1 e 0 sem trocar a imagem do móvel. Cada impacto aceito provoca uma sacudida suave de 0,18 segundo e uma emissão em arco de 0,65 segundo, com redução de escala e fade na chegada. O contador de estoque diminui ao iniciar a saída; o evento de coleta ocorre ao terminar o voo. Direção atual: após a última entrega, o móvel quebra e desaparece, liberando as quatro células.

Prévia interativa: `Docs/Escaninho_Preview.html`. Abrir no navegador e usar Simular match adjacente ou Reproduzir sequência. O botão simula um impacto; não está ligado ao gameplay.

Controlador Unity: `MailCubbyAnimation.cs`, com fila de impactos, reinício e eventos `LetterCollected`/`Emptied`. `MailCubbyTools.cs` importa sprites e cria o prefab pelo menu **Royal Aves → Elementos → Escaninho → Criar prefab da animação**. A janela **Testar animação em Play** instancia uma prévia e permite emitir/reiniciar. Pedido de criação automática salvo em `Logs/mail-cubby-build.request`, a ser processado após o editor importar e compilar os scripts em modo de edição.

Verificação: compilação dos scripts de runtime e editor com o compilador e referências da Unity passou sem erros. Ainda falta confirmar execução da animação em Play e conectar o objeto ao tabuleiro 2×2, matches, especiais e metas reais.

Prompts usados na ferramenta integrada imagegen:

- Base: remover os quatro envelopes e lacres do conceito cheio, preencher os nichos com interior de madeira escura, preservar moldura, divisória, placa, proporções, canvas, perspectiva, cores e luz; fundo transparente, sem texto.
- Carta: envelope creme independente no mesmo estilo, visto de frente, cantos arredondados, aba triangular e lacre circular vermelho; poucas formas amplas, sem texto, móvel ou sombra externa; fundo transparente.

## Laboratório no tabuleiro

Menu: **Royal Aves → Elementos → Escaninho → Teste no tabuleiro 2x2**. Usa uma cópia isolada da fase 1, com 40 jogadas, quatro cartas de estoque/meta e um Escaninho central nas posições (2,2), (3,2), (2,3), (3,3), em coordenadas iniciadas em zero. Nenhuma fase da campanha é alterada.

As quatro células bloqueiam entrada/saída de peças e compartilham um controlador. Matches no perímetro e impactos de especiais passam pelo dano do Square. O primeiro impacto de uma janela de 0,20 segundo emite uma carta; outros impactos nessa janela são absorvidos, para evitar quatro emissões de um especial que atravesse o objeto. Esse contrato é provisório: a deduplicação por identificador de ativação precisa ser definida antes da integração na campanha.

A meta só diminui na conclusão do voo. Uma barreira na fila de animações mantém a resolução/vitória aguardando a emissão. O contador de estoque diminui ao iniciar o voo. Após a última entrega, o móvel quebra em fragmentos de madeira e libera as quatro células. Ao concluir ou esgotar jogadas, o laboratório sai do Play sem gravar avanço na campanha, seguindo os demais laboratórios.

O painel exibe estoque, cartas coletadas, meta, animação ativa e controle da duração do voo, com impacto manual e reinício completo. O reinício recria a fase, metas e cartas. O menu Desativar laboratório restaura o carregamento normal. Ativar caixa ou grama desativa o Escaninho.

Validação na Unity: assets do laboratório e prefab criados pela API do editor; compilação sem erros; teste aberto na cena de jogo. Impacto simultâneo nas quatro células resultou em estoque 3, coletadas 1, meta 3. Impacto numa célula adjacente passou pelo fluxo `Square.DestroyBlock` e resultou em 2/2/2. Dois impactos seguintes concluíram em estoque 0, coletadas 4, meta 0. Escala dos sprites foi corrigida no importador próprio para manter cartas dentro dos nichos. Captura do tabuleiro em `Logs/mail-cubby-preview.png`.

### Revisão: carta original, entrega e quebra

Por decisão do usuário, as cartas dos nichos agora reutilizam o envelope fechado já usado na tela de vitória: `Assets/RoyalAves/Art/Effects/EnvelopeOpen/envelope-open-1.png`. O arquivo histórico `Pieces/envelope.png` atualmente contém a pena roxa, por isso não foi usado. A escala e o posicionamento do envelope são calculados pelos limites visíveis, sem alterar sua arte ou configuração de importação.

Cada carta levanta, inclina e acelera em curva para além da borda direita da câmera (1,20 no viewport, altura 0,82). Permanece opaca e com tamanho legível durante todo o voo; só é ocultada após sair da tela. O rastro creme/dourado dura 0,24 segundo e afina/desaparece atrás dela. A duração padrão do voo passa a 0,95 segundo, ajustável no painel. A meta é contabilizada no fim da entrega.

Depois da última carta, a placa do contador é ocultada e o móvel se desfaz em doze fragmentos de madeira usando o efeito já existente da caixa, com som e pequenos brilhos. A animação de quebra dura 0,62 segundo e participa da fila do jogo. As quatro células só são liberadas ao terminar a quebra; a vitória aguarda o efeito completo. Reiniciar/sair cancela os efeitos da fase antiga.

Validação desta revisão em Play: primeira entrega terminou em estoque 3, coletadas 1, meta 3. As quatro entregas e a quebra finalizaram com **cartas=4, meta=0, células livres=4**, conforme `Logs/mail-cubby-completion.result`. A prévia HTML também usa o envelope original, rastro e fragmentos. Captura da arte original nos nichos: `Logs/mail-cubby-delivery-preview.png`.


Revisão cartunesca: a carta executa uma pirueta completa de 360° entre 8% e 68% do voo, com aceleração/desaceleração suave, e termina na inclinação de entrega. O rastro acompanha a trajetória. A prévia HTML usa o mesmo giro.


Revisão atual: o destino volta a ser o ícone do contador da meta. A posição é resolvida no HUD a cada impacto e convertida para o espaço da câmera quando o Canvas usa tela. A pirueta e o rastro são preservados; a carta reduz um pouco de tamanho ao encostar na meta, e a coleta continua no fim do voo.

Carta cartoon v2: Assets/RoyalAves/Art/Obstacles/MailCubby/CM_escaninho_carta_cartoon_v2.png, aplicada aos nichos e à meta. Gerada pela ferramenta integrada imagegen. Prompt: envelope creme simples e arredondado, lacre vermelho liso, formas volumosas e poucos detalhes, brilho amplo e sombreamento suave no estilo do laço do jogo, sem textura de papel, inscrições ou bordas listradas; fundo transparente.


## Armário postal com portas — revisão aprovada para implementação

Substitui o visual do laboratório por um armário inspirado no modelo de duas portas de vidro e quatro compartimentos. Corpo mais estreito (90% da largura das duas células); o corpo ocupa o espaço lógico 2×2 e o topo decorativo pode ultrapassar a linha acima, conforme instrução do usuário. A raiz avança 0,6 unidade em Z na direção da câmera; corpo, cartas e portas usam a camada Item mask nas ordens 30, 31 e 32, ficando à frente do grid e das peças próximas. Os efeitos de power-up continuam nas ordens superiores já existentes.

Cinco impactos no total: o primeiro arranca ambas as portas em 0,55 segundo, sem coletar cartas e sem alterar a meta. Os quatro seguintes entregam uma carta cada com a pirueta, o rastro e chegada à meta. Após a quarta entrega, a quebra libera as quatro células. A remoção das portas participa da fila de animações e consome integralmente o impacto que a iniciou. Reiniciar repõe portas e quatro cartas.

Artes produzidas com imagegen integrado: CM_armario_base_v1.png (corpo sem portas e sem cartas, quatro nichos, madeira cartoon e topo curvo) e CM_armario_porta_v1.png (porta esquerda independente com vidro de abertura transparente, refletida para formar a direita). Prompts: adaptar o armário aprovado para base vazia e mais estreita com quatro nichos; gerar uma porta isolada com madeira arredondada, maçaneta dourada e abertura de vidro transparente com reflexo azul discreto. Caminhos: Assets/RoyalAves/Art/Obstacles/MailCubby/.

Verificado em Play: impacto simultâneo nas quatro células removeu portas com estoque=4, coletadas=0, meta=4; impacto seguinte produziu estoque=3, coletadas=1, meta=3. A sequência inteira terminou com cartas=4, meta=0, células livres=4. Capturas: Logs/cabinet-doors-full.png e Logs/cabinet-doors-removed.png.


Ajuste de encaixe: os limites visíveis da madeira (alpha ≥ 128) são medidos no editor e serializados no prefab. A largura visível agora preenche 100% das duas células. O ponto mais baixo dos pés fica exatamente no limite inferior da área 2×2, e a altura total passa a 1,40 vezes a altura reservada, reduzindo um pouco o topo. A profundidade Z e a ordenação à frente do grid são preservadas. Captura: Logs/cabinet-grid-fit.png.


Ajuste das portas: encaixe calculado pelos limites alpha da própria porta, incluindo compensação do centro ao espelhar a direita. Cada porta mede 0,63 unidade local, com centros em ±0,32 e folga central de 0,01 unidade, eliminando a sobreposição das molduras e maçanetas.


Revisão visual: contador inferior oculto; altura total reduzida para 1,10 vezes a altura do espaço 2×2, preservando alinhamento inferior e largura total. Proporção das cartas ajustada ao novo encaixe. Grid global com azul claro uniforme, células arredondadas e filete suave; moldura reduzida de 0,45 para 0,28 no fator de espessura, em dourado mais simples, sem ornamentos sobre o tabuleiro.


As áreas entre os cantos arredondados recebem azul suave, evitando frestas pretas. A nova célula é desenhada em código e acompanha os layouts do tabuleiro, sem alterar fases ou regras.


Grid: os cantos externos do fundo azul agora têm recorte arredondado de raio 20% da célula, acompanhando a moldura. O recorte é aplicado apenas quando os dois lados do canto não têm vizinho jogável; as junções internas mantêm o azul contínuo.

