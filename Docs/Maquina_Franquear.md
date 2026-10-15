# Máquina de Franquear — protótipo

## Correção de posicionamento — 15/10/2026

Usuário corrigiu a orientação: a franqueadora permanece **dentro do grid**, ocupando uma célula 1x1. Pincel FrankingMachine restaurado, painel lateral removido e câmera voltou ao enquadramento normal. Acionamento original por match adjacente ou impacto de especial na máquina.

Mantidos meta própria “Cartas da franqueadora”, quantidade manual, separação das metas do armário, limite de entregas simultâneas e envelope 80% maior. Laboratório restaurado para uma máquina dentro do grid com oito cartas.

O nível 4 já continha posições laterais salvas. `RestoreGridPlacements` lê esse formato legado e converte cada posição para a célula livre mais próxima da mesma linha/lado, sem substituir outros obstáculos. Conversão executada ao abrir o nível no editor (salva pelo fluxo normal do editor) e ao montar o tabuleiro em Play. Formato legado mantido oculto somente para compatibilidade; posições sem espaço livre permanecem pendentes. Nenhuma geração externa continua ativa.

Runtime e editor recompilados sem erros. Conferência interativa ainda pendente. A seção seguinte documenta a revisão lateral substituída.

## Histórico: revisão lateral substituída — 15/10/2026

O comportamento anterior de ocupar uma célula foi substituído. No criador de níveis, o painel **Franqueadoras fora do grid** adiciona máquinas por lado (esquerda/direita) e linha (contada a partir de 1). A lista fica em `FieldData.frankingMachines`, é salva com o nível/subfase e copiada separadamente ao entrar em Play. A câmera reserva margens para os geradores; as células continuam disponíveis para peças e queda.

Cada máquina se associa à célula jogável mais externa da linha escolhida. Impactos de matches/especiais nessa célula acionam uma entrega. Máquinas pintadas anteriormente como FrankingMachine são convertidas para posições laterais, removendo apenas a camada da máquina e preservando o restante da célula. O pincel antigo deixa de aparecer. Linhas sem células jogáveis não criam máquina.

Nova meta **Cartas da franqueadora** (`MachineLetters`), adicionada ao fim da configuração global para preservar todos os índices existentes. Quantidade manual, inicialmente 8 ao selecioná-la; editável tanto no criador quanto no Target editor. Somente a entrega de uma carta da máquina reduz essa meta. Cartas do armário e seu destino no HUD excluem o objetivo da máquina. Reservas de entregas em andamento impedem emissões excedentes por máquinas simultâneas; a entrega e o acionamento ainda pertencem à fila de animações do tabuleiro.

Envelope aumentado em 80% durante a ejeção e o voo, apenas nas máquinas. A animação compartilhada do armário mantém escala padrão. O laboratório foi adaptado para máquina na lateral com meta própria de oito cartas.

Validação: compilação runtime e editor via Roslyn do Unity sem erros. Conferência interativa de posicionamento, impactos e contador ainda pendente no Unity. As notas abaixo documentam as versões anteriores.

Próximo elemento do plano, com estreia proposta na fase 21. Laboratório iniciado em 14/10/2026.

## Regras provisórias do laboratório

Gerador fixo ocupando uma célula, bloqueando troca e queda. Um match adjacente ou impacto de especial aciona a prensa e gera uma carta. A máquina permanece no tabuleiro; não consome estoque nem quebra. Emissão direta ao objetivo, sem inserir cartas no grid, limitada ao restante da meta. Laboratório inicial: oito cartas, 40 jogadas, máquina em (2,2), coordenadas iniciadas em zero.

A prensa reage durante 0,18 segundo e a carta segue a animação já aprovada: pirueta, rastro e chegada ao ícone da meta. A contagem ocorre somente no fim do voo. A fila de animações impede vitória antes da entrega. Durante um ciclo, novos impactos são absorvidos; um próximo ciclo pode começar quando a máquina estiver pronta. Este contrato de ativação é provisório e deve ser validado com combinações de especiais antes da campanha.

## Como testar

