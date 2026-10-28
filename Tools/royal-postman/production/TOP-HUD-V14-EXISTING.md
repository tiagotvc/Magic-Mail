# Controles com os assets existentes

Autoridade visual indicada pelo usuário: `Assets/RoyalAves/Art/UI/Buttons/fechar.png` e `engrenagem-plana.png`. As novas gerações foram descartadas. Os PNGs usados são cópias byte a byte dos originais, sem pintura nova ou edição raster:

- `Art/RoyalPostmanUI2026/top-round-existing-v14.png`: original fechar.png.
- `Art/RoyalPostmanUI2026/top-plus-symbol-existing-v14.png`: original fechar.png, com recorte nativo de Sprite para o símbolo. O X é isolado no material e rotacionado em 45 graus para virar +.
- `Art/RoyalPostmanUI2026/top-gear-existing-v14.png`: original engrenagem-plana.png.

O material da UI troca o vermelho por verde ou roxo, preservando o creme. Na base, a área do X é preenchida em tempo de renderização com quatro amostras livres da face original. A borda e os reflexos originais são preservados. O contorno vermelho original da engrenagem acompanha o roxo. FixedShoulder e MovingCenter continuam separados. O parâmetro Button color pode ser editado no Inspector.

Revisões nativas: `review/top-hud-v14.png`, `review/top-hud-v14-pressed.png` e `review/top-controls-v14.png`. São capturas de prefab, não da sessão Play já aberta do usuário. A aprovação visual continua com o usuário.

A HUD pode ser editada manualmente pelo menu Royal Postman. [Instruções de edição](../MANUAL-AUTHORING.md).

Validação: fabricação/captura nativa `ROYAL_POSTMAN_KIT_OK`; integração `ROYAL_POSTMAN_BINDINGS_OK`, incluindo cliques, centro móvel, RectTransforms manuais após troca de aba e visibilidade manual após esconder/mostrar a home. Compilação das assemblies reais de Runtime e Editor: exit 0, em saídas isoladas. O instalador `install-existing-controls-v14.cjs` alterou somente 9 Images e 3 RectTransforms de símbolos no Home existente, preservando os demais blocos e verificando SHA-256 dos PNGs contra os originais. Os prefabs individuais e materiais também foram instalados.
