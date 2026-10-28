using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.Meta.WorldMap;

namespace RoyalAves.Meta
{
    // Shared by the actual lobby and the editor preview. No progression, wallet or construction logic lives here.
    public sealed class RoyalPostmanHome : MonoBehaviour
    {
        public const string HeroPath = "RoyalPostmanHome/courier-and-organizer-v1";
        public const float NavigationHeight = 186;
        public RectTransform Body { get; private set; }
        public RectTransform Navigation { get; private set; }
        public Button PlayButton { get; private set; }
        public RoyalPostmanPress PlayPress { get; private set; }
        public RectTransform GoldRim { get; private set; }
        public TMP_Text LevelText { get; private set; }
        public int SelectedTab { get; private set; } = 2;
        TMP_Text coinText,lifeCount,lifeState;
        TMP_FontAsset font;
        Material creamMaterial;
        RectTransform header,selection,underline;
        readonly Image[] icons = new Image[5];
        readonly TMP_Text[] labels = new TMP_Text[5];
        RawImage portrait;
        Image customPortrait;
        Rect lastSafeArea;
        Vector2Int lastScreen;

        public static RoyalPostmanHome Create(RectTransform parent, LobbyFeaturesConfig art, WorldMapKit kit,
            Action play, Action profile, Action coins, Action lives, Action settings, Action<int> navigate)
        {
            var root=Node(parent,"RoyalPostmanHome");Stretch(root);
            var home=root.gameObject.AddComponent<RoyalPostmanHome>();home.Body=root;
            home.font=art!=null?art.displayFont:TMP_Settings.defaultFontAsset;
            home.Build(art,kit,play,profile,coins,lives,settings,navigate);
            return home;
        }

