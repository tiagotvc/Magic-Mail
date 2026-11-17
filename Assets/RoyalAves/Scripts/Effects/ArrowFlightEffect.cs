// Flecha Postal atravessando uma LINHA do tabuleiro (horizontal, da esquerda para a direita), no estilo do mock
// "efeito_flexa_postal_explosivo": a tela fica escura até o efeito acabar, a flecha vai na frente com aura dourada e
// uma cauda de luz (núcleo branco, faixa dourada, halo), fitas mágicas magenta/violeta ondulando, estrelinhas, pedacinhos
// coloridos saindo de cada peça atingida e microflash + anel em cada casa. Cada peça é destruída no instante em que a
// ponta passa (o jogo faz o resto: bombas, foguetes e outros itens da linha são ativados como em qualquer destruição).
//
// Usa as 22 máscaras de Royal Postman Art/VFX/Arrow/Mascaras_PNG (luz aditiva, fragmentos com mistura normal). A cauda, as
// estrelas e os fragmentos ficam no mundo (não são filhos da flecha), então deixam rastro e somem depois que ela passa.
// É o prefab Resources/Effects/Arrow/ArrowFlight: todos os parâmetros aparecem no Inspector.
using System;
using System.Collections.Generic;
using System.Linq;
using RoyalAves.Meta;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using UnityEngine;

