using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.Blocks;

namespace RoyalAves.Effects
{
    public sealed class FallingBoardDebris : MonoBehaviour
    {
        sealed class Piece
        {
            public Transform transform;
            public Vector3 origin, velocity;
            public float spin;
            public Mesh mesh;
            public Color[] colors;
            public float originalAlpha;
            public Quaternion rotation = Quaternion.identity;
        }
        readonly List<Piece> pieces = new List<Piece>();
        readonly List<Mesh> meshes = new List<Mesh>();
        Material material;
        Material burstMaterial;
        float bottom;

        public static void Shatter(SpriteRenderer source)
        {
            Shatter(source, false);
        }

        public static void ShatterEgg(SpriteRenderer source)
        {
            Shatter(source, true);
        }

        public static void ShatterGlass(SpriteRenderer source)
        {
            Shatter(source, true, true);
        }

        static void Shatter(SpriteRenderer source, bool eggshell, bool glass = false)
        {
            if (source == null || source.sprite == null) return;
            var effect = Create(source, glass ? "FallingPotionGlass" : eggshell ? "FallingEggshellShards" : "FallingMatchShards");
            effect.material = new Material(source.sharedMaterial);
            effect.material.SetTexture("_MainTex", glass ? Texture2D.whiteTexture : source.sprite.texture);
            int shardCount = eggshell ? 14 : 8;
            if (eggshell) effect.StartCoroutine(effect.EggBurst(source));
            float scatterAngle = Random.Range(0f, Mathf.PI * 2f);
            for (int shard = 0; shard < shardCount; shard++)
            {
                // Sample a few scattered chips instead of tessellating the entire image.
                float angle = scatterAngle + shard * 2.399963f;
                float radius = Random.Range(0.12f, 0.33f);
                var center = Vector2.one * 0.5f + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                var mesh = BuildChip(source.sprite, center);
                var origin = mesh.bounds.center;
                var vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; i++) vertices[i] -= origin;
                mesh.vertices = vertices;
                var colors = new Color[mesh.vertexCount];
                var shardColor = glass ? new Color(.72f, .9f, 1f, .7f) : source.color;
                for (int i = 0; i < colors.Length; i++) colors[i] = shardColor;
                mesh.colors = colors;
                mesh.RecalculateBounds();
                effect.meshes.Add(mesh);
                var node = new GameObject("ColoredShard");
                node.transform.SetParent(effect.transform, false);
                node.transform.localPosition = origin;
                node.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = node.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = effect.material;
                renderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
                renderer.sortingOrder = PowerUpEffectOrder.Value + 2;
                var velocity = eggshell
                    ? new Vector3(Mathf.Cos(angle) * Random.Range(1.8f, 3.1f), Mathf.Sin(angle) * 2f + 2.2f)
                    : new Vector3((center.x - 0.5f) * 1.4f + Random.Range(-0.1f, 0.1f), Random.Range(0.35f, 0.75f));
                effect.pieces.Add(new Piece { transform = node.transform, origin = node.transform.position,
                    velocity = velocity, spin = eggshell ? Random.Range(-280f, 280f) : Random.Range(-65f, 65f),
                    mesh = mesh, colors = colors, originalAlpha = shardColor.a });
            }
            // Scale the entire ballistic displacement by 30%, preserving its timing.
            foreach (var piece in effect.pieces) piece.velocity *= 1.3f;
            effect.StartCoroutine(eggshell
                ? effect.Fall(13f, float.PositiveInfinity, float.PositiveInfinity)
                : effect.Fall(13f, 0.55f, 0.18f));
        }

        // A short local flash and expanding pressure ring sell the burst without
        // shaking the board or delaying gravity/goal updates.
        IEnumerator EggBurst(SpriteRenderer source)
        {
            burstMaterial = new Material(source.sharedMaterial);
            burstMaterial.SetTexture("_MainTex", Texture2D.whiteTexture);
            var flash = BurstMesh("EggFlash", false);
            var ring = BurstMesh("EggImpactRing", true);
            float size = Mathf.Max(source.sprite.bounds.size.x, source.sprite.bounds.size.y);
            float elapsed = 0f;
            while (elapsed < 0.32f)
            {
                float t = elapsed / 0.32f;
                flash.transform.localScale = Vector3.one * size * Mathf.Lerp(0.25f, 0.85f, t);
                ring.transform.localScale = Vector3.one * size * Mathf.Lerp(0.35f, 1.5f, 1f - Mathf.Pow(1f - t, 3f));
                TintBurst(flash.GetComponent<MeshFilter>().sharedMesh,
                    new Color(1f, 0.94f, 0.75f, Mathf.Max(0f, 1f - t * 2.4f) * 0.85f));
                TintBurst(ring.GetComponent<MeshFilter>().sharedMesh,
                    new Color(0.9f, 0.78f, 1f, (1f - t) * 0.8f));
                elapsed += Time.deltaTime;
                yield return null;
            }
            Destroy(flash);
            Destroy(ring);
        }

