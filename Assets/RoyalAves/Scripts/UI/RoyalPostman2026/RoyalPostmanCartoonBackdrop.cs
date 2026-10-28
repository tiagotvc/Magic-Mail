using UnityEngine;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>Keeps the painted tabletop under the separate apparatus without stretching either asset.</summary>
    public sealed class RoyalPostmanCartoonBackdrop : MonoBehaviour
    {
        public RectTransform artwork;
        public float aspect = .75f;
        [Range(.1f,.9f)] public float tableFractionFromTop = .684f;
        public float tableHeightFromBottom = 580;
        public void Fit()
        {
            if(artwork==null)return;
            var r=((RectTransform)transform).rect;
            float h=Mathf.Max(r.width/aspect,tableHeightFromBottom/(1-tableFractionFromTop),(r.height-tableHeightFromBottom)/tableFractionFromTop);
            artwork.anchorMin=artwork.anchorMax=artwork.pivot=Vector2.one*.5f;
            artwork.sizeDelta=new Vector2(h*aspect,h);
            artwork.anchoredPosition=new Vector2(0,r.yMin+tableHeightFromBottom+(tableFractionFromTop-.5f)*h);
        }
        void OnEnable()=>Fit();
    }
}
