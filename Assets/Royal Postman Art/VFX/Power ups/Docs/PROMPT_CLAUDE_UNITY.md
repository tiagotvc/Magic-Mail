# Royal Postman — VFX dos 4 efeitos (Unity 2D)

Quero que você integre ao projeto Unity os 30 PNGs de máscaras brancas com alpha real deste pacote e desenvolva QUATRO efeitos diferentes: Match Normal, Bomba de Área, Line Cleaner e Impacto do Seeker/Girocóptero. Use o mock visual como direção de arte; não copie imagens ou assets de outros jogos. O objetivo é qualidade premium mobile, legibilidade e magia com cores próprias do Royal Postman.

## Primeiro: analisar o projeto
Identifique a versão Unity, Built-in/URP, ordem de renderização, câmera/canvas, implementação atual dos quatro VFX, posição do centro das células, tamanho da célula em unidades de mundo e sistema de pooling. Preserve a lógica existente. Não troque o sprite de nenhum booster; as máscaras são exclusivamente de efeitos.

## Regras para as máscaras
- Cada arquivo é um PNG RGBA com RGB branco e alpha graduado. Use Sprite (2D and UI), Alpha Is Transparency e compressão adequada (cuidar de banding em glows). Pivot central. Para máscaras alongadas, orientação horizontal: a esquerda é a cauda, direita a frente. Rotacione para movimentos verticais.
- Colorize glows, raios, anéis e fitas usando os componentes atuais e materiais compatíveis com a pipeline. Use blending aditivo apenas quando necessário/compatível. Não dependa de Bloom; o efeito deve funcionar sem pós-processamento.
- Não faça um grande sprite achatado com tudo: monte camadas separadas e anime scales/opacity/posição em tempos diferentes. Spawn de fragmentos individuais com variação controlada.
- Use os fragmentos já existentes se estiverem melhores; as máscaras da pasta 00_Comuns são opções universais.

## 1) Match Normal (efeito pequeno, delicado)
Arquivos: 01_Match_Normal + 00_Comuns/02_sparkle_4_pontas + fragmentos comuns.
- 1 ANEL branco único expandindo do centro da célula, 0.25 → 0.95 célula em 110-160 ms, fade para zero.
- Exatamente 3 sparkles brancos (não dezenas), nascem quase no centro e se afastam radialmente acompanhando o anel. Leve rotação e variação no tamanho.
- Manter os fragmentos coloridos já implementados e sincronizar com o anel.
- O microclarão 02_micro_clarao deve ficar MUITO discreto, não virar uma minibomba.
- Duração total 160-230 ms. Não introduzir smoke/fire, não pulsar tela, não ofuscar peças vizinhas.

## 2) Bomba / Explosão em área (impacto radial)
Arquivos: 02_Bomba_Area + fragmentos de 00_Comuns.
- FLASH inicial branco-amarelado entre 0-65 ms.
- ONDA circular principal (02_anel_choque_fino) expande de 0.25 até cerca de 3-5 células de diâmetro, conforme raio real da bomba. 0-250 ms, alpha reduz até zero.
- Segunda onda (03_anel_choque_largo) com 20-40 ms de atraso, menos opaca, e raios radiais curtos/longos com escalas independentes.
- Fragmentos simples e pequenos, nas cores reais das peças destruídas, lançados radialmente em pequena quantidade por célula. Eles devem sair de suas próprias células, não de um ponto único se a área de limpeza for grande.
- Glow residual termina por volta de 350-500 ms. Nunca usar fogo, nuvens, crispações ou gemas enormes.

## 3) Line Cleaner (energia contínua deslocando-se pelo tabuleiro)
Arquivos: 03_Line_Cleaner + fragmentos comuns.
- NÚCLEO branco intenso e fino + GLOW externo maior, dimensões em unidades de célula configuráveis no Inspector.
- Duas FITAS sinuosas rosa e roxo em fases/velocidades diferentes ao redor do feixe. Podem deslizar via UV, escala ou animação independente. Devem ser elegantes e não ocultar o board.
- FRENTE de impacto (05_ponta_impacto) acompanha o ponto de limpeza REAL. O resto do feixe deve ter cauda / retração temporal; não aparecer instantaneamente como uma barra imóvel, a menos que o gameplay faça tiro instantâneo.
- 06_micro_anel_quebra e fragmentos surgem somente quando cada célula é realmente atingida. As partículas permanecem brevemente na posição da célula e dissipam sem seguir a frente do tiro.
- Suportar movimento horizontal, vertical, direita→esquerda, esquerda→direita, cima→baixo e baixo→cima. Componentes ajustáveis para 1.0-2.0 larguras de célula de glow e 0.1-0.3 de núcleo; calibrar visualmente sem mexer na velocidade de gameplay.

## 4) Seeker/Girocóptero — IMPACTO na peça
Arquivos: 04_Seeker_Impacto + fragmentos comuns.
- Não criar um novo sprite de girocóptero: usar o booster já existente e o seu movimento atual.
- Chegada: rastro curto que decai antes do impacto, opcional arco de energia breve.
- Na peça-alvo: um círculo indicador mágico e flash branco-dourado, com 6-9 raios curtos e um halo circular que se expande rapidamente. O aro nunca precisa ficar visível depois da destruição.
- Fragmentos com a cor da peça removida e 2-5 sparkles suaves ao redor do ponto de impacto.
- Desacoplar o efeito da posição do seeker após a colisão: o impacto fica na célula original e dissipa em 220-380 ms.
- Evitar dar aparência de uma bomba grande: raio pequeno (aprox. 1-1.6 células).

## Arquitetura e controles
- APIs recomendadas: PlayMatch(cellPos, pieceColor), PlayBomb(center, radius, affectedCells), PlayLineCleaner(start,end,hitEvents), PlaySeekerImpact(targetCell, pieceColor). Adaptar aos tipos reais do projeto.
- Inspector: cor primária/secundária, intensidade glow, duração, escala em células, contagem de sparkles, contagem de debris, sorting order, parâmetros específicos do efeito.
- Pooling e baixo GC; cuidado com dezenas de partículas simultâneas. Prefabs e referências via SerializeField, não Resources.Load em cada execução.
- Implementar Scene/GameView de preview com botões para disparar os quatro efeitos. Os testes devem incluir match de 3/4, bombas encadeadas, tiros nos 4 sentidos e seeker em alvos variados.
- Não alterar pontuação, remoção lógica, colisões ou movimentos existentes. Cada efeito escuta a lógica de gameplay, nunca decide o resultado dela.
- Entregue implementação no projeto, prefabs, scripts C# compiláveis, instruções de importação, presets e lista dos arquivos alterados. Se algo estiver ausente, informe especificamente, sem inventar integrações.

**Importante:** O mock_composicao_4_efeitos.jpg é uma simulação estática criada para orientar camadas e cores. As máscaras na pasta Mascaras_PNG são os assets reais.
