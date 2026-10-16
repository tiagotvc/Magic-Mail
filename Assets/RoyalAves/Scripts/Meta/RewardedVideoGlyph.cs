using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    // A small film/play symbol, drawn by the UI so it stays sharp at every phone resolution.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RewardedVideoGlyph : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            void Quad(float x, float y, float w, float h, Color32 tint)
            {
                var n = vh.currentVertCount;
                vh.AddVert(new Vector2(r.x + x * r.width, r.y + y * r.height), tint, Vector2.zero);
                vh.AddVert(new Vector2(r.x + x * r.width, r.y + (y + h) * r.height), tint, Vector2.zero);
                vh.AddVert(new Vector2(r.x + (x + w) * r.width, r.y + (y + h) * r.height), tint, Vector2.zero);
                vh.AddVert(new Vector2(r.x + (x + w) * r.width, r.y + y * r.height), tint, Vector2.zero);
                vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
            }
            var ink = new Color32(17, 97, 31, (byte)(color.a * 255));
            Quad(.02f, .02f, .96f, .96f, color);
            for (var i = 0; i < 4; i++)
            {
                Quad(.1f, .1f + i * .23f, .12f, .1f, ink);
                Quad(.78f, .1f + i * .23f, .12f, .1f, ink);
            }
            var start = vh.currentVertCount;
            vh.AddVert(new Vector2(r.x + .35f * r.width, r.y + .23f * r.height), ink, Vector2.zero);
            vh.AddVert(new Vector2(r.x + .35f * r.width, r.y + .77f * r.height), ink, Vector2.zero);
            vh.AddVert(new Vector2(r.x + .71f * r.width, r.y + .5f * r.height), ink, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
        }
    }
}
