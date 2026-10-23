# Bolsa do carteiro (antigo Malote)

**Status: feita e aceita pelo usuário em 23/10/2026.** Próximo elemento: Encomenda Encantada.

## Funcionamento aprovado

- Bolsa fixa, frontal e sem alça. Match ortogonal adjacente ou especial aciona uma entrega; impactos repetidos durante a animação são ignorados.
- Abre e fecha em 0,9 s. Brilho dourado discreto de 0,28 s na boca, com textura existente e material compatível com o jogo.
- Pergaminho sai fechado, desenrola, faz uma pirueta e voa em arco ao contador. Saída e voo totalizam 0,72 s; conserva a largura do rolo fechado, aumentando a altura ao abrir, sem encolher na chegada.
- Meta **Pergaminhos**, quantidade manual no Level Editor; ID interno `BagOpenings` preservado. Conta apenas na chegada e reserva as entregas em voo para não exceder o saldo.
- HUD usa pergaminho aberto, com proporção preservada, centralizado e separado do contador.
- Laboratório 7×7 com 19 bolsas em U, 50 jogadas e envelope azul no tabuleiro. Menu: Royal Aves > Elementos > Bolsa do carteiro > Laboratório de abertura em U.

Aceite do laboratório concluído. Inserção e balanceamento na campanha seguem etapa própria.

## Histórico de protótipos e revisões (substituído pelo estado aprovado acima)


## Direção revisada — bolsa do carteiro

Pergaminho fechado → aberto: `Assets/RoyalAves/Art/Obstacles/Mailbag/CM_pergaminho_abrir_sheet_v1.png`, transparente, 16 quadros em 4×4, imagegen integrado a partir do pergaminho v1. Começa enrolado num cilindro horizontal e revela gradualmente o papel até ficar aberto, sem texto. Prévia `Pergaminho_Abrir_Preview.html` a 20 fps, com recortes individuais em `Pergaminho_Abrir_Frames.json`. Ainda não integrado à emissão da bolsa.

Coletável escolhido pelo usuário: **pergaminhos** saem da bolsa ao receber impactos válidos. Espíritos de luz, borboletas, estrelas e runas foram descartados. Protótipo visual `Assets/RoyalAves/Art/Obstacles/Mailbag/CM_pergaminho_prototipo_v1.png`, gerado com imagegen integrado: pergaminho marfim parcialmente aberto, extremidades enroladas, volume 3D suave e pequenos reflexos turquesa; sem texto ou selo. Arte ainda em avaliação. Emissão/voo até a meta ainda não integrados ao laboratório de abertura.

### Laboratório atual: abertura em U

**Royal Aves → Elementos → Bolsa do carteiro → Laboratório de abertura em U** (também acessível em Malote). Prepara a tela normal de jogo com tabuleiro 7×7, **19 bolsas fixas nas duas laterais e na borda inferior**, topo livre e centro com peças jogáveis. Usa a spritesheet frontal sem alça e recortes/pivôs medidos em `Bolsa_Abrir_Frames.json`. O comando prepara os 16 sprites e um prefab próprio de estação fixa; substitui a configuração do laboratório anterior, sem editar fases da campanha.

Matches ortogonais adjacentes e especiais acionam abertura, breve pausa aberta e fechamento. Durante o movimento, impactos repetidos são ignorados. A bolsa permanece fixa e não é destruída por dois impactos. Ainda não há coletável mágico escolhido/emitido: a meta temporária de 100 aberturas existe só para a bancada. São 50 jogadas. Os cantos inferiores ficam acessíveis por especiais ou pelos botões de diagnóstico.

O painel tem **Abrir todas as bolsas**, **Abrir uma bolsa**, velocidade de 0,25× a 2×, reinício e desativação. A abertura usa 20 fps (0,8 s), pausa de 0,18 s e fechamento em reverso. Primeira jogada preparada: trocar (3,1) com (3,2), contando de zero; foguete horizontal em (3,3) para as duas laterais.

Validação: runtime/editor compilados sem erros; 83 verificações contra o assembly compilado conferem as 19 posições exatas, limites, topo/miolo livres e tipos da estação/meta. Conferidos também limites dos 16 recortes e pivôs na atlas sem alça. Assets são preparados pelo comando do editor; execução visual e enquadramento ainda precisam ser conferidos em Play. O laboratório móvel de dois impactos abaixo é histórico.

