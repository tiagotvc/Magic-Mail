# Plano de refinamento visual — Magic Mail

Data: 19/10/2026. Status: análise e proposta; sem alterações no jogo ou geração de novos mocks nesta etapa.

## Objetivo e escopo

Aproximar a clareza, o frescor das cores e a simplicidade de volumes das referências de Candy Crush fornecidas pelo usuário, mantendo identidade própria de correio mágico. Priorizar terreno, percurso e botões de fase. Para HUD superior e inferior, produzir alternativas antes de substituir qualquer arte. A modal atual entra como contexto de identidade; sua reformulação completa não faz parte desta primeira rodada.

A referência visual é a captura fornecida, com limitações de escala, compressão e marcações. Cores exatas devem ser verificadas em fontes melhores se disponíveis; as observações abaixo descrevem relações de cor, forma e hierarquia, não uma amostragem colorimétrica definitiva.

## Diagnóstico

1. **Terreno:** o gramado atual tende ao verde acinzentado/oliva e a trilha ao bege amarronzado, enquanto os objetos têm saturação e reflexos muito fortes. A sensação de cenário envelhecido vem dessa diferença e da perda de cor nas áreas escuras. Na referência, verde claro, verde médio e sombra conservam identidades cromáticas; a areia tem faixas de amarelo e ocre vivo.
2. **Percurso e enquadramento:** o nosso mapa parece uma faixa larga relativamente reta; o fio central faz mais movimento do que as bordas da estrada. Os discos projetados comprimem rapidamente no fundo. É necessário comparar desenho lateral, largura e câmera separadamente. A referência apresenta uma composição mais ilustrada, com objetos laterais recortados pela tela e contornos de terreno suaves.
3. **Materiais:** cenário suave, casas muito glossy, HUD metálica, medalhões com vários anéis e retratos com molduras competem entre si. A identidade precisa de uma regra de materiais: terreno com cor limpa e acabamento suave; objetos e ícones com esmalte glossy; superfícies extensas da HUD acetinadas; metal como acento localizado.
4. **HUD inferior:** faltam a unidade de cor e a hierarquia da referência. Ela usa uma família de rosa, pequenas separações claras/escuras, sombra curta e volumes locais discretos. Os ícones se destacam por silhueta e borda azulada; o material da barra fica quieto. O relevo superior das células inativas deve ser estudado no mock, com uma elevação rasa, sem inventar grandes abas triangulares.
5. **HUD superior:** o nosso formato flutuante e os sucessivos filetes dourados/cristalinos criam aparência de joalheria. A referência funciona como uma faixa que preenche a região superior, com textura suave, um acabamento arredondado na borda inferior, ícones claros e cápsulas de valor simples. O texto parece integrado à superfície por cor escura da mesma família e pequeno relevo, mantendo contraste.
6. **Porquinho:** o rosto domina e esconde a função de cofre. Cabeça e barriga precisam formar uma massa compacta; a fenda superior tem que aparecer imediatamente. O vermelho atual e os elementos pontudos fazem o ícone parecer mais um personagem de ação. O suporte atrás pode ser um escudo postal de duas camadas largas, com o porco ultrapassando sua borda superior.
7. **Fases:** os discos parecem fichas achatadas. Há pouco espaço de face para o número e informação demais na periferia. O volume precisa vir de uma forma abaulada clara, uma lateral curta e uma sombra pequena. Na referência, números, estrelas e coroas ajudam a leitura dos estados. Aparência, orientação e progressão de escala devem ser avaliadas juntas.

## Evidências do projeto

- `RoyalAvesWorldMapIslands.asset`: `vibrance: 4` e `pathSway: 1`.
- `MAP-PALETTE-V94.md`: direção anterior explicitamente suavizou a paleta para sage/olive e reduziu vibrance de 10 para 4. A nova solicitação revisa essa direção; não significa simplesmente restaurar todos os valores antigos.
- `MapSurface.shader`: combina graduação inicial, faixas, manchas e sombras multiplicativas. A trilha recebe uma mistura de tons mais escuros nas bordas. Alterar somente o valor global de vibrance não controla essas relações.
- `WorldMapRoute.CenterX`: há controle independente da amplitude lateral, além da geometria de curvatura e câmera. A primeira missão utiliza uma onda própria, portanto testar só um trecho não basta.
- `WorldMapTokenMesh`: existem camadas de cristal, ouro, canal roxo e lábio dourado em torno da face; o shader também adiciona reflexos. A orientação do token é calculada separadamente em `WorldMapView`.
- `WorldMapTopHud`: a faixa tem altura 188 e deslocamento superior 32 na referência de layout 1080. A proposta de faixa contínua exige estudar a ocupação do topo e as áreas seguras, além de trocar a imagem.
- `WorldMapLetterVault`: o porquinho e o suporte atuais são consumidos como um único sprite. Para mock e futura troca sazonal, avaliar porco, chapéu e escudo separados.

