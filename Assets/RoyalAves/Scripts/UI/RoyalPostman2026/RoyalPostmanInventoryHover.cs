using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>Transient pointer feedback, independent from the selected inventory item.</summary>
    public sealed class RoyalPostmanInventoryHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Outline outline;
        public bool IsHovered { get; private set; }

        public void OnPointerEnter(PointerEventData data) { SetHovered(true); }
        public void OnPointerExit(PointerEventData data) { SetHovered(false); }
        void OnDisable() { SetHovered(false); }
        void SetHovered(bool value)
        {
            var button=GetComponent<Button>();
            IsHovered=value&&isActiveAndEnabled&&button!=null&&button.IsInteractable();
            if(outline!=null)outline.enabled=IsHovered;
        }
        // The layout editor forwards hover only; its clicks continue selecting authored elements.
        public void PreviewHover(bool value)
        {
            SetHovered(value);
            var button=GetComponent<Button>();if(button==null)return;
            var pointer=new PointerEventData(null);
            if(value)button.OnPointerEnter(pointer);else button.OnPointerExit(pointer);
        }
    }
}
