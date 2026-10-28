# Escaninho — início da criação

## Quebra da madeira revisada — 23/10/2026

## Ritmo acelerado do armário

**Preferência final do usuário:** manter somente a entrega das cartas acelerada. Abertura, queda das portas, reação do móvel e quebra/liberação final voltam à velocidade original. Configuração atual do prefab e do gerador: `playbackSpeed = 1`, `emissionDuration = 0,22` e `letterStagger = 0,01`. Isso preserva exatamente o voo de 0,22 s e o intervalo nominal de 0,01 s da entrega aprovada, sem o multiplicador 2× nos demais movimentos. Meta e quantidade de cartas permanecem iguais. As revisões abaixo são históricas.

Revisão focada na entrega, por feedback do usuário: `emissionDuration = 0,44` e `letterStagger = 0,02`, mantendo velocidade 2×. Voo efetivo de 0,22 s e intervalo nominal de 0,01 s; lote de nove cartas em aproximadamente 0,4–0,5 s, dependendo dos frames. Abertura e quebra mantêm o ajuste anterior. A meta continua contando na chegada real de cada carta; não é antecipada para encurtar o fluxo. Prefab e gerador atualizados juntos. Os tempos listados abaixo descrevem a primeira revisão de velocidade e foram substituídos para a entrega.

Velocidade do armário ajustada para **2×** no prefab visual e no gerador `MailCubbyTools`. O componente compartilhado `MailCubbyAnimation` mantém velocidade padrão 1×; a franqueadora conserva seu ritmo.

- Reação à retirada de cartas: 0,13 → 0,065 s.
- Intervalo entre as nove cartas: 0,12 → 0,06 s.
- Voo de cada carta: 0,95 → 0,475 s.
- Um lote de nove cartas: aproximadamente 2,04 → 1,02 s, incluindo reação, espaçamento e último voo (antes da quantização por frames).
- Reação da abertura: 0,18 → 0,09 s; queda das portas acompanha a mesma aceleração.
- Sequência de quebra/liberação final: velocidade dobrada, mantendo o multiplicador global de feedback existente. Lascas decorativas mantêm seu comportamento e não seguram a liberação.

A meta continua sendo decrementada na chegada de cada carta; continuam sendo 36 cartas por armário. O estoque, os cinco impactos e as barreiras de conclusão foram preservados. Runtime/editor compilados via MSBuild; conferência visual no Unity pendente. Backup do prefab anterior em `Backups/MailCubby-before-speed-2x`.

A quebra final agora dispara pedaços angulares de tábuas com veios, faces claras/escuras, impulso radial e rotação própria, mais resíduos pequenos e breves. São 28 pedaços e 10 resíduos; não usa mais os recortes arredondados do PNG. Mantidos portas inteiras no primeiro impacto, entrega de cartas e callback final de liberação. Detalhes e validação em [Madeira_Explosao.md](Madeira_Explosao.md).

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


## Uso no criador de fases — correção de 19/10/2026

Sintoma relatado: as células pintadas com o armário ficavam com um bloqueio invisível, sem peça caindo. Causa: o móvel é um único objeto 2×2 construído pela célula superior-esquerda (`MailCubbyCell.IsGroupAnchor`), mas o prefab tem `sizeInSquares` 1×1 e o pincel do editor pintava uma célula por clique. Células soltas não encontram âncora, não criam o móvel, e o `SpriteRenderer` da célula é desligado no `LateUpdate` — sobra um bloqueio sem arte. Comprovado em `Level_11`, com duas células isoladas em (1,3) e (6,4).

Correções aplicadas:

- `LevelMakerEditor`: pincel próprio do armário. Um clique em célula livre carimba o 2×2 inteiro a partir dela; um clique em qualquer célula de um armário existente apaga o armário todo. O carimbo é recusado, com aviso na janela, se o 2×2 não couber na borda, encostar em outro armário ou cobrir um buraco. O pincel "-1" continua operando camada a camada.
- `LevelMakerEditor.SaveLevel`: `ValidateMailCubbyGroups` avisa na janela e no console se o nível ainda tiver células de armário fora de um 2×2 completo, listando subfase e coordenadas.
- `MailCubbyCell`: célula órfã não vira mais parede invisível. Depois de dois quadros sem dono, ela se remove com aviso no console, mantendo o tabuleiro jogável.

