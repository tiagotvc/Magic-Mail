using System;
using System.Collections.Generic;
using SweetSugar.LeanTween.Framework;
using UnityEngine;

namespace RoyalAves.Effects
{
    // Correio Mágico: quando o foguete (HORIZONTAL_STRIPED/VERTICAL_STRIPED) ativa, ele se parte ao meio e cada
    // metade sai voando pra um lado da linha/coluna soltando um rastro de chama, no lugar de simplesmente sumir.
    public static class RocketSplitEffect
    {
        static readonly HashSet<GameObject> activeFlights = new HashSet<GameObject>();
        public static bool HasActiveFlights
        {
            get
            {
                activeFlights.RemoveWhere(root => root == null);
                return activeFlights.Count > 0;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetFlights() => activeFlights.Clear();
        const float ExitOvershoot = 0.8f;
        const float TrailTail = 0.5f; // how long after the flight the board still waits for the effect (HasActiveFlights)
        // Multiplies the flame particles' size. Their Flame object is scaled by RestScale, which shrinks them a lot.
        const float TrailSize = 2f;
        // World units/second the rocket halves travel. Faster than the helix on purpose - the rocket should feel
        // like it's darting across the row/column, not cruising.
        const float FlightSpeed = 9f;
        const float MinFlightTime = 0.25f;

        // Every board item is scaled 0.42 at the root (Square.cs) times ~0.88 on its own resting sprite (set per
        // item this session) - these halves aren't spawned through that pipeline, so they must apply it themselves
        // or they render at raw sprite size, much bigger than every other piece.
        const float RestScale = 0.42f * 0.88f;
        // The split halves (and their flame, scaled the same) render slightly bigger than the resting piece, so the
        // activation reads more clearly.
        const float ActivatedScale = RestScale * 1.05f;

        static RocketSplitEffectData _data;
        static bool buildTail = true;

        static RocketSplitEffectData Data
        {
            get
            {
                if (_data == null) _data = Resources.Load<RocketSplitEffectData>("Effects/RocketSplitEffectData");
                return _data;
            }
        }

        // towardA/towardB: world positions of the row/column's two far ends, so each half flies the right way and
        // exits exactly past the board's edge regardless of how big the level is.
        // cells/onCellPassed: each cell is reported (by index) the moment a half reaches it, so the pieces can be
        // destroyed as the rocket passes over them. Returns how long the whole effect takes, or 0 when there's no
        // effect to show (caller then has to handle the cells itself).
        public static float Play(GameObject rocketObject, bool horizontal, Vector3 towardA, Vector3 towardB,
            IList<Vector3> cells, Action<int> onCellPassed, bool stopAtA = false, bool stopAtB = false, Vector3? boardOrigin = null, bool tail = true)
        {
            // tail = false: só as duas metades voando; quem desenha a cauda de fogo é o efeito de linha (PowerUpVfx.PlayLine).
            buildTail = tail;
            var data = Data;
            if (data == null || rocketObject == null) return 0f;

            var sourceItem = rocketObject.GetComponent<SweetSugar.Scripts.Items.Item>();
            var origin = boardOrigin ?? (sourceItem != null && sourceItem.square != null
                ? sourceItem.square.transform.position : rocketObject.transform.position);
            // All entry points, including visual-only callers, are constrained to the selected grid axis.
            // An item can still be between cells or have an animated child offset at activation.
            if (cells != null && cells.Count > 0)
            {
                if (horizontal) origin.y = cells[0].y;
                else origin.x = cells[0].x;
            }
            if (horizontal) towardA.y = towardB.y = origin.y;
            else towardA.x = towardB.x = origin.x;
            // Board motion is two-dimensional. The item's render depth must not create
            // a fake second path that reports every cell as passed at the same instant.
            towardA.z = towardB.z = origin.z;
            var spriteToHide = sourceItem != null ? sourceItem.GetSpriteRenderer() : null;
            var bodyHeight = spriteToHide != null && spriteToHide.sprite != null
                ? (horizontal ? spriteToHide.bounds.size.y : spriteToHide.bounds.size.x)
                : 155f / HalfPixelsPerUnit * RestScale;
            if (spriteToHide != null) spriteToHide.enabled = false;

            var root = new GameObject("RocketSplitFX");
            activeFlights.Add(root);
            root.transform.position = origin;

            var overshootA = stopAtA ? 0f : ExitOvershoot;
            var overshootB = stopAtB ? 0f : ExitOvershoot;
            var durationA = FlightTime(origin, towardA, overshootA);
            var durationB = FlightTime(origin, towardB, overshootB);
            var halfA = BuildHalf(root.transform, data, data.HalfLeft, data.EngineNozzle, data.FlameMaterial, origin, towardA, horizontal, PowerUpEffectOrder.Value, durationA, overshootA, false, bodyHeight);
            var halfB = BuildHalf(root.transform, data, data.HalfRight, data.EngineNozzle, data.FlameMaterial, origin, towardB, horizontal, PowerUpEffectOrder.Value, durationB, overshootB, true, bodyHeight);
            root.AddComponent<RocketAxisPassObserver>().Initialize(origin, horizontal, halfA, halfB, cells, onCellPassed);

            var total = Mathf.Max(durationA, durationB);
            // A fumaça fica para trás mais tempo do que o tabuleiro deve esperar: o objeto vive até ela sumir,
            // mas deixa de contar como voo ativo no mesmo instante de antes.
            LeanTween.delayedCall(root, total + TrailTail, () => activeFlights.Remove(root));
            UnityEngine.Object.Destroy(root, total + Mathf.Max(TrailTail, data.smokeLifetime * 1.15f + 0.1f));
            return total;
        }

        // Length of a half's path from origin to its exit point (past the board's edge).
        static float PathLength(Vector3 origin, Vector3 to, float overshoot)
        {
            Vector2 path = to - origin;
            if (path.magnitude < 0.01f) return 0f;
            return (path + path.normalized * overshoot).magnitude;
        }

        static float FlightTime(Vector3 origin, Vector3 to, float overshoot)
        {
            return Mathf.Max(MinFlightTime, PathLength(origin, to, overshoot) / FlightSpeed);
        }

        const float HalfPixelsPerUnit = 128f; // matches capsule-half-left/right.png's import setting

        static Transform BuildHalf(Transform parent, RocketSplitEffectData data, Texture2D texture, Texture2D nozzleTexture, Material flameMat, Vector3 from, Vector3 to,
            bool horizontal, int sortOrder, float duration, float overshoot, bool rightHalf, float bodyHeight)
        {
            if (texture == null) return null;
            // Defensive invariant: never allow a diagonal tween, even with malformed/off-grid endpoints.
            if (horizontal) to.y = from.y; else to.x = from.x;
            to.z = from.z;
            // A rocket launched at an edge still sends its outward half off the board.
            if (Vector2.Distance(from,to)<.01f)
                to=from+(horizontal?Vector3.right:Vector3.up)*(rightHalf ? .001f : -.001f);
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), HalfPixelsPerUnit);

            var half = new GameObject("RocketHalf");
            half.transform.SetParent(parent, true);
            // Match the visible rocket being replaced instead of fitting inside a width budget.
            half.transform.localScale = Vector3.one * (bodyHeight / sprite.bounds.size.y);
            if (!horizontal) half.transform.rotation = Quaternion.Euler(0, 0, 90);

            var sr = half.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            sr.sortingOrder = sortOrder + 1;

            // Correio Mágico: cada metade nasce encostada na outra, igual ao foguete original, e só então voa pro
            // seu lado - antes as duas nasciam centradas exatamente na mesma posição, uma em cima da outra, e a
            // silhueta do foguete inteiro nunca aparecia.
            var direction = (to - from).normalized;
            var halfExtent = sprite.bounds.extents.x * half.transform.localScale.x;
            var rearSign = rightHalf ? -1f : 1f;
            var exhaust = new Vector3(rearSign * sprite.bounds.extents.x, 0f, 0f);
            var nozzleLength = 0f;
            if (nozzleTexture != null)
            {
                // Keep the original hull artwork intact. Only the separate nozzle is generated.
                // Diameter matches the 0.34-unit flame wake at the normal board scale.
                var nozzleHeight = sprite.bounds.size.y * 0.78f;
                var nozzleSprite = Sprite.Create(nozzleTexture,
                    new Rect(0, 0, nozzleTexture.width, nozzleTexture.height),
                    new Vector2(0.5f, 0.5f), nozzleTexture.height / nozzleHeight);
                // Recess most of the motor into the hull; only the short exhaust lip protrudes.
                nozzleLength = nozzleSprite.bounds.size.x * 0.30f;
                var nozzle = new GameObject("EngineNozzle");
                nozzle.transform.SetParent(half.transform, false);
                nozzle.transform.localPosition = exhaust + Vector3.right * rearSign *
                    (nozzleLength - nozzleSprite.bounds.extents.x);
                var nozzleRenderer = nozzle.AddComponent<SpriteRenderer>();
                nozzleRenderer.sprite = nozzleSprite;
                nozzleRenderer.flipX = rightHalf;
                nozzleRenderer.sortingLayerName = sr.sortingLayerName;
                nozzleRenderer.sortingOrder = sortOrder;
                exhaust.x += rearSign * nozzleLength;
            }
            half.transform.position = from + direction * (halfExtent + nozzleLength * half.transform.localScale.x);
            // Cauda de foguete saindo do anel dourado (a borda de trás da metade): rastro contínuo em três camadas,
            // labaredas, faíscas e fumaça amarelada ficando para trás.
            var toOvershot = to + direction * overshoot;
            if (!buildTail)
            {
                LeanTween.move(half, toOvershot, duration).setEase(LeanTweenType.easeInQuad).setOnComplete(() => Arrive(half));
                return half.transform;
            }
            var wide = data.trailWidth;
            AddBeam(half, flameMat, sortOrder - 3, 0.56f * wide, data.fireEnd, exhaust, data.trailTime);
            AddBeam(half, flameMat, sortOrder - 2, 0.38f * wide, data.fireMid, exhaust, data.trailTime * 0.85f);
            AddBeam(half, flameMat, sortOrder - 1, 0.15f * wide, Color.Lerp(data.fireHot, Color.white, 0.45f), exhaust, data.trailTime * 0.6f);

            var smoke = AddEmitter(half, "Smoke", exhaust);
            ConfigureSmoke(smoke, data, sortOrder - 5, duration);
            var ps = AddEmitter(half, "Flame", exhaust);
            ConfigureFlame(ps, data, flameMat, sortOrder - 4, duration);
            var sparks = AddEmitter(half, "Sparks", exhaust);
            ConfigureSparks(sparks, data, flameMat, sortOrder, duration);

            LeanTween.move(half, toOvershot, duration).setEase(LeanTweenType.easeInQuad)
                .setOnComplete(() => Arrive(half));
            return half.transform;
        }

