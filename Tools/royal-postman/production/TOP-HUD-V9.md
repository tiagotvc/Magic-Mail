# Top HUD v9

Focused correction of the rejected v8 circular controls and internal symbols. Four individual built-in image_gen PNGs in `Assets/RoyalAves/Art/RoyalPostmanUI2026/`: `top-settings-base-v9.png`, `top-plus-base-v9.png`, `top-gear-cream-v9.png`, `top-plus-symbol-v9.png`. Exact selected prompts: [TOP-HUD-V9-PROMPTS.json](TOP-HUD-V9-PROMPTS.json). No atlas and no raster pixel postprocessing.

The new purple base has a quieter broad center transition; the teal base removes the electric cyan rim. The cream symbols use shallow relief without the old white edge tracing. The fixed circular artwork remains opaque behind its independently moving face, preventing a dark compositing seam. The gear has a native circular aperture to keep its hole clear at HUD scale; source pixels are unchanged. PNGs remain uncompressed, mip-free and at their original resolution.

Avatar, counters, coin, heart, lower navigation and the main Nível/Pet dimensions remain as before. Native review renders: `review/top-hud-v9.png`, `review/top-hud-v9-pressed.png` and `review/home.png`.

Validation: native fabrication/capture completed with `ROYAL_POSTMAN_KIT_OK`; the existing home/modal/ad integration suite passed with `ROYAL_POSTMAN_BINDINGS_OK`. Actual Editor assembly compilation passed. Selected source metadata, native Sprites, materials and consuming Home/CoinPlus/LifePlus/SettingsButton prefabs were copied into the active project. These are native prefab review renders, not a screenshot of the user's already running Play session.
