# Navegação pelo mapa no editor

Na janela Mapa de níveis, clique com o botão esquerdo sobre o mapa e arraste verticalmente para avançar ou voltar. O movimento usa `CurvedLevelMap.BeginDrag`, `DragByViewport`, `EndDrag` e a mesma inércia do jogo. Um deslocamento mínimo de seis pontos distingue clique de arraste. A posição do controle lateral acompanha o mapa.

Com Editar objetos ativo, peças e efeitos mantêm prioridade de seleção e arraste. Arrastar uma área livre do chão navega pelo mapa. O botão do meio continua deslocando o enquadramento, e a roda continua ajustando o zoom. Perder o foco, soltar o mouse fora da imagem ou reconstruir a cena encerra o gesto.

O redesenho contínuo agora avança o movimento sem chamar `PreviewAt` a cada quadro; essa chamada zerava a velocidade. Alterações explícitas da posição pelos controles laterais continuam usando `PreviewAt`.

Validação em `preview-drag-validation.txt`: avanço, retorno, inércia durante dez redesenhos e salto explícito para a fase 81 sem movimento residual. Para repetir, copie `MapPreviewDragValidation.cs` temporariamente para `Assets/RoyalAves/Editor`, aguarde a compilação e crie `Temp/map-preview-drag.request`. O resultado fica em `Temp/map-preview-drag.done`. Remova a cópia e seu `.meta` após executar. A validação usa uma cena temporária e não salva configurações nem progresso do jogador.
