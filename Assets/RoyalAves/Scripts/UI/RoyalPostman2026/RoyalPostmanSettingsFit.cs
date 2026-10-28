using UnityEngine;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// Fits the painted window as one proportional layout. The dimmer still covers the full lobby.
    [ExecuteAlways]
    public sealed class RoyalPostmanSettingsFit : MonoBehaviour
    {
        public RectTransform panel;
        public Vector2 designSize = new Vector2(1000, 1350);
        public Rect? SafeAreaOverride { get; set; }

        void OnEnable() => Fit();
        void LateUpdate() => Fit();

        public void Fit()
        {
            var host = transform as RectTransform;
            if (host == null || panel == null) return;
            // The game scene retains an old -107px settings offset. This window's full-screen
            // input shield must follow its parent instead of inheriting that obsolete offset.
            host.anchorMin = Vector2.zero;
            host.anchorMax = Vector2.one;
            host.pivot = Vector2.one * .5f;
            host.sizeDelta = Vector2.zero;
            host.anchoredPosition = Vector2.zero;
            host.localScale = Vector3.one;
            if (host.rect.width <= 0 || host.rect.height <= 0) return;
            var safe = host.rect;
            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Rect? pixels = SafeAreaOverride;
            if (!pixels.HasValue && Application.isPlaying && (camera == null || camera.targetTexture == null)) pixels = Screen.safeArea;
            if (pixels.HasValue && pixels.Value.width > 0 && pixels.Value.height > 0)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(host, pixels.Value.min, camera, out var min);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(host, pixels.Value.max, camera, out var max);
                var area = Rect.MinMaxRect(Mathf.Max(safe.xMin, min.x), Mathf.Max(safe.yMin, min.y),
                    Mathf.Min(safe.xMax, max.x), Mathf.Min(safe.yMax, max.y));
                if (area.width > 0 && area.height > 0) safe = area;
            }
            float margin = safe.width * .045f;
            float scale = Mathf.Min((safe.width - 2 * margin) / designSize.x,
                (safe.height - 2 * margin) / designSize.y);
            panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.one * .5f;
            panel.anchoredPosition = safe.center - host.rect.center;
            panel.sizeDelta = designSize;
            panel.localScale = Vector3.one * Mathf.Max(.001f, scale);
        }
    }
}
