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
        const float TrailTail = 0.5f; // keeps the effect object alive long enough for the last particle to fade
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
            IList<Vector3> cells, Action<int> onCellPassed, bool stopAtA = false, bool stopAtB = false, Vector3? boardOrigin = null)
        {
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
            var halfA = BuildHalf(root.transform, data.HalfLeft, data.EngineNozzle, data.FlameMaterial, origin, towardA, horizontal, PowerUpEffectOrder.Value, durationA, overshootA, false, bodyHeight);
            var halfB = BuildHalf(root.transform, data.HalfRight, data.EngineNozzle, data.FlameMaterial, origin, towardB, horizontal, PowerUpEffectOrder.Value, durationB, overshootB, true, bodyHeight);
            root.AddComponent<RocketAxisPassObserver>().Initialize(origin, horizontal, halfA, halfB, cells, onCellPassed);

            var total = Mathf.Max(durationA, durationB);
            UnityEngine.Object.Destroy(root, total + TrailTail);
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

        static Transform BuildHalf(Transform parent, Texture2D texture, Texture2D nozzleTexture, Material flameMat, Vector3 from, Vector3 to,
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
            // Each nozzle sits at the cut, with its flame trailing behind the flying half.
            // Keep both wakes behind the hull so the mouth of the nozzle remains visible.
            AddBeam(half, flameMat, sortOrder - 2, 0.34f, new Color(1f, 0.67f, 0.12f), exhaust);
            AddBeam(half, flameMat, sortOrder - 1, 0.11f, new Color(1f, 1f, 0.85f), exhaust);

            var flame = new GameObject("Flame");
            flame.transform.SetParent(half.transform, false);
            flame.transform.localPosition = exhaust;
            // Particle size/speed use the Flame object's own local scale (Scaling Mode: Local), which doesn't
            // inherit from its parent automatically - match the half's scale so the trail isn't oversized next to it.
            flame.transform.localScale = Vector3.one * ActivatedScale;

            var ps = flame.AddComponent<ParticleSystem>();
            ConfigureFlame(ps, flameMat, sortOrder - 3, duration);

            var toOvershot = to + direction * overshoot;
            LeanTween.move(half, toOvershot, duration).setEase(LeanTweenType.easeInQuad)
                .setOnComplete(() => Arrive(half, sr, ps));
            return half.transform;
        }

        // Reaches the edge: just stops emitting and disappears, instead of sitting there finished-but-visible.
        // The trail keeps fading on its own (it lives in world space already).
        static void Arrive(GameObject half, SpriteRenderer sr, ParticleSystem ps)
        {
            if (half == null) return;
            if (ps != null)
            {
                var emission = ps.emission;
                emission.enabled = false;
            }
            foreach (var renderer in half.GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = false;
        }

        static void ConfigureFlame(ParticleSystem ps, Material mat, int baseSortOrder, float duration)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.duration = duration + TrailTail;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.22f * TrailSize, 0.4f * TrailSize);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.4f), new Color(1f, 0.5f, 0.1f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 80;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            // Correio Mágico: raio da área de emissão - é o que dá a largura do rastro (espalhamento lateral das
            // partículas), separado do tamanho de cada partícula (TrailSize).
            shape.radius = 0.22f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f),
                    new GradientColorKey(new Color(1f, 0.35f, 0.08f), 0.6f),
                    new GradientColorKey(new Color(0.55f, 0.1f, 0.05f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
            psRenderer.material = mat;
            psRenderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            psRenderer.sortingOrder = baseSortOrder;
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            psRenderer.minParticleSize = 0;

            ps.Play();
        }

        static void AddBeam(GameObject half, Material source, int order, float width, Color color, Vector3 exhaust)
        {
            var node = new GameObject("RocketBrightWake");
            node.transform.SetParent(half.transform, false);
            node.transform.localPosition = exhaust;
            var trail = node.AddComponent<TrailRenderer>();
            var material = new Material(source);
            material.SetTexture("_MainTex", Texture2D.whiteTexture);
            node.AddComponent<RocketBeamMaterial>().ownedMaterial = material;
            trail.sharedMaterial = material;
            trail.time = 0.20f;
            trail.minVertexDistance = 0.015f;
            trail.widthMultiplier = width;
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
