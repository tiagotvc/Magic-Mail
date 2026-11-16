# Royal Postman — Universal Hint VFX Kit

## Conteudo
- 14 masks RGBA PNG with genuine alpha and white RGB. Do not tint in an art editor; use SpriteRenderer color/material in Unity.
- 3 PNG previews: six geometry cases, all texture masks, and small nudge motion.
- Full Claude Code prompt for modifying the current hint system.
- manifest.json with sprite intent and dimensions.

## Key correction
Instead of one rectangle around every pattern, compute the **outer perimeter of the union of marked cells**. No internal edges between adjacent cells. Supports 3/4/5, straight vertical/horizontal, L/T and disconnected components. A LineRenderer or simple procedural outline mesh is more reliable than building complex contours out of corner sprites.

## Recommended layering
1. Low-alpha warm gold outer halo (under items).
2. Thin pale gold path.
3. Crisp warm white core following same path.
4. 2–4 tiny detached sparkles distributed along the contour.
5. Optional tiny soft highlight under predicted match cells.
6. Nudge just one candidate sprite 0.16–0.23 cell and return; no scaling of tiles.

## Unity import
Set Texture Type = Sprite (2D and UI), alpha transparency = true, Filter Mode = Bilinear, Wrap Mode = Clamp, Mipmaps = Off. Prefer tight texture compression / RGBA8 for max fidelity. Choose the project's compatible sprite material; do not mandate a particular render pipeline.

## Files
- `Assets/HintMasks/01_border_edge_core.png` — Seamless white sharp border stroke, tile/stretch/rotate
- `Assets/HintMasks/02_border_edge_halo.png` — Soft halo underneath border stroke
- `Assets/HintMasks/03_border_corner_convex.png` — Convex outer corner quarter arc, rotate 90 degrees as needed
- `Assets/HintMasks/04_border_corner_concave.png` — Concave inset corner quarter arc for L and T shapes
- `Assets/HintMasks/05_border_light_sweep.png` — Optional small traveling glimmer along border
- `Assets/HintMasks/06_sparkle_four_point.png` — Small 4-point spark, tint white or warm gold
- `Assets/HintMasks/07_sparkle_six_point.png` — 6-point accent sparkle for subtle random detail
- `Assets/HintMasks/08_sparkle_tiny_cross.png` — Micro glint, use 2-4 sparks per hint
- `Assets/HintMasks/09_sparkle_soft_dot.png` — Soft dot for rare drifting dust particles
- `Assets/HintMasks/10_highlight_cell_soft.png` — Very subtle square-ish local glow behind matched cells; avoid seams
- `Assets/HintMasks/11_highlight_group_radial.png` — Optional soft radial fill behind target region
- `Assets/HintMasks/12_nudge_trail_horizontal.png` — Short motion streak behind moving piece; rotate for any direction
- `Assets/HintMasks/13_nudge_streak_thin.png` — Very thin optional nudge glint; do not use big arrows
- `Assets/HintMasks/14_nudge_local_glow.png` — Small glow underneath nudged sprite; fade out quickly
