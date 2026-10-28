using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    [RequireComponent(typeof(Text))]
    public sealed class RoyalPostmanCreamText : BaseMeshEffect
    {
        public override void ModifyMesh(VertexHelper vh)
        {
            if(!IsActive())return;var rect=((RectTransform)transform).rect;var v=new UIVertex();
            for(int i=0;i<vh.currentVertCount;i++)
            {
                vh.PopulateUIVertex(ref v,i);float t=Mathf.InverseLerp(rect.yMin,rect.yMax,v.position.y);
                var c=Color.Lerp(new Color(1,.83f,.56f),new Color(1,.99f,.90f),Mathf.Clamp01(t*1.45f));
                c.a=v.color.a/255f;v.color=c;vh.SetUIVertex(v,i);
            }
        }
    }
}
