# Royal Postman — home proposal, 19 October 2026

Current design task: pre-level/play modal. User explicitly confirmed two actions: Jogar and Grátis (rewarded video). `previews/play-modal-v1.png` is the first approval concept, not applied to runtime. Goal/booster content and numbers are illustrative. Full prompt and scope: `PLAY-MODAL-V1-PROMPT.md`.

Latest visual study: `previews/home-hud-v5-pet-and-events.png` adds the requested Pet button, four user-selected event controls (Cofre, Roleta, Passe, Entrega), and teal plus badges. New control placement/artwork awaits review; v4 remains the approved baseline for existing HUD. This is a raster concept with no runtime integration, layered production separation or customizable-avatar functionality implemented yet. Full current criteria remain in `ACCEPTANCE.md`.

Current acceptance authority: `ACCEPTANCE.md`. Preserve all approved mock materials, typography and proportions. Explicit changes/additions: plus controls use the main button's emerald-teal; independently customizable frame/portrait; pet button beside Play; four event accesses (piggy bank, daily wheel, battle pass, prize delivery). Pet direction is Tamagotchi-style care with assistance in a future battle minigame. Earlier narrative-delivery pet proposals below are history, not selected mechanics. New controls remain in visual review.

Latest discussion: clean painting without stains or artifacts and typography matching the mockup are explicit acceptance requirements. The central character image is provisional. The user is considering replacing it with a magical postal mascot system, with no construction currently desired. See `QUALITY-AND-MASCOT-DIRECTION.md` for requirements and the unapproved design proposal. Do not treat the mascot idea as permission to implement or replace the approved HUD.

Latest refinement: the user approved the v3 proportions and requested an embossed mail envelope on the top coin instead of its crown, plus correction of the Courier's arm anatomy. Current visual: `previews/home-hud-v4-letter-coin-corrected-arms.png`. The offering hand remains on the viewer-left; the other hand/forearm visibly connects to the viewer-right shoulder and holds a cross-body satchel strap. Preserve the approved HUD finish and compact taller button. This is still preview artwork, not a production capture or a layered export.

Current review: the user approved the painted v2 direction ("perfeito") and requested only a narrower, slightly taller level button. `previews/home-hud-v3-button-proportions.png` contains that refinement, preserving the approved painting, colors and composition. The native procedural prototype remains rejected. No game integration was requested in this refinement turn. Keep the sunny cartoon-gold rim and teal center as separate production pieces when assets are prepared.

Status: preview for the user's approval. Not connected to the game's lobby or Simulator.

Latest review: the user rejected the native procedural HUD as too flat/crude and revoked the elongated button-format request. The current proposal is `previews/home-hud-v2-painted.png`, a full-screen painted imagegen concept matching the supplied Royal Match/Royal Kingdom material quality and ordinary substantial rounded-rectangle level button. It is not a Unity screenshot or a layered production export. The earlier prototype and pressed screenshots remain history, not accepted targets. Approval is still required before game integration. Rim/center/labels must remain separate when production parts are prepared.

The supplied screens repeat a consistent visual hierarchy: small resource capsules and avatar at the top, one dominant central illustration, one obvious level/play button, and a continuous bottom bar. Large surfaces are calm; broad highlights and short colored shadows give the icons and controls volume. Warm cream lettering contrasts with saturated fields. Gold works as a substantial simple edge rather than elaborate decoration.

Our proposed home uses the established young Courier and Organizer owl instead of construction scenery. Their identities, costume colors and original navigation icons are preserved. The Courier's expressive hair, nose, gloves, boots and asymmetry supply the Crash-like shapes within the softer modern match-3 painting.

Palette roles: red lower navigation, sunny-yellow/orange gold rail and button rim, cream counter fields and text, plum ink/shadows/settings, teal main button and quiet blue/teal background. Coins and lives are represented; construction stars are omitted from the home. Values in the approval images are sample values (1,867 coins, 5 lives, level 14), not changes to the player's save.

The Play button uses the user's elongated chamfered format. Gold ring, stationary green well, moving teal face, arrow and live text remain individual Unity objects. Only face/arrow/text sink on press. Screenshots include normal and pressed states at 720×1600, 720×1280 and 360×800.

Prototype source/art are held outside production Assets under `Prototype/`. Native rendering uses a disposable isolated Unity preview project; it does not modify the user's current scene or player saves. Production runtime and Simulator connections were removed after the user clarified the approval-first workflow. Existing unrelated project edits are preserved.

Next step after visual approval: import the separated components, bind resource values and current level to the actual save, reuse the existing play/profile/shop/settings/menu routes, and replace the lobby/Simulator home. Gameplay, progression and secondary modal redesign remain separate work.
