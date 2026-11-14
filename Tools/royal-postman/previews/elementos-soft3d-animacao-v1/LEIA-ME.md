# Seis elementos — acabamento 3D das animações

Escopo solicitado: somente caixa de madeira, pedra rúnica, tinteiro, poções, bolsa do carteiro e sinalizador. Cofre, escaninho, fita, power-ups e demais efeitos não foram alterados.

Esta entrega é uma **prévia animada de revisão**, em uma cena nativa isolada do Unity. Não substitui ainda os componentes/prefabs da partida. Não carrega níveis, campanhas, metas ou progresso. As treze entradas do Level Editor permanecem como instaladas.

## Visualizar

- `index.html`: player do vídeo com repetição e links.
- `previa-seis-elementos.mp4`: render nativo 1200×800, 24 fps, 7 segundos.
- `previa-seis-elementos.gif`: versão leve para comparação.
- Unity: **Royal Aves / Elementos / Remodelagem 3D / Previa das seis animacoes**. Janela independente, pausa, reinício e controle de tempo; não exige Play nem modifica a cena aberta.

## Estudo visual

- Caixa: três estágios; tábuas destacadas com textura da nova arte; fragmentos de madeira com espessura.
- Pedra: impacto e pedaços sólidos cinza-lilás, sem decalque flutuante da pata.
- Tinteiro: cinco estados de dano, gotas violetas e fragmentos de cerâmica/dourado.
- Poções: suporte separado, quatro frascos independentes, vidro e gotas das quatro cores, quebra final do suporte.
- Bolsa: dezesseis quadros sem alça, base registrada por medida da região opaca; três pergaminhos abrindo e saindo em curva; fechamento.
- Sinalizador: cobre preservado durante a troca da lente, ícones reais do prefab atual, três indicadores de resistência e fragmentação.

Os fragmentos são malhas com frente, chanfro e espessura; sua iluminação acompanha a rotação. Gotas usam superfícies arredondadas com luz superior esquerda. Não são a animação de quebra de um atlas.

## Fontes e limites

Artes derivadas das seis propostas de `../elementos-soft3d-v1/`. PNGs originais do ImageGen preservados em seus caminhos de origem. `prompts.json` e `cleanup-prompts.json` registram prompts e fontes. As tentativas de limpeza do primeiro atlas de tinteiro mantiveram aura e foram rejeitadas; a prévia usa a nova faixa de cinco estados, cuja origem está no campo `inkRetry`. A política de alpha do shader da prévia elimina franjas de baixa opacidade sem regravar os PNGs.

Animações são uma demonstração de acabamento e movimento, não testes de regras reais. A migração de produção exigirá adaptar os pivôs/âncoras do tinteiro, encaixes dos frascos, estados e controles dos componentes atuais; não basta substituir PNGs. Não há alegação de validação de toque, áudio, dispositivo ou campanha nesta revisão.

Código: `Assets/RoyalAves/Editor/Soft3DAnimationLaboratory.cs`; shader exclusivo da prévia `Assets/RoyalAves/Shaders/Soft3DPreviewLens.shader`.
