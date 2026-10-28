# Bancada apoiada V64

Correção pedida em22/10/2026: a plataforma oval isolada aparentava flutuar. A referência mostra piso/tampo contínuo ao redor da plataforma, avançando até a barra dourada. Novo PNG conecta piso e plataforma num único suporte, com sombra de contato e limite inferior reto.

Arquivo: Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsLayeredV63/counter-supported-v64.png. Sprite nativo: Sprites/counter-supported-v64.asset na mesma pasta. Gerado com ImageGen integrado, usando as duas imagens da mensagem como referências. PNG preservado sem alterações de pixels; recorte do Sprite feito nativamente no Unity.

Apenas dois componentes foram alterados no prefab principal: Image e RectTransform de Body/Workshop/Counter. Novo tamanho1650x235, posição local(0,-1045), para o tampo continuar além das laterais visíveis e fechar sob a divisória. Todas as demais edições atuais do usuário preservadas por comparação semântica. O prefab atual foi lido antes da aplicação; nenhum builder histórico foi executado na tela principal.

[Captura real corrigida](production/review/pets-counter-v64/corrected-720.png). Três posições foram renderizadas no Unity isolado; escolhida1045 após inspeção visual. Sessão do usuário não operada.

Original: C:/Users/Tiago Carvalho/.codex/generated_images/01a15af4-fedb-77d3-b750-1054318794bd/exec-7d73515c-89d0-46d1-b153-fe6ab0992bec.png

## Prompt

```text
Production game-art PNG, true transparent background. Use the TWO attached screenshots as references: image1 is the INCORRECT current assembly with an isolated oval slab appearing to float; image2 is the CORRECT construction to reproduce.
Create ONLY the TERRACOTTA COUNTER SURFACE AND ITS LOW RAISED ROUND CENTRAL PLATFORM, as ONE connected ground asset. The small round platform MUST REST ON A BROAD CONTINUOUS STONE-TILED COUNTERTOP that extends to BOTH LEFT AND RIGHT EDGES and all the way to a STRAIGHT HORIZONTAL BOTTOM EDGE. The countertop is a broad receding horizontal plane, not a second isolated disc or a hanging shelf. Its visible warm clay surface surrounds the raised round platform, with clean perspective tile joints continuing behind/alongside the platform and toward the foreground edge. The round platform has a LOW short front wall of a few large rounded terracotta bricks. A tight contact shadow joins that wall to the countertop. No visible gap or transparent space underneath the platform. The lower counter foreground edge is straight and full width, ready to meet a separate gold divider.
Match image2's simple grounded spatial construction: platform modestly raised, top surface under an incubator footprint, surrounding counter stretching out to screen sides. Match image1's existing warm orange-red terracotta colors and premium clean rounded cartoon painting. Smooth broad shading/highlights, few large stone shapes, no grains/stains, no wood texture or shiny realistic metal.
Landscape canvas roughly3:1, front-facing slight top-down perspective like the screenshots. Whole connected tabletop/platform is isolated on genuine alpha; the only transparent area is ABOVE the upper silhouette. THE BOTTOM EDGE MUST BE SOLID CONTINUOUS TERRACOTTA ACROSS THE WHOLE CANVAS WIDTH, not curved upward at outer ends or transparent under an ellipse. Platform centered horizontally, occupies about70% width; counter visible on both sides. No floating detached shadows, no extra round levitating slab.
Absolutely NO incubator, machine, glass, paw/button, placeholders, UI, gold bar, text/labels, eggs, bricks-wall background, arch, plants, game controls or decoration. We are replacing ONLY the ground beneath the machine. Do not render a complete screen or a comparison image.
```

