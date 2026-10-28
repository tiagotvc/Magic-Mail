using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.UI.RoyalPostman2026;

public static class HatcheryV87Build
{
    public const string Prefab="Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab";
    public const string Art="Assets/RoyalAves/Art/UI/Menu/RoyalPostman/HatcheryV87/";
    public const string Output="../hatchery-v87";
    static Font font;
    public static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static RectTransform Node(Transform parent,string name,Vector2 position,Vector2 size,bool top=false)
    {
        var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=new Vector2(.5f,top?1:.5f);r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size;return r;
    }
    static Image Image(Transform parent,string name,Sprite sprite,Vector2 position,Vector2 size,bool top=false)
    {
        var r=Node(parent,name,position,size,top);r.gameObject.AddComponent<CanvasRenderer>();var i=r.gameObject.AddComponent<Image>();i.sprite=sprite;i.raycastTarget=false;return i;
    }
    static Text Label(Transform parent,string name,string value,Vector2 position,Vector2 size,int point,bool top=false,bool finish=true)
    {
        var r=Node(parent,name,position,size,top);r.gameObject.AddComponent<CanvasRenderer>();var t=r.gameObject.AddComponent<Text>();
        t.font=font;t.fontSize=point;t.alignment=TextAnchor.MiddleCenter;t.text=value;t.color=new Color(1,.97f,.86f);t.raycastTarget=false;
        t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;
        if(finish){r.gameObject.AddComponent<RoyalPostmanCreamText>();var s=r.gameObject.AddComponent<Shadow>();s.effectColor=new Color(.23f,.045f,.012f,1);s.effectDistance=new Vector2(0,-3);var o=r.gameObject.AddComponent<Outline>();o.effectColor=new Color(.20f,.035f,.012f,1);o.effectDistance=new Vector2(1.6f,-1.6f);}
        return t;
    }
    static Sprite Asset(string path)=>AssetDatabase.LoadAssetAtPath<Sprite>(path);
    static Sprite Sprite(string name,bool sliced=false)
    {
        var imp=(TextureImporter)AssetImporter.GetAtPath(Art+name+".png");imp.textureType=TextureImporterType.Default;imp.isReadable=true;imp.alphaIsTransparency=true;
        imp.maxTextureSize=8192;imp.npotScale=TextureImporterNPOTScale.None;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.crunchedCompression=false;
        imp.mipmapEnabled=true;imp.mipmapFilter=TextureImporterMipFilter.BoxFilter;imp.filterMode=FilterMode.Trilinear;imp.wrapMode=TextureWrapMode.Clamp;
        foreach(var p in new[]{"Standalone","Android","iPhone","WebGL"})imp.ClearPlatformTextureSettings(p);imp.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+name+".png");var pixels=texture.GetPixels32();int l=texture.width,b=texture.height,r=-1,t=-1;
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>=160){l=Math.Min(l,x);b=Math.Min(b,y);r=Math.Max(r,x);t=Math.Max(t,y);}
        Check(r>l&&t>b,"Empty original source: "+name);var rect=new Rect(l,b,r-l+1,t-b+1);var border=sliced?Vector4.one*Mathf.Min(150,rect.height*.24f):Vector4.zero;
        var sprite=UnityEngine.Sprite.Create(texture,rect,Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border);sprite.name=name;
        Directory.CreateDirectory(Art+"Sprites");AssetDatabase.Refresh();string file=Art+"Sprites/"+name+".asset";
        var existing=Asset(file);if(existing==null)AssetDatabase.CreateAsset(sprite,file);else{EditorUtility.CopySerialized(sprite,existing);UnityEngine.Object.DestroyImmediate(sprite);sprite=existing;}
        File.AppendAllText(Output+"/sprites.txt",name+" "+texture.width+"x"+texture.height+" native crop "+rect+"\n");return sprite;
    }
    static Sprite Front(Sprite original)
    {
        var rect=original.rect;rect.height*=.34f;var sprite=UnityEngine.Sprite.Create(original.texture,rect,Vector2.one*.5f,100,0,SpriteMeshType.FullRect);sprite.name="cradle-front";
        string file=Art+"Sprites/cradle-front.asset";var old=Asset(file);if(old==null)AssetDatabase.CreateAsset(sprite,file);else{EditorUtility.CopySerialized(sprite,old);UnityEngine.Object.DestroyImmediate(sprite);sprite=old;}return sprite;
    }
    static Button Round(Button template,Transform parent,string name,Vector2 position,float diameter,Sprite icon,bool mirror=false)
    {
        var go=UnityEngine.Object.Instantiate(template.gameObject,parent,false);go.name=name;go.SetActive(true);var r=(RectTransform)go.transform;
        r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=Vector2.one*diameter;r.localScale=Vector3.one;
        var button=go.GetComponent<Button>();var press=go.GetComponent<RoyalPostmanPaintedButton>();
        var image=press.movingCenter.Find("Icon").GetComponent<Image>();image.sprite=icon;image.preserveAspect=true;
        var ir=image.rectTransform;ir.anchorMin=ir.anchorMax=ir.pivot=Vector2.one*.5f;ir.anchoredPosition=new Vector2(-3.5f,1.5f);ir.sizeDelta=new Vector2(diameter*.52f,diameter*.52f);ir.localScale=new Vector3(mirror?-1:1,1,1);
        foreach(var i in go.GetComponentsInChildren<Image>(true))i.raycastTarget=false;var hit=button.GetComponent<Image>();hit.raycastTarget=true;hit.color=Color.clear;button.targetGraphic=hit;button.transition=Selectable.Transition.None;
        return button;
    }
    static Button Placeholder(Transform parent,string name,string title,Vector2 position)
    {
        var image=Image(parent,name,null,position,new Vector2(425,98),true);image.color=new Color(.53f,.53f,.53f);image.raycastTarget=true;
        var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;
        Label(image.transform,"Label",title,Vector2.zero,new Vector2(400,70),38,false,false);return button;
    }
    static Material Plinth(string name,Color shadow,Color mid,Color light)
    {
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/RoyalAves/Art/UI/Button/RoyalPostman/Shaders/RoyalPostmanNeutralPalette.shader");
        string file=Art+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(file);if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,file);}
        mat.SetFloat("_UseCustomPalette",1);mat.SetColor("_Color",Color.white);mat.SetColor("_PaletteShadow",shadow);mat.SetColor("_PaletteMid",mid);mat.SetColor("_PaletteHighlight",light);return mat;
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output);File.WriteAllText(Output+"/sprites.txt","");
        try
        {
            AssetDatabase.Refresh();font=AssetDatabase.LoadAssetAtPath<Font>("Assets/RoyalAves/Fonts/LilitaOne-Regular.ttf");Check(font!=null,"Game font missing");
            Sprite chamber=Sprite("chamber"),cradle=Sprite("cradle"),front=Front(cradle),alcove=Sprite("alcove"),plinth=Sprite("plinth"),display=Sprite("display",true);
            Sprite titlePT=Sprite("title-pt"),titleEN=Sprite("title-en"),petsPT=Sprite("pets-pt"),petsEN=Sprite("pets-en");
            var colors=new[]{Plinth("CommonPlinth",new Color(.01f,.18f,.22f),new Color(0,.62f,.69f),new Color(.25f,1,1)),Plinth("RarePlinth",new Color(.13f,.02f,.2f),new Color(.42f,.09f,.70f),new Color(.85f,.48f,1)),Plinth("EpicPlinth",new Color(.24f,0,.02f),new Color(.78f,.015f,.075f),new Color(1,.35f,.42f)),Plinth("LegendaryPlinth",new Color(.29f,.07f,0),new Color(.93f,.49f,0),new Color(1,.91f,.22f))};
            var root=PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var view=root.GetComponent<RoyalPostmanPetsScreen>();var round=view.backButton;
                var eggs=new Sprite[4];for(int i=0;i<4;i++)eggs[i]=view.items[i].button.transform.Find("Icon").GetComponent<Image>().sprite;
                foreach(var name in new[]{"Room","Body","Divider","Inventory","CurrentTab","IncubatorTab"})root.transform.Find(name).gameObject.SetActive(false);
                var main=Node(root.transform,"Hatchery",Vector2.zero,new Vector2(1080,1920));main.SetSiblingIndex(4);
                main.anchorMin=Vector2.zero;main.anchorMax=Vector2.one;main.sizeDelta=Vector2.zero;
                var h=main.gameObject.AddComponent<RoyalPostmanHatchery>();
                var bg=Node(main,"BlueWall",Vector2.zero,new Vector2(1080,1920));bg.anchorMin=Vector2.zero;bg.anchorMax=Vector2.one;bg.sizeDelta=Vector2.zero;var gradient=bg.gameObject.AddComponent<RoyalPostmanGradient>();gradient.top=new Color(.025f,.16f,.35f);gradient.bottom=new Color(.025f,.23f,.46f);gradient.raycastTarget=false;
                var door=Image(main,"Door",chamber,new Vector2(0,-695),new Vector2(660,610),true);door.preserveAspect=false;
                var inside=Node(door.transform,"InteriorMask",new Vector2(0,-10),new Vector2(550,475));inside.gameObject.AddComponent<RectMask2D>();
                var rear=Node(inside,"ParticlesRear",Vector2.zero,new Vector2(550,475));
                Image(inside,"CushionBack",cradle,new Vector2(0,-180),new Vector2(420,128));
                var egg=Image(inside,"Egg",eggs[0],new Vector2(0,-95),new Vector2(245,280));egg.preserveAspect=true;
                Image(inside,"CushionFront",front,new Vector2(0,-222.24f),new Vector2(420,43.52f));
                var forward=Node(inside,"ParticlesFront",Vector2.zero,new Vector2(550,475));
                var particle=door.gameObject.AddComponent<RoyalPostmanIncubatorParticles>();particle.rearLayer=rear;particle.frontLayer=forward;particle.tint=new Color(1,.79f,.23f);
                var star=Asset("Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsLayeredV63/Sprites/particle-star.asset");var motes=new List<Image>();
                for(int i=0;i<6;i++)motes.Add(Image(i<4?rear:forward,"Mote"+i,star,Vector2.zero,Vector2.one*(i%2==0?23:15)));particle.motes=motes.ToArray();particle.SetRunning(false);
                var lockImage=Image(inside,"Lock",Asset("Assets/RoyalAves/Art/UI/Icon/RoyalPostman/Sprites/lock-gray.asset"),new Vector2(0,-15),new Vector2(120,140));lockImage.material=colors[3];lockImage.preserveAspect=true;
                var status=Label(inside,"SpaceStatus","Espaço 1",new Vector2(0,185),new Vector2(480,44),27);
                var arrow=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/UI/UI assets/seta_voltar_creme_contorno.png");
                var previous=Round(round,main,"PreviousSpace",new Vector2(-435,-695),128,arrow);
                var next=Round(round,main,"NextSpace",new Vector2(435,-695),128,arrow,true);
                var paw=Round(round,main,"Incubate",new Vector2(0,-1138),216,Asset("Assets/RoyalAves/Art/UI/Icon/RoyalPostman/Sprites/paw-cream.asset"));
                var caption=Label(main,"ActionLabel","Incubar",new Vector2(0,-1300),new Vector2(600,66),57,true);
                var duration=Image(main,"DurationDisplay",display,new Vector2(-165,-1440),new Vector2(575,165),true);duration.type=UnityEngine.UI.Image.Type.Sliced;duration.pixelsPerUnitMultiplier=display.rect.height/165f;
                var timeLabel=Label(duration.transform,"Label","Duração",new Vector2(0,48),new Vector2(520,40),29);
                var digits=Node(duration.transform,"DigitalTime",new Vector2(0,-19),new Vector2(468,70));var clock=digits.gameObject.AddComponent<RoyalPostmanDigitalClock>();clock.color=new Color(.60f,.96f,1);clock.raycastTarget=false;
                var speed=Image(main,"AcceleratorDisplay",display,new Vector2(330,-1440),new Vector2(305,165),true);speed.type=UnityEngine.UI.Image.Type.Sliced;speed.pixelsPerUnitMultiplier=display.rect.height/165f;speed.raycastTarget=true;
                var speedButton=speed.gameObject.AddComponent<Button>();speedButton.targetGraphic=speed;speedButton.transition=Selectable.Transition.None;
                var speedLabel=Label(speed.transform,"Label","Acelerar",new Vector2(0,48),new Vector2(280,40),29);
                var hourglass=Image(speed.transform,"Icon",Asset("Assets/RoyalAves/Art/UI/Icon/RoyalPostman/Pets/Sprites/pets-hourglass-v2.asset"),new Vector2(-75,-20),new Vector2(80,100));hourglass.preserveAspect=true;
                var speedQuantity=Label(speed.transform,"Quantity","x0",new Vector2(55,-22),new Vector2(170,60),46);speedQuantity.resizeTextForBestFit=true;speedQuantity.resizeTextMinSize=22;speedQuantity.resizeTextMaxSize=46;
                var legacyRoot=Image(main,"LegacySavedIncubation",null,new Vector2(0,-1570),new Vector2(700,45),true);legacyRoot.color=Color.clear;legacyRoot.raycastTarget=true;
                var legacyButton=legacyRoot.gameObject.AddComponent<Button>();legacyButton.targetGraphic=legacyRoot;
                var legacyLabel=Label(legacyRoot.transform,"Label","Incubação anterior preservada",Vector2.zero,new Vector2(690,44),26);
                var footer=Image(main,"Footer",alcove,Vector2.zero,new Vector2(1080,282));var fr=footer.rectTransform;fr.anchorMin=fr.anchorMax=new Vector2(.5f,0);fr.anchoredPosition=new Vector2(0,141);
                var itemNames=new Text[4];var quantities=new Text[4];var selections=new GameObject[4];var items=new RoyalPostmanPetsScreen.InventoryItem[4];
                var glow=Asset("Assets/RoyalAves/Art/UI/Icon/RoyalPostman/PetsCartoon/Sprites/pets-cartoon-v24-egg-glow.asset");
                for(int i=0;i<4;i++)
                {
                    float x=-405+i*270;var hit=Image(footer.transform,"Egg"+i,null,new Vector2(x,0),new Vector2(248,244));hit.color=Color.clear;hit.raycastTarget=true;
                    var button=hit.gameObject.AddComponent<Button>();button.targetGraphic=hit;button.transition=Selectable.Transition.None;
                    var selected=Image(hit.transform,"Selected",glow,new Vector2(0,20),new Vector2(165,170));selected.color=new Color(.15f,.9f,1,.75f);selections[i]=selected.gameObject;
                    var icon=Image(hit.transform,"Icon",eggs[i],new Vector2(0,30),new Vector2(90,110));icon.preserveAspect=true;
                    var support=Image(hit.transform,"Support",plinth,new Vector2(0,-55),new Vector2(205,66));support.material=colors[i];
                    itemNames[i]=Label(hit.transform,"Name",new[]{"Comum","Raro","Épico","Lendário"}[i],new Vector2(0,-53),new Vector2(235,40),30);
                    quantities[i]=Label(hit.transform,"Quantity","x0",new Vector2(0,-108),new Vector2(230,46),40);
                    var old=view.items[i];items[i]=new RoyalPostmanPetsScreen.InventoryItem{id=old.id,title=old.title,description=old.description,kind=old.kind,button=button,quantity=quantities[i],selection=selected.gameObject};
                }
                var tabs=Node(root.transform,"HatcheryTabs",Vector2.zero,new Vector2(1080,1920));tabs.anchorMin=Vector2.zero;tabs.anchorMax=Vector2.one;tabs.sizeDelta=Vector2.zero;tabs.SetSiblingIndex(root.transform.Find("Header").GetSiblingIndex()+1);
                var petsTab=Placeholder(tabs,"Mascotes_PLACEHOLDER","Mascotes",new Vector2(-225,-290));
                var hatcheryTab=Placeholder(tabs,"Incubadora_PLACEHOLDER","Incubadora",new Vector2(225,-290));
                var title=root.GetComponentInChildren<RoyalPostmanHeaderTitle>(true);title.portuguese=titlePT;title.english=titleEN;title.petsPortuguese=petsPT;title.petsEnglish=petsEN;title.titleHeight=108;title.SetPetsSection(false);
                h.egg=egg;h.eggs=eggs;h.lockIcon=lockImage;h.incubate=paw;h.accelerate=speedButton;h.legacy=legacyButton;h.actionLabel=caption;h.timeLabel=timeLabel;h.accelerateLabel=speedLabel;h.accelerateQuantity=speedQuantity;h.spaceLabel=status;h.legacyLabel=legacyLabel;
                h.itemNames=itemNames;h.quantities=quantities;h.selections=selections;h.display=clock;h.particles=particle;h.headerTitle=title;h.unlockLevels=new[]{30,50,100};h.owner=view;h.Refresh();
                view.hatchery=h;view.workshop=main.gameObject;view.items=items;view.currentTab=petsTab;view.incubatorTab=hatcheryTab;view.previousIncubator=previous;view.nextIncubator=next;view.defaultCurrentTab=false;
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);
                var headerCopy=UnityEngine.Object.Instantiate(root.transform.Find("Header").gameObject);headerCopy.name="HatcheryHeaderV86";
                try{PrefabUtility.SaveAsPrefabAsset(headerCopy,"Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Components/HatcheryHeaderV86.prefab");}finally{UnityEngine.Object.DestroyImmediate(headerCopy);}
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();File.Copy(Prefab,Output+"/assembled.prefab",true);
            File.WriteAllText(Output+"/build.txt","PASS: blue hatchery assembled,9 original PNGs/native sprites, all round controls reuse user PNGs,2 plain rectangular placeholders, layered egg/cushion/particles, seven-segment time, paired acceleration module, compact4-egg alcove, straight cream/brown titles PT/EN.\n");EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Output+"/build.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
