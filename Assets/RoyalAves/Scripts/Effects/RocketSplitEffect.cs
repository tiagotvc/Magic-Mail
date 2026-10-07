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
        const float ExitOvershoot = 0.8f;
        const float TrailTail = 0.5f; // keeps the effect object alive long enough for the last particle to fade
        // Multiplies the flame particles' size. Their Flame object is scaled by RestScale, which shrinks them a lot.
        const float TrailSize = 2.5f;
        // Same speed (world units/second) and minimum time as the helix (MarmaladeFly), so both specials move alike.
        const float FlightSpeed = 5f;
        const float MinFlightTime = 0.5f;

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
            IList<Vector3> cells, Action<int> onCellPassed)
        {
            var data = Data;
            if (data == null || rocketObject == null) return 0f;

            var origin = rocketObject.transform.position;
            var spriteToHide = rocketObject.GetComponentInChildren<SpriteRenderer>();
            if (spriteToHide != null) spriteToHide.enabled = false;

            var root = new GameObject("RocketSplitFX");
            root.transform.position = origin;

            var durationA = FlightTime(origin, towardA);
            var durationB = FlightTime(origin, towardB);
            BuildHalf(root.transform, data.HalfLeft, data.FlameMaterial, origin, towardA, horizontal, PowerUpEffectOrder.Value, durationA);
            BuildHalf(root.transform, data.HalfRight, data.FlameMaterial, origin, towardB, horizontal, PowerUpEffectOrder.Value, durationB);
            ScheduleCells(origin, towardA, towardB, durationA, durationB, cells, onCellPassed);

            var total = Mathf.Max(durationA, durationB);
            UnityEngine.Object.Destroy(root, total + TrailTail);
            return total;
        }

        // Length of a half's path from origin to its exit point (past the board's edge).
        static float PathLength(Vector3 origin, Vector3 to)
        {
            var path = to - origin;
            if (path.magnitude < 0.01f) return 0f;
            return (path + path.normalized * ExitOvershoot).magnitude;
        }

        static float FlightTime(Vector3 origin, Vector3 to)
        {
            return Mathf.Max(MinFlightTime, PathLength(origin, to) / FlightSpeed);
        }

        // Mirrors BuildHalf's motion: easeInQuad over the half's duration along its path, so a point at distance d
        // along it is reached at duration * sqrt(d / pathLength).
        static void ScheduleCells(Vector3 origin, Vector3 towardA, Vector3 towardB, float durationA, float durationB,
            IList<Vector3> cells, Action<int> onCellPassed)
        {
            var ends = new[] { towardA, towardB };
            var durations = new[] { durationA, durationB };
            for (var i = 0; i < cells.Count; i++)
            {
                var passAt = float.MaxValue;
                for (var h = 0; h < ends.Length; h++)
                {
                    var length = PathLength(origin, ends[h]);
                    if (length <= 0f) continue;
                    var direction = (ends[h] - origin).normalized;
                    var along = Vector3.Dot(cells[i] - origin, direction);
                    if (along < -0.01f) continue; // the other half's side
                    passAt = Mathf.Min(passAt, durations[h] * Mathf.Sqrt(Mathf.Max(along, 0f) / length));
                }
                if (passAt == float.MaxValue) passAt = 0f;

                var index = i;
                if (passAt <= 0f) onCellPassed(index);
                else LeanTween.delayedCall(passAt, () => onCellPassed(index));
            }
        }

        static void BuildHalf(Transform parent, Sprite sprite, Material flameMat, Vector3 from, Vector3 to,
            bool horizontal, int sortOrder, float duration)
        {
            if (sprite == null) return;

            var half = new GameObject("RocketHalf");
            half.transform.SetParent(parent, true);
            half.transform.localScale = Vector3.one * ActivatedScale;
            if (!horizontal) half.transform.rotation = Quaternion.Euler(0, 0, 90);

            var sr = half.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            sr.sortingOrder = sortOrder + 1;

            // Correio Mágico: cada metade nasce encostada na outra, igual ao foguete original, e só então voa pro
            // seu lado - antes as duas nasciam centradas exatamente na mesma posição, uma em cima da outra, e a
            // silhueta do foguete inteiro nunca aparecia.
            var direction = (to - from).normalized;
            var halfExtent = sprite.bounds.extents.x * ActivatedScale;
            half.transform.position = from + direction * halfExtent;

            var flame = new GameObject("Flame");
            flame.transform.SetParent(half.transform, false);
            // Particle size/speed use the Flame object's own local scale (Scaling Mode: Local), which doesn't
            // inherit from its parent automatically - match the half's scale so the trail isn't oversized next to it.
            flame.transform.localScale = Vector3.one * ActivatedScale;

            var ps = flame.AddComponent<ParticleSystem>();
            ConfigureFlame(ps, flameMat, sortOrder, duration);

            var toOvershot = to + direction * ExitOvershoot;
            LeanTween.move(half, toOvershot, duration).setEase(LeanTweenType.easeInQuad)
                .setOnComplete(() => Arrive(half, sr, ps));
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
            if (sr != null) sr.enabled = false;
        }

        static void ConfigureFlame(ParticleSystem ps, Material mat, int baseSortOrder, float duration)
        {
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
            shape.radius = 0.16f;

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
    }
}
