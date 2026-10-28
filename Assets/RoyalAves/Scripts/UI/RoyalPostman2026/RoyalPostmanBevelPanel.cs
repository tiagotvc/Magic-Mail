using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>Resolution independent enamel recesses with a continuous rounded bevel.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalPostmanBevelPanel : MaskableGraphic
    {
        public Color top=new Color(.015f,.25f,.67f),bottom=new Color(.005f,.04f,.17f);
        public Color rim=new Color(.025f,.66f,1),shadow=new Color(0,.015f,.07f);
        public float radius=36,bevel=7;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;const int steps=12,count=52;
            for(int ring=0;ring<5;ring++)
            {
                float inset=ring==0?-1:ring==1?0:ring==2?bevel*.35f:ring==3?bevel:bevel+2;
                var box=new Rect(r.x+inset,r.y+inset,r.width-inset*2,r.height-inset*2);
                float rad=Mathf.Clamp(radius-inset,0,Mathf.Min(box.width,box.height)*.5f);
                for(int corner=0;corner<4;corner++)for(int j=0;j<=steps;j++)
                {
                    float angle=(corner*90+j*90f/steps)*Mathf.Deg2Rad;
                    var p=box.center+new Vector2((corner==0||corner==3?1:-1)*(box.width*.5f-rad),(corner<2?1:-1)*(box.height*.5f-rad))+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*rad;
                    float t=Mathf.InverseLerp(r.yMin,r.yMax,p.y);var c=ring<=1?shadow:ring==2?Color.Lerp(rim*.35f,rim,t):ring==3?shadow:Color.Lerp(bottom,top,t);
                    c*=color;if(ring==0)c.a=0;vh.AddVert(p,c,Vector2.zero);
                }
            }
            for(int ring=0;ring<4;ring++)for(int j=0;j<count;j++){int n=(j+1)%count,a=ring*count+j,b=ring*count+n,c=a+count,d=b+count;vh.AddTriangle(a,c,d);vh.AddTriangle(a,d,b);}
            int middle=vh.currentVertCount;vh.AddVert(r.center,Color.Lerp(bottom,top,.5f)*color,Vector2.zero);
            for(int j=0;j<count;j++)vh.AddTriangle(middle,count*4+j,count*4+(j+1)%count);
        }
    }
}
