# Pedra rúnica — laboratório da Encomenda Encantada

29/10/2026. Protótipo isolado; não inserido na campanha.

## Abrir

Unity: **Royal Aves > Elementos > Pedra rúnica > Laboratório no jogo**.
O comando prepara os assets próprios, abre diretamente `game.unity` e inicia o tabuleiro normal
com uma fase isolada de 7×7, três pedras, meta de três e 40 jogadas.
Se houver cena com alterações, a Unity oferece salvá-la antes da troca.

O painel permite reiniciar, desativar, ajustar velocidade/dispersão e aplicar impactos
de diagnóstico. Esses botões não testam entrada real por ponteiro e não usam boosters pagos.
Use os especiais já posicionados para testar pelo grid. Coordenadas começam em zero:

- Pedra (1,2), com foguete vertical em (1,4).
- Pedra (5,3), com foguete horizontal em (3,3).
- Pedra (3,5), com dinamite em (3,4).
- Match comum preparado: trocar (2,1) com (3,1), formando uma coluna ao lado da primeira pedra.

## Regra do protótipo

Pedra fixa de uma célula e uma resistência. Não troca nem cai. Combinações adjacentes
só causam uma reação curta, sem dano; impacto direto de especial inicia a quebra.
Chamadas repetidas durante a quebra são ignoradas. Liberação da célula e meta ocorrem
uma vez, após 0,12 s na velocidade padrão; os detritos continuam caindo independentemente.
O laboratório permanece aberto ao zerar a meta ou esgotar jogadas, sem entrar na vitória
ou premiar a campanha. Reinicie ou desative pelo painel.

O filtro atual usa o contrato existente de `Square.DestroyBlock`: impactos adjacentes
carregam a célula de origem; especiais atingem a célula diretamente. Ferramentas que
chamam essa mesma rota direta também podem quebrar a pedra; sua política definitiva
ainda precisa ser decidida antes de integrar à campanha.

## Visual

PNG frontal com pata, original gerado por ImageGen na conversa, copiado sem mudar os
bytes para `Assets/RoyalAves/Art/Obstacles/EnchantedStone/stone-paw-v1.png`.
O Sprite nativo recorta somente a margem transparente. A quebra não usa spritesheet:
A revisao 2 usa 30 fragmentos solidos de tamanhos variados (7 maiores, 12 medios e
11 pedriscos), sem recortar o PNG. Malhas rochosas irregulares tem faces iluminadas,
espessura e rotacao em tres eixos, impulso radial, gravidade e po breve. A pata nao
sobrevive como mosaico nos pedacos. Fragmentos sólidos não encolhem nem desaparecem
por fade; são limpos fora da câmera ou pelo limite defensivo de tres segundos de simulação.
Destruir o campo limpa efeito, malhas, materiais e textura temporária do pó.
Não há áudio próprio de pedra nesta entrega.

## Isolamento e verificação

Fase, meta, Sprite e prefab próprios em `Assets/RoyalAves/Laboratory/EnchantedStone*`.
Nenhum enum, registro global de metas, prefab de madeira ou fase da campanha é substituído.
Três pontos de integração: carregamento de laboratório no Editor, resolução de impactos
somente quando o componente EnchantedStone está presente e bloqueio de resultados/recompensas
durante esta bancada. Os demais prefabs e edições manuais não são reconstruídos.

Runtime/editor compilados sem erros usando as referências da Unity; binários de revisão
somente em Logs. Assets preparados e importados pelo editor Unity já aberto.
Relatórios: `Logs/stone-runtime-build.log`, `Logs/stone-editor-build.log`,
`Logs/stone-lab-prepare.result` e, quando executado, `Logs/stone-lab-native-test.result`.
O teste nativo de diagnóstico é acionado somente por pedido explícito no arquivo
`Logs/stone-lab.request` com conteúdo `test`; não é executado automaticamente ao abrir o menu.

Pendentes de aceite: aparência/ritmo da quebra no grid, entrada real por ponteiro,
combinações de especiais, hélice/multicolor, política das ferramentas, áudio e dispositivo.
Não houve validação completa da campanha ou de um merge nesta entrega.

SHA-256 do PNG original preservado: `72fa65a5a10eec73e5db9a7e03e1bafdaef3fec00b19958d11d9b1d3745838d3`.

## Revis?o 2 ? refer?ncia da coruja

Refer?ncia fornecida pelo usu?rio: https://www.youtube.com/shorts/7-34U_-kAgU
V?deo acessado e analisado quadro a quadro em 29/10/2026, especialmente 9,008?9,800 s
(aproximadamente 23,98 fps). Quadros para an?lise, sem uso como arte do jogo:
`Logs/video-reference/owl-break-frames.jpg`; v?deo local `runa-owl-reference.mp4`.

Observa??o: a quebra se l? como uma explos?o de peda?os pequenos e irregulares com
volume, faces claras/escuras, dispers?o e giro, seguidos por queda. H? v?rios efeitos
simult?neos no v?deo, portanto n?o se atribui todo clar?o ou poeira exclusivamente ? coruja.
A implementa??o original em nove recortes regulares foi rejeitada pelo usu?rio.
A revis?o usa geometria original e mant?m o cinza da runa; n?o extrai sprites do Royal Match.
O movimento foi aproximado visualmente, sem alega??o de reproduzir a implementa??o original.

Backup do efeito anterior: `Logs/stone-break-v2/StoneShatterEffect-v1.cs`.

Hit comum: nao reproduz animacao nem muda a cor da pedra (ajuste solicitado em 29/10/2026). Mantido apenas o registro de impacto rejeitado. Hit de powerup conserva o efeito StoneShatterEffect e a remocao existentes.