namespace RoyalAves.Effects
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Royal Aves/Arrow Flight Effect")]
    public sealed class ArrowFlightEffect : MonoBehaviour
    {
        public const string ResourcePath = "Effects/Arrow/ArrowFlight";

        [Header("Flecha (sprite original, não é alterado)")]
        public Sprite arrowSprite;
        [Tooltip("Ângulo, em graus, que a ponta da arte aponta acima da horizontal (a Arrow.png aponta uns 22° para cima).")]
        public float artAngle = 22f;
        [Header("Brilho no contorno da flecha (shader ArrowRim)")]
        [Tooltip("Material RoyalAves/ArrowRim: faz a luz só por fora da silhueta da flecha.")]
        public Material rimMaterial;
        [Tooltip("Alcance do brilho em coordenadas da textura: a camada de dentro (fina e forte) e a de fora (larga e fraca).")]
        public Vector2 rimRadius = new Vector2(0.018f, 0.05f);
        [Header("Visualização (só para o preview do editor)")]
        [Tooltip("Mantém a flecha parada no lugar enquanto todo o resto (cauda, espiral, estrelinhas) continua em movimento.")]
        public bool freezeArrow;
        public float freezeX;
        [Header("Brilho no contorno da flecha (legado)")]
        public Color arrowGlowColor = new Color(1f, 0.66f, 0.18f, 1f);
        [Range(0f, 1f)] public float arrowGlowAlpha = 1f;
        [Tooltip("Escala das duas cópias da flecha que formam o brilho do contorno (a de dentro e a de fora): perto de 1 = brilho só na borda.")]
        public Vector2 arrowGlowScale = new Vector2(1.035f, 1.09f);
        [Header("Energia pulsando na cabeça (pontinhos que saem do meio para fora)")]
        [Min(0)] public int headPulseCount = 14;
        [Min(0)] public float headPulseSize = 0.05f;
        [Tooltip("Até onde os pontinhos chegam, em casas.")]
        [Min(0)] public float headPulseRadius = 0.65f;
        [Min(0.1f)] public float headPulseLife = 0.6f;
        [Range(0f, 1f)] public float headPulseAlpha = 0.9f;

        [Header("Máscaras de luz (aditivas)")]
        public Sprite aura;            // 01_aura_item_suave
        public Sprite tipGlow;         // 02_glow_ponta_dourado
        public Sprite tailHalo;        // 03_cauda_halo_largo
        public Sprite tailCore;        // 04_cauda_coracao_branco
        public Sprite tailGold;        // 05_cauda_faixa_dourada
        public Sprite ribbonTop;       // 06_fita_roxa_superior
        public Sprite ribbonBottom;    // 07_fita_roxa_inferior
        public Sprite ribbonMid;       // 08_fita_rosa_intermediaria
        public Sprite ribbonHalo;      // 09_fita_halo_difuso
        public Sprite residue;         // 10_residuo_ondulado
        public Sprite sparkle4;        // 11_sparkle_quatro_pontas
        public Sprite sparkle6;        // 12_sparkle_seis_pontas
        public Sprite sparkleMini;     // 13_sparkle_mini
        public Sprite lightDot;        // 14_ponto_luz
        public Sprite speedStreak;     // 15_risco_velocidade
        public Sprite flash;           // 20_flash_impacto_celula
        public Sprite ring;            // 21_anel_impacto_celula
        public Sprite whizz;           // 22_whizz_frontal
        public Material additive;

        [Header("Fragmentos (mistura normal)")]
        public Sprite[] fragments = new Sprite[0];   // 16..19
        public Color[] fragmentColors =
        {
            new Color(0.93f, 0.22f, 0.30f), new Color(0.22f, 0.78f, 0.38f), new Color(0.26f, 0.52f, 0.98f), new Color(1f, 0.80f, 0.16f),
        };

        [Header("Cores")]
        public Color white = new Color(1f, 0.97f, 0.88f, 1f);
        public Color gold = new Color(1f, 0.77f, 0.30f, 1f);          // #FFC54D
        public Color magenta = new Color(0.93f, 0.44f, 0.95f, 1f);    // #EE70F1
        public Color violet = new Color(0.62f, 0.35f, 0.95f, 1f);
        [Tooltip("Azul claro das estrelinhas e da aura de vento.")]
        public Color blue = new Color(0.5f, 0.7f, 1f, 1f);     // #9F5AF2

        [Header("Voo")]
        [Tooltip("Comprimento da flecha em casas (ela precisa caber na linha).")]
        [Min(0.3f)] public float arrowLengthCells = 1.7f;
        [Tooltip("Velocidade média em casas por segundo.")]
        [Min(1f)] public float speedCells = 11f;
        [Min(0f)] public float windupSeconds = 0.2f;
        [Tooltip("1 = velocidade constante; maior = começa devagar e termina rápida.")]
        [Min(1f)] public float acceleration = 1.4f;
        [Tooltip("Distância da ponta ao centro da arte, em múltiplos do comprimento.")]
        public float tipOffset = 0.45f;
        [Tooltip("Quanto a flecha começa antes da primeira casa e termina depois da última, em casas.")]
        public Vector2 overshootCells = new Vector2(1.3f, 1.4f);
        [Tooltip("Tempo que a cauda leva para sumir depois que a flecha sai.")]
        [Min(0.05f)] public float tailFadeSeconds = 0.35f;
        [Min(0f)] public float lingerSeconds = 0.9f;

        [Header("Tela escura durante o efeito")]
        [Range(0f, 1f)] public float dimAlpha = 0.6f;
        [Min(0f)] public float dimFadeSeconds = 0.18f;

        [Header("Aura e ponta")]
        [Range(0f, 1f)] public float auraAlpha = 0f;
        [Min(0)] public float auraSizeCells = 2.2f;
        [Tooltip("Aura macia atrás do corpo (0 = desligada; ficava parecendo um halo amarelo estranho).")]
        [Range(0f, 1f)] public float tipGlowAlpha = 0f;
        [Min(0)] public float tipGlowSizeCells = 0.5f;
        [Range(0f, 1f)] public float whizzAlpha = 0f;
        [Min(0)] public float whizzSizeCells = 1.0f;

        [Header("Cauda de luz")]
        [Min(0)] public float tailLengthCells = 3f;
        [Tooltip("Cauda afunilada: as espessuras abaixo são as da saída da flecha; a cauda afina até quase zero no fim.")]
        public bool taperedTail = true;
        [Min(0.1f)] public float taperPower = 0.85f;
        [Min(0)] public float coreThickness = 0.15f;
        [Min(0)] public float goldThickness = 0.42f;
        [Tooltip("Brilho forte em volta da faixa dourada.")]
        [Min(0)] public float glowThickness = 0.62f;
        [Range(0f, 1f)] public float glowAlphaTail = 0.55f;
        [Header("Movimento da cauda (fluxo de luz correndo para trás)")]
        [Tooltip("Velocidade do fluxo, em ciclos por segundo.")]
        [Min(0)] public float tailFlowSpeed = 7f;
        [Tooltip("Quantas ondas de luz cabem na cauda.")]
        [Min(0)] public float tailFlowWaves = 3.2f;
        [Range(0f, 1f)] public float tailFlowWidth = 0.14f;
        [Range(0f, 1f)] public float tailFlowAlpha = 0.28f;
        [Tooltip("Espessura do halo macio em volta da cauda (pequeno e discreto).")]
        [Min(0)] public float haloThickness = 0.7f;
        [Tooltip("Liga: o halo é gerado em código, macio nas duas pontas (a máscara 03 tem uma quina reta na ponta).")]
        public bool proceduralHalo = true;
        [Range(0f, 1f)] public float haloAlpha = 0.22f;
        [Range(0f, 1f)] public float goldAlpha = 0.9f;
        [Range(0f, 1f)] public float coreAlpha = 1f;

        [Header("Fitas mágicas")]
        [Tooltip("Altura da máscara das fitas, em casas: quanto maior, mais elas se afastam do núcleo e mais grossas ficam.")]
        [Min(0)] public float ribbonHeight = 1.7f;
        [Tooltip("Quanto as fitas sobem e descem, em casas.")]
        [Min(0)] public float ribbonWave = 0.16f;
        [Range(0f, 1f)] public float ribbonAlpha = 1f;
        [Range(0f, 1f)] public float ribbonHaloAlpha = 0.5f;
        [Range(0f, 1f)] public float residueAlpha = 0.25f;

        [Header("Espiral mágica (fitas que giram e se cruzam atrás da flecha)")]
        [Tooltip("Liga: as fitas são curvas geradas em código que giram de verdade em volta do eixo (o resto, as máscaras 06-09, fica só como reserva).")]
        public bool helixRibbons = true;
        [Tooltip("Quanto as fitas se afastam do eixo, em casas.")]
        [Min(0)] public float helixAmplitude = 0.42f;
        [Tooltip("Voltas ao longo da cauda.")]
        [Min(0.2f)] public float helixTurns = 2.2f;
        [Tooltip("Rotações por segundo.")]
        public float helixSpin = 3.5f;
        [Min(0)] public float helixThickness = 0.06f;
        [Tooltip("Espessura do halo macio de cada fita, em múltiplos da fita.")]
        [Min(1)] public float helixHaloScale = 5f;
        [Range(0f, 1f)] public float helixAlpha = 1f;
        [Range(0f, 1f)] public float helixHaloAlpha = 0.3f;
        [Tooltip("Funil: quanto a espiral afina ao longo da cauda (1 = linear; menor = mantém a largura por mais tempo).")]
        [Min(0.2f)] public float helixFunnel = 0.8f;

        [Header("Aura de vento mágico (cone transparente da ponta até o fim, afunilando)")]
        public bool windEnabled = true;
        [Tooltip("Largura máxima, em casas.")]
        [Min(0)] public float windWidth = 1f;
        [Range(0f, 1f)] public float windAlpha = 0.36f;
        [Tooltip("Velocidade da ondulação correndo para trás.")]
        public float windSpeed = 2.2f;

        [Header("Onda em concha (quebra de atmosfera: arcos que abrem da cabeça para trás)")]
        public bool shellEnabled = true;
        [Tooltip("Quanto os arcos se abrem no fim, em casas.")]
        [Min(0)] public float shellSpread = 0.78f;
        [Min(0)] public float shellThickness = 0.07f;
        [Range(0f, 1f)] public float shellAlpha = 0.6f;
        [Tooltip("Velocidade do brilho correndo pelos arcos.")]
        public float shellSpeed = 2.6f;

        [Header("Estrelinhas e riscos")]
        [Min(0)] public int sparkleCount = 46;
        [Min(0)] public float sparkleSize = 0.11f;
        [Min(0.1f)] public float sparkleLife = 0.55f;
        [Min(0)] public int streakCount = 7;
        [Min(0)] public float streakLengthCells = 0.9f;

        [Header("Fragmentos de cada peça atingida")]
        [Min(0)] public int fragmentsPerHit = 14;
        [Min(0)] public float fragmentSize = 0.13f;
        [Min(0.1f)] public float fragmentLife = 0.8f;
        [Tooltip("Os fragmentos saem com a cor de cada peça destruída (lida da arte dela).")]
        public bool pieceColorFragments = true;
        [Header("Explosão de cada peça (estouro colorido)")]
        [Tooltip("Raios curtos que saem do centro da peça em todas as direções.")]
        [Min(0)] public int burstRays = 10;
        [Min(0)] public float burstRayLength = 1f;
        [Min(0.05f)] public float burstSeconds = 0.3f;
        [Tooltip("Clarão colorido que cresce rápido atrás da explosão, em casas.")]
        [Min(0)] public float burstGlowSize = 1.7f;
        [Tooltip("Tremidinha da câmera a cada peça atingida, em casas.")]
        [Min(0)] public float hitShake = 0.02f;
        [Tooltip("Velocidade inicial, em casas por segundo.")]
        [Min(0)] public float fragmentSpeed = 3.8f;
        [Min(0)] public float fragmentGravity = 6.5f;

        [Header("Impacto em cada casa")]
        public bool hitBursts = true;
        [Min(0.05f)] public float flashSeconds = 0.12f;
        [Min(0)] public float flashSizeCells = 1.0f;
        [Min(0.05f)] public float ringSeconds = 0.2f;
        [Min(0)] public float ringSizeCells = 1.3f;
        [Range(0f, 1f)] public float waveAlpha = 0.55f;
        [Min(0.1f)] public float waveSeconds = 0.45f;
        [Min(0)] public float waveReach = 1.0f;

        [Header("Impacto no fim da tela")]
        [Tooltip("A flecha nasce fora da tela, à esquerda, atravessa a linha e bate na borda direita da tela, com um impacto grande.")]
        public bool edgeImpact = true;
        [Tooltip("Tamanho do flash, do anel e da onda do impacto final, em relação aos de cada casa.")]
        [Min(0.5f)] public float edgeImpactScale = 2.4f;
        [Tooltip("Tremida extra da câmera no impacto final, em casas.")]
        [Min(0)] public float edgeShake = 0.16f;

        [Header("Tremida da câmera")]
        [Tooltip("Em casas (0 = sem tremida).")]
        [Min(0)] public float shakeAmount = 0.05f;
        [Min(0)] public float shakeSeconds = 0.7f;

        // ------------------------------------------------------------------------------------------ uso

        static ArrowFlightEffect prefab;
        static bool prefabSearched;

        /// true quando o prefab do efeito existe (senão o jogo usa o foguete antigo).
        public static bool IsAvailable
        {
            get
            {
                if (!prefabSearched || prefab == null) { prefab = Resources.Load<ArrowFlightEffect>(ResourcePath); prefabSearched = true; }
                return prefab != null && prefab.arrowSprite != null;
            }
        }

        /// squares: as casas da linha, em qualquer ordem. onHit: chamado uma vez por casa, no instante em que a ponta passa.
        /// Retorna o tempo até a última peça ser atingida (o resto do efeito termina sozinho).
        public static float Play(IList<Square> squares, Action<Square> onHit)
        {
            if (squares == null || squares.Count == 0) return 0f;
            if (!IsAvailable)
            {
                foreach (var sq in squares) onHit(sq);
                return 0f;
            }
            var instance = Instantiate(prefab);
            instance.name = "ArrowFlightFX";
            var ordered = squares.OrderBy(s => s.transform.position.x).ToList();
            var cellSize = LevelManager.THIS != null ? LevelManager.THIS.squareWidth : 1.2f;
            instance.hitColors = ordered.Select(SamplePieceColor).ToArray();   // lê a cor de cada peça ANTES de ela ser destruída
            return instance.Begin(ordered.Select(s => s.transform.position).ToList(), cellSize, i => onHit(ordered[i]), Camera.main, true);
        }

        /// Só para a janela de preview do editor: cria o efeito num tabuleiro de mentira. O tempo é controlado de fora
        /// (Evaluate), a câmera não treme e a tela de power-up não é avisada. positions: centro de cada casa da linha.
        public static ArrowFlightEffect CreatePreview(ArrowFlightEffect source, IList<Vector3> positions, float cellSize, Action<int> onHit, Camera viewCamera)
        {
            var instance = Instantiate(source);
            instance.name = "ArrowFlightPreview";
            instance.manualTime = true;
            instance.Begin(positions, cellSize, onHit, viewCamera, false);
            return instance;
        }

        /// Duração total do efeito, em segundos.
        public float Duration => total;

        // ------------------------------------------------------------------------------------------ estado

        Color[] hitColors;

        /// Cor média da arte da peça (lida na GPU, então funciona mesmo com a textura não legível), reforçando a saturação.
        static Color SamplePieceColor(Square square)
        {
            try
            {
                var item = square != null ? square.Item : null;
                if (item == null) return Color.clear;
                var renderer = item.GetSpriteRenderers().Where(r => r != null && r.enabled && r.sprite != null)
                    .OrderByDescending(r => r.bounds.size.x * r.bounds.size.y).FirstOrDefault();
                if (renderer == null) return Color.clear;
                var sprite = renderer.sprite; var texture = sprite.texture; var rect = sprite.textureRect;
                var target = RenderTexture.GetTemporary(16, 16, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(texture, target, new Vector2(rect.width / texture.width, rect.height / texture.height), new Vector2(rect.x / texture.width, rect.y / texture.height));
                var previous = RenderTexture.active;
                RenderTexture.active = target;
                var read = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                read.ReadPixels(new Rect(0, 0, 16, 16), 0, 0);
                read.Apply();
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                var pixels = read.GetPixels();
                Destroy(read);
                double r = 0, g = 0, b = 0, w = 0;
                foreach (var c in pixels)
                {
                    var chroma = Mathf.Max(c.r, c.g, c.b) - Mathf.Min(c.r, c.g, c.b);
                    var weight = c.a * (0.15f + chroma);
                    r += c.r * weight; g += c.g * weight; b += c.b * weight; w += weight;
                }
                if (w <= 1e-4) return Color.clear;
                Color.RGBToHSV(new Color((float)(r / w), (float)(g / w), (float)(b / w)), out var h, out var sat, out var val);
                var result = Color.HSVToRGB(h, Mathf.Min(1f, sat * 1.25f), Mathf.Min(1f, 0.55f + val * 0.6f));
                result.a = 1f;
                return result;
            }
            catch (Exception) { return Color.clear; }
        }

        sealed class Hit
        {
            public int index; public float time; public bool done; public Vector3 position; public bool edge;
            public Color baseColor; public SpriteRenderer puff; public SpriteRenderer[] rays;
            public SpriteRenderer flash, ring, wave;
            public SpriteRenderer[] frags;
            public Vector2[] velocity; public float[] spin, size; public Color[] color;
        }

        readonly List<Hit> hits = new List<Hit>();
        Action<int> onHitCallback;
        Camera viewCamera;
        bool manualTime;
        bool realGame;
        Strand[] tailStrands = new Strand[0];
        SpriteRenderer[] pulseDots = new SpriteRenderer[0];
        SpriteRenderer arrowGlow1, arrowGlow2, arrow, auraSr, tipSr, whizzSr, haloSr, goldSr, coreSr, ribTop, ribBottom, ribMid, ribHaloA, ribHaloB, residueSr, dim;
        SpriteRenderer[] sparkles, streaks;
        float cell, x0, x1, y, z, flight, total, windup, edgeX;
        Camera shakeCamera;
        Vector3 cameraBase;
        bool shaking;
        float age;
        sealed class Strand { public Mesh mesh; public Vector3[] v; public Color[] c; public Vector2[] uv; }
        const int HelixSegments = 72;
        Strand[] strands = new Strand[0];
        Strand[] strandsFront = new Strand[0];   // metade da espiral que passa POR CIMA da flecha (a "strands" fica por trás)
        static Texture2D helixProfile;

        static Texture2D HelixProfile
        {
            get
            {
                if (helixProfile != null) return helixProfile;
                const int h = 64;
                helixProfile = new Texture2D(4, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (var y = 0; y < h; y++)
                {
                    var v = (y + 0.5f) / h * 2f - 1f;
                    var a = Mathf.Exp(-v * v * 4.5f) * Mathf.Clamp01((1f - Mathf.Abs(v)) * 5f);
                    for (var x = 0; x < 4; x++) helixProfile.SetPixel(x, y, new Color(1, 1, 1, a));
                }
                helixProfile.Apply();
                return helixProfile;
            }
        }

        Strand MakeStrand(string label, int order)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = additive;
            renderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            renderer.sortingOrder = PowerUpEffectOrder.Value + order;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            block.SetTexture("_MainTex", HelixProfile);
            renderer.SetPropertyBlock(block);
            var st = new Strand { mesh = new Mesh { name = "HelixStrand" }, v = new Vector3[2 * (HelixSegments + 1)], c = new Color[2 * (HelixSegments + 1)], uv = new Vector2[2 * (HelixSegments + 1)] };
            st.mesh.MarkDynamic();
            var tris = new int[HelixSegments * 6];
            for (var i = 0; i < HelixSegments; i++)
            {
                var a = i * 2;
                tris[i * 6] = a; tris[i * 6 + 1] = a + 1; tris[i * 6 + 2] = a + 2;
                tris[i * 6 + 3] = a + 1; tris[i * 6 + 4] = a + 3; tris[i * 6 + 5] = a + 2;
                st.uv[a] = new Vector2(i / (float)HelixSegments, 0f); st.uv[a + 1] = new Vector2(i / (float)HelixSegments, 1f);
            }
            st.uv[2 * HelixSegments] = new Vector2(1f, 0f); st.uv[2 * HelixSegments + 1] = new Vector2(1f, 1f);
            st.mesh.vertices = st.v; st.mesh.uv = st.uv; st.mesh.colors = st.c; st.mesh.triangles = tris;
            st.mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 500f);
            filter.sharedMesh = st.mesh;
            return st;
        }

        /// Atualiza as duas cópias (trás e frente) de uma fita. A espiral nasce na PONTA da flecha, já com a largura cheia, e
        /// afunila até o fim da cauda; cada ponto aparece na cópia da frente ou na de trás conforme a fase da rotação.
        static readonly float[] TipSpread = new float[10];   // zero: as fitas se encontram na ponta da flecha

        void UpdateStrandPair(int index, float startX, float endX, float t, float phase, float amp, float thickness, Color color, float alpha, float spinSign)
        {
            var back = strands[index]; var front = strandsFront[index];
            var length = startX - endX;
            var centers = new Vector3[HelixSegments + 1];
            var widths = new float[HelixSegments + 1];
            var baseAlpha = new float[HelixSegments + 1];
            var frontF = new float[HelixSegments + 1];
            var backF = new float[HelixSegments + 1];
            for (var i = 0; i <= HelixSegments; i++)
            {
                var s = i / (float)HelixSegments;            // 0 = ponta da flecha, 1 = fim da cauda
                var theta = s * helixTurns * 2f * Mathf.PI - t * helixSpin * 2f * Mathf.PI * spinSign + phase;
                var env = Smooth(s / 0.22f) * Mathf.Pow(1f - s, helixFunnel);   // unidas na ponta, abrem logo atrás e afunilam até o fim
                // Cada fita tem um afastamento próprio na ponta (some ao longo da cauda): elas não se juntam num ponto junto da flecha.
                var spread = TipSpread[index] * cell * (1f - s) * (1f - s);
                centers[i] = new Vector3(startX - s * length, y + Mathf.Sin(theta) * amp * cell * env * (1f + 0.1f * Mathf.Sin(t * 11f + phase)) + spread, z);
                var depth = Mathf.Cos(theta);
                widths[i] = thickness * cell * (0.7f + 0.3f * depth) * (0.3f + 0.7f * Mathf.Pow(1f - s, 0.7f));
                baseAlpha[i] = alpha * Mathf.Pow(1f - s, 0.45f) * Smooth((1f - s) / 0.04f);
                frontF[i] = Smooth((depth + 0.15f) / 0.8f);
                backF[i] = Smooth((-depth + 0.15f) / 0.8f);
            }
            for (var i = 0; i <= HelixSegments; i++)
            {
                var prev = centers[Mathf.Max(0, i - 1)]; var next = centers[Mathf.Min(HelixSegments, i + 1)];
                var tangent = next - prev; tangent.z = 0f;
                if (tangent.sqrMagnitude < 1e-8f) tangent = Vector3.left;
                tangent.Normalize();
                var half = new Vector3(-tangent.y, tangent.x, 0f) * (widths[i] * 0.5f);
                back.v[2 * i] = centers[i] - half; back.v[2 * i + 1] = centers[i] + half;
                front.v[2 * i] = back.v[2 * i]; front.v[2 * i + 1] = back.v[2 * i + 1];
                var cb = new Color(color.r, color.g, color.b, baseAlpha[i] * backF[i]);
                var cf = new Color(color.r, color.g, color.b, baseAlpha[i] * frontF[i]);
                back.c[2 * i] = cb; back.c[2 * i + 1] = cb;
                front.c[2 * i] = cf; front.c[2 * i + 1] = cf;
            }
            back.mesh.vertices = back.v; back.mesh.colors = back.c;
            front.mesh.vertices = front.v; front.mesh.colors = front.c;
        }

        /// Uma fita da espiral (versão antiga, sem separar frente e trás): ondula em volta do eixo (seno), afina e some até o fim da cauda; a fase gira com o tempo,
        /// então a fita sobe e desce de verdade e cruza com a outra, como um fio enrolado.
        void UpdateStrand(Strand st, float tailRight, float tailLen, float t, float phase, float amp, float thickness, Color color, float alpha, float spinSign)
        {
            var centers = new Vector3[HelixSegments + 1];
            var widths = new float[HelixSegments + 1];
            var alphas = new float[HelixSegments + 1];
            for (var i = 0; i <= HelixSegments; i++)
            {
                var s = i / (float)HelixSegments;            // 0 = junto da flecha, 1 = fim da cauda
                var theta = s * helixTurns * 2f * Mathf.PI - t * helixSpin * 2f * Mathf.PI * spinSign + phase;
                var env = Smooth(s / 0.04f) * Mathf.Pow(1f - s, helixFunnel);   // funil: larga junto da flecha, fecha no fim
                centers[i] = new Vector3(tailRight - s * tailLen, y + Mathf.Sin(theta) * amp * cell * env * (1f + 0.12f * Mathf.Sin(t * 11f + phase)), z);
                widths[i] = thickness * cell * (0.62f + 0.38f * Mathf.Cos(theta)) * (0.25f + 0.75f * Mathf.Pow(1f - s, 0.7f));
                alphas[i] = alpha * Mathf.Pow(1f - s, 0.5f) * Smooth(s / 0.03f);
            }
            for (var i = 0; i <= HelixSegments; i++)
            {
                var prev = centers[Mathf.Max(0, i - 1)]; var next = centers[Mathf.Min(HelixSegments, i + 1)];
                var tangent = (next - prev); tangent.z = 0f;
                if (tangent.sqrMagnitude < 1e-8f) tangent = Vector3.left;
                tangent.Normalize();
                var normal = new Vector3(-tangent.y, tangent.x, 0f);
                var half = normal * (widths[i] * 0.5f);
                st.v[2 * i] = centers[i] - half; st.v[2 * i + 1] = centers[i] + half;
                var col = new Color(color.r, color.g, color.b, alphas[i]);
                st.c[2 * i] = col; st.c[2 * i + 1] = col;
            }
            st.mesh.vertices = st.v; st.mesh.colors = st.c;
        }

        /// Camada reta da cauda que afunila: largura w0 na saída da flecha, diminuindo até quase zero na ponta de trás.
        void UpdateTaper(Strand st, float tailRight, float tailLen, float w0, Color color, float alpha, float alphaPower, float t, float layer)
        {
            for (var i = 0; i <= HelixSegments; i++)
            {
                var s = i / (float)HelixSegments;
                var x = tailRight - s * tailLen;
                // Nasce fina embaixo da flecha, alarga logo atrás dela e afina até o fim; um fluxo de luz corre para trás.
                var flow = Mathf.Sin(s * tailFlowWaves * 2f * Mathf.PI - t * tailFlowSpeed * 2f * Mathf.PI + layer);
                var width = Mathf.Max(0.0005f, w0 * cell * Mathf.Pow(1f - s, taperPower) * Smooth(s / 0.1f) * (1f + tailFlowWidth * flow));
                var col = new Color(color.r, color.g, color.b, alpha * Mathf.Pow(1f - s, alphaPower) * Smooth(s / 0.08f) * (1f - tailFlowAlpha + tailFlowAlpha * (0.5f + 0.5f * flow)));
                st.v[2 * i] = new Vector3(x, y - width * 0.5f, z); st.v[2 * i + 1] = new Vector3(x, y + width * 0.5f, z);
                st.c[2 * i] = col; st.c[2 * i + 1] = col;
            }
            st.mesh.vertices = st.v; st.mesh.colors = st.c;
        }

        static Sprite softDotSprite;

        static Sprite SoftDot
        {
            get
            {
                if (softDotSprite != null) return softDotSprite;
                const int n = 64;
                var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (var py = 0; py < n; py++)
                    for (var px = 0; px < n; px++)
                    {
                        var dx = (px + 0.5f) / n * 2f - 1f; var dy = (py + 0.5f) / n * 2f - 1f;
                        var d2 = dx * dx + dy * dy;
                        texture.SetPixel(px, py, new Color(1, 1, 1, d2 >= 1f ? 0f : Mathf.Exp(-d2 * 4.5f) * (1f - d2)));
                    }
                texture.Apply();
                softDotSprite = Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n, 0, SpriteMeshType.FullRect);
                softDotSprite.hideFlags = HideFlags.HideAndDontSave;
                return softDotSprite;
            }
        }

        Strand[] windStrands = new Strand[0], shellStrands = new Strand[0];

        /// Cone de vento mágico: nasce fino na ponta, abre logo atrás da flecha e afunila até o fim, com a borda ondulando e o
        /// brilho correndo para trás. Bem transparente.
        void UpdateWind(Strand st, float startX, float endX, float w0, Color color, float alpha, float phase, float t)
        {
            var length = startX - endX;
            for (var i = 0; i <= HelixSegments; i++)
            {
                var s = i / (float)HelixSegments;
                var wob = Mathf.Sin(s * 9f - t * windSpeed * 6f + phase);
                var width = w0 * cell * Smooth(s / 0.14f) * Mathf.Pow(1f - s, 0.85f) * (1f + 0.2f * wob);
                var yy = y + Mathf.Sin(s * 6.5f - t * windSpeed * 5f + phase * 1.3f) * 0.06f * cell * s;
                var a = alpha * Mathf.Pow(1f - s, 0.7f) * Smooth(s / 0.05f) * (0.78f + 0.22f * Mathf.Sin(s * 14f - t * windSpeed * 8f + phase));
                var col = new Color(color.r, color.g, color.b, a);
                var x = startX - s * length;
                st.v[2 * i] = new Vector3(x, yy - width * 0.5f, z); st.v[2 * i + 1] = new Vector3(x, yy + width * 0.5f, z);
                st.c[2 * i] = col; st.c[2 * i + 1] = col;
            }
            st.mesh.vertices = st.v; st.mesh.colors = st.c;
        }

        /// Arco da onda em concha: parte da ponta e abre para cima (sign +1) ou para baixo (-1) conforme volta pela cauda, como o
        /// cone de uma onda de choque.
        void UpdateShell(Strand st, float startX, float endX, float sign, float spread, float thickness, Color color, float alpha, float phase, float t)
        {
            var length = startX - endX;
            for (var i = 0; i <= HelixSegments; i++)
            {
                var s = i / (float)HelixSegments;
                var yy = y + sign * spread * cell * Mathf.Pow(s, 0.6f);
                var width = thickness * cell * (0.25f + 0.75f * Mathf.Pow(1f - s, 0.6f)) * Smooth(s / 0.04f);
                var run = 0.55f + 0.45f * Mathf.Sin(s * 12f - t * shellSpeed * 6.28f + phase);
                var a = alpha * Mathf.Pow(1f - s, 0.55f) * Smooth(s / 0.03f) * run;
                var col = new Color(color.r, color.g, color.b, a);
                var x = startX - s * length;
                var next = Mathf.Min(1f, s + 0.01f);
                var dy = sign * spread * cell * (Mathf.Pow(next, 0.6f) - Mathf.Pow(s, 0.6f));
                var tangent = new Vector3(-0.01f * length, dy, 0f).normalized;
                var half = new Vector3(-tangent.y, tangent.x, 0f) * (width * 0.5f);
                st.v[2 * i] = new Vector3(x, yy, z) - half; st.v[2 * i + 1] = new Vector3(x, yy, z) + half;
                st.c[2 * i] = col; st.c[2 * i + 1] = col;
            }
            st.mesh.vertices = st.v; st.mesh.colors = st.c;
        }

        static Sprite coreDotSprite;

        // Ponto pequeno e forte (centro bem definido), para o miolo das estrelinhas.
        static Sprite CoreDot
        {
            get
            {
                if (coreDotSprite != null) return coreDotSprite;
                const int n = 32;
                var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (var py = 0; py < n; py++)
                    for (var px = 0; px < n; px++)
                    {
                        var dx = (px + 0.5f) / n * 2f - 1f; var dy = (py + 0.5f) / n * 2f - 1f;
                        var d = Mathf.Sqrt(dx * dx + dy * dy);
                        texture.SetPixel(px, py, new Color(1, 1, 1, d >= 1f ? 0f : Mathf.Clamp01(1.4f - d * 1.4f)));
                    }
                texture.Apply();
                coreDotSprite = Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n, 0, SpriteMeshType.FullRect);
                coreDotSprite.hideFlags = HideFlags.HideAndDontSave;
                return coreDotSprite;
            }
        }

        SpriteRenderer[] sparkleCores = new SpriteRenderer[0], sparkleStars = new SpriteRenderer[0], pulseCores = new SpriteRenderer[0];

        static Sprite solid, softStreakSprite;

        // Raia macia horizontal: some nas duas pontas e nas bordas (sem quinas retas), para o halo da cauda.
        static Sprite SoftStreak
        {
            get
            {
                if (softStreakSprite != null) return softStreakSprite;
                const int w = 256, h = 64;
                var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (var x = 0; x < w; x++)
                {
                    var u = x / (float)(w - 1);
                    var along = Mathf.Clamp01(u / 0.45f); along = along * along * (3f - 2f * along);        // nasce devagar na ponta de trás
                    var head = 1f - Mathf.Clamp01((u - 0.82f) / 0.18f); head = head * head * (3f - 2f * head); // some perto da flecha
                    for (var y = 0; y < h; y++)
                    {
                        var v = (y + 0.5f) / h * 2f - 1f;
                        texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Exp(-v * v * 3.2f) * along * head));
                    }
                }
                texture.Apply();
                softStreakSprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                softStreakSprite.hideFlags = HideFlags.HideAndDontSave;
                return softStreakSprite;
            }
        }

        // Fração da altura da máscara que a linha ocupa (medida nos PNGs: 13/256 e 36/256 pixels).
        const float CoreMaskFraction = 13f / 256f, GoldMaskFraction = 36f / 256f;

        static Sprite Solid
        {
            get
            {
                if (solid != null) return solid;
                var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Point };
                for (var i = 0; i < 16; i++) texture.SetPixel(i % 4, i / 4, Color.white);
                texture.Apply();
                solid = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f, 0, SpriteMeshType.FullRect);
                solid.hideFlags = HideFlags.HideAndDontSave;
                return solid;
            }
        }

        float Begin(IList<Vector3> positions, float cellSize, Action<int> onHit, Camera camera, bool game)
        {
            onHitCallback = onHit;
            viewCamera = camera;
            realGame = game;
            var xs = positions.Select(p => p.x).ToList();
            cell = cellSize;
            if (positions.Count > 1)
            {
                var step = float.MaxValue;
                for (var i = 1; i < xs.Count; i++) { var d = xs[i] - xs[i - 1]; if (d > 0.01f) step = Mathf.Min(step, d); }
                if (step < float.MaxValue) cell = step;
            }
            y = positions.Average(p => p.y);
            z = positions[0].z;
            x0 = xs[0] - cell * overshootCells.x;
            x1 = xs[xs.Count - 1] + cell * overshootCells.y;
            edgeX = x1;
            // A flecha nasce fora da tela (à esquerda) e voa até a borda direita da tela, onde bate.
            if (viewCamera != null && viewCamera.orthographic)
            {
                var halfWidth = viewCamera.orthographicSize * viewCamera.aspect;
                var camX = viewCamera.transform.position.x;
                var arrowLen = arrowLengthCells * cell;
                x0 = Mathf.Min(x0, camX - halfWidth - arrowLen * 0.7f);
                edgeX = camX + halfWidth;
                x1 = Mathf.Max(x1, edgeX - arrowLen * tipOffset);
            }
            windup = windupSeconds;
            flight = Mathf.Max(0.2f, (x1 - x0) / (cell * speedCells));
            total = windup + flight + Mathf.Max(lingerSeconds, tailFadeSeconds + 0.1f);

            var tipLead = arrowLengthCells * cell * tipOffset;
            for (var i = 0; i < positions.Count; i++)
            {
                var p = Mathf.Clamp01((positions[i].x - cell * 0.3f - tipLead - x0) / Mathf.Max(0.01f, x1 - x0));
                var f = Mathf.Pow(p, 1f / acceleration);
                var baseColor = hitColors != null && i < hitColors.Length && hitColors[i].a > 0.5f ? hitColors[i]
                    : (fragmentColors.Length > 0 ? fragmentColors[i % fragmentColors.Length] : gold);
                hits.Add(new Hit { index = i, time = windup + f * flight, position = positions[i], baseColor = baseColor });
            }

            if (edgeImpact)
                hits.Add(new Hit { index = -1, edge = true, time = windup + flight, position = new Vector3(Mathf.Max(edgeX - cell * 0.15f, xs[xs.Count - 1]), y, z) });
            BuildVisuals();
            // A tela de power-up (se estiver aberta) fica escura enquanto o efeito durar.
            if (realGame) PowerUpVfx.SuppressMatchFor(total);   // as peças da linha já têm a explosão da flecha
            if (realGame) BoosterIntroScreen.HoldUntil = Time.unscaledTime + total;
            if (realGame && (shakeAmount > 0f || edgeShake > 0f) && Camera.main != null)
            {
                shakeCamera = Camera.main;
                cameraBase = shakeCamera.transform.position;
                shaking = true;
            }
            Evaluate(0f);
            return windup + flight;
        }

        SpriteRenderer Make(string label, Sprite sprite, int order, bool additiveMaterial = true)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (additiveMaterial && additive != null) sr.sharedMaterial = additive;
            sr.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            sr.sortingOrder = PowerUpEffectOrder.Value + order;
            sr.color = new Color(1, 1, 1, 0);
            return sr;
        }

        Sprite Sparkle(int i)
        {
            var r = Hash(i, 90);
            var s = r < 0.35f ? sparkle4 : r < 0.6f ? sparkle6 : r < 0.85f ? sparkleMini : lightDot;
            return s != null ? s : lightDot;
        }

        void BuildVisuals()
        {
            // Ordem de baixo para cima (README do kit): escuro, halo, fitas, faixa e núcleo, brilhos, ponta, flecha.
            dim = Make("Escurecimento", Solid, -2, false);
            if (taperedTail)
            {
                tailStrands = new[] { MakeStrand("Cauda: halo", 0), MakeStrand("Cauda: brilho", 1), MakeStrand("Cauda: faixa dourada", 4), MakeStrand("Cauda: núcleo", 5) };
            }
            else haloSr = Make("Cauda: halo largo", proceduralHalo ? SoftStreak : tailHalo, 0);
            if (!helixRibbons)
            {
                ribHaloA = Make("Fita: halo A", ribbonHalo, 1);
                ribHaloB = Make("Fita: halo B", ribbonHalo, 1);
            }
            residueSr = Make("Resíduo ondulado", residue, 1);
            if (helixRibbons)
            {
                // Duas fitas em fase oposta (cruzam-se) + uma fina e curta no meio; cada uma com um halo macio por baixo.
                // Cada fita tem duas cópias: a de trás (abaixo da flecha) e a da frente (acima dela); o brilho de cada uma depende
                // de a fita estar passando por trás ou pela frente naquele ponto, então a espiral abraça a flecha como uma aura.
                strands = Enumerable.Range(0, 10).Select(i => MakeStrand("Espiral (trás) " + i, 10)).ToArray();
                strandsFront = Enumerable.Range(0, 10).Select(i => MakeStrand("Espiral (frente) " + i, 12)).ToArray();
            }
            else
            {
                ribBottom = Make("Fita inferior", ribbonBottom, 2);
                ribMid = Make("Fita rosa", ribbonMid, 2);
                ribTop = Make("Fita superior", ribbonTop, 3);
            }
            if (!taperedTail)
            {
                goldSr = Make("Cauda: faixa dourada", tailGold, 4);
                coreSr = Make("Cauda: núcleo branco", tailCore, 5);
            }
            if (windEnabled) windStrands = new[] { MakeStrand("Vento mágico (fora)", 13), MakeStrand("Vento mágico (dentro)", 14) };   // por cima da flecha e da cauda
            if (shellEnabled) shellStrands = new[] { MakeStrand("Concha: arco 1 (cima)", 6), MakeStrand("Concha: arco 1 (baixo)", 6), MakeStrand("Concha: arco 2 (cima)", 6), MakeStrand("Concha: arco 2 (baixo)", 6) };
            pulseDots = Enumerable.Range(0, headPulseCount).Select(i => Make("Energia da cabeça (halo)", SoftDot, 10)).ToArray();
            pulseCores = Enumerable.Range(0, headPulseCount).Select(i => Make("Energia da cabeça (miolo)", CoreDot, 11)).ToArray();
            streaks = Enumerable.Range(0, streakCount).Select(i => Make("Risco", speedStreak, 5)).ToArray();
            sparkles = Enumerable.Range(0, sparkleCount).Select(i => Make("Estrelinha (halo)", SoftDot, 7)).ToArray();
            sparkleStars = Enumerable.Range(0, sparkleCount).Select(i => Make("Estrelinha (brilho)", Sparkle(i), 7)).ToArray();
            sparkleCores = Enumerable.Range(0, sparkleCount).Select(i => Make("Estrelinha (miolo)", CoreDot, 8)).ToArray();
            auraSr = Make("Aura", lightDot != null ? lightDot : aura, 8);
            whizzSr = Make("Whizz", whizz, 9);
            arrowGlow2 = Make("Brilho do contorno (fora)", arrowSprite, 9, false);
            arrowGlow1 = Make("Brilho do contorno (dentro)", arrowSprite, 10, false);
            if (rimMaterial != null)
            {
                arrowGlow2.sharedMaterial = rimMaterial; arrowGlow1.sharedMaterial = rimMaterial;
                SetRim(arrowGlow1, rimRadius.x); SetRim(arrowGlow2, rimRadius.y);
            }
            else { arrowGlow1.enabled = false; arrowGlow2.enabled = false; }
            tipSr = Make("Glow da ponta", tipGlow, 10);
            arrow = Make("Flecha", arrowSprite, 11, false);
            foreach (var hit in hits)
            {
                hit.puff = Make("Clarão colorido", SoftDot, 12);
                hit.rays = new SpriteRenderer[hit.edge ? 0 : burstRays];
                for (var r = 0; r < hit.rays.Length; r++) hit.rays[r] = Make("Raio do estouro", speedStreak, 12);
                hit.wave = Make("Onda", speedStreak, 6);
                hit.ring = Make("Anel", ring, 12);
                hit.flash = Make("Flash", flash, 12);
                var n = fragments.Length > 0 ? (hit.edge ? fragmentsPerHit * 3 : fragmentsPerHit) : 0;
                hit.frags = new SpriteRenderer[n];
                hit.velocity = new Vector2[n]; hit.spin = new float[n]; hit.size = new float[n]; hit.color = new Color[n];
                for (var k = 0; k < n; k++)
                {
                    hit.frags[k] = Make("Fragmento", fragments[k % fragments.Length], 13, false);
                    var angle = Hash(k, 301 + hits.Count) * Mathf.PI * 2f;
                    hit.velocity[k] = new Vector2(Mathf.Cos(angle) * 0.7f + 0.55f, Mathf.Sin(angle)) * fragmentSpeed * (0.5f + Hash(k, 302 + hits.Count));
                    if (hit.edge) hit.velocity[k] = new Vector2(-Mathf.Abs(hit.velocity[k].x) * 1.3f - 0.4f * fragmentSpeed, hit.velocity[k].y * 1.4f);   // bate na parede e volta
                    hit.spin[k] = (Hash(k, 303) - 0.5f) * 720f;
                    // Pedaços grandes e pequenos; a maioria com a cor da peça (mais clara ou mais escura), alguns brancos/dourados (faíscas).
                    var big = Hash(k, 304) < 0.3f;
                    hit.size[k] = fragmentSize * (big ? 1.5f + 0.6f * Hash(k, 307) : 0.45f + 0.5f * Hash(k, 308));
                    var kind = Hash(k, 309);
                    if (hit.edge || !pieceColorFragments)
                        hit.color[k] = fragmentColors.Length > 0 ? fragmentColors[(int)(Hash(k, 305 + hits.Count) * fragmentColors.Length) % fragmentColors.Length] : Color.white;
                    else if (kind < 0.18f) hit.color[k] = Color.Lerp(white, gold, Hash(k, 310));
                    else if (kind < 0.55f) hit.color[k] = Color.Lerp(hit.baseColor, Color.white, 0.25f + 0.3f * Hash(k, 311));
                    else hit.color[k] = Color.Lerp(hit.baseColor, Color.black, 0.15f * Hash(k, 312));
                }
            }
            arrow.transform.rotation = Quaternion.Euler(0, 0, -artAngle);
            arrowGlow1.transform.rotation = arrowGlow2.transform.rotation = arrow.transform.rotation;
        }

        static void SetRim(SpriteRenderer sr, float radius)
        {
            var block = new MaterialPropertyBlock();
            sr.GetPropertyBlock(block);
            block.SetFloat("_Radius", radius);
            sr.SetPropertyBlock(block);
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

        static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
        static float EaseOutBack(float x)
        {
            x = Mathf.Clamp01(x);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        static Color A(Color c, float a) { c.a = a; return c; }
        static float Sw(Sprite s) => s == null ? 1f : Mathf.Max(0.01f, s.bounds.size.x);
        static float Sh(Sprite s) => s == null ? 1f : Mathf.Max(0.01f, s.bounds.size.y);

        float HeadX(float t)
        {
            if (t <= windup) return x0;
            var f = Mathf.Clamp01((t - windup) / flight);
            return Mathf.Lerp(x0, x1, Mathf.Pow(f, acceleration));
        }

        void Update()
        {
            if (manualTime) return;
            age += Time.deltaTime;
            Evaluate(age);
            if (age >= total) Finish();
        }

        /// Deslocamento da câmera no instante t: uma tremida leve durante o voo e uma forte no impacto da borda da tela.
        /// O jogo aplica na câmera principal; o preview aplica na câmera dele.
        public Vector3 CameraShakeOffset(float t)
        {
            var amp = 0f;
            var local = t - windup;
            if (shakeAmount > 0f && local >= 0f && local <= shakeSeconds) { var e = 1f - local / shakeSeconds; amp += shakeAmount * cell * e * e; }
            var impact = t - (windup + flight);
            if (edgeImpact && edgeShake > 0f && impact >= 0f && impact <= 0.45f) { var e = 1f - impact / 0.45f; amp += edgeShake * cell * e * e; }
            if (hitShake > 0f)
                foreach (var hit in hits)
                {
                    if (hit.edge) continue;
                    var d = t - hit.time;
                    if (d >= 0f && d <= 0.12f) { var e = 1f - d / 0.12f; amp += hitShake * cell * e * e; }
                }
            if (amp <= 0f) return Vector3.zero;
            return new Vector3((Mathf.PerlinNoise(t * 38f, 1.7f) - 0.5f) * 2f, (Mathf.PerlinNoise(3.1f, t * 41f) - 0.5f) * 2f, 0f) * amp;
        }

        void LateUpdate()
        {
            if (!shaking || shakeCamera == null) return;
            shakeCamera.transform.position = cameraBase + CameraShakeOffset(age);
        }

        void Finish()
        {
            if (shaking && shakeCamera != null) shakeCamera.transform.position = cameraBase;
            shaking = false;
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (shaking && shakeCamera != null) shakeCamera.transform.position = cameraBase;
        }

        void Place(SpriteRenderer sr, Vector3 center, float width, float height, Color color, float angle = 0f)
        {
            if (sr == null) return;
            sr.transform.position = center;
            sr.transform.rotation = Quaternion.Euler(0, 0, angle);
            sr.transform.localScale = new Vector3(Mathf.Max(0.001f, width) / Sw(sr.sprite), Mathf.Max(0.001f, height) / Sh(sr.sprite), 1f);
            sr.color = color;
        }

        /// Posiciona tudo no instante t (segundos desde o início). O jogo chama a cada quadro; o preview, quando quiser.
        public void Evaluate(float t)
        {
            var headX = freezeArrow ? freezeX : HeadX(t);
            var head = new Vector3(headX, y, z);
            var flightF = freezeArrow ? 0.5f : Mathf.Clamp01((t - windup) / flight);
            var flying = freezeArrow || t > windup;
            var windupF = freezeArrow ? 1f : windup > 0f ? Mathf.Clamp01(t / windup) : 1f;
            var tAfter = freezeArrow ? 0f : Mathf.Max(0f, t - (windup + flight));
            var tailFade = 1f - Smooth(tAfter / tailFadeSeconds);               // cauda some 0,2-0,35 s depois que a flecha sai
            var arrowVisible = freezeArrow ? 1f : (windup > 0f ? Smooth(t / windup * 2f) : 1f) * (1f - Smooth(tAfter / 0.1f));
            var scale = Mathf.Max(0.05f, EaseOutBack(windupF * 0.999f) * 0.8f + 0.2f);
            var pulse = 0.9f + 0.1f * Mathf.Sin(t * 24f);
            var length = arrowLengthCells * cell * scale;

            // Tela escura: entra rápido, fica durante todo o efeito, sai no fim.
            var dimIn = freezeArrow ? 1f : dimFadeSeconds > 0f ? Smooth(t / dimFadeSeconds) : 1f;
            var dimOut = freezeArrow ? 1f : 1f - Smooth((t - (total - dimFadeSeconds)) / Mathf.Max(0.01f, dimFadeSeconds));
            var cam = viewCamera;
            if (cam != null && dim != null)
            {
                var h = cam.orthographicSize * 2f * 1.4f;
                var w = h * Mathf.Max(1f, cam.aspect) * 1.1f;
                var camPos = shaking && shakeCamera != null ? cameraBase : cam.transform.position;
                Place(dim, new Vector3(camPos.x, camPos.y, z), w, h, A(Color.black, dimAlpha * dimIn * dimOut));
            }

            // Flecha (sprite original)
            arrow.transform.position = head;
            arrow.transform.localScale = Vector3.one * (length / Sw(arrowSprite));
            arrow.color = new Color(1, 1, 1, arrowVisible);

            // Brilho só no contorno da flecha: duas cópias aditivas da própria arte, um pouco maiores, atrás dela
            var arrowScale = length / Sw(arrowSprite);
            arrowGlow1.transform.position = arrowGlow2.transform.position = head;
            arrowGlow1.transform.localScale = Vector3.one * arrowScale;
            arrowGlow2.transform.localScale = Vector3.one * arrowScale;
            arrowGlow1.color = A(Color.Lerp(arrowGlowColor, white, 0.25f), arrowGlowAlpha * arrowVisible);
            arrowGlow2.color = A(arrowGlowColor, arrowGlowAlpha * 0.55f * arrowVisible * (0.88f + 0.12f * pulse));

            // Aura macia atrás do corpo e um brilhinho na ponta
            var tipPos = head + Vector3.right * (length * tipOffset);
            Place(auraSr, head, auraSizeCells * cell * pulse * scale, auraSizeCells * cell * 0.55f * pulse * scale, A(Color.Lerp(gold, white, 0.1f), auraAlpha * arrowVisible));
            Place(tipSr, tipPos, tipGlowSizeCells * cell * pulse * scale, tipGlowSizeCells * cell * pulse * scale, A(Color.Lerp(gold, white, 0.35f), tipGlowAlpha * arrowVisible));
            var whizzGrow = 0.75f + 0.35f * Mathf.Sin(t * 14f) * 0.5f + 0.4f * flightF;
            Place(whizzSr, tipPos + Vector3.right * (cell * 0.15f), whizzSizeCells * cell * whizzGrow, whizzSizeCells * cell * 0.45f * whizzGrow, A(Color.Lerp(white, gold, 0.4f), whizzAlpha * arrowVisible * (flying ? 1f : 0.3f)));

            // Cauda de luz: no mundo, atrás das penas, esticando durante o voo e sumindo depois que a flecha sai
            var travelled = Mathf.Max(0f, headX - x0);
            var tailLen = Mathf.Min(tailLengthCells * cell, travelled + cell * 0.5f) * (flying ? 1f : 0.15f) * Mathf.Max(0.001f, tailFade);
            var tailRight = headX - length * 0.28f;
            var tailCenter = new Vector3(tailRight - tailLen * 0.5f, y, z);
            var fade = freezeArrow ? 1f : tailFade * (windup > 0f ? Smooth(t / windup * 3f) : 1f);
            // Três camadas bem distintas: halo macio (largo e fraco), faixa dourada, núcleo branco fino e forte.
            if (taperedTail && tailStrands.Length == 4)
            {
                // Efeito 1: cauda amarela com brilho forte e halo pequeno, grossa na saída da flecha e afinando até quase nada.
                UpdateTaper(tailStrands[0], tailRight, tailLen, haloThickness, Color.Lerp(gold, white, 0.15f), haloAlpha * fade, 0.8f, t, 0f);
                UpdateTaper(tailStrands[1], tailRight, tailLen, glowThickness, gold, glowAlphaTail * fade, 0.7f, t, 1.3f);
                UpdateTaper(tailStrands[2], tailRight, tailLen * 0.96f, goldThickness, Color.Lerp(gold, white, 0.12f), goldAlpha * fade, 0.55f, t, 2.6f);
                UpdateTaper(tailStrands[3], tailRight, tailLen * 0.9f, coreThickness, white, coreAlpha * fade, 0.45f, t, 3.9f);
            }
            else if (haloSr != null)
            {
                Place(haloSr, tailCenter + Vector3.right * (tailLen * 0.08f), tailLen * 1.18f, haloThickness * cell * (proceduralHalo ? 1f : 1.6f), A(Color.Lerp(gold, white, 0.2f), haloAlpha * fade));
                Place(goldSr, tailCenter, tailLen * 0.92f, goldThickness * cell / GoldMaskFraction, A(gold, goldAlpha * fade));
                Place(coreSr, tailCenter + Vector3.right * (tailLen * 0.04f), tailLen * 0.96f, coreThickness * cell / CoreMaskFraction, A(white, coreAlpha * fade));
            }

            // Fitas mágicas: curvas suaves com fases diferentes, atrás do núcleo
            if (helixRibbons && strands.Length == 10 && strandsFront.Length == 10)
            {
                // Efeito 2: espiral roxa que sai da PONTA da flecha, envolve o corpo (passa por trás e pela frente) e gira até o
                // fim da cauda, afunilando. Várias linhas: umas largas e macias (halo), outras finas e nítidas (glow).
                var pink = Color.Lerp(magenta, white, 0.5f);
                var startX = tipPos.x;
                var endX = Mathf.Min(startX - cell * 0.5f, tailRight - tailLen);
                UpdateStrandPair(0, startX, endX, t, 0f, helixAmplitude, helixThickness * helixHaloScale, magenta, helixHaloAlpha * arrowVisible * Mathf.Max(fade, 0.0001f), 1f);
                UpdateStrandPair(1, startX, endX, t, Mathf.PI, helixAmplitude, helixThickness * helixHaloScale, violet, helixHaloAlpha * arrowVisible * Mathf.Max(fade, 0.0001f), 1f);
                UpdateStrandPair(2, startX, endX, t, Mathf.PI * 0.5f, helixAmplitude * 0.72f, helixThickness * helixHaloScale * 0.8f, violet, helixHaloAlpha * 0.8f * arrowVisible * fade, 1f);
                UpdateStrandPair(3, startX, endX, t, Mathf.PI * 1.5f, helixAmplitude * 0.72f, helixThickness * helixHaloScale * 0.8f, magenta, helixHaloAlpha * 0.8f * arrowVisible * fade, 1f);
                UpdateStrandPair(4, startX, endX, t, 0f, helixAmplitude, helixThickness, magenta, helixAlpha * arrowVisible * fade, 1f);
                UpdateStrandPair(5, startX, endX, t, Mathf.PI, helixAmplitude, helixThickness, violet, helixAlpha * arrowVisible * fade, 1f);
                UpdateStrandPair(6, startX, endX, t, Mathf.PI * 0.5f, helixAmplitude * 0.72f, helixThickness * 0.65f, violet, helixAlpha * 0.9f * arrowVisible * fade, 1f);
                UpdateStrandPair(7, startX, endX, t, Mathf.PI * 1.5f, helixAmplitude * 0.72f, helixThickness * 0.65f, magenta, helixAlpha * 0.9f * arrowVisible * fade, 1f);
                UpdateStrandPair(8, startX, endX, t, 0.5f, helixAmplitude * 0.5f, helixThickness * 0.5f, pink, helixAlpha * 0.85f * arrowVisible * fade, -1f);
                UpdateStrandPair(9, startX, endX, t, Mathf.PI + 0.5f, helixAmplitude * 0.5f, helixThickness * 0.5f, pink, helixAlpha * 0.85f * arrowVisible * fade, -1f);
            }
            else if (ribTop != null)
            {
                Ribbon(ribTop, tailCenter, tailLen, t, 0.0f, 0.9f, magenta, ribbonAlpha * fade, +1f);
                Ribbon(ribBottom, tailCenter, tailLen * 0.95f, t, 2.1f, 1.15f, violet, ribbonAlpha * 0.9f * fade, -1f);
                Ribbon(ribMid, tailCenter, tailLen * 0.8f, t, 4.0f, 0.7f, Color.Lerp(magenta, white, 0.45f), ribbonAlpha * 0.7f * fade, +0.5f);
                Ribbon(ribHaloA, tailCenter, tailLen, t, 0.3f, 0.9f, magenta, ribbonHaloAlpha * fade, +1f);
                Ribbon(ribHaloB, tailCenter, tailLen * 0.92f, t, 2.4f, 1.15f, violet, ribbonHaloAlpha * 0.8f * fade, -1f);
            }
            // Efeito 5: aura de vento mágico que sai da ponta e vai afunilando até o fim da cauda (bem transparente)
            // Efeito 6: onda em concha, dois arcos que abrem da cabeça para trás, como uma quebra de atmosfera
            {
                var coneStart = tipPos.x;
                var coneEnd = Mathf.Min(coneStart - cell * 0.8f, tailRight - tailLen);
                var visible = arrowVisible * fade;
                if (windStrands.Length == 2)
                {
                    UpdateWind(windStrands[0], coneStart, coneEnd, windWidth * 1.35f, Color.Lerp(blue, violet, 0.35f), windAlpha * 0.8f * visible, 0f, t);
                    UpdateWind(windStrands[1], coneStart, coneEnd, windWidth * 0.85f, Color.Lerp(violet, magenta, 0.4f), windAlpha * visible, 2.1f, t);
                }
                if (shellStrands.Length == 4)
                {
                    var shellColor = Color.Lerp(white, gold, 0.2f);
                    UpdateShell(shellStrands[0], coneStart, coneEnd, +1f, shellSpread, shellThickness, shellColor, shellAlpha * visible, 0f, t);
                    UpdateShell(shellStrands[1], coneStart, coneEnd, -1f, shellSpread, shellThickness, shellColor, shellAlpha * visible, 0f, t);
                    UpdateShell(shellStrands[2], coneStart, coneEnd, +1f, shellSpread * 0.6f, shellThickness * 0.7f, Color.Lerp(white, violet, 0.3f), shellAlpha * 0.7f * visible, 1.6f, t);
                    UpdateShell(shellStrands[3], coneStart, coneEnd, -1f, shellSpread * 0.6f, shellThickness * 0.7f, Color.Lerp(white, violet, 0.3f), shellAlpha * 0.7f * visible, 1.6f, t);
                }
            }
            Place(residueSr, tailCenter - Vector3.right * (tailLen * 0.12f), tailLen * 1.1f, ribbonHeight * cell * 0.9f, A(Color.Lerp(violet, white, 0.3f), residueAlpha * fade));

            // Riscos de velocidade ao lado da cauda
            for (var i = 0; i < streaks.Length; i++)
            {
                var s = streaks[i];
                var cycle = 0.14f + 0.14f * Hash(i, 41);
                var phase = Mathf.Repeat(t / cycle + Hash(i, 42), 1f);
                var side = ((i % 2 == 0) ? 1f : -1f) * cell * (0.28f + 0.38f * Hash(i, 43));
                var px = headX - length * 0.4f - phase * cell * 2.2f;
                var a = Mathf.Sin(Mathf.PI * phase) * (flying ? 1f : 0f) * fade;
                Place(s, new Vector3(px, y + side, z), streakLengthCells * cell * (0.6f + 0.6f * Hash(i, 44)), cell * 0.07f, A(Color.Lerp(white, gold, 0.4f), 0.7f * a));
            }

            // Estrelinhas douradas que piscam e somem em momentos variados, deixadas no caminho
            // Efeito 3: estrelinhas que nascem no eixo da cauda e saem para fora (para cima ou para baixo) enquanto brilham e somem,
            // como os pontinhos da ponta. Cada uma tem halo redondo macio, miolo forte e (algumas) as quatro pontas finas.
            for (var i = 0; i < sparkles.Length; i++)
            {
                var halo = sparkles[i]; var star = sparkleStars[i]; var coreDot = sparkleCores[i];
                var cycleLen = sparkleLife * (0.8f + 0.5f * Hash(i, 51));
                var phaseS = t / cycleLen + Hash(i, 52);
                var cycleS = Mathf.FloorToInt(phaseS);
                var f = phaseS - cycleS;
                var seedS = i * 211 + cycleS;
                var emitT = (cycleS - Hash(i, 52)) * cycleLen;                      // instante em que este brilho nasceu
                var sAlong = Mathf.Pow(Hash(seedS, 53), 0.9f);
                var anchor = freezeArrow ? headX : HeadX(Mathf.Max(0f, emitT));    // fica no lugar do mundo onde nasceu (a flecha passa)
                var px = anchor - length * 0.3f - sAlong * Mathf.Max(tailLen, cell * 0.6f) * 0.9f;
                var dirSign = Hash(seedS, 54) < 0.5f ? -1f : 1f;
                var spreadMax = cell * (0.28f + 0.5f * Hash(seedS, 55)) * (1f - 0.5f * sAlong);
                var outward = 1f - (1f - f) * (1f - f);
                var pos = new Vector3(px - cell * 0.25f * outward * Hash(seedS, 56), y + dirSign * spreadMax * outward, z);
                var tw = Mathf.Sin(Mathf.Pow(f, 0.6f) * Mathf.PI) * fade * ((!flying && windup > 0f && t < windup * 0.5f) ? 0f : 1f);
                var size = sparkleSize * cell * (0.5f + 0.9f * Hash(i, 56)) * (1f - 0.4f * f);
                // Pontos nítidos (sem halo), em azul claro, branco e dourado; alguns ganham as quatro pontas finas.
                var pick = Hash(i, 57);
                var tint = pick < 0.5f ? blue : pick < 0.75f ? white : gold;
                halo.color = A(Color.white, 0f);
                Place(coreDot, pos, size * 0.9f, size * 0.9f, A(Color.Lerp(tint, white, 0.25f), tw));
                var hasStar = Hash(i, 58) < 0.3f;
                Place(star, pos, size * 2.2f, size * 2.2f, A(Color.Lerp(tint, white, 0.4f), hasStar ? 0.75f * tw : 0f), f * 60f + Hash(i, 55) * 45f);
            }

            // Efeito 4: pontinhos de energia saindo do meio da cabeça para fora, em círculo, pulsando (halo + miolo forte)
            for (var i = 0; i < pulseDots.Length; i++)
            {
                var ph = t / headPulseLife + Hash(i, 71);
                var cy = Mathf.FloorToInt(ph);
                var f = ph - cy;
                var seedP = i * 97 + cy;
                var ang = (i / (float)Mathf.Max(1, pulseDots.Length)) * Mathf.PI * 2f + Hash(seedP, 72) * 0.5f;
                var dist = headPulseRadius * cell * (0.2f + 0.8f * (1f - (1f - f) * (1f - f))) * (0.7f + 0.3f * Hash(seedP, 73));
                var origin = head + Vector3.right * (length * 0.22f);
                var pos = origin + new Vector3(Mathf.Cos(ang) * dist, Mathf.Sin(ang) * dist * 0.85f, 0f);
                var size = headPulseSize * cell * (0.6f + 0.8f * Hash(seedP, 74)) * (1f - 0.5f * f);
                var alphaP = headPulseAlpha * Mathf.Sin(Mathf.PI * f) * arrowVisible;
                Place(pulseDots[i], pos, size * 1.8f, size * 1.8f, A(gold, 0.14f * alphaP));
                Place(pulseCores[i], pos, size * 0.8f, size * 0.8f, A(white, alphaP));
            }

            // Casas atingidas: destrói a peça, solta fragmentos, microflash + anel + onda lateral
            foreach (var hit in hits)
            {
                if (!hit.done && t >= hit.time && !freezeArrow)
                {
                    hit.done = true;
                    if (hit.index >= 0) onHitCallback?.Invoke(hit.index);
                }
                var a = t - hit.time;
                var pos = new Vector3(hit.position.x, y, z);

                var m = hit.edge ? edgeImpactScale : 1f;
                var fa = a / (flashSeconds * (hit.edge ? 1.8f : 1f));
                if (a >= 0f && fa <= 1f) Place(hit.flash, pos, flashSizeCells * m * cell * (0.7f + 0.5f * fa), flashSizeCells * m * cell * (0.7f + 0.5f * fa), A(white, (1f - fa) * (1f - fa)));
                else hit.flash.color = A(white, 0f);

                var ra = a / (ringSeconds * (hit.edge ? 1.6f : 1f));
                if (a >= 0f && ra <= 1f) Place(hit.ring, pos, ringSizeCells * m * cell * Mathf.Lerp(0.3f, 1f, 1f - (1f - ra) * (1f - ra)), ringSizeCells * m * cell * Mathf.Lerp(0.3f, 1f, 1f - (1f - ra) * (1f - ra)), A(Color.Lerp(white, gold, ra), 0.9f * (1f - ra)));
                else hit.ring.color = A(white, 0f);

                var wa = a / (waveSeconds * (hit.edge ? 1.5f : 1f));
                if (a >= 0f && wa <= 1f)
                {
                    var spread = 1f - (1f - wa) * (1f - wa);
                    Place(hit.wave, pos, waveReach * m * 2f * cell * Mathf.Max(0.1f, spread), cell * 0.12f * m * (1.2f - wa), A(Color.Lerp(white, gold, wa), waveAlpha * (1f - wa) * (1f - wa)), 90f);
                }
                else hit.wave.color = A(white, 0f);

                // Estouro colorido: clarão da cor da peça + raios que saem do centro em todas as direções
                if (!hit.edge && hit.rays != null)
                {
                    var bf = a / burstSeconds;
                    var burstOn = a >= 0f && bf <= 1f;
                    var ease = 1f - (1f - Mathf.Clamp01(bf)) * (1f - Mathf.Clamp01(bf));
                    var glowColor = Color.Lerp(hit.baseColor, white, 0.3f);
                    if (burstOn) Place(hit.puff, pos, burstGlowSize * cell * (0.5f + 0.8f * ease), burstGlowSize * cell * (0.5f + 0.8f * ease), A(glowColor, (1f - bf) * (1f - bf) * 0.85f));
                    else hit.puff.color = A(white, 0f);
                    for (var r = 0; r < hit.rays.Length; r++)
                    {
                        var ray = hit.rays[r];
                        if (!burstOn) { ray.color = A(white, 0f); continue; }
                        var ang = (r / (float)hit.rays.Length) * 360f + (Hash(r, hit.index + 400) - 0.5f) * 24f;
                        var len = burstRayLength * cell * (0.45f + 0.55f * ease) * (0.7f + 0.5f * Hash(r, 401));
                        var dir = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad), 0f);
                        Place(ray, pos + dir * (len * 0.5f + cell * 0.12f * ease), len, cell * 0.07f * (1f - bf), A(Color.Lerp(hit.baseColor, white, 0.55f), Mathf.Pow(1f - bf, 1.4f) * 0.95f), ang);
                    }
                }
                else if (hit.puff != null) hit.puff.color = A(white, 0f);

                for (var k = 0; k < hit.frags.Length; k++)
                {
                    var fr = hit.frags[k];
                    var life = fragmentLife * (0.8f + 0.4f * Hash(k, 306));
                    if (a < 0f || a > life) { fr.color = A(Color.white, 0f); continue; }
                    var f = a / life;
                    var p = new Vector3(pos.x + hit.velocity[k].x * cell * a, pos.y + hit.velocity[k].y * cell * a - 0.5f * fragmentGravity * cell * a * a, z);
                    fr.transform.position = p;
                    fr.transform.rotation = Quaternion.Euler(0, 0, hit.spin[k] * a);
                    var size = hit.size[k] * cell * (1f - 0.35f * f);
                    fr.transform.localScale = new Vector3(size / Sw(fr.sprite), size / Sh(fr.sprite), 1f);
                    fr.color = A(hit.color[k], 1f - Smooth((f - 0.55f) / 0.45f));
                }
            }
        }

        void Ribbon(SpriteRenderer sr, Vector3 center, float length, float t, float phase, float speed, Color color, float alpha, float sign)
        {
            var wave = Mathf.Sin(t * 5.5f * speed + phase);
            var breathe = 1f + 0.15f * Mathf.Sin(t * 3.1f * speed + phase * 1.7f);
            var shimmer = 0.75f + 0.25f * Mathf.Sin(t * 7f * speed + phase * 2.3f);
            Place(sr, center + Vector3.up * (wave * ribbonWave * cell * sign), length, ribbonHeight * cell * breathe, A(color, alpha * shimmer));
        }
    }
}
