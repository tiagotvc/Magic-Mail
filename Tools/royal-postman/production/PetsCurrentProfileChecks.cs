using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.EditorTools;
using RoyalAves.UI.RoyalPostman2026;

public static class PetsCurrentProfileChecks
{
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    public static void Run(RoyalPostmanRuntimeKit kit,string output)
    {
        foreach(var size in new[]{new Vector2Int(360,800),new Vector2Int(720,1280),new Vector2Int(1080,1920)})using(var d=new RoyalPostmanScreenDocument(kit,"pets",size.x,size.y)){
            var owner=d.Root.GetComponent<RoyalPostmanPetsScreen>();owner.Initialize(()=>{},i=>{});var view=owner.currentPanel.GetComponent<RoyalPostmanCurrentPet>();
            Check(owner.defaultCurrentTab&&owner.currentPanel.activeSelf&&!owner.workshop.activeSelf,"Current must open by default");
            Check(!d.Root.transform.Find("Inventory").gameObject.activeSelf&&!d.Root.transform.Find("Divider").gameObject.activeSelf,"Incubation inventory leaks into profile");
            Check(view.pets.Length==3&&view.heroName.text=="Lumi"&&view.lockedTabs.All(b=>!b.interactable),"Initial profile/locked categories incorrect");
            Check(view.rarityBadges.Length==3&&view.cardBackgrounds.Length==3&&view.cardStatuses.Length==3,"Painted profile variants missing");
            Check(view.xpFill.type==Image.Type.Sliced&&view.energyFill.type==Image.Type.Sliced&&view.xpFill.sprite.border.x>0,"Progress caps must be preserved by sliced sprites");
            Check(view.xpFill.sprite.name=="pets-progress-blue-v16"&&view.energyFill.sprite.name=="pets-progress-green-v16","Painted progress fills missing");
            float maxWidth=((RectTransform)view.xpFill.transform.parent).rect.width-10;
            Check(Mathf.Abs(view.xpFill.rectTransform.rect.width-maxWidth*.72f)<.1f,"XP does not follow pet data");
            int initialXP=view.pets[0].xp;view.pets[0].xp=0;view.Refresh();Check(!view.xpFill.enabled,"Zero progress still paints fill");view.pets[0].xp=1000;view.Refresh();Check(view.xpFill.enabled&&Mathf.Abs(view.xpFill.rectTransform.rect.width-maxWidth)<.1f,"Full progress loses its available width");view.pets[0].xp=initialXP;view.Refresh();
            Check(view.lockedTabs.All(b=>b.GetComponent<Image>().sprite.name=="pets-tab-inactive-v16"&&b.transform.Find("Icon")!=null&&b.transform.Find("Lock")!=null),"Locked tabs must retain painted icons and separate padlocks");
            Check(d.Root.transform.Find("CurrentProfile/Divider/Ornament").GetComponent<Image>().sprite==d.Root.transform.Find("Divider/Ornament").GetComponent<Image>().sprite,"Profile must reuse the exact divider asset");
            d.Export(output+"/pets-current-lumi-"+size.x+".png");
            for(int i=0;i<3;i++){
                view.cards[i].button.onClick.Invoke();Check(view.SelectedIndex==i&&view.hero.sprite==view.pets[i].portrait&&view.passiveName.text==view.pets[i].passiveName&&view.magicName.text==view.pets[i].magicName,"Per-pet binding does not follow card selection");
                foreach(var card in view.cards){var pet=view.pets.First(p=>p.displayName==card.name.text);Check(card.rarityFace.sprite==view.rarityBadges[(int)pet.rarity]&&card.paintedBackdrop.sprite==view.cardBackgrounds[(int)pet.rarity],"Painted rarity assets do not follow sorted pet data");}
                d.Render();foreach(var card in view.cards){var r=d.Bounds(card.button.GetComponent<RectTransform>());Check(r.xMin>=0&&r.xMax<=size.x&&r.yMin>=0&&r.yMax<=size.y,"Collection card clipped");}
                Check(view.passiveDescription.preferredHeight<=view.passiveDescription.rectTransform.rect.height+1&&view.magicDescription.preferredHeight<=view.magicDescription.rectTransform.rect.height+1,"Skill description clips");
            }
            view.next.onClick.Invoke();Check(view.SelectedIndex==0,"Hero arrow failed to wrap");view.previous.onClick.Invoke();Check(view.SelectedIndex==2,"Previous hero arrow failed");
            view.equip.onClick.Invoke();Check(view.equipLabel.text=="Equipado"&&!view.equip.interactable&&view.cards[2].status.text.Contains("Equipado"),"Mock equipment presentation not synchronized");
            view.passiveInfo.onClick.Invoke();Check(owner.itemTooltip.activeSelf&&owner.hintTitle.text==view.pets[2].passiveName,"Skill info does not match current pet");owner.HideItemTooltip();
            owner.SelectItem(0);view.passiveInfo.onClick.Invoke();owner.BindInventory(new RoyalAves.Meta.PetsIncubationState(),()=>{},cost=>false,()=>0,()=>1700000000);Check(owner.hintTitle.text==view.pets[2].passiveName,"Inventory refresh overwrote pet skill tooltip");owner.HideItemTooltip();
            view.sort.onClick.Invoke();Check(view.sortMenu.activeSelf,"Sort menu failed");view.sortName.onClick.Invoke();Check(!view.sortMenu.activeSelf&&view.cards[0].name.text=="Biscoito","Name sort failed");
            view.ResetPreview();d.PreviewClick(view.cards[1].button.GetComponent<RectTransform>());Check(view.SelectedIndex==1,"Editor pet selection differs from runtime");
            if(size.x==720)d.Export(output+"/pets-current-foxy-720.png");
            owner.incubatorTab.onClick.Invoke();Check(owner.workshop.activeSelf&&!owner.currentPanel.activeSelf&&d.Root.transform.Find("Inventory").gameObject.activeSelf,"Incubator tab did not restore authored view");
            owner.currentTab.onClick.Invoke();Check(owner.currentPanel.activeSelf,"Current tab did not return");
        }
        using(var d=new RoyalPostmanScreenDocument(kit,"pets",720,1280)){
            var owner=d.Root.GetComponent<RoyalPostmanPetsScreen>();var view=owner.currentPanel.GetComponent<RoyalPostmanCurrentPet>();view.SelectPet(2);owner.ShowCurrent(false);d.Save();
            using(var fresh=new RoyalPostmanScreenDocument(kit,"pets",720,1280)){var next=fresh.Root.GetComponent<RoyalPostmanPetsScreen>();Check(next.currentPanel.activeSelf&&next.currentPanel.GetComponent<RoyalPostmanCurrentPet>().heroName.text=="Lumi","Save did not restore default profile presentation");}
        }
        using(var detail=new RoyalPostmanScreenDocument(kit,"pets",2160,3840)){detail.Root.GetComponent<RoyalPostmanPetsScreen>().Initialize(()=>{},i=>{});detail.Export(output+"/pets-current-detail-2160.png");}
    }
}
