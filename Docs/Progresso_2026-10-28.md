# Progresso de 28/10/2026

- Campanha 1–30: formas variadas, balanceamento de metas, movimentos e estrelas; desafios a cada cinco fases e receitas de geração/validação.
- Apresentação dos elementos na estreia; franqueadoras nas extremidades em linhas, colunas ou U.
- Bônus de movimentos: colocação dos foguetes a cada 0,08 s, espera da formação e pausa de 0,18 s antes dos disparos. Cascatas e novos power-ups, incluindo hélices, resolvem antes da pontuação final; removida a limitação de formação da campanha durante o bônus.
- Hélice: alvo da franqueadora identificado pelo componente, conversão segura de metas, liberação de reservas e limite de busca/voo. Alvos indisponíveis não bloqueiam as alternativas de peças comuns. Registro de busca sem alvo no Editor.
- Áudios fornecidos pelo usuário: impacto de madeira, hélice sincronizada ao voo, duas variações de quebra de vidro e ativação da franqueadora. Removidos o som herdado do armário na franqueadora e o som genérico de quebra na ativação do foguete.
- Cartas do armário com entrega acelerada; demais movimentos mantêm a velocidade anterior.
- Cascata: verificação de espaços vazios por frame enquanto resolve e pausa após queda reduzida de 0,2 s para 0,06 s.
- Editor de níveis protegido contra sobrescrita por estado antigo (caso dos 500 movimentos da fase 11).

## Verificação

Compilações de runtime/editor concluídas sem erros nas últimas alterações, incluindo `Logs/cascade-response-build.log`. Logs e binários temporários ficam ignorados pelo Git. Conferência final em Play Mode ainda necessária para ritmo das cascatas, última hélice e bônus completo. Não houve teste automatizado do voo nativo fora do Unity.

Os backups locais, fontes de áudio, documentação e ferramentas acompanham o salvamento solicitado pelo usuário.

## Integração com o remoto

Integrado `origin/sincronizar-remoto` no commit remoto `9aa8551` (mapa, HUD e janelas). Os conflitos foram resolvidos com as fontes da interface remota, a importação local sem compressão das peças e do armário, e a união da contabilização por estrelas com os eventos: `RecordFirstWin` é chamado apenas na primeira vitória; melhorias em replays continuam creditando a diferença de estrelas.

Após integrar, runtime e editor compilaram sem erros, incluindo os novos scripts remotos (`Logs/merge-28-10-build.log`). Os quatro testes de escala/animação das peças passaram. O validador de receitas 11–30 encontra divergência na fase 21, célula 22: há um power-up inicial configurado manualmente no salvamento local, enquanto a receita exige peça comum. Essa configuração já estava no commit local anterior ao merge e foi preservada. Não foram regeneradas fases para forçar o teste a passar. A verificação de whitespace aponta formatação já recebida nos assets/metas remotos; não foi feita reformatação em massa.

O Unity removeu automaticamente dois símbolos Android do ProjectSettings durante a atualização dos arquivos; foi mantida a versão integrada do remoto, preservando `SENTIS_ANALYTICS_ENABLED` e `APP_UI_EDITOR_ONLY`.
