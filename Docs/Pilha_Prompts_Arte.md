# Pilha de Encomendas — prompts de arte

Conjunto anterior, substituído por solicitação do usuário. A direção atual é um pacote de tamanho constante com embrulho rasgado; consulte `Pilha_Prompts_Embrulho.md`. As imagens antigas estão em `Backups/ParcelStack-pilha-v1`.

Gerados com a ferramenta integrada image_gen, com transparência real. Referências de estilo: parcel.png, gift.png e stamp.png existentes no projeto. Os estados 1–3 são edições da nova pilha de quatro caixas.

Arquivos finais em `Assets/RoyalAves/Art/Obstacles/ParcelStack/`, nomeados `CM_pilha_estado01_00.png` a `CM_pilha_estado04_00.png`. Os PNGs originais gerados foram copiados sem edição de pixels. O importador alinha a base visual dos quatro estados pela leitura do alpha.

+## Estado 1

Use case: precise-object-edit. Create resistance state ONE from the referenced four-parcel match-3 obstacle. Exactly ONE kraft-paper postal parcel remains. Reuse the visual design of the bottom left parcel: rounded golden kraft box, cream blank postal label, beige twine and slightly overhead frontal perspective with upper-left lighting. Place the single box horizontally centered on the SAME square transparent canvas, its bottom at exactly the same baseline as the original pile; preserve the original bottom parcel height and scale. Nothing above or beside it. Match material, highlights and contours, no red seals. Truly transparent alpha PNG RGBA. Do not crop the square canvas or vertically recenter the shorter single box; preserve empty transparent space above for stable game alignment. No text, numbers, effects, ground shadow, background, UI, watermark.

## Estado 2

Use case: precise-object-edit. Edit this FOUR-parcel sprite into resistance state TWO for the same mobile match-3 obstacle. Remove the uppermost parcel AND the middle parcel, leaving exactly the TWO side-by-side bottom parcels unchanged. Preserve their exact positions, sizes, bottom anchor, perspective, golden kraft shading, blank cream labels and twine. Keep the exact square canvas dimensions. Removed regions must become genuinely transparent alpha. Do not recenter, enlarge or crop. No ground shadow, background, text, numbers, extra items, effects, or UI. Consistent damage-state PNG RGBA; keep the surviving parcels pixel aligned to the reference.

## Estado 3

Use case: precise-object-edit. Edit the referenced newly generated FOUR-parcel game sprite into resistance state THREE. Remove ONLY the uppermost single parcel. Keep the TWO bottom parcels and ONE middle parcel absolutely unchanged in position, dimensions, labels, twine, contours, color, lighting and perspective. Exactly THREE parcels remain. Do NOT recenter, rescale or crop the remaining pile; preserve the exact square canvas dimensions and bottom anchor from the reference. Deleted upper region becomes genuinely transparent alpha. No ground shadow, background, numbers, lettering, new objects, effects or UI. PNG RGBA transparent. This is a damage-state sprite for a match-3 cell, so pixel alignment of unchanged lower parcels is essential.

## Estado 4

Use case: stylized-concept. Create ONE production sprite for the mobile match-3 game Correio Mágico: Pilha de Encomendas, resistance state 4. Reference images provided are style references only, not edit targets. Exactly FOUR distinct small kraft postal parcels piled in a compact stable arrangement (two at bottom, one middle, one top), with rounded corners and simple beige twine around each. No red wax seals: the existing red stamp is a combinable piece and must stay distinct. Warm golden kraft paper, tiny cream postal labels without writing, beveled softly shaded forms, polished bright casual mobile game illustration matching reference gloss and clean contour, slight overhead frontal camera with top planes visible, light upper left. The count of four packages must read instantly at small cell size. SINGLE isolated sprite on REAL transparent alpha, square 1024x1024 canvas. Fit object in x=120..904, y=140..880 with empty transparent padding, bottom center anchor at (512,880). Wide bottom parcel establishes base width. No scenery, floor, background, rectangular shadow, text, numerals, UI, frame, particles, watermark, extra objects. This is first of consistent 4-to-1 damage states; retain base geometry for subsequent edits.
