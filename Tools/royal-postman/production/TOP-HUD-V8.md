# Top HUD v8

Visual authority: approved/home.png and user's settings close-up, which supersedes the rejected wide-rim/detached-disk purple base.

Nine standalone source PNGs in Assets/RoyalAves/Art/RoyalPostmanUI2026: top-avatar-frame-v7, top-avatar-portrait-v7, top-coin-envelope-v7, top-heart-v7, top-plus-base-v8, top-plus-symbol-v7, top-settings-base-v8, top-gear-cream-v7, top-value-container-v7. Each component was generated individually, preserving source alpha; no icon atlas or manual raster-pixel processing. Built-in image_gen prompts: TOP-HUD-V8-PROMPTS.json.

Avatar frame and portrait are independent and remain independently replaceable. A native rounded mask clips selected portraits behind the frame. Currency envelope coin, heart, cream plus, gear and numeric labels are separate. Circular teal plus / purple settings bodies have fixed outer shoulders and independently moving center+symbol, without gold. Purple follows the user's corrected circular reference, not the discarded high dome. Both value panels reuse one clean cream source with 9-slice metadata at one pixel scale, retaining uniform bevels.

Home binds existing currency/lives/profile/settings routes and real values. Native integration checks passed top button callbacks, independent press, no gold frame on circular controls, closeup avatar binding and previous level/modal/ad bindings. No player progress/wallet/inventory test writes. Runtime and Editor compile exit 0; native assembled captures normal and pressed in review/top-hud-v8*.png. Current typography is the existing native Lilita One candidate; exact generated-reference glyph equivalence is not certified.
