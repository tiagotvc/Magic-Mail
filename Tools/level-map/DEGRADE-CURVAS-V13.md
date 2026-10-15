# Degradê e curvas — v13

O shader compartilhado do mapa verde ganhou uma transição mais ampla de verde esmeralda frio nas bordas para verde quente perto da trilha. A luz amarelada difusa acompanha os finais arredondados de cada trecho. As faixas claras e escuras têm transições mais suaves; o centro da trilha bege recebeu uma pequena elevação de luz.

A amplitude da curva contínua passou de 0,30 para 0,36 no mundo verde unificado (+20%). O período, a suavidade nas pontes, a largura e o rebaixo foram mantidos. Objetos e terreno usam o mesmo deslocamento. As posições e tamanhos dos objetos salvos não são regravados.

As mudanças estão no shader `MapSurface.shader` e em `WorldMapRoute.CenterX`, compartilhados pelo jogo e pelo editor. Não houve geração de novas imagens nem troca de assets.

Verificação visual em renders nativos do Unity: início, trecho intermediário, ponte e tela compacta. O processo usa uma cópia isolada do projeto, sem fechar o editor principal. Compare em [review.html](previews/v13/review.html).
