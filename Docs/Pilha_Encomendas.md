# Caixa de madeira — encomendas

O pacote em papel foi substituído por uma caixa de madeira com visão totalmente frontal, cantos arredondados e textura simplificada, seguindo as peças do jogo. O topo e as laterais não aparecem. Os quatro estados mantêm a mesma silhueta e tamanho de apresentação.

| Resistência | Aparência |
| --- | --- |
| 4 | Três travessas horizontais |
| 3 | Travessas central e inferior |
| 2 | Apenas a travessa inferior |
| 1 | Painel de madeira fechado, sem travessas |

As imagens ativas continuam em `Assets/RoyalAves/Art/Obstacles/ParcelStack/CM_pilha_estado01_00.png` até `CM_pilha_estado04_00.png`, preservando os GUIDs. Todas são PNGs RGBA de 1254 × 1254. A importação normaliza o corpo visível para 1,72 × 1,72 unidades locais, centralizando cada estado na célula. Prompts e destinos completos em [Caixa_Madeira_Prompts.md](Caixa_Madeira_Prompts.md).

## Regras preservadas

- Ocupa uma célula fixa e bloqueia entrada/saída de peças enquanto houver camadas.
- Resistência de uma a quatro vidas; cada impacto aceito remove uma camada.
- Mantém os danos por match adjacente ortogonal e especiais do `SolidBlock`.
- Danos intermediários não reduzem a meta. Cada caixa completa conta como um único objetivo.
- O último impacto inicia a quebra de 0,62 segundo. A célula permanece bloqueada durante o efeito; a remoção, liberação e coleta ocorrem no callback final.
- Impactos repetidos durante a conclusão não iniciam nova coleta. Reiniciar ou sair cancela o efeito sem coletar a caixa antiga.
- Apenas a camada de resistência 1 possui `TargetComponent`; a proteção nativa contra impactos repetidos permanece.

## Efeitos e integração

`ParcelAccessoryEffect` conserva seu nome interno, mas agora destaca cada travessa como uma peça rígida que gira, cai e desaparece. A sequência é superior → central → inferior.

`ParcelTearEffect` faz uma pequena tremida e divide a caixa em doze fragmentos texturizados, que se dispersam e desaparecem. A fila nativa de animações e o momento da coleta foram preservados. Os sons existentes continuam disponíveis.

O prefab `Assets/SweetSugar/Resources/Blocks/SolidBlock.prefab`, usado pelo jogo, mantém nome, enum e quatro camadas. Os sprites de objetivos existentes foram sincronizados sem alterar layouts ou quantidades da campanha.

A arte de papel e os efeitos anteriores foram guardados em `Backups/ParcelStack-Kraft-Before-WoodenCrate`. As imagens frontais adotadas também estão em `Backups/WoodenCrate-v2`. A proposta em perspectiva, rejeitada pelo usuário, não é usada pelo jogo.

## Teste na Unity

Usar **Royal Aves → Elementos → Pilha de Encomendas → Teste das quatro caixas de madeira**. O comando prepara o laboratório em modo de edição e inicia um Play limpo, com quatro caixas de resistências 1, 2, 3 e 4, meta 4 e 40 jogadas. A entrada de diagnóstico pula a compra de vidas e boosters do lobby.

O painel de teste permite impactos sem gastar jogadas, ajuste de velocidade/intensidade dos efeitos e volume. A seleção do laboratório só existe na sessão do editor, fora do build. O laboratório mantém o encerramento do Play antes do fluxo normal de vitória/derrota.

Prévia do tabuleiro: `Logs/wooden-crate-game-preview.png`.

## Conferência

As quatro imagens foram inspecionadas e tiveram transparência e dimensões verificadas. O prefab foi salvo pela API do editor e o teste foi carregado na Unity com meta 4. Os quatro estados foram conferidos no tabuleiro, com o mesmo tamanho e sem perspectiva. Compilação de jogo e editor passou com as referências atuais do projeto.

Teste de efeitos realizado na Unity: um impacto intermediário reduziu a caixa de 4 para 3 vidas mantendo a meta em 4. No impacto final em uma caixa de uma vida, a meta ainda era 4 imediatamente após o acerto; depois da animação passou para 3, a caixa foi removida e não restaram efeitos finais ativos. Registro em `Logs/wooden-crate-validation.result`. O laboratório foi restaurado com as quatro resistências para teste manual.
