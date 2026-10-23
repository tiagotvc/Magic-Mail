# Aviso de novo elemento — protótipo visual

Revisão do usuário: testar com **Caixas de madeira na fase 1**, usando as fases **1 a 3**, já preparadas, como base de revisão. As estreias dos demais elementos ainda serão revistas; não fixar Ovo na fase 21.

Menu Unity: **Royal Aves > Tutoriais > Prévia — Caixas (fases 1 a 3)**. A prévia agendada aguarda uma dessas fases em Play, mostra uma faixa discreta com o ícone da caixa e a instrução. Sem título ou botão, com fundo escurecido e entrada temporariamente bloqueada durante a leitura. Aparece suavemente, permanece por 5 segundos e desaparece com fade.

Conteúdo: **Faça combinações ao lado das caixas ou use especiais para quebrá-las!** Usa a caixa atual, painel e botão do jogo.

O menu continua disponível para repetir a prévia. O disparo automático das caixas foi ligado conforme a ordem revisada abaixo; intervalos dos demais elementos continuam pendentes.

## Ordem revisada

A animação inicial da meta (`PrePlay`) termina primeiro. Seu evento de encerramento chama a orientação automaticamente nas fases 1 a 3 que contêm caixas. Só então escurece a tela e aparece a faixa discreta, sem botão. A partida aguarda a leitura por 5 segundos, o aviso desaparece com fade e a entrada é liberada. Não usa atraso estimado para sincronizar com a meta. Tabuleiros e metas preservados; demais elementos ainda sem intervalos definidos.

## Revisão visual por referência

Protótipo atualizado para a composição enviada pelo usuário: fundo escurecido, título **Caixas!** creme com contornos cereja/dourado, subtítulo **Novo elemento desbloqueado!**, caixa grande com luz suave ao centro e placa creme de borda cereja abaixo. Palavras-chave destacadas na instrução. Sem botão; preserva o disparo após a meta e a saída automática. Usa a arte atual da caixa e UI gerada em código, sem importar arte do jogo de referência.
