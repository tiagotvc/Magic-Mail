# Campanha Magic Mail: níveis 1–200

> A distribuição atual dos tabuleiros foi revisada: consulte [Campanha — densidade V2](Campanha_Densidade_V2.md). Os números de distribuição e testes abaixo registram a primeira versão; as referências e a agenda de apresentações permanecem válidas.

Pedido de 13/11/2026: criar 200 níveis, explicar cada novo elemento na estreia e substituir livremente os testes 101 e 102. Esta entrega considera a campanha **do nível 1 ao 200**, com os arquivos normais de autoria do Level Editor; não são laboratórios nem níveis de demonstração paralelos.

## Estrutura de autoria

- 200 receitas determinísticas, com semente individual, 16 silhuetas e oito padrões de distribuição. Cada tabuleiro e abertura são próprios.
- Progressão em ondas: introdução, prática, mistura de regras, desafio a cada cinco níveis e respiro. A composição fica mais exigente ao longo da campanha; uma estreia pode ser mais fácil que a fase anterior.
- Tabuleiros iniciais 7×7, depois 8×8, 8×9 e 9×9, com recortes e algumas variações de largura. Quatro cores no começo; quinta após 80 e sexta em parte das fases após 160. As cores necessárias às poções e ao sinalizador permanecem disponíveis.
- Até três grupos de metas por fase. Quantidades respeitam as regras existentes: 36 cartas por escaninho, quatro frascos por bancada, um cofre por grupo 2×2, uma unidade por caixa/célula de gelo independentemente das camadas. Franqueadoras e bolsas compartilham a quota manual da fase.
- Fita postal é somente bloqueio, sem meta. Nenhum elemento legado do Sweet Sugar foi acrescentado.
- Uma troca inicial legal é preparada em cada receita. Não há trincas nem quadrados de quatro cores iguais na posição inicial, incluindo a cor lógica dos especiais prontos: peças autoradas são preservadas pela preparação do jogo e não podem começar em combinações que impeçam esse processo de terminar.
- Entradas de reposição nos segmentos de gravidade, inclusive abaixo de estações fixas e após recortes. Grupos de cofres e poções usam os validadores nativos de ocupação; escaninhos ficam separados para evitar grupos ambíguos.

O índice de pressão da planilha é uma **estimativa de autoria**, calculada por trabalho dos obstáculos, cores e jogadas. Não é uma taxa de vitória medida, nem prova de que todos os níveis são vencíveis dentro do limite. O ajuste fino depende de partidas completas e telemetria.

## Apresentações automáticas

Reutilizam a tela de apresentação existente, após as metas do pré-jogo. A partida fica pausada durante a apresentação e o controle retorna ao fim. A estreia volta a ser explicada ao rejogar o respectivo nível, como no comportamento anterior. Não foi criado um bloqueio para impedir o jogador de formar especiais antes da demonstração planejada.

| Nível | Apresentação |
|---:|---|
| 1 | Caixa de madeira |
| 2 | Foguete |
| 3 | Dinamite |
| 4 | Grama |
| 6 | Hélice |
| 8 | Globo de luz |
| 11 | Escaninho |
| 21 | Franqueadora |
| 31 | Gelo |
| 36 | Gelo de duas camadas |
| 46 | Ovo mágico |
| 61 | Poções |
| 76 | Bolsa fixa do carteiro |
| 91 | Pedra rúnica |
| 106 | Tinteiro |
| 121 | Cofre |
| 136 | Sinalizador de cor |
| 151 | Fita postal |

Os testes antigos 101 e 102 deixam de ser vitrines com todos os elementos: o 101 mistura pedras rúnicas com a bolsa; o 102 mistura poções com a bolsa. O tinteiro, o cofre, o sinalizador e a fita ainda não aparecem nesses dois níveis.

## Referências consultadas na internet

Os layouts são originais. Foram aproveitados princípios de leitura, acesso a alvos, camadas, combinações e ensino gradual, adaptados às regras do Magic Mail. As páginas e os exemplos públicos não fornecem garantia de balanceamento da nossa campanha. Níveis de outros jogos podem mudar de versão.

