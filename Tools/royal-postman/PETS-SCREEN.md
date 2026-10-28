# Atual V16

Sprites, bars, badges, subtabs and collection spacing revised against the mock. See [V16 details and captures](production/PETS-CURRENT-V16.md). This supersedes the V15 generic presentation below; mechanics and shared Incubator assets remain intact.

# Aba Atual ? V15

Atual abre por padr?o ao entrar em Mascotes. O perfil usa o jardim e o p?dio separados, Lumi provis?ria e dois outros exemplos (Foxy e Biscoito). Selecionar por setas, cards ou bolinhas atualiza retrato, nome, raridade, n?vel, XP, energia, habilidade passiva e magia. H? ordena??o e informa??es por clique. Equipar ? uma demonstra??o local da interface; Alimentar permanece desativado. Apar?ncia, Selos e Contratos t?m cadeado e n?o abrem.

O cabe?alho, os quatro estados das abas Atual/Incubadora, as setas e o divisor s?o os mesmos componentes aprovados da tela anterior. Incubadora conserva seu layout e suas regras. Este padr?o Atual substitui o padr?o Incubadora descrito na se??o V14 abaixo.

O cat?logo edit?vel est? no componente RoyalPostmanCurrentPet de Pets/CurrentProfile. N?o s?o concedidos pets nem efeitos de combate. Imagens originais separadas por uso em Art/UI/Menu/RoyalPostman/Pets e Art/UI/Icon/RoyalPostman/Pets. O manifesto production/PETS-CURRENT-V15.json registra os oito assets, dimens?es, hashes, origens e prompts completos do ImageGen integrado.

Valida??o do prefab realmente instalado, sem reconstru?-lo: passou no Unity em 360?800, 720?1280 e 1080?1920; inclui sele??o/dados por pet, bloqueios, abas, ordena??o, tooltip, equipar demonstrativo, pr?via/salvamento e regress?es da incuba??o. Editor/runtime t?m zero pixels diferentes. Compila??o de runtime/editor passou. Capturas e relat?rio em production/review/pets-current-v15/. Use Recarregar no editor ap?s esta atualiza??o externa.

---

# Mascotes — mock corrigido

Abas V14: **dourado indica sele??o; azul indica inatividade nas duas abas**. Atual tem pata azul em recipiente azul/dourado; Incubadora tem ovo dourado em recipiente azul/dourado. `ShowCurrent` alterna os sprites reais e o conte?do, com cor branca na Image. Duas novas imagens individuais (1254 ? 1254) completam os estados ausentes; as duas V4 originais s?o reutilizadas sem altera??o. Recortes de Sprite delimitam o componente com margem de antialias, sem redimensionar nem editar pixels. O editor permite pr?via por clique nas abas; salva a c?pia com Incubadora inicialmente selecionada, preservando a aba aberta na pr?via. Manifesto/prompt completo: `production/PETS-TAB-STATES-V14.json`; ambas as sele??es renderizadas em `production/review/pets-tabs-v14/`. Instala??o somente nas quatro refer?ncias de sprites, duas faces e cores dos dois bot?es; demais elementos preservados.

Tooltip V13: clique no item abre um cart?o azul-marinho com ?cone, nome, descri??o/dura??o, requisito de n?vel para ovos raros/?picos e quantidade. O cart?o aparece junto do item e respeita os limites da tela. Clique em outro item troca o conte?do; X, clique fora, fundo do invent?rio, mudan?a de p?gina/aba ou sa?da fecham. A pr?via do editor usa a mesma sele??o/cart?o, distinguindo clique de arraste. Tooltip e sele??o de invent?rio s?o transit?rios e n?o entram no prefab salvo. O texto **Incubadora** agora tem largura interna de 100 unidades e ajuste entre 14?18px, validado sem quebra nem transbordamento. Apenas componentes necess?rios do tooltip e legenda foram alterados; bot?o ?nico e regras V12 preservados. Nenhum novo PNG. Revis?o: `production/review/pets-tooltip-v13/`.

