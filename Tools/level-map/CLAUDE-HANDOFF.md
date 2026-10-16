# Handoff para Claude — mapa curvo do Correio Mágico
Data: 13/10/2026.

**Escopo ampliado pelo usuário:** o primeiro mundo é Green Land (200 posições); o segundo é Lava Land (250 posições dedicadas ao vulcânico); o terceiro será pré-histórico, com contagem ainda indefinida. Natal é a primeira season e deve vestir os mundos por sobreposições reutilizáveis. A direção mais recente está em `PLANO-MUNDOS-SEASONS.md` e prevalece sobre a hipótese anterior de uma única campanha de 200 posições. Esta atualização é planejamento; os assets novos do backlog ainda não foram gerados.
Projeto: `C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail`.
Unity: `6000.6.0f1`.
Tarefa autorizada: implementar a evolução do mapa a partir deste projeto, plano e kit, preservando o fluxo do match-3 existente. Este pacote foi preparado para reduzir a necessidade de reenviar a conversa.

## 1. Objetivo do usuário

O usuário quer um mapa de fases com sensação 2.5D, próximo e mais frontal, inspirado na leitura espacial de Candy Crush mas com identidade postal própria. A curva acontece principalmente no sentido do avanço, com horizonte quase horizontal. Objetos nas laterais surgem parcialmente atrás do horizonte e vão se erguendo durante o scroll.

Ele quer ver lateral e topo do mesmo botão conforme a câmera se aproxima, margens mais altas que o caminho, água interrompendo o terreno, pontes com espessura, mudanças reais de cenário, cores cartoon largas, caixas de correio, cachorro e ladrilhos em trechos difíceis. Evitar assets realistas, excesso de detalhe e a vista atual tão distante/de cima.

**Correção de bug obrigatória:** fase distante aparece sobre o telhado de uma casa mais próxima (exemplo: nível 15). Corrigir profundidade e composição, sem substituir o problema por fases inacessíveis escondidas por prédios.

**Último ajuste de arte:** luminária e caixa de correio são referências aprovadas; o usuário prefere seu volume, reflexos e luz suave aos sprites inicialmente chapados. Usar revisões de luz/espaçamento identificadas por `recommended:true` no manifesto. O atlas de quatro texturas de terreno foi rejeitado: não usá-lo. Construir o chão com geometria, faixas de cor e bordas na perspectiva final.

## 2. Leia antes de implementar

Primeiro leia os documentos de expansão:
- `Tools/level-map/PLANO-MUNDOS-SEASONS.md`: três mundos, passagens, animações e encaixes de Natal.
- `Tools/level-map/PLANO-PRODUCAO-ASSETS-MUNDOS.md` e `asset-production-backlog.json`: catálogo futuro, prioridades e contrato de produção; não são arquivos de arte existentes.
- `Tools/level-map/mundos-seasons-roadmap.json` e `BRAINSTORM-LAVA-250-FASES.md`: 250 posições vulcânicas em 50 momentos e regra de numeração proposta.
- `Tools/level-map/PLANO-CLIMA-E-BACKGROUNDS.md` e `environment-weather-roadmap.json`: background próprio por área, vinte perfis, clima animado e transições reversíveis. É requisito do mapa, não um efeito extra opcional.

As referências de implementação/base Green continuam:

1. `Tools/level-map/PLANO-MAPA-200-FASES.md`: direção visual, arquitetura, diagnóstico e aceitação.
2. `Tools/level-map/ASSETS-MAPKIT-V2.md`: arquivos finais, slicing, materiais e limitações.
3. `Tools/level-map/BRAINSTORM-200-FASES.md`: dez regiões e 200 momentos visuais individualizados.
4. `Tools/level-map/brainstorm-200-fases.json`: dados estruturados de autoria, em estado draft.
5. `Tools/level-map/mapkit-v2-image-manifest.json` e `mesh-kit-manifest.json`: inventários.
6. `CLAUDE.md` e eventuais regras do repositório: conferir aplicabilidade e estado real.

