# Lateral map mist

V79 adds pale mint-white, dark petrol and dark violet mist ribbons. The transparent PNGs live in `Assets/RoyalAves/Art/MapKitV3/MistV79`. Their exact ImageGen prompts, original source files and installed paths are in [mist-v79-manifest.json](mist-v79-manifest.json). Generated originals are retained.

All three are available in the editor's Effects library as Pale mint mist, Dark petrol mist and Dark violet mist. Choose a sprite, place it, adjust size/position, flip it and use **Mist opacity**. Saving the layout preserves opacity and the selected artwork through the existing placement data. Shared automatic island recipes expose **Mist**, **Mist size** and **Mist opacity** for editing a theme once across its repeated islands.

Each automatic island receives two sparse bank ribbons. Normal green/Christmas recipes favor pale mist; Ink Springs and Ember Stampworks favor dark variants; other recipes mix them. Existing scenery source IDs and positions are retained. The first authored island receives no changes, and saved layout resources are not rewritten.

`PropKind.Mist` is appended as value 18, preserving all existing serialized enum values. It uses the existing Vapor alpha-blend material rather than the cutout card shader, with full-rect imported sprites to retain their soft alpha fades. Mist faces the map camera, stays low above the terrain, has no shadow, snow overlay, rise, sway or opacity pulse. Three cached texture materials and pooled card meshes are reused; no new particle system or continuous editor repaint is added.

Run `node Tools/level-map/install-mist-v79.cjs` to reinstall artwork/library items and shared recipe references. The installer preserves the raw island settings tail and does not edit layouts. Native validation `MistV79Validation.Run` verifies first-island signatures, automatic placements, actual alpha-blended renderers, full-rect imports, layout roundtripping and stationary transforms/opacity at two different times, then exports theme previews without saving a layout.
