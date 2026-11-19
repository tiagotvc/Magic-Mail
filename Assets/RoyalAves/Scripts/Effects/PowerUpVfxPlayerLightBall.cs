// Light Ball (um globo) e combinação de dois globos: efeitos prismáticos com núcleo em camadas, fitas, poeira mágica,
// faíscas, brilhos e eletricidade mágica. Parte do PowerUpVfxPlayer (mesmo pool, mesmos materiais).
// Regra de cor: branco só no miolo fino dos raios e no instante do impacto; o resto usa a paleta prismática.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoyalAves.Effects
{
    public sealed partial class PowerUpVfxPlayer
    {
        [Header("Paleta prismática (Light Ball e combinação dupla)")]
        public Color prismViolet = new Color(0.506f, 0.169f, 1f);    // #812BFF
        public Color prismMagenta = new Color(0.820f, 0.169f, 1f);   // #D12BFF
        public Color prismPink = new Color(1f, 0.341f, 0.776f);      // #FF57C6
        public Color prismGold = new Color(1f, 0.761f, 0.278f);      // #FFC247
        public Color prismCyan = new Color(0.286f, 0.867f, 0.961f);  // #49DDF5

        [Header("Bloom: brilho de pós-processamento, ligado só enquanto o Light Ball ou a dupla tocam")]
        [Tooltip("Liga HDR + Bloom na câmera do jogo durante o efeito e devolve como estava no fim. Desligado, o efeito roda sem brilho extra.")]
        public bool useBloom = true;
        [Range(0f, 4f)] public float bloomIntensity = 1.4f;
        [Tooltip("Só o que passa deste brilho vaza luz. Acima de 1 a arte normal do jogo quase não é afetada.")]
        [Range(0.5f, 2f)] public float bloomThreshold = 1.15f;
        [Range(0f, 1f)] public float bloomScatter = 0.7f;
        [Tooltip("Quanto os miolos, raios e fitas ficam mais fortes que o branco normal (é isso que o Bloom espalha).")]
        [Range(1f, 5f)] public float hdrBoost = 2.6f;

        Volume bloomVolume; VolumeProfile bloomProfile; Bloom bloomOverride;
        Camera bloomCamera; UniversalAdditionalCameraData bloomCameraData;
        bool bloomSavedPost, bloomSavedHdr, bloomOn;
        float bloomWeight;

        /// Multiplicador de brilho dos elementos "quentes": 1 sem bloom; sobe até hdrBoost com o bloom ligado.
        float Hdr => 1f + (hdrBoost - 1f) * bloomWeight;

        static Color X(Color c, float k) { c.r *= k; c.g *= k; c.b *= k; return c; }

        void UpdateBloom(float dt)
        {
            var wanted = false;
            if (useBloom && !manual && Application.isPlaying)
                for (var i = 0; i < active.Count && !wanted; i++) wanted = active[i] is LightBallEffect || active[i] is DoubleLightBallEffect;
            if (wanted && !bloomOn)
            {
                bloomCamera = ViewCam;
                bloomCameraData = bloomCamera != null ? bloomCamera.GetUniversalAdditionalCameraData() : null;
                if (bloomCameraData == null) return;
                bloomSavedPost = bloomCameraData.renderPostProcessing; bloomSavedHdr = bloomCamera.allowHDR;
                bloomCameraData.renderPostProcessing = true; bloomCamera.allowHDR = true;
                if (bloomVolume == null)
                {
                    var go = new GameObject("Bloom do Light Ball");
                    go.transform.SetParent(transform, false);
                    bloomVolume = go.AddComponent<Volume>();
                    bloomVolume.isGlobal = true; bloomVolume.priority = 100f;
                    bloomProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                    bloomProfile.hideFlags = HideFlags.HideAndDontSave;
                    bloomOverride = bloomProfile.Add<Bloom>(true);
                    bloomVolume.sharedProfile = bloomProfile;
                }
                bloomVolume.gameObject.SetActive(true);
                bloomOn = true;
            }
            if (!bloomOn) return;
            bloomWeight = Mathf.MoveTowards(bloomWeight, wanted ? 1f : 0f, dt / 0.2f);
            bloomOverride.threshold.Override(bloomThreshold);
            bloomOverride.intensity.Override(bloomIntensity);
            bloomOverride.scatter.Override(bloomScatter);
            bloomVolume.weight = bloomWeight;
            if (!wanted && bloomWeight <= 0f) RestoreBloom();
        }

        void RestoreBloom()
        {
            if (bloomOn)
            {
                if (bloomCameraData != null) bloomCameraData.renderPostProcessing = bloomSavedPost;
                if (bloomCamera != null) bloomCamera.allowHDR = bloomSavedHdr;
            }
            bloomOn = false; bloomWeight = 0f;
            if (bloomVolume != null) bloomVolume.gameObject.SetActive(false);
        }

        static Gradient Grad(params Color[] colors)
        {
            var keys = new GradientColorKey[colors.Length];
            for (var i = 0; i < colors.Length; i++) keys[i] = new GradientColorKey(colors[i], colors.Length > 1 ? i / (float)(colors.Length - 1) : 0f);
            var gradient = new Gradient();
            gradient.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        [Serializable]
        public sealed class PrismSettings
        {
            [Header("Emissão (vale para um globo e para a combinação dupla)")]
            [Tooltip("Multiplica a quantidade de todas as partículas.")]
            [Range(0f, 2f)] public float emission = 1f;
            [Tooltip("Poeira mágica: pontinhos violeta, rosa e dourados em curvas suaves.")]
            [Range(0f, 2f)] public float dustDensity = 1f;
            [Tooltip("Faíscas de energia: pontos rápidos com rastro curto (algumas ciano).")]
            [Range(0f, 2f)] public float sparkDensity = 1f;
            [Tooltip("Partículas ciano/douradas orbitando o núcleo em velocidades diferentes.")]
            [Min(0)] public int orbitalParticles = 14;
            [Tooltip("Limite de partículas ao mesmo tempo, por tipo. Em celulares com pouca memória o limite cai sozinho.")]
            [Min(8)] public int maxParticles = 220;
            [Tooltip("Quanto as cores variam entre uma partícula e outra.")]
            [Range(0f, 1f)] public float colorVariation = 0.6f;
            [Header("Brilhos do núcleo")]
            [Tooltip("Miolo branco-dourado (pequeno).")]
            [Range(0f, 1.5f)] public float coreGlow = 0.9f;
            [Range(0f, 1.5f)] public float magentaGlow = 0.6f;
            [Range(0f, 1.5f)] public float goldenGlow = 0.7f;
            [Range(0f, 1.5f)] public float violetGlow = 0.55f;
            [Header("Fitas do vórtice (combinação dupla)")]
            [Range(2, 8)] public int ribbonCount = 5;
            [Min(0)] public float ribbonWidth = 0.46f;
            [Tooltip("Cor da fita do centro (esquerda) até a ponta (direita).")]
            public Gradient ribbonGradient = Grad(new Color(1f, 0.341f, 0.776f), new Color(0.820f, 0.169f, 1f), new Color(0.506f, 0.169f, 1f));
            [Tooltip("Quanto o vórtice se contrai antes da explosão.")]
            [Range(0f, 1f)] public float vortexCompression = 0.7f;
            [Header("Ondas de choque")]
            [Range(0.3f, 3f)] public float shockThickness = 1f;
            [Range(0f, 1.5f)] public float shockBrightness = 1f;
            [Tooltip("Partículas que acompanham a frente de cada onda.")]
            [Min(0)] public int shockParticles = 22;
            [Header("Impacto em cada peça")]
            [Range(0f, 1.5f)] public float targetImpact = 1f;
            [Header("Brilhos residuais")]
            [Min(0.1f)] public float residualSeconds = 0.9f;
        }

        [Serializable]
        public sealed class LightningSettings
        {
            [Range(0f, 2f)] public float intensity = 1f;
            [Tooltip("Arcos ligando os dois globos durante a órbita.")]
            [Range(0, 6)] public int arcCount = 2;
            [Range(0f, 1f)] public float branchProbability = 0.35f;
            [Tooltip("Largura do miolo branco, em casas.")]
            [Min(0)] public float arcWidth = 0.045f;
            [Tooltip("Quanto tempo cada desenho de raio dura antes de mudar.")]
            [Min(0.02f)] public float arcLifetime = 0.08f;
            [Min(0)] public float flickerFrequency = 18f;
            [Tooltip("Miolo (esquerda), brilho interno (meio) e brilho externo (direita).")]
            public Gradient electricGradient = Grad(Color.white, new Color(0.286f, 0.867f, 0.961f), new Color(0.506f, 0.169f, 1f));
            [Tooltip("Quantidade de filamentos elétricos acompanhando os braços do vórtice.")]
            [Range(0f, 2f)] public float vortexDensity = 1f;
            [Tooltip("Quantidade de descargas correndo pela borda das ondas.")]
            [Range(0f, 2f)] public float shockwaveDensity = 1f;
        }

        public PrismSettings prism = new PrismSettings();
        public LightningSettings lightning = new LightningSettings();

        static float QualityScale => Application.isMobilePlatform && SystemInfo.systemMemorySize < 3500 ? 0.6f : 1f;

        int Budget(float count, float density) => Mathf.Clamp(Mathf.RoundToInt(count * density * prism.emission * QualityScale), 0, prism.maxParticles);

        /// Uma das cores da paleta (índice) puxada em direção à cor seguinte conforme a variação pedida.
        Color Prism(int index, float vary = 0f)
        {
            Color Pick(int i)
            {
                switch (((i % 5) + 5) % 5)
                {
                    case 0: return prismViolet;
                    case 1: return prismMagenta;
                    case 2: return prismPink;
                    case 3: return prismGold;
                    default: return prismCyan;
                }
            }
            return Color.Lerp(Pick(index), Pick(index + 1), Mathf.Clamp01(vary) * prism.colorVariation);
        }

        // ---- sprites em código

        static Sprite hollowGlow;

        /// Brilho em anel macio (vazio no centro): colore em volta do globo sem cobri-lo de branco.
        static Sprite HollowGlow => hollowGlow != null ? hollowGlow : hollowGlow = Procedural(128, (x, y) =>
        {
            var r = Mathf.Sqrt(x * x + y * y);
            return r >= 1f ? 0f : Gauss(r - 0.5f, 0.22f) * Mathf.Sqrt(1f - r);
        });

        static Sprite rimHalo, rimHaloWide;

        /// Brilho de contorno para um sprite redondo: forte colado na borda (em 0,5 da metade do sprite) e apagando para fora.
        static Sprite RimHalo => rimHalo != null ? rimHalo : rimHalo = Procedural(256, (x, y) => Rim(x, y, 0.15f));
        /// O mesmo, com a luz indo bem mais longe (halo colorido).
        static Sprite RimHaloWide => rimHaloWide != null ? rimHaloWide : rimHaloWide = Procedural(256, (x, y) => Rim(x, y, 0.34f));

        static float Rim(float x, float y, float reach)
        {
            var r = Mathf.Sqrt(x * x + y * y);
            var d = r - 0.5f;
            if (d < 0f) return Smooth((d + 0.05f) / 0.05f);
            return Mathf.Exp(-d / reach) * Mathf.Clamp01((1f - r) / 0.12f);
        }

        /// Raio (em mundo) do desenho de um sprite redondo do jogo; "fallback" se não houver sprite.
        static float RoundRadius(SpriteRenderer source, float fallback)
        {
            if (source == null || source.sprite == null) return fallback;
            var extents = source.bounds.extents;
            var radius = Mathf.Max(extents.x, extents.y) * 0.94f;
            return radius > 1e-4f ? radius : fallback;
        }

        /// Risco curto de luz orientado numa direção (faísca com rastro).
        static void PlaceStreak(SpriteRenderer sr, Vector3 position, Vector3 direction, float length, float width, Color color)
        {
            Place(sr, position, length, width, color, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        /// Fita em arco de elipse inclinada: a ponta (head) é mais larga e clara, a cauda afina e some.
        static void OrbitRibbon(Ribbon ribbon, Vector3 center, float rx, float ry, float tiltDegrees, float head, float span, float direction, float width, Color headColor, Color tailColor, float alpha)
        {
            var tilt = tiltDegrees * Mathf.Deg2Rad;
            var cosT = Mathf.Cos(tilt); var sinT = Mathf.Sin(tilt);
            for (var k = 0; k <= RibbonSegments; k++)
            {
                var q = k / (float)RibbonSegments;
                var a = head - direction * q * span;
                var lx = Mathf.Cos(a) * rx; var ly = Mathf.Sin(a) * ry;
                var tx = -Mathf.Sin(a) * rx; var ty = Mathf.Cos(a) * ry;
                var point = center + new Vector3(lx * cosT - ly * sinT, lx * sinT + ly * cosT, 0f);
                var tangent = new Vector3(tx * cosT - ty * sinT, tx * sinT + ty * cosT, 0f).normalized;
                var half = new Vector3(-tangent.y, tangent.x, 0f) * (width * Mathf.Pow(1f - q, 0.7f) * Smooth(q / 0.03f + 0.4f) * 0.5f);
                ribbon.v[2 * k] = point - half; ribbon.v[2 * k + 1] = point + half;
                var col = Color.Lerp(headColor, tailColor, Mathf.Clamp01(q * 1.6f));
                col.a = Mathf.Clamp01(alpha * Mathf.Pow(1f - q, 0.6f));
                ribbon.c[2 * k] = col; ribbon.c[2 * k + 1] = col;
            }
            ribbon.mesh.vertices = ribbon.v; ribbon.mesh.colors = ribbon.c;
        }

        // ------------------------------------------------------------------------------------------ ELETRICIDADE MÁGICA

        /// Conjunto de raios reutilizáveis de um efeito: cada raio é uma fita fina (miolo branco) e uma larga (brilho ciano/violeta),
        /// com desenho irregular que muda a cada "arcLifetime", aparições intermitentes e ramificações curtas.
        /// Uso por quadro: Begin(), vários Arc(...), End() (apaga os que não foram usados).
        sealed class BoltSet
        {
            readonly PowerUpVfxPlayer player;
            readonly Ribbon[] core, glow;
            readonly Vector3[] points = new Vector3[RibbonSegments + 1];
            int used, lastUsed;

            public BoltSet(PowerUpVfxPlayer player, List<GameObject> owner, int order, int capacity)
            {
                this.player = player;
                core = new Ribbon[capacity]; glow = new Ribbon[capacity];
                for (var i = 0; i < capacity; i++)
                {
                    glow[i] = player.MakeRibbon("Raio (brilho)", order, owner);
                    core[i] = player.MakeRibbon("Raio", order + 1, owner);
                }
            }

            public void Begin() => used = 0;

            public void End()
            {
                for (var i = used; i < lastUsed; i++) { Clear(core[i]); Clear(glow[i]); }
                lastUsed = used;
            }

            static void Clear(Ribbon ribbon)
            {
                for (var i = 0; i < ribbon.c.Length; i++) ribbon.c[i].a = 0f;
                ribbon.mesh.colors = ribbon.c;
            }

            static float Knot(float x, int seed)
            {
                var i = Mathf.FloorToInt(x);
                return Mathf.Lerp(Hash(i, seed), Hash(i + 1, seed), x - i) * 2f - 1f;
            }

            /// Um raio de a até b. time: relógio do efeito; seed: identidade do raio; bend: curva do caminho (fração do comprimento).
            public void Arc(Vector3 a, Vector3 b, float time, int seed, float alpha, float cell, float bend = 0f, bool branches = true, float duty = 0.66f)
            {
                var L = player.lightning;
                alpha *= L.intensity;
                if (alpha <= 0.004f) return;
                var frame = Mathf.FloorToInt(time / Mathf.Max(0.02f, L.arcLifetime));
                if (Hash(seed, frame * 7 + 1) > duty) return;                                    // intermitente: some em parte dos quadros
                alpha *= 0.65f + 0.35f * Mathf.Sin(time * L.flickerFrequency * Mathf.PI * 2f + seed);
                if (!Draw(a, b, seed * 131 + frame, alpha, cell, bend, 1f)) return;
                if (!branches || Hash(seed, frame * 7 + 2) >= L.branchProbability) return;
                var from = points[Mathf.Clamp((int)(RibbonSegments * (0.3f + 0.4f * Hash(seed, frame * 7 + 3))), 1, RibbonSegments - 1)];
                var along = b - a; along.z = 0f;
                var length = along.magnitude;
                if (length < 1e-4f) return;
                var side = new Vector3(-along.y, along.x, 0f) / length * (Hash(seed, frame * 7 + 4) > 0.5f ? 1f : -1f);
                var to = from + (along / length * 0.6f + side * 0.8f).normalized * (length * (0.16f + 0.2f * Hash(seed, frame * 7 + 5)));
                Draw(from, to, seed * 977 + frame, alpha * 0.7f, cell, 0f, 0.7f);
            }

            bool Draw(Vector3 a, Vector3 b, int seed, float alpha, float cell, float bend, float widthScale)
            {
                if (used >= core.Length) return false;
                var delta = b - a; delta.z = 0f;
                var length = delta.magnitude;
                if (length < 1e-4f) return false;
                var L = player.lightning;
                var c = core[used]; var g = glow[used]; used++;
                var side = new Vector3(-delta.y, delta.x, 0f) / length;
                var amplitude = Mathf.Min(length * 0.16f, cell * 0.36f);
                for (var k = 0; k <= RibbonSegments; k++)
                {
                    var u = k / (float)RibbonSegments;
                    var jag = 0.65f * Knot(u * 7f, seed) + 0.35f * Knot(u * 19f, seed + 53);
                    var envelope = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 0.6f);
                    points[k] = a + delta * u + side * (jag * amplitude * envelope + bend * length * Mathf.Sin(Mathf.PI * u));
                }
                var hdr = player.Hdr;
                var coreColor = X(L.electricGradient.Evaluate(0f), hdr);
                for (var k = 0; k <= RibbonSegments; k++)
                {
                    var u = k / (float)RibbonSegments;
                    var tangent = points[Mathf.Min(RibbonSegments, k + 1)] - points[Mathf.Max(0, k - 1)]; tangent.z = 0f;
                    tangent = tangent.sqrMagnitude > 1e-12f ? tangent.normalized : delta / length;
                    var normal = new Vector3(-tangent.y, tangent.x, 0f);
                    var ends = Smooth(u / 0.07f) * Smooth((1f - u) / 0.07f);
                    var width = L.arcWidth * cell * widthScale * (0.55f + 0.9f * Hash(seed, 40 + (int)(u * 9f))) * ends;
                    c.v[2 * k] = points[k] - normal * (width * 0.5f); c.v[2 * k + 1] = points[k] + normal * (width * 0.5f);
                    g.v[2 * k] = points[k] - normal * (width * 3.4f); g.v[2 * k + 1] = points[k] + normal * (width * 3.4f);
                    var col = coreColor; col.a = Mathf.Clamp01(alpha * ends);
                    c.c[2 * k] = col; c.c[2 * k + 1] = col;
                    col = X(L.electricGradient.Evaluate(0.45f + 0.55f * Mathf.Abs(2f * u - 1f)), 1f + 0.6f * (hdr - 1f)); col.a = Mathf.Clamp01(0.7f * alpha * ends);
                    g.c[2 * k] = col; g.c[2 * k + 1] = col;
                }
                c.mesh.vertices = c.v; c.mesh.colors = c.c;
                g.mesh.vertices = g.v; g.mesh.colors = g.c;
                return true;
            }
        }

        // ------------------------------------------------------------------------------------------ LIGHT BALL (um globo)

        /// 1. Ativação (núcleo em camadas) → 2. conexões mágicas → 3. alvos carregando → 4. explosão → 5. limpeza → 6. finalização.
        sealed class LightBallEffect : Effect
        {
            public Transform follow; public float cell;
            LightBallSettings s; PrismSettings P;
            SpriteRenderer auraViolet, haloMagenta, coreGold, flare, orbitA, orbitB, flashCore, hot, burstGold, burstPink, burstViolet, ringGold, ringMagenta, ringCyan;
            readonly SpriteRenderer[] pulses = new SpriteRenderer[2];
            SpriteRenderer[] dust, orbiters, glints, radial;
            Ribbon[] swirls;
            Ribbon[] sweeps = new Ribbon[0];
            Shard[] confetti = new Shard[0];
            BoltSet bolts, strikes;
            SpriteRenderer rimTight, rimWide, ballSource, litBall, warmHalo;
            SpriteRenderer[] rainbow;
            readonly List<Beam> beams = new List<Beam>();
            Vector3 center;
            float detonatedAt = -1f, cancelledAt = -1f, ringK = BombRing;
            const float HaloRing = 0.62f;       // raio do anel na máscara 10

            public bool Ended => detonatedAt >= 0f || cancelledAt >= 0f;

            sealed class Beam
            {
                public Transform target; public Vector3 end, control; public Color color; public float born, seed, length; public int index;
                public Ribbon glow, mid, core, strand, strand2;
                public SpriteRenderer tint, node, dots, star, flash, pop;
                public SpriteRenderer[] sparks = new SpriteRenderer[0], sparkles = new SpriteRenderer[0], twinkles = new SpriteRenderer[0];
                public Shard[] confetti = new Shard[0];
            }

            static Sprite Or(Sprite mask, Sprite fallback) => mask != null ? mask : fallback;

            static void SetUv(Ribbon ribbon, float u0, float u1, float v0, float v1)
            {
                for (var i = 0; i <= RibbonSegments; i++)
                {
                    var u = Mathf.Lerp(u0, u1, i / (float)RibbonSegments);
                    ribbon.uv[2 * i] = new Vector2(u, v0); ribbon.uv[2 * i + 1] = new Vector2(u, v1);
                }
                ribbon.mesh.uv = ribbon.uv;
            }

            public override void Begin()
            {
                s = player.lightBall; P = player.prism;
                var p = player;
                duration = 30f;
                center = follow.position;
                if (p.lbPulseRing != null) ringK = 0.645f;
                ballSource = follow.GetComponent<SpriteRenderer>();
                rainbow = new SpriteRenderer[s.rainbowRays * 2];
                for (var i = 0; i < rainbow.Length; i++) rainbow[i] = Add(GlowDot, 0);
                warmHalo = Add(GlowDot, 0);
                auraViolet = Add(HollowGlow, 0);
                haloMagenta = Add(HollowGlow, 0);
                rimWide = Add(RimHaloWide, 0);
                rimTight = Add(RimHalo, 1);
                litBall = Add(GlowDot, 1);
                flare = Add(Or(p.lbChargeFlare, SeekerRaysLine), 1);
                burstViolet = Add(SeekerRaysLine, 1);
                burstPink = Add(SeekerRaysLine, 2);
                burstGold = Add(Or(p.lbFinalBurst, SeekerRaysLine), 3);
                for (var i = 0; i < pulses.Length; i++) pulses[i] = Add(Or(p.lbPulseRing, BombRingLine), 3);
                ringCyan = Add(Or(p.lbPulseRing, BombRingLine), 3);
                ringMagenta = Add(Or(p.lbPulseRing, BombRingMagenta), 3);
                ringGold = Add(Or(p.lbPulseRing, BombRingLine), 4);
                if (p.lbOrbitEllipse != null) orbitA = Add(p.lbOrbitEllipse, 4);
                if (p.lbOrbitArc != null) orbitB = Add(p.lbOrbitArc, 4);
                swirls = new Ribbon[s.swirlCount];
                for (var i = 0; i < swirls.Length; i++) swirls[i] = player.MakeRibbon("Rastro orbital", 5, objects);
                dust = new SpriteRenderer[p.Budget(18f, P.dustDensity)];
                for (var i = 0; i < dust.Length; i++) dust[i] = Add(GlowDot, 5);
                orbiters = new SpriteRenderer[p.Budget(P.orbitalParticles, 1f)];
                for (var i = 0; i < orbiters.Length; i++) orbiters[i] = Add(GlowDot, 9);
                glints = new SpriteRenderer[p.Budget(s.sparkleCount, 1f)];
                for (var i = 0; i < glints.Length; i++) glints[i] = Add(CrossStar, 10);
                radial = new SpriteRenderer[p.Budget(14f, P.sparkDensity)];
                for (var i = 0; i < radial.Length; i++) radial[i] = Add(GlowDot, 12);
                bolts = new BoltSet(player, objects, 10, 30);
                coreGold = Add(GlowDot, 11);
                hot = Add(GlowDot, 12);
                flashCore = Add(BombCoreWhite, 13);
            }

            public void AddBeam(Transform target, Color color)
            {
                if (Ended) return;
                foreach (var existing in beams) if (existing.target == target) return;
                var light = beams.Count >= s.lightAbove;
                var p = player;
                var beam = new Beam { target = target, end = target.position, color = color, born = age, index = beams.Count, seed = Hash(beams.Count * 7 + 3, 41) };
                beam.glow = player.MakeRibbon("Conexão (halo)", 6, objects, p.lbBeamHalo != null ? p.lbBeamHalo.texture : null);
                if (p.lbBeamHalo != null) SetUv(beam.glow, 0.02f, 0.975f, 0.22f, 0.77f);
                beam.mid = player.MakeRibbon("Conexão (energia)", 7, objects);
                if (!light && s.beamStrandAlpha > 0f)
                {
                    beam.strand = player.MakeRibbon("Conexão (fita)", 7, objects, p.lbRibbonWave != null ? p.lbRibbonWave.texture : null);
                    if (p.lbRibbonWave != null) SetUv(beam.strand, 0.02f, 0.98f, 0.34f, 0.66f);
                    if (p.lbRibbonSecondary != null)
                    {
                        beam.strand2 = player.MakeRibbon("Conexão (fita 2)", 7, objects, p.lbRibbonSecondary.texture);
                        SetUv(beam.strand2, 0.02f, 0.98f, 0.35f, 0.68f);
                    }
                }
                beam.core = player.MakeRibbon("Conexão (miolo)", 8, objects, p.lbBeamCore != null ? p.lbBeamCore.texture : null);
                if (p.lbBeamCore != null) SetUv(beam.core, 0.02f, 0.975f, 0.42f, 0.57f);
                beam.tint = Add(GlowDot, 9);
                beam.node = Add(Or(p.lbTargetHalo, BombRingMagenta), 9);
                if (!light) beam.dots = Add(DottedRing, 11);
                beam.star = Add(Or(p.lbTargetPreflash, CrossStar), 10);
                if (!light)
                {
                    beam.sparks = new SpriteRenderer[p.Budget(2f, P.sparkDensity)];
                    for (var i = 0; i < beam.sparks.Length; i++) beam.sparks[i] = Add(GlowDot, 9);
                }
                beams.Add(beam);
            }

            public void Detonate()
            {
                if (Ended) return;
                detonatedAt = age;
                duration = age + Mathf.Max(Mathf.Max(s.burstSeconds + 0.3f, 0.15f + s.sweepSeconds), Mathf.Max(0.3f + s.finishSeconds, s.confettiLife * 1.25f));
                var light = beams.Count > s.lightAbove;
                sweeps = new Ribbon[2];
                for (var i = 0; i < sweeps.Length; i++) sweeps[i] = player.MakeRibbon("Redemoinho", 6, objects);
                strikes = new BoltSet(player, objects, 12, Mathf.Min(beams.Count, 24) * 2);
                for (var b = 0; b < beams.Count; b++)
                {
                    var beam = beams[b];
                    if (beam.target != null) beam.end = beam.target.position;
                    beam.flash = Add(BombCoreWhite, 13);
                    if (!light)
                    {
                        beam.pop = Add(Or(player.lbTargetPop, CrossStar), 12);
                        beam.sparkles = new SpriteRenderer[player.Budget(3f + 2f * Hash(b, 91), 1f)];      // 3 a 5 por peça
                        for (var i = 0; i < beam.sparkles.Length; i++) beam.sparkles[i] = Add(CrossStar, 12);
                    }
                    beam.twinkles = new SpriteRenderer[light ? (b % 3 == 0 ? 1 : 0) : 2];
                    for (var i = 0; i < beam.twinkles.Length; i++) beam.twinkles[i] = Add(CrossStar, 12);
                    beam.confetti = new Shard[light ? Mathf.Min(2, s.targetConfetti) : s.targetConfetti];
                    for (var i = 0; i < beam.confetti.Length; i++)
                    {
                        var id = b * 31 + i;
                        var angle = Hash(id, 51) * Mathf.PI * 2f;
                        beam.confetti[i] = new Shard
                        {
                            sr = Add(player.Fragment(id), 14, false), origin = beam.end,
                            velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (1.6f + 2.2f * Hash(id, 53)),
                            spin = (Hash(id, 54) - 0.5f) * 720f, size = s.confettiSize * (0.6f + 0.8f * Hash(id, 55)),
                            life = s.confettiLife * (0.7f + 0.5f * Hash(id, 56)), delay = detonatedAt + 0.02f,
                            color = Hash(id, 52) < 0.25f ? player.Prism(id, Hash(id, 57)) : Color.Lerp(beam.color, Color.white, 0.15f),
                        };
                    }
                }
                confetti = new Shard[s.ballConfetti];
                for (var i = 0; i < confetti.Length; i++)
                {
                    var angle = (i + Hash(i, 61)) / confetti.Length * Mathf.PI * 2f;
                    var palette = player.confettiColors;
                    confetti[i] = new Shard
                    {
                        sr = Add(player.Fragment(i), 14, false), origin = center,
                        velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (2.2f + 3f * Hash(i, 63)),
                        spin = (Hash(i, 64) - 0.5f) * 720f, size = s.confettiSize * (0.7f + 0.9f * Hash(i, 65)),
                        life = s.confettiLife * (0.75f + 0.5f * Hash(i, 66)), delay = detonatedAt + 0.02f,
                        color = Hash(i, 62) < 0.45f || palette.Length == 0 ? player.Prism(i, Hash(i, 67)) : palette[i % palette.Length],
                    };
                }
            }

            public void Cancel()
            {
                if (Ended) return;
                cancelledAt = age;
                duration = age + 0.2f;
            }

            /// Ponto e normal da conexão na fração u (0 = globo, 1 = peça): curva de Bézier com ondulação em S presa nas pontas.
            void Curve(Beam beam, float u, float t, out Vector3 point, out Vector3 normal)
            {
                point = (1f - u) * (1f - u) * center + 2f * (1f - u) * u * beam.control + u * u * beam.end;
                var tangent = 2f * (1f - u) * (beam.control - center) + 2f * u * (beam.end - beam.control); tangent.z = 0f;
                tangent = tangent.sqrMagnitude > 1e-10f ? tangent.normalized : Vector3.right;
                normal = new Vector3(-tangent.y, tangent.x, 0f);
                var cells = beam.length / cell;
                point += normal * (Mathf.Sin(u * Mathf.PI * 2f * s.beamWaves + beam.seed * 6.28f + t * 2.2f) * s.beamWave * cell * Mathf.Sin(Mathf.PI * u) * Mathf.Min(1f, cells / 3f));
            }

            public override void Evaluate(float t)
            {
                if (follow != null && follow.gameObject.activeInHierarchy) center = follow.position;
                else if (!Ended) Cancel();

                var p = player;
                var detonated = detonatedAt >= 0f;
                var since = detonated ? t - detonatedAt : 0f;
                var e = detonated ? Mathf.Clamp01(since / s.burstSeconds) : 0f;
                var outFade = 1f - e;
                var cancelFade = cancelledAt < 0f ? 1f : 1f - Smooth((t - cancelledAt) / 0.18f);
                var charge = Smooth(t / 0.18f) * cancelFade;
                var pulse = 0.5f + 0.5f * Mathf.Sin(t * 9f);
                var level = Mathf.Clamp01(beams.Count / 6f);                 // a magia fica mais intensa a cada peça conectada
                var finish = Mathf.Clamp01((since - 0.3f) / s.finishSeconds);
                var live = charge * outFade;
                var off = A(Color.white, 0f);
                var hdr = p.Hdr;                    // > 1 só com o bloom ligado: é o que faz miolos, raios e fitas vazarem luz
                var warm = 1f + 0.5f * (hdr - 1f);

                // ---------- CONTORNO LUMINOSO do globo (como o da flecha): luz forte colada na borda e halo magenta em volta;
                // mais forte no instante em que o globo acende
                var rimRadius = RoundRadius(ballSource, cell * 0.43f);
                var rimKick = 1f + 1.4f * (1f - Smooth(t / 0.8f));
                Place(rimTight, center, rimRadius * 4f, rimRadius * 4f, A(X(Color.Lerp(p.prismGold, Color.white, 0.35f), hdr), 0.9f * live * rimKick * (0.85f + 0.15f * pulse)));
                // brilho amarelo quente colado no globo (o mesmo da flecha) e um clarão quente atrás
                Place(rimWide, center, rimRadius * 4f, rimRadius * 4f, A(X(s.warmGlow, warm), s.warmGlowAlpha * live * rimKick));
                var warmSize = s.warmGlowSize * cell * (0.92f + 0.08f * pulse);
                Place(warmHalo, center, warmSize, warmSize, A(Color.Lerp(s.warmGlow, Color.white, 0.25f), 0.6f * s.warmGlowAlpha * live));

                // raios de várias cores abrindo atrás do globo: cada raio de uma cor, girando devagar e pulsando
                var rainbowLive = charge * (1f - Smooth(e / 0.5f));
                var rainbowCount = rainbow.Length / 2;
                for (var j = 0; j < rainbowCount; j++)
                {
                    var angle = (j + 0.5f) / rainbowCount * Mathf.PI * 2f + t * s.rainbowSpin * Mathf.Deg2Rad;
                    var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                    var tint = Color.HSVToRGB(Mathf.Repeat(j / (float)rainbowCount + 0.04f * t, 1f), 0.82f, 1f);
                    var beat = 0.78f + 0.22f * Mathf.Sin(t * 5f + j * 1.7f);
                    var length = s.rainbowLength * cell * (0.6f + 0.4f * Hash(j, 611)) * beat * (0.55f + 0.45f * Smooth(t / 0.35f)) * (1f + 0.5f * EaseOut(e));
                    var width = s.rainbowWidth * cell * (0.7f + 0.6f * Hash(j, 612));
                    var at = center + direction * (rimRadius * 0.8f + length * 0.5f);
                    PlaceStreak(rainbow[2 * j], at, direction, length, width, A(tint, 0.6f * s.rainbowAlpha * rainbowLive));
                    PlaceStreak(rainbow[2 * j + 1], at, direction, length * 0.8f, width * 0.26f, A(X(Color.Lerp(tint, Color.white, 0.6f), warm), 0.75f * s.rainbowAlpha * rainbowLive * beat));
                }
                // o próprio globo acende (pulsa junto com o giro)
                p.PlaceLit(litBall, ballSource, Color.Lerp(p.prismGold, Color.white, 0.45f), p.presence.ballLight * live * (0.55f + 0.45f * pulse) * (0.6f + 0.4f * level));

                // ---------- 1. NÚCLEO EM CAMADAS: aura violeta e halo magenta (vazios no meio, o globo continua visível),
                // miolo dourado pequeno, brilho de carga, órbitas, pulsos periódicos, partículas orbitando e descargas
                var spread = 1f + 0.6f * EaseOut(e);
                var auraSize = s.auraSize * cell * (0.94f + 0.06f * pulse) * spread;
                Place(auraViolet, center, auraSize, auraSize, A(p.prismViolet, P.violetGlow * s.auraAlpha * live));
                Place(haloMagenta, center, auraSize * 0.66f, auraSize * 0.66f, A(p.prismMagenta, P.magentaGlow * live * (0.55f + 0.45f * level)));
                var coreSize = cell * (0.75f + 0.12f * pulse + 0.2f * level);
                Place(coreGold, center, coreSize, coreSize, A(Color.Lerp(p.prismGold, Color.white, 0.3f), 0.5f * P.goldenGlow * live));
                var flareSize = s.raysSize * cell * (0.8f + 0.12f * pulse + 0.25f * level);
                Place(flare, center, flareSize, flareSize, A(p.prismGold, 0.5f * s.raysAlpha * charge * (0.7f + 0.3f * pulse) * (1f - Smooth(e / 0.3f))), t * s.raysSpin);

                var orbitEnv = Smooth(t / 0.2f) * cancelFade * Mathf.Pow(outFade, 1.2f);
                var orbitSize = s.orbitSize * cell * (1f + 0.9f * EaseOut(e));
                var orbitSpin = s.orbitSpin * (1f + 0.8f * level);
                Place(orbitA, center, orbitSize, orbitSize, A(p.prismGold, s.orbitAlpha * orbitEnv), t * orbitSpin);
                Place(orbitB, center, orbitSize * 0.95f, orbitSize * 0.95f, A(p.prismPink, s.orbitAlpha * orbitEnv), 40f - t * orbitSpin * 0.8f);
                for (var i = 0; i < swirls.Length; i++)
                {
                    var direction = i % 2 == 0 ? 1f : -1f;
                    var rx = s.swirlRadius * cell * (1f + 0.22f * i) * (1f + 1.6f * EaseOut(e));
                    OrbitRibbon(swirls[i], center, rx, rx * 0.34f, -14f + i * 24f + 6f * Mathf.Sin(t * 2f + i), (t * s.swirlSpeed * (1f + 0.6f * level) + i * 0.37f) * Mathf.PI * 2f * direction,
                        300f * Mathf.Deg2Rad * Smooth(t / 0.3f), direction, s.swirlWidth * cell,
                        i % 2 == 0 ? Color.Lerp(p.prismGold, Color.white, 0.25f) : p.prismCyan, i % 2 == 0 ? p.prismPink : p.prismViolet, s.swirlAlpha * orbitEnv);
                }
                for (var i = 0; i < pulses.Length; i++)
                {
                    var phase = Mathf.Repeat(t * 1.5f + i * 0.5f, 1f);
                    var size = cell * (0.9f + 1.8f * phase) / ringK;
                    Place(pulses[i], center, size, size, A(X(i == 0 ? p.prismMagenta : p.prismCyan, warm), 0.55f * live * Mathf.Pow(1f - phase, 1.5f) * (0.45f + 0.55f * level)));
                }
                for (var j = 0; j < orbiters.Length; j++)
                {
                    var lane = j % 3;
                    var speed = (1.5f - 0.4f * lane) * (j % 2 == 0 ? 1f : -1f) * (1f + level);
                    var angle = t * speed * 2.4f + Hash(j, 71) * Mathf.PI * 2f;
                    var radius = cell * (0.55f + 0.2f * lane) * (1f + 2.2f * EaseOut(e));
                    var tilt = (lane * 55f - 30f) * Mathf.Deg2Rad;
                    var lx = Mathf.Cos(angle) * radius; var ly = Mathf.Sin(angle) * radius * 0.5f;
                    var size = cell * 0.11f * (0.6f + 0.8f * Hash(j, 72));
                    Place(orbiters[j], center + new Vector3(lx * Mathf.Cos(tilt) - ly * Mathf.Sin(tilt), lx * Mathf.Sin(tilt) + ly * Mathf.Cos(tilt), 0f), size, size,
                        A(lane == 0 ? p.prismCyan : lane == 1 ? p.prismGold : p.prismPink, live * (0.6f + 0.4f * Mathf.Sin(t * 14f + j))));
                }
                for (var j = 0; j < dust.Length; j++)
                {
                    // poeira mágica: sai do globo numa curva suave e some
                    var phase = Mathf.Repeat(t * (0.4f + 0.3f * Hash(j, 75)) + Hash(j, 76), 1f);
                    var angle = Hash(j, 77) * Mathf.PI * 2f + phase * 2.6f * (j % 2 == 0 ? 1f : -1f);
                    var radius = cell * (0.4f + 1.15f * phase) * spread;
                    var size = cell * 0.075f * (0.5f + Hash(j, 78));
                    Place(dust[j], center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f), size, size,
                        A(p.Prism(j % 4, Hash(j, 79)), 0.85f * live * Mathf.Sin(Mathf.PI * phase)));
                }
                for (var j = 0; j < glints.Length; j++)
                {
                    // brilhos de 4 pontas: cada um aparece num instante e num lugar diferente
                    var clock = t * (0.8f + 0.6f * Hash(j, 81)) + Hash(j, 82);
                    var cycle = Mathf.FloorToInt(clock); var phase = clock - cycle;
                    if (phase > 0.4f) { glints[j].color = off; continue; }
                    var env = Mathf.Sin(Mathf.PI * phase / 0.4f);
                    var angle = Hash(j, cycle * 3 + 1) * Mathf.PI * 2f;
                    var radius = cell * (0.5f + 0.8f * Hash(j, cycle * 3 + 2)) * spread;
                    var size = cell * 0.36f * (0.45f + 0.75f * Hash(j, 83)) * env;
                    Place(glints[j], center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f), size, size,
                        A(j % 3 == 0 ? p.prismCyan : j % 3 == 1 ? Color.Lerp(p.prismGold, Color.white, 0.5f) : Color.white, live * env));
                }

                bolts.Begin();
                if (!detonated)
                    for (var j = 0; j < 3; j++)
                    {
                        // pequenas descargas prismáticas na superfície do globo
                        var a0 = j / 3f * Mathf.PI * 2f + t * 0.7f;
                        var a1 = a0 + 1.1f + 0.6f * Mathf.Sin(t * 2f + j);
                        bolts.Arc(center + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * (0.5f * cell), center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * (0.62f * cell),
                            t, 300 + j, 0.85f * charge * (0.4f + 0.6f * level), cell, 0.22f);
                    }
                if (strikes != null) strikes.Begin();

                // ---------- 5. LIMPEZA: redemoinho rosa/violeta e ciano/violeta abrindo pelo tabuleiro
                var sweep = Mathf.Clamp01((since - 0.15f) / s.sweepSeconds);
                var sweepEnv = detonated ? Smooth(sweep / 0.15f) * (1f - Smooth((sweep - 0.55f) / 0.45f)) : 0f;
                for (var i = 0; i < sweeps.Length; i++)
                {
                    var direction = i % 2 == 0 ? 1f : -1f;
                    var rx = s.sweepRadius * cell * (0.55f + 0.45f * EaseOut(sweep)) * (1f - 0.22f * i);
                    OrbitRibbon(sweeps[i], center, rx, rx * 0.55f, -12f + i * 34f, (sweep * 1.3f + i * 0.5f) * Mathf.PI * 2f * direction, 250f * Mathf.Deg2Rad, direction,
                        s.sweepWidth * cell, i == 0 ? p.prismPink : p.prismCyan, p.prismViolet, s.sweepAlpha * sweepEnv);
                }

                // ---------- 2. CONEXÕES MÁGICAS
                var beamFade = detonated ? 1f - Smooth(since / 0.32f) : cancelFade;
                var beamBoost = detonated ? 1f + 0.9f * (1f - Mathf.Clamp01(since / 0.2f)) : 1f;
                var coreScale = p.lbBeamCore != null ? 2.5f : 1f;
                var glowScale = p.lbBeamHalo != null ? 1.9f : 1f;
                var waveMask = p.lbRibbonWave != null;
                var haloMask = p.lbTargetHalo != null;
                for (var b = 0; b < beams.Count; b++)
                {
                    var beam = beams[b];
                    if (!detonated && beam.target != null && beam.target.gameObject.activeInHierarchy) beam.end = beam.target.position;
                    var reach = Mathf.Clamp01((t - beam.born) / s.beamTravelSeconds);
                    var delta = beam.end - center; delta.z = 0f;
                    beam.length = Mathf.Max(0.001f, delta.magnitude);
                    var side = new Vector3(-delta.y, delta.x, 0f) / beam.length;
                    var bend = (beam.seed > 0.5f ? 1f : -1f) * s.beamCurve * (0.6f + 0.8f * Hash(b, 42));
                    beam.control = center + delta * 0.5f + side * (beam.length * bend + Mathf.Sin(t * 3f + beam.seed * 20f) * 0.06f * cell);
                    var cells = beam.length / cell;
                    var sway = 0.75f + 0.25f * Mathf.Sin(t * 7f + beam.seed * 13f);
                    for (var k = 0; k <= RibbonSegments; k++)
                    {
                        var q = k / (float)RibbonSegments;
                        var u = q * reach;
                        Curve(beam, u, t, out var point, out var normal);
                        var emerge = Smooth(u * cells / 1.2f);                 // nasce na borda do globo, não no meio dele
                        var taper = Mathf.Lerp(1.2f, 0.6f, u) * (reach < 1f ? Smooth((1f - q) / 0.15f + 0.2f) : 1f) * beamBoost;
                        var flow = 0.7f + 0.3f * Mathf.Sin(u * cells * 5f - t * 16f + beam.seed * 9f);      // energia correndo até a peça

                        // miolo: único lugar branco, bem fino
                        var half = normal * (s.beamCoreWidth * 0.55f * cell * taper * 0.5f * coreScale);
                        beam.core.v[2 * k] = point - half; beam.core.v[2 * k + 1] = point + half;
                        var col = X(Color.white, hdr); col.a = Mathf.Clamp01(0.6f * s.beamAlpha * beamFade * flow * emerge);
                        beam.core.c[2 * k] = col; beam.core.c[2 * k + 1] = col;

                        // energia: dourado perto do globo, rosa no meio, magenta na peça
                        half = normal * (s.beamCoreWidth * 1.7f * cell * taper * 0.5f);
                        beam.mid.v[2 * k] = point - half; beam.mid.v[2 * k + 1] = point + half;
                        // (verde baixo em todo o caminho: somar dourado ou ciano com magenta é o que deixava a conexão branca)
                        col = X(u < 0.22f ? Color.Lerp(p.prismGold, p.prismPink, u / 0.22f) : Color.Lerp(p.prismPink, p.prismMagenta, (u - 0.22f) / 0.78f), warm);
                        col.a = Mathf.Clamp01(0.8f * s.beamAlpha * beamFade * (0.6f + 0.4f * flow) * emerge);
                        beam.mid.c[2 * k] = col; beam.mid.c[2 * k + 1] = col;

                        // halo largo e translúcido: magenta → violeta
                        half = normal * (s.beamGlowWidth * 0.8f * cell * taper * 0.5f * glowScale);
                        beam.glow.v[2 * k] = point - half; beam.glow.v[2 * k + 1] = point + half;
                        col = Color.Lerp(p.prismMagenta, p.prismViolet, Mathf.Clamp01(u * 1.4f));
                        col.a = Mathf.Clamp01(0.5f * s.beamGlowAlpha * beamFade * emerge);
                        beam.glow.c[2 * k] = col; beam.glow.c[2 * k + 1] = col;

                        if (beam.strand == null) continue;
                        var wave = Mathf.Max(0f, Mathf.Sin(Mathf.PI * u));
                        var twisted = waveMask ? point : point + normal * (Mathf.Sin(u * cells * 4.5f - t * 9f + beam.seed * 30f) * 0.11f * cell * wave);
                        half = waveMask ? normal * (s.beamStrandWidth * cell * 0.5f * Mathf.Sqrt(wave) * sway * beamBoost)
                            : normal * (s.beamCoreWidth * 0.7f * cell * taper * 0.5f);
                        beam.strand.v[2 * k] = twisted - half; beam.strand.v[2 * k + 1] = twisted + half;
                        col = Color.Lerp(p.prismPink, p.prismViolet, u);
                        col.a = Mathf.Clamp01(0.7f * s.beamStrandAlpha * beamFade * Mathf.Sqrt(wave) * emerge);
                        beam.strand.c[2 * k] = col; beam.strand.c[2 * k + 1] = col;
                        if (beam.strand2 == null) continue;
                        half *= -(1.75f - sway);
                        beam.strand2.v[2 * k] = point - half; beam.strand2.v[2 * k + 1] = point + half;
                        col = Color.Lerp(p.prismMagenta, p.prismViolet, u);
                        col.a = Mathf.Clamp01(0.6f * s.beamStrandAlpha * beamFade * Mathf.Sqrt(wave) * emerge);
                        beam.strand2.c[2 * k] = col; beam.strand2.c[2 * k + 1] = col;
                    }
                    beam.core.mesh.vertices = beam.core.v; beam.core.mesh.colors = beam.core.c;
                    beam.mid.mesh.vertices = beam.mid.v; beam.mid.mesh.colors = beam.mid.c;
                    beam.glow.mesh.vertices = beam.glow.v; beam.glow.mesh.colors = beam.glow.c;
                    if (beam.strand != null) { beam.strand.mesh.vertices = beam.strand.v; beam.strand.mesh.colors = beam.strand.c; }
                    if (beam.strand2 != null) { beam.strand2.mesh.vertices = beam.strand2.v; beam.strand2.mesh.colors = beam.strand2.c; }

                    // faíscas de energia correndo pela conexão, do globo até a peça
                    for (var j = 0; j < beam.sparks.Length; j++)
                    {
                        var u = Mathf.Repeat(t * 1.5f + beam.seed + j * 0.5f, 1f) * reach;
                        Curve(beam, u, t, out var at, out var n);
                        PlaceStreak(beam.sparks[j], at, new Vector3(n.y, -n.x, 0f), cell * 0.34f, cell * 0.1f,
                            A(j % 2 == 0 ? p.prismCyan : Color.Lerp(p.prismGold, Color.white, 0.4f), beamFade * Mathf.Sin(Mathf.PI * u) * Smooth(u * cells)));
                    }

                    var vary = 0.8f + 0.4f * beam.seed;
                    // estalos elétricos correndo por um trecho da conexão, de vez em quando
                    if (b < 20 && reach >= 1f && beamFade > 0.01f)
                    {
                        var u0 = 0.15f + 0.5f * Hash(b, Mathf.FloorToInt(t * 6f));
                        Curve(beam, u0, t, out var from, out _);
                        Curve(beam, Mathf.Min(1f, u0 + 0.3f), t, out var to, out _);
                        bolts.Arc(from, to, t, 800 + b, 0.9f * beamFade, cell, 0f, true, 0.3f);
                    }
                    if (!detonated)
                    {
                        // ---------- 3. ALVOS CARREGANDO: brilho discreto na cor da peça, aura magenta e pontinhos rosa girando
                        var sinceHit = t - beam.born - s.beamTravelSeconds;
                        var arrive = Smooth(sinceHit / 0.1f) * cancelFade;
                        var ringIn = Smooth((sinceHit - 0.1f) / 0.22f) * cancelFade;
                        var beat = 0.5f + 0.5f * Mathf.Sin(t * 10f + beam.seed * 9f);
                        Place(beam.tint, beam.end, cell * (1.25f + 0.12f * beat), cell * (1.25f + 0.12f * beat), A(beam.color, 0.6f * s.targetGlowAlpha * arrive));
                        var nodeSize = s.nodeSize * cell * (1.3f - 0.3f * ringIn) * (0.96f + 0.06f * beat) / (haloMask ? HaloRing : BombRing) * 0.72f;
                        Place(beam.node, beam.end, nodeSize, nodeSize, A(p.prismMagenta, s.nodeAlpha * ringIn));
                        var dotsSize = s.nodeSize * cell * 0.72f * 1.28f / BombRing;
                        Place(beam.dots, beam.end, dotsSize, dotsSize, A(p.prismPink, s.nodeAlpha * ringIn), t * 50f * (beam.seed > 0.5f ? 1f : -1f));
                        var hit = 1f - Smooth(sinceHit / 0.22f);
                        var preSize = cell * (0.45f + 0.6f * hit + 0.06f * beat) * vary;
                        Place(beam.star, beam.end, preSize, preSize, A(Color.Lerp(p.prismGold, Color.white, 0.5f + 0.5f * hit), s.nodeAlpha * arrive * (0.3f + 0.7f * hit)), beam.seed * 90f);
                        continue;
                    }

                    // ---------- 4. EXPLOSÃO em cada peça: anel magenta pequeno, clarão branco-dourado curto, 3 a 5 brilhos e
                    // fragmentos na cor da peça. Tamanho, rotação e intensidade variam de peça para peça.
                    var grow = EaseOut(Mathf.Clamp01(e / 0.45f));
                    var impact = P.targetImpact;
                    // um raio cai em cada peça no instante da explosão
                    if (b < 24 && strikes != null)
                        strikes.Arc(beam.end + new Vector3((beam.seed - 0.5f) * 0.9f, 2.1f, 0f) * cell, beam.end, t, 700 + b, impact * (1f - Smooth(since / 0.2f)), cell, 0f, true, 1f);
                    var flashSize = s.targetFlashSize * 0.6f * cell * vary;
                    Place(beam.flash, beam.end, flashSize * (0.6f + 0.5f * grow), flashSize * (0.6f + 0.5f * grow), A(Color.Lerp(Color.white, p.prismGold, 0.35f), impact * outFade * outFade));
                    Place(beam.tint, beam.end, cell * (1.2f + 0.5f * grow), cell * (1.2f + 0.5f * grow), A(Color.Lerp(beam.color, p.prismPink, 0.3f), 0.55f * impact * Mathf.Pow(outFade, 1.5f)));
                    var shock = s.targetRingSize * cell * vary * (0.5f + 0.5f * EaseOut(Mathf.Clamp01(e / 0.6f)));
                    var shockK = haloMask ? HaloRing : BombRing;
                    Place(beam.node, beam.end, shock / shockK, shock / shockK, A(X(p.prismMagenta, warm), s.targetRingAlpha * impact * outFade));
                    Place(beam.dots, beam.end, shock * 1.28f / BombRing, shock * 1.28f / BombRing, A(p.prismPink, impact * outFade), t * 60f);
                    var cross = cell * 0.9f * vary * (0.6f + 0.4f * grow);
                    Place(beam.star, beam.end, cross, cross, A(Color.Lerp(Color.white, p.prismGold, 0.3f), 0.8f * impact * outFade * outFade), beam.seed * 90f);
                    var pop = s.targetStarSize * 0.6f * cell * vary * (0.5f + 0.5f * grow);
                    Place(beam.pop, beam.end, pop, pop, A(Color.Lerp(p.prismGold, p.prismPink, beam.seed), 0.8f * impact * Mathf.Pow(outFade, 1.4f)), beam.seed * 360f);
                    for (var j = 0; j < beam.sparkles.Length; j++)
                    {
                        var angle = (j + Hash(b * 13 + j, 92)) / beam.sparkles.Length * Mathf.PI * 2f + beam.seed * 6.28f;
                        var dist = cell * (0.25f + 0.65f * EaseOut(e)) * (0.7f + 0.6f * Hash(b * 13 + j, 93));
                        var env = Mathf.Sin(Mathf.PI * Mathf.Pow(e, 0.6f));
                        var size = cell * 0.3f * (0.5f + Hash(b * 13 + j, 94)) * (0.5f + 0.5f * env);
                        Place(beam.sparkles[j], beam.end + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0f), size, size,
                            A(j % 4 == 0 ? p.prismCyan : j % 4 == 1 ? p.prismPink : Color.Lerp(p.prismGold, Color.white, 0.5f), impact * env));
                    }
                    for (var j = 0; j < beam.twinkles.Length; j++)
                    {
                        // ---------- 6. FINALIZAÇÃO: emissão residual, cada brilho com seu próprio instante
                        var g = Mathf.Clamp01((finish - 0.25f * j - 0.2f * beam.seed) / 0.55f);
                        var env = g > 0f && g < 1f ? Mathf.Sin(Mathf.PI * g) : 0f;
                        var at = beam.end + new Vector3(Hash(b * 5 + j, 95) - 0.5f, Hash(b * 5 + j, 96) - 0.5f, 0f) * (cell * 0.6f);
                        var size = cell * 0.42f * (0.5f + 0.7f * Hash(b * 5 + j, 97)) * env;
                        Place(beam.twinkles[j], at, size, size, A(j == 0 ? Color.Lerp(p.prismGold, Color.white, 0.5f) : p.prismPink, s.finishAlpha * env));
                    }
                    for (var i = 0; i < beam.confetti.Length; i++) UpdateShard(beam.confetti[i], t, cell, s.confettiGravity);
                }

                // ---------- 4. EXPLOSÃO no globo: miolo branco pequeno e curto; raios dourados, rosa e violeta; anéis dourado,
                // magenta e ciano; faíscas radiais e descargas elétricas curtas
                if (detonated)
                {
                    var grow = EaseOut(Mathf.Clamp01(e / 0.45f));
                    var flash = s.ballFlashSize * cell;
                    Place(flashCore, center, flash * 0.45f * (0.7f + 0.5f * grow), flash * 0.45f * (0.7f + 0.5f * grow), A(Color.white, P.coreGlow * outFade * outFade));
                    Place(hot, center, flash * 0.9f * (0.7f + 0.5f * grow), flash * 0.9f * (0.7f + 0.5f * grow), A(p.prismGold, 0.6f * P.goldenGlow * Mathf.Pow(outFade, 1.5f)));
                    var rays = s.ballRaysSize * cell * (0.45f + 0.55f * EaseOut(Mathf.Clamp01(e / 0.35f)));
                    var raysFade = Mathf.Pow(outFade, 1.2f);
                    Place(burstGold, center, rays * 0.5f, rays * 0.5f, A(p.prismGold, 0.75f * raysFade));
                    Place(burstPink, center, rays * 0.9f, rays * 0.9f, A(p.prismPink, s.ballStreakAlpha * raysFade), 22.5f);
                    Place(burstViolet, center, rays, rays, A(p.prismViolet, 0.75f * raysFade), 11f);
                    var ring = s.ballRingSize * cell * (0.3f + 0.7f * EaseOut(Mathf.Clamp01(e / 0.7f))) / ringK;
                    Place(ringGold, center, ring * 0.8f, ring * 0.8f, A(Color.Lerp(p.prismGold, Color.white, 0.3f), outFade));
                    Place(ringMagenta, center, ring, ring, A(p.prismMagenta, 0.9f * outFade));
                    ringGold.color = X(ringGold.color, warm); ringMagenta.color = X(ringMagenta.color, warm);
                    var slow = s.ballRingSize * 1.2f * cell * (0.25f + 0.75f * EaseOut(Mathf.Clamp01(e / 0.9f))) / ringK;
                    Place(ringCyan, center, slow, slow, A(p.prismCyan, 0.6f * outFade));
                    for (var j = 0; j < radial.Length; j++)
                    {
                        var angle = (j + Hash(j, 101)) / radial.Length * Mathf.PI * 2f;
                        var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                        var dist = cell * (0.4f + (2.2f + 2f * Hash(j, 102)) * EaseOut(e));
                        PlaceStreak(radial[j], center + direction * dist, direction, cell * (0.5f - 0.25f * e), cell * 0.1f,
                            A(j % 3 == 0 ? p.prismCyan : j % 3 == 1 ? p.prismGold : p.prismPink, Mathf.Pow(outFade, 1.3f)));
                    }
                    var electric = 1f - Smooth(since / 0.24f);
                    for (var j = 0; j < 6; j++)
                    {
                        var angle = (j + 0.5f * Hash(j, 103)) / 6f * Mathf.PI * 2f;
                        var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                        bolts.Arc(center + direction * (0.3f * cell), center + direction * ((1.5f + 0.9f * Hash(j, 104)) * cell * EaseOut(since / 0.16f)), t, 400 + j, electric, cell);
                    }
                    for (var i = 0; i < confetti.Length; i++) UpdateShard(confetti[i], t, cell, s.confettiGravity);
                }
                else
                {
                    flashCore.color = off; hot.color = off; burstGold.color = off; burstPink.color = off; burstViolet.color = off;
                    ringGold.color = off; ringMagenta.color = off; ringCyan.color = off;
                    for (var j = 0; j < radial.Length; j++) radial[j].color = off;
                }
                bolts.End();
                if (strikes != null) strikes.End();
            }
        }

        // ------------------------------------------------------------------------------------------ DOIS LIGHT BALLS

        /// Escurecimento → carga → órbita → vórtice → colapso → explosão → ondas de destruição → dissipação.
        /// Os globos são os sprites do próprio jogo (o jogo os move com DoubleBallPlan.Pose); aqui ficam as luzes.
        /// Metade dos braços do vórtice é desenhada atrás dos globos e metade na frente, para dar profundidade.
        sealed class DoubleLightBallEffect : Effect
        {
            public DoubleBallPlan plan; public Transform ballA, ballB; public IList<Vector3> cells; public IList<Color> colors;
            DoubleBallSettings s; PrismSettings P; LightningSettings L;
            float cell, ringThinK, ringThickK, sparkleRingK;
            Vector3 center;
            SpriteRenderer dim, coreGold, haloMagenta, auraViolet, vortexLarge, spiral, vortexSmall, flash, burstGold, burstPink, burstViolet, burstSoft;
            readonly SpriteRenderer[] ballGlow = new SpriteRenderer[2], ballOrbit = new SpriteRenderer[2], ballPulse = new SpriteRenderer[2];
            readonly Ribbon[] trail = new Ribbon[2], trailGlow = new Ribbon[2];
            // Anel neon de cada globo: miolo fino + halo largo, e dois cometas correndo por ele (um globo gira para cada lado)
            readonly SpriteRenderer[] ringCore = new SpriteRenderer[2], ringHalo = new SpriteRenderer[2];
            readonly SpriteRenderer[] rimTight = new SpriteRenderer[2], rimWide = new SpriteRenderer[2], ballSource = new SpriteRenderer[2];
            readonly Ribbon[] ringArc = new Ribbon[4], ringArcGlow = new Ribbon[4];
            readonly List<Vector4>[] history = { new List<Vector4>(), new List<Vector4>() };
            readonly Vector3[] last = new Vector3[2];
            readonly Vector3[] points = new Vector3[RibbonSegments + 1];
            Ribbon[] arms, filaments;
            SpriteRenderer[] dust, orbiters, sparks, glints, radial, waveCore, waveBand, waveHalo, waveSparks, cellFlash, cellStar, cellRing, cellSparkles, linger;
            SpriteRenderer[][] waveDots;
            int sparklesPerCell;
            float[] cellHit;
            Shard[] shards;
            BoltSet bolts;
            Camera shakeCamera; Vector3 cameraBase; bool shaking, shakeDone;

            bool Has(int index) { var m = player.doubleMasks; return m != null && index < m.Length && m[index] != null; }
            Sprite Mask(int index, Sprite fallback) => Has(index) ? player.doubleMasks[index] : fallback;

            public override void Begin()
            {
                s = player.doubleBall; P = player.prism; L = player.lightning;
                cell = plan.cell; center = plan.center;
                duration = plan.Total;
                ringThinK = Has(2) ? 0.66f : BombRing; ringThickK = Has(3) ? 0.66f : BombRing; sparkleRingK = Has(13) ? 0.63f : BombRing;
                dim = Add(Solid, -4, false);                                      // os globos do jogo ficam em -1; -3 e -2 são o "atrás dos globos"
                auraViolet = Add(HollowGlow, -3);
                if (Has(9)) vortexLarge = Add(player.doubleMasks[9], -3);
                if (Has(7)) spiral = Add(player.doubleMasks[7], -2);
                arms = new Ribbon[P.ribbonCount]; filaments = new Ribbon[P.ribbonCount];
                for (var i = 0; i < arms.Length; i++)
                {
                    arms[i] = player.MakeRibbon("Braço do vórtice", i % 2 == 0 ? 1 : -2, objects);
                    filaments[i] = player.MakeRibbon("Filamento dourado", i % 2 == 0 ? 2 : -2, objects);
                }
                haloMagenta = Add(HollowGlow, 0);
                if (Has(8)) vortexSmall = Add(player.doubleMasks[8], 2);
                for (var i = 0; i < 2; i++)
                {
                    ballGlow[i] = Add(HollowGlow, 0);
                    trailGlow[i] = player.MakeRibbon("Rastro orbital (brilho)", 1, objects);
                    trail[i] = player.MakeRibbon("Rastro orbital", 2, objects);
                    ballSource[i] = (i == 0 ? ballA : ballB).GetComponent<SpriteRenderer>();
                    rimWide[i] = Add(RimHaloWide, 0);
                    rimTight[i] = Add(RimHalo, 1);
                    ringHalo[i] = Add(BombRingMagenta, 3);
                    ringCore[i] = Add(BombRingLine, 4);
                    for (var j = 0; j < 2; j++)
                    {
                        ringArcGlow[i * 2 + j] = player.MakeRibbon("Anel neon (halo)", 3, objects);
                        ringArc[i * 2 + j] = player.MakeRibbon("Anel neon", 4, objects);
                    }
                    ballPulse[i] = Add(Mask(2, BombRingLine), 4);
                    last[i] = (i == 0 ? ballA : ballB).position;
                }
                dust = new SpriteRenderer[player.Budget(56f, P.dustDensity)];
                for (var i = 0; i < dust.Length; i++) dust[i] = Add(GlowDot, 3);
                orbiters = new SpriteRenderer[player.Budget(P.orbitalParticles * 2f, 1f)];
                for (var i = 0; i < orbiters.Length; i++) orbiters[i] = Add(GlowDot, 5);
                sparks = new SpriteRenderer[player.Budget(18f, P.sparkDensity)];
                for (var i = 0; i < sparks.Length; i++) sparks[i] = Add(GlowDot, 5);
                glints = new SpriteRenderer[player.Budget(12f, 1f)];
                for (var i = 0; i < glints.Length; i++) glints[i] = Add(CrossStar, 6);
                coreGold = Add(Mask(0, GlowDot), 5);

                var waves = Mathf.Max(1, s.waveCount);
                waveCore = new SpriteRenderer[waves]; waveBand = new SpriteRenderer[waves]; waveHalo = new SpriteRenderer[waves]; waveSparks = new SpriteRenderer[waves];
                waveDots = new SpriteRenderer[waves][];
                for (var i = 0; i < waves; i++)
                {
                    waveHalo[i] = Add(Mask(3, BombRingMagenta), 6);
                    waveBand[i] = Add(Mask(3, BombRingMagenta), 6);
                    waveCore[i] = Add(Mask(2, BombRingLine), 7);
                    waveSparks[i] = Add(Mask(13, DottedRing), 7);
                    waveDots[i] = new SpriteRenderer[player.Budget(P.shockParticles * (i % 3 == 1 ? 1.5f : 1f), 1f)];
                    for (var j = 0; j < waveDots[i].Length; j++) waveDots[i][j] = Add(i % 3 == 2 && j % 2 == 0 ? CrossStar : GlowDot, 8);
                }
                burstViolet = Add(Mask(10, SeekerRaysLine), 8);
                burstPink = Add(Mask(10, SeekerRaysLine), 8);
                burstGold = Add(Mask(10, SeekerRaysLine), 9);
                burstSoft = Add(Mask(11, SeekerRaysGlow), 9);
                radial = new SpriteRenderer[player.Budget(24f, P.sparkDensity)];
                for (var i = 0; i < radial.Length; i++) radial[i] = Add(GlowDot, 9);
                bolts = new BoltSet(player, objects, 10, 36);
                flash = Add(Mask(1, BombCoreWhite), 12);

                var count = cells.Count;
                cellHit = new float[count]; cellFlash = new SpriteRenderer[count]; cellStar = new SpriteRenderer[count]; cellRing = new SpriteRenderer[count];
                sparklesPerCell = count > 50 ? 1 : 3;
                cellSparkles = new SpriteRenderer[count * sparklesPerCell];
                var perCell = count > 50 ? Mathf.Min(1, s.fragmentsPerCell) : s.fragmentsPerCell;   // tabuleiro cheio: menos fragmentos por peça
                shards = new Shard[count * perCell];
                for (var c = 0; c < count; c++)
                {
                    cellHit[c] = plan.HitTime(cells[c]);
                    cellRing[c] = Add(BombRingMagenta, 8);
                    cellFlash[c] = Add(GlowDot, 8);
                    cellStar[c] = Add(CrossStar, 9);
                    for (var k = 0; k < sparklesPerCell; k++) cellSparkles[c * sparklesPerCell + k] = Add(CrossStar, 9);
                    var away = cells[c] - center; away.z = 0f;
                    away = away.sqrMagnitude > 1e-6f ? away.normalized : Vector3.up;
                    for (var k = 0; k < perCell; k++)
                    {
                        var id = c * 7 + k;
                        var angle = Hash(id, 211) * Mathf.PI * 2f;
                        var direction = new Vector2(Mathf.Cos(angle) + away.x * 0.8f, Mathf.Sin(angle) + away.y * 0.8f).normalized;
                        shards[c * perCell + k] = new Shard
                        {
                            sr = Add(player.Fragment(id), 11, false), origin = cells[c],
                            velocity = direction * (s.fragmentSpeed * s.speed * (0.5f + 0.6f * Hash(id, 212))),
                            spin = (Hash(id, 213) - 0.5f) * 720f, size = s.fragmentSize * (0.6f + 0.8f * Hash(id, 214)),
                            life = s.fragmentLife / s.speed * (0.8f + 0.4f * Hash(id, 215)), delay = plan.ExplodeAt + cellHit[c] + 0.02f,
                            color = Hash(id, 216) < 0.2f ? player.Prism(id, Hash(id, 217)) : Color.Lerp(colors[c], Color.white, 0.12f),
                        };
                    }
                }
                linger = new SpriteRenderer[s.lingerSparkles];
                for (var i = 0; i < linger.Length; i++) linger[i] = Add(CrossStar, 9);
            }

            protected override void OnRelease()
            {
                if (shaking && shakeCamera != null) shakeCamera.transform.position = cameraBase;
                shaking = false;
            }

            Vector3 Sample(int ball, float time)
            {
                var h = history[ball];
                for (var i = h.Count - 1; i >= 0; i--)
                {
                    if (h[i].w > time) continue;
                    if (i == h.Count - 1) return h[i];
                    var a = h[i]; var b = h[i + 1];
                    return Vector3.Lerp(a, b, Mathf.Clamp01((time - a.w) / Mathf.Max(1e-5f, b.w - a.w)));
                }
                return h.Count > 0 ? (Vector3)h[0] : last[ball];
            }

            /// Ponto do braço "arm" do vórtice na fração q (0 = centro, 1 = ponta), no ângulo de giro atual.
            Vector3 ArmPoint(int arm, float q, float rotation, float radius, float twist)
            {
                var angle = arm / (float)arms.Length * Mathf.PI * 2f + rotation - q * twist;
                return center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (radius * (0.1f + 0.9f * q));
            }

            public override void Evaluate(float t)
            {
                var p = player;
                var T = t * s.speed;
                var E = T - s.ExplodeT;
                var exploded = E >= 0f;
                var I = s.intensity;
                var totalT = s.TotalT;
                var orbit = Mathf.Clamp01((T - s.chargeSeconds) / s.orbitSeconds);
                var charge = Smooth(T / s.chargeSeconds);
                var pre = exploded ? 1f - Smooth(E / 0.12f) : 1f;              // o que é "antes da explosão" some rápido depois dela
                var linger1 = exploded ? 1f - Smooth(E / 0.3f) : 1f;           // as fitas ficam um pouco mais, abrindo com a explosão
                var rotation = Mathf.PI * 2f * s.turns * s.orbitAngle.Evaluate(orbit) * s.vortexSpin;
                var rotationDeg = rotation * Mathf.Rad2Deg;
                var off = A(Color.white, 0f);
                var hdr = p.Hdr;
                var warm = 1f + 0.5f * (hdr - 1f);

                // ESCURECIMENTO
                PlaceDim(dim, p.ViewCam, center.z, s.darkness * Smooth(T / s.dimSeconds) * (1f - Smooth((T - (totalT - s.restoreSeconds)) / s.restoreSeconds)));

                bolts.Begin();

                // ---------- CARGA e ÓRBITA: cada globo com aura colorida (vazia no meio), anel orbital, pulso, rastro curvo e descargas
                var moving = Smooth(orbit / 0.1f);
                for (var i = 0; i < 2; i++)
                {
                    var follow = i == 0 ? ballA : ballB;
                    if (orbit < 1f && follow != null && follow.gameObject.activeInHierarchy)
                    {
                        last[i] = follow.position;
                        history[i].Add(new Vector4(last[i].x, last[i].y, last[i].z, T));
                        while (history[i].Count > 2 && history[i][1].w < T - s.trailSeconds - 0.1f) history[i].RemoveAt(0);
                    }
                    var pos = orbit < 1f ? last[i] : center;
                    var beat = 0.5f + 0.5f * Mathf.Sin(T * s.pulseFrequency + i * Mathf.PI);
                    Place(ballGlow[i], pos, cell * (2.2f + 0.25f * beat), cell * (2.2f + 0.25f * beat), A(i == 0 ? p.prismViolet : p.prismGold, s.chargeGlow * I * charge * pre));
                    // Anel neon: miolo quase branco (forte, vaza luz com o bloom), halo colorido largo, e dois cometas dando voltas.
                    // O primeiro globo gira no sentido anti-horário e o segundo no horário; a volta acelera com a órbita.
                    var neon = i == 0 ? p.prismCyan : p.prismGold;
                    var haloTint = i == 0 ? p.prismViolet : p.prismPink;
                    var ringRadius = cell * 0.68f * (1f + 0.04f * beat);
                    var ringAlpha = I * charge * pre;
                    Place(ringHalo[i], pos, 2f * ringRadius / BombRing, 2f * ringRadius / BombRing, A(X(haloTint, warm), 0.75f * ringAlpha));
                    Place(ringCore[i], pos, 2f * ringRadius / BombRing, 2f * ringRadius / BombRing, A(X(Color.Lerp(neon, Color.white, 0.55f), hdr), 0.95f * ringAlpha));
                    var spinDirection = i == 0 ? 1f : -1f;
                    for (var j = 0; j < 2; j++)
                    {
                        var head = spinDirection * (T * (1.3f + 1.5f * orbit) * Mathf.PI * 2f) + j * Mathf.PI + i * 0.9f;
                        OrbitRibbon(ringArcGlow[i * 2 + j], pos, ringRadius, ringRadius, 0f, head, 150f * Mathf.Deg2Rad, spinDirection, cell * 0.26f,
                            X(neon, warm), haloTint, 0.6f * ringAlpha);
                        OrbitRibbon(ringArc[i * 2 + j], pos, ringRadius, ringRadius, 0f, head, 150f * Mathf.Deg2Rad, spinDirection, cell * 0.09f,
                            X(Color.white, hdr), X(neon, warm), ringAlpha);
                    }
                    // Contorno luminoso do globo (como o da flecha): luz forte colada na borda e halo colorido; mais forte quando acende
                    var rimRadius = orbit < 1f ? RoundRadius(ballSource[i], cell * 0.43f) : cell * 0.2f;
                    var rimKick = 1f + 1.4f * (1f - Smooth(T / 0.8f));
                    Place(rimTight[i], pos, rimRadius * 4f, rimRadius * 4f, A(X(Color.Lerp(neon, Color.white, 0.45f), hdr), 0.9f * ringAlpha * rimKick * (0.85f + 0.15f * beat)));
                    Place(rimWide[i], pos, rimRadius * 4f, rimRadius * 4f, A(X(haloTint, warm), 0.85f * ringAlpha * rimKick));
                    var pf = (T - 0.2f - 0.1f * i) / 0.4f;
                    var pulseSize = cell * (0.8f + 1.6f * EaseOut(pf)) / ringThinK;
                    Place(ballPulse[i], pos, pulseSize, pulseSize, A(i == 0 ? p.prismCyan : p.prismGold, pf > 0f && pf < 1f ? 0.8f * I * (1f - pf) : 0f));
                    if (!exploded)
                        for (var j = 0; j < 2; j++)
                        {
                            var a0 = j * Mathf.PI + T * 1.3f + i;
                            var a1 = a0 + 1.2f + 0.5f * Mathf.Sin(T * 3f + j);
                            bolts.Arc(pos + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * (0.48f * cell), pos + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * (0.6f * cell),
                                T, 500 + i * 10 + j, 0.8f * I * charge, cell, 0.2f);
                        }

                    for (var k = 0; k <= RibbonSegments; k++) points[k] = Sample(i, T - k / (float)RibbonSegments * s.trailSeconds);
                    for (var k = 0; k <= RibbonSegments; k++)
                    {
                        var q = k / (float)RibbonSegments;
                        var tangent = points[Mathf.Max(0, k - 1)] - points[Mathf.Min(RibbonSegments, k + 1)]; tangent.z = 0f;
                        tangent = tangent.sqrMagnitude > 1e-10f ? tangent.normalized : Vector3.up;
                        var normal = new Vector3(-tangent.y, tangent.x, 0f);
                        var width = s.trailWidth * cell * Mathf.Pow(1f - q, 0.8f) * Smooth(q / 0.04f + 0.3f);
                        trail[i].v[2 * k] = points[k] - normal * (width * 0.5f); trail[i].v[2 * k + 1] = points[k] + normal * (width * 0.5f);
                        trailGlow[i].v[2 * k] = points[k] - normal * (width * 1.5f); trailGlow[i].v[2 * k + 1] = points[k] + normal * (width * 1.5f);
                        var col = i == 0 ? Color.Lerp(p.prismPink, p.prismViolet, Mathf.Clamp01(q * 1.6f)) : Color.Lerp(p.prismGold, new Color(1f, 0.5f, 0.15f), Mathf.Clamp01(q * 1.6f));
                        col = Color.Lerp(Color.white, col, Mathf.Clamp01(q / 0.08f + 0.35f));
                        col.a = Mathf.Clamp01(s.trailAlpha * I * moving * pre * Mathf.Pow(1f - q, 0.6f));
                        trail[i].c[2 * k] = col; trail[i].c[2 * k + 1] = col;
                        var glowColor = i == 0 ? p.prismViolet : p.prismGold;
                        glowColor.a = Mathf.Clamp01(0.45f * s.trailAlpha * I * moving * pre * Mathf.Pow(1f - q, 0.7f));
                        trailGlow[i].c[2 * k] = glowColor; trailGlow[i].c[2 * k + 1] = glowColor;
                    }
                    trail[i].mesh.vertices = trail[i].v; trail[i].mesh.colors = trail[i].c;
                    trailGlow[i].mesh.vertices = trailGlow[i].v; trailGlow[i].mesh.colors = trailGlow[i].c;
                }
                // arcos elétricos ligando os dois globos enquanto giram
                if (!exploded && orbit > 0f && orbit < 1f)
                    for (var j = 0; j < L.arcCount; j++)
                        bolts.Arc(last[0], last[1], T, 520 + j, I * moving, cell, (j % 2 == 0 ? 1f : -1f) * (0.12f + 0.1f * j));

                // ---------- VÓRTICE: braços espirais (violeta/magenta/rosa) com filamentos dourados, sobre as máscaras macias.
                // Começa pequeno, cresce com a órbita, acelera e se contrai antes da explosão; na explosão as fitas abrem e somem.
                var vort = Smooth(orbit / 0.3f);
                var compress = 1f - P.vortexCompression * Smooth((orbit - 0.78f) / 0.22f);
                var blast = exploded ? 1f + 1.6f * EaseOut(Mathf.Clamp01(E / 0.3f)) : 1f;
                var radius = s.vortexScale * 0.5f * cell * (0.35f + 0.65f * Smooth(orbit / 0.55f)) * compress * blast;
                var twist = 2.2f + 2.4f * orbit;                                 // os braços enrolam mais conforme acelera
                var corePulse = 0.5f + 0.5f * Mathf.Sin(T * (8f + 22f * orbit));
                var body = s.vortexAlpha * I * vort * pre;
                var vortexSize = radius * 2f;
                Place(vortexLarge, center, vortexSize * 1.05f, vortexSize * 1.05f, A(p.prismViolet, 0.5f * body), rotationDeg * 0.6f);
                Place(spiral, center, vortexSize * 0.85f, vortexSize * 0.85f, A(p.prismMagenta, 0.5f * body), rotationDeg);
                Place(vortexSmall, center, vortexSize * 0.5f, vortexSize * 0.5f, A(p.prismGold, 0.55f * body), rotationDeg * 1.5f);
                for (var i = 0; i < arms.Length; i++)
                {
                    var arm = arms[i]; var filament = filaments[i];
                    var armAlpha = s.vortexAlpha * I * vort * linger1;
                    for (var k = 0; k <= RibbonSegments; k++)
                    {
                        var q = k / (float)RibbonSegments;
                        var point = ArmPoint(i, q, rotation, radius, twist);
                        var next = ArmPoint(i, Mathf.Min(1f, q + 0.02f), rotation, radius, twist);
                        var prev = ArmPoint(i, Mathf.Max(0f, q - 0.02f), rotation, radius, twist);
                        var tangent = next - prev; tangent.z = 0f;
                        tangent = tangent.sqrMagnitude > 1e-12f ? tangent.normalized : Vector3.right;
                        var normal = new Vector3(-tangent.y, tangent.x, 0f);
                        var shape = Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Clamp01(q * 0.92f + 0.04f)), 0.7f);
                        var flow = 0.7f + 0.3f * Mathf.Sin(q * 9f - T * 14f + i);
                        var half = normal * (P.ribbonWidth * cell * shape * (0.55f + 0.45f * vort) * 0.5f);
                        arm.v[2 * k] = point - half; arm.v[2 * k + 1] = point + half;
                        var col = X(P.ribbonGradient.Evaluate(q), warm);
                        col.a = Mathf.Clamp01(armAlpha * shape * flow);
                        arm.c[2 * k] = col; arm.c[2 * k + 1] = col;
                        // filamento dourado fino, um pouco à frente do braço
                        var gold = ArmPoint(i, q, rotation + 0.32f, radius * 1.06f, twist);
                        half = normal * (0.07f * cell * shape * 0.5f);
                        filament.v[2 * k] = gold - half; filament.v[2 * k + 1] = gold + half;
                        col = X(Color.Lerp(Color.Lerp(p.prismGold, Color.white, 0.35f), p.prismGold, q), hdr);
                        col.a = Mathf.Clamp01(0.9f * armAlpha * shape * (0.6f + 0.4f * Mathf.Sin(q * 13f - T * 20f + i * 2f)));
                        filament.c[2 * k] = col; filament.c[2 * k + 1] = col;
                    }
                    arm.mesh.vertices = arm.v; arm.mesh.colors = arm.c;
                    filament.mesh.vertices = filament.v; filament.mesh.colors = filament.c;
                }
                // núcleo: aura violeta e halo magenta vazios no meio, miolo dourado que pulsa mais rápido conforme acelera
                var auraSize = radius * 2.3f;
                Place(auraViolet, center, auraSize, auraSize, A(p.prismViolet, P.violetGlow * I * vort * pre));
                Place(haloMagenta, center, radius * 1.2f, radius * 1.2f, A(p.prismMagenta, P.magentaGlow * I * vort * pre * (0.6f + 0.4f * corePulse)));
                var coreSize = cell * (0.9f + 0.9f * orbit * orbit) * (0.9f + 0.2f * corePulse);
                Place(coreGold, center, coreSize, coreSize, A(Color.Lerp(p.prismGold, Color.white, 0.5f * orbit), P.goldenGlow * I * vort * pre * (0.3f + 0.7f * orbit * orbit)));

                // poeira mágica: entra em espiral pela borda do vórtice até o centro
                for (var j = 0; j < dust.Length; j++)
                {
                    var phase = Mathf.Repeat(T * (0.9f + 0.6f * Hash(j, 204)) + Hash(j, 201), 1f);
                    var r = (1f - phase) * radius * 1.15f;
                    var angle = rotation * 0.5f + Hash(j, 202) * Mathf.PI * 2f + phase * 4.5f;
                    var size = cell * 0.1f * (0.5f + Hash(j, 203)) * (0.4f + 0.6f * (1f - phase));
                    Place(dust[j], center + new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f), size, size, A(p.Prism(j % 4, Hash(j, 205)), vort * pre * I * Mathf.Sin(Mathf.PI * phase)));
                }
                // partículas ciano e douradas orbitando em faixas com velocidades diferentes
                for (var j = 0; j < orbiters.Length; j++)
                {
                    var lane = j % 4;
                    var angle = rotation * (1.6f - 0.3f * lane) + Hash(j, 221) * Mathf.PI * 2f + T * (0.8f + 0.4f * lane);
                    var r = radius * (0.35f + 0.2f * lane);
                    var size = cell * 0.12f * (0.6f + 0.7f * Hash(j, 222));
                    Place(orbiters[j], center + new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f), size, size,
                        A(j % 3 == 0 ? p.prismGold : p.prismCyan, vort * pre * I * (0.6f + 0.4f * Mathf.Sin(T * 15f + j))));
                }
                // faíscas de energia: riscos curtos que aceleram pelo giro em direção ao centro
                for (var j = 0; j < sparks.Length; j++)
                {
                    var phase = Mathf.Repeat(T * (1.3f + Hash(j, 231)) + Hash(j, 232), 1f);
                    var eased = phase * phase;
                    var r = (1f - eased) * radius * 1.05f;
                    var angle = rotation * 0.8f + Hash(j, 233) * Mathf.PI * 2f + eased * 5f;
                    var direction = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0f);
                    PlaceStreak(sparks[j], center + new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f), direction, cell * (0.25f + 0.35f * eased), cell * 0.09f,
                        A(j % 3 == 0 ? p.prismCyan : j % 3 == 1 ? Color.white : p.prismPink, vort * pre * I * Mathf.Sin(Mathf.PI * phase)));
                }
                // brilhos de 4 pontas, cada um no seu instante
                for (var j = 0; j < glints.Length; j++)
                {
                    var clock = T * (0.9f + 0.7f * Hash(j, 241)) + Hash(j, 242);
                    var cycle = Mathf.FloorToInt(clock); var phase = clock - cycle;
                    if (phase > 0.4f || exploded) { glints[j].color = off; continue; }
                    var env = Mathf.Sin(Mathf.PI * phase / 0.4f);
                    var angle = Hash(j, cycle * 3 + 1) * Mathf.PI * 2f;
                    var r = cell * (0.6f + 1.6f * Hash(j, cycle * 3 + 2));
                    var size = cell * 0.4f * (0.45f + 0.75f * Hash(j, 243)) * env;
                    Place(glints[j], center + new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f), size, size,
                        A(j % 3 == 0 ? p.prismCyan : Color.Lerp(p.prismGold, Color.white, 0.5f), charge * env));
                }
                // eletricidade no vórtice: filamentos ao longo dos braços (mais frequentes conforme acelera) e, no colapso, puxados para o núcleo
                if (!exploded && vort > 0.05f)
                {
                    var electricTime = T * (1f + 1.5f * orbit);
                    var count = Mathf.RoundToInt(4f * L.vortexDensity);
                    for (var j = 0; j < count; j++)
                    {
                        var arm = j % Mathf.Max(1, arms.Length);
                        bolts.Arc(ArmPoint(arm, 0.22f, rotation + 0.15f, radius, twist), ArmPoint(arm, 0.75f, rotation + 0.15f, radius, twist), electricTime, 540 + j, I * vort * (0.4f + 0.6f * orbit), cell, 0.12f);
                    }
                    var pull = Smooth((orbit - 0.75f) / 0.2f);
                    if (pull > 0f)
                        for (var j = 0; j < count; j++)
                        {
                            var angle = rotation + j / (float)Mathf.Max(1, count) * Mathf.PI * 2f;
                            bolts.Arc(center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (radius * 1.1f), center, electricTime, 560 + j, I * pull, cell, 0.18f);
                        }
                }

                // ---------- EXPLOSÃO: miolo branco curto e pequeno; raios dourados, rosa e violeta; faíscas e arcos radiais
                if (exploded)
                {
                    var x = Mathf.Clamp01(E / s.flashSeconds); var grow = EaseOut(x); var fade = 1f - x;
                    var size = s.explosionScale * cell;
                    Place(flash, center, size * 0.3f * (0.5f + 0.5f * grow), size * 0.3f * (0.5f + 0.5f * grow), A(Color.Lerp(Color.white, p.prismGold, 0.15f), P.coreGlow * s.flashIntensity * I * fade * fade));
                    Place(burstSoft, center, size * 0.5f * (0.5f + 0.5f * grow), size * 0.5f * (0.5f + 0.5f * grow), A(p.prismGold, 0.8f * P.goldenGlow * I * Mathf.Pow(fade, 1.5f)));
                    var rays = Mathf.Pow(fade, 1.2f) * I;
                    Place(burstGold, center, size * 0.8f * (0.4f + 0.6f * grow), size * 0.8f * (0.4f + 0.6f * grow), A(p.prismGold, rays));
                    Place(burstPink, center, size * 0.95f * (0.4f + 0.6f * grow), size * 0.95f * (0.4f + 0.6f * grow), A(p.prismPink, 0.8f * rays), 22f);
                    Place(burstViolet, center, size * 1.1f * (0.4f + 0.6f * grow), size * 1.1f * (0.4f + 0.6f * grow), A(p.prismViolet, 0.75f * rays), 45f);
                    for (var j = 0; j < radial.Length; j++)
                    {
                        var angle = (j + Hash(j, 251)) / radial.Length * Mathf.PI * 2f;
                        var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                        var dist = cell * (0.5f + (3f + 3f * Hash(j, 252)) * EaseOut(Mathf.Clamp01(E / 0.5f)));
                        PlaceStreak(radial[j], center + direction * dist, direction, cell * 0.6f * Mathf.Max(0.2f, 1f - E / 0.5f), cell * 0.11f,
                            A(j % 3 == 0 ? p.prismCyan : j % 3 == 1 ? p.prismGold : p.prismPink, I * (1f - Smooth(E / 0.5f))));
                    }
                    var electric = 1f - Smooth(E / 0.22f);
                    for (var j = 0; j < 8; j++)
                    {
                        var angle = (j + 0.5f * Hash(j, 253)) / 8f * Mathf.PI * 2f;
                        var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                        bolts.Arc(center + direction * (0.4f * cell), center + direction * ((2f + 1.4f * Hash(j, 254)) * cell * EaseOut(E / 0.14f)), T, 580 + j, I * electric, cell);
                    }

                    if (!p.manual && s.shakeAmount > 0f && !shakeDone)
                    {
                        if (!shaking)
                        {
                            shakeCamera = Camera.main;
                            if (shakeCamera != null) { cameraBase = shakeCamera.transform.position; shaking = true; } else shakeDone = true;
                        }
                        if (shaking && shakeCamera != null)
                        {
                            var k = 1f - Mathf.Clamp01(E / s.shakeSeconds);
                            shakeCamera.transform.position = cameraBase + new Vector3(Mathf.Sin(T * 83f), Mathf.Cos(T * 71f), 0f) * (s.shakeAmount * k * k);
                            if (k <= 0f) { shakeCamera.transform.position = cameraBase; shaking = false; shakeDone = true; }
                        }
                    }
                }

                // ---------- ONDAS: cada uma é um conjunto de camadas (miolo fino, faixa de energia, halo translúcido, faíscas na
                // borda, partículas acompanhando a frente e descargas elétricas). O centro fica vazio.
                //   1ª impacto dourado: miolo branco-dourado, faixa dourada, halo rosa
                //   2ª energia violeta: miolo magenta claro, faixa magenta, halo violeta, mais partículas girando na borda
                //   3ª dissipação prismática: miolo ciano, faixa violeta, halo dourado, brilhos de 4 pontas
                for (var i = 0; i < waveCore.Length; i++)
                {
                    var tau = (E - i * s.waveDelay) / s.waveSeconds;
                    if (!exploded || tau <= 0f || tau >= 1f)
                    {
                        waveCore[i].color = off; waveBand[i].color = off; waveHalo[i].color = off; waveSparks[i].color = off;
                        for (var j = 0; j < waveDots[i].Length; j++) waveDots[i][j].color = off;
                        continue;
                    }
                    var front = plan.maxRadius * (1f - Mathf.Pow(1f - tau, 1.5f));
                    var appear = Smooth(tau / 0.06f) * P.shockBrightness * s.waveOpacity * I;
                    var coreFade = appear * (1f - Smooth((tau - 0.6f) / 0.4f));           // o miolo some primeiro
                    var bandFade = appear * (1f - Smooth((tau - 0.68f) / 0.32f));
                    var haloFade = appear * (1f - Smooth((tau - 0.78f) / 0.22f));         // o halo fica por último
                    var kind = i % 3;
                    var coreColor = kind == 0 ? Color.Lerp(Color.white, p.prismGold, 0.6f) : kind == 1 ? Color.Lerp(p.prismMagenta, Color.white, 0.4f) : Color.Lerp(p.prismCyan, Color.white, 0.25f);
                    var bandColor = kind == 0 ? p.prismGold : kind == 1 ? p.prismMagenta : p.prismViolet;
                    var haloColor = kind == 0 ? p.prismPink : kind == 1 ? p.prismViolet : p.prismGold;
                    var thick = P.shockThickness;
                    Place(waveCore[i], center, 2f * front / ringThinK, 2f * front / ringThinK, A(X(coreColor, hdr), 0.7f * coreFade * Mathf.Lerp(1f, 0.6f, tau)));
                    var band = 2f * front * (1f - 0.035f * thick) / ringThickK;
                    Place(waveBand[i], center, band, band, A(bandColor, 0.32f * bandFade * Mathf.Lerp(1f, 0.6f, tau)));
                    var halo = 2f * front * (1f + 0.07f * thick) / ringThickK;
                    Place(waveHalo[i], center, halo, halo, A(haloColor, 0.24f * haloFade));
                    var sparkSize = 2f * front * 1.03f / sparkleRingK;
                    Place(waveSparks[i], center, sparkSize, sparkSize, A(coreColor, 0.45f * bandFade), T * 30f * (i % 2 == 0 ? 1f : -1f));
                    var dots = waveDots[i];
                    for (var j = 0; j < dots.Length; j++)
                    {
                        // partículas que saem com a frente da onda; na 2ª onda elas também correm pela circunferência
                        var angle = (j + Hash(i * 97 + j, 261)) / dots.Length * Mathf.PI * 2f + (kind == 1 ? T * 1.6f * (j % 2 == 0 ? 1f : -1f) : 0f);
                        var r = front * (1f - 0.1f * Hash(i * 97 + j, 262) * tau);
                        var twinkle = 0.55f + 0.45f * Mathf.Sin(T * 16f + j * 1.7f);
                        var size = cell * (dots[j].sprite == CrossStar ? 0.34f : 0.12f) * (0.6f + 0.8f * Hash(i * 97 + j, 263));
                        Place(dots[j], center + new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f), size, size,
                            A(kind == 0 ? Color.Lerp(p.prismGold, p.prismPink, Hash(j, 264)) : kind == 1 ? Color.Lerp(p.prismMagenta, p.prismViolet, Hash(j, 264)) : p.Prism(j % 5, Hash(j, 264)), bandFade * twinkle));
                    }
                    // pequenas descargas correndo por trechos da circunferência
                    var arcs = Mathf.RoundToInt(4f * L.shockwaveDensity);
                    var slot = Mathf.FloorToInt(T * 7f);
                    for (var j = 0; j < arcs; j++)
                    {
                        var a0 = Hash(i * 31 + j, slot) * Mathf.PI * 2f;
                        var a1 = a0 + 0.32f + 0.2f * Hash(i * 31 + j, slot + 5);
                        bolts.Arc(center + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * front, center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * front,
                            T, 600 + i * 10 + j, bandFade, cell, -0.06f, false);
                    }
                }

                // ---------- CADA PEÇA: anel magenta pequeno, clarão branco-dourado, brilhos e fragmentos na cor da peça, quando a
                // onda da sua faixa chega. Tamanho e rotação variam de peça para peça.
                var sinceExplosion = t - plan.ExplodeAt;
                var impact = P.targetImpact * I;
                for (var c = 0; c < cellHit.Length; c++)
                {
                    var a = (sinceExplosion - cellHit[c]) * s.speed / s.cellFlashSeconds;
                    if (a < 0f || a > 1f)
                    {
                        cellFlash[c].color = off; cellStar[c].color = off; cellRing[c].color = off;
                        for (var k = 0; k < sparklesPerCell; k++) cellSparkles[c * sparklesPerCell + k].color = off;
                        continue;
                    }
                    var vary = 0.8f + 0.4f * Hash(c, 271);
                    var size = cell * s.cellFlashSize * 0.75f * vary * (0.6f + 0.6f * a);
                    Place(cellFlash[c], cells[c], size, size, A(Color.Lerp(Color.Lerp(Color.white, p.prismGold, 0.4f), colors[c], a), impact * Mathf.Pow(1f - a, 1.5f)));
                    var ring = cell * 1.25f * vary * (0.45f + 0.55f * EaseOut(a)) / BombRing;
                    Place(cellRing[c], cells[c], ring, ring, A(X(p.prismMagenta, warm), 0.9f * impact * (1f - a)));
                    var star = cell * 0.95f * vary * (0.5f + 0.5f * EaseOut(a));
                    Place(cellStar[c], cells[c], star, star, A(Color.Lerp(Color.white, p.prismGold, 0.3f), impact * Mathf.Pow(1f - a, 1.6f)), Hash(c, 272) * 90f);
                    for (var k = 0; k < sparklesPerCell; k++)
                    {
                        var id = c * 3 + k;
                        var angle = (k + Hash(id, 273)) / sparklesPerCell * Mathf.PI * 2f + Hash(c, 274) * 6.28f;
                        var dist = cell * (0.2f + 0.6f * EaseOut(a)) * (0.7f + 0.6f * Hash(id, 275));
                        var env = Mathf.Sin(Mathf.PI * Mathf.Pow(a, 0.6f));
                        var sparkle = cell * 0.28f * (0.5f + Hash(id, 276)) * (0.5f + 0.5f * env);
                        Place(cellSparkles[c * sparklesPerCell + k], cells[c] + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0f), sparkle, sparkle,
                            A(k == 0 ? p.prismCyan : k == 1 ? p.prismPink : Color.Lerp(p.prismGold, Color.white, 0.5f), impact * env));
                    }
                }
                for (var i = 0; i < shards.Length; i++) UpdateShard(shards[i], t, cell, 3.5f);

                // ---------- DISSIPAÇÃO: brilhos piscando pelo tabuleiro, cada um no seu instante, enquanto a tela volta ao normal
                var lastWave = s.ExplodeT + (waveCore.Length - 1) * s.waveDelay + s.waveSeconds * 0.5f;
                for (var j = 0; j < linger.Length; j++)
                {
                    var g = (T - lastWave - 0.5f * Hash(j, 305) * P.residualSeconds) / (P.residualSeconds * 0.6f);
                    if (g <= 0f || g >= 1f) { linger[j].color = off; continue; }
                    var at = cells.Count > 0 ? cells[Mathf.Min(cells.Count - 1, (int)(Hash(j, 301) * cells.Count))] : center;
                    at += new Vector3(Hash(j, 302) - 0.5f, Hash(j, 303) - 0.5f, 0f) * (cell * 0.6f);
                    var env = Mathf.Sin(Mathf.PI * g);
                    var size = cell * 0.6f * (0.5f + 0.8f * Hash(j, 304)) * env;
                    Place(linger[j], at, size, size, A(j % 3 == 0 ? p.prismPink : j % 3 == 1 ? p.prismCyan : Color.Lerp(p.prismGold, Color.white, 0.5f), env));
                }

                bolts.End();
            }
        }
    }
}
