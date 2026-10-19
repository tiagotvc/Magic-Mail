# Magic Mail: the first 100 levels

The central is built around a magical ink spring. The route preserves the soft grass palette and independently colored road. Broad glossy highlights, a few strong shapes and restrained decorations match the existing shrubs and birds.

| Levels | Landscape | Main postal phenomena |
| --- | --- | --- |
| 1–20 | Letter Gardens | Envelope tree, seal flowers, ribbon arch, central headquarters |
| 21–40 | Ink Springs | Stone inkwell, animated lilac waterfall and stream, quill mill, envelope bridge |
| 41–60 | Messenger Grove | Floating delivery garden, working/sleeping owls, griffin, phoenix nest |
| 61–80 | Enchanted Stampworks | Correspondence tubes, sealing-wax pond, stamp factory, workshop and training area |
| 81–100 | Grand Dispatch Hill | Parcel mountain depot, tethered mail cloud, dispatch tower, Scribble's wagon and royal exit |

## Character direction

Five full-body silhouettes: tall human courier, large round owl organizer, elderly humanoid turtle inventor, tiny ink sprite Scribble, and a broad-shouldered gorilla loader. The gorilla has two visible eyes behind clear glasses with envelope details, an angular jaw and large chin. He wears work clothes and carries cargo; he has no wand.

The turtle has a large shell, projected neck, beard, thin limbs and a frontal pose. Its body backing and two forearm/tool layers form a rigid articulated rig. Other character idles move only the selected forearm regions; body and head do not sway.

## Independent animation pieces

The ink source consists of static structure, falling ink, pool and individual droplets. Only liquid materials receive animated highlights. Rotor, ribbon tails, cloud mail, stamps, tube deliveries and small parcel gestures are independently editable adornments. Object position, piece anchors, scale, rotation, mirroring and animation are saved in the shared campaign layout.

The twelve routine scenes remain: collection box, enchanted watering, loading, weighing, lunch break, aviary, griffin play, phoenix nest, broom repair, flight training, ready delivery bags and outward remittance. Six routine animation families keep the scene quiet: letter flutter, watering droplets, owl blink, rope tug, windsock and cart wheel. The wheel rotates without moving the cart.

## Files and review

- [Opening composition revision: before/after and two phone formats](entrance-v54-review.html)
- [Opening composition native checks](previews/entrance-v54/validation.txt)
- [Interactive native review](magicmail-first100-review.html)
- [Landscape art and exact built-in ImageGen prompts](magic-landscapes-v53-manifest.json)
- [Routine art and exact built-in ImageGen prompts](work-props-v52-manifest.json)
- [Glossy buildings and character prompts](art-v51-manifest.json)
- [Turtle rig layer prompts](turtle-rig-v51-manifest.json)
- [Native landscape validation](previews/magic-landscapes-v53/validation.txt)
- [Native character validation](previews/magic-landscapes-v53/characters-validation.txt)

Original generated PNGs are copied unchanged into `Assets/RoyalAves/Art/MapKitV3/Library/`; turtle rig layers are in `Assets/RoyalAves/Art/MapKitV3/Rigs/TurtleV51/`. They are registered in the map asset library. The campaign uses `RoyalAvesMission100.asset` and `RoyalAvesMission100Layout.asset`. The original simple layout and the main Unity scene are not replaced.

The V54 opening revision replaces the previous dense entrance arrangement with 13 individually spaced objects across levels 1–20. Every level anchor is unique, and consecutive objects on the same bank are at least 2.5 levels apart. The envelope tree, mailbox and courier are larger, and the small bell and hours board are removed from the opening composition while their original art remains in the library. A warm parchment road stays distinct from the green lawn through the mission, while custom per-island road colors still take precedence. Header/navigation use a lower, restrained satin treatment. Objects whose contact point is behind the terrain crest are culled in opening views, including during editor picking. Existing placement entries after level 20 are retained.

Native validation uses the isolated project under `Library/MapValidation/PreviewProject`. Run `prepare-polish-preview.cjs` only when the validated isolated resources have already been copied back, because preparation copies the current main resource data. `MagicLandscapesV53Bake.Run` creates the attractions once; `MagicLandscapesV53Validation.PrepareAndRun` applies final composition adjustments and captures them. `MagicLandscapesV53Validation.Run` is read-only. Later region entries are compared against the V52 baseline. Manual editor pointer interaction and physical-device performance are not measured. Phase 50/100 events remain narrative hooks, not new playable modes.
