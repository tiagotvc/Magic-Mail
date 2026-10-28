using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.UI.RoyalPostman2026;

namespace RoyalAves.EditorTools
{
    /// <summary>Reproduces the supplied 1080x1920 Pets mock. Explicit authoring only.</summary>
    public static class RoyalPostmanPetsMockBuilder
    {
        static Font font;
        static readonly Color White = new Color32(250, 248, 255, 255);
        static readonly Color Gold = new Color32(255, 201, 28, 255);
        static readonly Color Navy = new Color32(10, 23, 47, 255);
        static Sprite panel;

        [MenuItem("Royal Postman/Mascotes/Recriar conforme mock (substitui Pets)")]
        public static void Build()
        {
            foreach(var entry in new[]{new[]{"Menu","pets-workshop-v5"},new[]{"Menu","pets-panel-v4"},
                new[]{"Menu","pets-scroll-track-v5"},new[]{"Button","pets-scroll-thumb-v5"},
                new[]{"Menu","pets-incubator-empty-v1"},new[]{"Icon","pets-egg-v1"},
                new[]{"Button","pets-accelerate-v2"},new[]{"Button","pets-use-item-v2"},
                new[]{"Button","pets-back-v4"},new[]{"Button","pets-help-v4"},
                new[]{"Button","pets-page-arrow-v6"},
                new[]{"Tab","pets-tab-current-v4"},new[]{"Tab","pets-tab-incubator-v4"},
                new[]{"Icon","pets-potion-v2"},new[]{"Icon","pets-crystal-v2"},new[]{"Icon","pets-hourglass-v2"},
                new[]{"Menu","pets-divider-lines-v3"},new[]{"Menu","pets-ornament-center-v3"}})
                RoyalPostmanPetsBuilder.Import(entry[0],entry[1]);
            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/RoyalAves/Fonts/Pets/Nunito-ExtraBold.ttf");
            if(font==null)throw new InvalidOperationException("Missing Nunito font source");
            panel = Sprite("pets-panel-v4");
            CreateDividerSegments();
            CreateHeaderSegments();
            var scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("Pets",typeof(RectTransform));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            try
            {
                var r=(RectTransform)root.transform;r.sizeDelta=new Vector2(1080,1920);
                Gradient(r,"Background",Navy,Navy,true);
                var room=Top(r,"Room",540,640,1080,1080);
                // The revised painting puts the tabletop behind every foot, preserving the room camera.
                var backdrop=Pic(room,"WorkshopBackground","pets-workshop-v5",0,0,1080,1080);
                backdrop.raycastTarget=false;
                var body=Top(r,"Body",526,620,1080,1100);
                var controller=root.AddComponent<RoyalPostmanPetsScreen>();
                var workshop=Box(body,"Workshop",0,0,1080,1100);controller.workshop=workshop.gameObject;
                controller.incubators=new RoyalPostmanPetsScreen.IncubatorView[3];
                for(int i=0;i<3;i++)controller.incubators[i]=Incubator(workshop,i);

                var current=Panel(body,"CurrentPet",0,-100,780,680);
                Pic(current.rectTransform,"Paw","paw-cream",0,135,160,160,true).color=new Color(.66f,.6f,1);
                Label(current.rectTransform,"Title","Seu companheiro",0,-5,690,70,53,White);
                Label(current.rectTransform,"Copy","Nenhum mascote ativo.",0,-115,690,80,36,White);
                controller.currentPanel=current.gameObject;current.gameObject.SetActive(false);

                controller.currentTab=Tab(r,"CurrentTab","Atual","pets-tab-current-v4",1000,232,false);
                controller.incubatorTab=Tab(r,"IncubatorTab","Incubadora","pets-tab-incubator-v4",1000,370,true);
                BuildInventory(r,controller);
                BuildDivider(r);
                BuildHeader(r,controller);
                BuildHelp(r,controller);
                BuildItemTooltip(r,controller);
                CreateTabStateSprites();ConfigureTabStates(controller);
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,RoyalPostmanPetsBuilder.PrefabPath);
                var kit=RoyalPostmanRuntimeKit.Load();var screen=kit.additionalScreens.Find(s=>s.id=="pets");
                if(screen==null){screen=new RoyalPostmanRuntimeKit.Screen{id="pets"};kit.additionalScreens.Add(screen);}
                screen.title="Mascotes";screen.prefab=prefab;screen.hideNavigation=true;
                EditorUtility.SetDirty(kit);AssetDatabase.SaveAssets();
            }
            finally{UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
            RoyalPostmanCurrentPetBuilder.Build();
        }