## Direção proposta

### Terreno

Desenhar cores próprias para luz, meio-tom e sombra, em vez de obter todas as regiões multiplicando a mesma cor. Manter a sombra verde com cor e reservar amarelo/ocre para a trilha. Preservar faixas grandes e limpas; reduzir manchas ou escurecimentos que pareçam sujeira. Evitar neon, aumento global de saturação ou brilho branco no chão.

Comparar três estudos: A) verde fresco próximo da referência; B) verde de prado com areia quente; C) verde levemente mágico com amarelo manteiga. Em cada estudo, manter câmera, objetos, posição e exposição iguais. Depois comparar o melhor com os objetos atuais para conferir se continuam pertencendo à mesma cena.

### Percurso e visão

Comparar três desenhos: atual, S moderado e S mais expressivo. Estudar amplitude e comprimento da curva; aumentar amplitude sozinho pode manter curvas longas demais. Fazer o contorno da estrada acompanhar a serpentina, com largura controlada e concordância com a fita de progressão. Testar câmera só depois, em dois enquadramentos, para não confundir ganhos de cor com ganhos de projeção.

Preservar contato no solo, casas frontais, base horizontal e comportamento reversível dos objetos ao rolar. Conferir passagens, pontes, oclusões e áreas clicáveis. A escolha da curva poderá exigir ajustes locais de composição, a serem registrados individualmente.

### HUD inferior — seis propostas

Todas com cinco destinos, um estado selecionado claro, separadores discretos, sombra curta, relevo superior suave e família consistente de bordas dos ícones. Avaliar rótulo no item ativo e espaço suficiente para tradução.

1. Violeta acetinado com seleção lilás.
2. Lilás claro com ícones de contorno azul-violeta.
3. Azul postal com seleção pervinca.
4. Ameixa suave com creme e mínimo dourado.
5. Madeira pintada de lavanda, textura quase imperceptível.
6. Creme mágico com divisórias violetas e seleção lilás.

Comparar as primeiras alternativas com o mesmo conjunto de ícones para julgar a barra. Na segunda passagem, refinar ícones e repetir a comparação. Mostrar estados normal, selecionado, pressionado e aviso; não depender só de cor para distinguir seleção.

### HUD superior — doze propostas

Manter vidas, saldo/envelopes, avatar e configurações nas mesmas funções. Valores e rótulos permanecem texto independente, sem serem pintados na arte. O fundo pode chegar ao topo físico, mas controles respeitam recortes e área segura do aparelho.

| Família | Três variações |
| --- | --- |
| Esmalte mágico | Violeta/lilás; pervinca; azul postal |
| Postal claro | Creme/ameixa; azul claro/creme; verde-água/violeta |
| Madeira | Bordo claro/violeta; mel suave/azul; madeira pintada de lavanda |
| Papel e relevo | Papel mágico com trama; envelope largo com dobra discreta; faixa de cetim com borda arredondada |

Em todas: uma superfície principal, textura baixa, profundidade concentrada na borda inferior, cápsulas simples, configurações em um botão redondo e dourado apenas onde contribuir. Explorar relevo de texto escuro com pequeno realce claro; evitar letras com o mesmo valor do fundo. Não introduzir partículas ou brilhos animados para compensar o desenho.

Cada proposta deve aparecer primeiro isolada e depois no mesmo recorte do mapa. Selecionar três finalistas e detalhar as três, inclusive legibilidade de valores longos e compatibilidade visual com a modal atual.

### Porquinho e escudo — quatro propostas

Comparar: rosa natural natalino; marfim com detalhes dourados; dourado de esmalte suave; azul-gelo natalino como alternativa. Preferência inicial: rosa claro, chapéu vermelho simples e escudo azul-violeta.

