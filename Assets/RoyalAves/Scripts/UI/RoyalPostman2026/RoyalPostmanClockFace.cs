using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>A single ivory clock face. Live stock is an independent Text on its lower half.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalPostmanClockFace : MaskableGraphic
    {
        void Disk(VertexHelper vh,float radius,Color center,Color edge)
        {
            int n=vh.currentVertCount;vh.AddVert(Vector2.zero,center*color,Vector2.zero);
            for(int i=0;i<64;i++){float a=i*Mathf.PI/32;vh.AddVert(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,edge*color,Vector2.zero);}
            for(int i=0;i<64;i++)vh.AddTriangle(n,n+1+i,n+1+(i+1)%64);
        }
        void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c)
        {
            var side=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int n=vh.currentVertCount;c*=color;
            vh.AddVert(a-side,c,Vector2.zero);vh.AddVert(a+side,c,Vector2.zero);vh.AddVert(b+side,c,Vector2.zero);vh.AddVert(b-side,c,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();float u=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
            Disk(vh,u,new Color(.35f,.12f,.015f),new Color(.35f,.12f,.015f));
            Disk(vh,u*.95f,new Color(1,.9f,.35f),new Color(1,.57f,.035f));
            Disk(vh,u*.86f,Color.white,new Color(1,.91f,.66f));
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var d=new Vector2(Mathf.Sin(a),Mathf.Cos(a));Line(vh,d*u*.69f,d*u*.77f,u*.055f,new Color(.95f,.55f,.06f));}
            var navy=new Color(.015f,.045f,.13f);var pivot=new Vector2(0,u*.12f);
            Line(vh,pivot,pivot+Vector2.up*u*.47f,u*.105f,navy);Line(vh,pivot,pivot+Vector2.right*u*.36f,u*.105f,navy);
            // Rounded cap at the hands' junction; lower half intentionally reserved for xN.
            int n=vh.currentVertCount;vh.AddVert(pivot,navy,Vector2.zero);for(int i=0;i<24;i++){float a=i*Mathf.PI/12;vh.AddVert(pivot+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*u*.09f,navy,Vector2.zero);}for(int i=0;i<24;i++)vh.AddTriangle(n,n+1+i,n+1+(i+1)%24);
        }
    }
}
