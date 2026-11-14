# Pacote explosivo ? Explosion.tif

Substitui os efeitos rejeitados anteriores SOMENTE na explos?o do pacote explosivo. Fonte fornecida pelo usu?rio: Downloads/Explosion.tif, 2048?2048 RGB, grade 6?6 (36 quadros). C?pia de produ??o byte-id?ntica em Assets/RoyalAves/Art/Effects/PackageExplosion/Explosion.tif; hash em Tools/asset-imports/explosion-tif/manifest.json.

Material Explosion, albedo laranja e emiss?o HDR, Fade/Multiply, interpola??o entre quadros por UV2 + AnimBlend. Alpha da cor ? transportado por Color, sem duplicar o stream. A textura RGB n?o tem alpha: o shader deriva transpar?ncia da luminosidade, evitando fundo preto retangular.

O projeto usa URP. O material usa o shader dedicado RoyalAves/Explosion, equivalente ao comportamento solicitado de Particles/Standard Unlit, que ? do pipeline Built-in. Soft Particles/Far Fade 2 s?o expostos; a atenua??o por profundidade fica desligada no tabuleiro 2D sem interse??o de ch?o, para n?o ocultar o efeito com um depth buffer inadequado. Receive Shadows est? marcado, mas o acabamento ? Unlit.

Particle System ap?s ajuste solicitado: dura??o 1 s, lifetime aleat?rio 0,28?0,42 s, velocidade 0, tamanho 2?3, rota??o -360??360?, esfera de raio 0,35; 3 part?culas por burst com 5 ciclos a 35 ms, emiss?o cont?nua 0. Texture Sheet Animation Whole Sheet 6?6, uma passagem por vida; cores preto ? branco, alpha inicial zero e desvanecimento final. Renderer Distance, Max Particle Size 5, flip X/Y 0,5, camada Item mask (?ndice 2 do projeto).

Exce??o intencional ao tutorial: n?o h? loop nem prewarm. Este ? um efeito pontual de gameplay; prewarm em sistema cont?nuo consumiria a sequ?ncia antes do hit e repetiria a explos?o. O controlador avan?a s? esse efeito em passos locais de at? 50 ms por quadro, sem alterar o rel?gio global, e o remove ap?s 0,8 s de simula??o visual. Preserva a absor??o dos pacotes, alcance de dano e pausa da gravidade. A combina??o usa o mesmo efeito 35% maior.

Novo recurso: Assets/RoyalAves/Resources/Effects/PackageExplosionFlipbook.prefab. O c?digo n?o carrega mais o prefab GolemKin/cartunesco nem a onda procedural. O gerador do menu foi atualizado para produzir somente esta nova sequ?ncia.

Verifica??o e capturas: Logs/package-explosion-asset.result, Logs/explosive-package-lab-smoke.result e Logs/explosive-package-imported-start.png. N?o h? alega??o de teste f?sico, ?udio ou fluxo completo de vit?ria.

A importa??o direta do TIFF apresentou riscos no Unity. A vers?o de execu??o ? Explosion.png, convers?o lossless verificada com RGB decodificado id?ntico ao TIFF. O TIFF original permanece preservado. As UVs e o blend foram conferidos no mesh real de part?culas.

?ltimo ajuste solicitado: dura??o vis?vel m?xima aproximada de 0,56 s (?ltimo burst em 0,14 s + vida m?xima 0,42 s), em vez de cerca de 1,3 s. Alpha do material 0,4, alpha de pico das part?culas 0,65 e dissipa??o antecipada. A simula??o local protegida pode alongar o tempo de parede em quadros lentos; o rel?gio do jogo n?o ? alterado.

Valida??o final ap?s redu??o de dura??o/opacidade: PASS simples (48 impactos) e combina??o por troca (79), sem alterar maximumDeltaTime; descarte autom?tico e pausa antes da gravidade mantidos. Material alpha 0,4 conferido no asset instalado. Pico e dissipa??o inspecionados na Game View, sem os riscos da importa??o TIFF anterior. Resultado preservado em Tools/asset-imports/explosion-tif/native-verification.txt. Laborat?rio reaberto na combina??o.
