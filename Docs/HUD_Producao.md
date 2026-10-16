# HUD de jogo — base roxa e dourada

## Estado atual — montagem manual pelo usuário

A pedido do usuário, removidos `GameplayHud.cs` e sua instalação automática em runtime, junto com a chamada adicionada ao builder e a API auxiliar de escala do contador. A cena `game.unity` já recebeu a textura v3 manualmente pelo usuário e não foi editada nesta remoção. Preservadas as artes e a política de importação 4096. A substituição sazonal antiga ignora sprites `hud-base-purple-gold*`, preservando a imagem atribuída manualmente. As seções abaixo registram o histórico da integração retirada; não representam mais o comportamento ativo.

## Aplicação e limite 4K no Unity

Versão v3 mais alta permanece selecionada por `GameplayHud.FrameResource`. A importação da pasta `Resources/UI/Hud` agora fixa `Max Size = 4096`, sem mipmaps nem redimensionamento para potência de dois, e mantém overrides de 4096 para Standalone, Android e iPhone. Metadados da v3 atualizados e checados; editor compilado sem erros após a alteração. A regra é reaplicada a cada importação.

O PNG continua com seus 1860 × 846 pixels originais. `Max Size = 4096` é o limite de importação, não um upscale ou uma textura de origem com 4096 pixels. A aplicação ocorre ao carregar cena/fase; confirmação visual no editor continua pendente.

## Revisão de altura — versão ativa v3

Atendendo ao pedido de deixar a HUD mais alta, a base ativa passou para `Assets/RoyalAves/Resources/UI/Hud/hud-base-purple-gold-v3.png` (1860 × 846, proporção 2,20:1). Na mesma largura, ficou **22,2% mais alta** que v2 (2056 × 765, 2,69:1). As versões anteriores foram preservadas. A arte foi redesenhada pela ferramenta integrada de imagens para essa proporção; o código usa a proporção nativa da nova textura e mantém o retrato circular. Encaixes de títulos, objetivos e jogadas atualizados. Compilação runtime sem erros; conferência de espaço disponível até o tabuleiro e do resultado em Play Mode continua pendente.

Prompt da edição v3:

> Edit this production HUD frame to be SIGNIFICANTLY TALLER and more compact. Target overall width-to-height ratio exactly 2.2:1, about 22% taller at the same total width than the reference 2.69:1. REDRAW shapes with proper proportions; left portrait frame must stay perfectly CIRCULAR, never an ellipse. Give the center ivory goals panel noticeably more vertical room, and the right purple moves compartment a taller near-square shape. Preserve the exact existing glossy purple, ivory, sculpted gold style, upper blank level plaque and smaller blank goals plaque. Keep centers: portrait x15%, y45% from bottom; right moves x85%, y45%; upper level plaque x50%, y86%; small goals plaque x50%, y66%. Left portrait inner opening diameter about 24% of total width (about 53% of overall height); center panel spans x28..73%, y12..78%; right slot x76..98%, y12..79%. No text, numbers, icons or character; all interior wells remain blank for dynamic Unity overlays. Tight isolated transparent sprite, outside alpha fully transparent, small safe transparent margin, no shadow blobs, glow outside outline, snow or seasonal ornaments. The visibly taller central panel and near-square side wells are essential. Do not return the same wide flat proportions.

## Revisão visual: retorno ao conceito original

O usuário preferiu o primeiro mockup à primeira moldura de produção. A versão ativa agora é `Assets/RoyalAves/Resources/UI/Hud/hud-base-purple-gold-v2.png`, com placa de nível acima do painel central, placa menor de objetivos, contornos dourados esculpidos e painéis sobrepostos. A primeira textura permanece preservada. Retrato continua à esquerda e jogadas à direita, conforme a troca solicitada anteriormente.

`GameplayHud.FrameResource` aponta para v2. O componente ajusta a altura pela proporção da nova arte, reposiciona os encaixes, cria `TituloNivel` e mostra `LevelManager.currentLevel`. Títulos usam materiais locais com face clara para evitar que os presets postais escuros alterem a paleta. Contagens e bindings dos objetivos continuam preservados. Conferência visual e funcional no Play Mode permanece pendente.

Arte criada/editada com a ferramenta integrada de imagens, a partir do recorte de referência enviado pelo usuário. Prompt da revisão: recriar fielmente a moldura do recorte, trocar retrato e jogadas de lado, conservar placa superior de nível e placa de objetivos, retirar todo texto/ícones/personagem para composição dinâmica no Unity; preservar roxo, dourado e creme; transparência externa e ausência de elementos sazonais. Segunda edição retirou brilhos externos da moldura.

## Layout aprovado em 16/10/2026

Retrato à esquerda, objetivos ao centro e jogadas à direita. Base sem neve ou Natal. A HUD usa a gerente e os ícones reais do projeto; a personagem e o tabuleiro do conceito não são novos assets de gameplay.

## Levantamento dos arquivos