Pendência conhecida: a prévia do editor desenha o armário inteiro em cada uma das quatro células, então um armário aparece como quatro. Falta também um tipo de meta próprio: o laboratório usa o índice de `SolidBlock` com o ícone da carta, e `TargetEditorScriptable` ainda não tem entrada para as cartas do armário.

### Causa do "sumiço de linhas" no criador de fases — 19/10/2026

O log da Unity apontou o erro real: `NullReferenceException` em `SquareBlocks.SortMergeBlocks` (`LevelData.cs:461`), chamada por `SquareUtils.GetSquareTextures` dentro de `LevelMakerEditor.GUIGameField`. A exceção estourava no meio do `OnGUI`, então a grade parava de ser desenhada a partir da célula do armário — daí as linhas sumirem, seguidas de "Invalid GUILayout state".

`SortMergeBlocks` faz `Resources.LoadAll<BindLayer>("Blocks")` e, para cada camada da célula, procura o prefab pelo nome do enum e chama `GetComponent<LayeredBlock>()` no resultado. `MailCubby.prefab` e `FrankingMachine.prefab` eram os únicos prefabs de bloco **sem o componente `BindLayer`**, então o `Find` devolvia nulo e a chamada quebrava. O gelo não quebra porque já tinha `BindLayer` (order 5).

Correções: `BindLayer` com `order: 6` (mesmo do SolidBlock) adicionado aos dois prefabs, e proteção contra nulo em `SortMergeBlocks`, para que um bloco futuro sem `BindLayer` seja tratado como não-camadado em vez de derrubar a grade inteira do editor.

## Meta própria do armário — 19/10/2026

**Histórico:** os valores abaixo foram substituídos pela revisão de dois nichos amplos no fim do documento.

Pedido do usuário: o armário deve seguir a lógica da grama, do gelo e da caixa. Ao ser colocado, a fase ganha uma meta de cartas igual a **4 × o número de armários**, porque cada móvel carrega quatro cartas, e as cartas voam até o contador da meta.

- Novo tipo de objetivo **MailCubby** em `TargetEditorScriptable`, com `setCount = FromLevel`, ícone da carta (`CM_escaninho_carta_cartoon_v2`) e prefab `Resources/Blocks/MailCubby.prefab`. Rótulo no editor: "Cartas do armário postal".
- `MailCubbyCell.CountLetters(levelData)` conta as células de armário da fase. Como cada armário ocupa quatro células e entrega quatro cartas, a contagem de células **é** a quantidade de cartas. É lida dos dados da fase, então o número já aparece correto na tela inicial, antes de o tabuleiro existir.
- `SweetSugar.Scripts.TargetScripts.MailCubby` (em `MailCubbyTarget.cs`) atende o carregador legado quando o armário é a meta principal, no mesmo molde de `IceBlock`.
- `TargetCounter` recebeu o armário na mesma exceção do gelo: a quantidade inicial vem dos dados da fase, e não de componentes de cena. `LevelData.SetSquareTarget` deixa de somar uma unidade por célula para o armário, evitando contagem dobrada.
- `LevelMakerEditor`: carimbar um armário adiciona a meta sozinho, e o painel mostra "Quantidade automática pelo tabuleiro: N (x armário(s) x 4 cartas)". Fases antigas recebem a meta ao salvar.
- `MailCubbyBoardController` passa a procurar a meta pelo nome `MailCubby` antes de cair no casamento por sprite, porque a meta da franqueadora usa o mesmo ícone de carta. Vale para o destino do voo e para a baixa no contador.

## Novo desenho e ciclo de impactos — 19/10/2026

Direção visual aprovada para explorar: armário postal cartoon com moldura roxa e dourada, quatro nichos abertos em grade 2×2 e vários envelopes visíveis em cada nicho. A quantidade desenhada em cada nicho deve corresponder ao número de cartas que ele entrega. O desenho pode mostrar mais cartas do que o conceito anterior, sem aumentar a quantidade de impactos necessária para esvaziar e remover o armário.

