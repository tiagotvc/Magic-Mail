# Progressão da campanha — primeira proposta executável

Regra vigente de franqueadoras: sempre nas extremidades do grid, em grupos fechando fileiras, colunas ou U. Fases 21-30 atualizadas; ver Maquina_Franquear.md e Campanha_Fases_11_a_30.md. Nao posicionar unidades soltas no centro.

Campanha ampliada: fases 11-30 aplicadas. Fontes atuais e verificacoes em [Campanha_Fases_11_a_30.md](Campanha_Fases_11_a_30.md). Armario estreia na 11; franqueadora na 21; ambos com apresentacao. Desafios 15/20/25/30. Bloco aprovado 1-10 preservado; estrelas 11-30 iniciais, pendentes de playtest. Os registros abaixo de fases 11-100 ainda legadas sao historicos.

REGRA VIGENTE (28/10/2026): desafios a cada cinco fases. Parametros atuais de 1-10 em [Campanha_Ciclos_de_Cinco.md](Campanha_Ciclos_de_Cinco.md); essa revisao substitui os movimentos, alvos, camadas e papeis historicos abaixo.

Data: 23/10/2026. Estado: roteiro de design; não aplicado aos assets de fases.

## Base e escopo

Fonte de inventário: `Plano_Implementacao_Elementos.md`, revisão de 200 níveis, e `Introducao_Elementos.md`. Oito elementos existentes sustentam o roteiro inicial de 100 fases: Caixa, Grama, Armário postal, Franqueadora, Gelo, Ovo mágico, Poções e Bolsa do carteiro. Nomes internos antigos não representam elementos adicionais: Tinta é Grama e Carta Voadora é Ovo mágico. Os quatro elementos ainda a produzir não são requisitos deste lote.

O plano de 200 fases continua como horizonte. A primeira entrega concentra a autoria em 100 fases; fases 101–200 dependem de variedade comprovada em playtest e/ou das próximas mecânicas. Não há contagem matemática que garanta 100 fases divertidas apenas pela quantidade de elementos.

Preservar fases 1–3 existentes como base de revisão. Seus parâmetros na planilha ficam vazios de propósito: medir o conteúdo atual antes de propor mudanças. Nenhum layout, meta, movimento ou tutorial existente é alterado por este roteiro.

## Estreias e ritmo

| Fase | Elemento | Condição |
| --- | --- | --- |
| 1 | Caixa | Manter a estreia já definida |
| 4 | Grama | Ensinar match sobre o piso; uma camada |
| 11 | Armário postal | Um grupo completo 2×2; meta de 36 cartas |
| 21 | Franqueadora | Meta própria de cartas; começar com acesso aberto |
| 31 | Gelo fino | Ensinar match incluindo a peça congelada |
| 36 | Gelo grosso | Introduzir a segunda camada em fase fácil |
| 46 | Ovo mágico | Quantidade finita pintada; ensinar queda e quebra adjacente |
| 61 | Poções | Um conjunto 2×2; paleta com todas as cores exigidas |
| 81 | Bolsa do carteiro | Ensinar emissão de pergaminhos e contagem na chegada |

Essas datas retomam a proposta existente e permanecem ajustáveis por playtest. Fase de estreia não deve receber pico de dificuldade. Depois de um desafio, oferecer respiro. Novos especiais e combinações precisam de um roteiro de tutorial separado, conferindo antes o que já está ativo nas fases 1–3; mencionar um especial na planilha não bloqueia os demais no runtime.

## Planilha de autoria

Arquivo: `../Tools/level-map/campaign-progression-v1.csv`. Contém 100 intenções de fase e parâmetros iniciais para 4–20. São sugestões para autoria, não resultados de simulação nem arquivos importáveis pelo jogo.

Nas fases 4–20, dimensões estão em linhas × colunas; caixas e grama indicam células, cartas indicam entregas do armário. Todas as caixas propostas inicialmente têm uma camada. Grama de duas camadas só aparece depois do primeiro aprendizado. Metas de caixa contam caixas inteiramente destruídas; grama conta células inteiramente limpas. Um impacto absorvido pela caixa não limpa a grama abaixo no mesmo evento.

Movimentos e paleta são pontos de partida. Não inferir dificuldade somente da quantidade de alvos: considerar acesso, resistência, forma do tabuleiro, queda e disponibilidade de combos. As primeiras propostas usam tabuleiros conectados e quatro peças disponíveis; validar os IDs reais no editor antes de gerar.

Para fases 21–100, deixar quantidades e movimentos em aberto até avaliar as primeiras 20. O roteiro especifica a intenção sem inventar balanceamento ainda não medido.

## Primeiro lote jogável

1. Conferir as fases 1–3 existentes, registrar metas, paleta, dimensões e movimentos.
2. Criar propostas 4–10 em cópias separadas usando os layouts da planilha.
3. Conferir regras de caixa/grama, conclusão das metas, reinício e ausência de objetivos incompatíveis.
4. Jogar cada proposta pelo menos cinco vezes sem boosters comprados e sem bônus de sequência; registrar vitórias, movimentos restantes e motivo de derrota. Essa amostra serve para triagem, não estima com precisão a taxa de vitória.
5. Ajustar um fator por vez. Só então montar 11–20, validando o ciclo completo do armário.

Ficha de teste: fase, revisão, tentativa, vitória, movimentos restantes, boosters iniciais, motivo da derrota e observações. Se uma fase de ensino falhar repetidamente por falta de acesso ao elemento, abrir o layout antes de simplesmente somar movimentos.

## Automação a implementar após o primeiro lote

Gerador no editor com receitas fixas, seed registrada e saída em pasta própria de propostas. Entrada: elementos liberados, modelo de tabuleiro, paleta, alvos, camadas e movimentos. Saída: LevelContainer e metas compatíveis com o sistema existente, editáveis manualmente.

Primeiros modelos: retângulo aberto, faixa inferior, laterais, ilhas internas com circulação e centro aberto com armário. Transformações de espelho/rotação só são permitidas quando preservam queda, acesso e grupos 2×2.

Validações necessárias: IDs de paleta válidos, metas coerentes com obstáculos, grupos completos do armário/poções, cores exigidas pelas poções presentes, camadas permitidas, espaço jogável e nenhuma sobrescrita da campanha sem ação explícita. Conectividade sozinha não prova que uma fase é vencível. O gerador deve produzir candidatos, sem rotular automaticamente como balanceados.

## Apresenta??o obrigat?ria de novidades

Toda estreia deve incluir a tela aprovada de novo elemento, configurada junto da fase. Seguir Introducao_Elementos.md. Grama na fase 4 j? ligada ao fluxo; futuras estreias devem cadastrar conte?do e disparo.


## Variedade de formas ? 28/10/2026

Feedback do usu?rio: n?o repetir quadrados perfeitos em todos os n?veis. Planejar silhuetas distintas (recortes, faixas, leque, cruz, H e ampulheta), al?m de alvos e movimentos. Fases 4?10 revisadas com c?lulas ausentes reais e entradas de reposi??o nos segmentos verticais. Ver Campanha_Fases_4_a_10.md e Campanha_Formas_4_a_10.svg. Manter apresenta??o aprovada nas estreias de elementos.
