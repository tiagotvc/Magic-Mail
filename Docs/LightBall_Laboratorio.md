# Laboratório interativo da Light Ball

Unity: **Royal Aves > Efeitos > Light Ball > Laboratório no jogo**.
O painel também abre em **Painel de testes** nesse menu.

Escolha o cenário e pressione **Abrir / reiniciar cenário selecionado**:

- Cor comum: Light Ball com uma peça de cor zero à direita.
- Com dinamite.
- Com foguete horizontal.
- Com hélice.
- Duas Light Balls.

O tabuleiro real tem 7×7 células, 40 movimentos e duas metas de 20 peças cada. Arraste
a Light Ball no centro para a peça imediatamente à direita. O painel oferece ativação
do combo e coleta de uma peça por diagnóstico, além de velocidade de 0,2× a 1,5×.
Os comandos de diagnóstico não simulam ponteiro; a troca manual no grid usa a entrada
normal do jogo. Alterar o cenário no painel exige reiniciar para aplicá-lo.

Atalhos de boosters pagos são ocultados somente na instância em Play. O laboratório
não entra na premiação nem avança a campanha ao completar as metas ou acabar jogadas.
**Desativar e voltar às fases normais** sai do Play e remove a substituição da sessão.
A velocidade volta ao normal ao sair. Abrir outro laboratório também tem precedência
sobre este. Não são alterados os dados de fase da campanha.

## Implementação

Assets gerados próprios: `Assets/RoyalAves/Laboratory/LightBallLevel.asset` e
`LightBallTargets.asset`. Preparação pelo menu ou arquivo de pedido
`Logs/lightball-lab.request` com `prepare`; abertura com `open`.
Seleção de cenário e ativação usam SessionState do Editor, sem PlayerPrefs.

O carregamento faz uma cópia da fase e injeta o parceiro selecionado. Os cinco prefabs
já existentes são usados; sem arte nova, modificação de sprites ou registro de pool.
As metas na cópia de runtime acompanham a arte das peças efetivamente instanciadas.

O layout preparado é mantido sem limpeza aleatória inicial; depois segue a rota de
câmera/PrePlay/metas. O enquadramento do laboratório é aplicado somente em runtime,
com adaptação ao viewport; não salva câmera ou hierarquia em `game.unity`. O fitter
legado da câmera fica suspenso nessa bancada, para evitar reposicionar o grid fora da tela.

## Verificação

Compilações em `Logs/lightball-lab/*-build.log`. Preparação em
`Logs/lightball-lab-prepare.result`. Estado corrente em `Logs/lightball-lab-status.result`.
O pedido `smoke` abre o cenário comum, exige duas metas com área renderizada e dentro
da tela, testa uma troca por código, observa giro/coleta e reabre uma bancada limpa.
Relatório em `Logs/lightball-lab-smoke.result`; não é aceite por ponteiro físico.

Capturas nativas: `Logs/lightball-lab-ready.png`, `lightball-lab-spin.png` e, quando
amostrada, `lightball-lab-collection.png`. Aceite visual, áudio e teste em dispositivo
permanecem separados. O smoke do cenário comum não significa cinco partidas completas.

## Resultado final

PASS em Play nativo no cenario comum: 49 celulas, duas metas renderizadas com limites
conferidos dentro da tela, troca pela rota real por codigo, giro/marcacao, voos e
reducao das metas. Bancada reaberta limpa em Playing depois do teste.
Os cinco prefabs de parceiro foram conferidos na preparacao; nao se afirma cinco
partidas completas nem verificacao de ponteiro fisico.

O Game View usa resolucao fixa de 1440x2960 nesta bancada para coincidir com o display
de renderizacao do HUD remoto. A selecao anterior e guardada em SessionState e e
restaurada ao desativar; um tamanho criado pelo lab so e removido se ainda tiver o
rotulo proprio. A camera do Canvas de metas e vinculada apenas na instancia de Play.
Layout de pecas reaplicado antes do PrePlay para reinicios comparaveis.

As tentativas iniciais detectaram perda do prazo de smoke no reload de scripts e
um desencontro entre Game View 401x664 e HUD no display 1440x2960. Esses estados nao
foram tratados como aprovacao visual. A execucao final passou apos corrigir o prazo,
o enquadramento e a resolucao. Copias de resultado/capturas em `Logs/lightball-lab/`.
