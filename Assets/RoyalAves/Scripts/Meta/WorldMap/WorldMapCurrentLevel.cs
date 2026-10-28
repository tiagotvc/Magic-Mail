using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta.WorldMap
{
    // One screen-space marker for the current node. Artwork comes from the existing HUD/profile.
    public sealed class WorldMapCurrentLevel
    {
        readonly RectTransform focus,marker;
        readonly WorldMapCurrentFocusGraphic glow;
        readonly Image customPortrait;
        readonly WorldMapPostalGraphic defaultPortrait;
        readonly LobbyFeaturesConfig art;
        int avatar=-1;
        public bool Visible=>marker.gameObject.activeSelf;
        public RectTransform Marker=>marker;
        public RectTransform Focus=>focus;

        public WorldMapCurrentLevel(RectTransform viewport,LobbyFeaturesConfig art)
        {
            this.art=art;
            focus=Rect(viewport,"ActiveLevelFocus");glow=focus.gameObject.AddComponent<WorldMapCurrentFocusGraphic>();glow.raycastTarget=false;
            marker=Rect(viewport,"CurrentPositionMarker");
            marker.gameObject.AddComponent<WorldMapPositionFrame>().raycastTarget=false;
            var maskRect=Rect(marker,"RoundedPortraitMask");maskRect.anchorMin=new Vector2(.105f,.105f);maskRect.anchorMax=new Vector2(.895f,.895f);maskRect.offsetMin=maskRect.offsetMax=Vector2.zero;
            var maskGraphic=maskRect.gameObject.AddComponent<WorldMapPositionFrame>();maskGraphic.maskOnly=true;maskGraphic.raycastTarget=false;
            maskRect.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            var face=Rect(maskRect,"HudPortrait");Stretch(face);
            defaultPortrait=face.gameObject.AddComponent<WorldMapPostalGraphic>();defaultPortrait.surface=WorldMapPostalGraphic.Surface.Portrait;defaultPortrait.raycastTarget=false;
            var custom=Rect(maskRect,"ProfilePortrait");Stretch(custom);customPortrait=custom.gameObject.AddComponent<Image>();customPortrait.preserveAspect=true;customPortrait.raycastTarget=false;
            SetVisible(false);
        }
        static RectTransform Rect(Transform parent,string name)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer));go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=new Vector2(.5f,.5f);return r;
        }
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        void SetVisible(bool visible){if(focus.gameObject.activeSelf!=visible)focus.gameObject.SetActive(visible);if(marker.gameObject.activeSelf!=visible)marker.gameObject.SetActive(visible);}
        public void Tick(WorldMapView world,int level,float cursor,Vector2 size,bool dragging)
        {
            if(!world.ProjectLevelBounds(level,cursor,size.x/size.y,out var bounds)||bounds.center.y<.16f||bounds.center.y>.79f||bounds.width<.012f){SetVisible(false);return;}
            SetVisible(true);
            var center=Vector2.Scale(bounds.center,size);var tokenSize=Vector2.Scale(bounds.size,size);
            focus.anchoredPosition=center;focus.sizeDelta=new Vector2(tokenSize.x*1.64f,tokenSize.y*2.3f);
            glow.SetPhase(dragging?0:world.Clock);
            float card=Mathf.Clamp(tokenSize.x*1.08f,66,146);
            bool left=center.x>card+tokenSize.x*.6f+12;
            var position=center+new Vector2((left?-1:1)*(tokenSize.x*.5f+card*.5f+12),card*.09f);
            position.x=Mathf.Clamp(position.x,card*.5f+10,size.x-card*.5f-10);
            marker.sizeDelta=new Vector2(card,card*1.04f);marker.anchoredPosition=position;
            int selected=MetaProgress.Avatar;
            if(avatar==selected)return;avatar=selected;
            var sprite=selected>0&&art!=null&&selected<art.avatars.Count?art.avatars[selected]:null;
            customPortrait.sprite=sprite;customPortrait.gameObject.SetActive(sprite!=null);defaultPortrait.gameObject.SetActive(sprite==null);
        }
    }

    public sealed class WorldMapPositionFrame : MaskableGraphic
    {
        public bool maskOnly;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            if(maskOnly){Rounded(vh,r,r.width*.14f,Color.white,Color.white);return;}
            var shadow=r;shadow.position+=new Vector2(2,-5);Rounded(vh,shadow,r.width*.18f,new Color(.19f,.035f,.28f,.22f),new Color(.19f,.035f,.28f,.22f));
            Rounded(vh,r,r.width*.18f,new Color(1,.96f,.78f),new Color(.68f,.33f,.075f));
            r=Inset(r,2);Rounded(vh,r,r.width*.17f,new Color(1,.88f,.21f),new Color(1,.51f,.025f));
            r=Inset(r,3);Rounded(vh,r,r.width*.16f,new Color(1,1,.88f),new Color(.91f,.55f,.09f));
            r=Inset(r,2);Rounded(vh,r,r.width*.15f,new Color(.64f,.10f,.93f),new Color(.24f,.015f,.43f));
            var glint=new Rect(rectTransform.rect.xMin+rectTransform.rect.width*.16f,rectTransform.rect.yMax-6,rectTransform.rect.width*.37f,2.6f);
            Rounded(vh,glint,1.3f,new Color(1,1,1,.9f),new Color(1,1,.85f,.35f));
        }
        static Rect Inset(Rect r,float amount)=>new Rect(r.x+amount,r.y+amount,r.width-amount*2,r.height-amount*2);
        internal static void Rounded(VertexHelper vh,Rect r,float radius,Color top,Color bottom)
        {
            radius=Mathf.Min(radius,Mathf.Min(r.width,r.height)*.5f);int start=vh.currentVertCount;
            void Vertex(Vector2 p){vh.AddVert(p,Color.Lerp(bottom,top,Mathf.InverseLerp(r.yMin,r.yMax,p.y)),Vector2.zero);}
            Vertex(r.center);
            for(int corner=0;corner<4;corner++)
            {
                var center=new Vector2(corner==0||corner==3?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
                for(int j=0;j<=8;j++){float a=(corner*90+j*90f/8)*Mathf.Deg2Rad;Vertex(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius);}
            }
            const int count=36;for(int i=0;i<count;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%count);
        }
    }

    // A separate offscreen return control, anchored above the bottom navigation.
    // It shares the live profile/default HUD portrait with the on-node position marker.
    public sealed class WorldMapReturnToLevel
    {
        public const string FrameResourcePath="MapNavigationV99/return-pin";
        readonly RectTransform root;
        readonly Image customPortrait;
        readonly WorldMapPostalGraphic defaultPortrait;
        readonly LobbyFeaturesConfig art;
        int avatar=-1;
        public Button Button{get;}
        public WorldMapReturnToLevel(RectTransform parent,LobbyFeaturesConfig art,System.Action focus)
        {
            this.art=art;root=Rect(parent,"ReturnToCurrentLevel");
            root.anchorMin=root.anchorMax=new Vector2(.5f,0);root.pivot=new Vector2(.5f,0);
            root.sizeDelta=new Vector2(156,190);root.anchoredPosition=new Vector2(0,10);
            Button=root.gameObject.AddComponent<Button>();
            Button.navigation=new Navigation{mode=Navigation.Mode.None};
            var colors=Button.colors;colors.pressedColor=new Color(.82f,.74f,.94f);colors.highlightedColor=Color.white;Button.colors=colors;
            Button.onClick.AddListener(()=>focus?.Invoke());
            // Sized to the actual transparent aperture in the original portrait sprite.
            // The frame is drawn last so its inner bevel cleanly overlaps the portrait edge.
            var mask=Rect(root,"AvatarMask");mask.anchorMin=mask.anchorMax=new Vector2(.5f,.602f);mask.sizeDelta=Vector2.one*90;
            var circle=mask.gameObject.AddComponent<WorldMapReturnPinGraphic>();circle.maskOnly=true;circle.raycastTarget=false;
            mask.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            var face=Rect(mask,"HudPortrait");Stretch(face);
            defaultPortrait=face.gameObject.AddComponent<WorldMapPostalGraphic>();defaultPortrait.surface=WorldMapPostalGraphic.Surface.Portrait;defaultPortrait.raycastTarget=false;
            var custom=Rect(mask,"ProfilePortrait");Stretch(custom);customPortrait=custom.gameObject.AddComponent<Image>();customPortrait.preserveAspect=true;customPortrait.raycastTarget=false;
            var frame=Rect(root,"GlossyPinFrame");Stretch(frame);
            var graphic=frame.gameObject.AddComponent<Image>();graphic.sprite=Resources.Load<Sprite>(FrameResourcePath);graphic.preserveAspect=true;
            Button.targetGraphic=graphic;
            Refresh(false);
        }
        public void Refresh(bool visible)
        {
            if(root.gameObject.activeSelf!=visible)root.gameObject.SetActive(visible);
            if(!visible||avatar==MetaProgress.Avatar)return;avatar=MetaProgress.Avatar;
            var sprite=avatar>0&&art!=null&&avatar<art.avatars.Count?art.avatars[avatar]:null;
            customPortrait.sprite=sprite;customPortrait.gameObject.SetActive(sprite!=null);defaultPortrait.gameObject.SetActive(sprite==null);
        }
        static RectTransform Rect(Transform parent,string name)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer)){layer=parent.gameObject.layer};go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;return r;
        }
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    }

    // Native enamel/gold drop geometry, matching the existing position-frame icon system.
    public sealed class WorldMapReturnPinGraphic : MaskableGraphic
    {
        public bool maskOnly;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            if(maskOnly){Oval(vh,r,Color.white,Color.white);return;}
            Rect Inset(float n)=>new Rect(r.x+n,r.y+n,r.width-n*2,r.height-n*2);
            var shadow=r;shadow.position+=new Vector2(2,-5);Drop(vh,shadow,new Color(.15f,.025f,.23f,.28f),new Color(.15f,.025f,.23f,.28f));
            Drop(vh,Inset(1),new Color(1,.98f,.86f),new Color(.86f,.52f,.13f));
            Drop(vh,Inset(3),new Color(1,.88f,.20f),new Color(.86f,.40f,.025f));
            Drop(vh,Inset(7),new Color(1,1,.88f),new Color(1,.76f,.27f));
            Drop(vh,Inset(9),new Color(.76f,.10f,1),new Color(.30f,.015f,.58f));
            var face=new Rect(r.center.x-r.width*.342f,r.yMin+r.height*.635f-r.width*.342f,r.width*.684f,r.width*.684f);
            Oval(vh,face,new Color(1,.97f,.76f),new Color(1,.62f,.075f));
            var glint=new Rect(r.xMin+r.width*.20f,r.yMin+r.height*.895f,r.width*.25f,r.height*.035f);
            Oval(vh,glint,new Color(1,1,1,.85f),new Color(1,1,1,.14f));
            // A small chevron reinforces the destination below the portrait without a text label.
            var center=new Vector2(r.center.x,r.yMin+r.height*.17f);
            Stroke(vh,center+new Vector2(-9,5),center+new Vector2(0,-3),2.4f,new Color(1,.98f,.77f));
            Stroke(vh,center+new Vector2(0,-3),center+new Vector2(9,5),2.4f,new Color(1,.98f,.77f));
        }
        static void Drop(VertexHelper vh,Rect r,Color top,Color bottom)
        {
            int start=vh.currentVertCount;const int steps=16;
            void Vertex(Vector2 p){var point=new Vector2(r.center.x+p.x*r.width,r.yMin+p.y*r.height);vh.AddVert(point,Color.Lerp(bottom,top,p.y),Vector2.zero);}
            void Curve(Vector2 a,Vector2 b,Vector2 c,Vector2 d)
            {for(int i=0;i<steps;i++){float t=i/(float)steps,u=1-t;Vertex(u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d);}}
            Vertex(new Vector2(0,.58f));
            Curve(new Vector2(0,.02f),new Vector2(-.13f,.13f),new Vector2(-.46f,.33f),new Vector2(-.46f,.62f));
            Curve(new Vector2(-.46f,.62f),new Vector2(-.46f,.86f),new Vector2(-.25f,1),new Vector2(0,1));
            Curve(new Vector2(0,1),new Vector2(.25f,1),new Vector2(.46f,.86f),new Vector2(.46f,.62f));
            Curve(new Vector2(.46f,.62f),new Vector2(.46f,.33f),new Vector2(.13f,.13f),new Vector2(0,.02f));
            for(int i=0;i<steps*4;i++)vh.AddTriangle(start,start+i+1,start+(i+1)%(steps*4)+1);
        }
        static void Oval(VertexHelper vh,Rect r,Color top,Color bottom)
        {
            int start=vh.currentVertCount;const int steps=48;vh.AddVert(r.center,Color.Lerp(bottom,top,.5f),Vector2.zero);
            for(int i=0;i<steps;i++){float a=i*Mathf.PI*2/steps;var p=r.center+new Vector2(Mathf.Cos(a)*r.width*.5f,Mathf.Sin(a)*r.height*.5f);vh.AddVert(p,Color.Lerp(bottom,top,Mathf.InverseLerp(r.yMin,r.yMax,p.y)),Vector2.zero);}
            for(int i=0;i<steps;i++)vh.AddTriangle(start,start+i+1,start+(i+1)%steps+1);
        }
        static void Stroke(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color)
        {
            var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*width*.5f;int k=vh.currentVertCount;
            vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);
            vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
        }
    }

    // Procedural UI geometry: no particles, textures or per-frame object creation.
    public sealed class WorldMapCurrentFocusGraphic : MaskableGraphic
    {
        float phase=-1;
        public void SetPhase(float time){float value=Mathf.Floor(time*24)/24;if(value==phase)return;phase=value;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float pulse=Mathf.Sin(phase*2.5f)*.5f+.5f;
            var radius=new Vector2(r.width*.35f,r.height*.244f)*(1+pulse*.012f);
            Ring(vh,radius,11,new Color(1,.91f,.53f,.075f));
            Ring(vh,radius,5.5f,new Color(1,.97f,.76f,.68f+pulse*.14f));
            Ring(vh,radius,1.4f,new Color(1,1,.94f,.8f));
            for(int i=0;i<3;i++)
            {
                float x=(i-1)*r.width*.17f;float beat=.55f+.45f*Mathf.Sin(phase*1.4f+i*2);
                float height=r.height*(.26f+i%2*.10f);float y=radius.y*.88f;
                Beam(vh,x,y,height,2.2f,new Color(1,.98f,.73f,.08f+beat*.15f));
            }
            for(int i=0;i<5;i++)
            {
                float a=(i*72+19)*Mathf.Deg2Rad;var p=new Vector2(Mathf.Cos(a)*radius.x*1.06f,Mathf.Sin(a)*radius.y*1.05f);
                float bright=.35f+.65f*Mathf.Pow(Mathf.Sin(phase*2+i*1.7f)*.5f+.5f,2);
                Sparkle(vh,p,3.8f+bright*2.3f,new Color(1,1,.82f,bright));
            }
        }
        static void Ring(VertexHelper vh,Vector2 radius,float width,Color color)
        {
            int start=vh.currentVertCount;const int count=80;
            for(int i=0;i<=count;i++){float a=i*Mathf.PI*2/count;var dir=new Vector2(Mathf.Cos(a),Mathf.Sin(a));vh.AddVert(Vector2.Scale(dir,radius+Vector2.one*width*.5f),color,Vector2.zero);vh.AddVert(Vector2.Scale(dir,radius-Vector2.one*width*.5f),color,Vector2.zero);}
            for(int i=0;i<count;i++){int k=start+i*2;vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k+1,k+3,k+2);}
        }
        static void Beam(VertexHelper vh,float x,float y,float height,float width,Color color)
        {
            int start=vh.currentVertCount;var clear=color;clear.a=0;
            vh.AddVert(new Vector2(x-width,y),color,Vector2.zero);vh.AddVert(new Vector2(x+width,y),color,Vector2.zero);
            vh.AddVert(new Vector2(x+width*.4f,y+height),clear,Vector2.zero);vh.AddVert(new Vector2(x-width*.4f,y+height),clear,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
        static void Sparkle(VertexHelper vh,Vector2 p,float size,Color color)
        {
            int start=vh.currentVertCount;vh.AddVert(p,color,Vector2.zero);
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;vh.AddVert(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*size*(i%2==0?1:.22f),color,Vector2.zero);}
            for(int i=0;i<8;i++)vh.AddTriangle(start,start+i+1,start+(i+1)%8+1);
        }
    }
}
