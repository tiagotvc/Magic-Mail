# Bônus dos movimentos restantes — 28/10/2026

Ao completar as metas na campanha, o tabuleiro permanece visível antes do envelope de vitória. Cada movimento restante transforma uma peça normal livre em foguete horizontal ou vertical, com som e efeito de aparecimento existentes. O contador desce a cada conversão, em intervalos de 0,04 s. Não há o limite antigo de cinco movimentos.

Revisão de ritmo: aguardar os callbacks de todas as conversões e o término dos brilhos de nascimento antes de começar a detonar. Ativar um especial por frame, sem pausa cronometrada e sem aguardar uma cascata inteira entre disparos. Os efeitos podem se sobrepor. Ao terminar os disparos disponíveis, aguardar somente a resolução real das destruições, quedas e reservas da multicolorida antes de avaliar o resultado. Removida também a espera fixa de 0,4 s depois do bônus. Se for necessário dividir em lotes por falta de espaço, cada lote aguarda sua colocação antes de detonar.

Após as conversões, os especiais são ativados e o jogo aguarda a resolução de destruições e quedas antes de salvar a pontuação e mostrar a vitória. Especiais que já estavam no tabuleiro também participam, inclusive quando não restam movimentos. A destruição usa a pontuação existente do motor; a recompensa da campanha continua sendo uma estrela, fixada após o bônus.

Se houver mais movimentos do que peças normais disponíveis, detona um lote e preenche o tabuleiro antes de continuar. Peças congeladas, reservadas pela multicolorida, caindo ou em destruição não são convertidas; ovos, bolsas e outros itens especiais também não são candidatos. Em um tabuleiro excepcional sem peças conversíveis após a resolução, cada movimento restante vale 100 pontos diretos para evitar travar o encerramento. Fases de tempo não convertem segundos em foguetes.

Implementação: `LevelManager.CampaignWinBonus` e `FireCampaignWinSpecials`. A campanha não usa o gesto antigo de toque para interromper o bônus; ele poderia cancelar efeitos antes da resolução. O fluxo legado fora do lobby permanece separado.

## Validação

Runtime compilado via MSBuild. A conferência interativa no Unity ainda está pendente; compilação não verifica animação, sequência de efeitos ou balanceamento da pontuação.

Roteiro de Play:

- Ganhar com 0, 1, 6 e mais movimentos do que células livres: verificar consumo completo e envelope apenas após resolver o bônus.
- Ganhar com foguete, bomba, avião e multicolorida já presentes: conferir ativação e encerramento, inclusive sem movimentos restantes.
- Testar tabuleiro recortado, poucas peças normais, gelo remanescente e item reservado pela multicolorida: conferir ausência de conversão indevida ou travamento.
- Conferir pontuação final salva incluindo as explosões e recompensa de exatamente uma estrela.
- Tocar durante a sequência: não gastar movimento nem pular efeitos; conferir também reinício e saída de cena.


## Correcao vigente de estrelas (28/10/2026)

A regra anterior de uma estrela fixa foi substituida: pontuacao final, incluindo o bonus, determina 1 a 3 estrelas pelos limites de cada fase. Ver Estrelas_Por_Pontuacao.md.
