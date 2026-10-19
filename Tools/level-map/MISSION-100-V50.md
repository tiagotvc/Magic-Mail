# Magic Mail — First Mission, Levels 1–100

The road crosses the connected grounds of Magic Mail Central. Grass, the existing soft green palette, vegetation and the striped route remain the visual base. Buildings alternate sides; major landmarks are roughly ten levels apart. Physical island capacity remains a separate setting from the five narrative sections.

| Levels | Area | Main landmarks | Story |
|---|---|---|---|
| 1–20 | Central Entrance | MAGIC MAIL sign, signature mailbox, side gate, headquarters | Meet the Courier and prepare the royal delivery. |
| 21–40 | Parcel Yard | PARCEL YARD sign, loading shelter, scattered parcels, sorting hall | Scribble steals the Royal Stamp; the team follows the mess. |
| 41–60 | Mascot Garden | AVIARY sign, creature shelter, owl tree, messenger tower | An enchanted parcel pile distracts the team; owls reveal the escape route. |
| 61–80 | Workshop and Training Field | WORKSHOP sign, workshop, three training hoops | Meet the Inventor, prepare the pursuit and spot the overloaded wagon. |
| 81–100 | Dispatch Tower | DISPATCH sign, beacon tower, ROYAL EXIT sign, gate and wagon | Regroup, catch Scribble and recover the Royal Stamp. A dark-sealed coin hints at the larger plot. |

## Cast

All four characters are complete, including hands and feet. Their map heights differ deliberately. The shader painting uses broad glossy highlights and colored shadows; no extra costume clutter was added.

| Character | Silhouette and face | Outfit | Wand / prop | Map height |
|---|---|---|---|---|
| The Courier | Tall, slim, angular young face | Red coat, teal scarf, navy trousers, postal cap, tall brown boots | Crooked wooden wand with amber tip | 1.45 |
| The Organizer | Short pear shape, round face, gray bob, round glasses | Wide purple wizard hat, ivory blouse, teal waistcoat, broad purple skirt, small shoes | Straight violet wand with amethyst tip and clipboard | 1.06 |
| The Inventor | Very short round body, bald head, big round nose and white mustache | Goggles, orange work apron, violet shirt, teal short trousers, work shoes | Brass fork wand with turquoise orb | 0.88 |
| Scribble | Small hunched body, enormous sideways ears, broad green head and two teeth | Petrol hooded tunic, rope belt, short plum shoes | Stolen Royal Stamp | 0.66 |

Idle motion is confined to forearm/held-item mesh regions. Head, torso and legs remain still. Characters are not stretched to a common height.

## Editing

The map preview has a **Missão Central · fases 1–100** toggle and shortcuts to 1 / 21 / 41 / 61 / 81. The toggle selects the new mission layout or the existing layout. **Editar objetos** edits the currently selected layout; sign wording is editable through **Texto da placa (English)**. Save the layout through the existing editor controls.

- New mission: `Assets/RoyalAves/Resources/RoyalAvesMission100.asset`
- New placement data: `Assets/RoyalAves/Resources/RoyalAvesMission100Layout.asset`
- Original placement data retained: `Assets/RoyalAves/Resources/RoyalAvesWorldMapSimpleLayout.asset`
- Source PNGs: `Assets/RoyalAves/Art/MapKitV3/Library/Personagens`, `Construcoes`, `Props` (v50 files)
- Exact generation prompts and source paths: `mission100-assets-v50.json`; built-in image_gen mode, PNGs copied unchanged with their generated alpha.

The mission contains 124 placed objects, eleven story beats, and special narrative beats at 50 and 100. These are map/story foundations; they do not implement new boss mechanics or a separate playable mode. Existing suitable architecture and vegetation were reused. Small optional props from the original idea (for example the phoenix nest) have not all received new bespoke art.

Native validation results and phone-format captures are in `previews/mission100-v50`. Main scene, season and physical island settings are not saved by the validation workflow.
