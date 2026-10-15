# Kit de arte MapKitV2
Data: 13/10/2026. Assets para a implementação pelo Claude; ainda não ligados ao mapa.

## Direção final

O usuário escolheu a luminária e a caixa de correio como referência de acabamento: formas simples, volume arredondado, luz suave, contorno creme e reflexos largos. Os demais assets foram revisados com essas duas imagens como referências diretas. Usar as versões `-glow.png`; as primeiras versões ficam somente para comparação.

A grama, água e ponte devem ser construídas por geometria com cores, faixas e bordas iluminadas. **O usuário rejeitou `cartoon-terrain-patterns.png`. Não usar esse atlas.**

## Sprites recomendados

Todos ficam em `Assets/RoyalAves/Art/MapKitV2/Sprites/`. As folhas têm transparência real e metadados de importação/slicing. A ordem abaixo é da esquerda para a direita, de cima para baixo.

| Arquivo | Grade | Conteúdo |
|---|---|---|
| dog-courier-poses-glow-spaced.png | 2×2 | cachorro parado, cauda erguida, farejando, levando carta |
| mailbox-poses.png | 2×1 | caixa fechada, caixa aberta |
| postal-lamp.png | individual | luminária postal teal/roxa/dourada |
| round-purple-tree-glow.png | individual | árvore roxa de copas redondas |
| small-postal-props-glow-spaced.png | 2×2 | encomendas, flores, placa de envelope, arbusto |
| postal-region-landmarks-glow.png | 4×2 | barco, moinho, farol, casa serrana; relógio, grande agência, túnel, pipa |

Total recomendado: **20 sprites**, em seis PNGs. Existem também 25 sprites das versões iniciais/intermediárias para comparação; não misturar os acabamentos nem os recortes por acidente. O manifesto marca `recommended: true` nos arquivos finais.

Também reutilizar de `Assets/RoyalAves/Art/Maps/`: céu postal, pinheiro, agência pequena e fábrica de triagem. Estes assets anteriores precisam ser comparados no piloto com o acabamento novo; não presumir que toda a coleção já tem o mesmo grau de luz.

As construções desta folha são pinturas de um ângulo, não uma coleção de vistas frontal/superior. O barco não substitui uma ponte; o túnel pintado não substitui um buraco na geometria. Usar como decoração ou conceito de fachada sobre um volume simples quando necessário.

## Malhas de protótipo

`Assets/RoyalAves/Art/MapKitV2/Meshes/`: OBJ originais criados por script, Y para cima, avanço +Z, unidades de Unity. Total: **1.080 triângulos**.

| Arquivo | Dimensão aproximada | Triângulos | Uso |
|---|---|---:|---|
| postal-level-token.obj | raio 0,46; altura 0,16 | 192 | face roxa, aro dourado, lateral creme |
| raised-banks-segment.obj | largura 10; comprimento 12; margem +0,30 | 240 | caminho rebaixado com duas margens |
| postal-plank-bridge.obj | largura 2,6; comprimento 4,1 | 192 | tábuas, bordas e vigas com espessura |
| water-channel.obj | 10×6; cota −0,18 | 192 | superfície de água separada |
| postal-island-platform.obj | aproximadamente 9,2×8; espessura 0,40 | 24 | plataforma com faces laterais |
| hard-level-tile-patch.obj | 5×4 ladrilhos de 0,60 | 240 | trecho de destaque para fase difícil |

São peças de partida, em repouso e sem curvatura aplicada. O terreno tem divisão suficiente para o primeiro experimento; refinar se houver facetas perto do horizonte. O segmento não é a rota completa e suas pontas precisam casar com o próximo trecho. O número da fase não está na malha: colocar texto na face superior.

## Materiais

Nove arquivos URP Unlit em `Materials/`, com nomes correspondentes aos grupos OBJ:
`FacePurple`, `RimGold`, `SideCream`, `RoadLightGrass`, `GroundDarkGrass`, `BankEdge`, `BridgeTeal`, `WaterTeal`, `HardTilePurple`.

Atribuir explicitamente por grupo/submalha ou por filho importado. Não confiar na criação automática de materiais do OBJ. São bases de cor; o acabamento final precisa de gradientes/bordas e, para água, linhas claras móveis.

Não há UV unwrap final. Se Claude acrescentar padrões/decals, gerar UVs coerentes de mundo. O atlas rejeitado não deve ser usado para compensar a ausência de UVs. Materiais 3D precisam do Universal Renderer na câmera do mapa; o Renderer2D atual não é a implementação dessa cena.

## Importação, pivôs e animação

- 100 pixels por unidade é uma referência de importação, não o tamanho final de cada objeto. Normalizar pelo tamanho visível e pelo porte desejado.
- Pivô inicial customizado: centro horizontal e 5% acima da base da célula. Ajustar o pé real por sprite antes da animação; uma folha gerada não garante pés na mesma linha.
- As grades foram fatiadas com limites inteiros, inclusive nas folhas 1.774×887. No cachorro revisado, o divisor vertical fica no vão transparente em X=650 para preservar o focinho da pose farejando. O manifesto contém os retângulos exatos.
- Cachorro: quatro poses para gestos ocasionais. Não vender a troca das quatro imagens como um ciclo perfeito de caminhada ou de cauda. Para ciclo contínuo, separar cauda/corpo ou produzir quadros intermediários coerentes.
- Caixa de correio: duas poses coerentes. Pode ter transição curta; conferir encaixe do poste.
- Sombras de contato são uma camada separada presa ao chão; não adicionar sombra ao PNG inteiro.
- PNGs foram inspecionados visualmente. Conferir bordas de alpha, recortes e pivôs sobre fundo claro/escuro no mapa real; remover possíveis resíduos finos de geração se forem visíveis na escala de uso.
- Preferir material de cartão com recorte de alpha e escrita de profundidade para o corpo. Avaliar camada suave separada para glow, sem comprometer oclusão.
- Não aplicar bloom forte global nem tornar toda vegetação metálica para simular o acabamento da luminária.

## Proveniência e reprodução

Imagens: ferramenta integrada `image_gen`, fundo transparente nos sprites. Prompts iniciais em `mapkit-v2-image-prompts.json`; revisões de luz em `mapkit-v2-glow-prompts.json`; revisão de espaçamento em `mapkit-v2-spacing-prompts.json`; origem dos arquivos revisados em `mapkit-v2-glow-sources.json`. Os originais gerados foram preservados e os arquivos de uso copiados ao projeto.

Geometria/materiais: `build-handoff-meshes.cjs`; especificações em `mesh-kit-manifest.json`.
Cópia e slicing: `prepare-mapkit-images.cjs`; inventário final em `mapkit-v2-image-manifest.json`.
A reprodução da cópia usa os caminhos locais dos originais registrados no manifesto. Não requer uma nova chamada de geração. Não regenerar sem necessidade, pois o usuário já destacou quais referências devem ser preservadas.

## Escopo do kit

É um conjunto reutilizável para desenvolver e provar o sistema, incluindo marcos dos dez capítulos. Não são 200 fundos, dez regiões prontas nem 200 tabuleiros. O piloto decide quais novos ângulos de casas, personagens e detalhes exclusivos ainda são necessários.

