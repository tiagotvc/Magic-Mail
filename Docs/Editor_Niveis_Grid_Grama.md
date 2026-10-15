# Editor de níveis: grid e meta de grama — 15/10/2026

O log do Unity registrava `Texture2D.GetPixels` em `LevelMakerEditor.GUIGameField`, na composição da textura `dinamite-idle`, seguida de erro de layout. Esse erro interrompia o desenho e interação do grid em fases com dinamite, incluindo 4 e 5.

Removida a composição CPU redundante por AlphaBlend no grid e nas duas paletas de itens. A prévia usa diretamente o sprite completo da dinamite, já desenhado pela rotina existente, sem exigir Read/Write nas texturas ou criar uma textura a cada repaint.

Grama já era o objetivo SugarSquare. Seletor de metas do editor de níveis e seletor do Target editor agora exibem “Grama”; SolidBlock aparece como “Caixa (SolidBlock)”. Tooltip da ferramenta de grama também mostra o nome legível. Os nomes internos, índices, GUIDs e fases existentes permanecem preservados. Ao selecionar Grama há orientação para pintar células; a contagem segue automática pelo tabuleiro. O drawer usa o índice para obter o objetivo, sem buscar pelo nome traduzido.

Validação: editor compilado via Roslyn do Unity sem erros; revisão dos dados das fases 4 e 5 confirma quantidades de células compatíveis com as dimensões. Conferência interativa de edição, salvamento e reabertura ainda pendente no Unity.
