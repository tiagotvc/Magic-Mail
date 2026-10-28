# Incubadora: efeitos V107

O aquecimento agora combina um halo âmbar pulsante, um centro amarelo claro e 18 partículas maiores, em creme e dourado, subindo pelos lados do ovo. As duas camadas continuam recortadas pelas máscaras existentes da estufa e só ficam ativas durante a incubação.

A revelação escurece a interface com opacidade de 97,5%. O glow tem duas camadas; os 12 raios têm bordas suaves, rotação e intensidade pulsante. Duas ondas de 12 estrelas saem de trás do mascote, nas mesmas direções dos raios. Estrelas e halos são desenhados em uma única Graphic usando o sprite existente, sem criar materiais ou objetos por partícula. A animação usa tempo real, inclusive com o jogo pausado.

As alterações são de código. Prefab, sprites, materiais AllIn1 dos botões, layout, inventário e concessão de mascotes permanecem preservados. O catálogo real continua vazio; o teste continua usando a prévia provisória.

Para conferir uma simulação que já estava aberta, clique em **Reiniciar teste** em **Royal Postman > Testar incubadora**. Isso recria as camadas dinâmicas.

Validação nativa isolada: `production/HatchVfxV107Check.cs`. Compilação e render em 720×1280 e 390×844; diferença de pixels com aquecimento ligado/desligado; estrelas visíveis na estufa e na revelação; fundo escuro; pulso e movimento; quatro raridades; conclusão e confirmação do recebimento. A Unity principal não foi operada. Resultados e hashes em `production/review/hatch-vfx-v107/`.
