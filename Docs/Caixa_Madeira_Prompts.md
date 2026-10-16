# Caixa de madeira — arte frontal

Gerada e editada com a ferramenta integrada `image_gen.imagegen`, com fundo transparente. As imagens foram copiadas sem editar os pixels.

Destinos ativos: `Assets/RoyalAves/Art/Obstacles/ParcelStack/CM_pilha_estado01_00.png` até `CM_pilha_estado04_00.png`. Todos são RGBA 1254 × 1254. Os GUIDs existentes foram preservados.

Referência da base: sprite de pacote arredondado do projeto. Revisão adotada: caixa completamente frontal, cantos arredondados, textura de madeira simplificada, sem topo ou laterais visíveis.

A referência original de papel está preservada em `Backups/ParcelStack-Kraft-Before-WoodenCrate/CM_pilha_estado04_00.png`. As edições dos estados 1, 2 e 3 usaram a caixa frontal de quatro vidas em `Backups/WoodenCrate-v2/CM_pilha_estado04_00.png`.

| Resistência | Travessas | Arquivo |
| --- | --- | --- |
| 4 | Superior, central e inferior | `CM_pilha_estado04_00.png` |
| 3 | Central e inferior | `CM_pilha_estado03_00.png` |
| 2 | Inferior | `CM_pilha_estado02_00.png` |
| 1 | Nenhuma; painel fechado | `CM_pilha_estado01_00.png` |

## Prompt da base

```text
Use case: precise-object-edit. Edit this rounded kraft parcel game sprite into a WOODEN CRATE while matching its very simplified, rounded casual-game art style. The only visible surface is a perfectly FRONTAL square face: straight-on orthographic view, horizontal and vertical lines parallel to the canvas. ABSOLUTELY NO TOP FACE, NO SIDE FACES, NO PERSPECTIVE, no cube projection or angled edges. Preserve the reference's friendly rounded-square silhouette, generous rounded corners, warm golden-orange palette and smooth broad cartoon highlights. Replace paper and tapes with a chunky rounded WOODEN outer rim enclosing a closed front panel made of four broad vertical wooden boards. Show EXACTLY THREE simple removable horizontal reinforcement bars across the panel, at upper, middle and lower heights, with softly rounded ends and two small cute gray nail heads on each. Make every shape plump, simple, clean and easy to read at tiny match-3 tile size. Wood is a few flat honey/ochre tones with at most ONE or TWO short broad curved grain strokes per board; NO dense wood texture, NO realistic knots, NO fibers, NO photographic material. No realistic shipping-box look. This should look like a stylized cartoon puzzle-game blocker icon, with the same softness as the attached parcel sprite, not a rendered real wooden crate. Center the large single closed crate on the SAME square canvas with equal transparent margins. True transparent background. No seal, tape, ribbons, logo, crown, writing, scene, floating parts or ground shadow.
```

## Estado com 3 vidas

```text
Use case: precise-object-edit. Edit this exact rounded wooden-crate game sprite into a damage state. The crate is completely FRONT-FACING, with no top or side faces. Preserve its EXACT square canvas, outer silhouette, size, position, rounded permanent outer rim, warm honey colors, broad cartoon highlights, simplified wood grain and four CLOSED vertical front boards. Do not introduce perspective, realism, openings, shrinkage, extra objects, writing or shadows. Preserve true transparency. Remove ONLY the UPPERMOST removable horizontal reinforcement bar and its two gray nail heads (the bar near 30% from the top). Keep the center and bottom bars exactly where they are. Reveal the closed vertical boards behind the removed bar, with seams and simplified grain matching the visible boards below it. Add two tiny subtle nail holes where that bar was attached. This state has EXACTLY TWO removable horizontal bars, plus the unchanged permanent outer rim.
```

## Estado com 2 vidas

```text
Use case: precise-object-edit. Edit this exact rounded wooden-crate game sprite into a damage state. The crate is completely FRONT-FACING, with no top or side faces. Preserve its EXACT square canvas, outer silhouette, size, position, rounded permanent outer rim, warm honey colors, broad cartoon highlights, simplified wood grain and four CLOSED vertical front boards. Do not introduce perspective, realism, openings, shrinkage, extra objects, writing or shadows. Preserve true transparency. Remove the UPPER and CENTER removable horizontal reinforcement bars and their four gray nail heads. Keep ONLY the BOTTOM removable bar exactly in place (near 70% from the top), with its two original gray nail heads. Reveal the closed vertical boards in the two cleared areas, continuing the same seams and very simple grain from the original panel. Tiny subtle nail holes may remain. This state has EXACTLY ONE removable horizontal bar, plus the unchanged permanent outer rim.
```

## Estado com 1 vidas

```text
Use case: precise-object-edit. Edit this exact rounded wooden-crate game sprite into a damage state. The crate is completely FRONT-FACING, with no top or side faces. Preserve its EXACT square canvas, outer silhouette, size, position, rounded permanent outer rim, warm honey colors, broad cartoon highlights, simplified wood grain and four CLOSED vertical front boards. Do not introduce perspective, realism, openings, shrinkage, extra objects, writing or shadows. Preserve true transparency. Remove ALL THREE removable horizontal reinforcement bars and all SIX of their gray nail heads. Keep the entire permanent rounded outer wooden rim untouched. Reveal one fully CLOSED front panel of four broad vertical boards, with the same vertical seams and sparse broad cartoon wood strokes continuing through where the bars used to be. At most six tiny subtle nail holes remain. This state has ZERO reinforcement bars. The box stays fully closed, full-sized and square; it does not open or become an empty frame.
```

A versão anterior de papel foi guardada em `Backups/ParcelStack-Kraft-Before-WoodenCrate`. As imagens adotadas também estão em `Backups/WoodenCrate-v2`. O teste isolado contém quatro caixas com resistências 1, 2, 3 e 4; a meta inicial é 4.
