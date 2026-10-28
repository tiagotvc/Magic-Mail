using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>Soft UI light and radial beams; no texture allocation or moving gameplay camera.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalPostmanRevealLight : MaskableGraphic
    {
        public bool rays;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;var center=r.center;float radius=Mathf.Min(r.width,r.height)*.5f;
            if(rays)
            {
                for(int i=0;i<12;i++)
                {
                    float a=i*Mathf.PI/6,spread=(i%2==0?.14f:.19f);int n=vh.currentVertCount;var clear=color;clear.a=0;
                    // Broad, flat wedges with the original crisp sides, fading only at their tips.
                    vh.AddVert(center,color,Vector2.zero);
                    vh.AddVert(center+Direction(a-spread)*radius*.44f,color,Vector2.zero);
                    vh.AddVert(center+Direction(a+spread)*radius*.44f,color,Vector2.zero);
                    vh.AddVert(center+Direction(a-spread)*radius,clear,Vector2.zero);
                    vh.AddVert(center+Direction(a+spread)*radius,clear,Vector2.zero);
                    vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n+1,n+3,n+4);vh.AddTriangle(n+1,n+4,n+2);
                }
                return;
            }
            // Three rings keep the falloff soft instead of exposing a hard circular rim.
            int segments=64;vh.AddVert(center,color,Vector2.zero);
            for(int ring=1;ring<=3;ring++)for(int i=0;i<segments;i++)
            {
                var c=color;c.a*=ring==1?.72f:ring==2?.18f:0;var p=Direction(i*Mathf.PI*2/segments);p=new Vector2(p.x*r.width*.5f,p.y*r.height*.5f);vh.AddVert(center+p*(ring/3f),c,Vector2.zero);
            }
            for(int i=0;i<segments;i++)vh.AddTriangle(0,1+i,1+(i+1)%segments);
            for(int ring=0;ring<2;ring++)for(int i=0;i<segments;i++){int a=1+ring*segments+i,b=1+ring*segments+(i+1)%segments,c=a+segments,d=b+segments;vh.AddTriangle(a,c,d);vh.AddTriangle(a,d,b);}
        }
        static Vector2 Direction(float angle)=>new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
    }
}
