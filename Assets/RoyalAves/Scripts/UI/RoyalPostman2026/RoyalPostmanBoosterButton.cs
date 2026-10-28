using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>Visual state only. Inventory and booster activation stay with the game's existing systems.</summary>
    public sealed class RoyalPostmanBoosterButton : MonoBehaviour
    {
        public enum VisualState { Available, Selected, Locked }
        public Image face;
        public Material availablePaint, selectedPaint, lockedPaint;
        public GameObject quantity, selection, padlock;
        public Text quantityText;
        public void SetState(VisualState state)
        {
            if(face!=null)face.material=state==VisualState.Locked?lockedPaint:state==VisualState.Selected?selectedPaint:availablePaint;
            if(quantity!=null)quantity.SetActive(state!=VisualState.Locked);
            if(selection!=null)selection.SetActive(state==VisualState.Selected);
            if(padlock!=null)padlock.SetActive(state==VisualState.Locked);
            GetComponent<Button>().interactable=state!=VisualState.Locked;
        }
        public void SetQuantity(int value) { if(quantityText!=null)quantityText.text=Mathf.Max(0,value).ToString(); }
    }
}
