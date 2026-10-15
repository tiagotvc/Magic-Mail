using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    // A projected ground grid, not a background picture. Every row belongs to an absolute world
    // level. Region colors blend across a boundary while both regions are visible in perspective.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CurvedMapTerrain : MaskableGraphic
    {
        public LevelMapConfig Config { get; set; }
        public float Cursor { get; set; }
        public int LevelCount { get; set; }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (Config == null) return;
            const int rows = 140, columns = 36;
            var size = rectTransform.rect.size;
            var origin = rectTransform.rect.min;
            for (var row = 0; row <= rows; row++)
            {
                var d = Mathf.Lerp(-0.6f, Config.curveDepth, row / (float)rows);
                var scale = LevelMapProjection.Scale(d, Config);
                var ground = Tint(d, false);
                for (var col = 0; col <= columns; col++)
                {
                    var u = col / (float)columns;
                    var x = u * size.x;
                    var tint = ground;
                    tint = Color.Lerp(tint * 0.87f, tint, Mathf.Clamp01(1 - scale * 0.6f));
                    tint.a = 1;
                    mesh.AddVert(origin + new Vector2(x, LevelMapProjection.GroundY(d, x, size, Config)), tint, Vector2.zero);
                    if (row == 0 || col == 0) continue;
                    var v = row * (columns + 1) + col;
                    mesh.AddTriangle(v - columns - 2, v - 1, v);
                    mesh.AddTriangle(v - columns - 2, v, v - columns - 1);
                }
            }
            // A separate ribbon gives the wide corridor clean edges, even near the horizon.
            var start = mesh.currentVertCount;
            for (var row = 0; row <= rows; row++)
            {
                var d = Mathf.Lerp(-0.6f, Config.curveDepth, row / (float)rows);
                var scale = LevelMapProjection.Scale(d, Config);
                var center = LevelMapProjection.Point(d, Cursor, size, Config);
                var half = size.x * 0.27f * scale;
                var tint = Tint(d, true);
                for (var edge = -1; edge <= 1; edge += 2)
                {
                    var x = center.x + edge * half;
                    mesh.AddVert(origin + new Vector2(x, LevelMapProjection.GroundY(d, x, size, Config)), tint, Vector2.zero);
                }
                if (row == 0) continue;
                var v = start + row * 2;
                mesh.AddTriangle(v - 2, v, v - 1);
                mesh.AddTriangle(v - 1, v, v + 1);
            }
        }

        Color Tint(float distance, bool corridor)
        {
            var world = Mathf.Min(Mathf.Max(1, LevelCount), Cursor + distance);
            var region = Config.RegionAt(world);
            var previous = Config.RegionAt(world - 1.5f);
            var tint = region != null ? corridor ? region.path : region.ground : (Color)new Color32(105, 185, 102, 255);
            var phase = Mathf.Repeat(world - 1, Config.levelsPerRegion);
            if (previous != null && phase < 1.5f)
                tint = Color.Lerp(corridor ? previous.path : previous.ground, tint, phase / 1.5f);
            var fog = Mathf.SmoothStep(0, 0.22f, Mathf.InverseLerp(Config.curveDepth * 0.72f, Config.curveDepth, distance));
            return Color.Lerp(tint, new Color32(158, 215, 210, 255), fog);
        }
    }
}
