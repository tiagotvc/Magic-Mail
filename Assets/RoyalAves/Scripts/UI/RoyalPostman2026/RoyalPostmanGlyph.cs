using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>Crisp independent vector symbols; no text-font fallback or baked button artwork.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalPostmanGlyph : MaskableGraphic
    {
        public enum Symbol { Plus, Close, Check, Video, BackArrow, Clock, Capsule, ContactShadow, PageDot, Lightning }
        public Symbol symbol;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var size=rectTransform.rect.size;
            float unit=Mathf.Min(size.x,size.y);
            Vector2 center=rectTransform.rect.center;
            System.Action<Vector2,Vector2,float> line=(a,b,width)=>
            {
                a=center+a*unit;b=center+b*unit;
                Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*unit*.5f;
                int start=mesh.currentVertCount;
                mesh.AddVert(a-n,color,Vector2.zero);mesh.AddVert(a+n,color,Vector2.zero);mesh.AddVert(b+n,color,Vector2.zero);mesh.AddVert(b-n,color,Vector2.zero);
                mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
            };
            switch(symbol)
            {
                case Symbol.Lightning:
                    var points=new[]{new Vector2(.08f,.5f),new Vector2(-.34f,.02f),new Vector2(-.07f,.02f),new Vector2(-.16f,-.5f),new Vector2(.35f,.14f),new Vector2(.08f,.14f),new Vector2(.24f,.5f)};
                    foreach(var p in points)mesh.AddVert(center+p*unit,color,Vector2.zero);
                    foreach(var t in new[]{new[]{0,1,2},new[]{0,2,6},new[]{6,2,5},new[]{5,2,4},new[]{2,3,4}})mesh.AddTriangle(t[0],t[1],t[2]);break;
                case Symbol.PageDot:
                {
                    const int sides=64;
                    mesh.AddVert(center,Color.Lerp(color,Color.white,.13f),Vector2.zero);
                    for(int ring=0;ring<3;ring++)for(int i=0;i<sides;i++){
                        float angle=i*Mathf.PI*2/sides,dotRadius=ring==0?.83f:ring==1?.94f:1;
                        var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                          var warmShadow=new Color(color.r*.82f,color.g*.58f,color.b*.35f,color.a);
                          var shade=Color.Lerp(warmShadow,Color.Lerp(color,Color.white,.32f),(direction.y+1)*.5f);
                        if(ring==1)shade*=.72f;
                        shade.a=ring==2?0:color.a;
                        mesh.AddVert(center+direction*(unit*.5f*dotRadius),shade,Vector2.zero);
                    }
                    for(int i=0;i<sides;i++){
                        int next=(i+1)%sides;mesh.AddTriangle(0,1+i,1+next);
                        for(int ring=0;ring<2;ring++){
                            int a=1+ring*sides,b=a+sides;
                            mesh.AddTriangle(a+i,b+i,b+next);mesh.AddTriangle(a+i,b+next,a+next);
                        }
                    }
                    break;
                }
                case Symbol.ContactShadow:
                    mesh.AddVert(center,color,Vector2.zero);
                    for(int ring=0;ring<2;ring++)for(int i=0;i<48;i++){
                        float a=i*Mathf.PI*2/48,scale=ring==0?.48f:1;
                        var shade=color;shade.a*=ring==0?.38f:0;
                        mesh.AddVert(center+new Vector2(Mathf.Cos(a)*size.x,Mathf.Sin(a)*size.y)*(.5f*scale),shade,Vector2.zero);
                    }
                    for(int i=0;i<48;i++){
                        int next=(i+1)%48;mesh.AddTriangle(0,1+i,1+next);
                        mesh.AddTriangle(1+i,49+i,49+next);mesh.AddTriangle(1+i,49+next,1+next);
                    }
                    break;
                case Symbol.Capsule:
                    int origin=mesh.currentVertCount;mesh.AddVert(center,color,Vector2.zero);
                    float radius=size.y*.5f,half=Mathf.Max(0,(size.x-size.y)*.5f);
                    for(int i=0;i<40;i++){
                        float a=i*Mathf.PI*2/40,c=Mathf.Cos(a);
                        mesh.AddVert(center+new Vector2(c*radius+(c>=0?half:-half),Mathf.Sin(a)*radius),color,Vector2.zero);
                    }
                    for(int i=0;i<40;i++)mesh.AddTriangle(origin,origin+1+i,origin+1+(i+1)%40);
                    break;
                case Symbol.Plus:line(new Vector2(-.38f,0),new Vector2(.38f,0),.23f);line(new Vector2(0,-.38f),new Vector2(0,.38f),.23f);break;
                case Symbol.Close:line(new Vector2(-.30f,-.30f),new Vector2(.30f,.30f),.24f);line(new Vector2(-.30f,.30f),new Vector2(.30f,-.30f),.24f);break;
                case Symbol.Check:line(new Vector2(-.35f,0),new Vector2(-.08f,-.25f),.20f);line(new Vector2(-.08f,-.25f),new Vector2(.37f,.29f),.20f);break;
                case Symbol.BackArrow:
                    line(new Vector2(-.28f,0),new Vector2(.36f,0),.24f);
                    line(new Vector2(-.30f,0),new Vector2(.02f,.32f),.24f);
                    line(new Vector2(-.30f,0),new Vector2(.02f,-.32f),.24f);break;
                case Symbol.Clock:
                    for(int i=0;i<32;i++){
                        float a=i*Mathf.PI*2/32,b=(i+1)*Mathf.PI*2/32;
                        line(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.33f,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*.33f,.10f);
                    }
                    line(Vector2.zero,new Vector2(0,.20f),.08f);line(Vector2.zero,new Vector2(.17f,-.08f),.08f);
                    line(new Vector2(-.12f,.46f),new Vector2(.12f,.46f),.10f);break;
                case Symbol.Video:
                    var dark=new Color(.20f,.06f,.26f,color.a);
                    mesh.AddVert(center+new Vector2(-.48f,-.42f)*unit,color,Vector2.zero);
                    mesh.AddVert(center+new Vector2(-.48f,.42f)*unit,color,Vector2.zero);
                    mesh.AddVert(center+new Vector2(.48f,.42f)*unit,color,Vector2.zero);
                    mesh.AddVert(center+new Vector2(.48f,-.42f)*unit,color,Vector2.zero);
                    mesh.AddTriangle(0,1,2);mesh.AddTriangle(0,2,3);
                    mesh.AddVert(center+new Vector2(-.17f,-.26f)*unit,dark,Vector2.zero);
                    mesh.AddVert(center+new Vector2(-.17f,.26f)*unit,dark,Vector2.zero);
                    mesh.AddVert(center+new Vector2(.26f,0)*unit,dark,Vector2.zero);
                    mesh.AddTriangle(4,5,6);break;
            }
        }
    }
}
