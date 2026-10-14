# Temporadas visuais

No Unity, use **Royal Aves > Temporadas > Natal** para ativar a decoração e **Royal Aves > Temporadas > Padrão** para retornar à borda dourada original. A seleção é salva no asset `Assets/RoyalAves/Resources/UI/Board/BoardBorderAppearance.asset` e acompanha o build. Durante Play, os comandos atualizam as bordas existentes.

## Natal

As novas artes foram geradas com fundo transparente, inspiradas na referência natalina fornecida pelo usuário, e salvas sem modificar os pixels:

- `Christmas/hud-frame-v4.png`: versão natalina da moldura original do HUD, com festão, neve, luzes, laço, sinos, lanternas e bolas vermelhas integrados às bordas. Mantém o canvas de 2172 × 724 e os encaixes do retrato e dos dois painéis.
- `Christmas/snow-corner-v5.png`: neve curta, cheia e curva nos dois cantos superiores; o canto direito usa o mesmo sprite espelhado. O pivô fica na interseção das tangentes horizontal e vertical do recorte interno da neve. As duas superfícies apoiam na quina externa da moldura com sobreposição de 0,035 célula na lateral e 0,060 no topo, cobrindo também a transição curva sem frestas.

Os sprites anteriores `garland-snow-lights.png`, `bow-bells-snow.png` e `header-crest-v2.png` não são usados pelo tema Natal atualizado. O laço fica no HUD. Somente os cantos das bordas horizontais mais altas do contorno externo recebem neve, apoiada na borda; contornos de buracos internos não recebem decoração.

## Outras temporadas

No Natal, as oito luzinhas e as duas lanternas do HUD variam suavemente de brilho, com fases diferentes e um ciclo de 3,6 segundos. `SeasonalHudLights` controla o efeito localizado na arte, sem alterar o brilho do retrato ou dos painéis. `cycleSeconds` e `intensity` podem ser ajustados no componente durante Play. O efeito usa tempo independente da pausa e é desligado ao voltar ao tema padrão. Não usa partículas nem modifica os PNGs.

Crie um perfil no Project por **Create > Royal Aves > Temporada visual**. Configure `hudFrame` com uma moldura sazonal que preserve os encaixes do HUD original e `snowCap`/`snow` se houver neve. O campo `snowVisibleWidth` deve conter a largura visível do sprite em unidades Unity (largura dos pixels visíveis dividida por Pixels Per Unit), para descontar margens transparentes. O pivô de `snowCap` deve ficar no ponto em que a neve apoia sobre a moldura.

Selecione o perfil e use **Royal Aves > Temporadas > Ativar perfil selecionado**. Os perfis acrescentam temporadas sem exigir outro valor no enum. A seleção é uma ferramenta de autoria no editor, não um painel administrativo remoto.

## Verificação

Os três scripts envolvidos foram compilados com o compilador C# e as referências do Unity 6000.6.0f1 do projeto. Os PNGs possuem canal alfa com transparência real, incluindo a abertura do retrato. A moldura sazonal usa `Image.overrideSprite`, preservando os contadores e restaurando a arte original quando a temporada é desativada.

A neve de canto ocupa 0,60 da largura de uma celula, acompanha a quina arredondada e pode avancar discretamente sobre o grid. A arte anterior alongada foi substituida por uma cobertura compacta com curva e maior volume.

A v4 substitui o formato em gancho por uma cobertura em arco. O ajuste de altura (509/397) alinha os raios do recorte interno com a quina arredondada do tabuleiro; os pixels do PNG foram preservados.

A v5 aumenta o volume externo da neve, especialmente no topo, preservando a curva interna aprovada. A largura de exibicao acompanha a largura visivel da arte para manter a escala do encaixe; nao se reduz a arte mais cheia ao mesmo retangulo anterior.

A cobertura foi ampliada em 15% e deslocada para dentro do canto para esconder todas as faixas da borda curva, inclusive o filete interno. A madeira permanece visivel nos trechos retos depois das pontas da neve.
