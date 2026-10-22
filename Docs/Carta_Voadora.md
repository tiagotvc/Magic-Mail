# Carta Voadora — protótipo de 22/10/2026

**Histórico substituído:** o usuário rejeitou esta proposta e escolheu o **Ovo mágico**, um obstáculo móvel que quebra no tabuleiro. O voo ao HUD e a flutuação abaixo foram removidos. Regras atuais, arte e menu em [Ovo_Magico.md](Ovo_Magico.md). Os IDs internos foram preservados por compatibilidade.

## Funcionamento proposto

- Alvo móvel de uma célula, separado das peças combináveis e das cartas emitidas pelo armário/franqueadora.
- Uma carta recebe um impacto e é capturada: match em um dos quatro vizinhos ortogonais ou especial que alcance sua célula. Match diagonal não conta.
- Não tem cor de combinação, não forma trincas e não ativa ao ser tocada como um power-up. Pode participar de uma troca com peça vizinha se a outra peça formar uma jogada válida; uma troca sem match volta, seguindo o sistema atual.
- Acompanha a gravidade e usa o fluxo de teleporte de `Item`. Não é coletada simplesmente ao alcançar a última linha.
- Flutuação visual no filho do sprite: não altera a célula lógica, o collider ou o caminho da queda.
- Quantidade finita, pintada na aba de peças do Level Editor. O preenchimento aleatório não gera cartas adicionais.
- A meta própria **Carta Voadora** conta automaticamente as cartas iniciais pintadas, excluindo células marcadas para iniciar sem peça. Ao pintar uma carta, o editor acrescenta essa meta.
- A captura desocupa a célula imediatamente; a meta só diminui na chegada da animação ao HUD. O voo entra na fila existente de animações para impedir conclusão antes da entrega.
- Captura protegida contra chamadas repetidas; a peça retorna ao pool. O voo é independente do objeto reutilizado. Reinício/troca de fase cancela o efeito e o callback não altera a meta de outro carregamento.
- Gelo/camadas que prendem a peça absorvem o dano antes da captura. Interações complexas com essas camadas ainda precisam de aceite em Play.

## Como testar

No Unity, após recompilar: **Royal Aves → Elementos → Carta Voadora → Testar protótipo**. O painel também pode ser aberto em **Painel do protótipo**.

O teste usa um laboratório 7×7, quatro cartas e 30 jogadas. Não substitui nenhuma fase da campanha. Há uma jogada preparada: trocar a peça em coluna 2, linha 3 com coluna 2, linha 2, usando coordenadas iniciadas em zero. A linha 2 então forma uma trinca embaixo da carta da posição (2,1). Um foguete vertical em (4,4) permite testar a carta em (4,2). O painel oferece captura manual para diagnóstico, contador e reinício. **Desativar laboratório** restaura as fases normais no próximo carregamento. Vitória/derrota do laboratório não usa a economia da campanha.

No Level Editor normal: aba de peças, selecionar o envelope claro do tipo `FLYING_LETTER` e pintar as células. A meta tem nome próprio e não compartilha a contagem com envelopes comuns, cartas do armário ou cartas da franqueadora.

## Arte e integração

Arte provisória: cópia sem alteração dos pixels da carta cartoon já usada pelo armário. Mantida como sprite separado para não misturar objetivos. PNG 1254×1254 com alpha; 660 pixels por unidade, canvas de 1,9 unidades, filtro bilinear, sem mipmaps/compressão. O importador mantém essa escala após reimportar. Flutuação e leve balanço diferenciam o alvo no tabuleiro. Asas, identidade visual final e áudio próprio ficam para o aceite do conceito.

- Prefab: `Assets/SweetSugar/Resources/Items/FLYING_LETTER.prefab`.
- Arte: `Assets/RoyalAves/Art/Obstacles/FlyingLetter/CM_carta_voadora_prototipo.png`.
- Laboratório/meta: `Assets/RoyalAves/Laboratory/FlyingLetterLevel.asset` e `FlyingLetterTargets.asset`.
- `ItemsTypes.FLYING_LETTER = 9`, acrescentado sem mudar os valores anteriores. Objetivo `FlyingLetters` acrescentado ao fim da configuração global.
- Geração reproduzível dos assets: `Tools/build_flying_letter_prototype.py`. Não reexecutar para manter edições manuais feitas posteriormente no laboratório.

## Validação e limites

Runtime e editor compilados via MSBuild (`Logs/flying-letter-build.log`). Onze verificações gerenciadas executadas no assembly compilado: contagem finita/pintada, células vazias/buracos, agregação entre campos, guarda de captura repetida, suporte a impacto especial, identidade diferente de power-up, estabilidade de IDs e resolução da classe de meta. Harness em `Tools/FlyingLetterChecks.cs`; resultado em `Logs/flying-letter-managed-tests.result`.

Conferidos arquivos e referências do prefab, meta, pool, 49 células, quatro cartas, jogada preparada, foguete vertical, transparência e escala do sprite (`Logs/flying-letter-assets.result`). O validador nativo também está disponível no menu **Validar protótipo**.

Ainda não conferidos interativamente: sequência completa até meta zero, swaps, gravidade/cascatas, portais, interação com cada especial, gelo, reinício com voo em andamento e apresentação no HUD. A revisão do Unity aberta durante a implementação ainda não havia recarregado os assemblies novos; os testes gerenciados e a compilação não substituem esses testes de Play Mode. Campanha e fase 46 não foram alteradas.
