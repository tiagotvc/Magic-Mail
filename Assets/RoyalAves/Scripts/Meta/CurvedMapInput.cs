using UnityEngine;
using UnityEngine.EventSystems;

namespace RoyalAves.Meta
{
    // On a node, forward dragging to the map and suppress the eventual button click.
    // The relay lives on the same GameObject as Button, as required by EventSystem's drag lookup.
    public sealed class CurvedMapInput : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler, IPointerClickHandler
    {
        public CurvedLevelMap Map;
        public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold=true;
        public void OnBeginDrag(PointerEventData e) {if(e.button==PointerEventData.InputButton.Left)Map.BeginDrag(e);}
        public void OnDrag(PointerEventData e) {if(e.button==PointerEventData.InputButton.Left)Map.Drag(e);}
        public void OnEndDrag(PointerEventData e) {if(e.button==PointerEventData.InputButton.Left)Map.EndDrag(e);}
        public void OnScroll(PointerEventData e) => Map.Scroll(e.scrollDelta.y);
        public void OnPointerClick(PointerEventData e) {if(e.button==PointerEventData.InputButton.Left)Map.Click(e);}
    }
}