        void Build(LobbyFeaturesConfig art,WorldMapKit kit,Action play,Action profile,Action coins,Action lives,Action settings,Action<int> navigate)
        {
            Surface(Body,"CharacterBackdrop",RoyalPostmanSurface.Finish.Backdrop,Vector2.zero,Vector2.zero,true);
            var heroRegion=Node(Body,"CharacterIllustration");Stretch(heroRegion);
            heroRegion.offsetMin=new Vector2(52,440);heroRegion.offsetMax=new Vector2(-52,-260);
            var ground=Surface(heroRegion,"SoftContactShadow",RoyalPostmanSurface.Finish.Shadow,new Vector2(0,14),new Vector2(870,95));
            ground.rectTransform.anchorMin=ground.rectTransform.anchorMax=new Vector2(.5f,0);
            var hero=heroRegion.gameObject.AddComponent<Image>();hero.sprite=Resources.Load<Sprite>(HeroPath);
            hero.preserveAspect=true;hero.raycastTarget=false;
            // Shadow must draw behind the transparent character image, not over their boots.
            ground.transform.SetParent(Body,false);ground.rectTransform.anchorMin=ground.rectTransform.anchorMax=new Vector2(.5f,.5f);
            ground.rectTransform.anchoredPosition=new Vector2(0,-470);ground.transform.SetSiblingIndex(heroRegion.GetSiblingIndex());

            header=Node(Body,"AccountHeader");header.anchorMin=new Vector2(0,1);header.anchorMax=Vector2.one;
            header.pivot=new Vector2(.5f,1);header.sizeDelta=new Vector2(0,180);header.anchoredPosition=new Vector2(0,-40);
            var avatar=Hit(header,"Profile",new Vector2(98,-87),new Vector2(142,142),profile);
            portrait=Node(avatar,"CourierPortrait").gameObject.AddComponent<RawImage>();Stretch(portrait.rectTransform);
            portrait.texture=Resources.Load<Texture2D>("MapHudV56/portrait");portrait.raycastTarget=false;
            customPortrait=Node(avatar,"ChosenAvatar").gameObject.AddComponent<Image>();Stretch(customPortrait.rectTransform);
            customPortrait.rectTransform.offsetMin=Vector2.one*18;customPortrait.rectTransform.offsetMax=Vector2.one*-18;
            customPortrait.preserveAspect=true;customPortrait.raycastTarget=false;customPortrait.gameObject.SetActive(false);
            var coinRoot=Counter(header,"Coins",new Vector2(377,-87),new Vector2(334,92),art!=null?art.coin:null,coins,out coinText);
            coinText.rectTransform.anchoredPosition=new Vector2(28,0);
            Plus(coinRoot,new Vector2(-119,-38),coinRoot.GetComponent<Button>().onClick.Invoke);
            var life=Counter(header,"Lives",new Vector2(760,-87),new Vector2(346,92),art!=null?art.heart:null,lives,out lifeState);
            lifeState.rectTransform.anchoredPosition=new Vector2(35,0);lifeState.fontSize=43;
            lifeCount=Text(life,"LifeCount","5",new Vector2(-117,0),new Vector2(86,82),52,true);
            Plus(life,new Vector2(-117,-38),life.GetComponent<Button>().onClick.Invoke);
            var gear=Hit(header,"Settings",new Vector2(1007,-87),new Vector2(101,101),settings);
            var gearFace=Surface(gear,"PlumButton",RoyalPostmanSurface.Finish.Blue,Vector2.zero,new Vector2(101,101));gearFace.radius=50;
            var gearShape=Surface(gear,"Gear",RoyalPostmanSurface.Finish.Gear,Vector2.zero,new Vector2(60,60));

            var playRoot=Hit(Body,"PlayLevel",new Vector2(0,NavigationHeight+151),new Vector2(710,182),play);
            playRoot.anchorMin=playRoot.anchorMax=new Vector2(.5f,0);
            Surface(playRoot,"FixedGreenWell",RoyalPostmanSurface.Finish.ButtonWell,new Vector2(0,-7),new Vector2(674,159));
            GoldRim=Surface(playRoot,"SeparateGoldRim",RoyalPostmanSurface.Finish.ButtonRim,Vector2.zero,new Vector2(710,182)).rectTransform;
            var face=Surface(playRoot,"SeparateMovingFace",RoyalPostmanSurface.Finish.ButtonFace,new Vector2(0,4),new Vector2(602,151)).rectTransform;
            var playLabel=Text(face,"PlayLabel","Jogar",new Vector2(-28,9),new Vector2(420,100),82,true);
            Surface(face,"PlayArrow",RoyalPostmanSurface.Finish.Arrow,new Vector2(213,8),new Vector2(32,43));
            LevelText=Text(face,"CurrentLevel","Nível 1",new Vector2(-10,-42),new Vector2(330,44),31,true);
            PlayButton=playRoot.GetComponent<Button>();PlayButton.transition=Selectable.Transition.None;
            PlayPress=playRoot.gameObject.AddComponent<RoyalPostmanPress>();PlayPress.face=face;PlayPress.travel=9;

            Navigation=Node(Body.parent,"RoyalPostmanNavigation");Navigation.anchorMin=Vector2.zero;Navigation.anchorMax=Vector2.right;
            Navigation.pivot=new Vector2(.5f,0);Navigation.sizeDelta=new Vector2(0,NavigationHeight);
            Surface(Navigation,"ContinuousRedAndGoldPanel",RoyalPostmanSurface.Finish.Navigation,Vector2.zero,Vector2.zero,true);
            selection=Surface(Navigation,"ActiveTab",RoyalPostmanSurface.Finish.Selection,Vector2.zero,Vector2.zero).rectTransform;
            selection.anchorMin=new Vector2(.4f,0);selection.anchorMax=new Vector2(.6f,1);selection.offsetMin=Vector2.zero;selection.offsetMax=new Vector2(0,-14);
            var names=new[]{"Eventos","Ranking","Início","Equipes","Coleção"};
            var sprites=kit!=null&&kit.mapNavIcons!=null&&kit.mapNavIcons.Length==5?kit.mapNavIcons:kit!=null?kit.navIcons:null;
            for(int i=0;i<5;i++)
            {
                int tab=i;var item=Hit(Navigation,"Tab_"+names[i],Vector2.zero,Vector2.zero,()=>navigate?.Invoke(tab));
                item.anchorMin=new Vector2(i/5f,0);item.anchorMax=new Vector2((i+1)/5f,1);item.offsetMin=item.offsetMax=Vector2.zero;
                var icon=Node(item,"ExistingIcon").gameObject.AddComponent<Image>();icons[i]=icon;
                icon.sprite=sprites!=null&&i<sprites.Length?sprites[i]:null;icon.preserveAspect=true;icon.raycastTarget=false;
                labels[i]=Text(item,"ActiveLabel",names[i],new Vector2(0,-49),new Vector2(210,55),34,true);
            }
            underline=Node(Navigation,"ActiveUnderline");var underlineImage=underline.gameObject.AddComponent<Image>();underlineImage.color=new Color32(255,241,192,255);underlineImage.raycastTarget=false;
            underline.anchorMin=underline.anchorMax=new Vector2(.5f,0);underline.sizeDelta=new Vector2(116,5);underline.anchoredPosition=new Vector2(0,9);
            SelectTab(2);ApplySafeArea();
        }

        RectTransform Counter(RectTransform parent,string name,Vector2 pos,Vector2 size,Sprite sprite,Action click,out TMP_Text value)
        {
            var root=Hit(parent,name,pos,size,click);Surface(root,"CreamInset",RoyalPostmanSurface.Finish.Capsule,Vector2.zero,size).radius=26;
            var icon=Node(root,"IndependentIcon").gameObject.AddComponent<Image>();icon.sprite=sprite;icon.preserveAspect=true;icon.raycastTarget=false;
            icon.rectTransform.sizeDelta=new Vector2(115,115);icon.rectTransform.anchoredPosition=new Vector2(-size.x*.5f+53,0);
            value=Text(root,"Value","0",new Vector2(30,0),new Vector2(size.x-120,74),48,false);
            value.enableAutoSizing=true;value.fontSizeMin=31;value.fontSizeMax=48;return root;
        }
        void Plus(RectTransform parent,Vector2 pos,Action click)
        {var p=Hit(parent,"Add",pos,new Vector2(43,43),click);Surface(p,"RedFace",RoyalPostmanSurface.Finish.Red,Vector2.zero,new Vector2(43,43)).radius=21;Text(p,"Plus","+",new Vector2(0,3),new Vector2(43,43),38,true);}

