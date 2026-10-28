using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    // Native clipping geometry lets any independently selected portrait fit the avatar frame.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalPostmanPortraitMask : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var rect=rectTransform.rect;float radius=Mathf.Min(rect.width,rect.height)*.22f;
            mesh.AddVert(rect.center,color,Vector2.zero);
            for(int corner=0;corner<4;corner++)
            {
                var center=new Vector2(corner==0||corner==3?rect.xMax-radius:rect.xMin+radius,corner<2?rect.yMax-radius:rect.yMin+radius);
                for(int step=0;step<=8;step++)
                {
                    float angle=(corner*90+step*90f/8)*Mathf.Deg2Rad;
                    mesh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,color,Vector2.zero);
                }
            }
            for(int i=0;i<36;i++)mesh.AddTriangle(0,1+i,1+(i+1)%36);
        }
    }
}
