# Acelerar: setas mais grossas

O ícone de texto `>>` é substituído visualmente por duas setas arredondadas largas, usando o PNG existente `pets-page-arrow-v6.png` com um Sprite nativo e material creme. As pontas, a espessura dos braços e o relevo continuam legíveis no botão pequeno.

Objetos editáveis no prefab real: `Pets/Hatchery/Accelerate/PaintedCenter/FastForwardIcon/ChevronLeft` e `ChevronRight`. Cada seta é uma Image independente, com proporção original, contorno marrom e sombra curta. O container e seu RectTransform continuam na posição autorada; o Text anterior fica desabilitado para recuperação. Ambas acompanham o miolo no clique.

O aro, miolo azul, dimensões, posições, labels e callbacks do botão permanecem autorados. O instalador permite somente desativar o Text/efeitos antigos, adicionar os dois filhos ao container e instalar Sprite/material novos. Todos os outros componentes são comparados semanticamente com a versão atual, inclusive as edições recentes do usuário.

Não há PNG novo nem alteração de pixels dos PNGs existentes. Arquivos novos:

- `Assets/RoyalAves/Art/UI/Button/RoyalPostman/Pets/Sprites/fast-forward-chevron-v93.asset`
- `Assets/RoyalAves/Art/UI/Button/RoyalPostman/Pets/fast-forward-cream-v93.mat`

Validação nativa isolada: compilação/render, proporção/espaçamento em 390/720px, raycast, pressão do miolo com aro fixo e ação de acelerar com consumo/save único. A cópia isolada usa um shim do helper de autoria do AllIn1SpriteShader, mantendo o material já salvo: suas janelas/módulos de edição não fazem parte desse projeto mínimo. O plugin real não é modificado; os seus componentes serializados originais são preservados pelo merge.

Scripts: `production/FastForwardV93*.cs`, `stage-fast-forward-v93.cjs`, `merge-fast-forward-v93.cjs`. Backup/auditoria/resultado em `production/review/fast-forward-v93/` após instalação. A Unity principal não é operada.
