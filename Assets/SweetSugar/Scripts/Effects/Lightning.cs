using System.Collections.Generic;
using UnityEngine;
using RoyalAves.Effects;

namespace SweetSugar.Scripts.Effects
{
    // Shared pools are warmed at scene startup; activation only borrows existing renderers.
    static class ElectricEffects
    {
        public const int Capacity = 128;
        static Transform root;
        public static Material Ribbon, Star, Ring;
        public static Transform Root
        {
            get
            {
                if (root != null) return root;
                var node = new GameObject("ElectricEffectsPool");
                root = node.transform;
                if (Application.isPlaying) Object.DontDestroyOnLoad(node);
                Ribbon = Resources.Load<Material>("ElectricEffects/Ribbon");
                Star = Resources.Load<Material>("ElectricEffects/Star");
                Ring = Resources.Load<Material>("ElectricEffects/Ring");
                return root;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Warmup()
        {
            var unused = Root;
            Lightning.Prewarm();
            ElectricChargeParticles.Prewarm();
            PowerUpEnergyExit.Prewarm();
        }
    }

    public class Lightning : MonoBehaviour
    {
        public float amplitude = 1.3f;
        public Vector2 pos1, pos2;
        public bool perlin = true;
        public const float TravelTime = .045f;
        static readonly Stack<Lightning> available = new Stack<Lightning>(ElectricEffects.Capacity);
        static Transform poolRoot;
        static float nextSeed;
        LineRenderer core, halo;
        readonly LineRenderer[] branches = new LineRenderer[2];
        readonly Vector3[] points = new Vector3[64];
        Transform sourceAnchor, targetAnchor;
        float cellSize, elapsed, seed;
        bool held, initialized, leased, pooled;

        internal static void Prewarm()
        {
            if (poolRoot != null) return;
            poolRoot = ElectricEffects.Root;
            available.Clear();
            for (int i = 0; i < ElectricEffects.Capacity; i++)
            {
                var node = new GameObject("MulticolorElectricArc");
                node.transform.SetParent(ElectricEffects.Root, false);
                var arc = node.AddComponent<Lightning>();
                arc.pooled = true; arc.Initialize(); node.SetActive(false); available.Push(arc);
            }
        }
        public static Lightning Rent()
        {
            Prewarm();
            // More simultaneous targets than the board capacity: skip the decoration, never gameplay.
            if (available.Count == 0) return null;
            var arc = available.Pop(); arc.leased = true; arc.gameObject.SetActive(true); return arc;
        }
        void Initialize()
        {
            if (initialized) return;
            var unused = ElectricEffects.Root;
            core = GetComponent<LineRenderer>();
            if (core == null) core = gameObject.AddComponent<LineRenderer>();
            Configure(core, Color.white, 5);
            halo = NewLine("ElectricHalo", new Color(.16f, .55f, 1f, .42f), 3);
            for (int i = 0; i < branches.Length; i++)
                branches[i] = NewLine("ElectricBranch", new Color(.4f, .8f, 1f, .65f), 4);
            seed = nextSeed += .618034f;
            initialized = true;
        }
        public void SetLight(Vector3 from, Vector3 to, float cell = .82f, bool hold = false,
            Transform source = null, Transform target = null)
        {
            Initialize(); leased = true;
            pos1 = from; pos2 = to; elapsed = 0;
            cellSize = Mathf.Max(.01f, cell);
            held = hold; sourceAnchor = source; targetAnchor = target;
            core.widthMultiplier = cellSize * .045f;
            halo.widthMultiplier = cellSize * .15f;
            core.enabled = halo.enabled = true;
            foreach (var branch in branches) { branch.widthMultiplier = cellSize * .024f; branch.enabled = true; }
            Sample(0);
        }
        LineRenderer NewLine(string label, Color color, int order)
        {
            var node = new GameObject(label); node.transform.SetParent(transform, false);
            var line = node.AddComponent<LineRenderer>(); Configure(line, color, order); return line;
        }
        void Configure(LineRenderer line, Color color, int order)
        {
            line.sharedMaterial = ElectricEffects.Ribbon;
            line.useWorldSpace = true;
            line.widthCurve = AnimationCurve.Linear(0, 1, 1, .55f);
            line.startColor = line.endColor = color;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 0; line.numCornerVertices = 0;
            line.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            line.sortingOrder = PowerUpEffectOrder.Value + order;
        }
        public void Sample(float time)
        {
            if (!initialized) return;
            Vector3 from = sourceAnchor != null ? sourceAnchor.position : (Vector3)pos1;
            Vector3 to = targetAnchor != null ? targetAnchor.position : (Vector3)pos2;
            var delta = to - from;
            var side = new Vector3(-delta.y, delta.x, 0).normalized;
            float head = Mathf.Clamp01(time / TravelTime);
            float bend = Mathf.Min(delta.magnitude * .11f, cellSize * .65f) * Mathf.Sin(seed * 3.7f);
            var control1 = from + delta * .28f + side * bend;
            var control2 = from + delta * .72f + side * bend * .7f;
            for (int i = 0; i < points.Length; i++)
            {
                float t = i / (float)(points.Length - 1) * head, u = 1 - t;
                float envelope = Mathf.Sin(t * Mathf.PI);
                float noise = (Mathf.PerlinNoise(seed + t * 16, time * 22) - .5f) * .13f;
                noise += Mathf.Sin(t * 85 + time * 58 + seed) * .018f;
                points[i] = u*u*u*from + 3*u*u*t*control1 + 3*u*t*t*control2 + t*t*t*to
                    + side * (perlin ? noise * cellSize * amplitude * envelope : 0);
            }
            points[0] = from;
            if (head >= 1) points[points.Length - 1] = to;
            core.positionCount = halo.positionCount = points.Length;
            core.SetPositions(points); halo.SetPositions(points);
            for (int i = 0; i < branches.Length; i++)
            {
                int at = 18 + i * 23;
                var start = points[at];
                var end = start + delta.normalized * cellSize * .13f + side * cellSize * .16f * (i == 0 ? 1 : -1);
                var line = branches[i];
                line.enabled = head >= 1 && Mathf.Sin(time * 39 + seed + i * 2) > .15f;
                line.positionCount = 3;
                line.SetPosition(0, start);
                line.SetPosition(1, Vector3.Lerp(start, end, .55f) - delta.normalized * cellSize * .045f);
                line.SetPosition(2, end);
            }
        }
        void Update()
        {
            if (!leased) return;
            elapsed += Time.deltaTime;
            if ((sourceAnchor != null && !sourceAnchor.gameObject.activeInHierarchy) ||
                (targetAnchor != null && !targetAnchor.gameObject.activeInHierarchy) || elapsed >= (held ? 15 : .5f))
            { Release(); return; }
            Sample(elapsed);
        }
        public void Release()
        {
            if (!leased) return;
            leased = false; core.enabled = halo.enabled = false;
            foreach (var branch in branches) branch.enabled = false;
            sourceAnchor = targetAnchor = null;
            gameObject.SetActive(false);
            if (pooled) available.Push(this);
        }
    }

    // A quick outward pop continues independently after the source enters its gameplay pool.
    public sealed class PowerUpEnergyExit : MonoBehaviour
    {
        static readonly Stack<PowerUpEnergyExit> available = new Stack<PowerUpEnergyExit>(32);
        static Transform poolRoot;
        SpriteRenderer image;
        Vector3 originalScale;
        Color originalColor;
        float age;
        internal static void Prewarm()
        {
            if (poolRoot != null) return;
            poolRoot = ElectricEffects.Root;
            available.Clear();
            for (int i = 0; i < 32; i++)
            {
                var node = new GameObject("MulticolorExitPop");
                node.transform.SetParent(poolRoot, false);
                var effect = node.AddComponent<PowerUpEnergyExit>();
                effect.image = node.AddComponent<SpriteRenderer>();
                node.SetActive(false); available.Push(effect);
            }
        }
        public static void Play(SpriteRenderer source)
        {
            Prewarm();
            if (available.Count == 0) return;
            var effect = available.Pop();
            effect.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            effect.originalScale = source.transform.lossyScale;
            effect.originalColor = source.color;
            effect.image.sprite = source.sprite;
            effect.image.sharedMaterial = source.sharedMaterial;
            effect.image.flipX = source.flipX; effect.image.flipY = source.flipY;
            effect.image.sortingLayerID = source.sortingLayerID;
            effect.image.sortingOrder = Mathf.Max(source.sortingOrder, PowerUpEffectOrder.Value + 6);
            effect.age = 0; effect.Apply(0); effect.gameObject.SetActive(true);
        }
        void Apply(float t)
        {
            float scale = Mathf.Lerp(1, 1.38f, 1 - Mathf.Pow(1 - t, 2));
            transform.localScale = originalScale * scale;
            var color = Color.Lerp(originalColor, Color.white, Mathf.Clamp01(t * 4));
            color.a = originalColor.a * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .4f) / .6f)));
            image.color = color;
        }
        void Update()
        {
            age += Time.deltaTime;
            Apply(Mathf.Clamp01(age / .12f));
            if (age < .12f) return;
            image.sprite = null; gameObject.SetActive(false); available.Push(this);
        }
    }

    public sealed class ElectricChargeParticles : MonoBehaviour
    {
        static readonly Stack<ElectricChargeParticles>[] pools = {
            new Stack<ElectricChargeParticles>(128), new Stack<ElectricChargeParticles>(128), new Stack<ElectricChargeParticles>(16) };
        static Transform poolRoot;
        ParticleSystem particles;
        Transform anchor;
        int mode;
        bool leased;
        float remaining;
        internal static void Prewarm()
        {
            if (poolRoot != null) return;
            poolRoot = ElectricEffects.Root;
            foreach (var pool in pools) pool.Clear();
            for (int type = 0; type < 3; type++)
                for (int i = 0; i < (type == 2 ? 16 : 128); i++)
                {
                    var node = new GameObject(type == 0 ? "MarkedPieceElectricSparks" : type == 1 ? "ElectricDischargeBurst" : "ElectricActivationRing");
                    node.transform.SetParent(ElectricEffects.Root, false);
                    var effect = node.AddComponent<ElectricChargeParticles>();
                    effect.mode = type; effect.Initialize(); node.SetActive(false); pools[type].Push(effect);
                }
        }
        static ElectricChargeParticles Rent(int type, Vector3 position, Transform target, float cell, Color color)
        {
            Prewarm();
            if (pools[type].Count == 0) return null;
            var effect = pools[type].Pop(); effect.leased = true; effect.anchor = target;
            effect.transform.position = position; effect.gameObject.SetActive(true);
            effect.Play(cell, color); return effect;
        }
        public static ElectricChargeParticles Charge(Transform target, float cell) =>
            Rent(0, target.position, target, cell, new Color(.38f, .8f, 1));
        public static void Flash(Vector3 position, float cell) => Rent(2, position, null, cell, new Color(.5f, .85f, 1));
        public static void Discharge(Vector3 position, float cell) => Discharge(position, cell, new Color(.35f, .8f, 1));
        public static void Discharge(Vector3 position, float cell, Color color) => Rent(1, position, null, cell, color);
        void Initialize()
        {
            particles = gameObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = mode == 0; main.duration = 1; main.playOnAwake = false;
            main.startLifetime = mode == 2 ? new ParticleSystem.MinMaxCurve(.22f) : new ParticleSystem.MinMaxCurve(.2f, .4f);
            main.maxParticles = 24; main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI);
            var shape = particles.shape;
            shape.enabled = mode != 2; shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radiusThickness = mode == 0 ? .08f : 1;
            var emission = particles.emission;
            emission.rateOverTime = mode == 0 ? 18 : 0;
            emission.SetBursts(new[] {new ParticleSystem.Burst(0, (short)(mode == 0 ? 7 : mode == 1 ? 14 : 1))});
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = mode == 0; velocity.orbitalZ = 3;
            var size = particles.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, mode == 2
                ? AnimationCurve.EaseInOut(0, .3f, 1, 2.3f)
                : new AnimationCurve(new Keyframe(0, .35f), new Keyframe(.18f, 1), new Keyframe(1, 0)));
            var fade = particles.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] {new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1)},
                new[] {new GradientAlphaKey(1, 0), new GradientAlphaKey(.8f, .25f), new GradientAlphaKey(0, 1)});
            fade.color = gradient;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mode == 2 ? ElectricEffects.Ring : ElectricEffects.Star;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            renderer.sortingOrder = PowerUpEffectOrder.Value + 7;
        }
        void Play(float cell, Color color)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.startSpeed = mode == 1 ? new ParticleSystem.MinMaxCurve(cell * .9f, cell * 2.2f) : new ParticleSystem.MinMaxCurve(0);
            main.startSize = mode == 2 ? new ParticleSystem.MinMaxCurve(cell) : new ParticleSystem.MinMaxCurve(cell * .07f, cell * .15f);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.Lerp(color, Color.white, .2f), Color.white);
            var shape = particles.shape; shape.radius = cell * (mode == 0 ? .35f : .06f);
            remaining = mode == 2 ? .25f : .5f;
            particles.Play();
        }
        void Update()
        {
            if (!leased) return;
            if (mode == 0)
            {
                if (anchor == null || !anchor.gameObject.activeInHierarchy) { Clear(); return; }
                transform.position = anchor.position;
            }
            else { remaining -= Time.deltaTime; if (remaining <= 0) Clear(); }
        }
        public void Clear()
        {
            if (!leased) return;
            leased = false; anchor = null;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            gameObject.SetActive(false); pools[mode].Push(this);
        }
    }
}
