# Royal Postman — fabricação separada

Referências aprovadas: `../approved/home.png` e `../approved/play-modal-tight-inset.png`.

## Arquivos

- Pinturas PNG e materiais: `Assets/RoyalAves/Art/RoyalPostmanUI2026`.
- Prefabs da home, modal e controles individuais: `Assets/RoyalAves/Prefabs/RoyalPostmanUI2026`.
- Controles de apresentação: `Assets/RoyalAves/Scripts/UI/RoyalPostman2026`.
- Galeria local: `index.html`. Capturas nativas: `review/`.
- Origens, prompts e reaproveitamentos: `GENERATION.json`.
- Inspeção de alpha sem modificar os PNGs: `ALPHA-REPORT.json`.

## Construção

O centro quadrado e o circular são pinturas neutras compartilhadas. O shader `RoyalPostman/UI/PaintedPalette` aplica as cores de estado ao mesmo relevo. A borda dourada é outro Sprite e outro objeto. No clique, somente `MovingCenter` e seus filhos descem. Não há moldura dourada/prateada nos reforços, nem no check de seleção. O inset creme é um Sprite neutro próprio, imediatamente atrás do botão.

Os símbolos simples de mais, fechar, check e vídeo são componentes vetoriais independentes; não dependem de glifos ausentes na fonte. Os cinco ícones originais de navegação e os três objetivos foram preservados. Avatar, retrato e moldura têm referências independentes. Os estados de reforço podem ser alterados por `RoyalPostmanBoosterButton.SetState`, sem alterar inventário ou ativar efeitos.

Para reconstruir a revisão: executar `inspect-assets.cjs`, depois o menu Unity **Royal Postman > 2026 > Fabricar kit separado e capturar revisão**. O builder usa uma cena descartável e não altera a cena de jogo. Para revisão automatizada foi usado o projeto isolado em `Library/MapValidation/PreviewProject`.

## Integração autorizada — 19/10/2026

O usuário autorizou substituir a home e a modal no jogo. `RuntimeKit.asset` aponta para os prefabs fabricados. `LobbyController` abre a nova home no lugar do mapa/sala, lê o progresso real e mantém os menus existentes. A nova modal é uma apresentação do `MenuPlay` original: preserva seus controles de reforço, callbacks de início/fechamento e os objetivos da fase. O badge e o botão Grátis usam o `FreeAdBoosterButton` existente, com sua rotação configurada e captura do item ao clicar. As novas artes dos três reforços são resolvidas por tipo; os demais itens do pool mantêm seus ícones existentes. Não há migração ou reinicialização de progresso/inventário.

O acesso Pet abre uma indicação de recurso futuro; não foi inventada uma mecânica de cuidado/batalha. Os eventos foram adiados pelo usuário. Os testes isolados de vínculo estão em `../integration/`: usam o adaptador real e `FreeAdBoosterButton` real com fachadas de carteira/anúncio, sem modificar o save. Isso não representa uma conclusão de anúncio em uma rede real.

A fonte nativa é uma candidata de revisão, com texto independente e dinâmico. A equivalência exata dos glifos com a imagem gerada do mock não foi certificada e não deve ser apresentada como concluída. O personagem central continua substituível.
