# Independent trail curves V96 (current value restored by V97)

The independent trail control remains available, but the current `RoyalAvesWorldMapIslands.pathSway` is **1**, restoring the original left/right S shape. The active requested change is the [stronger downward terrain roll V97](GROUND-ROLL-V97.md). V96's 1.35 screenshots and harness record an earlier visual experiment, not the current production value. The island controls expose **Curvas da trilha (esq./dir.)** and **Arco horizontal do chão** separately, with Undo/dirty/rebuild handling through the existing editor flow.

`WorldMapRoute.CenterX` applies the multiplier to its existing continuous wave, retaining its wavelength, initial phase, crossing easing and first-mission transition. Terrain, level buttons, props, ground details, shadows, bridges and hit testing already share that route center, so the composition follows the stronger bend consistently. Stored first-island object IDs, sprites, levels, lateral offsets and heights are untouched. The data change adds only the new pathSway setting; there is no layout rewrite, regeneration or per-frame allocation.

The serialized field defaults to **1** for existing/other maps, preserving their earlier shape until configured. The editor slider supports **0.5–1.8**. Returning the current map to **1** restores its earlier S-curve; it does not change the horizontal ground arc.

`PathSwayV96Validation` checks 400 route samples against the earlier phase at a 1.35 multiplier, straight crossing centers, 32 actual projected token hit tests, unchanged opening scenery source data, and native normal/seasonal/crossing views plus two phone proportions. Main runtime compilation and isolated editor compilation pass with zero errors. Native before/after screenshots are under `previews/path-sway-v96/`.

`LateralArcV96Preview` is a separate read-only native experiment comparing the cross-map arc with 1.95. Its changes exist only in isolated Resource memory, are restored, and are not saved into the production asset.
