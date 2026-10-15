# Escala das peças e power-ups — 15/10/2026

Direção final do usuário: manter deformações e pulsos temporários; impedir aumento permanente. O tamanho normal corresponde à escala de sprite 0,88 dos prefabs atuais, com escala de item 0,42 no tabuleiro. Ingredientes mantêm sua escala de sprite original 1.

Causas encontradas: criação de peça e retorno ao pool escreviam escala 1 no sprite; ResetAnimTransform restaurava 0,88 depois da queda. Assim, peças criadas sem queda (incluindo power-ups de matches) podiam ficar maiores. Clips compartilhados de aterrissagem/idle também usavam valores absolutos de escala 1, enquanto as animações específicas do multicolor já usavam 0,88.

`Item.RestoreBoardScale` centraliza a restauração da escala original capturada do prefab e da escala de tabuleiro. Aplicada na inicialização, geração pela célula, término da queda, retorno ao pool e antes de copiar a peça para sua destruição. Não há bloqueio de escala em LateUpdate: animações temporárias permanecem livres.

Controles de animação próprios em `Assets/RoyalAves/Animations/BoardScale/` mantêm tempos, eventos, sprites e intensidade relativa dos pulsos, com curvas de escala relativas ao tamanho correto. As versões específicas de dinamite e multicolor preservam seus overrides. Fontes compartilhadas e animações do restante do jogo não foram alteradas. Cópias geradas via `python Tools/NormalizeBoardItemAnimations.py`, com GUIDs determinísticos; dez prefabs de itens receberam seus controles correspondentes.

Validação: runtime e editor compilados sem erros. Quatro testes em `Tools/test_board_item_animation_scale.py` passaram: aterrissagem termina no tamanho normal e preserva tempos/eventos; multicolor não sofre redução dupla; pulsação temporária preservada; referências dos overrides válidas. Conferência visual no Unity ainda pendente.
