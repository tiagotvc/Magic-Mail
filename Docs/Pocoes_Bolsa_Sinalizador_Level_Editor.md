# Poções, Bolsa fixa e Sinalizador no Level Editor

Os três estão disponíveis como pincéis normais, sem ativar laboratório:

| Pincel | Colocação | Objetivo |
| --- | --- | --- |
| Poções (2×2) | Quatro células em um grupo; clicar/apagar qualquer quadrante remove o grupo inteiro. A borda, buracos, teleportes e sobreposições incompatíveis são recusados antes de escrever. | Contagem automática de frascos: quatro por suporte. |
| Bolsa do carteiro | Uma célula fixa; reutiliza a bolsa aprovada e seus pergaminhos animados. | Quantidade de entregas digitada nas metas. Várias bolsas compartilham a mesma quantidade restante. |
| Sinalizador de triagem | Uma célula; mesmo aparelho de cobre, ícone inicial e ciclo de cores do laboratório. | Uma unidade por sinalizador; coleta após os três acertos e a quebra. |

Pintar prepara a inclusão da meta correspondente. Salvar também verifica os grupos de Poções e garante a presença das metas necessárias. Nenhuma fase da campanha foi pintada por esta instalação.

A Fita continua sendo **somente bloqueio, sem objetivo próprio**. O antigo malote móvel `MAILBAG` não foi reativado: o pincel novo é a Bolsa fixa.

## Assets e compatibilidade

Prefabs independentes em `Assets/SweetSugar/Resources/Blocks/`: `RoutePotions.prefab`, `BagOpenings.prefab` e `SortingSignals.prefab`. Os três tipos foram acrescentados ao fim de `SquareTypes`; os índices anteriores permanecem iguais.

Foram reutilizadas as artes, materiais, frames e animações dos laboratórios existentes. O prefab da Bolsa já serializa todos os frames de abertura e de pergaminho; não depende de carregamento via AssetDatabase durante uma build do jogo.

As definições de meta existentes foram atualizadas no lugar, sem criar ou reordenar índices. Apenas `RoutePotions`, `BagOpenings` e `SortingSignals` foram alteradas. Poções e Sinalizadores contam pelo tabuleiro; a Bolsa permanece manual. Os callbacks próprios contabilizam cada efeito, sem coleta genérica duplicada por sprite.

`PotionFootprint` mantém os grupos 2×2 no editor; cada frasco conserva sua própria célula de impacto e um único dono visual por suporte. A origem salva impede que suportes vizinhos sejam mesclados. Os laboratórios antigos continuam disponíveis e mantêm sua compatibilidade de contagem.

## Validação

PASS no Unity isolado: pincéis reais via API do editor; colocação de dois suportes vizinhos; apagamento por um quadrante; borda recusada sem gravação parcial; preview e recursos; configuração de metas; salvar/reimportar e carregar uma fase normal com laboratórios desligados. Em execução, conferidos oito frascos em dois suportes separados, duas Bolsas, dois Sinalizadores, match real iniciado pela API de troca, liberação das oito células de Poções, quebra/contagem dos Sinalizadores e entregas da Bolsa contando somente na chegada e parando na quantidade definida.

Compilação runtime/editor: zero erros. As chamadas são diagnósticas, sem alegação de ponteiro físico, áudio audível, aparelho ou campanha completa. A fase isolada de verificação 1902 não foi instalada no projeto principal.

A compatibilidade de carregamento e contagem dos três laboratórios separados anteriores também passou em verificação isolada. O Unity principal confirmou o cadastro dos três recursos; os indicadores de laboratório foram desligados para permitir a autoria e o teste normais de fases.

Evidências: `Tools/royal-postman/production/review/three-elements-editor-v1/`. Backup: `Backups/pending-elements-editor-v1-2026-11-13/`.
