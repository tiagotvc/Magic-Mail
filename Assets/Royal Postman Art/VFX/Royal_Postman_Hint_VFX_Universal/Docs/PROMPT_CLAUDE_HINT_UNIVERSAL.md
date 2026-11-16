# PROMPT PARA CLAUDE CODE — ROYAL POSTMAN / HINT VFX UNIVERSAL

## Objetivo
Substituir **somente a apresentacao visual** do sistema atual de dicas do Royal Postman (Unity 2D match-3). Hoje as pecas pulsam. Quero uma dica no estilo de qualidade das referencias fornecidas: contorno magico branco/dourado ao redor da **REGIAO ENVOLVIDA NO MOVIMENTO**, pequenos brilhos e movimento curto de uma unica peca na direcao da troca. **Sem pulsar as pecas.** Usar identidade visual propria.

ANEXO: pasta `Assets/HintMasks` com 14 texturas PNG RGBA de mascaras brancas com alpha verdadeiro, organizadas em Border, Sparkles, Highlights e Motion. Consultar `manifest.json` para a finalidade de cada uma. Importar como Sprite (2D and UI), Alpha Is Transparency ON, Filter Mode Bilinear, Wrap Mode Clamp, mipmaps off, compressao que preserve o alpha.

## REQUISITO CENTRAL — FUNCIONAR COM QUALQUER MATCH
Nao usar uma unica mascara de contorno quadrado esticada, nem contornar cada tile isoladamente. O contorno deve seguir **o PERIMETRO EXTERNO da uniao das celulas a destacar**, sem linhas internas:
- Match horizontal de 3, 4 ou 5;
- Match vertical de 3, 4 ou 5;
- Formatos L, T e combinacoes;
- Pecas nas bordas do tabuleiro;
- Regioes irregulares que venham a ser suportadas pelo resolvedor de dicas;
- Se houver mais de um componente desconectado, renderizar um contorno para cada componente (ou selecionar a melhor regiao de dica, conforme o sistema existente).

Usar as celulas da jogada sugerida: peca de origem, destino de troca, e celulas que efetivamente formariam match **DEPOIS** da troca. Escolher a regiao a destacar de modo coerente com o indicador atual e com a referencia; evitar supor que as pecas no tabuleiro ANTES do swap ja formam o grupo valido. Se o HintSolver so retornar duas coordenadas, simular o swap em memoria SEM ALTERAR o board nem afetar pontuacao, calcular o(s) match(es) hipotetico(s) e restaurar estado. Nao incluir pecas adicionais desnecessarias na borda.

### Geracao do perimetro
1. Receber um `HashSet<Vector2Int>` de celulas da regiao marcada. Nada de detectar cor pela UI nem usar bounds retangular para L ou T.
2. Para cada celula, inspecionar os quatro vizinhos; adicionar um segmento de aresta **apenas** se nao houver celula vizinha naquele lado.
3. Encadear arestas orientadas formando um ou mais loops fechados, de preferencia fundindo segmentos colineares consecutivos. Tratar cantos externos e reentrancias (L, T) corretamente.
4. Desenhar um contorno continuo seguindo os loops, com arredondamento leve nos vertices (raio aproximado de 0.08–0.13 vezes o tamanho de uma celula), posicionado ligeiramente para fora da silhueta (offset 0.03–0.06 celula). Evitar recortes nas bordas e sobreposicoes de segmentos.
5. Recomendo implementacao simples e performatica por LineRenderer com pontos interpolados/rounded joins e 2–3 linhas co-localizadas (halo / dourado / branco). Se a arquitetura for Canvas UI e for melhor gerar uma malha de borda ou usar UI Toolkit compatível, adapte sem quebrar o sorting. As texturas 01 a 05 podem servir como alternativa para quads/sprites de borda, cantos e sweep. Usar as mascaras 06 a 14 nas demais camadas.
6. Independentemente da tecnica, o resultado em L/T **NAO** deve ter bordas internas entre celulas adjacentes. A iluminacao nao deve parecer varias molduras quadradas coladas.

## APARENCIA — CONTORNO MAGICO
- Cor central: branco quente `#FFFDF0`; camada secundaria dourado claro `#FFE89A`; halo dourado palido `#FFD87D` com alpha baixo.
- Contorno sutil, claramente visivel em tabuleiro roxo, mas sem esconder as pecas.
- Um leve pulso de brilho **na borda**, nunca escalando o sprite das pecas.
- Em maximo 2 a 4 pontos ao longo de toda a borda, animar sparkles bem pequenos e brancos/dourados (sprites 06, 08 ou 09). Nao criar chuva de particulas nem centenas de sparkles.
- Opcional: sweep fino e discreto que percorre uma secao da borda (mascara 05); baixo contraste.
- A camada interna de highlight da regiao pode usar os sprites 10 e 11 com opacidade muito baixa, mas a prioridade visual e o PERIMETRO CONTINUO.

