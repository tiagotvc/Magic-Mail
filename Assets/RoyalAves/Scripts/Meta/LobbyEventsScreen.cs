using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.Meta.WorldMap;

namespace RoyalAves.Meta
{
    // The delivery-service harness still uses this contract; this view does not read progress.
    public interface IPostalEventsSource
    {
        DateTime Now{get;} int NextLevel{get;} int LevelsWon{get;} bool CanPlay{get;} int SeasonLevel{get;}
        PostalEventsConfig Config{get;} PostalEventState Progress{get;}
        bool IsMissionClaimed(string id);bool ClaimMission(LobbyFeaturesConfig.Mission mission);
        bool Join();bool ClaimDaily();bool ClaimSeason(int index);
    }

    public sealed class LobbyEventsScreen : MonoBehaviour
    {
        static readonly Color Ink=new Color32(72,31,83,255),Cream=new Color32(255,245,218,255);
        LobbyFeaturesConfig art;
        public IPostalEventsSource Source{get;private set;}
        public static LobbyEventsScreen Create(RectTransform parent,LobbyFeaturesConfig art,Action<int> play,Action close,Action changed,IPostalEventsSource source=null)
        {
            var root=Rect("EventsScreen",parent);Stretch(root);
            var screen=root.gameObject.AddComponent<LobbyEventsScreen>();
            screen.art=art;screen.Source=source;screen.Build();root.gameObject.SetActive(false);return screen;
        }
        void Build()
        {
            Graphic((RectTransform)transform,PostalEventGraphic.Style.ComingSoonBackground,true);
            var header=Rect("EventsHeader",transform);
            header.anchorMin=new Vector2(0,1);header.anchorMax=Vector2.one;header.pivot=new Vector2(.5f,1);header.sizeDelta=new Vector2(0,176);
            var chrome=header.gameObject.AddComponent<WorldMapPostalGraphic>();
            chrome.surface=WorldMapPostalGraphic.Surface.Navigation;chrome.raycastTarget=false;
            var heading=Label(header,"Events",80,Ink);
            Stretch(heading.rectTransform);heading.rectTransform.offsetMin=new Vector2(48,28);heading.rectTransform.offsetMax=new Vector2(-48,-20);
            var body=Rect("ComingSoon",transform);Stretch(body);body.offsetMax=new Vector2(0,-176);
            var scene=body.gameObject.AddComponent<PostalEventSceneImage>();
            scene.texture=Resources.Load<Texture2D>("EventsV92/scene");scene.raycastTarget=false;scene.Fit();
            PostalEventSurpriseMotion.Create(body,scene);
            var message=Label(body,"Eventos em breve",72,Cream);message.name="ComingSoonMessage";
            Area(message.rectTransform,new Vector2(.07f,.035f),new Vector2(.93f,.13f));
        }
        // Routing can refresh without rebuilding the screen or touching player saves.
        public void Refresh(){}
        TMP_Text Label(Transform parent,string copy,float size,Color color)
        {
            var text=Rect("Text",parent).gameObject.AddComponent<TextMeshProUGUI>();
            text.font=WorldMapHudTheme.Current?.font!=null?WorldMapHudTheme.Current.font:art!=null?art.displayFont:null;
            text.text=copy;text.fontSize=size;text.enableAutoSizing=true;text.fontSizeMin=size*.75f;text.fontSizeMax=size;
            text.alignment=TextAlignmentOptions.Center;text.color=color;text.raycastTarget=false;
            text.textWrappingMode=TextWrappingModes.Normal;if(text.font!=null)text.fontSharedMaterial=text.font.material;
            return text;
        }
        static PostalEventGraphic Graphic(RectTransform r,PostalEventGraphic.Style style,bool hit=false)
        {var g=r.gameObject.AddComponent<PostalEventGraphic>();g.style=style;g.raycastTarget=hit;return g;}
        static RectTransform Rect(string name,Transform parent)
        {var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer)){layer=parent.gameObject.layer};var r=(RectTransform)go.transform;r.SetParent(parent,false);return r;}
        static void Stretch(RectTransform r)=>Area(r,Vector2.zero,Vector2.one);
        static void Area(RectTransform r,Vector2 min,Vector2 max)
        {r.anchorMin=min;r.anchorMax=max;r.pivot=new Vector2(.5f,.5f);r.offsetMin=r.offsetMax=Vector2.zero;}
    }

    // Keep the entire cast width and the top crest. Tall phones extend the quiet floor
    // using the texture's clamped bottom edge; wider screens crop only the quiet floor.
    public sealed class PostalEventSceneImage : RawImage
    {
        protected override void OnEnable(){base.OnEnable();Fit();}
        protected override void OnRectTransformDimensionsChange(){base.OnRectTransformDimensionsChange();Fit();}
        public void Fit()
        {
            if(texture==null||rectTransform.rect.width<1||rectTransform.rect.height<1)return;
            float source=texture.width/(float)texture.height,target=rectTransform.rect.width/rectTransform.rect.height;
            var crop=new Rect(0,1-source/target,1,source/target);
            if(uvRect!=crop)uvRect=crop;
        }
        public Vector2 ImagePoint(Vector2 uv)
        {
            var r=rectTransform.rect;
            return r.min+Vector2.Scale(new Vector2((uv.x-uvRect.x)/uvRect.width,(uv.y-uvRect.y)/uvRect.height),r.size);
        }
    }

    // Only the small foreground canvas moves. The illustration, header and text stay static.
    // No ExecuteAlways: opening the editor never starts a repaint/animation loop.
    public sealed class PostalEventSurpriseMotion : MonoBehaviour
    {
        const int LetterCount=3,SparkCount=6;
        readonly RectTransform[] letters=new RectTransform[LetterCount],sparks=new RectTransform[SparkCount];
        readonly RawImage[] letterImages=new RawImage[LetterCount];
        readonly PostalEventSparkle[] sparkImages=new PostalEventSparkle[SparkCount];
        PostalEventSceneImage scene;
        float elapsed;
        public float Elapsed=>elapsed;
        public static PostalEventSurpriseMotion Create(RectTransform parent,PostalEventSceneImage scene)
        {
            var go=new GameObject("SurpriseMotion",typeof(RectTransform),typeof(Canvas)){layer=parent.gameObject.layer};
            var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            var motion=go.AddComponent<PostalEventSurpriseMotion>();motion.scene=scene;
            var texture=Resources.Load<Texture2D>("EventsV92/letter");
            for(int i=0;i<LetterCount;i++)
            {
                var child=Element("MagicLetter"+i,r);var image=child.gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;
                motion.letters[i]=child;motion.letterImages[i]=image;
            }
            for(int i=0;i<SparkCount;i++)
            {
                var child=Element("GoldenSpark"+i,r);var image=child.gameObject.AddComponent<PostalEventSparkle>();image.raycastTarget=false;
                motion.sparks[i]=child;motion.sparkImages[i]=image;
            }
            motion.SampleAt(0);return motion;
        }
        static RectTransform Element(string name,RectTransform parent)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer)){layer=parent.gameObject.layer};
            var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);return r;
        }
        void OnEnable(){if(scene!=null)SampleAt(0);}
        void OnRectTransformDimensionsChange(){if(scene!=null)SampleAt(elapsed);}
        void Update(){if(Application.isPlaying)SampleAt(elapsed+Time.unscaledDeltaTime);}
        public void SampleAt(float seconds)
        {
            if(scene==null||letters[0]==null)return;
            elapsed=Mathf.Max(0,seconds);scene.Fit();var area=scene.rectTransform.rect;
            if(area.width<1||area.height<1)return;
            float width=area.width/scene.uvRect.width,cycle=Mathf.Repeat(elapsed,8f);
            for(int i=0;i<LetterCount;i++)
            {
                float age=(cycle-i*.95f)/3.2f;
                bool visible=age>=0&&age<1;float p=Mathf.Clamp01(age);
                float alpha=visible?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.015f,.16f,p))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.76f,1,p))):0;
                float t=Mathf.SmoothStep(0,1,p),drift=(i-1)*.038f*t+Mathf.Sin(p*Mathf.PI*2+i)*.018f*t;
                letters[i].anchoredPosition=scene.ImagePoint(new Vector2(.55f+drift,.548f+.275f*t))-area.center;
                letters[i].sizeDelta=new Vector2(width*.128f,width*.128f/1.5f);
                letters[i].localScale=Vector3.one*Mathf.Lerp(.2f,1,Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.35f,p)));
                letters[i].localRotation=Quaternion.Euler(0,0,Mathf.Sin(p*5+i)*13);
                letterImages[i].color=new Color(1,1,1,alpha);
            }
            for(int i=0;i<SparkCount;i++)
            {
                float p=Mathf.Repeat(elapsed*.28f+i*.167f,1),t=Mathf.SmoothStep(0,1,p);
                float alpha=Mathf.Sin(p*Mathf.PI)*.72f;
                sparks[i].anchoredPosition=scene.ImagePoint(new Vector2(.55f+Mathf.Sin(i*2.4f)*.085f*t,.548f+.23f*t))-area.center;
                sparks[i].sizeDelta=Vector2.one*width*(.009f+.008f*Mathf.Sin(p*Mathf.PI));
                sparks[i].localRotation=Quaternion.Euler(0,0,p*50+i*17);
                sparkImages[i].color=new Color(1,.9f,.48f,alpha);
            }
        }
    }

    public sealed class PostalEventSparkle : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;var center=r.center;vh.AddVert(center,color,Vector2.zero);
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI*.25f,scale=(i%2==0?1:.24f);
                vh.AddVert(center+new Vector2(Mathf.Cos(a)*r.width*.5f,Mathf.Sin(a)*r.height*.5f)*scale,color,Vector2.zero);
            }
            for(int i=0;i<8;i++)vh.AddTriangle(0,i+1,(i+1)%8+1);
        }
    }
}
