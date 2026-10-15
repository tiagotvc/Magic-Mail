# Grama — piso com duas camadas

Ajuste de ritmo do desfolhar: partículas agora duram 0,8 s nos dois estados, comprimindo a trajetória anterior no tempo para preservar a distância de queda e dispersão. Mantidos aumento de tamanho de 45% e tempos de conclusão da camada/meta.

Revisão do desfolhar (15/10/2026): folhas do efeito aumentadas em 45% nas duas dimensões (ajustado de 65% a pedido do usuário), preservando o recorte arredondado e as cores da camada atingida. Partículas passam a durar cerca de 1,01 s no primeiro dano e 1,17 s na remoção final, com queda acelerada pela gravidade e desaparecimento somente nos últimos 28% do percurso. Encolhimento reduzido de 30% para 15%. A meta/liberação da camada continua nos tempos anteriores (0,46/0,62 s), sem aguardar a queda decorativa mais longa. Runtime compilado sem erros; conferência visual pendente no jogo.

Revisão do contorno (15/10/2026): o usuário considerou a borda orgânica anterior exagerada. `GrassOrganicBorder` agora mantém a sobreposição somente entre células com grama; bordas expostas seguem o limite da célula, com variação discreta para dentro de 0,2% a 0,6% e raio de canto de 13% da menor dimensão da célula. Removidas as saliências de até 8,8% somadas à sobreposição. O recorte usa coordenadas do campo para respeitar sua escala. Texturas, cores e regras das camadas preservadas; a silhueta inicial continua fixa durante a remoção das células vizinhas. Compilação runtime validada sem erros; conferência visual no jogo pendente.

Por decisão do usuário, o piso que estava sendo preparado como Tinta Mágica passa a usar grama, inclusive no nome exibido no laboratório. As regras já foram validadas.

- Piso fixo com uma ou duas camadas, permitindo peças e queda normalmente.
- Match na própria célula ou impacto de especial remove uma camada exposta. Match apenas vizinho não remove grama.
- Sequência: `grass.png` (tapete fechado claro, duas camadas) → `grass-sparse.png` (tapete fechado escuro, uma camada) → piso livre.
- A meta conta uma unidade somente quando a grama é completamente removida da célula.
- Um pacote acima absorve o impacto. Sua remoção final não atravessa para a grama no mesmo impacto.

Direção visual mais recente: tapete quadrado de pequenas folhas arredondadas, conforme a referência anexada pelo usuário. Ambos os estados são fechados e cobrem centro e cantos; a grama inicial é clara e, após o primeiro dano, fica mais escura. O segundo estado continua fechado, apesar do nome histórico `grass-sparse.png`. As novas imagens foram geradas/editadas com `imagegen`. Os destinos continuam `Assets/RoyalAves/Art/Obstacles/grass.png` e `grass-sparse.png`, preservando seus GUIDs. O SpriteRenderer usa branco para manter as cores das imagens. Os originais estão em `Backups/GrassFloor-original`; as revisões anteriores e atuais estão em `Backups/GrassFloor-v2` e `Backups/GrassFloor-v3`. Prompts atuais em [Grama_Prompts_Tapete_Quadrado.md](Grama_Prompts_Tapete_Quadrado.md).

Os estados são PNGs RGBA quadrados, com pivô central, bilinear, sem mipmaps e sem compressão. A importação normaliza o canvas para 1,95 unidade local; a preparação aplica escala/offset pelos limites visíveis (alpha ≥ 128), com sobreposição visual de 8% entre células. A massa visível mede 2,106 unidades locais ou 0,88452 unidade com a escala 0,42 do Square, cobrindo as divisórias de uma célula de 0,82. Essa pequena sobreposição fecha as frestas das bordas arredondadas quando a área está preenchida, nos dois estados. A grama é desenhada sobre o piso/grid e abaixo das peças. A sobreposição é somente visual: não altera a célula lógica nem os impactos. A grama clara também é o ícone da meta. O desfolhar amostra a textura do estado atingido, acompanhando suas cores e escala.

Teste atual solicitado pelo usuário: **50% de grama clara nas três últimas linhas completas**. O tabuleiro 6×6 tem 18 células com grama clara de duas camadas nas linhas 4, 5 e 6 (contadas de cima para baixo), e 18 com piso livre nas três primeiras linhas. A meta é limpar as 18 células; danos intermediários revelam o estado escuro sem contabilizar a remoção. A fase conserva 40 jogadas.

