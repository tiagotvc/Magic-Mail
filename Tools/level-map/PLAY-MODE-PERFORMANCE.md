# Map performance investigation

Measured locally on 2026-10-17. Editor timings are not device performance guarantees.

## Confirmed costs and changes

- Visible levels and props already use a bounded window and pooled slots. The campaign is not instantiated as 100 simultaneously rendered scenes.
- Layout and kit assets hold direct sprite references. Object pooling does not imply texture streaming; the changes below do not implement asset streaming.
- Terrain reconstruction originally produced 185,324 ground vertices in the entrance benchmark. Repeated small scrolls cost approximately 219–223 ms per `WorldMapView.Tick`.
- Terrain now uses broad rows for broad painted surfaces, independently refined rounded road caps, cached longitudinal bases and UV coordinates, and fewer repeated normal calculations. The tested entrance mesh has 29,592 vertices.
- Ground details and light pools reuse their conformed geometry while their transforms, shape and cursor remain unchanged. Animated colors and shader properties still update. One composed transform replaces two native transform calls per vertex when reprojection is needed.

## Validation

`Tools/level-map/MapCostValidation.cs`, copied to `Assets/Editor` in the isolated preview project and run through Unity batch mode, reported:

| Isolated test | Before | After |
| --- | ---: | ---: |
| Stationary map Tick | 3.19–3.72 ms | 1.25 ms |
| Alternating small scroll Tick | 219–223 ms | 20.43 ms |
| UpdateProps | 3.31 ms | 1.04 ms |

Native rendering and finite unit normals passed at levels 1, 21, 41, 61, 81, 100, 101, 150 and back to 1, without Unity errors. A rendered entrance image was inspected. No source scene was saved.

## Visible Play Mode sample

The Unity window was restored, Play entered, the title-screen play button clicked, and the actual map opened in Simulator. A 10-second capture discarded the first 3 seconds and sampled 405 frames:

- Frame interval: average 17.32 ms (~58 FPS), median 16.97 ms, p95 21.80 ms.
- One active map, 115 renderers, map target 1010 x 2048 with HDR and MSAA.
- Map Tick marker: average 2.51 ms, maximum 41.74 ms.
- Main-thread maximum: 49.08 ms. Intermittent stalls remain in this editor sample.
- GPU frame time was not captured. All-zero draw-call and presentation counters were not used as evidence.
- Allocation counters and graphics-driver memory include editor activity and cannot be attributed to the map alone.

An earlier capture had no active map and no draws; its frame intervals were discarded. The visible Play sample has no pre-change baseline. These results do not establish stable 60 FPS or resolve all idle-animation stutter.

The on-demand editor tool is `Royal Aves > Mapa de níveis > Profile Play Mode (10s)`, writing `Temp/map-play-profile.txt`. The test Play session was stopped and the project left open and visible.
