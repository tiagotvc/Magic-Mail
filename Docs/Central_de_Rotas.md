# Poções — histórico técnico

**Feito e aceito pelo usuário em 23/10/2026.** Nome atual: **Poções**; nome anterior: Central de Rotas. Ficha de referência atual em [Pocoes.md](Pocoes.md). As seções abaixo preservam o histórico das revisões; descrições antigas de protótipo ou pendências não substituem esse aceite.

## Referência Royal Match — estouro local e fragmentos independentes (23/10/2026)

Acertos de especiais revisados: cada célula da central corresponde ao frasco naquela posição (superior esquerda verde, superior direita roxo, inferior esquerda vermelho, inferior direita azul). O foguete horizontal pode quebrar os dois frascos da linha atravessada; o vertical, os dois da coluna. Explosões usam igualmente as células efetivamente atingidas. Removidas a seleção do primeiro frasco intacto e a proteção global de 0,25 s, que descartava o segundo acerto. Acertar uma célula cujo frasco já quebrou não desvia o dano para outro. Guardas individuais evitam duplicar quebra/meta. Matches adjacentes mantêm a seleção pela identidade/cor e a deduplicação da combinação. A última quebra continua acionando a madeira do suporte.

Validação desta regra: runtime/editor compilados sem erros; `Tools/RouteCentralHitChecks.cs` executado com Mono da Unity contra o assembly compilado passou 1290 verificações (16 estados de frascos quebrados, duas linhas/duas colunas em ambas as ordens, repetição, limites e identidades de match). As 53 verificações existentes de central/ovo também passaram; o runner fora do editor emite aviso de chamada nativa de Random durante a parte do ovo. O percurso visual do foguete e a integração de especiais ainda precisam de conferência em Play.

Suporte final: por nova orientação do usuário, a estante agora quebra quando todos os quatro frascos terminam a ruptura. Usa a mesma explosão de madeira da quebra final das caixas, com som e brilho correspondentes. Uma guarda impede duplicação por impactos simultâneos. Antes do último frasco, permanece intacta; **Repetir teste** restaura o suporte e limpa seus efeitos. Isso substitui a orientação anterior de manter a estante após a última poção. Liberação das células e contagem da meta mantidas. Runtime compilado sem erros; aparência em Play pendente.

Analisado o GIF enviado pelo usuário (`Potion_Bottle_Tutorial_Final.gif`, 170 quadros, 10 fps). Na quebra da poção, o respingo abre junto ao frasco e se fragmenta rapidamente; gotas e cacos percorrem trajetórias distintas. Não há um corpo contínuo de líquido atravessando toda a tela. A observação descreve o movimento; não identifica a implementação interna do Royal Match.

Efeito padrão revisado: mantém o alinhamento por frasco e os oito primeiros quadros da ruptura (0,32 s). No quinto quadro, dispara 22 gotas afuniladas e oito fragmentos angulares de vidro com impulsos diferentes. O sprite da ruptura fica no local e desaparece ao concluir a abertura; não é arrastado para baixo. As gotas giram de acordo com sua velocidade e se alongam sem reduzir sua área bidimensional; os cacos giram independentemente. Fragmentos saem pela borda inferior, com limite defensivo de cinco segundos. Um mesh/material por impacto, sem dependências externas nem novas imagens.

Teste: **Royal Aves → Elementos → Poções → Laboratório estouro e respingos no jogo**. Usa a fase jogável existente, HUD, matches e especiais. Variantes metaballs e desenhada continuam disponíveis para comparação. O novo efeito é também o padrão fora dessas variantes. Reset da prévia limpa respingos e libera os recursos próprios.

Runtime/editor compilados sem erros. Verificadas geometrias dos fragmentos e trajetórias em diferentes escalas; resultado visual ainda requer conferência no Play. Os efeitos da madeira foram revisados na mesma direção, conforme `Madeira_Explosao.md`.

## Revisão 23/10/2026 — conservar o volume durante a queda

