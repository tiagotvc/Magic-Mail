# Plano do mapa 2.5D e campanha visual de 200 fases
Data: 13/10/2026. Estado: projeto e kit de assets para implementação pelo Claude.

Atualização de escopo: estas 200 posições pertencem ao primeiro mundo **Green Land**. O usuário acrescentou um mundo Lava de 250 posições e um mundo pré-histórico futuro, além de Natal por sobreposições. Consulte `PLANO-MUNDOS-SEASONS.md` para o plano mestre atual; este documento continua sendo a base técnica e o roteiro inicial Green.

Este plano responde às três referências novas, ao mapa atual e ao bug do marcador de fase desenhado sobre uma casa. Não altera o jogo, os saves, a dificuldade nem a economia. As referências orientam proporção, câmera e movimento; não identificam a tecnologia usada pelo jogo das imagens.

## 1. Leitura das referências

- Nas imagens 1 e 2 o horizonte permanece alto, mas a passagem de um mesmo marcador pelo campo de visão muda quanto do topo e da lateral se vê. A largura e a espessura do botão continuam legíveis.
- Na imagem 3 há um trecho de água com ponte antes de uma nova plataforma. A estrada termina e recomeça em terrenos diferentes; o mapa não é uma faixa de grama infinita.
- As margens parecem mais altas que a faixa central. Cor, sombra e contorno acentuam essa diferença; o protótipo precisa testar uma diferença de altura real.
- O chão usa manchas largas e faixas claras/escuras, com detalhes muito pequenos espalhados. Não depende de uma textura realista.
- Construções e personagens grandes junto às laterais ocupam o primeiro plano. Não é necessário ver tantos marcadores ao mesmo tempo.
- O efeito de os objetos se erguerem deve continuar: é uma linguagem de animação do nosso mapa, combinada com a curvatura e a oclusão.
- A câmera deve se aproximar e olhar mais para a frente. A curva principal acompanha a direção do avanço; o horizonte lateral fica quase horizontal.

## 2. Diagnóstico do protótipo atual

O protótipo já resolve navegação, regiões, emergência do horizonte e o gesto de arraste. Ele é uma boa base funcional, mas a representação visual tem limites concretos:

1. `LevelMapProjection.Rise` normaliza a projeção entre o primeiro plano e o horizonte. Essa normalização volta a esticar a imagem para a mesma faixa vertical, o que reduz o efeito visual de apenas baixar `cameraHeight`.
2. `CurvedMapTerrain` usa uma superfície contínua. A faixa central tem outra cor, porém não tem uma cota inferior nem paredes de contenção.
3. `MapLevelDisc` desenha círculos de UI sobrepostos. Achatar seu RectTransform achata também a espessura e a tipografia; não revela a lateral de um objeto com volume.
4. `CurvedMapPropImage` deforma uma única pintura. Ela pode se inclinar e levantar, mas não revelar um teto, uma lateral ou uma face inferior que não existem na pintura.
5. As decorações ficam num grupo e os níveis em outro. Os níveis são criados depois, portanto todos podem aparecer sobre todas as casas. Isso explica o bug observado no horizonte.
6. A distribuição atual usa uma senoide e densidades determinadas por um cálculo de variação. Falta composição específica para cada trecho, sobretudo nas pontes e nas passagens estreitas.
7. O canvas reserva espaço para três ações grandes além da navegação inferior, diminuindo o espaço do mundo.

## 3. Decisão recomendada

Usar um mapa híbrido: geometria 3D simples com acabamento de ilustração cartoon, sprites 2D para vegetação/personagens e o HUD existente sobre a imagem do mundo.

