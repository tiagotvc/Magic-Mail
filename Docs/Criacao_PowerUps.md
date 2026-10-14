# Criação visual de power-ups

Aplicado em 14/10/2026 por decisão do usuário.

Matches que geram especiais atraem seus ícones para a célula escolhida pelo algoritmo atual de combinação. A atração dura 0,15 segundo, acelera/desacelera suavemente e reduz os ícones para 35% perto do centro. As posições lógicas das peças não são alteradas: sprites temporários representam o movimento, preservando dano a obstáculos, objetivos e contagem nativa.

A destruição normal/partículas é suprimida apenas nos membros registrados do match que gera o especial. Matches comuns mantêm seu comportamento. O novo especial surge depois da atração e recebe um brilho radial amarelo de 0,28 segundo, atrás de sua arte. O efeito foi ligado tanto ao match feito por troca quanto ao caminho de criação durante cascatas.

O efeito de atração participa da fila de animações; objetos temporários são removidos ao terminar ou sair da fase. O estado visual é restaurado quando peças retornam do pool. O tipo e a célula de criação continuam decididos pelo jogo.

Implementação: `PowerUpFormation.cs`, integração em `CombineManager`, `DestroyingPipeline` e `Item`. O brilho é uma textura radial produzida em código.

Menu de verificação: **Royal Aves → Efeitos → Testar criação de power-up no laboratório**. O fixture é permitido apenas nos laboratórios existentes e altera somente o tabuleiro em Play, sem salvar fases. Recriar o laboratório restaura seu layout.

Verificação na Unity: compilação sem erros, criação de foguete (VERTICAL_STRIPED), globo (MULTICOLOR) e hélice (MARMALADE), cada um com uma criação contabilizada pelo efeito. O teste observa a criação antes de liberar cascatas secundárias. A fila da atração não deixa objetos pendentes; animações de coleta independentes podem continuar. Captura do brilho em `Logs/power-up-formation.png`. Aceite visual do ritmo e da intensidade pelo usuário ainda pendente.
