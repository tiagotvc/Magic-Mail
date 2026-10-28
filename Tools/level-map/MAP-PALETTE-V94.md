# Softer map palette and compact houses V94

The supplied reference has sage/olive banks and pale sandy yellow paths, with broad painted light/shadow bands. V94 lowers the orange in the playable trail and uses a creamy sand highlight, softens yellow-green highlights in the lawn, makes the normal green base more muted, and changes road-edge shadows from peach/orange to neutral warm shadows. Map vibrance changes from 10 to 4. The glossy source artwork and cream/purple/gold account/navigation HUD are preserved. Repeated themed islands keep their own pastel hues; custom palette values and snow remain available.

The changes are made in the existing ground shader and palette constants. They introduce no textures, extra shader passes, objects, animation loops or editor repaint subscriptions.

## Houses

House heights and proportional widths are 20% smaller. Seven automatic recipe building sizes change from 1.6/1.72 to 1.28/1.376, while five matching editor library defaults are reduced too. Default common-house sizing is consistent for newly placed houses. Trees, stamp monuments, characters, bridges and large factory/lighthouse landmarks retain their sizes.

The current smaller-house request also changes the two opening cottages from 1.65 to 1.32. Their level/lateral anchors, artwork, effects and other fields remain untouched, as do every other first-island object's height, position and identity. This is a narrow size change rather than a regenerated first-island composition.

`tune-map-v94.cjs` performs the recorded data change once. If its migration manifest already exists, it leaves subsequent manual tuning intact. `map-palette-v94-migration.json` records each original/new size and asset hash. Do not rerun old island installers to change this tuning.

## Validation

`MapPaletteV94Validation.Before` snapshots current first-island runtime objects and renders four baseline views. `After` checks every first-island anchor, sprite and identity, allows only the two expected 0.8 cottage heights, checks compact recipe sizes, distinct themed palettes and custom color controls, and renders seven map positions plus two narrow-phone captures. Rendering runs in the isolated Unity preview project. No store, wallet or player-progress writes are performed.

Native before/after screenshots are under `previews/map-palette-v94/`; the report is `map-palette-v94-validation.txt`. The full main runtime also compiles with zero errors, with output redirected into Library.
