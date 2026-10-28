# Top HUD v10

Four individual built-in image_gen assets in `Assets/RoyalAves/Art/RoyalPostmanUI2026/`: `top-plus-base-v10.png`, `top-plus-symbol-v10.png`, `top-settings-base-v10.png`, `top-gear-cream-v10.png`. Selected prompts and source filenames: [TOP-HUD-V10-PROMPTS.json](TOP-HUD-V10-PROMPTS.json). Source PNG pixels remain unchanged.

Correction targets the rejected v9 line-like circular transition and internal symbols. Plus uses a narrower cream cross (34/64 control width); settings uses a larger cream gear (80/130 control width). Each symbol sits under MovingCenter; the fixed shoulder and independently masked painted face preserve click travel. Native gear aperture remains separate from the source pixels.

Avatar, currency/lives, counter panels, main level/Pet and navigation are outside this change. Native review renders are in `review/top-hud-v10.png` and `review/top-hud-v10-pressed.png`. These show the fabricated prefab, not the user's already running Play session. Visual approval remains with the user.

Validation completed: Unity native build/capture returned `ROYAL_POSTMAN_KIT_OK`, exit 0. Existing integration checks returned `ROYAL_POSTMAN_BINDINGS_OK`, exit 0, including circular fixed shoulder/moving center, top callbacks and independent avatar. Updated texture metadata, native Sprite assets, materials and Home/CoinPlus/LifePlus/SettingsButton prefabs are installed in the main project. No gameplay or save changes.
