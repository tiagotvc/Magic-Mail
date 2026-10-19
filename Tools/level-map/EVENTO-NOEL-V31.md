# Evento de surpresa no mapa

Os dois Noéis são humanos, ficam de frente para o jogador e aparecem somente atrás de arbustos. O simpático usa vermelho; o do mal usa roxo. A arte aprovada foi prolongada para baixo, sem usar as variantes de ombros musculosos. A parte inferior do desenho fica escondida pela copa do arbusto.

No editor do mapa, abrir **Evento surpresa de Natal** para configurar ativação, tempo até aparecer, duração, intervalo e os dois textos. Os botões **Testar Noel simpático** e **Testar Noel do mal** forçam uma aparição se houver um arbusto visível elegível. Desligar **Editar objetos** para clicar no Noel no preview. Se não houver arbusto elegível, o evento espera; árvores e construções não são usadas.

O clique na parte visível abre um balão e informa qual variante foi encontrada por `CurvedLevelMap.SantaEventCaught(bool evil)`. Por enquanto não inicia outro modo de jogo. Arrastar, fechar o mapa ou acabar o tempo esconde o personagem. A oclusão da copa também bloqueia cliques; a área transparente do sprite não responde.

## Arte

Arquivos finais em `Assets/RoyalAves/Art/MapKitV3/Library/Personagens/`:

- `santa-friendly-front-v31.png`
- `santa-villain-front-purple-v31.png`

Gerados/editados com a ferramenta integrada **image_gen**, com transparência preservada e cópia dos arquivos originais sem retoque de pixels. Prompts finais, referências e origens: [santa-v31.json](santa-v31.json). Instalador: `node Tools/level-map/install-santa-v31.cjs`. As entradas existentes na biblioteca apontam para as versões frontais; as imagens de corpo inteiro anteriores são preservadas.

## Chão

O shader `MapSurface` usa bordas diagonais retas na trilha, alternando a inclinação entre uma e outra como no desenho do usuário. A trilha tem contraste suave; a grama possui faixas próprias, mais largas, com posição e espaçamento independentes em cada margem. As faixas da trilha terminam na borda, sem continuar na grama. Manchas ovais e grupos esparsos de pontinhos ficam presos às coordenadas do terreno. As oito paletas continuam usando o mesmo tratamento e as cores autoradas são preservadas.

## Verificação

`SantaV30Validation.Run` roda em `Library/MapValidation/PreviewProject`, um projeto isolado. Capturas e resultado: `Tools/level-map/previews/santa-v30/`. A validação não salva cenas, ilhas ou layouts do projeto principal. Captura oito paletas e os dois Noéis, testa exclusão de árvores/construções, aparição, captura única, mensagem por variante, seleção normal de fases, expiração, arrasto e reabertura. Não mede desempenho em aparelho físico.
