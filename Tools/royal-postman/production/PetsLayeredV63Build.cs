using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.UI.RoyalPostman2026;
using RoyalAves.Meta;
using RoyalAves.EditorTools;

public static class PetsLayeredV63Build
{
    const string Prefab="Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab";
    const string Art="Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsLayeredV63/";
    const string Output="../pets-layered-v63";
    static Font font;
    static Sprite round,box;
    static readonly Color Plum=new Color32(54,16,69,255),Cream=new Color32(255,242,209,255),Gray=new Color32(186,188,190,255);
    static readonly string[] Ids={"common","rare","epic","legendary"},Names={"Comum","Raro","Épico","Lendário"};
    static readonly Color[] Tints={new Color(.25f,1,1),new Color(.8f,.45f,1),new Color(1,.5f,.3f),new Color(1,.85f,.3f)};
    static void Check(bool b,string message){if(!b)throw new Exception(message);}
    static RectTransform Node(Transform parent,string name,float x,float y,float w,float h)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=Vector2.one*.5f;
        r.anchoredPosition=new Vector2(x-540,-y);r.sizeDelta=new Vector2(w,h);return r;
    }
    static RectTransform Full(Transform parent,string name)
    {
        var r=Node(parent,name,540,960,1080,1920);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;
    }
    static Image Image(RectTransform r,Sprite s,Color c){var i=r.gameObject.AddComponent<Image>();i.sprite=s;i.color=c;i.raycastTarget=false;return i;}
    static Text Label(Transform p,string name,string text,float x,float y,float w,float h,int size,Color c)
    {
        var t=Node(p,name,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.text=text;t.color=c;t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;return t;
    }
    static Sprite Sprite(Texture2D texture,string name,Rect rect,Vector4 border=default)
    {
        var sprite=UnityEngine.Sprite.Create(texture,rect,Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border);sprite.name=name;
        string file=Art+"Sprites/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Sprite>(file);
        if(old==null)AssetDatabase.CreateAsset(sprite,file);else{EditorUtility.CopySerialized(sprite,old);UnityEngine.Object.DestroyImmediate(sprite);sprite=old;}return sprite;
    }
    static Texture2D Texture(string id)
    {
        string path=Art+id+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.isReadable=true;importer.alphaIsTransparency=true;importer.maxTextureSize=8192;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Sprite Cropped(string id)
    {
        var t=Texture(id);var pixels=t.GetPixels32();int x0=t.width,y0=t.height,x1=0,y1=0;
        for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++)if(pixels[y*t.width+x].a>=160){x0=Math.Min(x0,x);x1=Math.Max(x1,x);y0=Math.Min(y0,y);y1=Math.Max(y1,y);}
        x0=Math.Max(0,x0-2);y0=Math.Max(0,y0-2);x1=Math.Min(t.width-1,x1+2);y1=Math.Min(t.height-1,y1+2);
        return Sprite(t,id,new Rect(x0,y0,x1-x0+1,y1-y0+1));
    }
    static Button Button(Transform p,string name,string text,float x,float y,float w,float h,bool circular)
    {
        var r=Node(p,name,x,y,w,h);var im=Image(r,circular?round:box,Gray);im.raycastTarget=true;im.type=circular?UnityEngine.UI.Image.Type.Simple:UnityEngine.UI.Image.Type.Sliced;
        var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;var colors=b.colors;colors.disabledColor=new Color(.62f,.62f,.62f,.8f);b.colors=colors;
        var moving=Full(r,"MovingCenter");Label(moving,"Label",text,w*.5f,h*.5f,w-6,h-6,text.Length>10?17:text.Length>3?24:48,Plum);
        // Full() is intended for full-screen authoring; child label uses this button's local size.
        moving.anchorMin=Vector2.zero;moving.anchorMax=Vector2.one;moving.offsetMin=moving.offsetMax=Vector2.zero;
        var label=moving.GetComponentInChildren<Text>();var lr=label.rectTransform;lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=lr.offsetMax=Vector2.zero;
        var press=r.gameObject.AddComponent<RoyalPostmanPaintedButton>();press.movingCenter=moving;press.pressScale=.94f;press.pressTravel=2;
        if(name=="CurrentTab"||name=="IncubatorTab"){var selected=Node(r,"Selected",540,2,w-12,4);selected.anchorMin=selected.anchorMax=new Vector2(.5f,0);selected.anchoredPosition=new Vector2(0,4);Image(selected,null,new Color(.1f,.85f,.9f));}
        return b;
    }
    static Image MachinePart(Transform master,Texture2D texture,string id,string part,float y,float height)
    {
        var r=Node(master,part,540,960,760,height/1536*1000);
        r.anchorMin=r.anchorMax=Vector2.one*.5f;r.anchoredPosition=new Vector2(0,((y+height*.5f)/1536-.5f)*1000);
        return Image(r,Sprite(texture,id+"-"+part,new Rect(0,y,texture.width,height)),Color.white);
    }
    static RectTransform EffectLayer(Transform p,string name)
    {
        var r=Node(p,name,540,960,460,380);r.anchorMin=r.anchorMax=Vector2.one*.5f;r.anchoredPosition=new Vector2(0,5);
        Image(r,box,Color.white);var mask=r.gameObject.AddComponent<Mask>();mask.showMaskGraphic=false;return r;
    }
    static void Build()
    {
        Directory.CreateDirectory(Art+"Sprites");AssetDatabase.Refresh();
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/RoyalAves/Fonts/LilitaOne-Regular.ttf");Check(font!=null,"Missing approved font");
        round=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");box=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");Check(round!=null&&box!=null,"Missing Unity placeholder sprites");
        var environment=Texture("environment");var scene=Sprite(environment,"environment",new Rect(0,0,environment.width,environment.height));
        var counter=Cropped("counter");var shelf=Cropped("shelf");var divider=Cropped("divider");var star=Cropped("particle-star");
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var v=root.GetComponent<RoyalPostmanPetsScreen>();
            var oldItems=v.items;var oldSlots=v.incubators;
            var eggs=new Sprite[4];for(int i=0;i<4;i++)eggs[i]=oldItems.First(a=>a.kind==(RoyalPostmanPetsScreen.InventoryItem.Kind)(i==0?1:i+2)).button.transform.Find("Icon").GetComponent<Image>().sprite;
            var stockItem=oldItems.First(a=>a.kind==RoyalPostmanPetsScreen.InventoryItem.Kind.Acceleration);
            var hourglass=stockItem.button.transform.Find("Icon").GetComponent<Image>().sprite;
            var oldCurrent=v.currentPanel;
            foreach(string path in new[]{"Room","Body","Inventory","Divider","Header","CurrentTab","IncubatorTab"}){var old=root.transform.Find(path);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            v.layeredIncubatorLayout=true;v.integratedMachineLayout=false;v.cartoonLayout=true;v.mobileCarousel=true;v.defaultCurrentTab=false;v.inventory=null;v.inventoryRows=Array.Empty<RoyalPostmanPetsScreen.InventoryRow>();
            v.currentTabBlue=v.currentTabGold=v.incubatorTabBlue=v.incubatorTabGold=null;
            var room=Full(root.transform,"Room");Image(room,scene,Color.white);room.SetSiblingIndex(root.transform.Find("Background").GetSiblingIndex()+1);
            var body=Full(root.transform,"Body");var workshop=Full(body,"Workshop");v.workshop=workshop.gameObject;
            Image(Node(workshop,"Counter",540,1030,1000,180),counter,Color.white);
            v.incubators=new RoyalPostmanPetsScreen.IncubatorView[4];
            for(int i=0;i<4;i++)
            {
                var slot=new RoyalPostmanPetsScreen.IncubatorView{unlockLevel=oldSlots[i].unlockLevel};v.incubators[i]=slot;
                var page=Full(workshop,"Incubator"+i);page.gameObject.AddComponent<CanvasGroup>();slot.root=page.gameObject;
                var master=Node(page,"Machine",520,550,760,1000);
                var texture=Texture("machine-"+Ids[i]);Check(texture.width==1024&&texture.height==1536,"Unexpected machine canvas");
                Check(texture.GetPixel(512,768).a<.01f,"Glass center not transparent: "+Ids[i]);
                slot.apparatus=MachinePart(master,texture,Ids[i],"RearFloor",435,110);
                var particles=master.gameObject.AddComponent<RoyalPostmanIncubatorParticles>();particles.tint=Tints[i];slot.particles=particles;
                particles.rearLayer=EffectLayer(master,"ParticlesBehind");
                var egg=Node(master,"Egg",540,960,300,340);egg.anchorMin=egg.anchorMax=Vector2.one*.5f;egg.anchoredPosition=new Vector2(0,-55);slot.egg=Image(egg,eggs[i],Color.white);slot.egg.preserveAspect=true;
                particles.frontLayer=EffectLayer(master,"ParticlesFront");
                var motes=new List<Image>();
                for(int n=0;n<14;n++){var parent=n<11?particles.rearLayer:particles.frontLayer;var r=Node(parent,"Mote"+n,540,960,n%3==0?24:13,n%3==0?24:13);r.anchorMin=r.anchorMax=Vector2.one*.5f;r.anchoredPosition=Vector2.zero;motes.Add(Image(r,star,Color.white));}
                particles.motes=motes.ToArray();particles.SetRunning(false);
                MachinePart(master,texture,Ids[i],"GlassFront",435,655);
                MachinePart(master,texture,Ids[i],"Lid",1090,446);
                MachinePart(master,texture,Ids[i],"BaseFront",0,435);
                slot.useItem=Button(page,"PawButton_PLACEHOLDER","Botão",520,920,194,194,true);
                slot.accelerate=Button(page,"Accelerate_PLACEHOLDER","",745,205,92,92,true);
                var accIcon=Image(Node(slot.accelerate.transform,"Icon",540,960,54,65),hourglass,Color.white);accIcon.rectTransform.anchorMin=accIcon.rectTransform.anchorMax=Vector2.one*.5f;accIcon.rectTransform.anchoredPosition=Vector2.zero;
                slot.accelerationCost=Label(page,"AccelerationCost","",745,272,180,35,23,Plum);
                var available=Full(page,"Available");slot.available=available.gameObject;
                slot.empty=Node(available,"Empty",540,1318,800,46).gameObject;
                slot.running=Node(available,"Running",540,1318,800,46).gameObject;
                slot.remaining=Label(page,"Status","Selecione um ovo",540,1318,800,46,34,Plum);
                var track=Node(available,"TimeBar_PLACEHOLDER",540,1230,810,76);Image(track,box,new Color(.68f,.69f,.7f));track.GetComponent<Image>().type=UnityEngine.UI.Image.Type.Sliced;
                var fill=Full(track,"Fill_PLACEHOLDER");slot.progressFill=Image(fill,box,new Color(.54f,.55f,.56f));slot.progressFill.type=UnityEngine.UI.Image.Type.Filled;slot.progressFill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;slot.progressFill.fillAmount=0;
                slot.progress=Label(track,"TimeRemaining","Duração · "+RoyalPostmanPetsScreen.FormatIncubationTime(v.incubationRules.EggSeconds(i)),540,960,790,66,34,Plum);
                slot.progress.rectTransform.anchorMin=Vector2.zero;slot.progress.rectTransform.anchorMax=Vector2.one;slot.progress.rectTransform.offsetMin=new Vector2(10,5);slot.progress.rectTransform.offsetMax=new Vector2(-10,-5);
                slot.locked=Node(page,"Locked",540,1304,880,80).gameObject;
                var lockedText=Label(slot.locked.transform,"Label","Disponível no nível "+slot.unlockLevel,540,960,860,70,32,Plum);lockedText.rectTransform.anchorMin=lockedText.rectTransform.anchorMax=Vector2.one*.5f;lockedText.rectTransform.anchoredPosition=Vector2.zero;
            }
            v.previousIncubator=Button(workshop,"PreviousIncubator","‹",160,570,112,112,true);v.nextIncubator=Button(workshop,"NextIncubator","›",880,570,112,112,true);
            var caption=Node(workshop,"RarityPlate",520,1070,420,64);Image(caption,box,Cream).type=UnityEngine.UI.Image.Type.Sliced;
            v.incubatorRarity=Label(workshop,"Rarity","Comum · 1/4",520,1070,410,60,37,Plum);
            var dividerRoot=Node(root.transform,"Divider",540,1132,1080,90);Image(dividerRoot,divider,Color.white);
            var title=Label(dividerRoot,"Title","Incubadora",540,960,380,70,42,Cream);title.rectTransform.anchorMin=title.rectTransform.anchorMax=Vector2.one*.5f;title.rectTransform.anchoredPosition=new Vector2(0,4);
            var inventory=Full(root.transform,"Inventory");Image(Node(inventory,"Shelf",540,1642,1040,125),shelf,Color.white);
            v.items=new RoyalPostmanPetsScreen.InventoryItem[5];
            for(int i=0;i<4;i++)
            {
                var old=oldItems.First(a=>a.kind==(RoyalPostmanPetsScreen.InventoryItem.Kind)(i==0?1:i+2));
                var item=new RoyalPostmanPetsScreen.InventoryItem{id=old.id,title=old.title,description=old.description,kind=old.kind};v.items[i]=item;
                float x=150+i*260;item.button=Button(inventory,"Item"+i,"",x,1525,226,255,false);item.button.targetGraphic.color=Color.clear;
                foreach(var child in item.button.GetComponentsInChildren<Text>())child.text="";
                var glow=Node(item.button.transform,"Selected",540,960,228,260);glow.anchorMin=glow.anchorMax=Vector2.one*.5f;glow.anchoredPosition=Vector2.zero;Image(glow,eggs[i],new Color(.3f,1,1,.55f)).preserveAspect=true;item.selection=glow.gameObject;
                var icon=Node(item.button.transform,"Icon",540,960,216,250);icon.anchorMin=icon.anchorMax=Vector2.one*.5f;icon.anchoredPosition=Vector2.zero;Image(icon,eggs[i],Color.white).preserveAspect=true;
                var tag=Node(inventory,"Tag"+i,x,1640,225,52);Image(tag,box,Cream).type=UnityEngine.UI.Image.Type.Sliced;
                Label(inventory,"Rarity"+i,Names[i],x,1640,215,48,31,Plum);
                item.quantity=Label(inventory,"Quantity"+i,"x0",x,1720,180,62,46,Plum);
            }
            var stock=new RoyalPostmanPetsScreen.InventoryItem{id=stockItem.id,title=stockItem.title,description=stockItem.description,kind=stockItem.kind,aggregateAcceleration=true,accelerationMinutes=stockItem.accelerationMinutes};v.items[4]=stock;
            stock.button=Button(inventory,"AccelerationStock","",540,1840,220,100,false);stock.button.targetGraphic.color=Color.clear;
            var stockIcon=Image(Node(stock.button.transform,"Icon",540,960,70,90),hourglass,Color.white);stockIcon.rectTransform.anchorMin=stockIcon.rectTransform.anchorMax=Vector2.one*.5f;stockIcon.rectTransform.anchoredPosition=new Vector2(-48,0);
            stock.quantity=Label(stock.button.transform,"Quantity","x0",540,960,120,70,44,Plum);stock.quantity.rectTransform.anchorMin=stock.quantity.rectTransform.anchorMax=Vector2.one*.5f;stock.quantity.rectTransform.anchoredPosition=new Vector2(60,0);
            stock.selection=Node(stock.button.transform,"Selection",540,960,1,1).gameObject;stock.selection.SetActive(false);
            var header=Full(root.transform,"Header");v.backButton=Button(header,"Back","‹",84,86,112,112,true);v.helpButton=Button(header,"Close","×",996,86,112,112,true);
            v.currentTab=Button(root.transform,"CurrentTab","Mascote",992,255,132,128,false);v.incubatorTab=Button(root.transform,"IncubatorTab","Incubadora",992,418,132,128,false);
            var outside=v.itemTooltip.transform.Find("Outside")?.GetComponent<RoyalPostmanTooltipSurface>();if(outside!=null)outside.passThrough=inventory;
            Check(v.currentPanel==oldCurrent,"Current profile replaced");
            v.itemTooltip.transform.SetAsLastSibling();v.helpPanel.transform.SetAsLastSibling();v.ShowCurrent(false);v.SelectIncubator(0);v.RefreshAvailability(MetaProgress.NextLevel);
            for(int i=0;i<4;i++)v.incubators[i].root.SetActive(i==0);
            PrefabUtility.SaveAsPrefabAsset(root,Prefab);AssetDatabase.SaveAssets();
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        try{Build();File.WriteAllText(Output+"/build.txt","PASS: layered art and editable placeholders built.\n");EditorApplication.Exit(0);}
        catch(Exception e){File.WriteAllText(Output+"/build.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}