A ruptura agora termina no oitavo quadro, que ainda contém o respingo volumoso e os cacos. A queda transporta esse mesmo sprite inteiro, preservando pivô, escala, material, cor e ordem de desenho, sem reproduzir os quadros seguintes que eliminavam líquido. Todo o conjunto acelera para baixo por 0,72 s e só é removido após ultrapassar a borda inferior, com margem baseada na altura do sprite. Não há redução de escala nem fade. O tempo informado ao controlador acompanha os 0,32 s de ruptura mais 0,72 s de queda. Limitação: gotas e cacos caem como um conjunto, sem trajetórias independentes. Runtime compilado sem erros; conferência visual em Play pendente. Esta revisão substitui o uso dos quatro quadros finais descrito abaixo.

## Revisão 23/10/2026 — alinhamento da quebra

Corrigido o deslocamento do frasco entre os quadros da spritesheet: os pivôs agora compensam a posição do aro do gargalo, medida nos 12 quadros de ruptura, em vez de usar o centro de cada célula. Os quatro quadros de líquido herdam a compensação do último quadro de vidro para preservar a continuidade da transição. A arte original foi preservada. A correção remove a translação causada pelo desalinhamento; variações de desenho e inclinação presentes na arte permanecem. Conferência visual em Play pendente.

## Usar a própria tinta da sequência de quebra

Revisão solicitada pelo usuário: removidos o mesh viscoso e as partículas de líquido gerados por código. A quebra reproduz os primeiros 12 frames; os quatro frames finais, que já contêm apenas tinta e gotas, continuam em um sprite independente que cai até sair pela borda inferior da câmera. A transição preserva posição, pivô, escala e cor do frasco. A queda dura 0,72 s, após 0,48 s de ruptura, com leve abertura lateral. Nenhuma nova arte foi gerada: reutiliza `CM_frasco_quebra_sheet.png`. Ausência do spritesheet agora bloqueia o efeito com erro explícito, em vez de reativar o líquido antigo. Compilação aprovada; continuidade final depende de conferência no Play.

## Spritesheet de ruptura e saída do líquido

A pedido do usuário, a quebra agora usa uma sequência raster de 16 frames (4×4): frasco intacto/reação, rachaduras, abertura do vidro, expulsão do líquido e fios caindo. Gerada com imagegen integrado, salva em `Assets/RoyalAves/Resources/RouteCentral/CM_frasco_quebra_sheet.png`, PNG transparente sem compressão. Os dois primeiros intervalos preservam o sprite original; os seguintes reproduzem a ruptura a 25 fps, com cor do frasco. A estante continua independente. A guarda de quebra impede reiniciar a animação por impactos repetidos.

O frame 9 dispara o escoamento pela tela, a partir da posição do líquido já saindo do frasco, sobrepondo as duas etapas. A sequência dura 0,64 s. O reset da prévia restaura os sprites intactos, a cor branca e as guardas de quebra. A duração informada ao controlador inclui a transição. Compilação aprovada; continuidade e escala precisam de avaliação em Play.

Prompt de produção usado: spritesheet 4×4 de 16 frames cronológicos, câmera/escala/âncora fixas, um erlenmeyer com rolha e aro simples; líquido branco perolado para colorização na Unity; reação e rachaduras nos primeiros frames, vidro rompendo e líquido conectado ao corpo nos intermediários, fios viscosos e gotas descendo nos finais; mesma arte suave das poções, sem suporte, símbolos, bolhas, texto, linhas de grade ou fundo, transparência real.

## Referência de líquido viscoso — revisão rápida

Dispersão ampliada após avaliação do usuário: a base do escoamento abre progressivamente até 2,5 vezes a largura inicial, enquanto a saída permanece junto ao frasco. As gotas recebem mais velocidade lateral e leve turbulência adicional para acompanhar esse leque. Mantidos a cor do frasco e o tempo de aproximadamente 1,05 s. Compilação aprovada; conferir a amplitude no Play.

