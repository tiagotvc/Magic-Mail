# Ajuste do padrão dos assets — 13/10/2026

[Capturas atuais da Unity](previews/polish/asset-fit-review.html).

As novas peças foram redesenhadas usando os sprites originais da árvore roxa, do poste e dos objetos postais como referências: formas grandes, poucos detalhes, sombras ilustradas e contorno branco fino. Nesta versão foi seguido o contorno do kit original.

- Casa, seis objetos de cenário, três pontes e cinco ícones de navegação substituídos nas referências do kit.
- Cartas flutuantes retiradas do rio e da lista de inclusão do editor. Entradas antigas dessa categoria também são filtradas na apresentação.
- Selos de dificuldade deitados sobre a face dos níveis, dentro do botão.
- Envelope de Eventos e álbum de Coleção usados na HUD. Cartas decorativas nas construções permanecem presas às próprias peças.
- Referências antigas das peças substituídas atualizadas na apresentação, preservando posições e tamanhos manuais. O arquivo de disposição não foi regravado.

## Arquivos de arte

Gerados pela **ImageGen integrada**, com transparência, e salvos em `Assets/RoyalAves/Art/MapKitV3/`:

| Arquivo | Uso |
|---|---|
| `GreenLand/postal-scenery-cartoon-v2.png` | Seis objetos de cenário |
| `GreenLand/postal-house-cartoon-v2.png` | Casa postal |
| `Shared/postal-bridge-cartoon-v2.png` | Três pontes |
| `Shared/postal-navigation-cartoon-v2.png` | Cinco ícones da HUD |

[Prompts exatos, imagens de origem e referências de estilo](style-fit-assets.json). Os sprites são recortados pela importação da Unity; as imagens geradas não foram repintadas por scripts.

## Verificação

Compilação e renderização na Unity 6000.6.0f1 em projeto isolado. Conferidas quatro capturas reais: mapa inicial, travessia, níveis com dificuldade e LavaLand. Os testes confirmam a vinculação da arte nova, ausência de cartas flutuantes, três selos apoiados nas faces dos botões, apoio e inclinação dos objetos, separação entre céu e terreno e restauração da HUD ao fechar o mapa.

Relatórios: `previews/polish/asset-fit-validation.txt`, `presentation-validation.txt`, `hud-validation.txt` e `layout-validation.txt`. O hash SHA256 da disposição manual permaneceu `2A148482B8E77F0457D2BECED303D78E65F05640BE498DD9D4A0B037BF92114A`.
