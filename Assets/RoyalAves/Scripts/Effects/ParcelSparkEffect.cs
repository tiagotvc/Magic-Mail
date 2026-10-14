using System.Collections;
using UnityEngine;

namespace RoyalAves.Effects
{
    // Small postal sparkles reinforce a hit without covering the package or changing its objective.
    public sealed class ParcelSparkEffect : MonoBehaviour
    {
        Mesh mesh;
        Material material;
        Color32[] colors;

        public static void Play(SpriteRenderer source, bool completion)
        {
            if (source == null) return;
            var root = new GameObject("ParcelSparkFX");
            root.transform.SetParent(source.transform.parent, false);
            root.transform.localPosition = source.transform.localPosition;
            root.transform.localScale = source.transform.localScale;
            root.AddComponent<ParcelSparkEffect>().Initialize(source, completion);
        }

        void Initialize(SpriteRenderer source, bool completion)
        {
            mesh = new Mesh
            {
                name = "ParcelSparkDiamond",
                vertices = new[] { Vector3.zero, new Vector3(0f, 0.065f), new Vector3(0.032f, 0f), new Vector3(0f, -0.065f), new Vector3(-0.032f, 0f) },
                uv = new[] { new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0f, 0.5f) },
                triangles = new[] { 0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 1 }
            };
            colors = new Color32[5];
            for (int i = 0; i < colors.Length; i++) colors[i] = new Color(1f, 0.92f, 0.63f, 1f);
            mesh.colors32 = colors;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            material = new Material(source.sharedMaterial);
            material.SetTexture("_MainTex", Texture2D.whiteTexture);
            var sparks = new Transform[completion ? 8 : 4];
            for (int i = 0; i < sparks.Length; i++)
            {
                var spark = new GameObject("PostalSpark");
                spark.transform.SetParent(transform, false);
                spark.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = spark.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
                renderer.sortingOrder = PowerUpEffectOrder.Value + 3;
                sparks[i] = spark.transform;
            }
            StartCoroutine(Animate(sparks, completion));
        }

        IEnumerator Animate(Transform[] sparks, bool completion)
        {
            float elapsed = 0f;
            float duration = completion ? 0.34f : 0.22f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime * ParcelFeedbackSettings.Speed;
                float t = Mathf.Clamp01(elapsed / duration);
                float travel = 1f - Mathf.Pow(1f - t, 3f);
                for (int i = 0; i < sparks.Length; i++)
                {
                    float angle = (i + 0.5f) * Mathf.PI * 2f / sparks.Length;
                    float radius = (completion ? 0.48f + travel * 0.30f : 0.08f + travel * 0.30f) * ParcelFeedbackSettings.Intensity;
                    sparks[i].localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    sparks[i].localRotation = Quaternion.Euler(0f, 0f, i * 37f + t * 40f);
                    sparks[i].localScale = Vector3.one * ((0.5f + Mathf.Sin(t * Mathf.PI) * 0.65f) * ParcelFeedbackSettings.Intensity);
                }
                for (int i = 0; i < colors.Length; i++) colors[i].a = (byte)(255f * (1f - t));
                mesh.colors32 = colors;
                yield return null;
            }
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
        }
    }
}
