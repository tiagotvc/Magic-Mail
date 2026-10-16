using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta.WorldMap
{
    // The approved artwork supplies the shape; live text, portrait and input remain separate.
    public sealed class WorldMapPostalGraphic : MaskableGraphic
    {
        public enum Surface { Header, Counter, Settings, Navigation, Ribbon, Portrait }
        Surface kind;
        public Surface surface {get=>kind;set{kind=value;ApplyMaterial();SetAllDirty();}}
        static Texture2D header,footer;
        static readonly Rect HeaderUV=new Rect(84f/2172,204f/724,2001f/2172,347f/724);
        static readonly Rect FooterUV=new Rect(1f/2022,209f/778,2020f/2022,309f/778);
        static readonly Rect PortraitUV=new Rect(928f/2172,232f/724,263f/2172,271f/724);
        public static Rect PortraitPlacement=>new Rect((928f-84)/2001,(520f-492)/347,263f/2001,271f/347);
        int selected;
        bool configured;
        public int SelectedIndex {get=>selected;set{selected=Mathf.Clamp(value,0,4);}}
        public override Texture mainTexture
        {
            get
            {
                if(surface==Surface.Navigation){if(footer==null)footer=Resources.Load<Texture2D>("MapHudV57/navigation");return footer!=null?footer:s_WhiteTexture;}
                if(header==null)header=Resources.Load<Texture2D>("MapHudV57/header");return header!=null?header:s_WhiteTexture;
            }
        }
        protected override void OnEnable()
        {
            base.OnEnable();ApplyMaterial();
        }
        void ApplyMaterial()
        {
            var theme=WorldMapHudTheme.Current;if(theme==null)return;
            material=surface==Surface.Navigation&&theme.footerMaterial!=null?theme.footerMaterial:theme.artworkMaterial;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            if(surface==Surface.Portrait){Quad(vh,r,PortraitUV);return;}
            if(surface==Surface.Navigation){Quad(vh,r,FooterUV);return;}
            // Omit the inner portrait rectangle from the header mesh. The independent portrait
            // layer samples the same source, so there is no second face under a replacement.
            var p=PortraitPlacement;float[] xs={0,p.xMin,p.xMax,1},ys={0,p.yMin,p.yMax,1};
            for(int y=0;y<3;y++)for(int x=0;x<3;x++)
            {
                if(x==1&&y==1)continue;
                var local=new Rect(r.xMin+xs[x]*r.width,r.yMin+ys[y]*r.height,(xs[x+1]-xs[x])*r.width,(ys[y+1]-ys[y])*r.height);
                var uv=new Rect(HeaderUV.x+xs[x]*HeaderUV.width,HeaderUV.y+ys[y]*HeaderUV.height,(xs[x+1]-xs[x])*HeaderUV.width,(ys[y+1]-ys[y])*HeaderUV.height);
                Quad(vh,local,uv);
            }
        }
        static void Quad(VertexHelper vh,Rect r,Rect uv)
        {
            int k=vh.currentVertCount;
            vh.AddVert(r.min,Color.white,new Vector2(uv.xMin,uv.yMin));
            vh.AddVert(new Vector2(r.xMin,r.yMax),Color.white,new Vector2(uv.xMin,uv.yMax));
            vh.AddVert(r.max,Color.white,new Vector2(uv.xMax,uv.yMax));
            vh.AddVert(new Vector2(r.xMax,r.yMin),Color.white,new Vector2(uv.xMax,uv.yMin));
            vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
        }
        public void ConfigureNavigation()
        {
            if(configured)return;configured=true;
            for(int i=1;i<5;i++)
            {
                var go=new GameObject("TabSeam"+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.layer=gameObject.layer;go.transform.SetParent(transform,false);
                var r=(RectTransform)go.transform;r.anchorMin=new Vector2(i/5f,.22f);r.anchorMax=new Vector2(i/5f,.80f);r.sizeDelta=new Vector2(1.2f,0);r.anchoredPosition=Vector2.zero;
                var image=go.GetComponent<Image>();image.color=new Color(.67f,.36f,.15f,.19f);image.raycastTarget=false;
            }
        }
    }
}
