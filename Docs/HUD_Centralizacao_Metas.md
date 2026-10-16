# Centralização das metas na HUD

## Correção após teste com quatro metas

O screenshot do usuário mostrou que reduzir apenas a largura das células não reduzia os ícones e contadores internos, que têm dimensões fixas. Substituída a distribuição da HUD por posicionamento simétrico dos itens completos com escala uniforme, limitada a 1: só reduz quando necessário, sem ampliar metas de fases com poucos objetivos. `Goal Side Inset` (padrão 0,12 = 12% por lado, além de 10 unidades de margem) mantém os objetivos afastados das curvas e ornamentos. A escala original de cada item é capturada uma vez para evitar redução acumulativa. O cálculo acompanha mudanças de largura e metas ativas, mantendo as contagens e os destinos de coleta existentes. O HorizontalLayoutGroup é desativado somente na HUD sob TopPanel para não disputar as posições.

Editor: ajuste a área de Target e, se precisar de mais respiro, aumente Goal Side Inset. O padding é relativo ao retângulo Target, não mede automaticamente a parte creme do PNG. Runtime compilado sem erros. Verificação visual final depende de novo teste na cena do usuário.

As seções a seguir descrevem a primeira solução; a distribuição com escala proporcional acima substitui o uso de LayoutElements/HorizontalLayoutGroup na HUD.

`LevelData.GetTargetContainersForUI()` fornece os contadores da fase, ordenados pela quantidade. Quando há outras metas, omite o contador de estrelas. `TargetGUIGroup` ativa os primeiros elementos de sua lista, atribui o sprite e chama `BindTargetGUI`. Cada `TargetGUI` lê a quantidade restante do contador real e exibe o check quando chega a zero. Meta concluída continua ocupando sua posição; não some nem desloca os demais objetivos.

A cena possui seis espaços cadastrados; apenas os necessários ficam ativos. O layout Unity ignora os espaços inativos. A proposta de trabalhar com até quatro metas é atendida pela distribuição, mas esta alteração não impõe limite no editor de fases nem esconde metas excedentes.

## Correção

No grupo de metas sob `TopPanel`, `HorizontalLayoutGroup` usa `MiddleCenter`, margens laterais iguais (10), sem expansão forçada e levando em conta a escala dos filhos. Cada meta recebe `LayoutElement` de largura preferencial uniforme, com flexibilidade zero. A descrição oculta não reserva espaço nem altera o padding para 58/63. A configuração antiga podia expandir os espaços e ainda deslocar o centro com margens assimétricas.

As metas ocupam posições simétricas em relação ao centro de `Target`: uma no centro; duas com um espaço entre si; três com a segunda no centro; quatro em dois pares simétricos. A largura preferencial pode diminuir se o retângulo não tiver espaço suficiente. Os desenhos e textos internos não são redimensionados por esta alteração: o retângulo deve comportar os ícones no maior número previsto de objetivos.

## Ajuste no Inspector

- Centralize o RectTransform **Target** na parte creme destinada aos ícones/contagens, abaixo da placa de título. Sua largura define o espaço horizontal disponível. Sua altura e posição vertical definem a faixa central dos itens.
- Em **Target GUI Group**, ajuste **Goal Slot Width** (padrão 150, antes da escala individual do item) para a largura reservada por objetivo e **Goal Spacing** (padrão 12, nas unidades locais do layout) para a distância entre espaços.
- Ajuste tamanho/posição de imagem e contador dentro de `TargetGUI` quando necessário; o layout controla a posição horizontal e a largura do contêiner de cada meta.
- Ao testar no Play, altere o número de metas na fase, não a posição individual dos contêineres. Para uma ou duas metas, o conjunto fica centralizado sem reservar os espaços das metas ausentes.

Implementação em `Assets/SweetSugar/Scripts/GUI/TargetGUIGroup.cs`. Popups fora de `TopPanel` mantêm a regra anterior. Nenhuma edição da cena montada manualmente foi feita. Compilação runtime verificada separadamente; validação visual em Play Mode ainda necessária com uma, duas, três e quatro metas.
