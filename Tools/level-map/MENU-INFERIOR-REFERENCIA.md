# Menu inferior — reprodução da referência

A barra usa o arquivo original fornecido pelo usuário como alvo visual. O acabamento usa duas artes de fundo pintadas, compostas por `MapNavigation.shader` e aplicadas pela apresentação da HUD do mapa. Os sprites e as ações das abas foram preservados. A ordem continua **Mapa, Eventos, Ranking, Equipes, Coleção**.

Na largura de referência de 1080, a barra tem 205 de altura, cinco células de 216, friso dourado de 12,5 e cantos inferiores com raio de 84. As cores foram conferidas em amostras do PNG original. A superfície tem gradiente roxo, bordas moldadas com iluminação e sombra interna curta, sulcos estreitos em roxo e uma aba ativa lilás. O friso tem perfil arredondado, reflexo amarelo-claro e sombra inferior. Todo o acabamento fica dentro da barra.

Os ícones inativos têm tamanhos ajustados opticamente e ficam dentro da barra. O selecionado se eleva ligeiramente, com texto creme e sombra curta abaixo. O texto utiliza a fonte encorpada Lilita One já existente no projeto. Os materiais da superfície e da legenda são criados uma vez ao abrir e liberados ao fechar.

## Capturas

- [Comparação lado a lado na mesma escala](previews/navigation-reference/comparison-same-scale.png): referência à esquerda, captura do Unity à direita; ambas com largura de menu equivalente a 1080.
- [Página de conferência com as cinco abas](previews/navigation-reference/review.html).
- [Tela em 1080 × 1920](previews/navigation-reference/phone-1080x1920.png), resolução de referência do Canvas.
- [Tela em 1080 × 2280](previews/navigation-reference/phone-1080x2280.png), proporção do Redmi 6 Pro usado no Simulator.
- [Resultado da validação](previews/navigation-reference/validation.txt).

A comparação é um render de revisão do Unity que desenha o PNG original e a captura na mesma escala. O arquivo original permanece intacto; o recorte da referência exclui a moldura do aparelho e alinha o friso superior.

Compilação do assembly principal: zero erros e avisos. Conferência nativa numa cópia isolada: cinco estados selecionados, cinco ações encaminhadas uma vez cada, mesmas referências de sprites, abas contíguas e de mesma largura, um texto selecionado e liberação dos materiais. Zero erros do Unity e zero avisos de instanciação de material nessa execução.

Se o Play já estava aberto, é necessário parar e iniciar novamente para reconstruir a barra com o material atualizado.

## Arte de fundo v23

Criada com o **ImageGen integrado**, usando a última referência anexada como imagem-base. A pintura traz o friso com duas faces de cor — amarelo-claro em cima, âmbar embaixo — e os biséis iluminados dos cards. O shader mantém a seleção dinâmica, ajusta a paleta para a referência e recorta os cantos. A seleção muda sem substituir os ícones ou suas ações.

Arquivos finais, copiados sem alteração dos pixels:

- `Assets/RoyalAves/Art/MapKitV3/MapUI/nav-surface-v23.png`
- `Assets/RoyalAves/Art/MapKitV3/MapUI/nav-selected-v23.png`

[Prompts completos e arquivos de origem](navigation-paint-v23.json) · [Hashes e medidas](previews/navigation-reference/paint-validation.json).
