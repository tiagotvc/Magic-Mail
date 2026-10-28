# Stronger downward terrain curve V97

The terrain itself now rolls downward more strongly. The current kit radius changes from **26 to 20**, a curvature-strength multiplier of **1.3**. The earlier V96 increase to the trail's left/right S shape is reverted: `pathSway` is **1**. The cross-map arc parameter remains **1.55**; it continues to share the overall world radius.

Simply reducing radius would lower the horizon and enlarge the sky. `WorldMapKit.SetGroundCurveStrength` instead refits camera pitch/height while preserving the projected near-ground anchor and crest at 9:16. For the current kit this gives pitch **36.634096** and camera height **5.544536**. Horizontal camera framing, sprite dimensions, authored level/lateral anchors and object heights remain unchanged.

The editor exposes **Curvatura do chão (para baixo)**, with range **0.8–1.4**, independently from **Arco horizontal do chão** and **Curvas da trilha (esq./dir.)**. Editing records Undo on the kit, marks it dirty and uses the existing preview rebuild. **Salvar ilhas e ambiente** saves dirty assets through its existing SaveAssets action, including the kit. Camera/radius values are derived together instead of storing conflicting strength settings. Resetting strength to 1 recovers the earlier radius and framing.

All terrain, buttons, scenery contacts, ground-conformed props, water, bridges and picking use the same radius/camera data. The V89 readable frontal scenery hinge remains in place. No new runtime update loop or geometry density is introduced.

`GroundRollV97Validation` checks stronger downward world deformation, preserved near/crest framing, neutral S shape, unchanged opening object source data, **40 actual token hit tests**, **13 editor ground picks**, normal/seasonal/crossing renders, and both 720×1280 and 360×780 phones. It runs in the isolated Unity preview project and restores temporary Resource changes without saving them. Main runtime and isolated editor compilation pass without errors. Native screenshots are under `previews/ground-roll-v97/`; report: `ground-roll-v97-validation.txt`.
