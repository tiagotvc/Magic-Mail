# Botão de incubar e barra de tempo

O pedido atual autoriza produzir e aplicar estes controles, substituindo os
placeholders cinza anteriores. As artes novas vieram do ImageGen; os PNGs
originais ficam intactos em `Assets/RoyalAves/Art/UI/Menu/RoyalPostman/IncubatorControlsV70/`.
Os prompts e os caminhos originais estão em `production/INCUBATOR-CONTROLS-V70.json`.

As quatro incubadoras usam a mesma montagem independente:

```
Body/Workshop/Incubator0..3/PawButton_PLACEHOLDER
  Image                    # aro fixo e encaixe âmbar
  Button                   # mesma ligação de incubação
  RoyalPostmanPaintedButton # desloca apenas MovingCenter
  MovingCenter
    Label                  # antigo texto Botão, desativado
    RaisedFace             # centro dourado elevado, PNG separado
    PawIcon                # patinha existente, imagem independente
```

O centro desce8 unidades e reduz para96% ao pressionar, retornando ao soltar
ou retirar o ponteiro. O aro fica parado. As regras de ovos, desbloqueio e
incubação continuam controlando a disponibilidade no jogo. O estado
Interactable do prefab permanece ligado para edição.

Na barra, `Available/TimeBar_PLACEHOLDER` contém a moldura dourada com fundo
azul petróleo. `TimeRemaining` é um Text independente, mostrando duração,
tempo restante ou Pronto conforme o estado. `Fill_PLACEHOLDER` mantém o Image
que recebe fillAmount do código existente; seu filho `FillVisual` desenha o
preenchimento azul com9-slice, mantendo as pontas arredondadas. O
RoyalPostmanProgressClip usa a largura do pai quando useParentWidth está
ligado; o comportamento anterior de largura fixa continua como padrão.

Edite o conjunto em **Royal Postman → Editar incubadora na Unity**.
Os tamanhos e posições externos dos dois controles foram preservados.
As artes podem ser trocadas e as peças movidas e redimensionadas normalmente.

Ferramentas de produção (não executam automaticamente):

- `production/stage-incubator-controls-v70.cjs`: snapshot atual e dependências
  no projeto isolado.
- `production/IncubatorControlsV70Build.cs`: montagem nativa isolada.
- `production/merge-incubator-controls-v70.cjs`: preserva IDs e componentes
  alheios aos dois controles; rejeita conflito com edição simultânea.
- `production/IncubatorControlsV70Review.cs`: pressão, clique, tempo,
  preenchimento, redimensionamento, importação e capturas nativas.

Revisão e capturas: `production/review/incubator-controls-v70/`.
