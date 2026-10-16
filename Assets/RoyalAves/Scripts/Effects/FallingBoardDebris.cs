using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Level;

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
            effect.StartCoroutine(effect.Fall(10f, 0.55f, 0.18f));
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
                    point += new Vector2(Mathf.Cos(angle) * size.x, Mathf.Sin(angle) * size.y) * unevenness;
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

        IEnumerator Fall(float gravity, float duration = 3f, float fadeStart = 3f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                bool visible = false;
                foreach (var piece in pieces)
                {
                    var position = piece.origin + piece.velocity * elapsed - Vector3.up * (gravity * elapsed * elapsed * 0.5f);
                    piece.transform.position = position;
                    piece.transform.localRotation = Quaternion.Euler(0, 0, piece.spin * elapsed);
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
