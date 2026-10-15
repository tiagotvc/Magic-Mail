using System;
using System.Collections;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.System;
using UnityEngine;

namespace RoyalAves.Effects
{
    // Texture samples from the grass are clipped into rounded leaves and animated in one mesh.
    public sealed class GrassDefoliateEffect : MonoBehaviour
    {
        public const float LayerDuration = 0.46f;
        public const float FinalDuration = 0.62f;
        static readonly Vector2[] Outline =
        {
            new Vector2(0.50f, 0f), new Vector2(0.16f, 0.15f), new Vector2(0.04f, 0.50f),
            new Vector2(0.16f, 0.85f), new Vector2(0.50f, 1f), new Vector2(0.84f, 0.85f),
            new Vector2(0.96f, 0.50f), new Vector2(0.84f, 0.15f)
        };
        const int VerticesPerLeaf = 9;
        Leaf[] leaves;
        Mesh mesh;
        Material material;
        Vector3[] vertices;
        Color32[] colors;
        SpriteRenderer grass;
        Mesh fadingMesh;
        Material fadingMaterial;
        LevelManager manager;
        AnimateItems barrier;
        Action onComplete;
        Color sourceColor;
        float duration;
        const float LeafDuration = 0.8f;
        float trajectoryDuration;

        sealed class Leaf
        {
            public Vector3 origin;
            public Vector3[] shape;
            public Vector3 direction;
            public float lift, spin, delay, phase;
        }

        public static void Play(SpriteRenderer source, LevelManager owner, bool finalLayer, Action onComplete = null)
        {
            var root = new GameObject("GrassDefoliateFX");
            root.transform.SetParent(source.transform.parent, false);
            root.transform.localPosition = source.transform.localPosition;
            root.transform.localRotation = source.transform.localRotation;
            root.transform.localScale = source.transform.localScale;
            root.AddComponent<GrassDefoliateEffect>().Initialize(source, owner, finalLayer, onComplete);
        }

        void Initialize(SpriteRenderer source, LevelManager owner, bool finalLayer, Action callback)
        {
            manager = owner;
            onComplete = callback;
            duration = finalLayer ? FinalDuration : LayerDuration;
            trajectoryDuration = duration + 0.55f;
            sourceColor = source.color;
            // Keep the original completion timing; decorative leaves continue falling afterward.
            barrier = gameObject.AddComponent<AnimateItems>();
            barrier.target = true;
            barrier.linkObjectHash = source.transform.parent.gameObject.GetHashCode();
            manager.animateItems.Add(barrier);

            var fadingGrass = new GameObject("FadingGrass");
            fadingGrass.transform.SetParent(transform, false);
            grass = fadingGrass.AddComponent<SpriteRenderer>();
            grass.sprite = source.sprite;
            grass.sharedMaterial = source.sharedMaterial;
            grass.color = sourceColor;
            grass.sortingLayerID = source.sortingLayerID;
            grass.sortingOrder = source.sortingOrder;
            grass.flipX = source.flipX;
            grass.flipY = source.flipY;
            var organicBorder = source.GetComponent<GrassOrganicBorder>();
            if (organicBorder != null && organicBorder.CopyForFade(fadingGrass, out fadingMesh, out fadingMaterial) != null)
                grass.enabled = false;

            BuildLeaves(source, finalLayer);
            StartCoroutine(Animate());
        }

