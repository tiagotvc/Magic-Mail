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
            MulticolorSpinTrail.Prewarm();
            ElectricChargeParticles.Prewarm();
            PowerUpEnergyExit.Prewarm();
        }
    }

    public class Lightning : MonoBehaviour
    {
        public float amplitude = 1.3f;
        public Vector2 pos1, pos2;
        public bool perlin = false;
        public const float TravelTime = .045f;
        public const float VisualLifetime = .21f;
        static readonly Stack<Lightning> available = new Stack<Lightning>(ElectricEffects.Capacity);
        static Transform poolRoot;
        static float nextSeed;
        LineRenderer core, halo;
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
            halo = NewLine("ElectricHalo", new Color(1f, .76f, .42f, .4f), 3);
            seed = nextSeed += .618034f;
            initialized = true;
        }
        public void SetLight(Vector3 from, Vector3 to, float cell = .82f, bool hold = false,
            Transform source = null, Transform target = null)
        {
            Initialize(); leased = true;
            pos1 = from; pos2 = to; elapsed = 0;
            cellSize = Mathf.Max(.01f, cell);
            seed = nextSeed += .618034f;
            held = hold; sourceAnchor = source; targetAnchor = target;
            core.widthMultiplier = cellSize * .13f;
            halo.widthMultiplier = cellSize * .24f;
            core.enabled = halo.enabled = true;
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
            line.widthCurve = new AnimationCurve(new Keyframe(0,.75f),new Keyframe(.12f,1),new Keyframe(.85f,1),new Keyframe(1,.65f));
            line.startColor = line.endColor = color;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 4; line.numCornerVertices = 3;
            line.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            line.sortingOrder = PowerUpEffectOrder.Value + order;
        }
        public void Sample(float time)
        {
            if (!initialized) return;
            // Fading is decorative only. Keep the lease until its owning selection releases it,
            // so a recycled beam cannot be cleared later by a different bomb's EndSpin.
            bool visible = time < VisualLifetime;
            core.enabled = halo.enabled = visible;
            if (!visible) return;
            float fade = 1f-Mathf.SmoothStep(.11f,VisualLifetime,time);
            core.startColor=core.endColor=new Color(1, .98f, .92f, fade);
            halo.startColor=halo.endColor=new Color(1, .77f, .43f, .4f*fade);
            Vector3 from = sourceAnchor != null ? sourceAnchor.position : (Vector3)pos1;
            Vector3 to = targetAnchor != null ? targetAnchor.position : (Vector3)pos2;
            var delta = to - from;
            var side = new Vector3(-delta.y, delta.x, 0).normalized;
            float head = Mathf.Clamp01(time / TravelTime);
            float length = delta.magnitude;
            float drift = time * 5.5f;
            float spread = Mathf.Min(length * .23f, cellSize * .48f);
            float lobes = Mathf.Clamp(length / cellSize * .75f, 1.7f, 5.5f);
            for (int i = 0; i < points.Length; i++)
            {
                float t = i / (float)(points.Length - 1) * head;
                // Independent evolving bends per beam, tapered at both anchors.
                // Two noise scales avoid a regular sine wave or frame-to-frame jitter.
                float broad = (Mathf.PerlinNoise(seed * 7.13f + t * lobes, seed + drift) - .5f) * 2f;
                float detail = (Mathf.PerlinNoise(seed * 3.71f + t * lobes * 2.8f, seed + 19f - drift * 1.3f) - .5f) * 2f;
                float envelope = Mathf.Pow(Mathf.Sin(t * Mathf.PI), .65f);
                points[i] = from + delta * t + side * (broad + detail * .32f) * spread * envelope;
            }
            points[0] = from;
            if (head >= 1) points[points.Length - 1] = to;
            core.positionCount = halo.positionCount = points.Length;
            core.SetPositions(points); halo.SetPositions(points);
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
            sourceAnchor = targetAnchor = null;
            gameObject.SetActive(false);
            if (pooled) available.Push(this);
        }
    }

    // Pooled elliptical light trail; its rotation follows the existing sprite spin period.
    public sealed class MulticolorSpinTrail : MonoBehaviour
    {
        static readonly Stack<MulticolorSpinTrail> available = new Stack<MulticolorSpinTrail>(16);
        static Transform poolRoot;
        readonly Vector3[] points = new Vector3[56];
        LineRenderer core, glow;
        Transform anchor;
        float cell, age, period;
        bool manualPhase;
        public bool IsLeased { get; private set; }

        internal static void Prewarm()
        {
            if (poolRoot != null) return;
            poolRoot = ElectricEffects.Root; available.Clear();
            for (int i=0;i<16;i++)
            {
                var node=new GameObject("MulticolorSpinTrail");node.transform.SetParent(poolRoot,false);
                var trail=node.AddComponent<MulticolorSpinTrail>();
                trail.core=trail.MakeLine("SpinCore",.095f,5);
                trail.glow=trail.MakeLine("SpinGlow",.17f,3);
                node.SetActive(false);available.Push(trail);
            }
        }
        LineRenderer MakeLine(string name,float width,int order)
        {
            var node=new GameObject(name);node.transform.SetParent(transform,false);
            var line=node.AddComponent<LineRenderer>();line.sharedMaterial=ElectricEffects.Ribbon;
            line.useWorldSpace=true;line.positionCount=points.Length;line.widthMultiplier=width;
            line.numCapVertices=4;line.numCornerVertices=3;
            line.sortingLayerName=PowerUpEffectOrder.SortingLayer;line.sortingOrder=PowerUpEffectOrder.Value+order;
            line.widthCurve=new AnimationCurve(new Keyframe(0,.08f),new Keyframe(.65f,.8f),new Keyframe(1,1));
            var gradient=new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(new Color(.38f,.8f,1),0),
                new GradientColorKey(new Color(.86f,.57f,1),.35f),
                new GradientColorKey(new Color(1,.84f,.52f),.75f),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(name=="SpinGlow"?.22f:.8f,.65f),
                    new GradientAlphaKey(name=="SpinGlow"?.32f:1,1)});
            line.colorGradient=gradient;return line;
        }
        public static MulticolorSpinTrail Rent(Transform source,float cell,float period)
        {
            Prewarm();if(available.Count==0)return null;
            var trail=available.Pop();trail.anchor=source;trail.cell=cell;
            trail.period=Mathf.Max(.12f,period);trail.age=0;trail.manualPhase=false;trail.IsLeased=true;
            trail.core.widthMultiplier=cell*.095f;trail.glow.widthMultiplier=cell*.17f;
            trail.gameObject.SetActive(true);trail.Sample(0);return trail;
        }
        public void FollowPhase(float revolutions) {manualPhase=true;Sample(revolutions*period);}
        public void Sample(float time)
        {
            if(anchor==null)return;
            float head=time/period*Mathf.PI*2;
            var center=anchor.position+Vector3.down*(cell*.12f);
            for(int i=0;i<points.Length;i++)
            {
                float angle=head-(1-i/(float)(points.Length-1))*Mathf.PI*1.6f;
                points[i]=center+new Vector3(Mathf.Cos(angle)*cell*.48f,Mathf.Sin(angle)*cell*.21f,0);
            }
            core.SetPositions(points);glow.SetPositions(points);
        }
        void Update()
        {
            if(!IsLeased)return;
            if(anchor==null || !anchor.gameObject.activeInHierarchy){Release();return;}
            if(!manualPhase) {age+=Time.deltaTime;Sample(age);}
        }
        public void Release()
        {
            if(!IsLeased)return;
            IsLeased=false;anchor=null;gameObject.SetActive(false);available.Push(this);
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
            Rent(0, target.position, target, cell, new Color(1f, .85f, .52f));
        public static void Flash(Vector3 position, float cell) => Rent(2, position, null, cell, new Color(1f, .86f, .57f));
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
            emission.rateOverTime = mode == 0 ? 7 : 0;
            emission.SetBursts(new[] {new ParticleSystem.Burst(0, (short)(mode == 0 ? 3 : mode == 1 ? 14 : 1))});
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
