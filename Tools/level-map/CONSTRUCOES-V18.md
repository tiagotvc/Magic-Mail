# Mais construções postais — v18

Quatro construções na direção visual do cais: assimetria moderada, cores saturadas, degradês suaves, borda marfim e poucos volumes. Estação e farol predominantemente frontais; quiosque e garagem mostram uma lateral.

Cada PNG é um sprite independente com alpha, disponível em **Adicionar objeto → Construções**. Os objetos já posicionados nas ilhas permanecem intactos.

Arquivos finais relativos à raiz do projeto:

| Nome no editor | PNG |
|---|---|
| Estação de entregas | `Assets/RoyalAves/Art/MapKitV3/Library/Construcoes/delivery-station-v18.png` |
| Quiosque postal | `Assets/RoyalAves/Art/MapKitV3/Library/Construcoes/postal-kiosk-v18.png` |
| Garagem dos carteiros | `Assets/RoyalAves/Art/MapKitV3/Library/Construcoes/postman-garage-v18.png` |
| Farol de correspondências | `Assets/RoyalAves/Art/MapKitV3/Library/Construcoes/mail-lighthouse-v18.png` |

Cada peça possui um catálogo próprio em `Assets/RoyalAves/Resources/MapLibrary/Construcoes/arte-<nome-do-PNG>.asset`, com tipo Landmark e tamanho inicial próprio. Usa a pulsação padrão das construções do mapa.

Geradas com o **ImageGen integrado**, uma chamada por construção, sem edição dos pixels ou do alpha. Os [prompts finais e arquivos de origem](map-library-v18-assets.json) estão registrados.

[Ver galeria e capturas no mapa](previews/v18/review.html) · [Validação no Unity](previews/v18/validation.txt) · [Dimensões, alpha e preservação das ilhas](previews/v18/asset-validation.json).
