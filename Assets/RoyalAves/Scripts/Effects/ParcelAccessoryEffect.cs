using System.Collections;
using UnityEngine;

namespace RoyalAves.Effects
{
    // Legacy name retained; wooden reinforcement planks detach as rigid pieces.
    public sealed class ParcelAccessoryEffect : MonoBehaviour
    {
        const float Duration = 0.38f;
        Mesh mesh;
        Material material;
        Color32[] colors;

        public static void Play(SpriteRenderer source, int oldState)
        {
            var root = new GameObject("CratePlankRemovalFX");
            root.transform.SetParent(source.transform.parent, false);
            root.transform.localPosition = source.transform.localPosition;
            root.transform.localRotation = source.transform.localRotation;
            root.transform.localScale = source.transform.localScale;
            root.AddComponent<ParcelAccessoryEffect>().Initialize(source, oldState);
        }

        void Initialize(SpriteRenderer source, int state)
        {
            float y = state == 4 ? 0.65f : state == 3 ? 0.455f : 0.26f;
            mesh = ParcelTearEffect.BuildRegion(source.sprite, new Rect(0.16f, y, 0.665f, 0.10f));
            var origin = mesh.bounds.center;
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++) vertices[i] -= origin;
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            var visual = new GameObject("DetachedWoodenPlank");
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = origin;
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            material = new Material(source.sharedMaterial);
            material.SetTexture("_MainTex", source.sprite.texture);
            var renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            renderer.sortingOrder = PowerUpEffectOrder.Value + 1;
            colors = new Color32[mesh.vertexCount];
            for (int i = 0; i < colors.Length; i++) colors[i] = source.color;
            mesh.colors32 = colors;
            ParcelSoundEffects.Play(state);
            ParcelSparkEffect.Play(source, false);
            StartCoroutine(Animate(visual.transform, origin, state, source.color.a));
        }

        IEnumerator Animate(Transform visual, Vector3 origin, int state, float sourceAlpha)
        {
            float elapsed = 0f;
            float sign = state == 3 ? -1f : 1f;
            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime * ParcelFeedbackSettings.Speed;
                float t = Mathf.Clamp01(elapsed / Duration);
                float travel = 1f - Mathf.Pow(1f - t, 2f);
                var displacement = new Vector3(sign * 0.28f * travel, 0.22f * travel - 0.42f * t * t);
                visual.localPosition = origin + displacement * ParcelFeedbackSettings.Intensity;
                visual.localRotation = Quaternion.Euler(0f, 0f, sign * 42f * t);
                float alpha = sourceAlpha * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.4f) / 0.6f)));
                for (int i = 0; i < colors.Length; i++) colors[i].a = (byte)(255f * alpha);
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
