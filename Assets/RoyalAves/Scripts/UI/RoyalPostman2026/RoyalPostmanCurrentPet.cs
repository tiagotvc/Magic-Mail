using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    // Editable sample catalogue/presentation. It does not grant pets or apply combat effects.
    public sealed class RoyalPostmanCurrentPet : MonoBehaviour
    {
        public enum Rarity { Comum, Raro, Épico }
        [Serializable] public sealed class Pet
        {
            public string id,displayName;public Rarity rarity;public Sprite portrait;
            public int level=1,xp,xpMax=1000,energy=80,energyMax=100;
            public string passiveName,magicName;
            [TextArea] public string passiveDescription,magicDescription;
            public Sprite passiveIcon,magicIcon;
        }
        [Serializable] public sealed class Card
        {
            public Button button;public Image portrait,rarityFace,statusFace,paintedBackdrop;public RoyalPostmanGradient backdrop;public Text name,rarity,status;public Outline selected;
        }
        public RoyalPostmanPetsScreen owner;
        public Pet[] pets;public Card[] cards;
        public Image hero,xpFill,energyFill,passiveIcon,magicIcon;public Graphic equipFace,heroRarityFace;
        public GameObject equipCheck,sortMenu;
        public Text heroName,heroRarity,level,xp,energy,passiveName,passiveDescription,magicName,magicDescription,equipLabel,sortLabel;
        public Button previous,next,collectionPrevious,collectionNext,equip,feed,petInfo,passiveInfo,magicInfo,sort,sortRarity,sortName,sortLevel;
        public Button[] dots,lockedTabs;
        public Sprite[] rarityBadges,cardBackgrounds,cardStatuses;
        public Sprite equippedCardStatus,equipReadyFace,equipIdleFace;
        public Action<string> EquipRequested;
        int selectedIndex,equippedIndex;int[] order;bool initialized;
        public int SelectedIndex=>selectedIndex;
        public void Initialize(RoyalPostmanPetsScreen view)
        {
            owner=view;if(initialized)return;initialized=true;
            previous.onClick.AddListener(()=>Step(-1));next.onClick.AddListener(()=>Step(1));
            collectionPrevious.onClick.AddListener(()=>Step(-1));collectionNext.onClick.AddListener(()=>Step(1));
            for(int i=0;i<cards.Length;i++){int slot=i;cards[i].button.onClick.AddListener(()=>SelectPet(Order()[slot]));}
            for(int i=0;i<dots.Length;i++){int index=i;dots[i].onClick.AddListener(()=>SelectPet(Order()[index]));}
            equip.onClick.AddListener(Equip);petInfo.onClick.AddListener(()=>Info(0));passiveInfo.onClick.AddListener(()=>Info(1));magicInfo.onClick.AddListener(()=>Info(2));
            sort.onClick.AddListener(ToggleSort);sortRarity.onClick.AddListener(()=>Sort(0));sortName.onClick.AddListener(()=>Sort(1));sortLevel.onClick.AddListener(()=>Sort(2));
            Refresh();
        }
        int[] Order(){if(order==null||order.Length!=pets.Length)order=Enumerable.Range(0,pets.Length).ToArray();return order;}
        public void SelectPet(int index)
        {
            if(index<0||index>=pets.Length)return;selectedIndex=index;owner?.HideItemTooltip();sortMenu.SetActive(false);Refresh();
        }
        public void Step(int delta)
        {
            var indices=Order();int at=Array.IndexOf(indices,selectedIndex);SelectPet(indices[(at+delta%indices.Length+indices.Length)%indices.Length]);
        }
        void Equip(){if(selectedIndex==equippedIndex)return;equippedIndex=selectedIndex;EquipRequested?.Invoke(pets[selectedIndex].id);Refresh();}
        void ToggleSort(){sortMenu.SetActive(!sortMenu.activeSelf);owner?.HideItemTooltip();}
        void Sort(int mode)
        {
            var indices=Enumerable.Range(0,pets.Length);
            order=(mode==0?indices.OrderBy(i=>pets[i].rarity):mode==1?indices.OrderBy(i=>pets[i].displayName):indices.OrderByDescending(i=>pets[i].level)).ToArray();
            sortLabel.text=mode==0?"Raridade":mode==1?"Nome":"Nível";sortMenu.SetActive(false);Refresh();
        }
        void Info(int type)
        {
            var pet=pets[selectedIndex];var button=type==0?petInfo:type==1?passiveInfo:magicInfo;
            owner?.ShowDetails(type==0?pet.displayName+" · "+pet.rarity:type==1?pet.passiveName:pet.magicName,
                type==0?"Nível "+pet.level+"\nEnergia: "+pet.energy+" / "+pet.energyMax+"\nMascote de exemplo.":type==1?pet.passiveDescription:pet.magicDescription,
                type==0?pet.portrait:type==1?pet.passiveIcon:pet.magicIcon,(RectTransform)button.transform);
        }
        public void Refresh()
        {
            if(pets==null||pets.Length==0)return;selectedIndex=Mathf.Clamp(selectedIndex,0,pets.Length-1);var pet=pets[selectedIndex];
            bool painted=rarityBadges!=null&&rarityBadges.Length==3;
            hero.sprite=pet.portrait;heroName.text=pet.displayName;heroRarity.text=pet.rarity.ToString();heroRarityFace.color=painted?Color.white:RarityColor(pet.rarity);level.text="Nv. "+pet.level;
            xp.text=pet.xp+" / "+pet.xpMax+" XP";energy.text=pet.energy+" / "+pet.energyMax;
            SetProgress(xpFill,(float)pet.xp/Mathf.Max(1,pet.xpMax));SetProgress(energyFill,(float)pet.energy/Mathf.Max(1,pet.energyMax));
            passiveIcon.sprite=pet.passiveIcon;magicIcon.sprite=pet.magicIcon;passiveName.text=pet.passiveName;passiveDescription.text=pet.passiveDescription;magicName.text=pet.magicName;magicDescription.text=pet.magicDescription;
            bool equipped=selectedIndex==equippedIndex;equipLabel.text=equipped?"Equipado":"Equipar";equip.interactable=!equipped;equipCheck.SetActive(equipped);
            equipFace.color=painted?Color.white:equipped?new Color(.015f,.04f,.09f):new Color(.025f,.38f,.85f);if(painted&&equipFace is Image equipImage)equipImage.sprite=equipped?equipIdleFace:equipReadyFace;feed.interactable=false;
            var indices=Order();
            for(int i=0;i<cards.Length;i++){
                int data=indices[i%indices.Length];var card=cards[i];var item=pets[data];
                card.portrait.sprite=item.portrait;card.name.text=item.displayName;card.rarity.text=item.rarity.ToString();card.status.text=data==equippedIndex?"✓ Equipado":item.rarity.ToString();card.selected.enabled=data==selectedIndex;
                if(painted){int rarityIndex=(int)item.rarity;card.rarityFace.sprite=rarityBadges[rarityIndex];card.rarityFace.color=Color.white;card.statusFace.sprite=data==equippedIndex?equippedCardStatus:cardStatuses[rarityIndex];card.statusFace.color=Color.white;card.paintedBackdrop.sprite=cardBackgrounds[rarityIndex];}
                else{card.rarityFace.color=RarityColor(item.rarity);card.statusFace.color=data==equippedIndex?new Color(.02f,.5f,1):RarityColor(item.rarity);
                    card.backdrop.top=item.rarity==Rarity.Raro?new Color(.04f,.7f,1):item.rarity==Rarity.Comum?new Color(1,.7f,.04f):new Color(.6f,.02f,1);
                    card.backdrop.bottom=item.rarity==Rarity.Raro?new Color(.04f,.12f,.65f):item.rarity==Rarity.Comum?new Color(.5f,.2f,.015f):new Color(.22f,.015f,.5f);card.backdrop.SetVerticesDirty();}
            }
            for(int i=0;i<dots.Length;i++)dots[i].targetGraphic.color=indices[i]==selectedIndex?new Color(1,.78f,.04f):new Color(.47f,.39f,.3f);
            foreach(var tab in lockedTabs)tab.interactable=false;
        }
        static void SetProgress(Image fill,float value)
        {
            value=Mathf.Clamp01(value);fill.fillAmount=value;
            if(fill.type!=Image.Type.Sliced)return;
            // Resize the sliced sprite, preserving both rounded caps instead of cutting its right edge.
            var rect=fill.rectTransform;var parent=(RectTransform)rect.parent;float available=Mathf.Max(0,parent.rect.width-10);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,available*value);fill.enabled=value>0;
        }
        static Color RarityColor(Rarity rarity)=>rarity==Rarity.Comum?new Color(.23f,.25f,.28f):rarity==Rarity.Raro?new Color(.02f,.45f,1):new Color(.57f,.02f,.95f);
        public void ResetPreview(){selectedIndex=equippedIndex=0;order=null;sortLabel.text="Raridade";sortMenu.SetActive(false);Refresh();}
        static bool Hit(Transform pick,Button button)=>button!=null&&(pick==button.transform||pick.IsChildOf(button.transform));
        public bool PreviewClick(Transform pick)
        {
            if(pick==null)return false;
            for(int i=0;i<cards.Length;i++)if(Hit(pick,cards[i].button)){SelectPet(Order()[i]);return true;}
            for(int i=0;i<dots.Length;i++)if(Hit(pick,dots[i])){SelectPet(Order()[i]);return true;}
            if(Hit(pick,previous)||Hit(pick,collectionPrevious)){Step(-1);return true;}
            if(Hit(pick,next)||Hit(pick,collectionNext)){Step(1);return true;}
            if(Hit(pick,equip)){Equip();return true;}
            if(Hit(pick,petInfo)){Info(0);return true;}if(Hit(pick,passiveInfo)){Info(1);return true;}if(Hit(pick,magicInfo)){Info(2);return true;}
            if(Hit(pick,sort)){ToggleSort();return true;}if(Hit(pick,sortRarity)){Sort(0);return true;}if(Hit(pick,sortName)){Sort(1);return true;}if(Hit(pick,sortLevel)){Sort(2);return true;}
            return false;
        }
    }
}
