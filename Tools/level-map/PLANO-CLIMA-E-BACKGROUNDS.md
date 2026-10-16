# Céu, background e clima por área
Data: 13/10/2026. Estado: planejamento; imagens e runtime ainda não produzidos.
Pedido do usuário: o background muda conforme a área, com clima animado — chuva, sol/raios de sol, ventania, tempestade e outras variações.

Este documento complementa `PLANO-MUNDOS-SEASONS.md` e passa a ser uma exigência do handoff. Fonte estruturada: `environment-weather-roadmap.json`.

## 1. O background é parte da região

Não usar um único céu por mundo nem só trocar o chão. Cada região escolhe paleta do céu, silhuetas distantes, famílias de nuvens, luz aparente, vento e clima. Alguns trechos internos anunciam chuva, abertura do céu ou mudança de intensidade.

Mundo define identidade de base; região/trecho define ambiente atual; season acrescenta decoração e variantes compatíveis. Não consultar previsão do tempo real nem relógio do sistema: é direção visual autorada do jogo. A passagem do tempo anima nuvens/partículas e ciclos explicitamente escolhidos.

O céu deve mostrar pistas do lugar seguinte antes de chegar nele. Caminhando de volta, o fundo e o clima voltam a combinar com a área anterior. Teleporte/foco em outro nível resolve o ambiente de destino, sem reproduzir todos os climas intermediários.

## 2. Camadas independentes

| Camada | Conteúdo | Comportamento |
|---|---|---|
| Base de céu | Gradiente/ilustração simples de cor | Mistura entre perfis da área |
| Sol/lua quando compatível | Disco cartoon e brilho suave | Posição autorada; movimento discreto |
| Horizonte distante | Casas, mata, montanhas, vulcões ou fundo de caverna | Troca por área com parallax mínimo |
| Nuvens distantes | Massas pequenas/largas | Deriva lenta, loop fora do campo visível |
| Nuvens próximas | Recortes maiores, claros ou carregados | Movimento um pouco mais perceptível |
| Fachos/bruma | Raios de sol filtrados, névoa lateral | Intensidade gradual, máscaras próprias |
| Clima do mundo | Chuva, folhas, cartas ao vento, neve ou cinza | Emissores por trecho visível |
| Efeitos locais | Fumaça de fábrica, gêiser, brasa de lava | Continuam presos à origem e independentes do clima geral |

As bases de céu não contêm chão, marcadores, todos os prédios ou efeitos animados já pintados. Sol/raios não ficam fixos dentro de todas as pinturas. O background de caverna é específico, não uma foto de céu escurecida.

As camadas são apresentadas dentro do mundo do mapa/RenderTexture e atrás do HUD. O horizonte e a projeção curva permanecem estáveis. Nuvens não podem se deslocar na mesma velocidade do chão, como se fossem coladas à estrada.

## 3. Perfis propostos para as 20 regiões

As faixas são globais propostas, preservando as 200 posições Green e 250 Lava.

| Posições | Região | Céu / fundo distante | Clima principal |
|---|---|---|---|
| 1–20 | Bairro das Primeiras Cartas | azul-dourado; casas postais e copas arredondadas | sun-rays |
| 21–40 | Vila dos Selos | azul-menta; telhados da vila e torres com selos | partly-cloudy |
| 41–60 | Bosque dos Recados | verde-azul-suave; camadas de pinheiros e copas | forest-mist |
| 61–80 | Canal das Encomendas | azul-nublado; docas, pontes e silhuetas da próxima margem | rain-soft |
| 81–100 | Oficina dos Carimbos | pessego-industrial-cartoon; chaminés, tubos e fábrica postal | partly-cloudy |
| 101–120 | Campo das Pipas | azul-aberto; colinas, moinho e pipas | windy |
| 121–140 | Porto das Cartas | coral-costeiro; farol, barcos e linha de costa | coastal-wind |
| 141–160 | Serra do Correio | azul-cinza-altitude; terraços, túnel e montanhas suaves | storm-soft |
| 161–180 | Jardins do Relógio | lilas-azul-jardim; relógio, aqueduto e topiárias | sun-after-rain |
| 181–200 | Capital das Correspondências | rosa-dourado-capital; grande agência, telhados e torres | golden-hour |
| 201–225 | Portal de Basalto | laranja-pessego; primeiro vulcão e pedras de basalto | volcanic-clear |
| 226–250 | Rio das Brasas | laranja-coral-rio; ilhas, barco metálico e vulcões baixos | ember-drift |
| 251–275 | Campos dos Gêiseres | dourado-termal; bacias minerais e estação termal | thermal-mist |
| 276–300 | Forja dos Selos | laranja-ambar-forja; fábrica de selos e chaminés grossas | ash-breeze |
| 301–325 | Vale dos Cristais Incandescentes | ambar-lilas-cristais; cristais altos e torre do selo | volcanic-clear |
| 326–350 | Jardins de Cinza | lilas-acinzentado; árvores térmicas e estufa postal | ashfall-soft |
| 351–375 | Crateras das Encomendas | coral-crateras; bordas de crateras e balão postal | volcanic-wind |
| 376–400 | Correio Subterrâneo | caverna-ambar; parede distante, cristais e aberturas de rocha | cave-haze |
| 401–425 | Cordilheira dos Vulcões | coral-cinza-cordilheira; três vulcões e observatório postal | volcanic-storm |
| 426–450 | Grande Caldeira Postal | noite-brasa; grande caldeira e central postal | ember-night |

