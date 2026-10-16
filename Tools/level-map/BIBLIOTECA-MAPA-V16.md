# Biblioteca visual e cais — v16

Cada categoria do editor agora mostra miniaturas clicáveis e uma busca pelo nome. O filtro de categoria continua valendo. O seletor por texto também permanece disponível. A área vazia sob a biblioteca foi removida.

O preview atualiza a malha ao mudar o tamanho, tipo de animação ou pivô da mesma peça, inclusive quando reutiliza um objeto do pool. Antes, manter o mesmo Sprite podia conservar a malha do tamanho anterior. Há uma verificação específica para redimensionar o mesmo slot sem trocar o Sprite e outra para mudar seu material ao escolher uma animação diferente.

Novas artes feitas com o `image_gen` integrado, sem alterar os pixels ou o alpha:

- `Assets/RoyalAves/Art/MapKitV3/Library/Construcoes/postal-dock-v16.png`: cais postal pequeno, predominantemente frontal, com três tábuas e lanterna.
- `Assets/RoyalAves/Art/MapKitV3/Library/Props/water-stones-v16.png`: pedras baixas em tons de areia. Entram na categoria Props e nas passagens que usam pedras; o fallback anterior continua disponível se a referência estiver vazia.

Os [prompts e fontes](map-library-v16-assets.json) foram salvos. O catálogo evita cadastrar duas vezes um Sprite já presente na mesma categoria. Itens sem título usam o nome do asset para busca e apresentação.

Verificação em Unity isolado: compilação; categorias e referências; área das miniaturas; redimensionamento no mesmo slot; troca de material; renderização do cais, rio e tela compacta. A interação manual com o mouse e o desempenho em celular não foram medidos. A sincronização não copia os layouts ou as ilhas do ambiente de validação.

[Abrir prévia](previews/v16/review.html) · [Validação](previews/v16/validation.txt).
