**Atualização:** [ajuste do padrão dos assets](AJUSTE-PADRAO-ASSETS.md), com [capturas atuais](previews/polish/asset-fit-review.html). As famílias de cenário, pontes e navegação abaixo foram substituídas pelas versões cartoon-v2.

# Mapa — revisão pelas referências de 13/10/2026

[Comparação e capturas da Unity](previews/polish/reference-review.html).

## Mudanças

- **Visão:** espaçamento de níveis de 2,3 para 1,65 m, câmera fixa durante a rolagem, mais níveis visíveis e nível atual mais perto da parte inferior. O enquadramento mantém a altura do horizonte e do nível atual em telas mais altas. Fase e lateral das posições manuais preservadas.
- **Chão:** quatro materiais pintados independentes, faixas largas e textura discreta acompanhando o mundo. Rebaixo suave com borda clara e sombra interna leve substitui o contorno escuro. Céu, nuvens, vulcões e fumaça continuam separados.
- **Água:** margens onduladas com filete claro, borda física baixa e reflexos curtos. A parte de trás do mundo não desenha água sobre o horizonte.
- **Pontes:** tábuas verdes, madeira com amarrações e troncos. Travessias diagonais com tábuas transversais; modelos alternados ao longo da rota.
- **Cenário:** árvores redondas, pinheiros e árvores douradas; moinho postal, quiosque, lanterna e flores. LavaLand usa flora térmica, cristais, caixas de correio, luminárias, forja e construções vulcânicas.
- **Níveis:** faces em vidro turquesa, violeta e azul, aro dourado, lateral creme, sombra e volume à distância. Selos postais identificam a dificuldade editorial; estrelas e coroas usam o progresso existente.
- **HUD:** painel superior em menta, avatar entre vidas e moedas, valores escuros, configurações visíveis e navegação compacta em lavanda com cinco ícones novos. Os três botões grandes saem da visão 3D; tocar num nível abre a fase e a aba Agência continua disponível. “Meu nível” aparece quando é necessário voltar ao nível atual.

## Edição e integração

O arquivo `Assets/RoyalAves/Resources/RoyalAvesWorldMapLayout.asset` não foi regravado. O editor mantém arraste, troca de sprite, tamanho, inclusão, remoção e salvamento. Uma escolha explícita de sprite fica fixa. A HUD original da agência é restaurada ao sair do mapa.

Reabra **Royal Aves > Mapa de níveis > Pré-visualizar mundo curvo** depois da recompilação. **Prévia do chão** alterna GreenLand/LavaLand numa cópia temporária do kit. Permanecem as 100 fases jogáveis e as 200 posições editoriais; esta revisão não cria as futuras fases de LavaLand.

## Artes e prompts

Criadas com a **ImageGen integrada**, em `Assets/RoyalAves/Art/MapKitV3/`:

| Arquivo | Conteúdo |
|---|---|
| `Shared/painted-ground-materials.png` | Quatro materiais de chão |
| `Shared/postal-bridge-variants.png` | Três pontes |
| `GreenLand/postal-scenery-variety.png` | Seis peças de cenário |
| `Shared/premium-level-faces-v2.png` | Três faces de nível |
| `Shared/postal-navigation-icons.png` | Cinco ícones da navegação |
| `Shared/postal-difficulty-badges.png` | Dois selos de dificuldade |

[Prompts completos, fontes e destinos](reference-style-assets.json). Os recortes são sub-sprites independentes na Unity. Os pixels originais foram preservados. As imagens de referência copiadas para a comparação estão somente em `Tools/level-map/references/2026-10-13/`, fora dos assets do jogo.

## Validação

- Unity 6000.6.0f1 numa cópia isolada; editor principal mantido aberto.
- Câmera constante em 597 amostras e **5.020/5.020 seleções corretas** em três proporções de tela.
- Edição equivalente sem pixels acima da tolerância de 4/255 (máximo 1/255); nove âncoras de arraste e respeito a posição, tamanho e sprite manual verificados.
- 35 sprites com apoio no solo, dois objetos distantes inclinados com sombra e seis níveis distantes com volume. Margem livre antes das travessias.
- HUD: valores e ações preservados; retângulos, sprites e cores restaurados após fechar e reabrir.
- Capturas de GreenLand, LavaLand e telas estreita/larga conferidas. Sem teste em aparelho físico ou partida completa nesta rodada.

Relatórios em `previews/polish/`: `validation.txt`, `layout-validation.txt`, `edit-mode-validation.txt`, `presentation-validation.txt` e `hud-validation.txt`.
