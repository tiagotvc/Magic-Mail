# Reversible scenery lift — current V100

Standing island trees, buildings, characters, lamps, mailboxes and other upright props rotate from the curved terrain tangent to their standing pose on approach and lie back down on retreat. Their actual mesh scale stays at one (or negative one for horizontal mirroring). There is no miniature-to-full-size entrance and no idle scale breathing. Authored placements and sizes, including the opening island, are unchanged.

## V100: ground hinge, fixed size

V89's zero-to-one proportional entrance produced visible growing objects, while its 18-degree tilt barely unfolded. V100 removes that scale reveal. The card starts on the terrain tangent at the actual camera/radius crest, rotates around its visible ground contact, and finishes standing 3.5 meters below the crest. The short interval confines the edge-on part to the horizon. Beyond that interval the automatic scenery retains its normal frontal endpoint and manual scenery retains its authored standing orientation.

No downward burial animation is added. The existing authored standing sink is retained and compensated around the visible foot during rotation. The frontal endpoint is evaluated in the current terrain frame; its ground-contact height remains on that tangent throughout the hinge. Shadows and ground light follow the rotation amount. River plants, waterfalls, mist, ground-conformed props and bridges retain their existing treatment.

`SceneryRollV100Validation.Run` measures actual local/world geometry size, contact, rotation continuity and reverse traversal for authored/automatic buildings, trees and characters on two phone ratios. It checks crest culling, ground-tangent emergence, no buried artwork middle, standing endpoints and offsets, unchanged placement signatures, idle size stability, and unaffected river/mist decorations. Native nine-stage sequences and opening/crossing/Christmas captures are in `previews/scenery-roll-v100/`; the report is `scenery-roll-v100-validation.txt`. No layout or player data is written. Older tests below record earlier treatments and are not current acceptance tests.

## V89 history: preserve distant silhouettes

V82's 90-degree folded endpoint, exposed after removing the V85 burial, made the illustrated camera see distant props almost edge-on. The mesh scale was not squashed, but its projected height collapsed. A fully flat card was the wrong visible endpoint for this camera.

V89 limits illustrated standing scenery to 18 degrees away from its normal standing pose. During the 1.25-meter crest entrance before the main lift range, it scales all three axes proportionally from zero to full size. The visible contact hinge compensates for that uniform scale; no vertical sinking or independent height compression is added. The same distance function runs in reverse. Near standing rotations, authored offsets, actual placement data and the original island layouts remain unchanged. The perspective/legacy projection and ground/river/mist treatments retain their prior behavior.

`HorizonSilhouetteV89Validation.Run` measures actual camera-projected unit height versus width during 48 forward/reverse positions for manual/automatic buildings, trees and characters on two phone ratios. The acceptance floor is .85, independently of the implementation's tilt constant. It also checks a proportional crest entrance, no buried visible center, reversible transforms, preserved near offsets/frontal orientation, unaffected riverfall and current first-island signatures. Native captures show distant/approaching/near props and the current authored opening. Main runtime compilation and unchanged data-file hashes are recorded separately.

V82/V85 validation tools below are historical diagnostics for the earlier full-fold design, not acceptance tests for V89's visible silhouette treatment.

## V82 history

The regression was in `WorldMapView.Pose`: automatic frontal scenery was included in `forceStanding`, and its billboard rotation then overwrote the calculated lift. That override now supplies only the upright endpoint. A quaternion interpolation hinges the artwork from a flat ground pose to its standing pose, driven entirely by relative scroll distance. Near automatic scenery keeps its approved frontal camera alignment; the first island keeps its existing upright orientation and authored placement data.

Standing scenery is culled behind the curved horizon on every island. In the illustrated projection, the fold range ends 1.25 meters before that horizon, derived from the actual camera pitch and planet radius, including tall-phone framing. Cards are already flat before their pooled root becomes visible. Shadows and ground light pools follow the same reveal amount. The object's placement anchor, user-authored sink depth and saved layouts are unchanged.

Ground details stay conformed to the floor; river plants/falls and mist retain their intended frontal/static treatment. The lift uses no timers, coroutines, material creation or new meshes. It remains reversible at the same scroll position and does not require continuous editor repaint while stationary.

`SceneryLiftV82Validation.Run` measures actual pooled card transforms at the horizon and at 36 forward/reverse positions for automatic tree/building/character subjects on two phone ratios. It checks continuity, reversibility, frontal upright alignment, hidden early shadows, riverfall orientation and first-island signatures. Native captures cover folded/rising/standing, the opening and Christmas islands. The main runtime also compiles with zero compiler errors. The preview does not save layouts or player data.

## V85: hinge at the visible contact, no animated burial

V82 combined rotation with an extra downward translation of 12 percent of the sprite height and kept the authored burial offset along the terrain normal even when the card was flat. That made props disappear into the lawn as they folded.

V85 removes the extra translation and rotates around the visible contact line instead of the hidden sprite bottom. The contact is derived from the original standing offset and standing orientation; its height stays on the terrain tangent as the card rotates. The fully standing position remains identical to the previous authored position, including grass burial. No layout values are rewritten. The folded center/top rest on the terrain tangent instead of being pushed beneath it.

`SceneryHingeV85Validation.Run` adds physical geometry checks during 48 forward/reverse samples: the visible middle/top remain above the terrain tangent, flat artwork rests on it, upright offsets stay identical, and reverse scrolling restores the same pose. It covers authored first-island buildings, automatic trees/buildings/characters and narrow phones, with five-stage native capture sequences. Existing horizon, frontal orientation, riverfall and first-island placement checks remain.