        void BuildLeaves(SpriteRenderer source, bool finalLayer)
        {
            var random = new System.Random(barrier.linkObjectHash ^ (finalLayer ? 371 : 197));
            int count = finalLayer ? 14 : 8;
            leaves = new Leaf[count];
            vertices = new Vector3[count * VerticesPerLeaf];
            colors = new Color32[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[count * Outline.Length * 3];
            var sprite = source.sprite;
            var textureRect = sprite.textureRect;
            for (int i = 0; i < count; i++)
            {
                float width = Range(random, 0.10f, 0.14f);
                float height = Range(random, 0.12f, 0.17f);
                // Sample rounded foliage inside the silhouette instead of its transparent margin.
                float x = 0.08f + (i % 4) * 0.20f + Range(random, 0f, 0.04f);
                float y = 0.08f + (i / 4) * 0.18f + Range(random, 0f, 0.06f);
                var region = new Rect(x, y, width, height);
                var center = MapPosition(sprite, region.center);
                if (source.flipX) center.x = -center.x;
                if (source.flipY) center.y = -center.y;
                float angle = (i + Range(random, -0.3f, 0.3f)) * Mathf.PI * 2f / count;
                var leaf = new Leaf
                {
                    origin = center, shape = new Vector3[VerticesPerLeaf],
                    direction = new Vector3(Mathf.Cos(angle) * Range(random, 0.45f, 0.85f), Mathf.Sin(angle) * 0.40f),
                    lift = Range(random, 0.20f, 0.40f), spin = Range(random, 65f, 155f) * (i % 2 == 0 ? 1f : -1f),
                    delay = Range(random, 0.015f, 0.075f), phase = Range(random, 0f, Mathf.PI * 2f)
                };
                leaves[i] = leaf;
                int start = i * VerticesPerLeaf;
                for (int v = 0; v < VerticesPerLeaf; v++)
                {
                    var normalized = v == 0 ? new Vector2(0.47f, 0.48f) : Outline[v - 1];
                    var point = region.min + Vector2.Scale(normalized, region.size);
                    var position = MapPosition(sprite, point);
                    if (source.flipX) position.x = -position.x;
                    if (source.flipY) position.y = -position.y;
                    leaf.shape[v] = (position - center) * 1.45f;
                    vertices[start + v] = center + leaf.shape[v];
                    uv[start + v] = new Vector2((textureRect.x + point.x * textureRect.width) / sprite.texture.width,
                        (textureRect.y + point.y * textureRect.height) / sprite.texture.height);
                    colors[start + v] = new Color(sourceColor.r, sourceColor.g, sourceColor.b, 0f);
                }
                for (int edge = 0; edge < Outline.Length; edge++)
                {
                    int index = (i * Outline.Length + edge) * 3;
                    triangles[index] = start;
                    triangles[index + 1] = start + edge + 1;
                    triangles[index + 2] = start + (edge + 1) % Outline.Length + 1;
                }
            }
            mesh = new Mesh { name = "GrassFlyingBlades", vertices = vertices, uv = uv, triangles = triangles, colors32 = colors };
            mesh.MarkDynamic();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            material = new Material(source.sharedMaterial);
            material.SetTexture("_MainTex", sprite.texture);
            var flyingLeaves = new GameObject("FlyingLeaves");
            flyingLeaves.transform.SetParent(transform, false);
            flyingLeaves.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = flyingLeaves.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            renderer.sortingOrder = PowerUpEffectOrder.Value + 2;
        }

        IEnumerator Animate()
        {
            float elapsed = 0f;
            bool completed = false;
            while (elapsed < LeafDuration)
            {
                elapsed += Time.deltaTime * GrassFeedbackSettings.Speed;
                if (!completed && elapsed >= duration)
                {
                    completed = true;
                    try { onComplete?.Invoke(); }
                    finally { onComplete = null; ReleaseBarrier(); }
                }
                float fade = Mathf.Clamp01(elapsed / 0.20f);
                grass.color = new Color(sourceColor.r, sourceColor.g, sourceColor.b,
                    sourceColor.a * (1f - Mathf.SmoothStep(0f, 1f, fade)));
                if (fadingMaterial != null) fadingMaterial.SetColor("_Color", grass.color);
                grass.transform.localScale = Vector3.one * (1f - fade * 0.08f);
                for (int i = 0; i < leaves.Length; i++)
                {
                    var leaf = leaves[i];
                    // Compress the original trajectory into 0.8 s without shortening its travel.
                    float age = elapsed * trajectoryDuration / LeafDuration - leaf.delay;
                    float t = Mathf.Clamp01(age / (trajectoryDuration - leaf.delay));
                    float fallTime = Mathf.Max(0f, age);
                    float travel = 1f - Mathf.Exp(-3f * fallTime);
                    float flutter = Mathf.Sin(t * Mathf.PI * 3f + leaf.phase) * 0.045f * Mathf.Sin(t * Mathf.PI);
                    var offset = (leaf.direction * travel + Vector3.up * (leaf.lift * 2f * fallTime - 5.5f * fallTime * fallTime)
                        + Vector3.right * flutter) * GrassFeedbackSettings.Dispersion;
                    float rotation = leaf.spin * t * Mathf.Deg2Rad;
                    float sin = Mathf.Sin(rotation), cos = Mathf.Cos(rotation);
                    float scale = 1f - t * 0.15f;
                    float turn = 0.20f + 0.80f * Mathf.Abs(Mathf.Cos(t * Mathf.PI * 2f));
                    float alpha = age < 0f ? 0f : Mathf.Clamp01(age / 0.035f) *
                        (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.72f) / 0.28f)));
                    int start = i * VerticesPerLeaf;
                    for (int v = 0; v < VerticesPerLeaf; v++)
                    {
                        var point = leaf.shape[v];
                        float x = point.x * turn * scale, y = point.y * scale;
                        vertices[start + v] = leaf.origin + offset + new Vector3(x * cos - y * sin, x * sin + y * cos);
                        colors[start + v].a = (byte)(255f * sourceColor.a * alpha);
                    }
                }
                mesh.vertices = vertices;
                mesh.colors32 = colors;
                mesh.RecalculateBounds();
                yield return null;
            }
            Destroy(gameObject);
        }

        static float Range(System.Random random, float min, float max) => min + (max - min) * (float)random.NextDouble();
        static Vector3 MapPosition(Sprite sprite, Vector2 point) => new Vector3(
            (point.x * sprite.rect.width - sprite.pivot.x) / sprite.pixelsPerUnit,
            (point.y * sprite.rect.height - sprite.pivot.y) / sprite.pixelsPerUnit);

        void ReleaseBarrier()
        {
            if (manager != null && barrier != null) manager.animateItems.Remove(barrier);
            barrier = null;
        }

        void OnDestroy()
        {
            // Leaving/restarting the field cancels its callback instead of collecting old grass.
            onComplete = null;
            ReleaseBarrier();
            if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
            if (fadingMesh != null) Destroy(fadingMesh);
            if (fadingMaterial != null) Destroy(fadingMaterial);
        }
    }
}
