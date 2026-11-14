using UnityEngine;

namespace RoyalAves.Effects
{
    // Separate overlay: never edits the source material, sprite, renderer color or match color.
    public sealed class InkTintVisual : MonoBehaviour
    {
        SpriteRenderer source, overlay;
        Material material;
        float elapsed, duration;
        int orderOffset;
        Vector2 origin;
        public float Progress => duration <= 0 ? 1 : Mathf.Clamp01(elapsed / duration);
        public void SetFloorContour(Vector4 corners, Vector4 exposedEdges)
        {
            material.SetVector("_RoundCorners", corners);
            material.SetVector("_ExposedEdges", exposedEdges);
        }
        public static InkTintVisual Attach(SpriteRenderer source, Material template, Vector2 origin, float duration, bool floor = false, float delay = 0)
        {
            var root = new GameObject("InkTintVisual");
            root.layer = source.gameObject.layer;
            root.transform.SetParent(source.transform, false);
            root.transform.localPosition = new Vector3(0, 0, -.001f);
            var effect = root.AddComponent<InkTintVisual>();
            effect.source = source;
            effect.origin = origin;
            effect.duration = duration;
            effect.elapsed = -delay;
            effect.orderOffset = floor ? 0 : 1;
            effect.material = new Material(template);
            effect.material.SetFloat("_Floor", floor ? 1 : 0);
            // A lighter pigment on pieces keeps faces/glyphs readable against the darker painted floor.
            if (!floor) effect.material.SetColor("_InkColor", new Color(.72f, .32f, 1f, 1f));
            effect.overlay = root.AddComponent<SpriteRenderer>();
            effect.overlay.sharedMaterial = effect.material;
            effect.Refresh();
            return effect;
        }
        void LateUpdate()
        {
            elapsed += Time.deltaTime;
            Refresh();
        }
        void Refresh()
        {
            if (source == null || overlay == null) return;
            overlay.sprite = source.sprite;
            overlay.enabled = source.enabled && source.sprite != null;
            overlay.color = new Color(1, 1, 1, source.color.a);
            overlay.flipX = source.flipX; overlay.flipY = source.flipY;
            overlay.sortingLayerID = source.sortingLayerID;
            overlay.sortingOrder = source.sortingOrder + orderOffset;
            overlay.maskInteraction = source.maskInteraction;
            if (source.sprite != null)
            {
                var bounds = source.sprite.bounds;
                material.SetVector("_LocalRect", new Vector4(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y));
            }
            material.SetVector("_Origin", origin);
            material.SetFloat("_Progress", Mathf.SmoothStep(0, 1, Progress));
        }
        void OnDestroy()
        {
            if (material == null) return;
            if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);
        }
    }
}
