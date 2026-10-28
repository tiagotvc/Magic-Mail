# Glossy crown and padded reward stars

V77 replaces the thin crown with a broad rounded golden crown: large ball-tipped points, a simple substantial front body and clean glossy reflections. It sits against the top rim of a completed level disc. The existing rule remains: three earned stars on a normal completed level show the crown; difficulty emblems retain their existing behavior.

Stars use matched gold and cool platinum illustrations with a small warm cream cushion. Their plump five-point shapes and broad reflections replace the angular mesh facets. Each score selects the appropriate sprite independently, so 0/1/2/3 earned stars remain distinct. The center badge is slightly larger. Reward cards face the camera, compensate for their bottom pivot and remain clear of the ground and the active halo.

Completed-level rewards progressively shrink beyond 4.5 route meters and hide beyond 10.5 meters ahead of the camera cursor. This prevents overlapping crowns and star rows at the compressed horizon. The active level retains its full reward display. This affects presentation only; saved scores and crown eligibility are unchanged.

`WorldMapKit.rewardStarGold` and `rewardStarEmpty` hold the new states. Both `levelCrown` and `simpleCrown` reference the new crown so the editor/game's simplified style switch uses the same artwork. The illustrated meshes use the existing shared card material cache; kits without reward art keep the previous mesh fallback. Borrowed cached meshes are not destroyed by an individual map view.

PNG masters are in `Assets/RoyalAves/Art/MapKitV3/LevelRewardsV77`. Unity imports them at a maximum 512 pixels with mipmaps, suitable for small map rewards while retaining the full original art on disk. The assets have true transparency. Built-in ImageGen prompts and output paths are recorded in [level-rewards-v77-manifest.json](level-rewards-v77-manifest.json).

Native validation uses an in-memory progress provider for actual one-, two- and three-star scores, plus the unplayed current level. It checks chosen textures and crown eligibility, captures ordinary/current/crossing/narrow-phone views, and restores the progress provider without writing player saves.