| Parte | Representação proposta | Motivo |
|---|---|---|
| Chão, margem e degrau | Malhas simples com cores e padrões chapados | A câmera revela alturas e faces reais |
| Água e canais | Plano separado, duas cores e movimento discreto | Quebra a continuidade do terreno |
| Ponte | Volume simples com prancha, borda e base | Mostra o tabuleiro e a espessura durante o avanço |
| Botão da fase | Medalhão/cilindro baixo com topo e lateral | A perspectiva muda sem trocar imagens |
| Número e selo | Texto/arte presos ao topo do medalhão | O número surge naturalmente quando o topo fica visível |
| Árvores, flores, arbustos | Ilustrações em cartões com pivô no chão | Mantém o visual simples já escolhido |
| Casas importantes | Volume baixo simples ou duas poses coerentes | Permite ver frente, lateral e um pouco de teto |
| Cachorro e outros personagens | Sprites animados, em pé no terreno | Personalidade com pouca complexidade |
| Céu e cenário muito distante | Camadas 2D com movimento lento | Profundidade e continuidade visual |
| HUD, agência, pré-nível | Canvas atual | Reutiliza o fluxo e o visual existentes |

Alternativa: ampliar a solução atual com faces de UI, ordenação e várias poses de sprites. Continua viável para um mapa menor, mas exige mais correções manuais de perspectiva e oclusão. Recomendo a geometria simples para as superfícies que precisam mudar de vista.

