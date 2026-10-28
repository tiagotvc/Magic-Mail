using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>Fits the painted vertical track and fills it up to the live paw handle.</summary>
    [ExecuteAlways]
    public sealed class RoyalPostmanInventoryScrollbar : MonoBehaviour
    {
        public Scrollbar scrollbar;
        public ScrollRect inventory;
        public RectTransform trackArt, fillArt;

        void OnEnable() { Canvas.willRenderCanvases += Refresh; }
        void OnDisable() { Canvas.willRenderCanvases -= Refresh; }
        void LateUpdate() { Refresh(); }

        public void Refresh()
        {
            if (scrollbar == null || trackArt == null || fillArt == null || scrollbar.handleRect == null) return;
            var track = (RectTransform)scrollbar.transform;
            var bounds = track.rect;
            if (inventory != null && bounds.height > 0)
            {
                bool overflow = inventory.content.rect.height > inventory.viewport.rect.height + .1f;
                // The visible thumb is the small paw badge, so its travel must use the full rail.
                scrollbar.size = overflow ? Mathf.Clamp01(36 / bounds.height) : 1;
                scrollbar.interactable = overflow;
            }
            // Artwork stays horizontal in source space, rotated as a whole without distortion.
            trackArt.sizeDelta = new Vector2(bounds.height, bounds.width);
            float center = track.InverseTransformPoint(scrollbar.handleRect.position).y;
            float length = Mathf.Clamp(bounds.yMax - center, 0, bounds.height);
            fillArt.sizeDelta = new Vector2(length, Mathf.Max(1, bounds.width - 6));
            fillArt.anchoredPosition = new Vector2(0, (bounds.height - length) * .5f);
        }
    }
}
