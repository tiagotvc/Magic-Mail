# Porquinho Postal — separate layers and options V101

The built-in ImageGen created eight original transparent PNG assets in `Assets/RoyalAves/Art/Interface/PigBankV101/`: the isolated red piggy bank, four blue backing containers, and three blank notification-badge shapes. The pig keeps its oversized angular head, protruding round torso, short feet, curly tail and empty back slot, with no coin. Each piece is independently reusable. All originals are preserved; exact source/output paths are in `pig-layers-v101-manifest.json` and exact prompts in `pig-layers-v101-prompts.json` plus `pig-layers-v101-badge-prompts.json`.

Container choices: **1** round blue medallion; **2** blue postage stamp; **3** rounded blue shield; **4** blue oval with a red Christmas bow. All use the requested blue with glossy purple/gold trim. Notification-badge choices: **A** red round; **B** emerald seal; **C** gold star. A number or exclamation mark is supplied separately by the UI, not baked into the sprites.

Run `node Tools/events/install-pig-layers-v101.cjs` to copy exact originals and create stable Unity sprite metadata (512 maximum size for pig/containers, 256 for tiny badges, no mipmaps). These options are outside Resources and have not replaced the current production shortcut while the user chooses a design.

Run `node Tools/events/build-pig-layers-v101-review.cjs` to build the self-contained `previews/pig-layers-v101/escolher.html`. It previews each container with the same isolated pig, includes 88-pixel views, switches badges/live sample text, isolates the pig/container, and downloads the original individual PNGs. Selection buttons only mark the visual choice locally. `comparacao.png` is a headless browser capture of this review, not a flattened replacement asset. `sprites.zip` contains all eight reusable Unity assets and metadata.

Visual review confirms the pig is separate, blue containers have filled faces, badges have blank text areas and transparent outer backgrounds display correctly. Original/project SHA256 matching and metadata were checked for all eight PNGs. No gameplay, purchases, player saves, island placements or production icon code was changed in this delivery.
