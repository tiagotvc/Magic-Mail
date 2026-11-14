# Tela preta após o merge de 28/10

## Causa confirmada

O merge automático de `RoyalAvesLobby.prefab` eliminou cinco objetos/componentes do contêiner `TelaConfiguracoes` da versão V161, mas manteve seus filhos. A janela `Janela` ficou com `m_Father: 0`, criando uma segunda raiz. A importação do Unity passou a identificar essa janela como objeto principal do prefab. A referência `mapUiTemplate` também foi remapeada para essa janela.

Sem o `LobbyController` na instância carregada da cena, `LevelManager.EnableMap` seguia o caminho antigo do mapa e lançava `NullReferenceException` em `setNumbers`, antes de emitir `OnMapState`. O conteúdo da home não era aberto. O log original contém duas ocorrências dessa mesma sequência. A compilação C# anterior não validava a hierarquia serializada.

## Correção

- Restaurados os cinco objetos do contêiner V161 a partir do commit remoto `9aa8551`, mantendo os demais objetos do prefab integrado.
- Restaurados o vínculo da janela ao contêiner e a lista de filhos do conteúdo; agora o prefab tem exatamente uma raiz, `RoyalAvesLobby`.
- Restaurada somente a referência `mapUiTemplate` ao objeto principal; mantido o ajuste local de câmera.
- A configuração antiga local permanece preservada. `LobbyController` prioriza a janela com `RoyalPostmanSettingsFit`, evitando escolher o layout antigo pelo simples ordenamento dos componentes.
- Adicionado `LobbyMergeValidation` para validar no Unity a raiz importada, a janela instalada e as referências obrigatórias da cena real.

## Verificação executada

1. Comparação da hierarquia antes/depois: duas raízes no commit do merge; uma raiz e todos os vínculos pai/filho consistentes após a correção.
2. Compilação de runtime/editor sem erros.
3. Unity nativo: prefab reimportado, controlador na raiz, janela V161 na hierarquia, referência do kit e controlador ativo na cena real aprovados. Cena carregada em preview e fechada sem salvar (`Logs/lobby-merge-native.result`).
4. Play Mode em editor batch: cena real entrou no estado Map, conteúdo e `RoyalPostmanLiveHome` ativos e caminho real do botão Jogar abriu `MenuPlay` (`Logs/lobby-merge-play-smoke.result`). Nenhuma fase iniciada, vida gasta ou cena salva pelo teste. Não é uma partida completa nem uma conferência visual em aparelho.

Na importação apareceram dois metadados malformados antigos (`Resources/Localization.meta` e `Fonts/Candy Script_0 SDF.asset.meta`), presentes desde o commit inicial. Eles não foram alterados nesta correção. O pré-nível também emitiu a mensagem existente `set SugarSquare more than 0`; a abertura da home/pré-nível concluiu sem a exceção original.

Para repetir a verificação nativa em batch: executar `RoyalAves.EditorTools.LobbyMergeValidation.Run` com `-quit`. Para testar a rota de abertura em Play Mode: executar `RoyalAves.EditorTools.LobbyMergeValidation.RunPlaySmoke` sem `-quit`; o próprio teste encerra o editor. Nunca executar o smoke sobre uma sessão interativa do usuário.

## Segunda causa: Canvas desativado e clique em Jogar

O primeiro smoke chamava a ação `Play` por reflexão e não validava a área de toque nem a renderização do Canvas. Por isso ele não detectou o segundo defeito: a cena remota `9aa8551` tinha um override `m_Enabled: 0` para o Canvas `7410457247873206664` do lobby. O salvamento local `cbcb22b` tinha esse override ativo. O merge manteve o valor desativado do remoto.

O diagnóstico na sessão real confirmou o botão lógico ativo, mas `canvasRect` com área zero, escala zero e `Graphic.depth = -1`, sem receber raycasts. Restaurado o override ativo na cena e adicionada a ativação do Canvas em `LobbyController.Show`. A cena foi alterada em uma única propriedade.

Depois de compilar e reinicializar o Play Mode completamente, a verificação foi feita pela `EventSystem.RaycastAll` no centro do botão real: o primeiro alvo pertencia ao botão Jogar; um evento de ponteiro nessa área abriu `MenuPlay` na instância interativa do usuário. Resultado em `Logs/lobby-live-click.result`: PASS. Nenhuma fase foi iniciada nem vida gasta pelo teste. O smoke batch agora usa o mesmo caminho de raycast/clique, e a verificação de cena também exige Canvas habilitado. Os pedidos temporários de reinicialização/clique foram consumidos; o validador não executa essas ações sem arquivo de pedido.

## Vitória invisível após concluir a fase 22

O bônus concluiu normalmente e os logs registraram 3.880 pontos, três estrelas e mudança para vitória. O mesmo commit remoto também desativava o Canvas filho `7135652556380568917` da tela `Victory`. O salvamento local anterior não tinha esse override. O som tocava após `ShowVictory`, mas a tela de resultado não era desenhada.

Restaurado `m_Enabled: 1` nesse override da cena. `ShowVictory` também ativa explicitamente o Canvas antes de exibir o resultado. A validação nativa agora exige os dois Canvases do lobby habilitados; a conferência das substituições na cena não encontrou outros Canvases desse prefab desativados.

Na sessão real já vencida, foi habilitado somente o Canvas, sem chamar novamente `ShowVictory` ou rotinas de premiação. Após a atualização dos scripts, a navegação foi reconectada ao controlador existente e a referência estática de `LevelManager` recuperada, pois o hot reload não repete `Awake`. Testado por raycast o centro do botão Continuar: alvo `Continue`, PASS em `Logs/victory-canvas-recovery.result`. O teste não acionou Continuar nem iniciou outra fase. Compilação da correção sem erros em `Logs/victory-canvas-build.log`.
