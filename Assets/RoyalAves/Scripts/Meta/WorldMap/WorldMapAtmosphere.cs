using UnityEngine;
using UnityEngine.UI;
namespace RoyalAves.Meta.WorldMap
{
    // Two reusable sky layers animate across island changes, independently of ground and HUD.
    public sealed class WorldMapAtmosphere : MonoBehaviour
    {
        sealed class SkyLayer
        {
            public RectTransform root;public CanvasGroup group;public Image background,villain;
            public WorldMapWeatherGraphic gradient,sun,ambient,meteors,moon;
            public readonly Image[] clouds=new Image[4];public WorldMapIslands.Island style;
            public readonly Image[] meteorSprites=new Image[3];
            public bool staticDrawn;public WorldMapIslands.Island drawnStyle;public Vector2 drawnSize;
            public float drawnNight,drawnHorizon;public Vector4 drawnVillain;public Sprite drawnBackdrop,drawnVillainSprite;
        }
        WorldMapKit kit;WorldMapRoute route;WorldMapCameraModel skyCamera;
        SkyLayer current,previous;WorldMapWeatherGraphic effects,previousEffects;
        int active=-1,direction=1;float changedAt,lastTime=-1;
        public float TransitionProgress {get;private set;}=1;
        public float Night;   // 0 = dia, 1 = noite (definido pelo WorldMapView)
        public int ActiveIsland=>active;
        public bool NeedsAnimation=>route?.Islands!=null&&!route.Islands.staticSky&&active>=0&&route.Islands.ChristmasSky(current.style)&&route.Islands.christmasVillain!=null&&route.Islands.villainHop>0;
        public static WorldMapAtmosphere Create(RectTransform root,RectTransform skyParent,Image sky,WorldMapKit kit,WorldMapRoute route=null)
        {
            var a=root.gameObject.AddComponent<WorldMapAtmosphere>();a.kit=kit;a.route=route;
            a.skyCamera=new WorldMapCameraModel(route??new WorldMapRoute(),kit);sky.enabled=false;
            a.previous=a.Layer(skyParent,"CeuAnterior");a.current=a.Layer(skyParent,"CeuAtual");
            a.previousEffects=Graphic(root,"ClimaAnterior");a.effects=Graphic(root,"ClimaDoMapa");return a;
        }
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static WorldMapWeatherGraphic Graphic(RectTransform parent,string name)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(WorldMapWeatherGraphic));go.transform.SetParent(parent,false);
            Stretch((RectTransform)go.transform);var g=go.GetComponent<WorldMapWeatherGraphic>();g.raycastTarget=false;return g;
        }
        static Image Image(RectTransform parent,string name)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);
            var image=go.GetComponent<UnityEngine.UI.Image>();image.raycastTarget=false;return image;
        }
        SkyLayer Layer(RectTransform parent,string name)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasGroup));go.transform.SetParent(parent,false);
            var l=new SkyLayer{root=(RectTransform)go.transform,group=go.GetComponent<CanvasGroup>()};Stretch(l.root);
            l.group.blocksRaycasts=false;l.group.interactable=false;l.gradient=Graphic(l.root,"Gradiente");
            l.background=Image(l.root,"BackgroundSeparado");Stretch(l.background.rectTransform);
            l.villain=Image(l.root,"RibbonRookSilhouette");l.villain.preserveAspect=true;
            l.sun=Graphic(l.root,"RaiosDeSol");l.moon=Graphic(l.root,"LuaEEstrelas");l.ambient=Graphic(l.root,"LuzDoAmbiente");l.meteors=Graphic(l.root,"Meteoros");
            for(var i=0;i<4;i++){l.clouds[i]=Image(l.root,"Nuvem_"+i);l.clouds[i].preserveAspect=true;}
            for(var i=0;i<3;i++){l.meteorSprites[i]=Image(l.root,"MeteoroSprite_"+i);l.meteorSprites[i].preserveAspect=true;}
            return l;
        }
        float HorizonHeight(Vector2 size)
        {
            if(size.x<=0||size.y<=0)return .7f;
            var aspect=size.x/size.y;
            var point=MapCurve.Point(skyCamera.PitchAt(aspect)*Mathf.Deg2Rad*kit.radius,0,0,kit.radius,route?.Curvature??1);
            return skyCamera.ProjectPoint(point,aspect,out var uv,out _)?uv.y:.7f;
        }
        public void Tick(float cursor,float time)
        {
            bool still=route?.Islands?.staticSky==true;
            if(still)time=0;
            var focus=route!=null?route.MetersAt(cursor)+kit.focusAhead:0;
            var index=route?.Islands!=null?route.IslandIndexAt(focus):0;
            var style=route?.IslandAt(focus)??WorldMapIslands.DefaultIsland(kit.Lava?4:0);
            if(active<0){active=index;current.style=style;previous.style=style;changedAt=time-1;}
            else if(active!=index)
            {
                direction=index>active?1:-1;active=index;
                var a=current.style;var same=route?.Islands!=null&&route.Islands.ChristmasSky(a)&&route.Islands.ChristmasSky(style)||route?.Islands?.unifiedBase==true&&a!=null&&a.background==style.background&&a.skyTop==style.skyTop&&a.skyBottom==style.skyBottom&&a.cloudSmall==style.cloudSmall&&a.cloudWide==style.cloudWide&&a.weather==style.weather&&a.weatherIntensity==style.weatherIntensity&&a.meteors==style.meteors&&route.Islands.ChristmasSky(a)==route.Islands.ChristmasSky(style);
                if(same)current.style=style;
                else{var swap=previous;previous=current;current=swap;current.style=style;current.root.SetAsLastSibling();changedAt=time;}
            }
            if(time<lastTime)changedAt=time-1;lastTime=time;
            TransitionProgress=still?1:Mathf.SmoothStep(0,1,Mathf.Clamp01((time-changedAt)/.8f));
            var size=((RectTransform)current.root.parent).rect.size;var horizon=HorizonHeight(size);
            Draw(current,time,TransitionProgress,direction*(1-TransitionProgress)*size.x*.2f,size,horizon);
            Draw(previous,time,1-TransitionProgress,-direction*TransitionProgress*size.x*.2f,size,horizon);
            effects.enabled=previousEffects.enabled=!still;
            if(!still){Weather(effects,current.style,time,TransitionProgress);Weather(previousEffects,previous.style,time,1-TransitionProgress);}
        }
        void Draw(SkyLayer l,float time,float alpha,float slide,Vector2 size,float horizon)
        {
            var overscan=route?.Islands?.staticSky==true?.06f:.2f;
            l.group.alpha=alpha;l.root.anchorMin=new Vector2(-overscan,horizon-.04f);l.root.anchorMax=new Vector2(1+overscan,1);
            l.root.offsetMin=l.root.offsetMax=new Vector2(slide,0);
            var s=l.style;if(s==null)return;
            var winter=route?.Islands?.ChristmasSky(s)==true;
            var seasonal=route?.Islands;bool still=seasonal?.staticSky==true;
            var villainPose=seasonal!=null?new Vector4(seasonal.villainPosition.x,seasonal.villainPosition.y,seasonal.villainSize,seasonal.villainOpacity):Vector4.zero;
            if(still&&l.staticDrawn&&l.drawnStyle==s&&l.drawnSize==size&&l.drawnNight==Night&&l.drawnHorizon==horizon&&l.drawnVillain==villainPose&&l.drawnBackdrop==seasonal.christmasBackdrop&&l.drawnVillainSprite==seasonal.christmasVillain)return;
            l.staticDrawn=still;l.drawnStyle=s;l.drawnSize=size;l.drawnNight=Night;l.drawnHorizon=horizon;l.drawnVillain=villainPose;
            l.drawnBackdrop=seasonal?.christmasBackdrop;l.drawnVillainSprite=seasonal?.christmasVillain;
            l.background.sprite=winter?route.Islands.christmasBackdrop:s.background;l.background.enabled=l.background.sprite!=null;
            l.villain.sprite=winter?seasonal.christmasVillain:null;l.villain.enabled=l.villain.sprite!=null;
            if(l.villain.enabled)
            {
                float phase=Mathf.Repeat(time*seasonal.villainSpeed,1);
                float jump=Mathf.Sin(phase*Mathf.PI);
                var position=seasonal.villainPosition+(still?Vector2.zero:new Vector2((phase-.5f)*.035f,jump*seasonal.villainHop));
                l.villain.rectTransform.anchorMin=l.villain.rectTransform.anchorMax=position;
                l.villain.rectTransform.anchoredPosition=Vector2.zero;l.villain.rectTransform.sizeDelta=Vector2.one*seasonal.villainSize;
                l.villain.rectTransform.localRotation=still?Quaternion.identity:Quaternion.Euler(0,0,Mathf.Lerp(-4,4,phase));
                l.villain.color=still?new Color(1,1,1,seasonal.villainOpacity):new Color(.65f,.88f,.90f,seasonal.villainOpacity);
            }
            var n=Night;
            l.background.color=Color.Lerp(Color.white,winter?new Color(.65f,.72f,.93f):new Color(.20f,.26f,.52f),winter?n*.45f:n);
            l.gradient.enabled=!l.background.enabled;l.gradient.mode=7;
            l.gradient.skyTop=Color.Lerp(winter?new Color(.19f,.40f,.71f):s.skyTop,new Color(.03f,.05f,.17f),n);
            var dusk=Mathf.Sin(n*Mathf.PI)*.60f;
            l.gradient.skyBottom=Color.Lerp(Color.Lerp(winter?new Color(.63f,.87f,.92f):s.skyBottom,winter?new Color(.70f,.67f,.86f):new Color(.96f,.43f,.26f),dusk),new Color(.12f,.18f,.42f),n);l.gradient.SetVerticesDirty();
            l.moon.mode=13;l.moon.time=time;l.moon.enabled=!winter&&n>.01f;l.moon.canvasRenderer.SetAlpha(n);l.moon.SetVerticesDirty();
            if(still)l.moon.enabled=false;
            var underwater=!winter&&s.terrain==WorldMapTerrainTheme.CoastLand;
            var cavern=!winter&&s.terrain==WorldMapTerrainTheme.CrystalLand;
            l.sun.canvasRenderer.SetAlpha(1-n);l.sun.mode=winter?0:underwater?11:cavern?0:s.weather==WorldMapWeather.Clear||s.weather==WorldMapWeather.Wind?5:0;l.sun.time=time;l.sun.SetVerticesDirty();
            if(still)l.sun.mode=0;
            l.ambient.mode=winter?(l.background.enabled?0:14):route?.Islands?.unifiedBase==true?12:s.terrain==WorldMapTerrainTheme.ForestLand||s.terrain==WorldMapTerrainTheme.MarshLand||cavern?9:s.terrain==WorldMapTerrainTheme.AuroraLand?10:0;l.ambient.time=time;l.ambient.SetVerticesDirty();
            l.meteors.mode=0;l.meteors.SetVerticesDirty();
            for(var i=0;i<l.meteorSprites.Length;i++)
            {
                var sprite=l.meteorSprites[i];var phase=Mathf.Repeat(time*.17f+i*.37f,1);var t=phase/.52f;
                sprite.sprite=kit.meteorSprite;sprite.enabled=!still&&!winter&&s.meteors&&kit.meteorSprite!=null&&phase<.52f;
                var sign=i%2==0?1:-1;
                sprite.rectTransform.anchorMin=sprite.rectTransform.anchorMax=new Vector2(.18f+i*.26f+sign*t*.24f,1-t*.9f);
                sprite.rectTransform.anchoredPosition=Vector2.zero;sprite.rectTransform.sizeDelta=new Vector2(100+i*20,120+i*24);
                sprite.rectTransform.localScale=new Vector3(sign,1,1);sprite.color=new Color(1,1,1,Mathf.Sin(Mathf.Clamp01(t)*Mathf.PI)*.9f);
            }
            var lava=!winter&&s.terrain==WorldMapTerrainTheme.LavaLand;var wet=s.weather==WorldMapWeather.Rain||s.weather==WorldMapWeather.Storm;
            for(var i=0;i<4;i++)
            {
                var cloud=l.clouds[i];cloud.sprite=i%2==0?s.cloudWide:s.cloudSmall;
                if(route?.Islands?.unifiedBase==true&&cloud.sprite==null)cloud.sprite=i%2==0?kit.simpleCloudWide:kit.simpleCloudSmall;
                if(cloud.sprite==null)cloud.sprite=lava?(i%2==0?kit.lavaCloudWide:kit.lavaCloudSmall):(i%2==0?kit.warmCloudWide:kit.warmCloudSmall);
                cloud.enabled=!still&&!winter&&cloud.sprite!=null&&!underwater&&!cavern;cloud.color=wet?new Color(.65f,.73f,.87f,.7f):new Color(1,1,1,lava?.52f:.76f);if(winter)cloud.color=new Color(.93f,.97f,1,.65f);cloud.color=Color.Lerp(cloud.color,new Color(.32f,.38f,.66f,cloud.color.a*.8f),n);
                var x=Mathf.Repeat(i*.32f+time*(s.weather==WorldMapWeather.Wind?.014f:.004f),1.4f)-.2f;
                cloud.rectTransform.anchorMin=cloud.rectTransform.anchorMax=new Vector2(x,.35f+(i%3)*.22f);
                cloud.rectTransform.anchoredPosition=Vector2.zero;cloud.rectTransform.sizeDelta=route?.Islands?.unifiedBase==true?new Vector2(145+i*20,65+i*8):new Vector2(250+i*38,105+i*16);
            }
        }
        static void Weather(WorldMapWeatherGraphic g,WorldMapIslands.Island s,float time,float alpha)
        {
            if(s==null)return;g.time=time;
            g.mode=s.weather==WorldMapWeather.Rain?1:s.weather==WorldMapWeather.Wind?2:s.weather==WorldMapWeather.Storm?3:s.weather==WorldMapWeather.Snow?4:s.weather==WorldMapWeather.Embers?6:0;
            if(s.terrain==WorldMapTerrainTheme.CoastLand)g.mode=11;
            g.canvasRenderer.SetAlpha(alpha*s.weatherIntensity);g.SetVerticesDirty();
        }
    }

    public sealed class WorldMapWeatherGraphic : MaskableGraphic
    {
        public Color skyTop,skyBottom;
        public int mode;
        public float time;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            if(mode==14)
            {
                // Quiet snowy fir silhouettes sit behind every island's own terrain.
                for(int i=0;i<9;i++)
                {
                    float x=r.xMin+r.width*(.04f+i*.115f),h=r.height*(.17f+(i%3)*.055f);
                    for(int tier=0;tier<3;tier++)
                    {
                        float y=r.yMin+h*(.18f+tier*.22f),wide=h*(.38f-tier*.07f);
                        var tip=new Vector2(x,y+h*.48f);
                        Triangle(vh,tip,new Vector2(x-wide,y),new Vector2(x+wide,y),new Color(.34f,.65f,.76f,.34f));
                        Triangle(vh,tip,new Vector2(x-wide*.63f,y+h*.16f),new Vector2(x+wide*.63f,y+h*.16f),new Color(.90f,.98f,1,.45f));
                    }
                }
                for(int i=0;i<17;i++)
                {
                    var p=new Vector2(r.xMin+r.width*Mathf.Repeat(i*.618f+.11f,1),r.yMin+r.height*(.24f+.68f*Mathf.Repeat(i*.371f,1)));
                    var c=new Color(.96f,1,1,.45f);
                    Circle(vh,p,1.4f+i%3*.5f,c);
                    if(i%5==0){Line(vh,p-Vector2.right*4,p+Vector2.right*4,.8f,c);Line(vh,p-Vector2.up*4,p+Vector2.up*4,.8f,c);}
                }
                return;
            }
            if(mode==13)
            {
                // Lua grande e brilhante + estrelas que piscam (noite).
                var moon=new Vector2(r.xMin+r.width*.47f,r.yMin+r.height*.40f);
                for(var i=0;i<46;i++)
                {
                    var sp=new Vector2(r.xMin+r.width*Mathf.Repeat(i*.6180339f,1),r.yMin+r.height*(.08f+.84f*Mathf.Repeat(i*.3819f+.13f,1)));
                    var tw=.35f+.65f*Mathf.Pow(Mathf.Sin(time*(.9f+i%5*.35f)+i*1.7f)*.5f+.5f,2);
                    Circle(vh,sp,1.6f+i%3*.9f,new Color(1,.97f,.85f,.85f*tw));
                    if(i%6==0){Line(vh,sp-new Vector2(7,0),sp+new Vector2(7,0),1.2f,new Color(1,1,.9f,.5f*tw));Line(vh,sp-new Vector2(0,7),sp+new Vector2(0,7),1.2f,new Color(1,1,.9f,.5f*tw));}
                }
                MoonHalo(vh,moon,180);
                GlossyMoon(vh,moon,60);
                return;
            }
            if(mode==12)
            {
                // Soft silhouettes keep the horizon layered without adding foreground props.
                for(var i=0;i<7;i++)
                {
                    var x=r.xMin+r.width*(.06f+i*.15f);var y=r.yMin;
                    var height=r.height*(.16f+(i%3)*.055f);var width=r.width*.055f;
                    var c=new Color(1,.94f,.64f,.28f);
                    if(i%2==0)
                    {
                        Triangle(vh,new Vector2(x,y+height),new Vector2(x-width,y),new Vector2(x+width,y),c);
                    }
                    else
                    {
                        Circle(vh,new Vector2(x,y+height*.3f),height*.38f,c);
                        Circle(vh,new Vector2(x-width*.6f,y+height*.12f),height*.30f,c);
                        Circle(vh,new Vector2(x+width*.7f,y+height*.13f),height*.32f,c);
                    }
                }
                return;
            }
            if(mode==11)
            {
                for(var i=0;i<5;i++)
                {
                    var x=r.xMin+r.width*(.12f+i*.23f+Mathf.Sin(time*.22f+i)*.035f);
                    Triangle(vh,new Vector2(x,r.yMax),new Vector2(x-r.width*.16f,r.yMin),new Vector2(x+r.width*.08f,r.yMin),new Color(.45f,1,1,.035f));
                }
                for(var i=0;i<18;i++)
                {
                    var phase=Mathf.Repeat(i*.371f+time*.055f,1);
                    var center=new Vector2(r.xMin+r.width*Mathf.Repeat(i*.618f+Mathf.Sin(time*.6f+i)*.015f,1),r.yMin+r.height*phase);
                    var radius=3+i%4*1.8f;
                    for(var n=0;n<14;n++)
                    {
                        var a=n*Mathf.PI*2/14;var b=(n+1)*Mathf.PI*2/14;
                        Line(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,1.1f,new Color(.7f,1,1,.26f*Mathf.Sin(phase*Mathf.PI)));
                    }
                }
                return;
            }
            if(mode==9)
            {
                for(var i=0;i<18;i++)
                {
                    var p=new Vector2(r.xMin+r.width*Mathf.Repeat(i*.618f+Mathf.Sin(time*.2f+i)*.015f,1),r.yMin+r.height*(.12f+Mathf.Repeat(i*.371f+time*.025f, .8f)));
                    var a=.25f+.2f*Mathf.Sin(time*1.5f+i);Circle(vh,p,9,new Color(.5f,1,.7f,a*.12f));Circle(vh,p,2.5f,new Color(.7f,1,.65f,a));
                }
                return;
            }
            if(mode==10)
            {
                for(var ribbon=0;ribbon<2;ribbon++)for(var i=0;i<64;i++)
                {
                    var u=i/64f;var v=(i+1)/64f;
                    float Y(float x)=>r.yMin+r.height*(.45f+ribbon*.18f+Mathf.Sin(x*7+time*.18f+ribbon)*.13f);
                    var c=ribbon==0?new Color(.18f,1,.8f,.12f):new Color(.8f,.25f,1,.10f);var n=vh.currentVertCount;
                    vh.AddVert(new Vector2(r.xMin+r.width*u,Y(u)),c,Vector2.zero);vh.AddVert(new Vector2(r.xMin+r.width*u,Y(u)+r.height*.12f),c,Vector2.up);
                    vh.AddVert(new Vector2(r.xMin+r.width*v,Y(v)+r.height*.12f),c,Vector2.one);vh.AddVert(new Vector2(r.xMin+r.width*v,Y(v)),c,Vector2.right);
                    vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
                }
                return;
            }
            if(mode==7)
            {
                vh.AddVert(new Vector2(r.xMin,r.yMin),skyBottom,Vector2.zero);
                vh.AddVert(new Vector2(r.xMin,r.yMax),skyTop,Vector2.up);
                vh.AddVert(new Vector2(r.xMax,r.yMax),skyTop,Vector2.one);
                vh.AddVert(new Vector2(r.xMax,r.yMin),skyBottom,Vector2.right);
                vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);return;
            }
            if (mode == 5)
            {
                var center = new Vector2(r.xMin + r.width * .87f, r.yMin + r.height * .83f);
                Circle(vh, center, 38, new Color(1, .96f, .70f, .9f));
                Circle(vh, center, 64, new Color(1, .95f, .69f, .08f));
                // Leques de raios girando em sentidos opostos (volta em ~90 s e ~140 s), pulsando de leve.
                for (var layer = 0; layer < 2; layer++)
                    for (var i = 0; i < 12; i++)
                    {
                        var spin = (layer == 0 ? 1 : -1) * time * (layer == 0 ? 4f : 2.6f) + layer * 15;
                        var a = (i * 30 + spin) * Mathf.Deg2Rad;
                        var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        var pulse = .75f + .25f * Mathf.Sin(time * .9f + i * 1.7f + layer);
                        var width = layer == 0 ? .05f : .035f;
                        Triangle(vh, center + d * 36, center + Rotate(d, -width) * 640, center + Rotate(d, width) * 640,
                            new Color(1, layer == 0 ? .93f : .86f, layer == 0 ? .62f : .45f, (layer == 0 ? .075f : .06f) * pulse));
                    }
                Circle(vh, center, 96 + 6 * Mathf.Sin(time * 1.3f), new Color(1, .93f, .55f, .07f));
                return;
            }
            if (mode == 0) return;
            if(mode==6)
            {
                for(var i=0;i<24;i++)
                {
                    var phase=Mathf.Repeat(i*.371f+time*.12f,1);
                    Circle(vh,new Vector2(r.xMin+r.width*Mathf.Repeat(i*.618f+Mathf.Sin(time+i)*.012f,1),r.yMin+r.height*(.25f+phase*.6f)),
                        1.5f+i%3,new Color(1,.65f,.24f,(1-phase)*.4f));
                }
                return;
            }
            var count = mode == 2 ? 10 : mode == 3 ? 90 : 50;
            for (var i = 0; i < count; i++)
            {
                var a = Mathf.Repeat(i * .6180339f, 1);
                var phase = Mathf.Repeat(i * .371f + time * (mode == 4 ? .10f : .65f), 1);
                var x = r.xMin + r.width * Mathf.Repeat(a + Mathf.Sin(time * .4f + i) * .015f, 1);
                var y = r.yMin + r.height * (.12f + (1 - phase) * .74f);
                var p = new Vector2(x, y);
                if (mode == 4) Circle(vh, p, 2 + i % 3, new Color(1, 1, 1, .66f));
                else if (mode == 2) Line(vh, p, p + new Vector2(50 + i % 4 * 8, 8), 1.4f, new Color(1, 1, 1, .16f));
                else Line(vh, p, p + new Vector2(-6, 25), 1.6f, new Color(.84f, .93f, 1, mode == 3 ? .35f : .23f));
            }
            if (mode == 3 && Mathf.Repeat(time, 13) < .09f)
                Quad(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMax), new Color(1, 1, 1, .1f));
        }

        static Vector2 Rotate(Vector2 v, float a) => new Vector2(v.x * Mathf.Cos(a) - v.y * Mathf.Sin(a), v.x * Mathf.Sin(a) + v.y * Mathf.Cos(a));
        static void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            var i = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero); vh.AddVert(b, color, Vector2.zero); vh.AddVert(c, color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
        }
        static void GlossyMoon(VertexHelper vh,Vector2 center,float radius)
        {
            const int segments=64,rings=28;var start=vh.currentVertCount;
            Color Paint(Vector2 p)
            {
                float r=p.magnitude,z=Mathf.Sqrt(Mathf.Clamp01(1-r*r));
                float light=Mathf.Clamp01(.28f+z*.57f-p.x*.24f+p.y*.25f);
                var c=Color.Lerp(new Color(.40f,.61f,.84f),new Color(1,.98f,.84f),light);
                var a=p-new Vector2(-.20f,-.18f);var b=p-new Vector2(.28f,.10f);
                float craters=Mathf.Exp(-a.sqrMagnitude/.025f)*.075f+Mathf.Exp(-b.sqrMagnitude/.045f)*.06f;
                c=Color.Lerp(c,new Color(.53f,.67f,.78f),craters);
                float shine=Mathf.Exp(-Mathf.Pow((p.x+.29f)/.38f,2)-Mathf.Pow((p.y-.43f)/.17f,2));
                c=Color.Lerp(c,new Color(1,1,.97f),shine*.92f);c.a=1;return c;
            }
            vh.AddVert(center,Paint(Vector2.zero),Vector2.zero);
            for(int ring=1;ring<=rings;ring++)for(int k=0;k<segments;k++)
            {
                float t=ring/(float)rings,angle=k*Mathf.PI*2/segments;var p=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*t;
                vh.AddVert(center+p*radius,Paint(p),Vector2.zero);
                int current=start+1+(ring-1)*segments+k,next=start+1+(ring-1)*segments+(k+1)%segments;
                if(ring==1)vh.AddTriangle(start,current,next);
                else{int inner=current-segments,innerNext=next-segments;vh.AddTriangle(inner,current,next);vh.AddTriangle(inner,next,innerNext);}
            }
        }
        static void MoonHalo(VertexHelper vh,Vector2 center,float radius)
        {
            const int segments=48,rings=24;var start=vh.currentVertCount;
            vh.AddVert(center,new Color(.65f,.78f,1,.28f),Vector2.zero);
            for(int ring=1;ring<=rings;ring++)
            {
                float t=ring/(float)rings;var color=new Color(.65f,.78f,1,.28f*Mathf.Exp(-t*t*4)*(1-t));
                for(int k=0;k<segments;k++)
                {
                    float angle=k*Mathf.PI*2/segments;vh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius*t,color,Vector2.zero);
                    int current=start+1+(ring-1)*segments+k,next=start+1+(ring-1)*segments+(k+1)%segments;
                    if(ring==1)vh.AddTriangle(start,current,next);
                    else{int inner=current-segments,innerNext=next-segments;vh.AddTriangle(inner,current,next);vh.AddTriangle(inner,next,innerNext);}
                }
            }
        }
        static void Circle(VertexHelper vh, Vector2 center, float radius, Color color)
        {
            for (var i = 0; i < 24; i++)
            {
                var a = i * Mathf.PI / 12; var b = (i + 1) * Mathf.PI / 12;
                Triangle(vh, center, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, color);
            }
        }
        static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            var n = new Vector2(-(b - a).y, (b - a).x).normalized * width * .5f;
            Triangle(vh, a - n, a + n, b + n, color); Triangle(vh, a - n, b + n, b - n, color);
        }
        static void Quad(VertexHelper vh, Vector2 min, Vector2 max, Color color)
        {
            Triangle(vh, min, new Vector2(min.x, max.y), max, color);
            Triangle(vh, min, max, new Vector2(max.x, min.y), color);
        }
    }
}
