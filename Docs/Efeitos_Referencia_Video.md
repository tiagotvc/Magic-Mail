# Foguete, cacos e portas em queda

Referência analisada: `efeito-porta-caindo.mp4`, fornecida pelo usuário em 14/10/2026. Vídeo de 1,24 segundo; inspeção por quadros, com amostras detalhadas entre 0,12 e 1,05 segundo. Arquivos de análise em `Logs/video-reference`.

Características reproduzidas com a arte do projeto:

- Foguete divide e percorre a linha/coluna com um rastro estreito: núcleo branco quente, halo dourado e chama menor. O dano continua sincronizado com a passagem pelas células.
- Peças comuns se fragmentam em cacos triangulares da própria textura. Os cacos têm impulso inicial, rotação e gravidade; mantêm a opacidade até saírem por baixo da câmera. O efeito substitui o splash normal das peças comuns, inclusive quando atingidas pelo foguete. Matches geradores de power-up continuam usando a atração, sem cacos.
- Primeiro impacto no armário desprende as duas portas inteiras, com salto curto para os lados, giro e queda acelerada. Elas não somem por fade no meio da queda. O armário fica disponível após o feedback inicial de 0,18 segundo, enquanto as portas continuam caindo.

Os objetos visuais são separados da lógica de dano e coleta. Cacos e portas em queda não bloqueiam a resolução do tabuleiro; são removidos depois de passar abaixo da câmera ou quando a fase é destruída/reiniciada. Malhas e materiais próprios são liberados ao terminar.

Implementação em `FallingBoardDebris.cs`, `RocketSplitEffect.cs`, `MailCubbyAnimation.cs` e no fluxo de destruição de `Item`.

Verificação: scripts de jogo compilaram sem erros; ativação de foguete em laboratório confirmou cacos em queda e quatro renderizadores de rastro (halo/núcleo para cada metade). Primeiro impacto simultâneo no armário confirmou portas removidas com estoque 4, coletadas 0 e meta 4. Captura das portas desprendidas: `Logs/cabinet-doors-falling-reference.png`. Aceite visual fino pelo usuário ainda pendente.

Validação final: foguete confirmou quatro grupos de cacos visíveis durante a passagem e quatro renderizadores de rastro; captura em Logs/rocket-fragments-reference.png. Os cacos pertencem ao FieldBoard, com escala mundial preservada, para continuarem caindo mesmo depois que o Item original é devolvido ao pool.


Revisão dos matches comuns: quebra em 12 fragmentos triangulares maiores por sprite visível, com dispersão curta, rotação discreta e queda por gravidade até sair da tela. A peça original é ocultada imediatamente e restaurada ao retornar do pool. A formação de power-ups mantém a convergência. Compilação validada sem erros.

Fragmentos dos matches reduzidos: 40 cortes triangulares por sprite (5x4, divididos em dois), queda curta de 0,55 s com fade suave a partir de 0,18 s. As portas continuam usando a queda completa. Compilação validada sem erros.

Cacos orgânicos: reduzidos de 40 para 8 por sprite, mantendo dimensões pequenas. Polígonos irregulares de 5 a 7 lados, tamanhos, posições e velocidades variados; não recobrem toda a imagem nem seguem uma grade. Fade e duração de 0,55 s preservados. Compilação validada sem erros.
