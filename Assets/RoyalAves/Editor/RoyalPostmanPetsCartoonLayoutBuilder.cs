using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.UI.RoyalPostman2026;

namespace RoyalAves.EditorTools
{
    public static partial class RoyalPostmanPetsCartoonBuilder
    {
        static Button stockButton;
        static Text stockQuantity;
        static GameObject stockSelection;

        static void BuildWorkshop(RectTransform root,RoyalPostmanPetsScreen view,int[] unlocks)
        {
            var room=Box(root,"Room",0,0,0,0);Stretch(room);room.offsetMin=new Vector2(0,430);room.SetSiblingIndex(1);room.gameObject.AddComponent<RectMask2D>();
            var backdrop=Picture(room,"WorkshopBackground",S("workshop"),0,0,1080,1440);
            var fitter=room.gameObject.AddComponent<RoyalPostmanCartoonBackdrop>();fitter.artwork=backdrop.rectTransform;fitter.aspect=backdrop.sprite.rect.width/backdrop.sprite.rect.height;fitter.tableHeightFromBottom=580;fitter.Fit();
            var body=Box(root,"Body",0,0,0,0);Stretch(body);body.SetSiblingIndex(2);
            var workshop=Box(body,"Workshop",0,0,0,0);Stretch(workshop);view.workshop=workshop.gameObject;
            var panel=Picture(workshop,"ControlPanel",S("control-panel"),0,0,1040,400);Bottom(panel.rectTransform,0,730);panel.type=Image.Type.Sliced;panel.pixelsPerUnitMultiplier=2;
            view.incubators=new RoyalPostmanPetsScreen.IncubatorView[4];
            for(int i=0;i<4;i++)view.incubators[i]=BuildIncubator(workshop,i,i<unlocks.Length?unlocks[i]:0);
            Text rarity;var plaque=Plaque(workshop,"Rarity","action-purple","Comum",0,0,300,76,44,out rarity);Bottom(plaque,0,975);view.incubatorRarity=rarity;
            view.previousIncubator=HeroArrow(workshop,"PreviousIncubator",-440,true);
            view.nextIncubator=HeroArrow(workshop,"NextIncubator",440,false);
            var stock=Box(workshop,"AcceleratorStock",0,0,230,126);Top(stock,-395,-292);
            var hit=stock.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            Picture(stock,"Icon",S("hourglass"),-61,0,68,106,true);
            stockQuantity=Label(stock,"Quantity","x0",27,-4,125,80,57);
            stockButton=stock.gameObject.AddComponent<Button>();stockButton.targetGraphic=hit;stockButton.transition=Selectable.Transition.None;
            stockSelection=Box(stock,"Selected",0,0,0,0).gameObject;stockSelection.SetActive(false);
        }

