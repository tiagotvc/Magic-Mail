// Efeito visual das dicas (estilo "mágico" do Royal Match): contorno rosa/azul que acompanha o perímetro externo das
// casas da dica, faíscas pequenas, brasas, luz que passa por dentro das peças e um empurrãozinho só na peça que
// precisa se mover. Não conhece o jogo: recebe casas, mapa do tabuleiro e peças em Play() e é totalmente
// determinístico em função do tempo (Evaluate(t)), por isso a janela de preview usa exatamente o mesmo código.
// Todos os parâmetros aparecem no Inspector do prefab Resources/Effects/Hint/HintEffect.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoyalAves.Effects
{
    /// Uma peça que participa da dica: o transform que recebe o empurrão e os sprites que "acendem".
    public class HintPiece
    {
        public Transform nudge;
        public SpriteRenderer[] sources;
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("Royal Aves/Hint Effect")]
    public class HintEffect : MonoBehaviour
    {
        [Header("Máscaras (Royal_Postman_Hint_VFX_Universal)")]
        public Sprite edgeCore;
        public Sprite edgeHalo;
        public Sprite sparkleFour;
        public Sprite sparkleSix;
        public Sprite sparkleTiny;
        public Sprite softDot;
        public Sprite cellGlow;
        public Sprite nudgeTrail;
        public Sprite nudgeGlow;
        public Material additive;

        [Header("Cores (mais místico: rosa + azul)")]
        public Color colorA = new Color(1f, 0.58f, 0.08f, 1f);
        public Color colorB = new Color(1f, 0.82f, 0.22f, 1f);
        public Color hotColor = new Color(1f, 0.96f, 0.72f, 1f);

        [Header("Contorno")]
        [Range(0f, 0.5f)] public float cornerRadius = 0.22f;
        [Min(0)] public float haloWidth = 0.42f;
        [Min(0)] public float coreWidth = 0.05f;
        [Range(0f, 1f)] public float haloAlpha = 0.55f;
        [Range(0f, 1f)] public float coreAlpha = 1f;
        [Range(0f, 1f)] public float coreWhiteMix = 0.45f;
        [Tooltip("Ciclos de rosa→azul por casa de caminho.")]
        public float colorWaves = 0.35f;
        [Tooltip("Velocidade com que as cores correm pelo contorno.")]
        public float flowSpeed = 0.35f;
        [Range(0f, 1f)] public float flickerAmount = 0.35f;
        public float flickerSpeed = 3.2f;
        public float flickerScale = 1.4f;

        [Header("Poeira mágica (grãos brilhantes ao longo da linha)")]
        [Min(0)] public float grainsPerCell = 48f;
        [Min(0)] public float grainSize = 0.06f;
        [Tooltip("Quanto os grãos se afastam da linha, em casas.")]
        public float grainSpread = 0.05f;
        [Range(0f, 1f)] public float grainAlpha = 0.7f;
        public Color grainYellow = new Color(1f, 0.86f, 0.3f, 1f);
        [Tooltip("Parte dos grãos que fica amarela (o resto é rosa/azul).")]
        [Range(0f, 1f)] public float yellowShare = 0.8f;
        [Tooltip("Quanto a linha ondula/treme (em casas) e quanto ela falha em pedaços (0 = linha lisa).")]
        public float wobble = 0.012f;
        [Range(0f, 1f)] public float raggedness = 0.2f;

        [Header("Fumaça de poeira (nuvenzinhas que acompanham e correm pela linha)")]
        [Min(0)] public float dustPerCell = 7f;
        [Min(0)] public float dustSize = 0.22f;
        [Tooltip("Velocidade em casas por segundo ao longo da linha.")]
        public float dustSpeed = 0.4f;
        [Min(0.2f)] public float dustLife = 1.4f;
        [Range(0f, 1f)] public float dustAlpha = 0.22f;
        [Tooltip("Afastamento da linha, em casas.")]
        public float dustSpread = 0.16f;

        [Header("Focos de luz que correm no contorno (o \"foguinho\")")]
        [Min(0)] public int hotSpots = 2;
        [Tooltip("Voltas por segundo (em fração do contorno).")]
        public float hotSpeed = 0.35f;
        [Tooltip("Comprimento do foco em casas.")]
        public float hotLength = 0.35f;
        [Range(0f, 1f)] public float hotMix = 0.85f;
        public float hotWidthBoost = 1.1f;
        [Min(0)] public float hotHeadSize = 0.22f;

        [Header("Sparkles (brilhos em forma de estrela)")]
        [Min(0)] public int sparkleCount = 3;
        [Min(0)] public float sparkleSize = 0.26f;
        [Min(0.1f)] public float sparklePeriod = 1.25f;
        [Range(0f, 1f)] public float sparkleAlpha = 0.95f;
        [Tooltip("Desligado: os sparkles são pontinhos de luz. Ligado: parte deles vira cruz com raios.")]
        public bool useStarShape = false;

        [Header("Brasas (pontinhos que sobem do contorno)")]
        [Min(0)] public int emberCount = 10;
        [Min(0)] public float emberSize = 0.07f;
        [Min(0.1f)] public float emberLife = 0.95f;
        [Tooltip("Distância percorrida, em casas.")]
        public float emberDrift = 0.3f;
        [Range(0f, 1f)] public float emberAlpha = 0.9f;

        [Header("Luz dentro das peças")]
        [Range(0f, 1f)] public float shineIntensity = 0.5f;
        [Min(0.2f)] public float shinePeriod = 1.4f;
        [Tooltip("Atraso da onda de luz de uma peça para a seguinte (fração do período).")]
        [Range(0f, 1f)] public float shineStagger = 0.18f;
        public Color shineColor = new Color(1f, 0.9f, 0.97f, 1f);
        [Tooltip("Quantas posições acima do sprite da peça a luz é desenhada.")]
        public int shineOrderOffset = 20;

        [Header("Empurrão da peça (sem trocar de verdade)")]
        [Range(0f, 0.5f)] public float nudgeDistance = 0.19f;
        [Min(0.1f)] public float nudgeDuration = 0.55f;
        [Min(0.1f)] public float nudgePeriod = 1.5f;
        [Min(0)] public float nudgeStart = 0.35f;
        [Range(0f, 1f)] public float trailAlpha = 0.6f;
        [Range(0f, 1f)] public float nudgeGlowAlpha = 0.55f;

        [Header("Tempo e ordem de desenho")]
        [Min(0)] public float fadeIn = 0.25f;
        [Min(0)] public float fadeOut = 0.35f;
        public string sortingLayer = "Item mask";
        public int sortingOrder = 60;

        // ------------------------------------------------------------------------------------------ estado

        class PathLoop
        {
            public Vector3[] pts, normals;
            public float[] frac, dist;     // posição relativa (0..1) e distância em casas
            public float length, lengthCells;
            public int vertexStart;
        }

        class Ribbon
        {
            public Mesh mesh;
            public MeshRenderer renderer;
            public Vector3[] v;
            public Color[] c;
            public Vector2[] uv;
        }

        class Shine { public SpriteRenderer overlay, source; public int piece; }

        readonly List<PathLoop> loops = new List<PathLoop>();
        readonly List<Shine> shines = new List<Shine>();
        readonly List<SpriteRenderer> heads = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> sparkles = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> embers = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> grains = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> dust = new List<SpriteRenderer>();
        Ribbon halo, core;
        float[] pointHot, pointWave, pointFlick;
        SpriteRenderer trail, trailGlow;
        HintGrid grid;
        float cell, totalLength;
        float duration;
        List<HintPiece> pieces;
        int moverIndex;
        Vector3 moveDir;
        Vector3 nudgeBase;
        bool playing;
        MaterialPropertyBlock block;

        public bool IsPlaying => playing;

        // ------------------------------------------------------------------------------------------ API

        /// cells: casas do contorno (o conjunto final do match). pieces: peças que acendem; moverIndex: qual delas
        /// escorrega (-1 = nenhuma) na direção moveDir (mundo). duration: quanto tempo esta dica fica na tela.
        public void Play(IEnumerable<Vector2Int> cells, HintGrid map, List<HintPiece> hintPieces, int mover, Vector3 direction, float seconds)
        {
            Stop();
            grid = map;
            cell = Mathf.Max(0.0001f, map.Cell);
            pieces = hintPieces ?? new List<HintPiece>();
            moverIndex = mover;
            moveDir = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.zero;
            duration = Mathf.Max(0.5f, seconds);
            block = new MaterialPropertyBlock();

            BuildPath(cells);
            halo = BuildRibbon("Halo", edgeHalo, 0);
            core = BuildRibbon("Núcleo", edgeCore, 1);
            for (var i = 0; i < loops.Count * hotSpots; i++) heads.Add(NewSprite("Foco", Dot, 6));
            for (var i = 0; i < sparkleCount; i++) sparkles.Add(NewSprite("Sparkle", sparkleFour, 8));
            for (var i = 0; i < emberCount; i++) embers.Add(NewSprite("Brasa", Dot, 4));
            var grainTotal = Mathf.Min(1500, Mathf.RoundToInt(grainsPerCell * totalLength / cell));
            for (var i = 0; i < grainTotal; i++) grains.Add(NewSprite("Grão", Dot, 5));
            var dustTotal = Mathf.Min(400, Mathf.RoundToInt(dustPerCell * totalLength / cell));
            for (var i = 0; i < dustTotal; i++) dust.Add(NewSprite("Poeira", Dot, -2));
            BuildShines();
            if (moverIndex >= 0 && moverIndex < pieces.Count && pieces[moverIndex].nudge != null)
            {
                nudgeBase = pieces[moverIndex].nudge.localPosition;
                trailGlow = NewSprite("Brilho do empurrão", nudgeGlow, 2);
                trail = NewSprite("Rastro do empurrão", nudgeTrail, 3);
            }
            playing = true;
            Evaluate(0f);
        }

        /// Remove tudo e devolve a peça que escorregou para o lugar.
        public void Stop()
        {
            if (moverIndex >= 0 && pieces != null && moverIndex < pieces.Count && pieces[moverIndex].nudge != null)
                pieces[moverIndex].nudge.localPosition = nudgeBase;
            playing = false;
            moverIndex = -1;
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            foreach (var shine in shines)
                if (shine.overlay != null)
                {
                    if (Application.isPlaying) Destroy(shine.overlay.gameObject); else DestroyImmediate(shine.overlay.gameObject);
                }
            if (halo != null && halo.mesh != null) DestroyMesh(halo.mesh);
            if (core != null && core.mesh != null) DestroyMesh(core.mesh);
            halo = core = null;
            loops.Clear(); shines.Clear(); heads.Clear(); sparkles.Clear(); embers.Clear(); grains.Clear(); dust.Clear();
            trail = trailGlow = null;
        }

        static void DestroyMesh(Mesh mesh)
        {
            if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
        }

        void OnDisable() { if (playing) Stop(); }

        float lastT, lastEnvelope;

        // O Animator da peça roda depois do Update e sobrescreve a posição; reaplicamos o empurrão e a luz depois dele.
        void LateUpdate()
        {
            if (!playing || !Application.isPlaying) return;
            UpdateShines(lastT, lastEnvelope);
            UpdateNudge(lastT, lastEnvelope);
        }

        // ------------------------------------------------------------------------------------------ construção

        void BuildPath(IEnumerable<Vector2Int> cells)
        {
            loops.Clear();
            var spacing = cell * 0.1f;
            var index = 0;
            foreach (var lattice in HintShape.Loops(cells))
            {
                var pts = HintShape.Smooth(lattice, grid, cornerRadius * cell, 5, spacing);
                if (pts.Count < 8) continue;
                var loop = new PathLoop { pts = pts.ToArray() };
                var n = loop.pts.Length;
                loop.normals = new Vector3[n];
                loop.frac = new float[n];
                loop.dist = new float[n];
                var acc = 0f;
                for (var i = 0; i < n; i++)
                {
                    loop.dist[i] = acc / cell;
                    loop.frac[i] = 0f;
                    acc += Vector3.Distance(loop.pts[i], loop.pts[(i + 1) % n]);
                    var tangent = (loop.pts[(i + 1) % n] - loop.pts[(i + n - 1) % n]).normalized;
                    var right = new Vector3(tangent.y, -tangent.x, 0f);
                    loop.normals[i] = grid.Mirrored ? -right : right;   // as casas ficam sempre à esquerda; "fora" é a direita
                }
                loop.length = acc;
                loop.lengthCells = acc / cell;
                for (var i = 0; i < n; i++) loop.frac[i] = loop.dist[i] * cell / Mathf.Max(acc, 1e-5f);
                loops.Add(loop);
                index++;
            }
            totalLength = loops.Sum(l => l.length);
            var pointTotal = loops.Sum(l => l.pts.Length);
            pointHot = new float[pointTotal];
            pointWave = new float[pointTotal];
            pointFlick = new float[pointTotal];
        }

        // Pontinho redondo e macio gerado em código (a máscara do kit tinha pontas que viravam riscos).
        static Sprite dotSprite;
        static Sprite Dot
        {
            get
            {
                if (dotSprite != null) return dotSprite;
                const int n = 64;
                var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (var y = 0; y < n; y++)
                    for (var x = 0; x < n; x++)
                    {
                        var dx = (x + 0.5f) / n * 2f - 1f; var dy = (y + 0.5f) / n * 2f - 1f;
                        var d2 = dx * dx + dy * dy;
                        texture.SetPixel(x, y, new Color(1, 1, 1, d2 >= 1f ? 0f : Mathf.Exp(-d2 * 4.5f)));
                    }
                texture.Apply();
                dotSprite = Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
                dotSprite.hideFlags = HideFlags.HideAndDontSave;
                return dotSprite;
            }
        }

        static Texture2D haloProfile, coreProfile;

        static Texture2D Profile(int order)
        {
            ref var cache = ref (order == 0 ? ref haloProfile : ref coreProfile);
            if (cache != null) return cache;
            const int h = 64;
            var k = order == 0 ? 3.2f : 7f;
            var texture = new Texture2D(4, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (var y = 0; y < h; y++)
            {
                var v = (y + 0.5f) / h * 2f - 1f;
                var a = Mathf.Exp(-v * v * k) * Mathf.Clamp01((1f - Mathf.Abs(v)) * 6f);
                for (var x = 0; x < 4; x++) texture.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            texture.Apply();
            cache = texture;
            return texture;
        }

        Ribbon BuildRibbon(string label, Sprite sprite, int order)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            go.hideFlags = HideFlags.DontSave;
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = additive;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = sortingOrder + order;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            // Perfil gerado em código (borda suave, uniforme ao longo da linha): as máscaras de borda do kit têm pontas
            // que apareciam como riscos repetidos no contorno.
            block.Clear();
            block.SetTexture("_MainTex", Profile(order));
            renderer.SetPropertyBlock(block);

            var vertexCount = 0;
            foreach (var loop in loops) { loop.vertexStart = vertexCount; vertexCount += 2 * (loop.pts.Length + 1); }
            var ribbon = new Ribbon
            {
                mesh = new Mesh { name = "HintRibbon" },
                renderer = renderer,
                v = new Vector3[vertexCount],
                c = new Color[vertexCount],
                uv = new Vector2[vertexCount],
            };
            ribbon.mesh.MarkDynamic();
            var tris = new List<int>();
            foreach (var loop in loops)
            {
                var n = loop.pts.Length;
                for (var i = 0; i <= n; i++)
                {
                    var point = i % n;
                    var u = (i == n ? loop.lengthCells : loop.dist[point]) * 0.5f;
                    ribbon.uv[loop.vertexStart + 2 * i] = new Vector2(u, 0f);
                    ribbon.uv[loop.vertexStart + 2 * i + 1] = new Vector2(u, 1f);
                }
                for (var i = 0; i < n; i++)
                {
                    var a = loop.vertexStart + 2 * i;
                    tris.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 });
                }
            }
            ribbon.mesh.vertices = ribbon.v;
            ribbon.mesh.uv = ribbon.uv;
            ribbon.mesh.colors = ribbon.c;
            ribbon.mesh.triangles = tris.ToArray();
            ribbon.mesh.bounds = new Bounds(grid.origin, Vector3.one * 200f);
            filter.sharedMesh = ribbon.mesh;
            return ribbon;
        }

        SpriteRenderer NewSprite(string label, Sprite sprite, int order)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            go.hideFlags = HideFlags.DontSave;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = additive;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = sortingOrder + order;
            renderer.color = new Color(1, 1, 1, 0);
            return renderer;
        }

        void BuildShines()
        {
            for (var p = 0; p < pieces.Count; p++)
            {
                var sources = pieces[p].sources;
                if (sources == null) continue;
                foreach (var source in sources)
                {
                    if (source == null) continue;
                    var go = new GameObject("Luz da dica");
                    go.transform.SetParent(source.transform, false);
                    go.hideFlags = HideFlags.DontSave;
                    var overlay = go.AddComponent<SpriteRenderer>();
                    overlay.sharedMaterial = additive;
                    overlay.sprite = source.sprite;
                    overlay.color = new Color(1, 1, 1, 0);
                    shines.Add(new Shine { overlay = overlay, source = source, piece = p });
                }
            }
        }

        // ------------------------------------------------------------------------------------------ animação

        static float Smooth01(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

        static float Hash(int a, int b)
        {
            unchecked
            {
                var x = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ 0x9E3779B9u;
                x ^= x >> 13; x *= 0x5bd1e995u; x ^= x >> 15;
                return (x & 0xFFFFFF) / 16777216f;
            }
        }

        void SampleLoop(PathLoop loop, float s, out Vector3 position, out Vector3 normal)
        {
            var n = loop.pts.Length;
            var f = Mathf.Repeat(s, 1f) * n;
            var i = Mathf.Min(n - 1, Mathf.FloorToInt(f));
            var k = f - i;
            position = Vector3.Lerp(loop.pts[i], loop.pts[(i + 1) % n], k);
            normal = Vector3.Slerp(loop.normals[i], loop.normals[(i + 1) % n], k);
        }

        void Sample(float u, out Vector3 position, out Vector3 normal)
        {
            var target = Mathf.Repeat(u, 1f) * totalLength;
            foreach (var loop in loops)
            {
                if (target <= loop.length) { SampleLoop(loop, target / Mathf.Max(loop.length, 1e-5f), out position, out normal); return; }
                target -= loop.length;
            }
            var last = loops[loops.Count - 1];
            SampleLoop(last, 0f, out position, out normal);
        }

        public void Evaluate(float t)
        {
            if (!playing || loops.Count == 0) return;
            var envelope = (fadeIn > 0 ? Smooth01(t / fadeIn) : 1f) * (fadeOut > 0 ? Smooth01((duration - t) / fadeOut) : 1f);

            ComputePointFields(t);
            UpdateRibbon(halo, haloWidth, haloAlpha * envelope, 0f);
            UpdateRibbon(core, coreWidth, coreAlpha * envelope, coreWhiteMix);
            UpdateHeads(t, envelope);
            UpdateSparkles(t, envelope);
            UpdateEmbers(t, envelope);
            UpdateGrains(t, envelope);
            UpdateDust(t, envelope);
            lastT = t; lastEnvelope = envelope;
            UpdateShines(t, envelope);
            UpdateNudge(t, envelope);
        }

        float hotTime;

        void ComputePointFields(float t)
        {
            hotTime = t;
            var offset = 0;
            for (var li = 0; li < loops.Count; li++)
            {
                var loop = loops[li];
                for (var i = 0; i < loop.pts.Length; i++)
                {
                    var hot = 0f;
                    for (var k = 0; k < hotSpots; k++)
                    {
                        var center = Mathf.Repeat(t * hotSpeed / Mathf.Max(0.5f, loop.lengthCells) * 3f + k / (float)hotSpots + li * 0.37f, 1f);
                        var d = Mathf.Abs(loop.frac[i] - center);
                        d = Mathf.Min(d, 1f - d) * loop.lengthCells;
                        hot += Mathf.Exp(-(d / Mathf.Max(0.01f, hotLength)) * (d / Mathf.Max(0.01f, hotLength)));
                    }
                    pointHot[offset + i] = Mathf.Min(1f, hot);
                    pointWave[offset + i] = 0.5f + 0.5f * Mathf.Sin(6.2831853f * (loop.dist[i] * colorWaves - t * flowSpeed));
                    pointFlick[offset + i] = Mathf.PerlinNoise(loop.dist[i] * flickerScale + 11.3f * li, t * flickerSpeed);
                }
                offset += loop.pts.Length;
            }
        }

        void UpdateRibbon(Ribbon ribbon, float widthCells, float alpha, float whiteMix)
        {
            if (ribbon == null) return;
            var offset = 0;
            foreach (var loop in loops)
            {
                var n = loop.pts.Length;
                for (var i = 0; i <= n; i++)
                {
                    var p = i % n;
                    var hot = pointHot[offset + p];
                    var flick = pointFlick[offset + p];
                    var width = widthCells * cell * (1f + flickerAmount * (flick - 0.5f) * 2f + hot * hotWidthBoost);
                    var color = Color.Lerp(colorA, colorB, pointWave[offset + p]);
                    color = Color.Lerp(color, hotColor, Mathf.Max(whiteMix, hot * hotMix));
                    var broken = 1f - raggedness * (1f - Mathf.PerlinNoise(loop.dist[p] * 7f + 40f, Time.time * 0f + pointFlick[offset + p] * 0f + hotTime * 1.5f));
                    color.a = alpha * (1f - 0.35f * flickerAmount * (1f - flick)) * broken;
                    var center = loop.pts[p] + loop.normals[p] * ((Mathf.PerlinNoise(loop.dist[p] * 5f, hotTime * 0.9f + 3f) - 0.5f) * 2f * wobble * cell);
                    var normal = loop.normals[p] * (width * 0.5f);
                    ribbon.v[loop.vertexStart + 2 * i] = center - normal;
                    ribbon.v[loop.vertexStart + 2 * i + 1] = center + normal;
                    ribbon.c[loop.vertexStart + 2 * i] = color;
                    ribbon.c[loop.vertexStart + 2 * i + 1] = color;
                }
                offset += n;
            }
            ribbon.mesh.vertices = ribbon.v;
            ribbon.mesh.colors = ribbon.c;
        }

        void UpdateHeads(float t, float envelope)
        {
            var index = 0;
            for (var li = 0; li < loops.Count; li++)
                for (var k = 0; k < hotSpots; k++)
                {
                    var loop = loops[li];
                    var center = Mathf.Repeat(t * hotSpeed / Mathf.Max(0.5f, loop.lengthCells) * 3f + k / (float)hotSpots + li * 0.37f, 1f);
                    SampleLoop(loop, center, out var position, out _);
                    var head = heads[index++];
                    head.transform.position = position;
                    var pulse = 0.85f + 0.15f * Mathf.Sin(t * 9f + k * 2f);
                    head.transform.localScale = Vector3.one * (hotHeadSize * cell * pulse * SpriteScale(head.sprite));
                    var color = Color.Lerp(hotColor, colorA, 0.25f);
                    color.a = 0.95f * envelope;
                    head.color = color;
                }
        }

        void UpdateSparkles(float t, float envelope)
        {
            for (var i = 0; i < sparkles.Count; i++)
            {
                var phase = t / sparklePeriod + i / (float)Mathf.Max(1, sparkleCount);
                var cycle = Mathf.FloorToInt(phase);
                var f = phase - cycle;
                var seed = i * 131 + cycle;
                Sample(Hash(seed, 3), out var position, out var normal);
                var shape = Hash(seed, 5);
                var sprite = useStarShape && shape < 0.5f ? sparkleTiny : Dot;   // por padrão: pontinhos de luz, sem os riscos em cruz
                var envelopeLocal = Mathf.Sin(Mathf.PI * f);
                envelopeLocal *= envelopeLocal;
                var s = sparkles[i];
                s.sprite = sprite;
                s.transform.position = position + normal * (cell * 0.02f);
                s.transform.localRotation = Quaternion.Euler(0, 0, f * 90f + Hash(seed, 9) * 45f);
                s.transform.localScale = Vector3.one * (sparkleSize * cell * (0.4f + 0.6f * envelopeLocal) * SpriteScale(sprite));
                var color = Color.Lerp(hotColor, Hash(seed, 11) < 0.5f ? colorA : colorB, 0.3f);
                color.a = sparkleAlpha * envelopeLocal * envelope;
                s.color = color;
            }
        }

        void UpdateDust(float t, float envelope)
        {
            var travel = dustSpeed * cell / Mathf.Max(totalLength, 1e-5f);
            for (var i = 0; i < dust.Count; i++)
            {
                var phase = t / dustLife + Hash(i, 31);
                var cycle = Mathf.FloorToInt(phase);
                var f = phase - cycle;
                var seed = i * 7919 + cycle;
                Sample(Hash(seed, 32) + f * dustLife * travel, out var position, out var normal);
                var side = (Hash(seed, 33) - 0.5f) * 2f * dustSpread * cell + f * cell * 0.06f;
                var d = dust[i];
                d.transform.position = position + normal * side + Vector3.up * (f * cell * 0.05f);
                var size = dustSize * cell * (0.5f + Hash(seed, 34)) * (0.7f + 0.7f * f);
                d.transform.localScale = Vector3.one * (size * SpriteScale(Dot));
                var color = Color.Lerp(grainYellow, Hash(seed, 35) < 0.3f ? colorA : hotColor, 0.35f);
                var fade = Mathf.Sin(Mathf.PI * f);
                color.a = dustAlpha * fade * envelope;
                d.color = color;
            }
        }

        void UpdateGrains(float t, float envelope)
        {
            for (var i = 0; i < grains.Count; i++)
            {
                var baseU = (i + Hash(i, 21)) / grains.Count;
                Sample(baseU + t * 0.015f * (0.5f + Hash(i, 22)), out var position, out var normal);
                var tw = 0.5f + 0.5f * Mathf.Sin(6.2831853f * (t * (0.6f + 1.6f * Hash(i, 23)) + Hash(i, 24)));
                tw = tw * tw;
                var spread = (Hash(i, 25) - 0.5f) * 2f * grainSpread * cell;
                var g = grains[i];
                g.transform.position = position + normal * spread;
                g.transform.localScale = Vector3.one * (grainSize * cell * (0.4f + 2.4f * Hash(i, 26) * Hash(i, 26) * Hash(i, 26)) * (0.6f + 0.6f * tw) * SpriteScale(Dot));
                var pick = Hash(i, 27);
                var color = pick < yellowShare ? Color.Lerp(grainYellow, hotColor, 0.25f * Hash(i, 28))
                    : Color.Lerp(pick < yellowShare + (1f - yellowShare) * 0.5f ? colorA : colorB, hotColor, 0.6f + 0.4f * Hash(i, 28));
                color.a = grainAlpha * tw * envelope;
                g.color = color;
            }
        }

        void UpdateEmbers(float t, float envelope)
        {
            for (var i = 0; i < embers.Count; i++)
            {
                var phase = t / emberLife + Hash(i, 7);
                var cycle = Mathf.FloorToInt(phase);
                var f = phase - cycle;
                var seed = i * 977 + cycle;
                Sample(Hash(seed, 1), out var position, out var normal);
                var drift = emberDrift * cell * f;
                var sway = Mathf.Sin((f + Hash(seed, 2)) * 9f) * cell * 0.03f;
                var e = embers[i];
                e.transform.position = position + normal * (drift + sway) + Vector3.up * (drift * 0.35f);
                e.transform.localScale = Vector3.one * (emberSize * cell * (1f - 0.45f * f) * SpriteScale(Dot));
                var color = Color.Lerp(Hash(seed, 3) < 0.5f ? colorA : colorB, hotColor, 0.55f);
                color.a = emberAlpha * Mathf.Sin(Mathf.PI * f) * envelope;
                e.color = color;
            }
        }

        void UpdateShines(float t, float envelope)
        {
            foreach (var shine in shines)
            {
                if (shine.overlay == null) continue;
                var source = shine.source;
                var visible = source != null && source.enabled && source.gameObject.activeInHierarchy && source.sprite != null;
                shine.overlay.enabled = visible;
                if (!visible) continue;
                shine.overlay.sprite = source.sprite;
                shine.overlay.flipX = source.flipX;
                shine.overlay.flipY = source.flipY;
                shine.overlay.sortingLayerID = source.sortingLayerID;
                shine.overlay.sortingOrder = source.sortingOrder + shineOrderOffset;
                var wave = 0.5f + 0.5f * Mathf.Sin(6.2831853f * (t / shinePeriod - shine.piece * shineStagger) - 1.5708f);
                wave = wave * wave * (3f - 2f * wave);
                var color = Color.Lerp(shineColor, colorA, 0.25f * wave);
                color.a = shineIntensity * wave * envelope;
                shine.overlay.color = color;
            }
        }

        void UpdateNudge(float t, float envelope)
        {
            if (trail == null || moverIndex < 0 || moverIndex >= pieces.Count) return;
            var mover = pieces[moverIndex].nudge;
            if (mover == null) return;
            var local = Mathf.Repeat(t - nudgeStart, nudgePeriod);
            var w = t >= nudgeStart && local < nudgeDuration ? Mathf.Sin(Mathf.PI * local / nudgeDuration) : 0f;
            w = w * w * (3f - 2f * w);
            var offsetWorld = moveDir * (cell * nudgeDistance * w);
            var parent = mover.parent;
            var offsetLocal = parent != null ? parent.InverseTransformVector(offsetWorld) : offsetWorld;
            mover.localPosition = nudgeBase + offsetLocal;

            var angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
            trail.transform.position = mover.position - moveDir * (cell * 0.38f);
            trail.transform.rotation = Quaternion.Euler(0, 0, angle);
            trail.transform.localScale = new Vector3(cell * 1.0f / Mathf.Max(0.01f, SpriteWidth(trail.sprite)), cell * 0.5f / Mathf.Max(0.01f, SpriteHeight(trail.sprite)), 1f);
            var trailColor = Color.Lerp(colorA, hotColor, 0.5f);
            trailColor.a = trailAlpha * w * envelope;
            trail.color = trailColor;

            trailGlow.transform.position = mover.position;
            trailGlow.transform.localScale = Vector3.one * (cell * 1.05f * SpriteScale(trailGlow.sprite));
            var glowColor = Color.Lerp(colorB, hotColor, 0.35f);
            glowColor.a = nudgeGlowAlpha * w * envelope;
            trailGlow.color = glowColor;
        }

        // Escala para que o sprite ocupe 1 unidade de mundo, qualquer que seja o pixels-per-unit da importação.
        static float SpriteScale(Sprite sprite) => sprite == null ? 1f : 1f / Mathf.Max(0.01f, sprite.bounds.size.x);
        static float SpriteWidth(Sprite sprite) => sprite == null ? 1f : sprite.bounds.size.x;
        static float SpriteHeight(Sprite sprite) => sprite == null ? 1f : sprite.bounds.size.y;
    }
}
