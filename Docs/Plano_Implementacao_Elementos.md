# Correio Mágico — plano de implementação dos elementos

Data: 13/10/2026. Fonte: `Guia_Producao_Elementos_Correio_Magico.docx`, versão 1.0.

Revisão da primeira entrega: por decisão do usuário, a Pilha passa a ser representada por um pacote único de tamanho constante, com embrulho removido progressivamente. A quantidade de caixas deixa de indicar resistência; os quatro estados de proteção indicam o dano. Essa direção substitui as descrições iniciais de pilha neste plano e no guia.

A meta conta pacotes completos: danos intermediários não coletam; apenas a conclusão da animação do último rasgo destrói e contabiliza uma unidade.

Direção visual mais recente: pacote fechado em todos os estados; retirar selo, fita vertical e fita horizontal até restar papel pardo. A conclusão anima o desaparecimento inteiro do pacote, sem abrir ou rasgar sua embalagem.

## Escopo e situação atual

Implementar os 11 elementos previstos para as primeiras 100 fases, fechando regras, comportamento, arte, animação, áudio, objetivos e configuração no editor. O guia é uma proposta: suas regras e pendências são insumos de design, e não decisões já homologadas. As implementações e revisões abaixo registram o andamento após a aprovação do plano.

O projeto usa Unity 6000.6.0f1, Sweet Sugar e conteúdo próprio em `Assets/RoyalAves`. A inspeção inicial encontrou:

- `Square`, `SubSquare`, `LayeredBlock` e `BindLayer`: blocos, camadas, ordenação visual e permissões de movimento.
- `Item`: peças, possibilidade de combinação e destruição por vizinhos; candidato para os alvos móveis.
- `FieldBoard` e `Square`: fluxo de queda e destinos de teleporte.
- `TargetCounter`, `TargetComponent` e configuração de objetivos: pontos de integração para coleta e progresso.
- `LevelMakerEditor` e `LevelData`: configuração das fases e dos elementos.
- Arte de obstáculos existente (`crate`, `parcel`, `parcel-open`, entre outras): referências candidatas; compatibilidade visual ainda precisa ser avaliada no tabuleiro.

Isso não comprova suporte completo às novas regras. Antes de reutilizar um bloco, verificar sua cadeia de dano, contagem de objetivos e interação com especiais. Há alterações locais em código, arte e fases; preservar esse trabalho durante a implementação.

## Método de trabalho por elemento

Cada elemento será uma entrega completa, seguindo este ciclo:

1. **Ficha de regra:** ocupação, camadas, fontes de dano, movimento, objetivo, condições de término e exceções. Registrar decisões pendentes e propostas separadamente.
2. **Protótipo jogável:** prefab provisório, integração com editor e objetivos, fase pequena isolada. Validar a mecânica antes de produzir todos os frames.
3. **Conceito visual no grid:** silhueta e estado principal ao lado das peças atuais; comparar com elementos já aprovados.
4. **Produção:** estados de dano, animações, efeitos, ícone de objetivo e áudio. Manter âncora, escala e perspectiva.
5. **Integração e aceite:** testar matches, especiais, cascatas, camadas, queda, contadores e reinício; validar leitura no celular.
6. **Progressão:** inserir a estreia e fases de reforço; combinar com elementos anteriores e ajustar dificuldade.

Só avançar à arte final após estabilizar a regra. Só inserir na campanha após o aceite da fase isolada. Ajustes de regra após esse ponto devem explicitar o impacto sobre arte e fases.

## Etapa 0 — contrato comum de regras e integração

**Entrega:** ficha comum de interações, mapa de integração com o Sweet Sugar e uma fase de laboratório.

Decisões a fechar:

