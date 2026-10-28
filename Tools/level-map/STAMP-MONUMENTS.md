# Glossy stamp monuments

Three original oversized rubber-stamp props fill the visual role of whimsical roadside columns:

- Royal stamp: purple grip, cream fluted shaft and gold head, dark purple rubber pad; default height 1.35.
- Curved teal stamp: turquoise grip and curved handle, gold sun medallion and blue rubber pad; default height 1.05.
- Christmas stamp: ruby grip, striped handle, glossy holly, gold snowflake head and green rubber pad; default height 1.4.

All are frontal elevation sprites with real transparency, broad glossy highlights and a level bottom baseline. They are in the editor's **Props** library under their English titles. Their rendering kind is Landmark so they stand upright rather than flattening onto the terrain like a puddle; they are not treated as windowed buildings.

Choose **Add props**, select the stamp, and place it on either bank. Ground pinning can hide part of the lower rubber pad evenly; the native examples use 8% depth. The curved version bends only its handle while the base remains level. Size, position, mirroring and sinking remain editable through the existing controls.

The shared island recipes also have editable `monuments` sprite lists and `monumentSize`. Automatic scenery reserves one frontal stamp on the left bank before optional trees and ground accents. Letter Gardens, Blossom Post and Ember Stampworks use the royal stamp; Ink Springs and Melody Lagoon use teal; both Christmas recipes use the Christmas stamp. The extra object has a stable separate source ID, preserving IDs used by existing tree/prop overrides. Existing authored island settings and saved layouts are retained, including the first island. A manually saved island composition keeps its normal override behavior.

Native validation checks all 13 generated islands, the first island's placement signature, three automatic composition captures and additional disposable 8%-sink examples. Built-in ImageGen prompts, original output paths, selected project paths and GUIDs are recorded in [stamp-monuments-v76-manifest.json](stamp-monuments-v76-manifest.json).
