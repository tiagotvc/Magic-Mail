// Arte gerada em código para a tela de abertura de baú (ChestOpenScreen):
//   * as máscaras de luz usadas quando o prefab não tem as do projeto (brilho macio, estrela, anel, espinho, feixe, perfil
//     do rastro);
//   * o BAÚ PROVISÓRIO (corpo, tampa e interior em peças separadas), usado enquanto a arte de verdade não entra no projeto.
// Tudo é criado uma vez e guardado; nada é salvo em disco.
using System;
using UnityEngine;

namespace RoyalAves.Effects
{
    public static class ChestOpenArt
    {
        static Sprite glow, star, ring, spike, beam, body, lid, interior;
        static Texture2D trailProfile;

        // ------------------------------------------------------------------------------------------ máscaras de luz

        /// Brilho radial macio (também serve de ponto de luz).
        public static Sprite Glow => glow != null ? glow : glow = Make(128, 128, new Vector2(0.5f, 0.5f), (x, y) =>
        {
            var r = Radius(x, y, 128, 128);
            var a = Mathf.Clamp01(1f - r);
            return new Color(1, 1, 1, a * a * (3f - 2f * a) * a);
        });

        /// Estrela de quatro pontas com um miolo macio.
        public static Sprite Star => star != null ? star : star = Make(128, 128, new Vector2(0.5f, 0.5f), (x, y) =>
        {
            var px = Mathf.Abs((x + 0.5f) / 64f - 1f);
            var py = Mathf.Abs((y + 0.5f) / 64f - 1f);
            var points = Mathf.Clamp01((1f - (Mathf.Sqrt(px) + Mathf.Sqrt(py))) * 3.5f);
            var core = Mathf.Clamp01(1f - Mathf.Sqrt(px * px + py * py) * 2.2f);
            return new Color(1, 1, 1, Mathf.Clamp01(points + core * core * 0.7f));
        });

        /// Anel fino e macio.
        public static Sprite Ring => ring != null ? ring : ring = Make(256, 256, new Vector2(0.5f, 0.5f), (x, y) =>
        {
            var r = Radius(x, y, 256, 256);
            var d = (r - 0.82f) / 0.07f;
            return new Color(1, 1, 1, Mathf.Exp(-d * d));
        });

        /// Espinho de luz: base larga na esquerda (pivô), afinando até a ponta.
        public static Sprite Spike => spike != null ? spike : spike = Make(128, 32, new Vector2(0f, 0.5f), (x, y) =>
        {
            var t = x / 127f;
            var half = Mathf.Pow(1f - t, 0.85f) * 0.5f;
            var along = Mathf.Pow(1f - t, 0.6f) * Mathf.Clamp01(t * 10f + 0.35f);
            var dy = Mathf.Abs((y + 0.5f) / 32f - 0.5f);
            var edge = Mathf.Clamp01((half - dy) / Mathf.Max(0.02f, half * 0.7f));
            return new Color(1, 1, 1, edge * edge * (3f - 2f * edge) * along);
        });

        /// Coluna de luz que sai da boca do baú: forte embaixo (pivô), abrindo e sumindo para cima.
        public static Sprite Beam => beam != null ? beam : beam = Make(64, 128, new Vector2(0.5f, 0f), (x, y) =>
        {
            var v = (y + 0.5f) / 128f;
            var u = Mathf.Abs((x + 0.5f) / 32f - 1f);
            var open = Mathf.Lerp(0.45f, 1f, v);                     // cone: mais estreito na base
            var across = Mathf.Clamp01(1f - u / open);
            across = across * across * (3f - 2f * across);
            var up = Mathf.Pow(1f - v, 1.4f) * Mathf.Clamp01(v * 14f);
            return new Color(1, 1, 1, across * up);
        });

        /// Perfil do rastro (fita): transparente nas bordas, cheio no meio. U corre ao longo da fita, V atravessa.
        public static Texture2D TrailProfile
        {
            get
            {
                if (trailProfile != null) return trailProfile;
                const int w = 4, h = 32;
                trailProfile = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var pixels = new Color[w * h];
                for (var y = 0; y < h; y++)
                {
                    var s = Mathf.Sin(Mathf.PI * (y + 0.5f) / h);
                    for (var x = 0; x < w; x++) pixels[y * w + x] = new Color(1, 1, 1, s * s);
                }
                trailProfile.SetPixels(pixels);
                trailProfile.Apply(false, true);
                return trailProfile;
            }
        }

        // ------------------------------------------------------------------------------------------ baú provisório

        static readonly Color WoodDark = Hex(0x3B1773), WoodLight = Hex(0x7036C4), LidDark = Hex(0x5326A0), LidLight = Hex(0x8A4BE0);
        static readonly Color GoldDark = Hex(0xB8741A), GoldLight = Hex(0xFFD768), GoldEdge = Hex(0x8F5510), Outline = Hex(0x1E0838);

