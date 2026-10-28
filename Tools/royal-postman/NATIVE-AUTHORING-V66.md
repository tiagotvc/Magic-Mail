# Edição nativa da incubadora

Use **Royal Postman → Editar incubadora na Unity**, fora do Play Mode.

O atalho abre o `Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab`
real em Prefab Mode, em isolamento. A Scene View entra em 2D com a ferramenta
Rect (atalho T) e enquadra a tela. Não cria uma cópia da montagem nem usa a
janela Editor de telas.

- Selecione os elementos na Hierarchy ou na Scene View.
- Arraste as alças para redimensionar; ajuste posição, tamanho, sprite, cor e
  visibilidade no Inspector normal.
- Para inserir arte, crie **UI → Image** dentro do objeto desejado e arraste
  o Sprite para **Source Image**. A ordem dos irmãos na Hierarchy controla
  a sobreposição; os últimos ficam à frente.
- Salve com Ctrl+S ou com Auto Save do Prefab Mode. As alterações ficam no
  mesmo prefab que o jogo carrega.
- Edite fora do Play Mode. A aba Game mostra o resultado; as alças de edição
  ficam na aba Scene.

As quatro máquinas estão em `Body/Workshop/Incubator0..3`. As camadas ficam
em `Machine`; o botão da patinha fica separado em `PawButton_PLACEHOLDER`.
O fundo, balcão, divisor, prateleira e ovos continuam como objetos separados.
Para ver uma versão inativa, alterne o checkbox dos objetos na Hierarchy,
lembrando que isso também é uma propriedade salva do prefab.

O atalho não altera nem regenera layouts, PNGs, materiais, configurações de
importação ou cenas do jogo. Não roda automaticamente ao importar/compilar.
O teste nativo fica em `production/NativeAuthoringV66Review.cs`, executado
somente no projeto isolado `Library/MapValidation/PreviewProject`.

Referência do fluxo nativo: [Unity — Editing a Prefab in Prefab Mode](https://docs.unity.cn/Manual/EditingInPrefabMode.html).
