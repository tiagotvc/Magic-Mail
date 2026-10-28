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