        static RoyalPostmanPetsScreen.IncubatorView BuildIncubator(RectTransform workshop,int i,int unlock)
        {
            var slot=Box(workshop,"Incubator"+i,0,0,0,0);Stretch(slot);
            var egg=Picture(slot,"Egg",S("egg-"+Ids[i]),0,0,218,285,true);Bottom(egg.rectTransform,0,1315);egg.gameObject.SetActive(false);
            var apparatus=Picture(slot,"Apparatus",S("incubator-"+Ids[i]),0,0,550,700,true);Bottom(apparatus.rectTransform,0,1360);
            var panel=Box(slot,"Panel",0,0,1040,400);Bottom(panel,0,730);
            var available=Box(panel,"Available",0,0,1040,400);
            var track=Face(available,"ProgressTrack",Paint("action-teal-symmetric"),0,123,730,64,32,6);track.color=new Color(.14f,.28f,.38f,1);
            var clip=Box(track.rectTransform,"ProgressClip",-353,0,706,40);clip.pivot=new Vector2(0,.5f);clip.gameObject.AddComponent<RectMask2D>();
            Face(clip,"Paint",Paint("action-teal-symmetric"),353,0,706,40,20,3);
            var source=Box(available,"ProgressValue",0,0,0,0).gameObject.AddComponent<Image>();source.color=Color.clear;source.raycastTarget=false;source.type=Image.Type.Filled;source.fillMethod=Image.FillMethod.Horizontal;source.fillAmount=0;
            var progressClip=source.gameObject.AddComponent<RoyalPostmanProgressClip>();progressClip.valueSource=source;progressClip.clipping=clip;progressClip.fullWidth=706;progressClip.Refresh();
            var progress=Label(track.rectTransform,"Value","0 / 100",0,0,680,58,40);
            var empty=Box(available,"Empty",0,0,0,0);
            var running=Box(available,"Running",0,72,700,30);var remaining=Label(running,"Time","",0,0,690,30,27,false);running.gameObject.SetActive(false);
            Text eggCount,accCount;
            var incubate=MainAction(available,"UseItem","action-teal-symmetric","Incubar",S("egg-"+Ids[i]),-230,out eggCount);
            var accelerate=MainAction(available,"Accelerate","action-purple","Acelerar",S("hourglass"),230,out accCount);
            var cost=Label(available,"AccelerationCost","",230,-164,410,34,27,false);
            var locked=Box(panel,"Locked",0,0,1040,400);locked.gameObject.SetActive(false);
            Picture(locked,"Lock",Existing("lock-gray"),0,70,76,88,true);
            Label(locked,"Level","Nv. "+unlock,0,-15,850,70,55,false);
            Label(locked,"Unlock","Desbloqueia ovos "+new[]{"comuns","raros","épicos","lendários"}[i],0,-100,930,55,38,false);
            slot.gameObject.SetActive(i==0);
            return new RoyalPostmanPetsScreen.IncubatorView{root=slot.gameObject,apparatus=apparatus,egg=egg,unlockLevel=unlock,available=available.gameObject,empty=empty.gameObject,running=running.gameObject,locked=locked.gameObject,progress=progress,remaining=remaining,progressFill=source,useItem=incubate,accelerate=accelerate,actionQuantity=eggCount,accelerationQuantity=accCount,accelerationCost=cost};
        }

