# Roteiro obrigatório para o próximo merge

Criado em 29/10/2026 após falhas de abertura da home, clique em Jogar e exibição da vitória. Aplicar antes de declarar a integração validada e antes de publicar a integração quando houver autorização para push.

**Compilar sem erros não basta. O fluxo mínimo é: home → clique real em Jogar → pré-nível → iniciar fase → concluir objetivos → bônus/cascatas → vitória visível → clique em Continuar → home → abrir outra fase.**

## 1. Preparar e salvar o estado local

- [ ] Identificar projeto, branch atual, upstream e branch principal real do remoto. Não presumir `master`; neste repositório a principal é `main`.
- [ ] Conferir `git status`, alterações e arquivos novos; preservar o trabalho manual e os ajustes de teste do usuário.
- [ ] Salvar as cenas e os prefabs pretendidos. Sair do Play Mode e esperar a compilação/importação terminar.
- [ ] Criar um commit de segurança com o trabalho local antes de integrar. Registrar seu hash.
- [ ] Fazer `git fetch origin`, identificar os commits recebidos e ler o resumo das mudanças.
- [ ] Conferir se há ferramentas de laboratório ativas ou pedidos antigos em `Logs/*.request`. Não executar pedidos antigos durante a validação; removê-los apenas quando forem resíduos dos próprios testes.
- [ ] Evitar modificar scripts com o usuário jogando: o hot reload pode apagar referências estáticas e callbacks sem repetir `Awake`.

## 2. Integrar e revisar os assets

- [ ] Integrar sem reescrever histórico ou forçar o push.
- [ ] Resolver conflitos por intenção e comportamento; revisar também os assets mesclados automaticamente pelo Unity SmartMerge.
- [ ] Procurar marcadores de conflito restantes nos arquivos próprios e conferir GUIDs dos `.meta` alterados. Distinguir problemas recebidos de defeitos antigos.
- [ ] Verificar duplicação de GUIDs, scripts ausentes e referências perdidas nos assets alterados.
- [ ] Cada prefab simples deve ter uma única raiz. Conferir pais, filhos, objetos órfãos e se o objeto principal importado é o esperado.
- [ ] No `RoyalAvesLobby.prefab`, confirmar `LobbyController` na raiz e a janela de Configurações instalada dentro do conteúdo. Preservar layouts manuais; não reconstruir a interface com builders antigos para resolver um conflito.
- [ ] Conferir referências externas ao prefab: `RoyalAvesWorldMapKit.mapUiTemplate` precisa apontar para a raiz do lobby, nunca para uma janela interna.
- [ ] Revisar os overrides de `game.unity`, principalmente `m_Enabled`, `m_IsActive`, escala, hierarquia, câmera, ordenação, raycasters e objetos removidos.

### Pontos que quebraram neste merge

- [ ] Canvas principal do lobby habilitado: ID de origem `7410457247873206664`.
- [ ] Canvas da vitória habilitado: ID de origem `7135652556380568917`.
- [ ] O GameObject da vitória deve começar oculto e ser ativado ao vencer; seu componente Canvas continua habilitado. São estados diferentes.
- [ ] Não aceitar `activeInHierarchy == true` como prova de visibilidade. Conferir Canvas habilitado, dimensões, escala, câmera e ordenação; área zero e `Graphic.depth == -1` são sinais de interface não desenhada.
- [ ] `EventSystem` e módulo de entrada ativos; raycaster habilitado; nenhum painel invisível interceptando os controles.

## 3. Compilar e importar no Unity

- [ ] Compilar runtime e editor com todos os scripts novos recebidos. Projetos `.csproj` gerados podem estar desatualizados; não confundir fontes ausentes no projeto gerado com defeito no código.
- [ ] Reimportar os assets modificados no Unity e aguardar o fim da compilação e do reload.
- [ ] Não abrir uma segunda instância do Unity usando o mesmo projeto enquanto o editor do usuário está aberto. Para batch, fechar normalmente o editor após salvar, ou usar uma cópia isolada do projeto.
- [ ] Aguardar a saída real do processo batch e conferir o resultado gerado, sem tratar o retorno imediato do launcher de Windows como sucesso.
- [ ] Usar logs/resultados desta execução e deste código; não aproveitar um PASS antigo.

Validações disponíveis em `Assets/RoyalAves/Editor/LobbyMergeValidation.cs`:

| Método | O que verifica | Limite |
| --- | --- | --- |
| `RoyalAves.EditorTools.LobbyMergeValidation.Run` | Prefab importado, raiz/controlador, Configurações instalada, referência do kit, cena e Canvases da home/vitória habilitados | Não joga uma fase |
| `RoyalAves.EditorTools.LobbyMergeValidation.RunPlaySmoke` | Home em Play Mode, raycast no botão Jogar, evento de ponteiro e abertura do pré-nível | Batch isolado; não inicia nem conclui a fase |