Abertura v2 sem alça: removidas as alças de ombro e segmentos laterais em todos os 16 quadros, preservando corpo, aba e fivela. Novo asset `Assets/RoyalAves/Art/Obstacles/Mailbag/CM_bolsa_carteiro_abrir_sem_alca_v2.png`, gerado por edição com imagegen integrado. Prévia HTML atualizada para essa versão; a spritesheet anterior foi preservada como histórico.

Spritesheet de abertura: `Assets/RoyalAves/Art/Obstacles/Mailbag/CM_bolsa_carteiro_abrir_sheet_v1.png`, 1254×1254, transparente, 16 poses em 4×4, gerado com imagegen integrado a partir da frontal v3. A aba e a fivela sobem juntas, revelando o interior turquesa; quadros finais mantêm a bolsa aberta. Prévia: `Bolsa_Abrir_Preview.html`, 20 fps (~0,8 s), com abertura, fechamento em reverso e controle de quadro. A geração apresenta variação de posição entre células; `Bolsa_Abrir_Frames.json` registra recortes individuais e âncoras medidas no corpo inferior, usados na prévia para compensar o deslocamento e excluir resíduos das células vizinhas. PNG original preservado; spritesheet ainda não aplicada ao laboratório.

Revisão visual v3: vista estritamente frontal, fivela centralizada e corpo simétrico, com alça atrás. Removido o contorno ilustrado marcado; volume por gradientes, luz e sombra suaves para aproximar o acabamento 3D das peças do jogo. Asset: `Assets/RoyalAves/Art/Obstacles/Mailbag/CM_bolsa_carteiro_frontal_v3.png`, imagegen integrado, transparente. Substitui a v2 como conceito mais recente; aguardando avaliação visual, sem aplicar ao laboratório antigo.

Revisão visual v2: o usuário considerou a primeira bolsa realista demais. Novo conceito `Assets/RoyalAves/Art/Obstacles/Mailbag/CM_bolsa_carteiro_cartoon_v2.png`, gerado com imagegen integrado usando a bolsa v1 como alvo e o trevo `gift.png` e o armário `CM_armario_modelo_base_v2.png` como referências de estilo. Formas mais volumosas e arredondadas, cores lisas, brilho amplo e remoção de textura, costuras e detalhes finos. Arte ainda em avaliação; não altera o laboratório anterior.

O usuário rejeitou o malote de tecido e pediu uma bolsa baseada na arte mais recente do carteiro. Referência encontrada: `Assets/RoyalAves/Art/MapKitV3/Library/Personagens/courier-wizard-v50.png`; a versão aprovada do mapa `postman-map-v12.png` conserva a mesma bolsa. Novo conceito com imagegen integrado: `Assets/RoyalAves/Art/Obstacles/Mailbag/CM_bolsa_carteiro_conceito_v1.png`, couro caramelo, aba frontal e fivelas douradas, com brilho turquesa na abertura. Não inclui cartas nem um coletável definitivo.

Nova direção em discussão: bolsa fixa que reage a matches adjacentes e libera um coletável mágico que voa para a meta, semelhante à franqueadora. Sugestão principal: vaga-lumes mágicos; alternativas: estrelas de magia ou cristais encantados. Proposta para diferenciar da máquina: estoque finito de três coletáveis por bolsa; ao esvaziar, a bolsa murcha e sai, liberando a célula. Coletável, estoque e comportamento final ainda não escolhidos pelo usuário.

O laboratório móvel de dois impactos descrito abaixo fica como protótipo anterior, sem aceite. A mecânica nova ainda não foi aplicada ao código; não tratar a proposta como regra homologada.

**Status: protótipo e laboratório jogável implementados em 23/10/2026; validação e aceite em Play pendentes.** Próximo elemento após Poções.

## Laboratório jogável

Abrir **Royal Aves → Elementos → Malote → Laboratório no jogo**. O comando prepara sprites de dois estados a partir do conceito, prefab próprio `MAILBAG`, pool, meta `Mailbags` e fase isolada 7×7 com quatro malotes e 30 jogadas. Usa a tela normal de jogo, HUD e gravidade; não modifica fases da campanha. **Desativar laboratório** restaura o carregamento normal. O painel tem reinício, botão de impacto diagnóstico e lista de resistências por posição.

