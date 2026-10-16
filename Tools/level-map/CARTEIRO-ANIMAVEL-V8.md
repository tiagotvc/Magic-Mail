# Carteiro animável — v8

A ilustração aprovada continua em `Assets/RoyalAves/Art/MapKitV3/Characters/postman-stylized-v7.png`.

Esta versão acrescenta um protótipo de rig 2D por recortes: 18 sprites independentes, com articulações hierárquicas e sobreposição nas junções. O movimento é feito pelos Transforms das peças; não troca a figura inteira por desenhos de poses diferentes. Ainda não foi aplicado ao mapa.

## Usar no Unity

Arraste `Assets/RoyalAves/Characters/PostmanV8/PostmanV8.prefab` para uma cena com câmera 2D/URP. O Animator começa com respiração e balanço leve dos acessórios.

O parâmetro inteiro `Motion` seleciona os ciclos:

| Valor | Clipe | Movimento |
|---|---|---|
| 0 | Idle | Respiração, cabeça, cachecol, bolsa e caudas do casaco |
| 1 | Wave | Aceno de ombro, cotovelo e mão |
| 2 | Walk | Caminhada no lugar, pernas alternadas e acessórios |

Exemplo: `animator.SetInteger("Motion", 1);`

Os clipes são editáveis pela janela Animation. As articulações podem ser ajustadas no prefab. A caminhada é no lugar; o deslocamento no cenário pertence ao controlador de jogo.

O menu **Royal Aves > Personagens > Criar rig do carteiro** recompõe este protótipo e seus três clipes a partir das peças. Executá-lo sobrescreve os ajustes manuais nos arquivos gerados desta pasta; duplique o prefab e os clipes para manter uma animação personalizada.

## Arquivos

- `Assets/RoyalAves/Art/MapKitV3/Characters/postman-upper-rig-v8.png`: cabeça, tronco, quadril, braços e mãos.
- `Assets/RoyalAves/Art/MapKitV3/Characters/postman-lower-rig-v8.png`: pernas, botas, bolsa, caudas e cachecol.
- `Assets/RoyalAves/Characters/PostmanV8/`: prefab, material, controller e três clipes.
- `Tools/level-map/previews/postman-v8/review.html`: prova animada com seleção de ciclo e quadro.
- `Tools/level-map/previews/postman-v8/validation.txt`: resultado da validação no Unity.
- `Tools/level-map/postman-rig-v8-assets.json`: prompts finais, referência e origens das imagens, geradas com **ImageGen integrado**.
- `Tools/level-map/mapkit-v3-image-manifest.json`: nomes e recortes dos sprites; as imagens não foram reprocessadas por script.

## Reprodução da verificação

Execute `node Tools/level-map/prepare-polish-preview.cjs` e abra o projeto isolado em `Library/MapValidation/PreviewProject` com o método de batch `PolishPreviewBatch.RunPostmanRig`. O batch fecha apenas seu próprio processo. Não é necessário fechar o editor do jogo.

A prévia usa câmera fixa e 16 capturas nativas por ciclo; o Animator interpola as curvas continuamente. A validação confere a persistência do prefab, referências das 18 peças, três estados, movimento do cotovelo e fechamento dos ciclos. Este rig usa articulação rígida de sprites, sem deformação de malha.