**Royal Aves → Elementos → Máquina de Franquear → Teste no tabuleiro**. Painel: emitir carta manualmente, duração do voo, meta e quantidade emitida. Reiniciar recria a fase. Desativar laboratório restaura o carregamento normal. Ativar caixa, grama ou armário desativa a máquina e vice-versa.

Assets de laboratório são independentes dos assets da campanha. Objetivo por quantidade manual clonado apenas em runtime para preservar a configuração global do objetivo reutilizado.

## Visual inicial

Arte gerada com imagegen integrado: `Assets/RoyalAves/Art/Obstacles/FrankingMachine/CM_maquina_base_v1.png`. Madeira quente, prensa de latão e alavanca com ponta vermelha. Prompt: máquina de franquear compacta, quase quadrada, para uma célula de match-3; formas cartoon arredondadas, poucas peças grandes, sombras suaves, base de madeira, arco/prensa dourados e alavanca vermelha; sem ornamentos, engrenagens pequenas, texto ou cenário; fundo transparente.

O protótipo anima a compressão da prensa como um conjunto. A alavanca ainda não foi separada para um movimento independente. Som próprio e aceite visual ficam para a próxima revisão; nenhum ajuste de campanha foi realizado.

## Verificação

Scripts de runtime e editor compilados sem erros com as referências da Unity. Assets e prefab criados pela API do editor; laboratório aberto na tela de jogo. Primeira emissão: uma carta, meta 7. Impacto em célula adjacente pelo fluxo `Square.DestroyBlock`: duas cartas emitidas, meta 6, máquina fixa. Registro das entregas em `Logs/franking-machine-collection.result`; captura do tabuleiro em `Logs/franking-machine-preview.png`.

Sequência completa conferida em Play: oito cartas emitidas, meta zero, máquina permaneceu fixa. O laboratório encerrou após a última entrega e foi reiniciado para avaliação.


## Revisão: alavanca e saída pelo bocal

A máquina agora usa corpo e alavanca separados. A alavanca gira em torno da conexão lateral: desce de −28° a −100° em 0,24 segundo, permanece pressionada brevemente e retorna em 0,24 segundo depois da ejeção. A carta fica oculta durante o acionamento. Ao sair, mantém rotação zero e desliza pelo bocal durante 0,24 segundo; só depois levanta e faz a pirueta até a meta. A fila de animações inclui o acionamento e a entrega.

Artes produzidas pela ferramenta integrada imagegen: CM_maquina_corpo_v2.png e CM_maquina_alavanca_v2.png, em Assets/RoyalAves/Art/Obstacles/FrankingMachine/. Prompts: remover somente haste/manopla da máquina aprovada, preservando corpo, pivô e posição; criar a alavanca isolada em pé, com haste dourada e bola vermelha, sem pivô ou outras partes, com transparência.


A carta permanece achatada e sem giro durante toda a ejeção reta; ganha a perspectiva frontal quando levanta, já fora do bocal. A pirueta da máquina começa após 28% do voo, dando tempo para afastar a carta do corpo. Duas emissões foram conferidas com redução correta da meta e retorno da alavanca.


Revisão de ritmo e HUD: descida da alavanca 0,12 s, pressão 0,025 s, retorno 0,13 s, ejeção reta 0,14 s e voo 0,65 s. Blob elástico de 0,22 s aplicado ao conjunto quando ativado. Durante o voo, carta e rastro passam a uma camada Canvas acima do HUD (sortingOrder 32700), com conversão das coordenadas do tabuleiro; isso evita ocultação pelos painéis da fase. Os elementos visuais temporários não interceptam cliques e são removidos no fim ou ao reiniciar.


Validação da revisão rápida: prefab importado com voo 0,65 s, ejeção 0,14 s e entrega acima do HUD habilitada; uma ativação concluiu em emitidas=1, meta=7, sem erro de execução do novo ciclo. Laboratório reiniciado para avaliação.


Blob aumentado: deformação horizontal de 22% e vertical de 20%, com retorno elástico no mesmo intervalo de 0,22 s.


Revisão do impacto elástico: compressão em 0,055 s (28% na largura e 24% na altura), seguida de rebote amortecido por 0,365 s. A base permanece apoiada durante a deformação; posição e escala originais são restauradas ao terminar. Compilação validada sem erros.
