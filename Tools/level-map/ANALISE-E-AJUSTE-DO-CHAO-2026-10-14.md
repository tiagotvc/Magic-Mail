# Análise e ajuste do chão — 14/10/2026

## Ajuste posterior solicitado

Rebaixo aumentado em 50% sobre o ajuste suave: altura de 0,026 para 0,039 m e largura do bisel de 0,050 para 0,075 m. Curva, cores e linha de luz mantidas. Capturas atuais: `previews/polish/relief-green_2.55.png` e `previews/polish/relief-lava_2.55.png`. Os valores abaixo descrevem a etapa anterior.

[Comparação de antes e depois na Unity](previews/polish/smooth-review.html).

## Como o mapa está sendo desenhado

O chão do Magic Mail é uma malha curva renderizada por uma câmera para uma textura da interface. Ela recebe materiais pintados. Árvores, casas, postes e os demais objetos são sprites 2D colocados sobre essa superfície. O céu, as nuvens e o horizonte são camadas separadas.

As imagens de referência mostram um acabamento de ilustração com relevo discreto. Elas não permitem afirmar como o motor do outro jogo o implementa internamente. Para reproduzir esse acabamento, o nosso chão precisa de pouca altura física e de bordas pintadas com controle de largura e contraste.

## Por que o resultado anterior destoava

- A altura de 0,12 m e a largura de 0,13 m do bisel formavam uma faixa muito grossa na tela. Isso dava aparência de vala.
- O desenho lateral vinha de sequências irregulares de pontos por nível, com trechos quase retos, mudanças rápidas de direção e reinícios de padrão. A referência tem um ritmo de curva mais constante.
- As faixas claras e escuras do piso eram separadas por mudanças bruscas, e a pintura original tinha pouco peso no material. Aumentar a saturação deixava as mesmas áreas uniformes mais fortes, sem dar riqueza tonal.
- Os retângulos circulados no rio eram faces da parede da margem. As categorias da malha recebiam cores diferentes, expondo a divisão em segmentos. Não eram objetos de cenário.

## Ajustes aplicados

- Rebaixo reduzido de **0,12 para 0,026 m**; bisel estreitado de **0,13 para 0,05 m**. A linha clara também ficou mais fina e discreta.
- Curva contínua e regular, com amplitude de 0,30 m e período de 9,6 m de percurso em terra. A fase desacelera suavemente junto às pontes para a travessia manter as duas margens alinhadas.
- Amostragem do chão a cada 0,10 m, com refinamento adicional nas pontas arredondadas.
- Parede do rio com uma cor contínua ao longo da margem e uma gradação vertical suave, sem categorias de cor por pedaço.
- Maior presença da pintura existente, transições suaves entre faixas e pequena variação de luz no centro da trilha. As cores de base foram reequilibradas junto com esses tons.
- Fases, distância entre níveis, travessias e dados manuais do editor mantidos. As posições laterais dos objetos continuam relativas à curva da trilha.

## Verificação

Capturas reais da Unity 6000.6.0f1 em projeto isolado, incluindo início da rota, margem do rio, curvas, trilha lavanda e LavaLand. O editor principal não foi fechado.

Relatórios em `previews/polish/`: `smooth-validation.txt` (continuidade da curva, alinhamento de pontes e cor da margem), `validation.txt` (cliques), `layout-validation.txt`, `edit-mode-validation.txt`, `presentation-validation.txt`, `hud-validation.txt` e `sculpt-validation.txt`.

O arquivo de disposição manual não foi regravado. Não houve teste em aparelho físico nesta rodada.