O projeto tem muitas alterações anteriores do usuário. Inspecione o status e preserve-as. Não resetar o repositório nem reescrever grandes cenas/prefabs sem necessidade. Esta entrega adicionou arte e documentação; **não migrou o runtime nem corrigiu o bug ainda**.

As referências locais do usuário estão em Downloads:
- `WhatsApp Image 2026-10-13 at 11.09.13 (1).jpeg`
- `WhatsApp Image 2026-10-13 at 11.09.13.jpeg`
- `WhatsApp Image 2026-10-13 at 11.09.14.jpeg`

Na terceira referência, observe a plataforma além do canal, a ponte e a lateral dos níveis distantes. São referências de aparência; não temos evidência da implementação interna daquele jogo.

## 3. Código existente e integrações que precisam sobreviver

O lobby RoyalAves substitui o lobby/mapa original do template. Começar pela integração que já funciona:
- `Assets/RoyalAves/Scripts/Meta/LobbyController.cs`: criação do mapa, retorno, HUD e callback de seleção.
- `CurvedLevelMap.cs`: API `Create(...)`, `Open(...)`, `Close()`, `Visible`, fase atual, seleção e bloqueios.
- `CurvedMapRoad.cs` / `LevelMapProjection`: projeção atual normalizada, não reutilizar a normalização como câmera do novo mapa.
- `CurvedMapTerrain.cs`: terreno contínuo atual.
- `CurvedMapPropImage.cs`: deformação 2D atual.
- `MapLevelDisc.cs`, `MapPropShadow.cs`, `CurvedMapInput.cs`.
- `LevelMapConfig.cs` e `Assets/RoyalAves/Resources/RoyalAvesLevelMap.asset`.
- `Assets/RoyalAves/Editor/RoyalAvesLevelMapPreview.cs`: ferramenta de preview atual.

Leia o callback real em `LobbyController` perto de `CurvedLevelMap.Create`. Continue chamando o pré-nível/carregamento do jogo por essa integração. Não implementar um segundo loader. Preserve `MetaProgress`, `InitScript`, progresso/PlayerPrefs, fases liberadas, replay e a regra de **uma estrela por primeira vitória**. Não importar a economia de três estrelas/coroas das referências.

Há **100 assets Level_*.asset** em SweetSugar Resources/Levels na inspeção. As posições 101–200 são planejamento visual e editorial. Nunca apresentar fase sem tabuleiro como jogável nem liberar progresso artificialmente. Permitir preview editorial desses lugares e estado futuro/bloqueado no jogo. Criação e balanceamento de novos tabuleiros exigem trabalho próprio, não apenas duplicação do mapa.

O escopo novo acrescenta posições vulcânicas propostas 201–450, também sem tabuleiros prontos. Preservar IDs existentes; validar numeração contínua antes de qualquer migração. Quantidade de fases do mundo pré-histórico não foi definida pelo usuário.

## 4. Arquitetura escolhida

Usar terreno, medalhões e pontes de geometria simples, decoração ilustrada em cartões 2D, e o HUD atual sobre a imagem do mundo. A câmera de perspectiva do mapa renderiza para uma RenderTexture exibida por RawImage no lobby.

O URP Asset atual tem apenas Renderer2D. Adicionar um Universal Renderer específico para o mapa à lista existente e selecionar na câmera do mapa com `UniversalAdditionalCameraData.SetRenderer(index)`. Preservar o Renderer2D padrão e a câmera do gameplay. Não trocar o renderer global para tentar fazer o mapa funcionar.

A composição por câmera→RenderTexture é suportada pelo Unity 6.6:
https://docs.unity.com/en-us/engine/6000.6/manual/cameras/urp/multiple/rendering-to-a-render-texture

Implementar camada/culling próprios usando um slot disponível, câmera Base de mapa, resolução proporcional ao painel e lifecycle claro. Desligar câmera, animações e atualizações quando o mapa estiver fechado. Liberar RenderTexture/materiais instanciados ao destruir o componente. Não duplicar EventSystem.

Começar com URP Unlit, cores/gradientes cartoon, sombras de contato e água simples. Medir custo de RenderTexture no dispositivo antes de escolher resolução final. Não depender de reflexões em tempo real ou bloom global.

