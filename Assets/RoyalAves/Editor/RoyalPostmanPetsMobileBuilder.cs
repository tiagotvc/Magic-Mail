using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.UI.RoyalPostman2026;

namespace RoyalAves.EditorTools
{
    /// <summary>Explicit migration of the incubation tab. Keeps the authored Current profile, header and ornament.</summary>
    public static class RoyalPostmanPetsMobileBuilder
    {
        static Font font;
        static readonly Color Cream=new Color32(250,248,255,255),Navy=new Color32(10,23,47,255);
        static readonly string[] Ids={"common","rare","epic","legendary"};
        static readonly string[] Rarities={"Comum","Rara","Épica","Lendária"};
        static readonly Color[] RarityColors={new Color(.65f,.88f,1),new Color(.84f,.57f,1),new Color(1,.42f,.38f),new Color(1,.78f,.17f)};
        const string Materials="Assets/RoyalAves/Art/UI/Menu/RoyalPostman/Pets/Materials";
        [MenuItem("Royal Postman/Mascotes/Aplicar incubadora mobile (substitui apenas Incubadora)")]
        public static void Build()
        {
            foreach(string id in Ids){RoyalPostmanPetsBuilder.Import("Menu","pets-incubator-"+id+"-v17");RoyalPostmanPetsBuilder.Import("Icon","pets-egg-"+id+"-v17");}
            foreach(string color in new[]{"purple","blue"}){string id="pets-action-"+color+"-v17";RoyalPostmanPetsBuilder.Import("Button",id);RefineButtonSprite(id);}
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/RoyalAves/Fonts/Pets/Nunito-ExtraBold.ttf");
            Directory.CreateDirectory(Materials);AssetDatabase.Refresh();
            var root=PrefabUtility.LoadPrefabContents(RoyalPostmanPetsBuilder.PrefabPath);
            try
            {
                var r=(RectTransform)root.transform;var view=root.GetComponent<RoyalPostmanPetsScreen>();
                foreach(string path in new[]{"Room","Body","Inventory"}){var old=r.Find(path);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
                view.mobileCarousel=true;view.inventory=null;view.inventoryRows=Array.Empty<RoyalPostmanPetsScreen.InventoryRow>();
                view.incubationRules.legendaryEggSeconds=172800;
                var room=Box(r,"Room",0,0,0,0);Stretch(room);room.offsetMin=new Vector2(0,300);room.offsetMax=new Vector2(0,-136);room.SetSiblingIndex(1);room.gameObject.AddComponent<RectMask2D>();
                var backdrop=Picture(room,"WorkshopBackground","pets-workshop-v5",0,0,1080,1484,false);
                var fit=backdrop.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=backdrop.sprite.rect.width/backdrop.sprite.rect.height;
                var blur=Material("workshop-focus-v17","RoyalPostman/UI/WorkshopFocus");blur.SetFloat("_BlurRadius",24);EditorUtility.SetDirty(blur);backdrop.material=blur;backdrop.color=new Color(.86f,.86f,.9f,1);
                var body=Box(r,"Body",0,0,0,0);Stretch(body);body.SetSiblingIndex(2);
                var workshop=Box(body,"Workshop",0,0,0,0);Stretch(workshop);view.workshop=workshop.gameObject;
                view.incubators=new RoyalPostmanPetsScreen.IncubatorView[4];
                for(int i=0;i<4;i++)view.incubators[i]=Incubator(workshop,i);
                var rarity=Panel(workshop,"Rarity",0,0,390,60);Bottom(rarity.rectTransform,0,805);
                view.incubatorRarity=Label(rarity.rectTransform,"Label","Comum   ·   1 / 4",0,0,360,52,34,RarityColors[0]);
                view.previousIncubator=Arrow(workshop,"PreviousIncubator",96,1140,true);
                view.nextIncubator=Arrow(workshop,"NextIncubator",984,1140,false);
                BuildInventory(r,view);
                var divider=(RectTransform)r.Find("Divider");Bottom(divider,0,300);divider.SetSiblingIndex(r.Find("Header").GetSiblingIndex());
                // Keep the approved ornament and all of its source sprites unchanged.
                var tooltip=view.tooltipCard;if(tooltip!=null){tooltip.sizeDelta=new Vector2(580,430);view.hintTitle.rectTransform.anchoredPosition=new Vector2(20,126);view.hintDescription.rectTransform.anchoredPosition=new Vector2(0,-40);view.hintDescription.rectTransform.sizeDelta=new Vector2(496,260);view.dismissItemTooltip.GetComponent<RectTransform>().anchoredPosition=new Vector2(247,150);}
                var copy=view.helpPanel.transform.Find("Card/Copy")?.GetComponent<Text>();if(copy!=null)copy.text="Use as setas para escolher a raridade.\nCada incubadora recebe seu próprio ovo.\n\nO ovo aparece somente após incubar.\nItens ou gold reduzem o tempo restante.\nLendária: 48 horas.";
                view.SelectIncubator(0);view.RefreshAvailability(RoyalAves.Meta.MetaProgress.NextLevel);view.ShowCurrent(view.defaultCurrentTab);
                PrefabUtility.SaveAsPrefabAsset(root,RoyalPostmanPetsBuilder.PrefabPath);AssetDatabase.SaveAssets();
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        [MenuItem("Royal Postman/Mascotes/Aumentar botões da incubadora")]
        public static void RefineActionSpacing()
        {
            foreach(string color in new[]{"purple","blue"})RefineButtonSprite("pets-action-"+color+"-v17");
            var root=PrefabUtility.LoadPrefabContents(RoyalPostmanPetsBuilder.PrefabPath);
            try{
                var view=root.GetComponent<RoyalPostmanPetsScreen>();
                foreach(var slot in view.incubators){
                    var panel=(RectTransform)slot.available.transform.parent;panel.sizeDelta=new Vector2(680,480);Bottom(panel,0,540);
                    ((RectTransform)slot.available.transform).sizeDelta=new Vector2(680,480);((RectTransform)slot.locked.transform).sizeDelta=new Vector2(680,480);
                    ((RectTransform)slot.progress.transform.parent).anchoredPosition=new Vector2(0,187);
                    ((RectTransform)slot.empty.transform).anchoredPosition=new Vector2(0,128);((RectTransform)slot.running.transform).anchoredPosition=new Vector2(0,128);
                    Bottom(slot.apparatus.rectTransform,0,1245);
                    Bottom(slot.egg.rectTransform,0,1165);
                    var buttons=new[]{slot.useItem,slot.accelerate};
                    for(int i=0;i<2;i++){
                        var button=buttons[i];var r=(RectTransform)button.transform;r.sizeDelta=new Vector2(600,140);r.anchoredPosition=new Vector2(0,i==0?15:-147);
                        var moving=(RectTransform)button.transform.Find("MovingCenter");moving.sizeDelta=new Vector2(600,140);
                        FitActionPaint(button.transform.Find("FixedGold").GetComponent<Image>(),600,140);FitActionPaint(moving.Find("Face").GetComponent<Image>(),600,140);
                        ((RectTransform)moving.Find("Icon")).sizeDelta=new Vector2(64,82);
                        var label=moving.Find("Label").GetComponent<Text>();label.rectTransform.sizeDelta=new Vector2(480,124);label.fontSize=42;
                    }
                }
                Bottom((RectTransform)view.incubatorRarity.transform.parent,0,805);
                PrefabUtility.SaveAsPrefabAsset(root,RoyalPostmanPetsBuilder.PrefabPath);AssetDatabase.SaveAssets();
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static RoyalPostmanPetsScreen.IncubatorView Incubator(RectTransform parent,int i)
        {
            var slot=Box(parent,"Incubator"+i,0,0,0,0);Stretch(slot);
            var egg=Picture(slot,"Egg","pets-egg-"+Ids[i]+"-v17",0,0,285,390,true);Bottom(egg.rectTransform,0,1165);egg.gameObject.SetActive(false);
            var apparatus=Picture(slot,"Apparatus","pets-incubator-"+Ids[i]+"-v17",0,0,720,990,true);Bottom(apparatus.rectTransform,0,1245);
            var panel=Panel(slot,"Panel",0,0,680,480);Bottom(panel.rectTransform,0,540);
            var available=Box(panel.rectTransform,"Available",0,0,680,480);
            var track=Panel(available,"ProgressTrack",0,187,570,46);track.color=new Color(.58f,.65f,.8f,1);
            var fill=Box(track.rectTransform,"Fill",0,0,550,28).gameObject.AddComponent<Image>();fill.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");fill.color=RarityColors[i];fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillAmount=0;fill.raycastTarget=false;
            var progress=Label(track.rectTransform,"Value","0 / 100",0,0,550,45,34,Cream);
            var empty=Box(available,"Empty",0,128,590,60);Label(empty,"Status","Vazia",0,0,580,60,37,Cream);
            var running=Box(available,"Running",0,128,590,46);var remaining=Label(running,"Time","",0,0,580,45,33,Cream);running.gameObject.SetActive(false);
            var incubate=Action(available,"UseItem","Incubar x0","purple",0,15,600,140,"pets-egg-"+Ids[i]+"-v17");
            var accelerate=Action(available,"Accelerate","Acelerar","blue",0,-147,600,140,"pets-hourglass-v2");
            var locked=Box(panel.rectTransform,"Locked",0,0,680,480);locked.gameObject.SetActive(false);
            Picture(locked,"Lock","lock-gray",0,75,70,80,true);
            Label(locked,"Level","Nv. "+(i==1?30:100),0,-5,600,70,52,Cream);
            Label(locked,"Unlock","Desbloqueia ovos "+(i==1?"raros":"épicos"),0,-90,600,100,33,RarityColors[i]);
            slot.gameObject.SetActive(i==0);
            return new RoyalPostmanPetsScreen.IncubatorView{root=slot.gameObject,apparatus=apparatus,unlockLevel=i==1?30:i==2?100:0,egg=egg,available=available.gameObject,empty=empty.gameObject,running=running.gameObject,locked=locked.gameObject,progress=progress,remaining=remaining,progressFill=fill,useItem=incubate,accelerate=accelerate};
        }
        static void BuildInventory(RectTransform root,RoyalPostmanPetsScreen view)
        {
            var inventory=Box(root,"Inventory",0,0,1080,300);Bottom(inventory,0,150);inventory.SetSiblingIndex(root.Find("Header").GetSiblingIndex());
            var background=Box(inventory,"Background",0,0,0,0);Stretch(background);var bg=background.gameObject.AddComponent<Image>();bg.color=Navy;bg.raycastTarget=true;background.gameObject.AddComponent<RoyalPostmanTooltipSurface>().owner=view;
            view.itemTooltip.transform.Find("Outside").GetComponent<RoyalPostmanTooltipSurface>().passThrough=inventory;
            view.items=new RoyalPostmanPetsScreen.InventoryItem[5];
            for(int i=0;i<5;i++)
            {
                string icon=i<4?"pets-egg-"+Ids[i]+"-v17":"pets-hourglass-v2";
                var card=Panel(inventory,"Item"+i,-412+i*206,-3,184,230);card.raycastTarget=true;
                var image=Picture(card.rectTransform,"Icon",icon,0,20,133,160,true);
                var quantity=Label(card.rectTransform,"Quantity","x0",0,-83,165,60,38,Cream);
                var selected=Box(card.rectTransform,"Selected",0,0,178,224).gameObject.AddComponent<Image>();selected.sprite=card.sprite;selected.type=Image.Type.Sliced;selected.pixelsPerUnitMultiplier=4;selected.color=new Color(1,.8f,.2f,.2f);selected.raycastTarget=false;selected.gameObject.SetActive(false);
                var button=card.gameObject.AddComponent<Button>();button.targetGraphic=card;button.transition=Selectable.Transition.ColorTint;
                var outline=card.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.6f,.78f,1,.8f);outline.effectDistance=Vector2.one*2;outline.enabled=false;card.gameObject.AddComponent<RoyalPostmanInventoryHover>().outline=outline;
                view.items[i]=new RoyalPostmanPetsScreen.InventoryItem{id=i<4?Ids[i]+"-egg":"accelerators",title=i<4?"Ovo "+new[]{"comum","raro","épico","lendário"}[i]:"Itens de aceleração",description=i<4?"Incubação: "+(i==0?"1 hora.":i==1?"3 horas.":i==2?"12 horas.":"48 horas."):"Reduz o tempo restante.",button=button,quantity=quantity,selection=selected.gameObject,kind=i==0?RoyalPostmanPetsScreen.InventoryItem.Kind.CommonEgg:i==1?RoyalPostmanPetsScreen.InventoryItem.Kind.RareEgg:i==2?RoyalPostmanPetsScreen.InventoryItem.Kind.EpicEgg:i==3?RoyalPostmanPetsScreen.InventoryItem.Kind.LegendaryEgg:RoyalPostmanPetsScreen.InventoryItem.Kind.Acceleration,aggregateAcceleration=i==4,accelerationMinutes=30};
            }
        }
        static Button Action(RectTransform parent,string name,string text,string color,float x,float y,float w,float h,string icon)
        {
            var r=Box(parent,name,x,y,w,h);var hit=r.gameObject.AddComponent<Image>();hit.color=Color.clear;
            var fixedPaint=Picture(r,"FixedGold","pets-action-"+color+"-v17",0,0,w,h);FitActionPaint(fixedPaint,w,h);
            var moving=Box(r,"MovingCenter",0,0,w,h);var face=Picture(moving,"Face","pets-action-"+color+"-v17",0,0,w,h);
            FitActionPaint(face,w,h);
            var ready=new[]{ButtonMaterial(color,1,false),ButtonMaterial(color,2,false)};
            var disabled=new[]{ready[0],ButtonMaterial(color,2,true)};
            fixedPaint.material=ready[0];face.material=ready[1];
            Picture(moving,"Icon",icon,-235,0,64,82,true);
            var label=Label(moving,"Label",text,25,0,w-120,h-16,42,Cream);label.resizeTextForBestFit=false;
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=face;button.transition=Selectable.Transition.ColorTint;
            var colors=button.colors;colors.disabledColor=new Color(.72f,.74f,.8f,1);button.colors=colors;
            var press=r.gameObject.AddComponent<RoyalPostmanPaintedButton>();press.movingCenter=moving;press.pressTravel=4;
            var visual=r.gameObject.AddComponent<RoyalPostmanActionVisual>();visual.paint=new[]{fixedPaint,face};visual.ready=ready;visual.disabled=disabled;
            return button;
        }
        static Material ButtonMaterial(string color,int part,bool disabled)
        {
            var material=Material("mobile-"+color+"-"+part+(disabled?"-disabled":"")+"-v17","RoyalPostman/UI/PetsActionPaint");
            material.SetFloat("_UsePalette",0);material.SetFloat("_ButtonPart",part);material.SetFloat("_Saturation",disabled?0:1);material.SetFloat("_ClipPill",1);
            var s=Sprite("pets-action-"+color+"-v17");var rect=s.rect;material.SetVector("_ButtonUVRect",new Vector4(rect.x/s.texture.width,rect.y/s.texture.height,rect.width/s.texture.width,rect.height/s.texture.height));EditorUtility.SetDirty(material);return material;
        }
        static void RefineButtonSprite(string id)
        {
            // Derive a native Sprite region from the connected opaque button; source PNG pixels remain untouched.
            var path=RoyalPostmanArtPaths.Texture(id);var read=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(read,File.ReadAllBytes(path));
            try{
                var pixels=read.GetPixels32();int w=read.width,h=read.height;var seen=new bool[pixels.Length];var queue=new int[pixels.Length];int largest=0,l=0,r=0,b=0,t=0;
                for(int seed=0;seed<pixels.Length;seed++){
                    if(seen[seed]||pixels[seed].a<192)continue;int head=0,tail=1;queue[0]=seed;seen[seed]=true;int x0=w,x1=0,y0=h,y1=0;
                    while(head<tail){int at=queue[head++],x=at%w,y=at/w;x0=Math.Min(x0,x);x1=Math.Max(x1,x);y0=Math.Min(y0,y);y1=Math.Max(y1,y);
                        foreach(int next in new[]{x>0?at-1:-1,x+1<w?at+1:-1,y>0?at-w:-1,y+1<h?at+w:-1})if(next>=0&&!seen[next]&&pixels[next].a>=192){seen[next]=true;queue[tail++]=next;}
                    }
                    if(tail>largest){largest=tail;l=x0;r=x1;b=y0;t=y1;}
                }
                var bounds=Rect.MinMaxRect(Math.Max(0,l-2),Math.Max(0,b-2),Math.Min(w,r+3),Math.Min(h,t+3));
                var target=Sprite(id);var sprite=UnityEngine.Sprite.Create(target.texture,bounds,Vector2.one*.5f,100,0,SpriteMeshType.FullRect,Vector4.one*(bounds.height*.5f-2));sprite.name=id;EditorUtility.CopySerialized(sprite,target);EditorUtility.SetDirty(target);UnityEngine.Object.DestroyImmediate(sprite);
            }finally{UnityEngine.Object.DestroyImmediate(read);}
        }
        static void FitActionPaint(Image image,float width,float height)
        {image.rectTransform.sizeDelta=new Vector2(width,height);image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=image.sprite.rect.height/height;}
        static Material Material(string id,string shader)
        {string path=Materials+"/"+id+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}return m;}
        static Button Arrow(RectTransform parent,string name,float x,float bottom,bool left)
        {var r=Box(parent,name,0,0,144,180);Bottom(r,x-540,bottom);var image=Picture(r,"Face","pets-page-arrow-v6",0,0,94,136,true);if(left)image.rectTransform.localScale=new Vector3(-1,1,1);var hit=r.gameObject.AddComponent<Image>();hit.color=Color.clear;var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;return button;}
        static Sprite Sprite(string id)=>AssetDatabase.LoadAssetAtPath<Sprite>(RoyalPostmanArtPaths.Sprite(id))??throw new InvalidOperationException("Missing mobile sprite: "+id);
        static RectTransform Box(RectTransform parent,string name,float x,float y,float w,float h)
        {var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static void Bottom(RectTransform r,float x,float y){r.anchorMin=r.anchorMax=new Vector2(.5f,0);r.anchoredPosition=new Vector2(x,y);}
        static Image Picture(RectTransform parent,string name,string sprite,float x,float y,float w,float h,bool aspect=false)
        {var image=Box(parent,name,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=Sprite(sprite);image.preserveAspect=aspect;image.raycastTarget=false;return image;}
        static Image Panel(RectTransform parent,string name,float x,float y,float w,float h)
        {var image=Picture(parent,name,"pets-panel-v4",x,y,w,h);image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=4;return image;}
        static Text Label(RectTransform parent,string name,string text,float x,float y,float w,float h,int size,Color color)
        {var t=Box(parent,name,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.color=color;t.alignment=TextAnchor.MiddleCenter;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;return t;}
    }
}

