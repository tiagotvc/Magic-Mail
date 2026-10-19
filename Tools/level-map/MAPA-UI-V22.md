# Mapa e navegação — v22

O mapa passa a usar sua própria HUD e navegação. A interface antiga da agência fica oculta ao abrir o mapa, trocar de aba e voltar. A aba Mapa ocupa a primeira posição. Tocar numa fase mantém o caminho existente até `InitScript.OpenMenuPlay`, sem criar outra modal.

O chão usa peças de cor com largura, inclinação e quinas diferentes. A trilha tem contraste mais evidente; os dois lados do gramado têm padrões independentes e mais suaves. Não há uma faixa escura contínua atravessando trilha e gramado. As paletas de cada área e o degradê da entrada continuam preservados.

A barra inferior tem cinco células simples, roxo mais luminoso, reflexo suave na borda e divisórias discretas. Somente a aba selecionada mostra texto e eleva o ícone. Os cinco sprites têm silhuetas grandes e assimétricas.

O arraste e a roda usam a direção corrigida, com inércia e limites. Um arraste não abre a fase. A câmera está mais próxima. A prévia do Simulator em edição renderiza quando necessário, sem manter a câmera ativa continuamente. Os materiais de contorno dos números são compartilhados; o fechamento da prévia libera mundo, câmera e materiais.

As árvores aprovadas e os prédios existentes foram ligados ao mapa real. Não foram redesenhados. Os três arquivos de autoria de ilhas e posicionamento mantêm seus hashes anteriores.

## Conferência

- [Comparação das três paletas](previews/navigation/review.html)
- [Captura verde no Unity](previews/navigation/simulator-map.png)
- [Captura roxa](previews/navigation/floor-lavender.png)
- [Captura vulcânica](previews/navigation/floor-volcanic.png)
- [Resultado da validação](previews/navigation/validation.txt)

Compilação do assembly de runtime: zero erros e avisos. Validação em uma cópia isolada do projeto: seleção, arraste, roda, inércia, limites, alternância de abas, contadores, cinco ciclos de fechar/abrir, ausência da HUD antiga, ausência de duplicação e liberação da prévia. Zero erros do Unity e zero avisos de instanciação de material nessa execução. As três capturas foram inspecionadas visualmente.

Para carregar as alterações numa sessão que já estava em execução, pare e inicie o Play novamente. O editor principal não foi fechado nem teve seu Play alterado automaticamente.

## Assets

Ícones em `Assets/RoyalAves/Art/MapKitV3/MapUI/nav-*-v22.png`, criados com o **ImageGen integrado** e copiados sem alteração de pixels. Os importadores do Unity usam sprites individuais com transparência.

[Prompts completos e arquivos de origem](map-ui-v22-assets.json) · [Verificação dos sprites](previews/navigation/icons-validation.json)

Chão: `Assets/RoyalAves/Art/MapKitV2/Shaders/MapSurface.shader`.
Barra e HUD: `Assets/RoyalAves/Scripts/Meta/WorldMap/WorldMapHudPresentation.cs`.
