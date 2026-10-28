using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.UI.RoyalPostman2026;

public static class HatcheryV121Build
{
    const string Prefab="Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab";
    const string Art="Assets/RoyalAves/Art/UI/Button/RoyalPostman/";
    const string Output="../hatchery-v121";
    static RectTransform Node(Transform parent,string name,Vector2 p,Vector2 size)
    {
        var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);Place(r,p,size);return r;
    }
    static void Place(RectTransform r,Vector2 p,Vector2 size){r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=p;r.sizeDelta=size;r.localScale=Vector3.one;}
    static void Top(RectTransform r,float x,float y,float w,float h){Place(r,new Vector2(x,-y),new Vector2(w,h));r.anchorMin=r.anchorMax=new Vector2(.5f,1);}
    static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=Vector2.one*.5f;r.offsetMin=r.offsetMax=Vector2.zero;r.localScale=Vector3.one;}
    static Text Label(Text source,Transform parent,string name,string text,Vector2 p,Vector2 size,int font)
    {
        var label=UnityEngine.Object.Instantiate(source,parent,false);label.name=name;Place(label.rectTransform,p,size);label.text=text;label.fontSize=font;label.resizeTextForBestFit=true;label.resizeTextMinSize=22;label.resizeTextMaxSize=font;label.raycastTarget=false;label.gameObject.SetActive(true);return label;
    }
    static Image Picture(Transform parent,string name,Vector2 p,Vector2 size,Sprite sprite)
    {
        var image=Node(parent,name,p,size).gameObject.AddComponent<Image>();image.sprite=sprite;image.raycastTarget=false;return image;
    }
    static RoyalPostmanBevelPanel Panel(Transform parent,string name,Vector2 p,Vector2 size,float radius=38)
    {
        var panel=Node(parent,name,p,size).gameObject.AddComponent<RoyalPostmanBevelPanel>();panel.radius=radius;panel.raycastTarget=false;return panel;
    }
    static Button Clickable(Graphic graphic){var b=graphic.gameObject.AddComponent<Button>();b.targetGraphic=graphic;b.transition=Selectable.Transition.None;graphic.raycastTarget=true;return b;}
    static Material Paint(string name,Color low,Color middle,Color high)
    {
        string folder=Art+"Materials/IncubatorV121";Directory.CreateDirectory(folder);string path=folder+"/"+name+".mat";
        var paint=AssetDatabase.LoadAssetAtPath<Material>(path);if(paint==null){paint=new Material(AssetDatabase.LoadAssetAtPath<Material>(Art+"Materials/red.mat"));paint.name=name;AssetDatabase.CreateAsset(paint,path);}
        paint.SetColor("_Low",low);paint.SetColor("_Mid",middle);paint.SetColor("_High",high);EditorUtility.SetDirty(paint);return paint;
    }
    static RoyalPostmanGlyph Glyph(Transform parent,string name,Vector2 p,Vector2 size,RoyalPostmanGlyph.Symbol symbol,Color tint)
    {var g=Node(parent,name,p,size).gameObject.AddComponent<RoyalPostmanGlyph>();g.symbol=symbol;g.color=tint;g.raycastTarget=false;return g;}
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        try
        {
            AssetDatabase.Refresh();var root=PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var view=root.GetComponent<RoyalPostmanPetsScreen>();var h=view.hatchery;
                if(h.presentation!=null)throw new Exception("V121 already assembled; use pristine backed-up source");
                var p=h.gameObject.AddComponent<RoyalPostmanIncubatorPresentation>();h.presentation=p;p.hatchery=h;
                root.transform.Find("HatcheryTabs").gameObject.SetActive(false);
                var green=Paint("IncubateGreen",new Color(.035f,.28f,.005f),new Color(.23f,.72f,.015f),new Color(.71f,1,.10f));
                var red=Paint("CancelRed",new Color(.49f,.005f,.02f),new Color(.92f,.015f,.045f),new Color(1,.29f,.34f));
                var blue=Paint("DrawerBlue",new Color(.005f,.035f,.19f),new Color(.005f,.21f,.68f),new Color(.04f,.65f,1));
                p.greenPaint=green;p.redPaint=red;
                p.paw=h.incubate.GetComponent<RoyalPostmanPaintedButton>().movingCenter.Find("Icon").GetComponent<Image>().sprite;
                p.cross=view.helpButton.GetComponent<RoyalPostmanPaintedButton>().movingCenter.Find("Icon").GetComponent<Image>().sprite;
                var faceSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Sprites/button-face-master.asset");var rimSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Sprites/button-frame-gold.asset");

                // The authored frame is exactly square. All of its interior layers fit this opening.
                var door=(RectTransform)h.transform.Find("Door");Top(door,0,780,730,730);
                var interior=(RectTransform)door.Find("InteriorMask");Place(interior,new Vector2(0,-5),new Vector2(610,610));
                Place(h.egg.rectTransform,new Vector2(0,-58),new Vector2(330,400));h.egg.preserveAspect=true;
                Place((RectTransform)interior.Find("CushionBack"),new Vector2(0,-211),new Vector2(500,152.38f));
                Place((RectTransform)interior.Find("CushionFront"),new Vector2(0,-261.29f),new Vector2(500,51.81f));
                Place(h.lockIcon.rectTransform,new Vector2(0,20),new Vector2(150,170));
                Place(h.spaceLabel.rectTransform,new Vector2(0,-110),new Vector2(490,70));h.spaceLabel.fontSize=38;h.spaceLabel.gameObject.SetActive(true);
                // Space names disappear when unlocked; pagination below the chamber explains the carousel.
                Top((RectTransform)h.transform.Find("PreviousSpace"),-465,780,136,136);Top((RectTransform)h.transform.Find("NextSpace"),465,780,136,136);

                var timer=(RectTransform)h.transform.Find("DurationDisplay");Top(timer,0,300,560,146);timer.GetComponent<Image>().enabled=false;
                var recess=Panel(timer,"Recess",Vector2.zero,new Vector2(560,146),25);recess.transform.SetAsFirstSibling();recess.bevel=8;recess.top=new Color(.005f,.018f,.043f);recess.bottom=new Color(.001f,.006f,.018f);recess.rim=new Color(.015f,.31f,.77f);recess.raycastTarget=false;
                Place(h.timeLabel.rectTransform,new Vector2(0,43),new Vector2(510,33));h.timeLabel.fontSize=28;h.timeLabel.resizeTextMaxSize=28;
                if(h.display.GetComponent<CanvasRenderer>()==null)h.display.gameObject.AddComponent<CanvasRenderer>();
                Place(h.display.rectTransform,new Vector2(0,-17),new Vector2(430,67));h.display.color=new Color(1,.81f,.37f);h.display.raycastTarget=false;
                var digitLight=h.display.gameObject.AddComponent<Outline>();digitLight.effectColor=new Color(1,.23f,.015f,.4f);digitLight.effectDistance=new Vector2(.75f,.75f);digitLight.useGraphicAlpha=true;
                var glass=Panel(timer,"GlassReflection",new Vector2(0,19),new Vector2(512,3),1);glass.top=glass.bottom=new Color(.22f,.54f,.82f,.18f);glass.rim=glass.top;glass.bevel=0;

                var pagination=Node(h.transform,"Spaces",Vector2.zero,new Vector2(230,64));Top(pagination,0,1207,230,64);p.pages=new Button[3];p.dots=new RoyalPostmanGlyph[3];
                for(int i=0;i<3;i++)
                {
                    var hit=Picture(pagination,"Space"+i,new Vector2((i-1)*75,0),new Vector2(68,64),null);hit.color=Color.clear;p.pages[i]=Clickable(hit);
                    Glyph(hit.transform,"Socket",Vector2.zero,new Vector2(48,48),RoyalPostmanGlyph.Symbol.PageDot,new Color(.005f,.025f,.11f));p.dots[i]=Glyph(hit.transform,"Light",Vector2.zero,new Vector2(35,35),RoyalPostmanGlyph.Symbol.PageDot,Color.cyan);
                }

                // Preserve the user's original round artwork, but remove every obsolete accelerator symbol.
                var accel=(RectTransform)h.accelerate.transform;float ratio=220f/accel.sizeDelta.x;
                foreach(var rect in accel.GetComponentsInChildren<RectTransform>(true))if(rect!=accel){rect.anchoredPosition*=ratio;rect.sizeDelta*=ratio;}
                accel.SetParent(interior,false);Place(accel,new Vector2(0,145),Vector2.one*220);accel.SetAsLastSibling();
                foreach(string obsolete in new[]{"ResourceBadge","Image","Quantity","PaintedCenter/Icon","PaintedCenter/FastForwardIcon"}){var old=accel.Find(obsolete);if(old!=null)old.gameObject.SetActive(false);}
                var clock=Node(h.accelerate.GetComponent<RoyalPostmanPaintedButton>().movingCenter,"ClockFace",new Vector2(0,0),Vector2.one*148).gameObject.AddComponent<RoyalPostmanClockFace>();clock.raycastTarget=false;
                h.accelerateQuantity.transform.SetParent(clock.transform,false);Place(h.accelerateQuantity.rectTransform,new Vector2(0,-37),new Vector2(114,50));
                foreach(var effect in h.accelerateQuantity.GetComponents<BaseMeshEffect>())UnityEngine.Object.DestroyImmediate(effect);
                h.accelerateQuantity.fontSize=43;h.accelerateQuantity.resizeTextMaxSize=43;h.accelerateQuantity.resizeTextMinSize=24;h.accelerateQuantity.color=new Color(.01f,.035f,.12f);h.accelerateQuantity.gameObject.SetActive(true);h.separateAcceleratorStock=true;
                Place(h.accelerateLabel.rectTransform,new Vector2(0,-130),new Vector2(300,52));h.accelerateLabel.fontSize=37;h.accelerateLabel.resizeTextMaxSize=37;
                Place(h.accelerateCost.rectTransform,new Vector2(0,-174),new Vector2(290,38));h.accelerateCost.fontSize=29;h.accelerateCost.resizeTextMaxSize=29;
                var visual=h.accelerate.GetComponent<RoyalPostmanActionVisual>();if(visual!=null)UnityEngine.Object.DestroyImmediate(visual);

                // Warm lamps and reflected washes are separate, masked, running-only layers.
                var lamps=Node(interior,"RunningLights",Vector2.zero,new Vector2(610,610));lamps.SetAsFirstSibling();p.lamps=lamps.gameObject;
                foreach(float side in new[]{-1f,1f})
                {
                    var wash=Node(lamps,"SideLightWash"+(side<0?"L":"R"),new Vector2(side*253,0),new Vector2(165,530)).gameObject.AddComponent<RoyalPostmanRevealLight>();wash.color=new Color(1,.9f,.29f,.9f);wash.raycastTarget=false;
                    var bulb=Panel(lamps,"Heater"+(side<0?"L":"R"),new Vector2(side*276,25),new Vector2(16,280),8);bulb.top=new Color(1,1,.84f);bulb.bottom=new Color(1,.72f,.11f);bulb.rim=new Color(1,1,.62f);bulb.bevel=3;
                }
                var ceiling=Panel(lamps,"CeilingLamp",new Vector2(0,264),new Vector2(95,15),7);ceiling.top=ceiling.bottom=new Color(1,1,.9f);ceiling.rim=new Color(1,.95f,.4f);ceiling.bevel=2;lamps.gameObject.SetActive(false);

                // One rectangular command replaces the separate cancel/start pair.
                h.incubate.gameObject.SetActive(false);h.cancel.gameObject.SetActive(false);
                var primary=Node(h.transform,"PrimaryAction",Vector2.zero,new Vector2(570,174));Top(primary,0,1370,570,174);
                var hitPrimary=primary.gameObject.AddComponent<Image>();hitPrimary.color=Color.clear;h.incubate=ClickableExisting(hitPrimary);
                var moving=Node(primary,"MovingCenter",Vector2.zero,new Vector2(546,150));p.primaryFace=Picture(moving,"Face",Vector2.zero,new Vector2(546,150),faceSprite);p.primaryFace.type=Image.Type.Sliced;p.primaryFace.pixelsPerUnitMultiplier=3.8f;p.primaryFace.material=green;
                var fixedGold=Picture(primary,"FixedGold",Vector2.zero,new Vector2(570,174),rimSprite);fixedGold.type=Image.Type.Sliced;fixedGold.pixelsPerUnitMultiplier=3.8f;fixedGold.transform.SetAsFirstSibling();
                var press=primary.gameObject.AddComponent<RoyalPostmanPaintedButton>();press.movingCenter=moving;press.pressTravel=4;press.pressScale=.98f;
                p.primaryIcon=Picture(moving,"CommandIcon",new Vector2(-177,2),new Vector2(88,88),p.paw);p.primaryIcon.preserveAspect=true;
                h.actionLabel.transform.SetParent(moving,false);Place(h.actionLabel.rectTransform,new Vector2(54,2),new Vector2(350,106));h.actionLabel.fontSize=68;h.actionLabel.resizeTextMaxSize=68;h.actionLabel.resizeTextMinSize=32;h.actionLabel.gameObject.SetActive(true);
                var fx=Node(h.transform,"IncubateParticles",Vector2.zero,new Vector2(720,250));Top(fx,0,1370,720,250);fx.SetSiblingIndex(primary.GetSiblingIndex());p.incubateGlow=fx.gameObject;
                var aura=fx.gameObject.AddComponent<RoyalPostmanRevealLight>();aura.color=new Color(1,.74f,.04f,.7f);aura.raycastTarget=false;
                p.actionParticles=Node(fx,"GoldMotes",Vector2.zero,new Vector2(720,250)).gameObject.AddComponent<RoyalPostmanRadialSparkles>();p.actionParticles.color=new Color(1,.87f,.25f);p.actionParticles.radius=350;p.actionParticles.spread=new Vector2(1,.28f);p.actionParticles.ambient=false;p.actionParticles.raycastTarget=false;
                foreach(var mote in h.particles.motes)if(mote!=null&&mote.sprite!=null){p.actionParticles.sprite=mote.sprite;break;}

                // Closed drawer has no egg art. Actual inventory is visible only inside its open sheet.
                var handle=Panel(h.transform,"EggDrawerHandle",Vector2.zero,new Vector2(1080,264),38);handle.rectTransform.anchorMin=handle.rectTransform.anchorMax=new Vector2(.5f,0);handle.rectTransform.anchoredPosition=new Vector2(0,132);handle.top=new Color(.015f,.23f,.76f);handle.bottom=new Color(.005f,.045f,.25f);handle.bevel=7;
                var handleFace=Picture(handle.transform,"HandleFace",new Vector2(0,-10),new Vector2(1000,186),faceSprite);handleFace.type=Image.Type.Sliced;handleFace.pixelsPerUnitMultiplier=3.5f;handleFace.material=blue;p.drawerHandle=Clickable(handleFace);
                p.handleLabel=Label(h.actionLabel,handleFace.transform,"Label","Ovos",Vector2.zero,new Vector2(700,115),80);
                var grip=Panel(handle.transform,"GoldGrip",new Vector2(0,118),new Vector2(270,24),12);grip.top=new Color(1,1,.55f);grip.bottom=new Color(1,.56f,.025f);grip.rim=new Color(1,.87f,.16f);grip.bevel=3;
                var chevron=h.accelerate.transform.Find("PaintedCenter/FastForwardIcon/ChevronLeft").GetComponent<Image>();var up=Picture(handleFace.transform,"OpenChevron",new Vector2(382,0),new Vector2(39,66),chevron.sprite);up.material=chevron.material;up.rectTransform.localRotation=Quaternion.Euler(0,0,90);
                var overlay=Node(h.transform,"EggDrawer",Vector2.zero,Vector2.zero);Stretch(overlay);p.drawerRoot=overlay.gameObject;
                var backdrop=Picture(overlay,"Backdrop",Vector2.zero,Vector2.zero,null);Stretch(backdrop.rectTransform);backdrop.color=new Color(0,.005f,.025f,.67f);p.drawerBackdrop=Clickable(backdrop);
                var sheet=Panel(overlay,"Sheet",Vector2.zero,new Vector2(1080,1060),64);sheet.rectTransform.anchorMin=sheet.rectTransform.anchorMax=new Vector2(.5f,0);sheet.rectTransform.pivot=new Vector2(.5f,0);sheet.rectTransform.anchoredPosition=Vector2.zero;sheet.top=new Color(.01f,.25f,.78f);sheet.bottom=new Color(.005f,.08f,.37f);sheet.bevel=9;sheet.raycastTarget=true;p.drawerSheet=sheet.rectTransform;
                var openGrip=UnityEngine.Object.Instantiate(grip,sheet.transform,false);openGrip.name="GoldGrip";openGrip.rectTransform.anchoredPosition=new Vector2(0,1046);
                p.drawerTitle=Label(h.actionLabel,sheet.transform,"Title","Escolha um ovo",new Vector2(-32,956),new Vector2(840,106),72);
                p.drawerClose=UnityEngine.Object.Instantiate(view.helpButton,sheet.transform,false);p.drawerClose.name="CloseDrawer";p.drawerClose.onClick=new Button.ButtonClickedEvent();var closeRect=(RectTransform)p.drawerClose.transform;float closeScale=112f/closeRect.sizeDelta.x;foreach(var r in closeRect.GetComponentsInChildren<RectTransform>(true))if(r!=closeRect){r.anchoredPosition*=closeScale;r.sizeDelta*=closeScale;}Place(closeRect,new Vector2(455,958),Vector2.one*112);p.drawerClose.gameObject.SetActive(true);
                p.cards=new RoyalPostmanBevelPanel[4];
                for(int i=0;i<4;i++)
                {
                    var item=view.items[i];var card=(RectTransform)item.button.transform;card.SetParent(sheet.transform,false);Place(card,new Vector2(i%2==0?-260:260,i<2?674:253),new Vector2(486,400));
                    var oldImage=card.GetComponent<Image>();if(oldImage!=null){oldImage.enabled=false;oldImage.raycastTarget=false;}
                    card.Find("Support").gameObject.SetActive(false);card.Find("Selected").gameObject.SetActive(false);
                    var plate=Panel(card,"CardFace",Vector2.zero,new Vector2(486,400),42);plate.transform.SetAsFirstSibling();plate.bevel=5;plate.top=new Color(.005f,.12f,.38f);plate.bottom=new Color(.002f,.035f,.16f);plate.rim=new Color(.015f,.39f,.83f);plate.raycastTarget=true;p.cards[i]=plate;item.button.targetGraphic=plate;item.button.transition=Selectable.Transition.None;
                    var icon=card.Find("Icon").GetComponent<Image>();Place(icon.rectTransform,new Vector2(0,-18),new Vector2(302,295));icon.preserveAspect=true;icon.color=Color.white;icon.raycastTarget=false;
                    Place(h.itemNames[i].rectTransform,new Vector2(-12,148),new Vector2(410,76));h.itemNames[i].fontSize=58;h.itemNames[i].resizeTextMaxSize=58;h.itemNames[i].resizeTextMinSize=32;
                    var qty=Panel(card,"Stock",new Vector2(171,-148),new Vector2(99,67),16);qty.bevel=4;qty.top=new Color(.01f,.10f,.27f);qty.bottom=new Color(.005f,.026f,.10f);qty.rim=new Color(.025f,.5f,.91f);
                    h.quantities[i].transform.SetParent(qty.transform,false);Place(h.quantities[i].rectTransform,Vector2.zero,new Vector2(94,64));h.quantities[i].fontSize=49;h.quantities[i].resizeTextMaxSize=49;h.quantities[i].resizeTextMinSize=26;
                    var selected=Glyph(card,"SelectedCheck",new Vector2(190,147),new Vector2(48,48),RoyalPostmanGlyph.Symbol.Check,new Color(1,.88f,.20f));h.selections[i]=selected.gameObject;item.selection=selected.gameObject;
                }
                foreach(RectTransform child in sheet.transform){child.anchorMin=child.anchorMax=new Vector2(.5f,0);}
                h.transform.Find("Footer").gameObject.SetActive(false);overlay.gameObject.SetActive(false);
                Top((RectTransform)h.legacy.transform,0,1510,760,54);h.legacyLabel.fontSize=32;h.legacyLabel.resizeTextMaxSize=32;
                h.Refresh();p.CloseDrawer();PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();File.Copy(Prefab,Output+"/assembled.prefab",true);File.WriteAllText(Output+"/build.txt","PASS: native drawer, square chamber, digital readout, authored clock body and contextual primary action assembled.\n");EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Output+"/build.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static Button ClickableExisting(Graphic image){var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;b.transition=Selectable.Transition.None;image.raycastTarget=true;return b;}
}
