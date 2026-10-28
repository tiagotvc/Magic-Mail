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

public static class HatcheryV121Check
{
    const string Output="../hatchery-v121";
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Click(RoyalPostmanScreenDocument d,Button b)
    {
        Check(b.IsInteractable()&&b.gameObject.activeInHierarchy,"Inactive control: "+b.name);d.Render();
        var data=new PointerEventData(EventSystem.current){position=d.Camera.WorldToScreenPoint(b.transform.position)};var hits=new System.Collections.Generic.List<RaycastResult>();d.Root.GetComponentInParent<GraphicRaycaster>().Raycast(data,hits);
        Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b,"Raycast blocked: "+b.name+" by "+(hits.Count>0?hits[0].gameObject.name:"nothing"));b.onClick.Invoke();
    }
    static void Inside(RoyalPostmanScreenDocument d,RectTransform r)
    {var b=d.Bounds(r);Check(b.xMin>=-1&&b.xMax<=d.Width+1&&b.yMin>=-1&&b.yMax<=d.Height+1,"Unsafe bounds: "+r.name+" "+b);}
    static void DigitalPixels(RoyalPostmanScreenDocument d,RoyalPostmanDigitalClock display)
    {
        d.Render();var bounds=d.Bounds(display.rectTransform);var prior=RenderTexture.active;var pixels=new Texture2D((int)bounds.width,(int)bounds.height,TextureFormat.RGB24,false);
        try{RenderTexture.active=d.Texture;pixels.ReadPixels(new Rect(bounds.xMin,bounds.yMin,pixels.width,pixels.height),0,0);pixels.Apply();Check(pixels.GetPixels().Count(c=>c.r>.7f&&c.g>.3f&&c.g<.95f&&c.b<.8f)>120,"Digital value exists in code but its segments are invisible");}
        finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(pixels);}
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        try
        {
            Check(AssetDatabase.LoadAssetAtPath<PetHatchCatalog>("Assets/RoyalAves/Resources/Pets/PetHatchCatalog.asset").entries.Length==0,"Real catalogue was seeded with provisional pets");
            var kit=ScriptableObject.CreateInstance<RoyalPostmanRuntimeKit>();kit.additionalScreens.Add(new RoyalPostmanRuntimeKit.Screen{id="pets",hideNavigation=true,prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab")});
            try
            {
                using(var d=new RoyalPostmanScreenDocument(kit,"pets",720,1280){ReadOnly=true})
                {
                    var v=d.Root.GetComponent<RoyalPostmanPetsScreen>();v.Initialize(()=>{},i=>{});var h=v.hatchery;var p=h.presentation;Check(p!=null,"Production presentation missing");
                    var catalog=ScriptableObject.CreateInstance<PetHatchCatalog>();catalog.entries=new PetHatchCatalog.Entry[4];for(int i=0;i<4;i++)catalog.entries[i]=new PetHatchCatalog.Entry{eggKind=i,petId="test-"+i,portugueseName="Prévia de mascote",englishName="Pet preview",rarity=i,portrait=v.currentPanel.GetComponent<RoyalPostmanCurrentPet>().hero.sprite};h.ConfigureSimulation(catalog);
                    long now=1700000000;int saves=0,gold=10000;bool failSave=false,rejectGold=false;
                    var state=new PetsIncubationState{commonEggs=4,rareEggs=4,epicEggs=4,legendaryEggs=4,accelerationItems=new[]{3,0,0,0}};
                    v.BindInventory(state,()=>{if(failSave)throw new IOException("Test save failure");saves++;},cost=>{if(rejectGold||gold<cost)return false;gold-=cost;return true;},()=>gold,()=>now);v.RefreshAvailability(100);v.ShowCurrent(false);
                    Check(!v.currentTab.transform.parent.gameObject.activeSelf,"Old navigation tabs still visible");
                    var chamber=(RectTransform)h.transform.Find("Door");Check(chamber.rect.width==chamber.rect.height,"Chamber is not exactly 1:1");
                    Check(!h.egg.gameObject.activeSelf&&!h.incubate.interactable&&!h.accelerate.gameObject.activeSelf&&!p.lamps.activeSelf&&!p.drawerRoot.activeSelf,"Empty state contains running UI");
                    Check(!p.drawerHandle.GetComponentsInChildren<Image>().Any(i=>h.eggs.Contains(i.sprite)),"Closed drawer reveals egg thumbnails");
                    d.Export(Output+"/idle.png");Click(d,p.drawerHandle);Check(p.DrawerOpen&&p.drawerRoot.activeInHierarchy,"Drawer did not open");d.Export(Output+"/drawer.png");Click(d,p.drawerClose);Check(!p.DrawerOpen,"Drawer close failed");
                    Click(d,p.drawerHandle);Click(d,v.items[0].button);Check(!p.DrawerOpen&&h.SelectedEgg==0&&state.commonEggs==4&&h.incubate.interactable,"Selecting egg does not stage/close safely");
                    Click(d,h.egg.GetComponent<Button>());Check(!h.egg.gameObject.activeSelf&&state.commonEggs==4&&saves==0,"Staged removal consumes or saves");
                    h.SelectEgg(1);Click(d,p.pages[1]);Check(h.SelectedSpace==1&&h.SelectedEgg<0,"Slot staging leaked");Click(d,p.pages[0]);Check(h.SelectedEgg==1,"Slot staging lost");
                    Click(d,h.incubate);Check(state.Occupied(0)&&state.rareEggs==3&&saves==1&&h.particles.IsRunning&&p.lamps.activeSelf,"Start did not debit/save/heat once");
                    Check(h.actionLabel.text=="Cancelar"&&p.primaryFace.material==p.redPaint&&!p.incubateGlow.activeSelf&&h.accelerate.gameObject.activeSelf&&!h.cancel.gameObject.activeSelf,"Running action is not red non-glowing cancel");
                    Check(h.accelerateQuantity.text=="x3"&&h.accelerateQuantity.GetComponentInParent<RoyalPostmanClockFace>()!=null,"Live stock is not on the single clock");
                    Check(!h.accelerate.transform.Find("ResourceBadge").gameObject.activeSelf&&!h.accelerate.transform.Find("PaintedCenter/FastForwardIcon").gameObject.activeSelf,"Obsolete accelerator symbols visible");
                    Check(h.accelerate.transform.Find("PaintedCenter/rounded_total_close_center").GetComponent<Image>().material.name=="RoundButtonBlueV104","Authored blue round-button material lost");
                    h.particles.Sample(4.2);DigitalPixels(d,h.display);d.Export(Output+"/running.png");Check(!h.RemoveSelectedEgg(),"Running egg removed by stage click");Click(d,h.incubate);h.cancel.onClick.Invoke();Check(state.rareEggs==4&&saves==2&&!h.particles.IsRunning&&!p.lamps.activeSelf&&!h.accelerate.gameObject.activeSelf,"Cancel refund/visibility not exactly once");
                    // Item acceleration, then the existing gold curve, then a rejected charge.
                    h.SelectEgg(3);Click(d,h.incubate);long finish=state.finishes[0];int before=saves;Click(d,h.accelerate);Check(state.finishes[0]<finish&&state.accelerationItems.Sum()==2&&saves==before+1&&h.accelerateQuantity.text=="x2","Clock does not consume one boost/update live text");
                    state.accelerationItems=new[]{0,0,0,0};h.Refresh();int money=gold,purchases=state.goldPurchases[0];before=saves;Click(d,h.accelerate);Check(gold<money&&state.goldPurchases[0]==purchases+1&&saves==before+1,"Gold acceleration changed existing economy");
                    rejectGold=true;money=gold;finish=state.finishes[0];before=saves;Click(d,h.accelerate);Check(gold==money&&state.finishes[0]==finish&&saves==before,"Rejected payment mutated incubation");rejectGold=false;Click(d,h.incubate);Check(state.legendaryEggs==4,"Cancel did not return original egg");
                    // All four tiers complete through the full hatch animation and durable receipt.
                    for(int kind=0;kind<4;kind++)
                    {
                        before=saves;h.SelectEgg(kind);Click(d,h.incubate);Check(h.display.Value==RoyalPostmanPetsScreen.FormatIncubationTime(v.incubationRules.EggSeconds(kind)),"Wrong tier duration");now=state.finishes[0];h.Refresh();
                        Check(h.Reveal!=null&&h.Reveal.IsOpen&&state.pendingHatches.Count==1&&saves==before+2,"Completion failed to create one durable receipt");var receipt=state.pendingHatches[0].receiptId;h.Refresh();Check(saves==before+2&&state.hatchedPets[kind].copies==1,"Repeated refresh grants twice");
                        foreach(float t in new[]{.6f,1.6f,2.3f,3.5f}){h.Reveal.Sample(t);d.Render();}
                        var reveal=v.transform.Find("HatchReveal");var pet=reveal.Find("Pet").GetComponent<Image>();Check(pet.color==Color.white&&pet.rectTransform.rect.width<=580,"Pet is dark or oversized");Check(reveal.Find("Title").GetComponent<RoyalPostmanRewardText>()!=null,"Yellow/blue reward lettering missing");
                        var stars=reveal.Find("RotatingRays/PulseSparkles").GetComponent<RoyalPostmanRadialSparkles>();Check(stars.sprite!=null&&stars.ambient&&stars.color.r>.95f,"Separate gold radial particles missing");
                        if(kind==0){d.Export(Output+"/reveal.png");v.Close();Check(!h.Reveal.IsOpen&&state.pendingHatches[0].receiptId==receipt,"Closing discarded reward receipt");v.Open();Check(h.Reveal.IsOpen&&state.hatchedPets[0].copies==1,"Reopening grants twice");h.Reveal.Sample(4);}
                        h.Reveal.Continue();h.Reveal.Continue();Check(state.pendingHatches.Count==0&&saves==before+3,"Continue acknowledged twice");
                    }
                    h.SelectEgg(0);Click(d,h.incubate);now=state.finishes[0];failSave=true;bool caught=false;try{h.Refresh();}catch(IOException){caught=true;}failSave=false;Check(caught&&state.Occupied(0)&&state.pendingHatches.Count==0,"Save failure lost incubation");h.Refresh();h.Reveal.Sample(4);h.Reveal.Continue();
                    // Production has no mascots yet: selection still previews, start must remain disabled.
                    h.ConfigureSimulation(null);h.SelectEgg(0);before=state.commonEggs;h.incubate.onClick.Invoke();Check(!h.incubate.interactable&&state.commonEggs==before&&!state.Occupied(0),"Empty real catalogue consumed stock");h.ConfigureSimulation(catalog);h.Refresh();
                    h.RefreshAvailability(20);Check(h.lockIcon.gameObject.activeSelf&&!h.incubate.interactable,"Level gate lost");h.RefreshAvailability(100);
                    foreach(string language in new[]{"English","Portuguese"})
                    {
                        SweetSugar.Scripts.Level.CrosssceneData.selectedLanguage=language;h.headerTitle.editorLanguage=RoyalPostmanHeaderTitle.EditorLanguage.GameLanguage;h.Refresh();Check(p.handleLabel.text==(language=="English"?"Eggs":"Ovos"),"Drawer localization failed");Check(h.actionLabel.text==(language=="English"?"Incubate":"Incubar"),"Action localization failed");
                    }
                    foreach(var size in new[]{new Vector2Int(360,640),new Vector2Int(390,844),new Vector2Int(720,1280)})
                    {
                        d.Resize(size.x,size.y);Inside(d,chamber);Inside(d,(RectTransform)p.drawerHandle.transform);Inside(d,(RectTransform)h.incubate.transform);Check(!d.Bounds(chamber).Overlaps(d.Bounds((RectTransform)h.incubate.transform)),"Chamber overlaps primary command");
                        Click(d,p.drawerHandle);Inside(d,p.drawerSheet);for(int i=0;i<4;i++){Inside(d,(RectTransform)v.items[i].button.transform);Check(d.Bounds(h.itemNames[i].rectTransform).height>=18,"Egg label is unreadably small");}Click(d,p.drawerClose);
                    }
                    bool readOnly=false;try{d.Save();}catch(InvalidOperationException){readOnly=true;}Check(readOnly,"Sandbox can overwrite production prefab");UnityEngine.Object.DestroyImmediate(catalog);
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(kit);}
            File.WriteAllText(Output+"/checks.txt","PASS: native compile/render and real UI raycasts; exact square chamber; hidden tabs; drawer open/close/selection and no closed thumbnails; readable 2x2 stock; staged removal and per-slot selection; contextual red non-glowing Cancel; running lamps/heat only; single clock with live boost stock; authored blue AllIn1 material; item/gold/rejected-payment economy; all4 tier timers; crack/burst/full-colour smaller pet, yellow/blue text and gold pulse particles; grant/ack exactly once; close/reopen durable receipts; save-failure rollback; empty real catalogue safe; level gates; PT/EN; 360/390/720 bounds; read-only simulation.\n");EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Output+"/checks.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