Todas devem ter corpo/cabeça unidos numa silhueta compacta, expressão divertida, focinho simples e fenda do cofre larga e visível. O chapéu fica deslocado para não esconder a fenda. Usar um escudo de contorno arredondado com apenas duas camadas, pequena sombra e campo interno rebaixado. O porquinho sobrepõe o escudo, sem ficar encolhido dentro dele. Evitar brasão cheio de ornamentos, metal espelhado, presas e grandes acessórios. Testar no tamanho final do atalho. Chapéu separável permite versão comum ao fim do evento de Natal.

### Botões de fase — seis propostas

Comparar três construções em duas famílias cromáticas: A) botão de esmalte abaulado; B) selo mágico redondo, com borda simples; C) medalhão macio com apoio creme discreto. Roxo, rosa e âmbar são candidatos, não estados definitivos antes de mapear a lógica existente.

A face deve ocupar mais da silhueta e o número precisa ser maior, arredondado e bem centrado, com sombra curta. Reduzir anéis e separar o desenho do botão da identificação de fase atual. Testar inclinação menos achatada e compensação visual com a distância, sem desfazer a perspectiva do chão.

Mostrar em cada família: bloqueado, disponível, atual, concluído com 0/1/2/3 estrelas e especial quando aplicável. Coroas apenas onde o progresso justificar; estados especiais também usam forma/símbolo. Testar números de um, dois e três dígitos, posições próximas, médias e distantes, e o gesto de toque. Não avaliar por um botão ampliado isoladamente.

## Sequência de trabalho

1. **Baseline:** salvar capturas limpas do estado atual na mesma fase, horário/estação e resolução; incluir entrada, meio do mapa, horizonte e uma travessia. Registrar configurações para comparações repetíveis.
2. **Terreno:** três estudos de paleta, seleção de um, três curvas, seleção e duas opções de câmera. Fazer em ambiente de preview, preservando a HUD atual.
3. **Fases:** seis estudos de forma/material e estados. Integrar dois finalistas em capturas completas para avaliar projeção, hierarquia e ritmo.
4. **Porquinho:** quatro conjuntos porco/escudo; reduzir a dois e conferir em contexto. Preparar versão sazonal sem esconder a função de cofre.
5. **HUD em mocks:** doze superiores e seis inferiores, com rótulos e identificação comparável. Escolher três superiores e duas inferiores para acabamento. Não é necessário gerar todas as combinações possíveis.
6. **Conjuntos finais:** três telas completas combinando os melhores candidatos. Conferir coerência entre mapa, botões, ícones e HUD; incluir estudo pequeno de transição com a modal existente.
7. **Implementação posterior à escolha:** separar peças da arte, margens de escala e áreas seguras; importar assets; aplicar cores e materiais; conectar aos valores, cliques e estados existentes. Registrar o que mudou na direção de arte vigente.

São 31 estudos iniciais de componentes/terreno (3 paletas, 6 fases, 4 porquinhos, 12 HUDs superiores e 6 inferiores), além das comparações de curvas/câmera e das três composições finais. São estudos para triagem; somente os finalistas recebem acabamento de produção.

## Critérios de escolha e validação

- O cenário deve conservar cor também nas sombras; areia e gramado continuam claramente diferentes.
- O caminho deve ler como um S no enquadramento de jogo, com áreas laterais úteis e passagem desobstruída.
- Fases têm prioridade visual sobre cenário e promoções; o cofre é reconhecível sem ampliar.
- Volume vem de formas, bordas e sombras curtas, com poucos reflexos bem colocados.
- A HUD funciona em tamanho real de celular e com um olhar rápido. Conferir 360 e 390 pixels de largura lógica, aparelhos altos e tela com recorte, além de zoom para acabamento.
- Comparar sempre antes/depois na mesma escala e posição. Usar teste em tons de cinza para hierarquia, sem substituir a análise de cor.
- Preservar funções, progressão real, localização, áreas clicáveis e comportamento da rolagem. Validar custo de materiais e transparências ao implementar.

## Regras para esta rodada

Somente planejamento nesta etapa. A orientação recente do usuário sobre contorno branco nos objetos e a intenção de simplificar HUD/materiais revisam instruções visuais anteriores; consolidar as regras definitivas quando os mocks forem escolhidos. Não reescrever agora a direção global como se todas as propostas já estivessem aprovadas.
