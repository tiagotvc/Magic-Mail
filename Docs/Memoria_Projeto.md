# Memória do projeto — Magic Mail

## Sessão 14/10/2026

O usuário avaliou a performance da sessão com nota 10/10.

### Elementos e laboratórios

- Caixa de madeira e grama: arte, estados e efeitos próprios; documentação e backups das versões visuais preservados.
- Escaninho passou a ser um armário estreito de quatro compartimentos, ocupando 2x2. Primeiro hit arranca as duas portas de vidro; os quatro seguintes entregam uma carta cada; vazio, o armário quebra e libera as células.
- Armário com largura completa do 2x2, base alinhada ao grid, topo reduzido, portas sem sobreposição e contador inferior removido. Visual à frente das peças ao redor.
- Cartas seguem o estilo cartoon do jogo, saem com pirueta e rastro e chegam ao contador da meta.
- Máquina de franquear: laboratório próprio, alavanca que baixa antes da ejeção, carta paralela ao bocal e entrega acima do HUD. Protótipo fixo 1x1 com meta de oito cartas.
- Impacto da franqueadora: compressão de 0,055 s, rebote amortecido de 0,365 s; largura +28%, altura -24% na compressão, com a base apoiada.
- Laboratórios de armário e franqueadora permanecem separados da progressão da campanha.

### Tabuleiro e efeitos aprovados

- Grid azul claro uniforme (cores atualizadas em "Cores do grid"), quadrados arredondados, borda dourada fina e simples e cantos externos arredondados.
- Matches que criam power-ups atraem as peças para a célula escolhida pelo jogo em **0,15 s**, com brilho amarelo breve atrás da nova peça.
- Foguete com rastro e quebra das peças adaptados da referência em vídeo; portas do armário se desprendem e caem inteiras.
- Direção final da quebra dos matches comuns: **oito cacos pequenos por sprite**, formas irregulares de cinco a sete lados, tamanhos e posições variados, sem recortar toda a imagem em uma grade. Queda de **0,55 s**, com fade a partir de **0,18 s**. As portas mantêm a queda completa.
- Efeitos decorativos não bloqueiam a resolução do tabuleiro; sprites das peças são restaurados ao retornar ao pool.

### Validação e próximos passos

- Compilação C# validada sem erros após as últimas revisões; avisos existentes do projeto permanecem.
- Laboratórios já conferidos para entrega de cartas, redução da meta, liberação das quatro células do armário e criação de especiais.
- Plano das primeiras 100 fases prevê **11 elementos**. Caixa, grama, armário e franqueadora têm implementações/protótipos; isso não significa campanha completa ou balanceada.
- Próximo elemento previsto depois da franqueadora: carta voadora. Conferir `Plano_Implementacao_Elementos.md` antes de avançar.
- Detalhes das mecânicas e revisões estão em `Escaninho.md`, `Maquina_Franquear.md`, `Criacao_PowerUps.md` e `Efeitos_Referencia_Video.md`.

### Versionamento desta sessão

- Usuário autorizou salvar a memória, comitar todas as alterações locais e enviar ao remoto.
- Branch de trabalho: `sincronizar-remoto`; remoto: `origin` (`tiagotvc/Magic-Mail`).
- Assets, metadados Unity, código, documentação e backups devem acompanhar o commit; caches e logs continuam ignorados.

### Elementos no Level editor (15/10/2026)

- `SquareTypes` ganhou `MailCubby` e `FrankingMachine` (no fim do enum, sem reordenar). Prefabs `Resources/Blocks/MailCubby.prefab` e `FrankingMachine.prefab` são cópias das células de laboratório (mantêm o sprite da carta, que alimenta a contagem de metas); o ícone da paleta vem da arte via `SquareUtils.PaletteTexture`.
- Armário: pintar 4 células (2x2) com `MailCubby`; `MailCubbyCell` acha o canto superior esquerdo do grupo em qualquer posição (antes fixo em 2,2). Máquina: 1 célula.
- Pilha = `SolidBlock` e Grama = `SugarSquare` já estavam na paleta. Ainda não conferido na Unity: compilação, objetivo de cartas em fase normal e fases com mais de um armário.

### Cores do grid (15/10/2026)

- Invertido em relação à sessão anterior: quadrados arredondados em azul médio-claro e linhas entre eles em azul mais claro. Valores em `BoardCellVisual.cs`: preenchimento (181, 201, 229), borda interna (191, 210, 237), linhas (178, 207, 245).
- O preenchimento ficou quase igual às linhas; se faltar separação, escurecer as linhas e não os quadrados. Ainda sem conferência visual do usuário no jogo.


## Sess?o 15/10/2026 ? revis?o das caixas, editor e efeitos

### Decis?es visuais finais

