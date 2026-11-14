# Dois globos de luz â€” combinaÃ§Ã£o mais rÃ¡pida

A combinaÃ§Ã£o de dois globos de luz agora marca as peÃ§as em grupos de quatro raios simultÃ¢neos. Cada grupo espera o tempo real de percurso do raio antes de acender suas peÃ§as, com uma pausa de 5 ms entre grupos. A sequÃªncia anterior aguardava 80 ms por peÃ§a, uma por vez. A espera da marcaÃ§Ã£o de um tabuleiro cheio fica substancialmente menor.

Os **dois globos** recebem um multiplicador visual de **1,30 durante o giro**. O aumento Ã© aplicado ao renderer, junto da deformaÃ§Ã£o de giro existente; cÃ©lulas lÃ³gicas e alcance nÃ£o mudam. Ambos terminam o giro e limpam suas cargas antes da explosÃ£o. Ao terminar, cancelar ou reutilizar uma peÃ§a do pool, o tamanho original Ã© restaurado.

A seleÃ§Ã£o de peÃ§as, reservas, dano e condiÃ§Ãµes de destruiÃ§Ã£o da combinaÃ§Ã£o permanecem. Globo simples e combinaÃ§Ãµes com dinamite, foguete ou hÃ©lice mantÃªm a cadÃªncia anterior.

ImplementaÃ§Ã£o em `Assets/SweetSugar/Scripts/Items/ItemMulticolor.cs`; backup em `Backups/double-lightball-fast-2026-11-13/`. CompilaÃ§Ã£o de runtime/editor sem erros. A verificaÃ§Ã£o nativa focada usa a cÃ³pia isolada e cobre combinaÃ§Ã£o dupla, cancelamento/reutilizaÃ§Ã£o e coleta de peÃ§as; nÃ£o Ã© validaÃ§Ã£o de toque fÃ­sico, Ã¡udio ou dispositivo.

Refinamento V2: grupos de 4 → 16 e remoção da pausa de 5 ms; 80 peças passam de 20 para 5 ondas. Compilação runtime/editor sem erros. A medição nativa de 1,5565 s e a auditoria anterior correspondem à V1; não são uma medição desta versão mais rápida.
