# Ovo mágico — protótipo

Revisão de 22/10/2026 solicitada pelo usuário: substitui a Carta Voadora. Referência de mecânica: [Royal Egg](https://royalmatch.fandom.com/wiki/Royal_Egg); [ajuda oficial da Dream Games](https://dreamgames.helpshift.com/hc/en/3-royal-match/faq/355-royal-egg/). O usuário escolheu o visual de ovo mágico.

## Como funciona

- Obstáculo móvel de uma célula e uma vida. Não tem cor de combinação e não forma matches.
- Acompanha a queda das peças, em vez de ficar fixo como a caixa. Usa o fluxo de teleporte de `Item`.
- Troca com uma peça vizinha segue a exigência de jogada válida do jogo; uma troca sem match ou ativação de especial volta.
- Match em um dos quatro lados ortogonais ou impacto de especial quebra o ovo no tabuleiro. Match diagonal não conta.
- A célula é liberada, o sprite se desfaz em cacos e a meta diminui imediatamente. Não voa para o contador e não tem coleta adiada.
- Os cacos são decorativos e não seguram a resolução. A quebra usa temporariamente o som padrão de destruição do jogo.
- Não flutua enquanto parado, não se move sozinho a cada jogada e não é coletado por chegar à última linha. Movimento aqui significa troca/gravidade, como obstáculo móvel.
- Quantidade finita, pintada no editor; não aparece aleatoriamente na reposição. A meta conta automaticamente os ovos iniciais pintados.
- Proteção contra quebra duplicada na mesma instância e reinicialização ao retornar do pool. Gelo e demais bloqueios da célula são respeitados antes da quebra.

## Como testar

A explosão do ovo inclui um clarão creme de curta duração e um anel lilás que se expande por 0,32 s. São lançados 14 cacos com impulso radial e rotação mais fortes, mantendo a queda até saírem da tela. O efeito é local e não acrescenta dano às células vizinhas nem atrasa a atualização da meta. As peças comuns conservam seu efeito anterior. Compilação aprovada; intensidade visual pendente de avaliação no Play.

Matches só quebram o ovo quando uma peça da combinação está imediatamente acima, abaixo, à esquerda ou à direita. O dano encaminhado por bloqueadores preserva a célula de origem, impedindo que um segundo salto atinja ovos na diagonal ou distantes. Acertos diretos de especiais continuam válidos.

Os cacos usam `ShatterEgg`: caem sem fade até ultrapassarem a borda inferior da câmera, quando são removidos. As partículas das peças comuns mantêm a duração curta anterior. Compilação runtime/editor aprovada; 38 verificações gerenciadas passaram, incluindo 25 posições relativas para a regra de adjacência. A aparência da queda ainda requer conferência no Play.

Após a Unity recompilar: **Royal Aves → Elementos → Ovo mágico → Testar protótipo**.

Laboratório separado 7×7, quatro ovos e 30 jogadas. Não substitui fases da campanha. A primeira jogada preparada troca (2,3) com (2,2), usando coordenadas a partir de zero: a trinca formada quebra o ovo em (2,1). Há um foguete vertical em (4,4) para testar o ovo em (4,2). O painel tem reinício, status e botão **Quebrar um ovo — diagnóstico**. **Desativar laboratório** restaura a campanha no próximo carregamento.

No Level Editor, o ícone do ovo aparece na aba de peças com tooltip **Ovo mágico**. Pintá-lo acrescenta a meta correspondente. Os IDs internos `FLYING_LETTER = 9`, `FlyingLetters`, o prefab e os nomes dos scripts foram preservados para não invalidar referências e fases salvas durante o protótipo anterior; não representam mais uma carta coletável.

## Arte

Arte atual revisada com a ferramenta integrada imagegen: **somente o ovo**, creme/lilás, com manchinhas pastel e volume suave no estilo do trevo e da pena existentes no jogo. Cinta, selo e contorno escuro grosso removidos por solicitação do usuário. Aplicada em `Assets/RoyalAves/Art/Obstacles/FlyingLetter/CM_ovo_magico_prototipo.png`, preservando GUID, transparência e canvas de 1,9 unidades. Backup da versão anterior em `Backups/MagicEgg-before-soft-speckles/`. Prompt da revisão em [Ovo_Magico_Arte_Suave.md](Ovo_Magico_Arte_Suave.md).

Prompt inicial, substituído pela revisão suave acima:

> Use case: stylized-concept. Asset type: a SINGLE intact magical egg obstacle sprite for a friendly colorful mobile match-3 game named Correio Magico. Create one original plump egg, almost round with a gently narrowed top, warm ivory cream shell with soft pastel lavender shading, ONE chunky golden curved band around its middle and a small simple red wax seal with a tiny embossed envelope shape. Match simple rounded cartoon mobile game art: very bold dark warm brown contour, broad smooth color areas, simple soft highlights and clean readable shadows. NO realistic texture, NO fine grain, NO intricate filigree. This sprite will display at 40 to 80 pixels: strong clear silhouette and thick readable outlines, minimal large details. Front view, minimal depth, centered square composition, egg fills about 85 percent of canvas height, fully visible with even transparent margins. Single egg only, no wings, no float sparkles outside silhouette, no crown, no pedestal, no floor, no external cast shadow, no words, no watermark. Genuine transparent background. This is the intact state of a one-hit breakable movable obstacle; do not depict cracks, broken shells or additional states.

## Verificação

Correção do Level Editor: ao pintar o ovo, `GUIGameField` tentava acessar `IColorableComponent.GetSprite`, mas o ovo é um obstáculo com sprite único e não possui esse componente. A prévia agora usa `ResolveItemPreviewSprite`: consulta a paleta quando existe e, caso contrário, usa o `SpriteRenderer` do prefab. O validador nativo do protótipo passou a conferir esse caminho com o prefab real do ovo. Compilação aprovada; a execução dessa verificação nova e a conferência interativa dependem de a Unity recarregar o assembly atualizado.

Runtime e editor compilados sem erros via MSBuild (`Logs/flying-letter-build.log`). Treze verificações no assembly compilado: contagem finita/pintada, células vazias/buracos, agregação entre campos, guarda de quebra repetida, suporte a especial, identidade diferente de power-up, IDs estáveis, meta, remoção da classe de entrega ao HUD e remoção da flutuação (`Logs/flying-letter-managed-tests.result`). Verificação de arquivos e referências em `Logs/magic-egg-assets.result`.

Ainda pendentes de conferência interativa no Play: queda, swaps, portais, visual e áudio dos cacos, cada especial, gelo e sequência completa até meta zero. As verificações gerenciadas não executam a física, os efeitos nativos nem a janela de jogo.