Variações internas propostas:
- **Bairro das Primeiras Cartas:** Sol com raios largos discretos; nuvens pequenas deslizam ao fundo. Uma passagem de nuvens reduz os raios e depois abre o céu.
- **Vila dos Selos:** Nuvens mais largas alternam luz e sombra visual no fundo. Garoinha curta em um trecho da praça; fonte permanece estática.
- **Bosque dos Recados:** Névoa baixa lateral e raios entre copas; folhas esparsas. Chuva suave ao atravessar uma clareira.
- **Canal das Encomendas:** Chuva sobre o canal; pequenas ondulações longe dos marcadores. A chuva diminui antes de um trecho claro sobre as ilhas.
- **Oficina dos Carimbos:** Céu próprio de oficina; fumaça fabril separada das nuvens. Sol abre sobre o pátio de carimbos; nenhum efeito move a fonte.
- **Campo das Pipas:** Correntes de vento visíveis em poucos arcos; bandeiras, folhas e cartas acompanham. Rajada cresce e se acalma sem empurrar câmera ou botões.
- **Porto das Cartas:** Sol baixo e vento do porto; nuvens horizontais lentas. Trecho de tempestade curta no píer, seguido de abertura do céu.
- **Serra do Correio:** Nuvens escuras em altitude, chuva oblíqua e clarão localizado nas nuvens. Névoa na saída do túnel, com retorno de luz entre as montanhas.
- **Jardins do Relógio:** Céu limpo após chuva; raios suaves e arco-íris opcional distante. Orvalho/bruma lateral; água das fontes segue ilustrada e parada.
- **Capital das Correspondências:** Luz dourada de fim de tarde e nuvens rosadas, composição de grande entrega. Gradiente avança para crepúsculo na conclusão, mantendo níveis claros.
- **Portal de Basalto:** Céu quente, raios filtrados e fumaça separada sobre o vulcão. Faixa de nuvens de cinza anuncia o rio seguinte.
- **Rio das Brasas:** Brasas esparsas sobre canais de lava; nuvens longas independentes. Rajada muda a direção de fumaça e brasas, sem cobrir a ponte.
- **Campos dos Gêiseres:** Vapor dos gêiseres em ciclos separados, com névoa baixa nas laterais. Garoinha curta nas plataformas frias, vapor mais visível nas bacias.
- **Forja dos Selos:** Fumaça de chaminé e cinza suave cruzam o céu em camadas distintas. Céu abre sobre o pátio; forja conserva seu próprio movimento.
- **Vale dos Cristais Incandescentes:** Contraste frio/quente; brilho de cristal e nuvens violetas lentas. Névoa baixa em uma fenda, sem esconder os cristais principais.
- **Jardins de Cinza:** Cinza em poucos flocos lentos, sombras frias e espaço livre central. Chuva mineral leve em um jardim frio; lava continua reconhecível.
- **Crateras das Encomendas:** Rajadas nas bordas altas, fumaça inclinada e balão ancorado com oscilação pequena. Nuvens mais densas preparam entrada de caverna.
- **Correio Subterrâneo:** Fundo de caverna próprio, poeira/bruma lenta e luz por fendas. Sem chuva/sol celeste dentro do teto; fachos suaves por abertura autorada.
- **Cordilheira dos Vulcões:** Tempestade vulcânica com nuvens densas, vento e clarões internos raros. Chuva em terraços frios; fumos e lava persistem nas áreas quentes.
- **Grande Caldeira Postal:** Fundo noturno âmbar/roxo, brilho de lava e brasas esparsas. Abertura das nuvens revela um crepúsculo postal na praça final.

O preset não obriga que todas as posições de uma região tenham exatamente o mesmo clima. Autorar um começo/meio/fim ou um evento visual curto. Evitar trocar céu e efeito completos a cada nível; a leitura de região precisa durar.

## 4. Sol e raios de sol

Sol cartoon simples, nuvens separadas e dois/três fachos largos de borda suave. Os raios variam lentamente quando uma nuvem atravessa a direção aparente da luz. Começar com camadas/materiais leves e máscaras, sem exigir iluminação volumétrica real.

