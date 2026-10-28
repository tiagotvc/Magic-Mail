# Events — cartoon surprise V92

The current presentation is the [cartoon envelope surprise V92](EVENTS-SURPRISE.md), refined from the user's selected third V91 mockup. It adds a short staggered letter-escape animation on a separate foreground Canvas, only while Events is visible in Play. The cream header/navigation and native coming-soon text remain. Exact prompts and source images are recorded in the V92 manifest. See the linked document for animation, imports, reproduction and validation.

## Superseded V88 presentation record

The Events tab now tells a small waiting-room story: the courier sits on postal parcels with his cheek resting on his hand, while the Organizer owl peeks through a glossy purple curtain. A gold envelope crest, satchel, sealed letter and hourglass tie the stage to Magic Mail. The cream **Events** header reuses the approved bottom-navigation artwork; **Eventos em breve** stays as native text over the quiet lower floor. No cards, progress list, timers, Play or reward buttons are displayed.

The account HUD is hidden only on Events. The bottom navigation stays available; returning to Map restores its account HUD and world. The covered map stays paused. Account-header visibility survives global chrome hide/show and lobby refreshes.

The canonical cast was checked against `RoyalAvesMission100.asset`, including the Courier and Organizer owl (sprite GUID `aac497d37460686ef635454417c73131`). A first draft incorrectly used the older human manager artwork; the user rejected that character, and the current scene replaces her with the existing cream owl, amber eyes, gold spectacles, plum cape and teal bow. Do not use old manager portraits as current map-cast references merely because they remain in the project.

The production illustration is `Assets/RoyalAves/Resources/EventsV88/backstage.png`, generated/refined with built-in ImageGen. Exact prompts, identity references, original generations and the cast correction are recorded in `events-backstage-v88-manifest.json`. The original generations remain preserved. `node Tools/events/install-backstage-v88.cjs` copies the selected illustration and retains stable importer metadata.

The opaque scene uses one static RawImage, with no mipmaps or compression and a maximum texture size of 2048. `PostalEventSceneImage` crops outer curtain edges on tall phones and the quiet lower floor on wider screens, keeping image aspect and top crest. It updates only on layout changes. There are no animation loops, event-state subscriptions or periodic UI rebuilds. Opening, refreshing or reopening does not access event progress or claims.

## Retained delivery infrastructure

The earlier delivery rules and saved progress remain in place for future work. Daily dispatch tracks three new victories for 150 coins, with UTC-day reset and duplicate-win protection. Permanent milestones retain their existing claim IDs. Season events remain disabled in the config.

Configuration is in `Assets/RoyalAves/Resources/RoyalAvesEvents.asset`; permanent milestones remain in `RoyalAvesFeatures.asset`. Event JSON uses PlayerPrefs key `magic-mail-events-v1`; permanent claims use `royal-aves-meta-v1`. These saves are not deleted or reset by the coming-soon presentation.

## Validation

`EventsBackstageV88Validation.Run` renders the production screen in the isolated Unity preview project at 720×1280 and 360×780. It checks canonical owl attribution, scene aspect and character-safe cropping, exact text, absence of cards/actions, no event-state reads, Events account-HUD hiding, global chrome toggles, bottom navigation visibility and Map restoration. Its fake event source throws on any access, so the placeholder cannot accidentally observe/claim rewards during these checks.

Native screenshots are in `previews/backstage-v88/`; results are in `backstage-v88-validation.txt`. The full main runtime also compiles with zero compiler errors using the existing compiler response file, with output redirected into Library. V86 captures/checks document the superseded lone-standing-character presentation.

`EventsValidation.cs` and the older screenshots/report document the retired V81 card interface. That UI harness is historical and is not the current presentation check. The V83–V85 mockups remain visual proposals only.

`node Tools/events/install-events.cjs` preserves config tuning, script metadata and player saves. The isolated preview preparation also includes the courier library resource and its dependencies.
