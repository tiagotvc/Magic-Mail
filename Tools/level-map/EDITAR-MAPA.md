# Montar o mapa na Unity

Abra **Royal Aves > Mapa de níveis > Pré-visualizar mundo curvo**. Após atualizar o código, feche e reabra essa janela para reconstruir a prévia.

**Prévia do chão** alterna GreenLand e LavaLand numa cópia temporária do kit. Serve para avaliar terreno e atmosfera; não troca o tema salvo do jogo. O chão, o céu, as nuvens, os vulcões e as fumaças são camadas independentes.

1. Avance até o trecho que quer montar e ative **Editar objetos**.
2. Clique numa decoração e arraste sobre o mapa. Se estiver escondida ou sobreposta, escolha pela lista **Selecionar objeto**, que informa nome, fase e lado. Use **Fase / posição**, **Lateral** e **Tamanho** para refinar o encaixe. Valores negativos de lateral ficam à esquerda.
3. Para incluir uma peça, arraste um sprite recortado do Project para o campo ao lado de **Adicionar**, depois clique no botão. Nas folhas com várias peças, expanda a imagem no Project e escolha o sprite individual.
4. O campo **Objeto** troca a ilustração; **Tipo / animação** escolhe seu comportamento. **Remover objeto selecionado** apaga a peça e Ctrl+Z desfaz alterações.
5. Clique em **Salvar disposição**. **Ver dados** seleciona o arquivo com as posições salvas.

Os tamanhos padrão foram ampliados para a escala visual das referências: árvores, casas e postes têm presença maior, e arbustos e placas ficam legíveis. Arrastar **Tamanho** cria um ajuste individual. **Usar tamanho padrão da categoria** volta à escala de referência. A composição antiga passa a usar esse padrão sem regravar suas posições.

A disposição fica em `Assets/RoyalAves/Resources/RoyalAvesWorldMapLayout.asset`. O mapa do jogo lê esse mesmo arquivo: não é necessário exportar uma imagem nem reconstruir a cena à mão. As peças continuam independentes e acompanham a curvatura ao avançar.

Cada trecho de 20 fases passa a usar sua composição manual ao entrar em edição. Os outros trechos continuam com a composição automática. Selecionar uma decoração do trecho seguinte permite editar esse trecho também.

Este editor controla as decorações. Os botões de fase, os rios e a trilha continuam ligados à rota. O cachorro foi retirado do mapa. Exportar PNG produz somente uma captura para revisão; **Salvar disposição** grava o mapa que será usado no jogo.

As novas peças estão na folha `postal-scenery-variety` e nas demais famílias do MapKitV3. Ao escolher um sprite ou salvar uma composição em edição, a peça escolhida é mantida. A variedade automática não substitui essa escolha manual.
