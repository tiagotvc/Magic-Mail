# Editar a HUD manualmente na Unity

O editor visual atual está em **Royal Postman > Editor de telas**. Ele usa o
prefab real, salva automaticamente e centraliza a edição da navegação inferior
compartilhada na Home. Consulte [Editor de telas](SCREEN-EDITOR.md). O modo
Prefab abaixo continua disponível para edição direta.

Abra **Royal Postman > Editar menu principal**. O atalho abre `Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Home.prefab` no modo Prefab. Edite fora do Play para trabalhar com o arquivo salvo.

- Use RectTransform para posição, tamanho e anchors; Image para sprites e cores; Text para aparência da tipografia.
- `Navigation/Tab0` até `Tab4` contêm os ícones, a camada da seleção, o texto e o marcador. O componente RoyalPostmanNavigation preserva os RectTransforms por padrão. A opção Resize Selected Icons ativa o comportamento automático antigo.
- Nos materiais dos controles circulares, **Button color** muda a cor preservando o acabamento original. FixedShoulder permanece parado; MovingCenter contém a face e o símbolo que se deslocam no clique.
- Para o avatar, Portrait e Frame são independentes. As outras peças também podem ser selecionadas individualmente na Hierarchy.

Salvar o prefab atualiza o modelo usado na próxima criação da home. O script continua vinculando cliques e atualizando moedas, vidas, nível e avatar do jogador; esses valores de jogo substituem os textos de exemplo. Preserve os nomes e caminhos dos objetos usados por RoyalPostmanLiveHome ao reorganizar a estrutura.

Importar imagens ou recompilar scripts não recria os prefabs. O reparo de sprites também preserva o layout. A ferramenta explícita **Royal Postman > 2026 > Fabricação > Recriar kit (substitui prefabs)** fabrica novamente os modelos e substitui o layout; não faz parte da edição manual. O modal tem seu próprio atalho **Royal Postman > Editar modal de play**.
