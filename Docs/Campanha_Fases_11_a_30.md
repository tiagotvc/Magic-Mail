# Fases 11 a 30 — campanha aplicada

Vinte fases gravadas nos assets reais, com metas, paletas e limites de estrelas próprios. O bloco 1–10 aprovado foi preservado, incluindo metas, movimentos e GUIDs. Os arquivos anteriores de 11–30 estão em `Backups/Campaign-11-30-before-v1`.

## Ritmo e estreias

- Fase 11: armário postal. Um armário 2×2, acesso aberto, quatro tipos de peça e a apresentação aprovada após as metas iniciais.
- Fases 12–14: prática e combinações com caixas/grama; fase 15 é o desafio.
- Fase 16: respiro. Fases 17–19 preparam o desafio 20.
- Fase 21: franqueadora. Fileira de cinco máquinas 1×1 na borda inferior, oito cartas compartilhadas, quatro tipos de peça e sua apresentação após as metas.
- Fases 22–24: prática e combinação. Fase 25 reúne armário e franqueadoras em U, com metas independentes.
- Fase 26: respiro. Fases 27–29 preparam o desafio 30, com dois armários e máquinas em U.

As formas alternam recortes, leque, faixas horizontais, cruz, ampulheta, degraus e H. Cada armário ocupa quatro células completas; as máquinas ficam dentro do grid. Não há gelo ou outros elementos ainda não apresentados.

## Parâmetros aplicados

Caixas e gramas indicam unidades × camadas. Cartas da franqueadora são uma meta compartilhada entre as máquinas da fase; não são uma quantidade por máquina. Cada armário contém 36 cartas, com meta separada das cartas produzidas pelas máquinas.

| Fase | Papel | Movimentos | Peças | Armários | Cartas da franqueadora | Caixas | Grama |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 11 | Ensino | 22 | 4 | 1 | — | — | — |
| 12 | Combinação | 22 | 5 | 1 | — | — | 8×1 |
| 13 | Combinação | 23 | 5 | 1 | — | 6×2 | 8×1 |
| 14 | Preparação | 22 | 5 | — | — | 8×2 | 12×2 |
| 15 | Desafio | 22 | 5 | 2 | — | 10×2 | 14×2 |
| 16 | Respiro | 24 | 4 | 1 | — | 4×1 | 6×1 |
| 17 | Combinação | 22 | 5 | 1 | — | 6×2 | 10×1 |
| 18 | Combinação | 24 | 5 | 1 | — | 8×2 | 8×2 |
| 19 | Preparação | 23 | 5 | 1 | — | 6×2 | 12×2 |
| 20 | Desafio | 24 | 5 | 2 | — | 12×2 | 16×2 |
| 21 | Ensino | 22 | 4 | — | 8 (5 máquinas) | — | — |
| 22 | Combinação | 22 | 5 | — | 10 (4 máquinas) | 6×1 | 8×1 |
| 23 | Combinação | 24 | 5 | — | 12 (3 máquinas) | 8×2 | 8×1 |
| 24 | Preparação | 24 | 5 | — | 12 (3 máquinas) | 6×2 | 12×2 |
| 25 | Desafio | 25 | 5 | 1 | 16 (17 máquinas) | 8×2 | 12×2 |
| 26 | Respiro | 24 | 4 | — | 8 (6 máquinas) | 4×1 | 6×1 |
| 27 | Combinação | 24 | 5 | 1 | 12 (8 máquinas) | 6×2 | 10×1 |
| 28 | Combinação | 25 | 5 | — | 14 (9 máquinas) | 8×2 | 12×2 |
| 29 | Preparação | 25 | 5 | 1 | 12 (5 máquinas) | 10×2 | 12×2 |
| 30 | Desafio | 28 | 5 | 2 | 18 (21 máquinas) | 12×2 | 16×2 |

## Ferramentas e verificação

### Franqueadoras nas extremidades — revisão vigente

Fases 21–30 atualizadas para formações completas na borda. Uma máquina sempre tem uma face voltada ao exterior da área jogável; não há máquinas isoladas no centro.