        // Particle size/speed use the emitter object's own local scale (Scaling Mode: Local), which doesn't
        // inherit from its parent automatically - match the half's scale so the trail isn't oversized next to it.
        static ParticleSystem AddEmitter(GameObject half, string name, Vector3 exhaust)
        {
            var node = new GameObject(name);
            node.transform.SetParent(half.transform, false);
            node.transform.localPosition = exhaust;
            node.transform.localScale = Vector3.one * ActivatedScale;
            var ps = node.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        // Reaches the edge: just stops emitting and disappears, instead of sitting there finished-but-visible.
        // The trail keeps fading on its own (it lives in world space already).
        static void Arrive(GameObject half)
        {
            if (half == null) return;
            foreach (var ps in half.GetComponentsInChildren<ParticleSystem>())
            {
                var emission = ps.emission;
                emission.enabled = false;
            }
            foreach (var renderer in half.GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = false;
        }

        static Gradient Fade(Color a, Color b, Color c, float alphaStart, float alphaMid)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 0.45f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(alphaStart, 0f), new GradientAlphaKey(alphaMid, 0.55f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        static void Finish(ParticleSystem ps, Material mat, int order)
        {
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = mat;
            renderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            renderer.sortingOrder = order;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.minParticleSize = 0;
            ps.Play();
        }

        // Labaredas: nascem amarelas e grandes, viram laranja e terminam vermelhas, encolhendo (língua de fogo cartoon).
        static void ConfigureFlame(ParticleSystem ps, RocketSplitEffectData data, Material mat, int order, float duration)
        {
            var main = ps.main;
            main.loop = false;
            main.duration = duration + TrailTail;
            main.startLifetime = new ParticleSystem.MinMaxCurve(data.fireLifetime * 0.65f, data.fireLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f * TrailSize * data.fireSize, 0.55f * TrailSize * data.fireSize);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(data.fireHot, Color.Lerp(data.fireHot, data.fireMid, 0.6f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = data.fireRate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = data.fireSpread;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.75f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f)));

            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = Fade(data.fireHot, data.fireMid, data.fireEnd, 1f, 0.9f);

            Finish(ps, mat, order);
        }

        // Fumaça amarelada: nuvens opacas que crescem, giram devagar e sobem um pouco enquanto somem.
        static void ConfigureSmoke(ParticleSystem ps, RocketSplitEffectData data, int order, float duration)
        {
            var main = ps.main;
            main.loop = false;
            main.duration = duration + TrailTail;
            main.startLifetime = new ParticleSystem.MinMaxCurve(data.smokeLifetime * 0.7f, data.smokeLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.42f * TrailSize * data.smokeSize, 0.75f * TrailSize * data.smokeSize);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = Color.white;
            main.gravityModifier = -0.06f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = data.smokeRate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = data.fireSpread * 1.1f;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(0.35f, 1f), new Keyframe(1f, 1.5f)));

            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-0.9f, 0.9f);

            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = Fade(data.smokeStart, Color.Lerp(data.smokeStart, data.smokeEnd, 0.6f), data.smokeEnd, data.smokeAlpha, data.smokeAlpha * 0.7f);

