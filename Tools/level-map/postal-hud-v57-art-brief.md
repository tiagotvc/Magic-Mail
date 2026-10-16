# Magic Mail approved crystal HUD

Built-in image_gen, two production layers with alpha. These are stored unmodified in Assets/RoyalAves/Resources/MapHudV57/header.png and navigation.png. The approved header reference is exec-c36e5aec-9f58-4b43-b7ee-ece48cf49ebb.png (the FIRST solo header, before the height increase). Footer reference is exec-0e67556f-092f-46ac-82d7-d2d023f5ad30.png.

Header generation prompt: preserve exact approved header artwork and proportions, all icons, portrait, plus glyphs, pearl-white crystal border, purple channel, gold rims and glints. Remove only 5, Full and 0 and restore underlying surfaces. Replace external presentation background with alpha; retain purple inside the UI.

Footer generation prompt: isolate approved full-width rectangular lower background; retain straight gold upper seam, purple stripe, cream center, lower gold line and white crystal lower band. Remove icons, all text, Map badge and separators; no side frame, no rounded endcaps, no texture.

The runtime draws live values using the modal Calistoga font. The header mesh excludes the inner portrait region; an independent portrait graphic draws that region from the same source without a second face underneath. Top actions and two plus hit targets relay to existing game actions. Bottom navigation reuses existing mapNavIcons with unchanged references. Separators are independent geometry; the selected tab uses plain dark-purple text only.

Transparent extraction fringe is suppressed in a UI shader. Artwork uses bilinear filtering, no mipmaps and no compression. Main scene, map layout, island settings and seasons are not saved.
