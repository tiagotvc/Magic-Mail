# Badge de aceleradores V98

O selo aprovado acompanha o botão azul atual: borda dourada e miolo vermelho com o mesmo contorno recortado, ampulheta independente e quantidade dinâmica. Os PNGs e as posições autoradas do botão/setas/caption são preservados.

No prefab real, os elementos editáveis ficam em `Pets/Hatchery/Accelerate/ResourceBadge`: `GoldBorder`, `RedFace`, `Hourglass`, `Quantity`. O selo está em (86,55), 94x94, preso ao canto superior direito. Ele fica fixo com o aro enquanto o miolo azul afunda no clique; a badge também encaminha o clique para o mesmo Button do Acelerar. O contador e a ampulheta anteriores ficam inativos para recuperação.

A arte original do ImageGen é `Assets/RoyalAves/Art/UI/Menu/RoyalPostman/AcceleratorBadgeV98/badge.png`. Seus pixels são mantidos intactos. Um Sprite nativo recorta a margem transparente pelo maior componente de alpha. Dois materiais complementares, com o shader `RoyalPostman/UI/BadgeParts`, separam dourado e vermelho do mesmo desenho alinhado. Isso evita produzir contornos incompatíveis em imagens geradas separadamente. O encaixe é verificado por comparação de pixels do desenho completo com as duas camadas compostas.

O contador mostra sempre o estoque de aceleradores (`x0`, `x1` etc.). Quando uma incubação está em andamento e não restam itens, o próximo custo em ouro aparece abaixo do caption em `GoldCost`, com português/inglês. O consumo, a curva de preços e os callbacks/save permanecem os existentes. Campos opcionais mantêm o comportamento anterior nos prefabs sem badge.

Há um componente reutilizável em `Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Components/AcceleratorStockBadgeV98.prefab`. O prefab é visual; o controlador da tela continua responsável pelo estoque e custo.

Prompt, fonte e hashes: `production/ACCELERATOR-BADGE-V98.json`. Construção/revisão/merge: `production/AcceleratorBadgeV98*.cs`, `stage-accelerator-badge-v98.cjs`, `merge-accelerator-badge-v98.cjs`. Backup e resultado nativo: `production/review/accelerator-badge-v98/` após instalação.

As verificações usam o projeto Unity isolado: shader, composição por pixels, cliques na badge, aro fixo/miolo móvel, estoque e custo/save único, pagamento recusado, PT/EN, encaixe sem cobrir setas/caption e áreas seguras em 360/390/720. O helper de autoria AllIn1 usa o shim isolado já documentado em V93; o plugin real e seus materiais autorados não são alterados. A Unity principal não é operada.
