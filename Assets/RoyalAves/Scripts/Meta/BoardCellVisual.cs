using UnityEngine;

namespace RoyalAves.Meta
{
    public static class BoardCellVisual
    {
        static readonly Sprite[] tiles = new Sprite[16];
        public static void Apply(SpriteRenderer renderer, int outerCorners = 0)
        {
            var tile = tiles[outerCorners];
            if (tile == null)
            {
                const int size = 128;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.name = "RoundedLightBlueBoardCell";
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                var pixels = new Color[size * size];
                // Rounded squares in mid blue, lighter blue lines between them.
                var fill = new Color32(181, 201, 229, 255);
                var edge = new Color32(191, 210, 237, 255);
                Color background = new Color32(178, 207, 245, 255);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        var q = new Vector2(Mathf.Abs(x + 0.5f - 64f), Mathf.Abs(y + 0.5f - 64f)) - new Vector2(54f, 54f);
                        float distance = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - 8f;
                        Color color = Color.Lerp(fill, edge, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-1.8f, -0.5f, distance)));
                        color = Color.Lerp(background, color, 1f - Mathf.Clamp01(distance + 0.5f));
                        color.a = 1f;
                        const float radius = 26f;
                        float px = x + 0.5f, py = y + 0.5f;
                        void RoundCorner(int flag, float dx, float dy)
                        {
                            if ((outerCorners & flag) == 0 || dx >= radius || dy >= radius) return;
                            float d = new Vector2(dx - radius, dy - radius).magnitude - radius;
                            color.a = Mathf.Min(color.a, 1f - Mathf.Clamp01(d + 0.5f));
                        }
                        RoundCorner(1, px, size - py);
                        RoundCorner(2, size - px, size - py);
                        RoundCorner(4, px, py);
                        RoundCorner(8, size - px, py);
                        pixels[y * size + x] = color;
                    }
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                tile = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size / 1.96f);
                tile.name = "RoundedLightBlueBoardCell";
                tiles[outerCorners] = tile;
            }
            renderer.sprite = tile;
            renderer.color = Color.white;
        }
    }
}
