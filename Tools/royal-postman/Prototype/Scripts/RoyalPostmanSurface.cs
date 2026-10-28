using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    // Native UI geometry: the rim and moving face are different Graphics, never one baked button.
    public sealed class RoyalPostmanSurface : MaskableGraphic
    {
        public enum Finish { Backdrop, Capsule, Blue, Gold, Green, Red, Gear, Arrow, ButtonRim, ButtonFace, ButtonWell, Shadow, Navigation, Selection }
        public Finish finish;
        public float radius = 24;
        static Color C(uint n) => new Color32((byte)(n >> 16), (byte)(n >> 8), (byte)n, 255);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect;
            switch (finish)
            {
                case Finish.Backdrop:
                    // Bright quiet backdrop supports the cast, with no map or construction scenery.
                    Rect(vh,r,C(0xAADCD5),C(0x158AC5));
                    Halo(vh,new Vector2(r.center.x,r.yMin+r.height*.56f),new Vector2(r.width*.82f,r.height*.46f),new Color(.88f,1,.89f,.70f));
                    Halo(vh,new Vector2(r.xMin+r.width*.02f,r.yMin+r.height*.80f),new Vector2(r.width*.66f,r.height*.10f),new Color(.90f,1,1,.20f));
                    Halo(vh,new Vector2(r.xMax,r.yMin+r.height*.85f),new Vector2(r.width*.7f,r.height*.10f),new Color(.90f,1,1,.16f));
                    Halo(vh,new Vector2(r.center.x,r.yMin+r.height*.14f),new Vector2(r.width*.9f,r.height*.19f),new Color(1,.92f,.73f,.84f));
                    break;
                case Finish.Capsule:
                    Round(vh,r,radius,C(0x527E85),C(0x80A7AB));
                    Round(vh,Inset(r,3),radius-3,C(0xB9AA83),C(0xFFF9DE));
                    Round(vh,Inset(r,7),radius-7,C(0xE8D6AB),C(0xFFF5D5));break;
                case Finish.Blue:
                    Round(vh,r,radius,C(0x3C2057),C(0x6F4B87));
                    Round(vh,Inset(r,4),radius-4,C(0x7B438F),C(0xC287CB));
                    Round(vh,Inset(r,9),radius-9,C(0x63377F),C(0x9653A4));break;
                case Finish.Gold:
                    Round(vh,r,radius,C(0xB76412),C(0xDF9828));
                    Round(vh,Inset(r,3),radius-3,C(0xF9A715),C(0xFFF39B));
                    Round(vh,Inset(r,9),radius-9,C(0xF7A40B),C(0xFFDF45));break;
                case Finish.Green:
                    Round(vh,r,radius,C(0x127117),C(0x296C24));
                    Round(vh,Inset(r,3),radius-3,C(0x3AAB09),C(0xC3FC61));
                    Round(vh,Inset(r,7),radius-7,C(0x229F03),C(0x76D80E));break;
                case Finish.Red:
                    Round(vh,r,radius,C(0x8D253E),C(0xFFE2AA));
                    Round(vh,Inset(r,3),radius-3,C(0xB92E3D),C(0xF66859));break;
                case Finish.Gear:
                    Gear(vh,r,C(0xFFF3CD));break;
                case Finish.Arrow:
                    int a=vh.currentVertCount;
                    vh.AddVert(new Vector2(r.xMin,r.yMin),C(0xFFF3CD),Vector2.zero);
                    vh.AddVert(new Vector2(r.xMin,r.yMax),C(0xFFFBE0),Vector2.zero);
                    vh.AddVert(new Vector2(r.xMax,r.center.y),C(0xFFF3CD),Vector2.zero);vh.AddTriangle(a,a+1,a+2);break;
                case Finish.ButtonWell:
                    Seal(vh,r,1,C(0x084B4A),C(0x0C7168));break;
                case Finish.ButtonRim:
                    // Ring has an actual hole. The green well below it is an independent object.
                    Ring(vh,r,1,.974f,C(0xB26413),C(0xD79932));
                    Ring(vh,r,.974f,.94f,C(0xF39B13),C(0xFFF1A0));
                    Ring(vh,r,.94f,.875f,C(0xFFA90B),C(0xFFDB3C));
                    Ring(vh,r,.875f,.85f,C(0xC87913),C(0xFFF6AA));break;
                case Finish.ButtonFace:
                    Seal(vh,r,1,C(0x087F70),C(0x91E5AE));
                    Seal(vh,Inset(r,4),1,C(0x068977),C(0x35C6A8));
                    // Broad enamel highlight; no gemstone facets or glitter.
                    var cap = new Rect(r.xMin+r.width*.13f,r.yMin+r.height*.60f,r.width*.74f,r.height*.25f);
                    Round(vh,cap,cap.height*.45f,new Color(.70f,1,.87f,.02f),new Color(.85f,1,.90f,.27f));break;
                case Finish.Shadow:
                    Halo(vh,r.center,r.size*.5f,new Color(.03f,.24f,.31f,.23f));break;
                case Finish.Navigation:
                    Rect(vh,r,C(0xA32E42),C(0xDC514B));
                    Rect(vh,new Rect(r.xMin,r.yMax-14,r.width,14),C(0xD77910),C(0xFFF09A));
                    Rect(vh,new Rect(r.xMin,r.yMax-10,r.width,6),C(0xFFAD11),C(0xFFDC42));
                    for(int i=1;i<5;i++)
                    {
                        float x=r.xMin+r.width*i/5;
                        Rect(vh,new Rect(x-1.5f,r.yMin+18,2,r.height-37),new Color(.29f,.07f,.23f,.22f),new Color(.29f,.07f,.23f,.38f));
                        Rect(vh,new Rect(x+.5f,r.yMin+18,1.5f,r.height-37),new Color(1,.85f,.73f,.12f),new Color(1,.85f,.73f,.25f));
                    }
                    break;
                case Finish.Selection:
                    Rect(vh,r,new Color(.91f,.36f,.37f,.18f),new Color(1,.61f,.52f,.65f));break;
            }
        }

        static Rect Inset(Rect r,float n)=>new Rect(r.x+n,r.y+n,r.width-2*n,r.height-2*n);
        static void Gear(VertexHelper vh,Rect r,Color color)
        {
            const int n=80;int k=vh.currentVertCount;
            for(int i=0;i<=n;i++)
            {
                float a=i/(float)n*Mathf.PI*2;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                float outer=(i%10>=2&&i%10<=7)?.81f:1;
                vh.AddVert(r.center+Vector2.Scale(d,r.size*.5f)*outer,color,Vector2.zero);
                vh.AddVert(r.center+Vector2.Scale(d,r.size*.5f)*.37f,color,Vector2.zero);
                if(i>0){int j=k+i*2;vh.AddTriangle(j-2,j,j+1);vh.AddTriangle(j-2,j+1,j-1);}
            }
        }
        static void Rect(VertexHelper vh,Rect r,Color low,Color high)
        {
            int k=vh.currentVertCount;
            vh.AddVert(new Vector2(r.xMin,r.yMin),low,Vector2.zero);vh.AddVert(new Vector2(r.xMin,r.yMax),high,Vector2.zero);
            vh.AddVert(new Vector2(r.xMax,r.yMax),high,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMin),low,Vector2.zero);
            vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
        }
        static void Round(VertexHelper vh,Rect r,float radius,Color low,Color high)
        {
            radius=Mathf.Clamp(radius,0,Mathf.Min(r.width,r.height)*.5f);
            int k=vh.currentVertCount;
            vh.AddVert(r.center,Color.Lerp(low,high,.5f),Vector2.zero);
            const int count=64;
            for(int i=0;i<=count;i++)
            {
                float a=i/(float)count*Mathf.PI*2;
                var corner=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                var p=r.center+new Vector2(Mathf.Sign(corner.x)*(r.width*.5f-radius),Mathf.Sign(corner.y)*(r.height*.5f-radius))+corner*radius;
                vh.AddVert(p,Color.Lerp(low,high,Mathf.InverseLerp(r.yMin,r.yMax,p.y)),Vector2.zero);
                if(i>0)vh.AddTriangle(k,k+i,k+i+1);
            }
        }
        static void Halo(VertexHelper vh,Vector2 center,Vector2 axes,Color tint)
        {
            int k=vh.currentVertCount;vh.AddVert(center,tint,Vector2.zero);
            const int count=72;
            for(int ring=1;ring<=8;ring++)
            {
                float t=ring/8f;var c=tint;c.a*=Mathf.Pow(1-t*t,2);
                for(int i=0;i<count;i++)
                {
                    float a=i/(float)count*Mathf.PI*2;vh.AddVert(center+new Vector2(Mathf.Cos(a)*axes.x,Mathf.Sin(a)*axes.y)*t,c,Vector2.zero);
                    int cur=k+1+(ring-1)*count+i,next=k+1+(ring-1)*count+(i+1)%count;
                    if(ring==1)vh.AddTriangle(k,cur,next);
                    else {int prev=cur-count,prevNext=next-count;vh.AddTriangle(prev,cur,next);vh.AddTriangle(prev,next,prevNext);}
                }
            }
        }
        // Smooth elongated chamfered silhouette, slightly bowed across the upper/lower edges.
        static Vector2 Outline(int i,int count)
        {
            float u=i/(float)count*10;int segment=Mathf.Min(9,(int)u);float t=u-segment;
            Vector2 p,a,b,q;
            switch(segment)
            {
                case 0:p=new Vector2(-.5f,0);a=new Vector2(-.49f,.15f);b=new Vector2(-.44f,.34f);q=new Vector2(-.40f,.41f);break;
                case 1:p=new Vector2(-.40f,.41f);a=new Vector2(-.38f,.46f);b=new Vector2(-.33f,.50f);q=new Vector2(-.28f,.50f);break;
                case 2:p=new Vector2(-.28f,.50f);a=new Vector2(-.12f,.44f);b=new Vector2(.12f,.44f);q=new Vector2(.28f,.50f);break;
                case 3:p=new Vector2(.28f,.50f);a=new Vector2(.33f,.50f);b=new Vector2(.38f,.46f);q=new Vector2(.40f,.41f);break;
                case 4:p=new Vector2(.40f,.41f);a=new Vector2(.44f,.34f);b=new Vector2(.49f,.15f);q=new Vector2(.5f,0);break;
                case 5:p=new Vector2(.5f,0);a=new Vector2(.49f,-.15f);b=new Vector2(.44f,-.34f);q=new Vector2(.40f,-.41f);break;
                case 6:p=new Vector2(.40f,-.41f);a=new Vector2(.38f,-.46f);b=new Vector2(.33f,-.50f);q=new Vector2(.28f,-.50f);break;
                case 7:p=new Vector2(.28f,-.50f);a=new Vector2(.12f,-.44f);b=new Vector2(-.12f,-.44f);q=new Vector2(-.28f,-.50f);break;
                case 8:p=new Vector2(-.28f,-.50f);a=new Vector2(-.33f,-.50f);b=new Vector2(-.38f,-.46f);q=new Vector2(-.40f,-.41f);break;
                default:p=new Vector2(-.40f,-.41f);a=new Vector2(-.44f,-.34f);b=new Vector2(-.49f,-.15f);q=new Vector2(-.5f,0);break;
            }
            float v=1-t;return (v*v*v*p+3*v*v*t*a+3*v*t*t*b+t*t*t*q)*2;
        }
        static Vector2 Point(Rect r,int i,int n,float scale)
        {var p=Outline(i,n);return r.center+Vector2.Scale(p,r.size*.5f)*scale;}
        static void Seal(VertexHelper vh,Rect r,float scale,Color low,Color high)
        {
            int k=vh.currentVertCount;vh.AddVert(r.center,Color.Lerp(low,high,.5f),Vector2.zero);const int n=128;
            for(int i=0;i<=n;i++){var p=Point(r,i,n,scale);vh.AddVert(p,Color.Lerp(low,high,Mathf.InverseLerp(r.yMin,r.yMax,p.y)),Vector2.zero);if(i>0)vh.AddTriangle(k,k+i,k+i+1);}
        }
        static void Ring(VertexHelper vh,Rect r,float outer,float inner,Color low,Color high)
        {
            int k=vh.currentVertCount;const int n=128;
            for(int i=0;i<=n;i++)
            {
                var p=Point(r,i,n,outer);var q=Point(r,i,n,inner);
                vh.AddVert(p,Color.Lerp(low,high,Mathf.InverseLerp(r.yMin,r.yMax,p.y)),Vector2.zero);
                vh.AddVert(q,Color.Lerp(low,high,Mathf.InverseLerp(r.yMin,r.yMax,q.y)),Vector2.zero);
                if(i>0){int j=k+i*2;vh.AddTriangle(j-2,j,j+1);vh.AddTriangle(j-2,j+1,j-1);}
            }
        }
    }
}
