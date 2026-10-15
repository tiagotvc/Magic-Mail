using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta.WorldMap
{
    // Map-owned chrome uses the canonical UI, never scene overrides from the retired agency.
    public sealed class WorldMapHudPresentation
    {
        readonly RectTransform content,sourceNav;
        RectTransform nav,header;
        readonly WorldMapKit kit;
        readonly TMP_FontAsset navigationFont;
        readonly List<System.Action> restore=new List<System.Action>();
        readonly List<GameObject> panels=new List<GameObject>();
        readonly List<(TMP_Text source,TMP_Text target)> texts=new List<(TMP_Text,TMP_Text)>();
        readonly List<(Image source,Image target)> pictures=new List<(Image,Image)>();
        readonly Dictionary<int,Button> tabs=new Dictionary<int,Button>();
        Button avatarSource,coinsSource,livesSource,settingsSource;
        Button[] navSources;
        WorldMapPanelGraphic navigationPanel;
        Material navigationLabelMaterial;
        Material navigationSurfaceMaterial;
        WorldMapTopHud topHud;
        int selectedTab=2;
        bool active;
        public WorldMapHudPresentation(RectTransform content,RectTransform nav,WorldMapKit kit,TMP_FontAsset navigationFont=null)
        {this.content=content;sourceNav=nav;this.kit=kit;this.navigationFont=navigationFont;}
        public void BindButtons(Button avatar,Button coins,Button lives,Button settings,Button[] navigation)
        {avatarSource=avatar;coinsSource=coins;livesSource=lives;settingsSource=settings;navSources=navigation;}
        static int TabKey(string name)=>name=="Eventos"?0:name=="Ranking"?1:name=="Equipes"?3:name=="Coleção"?4:2;
        static string Relative(Transform item,Transform root)
        {
            var path=item.name;
            while(item.parent!=null&&item.parent!=root){item=item.parent;path=item.name+"/"+path;}
            return path;
        }
        void CreateChrome()
        {
            var sourceHeader=content.Find("HUD") as RectTransform;
            var template=kit.mapUiTemplate!=null?kit.mapUiTemplate.transform.Find("Content"):null;
            var headerTemplate=template!=null?template.Find("HUD"):sourceHeader;
            var navTemplate=template!=null?template.Find("Nav"):sourceNav;
            if(sourceHeader!=null&&WorldMapTopHud.Ready(kit))
            {
                topHud=new WorldMapTopHud(content,sourceHeader,kit,navigationFont,avatarSource,coinsSource,livesSource,settingsSource);
                header=topHud.Root;header.SetSiblingIndex(sourceHeader.GetSiblingIndex()+1);panels.Add(header.gameObject);
                texts.AddRange(topHud.TextBindings);sourceHeader.gameObject.SetActive(false);
            }
            else if(sourceHeader!=null&&headerTemplate!=null)
            {
                header=Object.Instantiate(headerTemplate.gameObject,content,false).GetComponent<RectTransform>();header.name="HUDDoMapa";
                header.SetSiblingIndex(sourceHeader.GetSiblingIndex()+1);header.gameObject.SetActive(true);panels.Add(header.gameObject);
                sourceHeader.gameObject.SetActive(false);
                foreach(var target in header.GetComponentsInChildren<TMP_Text>(true))
                {
                    var source=sourceHeader.Find(Relative(target.transform,header))?.GetComponent<TMP_Text>();
                    if(source!=null)texts.Add((source,target));
                }
                foreach(var target in header.GetComponentsInChildren<Image>(true))
                {
                    var source=sourceHeader.Find(Relative(target.transform,header))?.GetComponent<Image>();
                    if(source!=null)pictures.Add((source,target));
                }
                void Wire(string group,Button original)
                {
                    var root=header.Find(group);if(root==null||original==null)return;
                    if(root.GetComponent<Button>()==null)
                    {
                        var surface=root.GetComponent<Image>();if(surface==null){surface=root.gameObject.AddComponent<Image>();surface.color=Color.clear;}
                        root.gameObject.AddComponent<Button>().targetGraphic=surface;
                    }
                    foreach(var button in root.GetComponentsInChildren<Button>(true))
                    {button.onClick=new Button.ButtonClickedEvent();button.onClick.AddListener(()=>original.onClick.Invoke());}
                }
                Wire("Avatar",avatarSource);Wire("Coins",coinsSource);Wire("Stars",sourceHeader.Find("Stars")?.GetComponentInChildren<Button>(true));Wire("Lives",livesSource);Wire("Settings",settingsSource);
            }
            if(sourceNav!=null&&navTemplate!=null)
            {
                nav=Object.Instantiate(navTemplate.gameObject,content,false).GetComponent<RectTransform>();nav.name="NavegacaoDoMapa";
                nav.SetSiblingIndex(sourceNav.GetSiblingIndex()+1);nav.gameObject.SetActive(true);panels.Add(nav.gameObject);
                sourceNav.gameObject.SetActive(false);
                foreach(var layout in nav.GetComponents<UnityEngine.UI.LayoutGroup>())layout.enabled=false;
                foreach(var button in nav.GetComponentsInChildren<Button>(true))
                {
                    var key=TabKey(button.name);tabs[key]=button;
                    var original=navSources!=null&&key<navSources.Length?navSources[key]:sourceNav.Find(button.name)?.GetComponent<Button>();
                    button.onClick=new Button.ButtonClickedEvent();button.onClick.AddListener(()=>{SelectTab(key);if(original!=null)original.onClick.Invoke();});
                }
            }
        }
        public void Tick()
        {
            foreach(var pair in texts)if(pair.source!=null&&pair.target!=null&&pair.source.text!=pair.target.text)pair.target.text=pair.source.text;
            foreach(var pair in pictures)if(pair.source!=null&&pair.target!=null&&pair.source.sprite!=pair.target.sprite)pair.target.sprite=pair.source.sprite;
        }
        public void SelectTab(int key)
        {
            selectedTab=key;
            if(navigationPanel!=null)navigationPanel.SelectedIndex=key==2?0:key==0?1:key==1?2:key;
            foreach(var pair in tabs)
            {
                var chosen=pair.Key==key;var button=pair.Value;
                var label=button.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
                if(label!=null)label.gameObject.SetActive(chosen);
                var icon=button.transform.Find("Icon") as RectTransform;
                if(icon!=null){icon.sizeDelta=Vector2.one*(chosen?194:pair.Key==0?158:pair.Key==4?170:165);icon.anchoredPosition=new Vector2(0,chosen?13:-16);}
                if(button.image!=null)button.image.color=Color.clear;
            }
        }
        void Remember(RectTransform r)
        {
            var min=r.anchorMin;var max=r.anchorMax;var p=r.pivot;var pos=r.anchoredPosition;var size=r.sizeDelta;var scale=r.localScale;
            restore.Add(()=>{if(r==null)return;r.anchorMin=min;r.anchorMax=max;r.pivot=p;r.anchoredPosition=pos;r.sizeDelta=size;r.localScale=scale;});
        }
        void Remember(Image i)
        {
            if(i==null)return;var sprite=i.sprite;var color=i.color;var enabled=i.enabled;var type=i.type;var aspect=i.preserveAspect;
            restore.Add(()=>{if(i==null)return;i.sprite=sprite;i.color=color;i.enabled=enabled;i.type=type;i.preserveAspect=aspect;});
        }
        public void Show()
        {
            if(active||content==null)return;active=true;
            CreateChrome();
            var hud=header;
            if(hud!=null&&topHud==null)
            {
                foreach(var text in hud.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if(text.transform.parent.name=="Heart")continue;
                    var color=text.color;restore.Add(()=>{if(text!=null)text.color=color;});text.color=new Color(.35f,.18f,.36f);
                }
                var names=new[]{"Lives","Avatar","Coins","Stars","Settings"};
                var centers=new[]{180f,410f,660f,865f,1015f};var widths=new[]{250f,156f,230f,180f,90f};
                for(var n=0;n<names.Length;n++)
                {
                    var r=hud.Find(names[n]) as RectTransform;if(r==null)continue;Remember(r);
                    r.anchorMin=r.anchorMax=new Vector2(0,.5f);r.pivot=new Vector2(.5f,.5f);
                    r.anchoredPosition=new Vector2(centers[n],0);r.sizeDelta=new Vector2(widths[n],151);
                    if(names[n]=="Settings")
                    {var wasActive=r.gameObject.activeSelf;restore.Add(()=>{if(r!=null)r.gameObject.SetActive(wasActive);});r.gameObject.SetActive(true);}
                }
            }
            if(nav==null)return;
            Remember(nav);nav.sizeDelta=new Vector2(nav.sizeDelta.x,205);nav.anchoredPosition=new Vector2(nav.anchoredPosition.x,102.5f);
            if(content.Find("Menus") is RectTransform menus){Remember(menus);menus.offsetMin=new Vector2(menus.offsetMin.x,205);}
            var background=nav.GetComponent<Image>();Remember(background);
            if(background!=null){background.sprite=null;background.color=new Color(.105f,.105f,.105f);}
            var panel=new GameObject("FaixaDoMapa",typeof(RectTransform),typeof(CanvasRenderer),typeof(WorldMapPanelGraphic));
            panel.layer=nav.gameObject.layer;panel.transform.SetParent(nav,false);panel.transform.SetAsFirstSibling();
            var rect=(RectTransform)panel.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            navigationPanel=panel.GetComponent<WorldMapPanelGraphic>();navigationPanel.bottom=true;navigationPanel.raycastTarget=false;panels.Add(panel);
            if(kit.navigationSurface!=null)
            {
                navigationSurfaceMaterial=new Material(kit.navigationSurface){name="Abas moldadas do mapa",hideFlags=HideFlags.HideAndDontSave};
                if(kit.navigationPaint!=null&&kit.navigationSelectedPaint!=null)
                {
                    navigationSurfaceMaterial.SetTexture("_BasePaint",kit.navigationPaint);navigationSurfaceMaterial.SetTexture("_ActivePaint",kit.navigationSelectedPaint);
                    navigationSurfaceMaterial.SetVector("_BasePaintRect",kit.navigationPaintRect);navigationSurfaceMaterial.SetVector("_ActivePaintRect",kit.navigationSelectedPaintRect);
                    navigationSurfaceMaterial.SetFloat("_UsePaintedNav",1);
                }
                navigationPanel.material=navigationSurfaceMaterial;
            }
            foreach(Transform child in nav)
            {
                if(child==panel.transform)continue;
                var icon=child.Find("Icon")?.GetComponent<Image>();var label=child.Find("Label")?.GetComponent<TextMeshProUGUI>();
                var face=child.GetComponent<Image>();Remember(face);if(face!=null)face.color=Color.clear;
                if(icon!=null)
                {
                    Remember(icon);Remember(icon.rectTransform);
                    var key=TabKey(child.name);
                    var icons=kit.mapNavIcons.Length==5?kit.mapNavIcons:kit.navIcons;
                    if(icons!=null&&key<icons.Length&&icons[key]!=null)icon.sprite=icons[key];
                    icon.color=Color.white;icon.preserveAspect=true;
                    icon.rectTransform.anchorMin=icon.rectTransform.anchorMax=new Vector2(.5f,.62f);
                    icon.rectTransform.pivot=new Vector2(.5f,.5f);icon.rectTransform.anchoredPosition=Vector2.zero;
                    icon.rectTransform.sizeDelta=Vector2.one*(key==2?130:110);
                }
                if(label!=null)
                {
                    if(child.name!="Eventos"&&child.name!="Ranking"&&child.name!="Equipes"&&child.name!="Coleção")
                    {
                        var text=label.text;restore.Add(()=>{if(label!=null)label.text=text;});label.text="Mapa";
                    }
                    var color=label.color;restore.Add(()=>{if(label!=null)label.color=color;});
                    label.color=new Color(1,.96f,.82f);Remember(label.rectTransform);
                    if(navigationFont!=null)label.font=navigationFont;
                    label.enableVertexGradient=false;label.fontStyle=FontStyles.Normal;label.fontSize=42;
                    label.enableAutoSizing=false;label.alignment=TextAlignmentOptions.Center;
                    if(navigationLabelMaterial==null&&label.fontSharedMaterial!=null)
                    {
                        // A single owned SDF material gives every tab the same short, crisp shadow.
                        // Never change the shared font asset or instantiate a material each frame.
                        navigationLabelMaterial=new Material(label.fontSharedMaterial){name="Legenda da navegação do mapa",hideFlags=HideFlags.HideAndDontSave};
                        navigationLabelMaterial.SetColor(ShaderUtilities.ID_FaceColor,Color.white);
                        navigationLabelMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth,0);
                        navigationLabelMaterial.EnableKeyword("UNDERLAY_ON");navigationLabelMaterial.DisableKeyword("UNDERLAY_INNER");
                        navigationLabelMaterial.SetColor(ShaderUtilities.ID_UnderlayColor,new Color(.23f,.055f,.32f,.72f));
                        navigationLabelMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX,.10f);
                        navigationLabelMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY,-.18f);
                        navigationLabelMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate,0);
                        navigationLabelMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness,.025f);
                    }
                    if(navigationLabelMaterial!=null){label.fontSharedMaterial=navigationLabelMaterial;label.UpdateMeshPadding();}
                    label.rectTransform.anchorMin=new Vector2(0,0);label.rectTransform.anchorMax=new Vector2(1,0);
                    label.rectTransform.anchoredPosition=new Vector2(0,36);label.rectTransform.sizeDelta=new Vector2(0,42);
                }
            }
            var order=new[]{2,0,1,3,4};
            for(var i=0;i<order.Length;i++)if(tabs.TryGetValue(order[i],out var button))
            {
                var r=(RectTransform)button.transform;r.SetSiblingIndex(i+1);
                r.anchorMin=new Vector2(i/5f,0);r.anchorMax=new Vector2((i+1)/5f,1);r.offsetMin=r.offsetMax=Vector2.zero;
            }
            SelectTab(selectedTab);Tick();
        }
        public void Hide()
        {
            if(!active)return;active=false;
            for(var i=restore.Count-1;i>=0;i--)restore[i]();restore.Clear();
            foreach(var panel in panels)if(panel!=null){panel.SetActive(false);if(Application.isPlaying)Object.Destroy(panel);else Object.DestroyImmediate(panel);}panels.Clear();
            if(navigationLabelMaterial!=null){if(Application.isPlaying)Object.Destroy(navigationLabelMaterial);else Object.DestroyImmediate(navigationLabelMaterial);navigationLabelMaterial=null;}
            if(navigationSurfaceMaterial!=null){if(Application.isPlaying)Object.Destroy(navigationSurfaceMaterial);else Object.DestroyImmediate(navigationSurfaceMaterial);navigationSurfaceMaterial=null;}
            topHud?.Dispose();topHud=null;
            texts.Clear();pictures.Clear();tabs.Clear();header=null;nav=null;navigationPanel=null;
        }
        public void SetChromeVisible(bool visible)
        {if(header!=null)header.gameObject.SetActive(visible);if(nav!=null)nav.gameObject.SetActive(visible);}
    }

    // The painted HUD is separate from the retired scene layout. Values and actions still belong
    // to the original game controls; this class only owns their visual presentation.
    public sealed class WorldMapTopHud
    {
        public const float Height=210;
        public readonly RectTransform Root;
        public readonly List<(TMP_Text source,TMP_Text target)> TextBindings=new List<(TMP_Text,TMP_Text)>();
        readonly WorldMapKit kit;
        readonly TMP_FontAsset font;
        Material textMaterial;
        public static bool Ready(WorldMapKit kit)
        {
            if(kit==null||kit.topHudArt==null||kit.topHudRects==null||kit.topHudArt.Length!=6||kit.topHudRects.Length!=6)return false;
            for(var i=0;i<6;i++)if(kit.topHudArt[i]==null||kit.topHudRects[i].z<=0||kit.topHudRects[i].w<=0)return false;
            return true;
        }
        public WorldMapTopHud(RectTransform parent,RectTransform source,WorldMapKit kit,TMP_FontAsset font,Button avatar,Button coins,Button lives,Button settings)
        {
            this.kit=kit;this.font=font!=null?font:source.GetComponentInChildren<TMP_Text>(true)?.font;
            Root=Rect(parent,"HUDDoMapa",0,0,0,Height);Root.anchorMin=new Vector2(0,1);Root.anchorMax=Vector2.one;
            Root.pivot=new Vector2(.5f,1);Root.anchoredPosition=Vector2.zero;
            Art(Root,"FundoRoxoDourado",0,540,Height/2,1080,Height,false);
            var lifeButton=Control(Root,"Lives",278,85,300,145,lives!=null?lives:SourceButton(source,"Lives"));
            Art(lifeButton,"Contador",1,43,-10,212,73,false);
            Art(lifeButton,"Coracao",3,-72,-13,126,112);
            var lifeValue=source.Find("Lives/Heart/Count")?.GetComponent<TMP_Text>();
            var lifeLabel=source.Find("Lives/Frame/Label")?.GetComponent<TMP_Text>();
            Bind(lifeValue,Text(lifeButton,"Value","5",-72,-13,90,90,66));
            Bind(lifeLabel,Text(lifeButton,"State","Cheio",65,-10,139,68,43));
            var avatarButton=Control(Root,"Avatar",500,89,160,160,avatar!=null?avatar:SourceButton(source,"Avatar"));
            Art(avatarButton,"CarteiroAdulto",2,0,0,160,160);
            var coinButton=Control(Root,"Coins",724,85,270,140,coins!=null?coins:SourceButton(source,"Coins"));
            Art(coinButton,"Contador",1,18,-10,205,73,false);
            Art(coinButton,"CartaDourada",4,-83,-9,116,96);
            var coinTexts=source.Find("Coins")?.GetComponentsInChildren<TMP_Text>(true);
            var value=Text(coinButton,"Value","0",42,-10,120,68,50);value.enableAutoSizing=true;value.fontSizeMin=30;value.fontSizeMax=50;
            Bind(coinTexts!=null&&coinTexts.Length>0?coinTexts[0]:null,value);
            var gear=Control(Root,"Settings",916,76,106,115,settings!=null?settings:SourceButton(source,"Settings"));
            Art(gear,"Engrenagem",5,0,0,88,98);
        }
        void Bind(TMP_Text source,TMP_Text target){if(source!=null){target.text=source.text;TextBindings.Add((source,target));}}
        static Button SourceButton(Transform source,string name)=>source.Find(name)?.GetComponentInChildren<Button>(true);
        static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
        {
            var go=new GameObject(name,typeof(RectTransform));go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=new Vector2(.5f,.5f);
            r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(width,height);return r;
        }
        static RectTransform Control(Transform parent,string name,float x,float y,float width,float height,Button source)
        {
            var r=Rect(parent,name,x,y,width,height);var surface=r.gameObject.AddComponent<Image>();surface.color=Color.clear;
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=surface;button.transition=Selectable.Transition.None;
            button.navigation=new Navigation{mode=Navigation.Mode.None};if(source!=null)button.onClick.AddListener(()=>source.onClick.Invoke());return r;
        }
        void Art(Transform parent,string name,int index,float x,float y,float width,float height,bool fit=true)
        {
            var uv=kit.topHudRects[index];var texture=kit.topHudArt[index];
            if(fit){var aspect=texture.width*uv.z/(texture.height*uv.w);if(width/height>aspect)width=height*aspect;else height=width/aspect;}
            var r=Rect(parent,name,x,y,width,height);var image=r.gameObject.AddComponent<RawImage>();
            if(parent!=Root)r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);
            image.texture=texture;image.uvRect=new UnityEngine.Rect(uv.x,uv.y,uv.z,uv.w);image.raycastTarget=false;
        }
        TMP_Text Text(Transform parent,string name,string value,float x,float y,float width,float height,float size)
        {
            var r=Rect(parent,name,x,y,width,height);var text=r.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;
            r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);
            text.text=value;text.fontSize=size;text.enableAutoSizing=false;text.alignment=TextAlignmentOptions.Center;
            text.color=new Color(1,.94f,.79f);text.raycastTarget=false;text.enableVertexGradient=false;
            if(textMaterial==null&&text.fontSharedMaterial!=null)
            {
                textMaterial=new Material(text.fontSharedMaterial){name="Texto creme da HUD postal",hideFlags=HideFlags.HideAndDontSave};
                textMaterial.SetColor(ShaderUtilities.ID_FaceColor,Color.white);
                textMaterial.SetColor(ShaderUtilities.ID_OutlineColor,new Color(.36f,.12f,.075f));textMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth,.025f);
                textMaterial.EnableKeyword("UNDERLAY_ON");textMaterial.DisableKeyword("UNDERLAY_INNER");
                textMaterial.SetColor(ShaderUtilities.ID_UnderlayColor,new Color(.19f,.025f,.22f,.8f));
                textMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX,.08f);textMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY,-.24f);
                textMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness,.035f);
            }
            if(textMaterial!=null){text.fontSharedMaterial=textMaterial;text.UpdateMeshPadding();}return text;
        }
        public void Dispose(){if(textMaterial!=null){if(Application.isPlaying)Object.Destroy(textMaterial);else Object.DestroyImmediate(textMaterial);textMaterial=null;}}
    }

    public sealed class WorldMapPanelGraphic : MaskableGraphic
    {
        public bool bottom;
        int selectedIndex;
        public int SelectedIndex
        {
            get=>selectedIndex;
            set{value=Mathf.Clamp(value,0,4);if(selectedIndex==value)return;selectedIndex=value;SetVerticesDirty();}
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            if(bottom)
            {
                if(material!=null&&material.shader.name=="RoyalAves/UI/MapNavigation")
                {
                    material.SetVector("_PanelSize",new Vector4(r.width,r.height,0,0));material.SetFloat("_SelectedCell",selectedIndex);
                    Quad(vh,r.min,r.max,Color.white,Color.white);
                }
                else Navigation(vh,r);
                return;
            }
            var top=new Color(.89f,.98f,.94f);
            var low=new Color(.53f,.84f,.80f);
            for(var n=0;n<24;n++)
            {
                var t0=n/24f;var t1=(n+1)/24f;var y0=Mathf.Lerp(r.yMin,r.yMax,t0);var y1=Mathf.Lerp(r.yMin,r.yMax,t1);
                Quad(vh,new Vector2(r.xMin,y0),new Vector2(r.xMax,y1),Color.Lerp(low,top,t0),Color.Lerp(low,top,t1));
            }
            var edge=new Color(1,.93f,.72f,.9f);
            Quad(vh,new Vector2(r.xMin,r.yMin),new Vector2(r.xMax,r.yMin+5),edge,edge);
            for(var i=0;i<8;i++)
            {
                var x=r.xMin+r.width*i/7;var start=vh.currentVertCount;
                vh.AddVert(new Vector2(x-65,r.yMin),new Color(1,1,1,.10f),Vector2.zero);vh.AddVert(new Vector2(x+25,r.yMax),new Color(1,1,1,.10f),Vector2.zero);
                vh.AddVert(new Vector2(x+85,r.yMax),new Color(1,1,1,.10f),Vector2.zero);vh.AddVert(new Vector2(x-5,r.yMin),new Color(1,1,1,.10f),Vector2.zero);vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
            }
        }
        void Navigation(VertexHelper vh,Rect r)
        {
            const float rim=2,rail=9;
            var radius=Mathf.Min(80,r.height*.40f);
            // The rounded silhouette belongs to the panel mesh, so raised icons are not masked.
            Panel(vh,r,radius,r.xMin,r.xMax,r.yMin,r.yMax-rail,new Color(.57f,.16f,.86f),new Color(.76f,.38f,.97f));
            var inside=Rect.MinMaxRect(r.xMin+rim,r.yMin+rim,r.xMax-rim,r.yMax-rail);
            Panel(vh,inside,radius-rim,inside.xMin,inside.xMax,inside.yMin,inside.yMax,new Color(.275f,.065f,.47f),new Color(.60f,.20f,.75f));
            // Five touching cells. The active face shares the same outside corner silhouette.
            var cell=r.width/5;var left=r.xMin+cell*selectedIndex;var right=left+cell;
            Panel(vh,inside,radius-rim,left+1,right-1,inside.yMin,inside.yMax,new Color(.67f,.28f,.95f),new Color(.87f,.60f,1));
            Panel(vh,inside,radius-rim,left+3,right-3,inside.yMin+2,inside.yMax-2,new Color(.52f,.12f,.86f),new Color(.76f,.43f,.98f));
            // Confined satin highlight and a narrow recessed top edge; no outer glow.
            Panel(vh,inside,radius-rim,inside.xMin,inside.xMax,inside.yMax-18,inside.yMax-3,new Color(1,1,1,0),new Color(1,1,1,.065f));
            Panel(vh,inside,radius-rim,inside.xMin,inside.xMax,inside.yMax-3,inside.yMax,new Color(.20f,.035f,.31f,0),new Color(.20f,.035f,.31f,.27f));
            for(var i=1;i<5;i++)
            {
                var x=r.xMin+cell*i;
                Panel(vh,inside,radius-rim,x-1.1f,x+.7f,inside.yMin,inside.yMax,new Color(.19f,.035f,.32f,.63f),new Color(.25f,.06f,.39f,.68f));
                Panel(vh,inside,radius-rim,x+.7f,x+2,inside.yMin,inside.yMax,new Color(.80f,.44f,1,.20f),new Color(.90f,.65f,1,.32f));
            }
            // Continuous gold fillet: warm lower bevel, gold face, and a fine cream reflection.
            Quad(vh,new Vector2(r.xMin,r.yMax-rail),new Vector2(r.xMax,r.yMax-rail+1.5f),new Color(.48f,.20f,.04f),new Color(.77f,.35f,.035f));
            Quad(vh,new Vector2(r.xMin,r.yMax-rail+1.5f),new Vector2(r.xMax,r.yMax-2),new Color(1,.56f,.07f),new Color(1,.83f,.35f));
            Quad(vh,new Vector2(r.xMin,r.yMax-2),new Vector2(r.xMax,r.yMax-.7f),new Color(1,.91f,.54f),new Color(1,.97f,.73f));
            Quad(vh,new Vector2(r.xMin,r.yMax-.7f),new Vector2(r.xMax,r.yMax),new Color(1,.76f,.25f),new Color(.90f,.56f,.12f));
            for(var i=0;i<16;i++)
            {
                var t=i/16f;var intensity=Mathf.Pow(1-Mathf.Abs((i+.5f)/8-1),2)*.5f;
                var x=r.center.x+r.width*(-.13f+.26f*t);
                var shine=new Color(1,1,.90f,intensity);
                Quad(vh,new Vector2(x,r.yMax-3.2f),new Vector2(x+r.width*.26f/16,r.yMax-1),shine,shine);
            }
        }
        // Horizontal strips clip a fill to the two bottom quarter-circles. Cell fills, strokes and
        // highlights all use this same contour, with no masking component on the icon hierarchy.
        static void Panel(VertexHelper vh,Rect shape,float radius,float left,float right,float bottom,float top,Color low,Color high)
        {
            bottom=Mathf.Max(bottom,shape.yMin);top=Mathf.Min(top,shape.yMax);
            if(top<=bottom||right<=left)return;
            var steps=Mathf.Max(1,Mathf.CeilToInt((top-bottom)/2));
            for(var n=0;n<steps;n++)
            {
                var y0=Mathf.Lerp(bottom,top,n/(float)steps);var y1=Mathf.Lerp(bottom,top,(n+1)/(float)steps);
                var inset0=CornerInset(y0-shape.yMin,radius);var inset1=CornerInset(y1-shape.yMin,radius);
                var l0=Mathf.Max(left,shape.xMin+inset0);var h0=Mathf.Min(right,shape.xMax-inset0);
                var l1=Mathf.Max(left,shape.xMin+inset1);var h1=Mathf.Min(right,shape.xMax-inset1);
                if(h0<=l0&&h1<=l1)continue;
                l0=Mathf.Min(l0,h0);l1=Mathf.Min(l1,h1);
                var c0=Color.Lerp(low,high,n/(float)steps);var c1=Color.Lerp(low,high,(n+1)/(float)steps);var k=vh.currentVertCount;
                vh.AddVert(new Vector2(l0,y0),c0,Vector2.zero);vh.AddVert(new Vector2(l1,y1),c1,Vector2.up);
                vh.AddVert(new Vector2(h1,y1),c1,Vector2.one);vh.AddVert(new Vector2(h0,y0),c0,Vector2.right);
                vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
            }
        }
        static float CornerInset(float y,float radius)=>y>=radius?0:radius-Mathf.Sqrt(Mathf.Max(0,radius*radius-(radius-y)*(radius-y)));
        static void Quad(VertexHelper vh,Vector2 min,Vector2 max,Color low,Color high)
        {var i=vh.currentVertCount;vh.AddVert(min,low,Vector2.zero);vh.AddVert(new Vector2(min.x,max.y),high,Vector2.up);vh.AddVert(max,high,Vector2.one);vh.AddVert(new Vector2(max.x,min.y),low,Vector2.right);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
    }
}