Abrir em **Royal Aves → Elementos → Grama → Teste com 50% de grama clara**. O comando sai do Play se necessário, prepara a fase em modo de edição e inicia uma nova execução de teste. Isso impede os comportamentos de prefabs temporários de acessar o tabuleiro ativo e permite inicialização limpa dos serviços do jogo. A entrada de diagnóstico pula o fluxo de compra de vidas/boosters do lobby. O painel possui rolagem e acompanha todas as 18 células. O laboratório anterior de quatro células com pacote foi substituído por este layout para esta avaliação visual.

Integração na tela de jogo concluída: `Assets/SweetSugar/Resources/Blocks/SugarSquare.prefab`, carregado pelo jogo, agora possui os mesmos estados, escala/offset, comportamento e desfolhar do laboratório. A configuração dos objetivos e seus ícones foi sincronizada sem modificar layouts de fases. O menu **Grama → Aplicar na tela de jogo** também permite atualizar as gramas já carregadas em Play. Aplicações repetidas preservam a escala original, evitando que o tapete aumente a cada atualização.

Validação: compilação dos scripts de jogo/editor sem erros; gravação dos prefabs pela API do editor confirmada; visual conferido no Simulator da Unity, incluindo preenchimento da célula por baixo de uma peça e de um pacote. Captura de conferência em `Logs/grass-game-screen.png`.

Os nomes internos `InkFloorLayer`, `InkLaboratory` e os arquivos de laboratório são mantidos para preservar as referências serializadas. A interface do teste usa Grama.

## Contorno orgânico

As quatro bordas expostas agora têm ondulações de amplitudes diferentes, incluindo os cantos. A própria textura das folhas é deformada suavemente apenas perto da borda; o interior continua fechado. As saliências podem avançar sobre o piso vizinho. Bordas entre duas células com grama preservam a sobreposição que cobre as linhas do grid.

O contorno é definido na criação do tabuleiro e permanece fixo. Remover uma célula abre o caminho sem expandir, esticar ou reposicionar a grama sobrevivente. As bordas internas revelam o que já existia sob a célula removida. O estado escuro captura o mesmo contorno inicial mesmo enquanto está oculto, impedindo que a troca de camada ocupe espaços recém-limpos. O desfolhar mantém uma cópia desse contorno durante o fade. A variação é determinística por posição, sem consumir a aleatoriedade das peças.

No perímetro do tabuleiro, a geometria da grama fica contida no limite jogável, evitando folhas fora da moldura agora mais fina. A sobreposição orgânica entre células jogáveis permanece fixa.

Verificação anterior na Unity: 18 células com contorno orgânico; dois danos intermediários mantiveram a meta em 18; duas remoções finais reduziram a meta para 16 somente após o desfolhar, sem efeitos pendentes. O layout de teste foi restaurado com 18 células claras. As capturas `Logs/grass-organic-full.png`, `Logs/grass-organic-dark-state.png` e `Logs/grass-organic-exposed-sides.png` são da revisão anterior, com expansão ao abrir caminhos, agora removida. O menu **Grama → Capturar visual em Play** exporta o tabuleiro ativo para `Logs/grass-preview.png` usando uma câmera temporária, sem depender do zoom do Simulator.

## Efeito de desfolhar

Cada dano solta folhas arredondadas usando amostras da própria textura da grama. Elas se dispersam, giram, oscilam e desaparecem; a base antiga perde opacidade em 0,20 segundo, mantendo as peças legíveis por cima.

Grama clara → escura: oito folhas, animação de 0,46 segundo. O estado escuro aparece sob a camada que está desaparecendo; o dano intermediário não contabiliza a meta.

Grama escura → removida: quatorze folhas, animação de 0,62 segundo. A camada final só é removida e contabilizada após a animação. Impactos repetidos durante sua finalização não coletam novamente. O efeito participa da fila de animações do jogo para evitar vitória antes do fim do desfolhar; reiniciar/sair cancela a coleta antiga.

Velocidade e dispersão são ajustáveis no painel de teste da Grama. São controles temporários; o padrão começa em 1. A animação não usa o gerador aleatório do gameplay, preservando a sequência aleatória das peças. Mesh e material temporários são liberados ao terminar.

Compilação verificada. Validar em Play: claro → escuro → removido, preenchimento da célula, última célula da meta, impactos simultâneos, reinício durante o efeito e leitura das folhas atrás/sobre as peças. Próxima etapa após o aceite visual: som da remoção.
