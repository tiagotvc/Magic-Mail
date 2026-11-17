# Royal Postman — VFX 4 efeitos / 30 máscaras

Sprites **originais, feitos proceduralmente** para a Unity. Cada PNG é um arquivo individual RGBA, RGB branco e canal alpha real, sem checkerboard ou fundo. Colorir/materializar em runtime na Unity.

As máscaras não são frames de animação; combine e anime os elementos no Unity. O kit NÃO inclui o sprite do booster, nem altera suas artes existentes. Use o mock apenas como referência de direção visual.

## Conteúdo

- `00_Comuns`: 7 sprites de glow, sparkles e fragmentos.
- `01_Match_Normal`: 4 sprites para match delicado (um anel e 3 instâncias do sparkle comum).
- `02_Bomba_Area`: 6 sprites de flash radial, anéis de choque e raios.
- `03_Line_Cleaner`: 7 sprites de feixe, glow, duas fitas sinuosas, frente de impacto e poeira fina.
- `04_Seeker_Impacto`: 6 sprites de alvo, impacto, arco e rastro curto.
- `Previews/atlas_30_mascaras.png`: atlas para identificação; **não importar como spritesheet único**.
- `Previews/mock_composicao_4_efeitos.jpg`: composição simulada, apenas demonstração.
- `Docs/PROMPT_CLAUDE_UNITY.md`: briefing técnico de integração Unity.

## Parâmetros sugeridos

| Efeito | Duração | Diâmetro/escala | Característica |
|---|---:|---|---|
| Match | 0,16–0,23s | anel até ~1 célula | 1 anel branco + exatamente 3 sparkles + fragmentos atuais |
| Bomba | 0,35–0,50s | raio baseado na lógica da bomba | 2 anéis, clarão intenso, raios e fragmentos simples |
| Line Cleaner | duração do tiro existente | feixe escalado pela linha | centro branco + halo + duas fitas e impacto por célula |
| Seeker Impacto | 0,22–0,38s | ~1–1,6 célula | flash no alvo, anel e sparkles; não é outra bomba |

## Unity

Importe como Sprite (2D and UI), com Alpha is Transparency, sem usar fundo preto. Em URP escolha Sprite-Unlit/Sprite-Lit ou shader de partículas adequado; no Built-in use Sprite/Default e materiais de partícula para glows. Tint controlada no Renderer/material (não editar PNG). PPU consistente por grupo e escala em função da célula. Para Glow/Rays, use `Additive` quando apropriado, mas preserve legibilidade e evite overdraw excessivo.

> Observação: máscaras compridas têm a direção de arte da esquerda para a direita, e podem ser espelhadas ou rotacionadas. Movimento em tempo real requer efeito de rastro, não apenas sprite fixo achatado.

## Inventário

- `Mascaras_PNG/00_Comuns/01_radial_glow.png` — 256×256 — glow radial suave; suporta cores livres
- `Mascaras_PNG/00_Comuns/02_sparkle_4_pontas.png` — 256×256 — estrela magica simples
- `Mascaras_PNG/00_Comuns/03_sparkle_6_pontas.png` — 256×256 — estrela com seis pontas
- `Mascaras_PNG/00_Comuns/04_fragmento_losango.png` — 128×128 — fragmento de peca simplificado
- `Mascaras_PNG/00_Comuns/05_fragmento_lasca.png` — 128×128 — lasca angular pequena
- `Mascaras_PNG/00_Comuns/06_fragmento_triangulo.png` — 128×128 — triangulo irregular simples
- `Mascaras_PNG/00_Comuns/07_particula_ponto.png` — 128×128 — ponto de luz pequeno
- `Mascaras_PNG/01_Match_Normal/01_anel_unico_suave.png` — 256×256 — anel branco unico, expandir 0.45 a 1.0 celula
- `Mascaras_PNG/01_Match_Normal/02_micro_clarao.png` — 256×256 — flash central discreto
- `Mascaras_PNG/01_Match_Normal/03_anel_fragmentado.png` — 256×256 — alternativa suave e eterea para o anel
- `Mascaras_PNG/01_Match_Normal/04_rastro_sparkle_curto.png` — 256×96 — esticar e rotacionar apenas para os 3 sparkles
- `Mascaras_PNG/02_Bomba_Area/01_flash_central_intenso.png` — 512×512 — nucleo branco brilhante
- `Mascaras_PNG/02_Bomba_Area/02_anel_choque_fino.png` — 512×512 — onda principal, escala e fade
- `Mascaras_PNG/02_Bomba_Area/03_anel_choque_largo.png` — 512×512 — onda secundaria com bordas difusas
- `Mascaras_PNG/02_Bomba_Area/04_raios_radiais_longos.png` — 512×512 — raios finos e assimetricos
- `Mascaras_PNG/02_Bomba_Area/05_raios_impacto_curtos.png` — 512×512 — raios curtos para a primeira pancada
- `Mascaras_PNG/02_Bomba_Area/06_aura_dissipacao.png` — 512×512 — aura que dura um pouco mais, sem nuvens
- `Mascaras_PNG/03_Line_Cleaner/01_feixe_nucleo_branco.png` — 640×192 — centro estreito intenso, estirar de acordo com comprimento
- `Mascaras_PNG/03_Line_Cleaner/02_feixe_glow_largo.png` — 640×192 — halo externo do feixe, cor propria
- `Mascaras_PNG/03_Line_Cleaner/03_fita_magica_A.png` — 640×192 — fita sinuosa roxa/rosa em movimento
- `Mascaras_PNG/03_Line_Cleaner/04_fita_magica_B.png` — 640×192 — segunda fita com fase distinta
- `Mascaras_PNG/03_Line_Cleaner/05_ponta_impacto.png` — 256×256 — clarão na frente de limpeza
- `Mascaras_PNG/03_Line_Cleaner/06_micro_anel_quebra.png` — 256×256 — onda curta na celula atingida
- `Mascaras_PNG/03_Line_Cleaner/07_poeira_luminosa_canal.png` — 640×192 — poeira fina opcional e animavel
- `Mascaras_PNG/04_Seeker_Impacto/01_marcador_alvo_anel.png` — 256×256 — alvo circular pulsante ao atingir peca
- `Mascaras_PNG/04_Seeker_Impacto/02_flash_contato.png` — 512×512 — flash instantaneo no contato
- `Mascaras_PNG/04_Seeker_Impacto/03_estrela_impacto.png` — 512×512 — pequena explosao de impacto pontiaguda
- `Mascaras_PNG/04_Seeker_Impacto/04_arco_espiral.png` — 512×512 — arco magico de chegada
- `Mascaras_PNG/04_Seeker_Impacto/05_rastro_curto_de_chegada.png` — 640×192 — rastro de entrada antes do impacto
- `Mascaras_PNG/04_Seeker_Impacto/06_micro_estrela_radial.png` — 512×512 — micro raios radiais do toque
