# Nitidez de UI — V65

Pedido de22/10/2026: PNGs aparentando pouca nitidez no editor, especialmente o novo botão vermelho de fechar criado pelo usuário.

## Diagnóstico verificado no Unity isolado

- Border original1350x1374: carregado como RGBA32, mas sem mipmaps e filtro bilinear.
- Center original1160x1160: carregado como BC7 com perda, sem mipmaps e filtro bilinear.
- Importação declarava CompressedHQ; UI assets estava fora da política anterior de importação de RoyalPostman.
- O sprite interno Knob dos placeholders tem textura64x64 e região útil40x40; ampliá-lo mantém baixa definição. É provisório, para substituir pela arte do usuário.
- Ajustar desenhava uma textura intermediária de pelo menos1080x1920, depois a reduzia outra vez para a área visível (aprox497x884 na comparação).

## Correção aplicada

Nova RoyalPostmanUserSpriteImportPolicy restrita a Assets/RoyalAves/Art/UI/UI assets/: importação sem compressão com perda, mipmaps com filtro box e filtro trilinear para redução adequada de imagens grandes. Preserva textureType, spriteMode, GUIDs, dimensões, bordas e pivôs. Não converte Sprites em Default Texture, o que quebraria referências diretas. Configurações atuais aplicadas aos11 PNGs dessa pasta; bytes dos PNGs não alterados.

PreviewRenderSize passa a renderizar no tamanho físico exibido, considerando Fit/100%/200% e pixels por ponto do monitor, com limites de textura existentes. Evita reduzir novamente toda a tela no modo Ajustar. Alteração só do editor, sem mudar escala/tamanho/posição do prefab.

Não foram redesenhados botões, movidos elementos ou alterado o prefab. Placeholder cinza continua provisório.

## Evidência

- [Comparação visual](production/review/ui-sharpness-v65/index.html).
- [Auditoria antes/depois](production/review/ui-sharpness-v65/audit.txt).
- [Validação](production/review/ui-sharpness-v65/review.txt).

Testes nativos: importação dos11 PNGs; identidade do Sprite e dimensões preservadas;100%/200%/HiDPI/Fit; posição normalizada, picking/seleção após alteração da densidade e ausência de mudanças no prefab. Imagens da comparação são renders nativos. Não houve geração de imagem ou alteração dos arquivos PNG.

