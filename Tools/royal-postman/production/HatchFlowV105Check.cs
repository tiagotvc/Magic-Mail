using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RoyalAves.Meta;
using RoyalAves.EditorTools;
using RoyalAves.UI.RoyalPostman2026;

public static class HatchFlowV105Check
{
    const string Output="../hatch-flow-v105";
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Click(RoyalPostmanScreenDocument d,Button button)
    {
        d.Render();var eventData=new PointerEventData(EventSystem.current){position=d.Camera.WorldToScreenPoint(button.transform.position)};var hits=new System.Collections.Generic.List<RaycastResult>();d.Root.GetComponentInParent<GraphicRaycaster>().Raycast(eventData,hits);Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Actual UI hit blocked: "+button.name);button.onClick.Invoke();
    }
    static void StateChecks()
    {
        long now=1700000000;var rules=new PetsIncubationRules();
        for(int kind=0;kind<4;kind++)
        {
            var s=new PetsIncubationState{commonEggs=2,rareEggs=2,epicEggs=2,legendaryEggs=2};Check(s.IncubateSlot(0,kind,100,30,rules,now),"Cannot start egg tier");var entry=new PetHatchCatalog.Entry{eggKind=kind,petId="test-"+kind,portugueseName="Teste",rarity=kind};
            Check(!s.TryHatch(0,now,entry,out _)&&!s.TryHatch(-1,long.MaxValue,entry,out _),"Unfinished or invalid egg granted");Check(!s.TryHatch(0,s.finishes[0],null,out _),"Missing catalogue consumed egg");
            Check(s.TryHatch(0,s.finishes[0],entry,out var receipt)&&receipt.isNew&&receipt.eggKind==kind&&!s.Occupied(0)&&s.hatchedPets[0].copies==1,"Hatch did not grant and release slot");Check(!s.TryHatch(0,long.MaxValue,entry,out _)&&s.hatchedPets[0].copies==1,"Duplicate hatch grant");
            var loaded=JsonUtility.FromJson<PetsIncubationState>(JsonUtility.ToJson(s));loaded.Normalize();Check(loaded.pendingHatches[0].receiptId==receipt.receiptId&&loaded.hatchedPets[0].copies==1,"Reload lost pending presentation");Check(loaded.AcknowledgeHatch(receipt.receiptId)&&!loaded.AcknowledgeHatch(receipt.receiptId)&&loaded.hatchedPets[0].copies==1,"Acknowledgement grants twice");
            Check(loaded.IncubateSlot(0,kind,100,30,rules,now)&&loaded.TryHatch(0,loaded.finishes[0],entry,out var second)&&!second.isNew&&loaded.hatchedPets[0].copies==2,"Duplicate mascot copy lost");
        }
        var old=JsonUtility.FromJson<PetsIncubationState>("{\"commonEggs\":1,\"started\":[0,0,0,10],\"finishes\":[0,0,0,20]}");old.Normalize();Check(old.Occupied(3)&&old.finishes[3]==20&&old.pendingHatches!=null&&old.hatchedPets!=null,"Old save migration changed legacy timer");
        float area=0;foreach(var polygon in RoyalPostmanEggShard.Pieces()){float sum=0;for(int i=0;i<polygon.Length;i++){var a=polygon[i];var b=polygon[(i+1)%polygon.Length];sum+=a.x*b.y-b.x*a.y;}area+=Mathf.Abs(sum)*.5f;}Check(Mathf.Abs(area-1)<.0001f,"Shell pieces do not cover the egg UV rectangle");
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        try
        {
            StateChecks();var kit=ScriptableObject.CreateInstance<RoyalPostmanRuntimeKit>();kit.additionalScreens.Add(new RoyalPostmanRuntimeKit.Screen{id="pets",prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab"),hideNavigation=true});
            try
            {
                using(var d=new RoyalPostmanScreenDocument(kit,"pets",720,1280){ReadOnly=true})
                {
                    var v=d.Root.GetComponent<RoyalPostmanPetsScreen>();v.Initialize(()=>{},i=>{});var h=v.hatchery;var catalog=ScriptableObject.CreateInstance<PetHatchCatalog>();catalog.entries=new PetHatchCatalog.Entry[4];
                    for(int i=0;i<4;i++)catalog.entries[i]=new PetHatchCatalog.Entry{eggKind=i,petId="test-"+i,portugueseName="Prévia de mascote",englishName="Pet preview",rarity=i,portrait=v.currentPanel.GetComponent<RoyalPostmanCurrentPet>().hero.sprite};
                    h.ConfigureSimulation(catalog);int saves=0;long now=1700000000;bool failSave=false;var state=new PetsIncubationState{commonEggs=3,rareEggs=3,epicEggs=3,legendaryEggs=3,accelerationItems=new[]{3,0,0,0}};
                    v.BindInventory(state,()=>{if(failSave)throw new IOException("Test save failure");saves++;},n=>false,()=>0,()=>now);v.RefreshAvailability(100);v.ShowCurrent(false);
                    Check(!h.egg.gameObject.activeSelf&&!h.incubate.interactable,"Empty slot shows an egg or enables start");Click(d,v.items[0].button);Check(h.egg.gameObject.activeSelf&&h.incubate.interactable&&state.commonEggs==3,"Selection consumes inventory or is invisible");Click(d,h.egg.GetComponent<Button>());Check(!h.egg.gameObject.activeSelf&&state.commonEggs==3&&saves==0,"Removing staged egg changed inventory/save");
                    v.items[1].button.onClick.Invoke();h.SelectSpace(1);Check(h.SelectedEgg<0,"Staged selection leaks between slots");h.SelectSpace(0);Check(h.SelectedEgg==1,"Slot forgot staged egg");h.incubate.onClick.Invoke();h.incubate.onClick.Invoke();Check(state.rareEggs==2&&saves==1&&h.particles.IsRunning&&h.particles.motes.Length>=12,"Start/save/warm particles broken");Check(!h.RemoveSelectedEgg()&&state.Occupied(0),"Running egg removed without cancel");h.cancel.onClick.Invoke();h.cancel.onClick.Invoke();Check(state.rareEggs==3&&saves==2&&!h.particles.IsRunning&&!h.egg.gameObject.activeSelf,"Cancel duplicate refund or warm effect retained");
                    for(int kind=0;kind<4;kind++)
                    {
                        int before=saves;v.items[kind].button.onClick.Invoke();h.incubate.onClick.Invoke();Check(saves==before+1&&state.Occupied(0),"Tier cannot start");Check(h.display.Value==RoyalPostmanPetsScreen.FormatIncubationTime(v.incubationRules.EggSeconds(kind)),"Timer duration does not match egg");now=state.finishes[0];h.Refresh();
                        Check(h.Reveal!=null&&h.Reveal.IsOpen&&state.hatchedPets.Count==kind+1&&state.pendingHatches.Count==1&&saves==before+2,"Timer end did not present exactly one durable grant");var id=state.pendingHatches[0].receiptId;h.Refresh();Check(saves==before+2&&state.hatchedPets[kind].copies==1,"Refresh granted twice");
                        foreach(float time in new[]{.6f,1.6f,2.3f,3.5f}){h.Reveal.Sample(time);d.Render();}
                        Check(h.Reveal.Ready,"Reveal did not reach continue state");if(kind==0){v.Close();Check(!h.Reveal.IsOpen&&state.pendingHatches[0].receiptId==id,"Closing discarded pending reward");v.Open();Check(h.Reveal.IsOpen&&state.hatchedPets[0].copies==1&&saves==before+2,"Reopen duplicated reward");h.Reveal.Sample(4);}
                        h.Reveal.Continue();h.Reveal.Continue();Check(state.pendingHatches.Count==0&&saves==before+3,"Continue duplicated save or did not acknowledge");
                    }
                    v.items[0].button.onClick.Invoke();h.incubate.onClick.Invoke();now=state.finishes[0];failSave=true;bool caught=false;try{h.Refresh();}catch(IOException){caught=true;}failSave=false;Check(caught&&state.Occupied(0)&&state.pendingHatches.Count==0,"Save failure lost incubation");h.Refresh();Check(h.Reveal.IsOpen,"Save retry failed");h.Reveal.Sample(4);h.Reveal.Continue();
                    h.ConfigureSimulation(null);v.items[0].button.onClick.Invoke();int eggs=state.commonEggs;h.incubate.onClick.Invoke();Check(!state.Occupied(0)&&state.commonEggs==eggs&&!h.incubate.interactable,"Unconfigured real rewards consumed an egg");
                    foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(720,1280)}){d.Resize(size.x,size.y);d.Render();}
                    bool readOnly=false;try{d.Save();}catch(InvalidOperationException){readOnly=true;}Check(readOnly,"Simulation can overwrite production prefab");UnityEngine.Object.DestroyImmediate(catalog);
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(kit);}
            File.WriteAllText(Output+"/review.txt","PASS: native compile/render; all4 egg tiers select/remove/start/timer/heat/cancel; staged selections isolated by slot; no duplicate debit/grant/save; durable hatch receipts and owned-copy counts; close/reopen resume without granting again; save-failure rollback; missing real catalogue keeps stock; legacy save preserved; shell UV area; reveal crack/explosion/pet frames; 390/720 render; read-only sandbox cannot save prefab.\n");EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Output+"/review.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
