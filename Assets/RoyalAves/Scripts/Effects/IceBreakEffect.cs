using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Effects
{
    // Independent decorative effect: the board can reveal the next layer immediately.
    public sealed class IceBreakEffect : MonoBehaviour
    {
        sealed class Shard
        {
            public Vector3 center, velocity;
            public Vector3[] shape;
            public Vector2[] uv;
            public float spin;
        }

        readonly List<Shard> shards = new List<Shard>();
        Mesh mesh;
        Material material;
        SpriteRenderer flash;
        Vector3[] vertices;
        Color[] colors;
        Vector2[] uv;
        float elapsed;
        bool thick, animate;
        public float Duration => thick ? .64f : .48f;
        public int ShardCount => shards.Count;

        public static IceBreakEffect Play(SpriteRenderer source, bool thick, bool animate = true)
        {
            if (source == null || source.sprite == null) return null;
            var root = new GameObject(thick ? "ThickIceBreakFX" : "ThinIceBreakFX");
            root.transform.SetParent(source.transform.parent, false);
            root.transform.localPosition = source.transform.localPosition;
            root.transform.localRotation = source.transform.localRotation;
            root.transform.localScale = source.transform.localScale;
            var effect = root.AddComponent<IceBreakEffect>();
            effect.Initialize(source, thick, animate);
            return effect;
        }

        void Initialize(SpriteRenderer source, bool isThick, bool shouldAnimate)
        {
            thick = isThick;
            animate = shouldAnimate;
            material = new Material(source.sharedMaterial);
            material.SetTexture("_MainTex", source.sprite.texture);
            mesh = new Mesh { name = thick ? "ChunkyIceShards" : "FineIceShards" };
            mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            renderer.sortingOrder = PowerUpEffectOrder.Value + 2;
            var flashObject = new GameObject("IceImpactGlint");
            flashObject.transform.SetParent(transform, false);
            flash = flashObject.AddComponent<SpriteRenderer>();
            flash.sprite = source.sprite;
            flash.sharedMaterial = source.sharedMaterial;
            flash.flipX = source.flipX;
            flash.flipY = source.flipY;
            flash.sortingLayerName = renderer.sortingLayerName;
            flash.sortingOrder = renderer.sortingOrder + 1;
            Build(source);
            Sample(0f);
        }

        // Scattered ice chips with softened, uneven outlines; no rectangular plate tessellation.
        void Build(SpriteRenderer source)
        {
            var random = new System.Random(thick ? 6841 : 9173);
            int count = thick ? 5 : 7;
            var sprite = source.sprite;
            var rect = sprite.rect;
            var textureRect = sprite.textureRect;
            int totalVertices = 0;
            float phase = (float)random.NextDouble() * Mathf.PI * 2f;
            for (int shardIndex = 0; shardIndex < count; shardIndex++)
            {
                float direction = phase + shardIndex * 2.399963f;
                float distance = shardIndex == 0 ? .12f : .26f + (float)random.NextDouble() * .06f;
                var center = Vector2.one * .5f + new Vector2(Mathf.Cos(direction), Mathf.Sin(direction)) * distance;
                int sides = random.Next(5, 8);
                float radius = thick ? .11f + (float)random.NextDouble() * .035f : .075f + (float)random.NextDouble() * .03f;
                float aspect = .72f + (float)random.NextDouble() * .5f;
                var corners = new List<Vector2>();
                float rotation = (float)random.NextDouble() * Mathf.PI * 2f;
                for (int i = 0; i < sides; i++)
                {
                    float angle = rotation - i * Mathf.PI * 2f / sides;
                    float unevenness = .7f + (float)random.NextDouble() * .3f;
                    corners.Add(center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * aspect) * unevenness);
                }
                // Bevel each corner while retaining the uneven silhouette and different chip sizes.
                var polygon = new List<Vector2>();
                for (int i = 0; i < corners.Count; i++)
                {
                    var next = corners[(i + 1) % corners.Count];
                    polygon.Add(Vector2.Lerp(corners[i], next, .18f));
                    polygon.Add(Vector2.Lerp(corners[i], next, .82f));
                }
                Vector3 Local(Vector2 point) => new Vector3(
                    ((source.flipX ? 1f - point.x : point.x) * rect.width - sprite.pivot.x) / sprite.pixelsPerUnit,
                    ((source.flipY ? 1f - point.y : point.y) * rect.height - sprite.pivot.y) / sprite.pixelsPerUnit);
                var localCenter = Local(center);
                var shape = new Vector3[polygon.Count + 1];
                var shardUv = new Vector2[shape.Length];
                for (int i = 0; i < shape.Length; i++)
                {
                    var point = i == 0 ? center : polygon[i - 1];
                    shape[i] = Local(point) - localCenter;
                    shardUv[i] = new Vector2((textureRect.x + point.x * textureRect.width) / sprite.texture.width,
                        (textureRect.y + point.y * textureRect.height) / sprite.texture.height);
                }
                float width = rect.width / sprite.pixelsPerUnit;
                var outward = (center - Vector2.one * .5f).normalized;
                shards.Add(new Shard { center = localCenter, shape = shape, uv = shardUv,
                    velocity = new Vector3(outward.x * width * (thick ? .85f : 1.05f),
                        outward.y * width * .35f + width * (thick ? .6f : .4f)),
                    spin = ((float)random.NextDouble() * 2f - 1f) * (thick ? 230f : 340f) });
                totalVertices += shape.Length;
            }
            vertices = new Vector3[totalVertices];
            colors = new Color[totalVertices];
            uv = new Vector2[totalVertices];
            var triangles = new List<int>();
            int offset = 0;
            foreach (var shard in shards)
            {
                for (int i = 1; i < shard.shape.Length; i++)
                {
                    triangles.Add(offset);
                    triangles.Add(offset + i);
                    triangles.Add(offset + (i == shard.shape.Length - 1 ? 1 : i + 1));
                }
                for (int i = 0; i < shard.shape.Length; i++) uv[offset + i] = shard.uv[i];
                offset += shard.shape.Length;
            }
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles.ToArray();
        }

        public void Sample(float time)
        {
            float travel = Mathf.Max(0f, time - .025f);
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(Duration * .4f, Duration, time));
            float scale = Mathf.Lerp(1f, .75f, Mathf.Clamp01(time / Duration));
            int offset = 0;
            foreach (var shard in shards)
            {
                var rotation = Quaternion.Euler(0f, 0f, shard.spin * travel);
                var displacement = shard.velocity * travel + Vector3.down * (4.8f * travel * travel);
                for (int i = 0; i < shard.shape.Length; i++)
                {
                    vertices[offset] = shard.center + displacement + rotation * shard.shape[i] * scale;
                    // Shards are briefly more opaque than the intact ice so their silhouettes read.
                    colors[offset] = new Color(1f, 1f, 1f, (thick ? .9f : .75f) * fade);
                    offset++;
                }
            }
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.RecalculateBounds();
            flash.color = new Color(1f, 1f, 1f, .55f * (1f - Mathf.Clamp01(time / .065f)));
            flash.enabled = time < .065f;
        }

        void Update()
        {
            if (!animate) return;
            elapsed += Time.deltaTime;
            Sample(elapsed);
            if (elapsed >= Duration) Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (Application.isPlaying) { Destroy(mesh); Destroy(material); }
            else { DestroyImmediate(mesh); DestroyImmediate(material); }
        }
    }
}
