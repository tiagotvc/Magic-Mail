# Cofre — animação por fragmentos

09/11/2026. Prévia visual isolada do modelo turquesa. A imagem roxa do usuário orienta apenas a progressão; o desenho continua sendo o cofre turquesa com barra horizontal.

**Aceite final posterior em 09/11/2026:** após a integração 2×2 no Level Editor e a correção da hélice, o usuário declarou o elemento finalizado com sucesso e aprovado após seus testes. As menções a aceite pendente abaixo descrevem o estado histórico da entrega inicial da prévia e foram superadas por essa confirmação. Ver [Cofre_Level_Editor.md](Cofre_Level_Editor.md).

## Ver a animação

No Unity, fora do Play: **Royal Aves → Elementos → Cofre → Prévia da quebra**.

A janela reproduz a sequência e permite pausar, ajustar velocidade, repetir, reiniciar e aplicar acertos manuais. **Match sem dano** apenas sacode o cofre. A prévia usa uma cena temporária própria e não troca a cena de trabalho.

Prefab de inspeção: `Assets/RoyalAves/Laboratory/SafeAnimation.prefab`.

## Sequência

1. Cofre completo, barra e fechadura intactas.
2. Barra e suportes se desprendem; a fechadura fica.
3. Fechadura rachada, com fragmentos metálicos no impacto.
4. Fechadura removida; porta ainda fechada.
5. Porta destruída e moedas visíveis no interior.

O quinto acerto mantém o interior visível brevemente e despedaça a estrutura inteira. As moedas saltam, giram e caem separadamente; não seguem um voo de coleta ao HUD. Os cinco estados existem antes da destruição final: o quarto acerto revela o quinto estado e o quinto destrói o corpo.

`SafeBreakAnimation.Hit(true)` aplica um acerto válido; `Hit(false)` só faz a reação sem dano. Há proteção de 0,2 s contra impacto imediato repetido. `Finished` é emitido uma vez quando o corpo se rompe. `ResetAnimation` restaura o estado inicial e limpa os efeitos. O controlador é visual e não toma decisões sobre as fontes reais de dano do tabuleiro.

## Como é animado

Cinco imagens **estáticas** são regiões nativas do PNG de conceito, com centro e largura normalizados. Elas não constituem um flipbook de animação. O PNG original é preservado; o material próprio suprime a névoa translúcida da geração nas bordas.

`SafeDebrisEffect` usa o mesmo princípio balístico de `MaterialBurstEffect` (caixas/poções): impulso, gravidade e rotação independentes. A geometria específica do metal é fragmentada em polígonos com a textura do cofre, espessura extrudada, faces laterais sombreadas e giro nos três eixos. As moedas são pequenos meshes dourados com aro, rotação e simulação de giro pelo eixo, sem spritesheet. Não chama os efeitos de madeira nem modifica os seus parâmetros. Os fragmentos mantêm o tamanho durante a queda e são limpos após 2,6 s.

## Alcance e validação

Esta entrega implementa a animação e sua prévia. Não cadastra um novo obstáculo na paleta, não instala cofres em níveis e não altera metas, saldos ou recompensas. A quantidade de moedas é visual.

Atualização posterior de 09/11/2026: o usuário autorizou a inclusão do cofre como obstáculo 2×2 no Level Editor. Essa integração está documentada em [Cofre_Level_Editor.md](Cofre_Level_Editor.md); o limite acima descreve apenas a entrega original da prévia.

Capturas e verificações: `Tools/royal-postman/production/review/safe-v1/`. O exportador `Tools/export-safe-preview.py` empacota as capturas nativas em MP4/GIF. O método de captura verifica reação sem dano, cinco acertos, proteção contra repetição, término único, rejeição de dano após conclusão, reset e limpeza de fragmentos.

Continuam fora da validação: ponteiro real na janela, integração de matches/especiais reais do tabuleiro, metas/vitória, áudio, campanhas, aparelho físico e fluxo completo do jogo. Aceite visual da nova animação ainda depende do usuário.

Validação da versão final: compilação dos fontes atuais de runtime/editor sem erros; captura em Unity 6000.6.0f1 num projeto mínimo isolado (`Logs/SafeAnimationValidation`), com saída 0. Os 144 quadros nativos foram conferidos visualmente por amostragem, incluindo estado inicial, abertura e dispersão. Vídeo e GIF em `Tools/royal-postman/production/review/safe-v1/`; hashes e limites em `Tools/royal-postman/production/SAFE-ANIMATION-V1.json`.


## Correcao de prioridade da helice - 09/11/2026

MarmaladeFly agora reafirma Item mask e ordem minima 100 em LateUpdate durante todo o voo (incluindo preparacao e busca de alvo). ItemMarmalade.SetOrder preserva essa prioridade quando a helice esta voando. Evita que resets de camada/ordem das rotinas Item.JustCreatedItem e Square.Item coloquem o voo atras do cofre (90) e fragmentos/moedas (93). Arte e resistencia do cofre preservadas. Runtime/editor compilados com zero erros via Tools/inkwell-compile.py. Sem nova verificacao visual em Play nesta revisao.