        GameObject BurstMesh(string name, bool ring)
        {
            const int segments = 48;
            var vertices = new Vector3[segments * 2];
            var triangles = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle));
                vertices[i * 2] = direction * (ring ? 0.44f : 0f);
                vertices[i * 2 + 1] = direction * 0.5f;
                int next = ((i + 1) % segments) * 2;
                int index = i * 6;
                triangles[index] = i * 2; triangles[index + 1] = next; triangles[index + 2] = i * 2 + 1;
                triangles[index + 3] = i * 2 + 1; triangles[index + 4] = next; triangles[index + 5] = next + 1;
            }
            var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles,
                uv = new Vector2[vertices.Length] };
            TintBurst(mesh, Color.clear);
            mesh.RecalculateBounds();
            meshes.Add(mesh);
            var node = new GameObject(name);
            node.transform.SetParent(transform, false);
            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = node.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = burstMaterial;
            renderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            renderer.sortingOrder = PowerUpEffectOrder.Value + 3;
            return node;
        }

        static void TintBurst(Mesh mesh, Color color)
        {
            var colors = new Color[mesh.vertexCount];
            for (int i = 0; i < colors.Length; i++) colors[i] = color;
            mesh.colors = colors;
        }

        public static void ShatterWood(SpriteRenderer source, int state)
        {
            MaterialBurstEffect.ExplodeWood(source, state);
        }

        public static void ShatterCabinet(SpriteRenderer source)
        {
            MaterialBurstEffect.ExplodeWood(source, 1, true);
        }


        static Mesh BuildChip(Sprite sprite, Vector2 center)
        {
            int sides = Random.Range(5, 8);
            var vertices = new Vector3[sides + 1];
            var uv = new Vector2[sides + 1];
            var triangles = new int[sides * 3];
            float rotation = Random.Range(0f, Mathf.PI * 2f);
            var size = new Vector2(Random.Range(0.07f, 0.11f), Random.Range(0.06f, 0.1f));
            var rect = sprite.rect;
            var textureRect = sprite.textureRect;
            for (int i = 0; i <= sides; i++)
            {
                var point = center;
                if (i > 0)
                {
                    float angle = rotation - (i - 1) * Mathf.PI * 2f / sides;
                    float unevenness = Random.Range(0.65f, 1f);
                    var delta = new Vector2(Mathf.Cos(angle) * size.x, Mathf.Sin(angle) * size.y) * unevenness;
                    point += delta;
                    int triangle = (i - 1) * 3;
                    triangles[triangle] = 0;
                    triangles[triangle + 1] = i;
                    triangles[triangle + 2] = i % sides + 1;
                }
                vertices[i] = new Vector3((point.x * rect.width - sprite.pivot.x) / sprite.pixelsPerUnit,
                    (point.y * rect.height - sprite.pivot.y) / sprite.pixelsPerUnit);
                uv[i] = new Vector2((textureRect.x + point.x * textureRect.width) / sprite.texture.width,
                    (textureRect.y + point.y * textureRect.height) / sprite.texture.height);
            }
            var mesh = new Mesh { name = "IrregularMatchChip", vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        public static void DropCratePlank(SpriteRenderer source, int state)
        {
            if (source == null || source.sprite == null || state < 2) return;
            // Sample the unobstructed single-plank state, so neither the crate nor crossing bars fall with it.
            var prefab = Resources.Load<GameObject>("Blocks/SolidBlock");
            var layers = prefab != null ? prefab.GetComponent<LayeredBlock>() : null;
            if (layers == null || layers.layers.Length < 2) return;
            var plankSprite = layers.layers[1].GetComponent<SpriteRenderer>().sprite;
            if (plankSprite == null) return;
            var effect = Create(source, "FallingWholeCratePlank");
            effect.material = new Material(source.sharedMaterial);
            effect.material.SetTexture("_MainTex", plankSprite.texture);
            var mesh = BuildPlank(plankSprite, out float sourceAngle);
            effect.meshes.Add(mesh);
            var node = new GameObject("DetachedRoundedWoodenPlank");
            node.transform.SetParent(effect.transform, false);
            // Two reinforcements at most: rising diagonal, then descending diagonal.
            float angle = state >= 3 ? 45f : sourceAngle;
            node.transform.localRotation = Quaternion.Euler(0f, 0f, angle - sourceAngle);
            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = node.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = effect.material;
            renderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            renderer.sortingOrder = PowerUpEffectOrder.Value + 1;
            var colors = new Color[mesh.vertexCount];
            for (int i = 0; i < colors.Length; i++) colors[i] = source.color;
            mesh.colors = colors;
            float sign = state == 3 ? -1f : 1f;
            effect.pieces.Add(new Piece { transform = node.transform, origin = node.transform.position,
                rotation = node.transform.localRotation, mesh = mesh, colors = colors, originalAlpha = source.color.a,
                velocity = new Vector3(sign * 0.65f, 1.15f) * ParcelFeedbackSettings.Intensity,
                spin = sign * Random.Range(280f, 380f) });
            effect.StartCoroutine(effect.Fall(9.5f, float.PositiveInfinity, float.PositiveInfinity,
                ParcelFeedbackSettings.Speed * 1.4f));
        }

        static Mesh BuildPlank(Sprite sprite, out float angle)
        {
            // Rounded outline follows the two nail centers of the single descending plank.
            // Pixel-relative coordinates preserve its silhouette without a rectangular PNG cutout.
            var rect = sprite.rect;
            var a = new Vector2(rect.width * 0.24f, rect.height * 0.766f);
            var b = new Vector2(rect.width * 0.76f, rect.height * 0.26f);
            var center = (a + b) * 0.5f;
            var axis = (b - a).normalized;
            var normal = new Vector2(-axis.y, axis.x);
            angle = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg;
            float radius = Mathf.Min(rect.width, rect.height) * 0.12f;
            const int halfSegments = 12;
            int sides = (halfSegments + 1) * 2;
            var vertices = new Vector3[sides + 1];
            var uv = new Vector2[sides + 1];
            var triangles = new int[sides * 3];
            var textureRect = sprite.textureRect;
            for (int i = 0; i <= sides; i++)
            {
                var point = center;
                if (i > 0)
                {
                    int index = i - 1;
                    bool farEnd = index <= halfSegments;
                    int step = farEnd ? index : index - halfSegments - 1;
                    float arc = (farEnd ? Mathf.PI * 0.5f : -Mathf.PI * 0.5f) - step * Mathf.PI / halfSegments;
                    point = (farEnd ? b : a) + (axis * Mathf.Cos(arc) + normal * Mathf.Sin(arc)) * radius;
                    int t = index * 3;
                    triangles[t] = 0; triangles[t + 1] = i; triangles[t + 2] = i % sides + 1;
                }
                vertices[i] = (Vector3)((point - center) / sprite.pixelsPerUnit);
                uv[i] = new Vector2((textureRect.x + point.x) / sprite.texture.width,
                    (textureRect.y + point.y) / sprite.texture.height);
            }
            var mesh = new Mesh { name = "RoundedWholeWoodenPlank", vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        public static void DropDoor(SpriteRenderer source, float direction)
        {
            if (source == null) return;
            var effect = Create(source, "DetachedFallingCabinetDoor");
            var node = new GameObject("WholeDetachedDoor");
            node.transform.SetParent(effect.transform, false);
            var renderer = node.AddComponent<SpriteRenderer>();
            renderer.sprite = source.sprite; renderer.sharedMaterial = source.sharedMaterial;
            renderer.flipX = source.flipX; renderer.color = source.color;
            renderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            renderer.sortingOrder = PowerUpEffectOrder.Value + 4;
            effect.pieces.Add(new Piece { transform = node.transform, origin = source.transform.position,
                velocity = new Vector3(direction * 0.65f, 1.45f), spin = direction * 125f });
            effect.StartCoroutine(effect.Fall(9.5f));
        }

        static FallingBoardDebris Create(SpriteRenderer source, string name)
        {
            var root = new GameObject(name);
            // Match pieces return to the pool immediately. Debris belongs to the field,
            // so hiding/reusing the original piece cannot hide its falling shards.
            var item = source.GetComponentInParent<Item>();
            var field = item != null ? item.field : source.GetComponentInParent<FieldBoard>();
            var parent = field != null ? field.transform : source.transform.parent;
            root.transform.SetParent(parent, false);
            root.transform.position = source.transform.position;
            root.transform.rotation = source.transform.rotation;
            var parentScale = parent != null ? parent.lossyScale : Vector3.one;
            root.transform.localScale = new Vector3(source.transform.lossyScale.x / parentScale.x,
                source.transform.lossyScale.y / parentScale.y, source.transform.lossyScale.z / parentScale.z);
            var effect = root.AddComponent<FallingBoardDebris>();
            var camera = Camera.main;
            effect.bottom = camera != null
                ? camera.ViewportToWorldPoint(new Vector3(0, 0, Mathf.Abs(camera.transform.position.z - source.transform.position.z))).y - 1.5f
                : source.transform.position.y - 10f;
            return effect;
        }

        IEnumerator Fall(float gravity, float duration = 3f, float fadeStart = 3f, float speed = 1f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime * speed;
                bool visible = false;
                foreach (var piece in pieces)
                {
                    var position = piece.origin + piece.velocity * elapsed - Vector3.up * (gravity * elapsed * elapsed * 0.5f);
                    piece.transform.position = position;
                    piece.transform.localRotation = piece.rotation * Quaternion.Euler(0, 0, piece.spin * elapsed);
                    if (piece.mesh != null && elapsed > fadeStart)
                    {
                        float alpha = piece.originalAlpha * (1f - Mathf.SmoothStep(0f, 1f,
                            Mathf.InverseLerp(fadeStart, duration, elapsed)));
                        for (int i = 0; i < piece.colors.Length; i++) piece.colors[i].a = alpha;
                        piece.mesh.colors = piece.colors;
                    }
                    if (position.y > bottom) visible = true;
                }
                if (!visible) break;
                yield return null;
            }
            Destroy(gameObject);
        }
        void OnDestroy() { foreach (var mesh in meshes) if (mesh != null) Destroy(mesh); if (material != null) Destroy(material); if (burstMaterial != null) Destroy(burstMaterial); }
    }
}
