# Memória do projeto — Magic Mail

## Sessão 10/10/2026

Data registrada conforme solicitação do usuário: 10/10.

### Elementos e laboratórios

- Caixa de madeira e grama: arte, estados e efeitos próprios; documentação e backups das versões visuais preservados.
- Escaninho passou a ser um armário estreito de quatro compartimentos, ocupando 2x2. Primeiro hit arranca as duas portas de vidro; os quatro seguintes entregam uma carta cada; vazio, o armário quebra e libera as células.
- Armário com largura completa do 2x2, base alinhada ao grid, topo reduzido, portas sem sobreposição e contador inferior removido. Visual à frente das peças ao redor.
- Cartas seguem o estilo cartoon do jogo, saem com pirueta e rastro e chegam ao contador da meta.
- Máquina de franquear: laboratório próprio, alavanca que baixa antes da ejeção, carta paralela ao bocal e entrega acima do HUD. Protótipo fixo 1x1 com meta de oito cartas.
- Impacto da franqueadora: compressão de 0,055 s, rebote amortecido de 0,365 s; largura +28%, altura -24% na compressão, com a base apoiada.
- Laboratórios de armário e franqueadora permanecem separados da progressão da campanha.

### Tabuleiro e efeitos aprovados

- Grid azul claro uniforme, quadrados arredondados, borda dourada fina e simples e cantos externos arredondados.
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
