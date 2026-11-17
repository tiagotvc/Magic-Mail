// Brilho dourado "mágico" para a UI, reutilizável (booster, item desbloqueado, qualquer ícone): coloque este componente
// num RectTransform ATRÁS da imagem do objeto. Ele cria sozinho, como filhos escondidos, quatro camadas feitas com as
// máscaras de VFX que o projeto já tem:
//   1. glow radial macio (pulsa em escala e opacidade);
//   2. raios finos saindo do centro (cada um com comprimento e intensidade próprios, girando devagar);
//   3. sparkles de tamanhos diferentes, que nascem e somem em tempos diferentes (alguns sobem um pouquinho);
//   4. bolinhas de luz opcionais que flutuam para cima (usadas atrás do botão selecionado).
// Nada é aplicado sobre o sprite do objeto. Tudo é função do tempo (Evaluate), então roda igual no jogo e no editor.
// Todas as cores, quantidades, tamanhos, intensidades e velocidades estão no Inspector.
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Effects
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Royal Aves/UI Magic Glow")]
    public class UIMagicGlow : MonoBehaviour
    {
        [Header("Máscaras (Royal Postman Art/VFX/ImpactMasks) e material aditivo")]
        public Sprite glowSprite;
        public Sprite raySprite;
        public Sprite sparkleStar;
        public Sprite sparkleDot;
        [Tooltip("Máscara dos anéis (círculos), ex.: ring_soft.")]
        public Sprite ringSprite;
        public Material additive;

        [Header("Cores")]
        public Color glowColor = new Color(1f, 0.74f, 0.2f, 1f);
        public Color rayColor = new Color(1f, 0.88f, 0.45f, 1f);
        public Color sparkleColor = new Color(1f, 0.93f, 0.62f, 1f);

        [Header("1. Glow radial")]
        [Tooltip("Tamanho do glow, em múltiplos do menor lado deste retângulo.")]
        [Min(0)] public float glowSize = 1f;
        [Range(0f, 1f)] public float glowAlpha = 0.75f;
        [Tooltip("Quanto a escala pulsa (0.06 = 6%).")]
        [Range(0f, 0.5f)] public float glowPulse = 0.07f;
        [Tooltip("Pulsos por segundo.")]
        [Min(0)] public float glowSpeed = 0.7f;

        [Tooltip("Segundo glow, menor e mais quente, no centro: dá o brilho forte \"estourado\" (0 = desligado).")]
        [Range(0f, 1f)] public float innerGlowAlpha = 0f;
        [Tooltip("Tamanho do glow central, em múltiplos do glow principal.")]
        [Range(0.1f, 1f)] public float innerGlowSize = 0.5f;

        [Header("2. Raios")]
        [Tooltip("Liga: os raios são espinhos gerados em código (grossos na base, afinando até a ponta), então a espessura \"Ray Width\" " +
                 "é a espessura de verdade. Desligado: usa a máscara Ray Sprite (a máscara tem um fio fino no meio da imagem).")]
        public bool proceduralRays = true;
        [Min(0)] public int rayCount = 10;
        [Tooltip("Comprimento médio, em múltiplos do raio (metade do menor lado).")]
        [Min(0)] public float rayLength = 1f;
        [Range(0f, 1f)] public float rayLengthVariation = 0.35f;
        [Tooltip("Espessura, em múltiplos do raio.")]
        [Min(0)] public float rayWidth = 0.06f;
        [Range(0f, 1f)] public float rayAlpha = 0.55f;
        [Tooltip("Quanto a intensidade de cada raio oscila.")]
        [Range(0f, 1f)] public float rayFlicker = 0.4f;
        [Tooltip("Graus por segundo (negativo gira ao contrário).")]
        public float raySpin = 3f;
        [Tooltip("Halo macio embaixo de cada raio: largura em múltiplos da espessura do raio (0 = sem halo).")]
        [Min(0)] public float rayHaloWidth = 5f;
        [Range(0f, 1f)] public float rayHaloAlpha = 0.4f;

        [Header("3. Sparkles")]
        [Min(0)] public int sparkleCount = 10;
        [Tooltip("Tamanhos, em múltiplos do menor lado deste retângulo.")]
        [Min(0)] public float sparkleSizeMin = 0.05f;
        [Min(0)] public float sparkleSizeMax = 0.13f;
        [Range(0f, 1f)] public float sparkleAlpha = 0.95f;
        [Tooltip("Duração de um brilho (nascer, brilhar, sumir), em segundos.")]
        [Min(0.2f)] public float sparklePeriod = 1.8f;
        [Tooltip("Faixa de distância do centro onde nascem, em múltiplos do raio.")]
        public Vector2 sparkleRadius = new Vector2(0.35f, 0.95f);
        [Tooltip("Quanto sobem enquanto brilham, em múltiplos do menor lado.")]
        public float sparkleRise = 0.03f;
        [Range(0f, 1f)] public float sparkleStarShare = 0.7f;
        [Tooltip("Os sparkles saem do centro e vão para fora (em vez de nascerem parados no lugar).")]
        public bool sparkleOutward = false;
        [Tooltip("Halo macio atrás de cada sparkle: tamanho em múltiplos do sparkle (0 = sem halo).")]
        [Min(0)] public float sparkleHaloSize = 2.8f;
        [Range(0f, 1f)] public float sparkleHaloAlpha = 0.5f;

        [Header("4. Bolinhas de luz (opcional)")]
        [Min(0)] public int dotCount = 0;
        [Min(0)] public float dotSize = 0.05f;
        [Range(0f, 1f)] public float dotAlpha = 0.8f;
        [Min(0.2f)] public float dotLife = 2f;
        [Tooltip("Quanto sobem durante a vida, em múltiplos do menor lado.")]
        public float dotRise = 0.35f;

        [Header("5. Anéis (círculos)")]
        [Tooltip("Anéis que nascem pequenos, crescem e somem, em loop.")]
        [Min(0)] public int ringCount = 0;
        [Range(0f, 1f)] public float ringAlpha = 0.6f;
        [Min(0.3f)] public float ringPeriod = 2.4f;
        [Tooltip("Tamanho inicial e final do anel, em múltiplos do menor lado.")]
        public Vector2 ringSize = new Vector2(0.45f, 1f);
        [Tooltip("Anel fixo em volta do objeto (0 = sem).")]
        [Range(0f, 1f)] public float staticRingAlpha = 0f;
        [Range(0.1f, 1.5f)] public float staticRingSize = 0.62f;

        [Header("Geral")]
        [Min(0)] public float speed = 1f;
        [Tooltip("Liga o efeito no jogo sozinho; desligue para controlar por código (Evaluate).")]
        public bool autoPlay = true;
        [Range(0f, 1f)] public float intensity = 1f;

        static Sprite spikeSprite;

        /// Espinho: base larga no centro, afinando até a ponta, com bordas macias.
        static Sprite Spike
        {
            get
            {
                if (spikeSprite != null) return spikeSprite;
                const int w = 128, h = 32;
                var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (var x = 0; x < w; x++)
                {
                    var t = x / (float)(w - 1);
                    var half = Mathf.Pow(1f - t, 0.85f) * 0.5f;
                    var along = Mathf.Pow(1f - t, 0.5f);
                    for (var y = 0; y < h; y++)
                    {
                        var dy = Mathf.Abs((y + 0.5f) / h - 0.5f);
                        var edge = Mathf.Clamp01((half - dy) / Mathf.Max(0.02f, half * 0.6f));
                        texture.SetPixel(x, y, new Color(1, 1, 1, edge * edge * (3f - 2f * edge) * along));
                    }
                }
                texture.Apply();
                spikeSprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), 100f);
                spikeSprite.hideFlags = HideFlags.HideAndDontSave;
                return spikeSprite;
            }
        }

        Sprite RaySprite => proceduralRays ? Spike : raySprite;

        RectTransform container;
        Image glow, innerGlow, staticRing;
        Image[] rings = new Image[0];
        Image[] rays = new Image[0], sparkles = new Image[0], dots = new Image[0], rayHalos = new Image[0], sparkleHalos = new Image[0];
        float start;
        bool built;

        // ------------------------------------------------------------------------------------------ ciclo de vida

        void OnEnable()
        {
            start = Application.isPlaying ? Time.unscaledTime : 0f;
            Rebuild();
        }

        void OnDisable() => Clear();

