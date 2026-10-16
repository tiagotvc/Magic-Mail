# Metas de gelo e caixa

- Selecionar **Gelo** nas metas faz a quantidade vir das células pintadas na fase. Uma célula com gelo fino ou grosso vale **uma** unidade; a camada grossa é resistência adicional, não outra coleta.
- A quantidade aparece no editor e é calculada antes de montar o tabuleiro, para funcionar também na tela inicial da fase. Ignora números manuais antigos e objetos de outras cenas. A geração das camadas não soma novamente à meta; a remoção final continua reduzindo uma unidade.
- `IceLayer.CountCells` lê camadas atuais e dados antigos de bloco/obstáculo, incluindo múltiplos campos. Quando há uma lista de camadas, ela é a fonte da contagem, evitando dados antigos de `block` que já não representam a célula.
- `IceBlockTarget.cs` fornece `SweetSugar.Scripts.TargetScripts.IceBlock`, nome exigido pelo carregador legado ao selecionar gelo como meta principal. Isso corrige a exceção por tipo ausente ao salvar/carregar a fase.
- A meta **Caixa** usa `CM_pilha_estado01_00.png`, caixa sem tábuas. Os sprites dos outros estados continuam disponíveis para identificar caixas com várias resistências. Só o ícone do HUD muda.
- Configuração global, metas de fases existentes e laboratórios foram sincronizados. `ParcelStackTools` mantém a escolha da caixa base ao reconstruir os assets. O contador também usa o ícone canônico se encontrar uma marcação antiga na meta da fase.

Validação executada na Unity aberta: células finas/grossas contam uma vez; dados antigos e vários campos; quantidade inicial do popup/HUD vem do tabuleiro (3, ignorando o valor manual 999 do teste); carregamento por reflexão da meta principal; ícone de caixa base mesmo com marcação antiga no estado com tábuas. Compilação e verificações passaram.

Ferramentas: `Royal Aves > Elementos > Atualizar metas de gelo e caixa` e `Validar metas de gelo e caixa`, implementadas em `BoardGoalTools.cs`. Os testes usam dados temporários e não alteram o layout das fases.
