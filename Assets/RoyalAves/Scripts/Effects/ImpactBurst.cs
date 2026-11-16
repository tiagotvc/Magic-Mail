// Explosão 2D "Royal Postman" montada só com as máscaras de Assets/RoyalAves/Art/VFX/ImpactMasks: clarão, ondas de
// choque, raios, fragmentos coloridos e brilhos residuais animados por camada (nenhuma textura única com a explosão).
//
// Uso no gameplay:
//     ImpactBurst.Play(ImpactPreset.Potion, worldPosition, cellWorldSize, radiusInCells);
// Os presets são prefabs em Resources/Effects/Impact (ImpactBurst_Small, _Potion, _Chain): ajuste cores, quantidades e
// tamanhos no Inspector de cada um. Tudo é medido em "casas" do tabuleiro, então a explosão acompanha o zoom da fase.
// As instâncias são recicladas por um pool: nada é instanciado a cada explosão (use Prewarm para criar antes).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Effects
{
    public enum ImpactPreset { Small = 0, Potion = 1, Chain = 2 }

    [DisallowMultipleComponent]
    public sealed class ImpactBurst : MonoBehaviour
    {
        public const string ResourceFolder = "Effects/Impact/ImpactBurst_";

        [Serializable]
        public class Masks
        {
            public Sprite flashSoft, flashCore, ringSoft, ringThin, shockDisc, ringArc;
            public Sprite streakSoft, streakSharp;
            public Sprite sparkCross, sparkDot, microPop;
            [Tooltip("triangle, quad, chip, sliver, fleck, speck (as mais comuns primeiro não importa: sorteio uniforme).")]
            public Sprite[] fragments;
        }

        [Serializable]
        public class Settings
        {
            [Header("Tempo")]
            [Tooltip("Duração total em segundos. A linha do tempo (flash 0-60 ms, raios 25-160, anéis 50-300, fragmentos 40-420, brilhos 120-450) é escalada por duração/0,5.")]
            public float duration = 0.5f;

            [Header("Clarão central")]
            public Color flashColor = new Color(1f, 0.93f, 0.7f, 1f);
            [Tooltip("Diâmetro do brilho suave no pico, em casas.")] public float flashSize = 3.2f;
            [Tooltip("Diâmetro do núcleo branco, em casas.")] public float coreSize = 1.5f;

            [Header("Ondas de choque")]
            public Color ringColor = new Color(1f, 0.88f, 0.55f, 1f);
            [Tooltip("Raio final do anel maior, em casas. Play(..., radiusCells) sobrescreve (raio + 0,5).")] public float ringRadius = 3.4f;
            [Range(0f, 1f)] public float shockDiscAlpha = 0.32f;
            public bool useRingArc = true;

            [Header("Raios")]
            public Color rayColor = new Color(1f, 0.82f, 0.45f, 1f);
            public int rayCount = 14;
            [Tooltip("Comprimento mínimo/máximo de cada raio, em casas.")] public Vector2 rayLength = new Vector2(0.7f, 1.7f);
            [Tooltip("Até onde a ponta dos raios chega, em casas.")] public float rayTravel = 2.6f;
            [Tooltip("Espessura dos raios, em casas.")] public float rayThickness = 0.09f;

            [Header("Fragmentos")]
            public Vector2Int fragmentCount = new Vector2Int(55, 85);
            [Tooltip("Tamanho em fração da largura de uma casa (8% a 20%).")] public Vector2 fragmentSize = new Vector2(0.08f, 0.20f);
            [Tooltip("Distância percorrida, em casas.")] public Vector2 fragmentDistance = new Vector2(1.2f, 3.6f);
            [Tooltip("Cores saturadas dos fragmentos (vermelho, azul, verde, amarelo).")]
            public Color[] fragmentColors =
            {
                new Color(1f, 0.22f, 0.2f, 1f), new Color(0.2f, 0.5f, 1f, 1f),
                new Color(0.25f, 0.85f, 0.25f, 1f), new Color(1f, 0.8f, 0.1f, 1f),
            };

            [Header("Brilhos residuais")]
            public Color sparkColor = new Color(1f, 0.96f, 0.8f, 1f);
            public int sparkCount = 10;
        }

        [SerializeField] ImpactPreset preset = ImpactPreset.Potion;
        [SerializeField] Masks masks = new Masks();
        [Tooltip("Aditivo (clarão, anéis, raios, brilhos).")] [SerializeField] Material additiveMaterial;
        [Tooltip("Alpha normal (fragmentos, mantêm a cor saturada).")] [SerializeField] Material alphaMaterial;
        [SerializeField] Settings settings = new Settings();
        [Tooltip("Ordem de desenho dentro da camada 'Item mask' (acima das peças, abaixo da interface).")]
        [SerializeField] int sortingOrderBase = 100;

        public ImpactPreset Preset => preset;
        public Settings Config => settings;
        public bool Active { get; private set; }
        public float Age { get; private set; }
        public int PlayId { get; private set; }

        enum Kind { Flash, Core, Ring, Disc, Arc, Ray, Fragment, Spark }

        sealed class Part
        {
            public Kind kind;
            public Transform t;
            public SpriteRenderer r;
            public Vector2 spriteSize;
            public bool used;
            public float start, life, angle, spin, size, distance, extra;
            public Vector2 dir;
            public Color color;
        }

        readonly List<Part> parts = new List<Part>();
        ImpactBurst prefabOrigin;
        Vector3 center;
        float unit = 1f, ringRadiusWorld, scale = 1f;
        bool built;
        int built_rays, built_fragments, built_sparks;

        // ---------------------------------------------------------------- pool

        static readonly Dictionary<ImpactBurst, Stack<ImpactBurst>> pool = new Dictionary<ImpactBurst, Stack<ImpactBurst>>();
        static readonly Dictionary<ImpactPreset, ImpactBurst> presetPrefabs = new Dictionary<ImpactPreset, ImpactBurst>();
        static Transform poolRoot;
        static int playCounter;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { pool.Clear(); presetPrefabs.Clear(); poolRoot = null; playCounter = 0; }

        public static ImpactBurst LoadPreset(ImpactPreset preset)
        {
            if (presetPrefabs.TryGetValue(preset, out var cached) && cached != null) return cached;
            var prefab = Resources.Load<ImpactBurst>(ResourceFolder + preset);
            if (prefab == null) Debug.LogError("ImpactBurst prefab missing: Resources/" + ResourceFolder + preset);
            presetPrefabs[preset] = prefab;
            return prefab;
        }

        /// <param name="cellSize">Tamanho de uma casa em unidades do mundo.</param>
        /// <param name="radiusCells">Alcance da onda em casas (0 = o do preset).</param>
        public static ImpactBurst Play(ImpactPreset preset, Vector3 position, float cellSize, float radiusCells = 0f) =>
            Play(LoadPreset(preset), position, cellSize, radiusCells);

        public static ImpactBurst Play(ImpactBurst prefab, Vector3 position, float cellSize, float radiusCells = 0f)
        {
            if (prefab == null) return null;
            var burst = Rent(prefab);
            burst.Begin(position, cellSize, radiusCells);
            burst.gameObject.SetActive(true);
            return burst;
        }

        /// Cria as instâncias antes do primeiro uso (ex.: ao carregar a fase), para não gerar picos no meio do jogo.
        public static void Prewarm(ImpactPreset preset, int count)
        {
            var prefab = LoadPreset(preset);
            if (prefab == null) return;
            var made = new List<ImpactBurst>();
            for (var i = 0; i < count; i++) { var b = Rent(prefab); b.Build(); made.Add(b); }
            foreach (var b in made) b.Release();
        }

        static ImpactBurst Rent(ImpactBurst prefab)
        {
            if (!pool.TryGetValue(prefab, out var stack)) pool[prefab] = stack = new Stack<ImpactBurst>();
            while (stack.Count > 0)
            {
                var pooled = stack.Pop();
                if (pooled != null) return pooled;
            }
            if (poolRoot == null)
            {
                var root = new GameObject("ImpactBurstPool");
                if (Application.isPlaying) DontDestroyOnLoad(root);
                poolRoot = root.transform;
            }
            var instance = Instantiate(prefab, poolRoot);
            instance.name = prefab.name;
            instance.prefabOrigin = prefab;
            instance.gameObject.SetActive(false);
            return instance;
        }

        void Release()
        {
            Active = false;
            gameObject.SetActive(false);
            if (prefabOrigin == null || !Application.isPlaying) return;
            if (poolRoot != null) transform.SetParent(poolRoot, false);
            pool[prefabOrigin].Push(this);
        }

        // ---------------------------------------------------------------- build / begin

        void Build()
        {
            var needRays = Mathf.Max(0, settings.rayCount);
            var needFragments = Mathf.Max(settings.fragmentCount.x, settings.fragmentCount.y);
            var needSparks = Mathf.Max(0, settings.sparkCount);
            if (built && built_rays >= needRays && built_fragments >= needFragments && built_sparks >= needSparks) return;

            foreach (var part in parts) if (part.t != null) DestroyImmediateSafe(part.t.gameObject);
            parts.Clear();
            var order = 0;
            // Do fundo para a frente: disco, anéis, clarão, raios, fragmentos, brilhos.
            AddPart(Kind.Disc, masks.shockDisc, additiveMaterial, order++);
            AddPart(Kind.Ring, masks.ringSoft, additiveMaterial, order++);
            AddPart(Kind.Ring, masks.ringThin, additiveMaterial, order++);
            if (settings.useRingArc) AddPart(Kind.Arc, masks.ringArc, additiveMaterial, order++);
            AddPart(Kind.Flash, masks.flashSoft, additiveMaterial, order++);
            AddPart(Kind.Core, masks.flashCore, additiveMaterial, order++);
            for (var i = 0; i < needRays; i++) AddPart(Kind.Ray, i % 2 == 0 ? masks.streakSoft : masks.streakSharp, additiveMaterial, order++);
            for (var i = 0; i < needFragments; i++)
            {
                var frags = masks.fragments;
                if (frags == null || frags.Length == 0) break;
                AddPart(Kind.Fragment, frags[i % frags.Length], alphaMaterial, order++);
            }
            for (var i = 0; i < needSparks; i++)
            {
                var sprite = i % 3 == 0 ? masks.microPop : (i % 3 == 1 ? masks.sparkCross : masks.sparkDot);
                AddPart(Kind.Spark, sprite, additiveMaterial, order++);
            }
            built = true; built_rays = needRays; built_fragments = needFragments; built_sparks = needSparks;
        }

        static void DestroyImmediateSafe(GameObject go)
        {
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        void AddPart(Kind kind, Sprite sprite, Material material, int order)
        {
            if (sprite == null) return;
            var go = new GameObject(kind.ToString());
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            renderer.sortingOrder = sortingOrderBase + order;
            renderer.enabled = false;
            parts.Add(new Part { kind = kind, t = go.transform, r = renderer, spriteSize = sprite.bounds.size });
        }

        /// Prepara uma execução (sorteia direções, tempos e cores). O desenho começa no próximo Tick.
        public void Begin(Vector3 position, float cellSize, float radiusCells = 0f)
        {
            Build();
            var k = Mathf.Max(0.05f, settings.duration) / 0.5f;
            center = position;
            unit = Mathf.Max(0.01f, cellSize);
            ringRadiusWorld = (radiusCells > 0f ? radiusCells + 0.5f : settings.ringRadius) * unit;
            // Raios, fragmentos e brilhos acompanham o alcance, sem ficarem gigantes em explosões de tabuleiro inteiro.
            scale = radiusCells > 0f ? Mathf.Clamp((radiusCells + 0.5f) / Mathf.Max(0.5f, settings.ringRadius), 0.6f, 1.7f) : 1f;
            transform.position = position;
            Age = 0f;
            PlayId = ++playCounter;
            Active = true;

            int fragTotal = UnityEngine.Random.Range(settings.fragmentCount.x, settings.fragmentCount.y + 1);
            int rayIndex = 0, fragIndex = 0, sparkIndex = 0, ringIndex = 0;
            var colors = settings.fragmentColors != null && settings.fragmentColors.Length > 0
                ? settings.fragmentColors : new[] { Color.white };
            var rayOffset = UnityEngine.Random.value * 360f;

            foreach (var p in parts)
            {
                p.used = true;
                p.r.enabled = false;
                switch (p.kind)
                {
                    case Kind.Disc: p.start = 0.05f * k; p.life = 0.22f * k; break;
                    case Kind.Ring:
                        p.start = (0.05f + 0.04f * ringIndex) * k; p.life = (0.25f - 0.02f * ringIndex) * k;
                        p.extra = ringIndex == 0 ? 1f : 0.78f; ringIndex++; break;
                    case Kind.Arc:
                        p.start = 0.07f * k; p.life = 0.22f * k; p.angle = UnityEngine.Random.value * 360f;
                        p.spin = UnityEngine.Random.Range(180f, 320f) * (UnityEngine.Random.value < .5f ? -1f : 1f); break;
                    case Kind.Flash: p.start = 0f; p.life = 0.20f * k; break;
                    case Kind.Core: p.start = 0f; p.life = 0.13f * k; break;
                    case Kind.Ray:
                        {
                            var count = Mathf.Max(1, built_rays);
                            var inactive = rayIndex >= settings.rayCount;
                            p.used = !inactive;
                            var a = (rayIndex + UnityEngine.Random.Range(-0.35f, 0.35f)) / count * 360f + rayOffset;
                            p.angle = a; p.dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                            p.start = (0.025f + UnityEngine.Random.value * 0.045f) * k;
                            p.life = (0.07f + UnityEngine.Random.value * 0.065f) * k;
                            p.size = Mathf.Lerp(settings.rayLength.x, settings.rayLength.y, UnityEngine.Random.value);
                            p.distance = settings.rayTravel * Mathf.Lerp(0.7f, 1f, UnityEngine.Random.value);
                            p.extra = UnityEngine.Random.Range(0.25f, 0.5f);
                            rayIndex++; break;
                        }
                    case Kind.Fragment:
                        {
                            var inactive = fragIndex >= fragTotal;
                            p.used = !inactive;
                            var a = UnityEngine.Random.value * Mathf.PI * 2f;
                            p.dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                            p.start = (0.04f + Mathf.Pow(UnityEngine.Random.value, 2f) * 0.05f) * k;
                            p.life = Mathf.Min(0.42f * k - p.start, (0.22f + UnityEngine.Random.value * 0.16f) * k);
                            p.size = Mathf.Lerp(settings.fragmentSize.x, settings.fragmentSize.y, UnityEngine.Random.value);
                            p.distance = Mathf.Lerp(settings.fragmentDistance.x, settings.fragmentDistance.y, Mathf.Sqrt(UnityEngine.Random.value));
                            p.angle = UnityEngine.Random.value * 360f;
                            p.spin = UnityEngine.Random.Range(-540f, 540f);
                            p.color = colors[UnityEngine.Random.Range(0, colors.Length)];
                            fragIndex++; break;
                        }
                    case Kind.Spark:
                        {
                            var inactive = sparkIndex >= settings.sparkCount;
                            p.used = !inactive;
                            var a = UnityEngine.Random.value * Mathf.PI * 2f;
                            p.dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                            p.distance = Mathf.Lerp(0.35f, 0.85f, Mathf.Sqrt(UnityEngine.Random.value)) * ringRadiusWorld / unit;
                            p.start = (0.12f + UnityEngine.Random.value * 0.15f) * k;
                            p.life = Mathf.Min(0.45f * k - p.start, (0.12f + UnityEngine.Random.value * 0.1f) * k);
                            p.size = UnityEngine.Random.Range(0.14f, 0.32f);
                            p.angle = UnityEngine.Random.value * 90f;
                            sparkIndex++; break;
                        }
                }
            }
            Evaluate();
        }

        // ---------------------------------------------------------------- animation

        void Update()
        {
            if (!Active) return;
            Tick(Mathf.Min(Time.deltaTime, 0.05f));
        }

        /// Avança a animação. O Update chama sozinho; a ferramenta de preview do Editor chama fora do Play.
        public void Tick(float dt)
        {
            if (!Active) return;
            Age += dt;
            Evaluate();
            if (Age >= settings.duration) Release();
        }

        static float U(float t, float start, float life) => life <= 0f ? 1f : Mathf.Clamp01((t - start) / life);
        static float EaseOut(float u) => 1f - (1f - u) * (1f - u) * (1f - u);
        static float Fit(Vector2 spriteSize, float world) => world / Mathf.Max(0.0001f, Mathf.Max(spriteSize.x, spriteSize.y));

        void Evaluate()
        {
            var k = Mathf.Max(0.05f, settings.duration) / 0.5f;
            foreach (var p in parts)
            {
                if (!p.used || Age < p.start || Age > p.start + p.life + 0.0001f) { p.r.enabled = false; continue; }
                var u = U(Age, p.start, p.life);
                float alpha = 1f;
                var color = Color.white;
                var pos = Vector2.zero;
                var rot = 0f;
                var sx = 1f; var sy = 1f;
                switch (p.kind)
                {
                    case Kind.Flash:
                        {
                            var s = Fit(p.spriteSize, settings.flashSize * unit * scale) * Mathf.Lerp(0.35f, 1f, EaseOut(u));
                            sx = sy = s; color = settings.flashColor;
                            alpha = Age < 0.06f * k ? 1f : 1f - Mathf.SmoothStep(0f, 1f, U(Age, 0.06f * k, 0.14f * k));
                            break;
                        }
                    case Kind.Core:
                        {
                            var s = Fit(p.spriteSize, settings.coreSize * unit * scale) * Mathf.Lerp(0.5f, 1.15f, EaseOut(u));
                            sx = sy = s; color = new Color(1f, 1f, 0.95f, 1f);
                            alpha = Age < 0.045f * k ? 1f : 1f - U(Age, 0.045f * k, 0.085f * k);
                            break;
                        }
                    case Kind.Ring:
                    case Kind.Disc:
                    case Kind.Arc:
                        {
                            var radius = ringRadiusWorld * (p.kind == Kind.Disc ? 0.9f : p.kind == Kind.Arc ? 0.62f : p.extra);
                            var s = Fit(p.spriteSize, radius * 2f) * Mathf.Lerp(0.12f, 1f, EaseOut(u));
                            sx = sy = s;
                            color = settings.ringColor;
                            alpha = Mathf.Min(1f, u * 9f) * Mathf.Pow(1f - u, 1.5f);
                            if (p.kind == Kind.Disc) alpha *= settings.shockDiscAlpha;
                            if (p.kind == Kind.Arc) { rot = p.angle + p.spin * u; alpha *= 0.9f; }
                            break;
                        }
                    case Kind.Ray:
                        {
                            var e = EaseOut(u);
                            var length = p.size * unit * scale * (1f - 0.55f * u);
                            var offset = unit * scale * Mathf.Lerp(p.extra, p.distance, e);
                            pos = p.dir * offset;
                            rot = p.angle;
                            sx = length / Mathf.Max(0.0001f, p.spriteSize.x);
                            sy = settings.rayThickness * unit / Mathf.Max(0.0001f, p.spriteSize.y) * (1f - 0.4f * u);
                            color = settings.rayColor;
                            alpha = Mathf.Min(1f, u * 10f) * Mathf.Pow(1f - u, 1.2f);
                            break;
                        }
                    case Kind.Fragment:
                        {
                            // Acelera no começo e desacelera até parar; o alpha cai e a escala encolhe no fim.
                            var travel = EaseOut(u) * Mathf.SmoothStep(0f, 1f, u * 14f);
                            pos = p.dir * (p.distance * unit * scale * travel);
                            rot = p.angle + p.spin * u;
                            var s = Fit(p.spriteSize, p.size * unit);
                            s *= u < 0.12f ? Mathf.Lerp(0.4f, 1f, u / 0.12f) : 1f;
                            s *= u > 0.55f ? Mathf.Lerp(1f, 0.25f, (u - 0.55f) / 0.45f) : 1f;
                            sx = sy = s; color = p.color;
                            alpha = u > 0.6f ? 1f - (u - 0.6f) / 0.4f : 1f;
                            break;
                        }
                    case Kind.Spark:
                        {
                            var twinkle = Mathf.Sin(u * Mathf.PI);
                            pos = p.dir * (p.distance * unit);
                            rot = p.angle;
                            var s = Fit(p.spriteSize, p.size * unit) * (0.55f + 0.6f * twinkle);
                            sx = sy = s; color = settings.sparkColor; alpha = twinkle;
                            break;
                        }
                }
                p.t.localPosition = new Vector3(pos.x, pos.y, 0f);
                p.t.localRotation = Quaternion.Euler(0f, 0f, rot);
                p.t.localScale = new Vector3(sx, sy, 1f);
                color.a = Mathf.Clamp01(alpha);
                p.r.color = color;
                p.r.enabled = true;
            }
        }

        void OnDisable()
        {
            // Desativar no meio da animação (troca de cena) não deixa a instância "ativa" para sempre.
            if (Active && !gameObject.activeSelf) Active = false;
        }
    }
}
