# Biomas, curvatura e luz — v4

## Ilhas de tamanho uniforme

As 100 fases estão divididas em cinco ilhas de 20 fases: Jardim das Cartas (1–20), Bosque Encantado (21–40), Costa dos Cristais (41–60), Serra da Aurora (61–80) e Vale Termal (81–100). O tamanho é um parâmetro único do mapa; uma ilha adicionada no fim usa o mesmo tamanho. A configuração anterior foi arquivada em `backups/islands-before-uniform-v4.asset`. Os objetos manuais continuam nas fases e laterais salvas.

## Controles no editor

Em **Royal Aves → Mapa de níveis → Pré-visualizar mundo curvo**:

- **Curvatura das laterais** controla a curvatura transversal do chão e a orientação de objetos, sombras e medalhões. O arraste usa a mesma superfície. O padrão é 1,45, contra 1,0 anteriormente; a curvatura longitudinal do horizonte continua independente.
- **Chão, fundo e clima desta ilha** contém tema, paleta, cores, textura, fundo, nuvens, clima e meteoros.
- **Fases por ilha (todas)** altera o tamanho comum a todas as ilhas.
- **Luz e pós-processamento**, **Brilho suave** e **Vivacidade** são parâmetros salvos e usados pela câmera do jogo. A HUD fica fora dessa câmera.
- Use **Salvar ilhas e ambiente**. A disposição dos objetos usa seu botão de salvamento próprio.

## Acabamento e movimento

O jardim recebeu uma família nova de seis sprites, com verdes mais ricos, sombras coloridas e reflexos quentes. As três áreas novas têm seis sprites próprios cada: árvore, construção postal, luminária, caixa de correio, arbusto e detalhe pequeno. As variantes conhecidas dos sprites anteriores são atualizadas em apresentação, mantendo posição e tamanho manuais.

O terreno lateral recebeu bases de cor mais fortes. Pequenas cavidades pintadas ficam nas bordas internas da trilha, entre medalhões, mantendo o caminho central livre. As luminárias e os gêiseres têm luz de contato suave. O gêiser tem quatro frames com o mesmo recorte e escala para manter a base estável. O Vale Termal usa três gêiseres por ilha e meteoros no céu. Os vulcões do horizonte foram removidos da renderização.

Os gêiseres entram na seleção de objetos quando você ativa a edição daquele trecho. A posição editada passa a ser salva; remover um deles registra sua remoção para ele não reaparecer nem duplicar na geração automática.

A atmosfera usa duas camadas reutilizáveis: na troca de ilha, céu e nuvens entram e saem lateralmente com uma transição de 0,8 segundo. O clima também faz transição de intensidade. O bosque tem vaga-lumes; a serra tem aurora e neve. O chão e a HUD continuam separados da atmosfera.

## Pós-processamento e UI 2D

O mapa usa uma câmera URP que desenha para uma RenderTexture transparente, exibida por um RawImage. Essa câmera recebe bloom discreto e correção de cor, com HDR e processamento de alpha. O Volume tem alcance local e layer próprio do mapa; as câmeras do tabuleiro e da HUD não recebem esse perfil. O perfil em Resources mantém os efeitos referenciados no build. A UI em Screen Space Overlay desenhada depois da câmera não recebe esses efeitos diretamente.

Referências: [renderizar uma câmera para RenderTexture](https://docs.unity.com/en-us/engine/6000.6/manual/cameras/urp/multiple/rendering-to-a-render-texture) e [alpha e pós-processamento no URP](https://docs.unity.com/en-us/engine/6000.5/manual/render-pipelines/universal-render-pipeline/urp-reference/universalrp-asset).

## Artes e prompts

Artes geradas com o **ImageGen integrado**, com transparência original preservada; não foi usado o modo CLI. [Prompts completos, referências de estilo e origem de cada arquivo](biome-v4-assets.json).

- [Jardim](../../Assets/RoyalAves/Art/MapKitV3/GreenLand/garden-vivid-v4.png)
- [Bosque](../../Assets/RoyalAves/Art/MapKitV3/ForestLand/forest-postal-v4.png)
- [Costa](../../Assets/RoyalAves/Art/MapKitV3/CoastLand/coast-postal-v4.png)
- [Aurora](../../Assets/RoyalAves/Art/MapKitV3/AuroraLand/aurora-postal-v4.png)
- [Frames do gêiser](../../Assets/RoyalAves/Art/MapKitV3/LavaLand/thermal-geyser-v4.png)

## Verificação

Capturas e validações no projeto Unity isolado, sem fechar o editor principal nem alterar os saves. O relatório `previews/polish/biome-validation.txt` cobre as cinco áreas, curvatura e seu salvamento, cliques, arraste, transparência da câmera com pós-processamento, gêiseres, transição de céu e meteoros. Ainda não foi medido o desempenho em aparelho físico.

Foram verificados 300 cliques e 24 âncoras de arraste em três valores de curvatura. `biome-effects.txt` confirma que o stack da câmera renderizada recebeu bloom 0,18 e saturação 7. `islands-parity.txt` compara também os vértices, cores e câmera da prévia e do renderizador do jogo; a comparação de imagem considera luminância e alpha, pois o resolve HDR apresenta pequenas diferenças cromáticas nos reflexos mais claros.
