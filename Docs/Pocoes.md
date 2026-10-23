# Poções

**Status: feito e aceito pelo usuário em 23/10/2026.** Nome anterior: Central de Rotas. Usar **Poções** ao criar, listar ou planejar o elemento; não criar outro elemento com o nome antigo.

Obstáculo fixo 2×2 com quatro frascos: verde e roxo na linha superior, vermelho e azul na inferior. Match ortogonal adjacente quebra o frasco da cor correspondente. Especiais respeitam as células atingidas: foguetes podem quebrar os dois frascos da linha/coluna. Acertar uma posição vazia não transfere o dano. A meta conta um por frasco, total de quatro por conjunto.

Quebra aprovada: vidro em 0,17 s, estouro local, gotas de tinta afuniladas e cacos com trajetórias independentes. Gotas com tamanho acumulado de 1,38× o inicial (15% e depois mais 20%). Ao terminar a última ruptura, o suporte explode em lascas usando o efeito das caixas. Guardas individuais evitam repetição; a última poção libera as quatro células.

Menu: **Royal Aves → Elementos → Poções → Laboratório estouro e respingos no jogo**. Os testes anteriores ficam sob o mesmo nome Poções. Campanha: estreia proposta na fase 61, reforço 62–80; implantação e balanceamento da campanha são etapa separada do aceite deste elemento.

Classes `RouteCentral*`, identificador de meta `RoutePotions` e caminhos dos assets permanecem estáveis para preservar referências serializadas e carregamento. Histórico técnico em [Central_de_Rotas.md](Central_de_Rotas.md).
