using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>Swaps authored paint materials for a readable disabled state without changing source pixels.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class RoyalPostmanActionVisual : MonoBehaviour
    {
        public Image[] paint;
        public Material[] ready, disabled;
        public Graphic[] tintedWhenDisabled;
        public Color enabledTint = Color.white;
        public Color disabledTint = new Color(.60f,.60f,.64f,1);
        Button button;
        void OnEnable()=>Refresh();
        void LateUpdate()=>Refresh();
        public void Refresh()=>Refresh(null);
        // A caller can preview the paint without changing the button's actual interaction rules.
        public void Refresh(bool? interactableOverride)
        {
            if(button==null)button=GetComponent<Button>();
            bool interactable=interactableOverride??button.IsInteractable();
            if(tintedWhenDisabled!=null)foreach(var graphic in tintedWhenDisabled)
                if(graphic!=null)graphic.color=interactable?enabledTint:disabledTint;
            if(paint==null)return;
            var materials=interactable?ready:disabled;
            for(int i=0;materials!=null&&i<paint.Length&&i<materials.Length;i++)
                if(paint[i]!=null&&paint[i].material!=materials[i])paint[i].material=materials[i];
        }
    }
}