Incuba??o V12: um bot?o azul simples substitui os dois anteriores. **Incubar xN** usa a quantidade real do ovo correspondente e fica desativado quando o estoque ? zero. O clique consome um ovo e inicia o temporizador UTC salvo: comum 1h, raro 3h, ?pico 12h. Durante a incuba??o, o mesmo bot?o mostra **Acelerar** e a dura??o do item selecionado dispon?vel (30/60/90/120 minutos); sem sele??o v?lida, usa o primeiro acelerador dispon?vel. Na falta de itens, cada compra retira 30 minutos e cobra a pr?xima faixa **por ovo**: 500, 1.000, 1.500, 3.000, 4.500, 8.000, repetindo 8.000. Itens n?o sobem a faixa de moedas. O bot?o informa o pre?o antes da compra e desativa com saldo insuficiente; a carteira existente revalida o saldo ao clicar. Incuba??o conclu?da fica vis?vel e n?o aceita novas cobran?as; eclos?o/recompensas ser?o um fluxo posterior.

`PetsIncubationState` cont?m as regras e valida??es; `PetsProgress` salva estoques, hor?rios e faixa paga. `LobbyController` injeta a carteira real `InitScript`. Novos estoques come?am vazios; APIs de concess?o: `GrantCommonEggs`, `GrantRareEggs`, `GrantEpicEggs`, `GrantAccelerationItems(minutes,count)`. N?o converter n?meros demonstrativos em saldo real. Quantidades dos slots de ovos/aceleradores v?m desse estoque; as artes ainda s?o exemplos repetidos e os cristais continuam demonstrativos. `UseItemRequested`/`AccelerateRequested` notificam depois da transa??o. Revis?o: `production/review/pets-incubation-v12/`; capturas ready/item/gold usam estoque e carteira de teste injetados. Testes cobrem consumo, bloqueios, tempo por item, sequ?ncia/teto de pre?o, falha de pagamento, serializa??o/reabertura, pagina??o/hover e igualdade de pixels entre editor e runtime. Compila??o integral de runtime/editor passou. Nenhum PNG novo.

HUD V11: altura do cabe?alho aumentada de 105 para 136 unidades, com bot?es e t?tulo centralizados. A barra dourada acompanha a borda inferior e deixa 15 unidades livres abaixo dos bot?es. Apenas os tr?s RectTransforms do cabe?alho/fundo/barra mudam; demais posi??es preservadas. Revis?o: `production/review/pets-header-v11/`.

Ajuste V10: bolinhas no dourado das setas (p?gina atual viva, demais apagadas), com sombra alaranjada; duas categorias baixadas em 30 unidades, preservando seu espa?amento. Ornamento central 25% menor, barra e pontas 35% menores; espessura reta de 7,8 unidades. Imagens originais preservadas. Aplica??o somente nos campos solicitados do prefab atual; revis?o e valida??o: `production/review/pets-polish-v10/`.

Divisor V9: barra e pontas agora usam recortes nativos de **uma ?nica imagem**, produzida com o ornamento central como refer?ncia. As faces amarelas, o reflexo creme e as laterais alaranjadas seguem o dourado central. A ponta direita ? espelhada; apenas o perfil constante da barra ? repetido. PNG original de 1983 ? 793, preservado sem amplia??o ou repintura por c?digo. Barra de 12 pixels l?gicos; arabescos com propor??es uniformes. A joia e o ornamento central V3 ficam intactos. O editor confere a assinatura do prefab antes de salvar: uma c?pia aberta antes de uma altera??o externa n?o pode sobrescrever a vers?o atual. Nesse caso, use **Recarregar**. A instala??o valida que s? quatro imagens e quatro transforma??es do divisor mudaram no prefab atual. Prompt completo e proced?ncia: `production/PETS-DIVIDER-V9.json`; revis?o: `production/review/pets-divider-v9/`.

Feedback V8: contadores numéricos substituídos por bolinhas centralizadas abaixo
de cada lista. A página atual usa dourado; as demais, lilás. As bolinhas são
botões que mudam apenas a categoria correspondente. Os itens têm destaque de
borda e brilho ao passar o cursor; esse hover não altera a seleção, é limpo ao
sair/trocar de página e não é persistido ao salvar o prefab. O editor encaminha
o hover para os mesmos componentes usados no jogo, mantendo seus cliques para
seleção/edição do layout. Capturas e testes: `production/review/pets-feedback-v8/`.
As bolinhas são geometria nativa com gradiente suave, sem PNG ampliado.

