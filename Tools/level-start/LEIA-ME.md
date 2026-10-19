# Modal de início de fase

Visual montado com sprites de `Assets/RoyalAves/Art/modal`, na própria `CanvasGlobal/MenuPlay`.

- Atualizar pelo menu **Royal Aves > Aplicar modal de nível roxa e dourada**. Atualiza o prefab e a instância personalizada de `game.unity`.
- O mapa chama `LobbyController.PlayLevelFromMenu` → `InitScript.OpenMenuPlay`. O título e os objetivos vêm da fase selecionada.
- Mantém os três `BoostIcon` ativos da cena, seus tipos, ícones, contadores, travas, seleção e callbacks. Os controles antigos desativados continuam desativados. A animação de abertura agora inicializa os três controles ativos.
- Grátis usa o `FreeAdBoosterButton` existente, sua lista do `BoostShop` e o mecanismo original de anúncio recompensado.
- As margens transparentes e as seis partes da neve são recortadas na importação dos sprites; as imagens originais permanecem intactas.
- `LevelStartLayout` adapta o tamanho da janela à tela sem substituir a animação do menu.

## Verificação

Compilação e verificações dos vínculos salvos passaram. Foram renderizadas as fases reais 1 e 20 em 720x1280 e 720x960. As prévias usam estoque ilustrativo 2/7/5; no jogo, os contadores usam o estoque do jogador.

Play Mode passou: selecionar fase pelo mapa abre a modal com objetivos reais; Fechar fecha; selecionar novamente reabre; Jogar inicia a fase. Reprodução de anúncio e toque em aparelho físico não foram testados.

Resultados em `previews/validation.txt`, `previews/play-validation.txt` e nas imagens dessa pasta.

Para repetir: copie `SnowModalValidation.cs` e `SnowModalPlayValidation.cs` temporariamente para uma pasta `Editor`. Execute o Unity com `-batchmode -projectPath <projeto> -executeMethod SnowModalPlayValidation.PrepareRun -logFile <log>` (sem `-quit`; o teste encerra o editor ao terminar). Execute com o projeto fechado em outras instâncias do Unity. Remova as cópias e seus `.meta` após o teste.

Botões Jogar e Grátis: 448x208, com bordas em nove partes e filtragem trilinear com mipmaps na arte verde. `ApplyActionButtonPolish()` atualiza a apresentação completa mantendo os controles existentes.

Atualização da referência: botões em x ±234, y -468; fonte Calistoga e título “Nível”. Objetivos em 860x380, incluindo ícone e quantidade no cálculo do tamanho. Painel de boosters 958x400 em y -147; soquetes de 250 com centros em x -290/0/290. A prévia lê as definições diretamente, sem construir contadores dependentes do tabuleiro. Sprites exclusivos da modal compensam as margens transparentes dos power-ups sem alterar as imagens compartilhadas com o jogo.

A verificação `SnowReferenceValidation.cs` confere os limites dos ícones e números em todas as 100 fases e renderiza também a fase 81, com quatro objetivos. Para executar no editor aberto, copie esse arquivo e `SnowModalValidation.cs` para uma pasta Editor e crie `Temp/snow-reference-update.request`. O resultado fica em `Temp/snow-reference-update.done` e `previews/reference-update.txt`. Remova as cópias temporárias após executar.

Fonte da modal: Calistoga, com licenca SIL OFL em Assets/RoyalAves/Fonts/Calistoga-OFL.txt. Fonte oficial: https://github.com/google/fonts/tree/main/ofl/calistoga . Materiais exclusivos Cream/Gold/Purple, com atlas atualizado.

Abertura: LevelGoalsView configura icone, quantidade e posicao antes de ativar as copias. A area de objetivos usa ajuste matematico sem mascara. LevelStartLayout calcula a escala no LateUpdate, antes da reconstrucao do Canvas, evitando retangulos de recorte desatualizados. Center azul legado fica desativado; selecao e callbacks originais permanecem.

Para repetir o teste de abertura no editor aberto: copie SnowOpeningTask.cs para Editor e SnowOpeningProbe.cs para uma pasta de scripts runtime. Crie Temp/snow-opening.request. O teste abre/fecha as fases 1, 20 e 81, verifica os primeiros 36 quadros renderizados e salva capturas em previews/open-*.png. Retorna ao Edit Mode e restaura a fase selecionada; nao encerra o Unity nem gasta estoque. Remova as copias e seus .meta depois.
