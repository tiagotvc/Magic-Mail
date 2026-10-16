# Botões, gelo e iluminação — v37

Os botões seguem o formato da referência enviada pelo usuário: corpo baixo, formato redondo, degradê suave e reflexo discreto no topo, aro fino em dourado claro e lateral curta. Os não concluídos são roxos; os concluídos, amarelos alaranjados. Níveis difíceis e muito difíceis usam vermelho sangue em dois tons e uma caveira vetorial pequena acima da face. Os números e callbacks de seleção continuam no mesmo sistema de níveis. A classificação de dificuldade usa a tabela editorial já existente no projeto.

Os botões conservam luminosidade à noite. A lua tem volume pintado por degradê, sombra azulada e reflexo claro, com marcas internas suaves.

O gelo ganhou contraste de profundidade e reflexos amplos mais claros. Na trilha há pequenas placas de gelo fino junto às bordas, com contorno irregular, margem esbranquiçada suave e a cor da trilha visível por baixo. À noite, uma contribuição de luar e reflexos quentes/coloridos usa as posições dos postes, construções e árvores/arbustos com luzes próximos. Essas contribuições entram depois do escurecimento noturno. São reflexos estilizados da iluminação; não refletem a geometria completa da cena em tempo real. Neve, gelo, grama e trilha continuam como superfícies distintas.

Os arbustos identificados pela arte agora recebem automaticamente as luzes de Natal existentes. As opções individuais de ativar, mover, girar e dimensionar luzes permanecem disponíveis. No poste, a neve pelo contorno fica restrita à parte alta da cobertura, preservando o vidro. O vidro recebe uma máscara de emissão própria e um halo quente ao redor da área da lâmpada, além da luz no chão.

## Casas

As construções reconhecidas como casas/oficinas/estações recebem uma lâmpada dourada presa a um suporte na fachada, reutilizando a arte existente. Ela tem emissão mais forte à noite, halo suave e uma mancha de luz quente no terreno. Os halos do chão usam uma malha subdividida que acompanha a curvatura e a altura real do terreno. As janelas registradas em `WorldMapWindowProfiles.json` também acendem. Construções sem janelas desenhadas recebem a lâmpada sem inventar novos vãos na fachada.

O conjunto da luz da casa acompanha o tamanho, espelhamento e profundidade do objeto. Em **Editar objetos → selecionar casa → Encaixe dos efeitos neste objeto → Luzes de Natal**, os controles de posição, rotação, escala e ativação também ajustam a luminária e os halos da casa. A região pintada do vidro continua encaixada na própria janela.

## Verificação

`WinterV37Validation.Run` captura dia, entardecer, noite, níveis difíceis em primeiro plano e três casas distintas. Confere lâmpadas emissivas nas casas, halo do poste, luzes nos arbustos, parâmetros dos reflexos noturnos, cores dos níveis difíceis, malha da caveira, transição do céu e limpeza dos halos de dia. Usa um projeto Unity isolado e restaura o layout de teste na memória; não salva a cena ou os layouts do usuário. A compilação do runtime também é verificada. Desempenho em aparelho físico não foi medido.

[Mapa de dia](previews/winter-v37/day.png) · [Mapa à noite](previews/winter-v37/night.png) · [Nível difícil de dia](previews/winter-v37/hard-day.png) · [Nível difícil à noite](previews/winter-v37/hard-night.png) · [Casas iluminadas](previews/winter-v37/houses-night.png) · [Validação](previews/winter-v37/validation.txt)
