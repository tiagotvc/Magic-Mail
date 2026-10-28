# Porquinho Postal V98

The Map has a permanent glossy piggy-bank shortcut in the upper-right corner, below the account HUD. V98 uses a Christmas red pig with an oversized angular head, spiky forelock, prominent rounded torso, short feet and curly tail, protruding over an emerald backing container with a purple/gold rim. The back has only an empty recessed coin slot: no coin or extra accessories. Broad white highlights and colored shadows match the bottom HUD icons. It is a static transparent sprite, imported at 512 without mipmaps. The built-in ImageGen edit prompt is `pig-badge-v98-christmas-prompt.txt`; exact source/output paths and earlier blue studies are preserved in `pig-badge-v98-manifest.json`. The final project sprite is `Assets/RoyalAves/Resources/MapEventsV98/postal-pig-badge.png`.

The shortcut remains across islands. It hides with covered menu screens and global chrome. Tapping opens a dedicated cream/gold **Porquinho Postal** presentation marked **EM BREVE**, with the same illustration and a short explanation. The map and its chrome pause/hide behind this panel; **Entendi** or the outside dimmer closes it and restores Map. This is independent of the Events coming-soon scene.

## Current scope

This delivery is the requested icon, placement and explanatory preview. No balance is displayed, no deposit is recorded, no reward is granted and no store purchase can be started. The UI does not invent a price, countdown or already-available paid redemption. Store products, generic coin bundles and player saves are unchanged.

The intended later feature is the familiar persistent piggy-bank offer: qualifying play deposits into a capped reserve; a store purchase releases the saved reward, after which another filling cycle can begin. It is not tied to the seasonal event schedule. Deposit amount, eligible victories/replays, reserve capacity, unlock condition, purchase product and repeat-cycle rules still need to be defined for that later feature.

The existing project uses `UnityInAppsIntegration` and `InitScript.waitedPurchaseGems` for generic consumable coin bundles. No bank purchase was wired into that generic success callback. A future implementation must identify the bank product and its confirmed transaction, credit exactly once and reset the matching reserve only after the successful credit. Display the actual store metadata price rather than a hard-coded amount.

## Implementation and validation

`WorldMapLetterVault` builds the shortcut and lazily builds/caches its preview. `CurvedLevelMap` owns visibility and map/chrome suspension. No update loop, particles or extra material instances are introduced.

`node Tools/events/install-pig-badge-v98.cjs` copies the selected final sprite while retaining stable metadata. `PigBadgeV98Validation.Run` in the isolated Unity project checks real alpha, importer settings, safe HUD placement/touch size, island persistence, menu/chrome hiding, actual open/close actions, map/chrome restoration and standard/narrow phone layouts without text overflow. It never calls the store or writes player progress. Native screenshots/report are in `previews/pig-badge-v98/` and `pig-badge-v98-validation.txt`. V90 and V93 files preserve superseded safe-box and detailed freestanding-pig designs. The V93 apprentice remains a separate editor-library character and is unaffected by this icon revision.