        static Button MainAction(RectTransform parent,string name,string paint,string title,Sprite icon,float x,out Text quantity)
        {
            RectTransform moving;var button=PaintedControl(parent,name,paint,x,-40,430,180,54,true,out moving);
            Label(moving,"Label",title,-46,1,276,100,68);
            Picture(moving,"Icon",icon,120,2,36,44,true);
            quantity=Label(moving,"Quantity","x0",154,-3,60,44,34);
            quantity.resizeTextForBestFit=true;quantity.resizeTextMinSize=22;quantity.resizeTextMaxSize=34;
            return button;
        }
        static Button HeroArrow(RectTransform parent,string name,float x,bool left)
        {
            var r=Box(parent,name,0,0,140,184);Bottom(r,x,1285);
            var image=Picture(r,"Face",Existing("pets-page-arrow-v6"),0,0,110,156,true);if(left)image.rectTransform.localScale=new Vector3(-1,1,1);
            var hit=r.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;return button;
        }
        static Material PedestalMaterial(int i)
        {
            string path=MaterialFolder+"/pets-cartoon-v24-pedestal-"+i+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(Shader.Find("RoyalPostman/UI/PetsActionPaint"));AssetDatabase.CreateAsset(material,path);}
            material.SetFloat("_UsePalette",0);material.SetFloat("_RecolorPurple",i==1?0:1);material.SetColor("_TargetColor",RarityColors[i]);EditorUtility.SetDirty(material);return material;
        }
        static void BuildShelf(RectTransform root,RoyalPostmanPetsScreen view)
        {
            var inventory=Box(root,"Inventory",0,0,1080,430);Bottom(inventory,0,215);inventory.SetSiblingIndex(3);
            var background=Picture(inventory,"Background",S("egg-shelf"),0,0,1080,430);Stretch(background.rectTransform);background.type=Image.Type.Sliced;background.pixelsPerUnitMultiplier=2;background.raycastTarget=true;background.gameObject.AddComponent<RoyalPostmanTooltipSurface>().owner=view;
            view.items=new RoyalPostmanPetsScreen.InventoryItem[5];
            var lights=new Image[4];
            for(int i=0;i<4;i++)
            {
                var card=Box(inventory,"Item"+i,-405+i*270,0,250,350);
                var hit=card.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
                lights[i]=Picture(card,"Glow",S("egg-glow"),0,51,240,310,true);
                var pedestal=Picture(card,"Pedestal",S("pedestal"),0,-83,230,80,true);pedestal.material=PedestalMaterial(i);
                Picture(card,"Icon",S("egg-"+Ids[i]),0,48,190,245,true);
                var selected=Face(card,"Selected",Paint("action-rim"),0,-83,226,78,23,2,false,true);selected.color=new Color(.7f,1,1,.9f);selected.gameObject.SetActive(false);
                Label(card,"Rarity",Names[i],0,-90,215,46,33);
                var quantity=Label(card,"Quantity","x0",0,-145,210,48,43);
                var button=card.gameObject.AddComponent<Button>();button.targetGraphic=hit;button.transition=Selectable.Transition.None;
                view.items[i]=new RoyalPostmanPetsScreen.InventoryItem{id=Ids[i]+"-egg",title="Ovo "+Names[i].ToLowerInvariant(),description="Incubação: "+new[]{"1 hora.","3 horas.","12 horas.","48 horas."}[i],button=button,quantity=quantity,selection=selected.gameObject,kind=i==0?RoyalPostmanPetsScreen.InventoryItem.Kind.CommonEgg:i==1?RoyalPostmanPetsScreen.InventoryItem.Kind.RareEgg:i==2?RoyalPostmanPetsScreen.InventoryItem.Kind.EpicEgg:RoyalPostmanPetsScreen.InventoryItem.Kind.LegendaryEgg};
            }
            var pulse=inventory.gameObject.AddComponent<RoyalPostmanEggGlow>();pulse.lights=lights;pulse.palette=RarityColors;pulse.intensity=.62f;pulse.Apply(0);
            view.items[4]=new RoyalPostmanPetsScreen.InventoryItem{id="accelerators",title="Itens de aceleração",description="Reduz o tempo restante da incubação.",button=stockButton,quantity=stockQuantity,selection=stockSelection,kind=RoyalPostmanPetsScreen.InventoryItem.Kind.Acceleration,aggregateAcceleration=true,accelerationMinutes=30};
            Text title;Plaque(inventory,"Title","round-red","Ovos",0,220,330,78,49,out title);
        }

        static void BuildHeader(RectTransform root,RoyalPostmanPetsScreen view)
        {
            var header=Box(root,"Header",0,0,1080,190);Top(header,0,-95);
            Picture(header,"Background",Existing("pets-header-red-home-v4"),0,0,1080,190);
            Picture(header,"GoldRail",rail,0,-89,1080,12);
            Label(header,"Title","Mascotes",0,7,730,135,128);
            RectTransform backMoving,helpMoving;
            view.backButton=PaintedControl(header,"Back","round-red",-450,6,138,138,69,false,out backMoving);
            Picture(backMoving,"Icon",S("back"),0,0,72,66,true);
            view.helpButton=PaintedControl(header,"Help","action-purple",450,6,138,138,69,false,out helpMoving);
            Label(helpMoving,"Glyph","?",0,4,96,105,88);
            view.currentTab=SideTab(root,"CurrentTab","Atual",Existing("paw-cream"),-280);
            view.incubatorTab=SideTab(root,"IncubatorTab","Incubadora",S("egg-common"),-446);
        }
        static Button SideTab(RectTransform root,string name,string caption,Sprite icon,float y)
        {
            RectTransform moving;var button=PaintedControl(root,name,"action-purple",0,0,150,150,36,false,out moving);
            var r=(RectTransform)button.transform;r.anchorMin=r.anchorMax=new Vector2(1,1);r.anchoredPosition=new Vector2(-92,y);
            Picture(moving,"Icon",icon,0,12,66,72,true);
            Label(moving,"Label",caption,0,-42,128,35,caption.Length>7?24:29);
            Box(r,"Selected",0,0,0,0).gameObject.SetActive(false);return button;
        }
    }
}