Divisor V7: barra e pontas ampliadas em 2,2× para combinar com a espessura dos
arabescos centrais. As pontas mantêm proporção uniforme; só o trecho reto é
repetido. A aplicação altera apenas os quatro RectTransforms e os dois fatores
de amostragem das linhas, preservando as demais edições do prefab atual.
Captura: `production/review/pets-divider-v7/`.

Inventário V6: duas linhas separadas, **Ovos e gemas** e **Itens de aceleração**.
Cada categoria tem páginas de seis espaços completos, setas independentes e
contador de página. As setas ficam desativadas no começo/fim. A troca de página
mantém a seleção e não movimenta a outra categoria. Não há mais scrollbar,
arraste contínuo ou itens cortados na borda. Espaços restantes na última página
aparecem vazios. Os ícones e quantidades continuam demonstrativos.
O layout das páginas e os componentes persistem no mesmo Pets.prefab usado pelo
jogo/editor; só a página aberta é estado transitório. A composição da mesa,
incubadoras, cartões, cabeçalho e divisor V5 foi mantida.

Único PNG novo: seta dourada `pets-page-arrow-v6`, de 930 × 1691, produzida com
ImageGen integrado e espelhada para a esquerda. Fonte e prompt completo:
`production/PETS-INVENTORY-V6.json`. Capturas e validação atuais:
`production/review/pets-inventory-v6/`.

## Histórico V5

Correção V5: o inventário permanece abaixo das incubadoras, em seis colunas,
com **rolagem vertical e scrollbar em pé à direita**. A barra tem trilho pintado,
preenchimento azul e puxador lilás com pata; o puxador percorre todo o trilho.
O fundo `pets-workshop-v5` foi corrigido para colocar a borda traseira da mesa
acima dos pontos de apoio. As bases têm sombra de contato sobre o tampo e os
cartões são desenhados depois delas, em primeiro plano. Não se usa mais o
cartão como apoio visual. As pontas do divisor têm proporção uniforme; só o
trecho reto é repetido, com a emenda encoberta pelos arabescos centrais.
Os PNGs aprovados da incubadora, ovo e joia central continuam preservados.

Fontes e prompts novos: `production/PETS-CORRECTION-V5.json`, gerados com
ImageGen integrado. Fundo de 1254 × 1254, trilho de 2172 × 724 e puxador de
1881 × 836; nenhuma ampliação artificial dos arquivos de origem.

O cabeçalho preservado da V4 reutiliza regiões da **mesma textura da HUD inferior
da Home**, preservando o vermelho, o degradê e o filete dourado. Voltar e ajuda
têm sprites pintados próprios. As abas laterais usam a pata azul/lilás e o ovo
dourado simples com manchas alaranjadas; os rótulos continuam editáveis.
O painel azul-marinho foi substituído por uma versão com borda discreta, sem
o clarão azul no topo. A composição V5 substitui o antigo apoio sobre os cartões.

Fontes, prompts completos e hashes V4: `production/PETS-REFINEMENT-V4.json`.
Foram gerados cinco PNGs com ImageGen integrado: quatro de 1254 × 1254 e o
painel de 1176 × 1337. Pixels e alpha originais foram mantidos; nenhuma imagem
foi ampliada artificialmente para ser chamada de 4K/8K. As regiões do cabeçalho
são sprites nativos que apontam para a textura existente da navegação.
Somente os PNGs de Mascotes usam mipmaps com filtro trilinear para suavizar a
redução até o tamanho do celular. O nível de resolução original é preservado
para o zoom; as importações da Home permanecem iguais.

A revisão atual segue a segunda imagem reenviada pelo usuário: **inventário em
grade no rodapé, três incubadoras maiores, cartões abaixo delas e nenhuma HUD
da Home sobre esta tela**. Essa orientação substitui a antiga coluna à direita.
O painel extra “Escolha um ovo” foi removido. A fonte é Nunito ExtraBold estática,
com controles desenhados para este mock: Acelerar verde com moldura ornamentada e
gemas laterais; Usar Item azul sem dourado. O cabeçalho vermelho tem padrão de
patas e filete dourado fino.