Para `Run`, usar `-batchmode -executeMethod ...Run -quit`. Para `RunPlaySmoke`, usar `-batchmode -executeMethod ...RunPlaySmoke`, **sem `-quit`**: o próprio teste encerra o editor. Ambos usam `-projectPath` explícito e logs próprios. O smoke não deve ser disparado na sessão interativa do usuário.

## 4. Testar o fluxo completo, com interface real

- [ ] Começar uma execução limpa: terminar o Play anterior, aguardar a entrada completa no Edit Mode e só então iniciar de novo. Não ligar e desligar Play no mesmo ciclo de atualização.
- [ ] Conferir a home visível no Simulator/Game View usado pelo usuário, sem tela preta.
- [ ] Clicar em Jogar pela área visível. Em automação, verificar o primeiro alvo de `EventSystem.RaycastAll` e enviar evento de ponteiro; chamar diretamente `Play()` ou `onClick.Invoke()` não testa bloqueios de entrada.
- [ ] Conferir o pré-nível visível, metas, nível escolhido, botões Jogar/fechar e retorno à home.
- [ ] Iniciar a fase pelo botão real e confirmar tabuleiro visível e interação de match.
- [ ] Abrir e fechar Menu e Configurações; verificar retorno, input, som/mute e ausência de painéis que fiquem bloqueando a partida.
- [ ] Completar os objetivos e deixar movimentos sobrando para testar o bônus.
- [ ] Esperar colocação dos foguetes, disparos, quedas e cascatas. Novos power-ups, inclusive hélices, devem ser ativados e pontuar antes do resultado.
- [ ] Conferir vitória visível, animação, pontuação e estrelas finais. Som de vitória ou estado `Win` sozinho não comprova que a tela apareceu.
- [ ] Clicar em Continuar e conferir retorno à home, progresso e abertura da próxima fase.
- [ ] Testar Repetir e uma melhora de estrelas sem duplicar os prêmios de primeira vitória.
- [ ] Quando alterados, testar uma fase com caixas/armário e uma com franqueadoras; conferir impacto da hélice e ausência de voo sem alvo.
- [ ] Confirmar as apresentações combinadas nas estreias de elementos e a leitura correta dos movimentos/metas.

Usar um perfil de teste para testes que alterem progresso, vidas ou moedas. Na sessão real, não forçar vitórias, repetir concessões de recompensa ou iniciar fases com custo sem autorização/contexto suficiente. Registrar os trechos do fluxo que não puderam ser executados.

## 5. Conferir logs, pontuação e arquivos após os testes

- [ ] Separar erros novos dos avisos já existentes. Investigar exceções de inicialização, reload, fim da fase e navegação.
- [ ] Comparar o log `[Campaign bonus]` com a pontuação/estrelas exibidas. Confirmar que a fase só encerrou após os efeitos pendentes.
- [ ] Se a vitória já foi registrada, corrigir sua exibição sem chamar `ShowVictory` ou rotinas de recompensa outra vez.
- [ ] Conferir alterações automáticas do Unity após reimportar: fontes dinâmicas, `.meta`, símbolos de compilação e referências remapeadas. Não descartar nem versionar essas alterações sem revisar.
- [ ] Para mudanças de campanha, rodar os validadores de receitas; divergências de configurações manuais devem ser relatadas e preservadas, sem regenerar fases cegamente para fazer o teste passar.
- [ ] Rodar `git diff --check` nos arquivos alterados. Evitar reformatação em massa de assets remotos.
- [ ] Consumir/remover apenas pedidos temporários dos próprios testes e deixar o editor em um estado conhecido. Nenhuma automação de clique/reinício deve ficar pendente.

## 6. Fechar e publicar

- [ ] Registrar hashes local/remoto/merge, conflitos resolvidos, decisões sobre assets, testes executados e resultados.
- [ ] Distinguir compilação, validação estrutural, teste de clique e partida completa. Não declarar testes que não foram feitos.
- [ ] Se alguma etapa obrigatória não passou ou ficou pendente, relatar a limitação e não afirmar que o merge está completamente validado.
- [ ] Concluir o commit da integração e, com autorização, enviar à branch escolhida sem force push, incluindo os objetos LFS.
- [ ] Confirmar que o remoto contém o commit final e conferir o estado local após o envio.

## Registro de execução

Copiar esta ficha para a documentação do próximo merge:

```text
Data:
Branch local / destino autorizado:
Commit de segurança:
Commit remoto integrado:
Commit final:
Conflitos e decisões:
Compilação runtime/editor:
Prefab/cena/Canvases importados:
Clique real home → pré-nível:
Iniciar e jogar fase:
Bônus/cascatas/novos power-ups:
Vitória visível e pontos/estrelas:
Continuar → home → próxima fase:
Repetir / melhora de estrelas:
Menu / Configurações / input:
Logs e erros novos:
Perfil de teste e efeitos sobre dados:
Verificações pendentes:
Editor deixado em qual estado:
Commit remoto confirmado / estado local:
```

Referência do incidente: `Docs/Correcao_Tela_Preta_Merge_2026-10-29.md`.