## NUDGE — MOSTRAR O SWAP
- Animar **exatamente a peca que o jogador deve mover**, na direcao do destino calculado pelo HintSolver (cima, baixo, esquerda, direita).
- Movimento pequeno: cerca de 0.16 a 0.23 largura de celula (padrao 0.19). Nao realizar swap de verdade.
- Ir: 160–210 ms com ease-out; voltar: 200–260 ms com ease-in-out, pausar 450–750 ms entre ciclos. Fazer a peca voltar sempre para a posicao original pixel-perfect.
- Opcional: mascaras 12 (rastro), 13 (risco fino) e 14 (micro glow) com opacidade baixa. NAO usar setas gigantes, ghost piece nem escalar as pecas.
- Respeitar sorting/layer do tabuleiro e evitar a peca sumir atras de bloqueadores.
- Nao alterar transforms usados pelo gameplay de forma cumulativa. Capturar a posicao inicial, ancorar ao Grid e restaurar mesmo ao cancelar; preferir child visual transform ou overlay temporario para nao deslocar hitboxes/colisores ou coordenadas de jogo.

## ESTADOS E CICLO DE VIDA
- Mostrar ao terminar o delay de inatividade da logica de dica ja existente; nao introduzir segundo timer concorrente.
- Interromper instantaneamente e limpar a dica quando o usuario toca/arrasta, quando a fase termina, enquanto o board resolve cascata/gravity, quando uma modal abre, ao usar booster, ou se a jogada sugerida deixar de ser valida.
- Nao piscar sobre animacoes de destruicao. Nunca desencadear match automaticamente. Nunca bloquear input.
- Evitar coroutines duplicadas. Disparar no maximo um hint ativo e lidar com cancelamento/reentrada. Mesmo se o tempo escalar for zero durante pause/modal, garantir limpeza correta.

## CONTROLES NO INSPECTOR
`hintBorderThicknessCellRatio` (0.025), `hintHaloThicknessCellRatio` (0.09), `hintBorderOffsetCellRatio` (0.045), `hintCornerRadiusCellRatio` (0.10), `hintSparkleCount` (3), `hintSparkleScaleCellRatio` (0.22), `nudgeDistanceCellRatio` (0.19), `nudgeOutDuration` (0.19), `nudgeBackDuration` (0.23), `nudgeCyclePause` (0.6), `borderPulseDuration` (1.25), `hintTintCore`, `hintTintGold`, `hintTintHalo`, e enable/disable da luz de varredura.

## IMPORTANTE SOBRE TEXTURAS
- O kit contem mascaras BRANCAS com RGBA + alpha real; colorizar no Unity com material / particle tint. Nao usar o atlas de preview como Sprite de runtime, e sim os PNGs individuais.
- Texturas 01 a 05: componentes **opcionais** de borda para abordagem de sprites. Para shapes irregulares, PREFIRA contorno procedural exterior (LineRenderer ou malha) e deixe os sprites de borda como fallback; bordas em segmentos separados podem criar emendas. Textura 03: canto externo; 04: canto reentrante. Rotacionar corretamente.
- Texturas 06–09: brilho; 10–11: glow interno discreto; 12–14: rastro de nudge.
- Usar white core com blending Alpha no contorno e material aditivo somente se a pipeline realmente oferecer qualidade e contraste melhores. Efeito deve ficar bom SEM Bloom.

## COMPATIBILIDADE / PERFORMANCE
- Verificar antes se o projeto usa SpriteRenderer, UI Canvas ou URP, bem como o resolvedor de hints e o pool de VFX existente; integrar-se a eles sem duplicar sistemas.
- Evitar criar materiais dinamicos a cada frame. Pool de objetos reutilizaveis, cache de texturas / pontos / componentes e atualizacao apenas quando o hint muda. Transformar coordenadas Grid->World com os metodos ja existentes.
- O contorno nao precisa recalcular toda frame. So atualizar brilho/sparkle, e o movimento visual da peca.
- Validar 60 FPS mobile, multiplas dimensoes/aspect ratios e tile spacing variavel.

## ENTREGAS / CRITERIOS DE ACEITACAO
1. Scripts C# completos integrados e prefab (ou hierarquia criada por codigo documentada).
2. Nenhuma pulsacao/escala das pecas durante a dica.
3. Match 3H/3V/4/5/L/T tem **contorno exterior unico sem arestas internas**.
4. Sparkles discretos; nudge curto na direcao REAL da jogada; volta perfeita ao inicio.
5. O hint se cancela sem glitches ao tocar, resolver match ou trocar de fase.
6. Um modo Debug/Editor que mostra os seis formatos fornecidos em `Previews/01_preview_formatos.png`, inclusive canto concavo de L e T.
7. Explique quais arquivos originais foram alterados e os novos. Execute/teste o que for possivel no projeto; nao diga que compilou se nao foi validado.

Nao gerar arte adicional de pecas. Foque exclusivamente no HINT VFX.
