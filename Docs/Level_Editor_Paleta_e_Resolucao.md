# Paleta por fase e prévia do editor — 22/10/2026

O painel principal do Level Editor agora mostra **Peças desta fase**, com seleção por ícone e mínimo de três opções. A seleção é gravada em `LevelData.pieceColors`, mantendo os IDs das cores para preservar a identidade das metas. Dados antigos sem seleção explícita continuam usando as primeiras `colorLimit` opções. A cópia da fase clona a lista.

O gerador usa a seleção tanto nas reposições quanto nas regenerações; a seleção automática da bomba multicolorida também respeita a paleta. Peças iniciais combináveis fora da paleta são substituídas. A paleta de pincéis normais só mostra opções habilitadas. O editor avisa quando uma meta pede uma peça desmarcada; é necessário habilitar a peça ou ajustar a meta.

Os corações vistos no nível 3 são texturas antigas serializadas em peças normais fixadas pelo template. A prévia agora resolve essas peças pelo ID da cor e pela arte atual, em vez de usar a textura antiga. A aba de blocos oferece **Borracha de peças fixas**; na aba de peças há **Apagar peça** e **Apagar fixas**. O apagador de uma peça funciona mesmo sob caixas, preservando o chão e as camadas dos obstáculos. Nenhum layout de fase foi apagado ou substituído nesta alteração.

O tabuleiro do editor ganhou zoom de 100% a 200%. Em 100%, cada botão mede 50 pixels lógicos e o desenho da peça ocupa 40; em 200%, o desenho ocupa 80. A densidade física depende da escala da tela do editor. O zoom não muda o tamanho das peças durante o jogo.

## Auditoria de resolução

| Arte | PNG original | Importação |
| --- | --- | --- |
| Caixa, três estados | 724 × 724 cada | Máximo 2048, sem compressão, sem mipmaps, sem redimensionamento NPOT, filtro bilinear |
| Envelope rosa, presente, chave, selo, pena e tinteiro | 256 × 256 cada | Máximo 2048, sem compressão e sem mipmaps |
| Envelope azul (`bow.png`) | 1254 × 1254 | Máximo 2048, sem compressão e sem mipmaps |

O pipeline ativo SweetSugarURP está com `m_RenderScale: 1`. Os atlas legados consultados empacotam pastas do SweetSugar, não a pasta da nova caixa. Os PNGs da caixa são recortes dos pixels aprovados, com alpha preservado. Não há redução de 724 para uma textura menor configurada no importador. Traços finos da arte perdem contraste quando o desenho inteiro é amostrado para cerca de 40 pixels no editor; aumentar o PNG sem engrossar o traço não recupera um detalhe menor que um pixel de saída. O zoom permite conferir a diferença. A resolução efetiva do dispositivo/Game View durante Play não foi medida nesta alteração.

## Verificação

Compilação de runtime e editor concluída via MSBuild, registrada em `Logs/level-palette-build.log`. Doze verificações executadas nos assemblies compilados: compatibilidade da paleta antiga, IDs esparsos, limites, IDs repetidos e inválidos, seleção vazia, apagamento sob uma caixa preservando camadas, células aleatórias preservadas, cópia da seleção e sincronização da quantidade. Resultado em `Logs/level-palette-tests.result`. Essas verificações executam métodos gerenciados; a interação visual da janela e uma partida completa ainda precisam ser conferidas no Unity após a recompilação.