Conservar a luz pintada dos assets aprovados. Temperatura/contraste do cenário podem mudar discretamente; não inverter a iluminação de cada sprite nem escurecer número e HUD junto com o fundo.

No bosque, raios filtrados pelas copas; no porto, sol mais baixo; na capital, luz dourada. Na caverna fechada/noite, não desenhar sol celeste ou fachos vindos de um teto inteiro. Luz por abertura é um efeito local autorado.

## 5. Chuva e garoa

Gotas simples, velocidade/ângulo orientados pelo vento e intensidade ajustável. Garoa tem menor densidade/contraste que chuva de tempestade.

Pequenos impactos/ondulações aparecem em superfícies de água/rios especificadas. Não usar gotas de tela inteira caindo por cima do HUD. Em chuva forte, reduzir densidade na faixa central dos níveis e no primeiro plano das letras.

Volumes e máscaras autoradas evitam emissão dentro de caverna fechada, sob teto grande ou em áreas incompatíveis. Não fazer um raycast físico por gota para obter uma aparência que pode ser resolvida pelo trecho.

**Fontes continuam estáticas.** Não ligar jato/ondulação animada de fonte ao sistema de clima. Ondulações da chuva são efeitos do canal/água do trecho, com exclusão das fontes ilustradas.

## 6. Ventania e rajadas

Um vetor de vento comum orienta nuvens próximas, inclinação da chuva, fumaça, folhas, bandeiras, cauda/tecido compatível e cartas ao vento. Intensidade sobe/desce com curvas suaves e seeds por trecho.

Arcos de vento são poucos e de baixa opacidade, em regiões escolhidas. Folhas/cartas seguem trajetórias curtas e saem do campo visível. Não encher a estrada de risquinhos brancos.

Vegetação/tecido podem balançar mais durante uma rajada, dentro da amplitude permitida do prop. Prédios, chão, medalhões e câmera permanecem estáveis. O fluxo de cartas no rio mantém sua rota; vento acrescenta gesto suave, não remove cartas da superfície sem propósito autorado.

O Campo das Pipas demonstra o efeito com moinho/pipa/bandeiras; cordilheiras demonstram rajadas com fumaça inclinada e nuvens densas. Não reutilizar a mesma rajada exatamente sincronizada em todos os elementos.

## 7. Tempestade

Combinar fundo próprio, nuvens carregadas, vento, chuva oblíqua e raros clarões **dentro do background**. Raios têm desenho simples e aparecem atrás do mundo, sem cobrir números com um flash branco de tela inteira.

O modo de efeitos leves mantém céu carregado/chuva reduzida e desliga o clarão. Animações não sacodem a câmera nem alteram clique/progresso. Não disparar raio toda vez que o jogador atravessa a mesma coordenada; usar ciclo/seed estável e intervalo entre pulsos.

No vulcânico, combinar cinza/fumaça ambiente e tempestade de altitude com cuidado. A chuva ocupa terraços frios quando esse trecho a prevê. Gêiseres e lava conservam sua função visual; tempestade não substitui o mundo por chuva azul comum.

## 8. Outros ambientes possíveis

- Névoa baixa no bosque, túnel/serra, pântano futuro e caverna.
- Sol depois da chuva, com arco-íris opcional distante; produzir só após o efeito principal funcionar.
- Fim de tarde e crepúsculo com nuvens rosadas.
- Neve leve e céu frio na variante natalina compatível.
- Cinza suave e brasas esparsas nas áreas vulcânicas.
- Poeira de vento no cânion pré-histórico futuro.
- Vapor de plataforma termal como efeito local, separado da névoa de clima.
- Noite de brasa no final do Lava, com contraste do mapa preservado.

No pré-histórico, as oito regiões candidatas recebem direção de clima no JSON, ainda sem faixas de fase. Não definir quantidade de níveis só para preencher uma tabela de clima.

## 9. Natal junto com clima

Natal conserva os adereços separados do plano: neve acumulada, gorros, guirlandas e luzinhas. Acumulação decorativa e neve caindo são sistemas diferentes.

A season pode selecionar uma variante fria de céu/precipitação por região. Não converter todo mundo automaticamente em nevasca. Pode existir Natal ensolarado no bairro, neve leve no bosque, vento no porto e decoração natalina com brasa nas áreas de lava.

Regra padrão: um tipo principal de precipitação por trecho. Se chuva vira neve, reduzir um emissor enquanto aumenta o outro; não manter chuva + cinza + neve máximas simultaneamente. Mistura como granizo/neve molhada precisa ser um preset explícito, não um acidente de transição.