| Fase | Formação | Máquinas | Cartas compartilhadas |
| --- | --- | --- | --- |
| 21 | Fileira inferior | 5 | 8 |
| 22 | Coluna esquerda | 4 | 10 |
| 23 | Fileira inferior | 3 | 12 |
| 24 | Coluna direita | 3 | 12 |
| 25 | U | 17 | 16 |
| 26 | Fileira inferior | 6 | 8 |
| 27 | Coluna direita | 8 | 12 |
| 28 | Fileira inferior | 9 | 14 |
| 29 | Coluna esquerda | 5 | 12 |
| 30 | U | 21 | 18 |

Esta tabela substitui a quantidade anterior de máquinas na tabela de parâmetros. Preservados movimentos, paletas, quantidades e resistências de caixas/grama, número de armários e limites de estrelas. Nas fases 25/27/30, a silhueta foi ajustada para acomodar a formação na borda. Na 30, os dois armários foram deslocados para manter reposição nas células abaixo deles.

O verificador agora confere continuidade e formato das formações, acesso aos móveis e um caminho potencial de gravidade para cada célula que pode conter peças, considerando os móveis como bloqueios. Caixas são consideradas removíveis; isso não é uma simulação completa de partidas. A validação aceita as normalizações da Unity para células vazias (`blocks: []`) e paletas inteiras, sem regravar as fases 1–20. Prévia e planilha atualizadas.

### Correção da fase 11 sobrescrita

Após a entrega, o asset da fase 11 voltou ao layout antigo de 8×8, dois armários e 500 movimentos. O relatório nativo anterior tinha confirmado o layout novo de 7×7 e 22 movimentos; as fases 12–30 continuavam corretas. Reaplicada somente a receita 11, preservando as outras fases. A cópia com 500 movimentos está em `Backups/Level11-stale-500-moves`.

O Level Editor marcava a fase como alterada ao perder foco, mesmo sem edição, e podia salvar uma cópia antiga sobre o arquivo novo. Removida essa marcação forçada; alterações reais na aba Editor continuam sendo marcadas. Antes de salvar, uma revisão SHA-256 detecta mudanças no arquivo desde o carregamento: preserva o rascunho em `Logs/level-editor-conflicts`, importa a versão atual e evita a sobrescrita. O fluxo foi compilado; conferência interativa de troca de foco/conflito ainda necessária.

O validador nativo agora compara movimentos, paleta e dimensões com as receitas, além das checagens estruturais. Para reaplicar uma única fase: `python Tools/level-map/build_campaign_11_30.py --level 11`. O lote completo continua sendo validado depois.

Receitas editáveis: `Tools/level-map/campaign-11-30-recipes.json`. Reconstrução: `python Tools/level-map/build_campaign_11_30.py`. `--check` confere os assets sem reconstruir. A montagem preserva o primeiro backup, os `.meta` e o bloco aprovado. A planilha `campaign-progression-v1.csv` registra os parâmetros; a fonte da pontuação é `campaign-star-thresholds.csv`, compartilhada com o calibrador.

Prévia esquemática das vinte fases: [abrir painel](Campanha_Formas_11_a_30.svg).

Verificação estática executada: dimensões, coordenadas, alvos e quantidades, tipos/camadas das células, paletas, grupos 2×2, presença de caminhos de combinação adjacentes aos móveis, conectividade da forma, entradas acima dos segmentos, limites crescentes de estrelas e preservação do bloco 1–10. Relatório: `Logs/campaign-11-30-validation.json`. Runtime/editor compilados via MSBuild.

Validador nativo disponível em `Royal Aves > Campanha > Validar fases 11 a 30`. Ele confere os assets desserializados pela Unity, as artes da apresentação e as metas. A solicitação por arquivo `Logs/campaign-11-30-native.request` só é processada fora do Play, após compilação/importação. O resultado vai para `Logs/campaign-11-30-native.result`.

Os limites de estrelas e a dificuldade são valores iniciais para playtest, orientados pelo bloco aprovado. Checagem estrutural e compilação não medem taxa de vitória. Conferir em Play: apresentações 11/21, abertura e esvaziamento dos armários, coleta independente das cartas nas fases combinadas, abastecimento sob os móveis, vitória após concluir todas as metas e ritmo dos desafios 15/20/25/30. Não foram simuladas partidas completas nesta entrega.