**Ciclo completo: cinco impactos aceitos por armário.**

1. Primeiro impacto: as duas portas se soltam e caem; nenhum nicho é limpo, nenhuma carta voa e a meta não muda.
2. Segundo ao quinto impacto: cada impacto limpa um nicho ainda cheio. Todas as cartas daquele nicho saem em sequência, fazem a pirueta e seguem pelo rastro até o contador da meta. Cada carta reduz a meta ao concluir o voo.
3. Depois que o quarto nicho termina de entregar suas cartas, o armário se quebra e libera as quatro células, como no comportamento atual. A quebra aguarda a fila de voos.

Assim, preservam-se os cinco impactos totais: um para as portas e quatro para os nichos; não se acrescenta um impacto separado para a quebra final. Os impactos continuam deduplicados por ativação, para que um especial que alcance várias células do armário não limpe vários nichos de uma vez.

A quantidade da meta deixa de ser quatro cartas por armário. A primeira implementação usa três cartas por nicho, doze por armário; múltiplos armários somam seus estoques. A quebra visual de um nicho remove todas as cartas dele do móvel enquanto seus envelopes voam individualmente.

Efeitos a produzir para o novo conceito: arrancar e derrubar portas no primeiro impacto; feedback de acerto e limpeza clara do nicho atingido; ejeção escalonada das cartas, cada uma voando ao HUD; reação leve do corpo a cada impacto; quebra do armário vazio após a última entrega. Reutilizar a pirueta, o rastro, o voo ao contador e o efeito de quebra já existentes quando possível. A animação deve manter a resolução do tabuleiro aguardando voos e quebra, como hoje.

Implementação anterior: arte roxa/dourada aberta, prefab com doze envelopes e configuração inicial de três cartas por nicho. A revisão atual mantém doze cartas por armário e redistribui a saída em quatro impactos de três cartas depois da remoção das portas.

## Revisão: dois nichos amplos — 19/10/2026

Ajuste pedido pelo usuário para reduzir o visual de cartas empilhadas: o móvel agora tem somente dois nichos horizontais grandes, com seis cartas visíveis em cada um. O estoque e a meta permanecem em 12 cartas por armário.

Os cinco impactos também permanecem: o primeiro remove as portas sem coleta; os quatro seguintes enviam três cartas por impacto. Assim, cada nicho recebe dois hits — um remove a primeira fileira e o outro a segunda. Após o quarto hit de conteúdo (quinto total), as 12 cartas chegam ao contador, o armário quebra e libera as quatro células.

A contagem automática converte quatro células pintadas em um armário de 12 cartas. A arte-base atualizada está em `CM_armario_roxo_2nichos_v1.png`; a versão anterior foi preservada. As duas folhas horizontais independentes cobrem cada nicho. O corpo segue a proporção da arte no espaço 2×2 e os sprites do armário usam compressão desativada nas plataformas móveis. As cartas ficaram maiores e mais espaçadas em duas fileiras de três. O laboratório confirmou que o primeiro hit remove as portas sem coleta; os quatro seguintes entregam três cartas cada. Resultado final: 12 cartas coletadas, meta 0, armário quebrado e quatro células liberadas.

## Modelo de referência com portas laterais — 19/10/2026

**Estado visual vigente:** portas fechadas e corpo preenchendo as duas colunas, conforme a correção em [Armario_Portas_Fechadas.md](Armario_Portas_Fechadas.md). As portas abertas descritas nesta seção são histórico de conceito; estoque de 36 cartas preservado.

### Atualização aprovada: seis cartas por pilha

O usuário escolheu **36 cartas por armário**, em seis pilhas de seis envelopes, mantendo os cinco impactos. `LettersPerNiche` passa a 18 e `LettersPerHit` a 9; a contagem automática da meta e o laboratório usam esses valores. O primeiro impacto remove as portas; os quatro seguintes entregam nove cartas cada.

