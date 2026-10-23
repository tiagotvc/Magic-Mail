# Poção desenhada — protótipo 23/10/2026

Teste: **Royal Aves → Elementos → Poções → Laboratório poção desenhada no jogo**. Usa a fase jogável existente, com HUD, matches e especiais. O painel tem botão próprio para reiniciar essa variante. Metaballs e efeito comum continuam disponíveis nos menus anteriores; desativar o laboratório limpa a seleção.

Asset: `Assets/RoyalAves/Resources/RouteCentral/CM_pocao_escoamento_desenhado_v1.png`, 1254×1254, transparente, 16 quadros 4×4, gerado pelo imagegen integrado. Desenhos de líquido branco perolado para receber as quatro cores. A massa se alonga em fios e se abre em gotas grandes, sem terminar numa gotinha nem aplicar fade.

O vidro preserva os 12 quadros e o tempo atual. No quinto quadro começa o escoamento desenhado pela parte inferior do frasco. A queda pela tela é independente dos desenhos; a sequência acompanha a duração da trajetória, com velocidade equivalente ao metaballs acelerado. Pivôs usam centros ponderados pelo alpha, medidos por quadro, e pequenas correções uniformes de escala mantêm a área opaca aproximadamente constante (áreas brutas: 21719–25637 pixels equivalentes). Não é conservação física de volume.

Limitações: a spritesheet de vidro também contém líquido desenhado, então há sobreposição durante a ruptura. Transições de forma são discretas, sem interpolação entre desenhos. Aparência e importação do asset ainda precisam de conferência no Play.
