# Ribbon Rook standing character

The V73 illustration replaces the crouching map character in both shared Christmas recipes and the existing Characters library entry. It preserves the original V71 sky silhouette. The character stands on two natural legs with a shared foot baseline; empty arms are separated from the torso, and the gift sack is an independent prop.

## Cutout source

`Assets/RoyalAves/Art/MapKitV3/RibbonRookV73/ribbon-rook-cutout-parts-v73.png` contains six imported sprites: Body, LeftArm, RightArm, MouthClosed, MouthOpen and GiftSack. The body is painted without arms or beak so moving a part does not reveal a duplicate baked into the torso or face. Mouth sprites replace the complete beak for closed/open speaking poses.

The prefab is `Assets/RoyalAves/Characters/RibbonRookV73/RibbonRookV73.prefab`. LeftArm and RightArm have shoulder pivots; Mouth and GiftSack are independent objects. `RibbonRookCutout.SetSpeaking(bool)` switches mouth poses, and its component context menu previews each pose. Transform tracks can animate each arm. No Update loop or automatic animation is attached. The map currently uses the complete standing illustration; this separate prefab is the source for character animation work.

Rebuild the prefab and atlas slices from **Royal Aves / Personagens / Build Ribbon Rook cutout**. The builder imports the six regions, trims their transparent margins through sprite metadata, and saves the prefab and material. It does not modify the raster artwork.

Art was generated and edited with built-in ImageGen. Sources, prompts, selected paths and GUIDs are recorded in [ribbon-rook-v73-manifest.json](ribbon-rook-v73-manifest.json). Native validation renders the neutral prefab and a pose with rotated arms and an open mouth, then checks Christmas placements and the preserved first-island signature.
