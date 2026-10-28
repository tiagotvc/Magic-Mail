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
        static Texture2D header,footerPaint;
        static readonly Rect FooterPaintUV=new Rect(0,229f/724,1,269f/724);
        static readonly Rect HeaderUV=new Rect(84f/2172,204f/724,2001f/2172,347f/724);
        static readonly Rect PortraitUV=new Rect(928f/2172,232f/724,263f/2172,271f/724);
        public static Rect PortraitPlacement=>new Rect((928f-84)/2001,(520f-492)/347,263f/2001,271f/347);
        int selected;
        bool configured;
        RectTransform selectedPlate,selectedMarker;
        public int SelectedIndex {get=>selected;set{var next=Mathf.Clamp(value,0,4);if(next==selected)return;selected=next;UpdateSelection();SetVerticesDirty();}}
        public override Texture mainTexture
        {
            get
            {
                if(surface==Surface.Navigation){if(footerPaint==null)footerPaint=Resources.Load<Texture2D>("MapHudV57/navigation-plum-royal-cartoon");return footerPaint!=null?footerPaint:s_WhiteTexture;}
                if(header==null)header=Resources.Load<Texture2D>("MapHudV57/header");return header!=null?header:s_WhiteTexture;
            }
        }
        protected override void OnEnable()
        {
            base.OnEnable();ApplyMaterial();
        }
        void ApplyMaterial()
        {
            if(surface==Surface.Navigation){material=null;return;}
            var theme=WorldMapHudTheme.Current;if(theme==null)return;
            material=theme.artworkMaterial;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            if(surface==Surface.Portrait){Quad(vh,r,PortraitUV);return;}
            if(surface==Surface.Navigation){Navigation(vh,r);return;}
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
            if(configured)return;configured=true;material=null;SetAllDirty();
            RectTransform Layer(string name,Color color)
            {
                var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.layer=gameObject.layer;go.transform.SetParent(transform,false);
                var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return (RectTransform)go.transform;
            }
            selectedPlate=Layer("ActiveTabTint",new Color(1,1,1,.065f));
            selectedMarker=Layer("ActiveTabUnderline",new Color32(255,244,203,255));
            UpdateSelection();
        }
        void UpdateSelection()
        {
            if(selectedPlate!=null){selectedPlate.anchorMin=new Vector2(selected/5f,0);selectedPlate.anchorMax=new Vector2((selected+1)/5f,.89f);selectedPlate.offsetMin=new Vector2(1,4);selectedPlate.offsetMax=new Vector2(-1,0);}
            if(selectedMarker!=null){selectedMarker.anchorMin=new Vector2(selected/5f+.025f,0);selectedMarker.anchorMax=new Vector2((selected+1)/5f-.025f,0);selectedMarker.sizeDelta=new Vector2(0,4);selectedMarker.anchoredPosition=new Vector2(0,8);}
        }
        static Color C(byte r,byte g,byte b,byte a=255)=>new Color32(r,g,b,a);
        void Navigation(VertexHelper vh,Rect r)
        {
            if(footerPaint==null)footerPaint=Resources.Load<Texture2D>("MapHudV57/navigation-plum-royal-cartoon");
            if(footerPaint!=null){Quad(vh,r,FooterPaintUV);return;}
            void Fill(float left,float bottom,float right,float top,Color low,Color high)
            {
                int n=vh.currentVertCount;
                vh.AddVert(new Vector2(left,bottom),low,Vector2.zero);vh.AddVert(new Vector2(left,top),high,Vector2.zero);
                vh.AddVert(new Vector2(right,top),high,Vector2.zero);vh.AddVert(new Vector2(right,bottom),low,Vector2.zero);
                vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
            Fill(r.xMin,r.yMin,r.xMax,r.yMax,C(162,82,157),C(192,116,184));
            var left=r.xMin+r.width*selected/5;
            Fill(left,r.yMin+2,left+r.width/5,r.yMax,C(205,140,198),C(229,179,222));
            Fill(r.xMin,r.yMax-2,r.xMax,r.yMax,C(255,235,255,45),C(255,235,255,90));
            Fill(r.xMin,r.yMin,r.xMax,r.yMin+3,C(104,45,108),C(152,76,149));
            for(int i=1;i<5;i++)
            {
                var x=r.xMin+r.width*i/5;
                Fill(x-.9f,r.yMin+3,x+.4f,r.yMax-2,C(111,44,117,135),C(134,59,134,105));
                Fill(x+.4f,r.yMin+3,x+1.5f,r.yMax-2,C(245,195,237,70),C(255,224,251,100));
            }
        }
    }
}