- Caixas cartoon fofinhas e arredondadas, em marrom dourado/alaranjado pr?ximo ? refer?ncia. Sem s?mbolo de carta ou carimbo. Quatro estados: base, primeira t?bua diagonal, segunda na diagonal oposta e terceira horizontal no meio. Manter GUIDs dos sprites.
- Quebra das caixas mais r?pida: lascas grandes, menos numerosas e mais arredondadas (6 por t?bua, 12 na quebra final), contornos org?nicos. T?buas removidas tamb?m se soltam inteiras e caem girando com os estilha?os at? o fim da tela; a anima??o decorativa n?o prende o tabuleiro.
- Cacos dos matches percorrem aproximadamente 30% mais dist?ncia; dura??o de 0,55 s preservada.
- Grama com contorno discreto alinhado ao grid, cantos suaves e pequenas irregularidades. Desfolhar: part?culas 45% maiores, queda de 0,8 s mantendo o percurso aprovado. N?o voltar aos 65% nem ao contorno exagerado.
- Rastro do foguete engrossado novamente; largura externa 0,34, n?cleo 0,11 e raio das part?culas 0,22. Velocidade preservada.
- Pulsos tempor?rios de escala est?o aprovados. Nenhuma pe?a ou power-up deve permanecer maior ap?s forma??o, pouso, sele??o ou retorno ao pool. Raiz padr?o 0,42; respeitar a escala original do sprite (normalmente 0,88, ingredientes 1). Anima??es normalizadas em `Animations/BoardScale`; gerador e testes em `Tools/NormalizeBoardItemAnimations.py` e `Tools/test_board_item_animation_scale.py`.

### Editor de fases e metas

- Corrigido travamento que impedia editar o formato do grid nas fases 4 e 5: removida mistura de pixels desnecess?ria sobre textura n?o leg?vel.
- Nomes de metas: Grama (`SugarSquare`) e Caixa (`SolidBlock`). Bot?o + permite acrescentar v?rios tipos de meta na mesma fase; bot?o ? remove um grupo, preservando pelo menos um. Troca de tipo e contagens independentes, sem duplicar tipos.
- Meta espec?fica `MachineLetters` para cartas da franqueadora, quantidade manual (padr?o 8), separada das cartas do arm?rio. Envelope da m?quina ampliado (escala de voo 1,8).
- Decis?o final do usu?rio: franqueadora fica DENTRO do grid, em uma c?lula. A experi?ncia de posicion?-la fora foi revertida; manter a meta de cartas. Dados antigos de m?quinas externas s?o convertidos para c?lulas livres sem sobrescrever obst?culos.
- Op??o reutiliz?vel `startWithoutItem`: iniciar c?lula sem pe?a, inclusive com grama; afeta apenas o preenchimento inicial, a gravidade posterior segue normal. Editor tem pincel "Iniciar sem pe?a", a??o "Gramas sem pe?a" e limpeza das marca??es.
- Queda diagonal n?o pode atravessar paredes, buracos ou cantos fechados. Conferir os dois vizinhos ortogonais e restri??es de passagem nos quatro lados antes de transferir uma pe?a.

### Caixas bloqueiam efeitos

- Cada ativa??o captura as posi??es das caixas. Pode atingir a primeira caixa, mas n?o pe?as ou grama atr?s dela, inclusive evitando cortes diagonais pelo canto.
- Mesmo destruindo a ?ltima camada, o mesmo impacto continua bloqueado; apenas uma ativa??o posterior pode passar.
- Aplicado a foguetes, combina??es com bombas, explos?es e sua rea??o visual, sele??o/convers?o multicolorida e canh?o (origem na base). Metades do foguete param na caixa, sem avan?o al?m dela.
- Implementa??o compartilhada: `ImpactBarrierSnapshot`. Valida??o no menu `Tools > Magic Mail > Validate Crate Impact Barriers`.

### Valida??o e versionamento

- Runtime e editor C# compilados sem erros nas revis?es finais; permanecem avisos anteriores.
- Quatro testes do gerador de anima??es aprovados; harness de m?ltiplas metas aprovado; barreiras de impacto aprovadas em 12 cen?rios e 120 caminhos rec?procos.
- Compila??o e testes gerenciados n?o substituem confer?ncia visual em Play Mode. Valida??o visual final das ?ltimas regras permanece pendente; o editor encontrado estava na tela de termos da Unity.
- Usu?rio pediu salvar tudo na mem?ria e comitar as altera??es locais, incluindo assets, fases, prefabs, fontes, documenta??o e backups. Logs e caches permanecem ignorados.
- Documentos detalhados desta sess?o: `Caixa_Cartoon_Dourada.md`, `Caixa_Cartoon_Sem_Carimbo.md`, `Caixa_Lascas_Madeira.md`, `Editor_Niveis_Grid_Grama.md`, `Editor_Multiplas_Metas.md`, `Editor_Iniciar_Sem_Peca.md`, `Maquina_Franquear.md`, `Grama.md`, `Foguete_Largura_Rastro.md`, `Pecas_Escala_Padrao.md`, `Queda_Paredes_Buracos.md` e `Caixas_Barreira_Impactos.md`.
