// Sweet Sugar's loading overlay is a 5000 x 5000 square that overflows the screen whatever its size, so anything laid
// out inside it comes out huge. This keeps the loading art covering exactly the screen (the canvas) and "Carregando..."
// near its bottom, whatever the square's size and scale.
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public class LoadingOverlayLayout : MonoBehaviour
    {
        [SerializeField] RectTransform art;
        [SerializeField] RectTransform label;
        [Tooltip("Altura do centro do texto a partir da base da tela (fração da altura da tela).")]
        [SerializeField, Range(0f, 0.5f)] float labelFromBottom = 0.08f;
        [Tooltip("Largura e altura da faixa do texto (frações da tela).")]
        [SerializeField] Vector2 labelSize = new Vector2(0.8f, 0.06f);

        void LateUpdate()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var screen = (RectTransform)canvas.rootCanvas.transform;
            var size = screen.rect.size;
            // This square's scale in canvas units (its children have scale 1).
            var scale = screen.lossyScale.x > 0 ? transform.lossyScale.x / screen.lossyScale.x : 0;
            if (size.x <= 0 || size.y <= 0 || scale <= 0) return;

            if (art != null)
            {
                var image = art.GetComponent<Image>();
                var aspect = image != null && image.sprite != null ? image.sprite.rect.width / image.sprite.rect.height : size.x / size.y;
                // Cover the screen, cropping the art's excess instead of leaving bars.
                var cover = size.x / size.y > aspect ? new Vector2(size.x, size.x / aspect) : new Vector2(size.y * aspect, size.y);
                Place(art, cover / scale, screen.TransformPoint(screen.rect.center));
            }
            if (label != null)
                Place(label, Vector2.Scale(size, labelSize) / scale,
                    screen.TransformPoint(new Vector2(screen.rect.center.x, screen.rect.yMin + size.y * labelFromBottom)));
        }

        static void Place(RectTransform rect, Vector2 size, Vector3 worldCenter)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.position = worldCenter;
        }
    }
}
