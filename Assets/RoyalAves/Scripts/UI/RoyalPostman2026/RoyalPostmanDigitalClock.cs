using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>Editable seven-segment time, rendered natively without baking changing numbers into art.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalPostmanDigitalClock : MaskableGraphic
    {
        [SerializeField] string value="01:00:00";
        public string Value { get=>value; set{if(this.value==value)return;this.value=value;SetVerticesDirty();} }
        static readonly int[] Bits={63,6,91,79,102,109,125,7,127,111};
        void Quad(VertexHelper vh,float x,float y,float w,float h,Color c)
        {
            int start=vh.currentVertCount;
            vh.AddVert(new Vector2(x,y),c,Vector2.zero);vh.AddVert(new Vector2(x,y+h),c,Vector2.zero);
            vh.AddVert(new Vector2(x+w,y+h),c,Vector2.zero);vh.AddVert(new Vector2(x+w,y),c,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
        void Segment(VertexHelper vh,float x,float y,float w,float h,Color c)
        {
            float cut=Mathf.Min(w,h)*.42f;var points=w>h?
                new[]{new Vector2(x,y+h*.5f),new Vector2(x+cut,y+h),new Vector2(x+w-cut,y+h),new Vector2(x+w,y+h*.5f),new Vector2(x+w-cut,y),new Vector2(x+cut,y)}:
                new[]{new Vector2(x+w*.5f,y),new Vector2(x,y+cut),new Vector2(x,y+h-cut),new Vector2(x+w*.5f,y+h),new Vector2(x+w,y+h-cut),new Vector2(x+w,y+cut)};
            int first=vh.currentVertCount;foreach(var p in points)vh.AddVert(p,c,Vector2.zero);for(int i=1;i<5;i++)vh.AddTriangle(first,first+i,first+i+1);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(string.IsNullOrEmpty(value))return;
            float units=0;foreach(char ch in value)units+=ch==':'?.35f:.82f;
            var r=rectTransform.rect;float scale=Mathf.Min(r.height,r.width/units),left=r.center.x-units*scale*.5f,bottom=r.center.y-scale*.5f;
            foreach(char ch in value)
            {
                if(ch==':'){Quad(vh,left+.10f*scale,bottom+.28f*scale,.10f*scale,.10f*scale,color);Quad(vh,left+.10f*scale,bottom+.66f*scale,.10f*scale,.10f*scale,color);left+=.35f*scale;continue;}
                int bits=ch>='0'&&ch<='9'?Bits[ch-'0']:ch=='-'?64:0;
                for(int i=0;i<7;i++)
                {
                    bool on=(bits&(1<<i))!=0;var c=color;if(!on)c.a*=.035f;
                    float x=0,y=0,w=0,h=0;
                    if(i==0||i==3||i==6){x=.12f;y=i==0?.88f:i==3?0:.44f;w=.46f;h=.10f;}
                    else{x=(i==1||i==2)?.59f:.02f;y=(i==1||i==5)?.51f:.07f;w=.10f;h=.36f;}
                    Segment(vh,left+x*scale,bottom+y*scale,w*scale,h*scale,c);
                }
                left+=.82f*scale;
            }
        }
    }
}
