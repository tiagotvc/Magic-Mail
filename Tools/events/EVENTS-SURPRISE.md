# Events cartoon surprise V92

The selected third V91 mockup now tells a coherent postal story: an actual open envelope rests on parcels, the Courier crouches to peek inside, and the canonical Organizer owl holds its top flap. There is no door, ribbon locking an open letter, or unrelated human character. The approved cream Events header and bottom navigation remain native UI. The message remains native text: **Eventos em breve**.

Three small glossy letters escape from the envelope opening in a staggered eight-second cycle, with a pause between bursts and six small golden sparkles. The illustration and characters remain static. Flap articulation and character blinking are not implemented; the animation focuses on the contents escaping from the letter.

`PostalEventSurpriseMotion` lives in `LobbyEventsScreen.cs`. It updates only during Play and only while its Events GameObject is active. It uses unscaled time, resets on reopen, and has no ExecuteAlways or editor repaint subscription. Nine existing UI elements are reused; the animation creates no per-frame GameObjects, materials, particles, tweens or resource loads. A nested foreground Canvas isolates animated graphic rebuilds from the static header, illustration and text. It adds no GraphicRaycaster or overridden sorting. Coordinate mapping uses the illustration's actual crop, so letters originate at its opening on tall phones as well. The full cast width is preserved: taller phones extend the quiet bottom floor using the texture's clamped edge instead of cutting characters off at the sides.

The map behind Events stays covered/paused. No cards, timers, claims, currencies or event-state reads were added. Player progress and purchase systems are unchanged.

## Art and reproduction

Built-in ImageGen produced the scene and separate transparent letter. Exact prompts are in `events-surprise-v92-prompts.json`, source/output paths in `events-surprise-v92-manifest.json`. Originals remain preserved. `node Tools/events/install-surprise-v92.cjs` copies the selected outputs while preserving existing metadata. UI resources: `EventsV92/scene` (opaque, maximum 2048) and `EventsV92/letter` (alpha, maximum 512), both without mipmaps or compression.

## Validation

`EventsSurpriseV92Validation` renders both phone ratios and several motion phases in the isolated Unity project. It checks the cast, art import/alpha, exact labels, correct crop, motion trajectory and quiet period, fixed element count across 480 samples, editor Update guard, isolated animation Canvas, no actions/event-state reads, covered world and HUD restoration.

`EventsSurpriseV92PlayValidation` enters actual Play in the isolated preview project, checks animation while timeScale is zero, hides Events to verify it stops, then reopens it to verify reset/resume. It restores its isolated Play settings afterward. The user's Unity editor is not closed, minimized or put in Play by this workflow.

Native captures, interactive motion preview and test reports are under `previews/surprise-v92/`. Main-runtime compilation output is redirected into Library rather than overwriting active game assemblies.
