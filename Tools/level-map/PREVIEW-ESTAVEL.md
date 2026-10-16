# Preview do mapa: objeto oculto sem material

`WorldMapView.Pose` pode ocultar um cartão atrás da curvatura antes de atribuir seu material. O evento do Papai Noel percorria esses cartões e acessava `sharedMaterial.mainTexture` sem verificar se estavam prontos, interrompendo o desenho da janela com uma `NullReferenceException`.

`WorldMapSantaEvent.HostVisible` agora ignora cartões ocultos, sem renderer, com renderer desativado ou sem material. A identificação de arbustos e as aparições continuam funcionando.

Validação: 54 renderizações nos trechos 1, 21, 41, 61, 81 e retorno ao 1, avançando o relógio até 60 segundos por trecho; 20 quadros com Papai Noel ativo; zero erros do Unity. Também foram simulados um cartão oculto sem material e um cartão ativo sem material. O preview aberto foi conferido visualmente. Nenhuma cena, configuração de ilha, layout ou progresso do jogador foi salvo pelo teste.

Para repetir no editor aberto, copie `MapPreviewStabilityValidation.cs` temporariamente para `Assets/RoyalAves/Editor`, aguarde a compilação e crie `Temp/map-preview-stability.request`. O resultado fica em `Temp/map-preview-stability.done`, `Tools/level-map/preview-stability.txt` e `previews/stable-preview-*.png`. Remova a cópia temporária e seu `.meta` após o teste. O teste não encerra o editor.