## 5. Uma curva compartilhada, sem discrepância de clique

Não misturar coordenadas de UI normalizadas com geometria 3D. A curva deve ter um único serviço matemático, usado pelo chão, transform dos objetos, normal do terreno e interação.

Exemplo de primeiro experimento (não parâmetro final):
- `u`: avanço autorado na rota; `cursor`: posição do jogador no mapa; `s = u - cursor`.
- Raio longitudinal `R`, ângulo `theta = s / R`.
- Centro da superfície: `B = (x(u), R*(cos(theta)-1), R*sin(theta))`.
- Normal: `N = (0, cos(theta), sin(theta))`.
- Tangente de avanço: `T = (0, -sin(theta), cos(theta))`.
- Ponto com altura `h`: `B + h*N`.

A largura em X fica praticamente reta; não criar uma cúpula lateral. Trabalhar só na faixa perto da câmera/horizonte, não numa volta inteira do círculo. Deformar os vértices do terreno e manter objetos apoiados no mesmo referencial. Medalhões pequenos podem usar transform rígido orientado pela normal; conferir encaixe. Pontes longas precisam acompanhar a superfície e conectar as plataformas.

Colliders e raycasts devem acompanhar a geometria final. Evitar shader que curva apenas a aparência e deixa a seleção no lugar antigo. Se usar shader depois, oferecer seleção equivalente com a mesma matemática.

Teste enquadramentos de câmera baixos, por exemplo inclinações de 10/14/18 graus e FOV de 30–40 graus. Ajuste altura, distância, raio e escala juntos olhando screenshots. Meta: 5–7 níveis legíveis, personagem/fase atual no terço inferior e decoração grande. Não normalizar de novo a imagem entre primeiro plano e horizonte: isso anula parte do efeito de baixar a câmera.

## 6. Medalhões e profundidade

Use `postal-level-token.obj` como volume inicial. Face roxa, aro dourado, lateral creme. Coloque número TMP sobre o topo local, com pequeno afastamento para evitar z-fighting, e material que respeite profundidade. O texto não deve ser um overlay que atravessa casas. Preserve destaque e legibilidade da fase atual.

No horizonte aparece mais a lateral; perto aparece mais o topo e o número. Deixe a perspectiva/curva produzir isso, sem uma imagem por número. Transição de estado (bloqueado, disponível, concluído, selecionado) preserva identidade e acessibilidade.

Cartões 2D sólidos precisam de alpha clipping + teste/escrita de profundidade adequados; transparência comum sem cuidado pode repetir o bug. O contorno claro pertence à silhueta. Se um halo suave exigir transparência, separá-lo do corpo sólido. Não adicionar bias que mande todos os níveis para a frente.

Reserve espaço livre na rota. Teste bounds projetados de casas/árvores contra medalhões legíveis em vários avanços. Mova/remova/reduza decoração em colisão visual. Oclusão correta de um objeto distante pode ocorrer; a fase atual e fases acionáveis próximas devem continuar claras e clicáveis. Composição e profundidade resolvem juntas.

## 7. Chão, água, ponte e acabamento

Use as malhas fornecidas como peças iniciais, não como mapa final repetido inteiro. O corredor fica abaixo das margens; mostrar a face de borda com cor mais escura e topo discretamente claro. Faixas largas claras/escuras seguem o mundo. Manchas pequenas são autoradas e esparsas. Não aplicar o atlas rejeitado nem repetir quatro quadrados como textura do mapa.

Um canal é ausência de chão com água abaixo. A ponte tem topo, lateral e apoio; suas extremidades encontram plataformas reais. Evitar apenas pintar água por cima de uma faixa contínua de grama.

OBJ não tem UVs finais nem materiais automaticamente ligados. Atribuir os materiais por grupo/filho e gerar UVs quando necessários. Recolorir com paleta do capítulo. O modelo de ladrilhos destaca alguns nós difíceis sem marcar todas as fases como difíceis. Dificuldade do JSON é proposta de ritmo visual, não classificação validada dos tabuleiros.

