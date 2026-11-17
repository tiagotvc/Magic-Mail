// Efeitos mágicos dos power-ups e do match (kit Royal Postman/VFX/Power ups): MATCH normal, BOMBA em área e LINE CLEANER.
// Tudo é montado em camadas separadas de sprites (as máscaras do kit), animadas em função do tempo, com cores do Royal Postman
// (dourado #FFC54D, magenta #EE70F1, violeta #9F5AF2). Os sprites vêm de um pool, então não há instanciar/destruir por efeito.
//
// É o componente do prefab Resources/Effects/PowerUps/PowerUpVfx: arte, cores, tamanhos, contagens e tempos aparecem no
// Inspector (e na janela Royal Aves/VFX/Power-ups/Preview). O jogo usa a fachada PowerUpVfx; a janela de preview usa este
// componente direto, avançando o tempo à mão (manual = true).
//
// Cada efeito só escuta o gameplay: nunca decide pontuação, remoção de peças nem movimento.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Effects
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Royal Aves/Power-Up VFX Player")]
    public sealed class PowerUpVfxPlayer : MonoBehaviour
    {
        public const string ResourcePath = "Effects/PowerUps/PowerUpVfx";

        // ------------------------------------------------------------------------------------------ arte

        [Header("Máscaras comuns (00_Comuns)")]
        public Sprite radialGlow;
        public Sprite sparkle4;
        public Sprite sparkle6;
        public Sprite[] fragments = new Sprite[0];     // losango, lasca, triângulo
        public Sprite dot;

        [Header("Match (01_Match_Normal)")]
        public Sprite matchRing;
        public Sprite matchFlash;
        public Sprite matchRingFragmented;
        public Sprite matchSparkleTrail;

        [Header("Bomba (02_Bomba_Area)")]
        public Sprite bombFlash;
        public Sprite bombRingThin;
        public Sprite bombRingWide;
        public Sprite bombRaysLong;
        public Sprite bombRaysShort;
        public Sprite bombAura;

        [Header("Line Cleaner (03_Line_Cleaner)")]
        public Sprite lineCore;
        public Sprite lineGlow;
        public Sprite ribbonA;
        public Sprite ribbonB;
        public Sprite lineTip;
        public Sprite lineBreakRing;
        public Sprite lineDust;

        [Header("Seeker / girocóptero (04_Seeker_Impacto)")]
        public Sprite seekerRing;
        public Sprite seekerFlash;
        public Sprite seekerStar;
        public Sprite seekerArc;
        public Sprite seekerTrail;
        public Sprite seekerMicroStar;

        [Header("Material e desenho")]
        [Tooltip("Aditivo (luz). Os fragmentos usam o material normal de sprite.")]
        public Material additive;
        public string sortingLayer = "Item mask";
        [Tooltip("Ordem base dentro da camada; os efeitos ficam acima das peças.")]
        public int sortingBase = 70;

        [Header("Cores do Royal Postman")]
        public Color white = new Color(1f, 0.97f, 0.88f, 1f);
        public Color gold = new Color(1f, 0.77f, 0.30f, 1f);        // #FFC54D
        public Color magenta = new Color(0.93f, 0.44f, 0.95f, 1f);  // #EE70F1
        public Color violet = new Color(0.62f, 0.35f, 0.95f, 1f);   // #9F5AF2
        public Color[] confettiColors =
        {
            new Color(0.93f, 0.22f, 0.30f), new Color(0.22f, 0.78f, 0.38f), new Color(0.26f, 0.52f, 0.98f), new Color(1f, 0.80f, 0.16f),
        };

        // ------------------------------------------------------------------------------------------ parâmetros

        [Serializable]
        public sealed class MatchSettings
        {
            [Min(0.1f)] public float duration = 0.5f;
            [Header("1. Brilho de destaque (contorno dourado da peça)")]
            [Range(0f, 1f)] public float highlightAlpha = 0.85f;
            [Tooltip("Tamanho do contorno em relação à casa.")]
            [Min(0)] public float highlightSize = 0.92f;
            [Min(0.02f)] public float highlightSeconds = 0.16f;
            [Header("2. Explosão suave (clarão + brilho em cruz)")]
            [Range(0f, 1f)] public float flashAlpha = 0.8f;
            [Min(0)] public float flashSize = 1.25f;
            [Range(0f, 1f)] public float flareAlpha = 0.7f;
            [Min(0)] public float flareSize = 1.7f;
            [Min(0.05f)] public float burstSeconds = 0.2f;
            [Header("3. Partículas mágicas")]
            [Min(0)] public int sparkleCount = 5;
            [Min(0)] public float sparkleSize = 0.3f;
            [Min(0)] public float sparkleTravel = 0.42f;
            [Header("Fita mágica (dourado para magenta) que passa pela peça")]
            [Range(0f, 1f)] public float swooshAlpha = 0.75f;
            [Min(0)] public float swooshWidth = 0.1f;
            [Tooltip("Raio horizontal da volta da fita, em casas.")]
            [Min(0)] public float swooshRadius = 0.5f;
            [Header("Confetes (cores da peça)")]
            [Min(0)] public int confettiCount = 6;
            [Min(0)] public float confettiSize = 0.11f;
            [Min(0)] public float confettiSpeed = 2.6f;
            [Min(0)] public float confettiGravity = 5.5f;
            [Min(0.1f)] public float confettiLife = 0.55f;
        }

        [Serializable]
        public sealed class BombSettings
        {
            [Min(0.1f)] public float duration = 0.6f;
            [Header("Tela escura (o efeito brilha mais no escuro)")]
            [Range(0f, 1f)] public float dimAlpha = 0.55f;
            [Header("Núcleo (miolo branco + halo amarelo)")]
            [Range(0f, 1f)] public float coreAlpha = 1f;
            [Min(0)] public float coreSize = 1f;
            [Header("Explosão em estrela (raios grossos dourados)")]
            [Range(0f, 1f)] public float sunburstAlpha = 1f;
            [Tooltip("Tamanho dos raios em relação ao diâmetro da área.")]
            [Min(0)] public float sunburstSize = 1.25f;
            [Min(0)] public float sunburstSpin = 0f;
            [Range(0f, 1f)] public float warmGlowAlpha = 0.2f;
            [Range(0f, 1f)] public float ringBoldAlpha = 1f;
            [Header("Clarão central")]
            [Min(0.03f)] public float flashSeconds = 0.12f;
            [Min(0)] public float flashSize = 2.6f;
            [Header("Ondas de choque")]
            [Tooltip("Tempo para o círculo crescer do centro até a borda da área.")]
            [Min(0.05f)] public float ringSeconds = 0.2f;
            [Range(0f, 1f)] public float ringThinAlpha = 1f;
            [Range(0f, 1f)] public float ringWideAlpha = 1f;
            [Min(0)] public float ringWideDelay = 0.03f;
            [Header("Raios e aura")]
            [Range(0f, 1f)] public float raysAlpha = 1f;
            [Min(0)] public float raysSpin = 25f;
            [Range(0f, 1f)] public float auraAlpha = 0.5f;
            [Header("Fragmentos por célula atingida")]
            [Min(0)] public int fragmentsPerCell = 4;
            [Min(0)] public float fragmentSize = 0.13f;
            [Min(0)] public float fragmentSpeed = 3.4f;
            [Min(0)] public float fragmentGravity = 5f;
            [Min(0.1f)] public float fragmentLife = 0.6f;
            [Header("Estrelinhas radiais")]
            [Min(0)] public int sparkleCount = 16;
            [Min(0)] public float sparkleSize = 0.3f;
            [Header("Pedaços dourados lançados do centro")]
            [Min(0)] public int chunkCount = 0;
            [Min(0)] public float chunkSize = 0.2f;
        }

        [Serializable]
        public sealed class SeekerSettings
        {
            [Header("1. Decolagem (anéis mágicos girando embaixo)")]
            [Range(0f, 1f)] public float takeoffAlpha = 0.85f;
            [Min(0)] public float takeoffSize = 1.5f;
            [Header("2. Voo: rastro mágico (dourado na frente, magenta na cauda)")]
            [Tooltip("Quanto tempo de voo o rastro guarda.")]
            [Min(0.1f)] public float trailSeconds = 0.5f;
            [Min(0)] public float trailWidth = 0.42f;
            [Range(0f, 1f)] public float trailAlpha = 0.95f;
            [Range(0f, 1f)] public float trailGlowAlpha = 0.65f;
            [Min(0)] public int trailSparkles = 10;
            [Header("Marcador no alvo (anel dourado com brilho em cruz)")]
            [Range(0f, 1f)] public float markerAlpha = 0.9f;
            [Min(0)] public float markerSize = 1.1f;
            [Header("3. Impacto e limpeza")]
            [Min(0.1f)] public float duration = 0.5f;
            [Min(0)] public float flashSize = 0.85f;
            [Range(0f, 1f)] public float raysAlpha = 1f;
            [Min(0)] public float raysSize = 2.7f;
            [Tooltip("Riscos magenta entre os raios dourados.")]
            [Range(0f, 1f)] public float streakAlpha = 0.65f;
            [Min(0)] public float ringSize = 1.6f;
            [Min(0)] public int sparkleCount = 6;
            [Min(0)] public int fragmentCount = 9;
            [Min(0)] public float fragmentSize = 0.13f;
        }

        [Serializable]
        public sealed class LineSettings
        {
            [Header("Tela escura")]
            [Range(0f, 1f)] public float dimAlpha = 0.4f;
            [Header("Voo do feixe")]
            [Tooltip("Unidades de mundo por segundo (igual à do foguete, para andarem juntos).")]
            [Min(1f)] public float speed = 9f;
            [Tooltip("Comprimento visível atrás da frente, em casas.")]
            [Min(0.5f)] public float tailCells = 4.5f;
            [Min(0.05f)] public float retractSeconds = 0.35f;
            [Min(0)] public float exitOvershootCells = 0.8f;
            [Header("Espessuras (em casas)")]
            [Tooltip("Largura do halo da cauda na frente (afunila até a ponta).")]
            [Min(0)] public float glowWidth = 1.05f;
            [Tooltip("Largura do centro neon na frente.")]
            [Min(0)] public float coreWidth = 0.2f;
            [Header("Intensidades")]
            [Range(0f, 1f)] public float glowAlpha = 0.42f;
            [Range(0f, 1f)] public float coreAlpha = 1f;
            [Min(0)] public float tipSize = 1.5f;
            [Header("Aura esfumaçada (roxo, roxo escuro e amarelo)")]
            [Range(0f, 1f)] public float smokeAlpha = 0.5f;
            [Min(0)] public float smokeWidth = 2.5f;
            [Tooltip("Velocidade com que a fumaça desliza da frente para a ponta da cauda.")]
            public float smokeSpeed = 2.4f;
            [Header("Fogo amarelo esfumaçado")]
            [Range(0f, 1f)] public float fireAlpha = 0.8f;
            [Min(0)] public float fireWidth = 1.5f;
            [Header("Partículas redondas saindo da cauda")]
            [Min(0)] public int particleCount = 64;
            [Min(0)] public float particleSize = 0.11f;
            [Tooltip("Até onde as partículas se afastam do centro, em casas.")]
            [Min(0)] public float particleSpread = 0.85f;
            [Min(0)] public float particleRate = 2.2f;
            [Header("Cada casa atingida")]
            [Min(0.05f)] public float breakRingSeconds = 0.22f;
            [Min(0)] public float breakRingSize = 1.25f;
            [Min(0)] public int fragmentsPerCell = 5;
            [Min(0)] public float fragmentSize = 0.12f;
            [Min(0)] public float fragmentSpeed = 2.6f;
            [Min(0)] public float fragmentGravity = 5f;
            [Min(0.1f)] public float fragmentLife = 0.6f;
        }

        public SeekerSettings seeker = new SeekerSettings();
        public MatchSettings match = new MatchSettings();
        public BombSettings bomb = new BombSettings();
        public LineSettings line = new LineSettings();

        [Header("Execução")]
        [Tooltip("Câmera usada para cobrir a tela com o escurecimento (vazio = a câmera principal; o preview define a dele).")]
        public Camera viewCamera;
        [Tooltip("O preview do editor avança o tempo à mão (Tick); no jogo isto fica desligado.")]
        public bool manual;
        [Tooltip("Limite de efeitos ao mesmo tempo (os mais antigos terminam antes).")]
        [Min(4)] public int maxActive = 60;

        // ------------------------------------------------------------------------------------------ pool

        readonly Stack<SpriteRenderer> pool = new Stack<SpriteRenderer>();
        readonly List<Effect> active = new List<Effect>();
        Material defaultMaterial;
        Transform poolRoot;

        SpriteRenderer Rent(Sprite sprite, int order, bool additiveMaterial)
        {
            SpriteRenderer sr;
            if (pool.Count > 0) sr = pool.Pop();
            else
            {
                if (poolRoot == null) { poolRoot = new GameObject("Pool").transform; poolRoot.SetParent(transform, false); }
                var go = new GameObject("VFX");
                go.transform.SetParent(poolRoot, false);
                sr = go.AddComponent<SpriteRenderer>();
                if (defaultMaterial == null) defaultMaterial = sr.sharedMaterial;
            }
            sr.gameObject.SetActive(true);
            sr.sprite = sprite;
            sr.sharedMaterial = additiveMaterial && additive != null ? MaterialFor(sprite != null ? sprite.texture : null) : defaultMaterial;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingBase + order;
            sr.color = new Color(1, 1, 1, 0);
            sr.transform.localScale = Vector3.one;
            sr.transform.rotation = Quaternion.identity;
            return sr;
        }

        // Um material por textura: com um material só, a Unity junta os sprites num lote e desenha todos com a textura do primeiro.
        readonly Dictionary<Texture, Material> materialByTexture = new Dictionary<Texture, Material>();

        Material MaterialFor(Texture texture)
        {
            if (texture == null) return additive;
            if (materialByTexture.TryGetValue(texture, out var material) && material != null) return material;
            material = new Material(additive) { hideFlags = HideFlags.HideAndDontSave, mainTexture = texture };
            materialByTexture[texture] = material;
            return material;
        }

        void OnDestroy()
        {
            foreach (var material in materialByTexture.Values)
                if (material != null) { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
            materialByTexture.Clear();
        }

        void Return(SpriteRenderer sr)
        {
            if (sr == null) return;
            sr.sprite = null;
            sr.gameObject.SetActive(false);
            pool.Push(sr);
        }

        // ------------------------------------------------------------------------------------------ ciclo

        void Update()
        {
            if (!manual) Tick(Time.deltaTime);
        }

        /// Avança todos os efeitos ativos (o jogo chama a cada quadro; o preview, com o tempo que quiser).
        public void Tick(float dt)
        {
            for (var i = active.Count - 1; i >= 0; i--)
            {
                var effect = active[i];
                effect.age += dt;
                if (effect.age >= effect.duration) { effect.Release(); active.RemoveAt(i); continue; }
                effect.Evaluate(effect.age);
                if (DebugSolo >= 0) effect.Solo(DebugSolo);
            }
        }

        /// Diagnóstico (capturas do editor): mostra só a camada N de cada efeito. -1 = normal.
        public static int DebugSolo = -1;

        public int ActiveCount => active.Count;

        public void StopAll()
        {
            foreach (var effect in active) effect.Release();
            active.Clear();
        }

        void Launch(Effect effect)
        {
            if (active.Count >= maxActive) { active[0].Release(); active.RemoveAt(0); }
            effect.player = this;
            effect.Begin();
            active.Add(effect);
            effect.Evaluate(0f);
        }

        void OnDisable() => StopAll();

        // ------------------------------------------------------------------------------------------ API

        /// Match normal: efeito pequeno e delicado numa peça que sai do tabuleiro.
        public void PlayMatch(Vector3 position, Color pieceColor, float cell)
        {
            if (matchRing == null) return;
            Launch(new MatchEffect { position = position, pieceColor = Normalize(pieceColor), cell = cell });
        }

        /// Bomba em área: onda circular, clarão, raios e fragmentos saindo de cada célula atingida.
        /// radiusCells: raio da bomba em casas; cells/colors (opcionais): posição e cor das peças da área.
        public float PlayBomb(Vector3 center, float radiusCells, float cell, IList<Vector3> cells = null, IList<Color> colors = null)
        {
            if (bombRingThin == null) return 0f;
            var effect = new BombEffect { center = center, radius = Mathf.Max(0.5f, radiusCells), cell = cell, cells = cells, colors = colors };
            Launch(effect);
            return effect.duration;
        }

        /// Line Cleaner: feixe de energia que sai de origin para os dois lados (ou só um). cells: posição de cada casa da linha.
        /// stopA/stopB: o feixe termina na última casa (barreira) em vez de sair do tabuleiro.
        public float PlayLine(Vector3 origin, bool horizontal, Vector3 endA, Vector3 endB, bool stopA, bool stopB, IList<Vector3> cells, IList<Color> colors, float cell)
        {
            if (lineCore == null) return 0f;
            var effect = new LineEffect { origin = origin, horizontal = horizontal, endA = endA, endB = endB, stopA = stopA, stopB = stopB, cells = cells, colors = colors, cell = cell };
            Launch(effect);
            return effect.duration;
        }

        /// Impacto do girocóptero na peça-alvo: marcador mágico, clarão branco-dourado, estrela de raios e fragmentos na cor da peça.
        public float PlaySeekerImpact(Vector3 position, Color pieceColor, float cell)
        {
            if (seekerFlash == null) return 0f;
            var effect = new SeekerEffect { position = position, pieceColor = Normalize(pieceColor), cell = cell };
            Launch(effect);
            return effect.duration;
        }

        /// Decolagem e rastro mágico do girocóptero: acompanha o transform enquanto ele existir/estiver ativo.
        public void PlaySeekerFlight(Transform follow, float cell)
        {
            if (follow == null) return;
            Launch(new SeekerFlightEffect { follow = follow, cell = cell });
        }

        /// Marcador dourado no alvo enquanto o girocóptero voa até ele.
        public void PlaySeekerMarker(Vector3 position, float seconds, float cell)
        {
            Launch(new SeekerMarkerEffect { position = position, seconds = seconds, cell = cell });
        }

        Color Normalize(Color c)
        {
            if (c.a < 0.5f) return confettiColors.Length > 0 ? confettiColors[0] : gold;
            c.a = 1f;
            return c;
        }

        // ------------------------------------------------------------------------------------------ utilidades

        static float Hash(int a, int b)
        {
            unchecked
            {
                var x = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ 0x9E3779B9u;
                x ^= x >> 13; x *= 0x5bd1e995u; x ^= x >> 15;
                return (x & 0xFFFFFF) / 16777216f;
            }
        }

        static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
        static float EaseOut(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x); }
        static Color A(Color c, float a) { c.a = Mathf.Clamp01(a); return c; }
        static float Sw(Sprite s) => s == null ? 1f : Mathf.Max(0.01f, s.bounds.size.x);
        static float Sh(Sprite s) => s == null ? 1f : Mathf.Max(0.01f, s.bounds.size.y);

        /// Posiciona um sprite com largura/altura em unidades de mundo (independe do pixels-per-unit da importação).
        static void Place(SpriteRenderer sr, Vector3 pos, float width, float height, Color color, float angle = 0f)
        {
            if (sr == null) return;
            sr.transform.position = pos;
            sr.transform.rotation = Quaternion.Euler(0, 0, angle);
            sr.transform.localScale = new Vector3(Mathf.Max(0.0005f, width) / Sw(sr.sprite), Mathf.Max(0.0005f, height) / Sh(sr.sprite), 1f);
            sr.color = color;
        }

        static Sprite solidSprite, sunburstSprite, boldRingSprite;

        static Sprite Solid
        {
            get
            {
                if (solidSprite != null) return solidSprite;
                var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Point };
                for (var i = 0; i < 16; i++) texture.SetPixel(i % 4, i / 4, Color.white);
                texture.Apply();
                solidSprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f, 0, SpriteMeshType.FullRect);
                solidSprite.hideFlags = HideFlags.HideAndDontSave;
                return solidSprite;
            }
        }

        // Sprites gerados em código (FullRect, sem recorte em polígono). Usados onde as máscaras do kit são finas demais para a
        // intensidade da referência: estrela de raios, anéis nítidos com brilho e glow redondo.
        static Sprite glowDotSprite, ringCoreSprite, ringHaloSprite;

        static Sprite Procedural(int n, Func<float, float, float> alpha)
        {
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color[n * n];
            for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                    pixels[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(alpha((x + 0.5f) / n * 2f - 1f, (y + 0.5f) / n * 2f - 1f)));
            texture.SetPixels(pixels);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n, 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        /// Glow redondo e macio.
        static Sprite GlowDot => glowDotSprite != null ? glowDotSprite : glowDotSprite = Procedural(128, (x, y) =>
        {
            var d2 = x * x + y * y;
            return d2 >= 1f ? 0f : Mathf.Exp(-d2 * 5.5f) * (1f - d2);
        });

        /// Estrela de 16 raios: 8 longos e 8 curtos, cada um uma linha fina e muito clara com um brilho macio ao lado, e um núcleo no meio.
        static Sprite Sunburst
        {
            get
            {
                if (sunburstSprite != null) return sunburstSprite;
                const int n = 512, rays = 16;
                var step = Mathf.PI * 2f / rays;
                sunburstSprite = Procedural(n, (x, y) =>
                {
                    var r = Mathf.Sqrt(x * x + y * y);
                    if (r >= 1f) return 0f;
                    var theta = Mathf.Atan2(y, x);
                    var k = Mathf.RoundToInt(theta / step);
                    var delta = theta - k * step;
                    var perpendicular = r * Mathf.Sin(delta) * (n * 0.5f);     // distância ao raio, em pixels
                    var major = ((k % 2) + 2) % 2 == 0;
                    var reach = major ? 1f : 0.42f;
                    var fade = Mathf.Clamp01(1f - r / reach);
                    var line = Mathf.Exp(-(perpendicular / 1.5f) * (perpendicular / 1.5f)) * Mathf.Pow(fade, 0.35f);
                    var glow = Mathf.Exp(-(perpendicular / 11f) * (perpendicular / 11f)) * 0.16f * Mathf.Pow(fade, 0.9f);
                    return line + glow;
                });
                return sunburstSprite;
            }
        }

        /// Anel nítido (linha fina muito clara + brilho curto). O raio do anel é 0,9 da metade do sprite.
        static Sprite RingCore => ringCoreSprite != null ? ringCoreSprite : ringCoreSprite = Procedural(512, (x, y) =>
        {
            var r = Mathf.Sqrt(x * x + y * y);
            if (r >= 1f) return 0f;
            var d = (r - 0.9f) * 256f;
            return Mathf.Exp(-(d / 1.9f) * (d / 1.9f)) + 0.3f * Mathf.Exp(-(d / 9f) * (d / 9f));
        });

        /// Halo largo e macio em volta do anel (para tingir de magenta). O raio do anel é 0,74 da metade do sprite.
        static Sprite RingHalo => ringHaloSprite != null ? ringHaloSprite : ringHaloSprite = Procedural(512, (x, y) =>
        {
            var r = Mathf.Sqrt(x * x + y * y);
            if (r >= 1f) return 0f;
            var d = (r - 0.74f) * 256f;
            return 0.85f * Mathf.Exp(-(d / 26f) * (d / 26f));
        });

        // ---- Match: contorno dourado arredondado (destaque da peça) e estrela de 4 pontas (brilho em cruz e faíscas).
        static Sprite matchOutline, crossStar;

        static Sprite MatchOutline => matchOutline != null ? matchOutline : matchOutline = Procedural(256, (x, y) =>
        {
            const float half = 0.62f, corner = 0.24f;
            var qx = Mathf.Abs(x) - (half - corner); var qy = Mathf.Abs(y) - (half - corner);
            var outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
            var d = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - corner;       // distância à borda do quadrado arredondado
            return 0.9f * Gauss(d, 0.05f) + 0.4f * Gauss(d, 0.15f) + (d < 0f ? 0.22f : 0f);
        });

        static Sprite CrossStar => crossStar != null ? crossStar : crossStar = Procedural(256, (x, y) =>
        {
            var ax = Mathf.Abs(x); var ay = Mathf.Abs(y);
            if (ax >= 1f || ay >= 1f) return 0f;
            var horizontal = Gauss(ay, 0.012f + 0.05f * (1f - ax) * (1f - ax)) * Mathf.Pow(1f - ax, 1.3f);
            var vertical = Gauss(ax, 0.012f + 0.05f * (1f - ay) * (1f - ay)) * Mathf.Pow(1f - ay, 1.3f);
            return horizontal + vertical + 0.8f * Gauss(Mathf.Sqrt(x * x + y * y), 0.13f);
        });

        // ---- Bomba: camadas desenhadas em código. O anel fica em 0,8 da metade do sprite, para os raios dos eixos passarem dele.
        const float BombRing = 0.8f;
        static Sprite bombDisc, bombRingMagenta, bombRingGold, bombRingLine, bombRaysGlow, bombRaysLine, bombCoreWhite;

        static float Gauss(float d, float sigma) => Mathf.Exp(-(d / sigma) * (d / sigma));

        /// Preenchimento violeta do círculo (mais forte perto da borda).
        static Sprite BombDisc => bombDisc != null ? bombDisc : bombDisc = Procedural(256, (x, y) =>
        {
            var r = Mathf.Sqrt(x * x + y * y);
            return r < BombRing ? 0.45f + 0.55f * Mathf.Pow(r / BombRing, 2.5f) : Gauss(r - BombRing, 0.07f);
        });
        static Sprite BombRingMagenta => bombRingMagenta != null ? bombRingMagenta : bombRingMagenta = Procedural(512, (x, y) => Gauss(Mathf.Sqrt(x * x + y * y) - BombRing - 0.03f, 0.055f));
        static Sprite BombRingGold => bombRingGold != null ? bombRingGold : bombRingGold = Procedural(512, (x, y) => Gauss(Mathf.Sqrt(x * x + y * y) - BombRing + 0.012f, 0.032f));
        static Sprite BombRingLine => bombRingLine != null ? bombRingLine : bombRingLine = Procedural(512, (x, y) => Gauss(Mathf.Sqrt(x * x + y * y) - BombRing, 0.0075f));
        static Sprite BombRaysGlow => bombRaysGlow != null ? bombRaysGlow : bombRaysGlow = Procedural(512, (x, y) => BombRays(x, y, true));
        static Sprite BombRaysLine => bombRaysLine != null ? bombRaysLine : bombRaysLine = Procedural(512, (x, y) => BombRays(x, y, false));
        static Sprite BombCoreWhite => bombCoreWhite != null ? bombCoreWhite : bombCoreWhite = Procedural(128, (x, y) =>
        {
            var r = Mathf.Sqrt(x * x + y * y);
            return 1.7f * Gauss(r, 0.42f) * (1f - Mathf.Clamp01(r));
        });

        /// 8 raios longos afinando para a ponta (os dos eixos passam do anel, as diagonais chegam nele), 8 médios,
        /// risquinhos curtos entre eles e um brilho onde cada raio longo cruza o anel. glow = versão larga e macia.
        static Sprite seekerRaysGlow, seekerRaysLine;
        static Sprite SeekerRaysGlow => seekerRaysGlow != null ? seekerRaysGlow : seekerRaysGlow = Procedural(256, (x, y) => BombRays(x, y, true, false));
        static Sprite SeekerRaysLine => seekerRaysLine != null ? seekerRaysLine : seekerRaysLine = Procedural(256, (x, y) => BombRays(x, y, false, false));

        static float BombRays(float x, float y, bool glow, bool glints = true)
        {
            var r = Mathf.Sqrt(x * x + y * y);
            if (r >= 1f) return 0f;
            var theta = Mathf.Atan2(y, x);
            var a = 0f;
            const float step = Mathf.PI / 8f;
            var k = Mathf.RoundToInt(theta / step);
            var delta = theta - k * step;
            var p = Mathf.Abs(r * Mathf.Sin(delta));
            var idx = ((k % 16) + 16) % 16;
            var major = idx % 2 == 0;
            var reach = major ? (idx % 4 == 0 ? 0.99f : 0.86f) : 0.42f;
            if (r < reach && Mathf.Cos(delta) > 0f)
            {
                var q = 1f - r / reach;
                var w = (major ? 1f : 0.45f) * (0.0055f + 0.02f * Mathf.Pow(q, 1.4f));
                var end = Mathf.Clamp01(q / 0.1f);
                a += glow ? 0.75f * Gauss(p, w * 3.2f + 0.012f) * end * (0.45f + 0.55f * q) : Gauss(p, w) * end;
            }
            const float step2 = Mathf.PI / 16f;
            var k2 = Mathf.RoundToInt(theta / step2);
            var d2 = theta - k2 * step2;
            if (((k2 % 2) + 2) % 2 == 1)
            {
                var seed = Mathf.Repeat(Mathf.Abs(Mathf.Sin(k2 * 12.9898f) * 43758.5453f), 1f);
                var r1 = 0.3f + 0.1f * seed; var r2 = r1 + 0.16f + 0.1f * seed;
                var seg = Smooth((r - r1) / 0.04f) * (1f - Smooth((r - r2) / 0.06f));
                a += (glow ? 0.25f * Gauss(r * Mathf.Sin(d2), 0.014f) : 0.7f * Gauss(r * Mathf.Sin(d2), 0.0045f)) * seg;
            }
            if (major && glints)
            {
                var dist = Mathf.Sqrt(Mathf.Max(0f, r * r + BombRing * BombRing - 2f * r * BombRing * Mathf.Cos(delta)));
                a += glow ? 0.55f * Gauss(dist, 0.06f) : 0.9f * Gauss(dist, 0.022f);
            }
            return a;
        }

        /// Cobre a tela com preto (atrás do efeito, acima das peças).
        static void PlaceDim(SpriteRenderer sr, Camera cam, float z, float alpha)
        {
            if (sr == null) return;
            if (cam == null) { sr.color = A(Color.black, 0f); return; }
            var h = cam.orthographicSize * 2f * 1.4f;
            var w = h * Mathf.Max(1f, cam.aspect) * 1.1f;
            Place(sr, new Vector3(cam.transform.position.x, cam.transform.position.y, z), w, h, A(Color.black, alpha));
        }

        Camera ViewCam => viewCamera != null ? viewCamera : Camera.main;

        Sprite Fragment(int i) => fragments != null && fragments.Length > 0 ? fragments[i % fragments.Length] : null;

        // ------------------------------------------------------------------------------------------ fitas procedurais

        const int RibbonSegments = 56;
        static Texture2D ribbonProfile;

        static Texture2D RibbonProfile
        {
            get
            {
                if (ribbonProfile != null) return ribbonProfile;
                const int h = 64;
                ribbonProfile = new Texture2D(4, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (var y = 0; y < h; y++)
                {
                    var v = (y + 0.5f) / h * 2f - 1f;
                    var a = Mathf.Exp(-v * v * 4.5f) * Mathf.Clamp01((1f - Mathf.Abs(v)) * 5f);
                    for (var x = 0; x < 4; x++) ribbonProfile.SetPixel(x, y, new Color(1, 1, 1, a));
                }
                ribbonProfile.Apply();
                return ribbonProfile;
            }
        }

        sealed class Ribbon
        {
            public Mesh mesh;
            public Vector3[] v = new Vector3[2 * (RibbonSegments + 1)];
            public Color[] c = new Color[2 * (RibbonSegments + 1)];
            public Vector2[] uv = new Vector2[2 * (RibbonSegments + 1)];
        }

        static Texture2D smokeTexture;

        static float ValueNoise(float x, float y, int periodX, int seed)
        {
            var xi = Mathf.FloorToInt(x); var yi = Mathf.FloorToInt(y);
            var fx = x - xi; var fy = y - yi;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            int X(int v) => ((v % periodX) + periodX) % periodX;
            var a = Hash(X(xi) + yi * 57, seed); var b = Hash(X(xi + 1) + yi * 57, seed);
            var c = Hash(X(xi) + (yi + 1) * 57, seed); var d = Hash(X(xi + 1) + (yi + 1) * 57, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// Fumaça mágica em faixas (repete no comprimento): desliza pela cauda para dar a sensação de força saindo do foguete.
        static Texture2D SmokeTexture
        {
            get
            {
                if (smokeTexture != null) return smokeTexture;
                const int w = 256, h = 64;
                smokeTexture = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
                smokeTexture.wrapModeU = TextureWrapMode.Repeat; smokeTexture.wrapModeV = TextureWrapMode.Clamp;
                var pixels = new Color[w * h];
                for (var y = 0; y < h; y++)
                    for (var x = 0; x < w; x++)
                    {
                        var u = x / (float)w; var v = (y + 0.5f) / h;
                        var n = 0.55f * ValueNoise(u * 4f, v * 3f, 4, 201) + 0.3f * ValueNoise(u * 8f, v * 6f, 8, 202) + 0.15f * ValueNoise(u * 16f, v * 10f, 16, 203);
                        var vv = v * 2f - 1f;
                        var profile = Mathf.Exp(-vv * vv * 2.2f) * Mathf.Clamp01((1f - Mathf.Abs(vv)) * 3f);
                        pixels[y * w + x] = new Color(1, 1, 1, Smooth((n - 0.36f) / 0.34f) * profile);
                    }
                smokeTexture.SetPixels(pixels);
                smokeTexture.Apply();
                return smokeTexture;
            }
        }

        Ribbon MakeRibbon(string label, int order, List<GameObject> owner, Texture texture = null)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = MaterialFor(texture != null ? texture : RibbonProfile);
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = sortingBase + order;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            block.SetTexture("_MainTex", texture != null ? texture : RibbonProfile);
            renderer.SetPropertyBlock(block);
            var ribbon = new Ribbon { mesh = new Mesh { name = "PowerUpRibbon" } };
            ribbon.mesh.MarkDynamic();
            var tris = new int[RibbonSegments * 6];
            for (var i = 0; i < RibbonSegments; i++)
            {
                var a = i * 2;
                tris[i * 6] = a; tris[i * 6 + 1] = a + 1; tris[i * 6 + 2] = a + 2;
                tris[i * 6 + 3] = a + 1; tris[i * 6 + 4] = a + 3; tris[i * 6 + 5] = a + 2;
            }
            for (var i = 0; i <= RibbonSegments; i++)
            {
                ribbon.uv[2 * i] = new Vector2(i / (float)RibbonSegments, 0f);
                ribbon.uv[2 * i + 1] = new Vector2(i / (float)RibbonSegments, 1f);
            }
            ribbon.mesh.vertices = ribbon.v; ribbon.mesh.uv = ribbon.uv; ribbon.mesh.colors = ribbon.c; ribbon.mesh.triangles = tris;
            ribbon.mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 500f);
            filter.sharedMesh = ribbon.mesh;
            owner.Add(go);
            return ribbon;
        }

        // ------------------------------------------------------------------------------------------ efeito base

        abstract class Effect
        {
            public PowerUpVfxPlayer player;
            public float age, duration;
            protected readonly List<SpriteRenderer> sprites = new List<SpriteRenderer>();
            protected readonly List<GameObject> objects = new List<GameObject>();

            protected SpriteRenderer Add(Sprite sprite, int order, bool additive = true)
            {
                var sr = player.Rent(sprite, order, additive);
                sprites.Add(sr);
                return sr;
            }

            public void Solo(int index)
            {
                for (var i = 0; i < sprites.Count; i++)
                    if (i != index) { var c = sprites[i].color; c.a = 0f; sprites[i].color = c; }
            }

            public abstract void Begin();
            public abstract void Evaluate(float t);

            public void Release()
            {
                foreach (var sr in sprites) player.Return(sr);
                sprites.Clear();
                foreach (var go in objects)
                {
                    if (go == null) continue;
                    var filter = go.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null) { if (Application.isPlaying) Destroy(filter.sharedMesh); else DestroyImmediate(filter.sharedMesh); }
                    if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
                }
                objects.Clear();
            }
        }

        /// Fragmento que voa com gravidade (confete, caco).
        struct Shard
        {
            public SpriteRenderer sr;
            public Vector3 origin;
            public Vector2 velocity;
            public float spin, size, life, delay;
            public Color color;
        }

        static void UpdateShard(in Shard s, float t, float cell, float gravity)
        {
            var a = t - s.delay;
            if (a < 0f || a > s.life) { s.sr.color = A(Color.white, 0f); return; }
            var f = a / s.life;
            var p = new Vector3(s.origin.x + s.velocity.x * cell * a, s.origin.y + s.velocity.y * cell * a - 0.5f * gravity * cell * a * a, s.origin.z);
            var size = s.size * cell * (1f - 0.4f * f);
            s.sr.transform.position = p;
            s.sr.transform.rotation = Quaternion.Euler(0, 0, s.spin * a);
            s.sr.transform.localScale = new Vector3(size / Sw(s.sr.sprite), size / Sh(s.sr.sprite), 1f);
            s.sr.color = A(s.color, 1f - Smooth((f - 0.55f) / 0.45f));
        }

        // ------------------------------------------------------------------------------------------ MATCH

        sealed class MatchEffect : Effect
        {
            public Vector3 position; public Color pieceColor; public float cell;
            MatchSettings s;
            SpriteRenderer outline, inner, flash, flare;
            SpriteRenderer[] sparkles;
            Ribbon swoosh;
            Shard[] confetti;
            float seed;

            public override void Begin()
            {
                s = player.match;
                duration = s.duration;
                seed = Hash((int)(position.x * 100), (int)(position.y * 100));
                inner = Add(GlowDot, 0);
                outline = Add(MatchOutline, 1);
                flash = Add(GlowDot, 2);
                swoosh = player.MakeRibbon("Fita mágica", 3, objects);
                flare = Add(CrossStar, 4);
                sparkles = new SpriteRenderer[s.sparkleCount];
                for (var i = 0; i < sparkles.Length; i++) sparkles[i] = Add(CrossStar, 5);
                confetti = new Shard[s.confettiCount];
                for (var i = 0; i < confetti.Length; i++)
                {
                    var angle = Hash(i, 11) * Mathf.PI * 2f;
                    var speed = s.confettiSpeed * (0.45f + 0.75f * Hash(i, 12));
                    var kind = Hash(i, 13);
                    var color = kind < 0.2f ? Color.Lerp(player.white, player.gold, Hash(i, 14))
                        : kind < 0.3f ? player.magenta
                        : kind < 0.65f ? Color.Lerp(pieceColor, Color.white, 0.25f + 0.3f * Hash(i, 15))
                        : Color.Lerp(pieceColor, Color.black, 0.18f * Hash(i, 16));
                    confetti[i] = new Shard
                    {
                        sr = Add(player.Fragment(i), 8, false), origin = position,
                        velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                        spin = (Hash(i, 17) - 0.5f) * 720f, size = s.confettiSize * (0.6f + 0.9f * Hash(i, 18)),
                        life = s.confettiLife * (0.7f + 0.5f * Hash(i, 19)), delay = 0.08f + 0.02f * Hash(i, 20), color = color,
                    };
                }
            }

            public override void Evaluate(float t)
            {
                // 1. BRILHO DE DESTAQUE: contorno dourado em volta da peça, acende e apaga rápido
                var h = Smooth(t / 0.03f) * (1f - Smooth((t - s.highlightSeconds * 0.45f) / (s.highlightSeconds * 0.55f)));
                var outlineSize = s.highlightSize * cell / 0.62f * (0.96f + 0.1f * Mathf.Clamp01(t / s.highlightSeconds));
                Place(outline, position, outlineSize, outlineSize, A(Color.Lerp(player.gold, player.white, 0.25f), s.highlightAlpha * h));
                Place(inner, position, cell * 1.5f, cell * 1.5f, A(Color.Lerp(pieceColor, player.gold, 0.5f), 0.3f * s.highlightAlpha * h));

                // 2. EXPLOSÃO SUAVE: clarão macio branco-dourado e um brilho fino em cruz
                var ef = (t - 0.05f) / s.burstSeconds;
                if (ef >= 0f && ef <= 1f)
                {
                    var grow = EaseOut(ef);
                    var flashSize = s.flashSize * cell * (0.7f + 0.5f * grow);
                    Place(flash, position, flashSize, flashSize, A(Color.Lerp(player.white, player.gold, 0.3f), s.flashAlpha * (1f - ef) * (1f - ef)));
                    var flareSize = s.flareSize * cell * (0.6f + 0.5f * grow);
                    Place(flare, position, flareSize, flareSize, A(Color.Lerp(player.white, player.gold, 0.2f), s.flareAlpha * Mathf.Pow(1f - ef, 1.5f)));
                }
                else { flash.color = A(Color.white, 0f); flare.color = A(Color.white, 0f); }

                // 3. PARTÍCULAS MÁGICAS: estrelinhas de 4 pontas que se afastam piscando
                for (var i = 0; i < sparkles.Length; i++)
                {
                    var f = (t - 0.08f - 0.025f * i) / (duration - 0.2f);
                    if (f < 0f || f > 1f) { sparkles[i].color = A(Color.white, 0f); continue; }
                    var angle = (seed + i / (float)sparkles.Length + (Hash(i, 31) - 0.5f) * 0.12f) * Mathf.PI * 2f;
                    var dist = cell * (0.2f + s.sparkleTravel * EaseOut(f)) * (0.7f + 0.5f * Hash(i, 33));
                    var env = Mathf.Sin(Mathf.PI * Mathf.Pow(f, 0.6f));
                    var size = s.sparkleSize * cell * (0.6f + 0.7f * Hash(i, 32)) * (0.5f + 0.5f * env) * (0.85f + 0.15f * Mathf.Sin(t * 40f + i));
                    Place(sparkles[i], position + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist + cell * 0.08f * f, 0f), size, size,
                        A(i % 2 == 0 ? Color.Lerp(player.gold, player.white, 0.4f) : player.white, env));
                }

                // Fita mágica: dá uma volta achatada pela peça, dourada na frente e magenta na cauda
                var sf = Mathf.Clamp01((t - 0.1f) / (duration - 0.14f));
                var headAngle = (seed * 360f - 150f + 330f * EaseOut(sf)) * Mathf.Deg2Rad;
                var spanAngle = Mathf.Min(170f, 330f * EaseOut(sf)) * Mathf.Deg2Rad;
                var envelope = Smooth(sf / 0.12f) * (1f - Smooth((sf - 0.55f) / 0.45f));
                var rx = s.swooshRadius * cell; var ry = rx * 0.42f;
                var centre = position - new Vector3(0f, cell * 0.08f, 0f);
                for (var k = 0; k <= RibbonSegments; k++)
                {
                    var q = k / (float)RibbonSegments;
                    var a = headAngle - q * spanAngle;
                    var point = centre + new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry, 0f);
                    var tangent = new Vector3(-Mathf.Sin(a) * rx, Mathf.Cos(a) * ry, 0f).normalized;
                    var half = new Vector3(-tangent.y, tangent.x, 0f) * (s.swooshWidth * cell * Mathf.Pow(1f - q, 0.8f) * 0.5f);
                    swoosh.v[2 * k] = point - half; swoosh.v[2 * k + 1] = point + half;
                    var col = Color.Lerp(Color.Lerp(player.white, player.gold, 0.45f), player.magenta, Mathf.Clamp01(q * 1.4f));
                    col.a = Mathf.Clamp01(s.swooshAlpha * envelope * Mathf.Pow(1f - q, 0.5f));
                    swoosh.c[2 * k] = col; swoosh.c[2 * k + 1] = col;
                }
                swoosh.mesh.vertices = swoosh.v; swoosh.mesh.colors = swoosh.c;

                // Confetes com a cor da peça
                for (var i = 0; i < confetti.Length; i++) UpdateShard(confetti[i], t, cell, s.confettiGravity);
            }
        }

        // ------------------------------------------------------------------------------------------ BOMBA

        sealed class BombEffect : Effect
        {
            public Vector3 center; public float radius, cell;
            public IList<Vector3> cells; public IList<Color> colors;
            BombSettings s;
            SpriteRenderer dim, field, warm, haloRing, haloRing2, starB, starA, ring, ring2, flashHot, flashCore;
            SpriteRenderer[] sparkHalo, sparkStar, sparkCore, chunks, dust;
            Vector3[] dustAt;
            Shard[] shards;
            float diameter;

            public override void Begin()
            {
                s = player.bomb;
                duration = s.duration;
                diameter = (radius * 2f + 1f) * cell;
                dim = Add(Solid, -2, false);
                field = Add(BombDisc, 0);
                warm = Add(GlowDot, 1);
                haloRing = Add(BombRingMagenta, 2);
                haloRing2 = Add(BombRingGold, 2);
                starB = Add(BombRaysGlow, 3);
                starA = Add(BombRaysLine, 4);
                ring = Add(BombRingLine, 5);
                ring2 = Add(GlowDot, 6);
                flashHot = Add(GlowDot, 7);
                flashCore = Add(BombCoreWhite, 8);
                var n = s.sparkleCount;
                sparkHalo = new SpriteRenderer[n]; sparkStar = new SpriteRenderer[n]; sparkCore = new SpriteRenderer[n];
                for (var i = 0; i < n; i++) { sparkHalo[i] = Add(GlowDot, 9); sparkStar[i] = Add(player.sparkle4, 10); sparkCore[i] = Add(GlowDot, 11); }
                chunks = new SpriteRenderer[s.chunkCount];
                for (var i = 0; i < chunks.Length; i++) chunks[i] = Add(player.Fragment(i), 12, false);

                // Fragmentos: saem de cada célula atingida (ou de pontos na área, se não há lista), em dourado e nas cores das peças.
                var points = cells != null && cells.Count > 0 ? new List<Vector3>(cells) : new List<Vector3>();
                if (points.Count == 0)
                {
                    var count = Mathf.Max(4, Mathf.RoundToInt(radius * radius * 3.14f));
                    for (var i = 0; i < count; i++)
                    {
                        var a = Hash(i, 41) * Mathf.PI * 2f; var d = Mathf.Sqrt(Hash(i, 42)) * radius * cell;
                        points.Add(center + new Vector3(Mathf.Cos(a) * d, Mathf.Sin(a) * d, 0f));
                    }
                }
                var list = new List<Shard>();
                for (var c = 0; c < points.Count; c++)
                {
                    var color = colors != null && c < colors.Count && colors[c].a > 0.5f ? colors[c] : player.confettiColors[c % Mathf.Max(1, player.confettiColors.Length)];
                    var away = points[c] - center; away.z = 0f;
                    var baseDir = away.sqrMagnitude > 1e-4f ? away.normalized : new Vector3(Hash(c, 43) - 0.5f, Hash(c, 44) - 0.5f, 0f).normalized;
                    var perCell = Mathf.Clamp(120 / Mathf.Max(1, points.Count), 1, s.fragmentsPerCell);   // limita o total em bombas gigantes
                    for (var k = 0; k < perCell; k++)
                    {
                        var jitter = (Hash(c * 7 + k, 45) - 0.5f) * 1.6f;
                        var dir = Quaternion.Euler(0, 0, jitter * Mathf.Rad2Deg) * baseDir;
                        var tone = Hash(c * 7 + k, 46);
                        list.Add(new Shard
                        {
                            sr = Add(player.Fragment(c + k), 12, false), origin = points[c],
                            velocity = new Vector2(dir.x, dir.y) * s.fragmentSpeed * (0.5f + 0.8f * Hash(c * 7 + k, 47)),
                            spin = (Hash(c * 7 + k, 48) - 0.5f) * 800f, size = s.fragmentSize * (0.6f + 0.9f * Hash(c * 7 + k, 49)),
                            life = s.fragmentLife * (0.75f + 0.5f * Hash(c * 7 + k, 50)), delay = 0.03f + 0.02f * Hash(c, 51),
                            color = tone < 0.5f ? Color.Lerp(player.gold, player.white, Hash(c + k, 52) * 0.5f) : Color.Lerp(color, Color.white, 0.15f + 0.25f * Hash(c * 7 + k, 53)),
                        });
                    }
                }
                shards = list.ToArray();
                // Poeira violeta que fica em cada célula limpa e some devagar
                var dustCount = Mathf.Min(points.Count, 60);
                dust = new SpriteRenderer[dustCount]; dustAt = new Vector3[dustCount];
                for (var i = 0; i < dustCount; i++) { dust[i] = Add(GlowDot, 1); dustAt[i] = points[i]; }
            }

            public override void Evaluate(float t)
            {
                var D = diameter;
                var f = t / duration;
                var growth = EaseOut(Mathf.Clamp01(t / 0.2f));           // a explosão cresce rápido do centro para fora
                var late = Mathf.Clamp01((t - 0.12f) / 0.4f);

                // Tela escura (a explosão brilha mais no escuro)
                PlaceDim(dim, player.ViewCam, center.z, s.dimAlpha * Smooth(t / 0.05f) * (1f - Smooth((t - duration * 0.5f) / (duration * 0.5f))));

                // Tudo (círculo violeta, anel, raios) cresce junto do centro até a borda da área, segura e some.
                var k3 = 1f - Mathf.Clamp01(t / s.ringSeconds);
                var grow = 0.14f + 0.86f * (1f - k3 * k3 * k3) + 0.05f * Mathf.Clamp01(f);
                var ringRadius = D * 0.5f * 0.97f * grow;
                var S = 2f * ringRadius / BombRing;
                var life = Smooth(t / 0.03f) * (1f - Smooth((t - duration * 0.5f) / (duration * 0.5f)));
                var hot = Smooth(t / 0.02f) * (1f - Smooth((t - duration * 0.27f) / (duration * 0.57f)));
                warm.color = A(Color.white, 0f);
                Place(field, center, S, S, A(Color.Lerp(player.violet, player.magenta, 0.35f), s.auraAlpha * life));
                Place(haloRing, center, S, S, A(Color.Lerp(player.magenta, player.violet, 0.2f), 0.3f * s.ringWideAlpha * life));
                Place(haloRing2, center, S, S, A(new Color(1f, 0.62f, 0.12f), 0.95f * s.ringThinAlpha * life));
                Place(starB, center, S, S, A(new Color(1f, 0.55f, 0.1f), s.sunburstAlpha * life), t * s.sunburstSpin);
                Place(starA, center, S, S, A(new Color(1f, 0.86f, 0.5f), s.sunburstAlpha * life), t * s.sunburstSpin);
                Place(ring, center, S, S, A(new Color(1f, 0.88f, 0.55f), s.ringThinAlpha * life));

                // Núcleo: miolo branco, halo amarelo e borda laranja
                var core = D * (0.34f + 0.2f * (1f - k3 * k3 * k3)) * s.coreSize;
                Place(ring2, center, core * 2.8f, core * 2.8f, A(new Color(1f, 0.5f, 0.1f), 0.55f * s.coreAlpha * hot));
                Place(flashHot, center, core * 1.9f, core * 1.9f, A(new Color(1f, 0.8f, 0.25f), s.coreAlpha * hot));
                Place(flashCore, center, core, core, A(player.white, s.coreAlpha * hot));

                // Partículas saindo do centro: pontinhos dourados pequenos
                for (var i = 0; i < sparkCore.Length; i++)
                {
                    var delay = 0.01f + 0.012f * (i % 9);
                    var sparkLife = duration * (0.55f + 0.3f * Hash(i, 61));
                    var tt = Mathf.Clamp01((t - delay) / sparkLife);
                    var angle = (i / (float)sparkCore.Length) * Mathf.PI * 2f + Hash(i, 62) * 0.5f;
                    var dist = D * 0.5f * (0.1f + 1.0f * EaseOut(tt)) * (0.7f + 0.4f * Hash(i, 63));
                    var env = Mathf.Sin(Mathf.PI * Mathf.Pow(tt, 0.7f));
                    var size = s.sparkleSize * cell * (0.35f + 0.6f * Hash(i, 64)) * (0.6f + 0.4f * env);
                    var pos = center + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0f);
                    var tint = Hash(i, 65) < 0.7f ? player.gold : player.white;
                    Place(sparkHalo[i], pos, size * 1.8f, size * 1.8f, A(tint, 0.22f * env));
                    Place(sparkCore[i], pos, size * 0.7f, size * 0.7f, A(Color.Lerp(tint, player.white, 0.5f), 0.95f * env));
                    sparkStar[i].color = A(Color.white, 0f);
                }
                for (var i = 0; i < chunks.Length; i++) chunks[i].color = A(Color.white, 0f);

                // Fase "limpeza e partículas": poeira violeta em cada célula e cacos saindo das células
                for (var i = 0; i < dust.Length; i++)
                {
                    var df = Mathf.Clamp01((t - 0.12f - 0.01f * (i % 7)) / (duration - 0.12f));
                    var size = cell * (0.9f + 0.2f * Hash(i, 91));
                    Place(dust[i], dustAt[i], size, size, A(Color.Lerp(player.violet, player.magenta, 0.3f + 0.3f * Hash(i, 92)), 0.22f * Mathf.Sin(Mathf.PI * Mathf.Pow(df, 0.55f))));
                }
                for (var i = 0; i < shards.Length; i++) UpdateShard(shards[i], t, cell, s.fragmentGravity);
            }
        }

        // ------------------------------------------------------------------------------------------ SEEKER

        /// 3. Impacto e limpeza: núcleo branco, raios dourados, riscos magenta, anel e fragmentos na cor da peça.
        sealed class SeekerEffect : Effect
        {
            public Vector3 position; public Color pieceColor; public float cell;
            SeekerSettings s;
            SpriteRenderer glowOrange, glowYellow, core, raysGlow, raysLine, streaks, ring, ringGold;
            SpriteRenderer[] sparkles;
            Shard[] shards;

            public override void Begin()
            {
                s = player.seeker;
                duration = s.duration;
                glowOrange = Add(GlowDot, 0);
                streaks = Add(SeekerRaysLine, 1);
                ringGold = Add(BombRingGold, 2);
                raysGlow = Add(SeekerRaysGlow, 3);
                raysLine = Add(SeekerRaysLine, 4);
                ring = Add(BombRingLine, 5);
                glowYellow = Add(GlowDot, 6);
                core = Add(BombCoreWhite, 7);
                sparkles = new SpriteRenderer[s.sparkleCount];
                for (var i = 0; i < sparkles.Length; i++) sparkles[i] = Add(CrossStar, 8);
                shards = new Shard[s.fragmentCount];
                for (var i = 0; i < shards.Length; i++)
                {
                    var angle = Hash(i, 81) * Mathf.PI * 2f;
                    var tone = Hash(i, 82);
                    shards[i] = new Shard
                    {
                        sr = Add(player.Fragment(i), 9, false), origin = position,
                        velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (2f + 2.2f * Hash(i, 83)),
                        spin = (Hash(i, 84) - 0.5f) * 720f, size = s.fragmentSize * (0.6f + 0.9f * Hash(i, 85)),
                        life = duration * (0.7f + 0.3f * Hash(i, 86)), delay = 0.02f,
                        color = tone < 0.2f ? Color.Lerp(player.white, player.gold, Hash(i, 87)) : Color.Lerp(pieceColor, Color.white, 0.12f + 0.25f * Hash(i, 88)),
                    };
                }
            }

            public override void Evaluate(float t)
            {
                var grow = EaseOut(Mathf.Clamp01(t / 0.14f));
                var life = Smooth(t / 0.02f) * (1f - Smooth((t - 0.12f) / (duration - 0.12f)));
                var hot = Smooth(t / 0.02f) * (1f - Smooth((t - 0.05f) / 0.28f));
                var rays = s.raysSize * cell * (0.35f + 0.65f * grow);
                Place(raysGlow, position, rays, rays, A(new Color(1f, 0.55f, 0.1f), s.raysAlpha * life));
                Place(raysLine, position, rays, rays, A(new Color(1f, 0.9f, 0.6f), s.raysAlpha * life));
                var streak = s.raysSize * 1.25f * cell * (0.3f + 0.7f * grow);
                Place(streaks, position, streak, streak, A(player.magenta, s.streakAlpha * life), 22.5f);
                var ringDiameter = s.ringSize * cell * (0.35f + 0.65f * EaseOut(Mathf.Clamp01(t / 0.24f)));
                var ringAlpha = 1f - Smooth(t / 0.32f);
                Place(ring, position, ringDiameter / BombRing, ringDiameter / BombRing, A(new Color(1f, 0.9f, 0.6f), ringAlpha));
                Place(ringGold, position, ringDiameter / BombRing, ringDiameter / BombRing, A(Color.Lerp(player.magenta, player.gold, 0.4f), 0.7f * ringAlpha));
                var c = s.flashSize * cell * (0.6f + 0.4f * grow);
                Place(glowOrange, position, c * 2.4f, c * 2.4f, A(new Color(1f, 0.5f, 0.1f), 0.5f * hot));
                Place(glowYellow, position, c * 1.6f, c * 1.6f, A(new Color(1f, 0.82f, 0.28f), hot));
                Place(core, position, c * 0.9f, c * 0.9f, A(player.white, hot));
                for (var i = 0; i < sparkles.Length; i++)
                {
                    var tt = Mathf.Clamp01((t - 0.03f) / (duration * 0.85f));
                    var angle = (i / (float)Mathf.Max(1, sparkles.Length)) * Mathf.PI * 2f + Hash(i, 89) * 0.8f;
                    var dist = cell * (0.35f + 0.85f * EaseOut(tt)) * (0.75f + 0.4f * Hash(i, 91));
                    var e = Mathf.Sin(Mathf.PI * Mathf.Pow(tt, 0.7f));
                    var size = cell * 0.34f * (0.6f + 0.6f * Hash(i, 90)) * (0.5f + 0.5f * e);
                    Place(sparkles[i], position + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0f), size, size, A(i % 2 == 0 ? player.white : Color.Lerp(player.gold, player.white, 0.3f), e));
                }
                for (var i = 0; i < shards.Length; i++) UpdateShard(shards[i], t, cell, 5f);
            }
        }

        /// Marcador no alvo: anel dourado que fecha sobre a peça e pulsa, com brilho em cruz, enquanto o girocóptero chega.
        sealed class SeekerMarkerEffect : Effect
        {
            public Vector3 position; public float seconds, cell;
            SeekerSettings s;
            SpriteRenderer glow, ringGold, ring, flare;

            public override void Begin()
            {
                s = player.seeker;
                duration = seconds + 0.18f;
                glow = Add(GlowDot, 0);
                ringGold = Add(BombRingGold, 1);
                ring = Add(BombRingLine, 2);
                flare = Add(CrossStar, 3);
            }

            public override void Evaluate(float t)
            {
                var arrive = Smooth(t / 0.18f);
                var alpha = s.markerAlpha * arrive * (1f - Smooth((t - seconds) / 0.18f));
                var diameter = s.markerSize * cell * (1.6f - 0.6f * arrive) * (1f + 0.04f * Mathf.Sin(t * 14f));
                Place(ring, position, diameter / BombRing, diameter / BombRing, A(new Color(1f, 0.9f, 0.55f), alpha));
                Place(ringGold, position, diameter / BombRing, diameter / BombRing, A(new Color(1f, 0.6f, 0.12f), 0.9f * alpha));
                Place(glow, position, diameter * 1.5f, diameter * 1.5f, A(player.gold, 0.28f * alpha));
                var flareSize = diameter * 1.75f * (0.92f + 0.08f * Mathf.Sin(t * 9f));
                Place(flare, position, flareSize, flareSize, A(Color.Lerp(player.gold, player.white, 0.5f), 0.75f * alpha));
            }
        }

        /// 1. Decolagem e 2. voo com rastro mágico: segue o girocóptero. Termina sozinho quando ele some.
        sealed class SeekerFlightEffect : Effect
        {
            public Transform follow; public float cell;
            SeekerSettings s;
            SpriteRenderer aura, swirlA, swirlB;
            SpriteRenderer[] sparkles;
            Ribbon trail, trailGlow;
            readonly List<Vector4> history = new List<Vector4>();
            readonly Vector3[] points = new Vector3[RibbonSegments + 1];
            Vector3 last;
            float lostAt = -1f;

            public override void Begin()
            {
                s = player.seeker;
                duration = 12f;
                last = follow.position;
                aura = Add(GlowDot, 0);
                swirlB = Add(BombRingMagenta, 1);
                swirlA = Add(BombRingGold, 1);
                trailGlow = player.MakeRibbon("Rastro (brilho)", 1, objects);
                trail = player.MakeRibbon("Rastro mágico", 2, objects);
                sparkles = new SpriteRenderer[s.trailSparkles];
                for (var i = 0; i < sparkles.Length; i++) sparkles[i] = Add(CrossStar, 3);
            }

            Vector3 Sample(float time)
            {
                for (var i = history.Count - 1; i >= 0; i--)
                {
                    if (history[i].w > time) continue;
                    if (i == history.Count - 1) return history[i];
                    var a = history[i]; var b = history[i + 1];
                    var k = Mathf.Clamp01((time - a.w) / Mathf.Max(1e-5f, b.w - a.w));
                    return Vector3.Lerp(a, b, k);
                }
                return history.Count > 0 ? (Vector3)history[0] : last;
            }

            public override void Evaluate(float t)
            {
                var alive = follow != null && follow.gameObject.activeInHierarchy;
                if (alive)
                {
                    last = follow.position;
                    history.Add(new Vector4(last.x, last.y, last.z, t));
                    while (history.Count > 2 && history[1].w < t - s.trailSeconds - 0.1f) history.RemoveAt(0);
                }
                else if (lostAt < 0f) { lostAt = t; duration = t + 0.3f; }
                var fade = lostAt < 0f ? 1f : 1f - Smooth((t - lostAt) / 0.3f);

                // 1. DECOLAGEM: dois anéis achatados (como vistos de cima) abrindo embaixo dele + aura dourada
                var under = last - new Vector3(0f, cell * 0.28f, 0f);
                for (var i = 0; i < 2; i++)
                {
                    var tf = (t - 0.14f * i) / 0.45f;
                    var sr = i == 0 ? swirlA : swirlB;
                    if (tf < 0f || tf > 1f) { sr.color = A(Color.white, 0f); continue; }
                    var w = s.takeoffSize * cell * (0.35f + 0.75f * EaseOut(tf)) / BombRing;
                    Place(sr, under, w, w * 0.38f, A(i == 0 ? new Color(1f, 0.82f, 0.35f) : player.magenta, s.takeoffAlpha * Mathf.Pow(1f - tf, 0.8f) * fade));
                }
                Place(aura, last, cell * 1.7f, cell * 1.7f, A(Color.Lerp(player.gold, player.magenta, 0.25f), 0.5f * fade * Smooth(t / 0.2f)));

                // 2. RASTRO MÁGICO: fita pelo caminho percorrido, larga e dourada perto dele, fina e magenta na ponta
                for (var k = 0; k <= RibbonSegments; k++) points[k] = Sample(t - k / (float)RibbonSegments * s.trailSeconds);
                var travelled = (points[0] - points[RibbonSegments]).magnitude;
                var moving = Smooth(travelled / (0.8f * cell));
                for (var k = 0; k <= RibbonSegments; k++)
                {
                    var q = k / (float)RibbonSegments;
                    var tangent = points[Mathf.Max(0, k - 1)] - points[Mathf.Min(RibbonSegments, k + 1)]; tangent.z = 0f;
                    tangent = tangent.sqrMagnitude > 1e-10f ? tangent.normalized : Vector3.up;
                    var normal = new Vector3(-tangent.y, tangent.x, 0f);
                    var width = s.trailWidth * cell * Mathf.Pow(1f - q, 0.8f) * Smooth(q / 0.04f + 0.3f);
                    trail.v[2 * k] = points[k] - normal * (width * 0.5f); trail.v[2 * k + 1] = points[k] + normal * (width * 0.5f);
                    trailGlow.v[2 * k] = points[k] - normal * (width * 1.6f); trailGlow.v[2 * k + 1] = points[k] + normal * (width * 1.6f);
                    var col = q < 0.35f ? Color.Lerp(Color.Lerp(player.white, player.gold, 0.35f), player.gold, q / 0.35f) : Color.Lerp(player.gold, player.magenta, (q - 0.35f) / 0.5f);
                    col.a = Mathf.Clamp01(s.trailAlpha * fade * moving * Mathf.Pow(1f - q, 0.6f));
                    trail.c[2 * k] = col; trail.c[2 * k + 1] = col;
                    var glowColor = Color.Lerp(new Color(1f, 0.6f, 0.15f), player.violet, Mathf.Clamp01(q * 1.3f));
                    glowColor.a = Mathf.Clamp01(s.trailGlowAlpha * fade * moving * Mathf.Pow(1f - q, 0.7f));
                    trailGlow.c[2 * k] = glowColor; trailGlow.c[2 * k + 1] = glowColor;
                }
                trail.mesh.vertices = trail.v; trail.mesh.colors = trail.c;
                trailGlow.mesh.vertices = trailGlow.v; trailGlow.mesh.colors = trailGlow.c;

                // Faíscas piscando ao longo do rastro
                for (var i = 0; i < sparkles.Length; i++)
                {
                    var q = (i + 0.5f) / sparkles.Length;
                    var at = Sample(t - q * s.trailSeconds) + new Vector3((Hash(i, 121) - 0.5f) * cell * 0.5f, (Hash(i, 122) - 0.5f) * cell * 0.5f, 0f);
                    var twinkle = 0.5f + 0.5f * Mathf.Sin(t * 17f + i * 2.4f);
                    var size = cell * 0.3f * (0.5f + 0.7f * Hash(i, 123)) * (0.6f + 0.4f * twinkle) * (1f - 0.5f * q);
                    Place(sparkles[i], at, size, size, A(i % 3 == 0 ? Color.Lerp(player.magenta, player.white, 0.4f) : Color.Lerp(player.gold, player.white, 0.4f), s.trailAlpha * fade * moving * twinkle * (1f - 0.6f * q)));
                }
            }
        }

        // ------------------------------------------------------------------------------------------ LINE CLEANER

        sealed class LineEffect : Effect
        {
            public Vector3 origin, endA, endB; public bool horizontal, stopA, stopB;
            public IList<Vector3> cells; public IList<Color> colors; public float cell;
            LineSettings s;

            sealed class Beam
            {
                public Vector3 dir; public float length, angle; public float startOffset;
                public SpriteRenderer tip, warm;
                public Ribbon halo, mid, core;
                public Ribbon[] smoke;
                public SpriteRenderer[] particles;
            }

            /// Molda uma fita reta atrás da frente do feixe: larga na frente, afunilando até a ponta da cauda.
            void Shape(Ribbon ribbon, Vector3 head, Vector3 dir, Vector3 perp, float length, float width, float taper, Color color, float alpha, float alphaFall,
                float softNose, float wobble, float wobblePhase, bool scroll, float uScale, float uOffset)
            {
                for (var k = 0; k <= RibbonSegments; k++)
                {
                    var q = k / (float)RibbonSegments;                     // 0 = frente, 1 = ponta da cauda
                    var w = width * Mathf.Pow(1f - q, taper) * (1f + wobble * Mathf.Sin(q * 11f + wobblePhase));
                    var at = head - dir * (q * length);
                    var half = perp * (w * 0.5f);
                    ribbon.v[2 * k] = at - half; ribbon.v[2 * k + 1] = at + half;
                    var a = alpha * Mathf.Pow(1f - q, alphaFall) * (softNose > 0f ? Smooth(q / softNose) : 1f);
                    var col = new Color(color.r, color.g, color.b, Mathf.Clamp01(a));
                    ribbon.c[2 * k] = col; ribbon.c[2 * k + 1] = col;
                    if (scroll) { ribbon.uv[2 * k] = new Vector2(q * uScale + uOffset, 0f); ribbon.uv[2 * k + 1] = new Vector2(q * uScale + uOffset, 1f); }
                }
                ribbon.mesh.vertices = ribbon.v; ribbon.mesh.colors = ribbon.c;
                if (scroll) ribbon.mesh.uv = ribbon.uv;
            }

            sealed class CellHit
            {
                public float time; public Vector3 position; public bool beamB;
                public SpriteRenderer ring, puff;
                public Shard[] shards;
            }

            SpriteRenderer dim;
            readonly List<Beam> beams = new List<Beam>();
            readonly List<CellHit> hits = new List<CellHit>();

            Beam MakeBeam(Vector3 end, bool stop)
            {
                var to = end - origin; to.z = 0f;
                var overshoot = stop ? 0.3f * cell : s.exitOvershootCells * cell;
                var dist = to.magnitude;
                if (dist < 0.01f) return null;
                var dir = to / dist;
                var beam = new Beam { dir = dir, length = dist + overshoot, angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg };
                if (dim == null) dim = Add(Solid, -2, false);
                beam.warm = Add(GlowDot, 0);
                beam.halo = player.MakeRibbon("Cauda (halo)", 1, objects);
                beam.smoke = new Ribbon[5];
                for (var i = 0; i < beam.smoke.Length; i++) beam.smoke[i] = player.MakeRibbon("Aura esfumaçada " + i, 2, objects, SmokeTexture);
                beam.mid = player.MakeRibbon("Cauda (meio)", 3, objects);
                beam.core = player.MakeRibbon("Cauda (núcleo neon)", 4, objects);
                beam.particles = new SpriteRenderer[s.particleCount];
                for (var i = 0; i < beam.particles.Length; i++) beam.particles[i] = Add(GlowDot, 5);
                beam.tip = Add(player.lineTip, 6);
                beams.Add(beam);
                return beam;
            }

            public override void Begin()
            {
                s = player.line;
                var beamA = MakeBeam(endA, stopA);
                var beamB = MakeBeam(endB, stopB);
                var longest = Mathf.Max(beamA != null ? beamA.length : 0f, beamB != null ? beamB.length : 0f);
                duration = longest / s.speed + s.retractSeconds + 0.35f;

                if (cells != null)
                    for (var i = 0; i < cells.Count; i++)
                    {
                        var rel = cells[i] - origin; rel.z = 0f;
                        var useB = beamB != null && (beamA == null || Vector3.Dot(rel, beamB.dir) >= 0f);
                        var beam = useB ? beamB : beamA;
                        if (beam == null) continue;
                        var along = Mathf.Max(0f, Vector3.Dot(rel, beam.dir));
                        var hit = new CellHit { time = along / s.speed, position = cells[i], beamB = useB };
                        hit.ring = Add(player.lineBreakRing, 6);
                        hit.puff = Add(player.dot, 7);
                        var color = colors != null && i < colors.Count && colors[i].a > 0.5f ? colors[i] : player.confettiColors[i % Mathf.Max(1, player.confettiColors.Length)];
                        hit.shards = new Shard[s.fragmentsPerCell];
                        for (var k = 0; k < hit.shards.Length; k++)
                        {
                            var perp = Quaternion.Euler(0, 0, 90f) * beam.dir;
                            var side = Hash(i * 13 + k, 71) < 0.5f ? 1f : -1f;
                            var dir = (perp * side + beam.dir * (Hash(i * 13 + k, 72) - 0.3f)).normalized;
                            var tone = Hash(i * 13 + k, 73);
                            hit.shards[k] = new Shard
                            {
                                sr = Add(player.Fragment(i + k), 9, false), origin = cells[i],
                                velocity = new Vector2(dir.x, dir.y) * s.fragmentSpeed * (0.5f + 0.8f * Hash(i * 13 + k, 74)),
                                spin = (Hash(i * 13 + k, 75) - 0.5f) * 720f, size = s.fragmentSize * (0.6f + 0.9f * Hash(i * 13 + k, 76)),
                                life = s.fragmentLife * (0.75f + 0.5f * Hash(i * 13 + k, 77)), delay = hit.time,
                                color = tone < 0.25f ? Color.Lerp(player.white, player.gold, Hash(i + k, 78)) : Color.Lerp(color, Color.white, 0.15f + 0.25f * Hash(i * 13 + k, 79)),
                            };
                        }
                        hits.Add(hit);
                    }
            }

            public override void Evaluate(float t)
            {
                var dimK = Smooth(t / 0.08f) * (1f - Smooth((t - (duration - 0.45f)) / 0.45f));
                PlaceDim(dim, player.ViewCam, origin.z, s.dimAlpha * dimK);
                foreach (var beam in beams) EvaluateBeam(beam, t);

                foreach (var hit in hits)
                {
                    var a = t - hit.time;
                    var rf = a / s.breakRingSeconds;
                    if (a >= 0f && rf <= 1f)
                    {
                        var size = Mathf.Lerp(0.35f, 1f, EaseOut(rf)) * s.breakRingSize * cell;
                        Place(hit.ring, hit.position, size, size, A(Color.Lerp(player.white, player.magenta, rf), 0.95f * (1f - rf)));
                        var puffSize = cell * 0.9f * (0.6f + 0.7f * rf);
                        Place(hit.puff, hit.position, puffSize, puffSize, A(Color.Lerp(player.white, player.gold, 0.4f), 0.7f * (1f - rf) * (1f - rf)));
                    }
                    else { hit.ring.color = A(Color.white, 0f); hit.puff.color = A(Color.white, 0f); }
                    for (var k = 0; k < hit.shards.Length; k++) UpdateShard(hit.shards[k], t, cell, s.fragmentGravity);
                }
            }

            void EvaluateBeam(Beam beam, float t)
            {
                var travelled = Mathf.Min(beam.length, s.speed * t);
                var arrived = beam.length / s.speed;
                var retract = Mathf.Clamp01((t - arrived) / s.retractSeconds);
                var tail = Mathf.Min(s.tailCells * cell, travelled) * (1f - Smooth(retract));
                var head = origin + beam.dir * travelled;
                var center = head - beam.dir * (tail * 0.5f);
                var perp = Quaternion.Euler(0, 0, 90f) * beam.dir;
                var fade = 1f - Smooth(retract);
                var pulse = 0.9f + 0.1f * Mathf.Sin(t * 30f);

                var length = Mathf.Max(0.01f, tail);
                var show = fade * Smooth(tail / (0.6f * cell));
                var deep = new Color(0.45f, 0.16f, 0.9f);

                // CAUDA como a da flecha: larga na frente e afunilando; halo translúcido, corpo dourado e centro neon
                Shape(beam.halo, head, beam.dir, perp, length, s.glowWidth * cell, 0.8f, Color.Lerp(player.gold, new Color(1f, 0.55f, 0.12f), 0.3f), s.glowAlpha * show * pulse, 0.55f, 0f, 0f, 0f, false, 0f, 0f);
                Shape(beam.mid, head, beam.dir, perp, length, s.coreWidth * 2.1f * cell, 0.85f, new Color(1f, 0.56f, 0.1f), 0.95f * s.coreAlpha * show, 0.35f, 0f, 0f, 0f, false, 0f, 0f);
                Shape(beam.core, head, beam.dir, perp, length, s.coreWidth * cell, 0.9f, Color.Lerp(player.white, player.gold, 0.12f), s.coreAlpha * show, 0.25f, 0f, 0f, 0f, false, 0f, 0f);

                // AURA esfumaçada por cima (roxo, roxo escuro e amarelo): desliza sem parar da frente até a ponta da cauda
                var scale = length / (cell * 3.2f);
                Shape(beam.smoke[0], head, beam.dir, perp, length, s.smokeWidth * cell, 0.7f, player.violet, s.smokeAlpha * show, 0.5f, 0.06f, 0.14f, t * 9f, true, scale, -t * s.smokeSpeed);
                Shape(beam.smoke[1], head, beam.dir, perp, length, s.smokeWidth * 1.25f * cell, 0.65f, deep, s.smokeAlpha * show, 0.45f, 0.08f, 0.18f, -t * 7f + 2f, true, scale * 0.7f, -t * s.smokeSpeed * 0.7f + 0.37f);
                Shape(beam.smoke[2], head, beam.dir, perp, length, s.smokeWidth * 0.7f * cell, 0.8f, Color.Lerp(player.gold, player.white, 0.15f), 0.4f * s.smokeAlpha * show, 0.6f, 0.05f, 0.12f, t * 12f + 4f, true, scale * 1.3f, -t * s.smokeSpeed * 1.35f + 0.71f);
                Place(beam.warm, head - beam.dir * (length * 0.25f), Mathf.Max(length * 0.9f, cell), s.glowWidth * cell * 1.5f, A(new Color(1f, 0.6f, 0.15f), 0.12f * show), beam.angle);

                // FOGO amarelo esfumaçado: duas camadas quentes mais perto do centro, deslizando mais rápido e tremulando
                var flicker = 0.85f + 0.15f * Mathf.Sin(t * 37f);
                Shape(beam.smoke[3], head, beam.dir, perp, length, s.fireWidth * cell, 0.75f, new Color(1f, 0.62f, 0.14f), s.fireAlpha * show * flicker, 0.45f, 0.04f, 0.2f, t * 15f + 1f, true, scale * 1.1f, -t * s.smokeSpeed * 1.7f + 0.19f);
                Shape(beam.smoke[4], head, beam.dir, perp, length, s.fireWidth * 0.62f * cell, 0.85f, new Color(1f, 0.86f, 0.35f), s.fireAlpha * show, 0.4f, 0.03f, 0.16f, -t * 18f + 3f, true, scale * 1.6f, -t * s.smokeSpeed * 2.2f + 0.53f);

                // PARTÍCULAS redondas pequenas: nascem no centro da cauda e saem para fora, em todo o comprimento
                for (var i = 0; i < beam.particles.Length; i++)
                {
                    var h1 = Hash(i, 111); var h2 = Hash(i, 112); var h3 = Hash(i, 113);
                    var phase = Mathf.Repeat(t * s.particleRate * (0.7f + 0.6f * h2) + h1 * 7f, 1f);
                    var q = Mathf.Clamp01(0.04f + 0.9f * h3 + 0.08f * phase);
                    var side = i % 2 == 0 ? 1f : -1f;
                    var offset = side * (0.04f + phase * s.particleSpread * (0.5f + 0.5f * h2)) * cell * (1f - 0.45f * q);
                    var size = s.particleSize * cell * (0.55f + 0.9f * h1) * (1f - 0.55f * phase);
                    var tone = h2 < 0.5f ? Color.Lerp(player.gold, player.white, 0.35f) : h2 < 0.8f ? player.white : Color.Lerp(player.magenta, player.white, 0.3f);
                    Place(beam.particles[i], head - beam.dir * (q * length) + perp * offset, size, size, A(tone, show * Mathf.Sin(Mathf.PI * Mathf.Pow(phase, 0.6f)) * (1f - 0.5f * q)));
                }

                // Frente de impacto: acompanha o ponto de limpeza
                var tipSize = s.tipSize * cell * (0.9f + 0.15f * Mathf.Sin(t * 26f));
                Place(beam.tip, head, tipSize, tipSize, A(Color.Lerp(player.white, player.gold, 0.2f), (1f - Smooth(retract * 3f)) * (t > 0f ? 1f : 0f)), t * 120f);
            }
        }
    }
}
