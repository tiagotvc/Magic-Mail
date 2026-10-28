# Home button correction v6

Nível: 440 x 220. Pet: 200 x 210. Reference: approved/home.png plus user's button close-up.

Independent native layers: FixedWell, MovingCenter/Face, MovingCenter/LevelLabel (or Paw/PetLabel), FixedGoldFrame. Only MovingCenter travels 4 units on press. FixedGoldFrame stays still. Colored face and gold use complementary shader masks on the same aligned painted source, so their authored fit is preserved. Text and icon remain native independent objects. No player progress, inventory, level routing or ad logic changes.

Two individual source PNGs are in Assets/RoyalAves/Art/RoyalPostmanUI2026/{level,pet}-base-v6.png. Built-in image_gen was used. Exact prompt set: HOME-BUTTONS-V6.json. PNG pixels have not been edited by scripts; alpha bounds are read-only native Sprite metadata. Materials: home-fixed-gold-v6 and home-moving-center-v6. Prefabs: Home, LevelButton, PetButton.

Captures: review/home-buttons-v6.png and review/home-buttons-v6-pressed.png. The native font remains Lilita One; exact equivalence to the generated reference's lettering is not certified.

Uniform-rim correction: level-base-v6 native Sprite border is 45% of its source height, with Image.Type.Sliced and pixelsPerUnitMultiplier=4 on fixed well, moving face and fixed gold. Only center/straight spans stretch; corners and painted border thickness share one isotropic pixel scale. Level remains 440 x 220. Read-only measurement of the native 1080 x 320 capture: before L/R 20px, top 26px, bottom 28px; after L/R 19px, top 20px, bottom 22px (lower painted extrusion retained). Editor compilation and native build passed. Review: home-buttons-uniform-rim.png / home-buttons-uniform-rim-pressed.png.
