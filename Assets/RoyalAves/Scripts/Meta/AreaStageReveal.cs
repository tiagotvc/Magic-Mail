// Build-up effects for an area decoration stage or an area layer. Three styles (BuildEffect), chosen per upgrade:
//  - Magia: a real dissolve — the project's own AllIn1SpriteShader (Fade effect) fed a vertical gradient instead of its
//    usual random noise, so the burning edge sweeps bottom-to-top — with coloured dust along the rising edge and a burst
//    from the object's outline when it is done.
//  - PopElastico: the object pops out of the ground (scale with overshoot from its base), a shockwave ring spreads from
//    the base and confetti flies out of the outline.
//  - Varredura: the same dissolve fed a horizontal gradient, so a band of light wipes left-to-right revealing the object,
//    with cool sparkles on the band and short glints at the end.
// Everything about timing and every sparkle is a pure function of (style, time, seed), so the area editor's preview
// replays exactly what the game plays.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public static class AreaStageReveal
    {
        public enum BuildEffect { Magia = 0, PopElastico = 1, Varredura = 2, Raios = 3 }

        public static readonly string[] EffectNames =
        {
            "Magia — dissolve de baixo p/ cima + pó colorido",
            "Pop elástico — pula do chão + onda + confete",
            "Varredura de luz — esquerda → direita + brilhos",
            "Raios + estrelas — dissolve + faixas de luz e estrelas saindo do objeto",
        };

        public const float Duration = 1.1f;     // the Magia dissolve
        public const float SparkleInterval = 0.02f;
        public const int SparklesPerStep = 2;   // shed on the edge every SparkleInterval
        public const int BurstCount = 56;       // Magia: fly out of the object's outline when it finishes

        public static float DurationOf(BuildEffect e) => e == BuildEffect.PopElastico ? 0.7f : e == BuildEffect.Varredura ? 1.0f : Duration;
        /// Second (from the start) at which the burst goes off: when the pop lands, or when the dissolve ends.
        public static float BurstAtOf(BuildEffect e) => e == BuildEffect.PopElastico ? 0.3f : DurationOf(e);
        public static int BurstCountOf(BuildEffect e) => e == BuildEffect.PopElastico ? 44 : e == BuildEffect.Varredura ? 16 : BurstCount;
        public static bool UsesDissolve(BuildEffect e) => e != BuildEffect.PopElastico;
        public static bool HasTrail(BuildEffect e) => e != BuildEffect.PopElastico;

        /// Where the object is inside its (full-size) picture, the shape of its edge and how it builds in. bounds is 0..1
        /// with y from the bottom; outline holds points on the silhouette as (x, y, nx, ny) — position 0..1 and the unit
        /// direction pointing out of the object. No outline (old layers) falls back to an ellipse around the bounds.
        public struct RevealRegion
        {
            public Rect bounds;
            public Vector4[] outline;
            public BuildEffect style;
            public static RevealRegion Whole => new RevealRegion { bounds = new Rect(0, 0, 1, 1) };
        }

        /// Where burst sparkle k starts (0..1 in the picture, y up) and the direction it flies in (radians).
        public static void BurstPoint(RevealRegion region, int k, SparkleSpec spec, out Vector2 origin, out float angle) =>
            OutlinePoint(region, k, BurstCountOf(region.style), spec.angle, out origin, out angle);

        /// Point k of `count` spread along the object's outline (0..1 in the picture, y up) and the direction pointing out
        /// of the object there, turned by `spread` radians. Without an outline: an ellipse around the bounds.
        public static void OutlinePoint(RevealRegion region, int k, int count, float spread, out Vector2 origin, out float angle)
        {
            var o = region.outline;
            if (o != null && o.Length > 0)
            {
                var p = o[Mathf.Min(o.Length - 1, (int)((k + 0.5f) / count * o.Length))];
                origin = new Vector2(p.x, p.y);
                angle = Mathf.Atan2(p.w, p.z) + spread;
                return;
            }
            var b = region.bounds;
            var a = Mathf.PI * 2f * (k + 0.5f) / count;
            origin = new Vector2(b.x + b.width * (0.5f + 0.5f * Mathf.Cos(a)), b.y + b.height * (0.5f + 0.5f * Mathf.Sin(a)));
            angle = a + spread;
        }

        public static readonly Color[] Palette =
        {
            new Color(1.00f, 0.86f, 0.35f), // gold
            new Color(1.00f, 0.50f, 0.82f), // pink
            new Color(0.45f, 0.90f, 1.00f), // cyan
            new Color(0.72f, 0.55f, 1.00f), // violet
            new Color(0.55f, 1.00f, 0.75f), // mint
            new Color(1.00f, 1.00f, 1.00f), // white
        };

        static readonly Color[] LightPalette =
        {
            new Color(1.00f, 1.00f, 1.00f),
            new Color(0.65f, 0.95f, 1.00f),
            new Color(1.00f, 0.96f, 0.72f),
        };

        static Color[] PaletteOf(BuildEffect e) => e == BuildEffect.Varredura ? LightPalette : Palette;

        // AllIn1SpriteShader's _FadeAmount: pixels below this threshold (per the fade texture's red
        // channel) are cut away. -0.1 shows everything (the shader's own "off" default); 1 hides it all.
        public const float FadeAmountHidden = 1f;
        public const float FadeAmountShown = -0.1f;

        static Texture2D verticalGradient, horizontalGradient;
        static Shader fadeShader;

        /// Plays the build-up on every piece directly under "stage". Safe to call on a stage with any
        /// number of children, including zero.
        public static IEnumerator Play(MonoBehaviour host, RectTransform stage, Sprite sparkleSprite, Texture2D burnTexture)
        {
            if (stage == null) yield break;
            var pieces = new RectTransform[stage.childCount];
            for (var i = 0; i < pieces.Length; i++) pieces[i] = (RectTransform)stage.GetChild(i);
            yield return Play(host, stage, pieces, sparkleSprite, burnTexture);
        }

        /// Same build-up, but for an explicit set of pieces instead of everything directly under "stage" —
        /// for stages that swap one specific piece in (e.g. a "broken" clock replaced by a "fixed" one)
        /// rather than building every child at once. "stage" is only used to host spawned sparkles.
        public static IEnumerator Play(MonoBehaviour host, RectTransform stage, IReadOnlyList<RectTransform> pieces, Sprite sparkleSprite, Texture2D burnTexture) =>
            Play(host, stage, pieces, null, sparkleSprite, burnTexture);

        /// Same, for pieces that are full-size pictures with the furniture only in part of them (an area layer):
        /// regions[i] says where that is inside piece i and which style builds it, so the effect covers just that region
        /// instead of the whole picture. A null list means every piece is whole, in the Magia style.
        public static IEnumerator Play(MonoBehaviour host, RectTransform stage, IReadOnlyList<RectTransform> pieces, IReadOnlyList<RevealRegion> regions, Sprite sparkleSprite, Texture2D burnTexture)
        {
            if (stage == null || pieces == null) yield break;
            stage.gameObject.SetActive(true);

            var n = pieces.Count;
            var images = new Image[n];
            var originalMaterials = new Material[n];
            var fadeMaterials = new Material[n];
            var originalColors = new Color[n];
            var originalPositions = new Vector2[n];
            var originalScales = new Vector3[n];
            var styles = new BuildEffect[n];
            var burstDone = new bool[n];
            var progress = new float[n];
            var total = 0f;
            var shader = FadeShader();
            for (var i = 0; i < n; i++)
            {
                if (pieces[i] == null) continue;
                pieces[i].gameObject.SetActive(true);
                images[i] = pieces[i].GetComponent<Image>();
                styles[i] = RegionOf(regions, i).style;
                total = Mathf.Max(total, DurationOf(styles[i]));
                if (images[i] == null) continue;
                originalColors[i] = images[i].color;
                originalPositions[i] = pieces[i].anchoredPosition;
                originalScales[i] = pieces[i].localScale;
                originalMaterials[i] = images[i].material;
                if (UsesDissolve(styles[i]))
                {
                    if (shader == null) continue;
                    var mat = CreateFadeMaterial(burnTexture, BoundsOf(regions, i), styles[i]);
                    fadeMaterials[i] = mat;
                    images[i].material = mat;
                }
                else ApplyPop(pieces[i], images[i], RegionOf(regions, i), 0f, originalColors[i], originalPositions[i]);
            }

            Coroutine sparkles = null;
            // (the sparkleSprite argument is kept for callers, but the dust uses its own white star so it can be tinted)
            if (host != null)
                sparkles = host.StartCoroutine(TrailSparkles(host, stage, pieces, regions, progress));

            var t = 0f;
            while (t < total)
            {
                t += Time.unscaledDeltaTime;
                for (var i = 0; i < n; i++)
                {
                    if (images[i] == null) continue;
                    var d = DurationOf(styles[i]);
                    var f = Mathf.Clamp01(t / d);
                    // pieces further right build in slightly later, for an "under construction" left-to-right feel
                    var delay = n <= 1 ? 0f : 0.35f * i / (n - 1);
                    var pf = Mathf.Clamp01((f - delay) / (1 - delay));
                    progress[i] = pf;
                    if (fadeMaterials[i] != null)
                        fadeMaterials[i].SetFloat("_FadeAmount", Mathf.Lerp(FadeAmountHidden, FadeAmountShown, pf));
                    else if (!UsesDissolve(styles[i]))
                        ApplyPop(pieces[i], images[i], RegionOf(regions, i), pf, originalColors[i], originalPositions[i]);
                    if (!burstDone[i] && t >= BurstAtOf(styles[i]))
                    {
                        burstDone[i] = true;
                        Burst(host, stage, pieces[i], RegionOf(regions, i));
                    }
                }
                yield return null;
            }
            if (host != null && sparkles != null) host.StopCoroutine(sparkles);
            for (var i = 0; i < n; i++)
            {
                if (images[i] == null) continue;
                if (!burstDone[i]) Burst(host, stage, pieces[i], RegionOf(regions, i));
                images[i].material = originalMaterials[i]; // fully shown looks identical without the fade material
                images[i].color = originalColors[i];
                pieces[i].anchoredPosition = originalPositions[i];
                pieces[i].localScale = originalScales[i];
                if (fadeMaterials[i] != null) Object.Destroy(fadeMaterials[i]);
            }
        }

        static RevealRegion RegionOf(IReadOnlyList<RevealRegion> regions, int i)
        {
            if (regions == null || i >= regions.Count) return RevealRegion.Whole;
            var r = regions[i];
            if (r.bounds.width <= 0.001f || r.bounds.height <= 0.001f) r.bounds = new Rect(0, 0, 1, 1);
            return r;
        }

        static Rect BoundsOf(IReadOnlyList<RevealRegion> regions, int i) => RegionOf(regions, i).bounds;

        // ---- pop (PopElastico) --------------------------------------------------------------------------------------

        /// Scale of the pop at progress p (0..1): rushes past 1 and settles, like a spring.
        public static float PopScale(float p)
        {
            const float c1 = 2.2f, c3 = c1 + 1f;
            var x = Mathf.Clamp01(p) - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        public static float PopAlpha(float p) => Mathf.Clamp01(p / 0.12f);

        /// Scales the piece about the base of the object (the point it "grows out of"), keeping that point still.
        static void ApplyPop(RectTransform piece, Image image, RevealRegion region, float p, Color color, Vector2 anchored)
        {
            var s = PopScale(p);
            var size = piece.rect.size;
            var b = region.bounds;
            var pivotPoint = new Vector2(b.x + b.width / 2f, b.y);                       // base, middle of the object
            var d = new Vector2((pivotPoint.x - piece.pivot.x) * size.x, (pivotPoint.y - piece.pivot.y) * size.y);
            piece.localScale = new Vector3(s, s, 1f);
            piece.anchoredPosition = anchored + d * (1f - s);
            image.color = new Color(color.r, color.g, color.b, color.a * PopAlpha(p));
        }

        // The shockwave ring on the ground when the pop lands.
        public const float RingLife = 0.55f;
        public const float RingFlatten = 0.32f;
        public const float RingWidthFactor = 1.8f;      // of the object's width, at full size
        public static float RingScale(float u) => 0.15f + 0.85f * (1f - (1f - u) * (1f - u));
        public static float RingAlpha(float u) => 0.85f * (1f - u);
        public static readonly Color RingColor = new Color(1f, 0.95f, 0.8f);

        // ---- dissolve material --------------------------------------------------------------------------------------

        /// The dissolve material of a build-up, at "hidden". The editor's area preview builds the same one, so what it
        /// shows is what the game plays. bounds: where the object is inside its picture (0..1, y from the bottom).
        public static Material CreateFadeMaterial(Texture2D burnTexture, Rect bounds, BuildEffect style = BuildEffect.Magia)
        {
            var shader = FadeShader();
            if (shader == null) return null;
            var mat = new Material(shader);
            mat.EnableKeyword("FADE_ON");
            if (style == BuildEffect.Varredura)
            {
                // The gradient runs over the object's own width: the band starts at its left and ends at its right.
                var w = Mathf.Max(0.01f, bounds.width);
                mat.SetTexture("_FadeTex", HorizontalGradient());
                mat.SetTextureScale("_FadeTex", new Vector2(1f / w, 1f));
                mat.SetTextureOffset("_FadeTex", new Vector2(-bounds.x / w, 0f));
                mat.SetColor("_FadeBurnColor", new Color(0.8f, 0.96f, 1f, 1f));
                mat.SetFloat("_FadeBurnWidth", 0.07f);
                mat.SetFloat("_FadeBurnTransition", 0.10f);
                mat.SetFloat("_FadeBurnGlow", 9f);
            }
            else
            {
                // The gradient runs over the object's own height, not the whole picture's: the edge starts at its
                // bottom and ends at its top (outside it the picture is empty anyway).
                var h = Mathf.Max(0.01f, bounds.height);
                mat.SetTexture("_FadeTex", VerticalGradient());
                mat.SetTextureScale("_FadeTex", new Vector2(1f, 1f / h));
                mat.SetTextureOffset("_FadeTex", new Vector2(0f, -bounds.y / h));
                mat.SetColor("_FadeBurnColor", new Color(1f, 0.85f, 0.35f, 1f));
                mat.SetFloat("_FadeBurnWidth", 0.12f);
                mat.SetFloat("_FadeBurnTransition", 0.18f);
                mat.SetFloat("_FadeBurnGlow", 6f);
            }
            mat.SetTexture("_FadeBurnTex", burnTexture);
            mat.SetFloat("_FadeAmount", FadeAmountHidden);
            return mat;
        }

        // ---- dust ---------------------------------------------------------------------------------------------------
        // Every sparkle is described by a SparkleSpec made from (index, seed, style), so the editor's preview can replay
        // the exact same dust from the same numbers (deterministic) while the game just uses a fresh seed on every play.

        public struct SparkleSpec
        {
            public float x;        // where along the edge it starts (0..1)
            public float size;     // canvas pixels
            public float drift;    // sideways travel while it rises (trail sparkles)
            public float travel;   // distance travelled: up for trail sparkles, outwards for burst ones
            public float life;     // seconds
            public float spin;     // degrees per second
            public float angle;    // burst only: a spread (MakeBurst), then the final direction in radians (BurstPoint)
            public Color color;
        }

        /// A sparkle shed on the moving edge while the object is still filling in.
        public static SparkleSpec MakeSparkle(int index, int seed, BuildEffect style = BuildEffect.Magia)
        {
            var r = new System.Random(unchecked(seed * 31 + index * 7919 + 1));
            float Next(float a, float b) => a + (b - a) * (float)r.NextDouble();
            var palette = PaletteOf(style);
            return new SparkleSpec
            {
                x = Next(0f, 1f), size = Next(16f, 48f), drift = Next(-30f, 30f), travel = Next(40f, 120f),
                life = Next(0.5f, 1.1f), spin = Next(-240f, 240f), color = palette[r.Next(palette.Length)],
            };
        }

        /// A sparkle of the burst that goes off from the object's outline.
        public static SparkleSpec MakeBurst(int index, int seed, BuildEffect style = BuildEffect.Magia)
        {
            var r = new System.Random(unchecked(seed * 17 + index * 104729 + 3));
            float Next(float a, float b) => a + (b - a) * (float)r.NextDouble();
            var palette = PaletteOf(style);
            var s = new SparkleSpec { color = palette[r.Next(palette.Length)] };
            switch (style)
            {
                case BuildEffect.PopElastico: // confetti: fast and far
                    s.size = Next(18f, 44f); s.travel = Next(90f, 230f); s.life = Next(0.7f, 1.3f);
                    s.spin = Next(-480f, 480f); s.angle = Next(-0.6f, 0.6f);
                    break;
                case BuildEffect.Varredura:   // glints: big, brief, close to the object
                    s.size = Next(30f, 70f); s.travel = Next(12f, 45f); s.life = Next(0.35f, 0.7f);
                    s.spin = Next(-90f, 90f); s.angle = Next(-0.5f, 0.5f);
                    break;
                default:
                    s.size = Next(22f, 58f); s.travel = Next(60f, 170f); s.life = Next(0.6f, 1.2f);
                    s.spin = Next(-360f, 360f); s.angle = Next(-0.4f, 0.4f); // angle: spread around the outward direction
                    break;
            }
            return s;
        }

        /// Where a trail sparkle starts (0..1 in the picture, y up) when the effect is p of the way through: on the rising
        /// edge for Magia, on the sweeping band for Varredura.
        public static Vector2 TrailOrigin(BuildEffect style, SparkleSpec s, Rect b, float p) =>
            style == BuildEffect.Varredura
                ? new Vector2(b.x + b.width * p, b.y + s.x * b.height)
                : new Vector2(b.x + s.x * b.width, b.y + b.height * p);

        /// Position of a sparkle u (0..1) of the way through its life, in canvas pixels from where it started
        /// (y up). Burst sparkles slow down as they fly out; trail ones rise steadily.
        public static Vector2 SparkleOffset(SparkleSpec s, float u, bool burst) =>
            burst ? new Vector2(Mathf.Cos(s.angle), Mathf.Sin(s.angle)) * (s.travel * (1f - (1f - u) * (1f - u)))
                  : new Vector2(s.drift * u, s.travel * u);

        public static float SparkleAlpha(float u) => Mathf.Clamp01(u < 0.15f ? u / 0.15f : 1f - (u - 0.15f) / 0.85f);

        /// Pops in, then shrinks away.
        public static float SparkleScale(float u) => Mathf.Clamp01(u / 0.2f) * (1.1f - u * 0.7f);

        // ---- rays (Raios) -------------------------------------------------------------------------------------------
        // Streaks of light that shoot out of the object's outline, each with a bright head and a tail that catches up and
        // fades, plus the star burst.

        public const int RayCount = 44;

        public struct RaySpec
        {
            public float spread;   // radians off the outward direction
            public float length;   // canvas pixels the head travels
            public float width;    // canvas pixels
            public float delay;    // seconds after the burst goes off
            public float life;     // seconds
            public Color color;
        }

        public static RaySpec MakeRay(int index, int seed)
        {
            var r = new System.Random(unchecked(seed * 13 + index * 15485863 + 5));
            float Next(float a, float b) => a + (b - a) * (float)r.NextDouble();
            var tinted = r.NextDouble() > 0.55;
            return new RaySpec
            {
                spread = Next(-0.2f, 0.2f), length = Next(150f, 380f), width = Next(5f, 14f),
                delay = Next(0f, 0.12f), life = Next(0.45f, 0.8f),
                color = tinted ? Palette[r.Next(Palette.Length)] : Color.white,
            };
        }

        /// How far the head and the tail of a ray are from where it started, u (0..1) of the way through its life. The head
        /// rushes out and slows; the tail waits, then follows, so the streak stretches and then slides away.
        public static float RayHead(RaySpec s, float u) => s.length * (1f - (1f - u) * (1f - u));
        public static float RayTail(RaySpec s, float u) => s.length * Mathf.Pow(Mathf.Clamp01((u - 0.3f) / 0.7f), 1.6f);
        public static float RayAlpha(float u) => Mathf.Clamp01(Mathf.Min(u / 0.08f, 1f) * (1f - Mathf.Pow(u, 2.2f)));

        static Sprite sparkleSprite, ringSprite, raySprite;

        /// A thin streak lying along +x: soft tail on the left, bright head on the right.
        public static Sprite GetRaySprite()
        {
            if (raySprite != null) return raySprite;
            const int w = 128, h = 16;
            var pixels = new Color32[w * h];
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var u = x / (w - 1f);
                var v = (y - (h - 1) / 2f) / (h / 2f);
                var along = Mathf.Pow(u, 1.4f) * Mathf.Clamp01((1f - u) / 0.06f);
                var across = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(v)), 1.6f);
                pixels[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(along * across * 255f));
            }
            return raySprite = MakeSprite(w, h, pixels, "AreaStageReveal_Ray");
        }

        /// A soft white four-point star. White so each sparkle can be tinted any colour of the palette (the gold sprite
        /// assigned in the Inspector can only ever be gold).
        public static Sprite GetSparkleSprite()
        {
            if (sparkleSprite != null) return sparkleSprite;
            const int size = 64;
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                float dx = (x - (size - 1) / 2f) / (size / 2f), dy = (y - (size - 1) / 2f) / (size / 2f);
                var glow = Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Sqrt(dx * dx + dy * dy)), 2.4f);
                var rayX = Mathf.Exp(-Mathf.Abs(dy) * 16f) * Mathf.Clamp01(1f - Mathf.Abs(dx));
                var rayY = Mathf.Exp(-Mathf.Abs(dx) * 16f) * Mathf.Clamp01(1f - Mathf.Abs(dy));
                var a = Mathf.Clamp01(glow + Mathf.Max(rayX, rayY));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
            return sparkleSprite = MakeSprite(size, pixels, "AreaStageReveal_Sparkle");
        }

        /// A soft white ring, for the shockwave.
        public static Sprite GetRingSprite()
        {
            if (ringSprite != null) return ringSprite;
            const int size = 128;
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                float dx = (x - (size - 1) / 2f) / (size / 2f), dy = (y - (size - 1) / 2f) / (size / 2f);
                var off = (Mathf.Sqrt(dx * dx + dy * dy) - 0.82f) / 0.08f;
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Exp(-off * off) * 255f));
            }
            return ringSprite = MakeSprite(size, pixels, "AreaStageReveal_Ring");
        }

        static Sprite MakeSprite(int size, Color32[] pixels, string name) => MakeSprite(size, size, pixels, name);

        static Sprite MakeSprite(int width, int height, Color32[] pixels, string name)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = name,
            };
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        // Spawns sparkles along each piece's moving edge while it is still filling in.
        static IEnumerator TrailSparkles(MonoBehaviour host, RectTransform stage, IReadOnlyList<RectTransform> pieces, IReadOnlyList<RevealRegion> regions, float[] progress)
        {
            var corners = new Vector3[4];
            var sprite = GetSparkleSprite();
            var seed = Random.Range(1, 1 << 20);
            var n = 0;
            while (true)
            {
                for (var i = 0; i < pieces.Count; i++)
                {
                    if (pieces[i] == null) continue;
                    var region = RegionOf(regions, i);
                    var p = progress[i];
                    if (!HasTrail(region.style) || p <= 0f || p >= 1f) continue;
                    pieces[i].GetWorldCorners(corners); // 0 bottom-left, 1 top-left, 2 top-right, 3 bottom-right
                    for (var j = 0; j < SparklesPerStep; j++)
                    {
                        var spec = MakeSparkle(n++, seed, region.style);
                        var o = TrailOrigin(region.style, spec, region.bounds, p);
                        var origin = new Vector3(
                            corners[0].x + o.x * (corners[2].x - corners[0].x),
                            corners[0].y + o.y * (corners[1].y - corners[0].y), 0f);
                        host.StartCoroutine(Animate(SpawnSparkle(stage, sprite, origin, spec), spec, false));
                    }
                }
                yield return new WaitForSecondsRealtime(SparkleInterval);
            }
        }

        // The burst of a piece: sparkles start on the object's outline and fly out of it (plus the ring for the pop).
        static void Burst(MonoBehaviour host, RectTransform stage, RectTransform piece, RevealRegion region)
        {
            if (host == null || stage == null || piece == null) return;
            var corners = new Vector3[4];
            piece.GetWorldCorners(corners);
            if (region.style == BuildEffect.Raios) RayBurst(host, stage, corners, region); // first, so the stars draw over the rays
            var sprite = GetSparkleSprite();
            var seed = Random.Range(1, 1 << 20);
            var count = BurstCountOf(region.style);
            for (var k = 0; k < count; k++)
            {
                var spec = MakeBurst(k, seed, region.style);
                BurstPoint(region, k, spec, out var origin, out var angle);
                spec.angle = angle;
                var world = new Vector3(
                    corners[0].x + origin.x * (corners[2].x - corners[0].x),
                    corners[0].y + origin.y * (corners[1].y - corners[0].y), 0f);
                host.StartCoroutine(Animate(SpawnSparkle(stage, sprite, world, spec), spec, true));
            }
            if (region.style == BuildEffect.PopElastico) Ring(host, stage, corners, region.bounds);
        }

        // The streaks: created now (invisible) so the order of drawing is right, each one shoots out after its own delay.
        static void RayBurst(MonoBehaviour host, RectTransform stage, Vector3[] corners, RevealRegion region)
        {
            var sprite = GetRaySprite();
            var seed = Random.Range(1, 1 << 20);
            for (var k = 0; k < RayCount; k++)
            {
                var spec = MakeRay(k, seed);
                OutlinePoint(region, k, RayCount, spec.spread, out var origin, out var angle);
                var go = new GameObject("Ray", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var rect = (RectTransform)go.transform;
                rect.SetParent(stage, true);
                rect.position = new Vector3(
                    corners[0].x + origin.x * (corners[2].x - corners[0].x),
                    corners[0].y + origin.y * (corners[1].y - corners[0].y), 0f);
                var image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.raycastTarget = false;
                image.color = new Color(spec.color.r, spec.color.g, spec.color.b, 0f);
                rect.sizeDelta = new Vector2(1f, spec.width);
                rect.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
                host.StartCoroutine(AnimateRay(rect, image, angle, spec));
            }
        }

        static IEnumerator AnimateRay(RectTransform rect, Image image, float angle, RaySpec spec)
        {
            var start = rect.localPosition;
            var dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            for (var wait = 0f; wait < spec.delay; wait += Time.unscaledDeltaTime)
            {
                if (rect == null) yield break;
                yield return null;
            }
            var t = 0f;
            while (t < spec.life)
            {
                if (rect == null) yield break;
                t += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(t / spec.life);
                var head = RayHead(spec, u);
                var tail = RayTail(spec, u);
                rect.sizeDelta = new Vector2(Mathf.Max(1f, head - tail), spec.width);
                rect.localPosition = start + dir * ((head + tail) / 2f);
                image.color = new Color(spec.color.r, spec.color.g, spec.color.b, RayAlpha(u));
                yield return null;
            }
            if (rect != null) Object.Destroy(rect.gameObject);
        }

        static void Ring(MonoBehaviour host, RectTransform stage, Vector3[] corners, Rect b)
        {
            var go = new GameObject("Ring", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(stage, true);
            rect.position = new Vector3(
                corners[0].x + (b.x + b.width / 2f) * (corners[2].x - corners[0].x),
                corners[0].y + b.y * (corners[1].y - corners[0].y), 0f);
            var diameter = b.width * (corners[2].x - corners[0].x) * RingWidthFactor / Mathf.Max(0.0001f, stage.lossyScale.x);
            rect.sizeDelta = new Vector2(diameter, diameter * RingFlatten);
            var image = go.GetComponent<Image>();
            image.sprite = GetRingSprite();
            image.raycastTarget = false;
            image.color = new Color(RingColor.r, RingColor.g, RingColor.b, 0f);
            rect.localScale = Vector3.zero;
            host.StartCoroutine(AnimateRing(rect, image));
        }

        static IEnumerator AnimateRing(RectTransform rect, Image image)
        {
            var t = 0f;
            while (t < RingLife)
            {
                if (rect == null) yield break;
                t += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(t / RingLife);
                rect.localScale = Vector3.one * RingScale(u);
                image.color = new Color(RingColor.r, RingColor.g, RingColor.b, RingAlpha(u));
                yield return null;
            }
            if (rect != null) Object.Destroy(rect.gameObject);
        }

        static RectTransform SpawnSparkle(RectTransform stage, Sprite sprite, Vector3 worldPos, SparkleSpec spec)
        {
            var go = new GameObject("Sparkle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(stage, true);
            rect.position = worldPos;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.color = new Color(spec.color.r, spec.color.g, spec.color.b, 0f);
            rect.sizeDelta = new Vector2(spec.size, spec.size);
            rect.localScale = Vector3.zero;
            return rect;
        }

        static IEnumerator Animate(RectTransform sparkle, SparkleSpec spec, bool burst)
        {
            if (sparkle == null) yield break;
            var image = sparkle.GetComponent<Image>();
            var start = sparkle.localPosition;
            var t = 0f;
            while (t < spec.life)
            {
                if (sparkle == null) yield break;
                t += Time.unscaledDeltaTime;
                var u = Mathf.Clamp01(t / spec.life);
                var offset = SparkleOffset(spec, u, burst);
                sparkle.localPosition = start + new Vector3(offset.x, offset.y, 0f);
                sparkle.localRotation = Quaternion.Euler(0f, 0f, spec.spin * t);
                sparkle.localScale = Vector3.one * SparkleScale(u);
                image.color = new Color(spec.color.r, spec.color.g, spec.color.b, SparkleAlpha(u));
                yield return null;
            }
            if (sparkle != null) Object.Destroy(sparkle.gameObject);
        }

        static Shader FadeShader() =>
            fadeShader != null ? fadeShader : fadeShader = Shader.Find("AllIn1SpriteShader/AllIn1SpriteShader");

        // White at the bottom, black at the top: fed as AllIn1SpriteShader's _FadeTex (whose dissolve
        // keeps pixels visible longest where this value is highest), this makes the reveal sweep
        // bottom-to-top as _FadeAmount drops, instead of the shader's usual random noise pattern.
        // Unity's SetPixels/UV convention puts row 0 at the texture's bottom (V=0).
        static Texture2D VerticalGradient()
        {
            if (verticalGradient != null) return verticalGradient;
            const int size = 64;
            var tex = new Texture2D(1, size, TextureFormat.R8, false) { wrapMode = TextureWrapMode.Clamp, name = "AreaStageReveal_VerticalGradient" };
            var pixels = new Color[size];
            for (var y = 0; y < size; y++)
            {
                var v = 1f - y / (size - 1f); // row 0 (bottom) = 1 (white), top row = 0 (black)
                pixels[y] = new Color(v, v, v, 1f);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return verticalGradient = tex;
        }

        // White at the left, black at the right: the same trick sideways, so the band wipes left to right.
        static Texture2D HorizontalGradient()
        {
            if (horizontalGradient != null) return horizontalGradient;
            const int size = 64;
            var tex = new Texture2D(size, 1, TextureFormat.R8, false) { wrapMode = TextureWrapMode.Clamp, name = "AreaStageReveal_HorizontalGradient" };
            var pixels = new Color[size];
            for (var x = 0; x < size; x++)
            {
                var v = 1f - x / (size - 1f);
                pixels[x] = new Color(v, v, v, 1f);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return horizontalGradient = tex;
        }
    }
}