A referência enviada pelo usuário passa a orientar o escoamento: superfície conectada com três pontas irregulares de tinta, borda iluminada e corpo sombreado, deformados durante a queda. Partículas ficam apenas como gotas que se desprendem. A frente se alonga primeiro e a parte superior acompanha, esvaziando o frasco. Todo o escoamento atravessa a borda inferior da câmera em aproximadamente 1,05 s; o percurso é ajustado à distância real até essa borda. A cor vem do frasco quebrado (verde, lilás, vermelho ou azul), sem alterar a estante. Implementado como mesh animado e partículas nativas, sem copiar a spritesheet da referência. Compilação aprovada; aparência ainda requer avaliação no Play.

## Líquido por partículas

Revisão de fluidez: as partículas deixam de usar discos e passam a ter corpo alongado com cauda estreita, orientado pela velocidade. Rastros curtos de 0,14–0,24 s com largura afunilada conectam visualmente as gotas. A emissão principal é mais concentrada e varia de 210 gotas/s no início até zero ao esvaziar; o respingo inicial foi reduzido para 16 gotas. Menor dispersão lateral e turbulência mantêm o líquido coeso no início e permitem sua separação durante a queda. Compilação aprovada; aparência ainda depende de avaliação no Play. Esses ajustes substituem as quantidades da primeira versão descritas abaixo.

O escoamento rígido foi substituído por dois `ParticleSystem` nativos por frasco: um respingo inicial de 28 gotas e uma emissão curta de líquido (0,55 s, 85 gotas/s). Gotas arredondadas usam tamanhos, proporções, cores e velocidades variadas, gravidade e turbulência suave no plano da tela. Os meshes têm volume sugerido por cores de vértice, sem textura externa adicional. A simulação ocorre em espaço mundial, com tamanhos e gravidade ajustados à escala real do elemento.

A duração continua calculada pela borda inferior da câmera para que as gotas atravessem a tela antes da remoção do efeito. Estante e estilhaços mantêm suas camadas próprias. A meta continua aguardando a duração do efeito antes da conclusão da partida. Compilação runtime/editor aprovada; apresentação visual pendente de conferência no laboratório após recompilar. Esta revisão substitui os cinco fios geométricos descritos no histórico abaixo.

## Paleta e correspondência exata das peças

O laboratório usa exclusivamente **selo vermelho**, **envelope azul**, **trevo verde** e **pena lilás**. Ao carregar a fase, a paleta é reconstruída consultando os sprites reais do prefab de peças, em vez de confiar na paleta de assets antigos. Peças iniciais fora da seleção são corrigidas. Os arquivos históricos dos ícones são `stamp`, `bow`, `gift` e `envelope`, respectivamente; `envelope` é a pena lilás, enquanto `bow` é o envelope azul.

O impacto de match na central verifica a identidade da textura do sprite exibido: amarelo (`key`), tinteiro (`ink`) e pena azul (`quill`) não correspondem a nenhuma poção. Acertos diretos de especiais preservam sua regra própria. Sete verificações adicionais no assembly compilado cobrem as quatro identidades aceitas e as três rejeitadas; compilação aprovada. Conferência interativa da fase ainda necessária após recompilar e reiniciar o laboratório.

## Correção do alinhamento das camadas

O recorte dos frascos já usava a arte nova, mas os deslocamentos e o tamanho do conjunto ainda dependiam dos pixels por unidade da arte combinada, alterados pelo importador geral. Isso gerava frascos minúsculos e afastados da estante. A montagem agora normaliza todos os sprites para um canvas lógico de quatro unidades, com quadrantes de duas unidades e centros em (±1, ±1), independentemente de PPU ou pivô importado. O controlador dimensiona esse canvas diretamente para a largura de duas células. A arte combinada também recebeu regra própria no importador. A verificação nativa `verify-stand` confere dimensões, posições e permanência da estante depois de duas quebras em uma instância isolada fora da tela.

## Correção de estante e tinta — 22/10/2026

Correção adicional após o usuário reproduzir a falha: o prefab gerado ainda estava sem `standArtwork` e `flaskArtwork`, fazendo o runtime cair no recorte da arte combinada antiga. As referências foram restauradas e as camadas passaram para `Assets/RoyalAves/Resources/RouteCentral/`, preservando GUIDs. O runtime resolve esses sprites por `Resources` quando um prefab antigo está sem referências. O fallback que recortava a estante junto do frasco foi removido; ausência das camadas gera erro explícito. O gerador do laboratório também referencia o novo caminho.

