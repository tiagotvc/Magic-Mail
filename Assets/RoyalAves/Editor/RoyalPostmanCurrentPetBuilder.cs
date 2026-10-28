using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.UI.RoyalPostman2026;

namespace RoyalAves.EditorTools
{
    public static class RoyalPostmanCurrentPetBuilder
    {
        static Font font,bodyFont;static readonly Color Cream=new Color32(255,250,235,255);
        [MenuItem("Royal Postman/Mascotes/Criar aba Atual (preserva incubadoras)")]
        public static void Build()
        {
            RoyalPostmanPetProfileArt.ImportAll();
            foreach(var id in new[]{"pets-garden-v15","pets-podium-v15","pets-collection-frame-v15"})RoyalPostmanPetsBuilder.Import("Menu",id);
            foreach(var id in new[]{"pet-lumi-v15","pet-foxy-v15","pet-biscoito-v15","pets-skill-enchanted-letter-v15","pets-skill-friendship-v15"})RoyalPostmanPetsBuilder.Import("Icon",id);
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/RoyalAves/Fonts/Pets/Nunito-ExtraBold.ttf");
            bodyFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/RoyalAves/Fonts/Pets/Nunito.ttf");
            var root=PrefabUtility.LoadPrefabContents(RoyalPostmanPetsBuilder.PrefabPath);
            try{
                var owner=root.GetComponent<RoyalPostmanPetsScreen>();
                if(owner.currentPanel!=null)UnityEngine.Object.DestroyImmediate(owner.currentPanel);
                var prior=root.transform.Find("CurrentProfile");if(prior!=null)UnityEngine.Object.DestroyImmediate(prior.gameObject);
                var profile=Box((RectTransform)root.transform,"CurrentProfile",0,0,0,0);Stretch(profile);
                profile.SetSiblingIndex(root.transform.Find("CurrentTab").GetSiblingIndex());
                var view=profile.gameObject.AddComponent<RoyalPostmanCurrentPet>();view.owner=owner;owner.currentPanel=profile.gameObject;owner.defaultCurrentTab=true;
                view.rarityBadges=new[]{Sprite("pets-badge-common-v16"),Sprite("pets-badge-rare-v16"),Sprite("pets-badge-epic-v16")};
                view.cardBackgrounds=new[]{Sprite("pets-card-common-bg-v16"),Sprite("pets-card-rare-bg-v16"),Sprite("pets-card-epic-bg-v16")};
                view.cardStatuses=new[]{Sprite("pets-status-common-v16"),Sprite("pets-status-equipped-v16"),Sprite("pets-status-epic-v16")};
                view.equippedCardStatus=Sprite("pets-status-equipped-v16");view.equipReadyFace=Sprite("pets-progress-blue-v16");view.equipIdleFace=Sprite("pets-progress-track-v16");
                var bg=profile.gameObject.AddComponent<Image>();bg.color=new Color32(7,20,43,255);bg.raycastTarget=false;
                view.pets=new[]{
                    Pet("lumi","Lumi",RoyalPostmanCurrentPet.Rarity.Raro,"pet-lumi-v15",12,720,80,"Carta Encantada","Cria uma carta mágica especial durante a partida.","pets-skill-enchanted-letter-v15","Brilho da Amizade","Concede um impulso de pontos e remove peças especiais do tabuleiro.","pets-skill-friendship-v15"),
                    Pet("foxy","Foxy",RoyalPostmanCurrentPet.Rarity.Comum,"pet-foxy-v15",8,420,65,"Entrega Ágil","Ajuda a criar uma combinação especial durante a partida.","coin-envelope","Rastro Veloz","Libera um impulso que atravessa uma linha de peças.","booster-rocket"),
                    Pet("biscoito","Biscoito",RoyalPostmanCurrentPet.Rarity.Épico,"pet-biscoito-v15",15,850,95,"Faro de Tesouros","Ajuda a encontrar oportunidades de combinações especiais.","pets-crystal-v2","Explosão de Alegria","Cria uma explosão mágica que ajuda a limpar peças próximas.","booster-bomb")};
                BuildHero(profile,view);BuildStats(profile,view);BuildSkills(profile,view);BuildTabs(profile,view);BuildCollection(profile,view);
                var divider=UnityEngine.Object.Instantiate(root.transform.Find("Divider").gameObject,profile.transform);divider.name="Divider";divider.SetActive(true);
                var line=(RectTransform)divider.transform;line.anchoredPosition=new Vector2(0,-720);line.SetAsLastSibling();
                view.ResetPreview();owner.ShowCurrent(true);
                PrefabUtility.SaveAsPrefabAsset(root,RoyalPostmanPetsBuilder.PrefabPath);AssetDatabase.SaveAssets();
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static RoyalPostmanCurrentPet.Pet Pet(string id,string name,RoyalPostmanCurrentPet.Rarity rarity,string portrait,int level,int xp,int energy,string passive,string passiveText,string passiveIcon,string magic,string magicText,string magicIcon)
            =>new RoyalPostmanCurrentPet.Pet{id=id,displayName=name,rarity=rarity,portrait=Sprite(portrait),level=level,xp=xp,energy=energy,passiveName=passive,passiveDescription=passiveText,passiveIcon=Sprite(passiveIcon),magicName=magic,magicDescription=magicText,magicIcon=Sprite(magicIcon)};
        static void BuildHero(RectTransform root,RoyalPostmanCurrentPet view)
        {
            var area=Top(root,"Hero",540,428,1080,584);area.gameObject.AddComponent<RectMask2D>();
            var scene=Picture(area,"Garden","pets-garden-v15",0,0,1080,584,false,false);float scale=Mathf.Max(1080/scene.sprite.rect.width,584/scene.sprite.rect.height);scene.rectTransform.sizeDelta=scene.sprite.rect.size*scale;
            Picture(root,"Podium","pets-podium-v15",540,620,680,180,true);
            view.hero=Picture(root,"PetPortrait","pet-lumi-v15",540,570,500,420,true);view.hero.rectTransform.pivot=new Vector2(.5f,0);
            view.heroName=Text(root,"PetName","Lumi",540,622,310,55,47,Cream);
            var rarity=PaintedAction(root,"PetRarity","pets-progress-blue-v16",540,666,180,43,true);rarity.interactable=false;
            view.heroRarityFace=rarity.GetComponent<Graphic>();
            view.heroRarity=Text((RectTransform)rarity.transform,"Label","Raro",0,0,170,40,30,Cream,false);
            view.previous=Arrow(root,"PreviousPet",170,401,true,62,94,true);view.next=Arrow(root,"NextPet",840,401,false,62,94,true);
            view.petInfo=Info(root,"PetInfo",740,622,true,true);
        }
        static void BuildStats(RectTransform root,RoyalPostmanCurrentPet view)
        {
            Picture(root,"LevelPaw","pets-level-paw-v16",87,760,40,40,true);
            view.level=Text(root,"Level","Nv. 12",183,760,160,60,44,Cream);
            view.xpFill=Progress(root,"XP",515,760,500,46,"pets-progress-blue-v16");
            view.xp=Text(root,"XPValue","720 / 1000 XP",673,760,165,42,22,Cream);view.xp.alignment=TextAnchor.MiddleRight;
            Picture(root,"EnergyIcon","pets-energy-bolt-v16",89,820,46,56,true);
            view.energyFill=Progress(root,"Energy",455,820,640,46,"pets-progress-green-v16");
            view.energy=Text(root,"EnergyValue","80 / 100",701,820,120,42,24,Cream);view.energy.alignment=TextAnchor.MiddleRight;
            view.equip=PaintedAction(root,"Equip","pets-progress-track-v16",915,760,234,55,true);view.equipFace=view.equip.GetComponent<Image>();
            view.equipLabel=Text((RectTransform)view.equip.transform,"Label","Equipado",20,0,170,50,30,new Color(.1f,1,.72f),false);
            view.equipCheck=Picture((RectTransform)view.equip.transform,"Check","pets-equipped-check-v16",-92,0,48,48,true,false).gameObject;
            view.feed=PaintedAction(root,"Feed","pets-feed-button-v16",915,820,234,58);view.feed.interactable=false;
            Picture((RectTransform)view.feed.transform,"Food","pets-feed-bag-v16",-78,0,40,46,true,false);
            Text((RectTransform)view.feed.transform,"Label","Alimentar",22,0,171,50,29,new Color(.78f,.8f,.85f),false);
            Rule(root,"StatsLine",540,858,1020,2);
        }
        static void BuildSkills(RectTransform root,RoyalPostmanCurrentPet view)
        {
            Text(root,"PassiveHeading","Habilidade Passiva",226,892,330,42,29,Cream).alignment=TextAnchor.MiddleLeft;
            Text(root,"MagicHeading","Magia (habilidade ativa)",786,892,430,42,27,Cream).alignment=TextAnchor.MiddleLeft;
            view.passiveInfo=Info(root,"PassiveInfo",420,892,true);view.magicInfo=Info(root,"MagicInfo",1020,892,true);
            view.passiveIcon=Picture(root,"PassiveIcon","pets-skill-enchanted-letter-v15",141,990,124,124,true);
            view.magicIcon=Picture(root,"MagicIcon","pets-skill-friendship-v15",641,990,124,124,true);
            view.passiveName=Text(root,"PassiveName","Carta Encantada",370,936,300,42,31,Cream);view.passiveName.alignment=TextAnchor.MiddleLeft;Fit(view.passiveName,24,31);
            view.magicName=Text(root,"MagicName","Brilho da Amizade",879,936,320,42,30,Cream);view.magicName.alignment=TextAnchor.MiddleLeft;Fit(view.magicName,23,30);
            view.passiveDescription=Text(root,"PassiveDescription","",370,1016,300,112,25,Cream);Body(view.passiveDescription);
            view.magicDescription=Text(root,"MagicDescription","",879,1016,320,112,24,Cream);Body(view.magicDescription);
            Rule(root,"SkillDivider",540,981,2,190);
        }
        static void BuildTabs(RectTransform root,RoyalPostmanCurrentPet view)
        {
            var active=PaintedAction(root,"MascoteTab","pets-tab-active-v16",175,1145,287,100,true);active.interactable=false;
            Picture((RectTransform)active.transform,"Paw","pets-tab-paw-v16",-92,0,48,48,true,false);
            Text((RectTransform)active.transform,"Label","Mascote",33,0,190,70,36,Cream,false);
            view.lockedTabs=new Button[3];string[] names={"Apar\u00eancia","Selos","Contratos"};string[] icons={"pets-tab-shirt-v16","pets-tab-seal-v16","pets-tab-contract-v16"};float[] xs={457,716,959};float[] widths={267,242,235};
            for(int i=0;i<3;i++){
                var button=PaintedAction(root,"LockedTab"+i,"pets-tab-inactive-v16",xs[i],1145,widths[i],100,true);button.interactable=false;view.lockedTabs[i]=button;
                Picture((RectTransform)button.transform,"Icon",icons[i],-widths[i]*.34f,0,43,48,true,false);
                Picture((RectTransform)button.transform,"Lock","lock-gray",widths[i]*.5f-19,28,19,24,true,false);
                var label=Text((RectTransform)button.transform,"Label",names[i],23,0,widths[i]-64,70,29,new Color(.86f,.83f,.9f),false);Fit(label,24,29);
            }
        }
        static void BuildCollection(RectTransform root,RoyalPostmanCurrentPet view)
        {
            var collection=Box(root,"Collection",0,0,0,0);Stretch(collection);collection.offsetMin=new Vector2(0,35);collection.offsetMax=new Vector2(0,-1180);
            var panel=collection.gameObject.AddComponent<Image>();panel.sprite=Sprite("pets-collection-panel-v16");panel.type=Image.Type.Sliced;panel.pixelsPerUnitMultiplier=panel.sprite.rect.width/1080f;panel.raycastTarget=false;
            view.sort=PaintedAction(collection,"Sort","pets-sort-v16",860,55,280,58);
            view.sortLabel=Text((RectTransform)view.sort.transform,"Label","Raridade",2,0,173,50,31,new Color(.035f,.07f,.14f),false);
            view.cards=new RoyalPostmanCurrentPet.Card[3];
            for(int i=0;i<3;i++)view.cards[i]=Card(collection,i);
            view.collectionPrevious=Arrow(collection,"Previous",26,290,true,54,84,true);view.collectionNext=Arrow(collection,"Next",1054,290,false,54,84,true);
            view.dots=new Button[3];
            var dots=Top(collection,"SelectionDots",540,520,180,44);
            for(int i=0;i<3;i++){
                var hit=Box(dots,"Dot"+i,(i-1)*46,0,42,42);hit.gameObject.AddComponent<Image>().color=Color.clear;
                var face=Box(hit,"Face",0,0,28,28).gameObject.AddComponent<RoyalPostmanGlyph>();face.symbol=RoyalPostmanGlyph.Symbol.PageDot;face.raycastTarget=false;
                view.dots[i]=hit.gameObject.AddComponent<Button>();view.dots[i].targetGraphic=face;
            }
            var menu=Top(collection,"SortMenu",860,186,280,194);var menuPanel=menu.gameObject.AddComponent<Image>();menuPanel.sprite=Sprite("pets-panel-v4");menuPanel.type=Image.Type.Sliced;menuPanel.pixelsPerUnitMultiplier=6;
            var choices=new Button[3];for(int i=0;i<3;i++){choices[i]=Action(menu,"Option"+i,140,35+i*61,265,55,new Color(.06f,.12f,.23f));Text((RectTransform)choices[i].transform,"Label",new[]{"Raridade","Nome","Nível"}[i],0,0,250,50,28,Cream,false);}
            view.sortRarity=choices[0];view.sortName=choices[1];view.sortLevel=choices[2];view.sortMenu=menu.gameObject;menu.gameObject.SetActive(false);
        }
        static RoyalPostmanCurrentPet.Card Card(RectTransform parent,int index)
        {
            var card=Top(parent,"PetCard"+index,220+index*320,290,300,360);
            var portraitArea=Box(card,"PortraitArea",0,32,276,282);var clip=portraitArea.gameObject.AddComponent<Image>();clip.sprite=Skin();clip.type=Image.Type.Sliced;clip.raycastTarget=false;portraitArea.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            var backdrop=Picture(portraitArea,"Backdrop",new[]{"pets-card-rare-bg-v16","pets-card-common-bg-v16","pets-card-epic-bg-v16"}[index],0,0,276,282,false,false);
            var portrait=Picture(portraitArea,"Portrait",new[]{"pet-lumi-v15","pet-foxy-v15","pet-biscoito-v15"}[index],0,-141,286,286,true,false);portrait.rectTransform.pivot=new Vector2(.5f,0);
            var frame=Picture(card,"Frame","pets-card-frame-v16",0,0,300,360,false,false);frame.raycastTarget=true;
            var statusFace=Picture(card,"StatusColor",new[]{"pets-status-equipped-v16","pets-status-common-v16","pets-status-epic-v16"}[index],0,-146,264,46,false,false);
            var rarityFace=Picture(card,"RarityColor",new[]{"pets-badge-rare-v16","pets-badge-common-v16","pets-badge-epic-v16"}[index],0,171,228,57,false,false);
            var outline=frame.gameObject.AddComponent<Outline>();outline.effectDistance=new Vector2(1,-1);outline.effectColor=new Color(1,.96f,.65f,.5f);
            var button=card.gameObject.AddComponent<Button>();button.targetGraphic=frame;
            return new RoyalPostmanCurrentPet.Card{button=button,portrait=portrait,selected=outline,rarityFace=rarityFace,statusFace=statusFace,paintedBackdrop=backdrop,
                name=Text(card,"Name","",0,-102,236,38,33,new Color(.06f,.06f,.18f),false),rarity=Text(card,"Rarity","",28,171,143,36,26,Cream,false),status=Text(card,"Status","",0,-146,188,40,26,Cream,false)};
        }
        static Image Progress(RectTransform root,string name,float x,float y,float width,float height,string fillSprite)
        {
            var track=Picture(root,name,"pets-progress-track-v16",x,y,width,height);Sliced(track,height);
            // The reference reserves the right end of the track for the live value.
            var area=Box(track.rectTransform,"ProgressArea",0,0,width-160,height);area.anchorMin=area.anchorMax=new Vector2(0,.5f);area.pivot=new Vector2(0,.5f);area.anchoredPosition=Vector2.zero;
            var fill=Picture(area,"Fill",fillSprite,0,0,width-170,height-10,false,false);Sliced(fill,height-10);
            fill.rectTransform.anchorMin=fill.rectTransform.anchorMax=new Vector2(0,.5f);fill.rectTransform.pivot=new Vector2(0,.5f);fill.rectTransform.anchoredPosition=new Vector2(5,0);
            return fill;
        }
        static Button Info(RectTransform root,string name,float x,float y,bool top,bool blue=false)
        {
            var face=Picture(root,name,blue?"pets-info-blue-v16":"pets-info-lavender-v16",x,y,blue?50:34,blue?50:34,true,top);face.raycastTarget=true;
            var button=face.gameObject.AddComponent<Button>();button.targetGraphic=face;return button;
        }
        static void Sliced(Image image,float height){image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=image.sprite.rect.height/height;}
        static Button PaintedAction(RectTransform parent,string name,string sprite,float x,float y,float w,float h,bool sliced=false)
        {
            var image=Picture(parent,name,sprite,x,y,w,h);image.raycastTarget=true;if(sliced)Sliced(image,h);
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;var colors=button.colors;colors.disabledColor=Color.white;button.colors=colors;return button;
        }
        static Button Arrow(RectTransform parent,string name,float x,float y,bool left,float w,float h,bool top)
        {
            var rect=top?Top(parent,name,x,y,w,h):Box(parent,name,x-540,y,w,h);var image=rect.gameObject.AddComponent<Image>();image.sprite=Sprite("pets-page-arrow-v6");image.preserveAspect=true;if(left)rect.localScale=new Vector3(-1,1,1);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;return button;
        }
        static Button Action(RectTransform parent,string name,float x,float y,float w,float h,Color color)
        {
            var rect=Top(parent,name,x,y,w,h);var image=rect.gameObject.AddComponent<Image>();image.sprite=Skin();image.type=Image.Type.Sliced;image.color=color;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;var colors=button.colors;colors.disabledColor=Color.white;button.colors=colors;return button;
        }
        static void Rule(RectTransform parent,string name,float x,float y,float w,float h){var line=Top(parent,name,x,y,w,h).gameObject.AddComponent<Image>();line.color=new Color(.13f,.26f,.43f,.7f);line.raycastTarget=false;}
        static Image Picture(RectTransform parent,string name,string id,float x,float y,float w,float h,bool aspect=false,bool top=true){var rect=top?Top(parent,name,x,y,w,h):Box(parent,name,x,y,w,h);var image=rect.gameObject.AddComponent<Image>();image.sprite=Sprite(id);image.preserveAspect=aspect;image.raycastTarget=false;return image;}
        static Text Text(RectTransform parent,string name,string value,float x,float y,float w,float h,int size,Color color,bool top=true)
        {
            var rect=top?Top(parent,name,x,y,w,h):Box(parent,name,x,y,w,h);var text=rect.gameObject.AddComponent<Text>();text.font=font;text.text=value;text.fontSize=size;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Overflow;text.raycastTarget=false;
            var shadow=rect.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,.05f,.55f);shadow.effectDistance=new Vector2(1,-1.5f);return text;
        }
        static void Body(Text text){text.font=bodyFont;text.fontStyle=FontStyle.Bold;text.alignment=TextAnchor.UpperLeft;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;}
        static void Fit(Text text,int min,int max){text.resizeTextForBestFit=true;text.resizeTextMinSize=min;text.resizeTextMaxSize=max;}
        static RectTransform Box(RectTransform parent,string name,float x,float y,float w,float h){var rect=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(w,h);return rect;}
        static RectTransform Top(RectTransform parent,string name,float x,float y,float w,float h){var rect=Box(parent,name,0,0,w,h);rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(x-parent.rect.width*.5f,-y);return rect;}
        static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        static Sprite Skin()=>AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        static Sprite Sprite(string id)=>AssetDatabase.LoadAssetAtPath<Sprite>(RoyalPostmanArtPaths.Sprite(id))??throw new InvalidOperationException("Missing sprite: "+id);
    }
}