## 8. Sprites e efeito de levantar

Importação e recortes estão nos .meta e no manifesto. Use somente `recommended:true` para a coleção final. Normalize escala e ajuste pivô para o pé real de cada sprite antes de posicionar.

O pivô permanece no chão. Oriente cartão usando o referencial local do terreno, com orientação lateral controlada para a câmera. Calcule inclinação extra de levantar pela distância/visibilidade com easing e reversibilidade, nunca por evento único. Evite rotação completa que revele as costas vazias de uma pintura ou billboard que elimine a relação com o chão.

Duas poses da caixa de correio podem abrir/fechar ocasionalmente. Quatro poses do cachorro são gestos, não um ciclo de animação perfeito. Pés de poses diferentes precisam ser alinhados. Se fizer cauda contínua, separar a peça ou criar intermediários; não alternar tamanho do cachorro.

As construções atuais são de um único ângulo. Para prédios centrais do cenário, prefira volume simples com fachada pintada ou produza vistas coerentes do mesmo desenho; não esticar uma fachada para fingir um teto. Use marcos grandes nas laterais, não no caminho dos níveis.

## 9. Scroll e interação

Mantenha inércia, foco no nível atual e navegação até regiões anteriores. Clique e arraste não podem disparar seleção simultânea.

Converter pointer no RectTransform real do RawImage para UV, respeitando uvRect/crop e escala; gerar raio com a câmera do mapa; selecionar somente collider de marcador válido e disponível. HUD/popups têm prioridade. Bloquear seleção durante arraste/inércia conforme a regra já existente.

Resize/orientação/safe area atualizam painel, aspect e RenderTexture. Testar 9:16 e aparelhos mais altos. Não guardar coordenadas de clique antes do redimensionamento.

## 10. Conteúdo e autoria de 200 posições

Esta seção descreve o **primeiro mundo Green Land**. O roadmap mais recente acrescenta Lava Land com dez regiões de 25 posições, sem reduzir as 200 posições Green. Não repetir/recolorir simplesmente a mesma composição por 250 fases: seguir os 50 momentos autorados do roteiro vulcânico.

Separar dados e apresentação. Estrutura proposta: campanha → capítulos → segmentos → rota → marcadores/props. Um spline autorado ou lista de pontos suave permite planejar ilhas e pontes. Não gerar tudo por uma única senoide e hash.

JSON de brainstorm contém dez capítulos de 20 níveis, quatro momentos principais de cinco níveis por capítulo, terreno, transição, observação visual e dificuldade sugerida. Use como ponto de partida para ScriptableObjects/editor:
1–20 Bairro das Primeiras Cartas; 21–40 Vila dos Selos; 41–60 Bosque dos Recados; 61–80 Canal das Encomendas; 81–100 Oficina dos Carimbos; 101–120 Campo das Pipas; 121–140 Porto das Cartas; 141–160 Serra do Correio; 161–180 Jardins do Relógio; 181–200 Capital das Correspondências.

Tenha tool/editor para percorrer campanha, trocar cursor, mover props, visualizar zonas livres e conferir conexões de terreno. Instanciar somente trechos visíveis e margem, com pooling. Cores/silhuetas da próxima região aparecem antes da transição, e os objetos surgem aos poucos.

## 11. Ordem de execução e entregas verificáveis

1. Inspecionar estado real, referências, assets e fluxo de seleção. Compilar antes de migrar.
2. Criar piloto híbrido de 8–10 posições com medalhão volumétrico, margem elevada, um canal/ponte, luminária, caixa e cachorro; reaproveitar UI/loader.
3. Comparar screenshots de três avanços do mesmo trecho e vídeo/GIF de ida e volta. Ajustar proximidade, horizonte, luz, legibilidade e bug do telhado antes de expandir.
4. Integrar saves, replay, vitória/retorno e lifecycle. Preservar a campanha jogável de 100 níveis.
5. Construir autoria/pooling e três regiões de prova: bairro, bosque, canal.
6. Expandir a campanha visual para os dez capítulos e 200 posições com variação de terreno, elenco e transições do brainstorm.
7. Registrar o que ficou como arte exclusiva futura e tabuleiros futuros 101–200; não alegar 200 fases jogáveis.