O layout reutiliza a arte de carta existente, com seis planos por pilha: deslocamento progressivo para cima e à direita, redução uniforme de 3,5% por plano e sombra mais quente nas cartas de trás. Há 36 sprites reais animáveis, sem cartas decorativas extras. As três primeiras camadas de cada uma das três pilhas saem no primeiro impacto de conteúdo do nicho, e as três restantes no segundo. Ordem de desenho e cor de repouso são restauradas ao reiniciar.

Verificação em Unity: prefab com 36 objetos `Letter_`, captura em `Logs/mail-cubby-stacks-36.png`, estoque/meta iniciais em 36. Uma entrega de nove cartas confirmou coletadas=9 e meta=27. Runtime e editor compilados sem erros. Esta atualização substitui as referências a 12 cartas e duas cartas por pilha abaixo.

O novo modelo fornecido pelo usuário substitui o móvel ornamentado e as folhas horizontais: moldura dourada arredondada simples, topo roxo com medalhão vermelho e envelope, dois nichos largos separados por uma prateleira e duas portas roxas abertas para os lados, com puxadores dourados externos. Seis pilhas, três por prateleira; cada pilha tem dois envelopes reais e animáveis, mantendo o estoque de 12 e os cinco impactos atuais.

Artes geradas com image_gen integrado em `Assets/RoyalAves/Art/Obstacles/MailCubby/`:

- `CM_armario_modelo_base_v2.png`: prompt para recriar fielmente a estrutura simples roxa e dourada da referência, vazia, sem portas/cartas, exatamente dois nichos, sem colunas, pés ou ornamentos extras, vista frontal e transparência.
- `CM_armario_modelo_porta_aberta_v2.png`: prompt para porta esquerda independente, roxa sólida com rebaixo, borda externa e puxador dourados, vista já aberta de perfil, mais estreita no lado da dobradiça, sem vidro, texto ou fundo. A direita usa o mesmo sprite espelhado.

`MailCubbyTools.Build` monta o prefab com posições calculadas pelos limites visíveis das artes. As cartas mantêm escala uniforme e a ordem de desenho de frente/fundo é preservada ao reiniciar. `MailCubbyCell` encaixa a composição completa, incluindo portas, no espaço 2×2 com escala uniforme e alinhamento inferior. O primeiro impacto continua arrancando as duas portas; os quatro seguintes entregam três cartas cada. Regras de estoque/meta e deduplicação de impactos preservadas.

Validação: runtime e editor compilados sem erros com referências atuais do Unity; prefab reconstruído e modelo visualizado em Play no laboratório. Captura de tela em `Logs/mail-cubby-live.png`; o comando de laboratório `render` captura o tabuleiro sem a sobreposição do HUD em `Logs/mail-cubby-model-v2.png`. O ciclo completo de cinco impactos não foi repetido nesta revisão visual.

## Som das portas — 19/10/2026

As portas saíam com o `seal_pop` do pacote, emprestado da caixa. Agora usam um impacto de madeira quebrando, escolhido pelo usuário entre três variações geradas (seca, cheia e cartoon).

- Clipe: `Assets/RoyalAves/Resources/Audio/MailCubby/cubby_doors.wav` — mono, 44,1 kHz, 16 bits, 0,85 s, pico 0,87, mesmo padrão dos outros SFX.
- Síntese procedural, sem gravação ou banco de sons. Gerador versionado em `Tools/Audio/gen_wood_break.py`, fora de `Assets/` para a Unity não importar; a variação usada é a `cheio` (semente 23), reproduzível a qualquer momento. Camadas: estalo de ruído passa-banda no ataque, modos da tábua em senoides amortecidas, segundo arranque com a estrutura cedendo, lascas espalhadas nos primeiros 220 ms e corpo grave para dar peso.
- `RoyalAves.Effects.MailCubbySounds.PlayDoors` toca pelo `SoundBase` do jogo, respeitando volume de efeitos e mudo, com a mesma janela de 0,06 s contra impactos simultâneos nas quatro células. Chamado de `MailCubbyAnimation.RemoveDoors`.
- A quebra final do móvel continua com o som herdado do efeito de lascas da caixa.