A joia do divisor mantém facetas grandes, brilho nas faces e reflexos violeta,
com os arabescos completos. A peça central tem fonte própria de 2121 × 741 px,
separada das linhas laterais, evitando ampliar uma gema minúscula de uma tira.
Fontes finais e prompts: `production/PETS-V2-ASSETS.json` e
`production/PETS-ORNAMENT-V3.json`. O antigo divisor de losango em V2 foi
substituído pelo conjunto V3. Não foi feita reamostragem artificial dos PNGs.

Abra **Royal Postman > Editor de telas > Mascotes** e use **Recarregar** se a
janela ainda mostrar a composição anterior. O preview inicia em 1080 × 1920 e
renderiza mais pixels ao ampliar ou usar uma tela com maior densidade; o zoom
deixa de esticar uma captura de 360 px. A proporção é preservada no limite do RT.

`RuntimeKit.additionalScreens[pets].hideNavigation` está ativado para reproduzir
o mock. A navegação inferior compartilhada continua editável somente na Home
nas telas que a utilizam. O botão Pet abre Mascotes; voltar retorna à Home.

Receita explícita atual: **Royal Postman > Mascotes > Recriar conforme mock
(substitui Pets)**. Ela substitui somente Pets.prefab. Compilar ou importar não
refaz a composição. Use o editor para alterações manuais e persistentes.

Os PNGs de ovo e incubadora V1 aprovados foram preservados. Ovos continuam ocultos
quando inativos, inclusive nas cápsulas bloqueadas. A captura de demonstração
ativa apenas a primeira cápsula, sem gravar progresso ou consumir itens.

Capturas e validação atuais: `production/review/pets-correction-v5/`. As quantidades de
inventário são exemplos; `SetIncubation`, `UseItemRequested` e
`AccelerateRequested` continuam separados da economia do jogo.

Nunito ExtraBold: fonte estática do repositório Google Fonts, revisão
`c7e2740188205a85323c7385547f6f59b4f2245a`, em `Fonts/Pets/`, com licença OFL.

## Histórico: estrutura visual V1 rejeitada

A tela **Mascotes** era aberta pelo botão Pet da Home. Voltar retorna à Home;
a navegação inferior é o mesmo `Navigation.prefab`, compartilhado e bloqueado
para edição nesta tela. No editor, selecione **Royal Postman > Editor de telas >
Mascotes** para alterar assets e disposição com persistência no prefab real.

## Composição

- Oficina de fantasia com a perspectiva, janela noturna, iluminação âmbar,
  estantes e mesa da referência enviada pelo usuário.
- Três incubadoras à esquerda. O aparelho de vidro e o ovo são sprites
  separados. Todas começam vazias. Só `SetIncubation(..., true, egg, ...)`
  habilita o ovo de uma cápsula desbloqueada; encerrar a incubação o oculta.
- Inventário vertical à direita, com máscara de viewport, rolagem e scrollbar.
  Quatro tipos de ícone exemplificam o conteúdo. As quantidades e os rótulos de
  desbloqueio seguem o estudo visual, não um catálogo/economia aprovado.
- Cabeçalho usa o próprio sprite/material da HUD inferior da Home, com sua
  borda dourada voltada para baixo. Voltar e ajuda têm corpos inteiros; ações
  nas cápsulas mantêm moldura e centro separados para o clique.

## Arquivos

Prefab: `Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab`.

PNG, Sprite e materiais ficam junto de seus componentes:

```
Assets/RoyalAves/Art/UI/Menu/RoyalPostman/Pets/   fundo, aparelho, painel
Assets/RoyalAves/Art/UI/Icon/RoyalPostman/Pets/   ovo e ampulheta
Assets/RoyalAves/Art/UI/Button/RoyalPostman/Pets/ materiais dos botões
```

Cinco PNGs novos foram produzidos com o **image_gen integrado**, usando a imagem
do usuário como referência. Prompts: `production/PETS-V1-PROMPTS.json`; caminhos,
resoluções originais e hashes: `production/PETS-V1-ASSETS.json`. Pixels e alpha
originais foram preservados. Importação 2D, limite 8192, sem compressão/mipmaps.
O limite de importação não transforma as fontes em imagens nativas 8K.

