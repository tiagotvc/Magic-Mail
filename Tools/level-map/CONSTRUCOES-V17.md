# Construções postais — v17

Quatro sprites independentes na direção do cais v16: volumes simples, assimetria moderada, cores vivas, degradês suaves e borda marfim. A casa e a torre têm fachada predominantemente frontal; o abrigo e a oficina mostram uma lateral.

Disponíveis em **Adicionar objeto → Construções**, com miniatura, nome e tamanho inicial próprio. Os objetos existentes e a disposição salva das ilhas não foram substituídos. A biblioteca usa a animação padrão das construções do mapa.

Arquivos finais, relativos à raiz do projeto:

| Nome no editor | PNG com alpha |
|---|---|
| Casa do carteiro | `Assets/RoyalAves/Art/MapKitV3/Library/Construcoes/postman-cottage-v17.png` |
| Abrigo de encomendas | `Assets/RoyalAves/Art/MapKitV3/Library/Construcoes/parcel-shelter-v17.png` |
| Torre de sinal postal | `Assets/RoyalAves/Art/MapKitV3/Library/Construcoes/postal-signal-tower-v17.png` |
| Oficina de selos | `Assets/RoyalAves/Art/MapKitV3/Library/Construcoes/stamp-workshop-v17.png` |

Cada peça tem um catálogo próprio em `Assets/RoyalAves/Resources/MapLibrary/Construcoes/arte-<nome-do-PNG>.asset`, tipo Landmark. Isso permite controlar nome e escala sem alterar as demais peças.

Geradas com o **ImageGen integrado**, uma chamada por construção. Os pixels e o alpha foram copiados sem edição. Os [prompts finais e caminhos das fontes](map-library-v17-assets.json) estão registrados. O [relatório de arquivos](previews/v17/asset-validation.json) registra dimensões, alpha, hashes e preservação dos dados de autoria.

[Ver galeria e capturas no mapa](previews/v17/review.html) · [Validação no Unity](previews/v17/validation.txt).
