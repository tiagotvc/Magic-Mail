# Plano mestre — mundos de Magic Mail e primeira season
Data: 13/10/2026. Estado: **planejamento**, antes de produzir o próximo lote de imagens ou implementar o runtime.
Este documento atualiza o escopo do handoff anterior. A arquitetura curva, a correção de profundidade e o kit aprovado continuam válidos.

## 1. Escopo consolidado

| Conteúdo | Posições visuais | Numeração global proposta | Papel |
|---|---:|---|---|
| Green Land — Terras do Correio | 200 | 1–200 | Primeiro mundo: verde, correio mágico, cidades, bosque, canais e fábricas |
| Lava Land — Terras das Cartas Incandescentes | 250 | 201–450 | Segundo mundo inteiro vulcânico, com variações próprias |
| Terras Pré-históricas | A definir | A partir de 451, se numeração contínua | Terceiro mundo futuro, focado em dinossauros |
| Natal | Não adiciona fases por si só | Mantém IDs | Primeira season, aplicada como decoração dos mundos |

São **450 posições planejadas** nos dois primeiros mundos. Existem 100 tabuleiros na inspeção anterior; o mapa/arte não cria automaticamente os restantes. Não definir quantidade do mundo pré-histórico sem decisão do usuário. A numeração contínua é proposta técnica e deve preservar os IDs e saves existentes.

Mundo, região e season são coisas independentes:
- Mundo define terreno, linguagem, céu e elenco.
- Região define uma sequência com composição e passagens próprias.
- Season acrescenta decoração/variações sobre qualquer região compatível.
Natal não substitui Green Land nem transforma o vulcânico em um quarto mundo.

A campanha pode continuar no mesmo sistema de mapa: a grande entrega Green anuncia uma passagem postal para o Portal de Basalto. Entre 200 e 201, a transição revela pedra escura, silhueta de vulcão e céu mais quente antes de carregar o trecho Lava. Não acrescentar um nível fictício para a passagem. No final do Lava, reservar o marco/portal pré-histórico sem fingir que o terceiro mundo já é jogável.

## 2. Regras de aparência que continuam obrigatórias

Formas grandes e simples, volume ilustrado, contorno claro, reflexos suaves e sombras em poucas massas. A luminária e a caixa de correio do MapKitV2 são o padrão de acabamento. Não voltar a assets chapados demais nem a modelos realistas cheios de detalhes.

Câmera próxima/frontal, curva longitudinal e horizonte lateral quase horizontal. Medalhões, chão e pontes têm volume simples; vegetação e personagens usam arte em cartões. O chão se interrompe nas travessias, as margens têm altura e a câmera revela a lateral/topo ao avançar.

Manter a faixa de níveis legível, sem cartas, fumaça, neve, fontes ou prédios cobrindo números/cliques. A decoração usa profundidade real. O problema do nível distante desenhado sobre uma casa permanece um critério obrigatório de correção.

O atlas de quatro texturas rejeitado continua fora da produção. Faixas claras/escuras e relevo são compostos no mundo, com cores de vértice/materiais e detalhes específicos.

## 3. Green Land: evolução das 200 posições

Manter as dez regiões de 20 posições já escritas em `BRAINSTORM-200-FASES.md`. Elas formam um único mundo verde diverso, não dez mundos separados.

| Intervalo | Região existente | Evolução proposta |
|---|---|---|
| 1–20 | Bairro das Primeiras Cartas | Caixa de correio com gesto, cachorro com cauda, primeiro rio de cartas, ponte baixa |
| 21–40 | Vila dos Selos | Praça de fonte **estática**, ponte arqueada, guirlandas/iluminação no Natal |
| 41–60 | Bosque dos Recados | Passagem de tronco, pinheiros/copa roxa, cartas entre pequenas ilhas e clareiras |
| 61–80 | Canal das Encomendas | Rio de cartas mais largo, ilhotas, píer, barquinho, sequência de pontes |
| 81–100 | Oficina dos Carimbos | Fábrica de cartas, tubo de entregas, carimbador de peças separadas e praça ladrilhada |
| 101–120 | Campo das Pipas | Corrente de vento com cartas, moinho existente, ponte de corda e bandeiras |
| 121–140 | Porto das Cartas | Rio desemboca na baía, píer postal, farol existente e entrega por barco |
| 141–160 | Serra do Correio | Terraços verdes/pedra clara, túnel postal, travessia alta e pequenas quedas de água ilustradas |
| 161–180 | Jardins do Relógio | Canais estreitos, fontes estáticas, mosaicos, aqueduto e relógio postal |
| 181–200 | Capital das Correspondências | Central de triagem, pontes de praça, marcos grandes e grande entrega |

