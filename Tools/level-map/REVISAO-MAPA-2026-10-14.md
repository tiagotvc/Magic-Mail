**Atualização posterior:** [acabamento do chão e dos níveis](ACABAMENTO-CHAO-NIVEIS-2026-10-14.md), com [capturas atuais](previews/polish/sculpt-review.html).

# Revisão do mapa — 14/10/2026

[Antes e depois e capturas atuais da Unity](previews/polish/detail-review.html).

## Alterações aplicadas

- **Trilha:** margem física de 7,5 cm, luz fina no topo e sombra interna suave, acompanhando as curvas e o término arredondado.
- **Níveis:** textura muito brilhosa desativada na apresentação. Faces em esmalte suave, pequeno reflexo lateral, aro discreto e lateral física maior, com espessura reforçada à distância. Os cliques continuam usando a mesma geometria dos botões.
- **Cenário:** árvores predominantes e secundárias por capítulo; arbustos simples e largos em coral, dourado e menta; flores com poucas formas. Poças, pequenas pedras e tufos de grama acompanham o terreno e evitam as bases dos objetos. Paletas e construções variam ao longo da rota.
- **Orientação:** postes e caixas de correio à esquerda são espelhados para apontar para a trilha. À direita, mantêm a orientação original.
- **Distribuição:** objetos automáticos e padrões antigos guardam distância da trilha e dos outros objetos. A redistribuição fica dentro do próprio capítulo. As posições e os tamanhos explicitamente editados continuam fixos; o arquivo de disposição salvo não foi regravado.
- **Apoio:** sombra de contato mais próxima da base. O apoio dos detalhes baixos acompanha também a altura reduzida nas pontas das ilhas.
- **HUD:** faixa com o nome do bairro ocultada no mapa 3D. Céu, nuvens, vulcões e fumaça continuam separados.

Se a janela de prévia já estava aberta antes da recompilação, reabra **Royal Aves > Mapa de níveis > Pré-visualizar mundo curvo** para carregar a apresentação atualizada. A disposição continua editável em **Editar objetos**, com arraste e **Salvar disposição**.

## Arte salva

Nova folha produzida pela **ImageGen integrada**, seguindo o desenho da árvore roxa original e seu contorno branco fino:

[Assets/RoyalAves/Art/MapKitV3/GreenLand/postal-ground-details-v3.png](../../Assets/RoyalAves/Art/MapKitV3/GreenLand/postal-ground-details-v3.png).

São seis sub-sprites independentes: três arbustos, tulipas, flores em sino e pedras. As pedras da folha também ficam disponíveis para montagem manual; as pequenas pedras distribuídas no chão usam geometria própria do terreno.

[Prompt exato, arquivo de origem e referência de estilo](margin-detail-assets.json). Registro preservado em `mapkit-v3-records/margin-details-2026-10-14.json`.

## Validação

Unity 6000.6.0f1, com compilação e captura em projeto isolado. O editor principal não foi fechado.

- **5.020/5.020** cliques corretos e câmera fixa em 597 amostras, em três proporções de tela.
- Dez postes/caixas verificados com a orientação de cada lado e 14 níveis com a textura brilhosa desativada.
- 34 objetos com configuração de apoio no solo, dois objetos distantes inclinados com sombra e seis níveis distantes com volume.
- Edição: nove âncoras de arraste verificadas; entrar no modo manual e recarregar não mudou nenhum pixel acima da tolerância de 4/255. Máxima diferença de 1/255 por antialiasing.
- Escolhas explícitas de sprite, tamanho e posição preservadas. HUD restaurada após fechar e reabrir o mapa.
- Oito capturas de trechos diferentes conferidas; camada de céu e seis fumaças animadas independentes verificadas.

Relatórios em `previews/polish/`: `validation.txt`, `detail-polish-validation.txt`, `layout-validation.txt`, `edit-mode-validation.txt`, `presentation-validation.txt`, `hud-validation.txt` e `asset-fit-validation.txt`.

O SHA256 de `RoyalAvesWorldMapLayout.asset` permaneceu `2A148482B8E77F0457D2BECED303D78E65F05640BE498DD9D4A0B037BF92114A`. Não houve teste em aparelho físico nesta rodada.