        static RoyalPostmanPetsScreen.IncubatorView Incubator(RectTransform stage,int index)
        {
            var root=Box(stage,"Incubator"+index,(index-1)*312,0,320,1040);
            var shadow=Box(root,"TableContact",0,-192,332,32).gameObject.AddComponent<RoyalPostmanGlyph>();
            shadow.symbol=RoyalPostmanGlyph.Symbol.ContactShadow;shadow.color=new Color(.16f,.025f,.08f,.48f);shadow.raycastTarget=false;
            var egg=Pic(root,"Egg","pets-egg-v1",0,23,204,266,true);egg.gameObject.SetActive(false);
            Pic(root,"Apparatus","pets-incubator-empty-v1",0,37,320,460,true);
            // The apparatus belongs to the table; its UI plaque is a foreground overlay.
            var card=Panel(root,"Panel",0,-366,308,356);
            var available=Box(card.rectTransform,"Available",0,0,308,342);
            var track=Panel(available,"ProgressTrack",0,140,230,34);track.pixelsPerUnitMultiplier=8;
            track.gameObject.AddComponent<Mask>().showMaskGraphic=true;
            var fill=Box(track.rectTransform,"Fill",0,0,224,28).gameObject.AddComponent<Image>();
            fill.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillAmount=0;fill.color=new Color(.08f,.96f,.12f);fill.raycastTarget=false;
            var progress=Label(track.rectTransform,"Value","0 / 100",0,0,222,40,31,White);
            var empty=Box(available,"Empty",0,40,260,45);Label(empty,"Status","Vazia",0,0,250,45,31,White);
            var running=Box(available,"Running",0,40,260,45);
            var clock=Box(running,"Clock",-78,0,33,33).gameObject.AddComponent<RoyalPostmanGlyph>();clock.symbol=RoyalPostmanGlyph.Symbol.Clock;clock.color=White;clock.raycastTarget=false;
            var remaining=Label(running,"Time","",18,0,185,44,31,White);running.gameObject.SetActive(false);
            var use=Action(available,"UseItem","pets-use-item-v2",0,-70,268,94);
            Label((RectTransform)use.transform,"Label","Incubar x0",0,0,244,82,32,White);
            var useColors=use.colors;useColors.disabledColor=new Color(.48f,.52f,.62f,.75f);use.colors=useColors;use.interactable=false;
            var locked=Box(card.rectTransform,"Locked",0,0,308,342);
            Pic(locked,"Lock","lock-gray",0,55,56,66,true);
            Label(locked,"Level","Nv. "+(index==1?30:100),0,-12,278,60,48,White);
            Label(locked,"Unlock","Desbloqueia",0,-76,278,45,32,White);
            Label(locked,"Rarity",index==1?"Pets Raros":"Pets Épicos",0,-117,280,45,32,index==1?new Color(0,1,1):new Color(.95f,0,1));
            available.gameObject.SetActive(index==0);locked.gameObject.SetActive(index>0);
            return new RoyalPostmanPetsScreen.IncubatorView{unlockLevel=index==0?0:index==1?30:100,egg=egg,available=available.gameObject,empty=empty.gameObject,running=running.gameObject,locked=locked.gameObject,progress=progress,remaining=remaining,progressFill=fill,useItem=use};
        }

