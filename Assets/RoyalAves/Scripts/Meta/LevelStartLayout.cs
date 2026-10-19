using UnityEngine;

namespace RoyalAves.Meta
{
    // Scale the window, leaving MenuPlay's existing opening/closing animation free to animate its parent.
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class LevelStartLayout : MonoBehaviour
    {
        void OnEnable() => Fit();
        // RectMask2D/layout rebuilds run in willRenderCanvases. Changing scale there used to leave stale
        // clipping rectangles during the legacy opening animation. Finish geometry before that rebuild.
        void LateUpdate() => Fit();

        void Fit()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var rect = (RectTransform)transform;
            var center = Vector2.one * .5f;
            if (rect.anchorMin != center || rect.anchorMax != center || rect.pivot != center)
                rect.anchorMin = rect.anchorMax = rect.pivot = center;
            if (rect.anchoredPosition != Vector2.zero) rect.anchoredPosition = Vector2.zero;
            var bounds = ((RectTransform)canvas.rootCanvas.transform).rect.size;
            var scale = Mathf.Min(1.28f, bounds.x * .88f / 1060f, bounds.y * .9f / 1536f);
            if (scale > 0) transform.localScale = Vector3.one * scale;
        }
    }
}
