# Tinteiro e efeito de tinta

Implementação inicial no laboratório, 3 de novembro de 2026.

## Regra

- Cinco acertos de combinações ortogonais adjacentes ou poderes.
- Os quatro primeiros pintam, por acerto, um vizinho ortogonal livre ainda não pintado. Sem vizinho elegível, o acerto continua danificando o tinteiro, sem espalhar para diagonais.
- O quinto quebra o tinteiro e pinta a própria célula.
- O preenchimento cobre toda a célula, de borda a borda, mantendo o formato do piso existente. Não é uma poça pequena.
- A peça sobre o piso também fica tingida. Ela leva a tinta durante deslocamentos; o piso permanece pintado independentemente dela. Peças que entram no piso pintado também recebem tinta.
- Tinta é apresentação visual: não altera a cor lógica usada nos matches, não bloqueia peças, não cria meta de limpeza e não modifica recompensas.
- A meta baixa uma vez, após remover o tinteiro no quinto acerto. Danos parciais e pintura não contam como coleta.
- Uma proteção de 0,2 s impede dano duplicado imediato, como nos outros elementos do laboratório.

## Usar no Unity

Menu **Royal Aves → Elementos → Tinteiro → Abrir laboratório**, fora do Play. O laboratório usa uma cópia de fase 7×7, um tinteiro central, 40 movimentos e um foguete horizontal para teste. Não instala tinteiros nas fases da campanha e não concede resultados/recompensas de campanha.

O painel mostra os acertos e permite ajustar a duração de preenchimento entre 0,15 e 1,5 s. O botão de impacto é diagnóstico; não substitui jogar com matches e poderes reais. Para sair, usar **Desativar laboratório** ou parar o Play.

O editor de níveis (**Sweet Sugar → Level editor**) possui o elemento **Tinteiro** na paleta de blocos. Pintar um tinteiro solicita sua meta automaticamente; a quantidade é calculada pelo tabuleiro. O novo tipo `SquareTypes.Inkwell` foi acrescentado no índice 16, sem deslocar tipos antigos. A meta própria foi acrescentada ao catálogo no índice 15, preservando as entradas anteriores. Prefab de produção: `Assets/SweetSugar/Resources/Blocks/Inkwell.prefab`. Caixa e grama continuam independentes.

## Arte e efeito

`Assets/RoyalAves/Art/Obstacles/Inkwell/InkwellStages.png` reutiliza os bytes da prancha V4, baseada no protótipo confirmado pelo usuário, com coroa. Seis regiões nativas, sem tinta incorporada. Continua sendo arte preliminar, sem aprovação de sprites finais inferida.

`InkwellCell` controla os acertos. `InkwellStainedCell` mantém a marca no chão. `InkwellStainedPiece` mantém a tinta na peça e limpa seus próprios visuais quando a peça volta ao pool. `InkTintVisual` acompanha sprite, transformação, alpha e ordenação do renderer original.

O efeito `InkwellSplashEffect` projeta um jato curto e gotas brilhantes desde a boca do recipiente. A viagem dura 0,3 s; só depois começa a pintura. O impacto solta gotas menores, separadas dos sprites secos. Cada célula é reservada ao emitir o jato para dois acertos próximos não escolherem o mesmo destino.

O shader `RoyalAves/InkStain` usa frente irregular, filetes, variação de pigmento, acúmulo nas bordas e reflexos curvos de superfície molhada. Preenche até todos os cantos, sem deixar o resultado limitado a uma poça pequena. Na peça, o preenchimento desce preservando luz e sombra do desenho. Material e overlays separados não editam cores lógicas nem materiais originais.

A meta própria calcula o total pelos tinteiros salvos na fase. Instanciar o prefab não soma de novo essa quantidade; só o callback de quebra decrementa. A coleta genérica ignora o componente do tinteiro, e a troca do sprite por um estágio danificado não muda a meta. Subníveis consultam os recipientes ativos no campo atual, preservando a contagem total dos demais campos.

## Verificação

Compilação externa de runtime/editor em `Logs/inkwell-review/`, sem substituir DLLs do projeto. Verificação nativa dos assets em `Logs/inkwell-assets-verification.result`. Testes de execução isolada e capturas são registrados separadamente; não considerar uma chamada diagnóstica de dano como prova de input real.

