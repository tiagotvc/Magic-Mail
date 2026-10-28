using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>Two expanding waves of stars, drawn in the rotating rays' own coordinate space.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalPostmanRadialSparkles : MaskableGraphic
    {
        public Sprite sprite;
        public float radius=520,period=1.7f;
        public Vector2 spread=Vector2.one;
        public bool ambient=true;
        float elapsed,intensity;
        public override Texture mainTexture=>sprite!=null?sprite.texture:Texture2D.whiteTexture;
        public void Sample(float time,float strength){elapsed=time;intensity=strength;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(sprite==null||intensity<=0)return;var uv=DataUtility.GetOuterUV(sprite);var hot=new Vector2((uv.x+uv.z)*.5f,(uv.y+uv.w)*.5f);
            for(int wave=0;wave<3;wave++)for(int i=0;i<12;i++)
            {
                float phase=Mathf.Repeat(elapsed/Mathf.Max(.1f,period)+wave/3f,1),angle=i*Mathf.PI/6;
                float distance=Mathf.Lerp(65,radius*(i%3==0?.90f:1),1-(1-phase)*(1-phase));var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));var center=Vector2.Scale(direction*distance,spread);
                float life=Mathf.Sin(phase*Mathf.PI),size=(22+(i%3)*8)*( .65f+.65f*life),alpha=life*intensity;var c=Color.Lerp(color,Color.white,.85f);c.a=alpha;
                // Soft halos and white star centers share one draw, using the existing star texture.
                int first=vh.currentVertCount;var halo=color;halo.a=alpha*.34f;vh.AddVert(center,halo,hot);var clear=halo;clear.a=0;
                for(int p=0;p<12;p++){float a=p*Mathf.PI/6;vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*size*1.4f,clear,hot);}
                for(int p=0;p<12;p++)vh.AddTriangle(first,first+1+p,first+1+(p+1)%12);
                int n=vh.currentVertCount;float turn=elapsed*(i%2==0?1:-1)*.55f;var x=new Vector2(Mathf.Cos(turn),Mathf.Sin(turn))*size*.5f;var y=new Vector2(-x.y,x.x);
                vh.AddVert(center-x-y,c,new Vector2(uv.x,uv.y));vh.AddVert(center-x+y,c,new Vector2(uv.x,uv.w));vh.AddVert(center+x+y,c,new Vector2(uv.z,uv.w));vh.AddVert(center+x-y,c,new Vector2(uv.z,uv.y));vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
                // Short outward trails are emitted with each pulse, independently of the pet image.
                if(wave==0){int trail=vh.currentVertCount;var side=new Vector2(-direction.y,direction.x)*2.8f;var tail=center-Vector2.Scale(direction*(25+35*life),spread);var faint=c;faint.a=0;vh.AddVert(tail-side,faint,hot);vh.AddVert(tail+side,faint,hot);vh.AddVert(center+side,c,hot);vh.AddVert(center-side,c,hot);vh.AddTriangle(trail,trail+1,trail+2);vh.AddTriangle(trail,trail+2,trail+3);}
            }
            if(ambient)for(int i=0;i<42;i++)
            {
                float phase=Mathf.Repeat(elapsed*.22f+i*.618034f,1),angle=i*2.399963f+elapsed*.035f;
                var center=Vector2.Scale(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(radius*(.2f+.76f*phase)),spread);
                float size=(i%4==0?12:4)+4*Mathf.Sin(phase*Mathf.PI),alpha=Mathf.Sin(phase*Mathf.PI)*intensity*.85f;var c=Color.Lerp(color,Color.white,.7f);c.a=alpha;int n=vh.currentVertCount;
                vh.AddVert(center+new Vector2(-size,-size),c,new Vector2(uv.x,uv.y));vh.AddVert(center+new Vector2(-size,size),c,new Vector2(uv.x,uv.w));vh.AddVert(center+new Vector2(size,size),c,new Vector2(uv.z,uv.w));vh.AddVert(center+new Vector2(size,-size),c,new Vector2(uv.z,uv.y));vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