- Unidade de dano: distinguir jogada, cascata, ativação e impacto. Proposta inicial: uma mesma ativação não danifica duas vezes o mesmo alvo; ativações distintas podem causar novos impactos. Validar combinações de especiais antes de adotar.
- Adjacência: proposta inicial de quatro vizinhos ortogonais, sem diagonais.
- Matriz por fonte: match na célula, match adjacente, cada especial, combinação de especiais e ferramentas do jogador. Ferramentas precisam de regra explícita para alvos exclusivos de power-ups.
- Ordem de resolução: camada superior, ocupante e camada inferior; decidir se um impacto atravessa uma camada removida. Proposta inicial para fita: absorve o impacto que a remove.
- Objetivos: contar unidades coletadas ou objetos destruídos, não confundir dano com coleta. Definir quando a vitória é verificada em relação à emissão de cartas e aplicação de tinta.
- Dimensão de cada objeto, tamanho efetivo da célula, escala de sprite, pixels por unidade, margem de efeitos, sorting e importação. Medir na cena existente antes de fixar números.
- Distinguir cartas emitidas diretamente à interface de cartas inseridas no tabuleiro. Essa decisão define a Máquina e evita criar um sistema desnecessário.

Direção técnica: concentrar comportamento próprio em `Assets/RoyalAves/Scripts`, usando os pontos existentes de integração. Criar dados configuráveis para vida, fontes de dano, cor, emissão e espalhamento quando necessários; evitar uma infraestrutura genérica maior que o primeiro protótipo. Não reordenar enums serializados nem renomear prefabs carregados por `Resources` sem migração. Configurar fases pelo editor.

**Aceite:** uma interação tem resultado previsível, nenhum evento duplica coleta, e a fase de laboratório reproduz fontes de dano e camadas relevantes.

## Etapa 1 — Pilha de Encomendas e Grama

Primeiro implementar a Pilha por inteiro; depois o piso. Por decisão do usuário, o piso desta etapa passa a ser Grama, usando a arte original do projeto. Esse par valida ocupante e piso antes dos comportamentos mais complexos.

| Elemento / estreia | Regra do guia | Decisões e protótipo | Design e aceite específico |
| --- | --- | --- | --- |
| Pilha / fase 1 | Obstáculo com 1–4 camadas; match adjacente ou power-up remove uma camada por impacto | Definir bloqueio de queda/troca, objetivo por pilha ou camada e incidência de múltiplos matches. Laboratório com as quatro vidas | Quatro estados de caixas kraft com corda; dano e remoção com papel/barbante. Diferenciar do selo e da peça presente; cada dano deve ser visível |
| Grama / fase 4 | Piso com 1–2 camadas; match na célula ou power-up remove um nível | Regras validadas: objetivo por célula completamente limpa; pacote acima absorve o impacto. Laboratório com grama rala/cheia e peça acima | Arte original `grass` e `grass-sparse`, em verde, abaixo das peças e contida na célula; remoção e partículas a produzir |

**Entrega:** dois prefabs, configurações no editor, ícones de objetivo, efeitos/áudio e fases de estreia e reforço. Confirmar interação Pilha + Tinta antes da próxima etapa.

## Etapa 2 — Escaninho e Máquina de Franquear

| Elemento / estreia | Regra do guia | Decisões e protótipo | Design e aceite específico |
| --- | --- | --- | --- |
| Escaninho / fase 11 | Recipiente fixo; impactos adjacentes ou power-ups liberam cartas até zerar contador | Dimensão definida pelo usuário em 14/10/2026: um único objeto 2×2 (quatro células). Definir estoque, cartas por impacto e destino do recipiente vazio. Proposta: uma carta por impacto válido e contador local legível | Madeira com nichos; cheio, parcial, vazio, saída de carta e coleta. Contador não depende de envelopes minúsculos |
| Máquina / fase 21 | Gerador fixo de cartas-objetivo | Fechar fontes válidas, produção por ativação, limite, destino das cartas e estado após atingir a meta. Proposta para protótipo: emissão direta ao objetivo, sem exceder o restante | Prensa de latão/madeira; idle, alavanca, emissão e retorno. Distinguir geração de estoque; nenhuma carta pode ser contada duas vezes |

**Entrega:** fluxo de emissão/coleta integrado à interface, contador consistente durante animações e fases com recipientes múltiplos. A proposta de emissão direta precisa ser decidida antes de definir como a Máquina se relaciona com a Carta Voadora.

## Etapa 3 — Carta Voadora, Central de Rotas e Malote

Implementar nesta ordem para respeitar as estreias. Reutilizar o aprendizado de movimento da Carta no Malote.

