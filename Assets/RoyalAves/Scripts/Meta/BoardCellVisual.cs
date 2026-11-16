using UnityEngine;

namespace RoyalAves.Meta
{
    public static class BoardCellVisual
    {
        static readonly Sprite[] tiles = new Sprite[16];
        static readonly Sprite[] chess = new Sprite[2];
        const int TileSize = 128;
        const float TileUnits = 1.96f;

        /// col/row alternam as casas clara e escura quando o kit Royal Postman existe; sem ele, vale a arte azul antiga.
        public static void Apply(SpriteRenderer renderer, int outerCorners = 0, int col = 0, int row = 0)
        {
            var kit = RoyalPostmanGameplayKit.Load();
            if (kit != null && kit.squareLight != null && kit.squareDark != null)
            {
                var index = ((col + row) & 1) == 1 ? 1 : 0;
                if (chess[index] == null) chess[index] = BakeChessCell(kit.squareUnder, index == 1 ? kit.squareDark : kit.squareLight);
                renderer.sprite = chess[index];
                renderer.color = Color.white;
                return;
            }
            ApplyRoundedBlue(renderer, outerCorners);
        }

        // A casa do mock não é quadrada (94x85 sobre um fundo de 103x100): as duas imagens são esticadas para um
        // quadrado de 128 px, o fundo inteiro e a casa centrada por cima, para a célula manter o tamanho de antes.
        static Color32? underFill;

        /// Cor do fundo das casas (o pixel central do square_bg): também pinta os vãos das curvas da borda.
        public static Color32 UnderFill(Sprite under)
        {
            if (underFill.HasValue) return underFill.Value;
            var source = ReadSprite(under);
            Color32 color = source.GetPixelBilinear(0.5f, 0.5f);
            Object.Destroy(source);
            underFill = color;
            return color;
        }

        static Sprite BakeChessCell(Sprite under, Sprite top)
        {
            var pixels = new Color32[TileSize * TileSize];
            if (under != null)
            {
                // O fundo tem cantos arredondados: usado como sprite, deixaria um furo claro onde quatro casas se
                // encontram. Por isso vale só a cor dele, preenchendo a casa inteira até a borda.
                Color32 fill = UnderFill(under);
                for (var i = 0; i < pixels.Length; i++) pixels[i] = fill;
            }
            // A casa quase enche a célula: sobra só um fio do fundo entre uma casa e outra (RoyalPostmanGameplayKit.tileFill).
            var kit = RoyalPostmanGameplayKit.Load();
            var fillRatio = kit != null ? kit.tileFill : 0.97f;
            Composite(pixels, top, fillRatio, fillRatio);
            var texture = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false)
            {
                name = "RoyalPostmanChessCell", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var made = Sprite.Create(texture, new Rect(0, 0, TileSize, TileSize), Vector2.one * 0.5f, TileSize / TileUnits);
            made.name = "RoyalPostmanChessCell";
            return made;
        }

        static void Composite(Color32[] target, Sprite sprite, float scaleX, float scaleY)
        {
            var source = ReadSprite(sprite);
            var w = Mathf.RoundToInt(TileSize * scaleX);
            var h = Mathf.RoundToInt(TileSize * scaleY);
            var ox = (TileSize - w) / 2;
            var oy = (TileSize - h) / 2;
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    Color s = source.GetPixelBilinear((x + 0.5f) / w, (y + 0.5f) / h);
                    var index = (y + oy) * TileSize + x + ox;
                    Color d = target[index];
                    var outA = s.a + d.a * (1f - s.a);
                    if (outA <= 0f) continue;
                    var rgb = ((Vector3)(Vector4)new Color(s.r, s.g, s.b) * s.a + (Vector3)(Vector4)new Color(d.r, d.g, d.b) * d.a * (1f - s.a)) / outA;
                    target[index] = new Color(rgb.x, rgb.y, rgb.z, outA);
                }
            Object.Destroy(source);
        }

        // Lê o recorte do sprite por uma RenderTexture: a importação da arte não precisa de Read/Write.
        static Texture2D ReadSprite(Sprite sprite)
        {
            var rect = sprite.rect;
            var buffer = RenderTexture.GetTemporary(sprite.texture.width, sprite.texture.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(sprite.texture, buffer);
            var previous = RenderTexture.active;
            RenderTexture.active = buffer;
            var copy = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            copy.ReadPixels(new Rect(rect.x, rect.y, rect.width, rect.height), 0, 0);
            copy.Apply(false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(buffer);
            return copy;
        }

        static void ApplyRoundedBlue(SpriteRenderer renderer, int outerCorners)
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