| Arquivo | Papel / alteração |
| --- | --- |
| `Assets/RoyalAves/Resources/UI/Hud/hud-base-purple-gold.png` | Nova moldura transparente RGBA, 2172 × 724, com painéis vazios. Números, títulos, ícones e retrato continuam objetos Unity. |
| `Assets/RoyalAves/Scripts/Meta/GameplayHud.cs` | Instala automaticamente nas instâncias de `MolduraFase`, inclusive inativas, ao carregar cena/fase. Posiciona os elementos existentes e atualiza o contador com os dados da fase atual. |
| `Assets/RoyalAves/Editor/RoyalAvesGameScreenBuilder.cs` | O menu existente `Royal Aves > Aplicar moldura da tela de jogo` também aplica o novo layout quando encontra a arte. |
| `Assets/RoyalAves/Scripts/Meta/BoardBorderRenderer.cs` | Evita que a substituição sazonal antiga sobrescreva a HUD nova. A sazonalidade do tabuleiro mantém sua configuração. |
| `Assets/SweetSugar/Scripts/GUI/Counter_.cs` | API `SetRestingScale`: permite atualizar a escala de repouso depois da aplicação do layout, sem o contador restaurar a escala anterior. Mantém o relógio das fases por tempo. |
| `Assets/SweetSugar/Scenes/game.unity` | Tem cópia própria da HUD. Recebe a integração em runtime; sem edição de YAML nesta entrega. |
| `Assets/SweetSugar/Scenes/gameStatic.unity` | Tem outra cópia própria. Recebe a mesma integração em runtime. |
| `Assets/SweetSugar/Scripts/System/Orientation/OrientationPanel.prefab` | Terceira origem possível. Recebe a integração quando instanciado e ao carregar fase. |
| `Assets/SweetSugar/Scripts/GUI/TargetGUIGroup.cs` | Reutilizado: monta as metas reais da fase. Não foi substituído. |
| `Assets/SweetSugar/Scripts/TargetScripts/TargetSystem/TargetGUI.cs` | Reutilizado: quantidade restante, check de conclusão e vínculo com `TargetCounter`. Os objetos permanecem destinos das cartas coletadas. |
| `Assets/RoyalAves/Resources/GerenteExpressoes.asset` e arte referenciada | Reutilizados por `ManagerFace`, preservando as expressões da gerente. |
| `Assets/RoyalAves/Art/UI/Game/moldura-fase.png` e `Resources/UI/Board/Christmas/hud-frame-v*.png` | Arte anterior preservada. A nova base é aplicada por composição, sem sobrescrever os arquivos anteriores. |

Os `.meta` da pasta, textura e componente novos acompanham os arquivos. Não é necessário recriar ícones de metas nem gravar contagens na imagem. Boosters, modal pré-fase e tabuleiro não foram redesenhados.

## Dados e ciclo de vida

- Jogadas: `LevelManager.THIS.levelData.limit`, atualizado no `LateUpdate`, inclusive depois de receber movimentos extras. Reassocia o `Counter_` à fase atual em reinícios/trocas; não mantém uma cópia independente de jogadas.
- Fases por tempo: título `TEMPO`, formato `mm:ss`; o `Counter_` permanece habilitado, pois controla a passagem do tempo.
- Objetivos: preserva `TargetGUIGroup.list`, `TargetGUI.BindTargetGUI`, contagens, imagens e checkmarks existentes. O número de objetivos vem da fase, não do mockup de dois objetivos.
- Retrato: reaproveita a gerente existente, com máscara circular. Nas cópias antigas sem gerente, cria `ManagerFace`, que usa o conjunto de expressões existente.
- As coordenadas são proporcionais ao retângulo da moldura existente, preservando sua proporção e a estrutura de SafeArea atual.
- A troca sazonal antiga de moldura inteira é ignorada somente nesta HUD. Futuras molduras/overlays sazonais precisam respeitar os novos encaixes.

## Validação

Runtime e editor compilados com Roslyn e as referências/defines do projeto, sem erros; avisos existentes permanecem. Logs locais ignorados: `Logs/hud-runtime.log` e `Logs/hud-editor.log`. Verificada textura com canal alpha de 0 a 255 e proporção 3:1. Hierarquias das cenas e prefab inspecionadas; componentes e referências de metas existentes foram preservados.

Ainda pendente: conferência visual e funcional em Play Mode. Conferir fases com um, dois e vários objetivos; coleta até o check; decremento e movimentos extras; repetição e troca de fase; fase por tempo; proporções de tela e safe area. Compilação não equivale a esses testes.

## Arte e prompt

Gerada com a ferramenta integrada de imagens, usando o conceito anterior como referência de estilo. Arquivo final: `Assets/RoyalAves/Resources/UI/Hud/hud-base-purple-gold.png`.

Prompt utilizado:

> Use case: ui-mockup. Production sprite asset ONLY for mobile match-3 game top HUD. Use previous purple gold game HUD as STYLE reference, but produce ONLY blank horizontal HUD frame, no screen, no board. Transparent background. Wide 3:1 canvas. Exact layout: LEFT circular portrait medallion at center x18%, y50%, diameter 62% of canvas height, plain dark glossy purple interior, ornate thin gold rim; CENTER warm ivory rectangular objectives panel spanning x34% to74%, y22% to83%, with rounded corners sculpted shiny gold edge, small EMPTY purple title tab at top spanning x43% to65%, y14% to32%; RIGHT glossy purple rounded counter panel spanning x77% to96%, y20% to85%, sculpted gold rim. Connected polished deep purple and gold trim unifies the three slots. Outer frame fits x2% to98%, y10% to92%. The left portrait circle and right counter same visual weight. Premium lush casual game dimensional UI, glossy violet enamel, warm gold highlights, ivory parchment, soft bevels, clean silhouettes. All panels COMPLETELY EMPTY. NO text, letters, numbers, character, icons, stars, buttons, symbols. NO level crest above frame. NO snow, winter, frost, Christmas, seasonal decorations. Straight on orthographic. Actual transparent background outside single isolated HUD asset. Keep all gold rims uncut.

Os encaixes do código foram medidos no resultado gerado, que pode diferir das coordenadas solicitadas no prompt.