**Fábrica de cartas nova:** poucas massas; prédio creme, telhado roxo, envelope dourado grande, chute/tubo teal, chaminé separada. A fábrica anterior continua uma referência reutilizável. Criar nova peça só para oferecer outra composição, não refazer tudo sem motivo.

Três provas de identidade:
1. Jardim com cachorro levando correspondência.
2. Rio em que envelopes se deslocam sob uma ponte.
3. Fábrica que recebe essas cartas e carimba uma entrega.

A identidade postal deve aparecer na função/composição, além do ícone de envelope colocado em qualquer objeto.

## 4. Rio de cartas e passagens

Entre duas terras, realmente cortar a malha do chão. Mostrar margens, face de terra e água abaixo. Posicionar ponte com espessura e apoios, ou plataformas de travessia.

O rio tem camadas independentes:
1. Canal e margem geométricos.
2. Superfície com duas/três cores largas e linhas claras de fluxo.
3. Cartas/envelopes ou pacotinhos independentes, sobre a superfície.
4. Detalhes laterais: juncos postais, ilhota-selo, píer.
5. Ponte/apoios, acima do fluxo.

Cartas seguem trajetórias do trecho com oscilação leve, diferenças de velocidade e fase. Quando passam sob a ponte, a profundidade deve ocultá-las corretamente. Não animar uma textura cheia de cartas como parte do céu nem colocar todas na mesma altura.

Variar a passagem, não apenas sua cor:
- ponte baixa de tábuas;
- ponte arqueada postal;
- pedras/pequenas ilhas;
- tronco no bosque;
- ponte de corda na serra;
- píer/deck no porto;
- aqueduto/passarela nos jardins;
- portão/túnel entre áreas.

A travessia mantém o mesmo eixo de progressão e o clique de nível apoiado em superfície válida. Se um intervalo exige vazio/ponte, não deixar o marcador flutuando sobre água sem suporte. O tamanho e espaçamento das fases não precisam ser idênticos em todos os trechos.

## 5. Animações — o que se move e o que fica parado

| Elemento | Movimento | Produção |
|---|---|---|
| Nuvens | Deriva lenta e parallax suave; loop fora da tela | PNGs separados, sem gerar dezenas de frames |
| Cartas nos rios | Fluxo + bob suave + pequenas rotações | Sprites/peças independentes e trajeto por trecho |
| Cachorro | Cauda leve e gestos ocasionais | Mesmo corpo mestre; cauda separada; poses existentes |
| Caixa de correio | Abrir/fechar ocasionalmente, bandeira | Estados existentes + encaixes por parte móvel |
| Carimbador | Braço desce/sobe, roda simples | Peças separadas e controlador comum entre mundos |
| Bandeira/pipa | Balanço pequeno | Pivô no suporte, sem deformar o poste |
| Fumaça de chaminé/vulcão | Puffs sobem, ampliam e dissipam | Partículas com máscaras cartoon compartilhadas |
| Gêiser | Boca parada; emissão em ciclos diferentes | Geometria/arte do buraco + partículas do jato |
| Lava | Fluxo lento, faixas e luminosidade leve | Material e poucas partículas de brasa |
| Fontes de praça/jardim | **Sem animação** | Água e jato ilustrados estáticos, sem partículas/pulse |
| Luzinhas de Natal | Variação discreta e lenta opcional | Fio e lâmpadas separados |
| Neve sazonal | Poucos flocos com deriva | Partículas leves; acumulação decorativa estática |

Movimento contínuo de ambiente e gesto de personagem são ritmos diferentes. Não sincronizar todos os objetos. Somente o trecho visível e margem próxima ficam ativos; fechar mapa pausa processamento.

Usar parâmetros e seeds por instância para ciclos reproduzíveis no editor. Estado visual não depende de concluir fase. Scroll de volta apresenta objetos de forma coerente, sem evento de animação que funciona só uma vez.

## 6. Natal: sobreposições reutilizáveis

A opção principal é **asset base + peças sazonais**, com variantes específicas apenas onde uma mudança de silhueta não possa ser montada com adereços.

### Neve

Criar um kit por família de forma, não uma mancha universal:
- telhado inclinado à esquerda/direita;
- telhado plano e telhado largo de fábrica;
- três níveis de pinheiro;
- massa de neve para copa arredondada;
- topo arqueado de caixa de correio;
- tampa da luminária;
- topo de encomenda/placa;
- borda de ponte/margem;
- aro de fonte;
- saliência de basalto frio.