- [Royal Match — Dream Games](https://www.dreamgames.com/games/royal-match): variedade de obstáculos e combinações de power-ups.
- [Royal Match — exemplo público do nível 50](https://www.youtube.com/watch?v=TrfnGQhvL8E) e [guia do nível 100](https://apptipper.com/royal-match-level-100-tips/): referências públicas de fases intermediárias. A consulta a páginas de vídeos não constitui análise quadro a quadro da partida.
- [Royal Kingdom — Dream Games](https://www.dreamgames.com/games/royal-kingdom) e [guia oficial de jogabilidade](https://dreamgames.helpshift.com/hc/en/6-royal-kingdom-1676903479/section/36-gameplay-guide/): combinação de obstáculos e desafios. As batalhas/reinos não foram transplantados para este projeto.
- [Royal Kingdom — exemplo público dos níveis 46–50](https://www.youtube.com/watch?v=yvkUZg1IUGo): referência de sequência de fases, consultada como página de gameplay.
- [Candy Crush — guia oficial de bloqueadores](https://candycrush.zendesk.com/hc/en-us/articles/360000754717-Which-Blockers-can-I-find-in-the-game): abertura de espaço, camadas e diferentes condições de impacto.
- [Candy Crush — discussão e capturas do nível 695](https://community.king.com/en/candy-crush-saga/discussion/461653/help-beat-level-695): exemplo de restrições de acesso e bordas.
- [Entrevista sobre a escada de complexidade do Candy Crush](https://www.pocketgamer.biz/crafting-candy-crushs-difficulty-blockers-level-design-ai-and-the-complexity-staircase/): separar aprendizado de uma regra e combinação de regras já conhecidas.

## Arquivos e reprodução

- `Tools/level-map/build_campaign_200.py`: gera as receitas, a planilha e a galeria esquemática; não grava assets Unity.
- `Tools/level-map/campaign-200/recipes.json`: receita exata por fase, incluindo posições, camadas, metas, especiais e abertura.
- `Tools/level-map/campaign-200/campaign.csv`: revisão dos 200 níveis.
- `Tools/level-map/campaign-200/index.html`: galeria esquemática de todos os tabuleiros.
- `Assets/RoyalAves/Editor/Campaign200Tools.cs`: serialização nativa. Menu `Royal Aves > Campanha > Validar campanha 1–200`. O comando de aplicar substitui a campanha atual por estas receitas; não executar após edições manuais sem um novo backup.
- `Assets/RoyalAves/Scripts/Meta/CampaignElementIntroductions.cs`: textos, artes existentes e agenda das apresentações.
- `Backups/campaign-200-2026-11-13/`: campanha e apresentação anteriores, inclusive metadados e hashes.

Os níveis existentes são atualizados no mesmo asset, preservando seus GUIDs. Novos níveis recebem GUIDs próprios. O catálogo global de metas, prefabs, artes, mapa, recompensas e preferências de progresso não precisam ser alterados para esta campanha.

## Validação

A evidência final e seus limites ficam em `Tools/level-map/campaign-200/evidence/`. O validador estrutural cobre os 200 assets, ocupação 2×2, metas, paletas, aberturas e o catálogo do mapa. O teste isolado de execução usa carregamento normal com laboratórios desligados, contagens reais de metas, apresentações automáticas e a API de troca de peças.

Resultado estrutural no projeto principal: **PASS**, 200 níveis, 200 assets de metas, 413 grupos de metas no total e 18 apresentações agendadas. Os 206 arquivos de metadados anteriores foram preservados, e 389 arquivos de produção protegidos permaneceram idênticos, incluindo os prefabs e o catálogo global de metas.

Rodada nativa de execução: **39 fases representativas passaram**, cobrindo as 18 apresentações, contagens exatas no tabuleiro e uma troca real da API por fase. A apresentação do nível 1 também cumpriu o tempo completo e devolveu o estado de entrada. A cópia tinha três versões anteriores de alterações visuais, na bolsa, sinalizador e marcação dos globos; após sincronizar todo o código runtime com o principal, uma rodada final focada nos níveis 1, 76, 136, 151 e 200 confirmou o carregamento e as trocas na versão atual. As capturas são da câmera do tabuleiro, sem o HUD completo.

A comparação também encontrou uma referência de compatibilidade da meta da bolsa diferente apenas na cópia de teste. O catálogo principal foi preservado e copiado para a cópia isolada; os casos 1, 76 e 200 passaram novamente. Ao fim, **os 400 assets de campanha, o catálogo global de metas e todos os arquivos C# de runtime comparados ficaram idênticos entre principal e cópia isolada**. Os relatórios distinguem as rodadas de 39, cinco e três casos; são 39 fases únicas testadas em execução.

Durante o desenvolvimento, foram corrigidos o sentido inverso da API de arraste no teste, o quadro pendente de destruição de uma apresentação e a exclusão indevida dos especiais coloridos da checagem de matches iniciais. Os resultados finais substituem essas tentativas anteriores. Os logs ainda contêm avisos existentes de licenciamento, parâmetros de Animator e metadados antigos de fonte/localização; a campanha não os altera.

Isso não equivale a vencer as 200 fases, validar compras/recompensas, entrada física por ponteiro, dispositivo, áudio ou o fluxo completo home → partida → vitória → continuar. Não foi alterado o progresso salvo do usuário.