A implementação do Unity permite renderizar uma câmera para uma Render Texture, que outra câmera exibe na tela. Esse é o mecanismo proposto para apresentar o novo mundo dentro do lobby existente. [Documentação oficial do Unity 6.6](https://docs.unity.com/en-us/engine/6000.6/manual/cameras/urp/multiple/rendering-to-a-render-texture).

## 4. Câmera e curva

- Usar uma câmera de perspectiva real para o mapa, com escala vertical e horizontal relacionadas à mesma lente.
- Primeiro experimento: comparar três enquadramentos com inclinação aproximada de 10°, 14° e 18°, campo de visão entre 30° e 40°. São pontos de teste, não parâmetros finais.
- Alvo de composição: cerca de 5 a 7 fases bem legíveis, fase atual no terço inferior e objetos laterais maiores.
- Curvatura longitudinal mais perceptível, lateral discreta. Evitar voltar ao formato de cúpula.
- Preservar um horizonte estável; a coordenada de avanço movimenta a rota e os objetos em relação a ele.
- Terreno, pontes, medalhões, sombras e posições de clique devem usar a mesma transformação de mundo.
- Calcular a curvatura em geometria/posicionamento compartilhados no protótipo. Um shader que dobre apenas a aparência deixaria os colliders e os cliques nas posições erradas.
- Ajustar o enquadramento com um trecho curto antes de mudar dezenas de parâmetros ou fabricar muitos assets.
- Rever o bloco inferior: um acesso compacto ao nível atual/foco pode liberar espaço do mundo. A agência já tem acesso pelo menu inferior. Essa mudança de UI fica como proposta a testar.

## 5. Terreno com diferença de altura

O caminho tem uma cota, as margens outra. Uma borda arredondada liga os dois e mostra uma face de cor mais escura. É essa face que fica mais evidente com a câmera baixa.

```text
margem alta                      margem alta
──────────╲___ caminho baixo ___╱──────────
                   fase
```

- A diferença de altura começa pequena e exagerada o suficiente para ler numa tela de celular.
- As cores formam um conjunto: margem escura, topo da margem intermediário, estrada clara e face lateral sombreada.
- Faixas largas atravessam o chão em coordenadas de mundo. Elas avançam com o scroll, sem parecer um fundo fixo.
- A sombra de contato segue a borda; árvores e personagens não projetam sombras sobre o vazio.
- Nem toda região terá grama. Teremos calçada, terra, tábuas, pedra, areia, ladrilho e plataformas.
- As bordas podem ser macias no jardim, recortadas no bosque e retas na fábrica.
- Água ocupa uma superfície própria, abaixo das margens. A ponte fica acima da água e ancora suas extremidades nas plataformas.
- Os limites entre regiões são encontros desenhados: portão, rio, ponte, estação, túnel ou praça. Não apenas uma troca de paleta.
- Ladrilhos nas fases difíceis formam pequenos trechos de destaque ao redor do marcador, sem transformar todo o capítulo numa área difícil.

## 6. Botão de fase: frente, lateral e topo

Construir um medalhão postal de baixo custo geométrico: lateral creme/dourada, face roxa, aro e sombra de contato. Um selo ou uma asa pequena pode diferenciar o nosso design.

À distância, a câmera vê mais a lateral e uma parte pequena da face. Ao aproximar, o topo ocupa mais espaço e o número se torna legível. O texto fica preso à superfície; não deve olhar sempre para a câmera nem ser esticado junto com um círculo de UI.

A ideia de múltiplos sprites é útil para alguns prédios e personagens. Para o medalhão, um volume simples evita produzir dezenas de ângulos e evita saltos entre imagens. O número continua sendo texto do motor, não uma imagem por fase.

Se um objeto exigir poses 2D distintas:
- produzir frente, vista intermediária e vista superior a partir do mesmo desenho mestre;
- manter proporções, pivô, iluminação e contorno;
- escolher a pose pela relação entre câmera e objeto, não pelo número da fase;
- fazer transição curta e contínua, com estabilidade perto dos limites, para não alternar rapidamente entre poses quando o jogador oscila o scroll.

O mapa do Correio Mágico conserva a regra atual de uma estrela por primeira vitória. A referência ter três estrelas ou coroas não muda nossa economia.

## 7. Objetos se erguendo

- Os cartões 2D têm o pivô nos pés e uma sombra separada no chão.
- Ao sair do horizonte, o sprite passa de uma pose inclinada para a pose em pé.
- Câmera e profundidade determinam a apresentação; uma curva suave dá o toque de animação.
- Uma árvore pode se erguer mais lentamente; uma caixa de correio pode terminar com uma pequena oscilação.
- Casas não devem esticar elasticamente: sua mudança de vista usa o volume ou poses coerentes.
- O cachorro idealmente tem corpo principal e cauda animada separados. O kit entregue contém quatro poses inteiras para gestos ocasionais; separar a cauda ou produzir intermediários é trabalho adicional. A posição dos pés deve permanecer estável.
- O levantar é acionado pela posição no mundo e funciona também ao voltar o scroll. Não usar um evento único que deixe a animação errada no caminho de volta.

## 8. Bug do nível em cima da casa

Tratar geometria, ordenação e composição juntas:

1. Ordenar/ocultar objetos por profundidade real. Um nível distante não deve desenhar sobre o telhado de uma casa mais próxima.
2. Reservar a faixa central para os medalhões e seus números.
3. Definir uma área livre ao redor de cada fase, considerando o tamanho da casa na tela em vários pontos do avanço.
4. Testar a projeção dos limites dos objetos e deslocar, reduzir ou remover decoração que cubra um marcador interativo.
5. Compor os trechos estreitos manualmente; reduzir a quantidade de elementos perto da curva.
6. Dar prioridade à fase atual e aos acessos liberados, sem colocá-los artificialmente sobre telhados.
7. Usar teste de profundidade também nos cartões 2D. Uma simples lista de transparências pode continuar desenhando na ordem errada.
8. Verificar cliques com câmera baixa, ponte, terreno elevado, popups e scroll ativo.

Só mandar as casas para a frente trocaria o bug por fases cobertas. A correção completa combina profundidade correta e espaço livre.

## 9. Integração com o projeto

A inspeção encontrou Unity 6000.6.0f1 e um URP Asset com apenas o Renderer2D na lista.

Proposta:
- adicionar um Universal Renderer para a câmera exclusiva do mapa, preservando o Renderer2D padrão;
- renderizar o mapa para uma Render Texture apresentada atrás do HUD;
- manter câmera, luz/cores, materiais e objetos do mapa em um grupo próprio;
- converter a posição do toque no painel para um raio da câmera do mapa;
- separar clique e arraste com o bloqueio já existente;
- ao escolher uma fase, continuar chamando o pré-nível e o carregamento existentes;
- desligar o processamento do mapa ao entrar na partida;
- carregar apenas o trecho visível e uma margem próxima, reutilizando objetos;
- usar materiais cartoon simples, atlas de sprites, sombras de contato e água sem reflexão em tempo real.

O pacote instalado contém `UniversalAdditionalCameraData.SetRenderer(int)`; a seleção de renderer por câmera faz parte da proposta de isolamento. O protótipo precisa validar o comportamento em URP, no editor e no aparelho. Render Texture tem custo: resolução e frequência de atualização serão medidas, sem prometer desempenho antes dessa medição.

## 10. Estrutura de conteúdo proposta

Substituir a repetição automática de quatro paletas por uma rota com composição definida.

- `MapCampaignDefinition`: capítulos, comprimento, transições e referências de arte.
- `MapChapterDefinition`: intervalo de fases, céu, paleta, materiais e elenco.
- `MapSegmentDefinition`: chão, altura das margens, água, ponte, largura e pontos de conexão.
- `MapLevelDefinition`: fase real, posição na rota, tipo visual de dificuldade e estilo do medalhão.
- `MapPropPlacement`: asset, lado, posição, escala, pivô, animação e distância de segurança.
- `MapCameraProfile`: enquadramento, curva, zoom, foco e qualidade de render.

A rota deve ter curvas desenhadas, praças e pausas. Não repetir a mesma senoide nas 200 posições.

As categorias normal/difícil/muito difícil deste plano são propostas editoriais. Não encontrei um campo de dificuldade explícito em `LevelData`; será preciso definir uma metadata própria e confirmar a classificação pelo balanceamento.

## 11. Campanha: dez regiões, vinte fases cada

Cada região terá quatro pequenos trechos de cinco fases, um marco visual e uma transição. Mudanças locais aparecem a cada poucos níveis; a troca completa de região acontece a cada vinte.

| Fases | Região | Terreno e contraste | Elenco/objetos | Marco e transição |
|---|---|---|---|---|
| 1–20 | Bairro das Primeiras Cartas | Grama clara no caminho, margens verdes e canal curto | Cachorro curioso, carteiro, caixas de correio | Agência pequena e primeira ponte |
| 21–40 | Vila dos Selos | Calçadas, ladrilhos de praça e canteiros elevados | Florista, morador, placas e varais de envelopes | Fonte postal e portão arborizado |
| 41–60 | Bosque dos Recados | Terra clara, raízes, troncos e riacho | Coruja, vaga-lumes discretos, casinha na árvore | Ponte de troncos e saída para o canal |
| 61–80 | Canal das Encomendas | Água, ilhotas, decks e passarelas | Patos, barqueiro, boias de cartas | Barca postal e ponte móvel |
| 81–100 | Oficina dos Carimbos | Piso turquesa, ladrilhos roxos e plataformas | Máquina de triagem, funcionária, pacotes | Grande carimbo e saída de carga |
| 101–120 | Campo das Pipas | Faixas de pasto, terra, cercas e córrego | Cachorro reaparece, ovelhas e moinho | Pipa gigante e ponte rural |
| 121–140 | Porto das Cartas | Areia, tábuas, mar azul e cais | Marinheiro, gaivotas e barcos de envelopes | Farol postal e trilha costeira |
| 141–160 | Serra do Correio | Pedra clara, penhascos e terraços | Carteiro de montanha, cachorro de resgate | Ponte suspensa e túnel iluminado |
| 161–180 | Jardins do Relógio | Jardins geométricos, canais e mosaicos | Jardineira, pássaros e relógios animados | Aqueduto e relógio floral |
| 181–200 | Capital das Correspondências | Praças, calçadas, canal urbano e mármore cartoon | Elenco recorrente, carro postal e bandeiras | Grande agência e entrega final |

Os títulos e eventos de cada uma das 200 posições estão no apêndice gerado e no JSON de brainstorm. Eles descrevem o mapa; não são 200 tabuleiros prontos.

## 12. Ritmo, dificuldade e história

Proposta inicial de distribuição: 152 posições normais, 39 difíceis e 9 muito difíceis. É uma hipótese de trabalho, sujeita ao jogo real.

- Primeiro capítulo: apresentação mais gentil, três candidatos a desafios, sem fase muito difícil.
- Outros capítulos: quatro difíceis e uma muito difícil por vinte posições.
- Os desafios não devem ficar sempre com o mesmo desenho de tabuleiro. Variar tamanho, objetivos, espaço disponível e combinação de obstáculos já existentes.
- Antes de um desafio: sinal visível, pequeno trecho de ladrilhos, medalhão distinto e personagem indicando a entrega importante.
- Depois: trecho visual mais calmo. Isso não determina automaticamente que a fase seguinte será fácil.
- A última fase do capítulo representa uma entrega especial. Não exige um chefão, um novo modo de jogo ou uma recompensa adicional.
- O cachorro pode reencontrar o jogador em 1, 20, 105, 120, 145 e 200. É um fio visual da viagem.
- A campanha acompanha cartas que saem de um bairro e chegam à grande agência: uma história de entregas, com humor e pequenos reencontros.
- Caixas de correio de formas diferentes contam o lugar: madeira no bosque, boia no canal, metal na oficina, pedra na serra.
- Interações cosméticas propostas: cauda do cachorro, envelope que salta, coruja que pisca, caixa que abre, bandeira que balança.
- Sem bloquear passagem por restauração, compras ou recursos novos neste plano.
- Não inventar metas numéricas, quantidade de movimentos, chances de vitória ou prêmios antes do balanceamento.

O projeto tem 100 arquivos de fases jogáveis. As posições 101–200 são expansão proposta. O editor poderá pré-visualizá-las, mas o jogo só liberará fases com conteúdo real disponível.

## 13. Direção de arte e produção com IA

Manter roxo, dourado, creme e turquesa como assinatura; regiões verdes, azuis e ocres dão contraste.

Regras:
- poucas formas grandes, contorno limpo e três a cinco massas de cor;
- faixas claras/escuras no chão, detalhes localizados, superfícies sem ruído realista;
- sombras separadas, pivô e escala consistentes;
- nenhum número ou texto pintado nos assets de fase;
- gerar o objeto separado do fundo;
- construir chão, ponte e botão com geometria simples e materiais cartoon;
- gerar árvores, personagens, placas, flores e texturas ilustradas com IA;
- validar uma família visual antes de gerar o pacote inteiro;
- reutilizar o mesmo desenho mestre nas variantes de ângulo;
- incluir teste de todos os assets sobre as paletas do mapa, de frente, no meio e no horizonte.

Pacote inicial para o trecho piloto:
- 1 medalhão com variantes normal/difícil e final de trecho;
- 1 família de chão, margem, degrau, água e ponte;
- 2 árvores, 2 arbustos, 2 caixas de correio, 1 poste e 1 pacote;
- 1 casa e 1 agência pequena;
- 1 cachorro com animação simples de cauda;
- 1 personagem existente reaproveitado;
- 1 céu e 1 conjunto distante de silhuetas;
- efeitos discretos de carta, estrela e água.

Para dez regiões, a meta é uma biblioteca reutilizável com objetos únicos de destaque. Não são duzentos fundos ou duzentos kits completos. A contagem final de imagens depende do piloto e da opção escolhida para as casas.

## 14. Ordem de execução

1. **Trecho técnico de 8–10 fases:** câmera real, chão baixo, margens altas, rio, ponte e medalhão com faces reais.
2. **Composição e bug de profundidade:** casas/pinheiros grandes, zona livre dos botões, oclusão e clique em todos os ângulos.
3. **Arte do primeiro capítulo:** céu, cachorro, caixas de correio, faixas de grama e pequenas animações.
4. **Ligação com o jogo:** HUD, pré-nível, replay, retorno de vitória, progresso, última fase disponível e popups.
5. **Ferramenta de autoria:** posicionar trechos e props, marcar dificuldade visual e percorrer o mapa no editor.
6. **Três regiões de prova:** bairro, bosque e canal. Mostram se grama, árvores, água e ponte usam a mesma arquitetura.
7. **Expansão visual até 200 posições:** completar os dez capítulos com cenas compostas.
8. **Conteúdo jogável 101–200:** criar tabuleiros, classificar dificuldade e balancear; em paralelo à arte após o sistema estar estável.

Não recomendar produzir todos os assets ou todas as fases antes de aprovar visualmente o trecho piloto.

## 15. Critérios de aceitação do piloto

- A visão é próxima e mais frontal, com cerca de cinco a sete marcadores legíveis.
- O mesmo medalhão mostra lateral e topo conforme a câmera avança.
- A tipografia permanece clara, sem ser achatada com toda a interface.
- O caminho está visivelmente abaixo das margens.
- A água interrompe o terreno; a ponte tem espessura e conecta os dois lados.
- O horizonte é quase horizontal e oculta as bases dos objetos distantes.
- Sprites se erguem com suavidade e ficam presos ao terreno.
- Nenhum marcador aparece sobre telhados por uma prioridade global de desenho.
- A fase atual e as fases liberadas permanecem acessíveis.
- Scroll lento, rápido e de volta não provoca salto de pose, troca brusca de escala ou erro de clique.
- Telas 9:16 e altas preservam enquadramento e legibilidade.
- A qualidade é medida no aparelho, com foco em fluidez, resolução do mapa e uso de memória.
- O pré-nível e os saves continuam usando o jogo existente.

## 16. Situação desta entrega

Foram criados documentos, dados de brainstorm, sprites e seis malhas simples de protótipo com materiais. Os arquivos estão em `Assets/RoyalAves/Art/MapKitV2`; o inventário e as limitações estão em `ASSETS-MAPKIT-V2.md`. A migração híbrida, o uso desses volumes no mapa, a correção do bug e as fases adicionais ainda são trabalho planejado. O protótipo atual permanece como referência de comparação.

## 17. Acabamento aprovado pelo usuário

A luminária `postal-lamp.png` e a caixa `mailbox-poses.png` são as referências de acabamento aprovadas. O usuário destacou seu volume arredondado, reflexos suaves e aparência luminosa. Os primeiros outros sprites ficaram chapados; usar as revisões de luz e espaçamento marcadas `recommended:true` no manifesto para montar o novo mapa.

O objetivo é luz cartoon pintada: claros largos na face superior esquerda, sombra suave embaixo, contorno creme e pequenos reflexos em metal/tinta. Não acrescentar textura realista, centenas de detalhes ou um halo forte em tudo. Folhagem pode ter brilho suave na borda e volume nas massas de cor, mantendo superfície fosca. A paleta roxo, creme, dourado e teal continua sendo do Correio Mágico.

O atlas `cartoon-terrain-patterns.png` foi rejeitado visualmente pelo usuário. Não aplicá-lo ao terreno, ponte ou água. A direção correta usa cores de vértice/materiais, grandes faixas transversais, pequenas manchas autoradas e bordas iluminadas na geometria. O chão deve ser avaliado na perspectiva final, não como quatro amostras quadradas. Só produzir texturas específicas depois que uma região demonstrar necessidade.

Não depender de bloom global para corrigir arte chapada. Começar com os reflexos pintados. Um halo discreto na luminária pode ser uma camada própria, sem impedir o teste de profundidade dos objetos sólidos. Manter o contraste e a leitura dos níveis.

