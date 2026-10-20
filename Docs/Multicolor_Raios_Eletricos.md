# Multicolorido: raios de energia e sincronização

## Sequência preservada

Cada ativação segue `Idle → Marking → Charged → Detonating → Finished`. Todos os alvos são reservados antes do primeiro disparo. O multicolorido gira enquanto os raios alcançam as peças, uma por vez. Cada contato acrescenta glow e pequenas estrelas orbitais. Depois da última marcação, termina apenas a revolução em andamento, sem pausa adicional.

Antes da primeira explosão, `EndSpin` desliga todos os raios e as faíscas da origem; em seguida, a rotina limpa as partículas de carga dos alvos. Só então libera dano. A origem se desfaz no mesmo quadro da primeira detonação de alvo; sua instância invisível mantém a rotina até liberar a última peça. Dois multicoloridos compartilham a mesma sequência e se desfazem juntos.

Combinações com dinamite, foguete e hélice usam a mesma transição. Reservas protegem contra dano, conversão, movimento e devolução ao pool durante a marcação. Cancelamento libera efeitos, reservas e o bloqueio do tabuleiro.

## Visual de energia

- Flash circular expansivo na ativação e estrelas orbitando a origem durante o giro.
- Arcos cúbicos de Bézier com pontos de contato exatos. O disparo cresce da origem ao alvo em 45 ms; o glow do alvo começa depois desse percurso.
- Ruído Perlin suave na direção perpendicular ao arco, pequenas oscilações rápidas e duas ramificações intermitentes.
- Núcleo claro, halo azul e pulsos animados em direção ao alvo. O shader aditivo calcula um perfil suave, sem bordas de retângulo.
- Carga com pequenas estrelas de quatro pontas. Impactos emitem 14 partículas radiais, com expansão inicial, redução e fade. A cor segue o índice da peça: roxo, amarelo, verde, vermelho e azul; cores adicionais usam ciano.
- Todo raio permanece desligado durante as explosões. O splash legado `FireworkSplashMulticolor` e o efeito `Replace` não participam dessa sequência.

A referência fornecida de Royal Match orienta a linguagem visual; esta é uma implementação própria para o projeto.

## Reaproveitamento e materiais

`Lightning.cs` prepara os pools no início da cena: 128 arcos, 128 cargas, 128 descargas e 16 flashes. Cada arco contém quatro LineRenderers e um buffer fixo de 64 pontos. Não cria GameObjects, materiais ou buffers novos a cada ativação. Os materiais são compartilhados. Efeitos voltam ao pool desativados, com partículas limpas e âncoras removidas.

Se a capacidade simultânea for excedida, o efeito decorativo é omitido; o dano e a marcação continuam. Não há expansão do pool durante a ativação. A capacidade cobre o tabuleiro atual, incluindo dois multicoloridos.

O shader `Assets/RoyalAves/Shaders/ElectricEnergy.shader` é referenciado por materiais em `Assets/RoyalAves/Resources/ElectricEffects`, para manter referências de assets no build. Não foram medidos FPS ou consumo em aparelhos físicos.

## Validação

Menu: **Tools > Magic Mail > Audit Multicolor Sequence in Play**, a partir do Edit Mode. Usa o tabuleiro real temporariamente e retorna ao Edit Mode, sem salvar a cena.

Os oito cenários passaram com os novos raios: nenhum alvo; um alvo; muitos alvos com tentativas externas de dano/conversão/remoção; dinamite; foguete; hélice; dois multicoloridos; cancelamento e reutilização. A auditoria observa a entrada efetiva em `Item.DestroyCor`, todos os alvos marcados, giro encerrado, zero raios visíveis e zero partículas de carga durante a detonação.

Teste nativo isolado: 16 arcos simultâneos, crescimento do disparo, 64 amostras de contatos exatos, shaders compilados e renderizados, simulação dos três modos de partículas e 200 ciclos de reaproveitamento de raio/carga sem criar GameObjects. Runtime e editor recompilaram sem erros.

Evidências: [marcação](Multicolor_Marcacao_Unity.png), [detonação](Multicolor_Detonacao_Unity.png), [linha do tempo](Multicolor_Auditoria_Timeline.csv) e [render isolado](Multicolor_Raios_Render_Unity.png). Os resultados locais estão em `Logs/multicolor-audit.result` e `Logs/multicolor-electric-native.result`.

## Transições de entrada e saída

A saída do multicolorido usa `PowerUpEnergyExit`: uma cópia visual reaproveitada faz um pop para fora (até 138% do tamanho), mantém a presença inicial e desaparece com fade rápido em 0,12 s, junto a um flash circular e faíscas. Substitui os fragmentos de `FallingBoardDebris` nessa saída. A cópia não depende da instância original, portanto a detonação dos alvos e o retorno do item ao pool continuam no mesmo ritmo. O pool de 32 cópias é preparado junto com os efeitos elétricos.

`ItemPackage.PlayAppearance` anima a dinamite durante 0,24 s, com escala inicial de 16%, expansão até 112%, acomodação ao tamanho normal e fade inicial. `Square.GenItem` chama a entrada quando `noAnim` é falso, incluindo conversões do multicolorido. A reserva do combo não interrompe a entrada. Ativação, desativação e reutilização restauram escala e cor. Peças configuradas com `noAnim` mantêm essa opção.
Validação das transições: os oito cenários passaram novamente em Play. O cenário de dinamite verifica que a entrada continua ativa após a conversão e reserva pelo multicolorido. O teste nativo verifica expansão do pop, fade e retorno da cópia visual ao pool.


## Detonação simultânea dos alvos

Depois de marcar todos e encerrar o giro, a sequência limpa raios e cargas e dispara todos os `DestroyItem` no mesmo quadro. Foram removidos os intervalos de 45 ms e 300 ms entre os alvos. A origem faz seu pop na mesma transição. Os especiais recebem a ativação em conjunto e executam seus próprios efeitos de dinamite, foguete ou hélice.

Para peças comuns atingidas pelo multicolorido, `HasActiveCollectionTarget` verifica metas pendentes, sprites aceitos e a condição de alcançar o fundo quando aplicável. Peças coletáveis usam a animação existente em `TargetCounter.CheckItems` e não geram também fragmentos. Peças fora das metas quebram localmente. A contagem continua sendo feita pelo fluxo original de coleta, sem contar a mesma peça duas vezes.

A auditoria verifica que todas as chamadas de detonação e entradas efetivas em `DestroyCor` ocorrem no quadro da remoção dos raios. Os cenários de muitos alvos e dois multicoloridos conferem também o número de animações de coleta; o segundo inclui peças que não pertencem às metas.
Resultado: os oito cenários passaram em Play. Todos os alvos marcados iniciaram a destruição no mesmo quadro em cada cenário com detonação. As verificações de coleta passaram nos cenários de muitos alvos e dois multicoloridos, incluindo peças sem meta.