No cartão 2D, a peça compartilha o referencial e a iluminação da base. Na superfície 3D, usar uma pequena malha/camada de material com encaixe real na superfície; uma imagem colada no canvas não resolve a perspectiva do telhado/ponte.

Neve em massas grandes, creme-branca com sombra azul/lilás suave. Sem cristais realistas ou dezenas de gotas. Preservar a cor e a silhueta do objeto.

### Gorros, cachecóis e adornos

- Gorros frente, três quartos e lateral, com pompom grande.
- Gorro/cachecol presos à cabeça/pescoço por pose do cachorro.
- Guirlanda reta, curva e coroa de porta.
- Laços vermelhos, fita em encomendas e selo postal natalino.
- Luzinhas com fio separado.
- Boneco de neve carteiro e trenó postal como poucos objetos exclusivos de Natal.

Nem tudo precisa receber todos os adereços. Em cada composição, distribuir neve e dois/três sinais natalinos fortes, evitando uma cena inteira de gorros.

Não redesenhar o cachorro, cada casa e cada árvore inteiros só para colocar um gorro/neve. Preservar os originais e calibrar encaixes. Se for necessário gerar uma variante inteira, registrar qual parte não podia ser reaproveitada e conservar proporção/pivô.

## 7. Contrato de encaixe para os assets atuais

Cada prefab/base deve ter um perfil de encaixe autorado. Campos propostos:
`baseAssetId`, `viewOrPoseId`, `socketId`, `localPosition`, `localRotation`, `localScale`, `surfaceType`, `allowedOverlays`, `visibilityMask`.

| Base atual | Encaixes natalinos | Cuidado |
|---|---|---|
| Luminária aprovada | capSnow, poleBow, lightWire | Não tapar luz/envelope pendurado |
| Caixa fechada/aberta | topSnow, flagRibbon, postBow | Neve de topo acompanha casco; tampa móvel recebe encaixe próprio |
| Cachorro com quatro poses | headHat, neckScarf | Ajuste por pose; cauda animada não desloca o gorro |
| Pinheiro | upperSnow, middleSnow, lowerSnow | Mesmo pivô do tronco, sem escalar a neve sobre toda a copa |
| Árvore redonda roxa | crownSnow por lobo | Distribuir por grupos grandes |
| Agência e fábrica | roofSnow, wreath, garland | Curva/volume do telhado continuam visíveis |
| Encomendas, placa e arbusto | topSnow, bow | Não cobrir o envelope principal/contorno inteiro |
| Barco, moinho, farol, casa, relógio, grande agência, túnel e pipa | snow/wreath/flag por parte | Neve só em superfícies coerentes, não sobre o vazio da pipa |
| Ponte, ilhas e medalhão 3D | edgeSnow/margem e pequena decoração de aro | Não esconder número, estrela, estado nem collider |

A sobreposição herda a transformação de curva, o movimento de levantar e a profundidade da base. Resolver z-fighting com afastamento local mínimo, não com prioridade global de UI que atravesse tudo. Sobreposição sem interação não recebe clique.

O sistema remove somente os adereços que criou ao trocar season. Nada de destruir bases e reconstruir save/rotas.

## 8. Integração com temporadas existentes

Inspeção do projeto:
- `SeasonConfig.cs` guarda seasons e resolve o loading.
- `SeasonProps.cs` coloca adereços em coordenadas de **tela** e anima Float/Pulse/Sway/Spin.
- `SeasonScreen` contém Loading=0, Title=1, Lobby=2, Game=3.
- `RoyalAvesSeasons.asset` inspecionado está com `activeSeasonId: padrao`; Natal é a primeira season pedida, ainda a configurar.

Não copiar adereços de tela do loading para o mapa como se fossem posições de mundo. Reutilizar a seleção/ID da season, e criar um adaptador de decoração de mundo com sockets e profundidade.

Proposta: `MapSeasonDecorator` + `MapSeasonThemeDefinition`, vinculados ao ID da season existente. Season desconhecida/sem perfil retorna ao mundo base sem erro. Se acrescentar valor Map ao enum de telas, fazê-lo ao final sem renumerar os valores salvos; o mapa continua precisando do seu hospedeiro de mundo, não apenas do enum.

Preview precisa combinar mundo, região, cursor e season, incluindo ligar/desligar Natal na mesma cena. Preservar configurações/arte existentes. Não alterar a season ativa durante esta fase de planejamento.

“Primeira season” define prioridade de conteúdo, não um calendário automático. Não ativar/desativar Natal por data do sistema nem mudar progresso. A seleção continua pelo mecanismo de season configurado; qualquer agenda futura precisa de especificação própria.