Por solicitação do usuário, a estante é uma camada independente e permanece intacta após qualquer quebra, inclusive a última. Só o frasco atingido é ocultado e estilhaçado. As quatro células deixam de bloquear após a última poção, mas a estante continua visível. Isso substitui a limitação de quadrantes do primeiro protótipo.

A tinta não forma mais uma poça na célula. Cinco fios com gotas avançam para baixo, passam por outras linhas e só são removidos após atravessarem a borda inferior da câmera. A distância e a duração são calculadas com a escala do elemento e a câmera reais, para funcionar no laboratório e na tela de prévia. A conclusão da partida aguarda o maior tempo entre o vidro e o escoamento.

Camadas PNG transparentes produzidas com imagegen integrado e salvas em `Assets/RoyalAves/Art/Obstacles/RouteCentral/`: `CM_central_estante.png` e `CM_central_frascos.png`. A arte anterior permanece como referência. Prompt da estante: remover todos os frascos e reconstruir apenas o suporte lilás e as duas prateleiras douradas, mantendo alinhamento, proporção, canvas e transparência. Prompt dos frascos: remover todo o suporte e preservar apenas os quatro erlenmeyers verde/roxo/vermelho/azul no layout 2×2, reconstruindo suas bases de vidro, sem símbolos, bolhas ou fundo.

Para reconstruir e repetir na tela real: **Royal Aves → Elementos → Poções → Laboratório no jogo**. As descrições de poça local e desaparecimento do suporte nas seções históricas abaixo foram substituídas por esta revisão.

## Laboratório na tela real do jogo

Menu **Royal Aves → Elementos → Poções → Laboratório no jogo**. Esse é o teste jogável, distinto de **Testar visual e tinta**, que continua sendo uma prévia isolada. O novo menu para o Play anterior, prepara os assets, abre a cena principal e carrega o laboratório com a interface normal do jogo. Há painel para reiniciar e desativar. A campanha não é sobrescrita.

Tabuleiro 7×7, 35 jogadas, uma central fixa 2×2 nas colunas 3–4 e linhas 2–3 (coordenadas a partir de zero). Meta própria `RoutePotions`: quatro frascos quebrados, não uma central. Match ortogonal adjacente usa o tipo real da peça: trevo verde (ID 1), pena roxa (0), selo vermelho (2), envelope azul (6). Só quebra o frasco correspondente ainda intacto. Uma combinação é deduplicada por seu identificador, mesmo encostando em várias células.

Primeira troca preparada: (1,2) com (2,2), formando uma combinação roxa à esquerda da central. Foguete vertical em (3,5) para testar especial. Acerto direto de especial quebra o primeiro frasco restante na ordem verde, roxo, vermelho, azul. No laboratório, impactos diretos têm intervalo de proteção de 0,25 s para evitar que a passagem pela segunda célula duplique a quebra; combinações complexas de especiais ainda requerem validação específica.

Cada quebra exibe vidro e tinta escorrendo; a meta diminui uma vez. A quarta quebra libera as quatro células para queda. Um bloqueio de conclusão de 2,4 s permite terminar o efeito antes de verificar vitória. O laboratório encerra o Play ao ganhar/perder, seguindo os demais laboratórios. A tinta é visual e não cria piso ou dano adicional. A limitação provisória da arte dividida em quadrantes permanece.

Assets persistidos: `RouteCentralLevel.asset`, `RouteCentralTargets.asset` e `RouteCentralCell.prefab` em `Assets/RoyalAves/Laboratory`. Construção offline em `Tools/build_route_central_lab.py`; o menu também reconstrói pela API da Unity. Compilação runtime/editor e oito verificações gerenciadas de seleção por cor/estado aprovadas; ciclo completo de partida ainda precisa de conferência no Play.

Próximo elemento após o Ovo mágico, declarado concluído pelo usuário em 22/10/2026. Estreia prevista: fase 61; reforço: 62–80. Esta ficha inicia a definição, sem alterar a campanha.

## Regra prevista no plano

