# Single color-based center relief

User correction to v11: remove doubled markings. The center should have one raised-face indication almost only through color. Remove the dark upper crescent and center contour strokes; remove the dark outer annulus that reads as a second relief. The existing level button remains the material authority.

Two standalone source PNGs in `Assets/RoyalAves/Art/RoyalPostmanUI2026/`: `top-plus-base-v12.png` and `top-settings-base-v12.png`. Built-in image_gen edits of their individual v11 bodies; no raster pixel postprocessing. Exact prompts and source filenames: [TOP-HUD-V12-PROMPTS.json](TOP-HUD-V12-PROMPTS.json).

Each control retains its fixed shoulder and independently moving center/symbol. Existing separate cream glyphs, level/Pet controls, avatar, value panels, counters and bottom navigation remain unchanged. Native reviews: `review/top-controls-level-finish-v12.png`, `review/top-hud-v12.png`, `review/top-hud-v12-pressed.png`. They show prefab fabrication, not the user's already running Play session. Visual approval remains with the user.

Native build/capture passed (`ROYAL_POSTMAN_KIT_OK`) and existing integration checks passed (`ROYAL_POSTMAN_BINDINGS_OK`), including circular fixed shoulder / moving face, plus/settings callbacks and independent avatar. Generated metadata, native Sprites, materials and the Home/CoinPlus/LifePlus/SettingsButton prefabs are installed in the main project. PNGs retain original resolution, uncompressed import, no mipmaps and genuine alpha.