Etapas de expansão previstas: decoração Natal por sockets, rio de cartas, nuvens, fonte estática, fábrica e gestos; piloto Lava com céu laranja, vulcão/fumaça separados, gêiser e ponte sobre lava; Natal Lava; expansão visual 201–450. Produzir os assets do backlog em lotes antes de integrar cada família, registrando o que existe de verdade. Não ativar a season Natal só porque está planejada. O `SeasonProps` existente trabalha em coordenadas de tela; criar adaptador de mundo para acompanhar curva/profundidade. Preservar seleção de season existente e fallback do tema base. Veja o plano mestre para contratos e prioridades.

Não exigir nova autorização para passos rotineiros desta implementação. Fazer o piloto e continuar os passos dependentes quando ele cumprir os critérios visuais/técnicos; pedir preferência só se surgir uma decisão material que não esteja resolvida aqui.

## 12. Verificação e conclusão

Pedido mais recente: fundo e clima precisam mudar com a área. Implementar camadas separadas de céu/horizonte/sol/nuvens, chuva, raios de sol, vento, tempestade e variações coerentes por mundo. Usar bindings de ambiente ao longo da rota, com blend de no máximo dois perfis, reversível ao scroll. Preservar fontes estáticas e integrar Natal por variantes compatíveis. A produção dessas novas camadas ainda é backlog; consultar o documento de clima antes de gerar/copiar fundos.

- Compilação runtime/editor e importação sem assets quebrados.
- Seleção de disponível/concluído; bloqueado e futuro não carregam tabuleiro inexistente.
- Arraste não joga; popups bloqueiam o mundo; retorno de vitória foca progresso correto.
- Mesmos botões mostram lateral/topo em posições diferentes.
- Margem mais alta, ponte conectada, água com interrupção real.
- Casa à frente do nível 15: ordem correta, números não atravessam telhado, fases próximas continuam acessíveis.
- Scroll reversível sem troca brusca de pose/escala, pé flutuante ou sombra no vazio.
- Horizonte lateral quase horizontal, enquadramento próximo/frontal.
- Acabamento alinhado com luminária/caixa, sem realismo/excesso de detalhes; atlas rejeitado ausente.
- Duas áreas mostram backgrounds/silhuetas e climas diferentes; ida/volta/teleporte resolvem ambiente correto sem acumular emissores ou cobrir níveis/HUD.
- Profiling no aparelho e layout em dois aspectos, qualidade/resolução ajustadas com evidência.
- Entregar imagens/vídeo, arquivos alterados, testes feitos e limites restantes.

Existe projeto isolado de preview em `Library/MapValidation/PreviewProject`, criado por `Tools/level-map/prepare-preview.cjs`, para a implementação 2D atual. Ele usa facades de saves apenas para preview; não copiar esses substitutos para o jogo. Atualizar o preview para a nova arquitetura/URP se for usado. Não fechar a Unity principal apenas para fazer screenshots. Não declarar aprovação visual/desempenho com base só em compilação.

## Prompt curto para iniciar

> Leia Tools/level-map/CLAUDE-HANDOFF.md e os documentos que ele referencia no projeto Magic-Mail. Implemente o mapa híbrido curvo descrito, começando pelo piloto e validando a aparência antes de expandir para as 200 posições planejadas. Use os assets finais MapKitV2, com a luminária/caixa como referência de luz, descarte o atlas de texturas rejeitado, corrija profundidade e sobreposição dos níveis e preserve loader, saves, HUD e economia existentes. Entregue screenshots e vídeo de scroll, verificações e limites reais. As fases 101–200 são posições editoriais futuras até existirem tabuleiros válidos.

Para a expansão seguinte, leia primeiro `PLANO-MUNDOS-SEASONS.md`: Green 200 + Lava 250 + pré-histórico futuro, Natal por camadas e novos assets/animações em lotes. O catálogo dessa expansão está planejado, não gerado; não declarar famílias/450 fases prontas antes de produzi-las e validá-las.