## 9. Lava Land: 250 fases inteiramente vulcânicas

Dividir em dez regiões de **25 fases**, com cinco momentos de cinco fases em cada região. Todas permanecem no tema vulcânico. As diferenças vêm de terreno, profundidade, passagem, função postal e elenco, não de sair para outro bioma.

| Global proposta | Região | Identidade |
|---|---|---|
| 201–225 | Portal de Basalto | Primeiras fendas, portão postal, pedras frias e vulcão distante |
| 226–250 | Rio das Brasas | Lava larga, ilhas, pontes, barco postal de metal mágico |
| 251–275 | Campos dos Gêiseres | Bocas, bacias minerais, vapor e passarelas |
| 276–300 | Forja dos Selos | Fábrica, carimbador, tubos, chaminés e ladrilhos escuros |
| 301–325 | Vale dos Cristais Incandescentes | Rochedos violetas, cristais teal, fissuras laranja |
| 326–350 | Jardins de Cinza | Flora térmica cartoon, cinza lilás, estufa postal |
| 351–375 | Crateras das Encomendas | Plataformas circulares, lagos de lava e balão postal |
| 376–400 | Correio Subterrâneo | Caverna aberta, lanternas-selo, travessias sob rocha |
| 401–425 | Cordilheira dos Vulcões | Terraços, observatório e vários vulcões no horizonte |
| 426–450 | Grande Caldeira Postal | Anel de basalto, rios radiais e central da grande entrega |

A descrição dos **50 momentos** e suas faixas está em `BRAINSTORM-LAVA-250-FASES.md` e no roadmap JSON.

### Paleta e céu

Céus pêssego/laranja, depois coral/âmbar e noite de brasa. Solo em basalto azul/lilás, ocre mineral e cinza suave. Lava laranja-amarela com contorno mais escuro. Teal/roxo/creme e ouro dos objetos postais preservam identidade e contraste.

Não tornar céu, chão, prédios e botões todos vermelhos. Três bases de céu são ponto de partida, com camadas intermediárias por região. Vulcões são recortes separados; fumaça é independente. Nunca gerar fundo com mapa inteiro, fases e fumaça fixa.

### Terreno e vazios

Fendas estreitas, rios largos, lagos de cratera, placas resfriadas, saliências, ilhas e terraços oferecem topologias diferentes. Pontes de basalto, metal térmico e corda têm apoios e faces. Os gêiseres ficam em bacias fora da faixa das fases.

Na caverna, compor corte aberto com teto lateral e fundo visível; não cobrir metade da tela com rocha nem perder o horizonte/legibilidade. O mundo inteiro usa a mesma câmera e curva, com composição própria.

### Identidade Magic Mail

Caixa de basalto com bandeira, luminária-selo de brasa, forja de carimbos, tubo de envelopes, barquinho de encomendas, observatório postal e central da caldeira. Cartas encantadas com borda dourada atravessam o fluxo de lava; elas são diferentes das cartas do rio Green.

O mesmo cachorro pode receber óculos e bolsa de entrega térmica, separados. Não trocar a identidade do mascote. Fumaça/gêiseres são atmosfera; não inventar dano, combates ou novas regras de match-3 só por haver lava.

## 10. Natal no vulcânico

Aplicar guirlandas, laços, gorros, envelopes-presente e luzinhas sobre as bases vulcânicas. Neve em prédios, saliências frias, arbustos de cinza e borda externa de bacias. A lava continua líquida/luminosa; não recebe uma tampa branca.

Gêiser mantém boca livre e jato visível; neve fica no lado externo frio da bacia. Forja recebe neve no trecho frio do telhado, guirlanda e coroa; chaminé continua emitindo fumaça.

Estas restrições são de composição escolhida, não uma exigência de realismo físico. A combinação Natal + lava deve ser divertida e claramente Magic Mail. O kit comum cobre boa parte; adaptadores de neve em basalto/forja completam as formas específicas.

## 11. Terceiro mundo: direção pré-histórica

Reservar identidade própria, com verde mais tropical, pedra creme, rios teal, grandes folhas e fósseis simples. Diferenciar do Green pelo porte e silhueta de plantas/animais, não por espalhar um ovo em cada região antiga.

Regiões candidatas, ainda sem contagem de fases:
- Vale dos Ovos;
- Floresta dos Gigantes;
- Pântano das Pegadas;
- Cânion dos Fósseis;
- Lago dos Plesiossauros;
- Ninhos nas Alturas;
- Planalto dos Herbívoros;
- Central Postal Jurássica.