**Executado:** runtime e editor compilados sem erros; prefab, seis sprites/UVs e shader importados; execução nativa na cópia `Logs/InkwellValidationProject` com company/product separados do perfil real. PASS para impacto adjacente encaminhado, rejeição de diagonal e duplicado imediato, preenchimento intermediário/final, quatro vizinhos e centro no quinto, remoção da camada e meta decrementada uma vez. Deslocamento diagnóstico confirmou propriedade independente da tinta na peça e no chão; não foi um swap por ponteiro.

Captura após quedas/cascatas estabilizarem: `Tools/royal-postman/production/review/inkwell-v1/tingimento-unity.png`. Resultado: `native-verification.txt` na mesma pasta. As peças receberam pigmento mais claro após a primeira inspeção visual. O teste isolado encerrou o próprio editor; não há pedido de automação deixado no projeto principal.

Pendentes até registro explícito de conclusão: revisão visual em Play pelo usuário, matches/trocas/poderes com ponteiro real, comportamento em dispositivo e fluxo completo home → pré-nível → partida → bônus/cascatas → vitória visível → Continuar. Não houve merge nem publicação nesta tarefa.

### Revisão líquida V2

Teste nativo atualizado passou com uma troca pelo método normal de swap que forma match adjacente: o dano saiu do resolver real de match, emitindo o jato antes da pintura. Foram conferidos cinco jatos emitidos e cinco chegadas, frente intermediária/final de pintura e meta mantida em um até a quebra. Quatro acertos seguintes continuam diagnósticos; a troca foi iniciada por API, sem ponteiro físico. Capturas e resultado em `Tools/royal-postman/production/review/inkwell-v2/`; hashes em `INKWELL-V2.json`.

Após recompilar, parar o Play anterior e abrir novamente o laboratório para carregar o efeito e a meta corrigidos. Não reutilizar uma sessão que sofreu hot reload como validação da nova inicialização.

### Bordas arredondadas V3

Cantos externos do piso de tinta usam raio de 20% da célula e antialias. Os lados compartilhados por células já pintadas não recebem borda escura nem recorte de canto, formando uma mancha contínua. A silhueta da peça permanece a do sprite original. Compilação runtime/editor sem erros e teste nativo de match, cinco respingos e meta novamente PASS. Captura: `Tools/royal-postman/production/review/inkwell-v3/bordas-arredondadas.png`. Limites de input e fluxo completo permanecem os já registrados.

### Tamanho e editor de níveis V4

Importação alterada de 320 para 210 pixels/unidade, aplicada aos seis estados da mesma textura: aumento de aproximadamente 52%, sem regenerar PNGs. A medição nativa de alpha confirmou altura visível equivalente a 93,9% da célula. Prefabs de laboratório e produção usam o mesmo tipo e BindLayer 6, acima de grama e pisos.

PASS nativo: enum acrescentado sem deslocar antigos, prefab próprio, registro de meta único, ícone recortado na paleta, pincel real do editor em cópia de dados, solicitação de meta e serialização de ida e volta. Também conferidos subnível versus total, tamanho em Play, match adjacente, cinco respingos e contagem final. Não foi um teste de ponteiro físico no editor. Evidências e backup em `Tools/royal-postman/production/review/inkwell-v4/`. O catálogo foi atualizado acrescentando o novo bloco YAML; os bytes anteriores foram preservados. Nenhum layout de fase existente foi salvo por essa instalação.

Após importar/recompilar, reiniciar o Play e reabrir o editor de níveis para atualizar a paleta.

### Registro dos estágios V5

Corrigido o deslocamento entre sprites: a arte tinha offsets diferentes dentro dos seis quadros 512×512, e os pivôs anteriores usavam centro genérico/ajuste aproximado. Os cinco estágios em pé agora são registrados pelo centro da base dourada e pela linha de apoio, sem usar o centro da silhueta danificada. Os cacos finais também alinham seu apoio à mesma altura. O pivô intacto `(256,256)` e 210 px/unidade foram preservados; nenhum quadro foi redimensionado ou regenerado.

A origem do respingo usa o ponto da boca no estágio intacto, em vez dos bounds mutáveis do sprite danificado. O comando **Royal Aves → Elementos → Tinteiro → Corrigir alinhamento dos sprites** atualiza somente a importação, sem reconstruir prefabs ou fases.

