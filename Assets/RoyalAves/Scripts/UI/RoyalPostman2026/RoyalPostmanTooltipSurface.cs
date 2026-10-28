using UnityEngine;
using UnityEngine.EventSystems;

namespace RoyalAves.UI.RoyalPostman2026
{
    // Inventory controls remain clickable while the floating item card is open.
    public sealed class RoyalPostmanTooltipSurface : MonoBehaviour, ICanvasRaycastFilter, IPointerClickHandler
    {
        public RoyalPostmanPetsScreen owner;
        public RectTransform passThrough;
        public bool IsRaycastLocationValid(Vector2 point,Camera camera)=>passThrough==null||!RectTransformUtility.RectangleContainsScreenPoint(passThrough,point,camera);
        public void OnPointerClick(PointerEventData data)=>owner?.HideItemTooltip();
    }
}