Painel com compartimentos por cor e símbolo. Matches da cor designada ou power-ups avançam os compartimentos até a conclusão. Mostrar o símbolo da peça correspondente, sem depender apenas da cor.

## Proposta para o primeiro protótipo

- Objeto fixo de 2×2, sem troca nem queda, com quatro compartimentos visíveis.
- Cada compartimento pede um tipo de peça da paleta habilitada no nível; se houver só três tipos, um pode se repetir.
- Match imediatamente ao lado de qualquer célula do painel, sem diagonais: avança um compartimento ainda aberto que peça aquele tipo. Uma mesma combinação entrega uma vez por central, mesmo encostando em duas células.
- Cada compartimento requer uma entrega. Tipos repetidos são preenchidos um por vez em ordem visual.
- Especial que atinja o painel preenche um compartimento aberto, em ordem visual. Uma mesma ativação conta uma vez por central, mesmo atingindo várias das quatro células.
- Compartimento preenchido confirma a entrega e deixa de aceitar impactos. Match de tipo sem pedido aberto não avança.
- Ao preencher os quatro, o painel é removido, libera o espaço e conta uma unidade na meta.
- Reiniciar restaura os quatro pedidos. Laboratório isolado antes da arte final e da campanha.

Estas escolhas são uma proposta de design, ainda não aprovadas nem implementadas. O primeiro ponto a confirmar é o formato do painel e a interação por tipos de peças.

## Protótipo visual de poções — 22/10/2026

Revisão do enquadramento: a tela de teste agora mostra um tabuleiro de referência 8×8, com trevos e penas, e a central no centro ocupando 2×2 células. Esse tabuleiro é apenas contexto visual, sem jogadas. A câmera se adapta à proporção da janela. Botões Verde/Roxa/Vermelha/Azul disparam cada quebra e **Repetir teste** restaura os quatro frascos. Parar o Play e abrir novamente o menu **Testar visual e tinta** reconstrói a cena com essas alterações.

Direção revisada pelo usuário: quatro erlenmeyers de poção verde, roxa, vermelha e azul, sem símbolos, sem bolhas e com suporte minimalista. Essa direção substitui os pedidos por símbolos visíveis da proposta inicial. Arte gerada com a ferramenta integrada imagegen e copiada para `Assets/RoyalAves/Art/Obstacles/RouteCentral/CM_central_pocoes_prototipo.png` (1280×1280, transparente, sem compressão, 320 PPU).

Prefab visual: `Assets/RoyalAves/Prefabs/Elements/RouteCentralVisual.prefab`. Script: `RouteCentralPrototype`; entrada pública `BreakFlask(0..3)`, protegida contra acionamento repetido. A geração pela Unity também inclui uma prévia estática para posicionamento no editor.

Após a Unity recompilar, usar **Royal Aves → Elementos → Poções → Testar visual e tinta**. O menu gera uma cena isolada, abre e inicia o Play. Clicar em cada poção dispara estilhaços claros de vidro, pulso de impacto, cinco fios de líquido na cor correspondente e uma poça que se espalha e encolhe. O escoamento dura cerca de 2,3 s; os cacos seguem até sair pela parte inferior da câmera. Não aplica piso, dano nem bloqueio de tinta ao tabuleiro. Para repetir, parar e iniciar o Play.

Esta entrega é um protótipo visual interativo, sem filtro de matches por cor, meta ou pintura no Level Editor. A arte provisória é dividida em quadrantes: ao quebrar um frasco, também desaparece a parte do suporte naquele quadrante. Separar o suporte e os frascos na produção final. A campanha não foi alterada. Compilação runtime/editor verificada em `Logs/route-central-build.log`; o efeito ainda precisa de conferência visual em Play.

Prompt final da arte (imagegen integrado): quatro frascos erlenmeyer em 2×2; poções verde/roxa/vermelha/azul nas respectivas posições; sem símbolos, bolhas, estrelas ou brasão; líquido liso com superfície curva; suporte lilás fino com travessas douradas simples; formas arredondadas, volume suave de ícone, poucos reflexos; vista frontal e fundo transparente.
