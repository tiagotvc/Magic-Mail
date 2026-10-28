# Automatic island compositions

The map resource owns seven shared recipes: five normal environments and two Christmas environments. The initial route contains 200 positions on 14 islands of 15 levels, with five positions on the last island. The gameplay catalog currently contains 100 authored levels; the editor displays the remaining positions for scenery planning.

Island 1 retains its original settings and authored placements. The automatic generator starts on island 2. The cadence is normal, normal, Christmas; Christmas models alternate between Dispatch and Ribbon Rescue. Normal models cycle through Letter Gardens, Ink Springs, Melody Lagoon, Blossom Post and Ember Stampworks. Subsequent appearances rotate the selected trees and buildings while keeping predictable composition anchors.

In the map editor, expand **Automatic island sequence**. Edit the shared recipe sprite lists, sizes, density and sky colors. **Recompose islands 2+ and save (keep island 1)** applies the recipe configuration and clears manual overrides from islands 2–14. It preserves island 1. Individual objects remain editable; saving a disposition makes a manual override for that island until the next recompose.

Buildings and trees use frontal sprites and face the camera. Ground props lie on the terrain. Puddle footprints receive additional longitudinal room so their depth and highlights remain legible. Placement reserves room for the road, bank edges, water crossings and nearby objects. The last partial island uses fewer accents.

All colored artwork must follow the strong glossy finish in [ART-DIRECTION.md](ART-DIRECTION.md), using the approved purple cottage and HUD as material references. The Christmas cottage, Christmas depot and stamp workshop use the V74 revision with broad white reflections and saturated enamel surfaces. Their prompts and selected paths are in [glossy-buildings-v74-manifest.json](glossy-buildings-v74-manifest.json). Existing library entries and shared recipe references point to the revised artwork, without changing object placement or island settings. Distant sky silhouettes remain flat shadows.

Recipes include one glossy frontal stamp monument on the left bank, with an editable sprite list and size. Purple/cream royal and curved teal versions decorate normal models; the ruby Christmas stamp decorates seasonal models. Placement reserves room before optional vegetation and retains existing tree/prop source IDs. The authored first island is excluded. See [STAMP-MONUMENTS.md](STAMP-MONUMENTS.md) for asset and editor details.

Seasonal snow, skyline silhouettes, characters and Santa surprise events are restricted to Christmas recipes on the generated islands. The authored first island retains its placements and terrain settings; its shared Christmas sky uses the updated shadow artwork. Santa portraits peek from behind vegetation. Ribbon Rook uses a full-body sprite, with a ground shadow and no vegetation covering his face or present.

The sky is static by default across the map: clouds, meteors, sky weather particles and moving celestial effects are disabled. The Christmas backdrop combines larger shadow houses and mountains with a solid shadow star and a larger stationary gift-thief silhouette. The full-color character is available in the Characters library and in both Christmas recipes. Edit its shared size and sky silhouette position in the island controls. Art prompts, selected paths and the V72 backdrop refinement are recorded in [ribbon-rook-v71-manifest.json](ribbon-rook-v71-manifest.json). Native validation checks the fixed sky over 137 seconds, all four Christmas placements, unchanged first-island placements and seven renders with zero Unity errors.

The original island and layout resources are backed up in `Library/MapValidation/auto-islands-v70/backup`. First-island settings and authored chapter data were compared byte-for-byte; placement signatures were also compared in Unity. Validation renders all 14 islands, checks season boundaries and runs the actual editor recompose/save action.

New sprite files and their ImageGen prompts are recorded in [auto-islands-v70-manifest.json](auto-islands-v70-manifest.json).

## Initial sequence

| Island | Levels | Model |
|---|---|---|
| 1 | 1–15 | Authored island, preserved |
| 2 | 16–30 | Ink Springs |
| 3 | 31–45 | Christmas Dispatch |
| 4 | 46–60 | Melody Lagoon |
| 5 | 61–75 | Blossom Post |
| 6 | 76–90 | Christmas Ribbon Rescue |
| 7 | 91–105 | Ember Stampworks |
| 8 | 106–120 | Letter Gardens, second art variation |
| 9 | 121–135 | Christmas Dispatch, second variation |
| 10 | 136–150 | Ink Springs, second variation |
| 11 | 151–165 | Melody Lagoon, second variation |
| 12 | 166–180 | Christmas Ribbon Rescue, second variation |
| 13 | 181–195 | Blossom Post, second variation |
| 14 | 196–200 | Ember Stampworks, partial island |
