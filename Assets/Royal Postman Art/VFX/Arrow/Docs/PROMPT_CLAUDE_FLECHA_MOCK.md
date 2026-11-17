# CLAUDE CODE — Flecha Postal VFX exatamente como o mock (Unity)

Quero implementar o efeito da Flecha Postal atravessando horizontal ou verticalmente uma linha do tabuleiro do Royal Postman. **O arquivo `efeito_flexa_postal_explosivo.png` mostrado na conversa é a referência visual.** Estou anexando um kit de 22 máscaras PNG brancas RGBA com canal alpha verdadeiro e as prévias correspondentes.

## Objetivo visual preciso
- A flecha original permanece nítida, em primeiro plano, na ponta do efeito e apontando na direção do deslocamento. **Não redesenhar, substituir ou modificar o sprite da Flecha Postal**.
- Uma aura dourada suave envolve a frente do booster em movimento.
- Atrás dela fica uma cauda de luz com um núcleo branco estreito e muito brilhante, faixa dourada em volta e glow mais largo discreto.
- **Duas fitas mágicas sinuosas roxa/magenta** cruzam a cauda em ondas sutis. Uma terceira linha rosa clara é opcional. No mock elas aparecem atrás da flecha, não como um grande borrão opaco.
- Uma trilha de pequenas estrelas douradas pisca e desaparece em momentos variados.
- Fragmentos pequenos, simples, coloridos vermelho/verde/azul/amarelo são emitidos em pontos onde as peças são destruídas. Sem pedras enormes, bordas douradas ou cristais complexos.
- Cada célula atravessada pode ter um microflash branco e um anel rápido sem criar uma grande explosão.
- **Sem fogo, fumaça, nuvens cartoon, glow exagerado, streaks que tapem as peças ou explosão circular constante.**

## Máscaras a usar
- `01_aura_item_suave`: aura atrás do item.
- `02_glow_ponta_dourado`: glow da ponta.
- `03_cauda_halo_largo`, `05_cauda_faixa_dourada`, `04_cauda_coracao_branco`: 3 níveis distintos do rastro luminoso.
- `06_fita_roxa_superior`, `07_fita_roxa_inferior`, `08_fita_rosa_intermediaria`, `09_fita_halo_difuso`, `10_residuo_ondulado`: curvas/fitas em segundo plano; usar com alpha baixo, cores distintas e variância.
- `11_sparkle_quatro_pontas`, `12_sparkle_seis_pontas`, `13_sparkle_mini`, `14_ponto_luz`, `15_risco_velocidade`: brilhos adicionais.
- `16_fragmento_irregular`, `17_fragmento_pentagono`, `18_fragmento_triangulo`, `19_fragmento_quadrado`: peças quebradas colorizáveis.
- `20_flash_impacto_celula`, `21_anel_impacto_celula`: impactos localizados.
- `22_whizz_frontal`: expansão sutil da energia em torno da ponta.

## Implementação
1. Antes de programar, inspecione como o Line Cleaner e a Flecha Postal já se movem, como funciona o tabuleiro (world-space sprites, Canvas UI ou híbrido), quais são o render pipeline e as Sorting Layers.
2. Preserve a lógica de alvo, tempo, velocidade, destruição das células, cascatas e eventos já existentes. **Apenas adicione o VFX visual**.
3. Faça a animação acompanhar a **posição real do booster**, não use um timer independente que possa desincronizar. O ponto de emissão da cauda deve ficar perto das penas, no lado oposto à direção do voo.
4. **Separe dois espaços de simulação:** glow de ponta acompanha a Flecha Postal; cauda, sparkles e fragmentos deixam rastros no mundo/tabuleiro e se dissipam depois que a flecha passa. Não coloque tudo como filho da flecha em movimento.
5. A cauda deve se alongar durante o voo, ter borda final suave e sumir 0,2–0,35 s após o booster sair. Uma trilha dinâmica/mesh com UV animado ou partículas é preferível a um único SpriteRenderer esticado a largura da linha.
6. As duas fitas devem oscilar com curvas discretas, com phase offsets diferentes, sem cruzar o núcleo 20 vezes. Se usar os PNGs estáticos, distribua-os em segmentos ao longo da trilha; para qualidade premium, gerar curvas procedurais suaves que reutilizem as máscaras como texturas de opacidade.
7. Use o tint dos sprites brancos para definir as cores e materiais. Considere material aditivo apenas para luz, `alpha blended` para fragmentos; teste sem depender de bloom da câmera.
8. Cores de referência: núcleo branco-quente, halo dourado `#FFC54D`, fita magenta `#EE70F1`, segunda fita violeta `#9F5AF2`, sparkles dourados, fragmentos correspondentes às cores das peças atingidas.
9. Rotacione toda a orientação do VFX para esquerda, direita, cima e baixo com base no vetor de movimento. A flecha de apresentação tem orientação diagonal; adicione parâmetro de offset angular do sprite caso necessário.
10. Implemente pooling, parâmetros serializados no Inspector, reset limpo ao encerrar fase e múltiplas flechas em paralelo sem instanciar coisas ilimitadas.

## Preset inicial (ajustável pelo Inspector)
- Cauda visível: 1,8–3,0 células para trás da flecha.
- Núcleo branco: espessura 0,09–0,14 célula; faixa dourada: 0,2–0,35; glow externo: 0,35–0,65.
- Ondulação vertical das fitas: 0,10–0,24 célula.
- 8–14 sparkles simultâneos pequenos, 0,06–0,15 célula.
- Fragmentos curtos com emissão em cada tile destruído; 3–6 por peça (respeitar total máximo por frame). 0,06–0,18 célula.
- Microflash por tile: 70–110 ms; anel por tile: 110–160 ms.
- Decaimento de cauda: 0,2–0,35 s.

## Entrega exigida
Implementação funcional integrada ao projeto Unity, scripts e prefabs/configuração; sem alterar o gameplay, com preview de teste no Editor e parâmetros de ajuste fáceis. Informe quais arquivos do projeto foram modificados. O mock é referência visual, não uma textura a colar inteira em cima do tabuleiro.