Dinossauros simpáticos e estilizados: tricerátops pequeno para contato próximo, diplodoco como marco distante, pterossauros em silhueta e criatura aquática no lago. Correio com caixa-fóssil, cabana de cartas, marcas de carimbo em pegadas, arco de osso/rocha e encomendas em ninhos.

Água, pontes, céu, partículas e sockets sazonais reutilizam o sistema. Dinossauros/folhagem e assinaturas do terreno são novos. Gorros/folhas com neve terão perfis específicos quando as bases forem definidas.

Produção detalhada e elenco final ficam depois dos pilotos Green/Lava. Não gerar agora centenas de poses de dinossauros antes de estabilizar encaixes e animação.

## 12. Produção de assets e reutilização

Backlog atualizado: **50 famílias e 159 variantes/receitas propostas**, incluindo ilustrações, peças, máscaras, malhas/materiais, clima/background e reserva pré-histórica. Não são 159 PNGs prontos, nem uma ordem para gerar tudo de uma vez. Catálogo detalhado: `PLANO-PRODUCAO-ASSETS-MUNDOS.md`; fonte estruturada: `asset-production-backlog.json`.

Prioridade:
1. Nuvens, rio de cartas e passagens Green.
2. Fábrica/carimbador, fontes estáticas e cauda do cachorro.
3. Natal sobre as bases atuais e uma travessia; validar normal/Natal lado a lado.
4. Piloto Lava com céu laranja, vulcão separado, fumaça, margem/fenda, ponte, gêiser e dois props postais.
5. Natal Lava, incluindo adaptações para superfícies frias.
6. Expandir marcos das dez regiões vulcânicas e composição das 250 posições.
7. Conceitos pré-históricos depois da linguagem comum funcionar.

Reutilizar controladores de animação, shaders/cartões, superfícies, conectores de passagem, máscaras de partículas e seleção de season. Dar novas silhuetas/texturas às famílias que caracterizam cada mundo.

## 13. Critérios de qualidade do próximo lote

- Comparação direta com luminária/caixa aprovadas: acabamento luminoso simples, sem detalhe excessivo.
- Cada asset tem fundo transparente, pivô/escala legíveis e recorte com folga.
- Preferir PNG individual por peça; folha só quando regularidade/corpo comum for validada. O lote anterior precisou de correção de espaçamento.
- Neve/gorro encaixam sem flutuar, cortar base, tampar números ou atravessar objetos.
- Mesmo objeto normal/Natal conserva escala, pose, curva e sombra.
- Ponte oculta cartas abaixo; carta não desenha sobre margem de frente.
- Gêiser/fumaça/neve não cobrem as fases legíveis.
- Fontes permanecem estáticas.
- Um trecho vulcânico é reconhecível sem depender só da cor do céu.
- Testar transição entre regiões, scroll de ida/volta e dois aspectos de tela.
- Background, horizonte e clima mudam por área, com transições graduais e reversíveis; números/cliques permanecem livres de chuva, vento e tempestade.
- Desempenho medido no dispositivo; nenhuma promessa baseada apenas no número de sprites.
- Marcar em inventário o que foi gerado/importado/integrado/validado; não apresentar roadmap como implementação pronta.

## 14. Situação desta atualização

Foram preparados plano mestre, roteiro de 250 posições vulcânicas agrupadas em 50 momentos e backlog de arte/animação/Natal. **Nesta atualização não foram geradas novas imagens, modificados saves, ativado Natal ou implementadas animações.** O usuário pediu planejar primeiro. O kit MapKitV2 permanece a base real existente; o próximo lote segue este plano.

## 15. Background e clima específicos de cada área

O pedido mais recente exige mudança do background ao avançar: céu/paleta, silhuetas, nuvens e luz mudam com a região, além do chão. O plano detalhado está em `PLANO-CLIMA-E-BACKGROUNDS.md`; o resolver/editor usará os perfis de `environment-weather-roadmap.json`.

Foram propostos vinte perfis de área (dez Green, dez Lava), dezoito presets de clima e variações internas. Sol/raios de sol, nuvens, chuva, ventania, tempestade, névoa, cinza e brasa são camadas separadas. Fontes continuam estáticas. Natal compõe decoração e precipitação compatível sem forçar neve em todo mundo. O clima é autorado no jogo, sem previsão real ou calendário automático.

Mistura espacial suave entre dois perfis acompanha o cursor real da rota e funciona no scroll de volta. O horizonte permanece estável. Fumaça/gêiseres locais não somem ao trocar clima, e o modo de efeitos leves reduz partículas/desliga clarões. A produção futura começa provando duas áreas com fundos distintos e blend; depois chuva/vento/tempestade e as variantes Natal/Lava.

