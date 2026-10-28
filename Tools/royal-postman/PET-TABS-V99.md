# Pet screen tabs

The old placeholder code applied gray vertex tints to both navigation buttons, making the user's blue artwork look disabled. Both buttons were already interactable.

`RoyalPostmanHatchery.ShowSection` now clears that tint and assigns reusable selected/unselected materials. The actual `Btn_squared_blue_g.png` remains unchanged. Both materials use the user's `AllIn1SpriteShaderUiMask` shader with `HSV_ON`: selected red (210 degrees), unselected original blue (0 degrees). Materials are assigned directly; switching tabs creates no material instances. Adjust them under `Art/UI/Tab/RoyalPostman/Pets/TabsV99`.

Only the hatchery's two material references and the two tab Images' material/color fields are installed in `Pets.prefab`. Layout, artwork, user shader helper components, button listeners, pet profile, header and incubation mechanics are preserved.

Native isolated checks exercise repeated clicks, actual rendered red/blue pixel colors, retained source sprites, white tint, enabled controls, profile/workshop visibility, and Portuguese/English labels and header. Results and selective preservation audit are archived under `production/review/pet-tabs-v99`. The isolated preview uses an authoring-helper shim; the production AllIn1 plugin is unchanged.