| Elemento / estreia | Regra do guia | Decisões e protótipo | Design e aceite específico |
| --- | --- | --- | --- |
| Carta Voadora / fase 31 | Alvo móvel; um impacto adjacente ou power-up coleta | Definir troca manual, gravidade, rearranjo, portais e geração. Proposta: não combinável, acompanha queda e teleporte; troca manual pendente | Envelope com cantos encantados/asas; flutuação, movimento e captura. Distinguir da peça envelope atual; flutuação não altera posição lógica |
| Central / fase 41 | Compartimentos aceitam matches da cor designada ou power-ups | Mapear IDs reais das peças, quantidade por compartimento, posição válida do match e efeito de power-ups: um compartimento, todos ou seleção determinística | Painel com símbolos e cores, progresso por compartimento e conclusão. Mostrar qual cor falta sem depender exclusivamente de cor |
| Malote / fase 51 | Alvo móvel com dois impactos: rompe cinta, depois coleta | Reutilizar contrato de movimento; definir fontes de dano, manutenção de vida durante queda/portal e contagem final | Couro/tecido, metal e lacre; inteiro, cinta rompida, abertura, queda e coleta. Primeiro dano claramente distinto; não parecer caixa |

**Entrega:** alvos móveis estáveis durante cascatas e teleporte, filtragem por cor validada e fases de estreia/reforço dos três elementos.

## Etapa 4 — Encomenda Encantada e Tinteiro

| Elemento / estreia | Regra do guia | Decisões e protótipo | Design e aceite específico |
| --- | --- | --- | --- |
| Encantada / fase 61 | Só recebe dano de power-ups | Definir vida, posição fixa/móvel, lista de especiais/ferramentas válidos e coleta. Proposta inicial: um impacto válido; confirmar antes da arte | Pacote com glifo e proteção; reação ineficaz ao match, impacto válido e liberação. Reação ineficaz não pode parecer dano |
| Tinteiro / fase 71 | Cinco estados; ao quebrar espalha Tinta | Confirmar cinco impactos ou outra correspondência entre estados e vida; fontes de dano, geometria, prioridade, nível aplicado e tratamento de tinta existente. Usar máscara/alcance configuráveis | Cinco estados com rachaduras, ruptura e aplicação de tinta no grid. Preservar peças e excluir células inelegíveis |

**Aceite adicional do Tinteiro:** resolver aplicação antes de verificar vitória; definir se a tinta gerada passa a fazer parte da meta. Testar bordas, buracos, overlays, obstáculos e vários tinteiros na mesma resolução. Se não houver destino elegível, terminar sem travar.

## Etapa 5 — Fita e Cofre

| Elemento / estreia | Regra do guia | Decisões e protótipo | Design e aceite específico |
| --- | --- | --- | --- |
| Fita / fase 81 | Overlay superior; match adjacente ou power-up remove conforme regra a definir | Fechar conteúdos compatíveis, vida, bloqueio de troca/queda/ativação e prioridade do dano. Distinguir frames rasgado/desaparecimento de camadas reais de vida | Fita translúcida cruzada, rasgo e descolamento; variantes por conteúdo. Peça/obstáculo abaixo segue identificável; sorting consistente |
| Cofre / fase 91 | Obstáculo fixo com cinco etapas; só power-ups removem proteções | Confirmar cinco impactos, fontes válidas, dimensão, bloqueio e objetivo final. Reutilizar filtro da Encantada e estados progressivos | Metal escuro, dourado e lacres; cinco estados, impacto e abertura com documentos. Distinguir do Escaninho e mostrar progresso em cada etapa |

**Entrega:** composição de camadas validada com os elementos compatíveis e fases dos dois elementos, incluindo combinação com tinta e alvos resistentes.

## Etapa 6 — campanha 1–100 e acabamento

- Preservar a cadência proposta: 1, 4, 11, 21, 31, 41, 51, 61, 71, 81 e 91. Tratar como plano de progressão sujeito a playtest.
- Para cada estreia, ensinar uma regra principal; nas fases seguintes reforçá-la e depois combiná-la com regras anteriores.
- Conferir se metas, estoque de cartas e capacidade dos geradores permitem concluir cada fase; evitar dependência de alvos que não podem surgir ou ser atingidos.
- Ajustar layouts, jogadas e quantidades após playtests, sem prometer duração ou dificuldade antes de medir.
- Revisar tutorial, ícones, áudio, acessibilidade, efeitos em cascata e desempenho no dispositivo alvo.