No Lava, neve decorativa/queda compatível usa zonas frias, prédios e saliências; lava/boca ativa de gêiser continuam livres. Fumaça do vulcão não desaparece só porque Natal está ativo.

## 10. Modelo de dados e resolver de ambiente

Nomes propostos, ainda não implementados:
- `MapEnvironmentProfile`: paleta/base do céu, camadas de horizonte, nuvens, luz aparente e referência de clima.
- `MapWeatherProfile`: tipo, intensidade, vento, famílias de partículas, ciclo/seed, exclusões e qualidade.
- `MapAreaEnvironmentBinding`: mundo/região/trecho → perfil, variações locais e faixa de transição.
- `MapEnvironmentResolver`: resolve os perfis conforme cursor/posição na rota e season.
- `MapWeatherController`: apresenta e pausa camadas/emissores visíveis.
- `SeasonEnvironmentOverride`: variante natalina compatible e fallback de base.

Prioridade: mundo base → região → override de trecho → variante compatível da season. Intensidade/vento locais refinam o resultado. Season não substitui indiscriminadamente toda a topologia ou o perfil da caverna.

O cursor deve fornecer distância ao longo da rota autorada. A faixa por número de fase no JSON é localização editorial; no runtime, limites e blends usam essa distância, pois trechos/ponte podem ter espaçamento diferente.

Preview/editor precisa de controles: mundo, região, cursor, season, perfil de clima, intensidade, vento, tempo/seed e qualidade. Overrides de preview não escrevem no save nem tornam o clima aleatório do jogador.

## 11. Transições sem saltos

Misturar no máximo dois perfis de área de cada vez. Primeiro experimento: a pista do próximo ambiente começa nas duas/três últimas posições legíveis do trecho; ajustar distância real no piloto.

Cores e opacidade dos fundos/horizontes entram progressivamente. Nuvens preservam fase de deslocamento para não nascer todas na mesma posição. Partículas do clima anterior param de emitir e dissipam; o novo clima sobe de intensidade. Não criar um terceiro emissor a cada mudança de cursor.

Ao voltar o scroll, o blend volta conforme a rota. A posição/seed do clima é estável; o tempo pode continuar a animação ambiente. Não resetar jato/fumaça em cada atualização de peso. Ao saltar diretamente para outra região, resolver só origem/destino ou trocar diretamente para destino no foco editorial, sem acionar eventos de todas as regiões puladas.

Transição de noite para sol é autorada. Não misturar ao mesmo tempo sol intenso, céu noturno e tempestade máxima por uma soma ingênua de camadas. Na fronteira Green/Lava, mudar fundo distante/paleta junto com as pistas de terreno antes de concluir a troca de trecho.

## 12. Produção do próximo lote

Foram acrescentadas sete famílias ao backlog, totalizando **50 famílias e 159 variantes/receitas propostas**. São planejamento, não PNGs gerados.

| Família | Produção |
|---|---|
| green-area-sky-bases | Quatro bases de céu Green: claro, pôr do sol, carregado, jardim lilás |
| weather-sun-rays | Disco solar e fachos suaves separados |
| weather-storm-clouds | Duas formas de nuvem carregada, com variantes de paleta |
| weather-rain-kit | Máscara de gota, impacto e ondulação de rio |
| weather-wind-kit | Arcos de rajada e folha; cartas já existentes são reutilizadas |
| weather-lightning-kit | Desenho procedural/máscara de raio e luz interna de nuvem |
| weather-mist-kit | Massas simples de névoa baixa e faixa lateral |

Reutilizar as três bases de céu Lava já planejadas e suas variações de paleta; o novo binding exige composições específicas por região. Não produzir vinte pinturas completas de mundo. Produzir placas de fundo e recortes/camadas que o sistema combina.

## 13. Ordem de implementação/validação futura

1. Duas áreas Green com fundos/silhuetas diferentes e transição ao avançar/voltar.
2. Nuvens e sol/raios; provar que não estão colados ao chão.
3. Garoa/chuva no canal, vento no campo e tempestade curta na serra/porto.
4. Composição normal/Natal: céu, adereços e precipitação compatível.
5. Duas áreas Lava: céu quente + fumaça/vapor, depois noite de brasa/vento.
6. Aplicar os vinte perfis e variações locais, expandindo só após leitura e blend funcionarem.

Critérios: fundo coerente em toda área; diferença percebida também no céu/horizonte; efeitos animados reversíveis e sem acúmulo; nuvens/fumaça separadas; números/clicks livres; fontes estáticas; telas altas e 9:16; emissores fora da vista desligados; perfil de efeitos leves funcionando; custo medido no dispositivo.
Fechar mapa interrompe processamento e os recursos são liberados corretamente. Configuração/clima não alteram dificuldade, economia, fase liberada ou loader.

