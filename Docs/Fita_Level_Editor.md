# Fita postal no Level Editor

Disponível na paleta de blocos como **Fita postal**. É apenas um bloqueio: **não cria meta, não aparece entre os objetivos e não precisa ser removida para vencer**, caso as metas reais da fase já tenham sido alcançadas.

Selecione o pincel e clique sobre uma peça comum ou power-up. A fita ocupa uma camada da mesma célula, sem substituir a peça. Pintar novamente não duplica a resistência. O apagamento com esse pincel remove somente a fita, preservando a peça e o piso. Também é possível usá-la sobre uma peça que esteja na grama.

O editor recusa fita sobre gelo, caixas, móveis ou outros obstáculos, assim como sobre o ovo mágico. O pincel de itens também impede colocar um conteúdo incompatível ou marcar a célula com fita para começar sem peça. Remova a fita antes de adicionar outro bloqueio. Os botões de preenchimento e distribuição aleatória respeitam essas restrições.

O comportamento aprovado continua igual: bloqueia troca, queda e ativação; match que atinge a célula ou ocorre ortogonalmente ao lado e especial direto iniciam o descolamento; a peça é preservada na liberação. Mantida a animação pela ponta, com a tira superior saindo primeiro, velocidade 1,25× e duração total de 0,864 segundo. O impacto não atravessa a fita para danificar o conteúdo ou a grama.

## Instalação

Prefab independente: `Assets/SweetSugar/Resources/Blocks/PostalTape.prefab`. O ID `PostalTape` foi acrescentado ao fim de `SquareTypes`, preservando os IDs antigos. Não usa `WireBlock` nem substitui outro elemento.

`PostalTapePlacement` liga a camada salva à animação já existente. Ao terminar, a camada é removida pelo caminho normal de liberação da célula. A imagem em X da paleta foi composta nativamente no Unity a partir das duas tiras existentes; o PNG mestre original permanece intacto.

O antigo registro experimental `PostalTapes` foi mantido oculto apenas para não reindexar referências serializadas. Não é oferecido na escolha de objetivos e não recebe contagem pela fita. O laboratório agora tem uma meta de envelopes comuns, não de fitas.

## Verificação

PASS em cópia isolada do Unity: chamada do pincel e apagador reais, resistência única, preservação de peça e piso, recusa de gelo, visualização sobre a peça, salvamento/reimportação e carregamento normal de fase com todos os laboratórios desativados. Conferidos cinco bloqueios instanciados pelo prefab de produção, absorção de impactos repetidos, preservação da peça, remoção completa da camada, liberação de queda, match e foguete iniciados pelas APIs reais. Descolar uma fita isoladamente não alterou nenhuma meta. O catálogo global de objetivos permaneceu byte a byte igual.

Compilação runtime/editor: zero erros. O teste não equivale a ponteiro físico, áudio audível, aparelho ou campanha completa. A fase de teste 1901 existe somente na cópia isolada e não foi instalada no projeto principal. Nenhuma fase da campanha foi alterada.

Evidências e manifesto: `Tools/royal-postman/production/review/postal-tape-editor-v1/`. Backup: `Backups/postal-tape-editor-v1-2026-11-13/`.
