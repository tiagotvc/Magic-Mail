# Editor de telas

Abra **Royal Postman > Editor de telas** (também disponível em Royal Aves).
A Home usa o mesmo prefab e a mesma montagem de viewport do jogo. Não existe
uma cópia do layout desenhada pelo editor.

- Selecione um elemento no preview ou na lista. Arraste para mover; as setas
  ajustam 1 unidade, ou 10 com Shift. O Inspector permite editar posição,
  dimensões, âncoras, cores, fontes e componentes visuais.
- Troque a imagem pelo campo **Imagem (PNG ou Sprite)**. O PNG permanece separado
  do texto, da moldura e das outras peças do controle.
- **Salvar automaticamente** vem ativado. Também há Salvar, Recarregar, Undo/Redo,
  resoluções de preview, zoom e exportação PNG. As alterações são gravadas no
  prefab real da tela. Edite fora do Play.
- Valores de moedas, vidas e fase são fornecidos pelo jogo. O editor salva a
  aparência e a posição desses textos; o valor de exemplo é substituído ao jogar.

## Navegação inferior compartilhada

A HUD inferior é editada **somente na Home**. Ao salvar a Home, o editor atualiza
`Navigation.prefab`, usado como prefab aninhado na Home. Outras telas cadastradas
recebem a mesma navegação automaticamente, bloqueada no editor. Seus prefabs não
gravam cópias ou alterações locais da HUD inferior.

Mascotes usa o rodapé com inventário do mock, sem a barra da Home. Essa exceção
é configurada por `Hide Navigation` no cadastro da tela; a edição da barra
compartilhada continua restrita à Home.

Home e Mascotes estão cadastradas. O botão Pet da Home abre Mascotes.
Veja [Tela de mascotes](PETS-SCREEN.md). **Cadastro de telas** seleciona
`RuntimeKit.asset`; a lista `Additional Screens` permite acrescentar ID, título
e prefab de uma próxima tela. Os IDs devem ser únicos; `home` é reservado. Uma
nova tela usa `RuntimeKit.InstantiateScreen(id, parent)` para receber a mesma
montagem do editor. Cadastrar um prefab não altera sozinho as rotas do jogo.

## Assets

Os componentes visuais deste kit ficam em:

```
Assets/RoyalAves/Art/UI/
  Button/RoyalPostman/    corpos e peças de botões
  Tab/RoyalPostman/       barra e peças da navegação inferior
  Icon/RoyalPostman/      retrato, moldura e símbolos
  Menu/RoyalPostman/      fundos, painéis e ilustrações
```

Cada categoria mantém PNGs, Sprites e Materials correspondentes. A migração
preserva os GUIDs, mantendo as referências dos prefabs. O mapa de movimentações
está em `production/UI-FOLDERS-V16.json`. Prefabs continuam em
`Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/`.

A importação aceita fontes de até **8192 px**, sem compressão, mipmaps ou redução
NPOT. Esse limite não adiciona detalhes às fontes existentes: os PNGs V15 têm
1254 × 1254 px, exceto o painel de 2172 × 724 px. Não houve reamostragem ou nova
geração de arte nesta alteração.

Não execute **Recriar kit (substitui prefabs)** sobre a Home editada. Essa receita
antiga substitui a composição; o editor de telas salva diretamente os prefabs
autoriais.