PASS nativo: medição independente de pixels da base confirmou deslocamento de 0,0000 px entre os pontos de apoio dos seis quadros. Os cinco estágios em pé foram capturados no mesmo tabuleiro, com posição/escala do objeto e boca do respingo constantes; match, cinco respingos e meta também passaram. Instalação limitada às propriedades de pivô do `.meta` existente, mantendo IDs de sprites, PNG, prefabs, catálogo e layouts. Capturas, backup e auditoria em `Tools/royal-postman/production/review/inkwell-v5/`. Não equivale a teste por ponteiro físico nem a partida completa.

### Cacos de porcelana animados V6

Os quatro danos parciais soltam uma lasca física de porcelana, sem mover o recipiente. No quinto, o último estágio em pé é substituído por geometria animada: 22 cacos finos roxos/dourados, com miolo claro nas superfícies quebradas, e um fragmento frontal maior com a coroa extraída da textura original em escala de texel preservada. Os fragmentos giram nos três eixos, recebem iluminação por face e caem por gravidade; não é exibido o antigo sprite 5 de entulho. Essa região da textura continua no asset apenas por compatibilidade, sem ser usada na quebra.

O modo de porcelana reutiliza a estrutura de movimento/renderização de `StoneShatterEffect`, mantendo o modo padrão da pedra (30 fragmentos, geometria/material/cores e camada originais). `PorcelainBadgeDebris` conserva a textura e escala da coroa, mostrando espessura clara ao girar. Não há novo PNG nem novo áudio. Os efeitos sobrevivem à remoção da camada; a pintura central e a meta continuam no quinto acerto, com uma única coleta.

Runtime/editor compilados sem erros. PASS em cópia isolada: quatro emissões de lascas, uma quebra final com geometria visível e movimento, ausência do PNG final, escala constante do fragmento frontal, modo padrão da pedra, match/respingo/alinhamento e meta. Capturas de voo e queda em `Tools/royal-postman/production/review/inkwell-v6/`; hashes em `INKWELL-V6.json`. Não substitui teste de input físico, áudio, dispositivo ou fluxo completo até Continuar. Reiniciar o Play anterior para carregar a alteração.

### Centralização na célula V7

O registro anterior conservava o posicionamento antigo do estágio intacto, que estava deslocado dentro da célula. O novo pivô intacto é `(288,240.5)` px, no centro dos limites visíveis de alpha; os demais recebem exatamente os mesmos deslocamentos relativos de registro. Escala, 210 px/unidade e altura de 93,9% da célula foram mantidas. A base continua estável entre as rachaduras. Cacos agora consultam o centro do corpo calculado a partir do pivô; boca do respingo e fragmento frontal também acompanham a arte centralizada.

PASS nativo: deslocamento do centro visível para o centro da célula `(0,0)`, registro das bases com 0,0000 px de deriva, match, cinco respingos, meta e cacos. Aplicação limitada aos pivôs da textura existente e ajustes de origem no código; sem editar transforms de prefabs, cenas, layouts ou catálogo. Captura em `Tools/royal-postman/production/review/inkwell-v7/tinteiro-centralizado.png`; hashes/relatórios em `INKWELL-V7.json`. Reiniciar o Play para carregar a alteração; não houve teste por ponteiro físico ou partida completa.


### Acesso vis?vel no Level Editor V8

A paleta de blocos tinha uma ?nica linha, deixando o ?ltimo ?cone (Tinteiro) fora da ?rea vis?vel em janelas estreitas. Agora os blocos s?o distribu?dos em linhas conforme a largura, e as abas Blocks e Items t?m um bot?o expl?cito **Tinteiro** no in?cio. Esse acesso seleciona o pincel de bloco e desativa borracha, separador e marca??o sem pe?a; depois basta clicar nas c?lulas. Abra por **Sweet Sugar > Level editor** ap?s a recompila??o.

Runtime/editor compilados sem erros. PASS nativo na c?pia isolada: GUIBlocks/GUIItems reais renderizados a 390 e 640 px, bot?o dentro da janela e sele??o por eventos IMGUI MouseDown/MouseUp nos quatro casos, incluindo limpeza dos modos conflitantes. N?o equivale a mouse f?sico ou fluxo completo de partida. Nenhuma fase, cena, prefab ou textura foi salva por esta altera??o. Auxiliar e resultado em `Tools/royal-postman/production/review/inkwell-v8/`.
