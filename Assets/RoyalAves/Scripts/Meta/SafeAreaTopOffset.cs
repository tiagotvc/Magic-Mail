// Keeps a top-stretched RectTransform exactly `topOffsetPixels` device pixels below the safe area (the notch or
// status bar), on every device, regardless of screen resolution or aspect ratio. The canvas here scales its UI units
// with screen width (CanvasScaler, ScaleWithScreenSize), so a fixed on-screen pixel gap must be converted through
// canvas.scaleFactor rather than expressed as a literal anchoredPosition, which would only be correct at one width.
using UnityEngine;

namespace RoyalAves.Meta
{
    // ExecuteAlways so the gap is also exact in the Editor's Game view (including the Device Simulator) without
    // pressing Play, not only in builds.
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaTopOffset : MonoBehaviour
    {
        [SerializeField] float topOffsetPixels = 60f;

        void LateUpdate()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null || canvas.scaleFactor <= 0) return;
            var notchInset = Mathf.Max(0, Screen.height - Screen.safeArea.yMax);
            var rect = (RectTransform)transform;
            var pos = rect.anchoredPosition;
            pos.y = -(notchInset + topOffsetPixels) / canvas.scaleFactor;
            rect.anchoredPosition = pos;
        }
    }
}
