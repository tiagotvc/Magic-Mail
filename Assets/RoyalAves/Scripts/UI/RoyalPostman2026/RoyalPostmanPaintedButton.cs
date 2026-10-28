using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>The fixed rim never moves. Artwork and labels belong under MovingCenter.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class RoyalPostmanPaintedButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public RectTransform movingCenter;
        [Min(0)] public float pressTravel = 5;
        [Range(.5f, 1)] public float pressScale = 1;
        Vector2 rest;
        Vector3 restScale;
        bool captured;

        void OnEnable() { Capture(); SetPressed(false); }
        void Capture() { if (!captured && movingCenter != null) { rest = movingCenter.anchoredPosition; restScale = movingCenter.localScale; captured = true; } }
        public void SetPressed(bool pressed)
        {
            Capture();
            if (movingCenter != null)
            {
                movingCenter.anchoredPosition = rest + (pressed ? Vector2.down * pressTravel : Vector2.zero);
                movingCenter.localScale = pressed ? restScale * pressScale : restScale;
            }
        }
        public void OnPointerDown(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left && GetComponent<Button>().IsInteractable()) SetPressed(true); }
        public void OnPointerUp(PointerEventData data) { SetPressed(false); }
        public void OnPointerExit(PointerEventData data) { SetPressed(false); }
        void OnDisable() { SetPressed(false); }
    }
}
