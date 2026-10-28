using UnityEngine;
using UnityEngine.UI;
using RoyalAves.Meta.WorldMap;

namespace RoyalAves.Meta
{
    // Small UI geometry extends the approved enamel/gold chrome; illustrations remain separate sprites.
    public sealed class PostalEventGraphic : MaskableGraphic
    {
        public enum Style{Background,Card,Hero,GreenButton,PurpleButton,Track,Fill,Tile,ComingSoonBackground,Spotlight,ContactShadow}
        public Style style;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            Color C(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}
            Rect Inset(float n)=>new Rect(r.x+n,r.y+n,r.width-n*2,r.height-n*2);
            void Draw(Rect rect,float radius,Color top,Color bottom)=>WorldMapPositionFrame.Rounded(vh,rect,radius,top*color,bottom*color);
            if(style==Style.ComingSoonBackground){Draw(r,0,C("#543381"),C("#26153E"));return;}
            if(style==Style.Spotlight||style==Style.ContactShadow)
            {
                var tint=style==Style.Spotlight?new Color(.75f,.53f,1,.19f):new Color(.08f,.025f,.15f,.38f);
                const int rings=8,segments=48;
                for(int ring=0;ring<=rings;ring++)
                {
                    float ringRadius=ring/(float)rings;var shade=tint*color;shade.a*=Mathf.Pow(1-ringRadius*ringRadius,2);
                    for(int i=0;i<=segments;i++)
                    {
                        float angle=i*Mathf.PI*2/segments;
                        vh.AddVert(r.center+new Vector2(Mathf.Cos(angle)*r.width*.5f,Mathf.Sin(angle)*r.height*.5f)*ringRadius,shade,Vector2.zero);
                        if(ring==0||i==0)continue;
                        int a=(ring-1)*(segments+1)+i-1,b=a+segments+1;
                        vh.AddTriangle(a,a+1,b+1);vh.AddTriangle(a,b+1,b);
                    }
                }
                return;
            }
            if(style==Style.Background){Draw(r,0,C("#622B88"),C("#28113F"));return;}
            if(style==Style.Track){Draw(r,r.height/2,C("#4D235F"),C("#AE75B2"));Draw(Inset(3),r.height/2,C("#3B194F"),C("#753C87"));return;}
            if(style==Style.Fill){Draw(r,r.height/2,C("#FFF4A2"),C("#F0A524"));Draw(new Rect(r.x+5,r.y+r.height*.62f,Mathf.Max(0,r.width-10),r.height*.25f),r.height/4,new Color(1,1,1,.7f),new Color(1,1,1,.05f));return;}
            if(style==Style.Tile){Draw(r,20,C("#F9EBD9"),C("#E4C5DD"));return;}
            bool button=style==Style.GreenButton||style==Style.PurpleButton;float radius=button?r.height*.38f:32;
            Draw(r,radius,C("#763B97"),C("#301139"));Draw(Inset(2),radius-2,C("#FFFADE"),C("#CD7414"));Draw(Inset(5),radius-5,C("#FFE783"),C("#F5B328"));
            if(style==Style.Hero)Draw(Inset(10),radius-10,C("#8732B8"),C("#42134F"));
            else if(style==Style.GreenButton)Draw(Inset(9),radius-9,C("#37D55B"),C("#007F40"));
            else if(style==Style.PurpleButton)Draw(Inset(9),radius-9,C("#AF49F0"),C("#6616A4"));
            else Draw(Inset(10),radius-10,C("#FFF5DE"),C("#FFE2AF"));
            if(button||style==Style.Hero)Draw(new Rect(r.x+20,r.yMax-20,r.width*.56f,5),2.5f,new Color(1,1,1,.7f),new Color(1,1,1,.08f));
        }
    }
}
