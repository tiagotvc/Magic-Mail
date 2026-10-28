# Glossy island crossings

The shared `RoyalAvesWorldMapIslands` resource controls the game and map editor.

- Islands span 15 level positions. A gold token with a central star marks every 30th position; this changes map presentation, not gameplay rules or progress.
- Capacity stays at 510 planned positions. Existing authored objects retain their absolute saved level; manual chapters were regrouped for the new island boundaries.
- Ground colors cycle through eight palettes. The Christmas sky remains global.
- Liquid themes: Clear, Ink, Symphony, Lava, Crystal. Each supports gloss, custom liquid color, bank shape, channel width/depth and water motion.
- Choose Stones and set `Levels on lake stones` to extend the lake around the island's final 1–4 levels.
- `Individual stones`: level offset 0 is the island's final level, -1 the preceding level. Adjust lateral position and size independently. Empty entries use centered defaults.
- `Individual aquatic plants`: progress 0–1 along the crossing, lateral position and size. An empty list uses three sparse default pads. Lava uses cooled dark pads with warm highlights.
- Symphony exposes note count and rise speed. Eight pooled notes are shared by visible crossings; mesh geometry is reused.
- Land objects whose saved positions now fall inside a channel are not rendered over the water. They remain in the editor's object list and can be moved back onto land.

Validation: `GlossyCrossingsValidation.Run` checks pacing, capacity, saved crossing controls, optimized shoreline calculations, native captures across nine map positions, mesh finiteness and shader compilation. `MapCostValidation.Run` benchmarks the isolated editor and checks terrain normals across regions; its CPU timings are not device FPS.