## Contrato de produção visual

PNG RGBA com transparência real; mesma caixa e âncora em todos os estados; resolução mestre sugerida de 4×, confirmada após medir a célula. Nome: `CM_<elemento>_<estado>_<frame>.png`. Separar base, danos e efeitos; spritesheet apenas quando útil, com margem entre frames. Versionar `.meta` junto aos assets e validar atlas/importação no Unity.

Cada pacote inclui ícone de objetivo quando aplicável e feedback sonoro de dano, reação ineficaz ou conclusão conforme a regra. A direção postal deve manter perspectiva, luz e contorno coerentes com as peças existentes. Aprovar conceito no tamanho real antes de multiplicar estados e animações.

## Checklist de conclusão por elemento

- [ ] Regra documentada; nenhuma decisão que altera o comportamento continua implícita.
- [ ] Protótipo jogável, configuração no editor e objetivo funcionando.
- [ ] Matches válidos e inválidos, especiais e impactos repetidos conferidos.
- [ ] Contagem de dano/coleta e momento da vitória conferidos.
- [ ] Queda, portal e rearranjo conferidos quando aplicáveis.
- [ ] Sobreposições compatíveis e ordem de dano conferidas.
- [ ] Estados, animações, efeitos, áudio e ícone integrados.
- [ ] Leitura 1×, fundos distintos e acessibilidade conferidos.
- [ ] Reiniciar a fase restaura vida, estoque e overlays.
- [ ] Compilação no Unity e playtest das fases afetadas concluídos.
- [ ] Estreia e reforço integrados à campanha.

Automatizar verificações de regras quando houver lógica nova de deduplicação, emissão, filtragem por cor ou espalhamento; para leitura, animação e equilíbrio, usar playtests. Não ampliar testes sem necessidade após os critérios passarem.

## Andamento atual

Pilha de Encomendas: quatro estados de pacote fechado aplicados e validados pelo usuário no laboratório. Efeitos visuais e quatro sons próprios integrados; o áudio ainda precisa de escuta no Play. A meta conta apenas a conclusão da remoção.

Grama (substitui a direção visual de Tinta Mágica neste piso): regras validadas em laboratório isolado, com uma ou duas camadas, objetivo por célula limpa e composição com pacote. Direção mais recente segue a referência anexada: tapete quadrado fechado de folhas pequenas arredondadas, inicial claro e seguinte escuro, com encaixe pelos limites visíveis para preencher a célula inteira e ícone sincronizado. Desfolhar integrado aos dois danos, com folhas arredondadas em giro/dispersão e contagem final após a animação; controles de velocidade/dispersão no painel. Aceite visual em Play e som ainda pendentes. Regras e instruções em `Docs/Grama.md`. A relação futura com o Tinteiro será definida na etapa correspondente.

Escaninho/Armário postal: laboratório 2×2 criado em 14/10/2026. Visual atual com duas portas de vidro; primeiro impacto remove as duas sem coletar, quatro impactos seguintes entregam cartas, e o vazio quebra liberando as células. Corpo estreito e topo à frente do grid por decisão do usuário; contador e meta sincronizados após o voo. Impactos adjacentes e simultâneos nas quatro células conferidos no runtime. Aceite visual pelo usuário e contrato final de estoque/deduplicação ainda pendentes. Instruções em `Docs/Escaninho.md`.

A inserção na campanha e os demais elementos continuam pendentes.

Teste visual atual da Grama: tabuleiro 6×6 com as três últimas linhas completas em grama clara de duas camadas (18/36 células, 50%); três primeiras linhas livres. Meta: 18 células limpas. Layout salvo pela API do editor e tela de teste carregada/conferida pela contagem em runtime.





Máquina de Franquear: protótipo de gerador 1×1 com laboratório independente criado em 14/10/2026. Meta inicial de oito cartas, emissão após impacto adjacente ou especial, voo até a meta e parada ao completar. Validação em Play concluiu oito emissões, meta zero e máquina fixa. Regras e visual provisórios em Docs/Maquina_Franquear.md; alavanca independente, áudio e aceite visual ainda pendentes.