            Finish(ps, SmokeMaterial, order);
        }

        // Faíscas pequenas e rápidas saltando da boca do fogo.
        static void ConfigureSparks(ParticleSystem ps, RocketSplitEffectData data, Material mat, int order, float duration)
        {
            var main = ps.main;
            main.loop = false;
            main.duration = duration + TrailTail;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.9f, 2.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f * TrailSize, 0.15f * TrailSize);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.Lerp(data.fireHot, Color.white, 0.5f), data.fireHot);
            main.gravityModifier = 0.35f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = data.sparkRate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = data.fireSpread * 0.6f;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = Fade(Color.Lerp(data.fireHot, Color.white, 0.5f), data.fireHot, data.fireMid, 1f, 1f);

            Finish(ps, mat, order);
        }

        static Material smokeMaterial;

        // Material da fumaça: nuvem macia desenhada por código, com transparência normal (não soma luz como o fogo).
        static Material SmokeMaterial
        {
            get
            {
                if (smokeMaterial != null) return smokeMaterial;
                const int n = 96;
                var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
                var tufts = new[] { new Vector3(0f, 0.02f, 0.44f), new Vector3(-0.34f, 0.1f, 0.3f), new Vector3(0.33f, -0.06f, 0.31f), new Vector3(0.08f, 0.36f, 0.27f),
                    new Vector3(-0.1f, -0.36f, 0.28f), new Vector3(0.36f, 0.34f, 0.2f), new Vector3(-0.38f, -0.3f, 0.2f) };
                var pixels = new Color32[n * n];
                for (var y = 0; y < n; y++)
                    for (var x = 0; x < n; x++)
                    {
                        var u = (x + 0.5f) / n * 2f - 1f; var v = (y + 0.5f) / n * 2f - 1f;
                        var d2 = u * u + v * v;
                        var alpha = 0f;
                        if (d2 < 1f)
                        {
                            foreach (var tuft in tufts) { var dx = u - tuft.x; var dy = v - tuft.y; alpha += 0.7f * Mathf.Exp(-(dx * dx + dy * dy) / (tuft.z * tuft.z)); }
                            alpha = Mathf.Clamp01(alpha) * (1f - d2) * (1f - d2);
                        }
                        pixels[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                    }
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                smokeMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave, mainTexture = texture };
                return smokeMaterial;
            }
        }

        static void AddBeam(GameObject half, Material source, int order, float width, Color color, Vector3 exhaust, float time)
        {
            var node = new GameObject("RocketBrightWake");
            node.transform.SetParent(half.transform, false);
            node.transform.localPosition = exhaust;
            var trail = node.AddComponent<TrailRenderer>();
            var material = new Material(source);
            material.SetTexture("_MainTex", Texture2D.whiteTexture);
            node.AddComponent<RocketBeamMaterial>().ownedMaterial = material;
            trail.sharedMaterial = material;
            trail.time = time;
            trail.minVertexDistance = 0.015f;
            trail.widthMultiplier = width;
            // largo junto da metade e afinando para trás, como a cauda de um foguete
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.35f, 0.8f), new Keyframe(1f, 0.08f));
            trail.numCapVertices = 3;
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            trail.sortingOrder = order;
        }
    }

    public sealed class RocketBeamMaterial : MonoBehaviour
    {
        public Material ownedMaterial;
        void OnDestroy() { if (ownedMaterial != null) Destroy(ownedMaterial); }
    }
}
