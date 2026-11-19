# Royal Postman — Light Ball Premium VFX (Unity)

Analise antes o sistema atual de Light Ball, render pipeline, sorting e tweening. Modifique somente os VFX, sem alterar a lógica de escolha de cor, a lista dos alvos, a destruição das peças ou a duração de gameplay sem autorização.

## Objetivo visual

Três fases, iguais ao conceito entregue: (1) carga prismática da Light Ball com brilho radial, pulso e dois arcos orbitais rotativos; (2) feixes mágicos brancos/dourados, com halo violeta/rosa, que conectam a Light Ball às peças da mesma cor, em trajetórias suaves/curvas; (3) clarões pequenos em cada alvo, com círculos de choque e partículas coloridas simples, seguidos por um pop final no centro. Não usar fogo, fumaça nem nuvens cartoon. Não recriar a arte da Light Ball.

## Máscaras PNG

As 14 máscaras do diretório `Mascaras_PNG` são imagens RGBA brancas, com variação apenas de alpha. Use a cor de cada SpriteRenderer/material (não edite as texturas para tingi-las). Importar como Sprite (2D and UI), Alpha is Transparency ligado, Compression apropriada, Filter Mode Bilinear, mipmaps off para HUD. Configure PPU de acordo com o sistema atual.

- `01_ball_core_glow`: base de carga luminosa, atrás da Light Ball
- `02_ball_pulse_ring`: anel de choque escalável
- `03_ball_orbit_ellipse`: aro elíptico rotativo (pode ficar parcialmente atrás da bola)
- `04_ball_orbit_arc`: arco orbital parcialmente aberto
- `05_ball_charge_flare`: brilho central de carga
- `06_connection_beam_core`: miolo claro do feixe, textura horizontal UV contínua
- `07_connection_beam_halo`: brilho largo do feixe, colorizável roxo/rosa
- `08_connection_ribbon_wave`: fita sinuosa fina de conexão
- `09_connection_ribbon_secondary`: segunda fita para entrelaçar
- `10_target_precharge_halo`: aura em cada peça-alvo
- `11_target_preflash`: pré-clarão rápido em cada alvo
- `12_target_pop_burst`: explosão localizada da peça
- `13_final_central_burst`: flash final coordenado da bola
- `14_scattered_twinkling_stars`: pequenos brilhos residuais

## Implementação dos feixes — muito importante

Cada feixe deve sair da posição REAL da Light Ball e chegar ao centro da peça-alvo. **Não estique um sprite reto como única solução**. Crie uma spline Bézier por alvo, atualizada nos casos em que as posições mudem, com malha 2D de fita (`Mesh`) e UV ao longo da spline (ou um sistema de trail equivalente que respeite a curva). Utilize `06` e `07` com UV alongado, 08/09 como trilhas alternativas/decorativas. A conexão deve percorrer a spline (reveal parcial) e ter pontinha luminosa móvel; deixe algumas fases de offset de tempo entre os alvos para evitar aparência mecânica. Para UI Canvas, respeite UI material/CanvasRenderer; para SpriteRenderer use material unlit 2D compatível com pipeline. Additive/screen somente quando disponível; alpha-blended deve funcionar sem Bloom.

## Animação sugerida (ajuste ao pacing já existente)

- 0–180 ms: Light Ball cresce ~1.05x, pulso radial e arcos iniciam rotação pequena; não piscar agressivamente
- 160–470 ms: raios partem do centro e conectam aos alvos da mesma cor, cada um com atraso de 10–35 ms; raios claros com halo colorido e largura bem controlada (~0.10–0.18 da célula no núcleo)
- 380–650 ms: alvos brilham, pop coordenado com pequena variação escalonada de tempo, fragmentos locais mantidos do sistema atual
- 550–760 ms: pulso final na Light Ball e anéis de dissipação, sem saturar a tela

`Tween` no tempo local e cancelamento de tweens/timers quando board mudar, animação interrompida ou estado de jogo reiniciar.

## Regras

- Não modifique cores de peças não envolvidas; somente os alvos da cor selecionada devem acender.
- Não desenhe no topo do HUD: os efeitos pertencem à sorting layer do tabuleiro e ficam corretamente atrás ou à frente da peça conforme etapa.
- Use object pooling; limite de partículas simultâneas ajustável, e animação escalável para celulares fracos.
- Exponha parâmetros no Inspector: timings, intensidade, alvo, largura do feixe, duração, cor do glow, número máximo de sparkles, amplitude da curva.
- Com 2 Light Balls combinadas, ofereça um preset para a interação existente, não substitua o algoritmo do gameplay.
- Entregue scripts C# compiláveis, prefabs/cenas de preview utilizáveis e lista dos arquivos alterados. Teste pelo menos 1 alvo, múltiplos alvos, reset e troca de fase.