        public static void RefineIncubationActions()
        {
            var root=PrefabUtility.LoadPrefabContents(RoyalPostmanPetsBuilder.PrefabPath);
            try{
                var view=root.GetComponent<RoyalPostmanPetsScreen>();
                foreach(var slot in view.incubators){
                    if(slot.accelerate!=null)UnityEngine.Object.DestroyImmediate(slot.accelerate.gameObject);slot.accelerate=null;
                    var button=slot.useItem;var rect=(RectTransform)button.transform;rect.anchoredPosition=new Vector2(0,-70);rect.sizeDelta=new Vector2(268,94);
                    var label=button.transform.Find("Label").GetComponent<Text>();label.text="Incubar x0";label.fontSize=32;label.rectTransform.sizeDelta=new Vector2(244,82);
                    var colors=button.colors;colors.disabledColor=new Color(.48f,.52f,.62f,.75f);button.colors=colors;button.interactable=false;
                    foreach(var status in new[]{slot.empty,slot.running}){var r=(RectTransform)status.transform;r.anchoredPosition=new Vector2(r.anchoredPosition.x,40);}
                }
                ConfigureInventoryQuantities(view);
                PrefabUtility.SaveAsPrefabAsset(root,RoyalPostmanPetsBuilder.PrefabPath);AssetDatabase.SaveAssets();
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void ConfigureInventoryQuantities(RoyalPostmanPetsScreen view)
        {
            int eggOrdinal=0,accelerationOrdinal=0;
            foreach(var item in view.items){
                string icon=item.button.transform.Find("Icon").GetComponent<Image>().sprite.name;
                if(icon.Contains("egg")){
                    int rarity=eggOrdinal++%3;
                    item.kind=rarity==0?RoyalPostmanPetsScreen.InventoryItem.Kind.CommonEgg:rarity==1?RoyalPostmanPetsScreen.InventoryItem.Kind.RareEgg:RoyalPostmanPetsScreen.InventoryItem.Kind.EpicEgg;
                    item.title=rarity==0?"Ovo comum":rarity==1?"Ovo raro":"Ovo épico";
                    item.description=rarity==0?"Incubação: 1h.":rarity==1?"Incubação: 3h.":"Incubação: 12h.";
                }else if(icon.Contains("hourglass")||icon.Contains("potion")){
                    item.kind=RoyalPostmanPetsScreen.InventoryItem.Kind.Acceleration;
                    item.accelerationMinutes=new[]{30,60,90,120}[accelerationOrdinal++%4];
                    item.title="Acelerador de "+item.accelerationMinutes+" min";item.description="Retira "+item.accelerationMinutes+" minutos de uma incubação ativa.";
                }else item.kind=RoyalPostmanPetsScreen.InventoryItem.Kind.Example;
                item.quantity=item.button.transform.Find("Quantity").GetComponent<Text>();
                if(item.kind!=RoyalPostmanPetsScreen.InventoryItem.Kind.Example)item.quantity.text="x0";
            }
        }

        static void BuildInventory(RectTransform root,RoyalPostmanPetsScreen view)
        {
            var inventory=Box(root,"Inventory",0,0,1080,746);inventory.anchorMin=Vector2.zero;inventory.anchorMax=Vector2.one;inventory.offsetMin=Vector2.zero;inventory.offsetMax=new Vector2(0,-1174);
            Gradient(inventory,"Background",Navy,Navy,true);
            var heading=Top(inventory,"Title",540,48,980,60).gameObject.AddComponent<Text>();TextStyle(heading,"Inventário",43,White);heading.alignment=TextAnchor.MiddleLeft;
            string[] icons={"pets-hourglass-v2","pets-egg-v1","pets-crystal-v2","pets-potion-v2","pets-potion-v2","pets-hourglass-v2","pets-crystal-v2","pets-egg-v1","pets-potion-v2","pets-hourglass-v2","pets-hourglass-v2","pets-egg-v1","pets-potion-v2","pets-crystal-v2","pets-hourglass-v2","pets-crystal-v2","pets-potion-v2","pets-potion-v2","pets-egg-v1","pets-crystal-v2","pets-hourglass-v2","pets-potion-v2","pets-egg-v1","pets-crystal-v2"};
            int[] counts={8,3,4,5,2,3,4,1,5,8,2,3,5,4,8,4,5,2,1,4,3,5,3,4};
            view.items=new RoyalPostmanPetsScreen.InventoryItem[icons.Length];
            var groups=new[]{new List<int>(),new List<int>()};
            for(int i=0;i<icons.Length;i++)groups[icons[i].Contains("egg")||icons[i].Contains("crystal")?0:1].Add(i);
            view.inventoryRows=new RoyalPostmanPetsScreen.InventoryRow[2];
            for(int rowIndex=0;rowIndex<2;rowIndex++){
                string id=rowIndex==0?"EggsAndGems":"Acceleration";
                var row=Top(inventory,id,540,rowIndex==0?260:550,1080,260);
                row.anchorMin=row.anchorMax=new Vector2(.5f,rowIndex==0?.5f:0);
                row.anchoredPosition=new Vector2(0,rowIndex==0?113:196);
                var label=Top(row,"Title",540,24,924,43).gameObject.AddComponent<Text>();
                TextStyle(label,rowIndex==0?"Ovos e gemas":"Itens de aceleração",34,White);label.alignment=TextAnchor.MiddleLeft;
                var rowView=new RoyalPostmanPetsScreen.InventoryRow{id=id,previous=PageArrow(row,"Previous",34,true),next=PageArrow(row,"Next",1046,false)};
                int pageCount=Mathf.Max(1,Mathf.CeilToInt(groups[rowIndex].Count/6f));rowView.pages=new GameObject[pageCount];
                for(int pageIndex=0;pageIndex<pageCount;pageIndex++){
                    var page=Top(row,"Page"+pageIndex,540,153,940,186);rowView.pages[pageIndex]=page.gameObject;
                    for(int slot=0;slot<6;slot++){
                        float x=(slot-2.5f)*156;
                        int entry=pageIndex*6+slot;
                        if(entry>=groups[rowIndex].Count){var empty=Panel(page,"EmptySlot"+slot,x,0,144,174);empty.pixelsPerUnitMultiplier=8;empty.color=new Color(1,1,1,.3f);continue;}
                        int i=groups[rowIndex][entry];
                        var cell=Panel(page,"Item"+i,x,0,144,174);cell.pixelsPerUnitMultiplier=8;cell.raycastTarget=true;
                        var chosen=Panel(cell.rectTransform,"Selected",0,0,144,174);Stretch(chosen.rectTransform);chosen.pixelsPerUnitMultiplier=8;chosen.color=new Color(.9f,.55f,1,.45f);chosen.gameObject.SetActive(false);
                        Pic(cell.rectTransform,"Icon",icons[i],0,17,116,118,true);
                        Label(cell.rectTransform,"Quantity","x"+counts[i],0,-59,142,41,34,White);
                        var button=cell.gameObject.AddComponent<Button>();button.targetGraphic=cell;button.transition=Selectable.Transition.ColorTint;
                        string title=icons[i].Contains("egg")?"Ovo mágico":icons[i].Contains("hourglass")?"Areia do tempo":icons[i].Contains("potion")?"Poção estelar":"Cristal mágico";
                        view.items[i]=new RoyalPostmanPetsScreen.InventoryItem{id="sample-"+i,title=title,description="Item do inventário de mascotes.",button=button,selection=chosen.gameObject};
                    }
                }
                view.inventoryRows[rowIndex]=rowView;view.SetInventoryPage(rowIndex,0);
            }
            ConfigureInventoryFeedback(view);
            ConfigureInventoryQuantities(view);
        }
        public static void RefineInventoryFeedback()
        {
            var root=PrefabUtility.LoadPrefabContents(RoyalPostmanPetsBuilder.PrefabPath);
            try{ConfigureInventoryFeedback(root.GetComponent<RoyalPostmanPetsScreen>());PrefabUtility.SaveAsPrefabAsset(root,RoyalPostmanPetsBuilder.PrefabPath);AssetDatabase.SaveAssets();}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        public static void RefineInventoryPolish()
        {
            var root=PrefabUtility.LoadPrefabContents(RoyalPostmanPetsBuilder.PrefabPath);
            try{
                ApplyDividerThickness(root.transform.Find("Divider"));
                var view=root.GetComponent<RoyalPostmanPetsScreen>();
                for(int i=0;i<view.inventoryRows.Length;i++){
                    var row=view.inventoryRows[i];var rect=(RectTransform)row.pages[0].transform.parent;
                    var position=rect.anchoredPosition;position.y=i==0?-260:-550;rect.anchoredPosition=position;
                    row.activeDotColor=new Color(1,.78f,.04f);row.inactiveDotColor=new Color(.5f,.39f,.02f,.38f);
                    view.SetInventoryPage(i,0);
                }
                PrefabUtility.SaveAsPrefabAsset(root,RoyalPostmanPetsBuilder.PrefabPath);AssetDatabase.SaveAssets();
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void ConfigureInventoryFeedback(RoyalPostmanPetsScreen view)
        {
            for(int rowIndex=0;rowIndex<view.inventoryRows.Length;rowIndex++){
                var row=view.inventoryRows[rowIndex];
                if(row.pagination!=null)UnityEngine.Object.DestroyImmediate(row.pagination.gameObject);row.pagination=null;
                var page=(RectTransform)row.pages[0].transform;var parent=(RectTransform)page.parent;
                var old=parent.Find("PageDots");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
                float y=-page.anchoredPosition.y+page.rect.height*.5f+20;
                var dots=Top(parent,"PageDots",540,y,400,40);
                row.dots=new Button[row.pages.Length];
                for(int i=0;i<row.dots.Length;i++){
                    var hit=Box(dots,"Dot"+i,(i-(row.dots.Length-1)*.5f)*42,0,40,40);
                    hit.gameObject.AddComponent<Image>().color=Color.clear;
                    var face=Box(hit,"Face",0,0,28,28).gameObject.AddComponent<RoyalPostmanGlyph>();
                    face.symbol=RoyalPostmanGlyph.Symbol.PageDot;face.raycastTarget=false;
                    var button=hit.gameObject.AddComponent<Button>();button.targetGraphic=face;
                    ConfigureHover(button,face);row.dots[i]=button;
                }
                view.SetInventoryPage(rowIndex,0);
            }
            foreach(var item in view.items)ConfigureHover(item.button,item.button.targetGraphic);
        }
        static void ConfigureHover(Button button,Graphic face)
        {
            var hover=button.GetComponent<RoyalPostmanInventoryHover>()??button.gameObject.AddComponent<RoyalPostmanInventoryHover>();
            if(hover.outline==null)hover.outline=face.gameObject.AddComponent<Outline>();
            hover.outline.effectColor=new Color(.88f,.82f,1,.9f);hover.outline.effectDistance=new Vector2(2,2);hover.outline.enabled=false;
            var colors=button.colors;colors.normalColor=face is Image?new Color(.84f,.88f,.98f):Color.white;
            colors.highlightedColor=colors.selectedColor=Color.white;colors.pressedColor=new Color(.75f,.8f,.95f);colors.fadeDuration=0;
            button.colors=colors;
        }
        static Button PageArrow(RectTransform row,string name,float x,bool left)
        {
            var root=Top(row,name,x,153,62,192);var hit=root.gameObject.AddComponent<Image>();hit.color=Color.clear;
            var arrow=Pic(root,"Arrow","pets-page-arrow-v6",0,0,44,66,true);if(left)arrow.rectTransform.localScale=new Vector3(-1,1,1);
            var button=root.gameObject.AddComponent<Button>();button.targetGraphic=arrow;button.transition=Selectable.Transition.ColorTint;
            var colors=button.colors;colors.disabledColor=new Color(.5f,.5f,.5f,.38f);button.colors=colors;return button;
        }

        static void BuildDivider(RectTransform root)
        {
            var divider=Top(root,"Divider",540,1174,1080,80);
            // Tips and jewel keep their native proportions. Only an unadorned straight strip tiles.
            Pic(divider,"LeftTip","pets-divider-tip-v9",-518,-3.7f,37.7f,24.94f,true);
            Pic(divider,"RightTip","pets-divider-tip-v9",518,-3.7f,37.7f,24.94f,true);
            foreach(bool left in new[]{true,false}){
                var line=Pic(divider,left?"LeftLine":"RightLine","pets-divider-straight-v9",left?-299:299,-5,410,5.2f);
                line.type=Image.Type.Tiled;line.pixelsPerUnitMultiplier=18/5.2f;
            }
            Pic(divider,"Ornament","pets-ornament-center-v3",0,0,230,81,true);
            ApplyDividerThickness(divider);
        }
        // Selectively refines the authored prefab without regenerating the rest of the screen.
        public static void RefineDivider()
        {
            CreateDividerSegments();
            var root=PrefabUtility.LoadPrefabContents(RoyalPostmanPetsBuilder.PrefabPath);
            try{ApplyDividerThickness(root.transform.Find("Divider"));PrefabUtility.SaveAsPrefabAsset(root,RoyalPostmanPetsBuilder.PrefabPath);AssetDatabase.SaveAssets();}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void ApplyDividerThickness(Transform divider)
        {
            // Both the finial and bar sample the SAME painted ribbon at a uniform scale.
            // Only the constant straight cross-section tiles; the curls never stretch.
            const float scale=7.8f/166;
            float width=980*scale,height=618*scale,x=537-width*.5f;
            float outer=x-width*.5f+1,inner=64.5f;
            divider.Find("Ornament").GetComponent<RectTransform>().sizeDelta=new Vector2(172.5f,60.75f);
            foreach(bool left in new[]{true,false}){
                var tip=divider.Find(left?"LeftTip":"RightTip").GetComponent<RectTransform>();
                var tipImage=tip.GetComponent<Image>();tipImage.sprite=Sprite("pets-divider-tip-v9");tipImage.type=Image.Type.Simple;tipImage.preserveAspect=true;
                tip.localScale=new Vector3(left?1:-1,1,1);
                tip.sizeDelta=new Vector2(width,height);tip.anchoredPosition=new Vector2(left?-x:x,-5+92*scale);
                var line=divider.Find(left?"LeftLine":"RightLine").GetComponent<Image>();
                line.sprite=Sprite("pets-divider-straight-v9");line.type=Image.Type.Tiled;line.preserveAspect=false;
                line.rectTransform.sizeDelta=new Vector2(outer-inner,166*scale);
                line.rectTransform.anchoredPosition=new Vector2((left?-1:1)*(outer+inner)*.5f,-5);
                line.pixelsPerUnitMultiplier=1/scale;
            }
        }
        static void CreateDividerSegments()
        {
            RoyalPostmanPetsBuilder.Import("Menu","pets-divider-finial-v9");
            var source=Sprite("pets-divider-finial-v9");
            SaveRegion(source.texture,new Rect(20,84,980,618),"pets-divider-tip-v9");
            SaveRegion(source.texture,new Rect(1200,218,64,166),"pets-divider-straight-v9");
        }
        static Button Tab(RectTransform root,string name,string label,string icon,float x,float y,bool selected)
        {
            var container=Top(root,name,x,y,118,122);
            Box(container,"Selected",0,0,118,122).gameObject.SetActive(selected);
            // Body + icon are painted together, with the label remaining independently editable.
            var face=Pic(container,"Face",icon,0,0,118,122);face.raycastTarget=true;
            face.color=selected?Color.white:new Color(.86f,.89f,.97f);
            var caption=Label(container,"Label",label,0,-35,label=="Atual"?113:100,33,label=="Atual"?23:18,White);
            if(label!="Atual")FitTabCaption(caption);
            var button=container.gameObject.AddComponent<Button>();button.targetGraphic=face;button.transition=Selectable.Transition.ColorTint;return button;
        }
        static void FitTabCaption(Text text)
        {
            text.fontSize=18;text.resizeTextForBestFit=true;text.resizeTextMinSize=14;text.resizeTextMaxSize=18;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            text.rectTransform.sizeDelta=new Vector2(100,30);
        }
        static void CreateTabStateSprites()
        {
            RoyalPostmanPetsBuilder.Import("Tab","pets-tab-current-gold-v14");
            RoyalPostmanPetsBuilder.Import("Tab","pets-tab-incubator-blue-v14");
            // Native regions exclude disconnected alpha specks while preserving source PNGs unchanged.
            SaveTabRegion(Sprite("pets-tab-current-v4").texture,new Rect(151,155,950,942),"pets-tab-current-blue-v14");
            SaveTabRegion(Sprite("pets-tab-incubator-v4").texture,new Rect(146,142,960,969),"pets-tab-incubator-gold-v14");
            SaveTabRegion(Sprite("pets-tab-current-gold-v14").texture,new Rect(139,140,972,974),"pets-tab-current-gold-v14");
            SaveTabRegion(Sprite("pets-tab-incubator-blue-v14").texture,new Rect(148,150,956,948),"pets-tab-incubator-blue-v14");
        }
        static void SaveTabRegion(Texture2D texture,Rect rect,string id)
        {
            var sprite=UnityEngine.Sprite.Create(texture,rect,Vector2.one*.5f,100,0,SpriteMeshType.FullRect);sprite.name=id;
            string path="Assets/RoyalAves/Art/UI/Tab/RoyalPostman/Pets/Sprites/"+id+".asset";var old=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(old==null)AssetDatabase.CreateAsset(sprite,path);else{EditorUtility.CopySerialized(sprite,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(sprite);}
        }
        static void ConfigureTabStates(RoyalPostmanPetsScreen view)
        {
            view.currentTabBlue=Sprite("pets-tab-current-blue-v14");view.currentTabGold=Sprite("pets-tab-current-gold-v14");
            view.incubatorTabBlue=Sprite("pets-tab-incubator-blue-v14");view.incubatorTabGold=Sprite("pets-tab-incubator-gold-v14");
            foreach(var button in new[]{view.currentTab,view.incubatorTab}){var colors=button.colors;colors.normalColor=colors.highlightedColor=colors.selectedColor=Color.white;button.colors=colors;}
            view.ShowCurrent(false);
        }
        public static void RefineTabStates()
        {
            CreateTabStateSprites();var root=PrefabUtility.LoadPrefabContents(RoyalPostmanPetsBuilder.PrefabPath);
            try{ConfigureTabStates(root.GetComponent<RoyalPostmanPetsScreen>());PrefabUtility.SaveAsPrefabAsset(root,RoyalPostmanPetsBuilder.PrefabPath);AssetDatabase.SaveAssets();}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        public static void RefineItemTooltip()
        {
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/RoyalAves/Fonts/Pets/Nunito-ExtraBold.ttf");panel=Sprite("pets-panel-v4");
            var root=PrefabUtility.LoadPrefabContents(RoyalPostmanPetsBuilder.PrefabPath);
            try{
                var view=root.GetComponent<RoyalPostmanPetsScreen>();
                FitTabCaption(root.transform.Find("IncubatorTab/Label").GetComponent<Text>());
                BuildItemTooltip((RectTransform)root.transform,view);
                PrefabUtility.SaveAsPrefabAsset(root,RoyalPostmanPetsBuilder.PrefabPath);AssetDatabase.SaveAssets();
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void BuildItemTooltip(RectTransform root,RoyalPostmanPetsScreen view)
        {
            var old=root.Find("ItemTooltip");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var overlay=Box(root,"ItemTooltip",0,0,0,0);Stretch(overlay);
            var dismiss=Box(overlay,"Outside",0,0,0,0);Stretch(dismiss);
            var outside=dismiss.gameObject.AddComponent<Image>();outside.color=Color.clear;
            var surface=dismiss.gameObject.AddComponent<RoyalPostmanTooltipSurface>();surface.owner=view;surface.passThrough=(RectTransform)root.Find("Inventory");
            var background=root.Find("Inventory/Background");background.GetComponent<Graphic>().raycastTarget=true;
            var inventorySurface=background.GetComponent<RoyalPostmanTooltipSurface>()??background.gameObject.AddComponent<RoyalPostmanTooltipSurface>();inventorySurface.owner=view;
            var card=Panel(overlay,"Card",0,0,560,260);card.pixelsPerUnitMultiplier=6;card.raycastTarget=true;
            var backing=Box(card.rectTransform,"Backing",0,0,0,0);Stretch(backing);backing.offsetMin=new Vector2(10,10);backing.offsetMax=new Vector2(-10,-10);
            var solid=backing.gameObject.AddComponent<Image>();solid.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");solid.type=Image.Type.Sliced;solid.color=new Color32(10,22,43,255);solid.raycastTarget=false;
            view.itemTooltip=overlay.gameObject;view.tooltipCard=card.rectTransform;
            var icon=Box(card.rectTransform,"Icon",-218,65,76,96).gameObject.AddComponent<Image>();icon.preserveAspect=true;icon.raycastTarget=false;view.tooltipIcon=icon;
            view.hintTitle=Label(card.rectTransform,"Title","",20,76,350,55,36,new Color32(255,222,137,255));
            view.hintTitle.alignment=TextAnchor.MiddleLeft;view.hintTitle.resizeTextForBestFit=true;view.hintTitle.resizeTextMinSize=26;view.hintTitle.resizeTextMaxSize=36;
            view.hintDescription=Label(card.rectTransform,"Description","",0,-55,496,130,28,White);
            view.hintDescription.alignment=TextAnchor.UpperLeft;view.hintDescription.horizontalOverflow=HorizontalWrapMode.Wrap;view.hintDescription.verticalOverflow=VerticalWrapMode.Truncate;
            var close=Box(card.rectTransform,"Close",247,100,44,44);close.gameObject.AddComponent<Image>().color=Color.clear;
            var cross=Box(close,"Cross",0,0,23,23).gameObject.AddComponent<RoyalPostmanGlyph>();cross.symbol=RoyalPostmanGlyph.Symbol.Close;cross.color=White;cross.raycastTarget=false;
            view.dismissItemTooltip=close.gameObject.AddComponent<Button>();view.dismissItemTooltip.targetGraphic=cross;
            view.ResetTooltipPreview();
        }
        static void CreateHeaderSegments()
        {
            // Region sprites reuse the exact Home navigation pixels; no recolor or new approximation.
            var source=Sprite("navigation-bar-approved-v2");
            SaveRegion(source.texture,new Rect(10,218,2153,263),"pets-header-red-home-v4");
            SaveRegion(source.texture,new Rect(10,484,2153,30),"pets-header-gold-home-v4");
        }
        static void SaveRegion(Texture2D texture,Rect rect,string id,Vector4 border=default)
        {
            var sprite=UnityEngine.Sprite.Create(texture,rect,Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border);sprite.name=id;
            string path="Assets/RoyalAves/Art/UI/Menu/RoyalPostman/Pets/Sprites/"+id+".asset";var old=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(old==null)AssetDatabase.CreateAsset(sprite,path);else{EditorUtility.CopySerialized(sprite,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(sprite);}
        }
        static void BuildHeader(RectTransform root,RoyalPostmanPetsScreen view)
        {
            var header=Top(root,"Header",540,68,1080,136);
            Pic(header,"Background","pets-header-red-home-v4",0,0,1080,136).raycastTarget=true;
            for(int i=0;i<10;i++)Pic(header,"PawPattern"+i,"paw-cream",-490+i*110,i%2==0?18:-15,52,48,true).color=new Color(.38f,0,.015f,.13f);
            Pic(header,"GoldRail","pets-header-gold-home-v4",0,-64.5f,1080,7);
            Label(header,"Title","Mascotes",0,0,790,90,66,White);
            view.backButton=Round(header,"Back",-461,false);view.helpButton=Round(header,"Help",461,true);
        }
        public static void RefineHeaderSpacing()
        {
            var root=PrefabUtility.LoadPrefabContents(RoyalPostmanPetsBuilder.PrefabPath);
            try{
                var header=(RectTransform)root.transform.Find("Header");
                header.sizeDelta=new Vector2(header.sizeDelta.x,136);
                header.anchoredPosition=new Vector2(header.anchoredPosition.x,-68);
                var background=(RectTransform)header.Find("Background");background.sizeDelta=new Vector2(background.sizeDelta.x,136);
                var rail=(RectTransform)header.Find("GoldRail");rail.anchoredPosition=new Vector2(rail.anchoredPosition.x,-64.5f);
                PrefabUtility.SaveAsPrefabAsset(root,RoyalPostmanPetsBuilder.PrefabPath);AssetDatabase.SaveAssets();
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static Button Round(RectTransform parent,string name,float x,bool help)
        {
            var image=Pic(parent,name,help?"pets-help-v4":"pets-back-v4",x,0,92,92,true);image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.ColorTint;return button;
        }
        static void BuildHelp(RectTransform root,RoyalPostmanPetsScreen view)
        {
            var shade=Box(root,"HelpPanel",0,0,0,0);Stretch(shade);shade.gameObject.AddComponent<Image>().color=new Color(0,0,0,.75f);
            var card=Panel(shade,"Card",0,0,820,540);Label(card.rectTransform,"Title","Incubadoras",0,175,740,75,56,Gold);
            Label(card.rectTransform,"Copy","Os ovos aparecem nas cápsulas somente\ndurante uma incubação ativa.\n\nSelecione os itens no inventário abaixo.\nAs cápsulas extras são liberadas\nconforme você avança nas fases.",0,0,740,280,33,White);
            var close=Action(card.rectTransform,"Close","pets-use-item-v2",0,-190,330,88);Label((RectTransform)close.transform,"Label","Entendi",0,0,300,70,36,White);view.dismissHelp=close;view.helpPanel=shade.gameObject;shade.gameObject.SetActive(false);
        }
        static Button Action(RectTransform parent,string name,string sprite,float x,float y,float w,float h)
        {
            var image=Pic(parent,name,sprite,x,y,w,h);image.raycastTarget=true;var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.ColorTint;
            var colors=button.colors;colors.disabledColor=Color.white;button.colors=colors;return button;
        }
        static RoyalPostmanGradient Gradient(RectTransform parent,string name,Color top,Color bottom,bool hit=false)
        {var r=Box(parent,name,0,0,0,0);Stretch(r);var g=r.gameObject.AddComponent<RoyalPostmanGradient>();g.top=top;g.bottom=bottom;g.raycastTarget=hit;return g;}
        static RectTransform Box(RectTransform parent,string name,float x,float y,float w,float h)
        {var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=Vector2.one*.5f;r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;}
        static RectTransform Top(RectTransform parent,string name,float x,float y,float w,float h)
        {var r=Box(parent,name,x-540,-y,w,h);r.anchorMin=r.anchorMax=new Vector2(.5f,1);return r;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static Sprite Sprite(string id)=>AssetDatabase.LoadAssetAtPath<Sprite>(RoyalPostmanArtPaths.Sprite(id))??throw new InvalidOperationException("Missing sprite: "+id);
        static Image Pic(RectTransform p,string name,string id,float x,float y,float w,float h,bool aspect=false)
        {var image=Box(p,name,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=Sprite(id);image.raycastTarget=false;image.preserveAspect=aspect;return image;}
        static Image Panel(RectTransform p,string name,float x,float y,float w,float h)
        {var image=Box(p,name,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=panel;image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=3;image.color=Color.white;image.raycastTarget=false;return image;}
        static Text Label(RectTransform p,string name,string value,float x,float y,float w,float h,int size,Color color)
        {var text=Box(p,name,x,y,w,h).gameObject.AddComponent<Text>();TextStyle(text,value,size,color);return text;}
        static void TextStyle(Text text,string value,int size,Color color)
        {text.font=font;text.fontStyle=FontStyle.Normal;text.text=value;text.fontSize=size;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Overflow;text.raycastTarget=false;var shadow=text.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,.04f,.65f);shadow.effectDistance=new Vector2(1.2f,-2);}
    }
}
