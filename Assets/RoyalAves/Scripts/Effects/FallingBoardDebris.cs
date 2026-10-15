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
        float bottom;

        public static void Shatter(SpriteRenderer source)
        {
            if (source == null || source.sprite == null) return;
            var effect = Create(source, "FallingMatchShards");
            effect.material = new Material(source.sharedMaterial);
            effect.material.SetTexture("_MainTex", source.sprite.texture);
            const int shardCount = 8;
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
                for (int i = 0; i < colors.Length; i++) colors[i] = source.color;
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
                effect.pieces.Add(new Piece { transform = node.transform, origin = node.transform.position,
                    velocity = new Vector3((center.x - 0.5f) * 1.4f + Random.Range(-0.1f, 0.1f),
                        Random.Range(0.35f, 0.75f)), spin = Random.Range(-65f, 65f),
                    mesh = mesh, colors = colors, originalAlpha = source.color.a });
            }
            // Scale the entire ballistic displacement by 30%, preserving its timing.
            foreach (var piece in effect.pieces) piece.velocity *= 1.3f;
            effect.StartCoroutine(effect.Fall(13f, 0.55f, 0.18f));
        }

        public static void ShatterWood(SpriteRenderer source, int state)
        {
            if (source == null || source.sprite == null) return;
            var effect = Create(source, "FallingWoodSplinters");
            effect.material = new Material(source.sharedMaterial);
            effect.material.SetTexture("_MainTex", source.sprite.texture);
            bool plank = state > 1;
            float angle = state == 4 ? 0f : state == 3 ? 45f : -45f;
            var axis = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            var normal = new Vector2(-axis.y, axis.x);
            int count = plank ? 6 : 12;
            for (int i = 0; i < count; i++)
            {
                var center = plank
                    ? Vector2.one * 0.5f + axis * Mathf.Lerp(-0.34f, 0.34f, (i + 0.5f) / count)
                        + normal * Random.Range(-0.045f, 0.045f)
                    : new Vector2(Random.Range(0.17f, 0.83f), Random.Range(0.17f, 0.83f));
                var mesh = BuildChip(source.sprite, center, true, plank ? angle : Random.Range(65f, 115f));
                var origin = mesh.bounds.center;
                var vertices = mesh.vertices;
                for (int j = 0; j < vertices.Length; j++) vertices[j] -= origin;
                mesh.vertices = vertices;
                mesh.RecalculateBounds();
                effect.meshes.Add(mesh);
                var node = new GameObject("JaggedWoodSplinter");
                node.transform.SetParent(effect.transform, false);
                node.transform.localPosition = origin;
                node.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = node.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = effect.material;
                renderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
                renderer.sortingOrder = PowerUpEffectOrder.Value + 2;
                var colors = new Color[mesh.vertexCount];
                for (int j = 0; j < colors.Length; j++) colors[j] = source.color;
                mesh.colors = colors;
                effect.pieces.Add(new Piece { transform = node.transform, origin = node.transform.position,
                    velocity = new Vector3(Random.Range(-1.1f, 1.1f), Random.Range(0.65f, 1.8f)) * ParcelFeedbackSettings.Intensity,
                    spin = Random.Range(-260f, 260f), mesh = mesh, colors = colors, originalAlpha = source.color.a });
            }
            // No fade or fixed timeout: wood survives until every splinter leaves the screen.
            effect.StartCoroutine(effect.Fall(9.5f, float.PositiveInfinity, float.PositiveInfinity,
                ParcelFeedbackSettings.Speed * 1.4f));
        }

        static Mesh BuildChip(Sprite sprite, Vector2 center, bool wood = false, float angleDegrees = 0f)
        {
            int sides = wood ? Random.Range(12, 17) : Random.Range(5, 8);
            var vertices = new Vector3[sides + 1];
            var uv = new Vector2[sides + 1];
            var triangles = new int[sides * 3];
            float rotation = wood ? 0f : Random.Range(0f, Mathf.PI * 2f);
            float woodRadius = wood ? Random.Range(0.075f, 0.11f) : 0f;
            var size = wood
                ? new Vector2(woodRadius, woodRadius * Random.Range(0.85f, 1.1f))
                : new Vector2(Random.Range(0.07f, 0.11f), Random.Range(0.06f, 0.1f));
            var rect = sprite.rect;
            var textureRect = sprite.textureRect;
            for (int i = 0; i <= sides; i++)
            {
                var point = center;
                if (i > 0)
                {
                    float angle = rotation - (i - 1) * Mathf.PI * 2f / sides;
                    float unevenness = Random.Range(wood ? 0.86f : 0.65f, 1f);
                    var delta = new Vector2(Mathf.Cos(angle) * size.x, Mathf.Sin(angle) * size.y) * unevenness;
                    float radians = angleDegrees * Mathf.Deg2Rad;
                    point += new Vector2(delta.x * Mathf.Cos(radians) - delta.y * Mathf.Sin(radians),
                        delta.x * Mathf.Sin(radians) + delta.y * Mathf.Cos(radians));
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
            var mesh = new Mesh { name = wood ? "JaggedWoodSplinter" : "IrregularMatchChip", vertices = vertices, uv = uv, triangles = triangles };
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
            // Match the bar removed by this damage state: horizontal, rising diagonal, descending diagonal.
            float angle = state == 4 ? 0f : state == 3 ? 45f : sourceAngle;
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
            var a = new Vector2(rect.width * 0.25f, rect.height * 0.756f);
            var b = new Vector2(rect.width * 0.776f, rect.height * 0.262f);
            var center = (a + b) * 0.5f;
            var axis = (b - a).normalized;
            var normal = new Vector2(-axis.y, axis.x);
            angle = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg;
            float radius = Mathf.Min(rect.width, rect.height) * 0.103f;
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
        void OnDestroy() { foreach (var mesh in meshes) if (mesh != null) Destroy(mesh); if (material != null) Destroy(material); }
    }
}
