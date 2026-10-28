# Top HUD V15 — applied visual refinement

User authorized these top-HUD adjustments on 20 October 2026, superseding older V14 restrictions for this header. Purple/gold plus/settings controls now share one clean painted body and independent gold symbols. Currency/life cream panels align at one height; the plus controls sit at their ends. Avatar is 166 logical pixels (was 184); heart is 108 (was 128); status is 44 font size (was 59). Numbers and text remain live, portraits/frames independently replaceable, and button centers press independently of their fixed shoulders. Invisible 132-pixel targets provide 44-pixel taps on a 360-wide phone.

Eight individual original PNGs are in `Assets/RoyalAves/Art/RoyalPostmanUI2026/`, named `top-button-body-v15.png`, `top-gear-v15.png`, `top-plus-v15.png`, `top-avatar-frame-v15.png`, `top-heart-v15.png`, `top-envelope-coin-v15.png`, `top-value-panel-v15.png`, and `top-avatar-portrait-v15.png`. They were created with built-in ImageGen. Exact prompts: `TOP-HUD-V15-PROMPTS.json`; original/project paths and measured source dimensions: `TOP-HUD-V15-MANIFEST.json`. No raster resizing or pixel editing was performed. Import preserves the actual source resolution with uncompressed textures, no mipmaps and a maximum of 4096.

## Resolution limitation

The native 4K master request remains **unfulfilled**. Despite explicit 3840-pixel prompts, the built-in tool returned 1254 × 1254 square PNGs and a 2172 × 724 panel. One explicit high-resolution edit retry also returned 1254 × 1254; it was retained as an unused study. Further generations were stopped after the user's token-cost complaint. Do not label these source assets native 4K. `review/top-hud-v15/top-4k.png` is a genuine 3840-pixel-wide native Unity UI capture; its text/geometry render at that resolution, but it is not evidence of 4K source PNGs.

## Installation and checks

`TopHudV15Apply.cs` runs only in the isolated QA project. `install-top-hud-v15.cjs` copied the resulting Sprite/material assets and patched exactly 41 top-HUD component blocks in the authored `Home.prefab`. Every other Home block was preserved byte-for-byte; existing standalone top component prefabs were updated. Backup: `Library/MapValidation/AuthoringBackup/Home-before-top-v15.prefab`. No automatic prefab rebuild or runtime gameplay-code change was added. The older explicit V14 fabrication recipe must not overwrite this authored Home.

Focused native checks passed profile/currency/life/settings callbacks, fixed-shoulder/moving-center press, phone touch targets, covered-menu hide/restore and zero Unity errors. Native reviews include normal/pressed 1080-wide header, 360-wide header, 3840-wide header and the full home. Report: `review/top-hud-v15/validation.txt`. No user Unity window/session was controlled; changes are in the assets for the next instantiated Home. Player progress, wallet, gameplay, navigation and play modal were not modified.
