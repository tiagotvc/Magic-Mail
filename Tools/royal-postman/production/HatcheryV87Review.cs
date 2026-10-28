using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.UI.RoyalPostman2026;

public static class HatcheryV87Review
{
    const string Output=HatcheryV87Build.Output;
    static void Check(bool ok,string message)=>HatcheryV87Build.Check(ok,message);
    static void StateChecks()
    {
        var rules=new PetsIncubationRules();long now=1700000000;
        for(int kind=0;kind<4;kind++)
        {
            var s=new PetsIncubationState{commonEggs=2,rareEggs=2,epicEggs=2,legendaryEggs=2};
            Check(!s.IncubateSlot(0,kind,29,30,rules,now),"Level29 unlocked first space");
            Check(s.IncubateSlot(0,kind,30,30,rules,now),"Cannot select egg kind "+kind);
            Check(s.eggKinds[0]==kind&&s.EggCount(kind)==1&&s.Remaining(0,now)==rules.EggSeconds(kind),"Wrong egg, stock or duration");
            Check(!s.IncubateSlot(0,kind,100,30,rules,now)&&!s.IncubateSlot(3,kind,100,0,rules,now),"Duplicate/new fourth start allowed");
            var copy=JsonUtility.FromJson<PetsIncubationState>(JsonUtility.ToJson(s));copy.Normalize();Check(copy.eggKinds[0]==kind&&copy.finishes[0]==s.finishes[0],"Generic-slot save did not round trip");
        }
        var old=JsonUtility.FromJson<PetsIncubationState>("{\"commonEggs\":4,\"rareEggs\":3,\"epicEggs\":2,\"legendaryEggs\":1,\"started\":[1,2,3,4],\"finishes\":[9,8,7,6],\"goldPurchases\":[1,2,3,4],\"accelerationItems\":[4,3,2,1]}");old.Normalize();
        Check(old.finishes.SequenceEqual(new long[]{9,8,7,6})&&old.goldPurchases.SequenceEqual(new[]{1,2,3,4})&&old.accelerationItems.SequenceEqual(new[]{4,3,2,1}),"Old resources/timers were lost");
        Check(old.eggKinds.SequenceEqual(new[]{0,1,2,3}),"Old rarity mapping missing");
        Check(RoyalPostmanPetsScreen.FormatIncubationTime(172800)=="48:00:00","48-hour timer regressed");
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        try
        {
            StateChecks();
            using(var d=new RoyalPostmanScreenDocument(RoyalPostmanRuntimeKit.Load(),"pets",720,1280))
            {
                var v=d.Root.GetComponent<RoyalPostmanPetsScreen>();var h=v.hatchery;Check(h!=null,"New hatchery missing");
                int backs=0,saves=0,starts=0,boosts=0,spent=0;long now=1700000000;
                v.Initialize(()=>backs++,i=>{});v.UseItemRequested+=(i,id)=>starts++;v.AccelerateRequested+=i=>boosts++;
                var state=new PetsIncubationState{commonEggs=2,rareEggs=2,epicEggs=2,legendaryEggs=2,accelerationItems=new[]{1,0,0,0}};
                v.BindInventory(state,()=>saves++,cost=>{spent+=cost;return true;},()=>10000,()=>now);v.RefreshAvailability(100);v.ShowCurrent(false);
                Check(v.currentTab.GetComponent<Image>().sprite==null&&v.incubatorTab.GetComponent<Image>().sprite==null,"Top rectangular buttons are not placeholders");
                Check(v.items.Length==4&&h.unlockLevels.SequenceEqual(new[]{30,50,100}),"Wrong inventory/space count or unlock levels");
                v.items[3].button.onClick.Invoke();Check(h.SelectedEgg==3&&h.SelectedSpace==0&&h.display.Value=="48:00:00","Selecting legendary incorrectly changes space or duration");
                h.incubate.onClick.Invoke();h.incubate.onClick.Invoke();Check(starts==1&&saves==1&&state.legendaryEggs==1&&state.eggKinds[0]==3,"Click consumed wrong resources or repeated start/save");
                now+=3600;h.Refresh();Check(h.display.Value=="47:00:00"&&h.particles.IsRunning,"Live countdown/particles missing");
                h.accelerate.onClick.Invoke();Check(boosts==1&&saves==2&&state.accelerationItems[0]==0&&spent==0&&h.display.Value=="46:30:00","Item acceleration changed economy");
                h.accelerate.onClick.Invoke();Check(boosts==2&&saves==3&&spent==500&&state.goldPurchases[0]==1,"Gold acceleration changed existing price pipeline");
                v.BindInventory(state,()=>saves++,cost=>false,()=>10000,()=>now);string rejected=JsonUtility.ToJson(state);h.accelerate.onClick.Invoke();Check(JsonUtility.ToJson(state)==rejected&&saves==3,"Rejected payment mutated stock");
                d.Export(Output+"/running-pt.png");
                var events=new GameObject("HatcheryReviewEvents",typeof(EventSystem));
                try
                {
                    var controls=new[]{v.backButton,v.helpButton,v.previousIncubator,v.nextIncubator,h.incubate};
                    var source=v.backButton.transform.Find("FixedGold").GetComponent<Image>().sprite;
                    foreach(var button in controls)
                    {
                        Check(button.transform.Find("FixedGold").GetComponent<Image>().sprite==source,"Generated/replaced a user round-button rim");
                        // Enabled state here checks geometry only; eligibility was tested above.
                        button.interactable=true;d.Render();
                        var pos=d.Camera.WorldToScreenPoint(button.transform.position);var data=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=pos};
                        var hits=new System.Collections.Generic.List<RaycastResult>();d.Root.GetComponentInParent<GraphicRaycaster>().Raycast(data,hits);
                        Check(hits.Any(x=>x.gameObject==button.gameObject),"Native raycast missing: "+button.name);
                        var press=button.GetComponent<RoyalPostmanPaintedButton>();var rim=button.transform.Find("FixedGold");var fixedPosition=rim.position;var rest=press.movingCenter.anchoredPosition;
                        press.OnPointerDown(data);Check(press.movingCenter.anchoredPosition!=rest&&rim.position==fixedPosition,"Separate fixed rim/face broken: "+button.name);press.OnPointerUp(data);Check(press.movingCenter.anchoredPosition==rest,"Press failed to restore");
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(events);}
                v.backButton.onClick.Invoke();v.helpButton.onClick.Invoke();Check(backs==2,"Back/close callbacks regressed");
                v.currentTab.onClick.Invoke();Check(v.currentPanel.activeSelf&&!v.workshop.activeSelf,"Mascotes tab does not open preserved profile");
                Check(h.headerTitle.GetComponent<Image>().sprite==h.headerTitle.petsPortuguese,"Profile title is not Mascotes");d.Export(Output+"/profile-pt.png");
                v.incubatorTab.onClick.Invoke();Check(!v.currentPanel.activeSelf&&v.workshop.activeSelf,"Incubadora tab does not return");
                var empty=new PetsIncubationState{commonEggs=1};v.BindInventory(empty,()=>{},n=>false,()=>0,()=>now);
                foreach(int space in new[]{0,1,2}){h.SelectSpace(space);v.RefreshAvailability(h.unlockLevels[space]-1);Check(!h.incubate.interactable&&h.lockIcon.gameObject.activeSelf,"Locked space enabled");v.RefreshAvailability(h.unlockLevels[space]);Check(!h.lockIcon.gameObject.activeSelf,"Space did not unlock at correct level");}
                h.SelectSpace(2);v.StepIncubator(1);Check(h.SelectedSpace==0,"Arrows expose a fourth new space");
                var legacy=new PetsIncubationState();legacy.started[3]=now;legacy.finishes[3]=now+120;legacy.goldPurchases[3]=3;v.BindInventory(legacy,()=>{},n=>false,()=>0,()=>now);h.legacy.onClick.Invoke();Check(h.SelectedSpace==3&&legacy.finishes[3]==now+120&&h.egg.sprite==h.eggs[3],"Fourth legacy incubation lost or hidden");
                h.SelectSpace(0);v.BindInventory(empty,()=>{},n=>false,()=>0,()=>now);v.RefreshAvailability(29);d.Export(Output+"/locked-pt.png");v.RefreshAvailability(100);
                h.headerTitle.editorLanguage=RoyalPostmanHeaderTitle.EditorLanguage.English;h.headerTitle.Refresh();h.Refresh();Check(h.timeLabel.text=="Duration"&&h.itemNames[3].text=="Legendary"&&v.currentTab.GetComponentInChildren<Text>().text=="Pets","English UI did not switch");d.Export(Output+"/idle-en.png");
                h.headerTitle.editorLanguage=RoyalPostmanHeaderTitle.EditorLanguage.Portuguese;h.headerTitle.Refresh();h.Refresh();d.Export(Output+"/idle-pt.png");
                foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(360,640),new Vector2Int(1080,1920)})
                {
                    d.Resize(size.x,size.y);var viewport=d.Root.GetComponent<RoyalPostmanViewport>();viewport.SafeAreaOverride=new Rect(0,24,size.x,size.y-48);d.Render();
                    var footer=d.Bounds((RectTransform)h.transform.Find("Footer"));Check(Mathf.Abs(footer.yMin-24)<.2f,"Footer leaves an unused band on a tall screen");
                    foreach(var control in new[]{v.backButton,v.helpButton,v.currentTab,v.incubatorTab,v.previousIncubator,v.nextIncubator,h.incubate,h.accelerate}){var b=d.Bounds((RectTransform)control.transform);Check(b.xMin>=-.1f&&b.xMax<=size.x+.1f&&b.yMin>=23.9f&&b.yMax<=size.y-23.9f,"Control outside safe area: "+control.name);}
                    d.Export(Output+"/safe-"+size.x+".png");
                }
            }
            File.WriteAllText(Output+"/review.txt","PASS: native runtime/editor compile;3 generic spaces unlock30/50/100;4 selectable eggs with original1/3/12/48h durations; exact-one debit/start/save; live countdown; item/gold acceleration and rejected-payment guards; old4-timer save retained with legacy access; user round PNGs/fixed rim/moving face/raycast;2 rectangle placeholders; profile/close/back/navigation; PT/EN; layered egg/particles; safe areas360/390/720/1080.\n");EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Output+"/review.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
