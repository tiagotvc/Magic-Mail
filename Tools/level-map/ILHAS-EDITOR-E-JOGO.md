# Ilhas no editor e no jogo

**Atualização v4:** o tamanho agora é uniforme, com cinco ilhas de 20 fases. Veja [biomas, curvatura e luz](BIOMAS-CURVATURA-E-LUZ-V4.md) para os controles e ambientes atuais. As informações de configuração individual de tamanho abaixo descrevem a versão anterior.

O mapa é contínuo, composto por ilhas configuráveis. A janela **Royal Aves → Mapa de níveis → Pré-visualizar mundo curvo** e o mapa aberto pelo lobby usam o mesmo `WorldMapView`, o mesmo arquivo de ilhas e a mesma disposição de objetos. O mapa antigo do Sweet Sugar já é desligado por `LevelManager.EnableMap` quando o lobby do Magic Mail está presente; a configuração do lobby seleciona agora explicitamente o mapa curvo.

## Editar

1. Escolha a ilha na lista **Ilha**. A câmera vai ao começo dela.
2. Abra **Chão, fundo e clima desta ilha**.
3. Ajuste o nome, as posições reservadas para fases, o tema e a paleta. **Personalizar cores** permite escolher chão lateral, trilha e água/lava. A textura opcional usa o atlas de quatro quadrantes dos materiais atuais; para mudanças de cor, use as paletas ou os controles de cor.
4. Escolha um **Background separado** ou as duas cores do céu. As nuvens e os vulcões continuam em camadas independentes. É possível trocar as nuvens e ligar/desligar os vulcões por ilha.
5. Escolha o clima (limpo, vento, chuva, tempestade, neve ou brasas) e sua intensidade.
6. Use **Salvar ilhas e ambiente**. Os objetos continuam sendo ajustados por **Editar objetos** e **Salvar disposição**.

**+ Nova ilha** acrescenta 20 posições depois da última ilha configurada. Ela pode ser preparada antes de suas fases existirem. Alterar seu tamanho muda o começo das ilhas seguintes; os objetos manuais continuam presos à fase e à lateral já salvas. Não há reordenação ou remoção automática de fases.

## Fases reais

O catálogo lê os arquivos `Level_N` em `Resources/Levels`, a mesma pasta utilizada pelo editor de fases. São 100 fases neste momento. O jogo mostra somente os IDs que existem e mantém o carregamento original da fase, progresso e estrelas. Excluir uma fase remove seu botão; um buraco na numeração é ignorado ao procurar a próxima fase existente.

Ilhas vazias são navegáveis na prévia para montar o cenário. No jogo, a navegação fica limitada às fases reais. Se houver mais fases que posições planejadas, o ambiente da última ilha se estende até elas; o editor avisa para você acrescentar ilhas. A rota não tem mais o limite fixo de 200 posições.

As travessias anteriores foram mantidas ao criar a configuração inicial: 17 ilhas abrangem as 100 fases existentes. Uma ilha nova começa na fase 101. O arquivo de objetos manuais não foi migrado ou substituído.

## Dados

- `Assets/RoyalAves/Resources/RoyalAvesWorldMapIslands.asset`: ordem, tamanho e ambiente das ilhas, incluídos no build por Resources.
- `Assets/RoyalAves/Resources/RoyalAvesWorldMapLayout.asset`: objetos e ajustes manuais existentes.
- `WorldMapRoute`: deriva posições e pontes da ordem e do tamanho das ilhas.
- `WorldMapTerrain`: mantém materiais independentes nas ilhas visíveis ao mesmo tempo.
- `WorldMapAtmosphere`: aplica o fundo e o clima da ilha em foco, separadamente do chão.

## Verificação

Validação em projeto Unity isolado, sem fechar o editor principal. Os IDs de teste são extraídos dos arquivos reais do editor de fases; os tabuleiros e saves não são alterados. Os relatórios e capturas ficam em `previews/polish/islands-*`. A comparação da prévia com o renderizador do jogo usa o mesmo tamanho e o mesmo instante de animação. Não foi feito build em aparelho físico nesta etapa.
