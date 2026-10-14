# Tinta Mágica — primeira etapa

**Direção substituída:** o usuário pediu para manter este piso como grama, usando `grass` e `grass-sparse` do projeto inicial. A especificação atual está em [Grama.md](Grama.md). O restante deste documento registra a proposta anterior.

As regras do laboratório foram validadas pelo usuário. Esta etapa produz os dois estados visuais próprios; o guia de produção continua como referência.

- Piso fixo com uma ou duas camadas. As peças podem ocupar, atravessar e sair da célula normalmente.
- Match na própria célula ou impacto de especial remove uma camada exposta. Match apenas vizinho não limpa tinta.
- A meta conta células completamente limpas: tinta grossa → fina não contabiliza; fina → limpa contabiliza uma unidade.
- Um pacote colocado acima absorve o dano. O impacto que termina o pacote não atravessa para a tinta. A tinta continua na célula após a animação e permite a entrada de peças.
- O protótipo conserva a proteção nativa contra impactos repetidos em 0,2 segundo. A política de múltiplos especiais na mesma resolução ainda precisa de playtest.
- Nenhuma fase da campanha é alterada por este laboratório.

O laboratório tem quatro células de tinta com resistências 1, 2, 1 e 2; a última fica sob um pacote de duas vidas. Objetivos: quatro células limpas e um pacote removido. Ativar em **Royal Aves → Elementos → Tinta Mágica → Ativar laboratório para próximo Play**; abrir uma fase após entrar em Play. O painel permite impactos diretos e vizinhos sem usar boosters.

Visual próprio: dois PNGs RGBA em `Assets/RoyalAves/Art/Obstacles/InkFloor/`, gerados pela ferramenta integrada `imagegen`. O estado 02 é uma poça violeta contínua com volume baixo e brilho suave; o estado 01 conserva a posição e a escala, com menos volume, tom mais suave e falhas transparentes de limpeza. O estado grosso também representa a tinta no objetivo.

Os estados usam canvas quadrado, pivô central, bilinear, sem mipmaps e sem compressão. O canvas ocupa no máximo 1,95 unidades locais; com escala 0,42 do Square, isso equivale a 0,819 unidade, dentro da célula de 0,82. O SpriteRenderer fica branco para preservar a cor da nova arte, e a tinta continua abaixo das peças. A preparação do laboratório importa os PNGs e atualiza prefab e ícone pela API do editor.

Nesta etapa, validar o visual e a troca de estado no tamanho do tabuleiro. Limpeza animada, partículas e áudio da tinta ficam para a próxima etapa.

Aceite desta etapa: testar matches na célula e ao lado, especiais, primeira/última camada, contador, pacote sobre tinta e reinício. Os botões verificam a remoção, mas não substituem matches reais e combinações de especiais.
