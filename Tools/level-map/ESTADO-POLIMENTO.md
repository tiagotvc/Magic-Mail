# Mapa — revisão de 13/10/2026

O chão e o céu são arquivos independentes. O terreno verde e o vulcânico têm a região acima do horizonte transparente; nuvens, vulcões, fumaça e raios ficam em camadas próprias. As demais áreas usam gradientes de céu separados, sem nuvens incorporadas no fundo. As capturas da revisão vêm do código de produção renderizado na Unity.

A revisão posterior às 14 novas referências está em [visão, chão, pontes, cenário e HUD](REVISAO-REFERENCIAS-2026-10-13.md). As capturas e especificações abaixo documentam a rodada anterior.

## Ajustes desta rodada

- Correção posterior do LavaLand: os vulcões acompanham a altura da curva do horizonte em sua posição lateral, com 30% da base ocultos atrás do mundo. A fumaça parte da cratera de cada sprite e continua independente.
- Rebaixo com coordenadas próprias em metros: filete externo claro e parede interna sombreada acompanham as laterais e a ponta elíptica sem depender da cor do tema. Conferido também no trecho 89–93 e em três proporções de tela; [captura nova](previews/polish/lava-horizon-fix_89.00.png).
- Rebaixo mais discreto, borda lateral clara e sombra mais marcada na ponta arredondada. A malha continua esférica e subdividida; a câmera permanece fixa.
- Um metro de gramado livre entre o término arredondado da trilha e a borda da ilha. O espaço da travessia foi ampliado para manter o último botão dentro da trilha e a largura do rio.
- Ponte com deck ilustrado independente: tábuas verdes, bordas, relevos e rebites. Sua malha acompanha a curvatura do mundo.
- Casa refeita com telhas roxas arredondadas, porta verde-água, dourado e o mesmo acabamento dos demais sprites. Referências antigas à casa são resolvidas para a nova arte sem regravar as posições salvas.
- Sprites distantes voltam a se deitar na superfície e se erguem suavemente ao aproximar. A sombra não desaparece por causa dessa inclinação.
- Apoio dos objetos corrigido: contorno claro recortado apenas junto à base, pequeno encaixe no solo e sombra de contato. Coroas, estrelas, neve e cartas flutuantes não recebem o recorte da base.
- Botões com lateral mais espessa no horizonte e topo ainda visível. Atual verde-água, concluídos roxos, próximos lilás; detalhes dourados, estrelas e coroas continuam separados.
- Chão vulcânico com basalto em tom ameixa, trilha coral/apricot e água alaranjada. Céu, três vulcões e seis camadas de fumaça animadas são independentes do chão.
- Editor mantém seleção por mapa ou lista, arraste, posição, tamanho individual, inclusão, remoção e desfazer. O cachorro continua removido.

## Revisar na Unity

Abra **Royal Aves > Mapa de níveis > Pré-visualizar mundo curvo**. Reabra a janela após recompilar. **Prévia do chão** alterna GreenLand/LavaLand usando uma cópia temporária do kit; não muda o tema salvo do jogo. A prévia vulcânica demonstra terreno e atmosfera com as decorações atuais.

Em **Editar objetos**, use **Selecionar objeto** para escolher inclusive peças escondidas, ajuste posição/tamanho e clique em **Salvar disposição**. O jogo lê esse mesmo arquivo em Resources. As posições existentes não foram substituídas; os agrupamentos que resultam da escala maior continuam editáveis.

- [Galeria](previews/polish/review.html).
- [Scroll em 33 capturas](previews/polish/scroll.html).
- [Ponte e margem livre](previews/polish/bridge_17.40.png).
- [Prévia do chão vulcânico](previews/polish/lava-layers_1.00.png).
- [Guia de edição](EDITAR-MAPA.md).
- [Assets ativos](ASSETS-V3.html).

## Arquivos e prompts

Novos arquivos em `Assets/RoyalAves/Art/MapKitV3/`:

| Pasta | Arquivo | Uso |
|---|---|---|
| GreenLand | `ground-green-layer.png` | Chão verde, sem céu |
| LavaLand | `ground-lava-layer.png` | Chão vulcânico, sem céu ou vulcões |
| Shared | `sky-golden-layer.png` | Céu sem objetos |
| Shared | `warm-cloud-layers.png` | Três nuvens recortadas |
| LavaLand | `lava-cloud-layers.png` | Três nuvens/fumaças recortadas |
| GreenLand | `green-post-office-polished.png` | Casa nova |
| GreenLand | `green-bridge-deck.png` | Deck da ponte |
| Shared | `level-faces-magic-mail.png` | Três faces dos botões |

Gerados pela ferramenta **ImageGen integrada**. Prompts completos, fontes e destinos: [camadas, casa e ponte](mapkit-v3-records/ground-house-bridge-layers.json), [faces dos botões](mapkit-v3-records/level-faces-magic-mail.json). As imagens compostas anteriores são histórico e não aparecem no catálogo ativo.

O manifesto mantém 57 imagens e 130 subassets, incluindo histórico. O catálogo ativo mostra 113 sprites, 9 fundos e 2 terrenos. Folhas de nuvens e botões têm sub-sprites independentes na Unity.

## Validação

Compilado e capturado numa cópia isolada da Unity 6000.6.0f1. O editor principal permaneceu aberto.

- Câmera constante em 597 amostras e 4.086/4.086 seleções corretas em três proporções de tela.
- Edição: zero pixels acima da tolerância de 4/255 entre reconstruções equivalentes, seis pontos de arraste verificados; posições, tamanho individual e dados do cenário respeitados.
- Apresentação: 28 sprites com máscara de apoio; três objetos distantes inclinados com sombra; oito botões distantes com espessura; margem livre em todas as travessias.
- Camadas: céu separado, três sprites de vulcão e seis fumaças com posições animadas verificadas; transparência dos terrenos validada por leitura de pixels.
- Metadados e kit validados copiados para o projeto; arquivo de disposição manual preservado por comparação SHA-256.

Relatórios: `previews/polish/validation.txt`, `layout-validation.txt`, `edit-mode-validation.txt`, `presentation-validation.txt` e `geometry-report.txt`. A auditoria conservadora ainda marca sobreposições na composição manual antiga ampliada; as peças não são reposicionadas automaticamente. Não foi executada uma partida completa nem um teste em aparelho físico.

O jogo mantém suas 100 fases reais e a rota editorial de 200 posições. A prévia de terreno vulcânico não cria as 250 fases jogáveis planejadas para LavaLand.