        /// Corpo do baú provisório (320 x 220): madeira violeta, cantoneiras e tiras douradas, fechadura.
        public static Sprite ChestBody => body != null ? body : body = Make(320, 220, new Vector2(0.5f, 0.5f), (x, y) =>
        {
            float px = x + 0.5f - 160f, py = y + 0.5f - 110f;
            var d = Box(px, py, 156f, 106f, 28f);
            var alpha = Mathf.Clamp01(0.5f - d);
            if (alpha <= 0f) return Color.clear;
            var t = y / 219f;
            var color = Wood(x, y, t, WoodDark, WoodLight, 46f);
            if (d > -18f) color *= 0.6f;                                              // sombra por dentro da moldura
            var strap = Mathf.Min(Mathf.Abs(px - 92f), Mathf.Abs(px + 92f));
            if (strap < 17f) color *= 0.6f;
            if (strap < 14f) color = Gold(t, strap > 11f);
            if (d > -15f) color = Gold(t, d < -12.5f);
            if (d > -15f && py > 96f) color = Color.Lerp(color, Color.white, 0.25f);  // filete de luz em cima

            var plate = Box(px, py - 50f, 30f, 38f, 12f);                             // fechadura
            if (plate < 3f) color = GoldEdge;
            if (plate < 0f) color = Color.Lerp(Hex(0xFFE08A), GoldLight, Mathf.Clamp01((py - 12f) / 76f));
            var hole = Mathf.Min(Mathf.Sqrt(px * px + (py - 58f) * (py - 58f)) - 9f, Box(px, py - 44f, 4.5f, 12f, 2f));
            if (hole < 0f) color = Hex(0x2A0D4D);

            if (d > -3f) color = Outline;
            color.a = alpha;
            return color;
        });

        /// Tampa do baú provisório (320 x 150), arredondada em cima e reta embaixo (o pivô fica na base, onde ela dobra).
        public static Sprite ChestLid => lid != null ? lid : lid = Make(320, 150, new Vector2(0.5f, 0f), (x, y) =>
        {
            float px = x + 0.5f - 160f, py = y + 0.5f;
            var d = Box(px, py, 156f, 146f, 78f);
            var alpha = Mathf.Clamp01(0.5f - d);
            if (alpha <= 0f) return Color.clear;
            var t = y / 149f;
            var color = Wood(x, y, t, LidDark, LidLight, 40f);
            if (d > -30f && d < -24f && py > 50f) color = Color.Lerp(color, Color.white, 0.16f);   // reflexo acompanhando a curva
            if (d > -18f || py < 27f) color *= 0.6f;
            var strap = Mathf.Min(Mathf.Abs(px - 92f), Mathf.Abs(px + 92f));
            if (strap < 17f) color *= 0.6f;
            if (strap < 14f) color = Gold(t, strap > 11f);
            if (d > -15f || py < 24f) color = Gold(t, py > 21.5f && (py < 24f || d < -12.5f));
            if (d > -3f || py < 2.5f) color = Outline;
            color.a = alpha;
            return color;
        });

        /// Boca aberta do baú provisório (300 x 100): um oval escuro com luz quente no fundo. Fica atrás do corpo.
        public static Sprite ChestInterior => interior != null ? interior : interior = Make(300, 100, new Vector2(0.5f, 0.5f), (x, y) =>
        {
            float px = (x + 0.5f - 150f) / 146f, py = (y + 0.5f - 50f) / 46f;
            var e = px * px + py * py;
            var alpha = Mathf.Clamp01((1f - e) * 14f);
            if (alpha <= 0f) return Color.clear;
            var warm = Mathf.Clamp01(1f - e);
            var color = Color.Lerp(Hex(0x12041F), Hex(0xFFB53D), warm * warm * 0.6f);
            if (e > 0.84f) color = GoldEdge;
            color.a = alpha;
            return color;
        });

        // ------------------------------------------------------------------------------------------ utilitários

        static Color Wood(int x, int y, float t, Color dark, Color light, float plank)
        {
            var color = Color.Lerp(dark, light, t) * (0.96f + 0.04f * Mathf.Sin(x * 0.35f + y * 0.05f));
            if (Mathf.Repeat(y + 8f, plank) < 3f) color *= 0.72f;   // junta das tábuas
            color.a = 1f;
            return color;
        }

        static Color Gold(float t, bool edge)
        {
            var color = Color.Lerp(GoldDark, GoldLight, Mathf.Clamp01(t * 1.1f));
            return edge ? Color.Lerp(color, GoldEdge, 0.65f) : color;
        }

        /// Distância (com sinal) até um retângulo de cantos arredondados centrado na origem.
        static float Box(float px, float py, float halfX, float halfY, float radius)
        {
            var qx = Mathf.Abs(px) - halfX + radius;
            var qy = Mathf.Abs(py) - halfY + radius;
            var outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
            return Mathf.Min(Mathf.Max(qx, qy), 0f) + outside - radius;
        }

        static float Radius(int x, int y, int w, int h)
        {
            var px = (x + 0.5f) / (w * 0.5f) - 1f;
            var py = (y + 0.5f) / (h * 0.5f) - 1f;
            return Mathf.Sqrt(px * px + py * py);
        }

        static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        static Sprite Make(int w, int h, Vector2 pivot, Func<int, int, Color> pixel)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color[w * h];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++) pixels[y * w + x] = pixel(x, y);
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, w, h), pivot, 100f, 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
