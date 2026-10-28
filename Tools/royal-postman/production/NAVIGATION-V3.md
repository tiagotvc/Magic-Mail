# Navigation correction — individual icons

Authority: approved/home.png and explicit user clarification: one continuous red bar, integrated gold upper rail, visual seams, separate icons/text, active color overlay. Each icon generated separately with built-in image_gen. Rejected multi-icon atlas is not consumed by prefabs.

Sources: navigation-{clipboard,trophy,home,teams,album}-individual-v3.png, each 1254 x 1254 with original transparency. navigation-bar-approved-v2.png is the single continuous background.

Shared generation prompt: Faithfully reproduce the corresponding bottom navigation icon of approved/home.png, one individual icon on transparent square canvas, broad simple cartoon volumes, reference colors, smooth clean paint, no text/background/button/ground shadow/grain. Subject-specific prompts: cream clipboard green check; gold trophy purple star; teal folded map cream envelope red wax seal; teal and coral people cream heart; purple spiral album gold envelope. Original image generator outputs preserved in .codex/generated_images/01a153d9-6a7a-7490-b117-92b367dcb7bd.

Read-only alpha inspection supplies native Sprite rectangles. PNG pixels have not been edited by code. RoyalPostmanNavigation sets one active color overlay, title and marker; button callbacks remain in RoyalPostmanLiveHome. Four lightweight native seams render over the continuous bar.

This corrects the bottom navigation only. It does not certify the level/Pet/top controls, typography or character painting as matching the approved mock.

Edge correction: navigation root, continuous bar and tab regions use horizontal stretch anchors. Four seams end below the gold rail; active overlay also excludes the gold. Editor compilation exit 0; native Unity review captured again.

Import regression diagnosed in the actual project: RoyalAvesAssetImporter reset kit textures to Sprite / 2048 / CompressedHQ after copying reviewed metadata. A native Sprite rect wider than 2048 then sampled beyond the imported texture. This also shifted the visible gold/red boundary relative to native dividers. Fixed explicit ownership/exclusion plus RoyalPostmanTextureImportPolicy (Default, source resolution up to 4096, uncompressed). Reimported metadata and rebuilt native sprites. Dividers now 153px = 75% of the 204px red field and end 47px above its lower edge. Simulator validation rejects rectangles outside texture bounds and navigation width/seam regressions.

Latest screenshot-mark correction supersedes 75%: source gold/red boundary measured from texture; rail is 24 logical px. Dividers begin at red top y=92 and extend 176.8px, ending y=-84.8 (about 85% red height). End fade reduced from 25% of seam to one 1/32 segment. Existing full-width anchors and import ownership retained.
