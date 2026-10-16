using System;
using System.Collections;
using System.Collections.Generic;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.System;
using UnityEngine;

namespace RoyalAves.Effects
{
    // Legacy class name retained. The wooden crate breaks into rigid fragments after its final hit.
    public sealed class ParcelTearEffect : MonoBehaviour
    {
        public const float Duration = 0.62f;
        const float TensionDuration = 0.12f;
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly List<Piece> pieces = new List<Piece>();
        Material material;
        LevelManager manager;
        AnimateItems barrier;
        Action onComplete;
        Vector3 restScale;
        Quaternion restRotation;
        SpriteRenderer sourceRenderer;

        sealed class Piece
        {
            public Transform transform;
            public Mesh mesh;
            public Color32[] colors;
            public Vector3 direction;
            public Vector3 origin;
            public float spin;
        }

        public static void Play(SpriteRenderer source, LevelManager manager, Action onComplete)
        {
            var root = new GameObject("ParcelCompletionFX");
            root.transform.SetParent(source.transform.parent, false);
            root.transform.localPosition = source.transform.localPosition;
            root.transform.localRotation = source.transform.localRotation;
            root.transform.localScale = source.transform.localScale;
            root.AddComponent<ParcelTearEffect>().Initialize(source, manager, onComplete);
        }

        void Initialize(SpriteRenderer source, LevelManager owner, Action callback)
        {
            manager = owner;
            onComplete = callback;
            restScale = transform.localScale;
            restRotation = transform.localRotation;
            sourceRenderer = source;
            // Join the native animation queue so win/lose and board resolution wait for the tear.
            barrier = gameObject.AddComponent<AnimateItems>();
            barrier.target = true;
            barrier.linkObjectHash = source.transform.parent.gameObject.GetHashCode();
            manager.animateItems.Add(barrier);
            material = new Material(source.sharedMaterial);
            material.SetTexture("_MainTex", source.sprite.texture);
            for (int row = 0; row < 3; row++)
            for (int col = 0; col < 4; col++)
            {
                var region = new Rect(col / 4f, row / 3f, 0.25f, 1f / 3f);
                var radial = (region.center - Vector2.one * 0.5f).normalized;
                var direction = new Vector3(radial.x * 0.55f, radial.y * 0.38f + 0.16f);
                float spin = ((row + col) % 2 == 0 ? 1f : -1f) * (55f + col * 13f);
                AddPiece(BuildRegion(source.sprite, region), source.color, direction, spin);
            }
            StartCoroutine(Animate());
        }

        void AddPiece(Mesh mesh, Color color, Vector3 direction, float spin)
        {
            meshes.Add(mesh);
            var origin = mesh.bounds.center;
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++) vertices[i] -= origin;
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            var child = new GameObject("WoodenCrateFragment");
            child.transform.SetParent(transform, false);
            child.transform.localPosition = origin;
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            renderer.sortingOrder = PowerUpEffectOrder.Value + pieces.Count;
            var colors = new Color32[mesh.vertexCount];
            for (int i = 0; i < colors.Length; i++) colors[i] = color;
            mesh.colors32 = colors;
            pieces.Add(new Piece { transform = child.transform, mesh = mesh, colors = colors, origin = origin, direction = direction, spin = spin });
        }

        IEnumerator Animate()
        {
            float elapsed = 0f;
            bool burstStarted = false;
            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime * ParcelFeedbackSettings.Speed;
                if (elapsed < TensionDuration)
                {
                    float shake = Mathf.Sin(elapsed / TensionDuration * Mathf.PI * 4f);
                    transform.localRotation = restRotation * Quaternion.Euler(0f, 0f, shake * 1.6f * ParcelFeedbackSettings.Intensity);
                }
                else
                {
                    if (!burstStarted)
                    {
                        burstStarted = true;
                        ParcelSoundEffects.Play(1);
                        ParcelSparkEffect.Play(sourceRenderer, true);
                    }
                    transform.localScale = restScale;
                    transform.localRotation = restRotation;
                    float t = Mathf.Clamp01((elapsed - TensionDuration) / (Duration - TensionDuration));
                    float travel = 1f - Mathf.Pow(1f - t, 3f);
                    float alpha = 1f - Mathf.SmoothStep(0f, 1f, t);
                    foreach (var piece in pieces)
                    {
                        piece.transform.localPosition = piece.origin + (piece.direction * travel - Vector3.up * (0.4f * t * t)) * ParcelFeedbackSettings.Intensity;
                        piece.transform.localScale = Vector3.one * (1f - travel * 0.15f);
                        piece.transform.localRotation = Quaternion.Euler(0f, 0f, piece.spin * travel);
                        for (int i = 0; i < piece.colors.Length; i++) piece.colors[i].a = (byte)(255f * alpha);
                        piece.mesh.colors32 = piece.colors;
                    }
                }
                yield return null;
            }
            // Keep the barrier while collection runs; the last goal must not win mid-animation.
            try { onComplete?.Invoke(); }
            finally
            {
                ReleaseBarrier();
                Destroy(gameObject);
            }
        }

        void ReleaseBarrier()
        {
            if (manager != null && barrier != null) manager.animateItems.Remove(barrier);
            barrier = null;
        }

        void OnDestroy()
        {
            // Destroying a field/restarting cancels its effect without collecting anything.
            ReleaseBarrier();
            foreach (var mesh in meshes) if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
            onComplete = null;
        }

        public static Mesh BuildRegion(Sprite sprite, Rect region, bool circular = false)
        {
            int segments = circular ? 32 : 4;
            var vertices = new Vector3[segments + 1];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[segments * 3];
            MapVertex(sprite, region.center, out vertices[0], out uv[0]);
            for (int i = 0; i < segments; i++)
            {
                Vector2 point;
                if (circular)
                {
                    float angle = -i * Mathf.PI * 2f / segments;
                    point = region.center + new Vector2(Mathf.Cos(angle) * region.width / 2f, Mathf.Sin(angle) * region.height / 2f);
                }
                else
                    point = new Vector2(i < 2 ? region.xMin : region.xMax,
                        i == 0 || i == 3 ? region.yMin : region.yMax);
                MapVertex(sprite, point, out vertices[i + 1], out uv[i + 1]);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % segments + 1;
            }
            return CreateMesh(vertices, uv, triangles);
        }

        static void MapVertex(Sprite sprite, Vector2 point, out Vector3 position, out Vector2 uv)
        {
            var rect = sprite.rect;
            var textureRect = sprite.textureRect;
            position = new Vector3((point.x * rect.width - sprite.pivot.x) / sprite.pixelsPerUnit,
                (point.y * rect.height - sprite.pivot.y) / sprite.pixelsPerUnit);
            uv = new Vector2((textureRect.x + point.x * textureRect.width) / sprite.texture.width,
                (textureRect.y + point.y * textureRect.height) / sprite.texture.height);
        }

        static Mesh CreateMesh(Vector3[] vertices, Vector2[] uv, int[] triangles)
        {
            var mesh = new Mesh { name = "ParcelVisualMesh", vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