Primeiro teste: trocar (2,3) com (2,2), em coordenadas a partir de zero, para formar um match adjacente ao malote em (2,1). Há um foguete vertical em (4,4), alinhado com o malote da coluna 4. O primeiro impacto muda para a arte da cinta rompida e faz uma pequena reação; o segundo retrai o tecido por 0,16 s, remove e conta uma unidade na meta. O malote cai pelo fluxo de Item e não forma combinações por cor; troca manual usa a regra existente de exigir jogada válida.

A mesma combinação identificada não pode consumir as duas resistências; callbacks diretos próximos têm uma proteção de 0,25 s. Isso ainda não equivale a um identificador global de ativação de especiais para explosões com várias ondas. O dano é preservado durante a movimentação; voltar ao pool reinicializa as resistências. Cancelar o objeto limpa a barreira de coleta. A fase inicial não inclui um portal; essa integração depende de teste específico posterior.

Validação: runtime/editor compilados sem erros; 321 verificações no assembly compilado cobrem dois impactos, repetição, proteção temporal, combinações, 50 reinicializações, IDs serializados, meta e exclusão de power-up. Assets são preparados pelo comando no editor. Aparência, execução nativa, troca, queda e cancelamento precisam de conferência no jogo. Arte e animação são provisórias, não uma versão aprovada.

## Base do guia

Alvo móvel com dois impactos: o primeiro rompe a cinta, o segundo coleta. O estado de dano deve ser preservado na queda e no portal. Estreia proposta na fase 81, reforço 82–100. Deve parecer um malote de tecido/couro, claramente diferente da caixa de madeira.

## Proposta inicial para o protótipo

- Uma célula, quantidade finita colocada no editor, sem geração espontânea. Acompanha a gravidade e os portais.
- Não forma matches por cor. Proposta de dano: match ortogonal adjacente ou impacto direto de especial, seguindo a regra do Ovo mágico.
- Primeiro impacto: cinta se rompe e se desprende; o malote permanece, sem diminuir a meta. Segundo impacto: abertura/retirada local e coleta de uma unidade.
- Deduplicar o mesmo impacto para não consumir as duas resistências de uma vez. Manter o dano ao cair/teleportar e restaurar duas resistências apenas em uma nova instância ou reinício da fase.
- A troca manual e a apresentação da coleta ainda são decisões de protótipo; não considerar homologadas pela descrição acima.

## Direção visual inicial

Bolsa postal arredondada de tecido bege quente, base macia, costuras simples, cinta de couro castanho e fecho de latão. Sem madeira, tampa rígida, texto ou número. Estado danificado conserva volume e posição, mas perde a cinta e fica ligeiramente afrouxado. Não usar fragmentação de madeira para tecido/couro.

Conceito em dois estados: `Assets/RoyalAves/Art/Obstacles/Mailbag/CM_malote_conceito_v1.png`. Gerado com imagegen integrado; é referência visual, ainda não sprites finais nem prefab.

## Próximas etapas

Conferir o laboratório em Play; refinar os sprites finais e a animação da cinta se necessário; testar troca, queda, reinício e portal; revisar a identidade de ativações dos especiais com múltiplas ondas; obter aceite visual e de regras. O protótipo não representa inserção na campanha.


## Coleta de pergaminhos implementada

A bolsa fixa abre em 0,9 s. Com a boca aberta, um brilho dourado acompanha a saída do pergaminho fechado. Ele desenrola durante o voo em arco, completa uma pirueta e chega ao contador. A meta só diminui na chegada. Reservas de entregas evitam exceder a quantidade restante em impactos simultâneos; interrupções liberam a reserva sem contar coleta.

No Level Editor, selecionar a meta **Pergaminhos** e definir a quantidade manualmente (ID interno `BagOpenings`, preservado). O laboratório em U inicia com 20 e preserva alterações posteriores na quantidade. Reabrir pelo menu Royal Aves > Elementos > Bolsa do carteiro > Laboratório de abertura em U recompila os sprites de bolsa e pergaminho.
