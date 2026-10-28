using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    [ExecuteAlways]
    public sealed class RoyalPostmanProgressClip : MonoBehaviour
    {
        public Image valueSource;
        public RectTransform clipping;
        public float fullWidth;
        // Opt in for authored bars whose parent can be resized in the Scene view.
        public bool useParentWidth;
        void LateUpdate() { if (useParentWidth) Refresh(); }
        public void Refresh()
        {
            if(valueSource==null||clipping==null)return;
            float width=useParentWidth&&clipping.parent is RectTransform parent?parent.rect.width:fullWidth;
            float target=Mathf.Max(0,width)*Mathf.Clamp01(valueSource.fillAmount);
            if(!Mathf.Approximately(clipping.rect.width,target))
                clipping.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,target);
        }
    }
}