#if UNITY_EDITOR
        void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null || !isActiveAndEnabled) return;
                Rebuild();
            };
        }
#endif

        void Update()
        {
            if (!autoPlay || !built) return;
            Evaluate(Application.isPlaying ? Time.unscaledTime - start : (float)UnityEditorTime());
        }

        static double UnityEditorTime()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorApplication.timeSinceStartup;
#else
            return 0;
#endif
        }

        // ------------------------------------------------------------------------------------------ construção

        public void Rebuild()
        {
            Clear();
            if (additive == null) return;
            var go = new GameObject("Efeito mágico (gerado)", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
            container = (RectTransform)go.transform;
            container.SetParent(transform, false);
            container.SetAsFirstSibling();
            container.anchorMin = container.anchorMax = container.pivot = Vector2.one * 0.5f;
            container.anchoredPosition = Vector2.zero;
            container.sizeDelta = Vector2.zero;

            glow = Make("Glow", glowSprite);
            innerGlow = innerGlowAlpha > 0f ? Make("Glow central", glowSprite) : null;
            staticRing = staticRingAlpha > 0f ? Make("Anel fixo", ringSprite) : null;
            rings = new Image[ringCount];
            for (var i = 0; i < ringCount; i++) rings[i] = Make("Anel " + i, ringSprite);
            rayHalos = new Image[rayHaloWidth > 0f ? rayCount : 0];
            for (var i = 0; i < rayHalos.Length; i++) rayHalos[i] = Make("Halo do raio " + i, RaySprite, new Vector2(0f, 0.5f));
            rays = new Image[rayCount];
            for (var i = 0; i < rayCount; i++) rays[i] = Make("Raio " + i, RaySprite, new Vector2(0f, 0.5f));
            sparkleHalos = new Image[sparkleHaloSize > 0f ? sparkleCount : 0];
            for (var i = 0; i < sparkleHalos.Length; i++) sparkleHalos[i] = Make("Halo do sparkle " + i, sparkleDot);
            sparkles = new Image[sparkleCount];
            for (var i = 0; i < sparkleCount; i++) sparkles[i] = Make("Sparkle " + i, sparkleDot);
            dots = new Image[dotCount];
            for (var i = 0; i < dotCount; i++) dots[i] = Make("Bolinha " + i, sparkleDot);
            built = true;
            Evaluate(Application.isPlaying ? 0f : 0.6f);
        }

        void Clear()
        {
            built = false;
            if (container != null)
            {
                if (Application.isPlaying) Destroy(container.gameObject); else DestroyImmediate(container.gameObject);
            }
            container = null; glow = null; innerGlow = null;
            rays = new Image[0]; sparkles = new Image[0]; dots = new Image[0]; rayHalos = new Image[0]; sparkleHalos = new Image[0];
            rings = new Image[0]; staticRing = null;
        }

        Image Make(string label, Sprite sprite, Vector2? pivot = null)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)) { hideFlags = HideFlags.HideAndDontSave };
            var rect = (RectTransform)go.transform;
            rect.SetParent(container, false);
            rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
            rect.pivot = pivot ?? Vector2.one * 0.5f;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.material = additive;
            image.raycastTarget = false;
            image.color = new Color(1, 1, 1, 0);
            return image;
        }

        // ------------------------------------------------------------------------------------------ animação

        static float Hash(int a, int b)
        {
            unchecked
            {
                var x = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ 0x9E3779B9u;
                x ^= x >> 13; x *= 0x5bd1e995u; x ^= x >> 15;
                return (x & 0xFFFFFF) / 16777216f;
            }
        }

        static Color WithAlpha(Color color, float alpha) { color.a = alpha; return color; }

        public void Evaluate(float time)
        {
            if (!built || container == null) return;
            var t = time * speed;
            var rect = ((RectTransform)transform).rect;
            var side = Mathf.Max(1f, Mathf.Min(rect.width, rect.height));
            var radius = side * 0.5f;

            if (glow != null && glowSprite != null)
            {
                var pulse = Mathf.Sin(t * glowSpeed * 6.2831853f);
                var size = side * glowSize * (1f + glowPulse * pulse);
                glow.rectTransform.sizeDelta = new Vector2(size, size);
                glow.color = WithAlpha(glowColor, glowAlpha * intensity * (0.88f + 0.12f * pulse));
                if (innerGlow != null)
                {
                    var inner = size * innerGlowSize;
                    innerGlow.rectTransform.sizeDelta = new Vector2(inner, inner);
                    innerGlow.color = WithAlpha(Color.Lerp(glowColor, sparkleColor, 0.6f), innerGlowAlpha * intensity * (0.9f + 0.1f * pulse));
                }
            }

            if (staticRing != null)
            {
                var pulse = 0.85f + 0.15f * Mathf.Sin(t * glowSpeed * 6.2831853f);
                var sz = side * staticRingSize;
                staticRing.rectTransform.sizeDelta = new Vector2(sz, sz);
                staticRing.color = WithAlpha(glowColor, staticRingAlpha * intensity * pulse);
            }
            for (var i = 0; i < rings.Length; i++)
            {
                if (rings[i] == null) continue;
                var f = Mathf.Repeat(t / ringPeriod + i / (float)Mathf.Max(1, rings.Length), 1f);
                var sz = side * Mathf.Lerp(ringSize.x, ringSize.y, 1f - (1f - f) * (1f - f));
                rings[i].rectTransform.sizeDelta = new Vector2(sz, sz);
                rings[i].color = WithAlpha(sparkleColor, ringAlpha * intensity * Mathf.Sin(Mathf.PI * f));
            }

            for (var i = 0; i < rays.Length; i++)
            {
                var ray = rays[i];
                if (ray == null || RaySprite == null) continue;
                var baseAngle = i * 360f / rays.Length + Hash(i, 1) * 360f / rays.Length * 0.5f;
                var length = radius * rayLength * (1f - rayLengthVariation * Hash(i, 2));
                var wave = 0.5f + 0.5f * Mathf.Sin(6.2831853f * (t * (0.25f + 0.35f * Hash(i, 3)) + Hash(i, 4)));
                var alpha = rayAlpha * intensity * (1f - rayFlicker + rayFlicker * wave);
                ray.rectTransform.localRotation = Quaternion.Euler(0, 0, baseAngle + t * raySpin);
                ray.rectTransform.sizeDelta = new Vector2(length, Mathf.Max(1f, radius * rayWidth));
                ray.color = WithAlpha(rayColor, alpha);
                if (i < rayHalos.Length && rayHalos[i] != null)
                {
                    var halo = rayHalos[i];
                    halo.rectTransform.localRotation = ray.rectTransform.localRotation;
                    halo.rectTransform.sizeDelta = new Vector2(length, Mathf.Max(2f, radius * rayWidth * rayHaloWidth));
                    halo.color = WithAlpha(rayColor, alpha * rayHaloAlpha);
                }
            }

            for (var i = 0; i < sparkles.Length; i++)
            {
                var s = sparkles[i];
                if (s == null) continue;
                var phase = t / sparklePeriod + Hash(i, 5);
                var cycle = Mathf.FloorToInt(phase);
                var f = phase - cycle;
                var seed = i * 313 + cycle;
                var angle = Hash(seed, 6) * 6.2831853f;
                var distance = radius * Mathf.Lerp(sparkleRadius.x, sparkleRadius.y, sparkleOutward ? f : Hash(seed, 7));
                var rise = side * sparkleRise * f;
                var star = Hash(i, 8) < sparkleStarShare;
                s.sprite = star ? sparkleStar : sparkleDot;
                var envelope = Mathf.Sin(Mathf.PI * f);
                envelope *= envelope;
                var size = side * Mathf.Lerp(sparkleSizeMin, sparkleSizeMax, Hash(i, 9)) * (0.45f + 0.55f * envelope);
                s.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance + rise);
                s.rectTransform.sizeDelta = new Vector2(size, size);
                s.rectTransform.localRotation = Quaternion.Euler(0, 0, star ? f * 60f + Hash(seed, 10) * 45f : 0f);
                s.color = WithAlpha(sparkleColor, sparkleAlpha * envelope * intensity);
                if (i < sparkleHalos.Length && sparkleHalos[i] != null)
                {
                    var halo = sparkleHalos[i];
                    halo.rectTransform.anchoredPosition = s.rectTransform.anchoredPosition;
                    halo.rectTransform.sizeDelta = new Vector2(size * sparkleHaloSize, size * sparkleHaloSize);
                    halo.color = WithAlpha(sparkleColor, sparkleHaloAlpha * envelope * intensity);
                }
            }

            for (var i = 0; i < dots.Length; i++)
            {
                var d = dots[i];
                if (d == null) continue;
                var phase = t / dotLife + Hash(i, 11);
                var cycle = Mathf.FloorToInt(phase);
                var f = phase - cycle;
                var seed = i * 577 + cycle;
                var x = (Hash(seed, 12) - 0.5f) * 2f * radius * 0.9f;
                var y = (Hash(seed, 13) - 0.7f) * radius * 0.6f + side * dotRise * f;
                var size = side * dotSize * (0.5f + Hash(seed, 14)) * (1f - 0.3f * f);
                d.rectTransform.anchoredPosition = new Vector2(x + Mathf.Sin((f + Hash(seed, 15)) * 7f) * side * 0.01f, y);
                d.rectTransform.sizeDelta = new Vector2(size, size);
                d.color = WithAlpha(sparkleColor, dotAlpha * Mathf.Sin(Mathf.PI * f) * intensity);
            }
        }
    }
}
