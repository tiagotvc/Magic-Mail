# Ground palette V80

The normal lawn now uses warm sage and olive tones, with a yellow undertone shared by the cream/sand road. The opening no longer turns blue-green toward the horizon. Road shoulders have less orange, and the broad bank shadows preserve leaf-green instead of multiplying in blue. Diagonal patches and subtle surface highlights remain anchored in world space.

The theme palettes use softer lawn colors: lavender, muted rose, sea green, warm volcanic mauve, sand, icy blue and periwinkle. Light warm paths keep level markers readable while the five normal and two Christmas island recipes retain their existing sequence and artwork.

`MapSurface.shader` defines illustrated ground gradients and shading; `WorldMapIslands.IllustratedPalette` supplies theme defaults; `WorldMapRoute.BasePalette` supplies normal ground/edge/detail defaults. `_PathMint` now follows the explicit mint palette option: unified illustrated mode no longer forces green paths on later normal islands after the entrance grade ends. Authored custom palette values still take precedence in palette selection.

This is a ground-only palette change. No image assets, positions, island layout resources, HUD colors, sky artwork, water palettes or progress data are rewritten. The first island's color presentation changes as requested; its authored prop signatures remain identical.

`GroundTonesV80Preview.Before/After` exports native Unity map renders for the opening, scrolling, normal themed islands, winter, crossings and the later green recipe. Final captures are under `previews/ground-tones-v80`; the validation report records compilation/render errors and the first-island placement check. Original source backups and initial captures remain under `Library/MapValidation/ground-tones-v80`.
