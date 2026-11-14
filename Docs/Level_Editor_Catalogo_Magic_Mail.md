Atualizacao posterior em 13/11/2026: Pocoes, Bolsa fixa e Sinalizador foram integrados ao editor normal por pedido explicito do usuario. Sao agora 13 elementos proprios, incluindo o ovo magico em Items e a Fita sem meta. Ver [Pocoes_Bolsa_Sinalizador_Level_Editor.md](Pocoes_Bolsa_Sinalizador_Level_Editor.md). O catalogo e a tabela abaixo refletem essa integracao; a ultima secao registra a rechecagem anterior.

# Catálogo do Level Editor — Magic Mail

Rechecagem em 13/11/2026. Auditoria inicial em 09/11/2026. Pedido do usuário: conferir quais elementos próprios podem ser usados no editor normal e retirar das opções de criação os elementos do projeto base Sweet Sugar.

## Disponibilidade real

Disponível aqui significa **pode ser colocado numa fase normal pelo Level Editor**, sem ativar um laboratório que substitui temporariamente outro elemento. Isso é diferente de ter um laboratório concluído/aprovado.

| Elemento | Editor normal | Onde / observação |
| --- | --- | --- |
| Caixa de madeira | Sim | Blocks; nome interno `SolidBlock`, já adaptado ao Magic Mail |
| Grama | Sim | Blocks; nome interno `SugarSquare`, já adaptado ao Magic Mail |
| Escaninho | Sim | Blocks, grupo 2×2; modelo de madeira V6 e voo/acomodação V7 instalados |
| Franqueadora | Sim | Blocks, `FrankingMachine` |
| Gelo | Sim | Blocks, `IceBlock` |
| Ovo mágico | Sim | **Items**, `FLYING_LETTER`; não é bloco fixo |
| Pedra rúnica | Sim | Blocks, `EnchantedStone` |
| Tinteiro | Sim | Blocks e atalho com nome |
| Cofre | Sim | Blocks e atalho Cofre 2×2; concluído e aprovado pelo usuário |
| Poções | Sim | Blocks, `RoutePotions`, grupo 2×2; quatro frascos por suporte |
| Bolsa do carteiro fixa | Sim | Blocks, `BagOpenings`; meta de entregas manual; não é o antigo `MAILBAG` móvel |
| Sinalizador de triagem | Sim | Blocks, `SortingSignals`; contagem automática pelo tabuleiro |
| Fita postal | Sim | Blocks, `PostalTape`; camada sobre peças/power-ups, somente bloqueio, **sem meta**; não usa `WireBlock` |

Resultado atual: **13 elementos proprios disponiveis no editor normal**: os dez anteriores, mais Pocoes, Bolsa fixa e Sinalizador. A Fita continua sem meta. Nenhum desses tres depende de ativar laboratorio para ser pintado ou carregado.

Pocoes e Bolsa estavam somente em laboratorios na auditoria inicial. Os dois e o Sinalizador receberam prefabs/pinceis de producao por pedido explicito do usuario; a arte e os efeitos anteriores foram reaproveitados.

## Opções oferecidas para fases novas

Catálogo explícito em `Assets/RoyalAves/Editor/MagicMailEditorCatalog.cs`, compartilhado pelos seletores.

- Blocks: chão normal, caixa, grama, escaninho, franqueadora, gelo, pedra, tinteiro, cofre e fita postal. O chão é ferramenta de edição e não entra na contagem de elementos.
- Items: peças normais do jogo, foguetes horizontal/vertical, dinamite, hélice, Light Ball e ovo mágico. A seleção valida também a identidade do prefab, evitando que um prefab de teste com o mesmo enum apareça.
- Random: chão, grama e caixa, com nomes próprios; itens seguem o catálogo acima. Blocos maiores não foram adicionados ao gerador aleatório sem tratamento de seus grupos.
- Geradores: nenhum, foguetes horizontal/vertical, hélice e Light Ball.
- Metas próprias e de peças/pontos mantidas com seus índices originais. Seletores do painel de metas, Inspector da fase e janela global de metas usam o mesmo filtro.

## Ocultos da criação nova

- Blocos base: `WireBlock`, `ThrivingBlock`, `JellyBlock`, `SpiralBlock`, `UndestrBlock`, `BigBlock`, `Marshmello`, `ChocoSpread`. Outros prefabs de teste fora do catálogo explícito também não são oferecidos.
- Itens base: `INGREDIENT`, `SPIRAL`, `TimeBomb`; também ocultado `MAILBAG`, protótipo móvel próprio já substituído pela bolsa fixa.
- Metas base: `Ingredients`, `JellyBlock`, `Marshmello`; também ocultada a meta antiga `Mailbags` do malote móvel.
- Geradores de ingrediente, bomba com temporizador e espiral não são oferecidos para novas colocações.
- Tutorial de TimeBomb e controles de temporizador/ingredientes não aparecem numa fase nova. Valores antigos continuam legíveis quando já usados por uma fase legada.

## Preservação e limites

Esta é uma alteração de **apresentação e escolhas do editor**. Nenhum prefab, enum, registro de meta ou fase foi excluído/reordenado. Não converter fases antigas silenciosamente. Se a fase aberta já contém um tipo de meta/gerador legado, só esse valor existente pode continuar aparecendo com indicação de legado, para preservar sua leitura; ele não é oferecido nas listas padrão de fases novas. Pincéis antigos restaurados em memória não podem colocar itens/blocos ocultos.

`itemsForEditorFull` conserva os dados de compatibilidade; a paleta visível usa a seleção filtrada. Caixa e grama permanecem, pois seus identificadores antigos já representam as mecânicas próprias. Cofre, arte e animação aprovados não foram alterados.

Backups: `Backups/editor-catalog-2026-11-09/`. Evidências e relatório nativo: `Tools/royal-postman/production/review/editor-catalog-v1/`.

Compilação de runtime/editor atuais sem erros. A auditoria usa os recursos reais no Unity isolado e verifica os filtros, índices, recusa de pincéis legados e preservação do catálogo/fase. Não equivale a teste físico de interface ou partida completa.

## Rechecagem inicial de 13/11/2026, antes da integracao

Inspeção do catálogo atual, caminho real de montagem da paleta, arquivos Resources e substituições dos laboratórios. Os nove blocos próprios (sem contar o chão normal) têm prefab em Resources; o ovo mágico está em Items, totalizando dez. Poções, Bolsa fixa e Sinalizador têm prefabs de laboratório, mas não aparecem como pincéis normais. Nenhuma migração, fase ou código de runtime foi alterado nesta rechecagem; não foi repetido teste físico de interface/partida.
