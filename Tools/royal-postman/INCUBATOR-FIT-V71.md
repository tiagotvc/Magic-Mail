# Encaixe do botão e cores — V71

O usuário ampliou e reposicionou manualmente as camadas da incubadora e pediu
que esses ajustes fossem preservados. Depois escolheu o centro vermelho,
usando o vermelho padrão do jogo, com o aro dourado combinado com a máquina.

A base é o Pets.prefab salvo depois da confirmação do usuário. As posições,
tamanhos, escalas, sprites e cores de Machine, Lid, GlassFront e BaseFront são
preservados. O botão agora é filho de `Machine/BaseFront`, com âncoras na
região útil do corpo metálico, acompanhando movimentos e mudanças de tamanho
da base sem controlar ou travar as ferramentas nativas. Fica circular, com escala uniforme e uma
sombra curta de contato. O centro continua saliente, separado e animado;
somente centro e patinha afundam ao clicar. A patinha usa sua arte creme,
garantindo contraste com o centro vermelho.

As cores usam o shader nativo existente RoyalPostman/UI/NeutralPalette em
dois materiais independentes, sem alterar PNGs ou gerar imagens novas:

- `Assets/RoyalAves/Art/UI/Menu/RoyalPostman/IncubatorControlsV70/GoldMatchedV71.mat`
  controla o dourado do aro.
- `Assets/RoyalAves/Art/UI/Menu/RoyalPostman/IncubatorControlsV70/RedMatchedV71.mat`
  controla o centro vermelho. O tom médio238,4,21 foi medido na placa
  Incubadora existente; a faixa de reflexo segue a borda superior da placa.

O centro e a patinha continuam sob MovingCenter. O aro é o Image do próprio
`Machine/BaseFront/PawButton_PLACEHOLDER`. Os dois materiais podem ser
ajustados no Inspector. As referências existentes aos Buttons são preservadas.

O encaixe usa a área central do recorte BaseFront: o intervalo opaco do corpo
metálico da máquina comum foi medido nos pixels1216..1422 da imagem original
1024x1536, dentro do recorte inferior435px. A posição é calculada nas
coordenadas atuais de cada base, respeitando a montagem salva. A hierarquia
e as âncoras nativas mantêm o encaixe; nenhum script reposiciona o botão em runtime.

Ajustes de status são aplicados somente se o texto sobrepuser a barra. A
última posição salva pelo usuário é mantida quando já está abaixo dela.
Barra, textos, inventário, cenário e demais controles não são reconstruídos.

Produção e verificação nativas acontecem no PreviewProject isolado:
`IncubatorFitV71Build.cs`, `IncubatorFitV71Review.cs`,
`stage-incubator-fit-v71.cjs` e `merge-incubator-fit-v71.cjs`.
A instalação preserva IDs e todos os componentes fora dos controles
explicitamente ajustados, rejeitando conflitos com edições simultâneas.
Capturas e auditoria ficam em `production/review/incubator-fit-v71/`.