        public void Refresh(int coins,int lives,string state,int level,bool canPlay,Sprite avatar=null)
        {
            coinText.SetText(coins.ToString("N0",System.Globalization.CultureInfo.GetCultureInfo("pt-BR")));
            lifeCount.SetText(lives.ToString());lifeState.SetText(state);
            LevelText.SetText(canPlay?"Nível "+level:"Em breve");PlayButton.interactable=canPlay;
            customPortrait.gameObject.SetActive(avatar!=null);customPortrait.sprite=avatar;
        }
        public void ShowRoute(bool home,int tab)
        {Body.gameObject.SetActive(home);Navigation.gameObject.SetActive(true);SelectTab(tab);}
        public void SelectTab(int tab)
        {
            SelectedTab=Mathf.Clamp(tab,0,4);
            selection.anchorMin=new Vector2(SelectedTab/5f,0);selection.anchorMax=new Vector2((SelectedTab+1)/5f,1);selection.offsetMin=Vector2.zero;selection.offsetMax=new Vector2(0,-14);
            underline.anchorMin=underline.anchorMax=new Vector2((SelectedTab+.5f)/5f,0);
            for(int i=0;i<5;i++)
            {
                bool active=i==SelectedTab;labels[i].gameObject.SetActive(active);
                var r=icons[i].rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);
                r.sizeDelta=Vector2.one*(active?148:112);r.anchoredPosition=new Vector2(0,active?41:5);
            }
        }
        void Update(){if(lastScreen!=new Vector2Int(Screen.width,Screen.height)||lastSafeArea!=Screen.safeArea)ApplySafeArea();}
        void ApplySafeArea()
        {
            lastScreen=new Vector2Int(Screen.width,Screen.height);lastSafeArea=Screen.safeArea;
            if(Screen.width<1||Screen.height<1)return;
            float units=1080f/Screen.width,top=(Screen.height-Screen.safeArea.yMax)*units,bottom=Screen.safeArea.yMin*units;
            header.anchoredPosition=new Vector2(0,-Mathf.Max(28,top+14));
            Navigation.anchoredPosition=new Vector2(0,bottom);
            PlayButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,NavigationHeight+151+bottom);
        }
        TMP_Text Text(RectTransform parent,string name,string copy,Vector2 position,Vector2 size,float fontSize,bool cream)
        {
            var r=Node(parent,name);r.anchoredPosition=position;r.sizeDelta=size;
            var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=copy;t.fontSize=fontSize;t.alignment=TextAlignmentOptions.Center;
            t.color=cream?new Color32(255,246,210,255):new Color32(74,38,70,255);t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.NoWrap;
            if(cream&&font!=null)
            {
                if(creamMaterial==null)
                {
                    creamMaterial=new Material(font.material){name="RoyalPostman cream lettering",hideFlags=HideFlags.HideAndDontSave};
                    creamMaterial.SetColor(ShaderUtilities.ID_OutlineColor,new Color32(65,36,62,255));creamMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth,.10f);
                    creamMaterial.EnableKeyword("UNDERLAY_ON");creamMaterial.SetColor(ShaderUtilities.ID_UnderlayColor,new Color(0.18f,.10f,.20f,.55f));
                    creamMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY,-.3f);creamMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness,.12f);
                }
                t.fontSharedMaterial=creamMaterial;
            }
            return t;
        }
        static RectTransform Hit(RectTransform parent,string name,Vector2 pos,Vector2 size,Action action)
        {
            var r=Node(parent,name);r.anchoredPosition=pos;r.sizeDelta=size;
            // Header coordinates are measured from the top left; other controls use their parent's center.
            if(parent.name=="AccountHeader")r.anchorMin=r.anchorMax=new Vector2(0,1);
            var image=r.gameObject.AddComponent<Image>();image.color=Color.clear;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.transition=Selectable.Transition.None;
            if(action!=null)b.onClick.AddListener(()=>action());return r;
        }
        static RoyalPostmanSurface Surface(RectTransform parent,string name,RoyalPostmanSurface.Finish finish,Vector2 pos,Vector2 size,bool stretch=false)
        {var r=Node(parent,name);r.anchoredPosition=pos;r.sizeDelta=size;if(stretch)Stretch(r);r.gameObject.AddComponent<CanvasRenderer>();var g=r.gameObject.AddComponent<RoyalPostmanSurface>();g.finish=finish;g.raycastTarget=false;return g;}
        static RectTransform Node(Transform parent,string name)
        {var r=new GameObject(name,typeof(RectTransform)){layer=parent.gameObject.layer}.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);return r;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        void OnDestroy(){if(Navigation!=null)DisposeOwned(Navigation.gameObject);if(creamMaterial!=null)DisposeOwned(creamMaterial);}
        static void DisposeOwned(UnityEngine.Object item){if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
    }
}
