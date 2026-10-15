# Primeiro acabamento de neve glossy

Revisão posterior: [Inverno v34](INVERNO-V34.md) reduz a cobertura do chão, retira as coberturas genéricas e acrescenta as luzes existentes e gelo nos galhos. Este documento registra a primeira versão.

Implementado para o chão ilustrado, o **arbusto coral v15** e o **pinheiro clássico glossy v29**. As imagens originais e as posições autoradas são preservadas. Outros objetos continuam usando as coberturas anteriores; este primeiro conjunto serve para avaliar o acabamento antes de ampliar o catálogo.

## Ver no editor

Na janela do mapa, escolher **Preview de season → Natal / neve sobreposta**. Isso mostra o chão nevado e os objetos que seguem a temporada, sem trocar a temporada ativa do jogo.

Em **Editar objetos**, selecionar um dos dois assets e usar **Adicionar tema**:

- **Da temporada (Natal: gelo)** acompanha o Natal.
- **Gelo** força a cobertura só naquele objeto, inclusive no mapa verde.
- Desligar **Adicionar tema** deixa aquela peça sem a decoração.

O espelhamento e a profundidade continuam nos controles existentes. A neve acompanha a mesma transformação da peça.

Para o jogo, **Editar seasons e camadas do mapa** abre o editor de temporadas. Em **Camadas da season no mapa**, **Neve sobre os objetos** ativa o tratamento e **Cobertura de neve no chão** ajusta sua quantidade. A cobertura é atualizada nos materiais quando o valor muda. Um atlas de chão explicitamente escolhido na temporada conserva seu tratamento anterior.

## Acabamento

O chão recebe neve branco-creme com sombras frias e áreas de grama expostas. A trilha fica azulada e conserva as diagonais suaves; as faixas laterais continuam independentes. A linha de contato tem sombra azulada e uma pequena crista clara. Tudo usa coordenadas do terreno, evitando que a pintura deslize durante a rolagem.

As coberturas dos objetos são PNGs separados, encaixados no enquadramento original. O arbusto recebe quatro pequenas capas sobre os volumes grandes; o pinheiro recebe três faixas numa única camada. A malha registra a imagem ao sprite original antes de aplicar espelhamento, animação e profundidade. `WorldMapKit.paintedSnow` guarda as relações entre cada sprite original e sua cobertura; novas relações podem ser adicionadas sem mudar as posições do mapa.

## Arte e reprodução

Ferramenta: **image_gen integrado**, com transparência. Os PNGs selecionados foram copiados sem alterar seus pixels.

- `Assets/RoyalAves/Art/MapKitV3/Christmas/snow-shrub-glossy-v33.png`
- `Assets/RoyalAves/Art/MapKitV3/Christmas/snow-pine-glossy-v33.png`

Prompts, referências e origens: [snow-glossy-v33.json](snow-glossy-v33.json). Instalador: `node Tools/level-map/install-snow-v33.cjs`. Dimensões e hashes: [art-validation.json](previews/snow-v33/art-validation.json).

## Verificação

Compilação do runtime com zero erros. `SnowV33Validation.Run` captura o código real em projeto Unity isolado a 1080×1920 e 1080×2280. Confere os sprites originais, registro das coberturas, espelhamento, ativação manual e sazonal, remoção de tema, reutilização dos slots e atualização da quantidade de neve. Também compara a quantidade de pixels nevados nas capturas com cobertura baixa e alta. Cenas, layouts e a temporada ativa do projeto principal não são salvos pelo teste. Não foi medido desempenho em aparelho físico.

Capturas e resultado: [previews/snow-v33](previews/snow-v33/validation.txt).
