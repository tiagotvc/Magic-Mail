using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    // A seam in the continuous painted bar, not a border around each tab.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalPostmanNavigationDivider : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var r=rectTransform.rect;
            const int rows=32;
            for(int row=0;row<rows;row++)
            {
                float t0=row/(float)rows,t1=(row+1)/(float)rows;
                for(int column=0;column<2;column++)
                {
                    var shade=column==0?new Color(.32f,.015f,.045f,.65f):new Color(1f,.48f,.35f,.32f);
                    int start=mesh.currentVertCount;
                    float x0=Mathf.Lerp(r.xMin,r.xMax,column*.5f),x1=Mathf.Lerp(r.xMin,r.xMax,(column+1)*.5f);
                    float y0=Mathf.Lerp(r.yMin,r.yMax,t0),y1=Mathf.Lerp(r.yMin,r.yMax,t1);
                    var lower=shade;lower.a*=Mathf.SmoothStep(0,1,t0*rows);
                    var upper=shade;upper.a*=Mathf.SmoothStep(0,1,t1*rows);
                    mesh.AddVert(new Vector3(x0,y0),lower,Vector2.zero);mesh.AddVert(new Vector3(x0,y1),upper,Vector2.zero);
                    mesh.AddVert(new Vector3(x1,y1),upper,Vector2.zero);mesh.AddVert(new Vector3(x1,y0),lower,Vector2.zero);
                    mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
                }
            }
        }
    }
}
