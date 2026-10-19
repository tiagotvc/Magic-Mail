# Chão e peças de nível — 14/10/2026

[Antes e depois e capturas da Unity](previews/polish/sculpt-review.html).

## Chão

A margem agora mantém 0,12 m de altura também depois da ponta arredondada da trilha. Antes, a altura era multiplicada pela largura da ponta: ao arredondar e estreitar a pista, a própria margem baixava até zero. A geometria usa agora a distância da borda elíptica para conservar o piso baixo e a margem alta.

O contorno escuro borrado foi removido do material. A face lateral recebe a cor da margem, com sombreamento suave e uma faixa clara no topo. Piso e margem têm paletas independentes no fragment shader, evitando faixas diagonais nas curvas entre regiões. Fita, apoio dos objetos, detalhes baixos e pontes acompanham as alturas correspondentes.

Gramado, trilha e céu dourado receberam cores mais vivas. Céu, nuvens, vulcões e fumaça continuam separados do chão.

## Peças de nível

- Aro bege mais largo e contínuo, lateral com gradação quente e face com degradê suave e pequeno reflexo curvo.
- Silhueta com 128 segmentos e RenderTexture com antialiasing de até 4 amostras, respeitando o suporte da plataforma.
- Apoio do botão corrigido para a inclinação não enterrar a borda frontal no terreno.
- Coroas, estrelas e símbolos de dificuldade desenhados em planos voltados para a câmera, sem herdar a escala não uniforme que dá espessura ao botão.
- Novas estrelas e coroas com formas legíveis. Caveiras turquesa e violeta identificam as duas dificuldades especiais, inclusive após concluir o nível. Estrelas continuam refletindo o progresso salvo; a coroa aparece nas conclusões com três estrelas em níveis sem caveira.

## Arte salva e prompts

Nova folha com quatro sub-sprites independentes, criada pela **ImageGen integrada**:

[Assets/RoyalAves/Art/MapKitV3/Shared/level-emblems-v3.png](../../Assets/RoyalAves/Art/MapKitV3/Shared/level-emblems-v3.png).

[Prompt exato, origem e referência](level-emblem-assets.json). Registro preservado em `mapkit-v3-records/level-emblems-2026-10-14.json`. As faces e os aros dos botões usam geometria e material próprios do jogo; a folha contém coroa, estrela e duas caveiras.

## Validação

Unity 6000.6.0f1 em projeto isolado, sem fechar o editor principal. Sete capturas de trechos verdes, lavanda e LavaLand foram conferidas.

- 5.020/5.020 seleções de nível corretas e câmera fixa em 597 amostras, em três proporções de tela.
- Altura da margem verificada nas pontas arredondadas de todas as travessias.
- Alvo com 4x MSAA; 15 estrelas, cinco coroas e uma caveira verificadas sem deformação herdada do botão.
- Bases de três símbolos de dificuldade verificadas sobre suas peças de nível.
- Edição e recarga preservam a imagem: nenhum pixel acima da tolerância de 4/255; máximo 1/255. Nove âncoras de arraste verificadas.
- Escolhas explícitas do editor e arquivo de disposição manual preservados; HUD restaurada ao fechar e reabrir.

Relatórios em `previews/polish/`: `sculpt-validation.txt`, `validation.txt`, `layout-validation.txt`, `edit-mode-validation.txt`, `asset-fit-validation.txt`, `presentation-validation.txt` e `hud-validation.txt`.

Não houve teste em aparelho físico nesta rodada. Reabra a janela de prévia depois da recompilação para carregar a nova geometria e os novos materiais.