## Interface e integração

`RoyalPostmanPetsScreen` vincula voltar, ajuda, abas, seleção de item e navegação.
`SetIncubation` recebe estado externo, progresso, tempo e sprite do ovo;
`UseItemRequested` e `AccelerateRequested` são os pontos para ligar o sistema de
mascotes. A interface não concede mascotes, desconta itens ou cobra moedas.
Não foi inventado um backend de incubação, inventário ou monetização.

`RoyalPostmanPetsBuilder` é uma receita explícita para a primeira montagem.
**Criar tela (substitui apenas Pets)** substitui a composição de Mascotes;
não execute essa receita sobre edições manuais que deseja preservar. Compilar
ou importar arquivos não refaz a tela, a Home ou a HUD compartilhada.

Capturas reais e validação: `production/review/pets-v1/`. Os testes verificam
cliques por raycast, rolagem, estados vazio/ativo, persistência no editor,
renderização igual entre editor/jogo e preservação da Home/HUD inferior.

## Adaptação ao aparelho (20 de outubro de 2026)

`RoyalPostmanViewport` mantém os controles de Mascotes dentro de `Screen.safeArea`.
O cabeçalho vermelho e o fundo azul continuam até as bordas da tela, inclusive
atrás do notch e do indicador de gestos. A composição mantém a escala uniforme;
em telas baixas cabe inteira, e em telas altas o inventário usa a altura extra.
A primeira fileira usa âncora vertical 0,5 com posição 113; a segunda usa âncora
0 com posição 196. Em 1080 × 1920 isso preserva as posições visuais anteriores.
O inventário usa azul uniforme (10, 23, 47), sem degradê inferior.

O editor e o jogo usam a mesma adaptação. Previews em RenderTexture ignoram a
área segura do monitor; `RoyalPostmanViewport.SafeAreaOverride` permite informar
o recorte do aparelho em pixels para validação. Ao salvar, o editor restaura as
dimensões autorais dos fundos e da raiz para não gravar margens de um aparelho.
Não recrie os prefabs para aplicar essa correção; o instalador seletivo é
`production/apply-pets-device-fit-v16.cjs`.

## Incubadora mobile (V17, botões V18)

A aba Incubadora mostra uma cápsula grande por vez. As setas alternam Comum,
Rara, Épica e Lendária; uma placa na base identifica a raridade e a posição.
Cada tipo tem um desenho próprio. Cápsulas vazias ficam escurecidas; bloqueadas,
ainda mais escuras. O ovo é um sprite independente e aparece apenas quando o
slot está ocupado. Não há estrelas flutuantes dentro das cápsulas.

Os dez PNGs novos e os prompts completos do gerador integrado estão em
`production/PETS-MOBILE-V17.json`, com caminhos, resolução e hash de cada fonte.
Os arquivos originais permanecem intactos. O cenário original é desfocado por
`WorkshopFocus`; o ornamento, a aba Atual e a arte do cabeçalho são preservados.

O rodapé tem quatro tipos de ovo e uma pilha de aceleradores que soma os itens
de 30/60/90/120 minutos. O tooltip detalha cada duração e permite tocar outro item
sem fechá-lo primeiro. Incubar e Acelerar são ações separadas. A Lendária leva
48 horas; os saves antigos migram de três para quatro slots preservando dados.
Os desbloqueios existentes continuam em 0/30/100; o nível da Lendária está no
campo `unlockLevel`, inicialmente 0 (o usuário informou apenas a duração).

Após o pedido de botões mais altos, os controles passaram de 600 × 110 para
600 × 140, com 22 unidades de separação e painel de 480 de altura. A pintura usa
9-slice e escala uniforme nas extremidades para preservar a borda. A moldura
fica fixa, e centro, ícone e texto descem juntos ao pressionar.

A receita explícita atual é `RoyalPostmanPetsMobileBuilder`; para ajustar somente
essa altura use `RefineActionSpacing`. Não execute as receitas históricas V1/mock
sobre esta composição. Revisão mais recente: `production/review/pets-action-height-v18/`.
Os testes usam estoque e carteira injetados e não alteram os recursos do jogador.
