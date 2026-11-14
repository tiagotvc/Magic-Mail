# Fita postal — primeiro laboratório

**Atualização do Level Editor:** por orientação do usuário, a fita é apenas um bloqueio, sem meta própria. Já está disponível no editor normal. O laboratório passou a usar envelopes como objetivo. As referências a cinco metas de fita abaixo são históricas, da primeira bancada, e foram substituídas. Consulte [Fita_Level_Editor.md](Fita_Level_Editor.md).

**Ajuste V3:** o usuário gostou do descolamento e pediu apenas mais rapidez. Aplicado multiplicador de velocidade 1,25×: duração total de 1,08 para 0,864 segundo, preservando a trajetória, a dobra e a sequência. Compilação runtime/editor sem erros; o ciclo nativo completo anterior refere-se à V2. As gravações V2 abaixo conservam o ritmo anterior.

## Animação revista V2

Após o usuário rejeitar o primeiro efeito, a remoção passou a simular uma fita puxada pela ponta: linha de adesão móvel, dobra arredondada de 150 graus e cauda solta inteira. A área ainda colada permanece imóvel; a tira não é comprimida em um rolinho e não perde opacidade enquanto continua aderida. A tira superior começa primeiro, e a inferior vem 0,22 segundo depois. Cada malha tem 36 segmentos; o conjunto leva 1,08 segundo, incluindo a breve saída das tiras já soltas. As regras e a arte permanecem iguais.

[Detalhe da nova animação](../Tools/royal-postman/production/review/postal-tape-peel-v2/descolando-detalhe.gif) · [Gravação no tabuleiro](../Tools/royal-postman/production/review/postal-tape-peel-v2/descolando-no-tabuleiro.mp4).

Revalidação nativa isolada PASS para bloqueios, match, foguete, preservação na liberação, contagem, conclusão e reinício. Compilação runtime/editor sem erros. Avaliação estética ainda depende do usuário. As descrições de 0,44 segundo abaixo registram a versão original e são substituídas pelos tempos desta revisão.

Visual baseado no esboço aceito para desenvolvimento: duas tiras translúcidas em X, sobre a peça. O PNG mestre é original, gerado com ImageGen; cada tira é independente. O descolamento usa uma malha segmentada que mantém a parte colada no lugar enquanto enrola a ponta, com breve tensão e desaparecimento final.

Arte salva em `Assets/RoyalAves/Art/Obstacles/PostalTape/TapeMaster.png`; [prompt e origem da geração](../Tools/royal-postman/production/review/postal-tape-v1/art-prompt.json).

## Regra deste protótipo

- Uma camada por célula; as duas tiras juntas contam como uma fita na meta.
- A peça continua visível e mantém sua identidade. A fita impede troca, saída/entrada por gravidade e ativação manual de power-up. Tocar em um foguete preso não consome uma jogada.
- Match que atinge a célula ou um match ortogonal adjacente, assim como um especial direto, inicia a remoção. Origem diagonal é recusada.
- O impacto é absorvido pela fita. A peça permanece intacta no instante da liberação; ela pode participar de matches/cascatas posteriores normalmente.
- A proteção continua durante os 0,44 segundo de descolamento. Acertos repetidos não duplicam a animação nem a coleta. A meta baixa uma vez ao terminar.
- Esta bancada não combina fita com gelo nem com obstáculos. Essas compatibilidades ainda não foram validadas.

## Abrir e testar

Menu **Royal Aves > Elementos > Fita postal > Abrir laboratorio no jogo**.

Tabuleiro com cinco fitas, meta própria e 40 jogadas. Primeira jogada preparada: mover para a direita o trevo da linha 3, coluna 2, formando um match verde ao lado da fita. Há um foguete livre na linha 6, coluna 1 e outro preso na linha 6, coluna 4. O painel permite reiniciar, desativar e aplicar impactos diagnósticos; os botões não substituem jogar no tabuleiro.

Os reforços pagos ficam ocultos. A conclusão permanece no laboratório, sem resultado de campanha ou recompensa. O laboratório não grava fases da campanha. O carregamento usa uma cópia própria e não reaproveita `WireBlock`, nem altera seu significado.

## Escopo da instalação

Runtime: `PostalTapeLayer`, com intervenções pequenas na consulta de bloqueios, recepção de impactos e proteção do toque de especiais. O comportamento normal das células sem fita permanece no caminho existente.

Assets próprios em `Assets/RoyalAves/Art/Obstacles/PostalTape/` e `Assets/RoyalAves/Laboratory/PostalTape*`. Meta `PostalTapes` acrescentada ao fim do catálogo, sem reordenar as anteriores, e escondida das escolhas normais pelo filtro existente. Ainda não há pincel de fita no editor normal: esta é a versão de avaliação jogável, não uma distribuição em fases da campanha.

Backup: `Backups/postal-tape-v1-2026-11-13/`. Evidências, prompts e manifesto em `Tools/royal-postman/production/review/postal-tape-v1/`. O aceite final das regras, do visual e da animação depende do teste do usuário.

## Validação

PASS em cópia isolada do Unity: cinco fitas/meta, recusa de troca e consulta de queda bloqueada, toque no foguete preso sem gasto de jogada, recusa de origem diagonal, match real iniciado pela API de troca, preservação das peças no instante da liberação, foguete real iniciado pela API de toque removendo as fitas da linha, contagem unitária após o descolamento e reinício real. As cascatas legítimas são contabilizadas separadamente. A conclusão não entrou em resultado de campanha. Os 18 registros de meta anteriores foram preservados, apenas acrescentando `PostalTapes`.

Compilação runtime/editor: zero erros. Capturas e [gravação do tabuleiro](../Tools/royal-postman/production/review/postal-tape-v1/fita-no-tabuleiro.mp4) mostram sprites e malhas reais. Isso não é validação de ponteiro físico, áudio audível, aparelho ou campanha completa. A queda foi verificada pelo bloqueio e pela API de movimento, sem uma cascata física preparada sobre um buraco.
