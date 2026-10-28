# Glossy ink puddles

V78 introduces three transparent overhead ground sprites: sky blue, violet and turquoise. Rounded irregular silhouettes, a broad crescent reflection and colored liquid rims keep them readable at map size. No bottle, splash particles, baked grass, animation or standing card.

The three items appear in the editor's Props library as Sky blue ink puddle, Violet ink puddle and Turquoise ink puddle. Their default size is 0.88. They use GroundDetail, so they conform to the map floor and remain independently movable/scalable through existing placement controls.

Shared automatic recipes choose colors: Letter Gardens uses blue, Ink Springs cycles all three, Melody Lagoon uses turquoise, Blossom Post uses violet and blue. Small stones remain in each selection. Christmas and volcanic recipe palettes are unchanged. The authored first island and saved placement resources are untouched. The old V70 artwork remains available for existing authored placements.

Source paths, exact prompts and installed paths are recorded in [ink-puddles-v78-manifest.json](ink-puddles-v78-manifest.json). Artwork was created with the built-in ImageGen tool, with original generated files preserved. Run `node Tools/level-map/install-ink-puddles-v78.cjs` to reinstall assets and their shared recipe references without changing the island settings tail or saved layouts.

Native visual validation: `InkPuddlesV78Validation.Run` in the isolated Unity preview project. It captures automatic placements and a disposable three-color composition, checks actual ground renderers, variant coverage, first-island signatures and Unity errors. It never saves layout data.
