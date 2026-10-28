using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using RoyalAves.EditorTools;
using RoyalAves.UI.RoyalPostman2026;

public static class PetsMockChecks
{
    const string Output="../pets-mock-v2";
    static bool refineCurrentProfile;
    static bool verifyOnly;
    public static void VerifyCurrentProfile(){refineCurrentProfile=true;verifyOnly=true;Run();}
    public static void RunCurrentProfile(){refineCurrentProfile=true;Run();}
    static bool refineTabStates;
    public static void RunTabStates(){refineTabStates=true;Run();}
    static bool refineItemTooltip;
    public static void RunItemTooltip(){refineItemTooltip=true;Run();}
    static bool refineIncubationActions;
    public static void RunIncubationActions(){refineIncubationActions=true;Run();}
    static bool refineHeaderSpacing;
    public static void RunHeaderSpacing(){refineHeaderSpacing=true;Run();}
    static bool refineDividerOnly;
    static bool refineInventoryFeedback;
    static bool refineInventoryPolish;
    public static void RunInventoryPolish(){refineInventoryPolish=true;Run();}
    public static void RunDividerRefinement(){refineDividerOnly=true;Run();}
    public static void RunInventoryFeedback(){refineInventoryFeedback=true;Run();}
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    static Color32[] Pixels(RoyalPostmanScreenDocument d){d.Render();var prior=RenderTexture.active;RenderTexture.active=d.Texture;var t=new Texture2D(d.Width,d.Height,TextureFormat.RGBA32,false);try{t.ReadPixels(new Rect(0,0,d.Width,d.Height),0,0);t.Apply();return t.GetPixels32();}finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(t);}}
    static void Hits(RoyalPostmanScreenDocument d,Button button,EventSystem events){var hits=new System.Collections.Generic.List<RaycastResult>();d.Root.GetComponentInParent<GraphicRaycaster>().Raycast(new PointerEventData(events){position=d.Camera.WorldToScreenPoint(button.transform.position)},hits);Check(hits.Count>0&&(hits[0].gameObject.transform==button.transform||hits[0].gameObject.transform.IsChildOf(button.transform)),"Button unreachable: "+button.name);}
    public static void Run(){
        Directory.CreateDirectory(Output);File.WriteAllText(Output+"/validation.txt","RUNNING");int errors=0;Application.LogCallback handler=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=handler;
        try{
            ShaderUtil.allowAsyncCompilation=false;
            string home="Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Home.prefab",nav="Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Navigation.prefab";
            var homeBefore=File.ReadAllBytes(home);var navBefore=File.ReadAllBytes(nav);
            var eggBefore=File.ReadAllBytes(RoyalPostmanArtPaths.Texture("pets-egg-v1"));var apparatusBefore=File.ReadAllBytes(RoyalPostmanArtPaths.Texture("pets-incubator-empty-v1"));
            if(verifyOnly){}else if(refineCurrentProfile)RoyalPostmanCurrentPetBuilder.Build();else if(refineTabStates)RoyalPostmanPetsMockBuilder.RefineTabStates();else if(refineItemTooltip)RoyalPostmanPetsMockBuilder.RefineItemTooltip();else if(refineIncubationActions)RoyalPostmanPetsMockBuilder.RefineIncubationActions();else if(refineHeaderSpacing)RoyalPostmanPetsMockBuilder.RefineHeaderSpacing();else if(refineInventoryPolish)RoyalPostmanPetsMockBuilder.RefineInventoryPolish();else if(refineInventoryFeedback)RoyalPostmanPetsMockBuilder.RefineInventoryFeedback();else if(refineDividerOnly)RoyalPostmanPetsMockBuilder.RefineDivider();else RoyalPostmanPetsMockBuilder.Build();var kit=RoyalPostmanRuntimeKit.Load();
            Check(!kit.ShowsNavigation("pets")&&kit.ShowsNavigation("home"),"Incorrect screen chrome");
            foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(360,800),new Vector2Int(1080,1920)})using(var d=new RoyalPostmanScreenDocument(kit,"pets",size.x,size.y)){
                var view=d.Root.GetComponent<RoyalPostmanPetsScreen>();int back=0;view.Initialize(()=>back++,i=>{});if(refineCurrentProfile)view.ShowCurrent(false);
                if(refineDividerOnly||refineInventoryPolish){
                    var divider=d.Root.transform.Find("Divider");
                    var tip=divider.Find("LeftTip").GetComponent<Image>();var right=divider.Find("RightTip").GetComponent<Image>();
                    var line=divider.Find("LeftLine").GetComponent<Image>();
                    Check(tip.sprite.name=="pets-divider-tip-v9"&&right.sprite==tip.sprite&&line.sprite.texture==tip.sprite.texture,"Divider must share one native gold source");
                    Check(right.transform.localScale.x==-1&&Mathf.Abs(line.rectTransform.sizeDelta.y-7.8f)<.001f,"Divider proportions changed");
                    Check(Mathf.Abs(divider.Find("Ornament").GetComponent<RectTransform>().sizeDelta.x-172.5f)<.001f,"Center ornament was not reduced");
                    Check(divider.Find("Ornament").GetComponent<Image>().sprite.name=="pets-ornament-center-v3","Approved central ornament changed");
                }
                Check(d.Root.transform.Find("Navigation")==null,"Home footer still present");
                Check(view.incubators.All(s=>!s.egg.gameObject.activeSelf),"Inactive egg visible");
                Check(view.inventory==null&&view.GetComponentsInChildren<ScrollRect>(true).Length==0,"Inventory must use complete pages without continuous scrolling");
                Check(view.inventoryRows.Length==2&&view.inventoryRows[0].id=="EggsAndGems"&&view.inventoryRows[1].id=="Acceleration","Inventory categories changed");
                Check(view.backButton.GetComponent<Image>().sprite.name=="pets-back-v4"&&view.helpButton.GetComponent<Image>().sprite.name=="pets-help-v4","Painted header controls missing");
                foreach(var slot in view.incubators){
                    var card=(RectTransform)slot.available.transform.parent;
                    var apparatus=card.parent.Find("Apparatus").GetComponent<RectTransform>();
                    Check(card.GetSiblingIndex()>apparatus.GetSiblingIndex(),"Cards must be foreground overlays");
                    Check(card.parent.Find("TableContact")!=null,"Table contact missing");
                }
                d.Export(Output+"/pets-empty-"+size.x+".png");
                view.SetIncubation(0,true,AssetDatabase.LoadAssetAtPath<Sprite>(RoyalPostmanArtPaths.Sprite("pets-egg-v1")),45,100,"2h 15m");
                d.Export(Output+"/pets-active-"+size.x+".png");
                Check(view.incubators.Count(s=>s.egg.gameObject.activeSelf)==1,"Active egg visibility incorrect");
                var events=new GameObject("TestEvents",typeof(EventSystem));try{Hits(d,view.backButton,events.GetComponent<EventSystem>());Hits(d,view.currentTab,events.GetComponent<EventSystem>());Hits(d,view.incubatorTab,events.GetComponent<EventSystem>());Hits(d,view.items[0].button,events.GetComponent<EventSystem>());Hits(d,view.incubators[0].useItem,events.GetComponent<EventSystem>());}finally{UnityEngine.Object.DestroyImmediate(events);}
                view.backButton.onClick.Invoke();Check(back==1,"Back callback lost");view.helpButton.onClick.Invoke();Check(view.helpPanel.activeSelf,"Help did not open");view.dismissHelp.onClick.Invoke();
                view.incubators[0].useItem.onClick.Invoke();Check(!view.helpPanel.activeSelf,"Empty stock should disable incubation without opening help");
                view.currentTab.onClick.Invoke();Check(view.currentPanel.activeSelf&&!view.workshop.activeSelf,"Current tab failed");view.incubatorTab.onClick.Invoke();
                view.SelectItem(1);Check(view.items[1].selection.activeSelf,"Item selection failed");
                var hover=view.items[0].button.GetComponent<RoyalPostmanInventoryHover>();
                var hoverEvents=new GameObject("HoverEvents",typeof(EventSystem));
                try{
                    var pointer=new PointerEventData(hoverEvents.GetComponent<EventSystem>());
                    ExecuteEvents.Execute(view.items[0].button.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
                    Check(hover.IsHovered&&hover.outline.enabled&&view.items[1].selection.activeSelf&&!view.items[0].selection.activeSelf,"Item hover changes selection or fails to highlight");
                    d.Render();if(size.x==720)d.Export(Output+"/pets-hover-720.png");
                    ExecuteEvents.Execute(view.items[0].button.gameObject,pointer,ExecuteEvents.pointerExitHandler);
                    Check(!hover.IsHovered&&!hover.outline.enabled,"Hover remained after pointer exit");
                    Check(d.Pick(d.Bounds(view.items[0].button.GetComponent<RectTransform>()).center).GetComponentInParent<RoyalPostmanInventoryHover>()==hover,"Editor hover picks a different item");
                }finally{UnityEngine.Object.DestroyImmediate(hoverEvents);}
                for(int rowIndex=0;rowIndex<2;rowIndex++){
                    var row=view.inventoryRows[rowIndex];
                    if(refineInventoryPolish){Check(row.activeDotColor==new Color(1,.78f,.04f)&&row.inactiveDotColor==new Color(.5f,.39f,.02f,.38f),"Dots must follow enabled/disabled gold arrows");Check(Mathf.Abs(((RectTransform)row.pages[0].transform.parent).anchoredPosition.y-(rowIndex==0?-260:-550))<.001f,"Inventory row did not move down");}
                    Check(row.pagination==null&&row.dots.Length==row.pages.Length,"Numeric counter remains or page dots missing");
                    Check(!row.previous.interactable&&row.next.interactable,"Initial pagination limits incorrect");
                    var events2=new GameObject("PageEvents",typeof(EventSystem));try{Hits(d,row.next,events2.GetComponent<EventSystem>());}finally{UnityEngine.Object.DestroyImmediate(events2);}
                    row.next.onClick.Invoke();Check(row.currentPage==1&&view.inventoryRows[1-rowIndex].currentPage==0,"Category page controls are coupled");
                    row.previous.onClick.Invoke();Check(row.currentPage==0,"Previous arrow failed");
                    row.dots[row.dots.Length-1].onClick.Invoke();Check(row.currentPage==row.pages.Length-1&&view.inventoryRows[1-rowIndex].currentPage==0,"Dot navigation is coupled or unbound");
                    for(int dot=0;dot<row.dots.Length;dot++)Check(row.dots[dot].targetGraphic.color==(dot==row.currentPage?row.activeDotColor:row.inactiveDotColor),"Page dot state does not match current page");
                    row.dots[0].onClick.Invoke();
                    for(int pageIndex=0;pageIndex<row.pages.Length;pageIndex++){
                        view.SetInventoryPage(rowIndex,pageIndex);d.Render();
                        Check(row.pages.Count(p=>p.activeSelf)==1,"More than one page visible");
                        foreach(var slot in row.pages[pageIndex].transform.Cast<Transform>()){
                            var rect=d.Bounds((RectTransform)slot);
                            Check(rect.xMin>=0&&rect.xMax<=size.x&&rect.yMin>=0&&rect.yMax<=size.y,"Inventory slot clipped by screen");
                            Check(rect.width>0&&rect.height>0,"Invalid inventory slot dimensions");
                        }
                        if(pageIndex==1&&size.x==720)d.Export(Output+"/inventory-row-"+rowIndex+"-page-2.png");
                    }
                    Check(!row.next.interactable,"Next arrow remains active at last page");
                    view.SetInventoryPage(rowIndex,999);Check(row.currentPage==row.pages.Length-1,"Last page clamp failed");
                    view.SetInventoryPage(rowIndex,-1);Check(row.currentPage==0&&!row.previous.interactable,"First page clamp failed");
                }
            }
            using(var detail=new RoyalPostmanScreenDocument(kit,"pets",2160,3840)){
                var view=detail.Root.GetComponent<RoyalPostmanPetsScreen>();view.Initialize(()=>{},i=>{});if(refineCurrentProfile)view.ShowCurrent(false);
                view.SetIncubation(0,true,AssetDatabase.LoadAssetAtPath<Sprite>(RoyalPostmanArtPaths.Sprite("pets-egg-v1")),45,100,"2h 15m");
                detail.Export(Output+"/pets-active-detail-2160.png");
            }
            using(var d=new RoyalPostmanScreenDocument(kit,"pets",720,1280)){
                var title=d.Root.transform.Find("Header/Title").GetComponent<RectTransform>();var original=title.anchoredPosition;title.anchoredPosition+=Vector2.right*9;
                var hover=d.Root.GetComponent<RoyalPostmanPetsScreen>().items[0].button.GetComponent<RoyalPostmanInventoryHover>();hover.PreviewHover(true);d.Save();Check(!hover.IsHovered&&!hover.outline.enabled,"Editor save persisted hover");
                using(var again=new RoyalPostmanScreenDocument(kit,"pets",720,1280))Check(again.Root.transform.Find("Header/Title").GetComponent<RectTransform>().anchoredPosition==original+Vector2.right*9,"Screen edits did not persist");
                title.anchoredPosition=original;d.Save();
            }
            using(var d=new RoyalPostmanScreenDocument(kit,"pets",720,1280)){
                var expected=Pixels(d);d.Root.SetActive(false);var live=RoyalPostmanPetsScreen.Create((RectTransform)d.Root.transform.parent,()=>{},i=>{});live.Open();var actual=Pixels(d);
                int changed=expected.Where((p,i)=>!p.Equals(actual[i])).Count();Check(changed==0,"Editor/runtime differ: "+changed);UnityEngine.Object.DestroyImmediate(live.gameObject);
            }
            Check(homeBefore.SequenceEqual(File.ReadAllBytes(home)),"Home changed");Check(navBefore.SequenceEqual(File.ReadAllBytes(nav)),"Home footer changed");
            using(var stale=new RoyalPostmanScreenDocument(kit,"pets",720,1280)){
                string path=AssetDatabase.GetAssetPath(stale.Source);var bytes=File.ReadAllBytes(path);bool refused=false;
                try{File.AppendAllText(path,"\n");try{stale.Save();}catch(InvalidOperationException){refused=true;}Check(refused,"Stale editor copy overwrote an external update");Check(File.ReadAllBytes(path).Length==bytes.Length+1,"Stale editor touched the updated file");}
                finally{File.WriteAllBytes(path,bytes);}
            }
            Check(eggBefore.SequenceEqual(File.ReadAllBytes(RoyalPostmanArtPaths.Texture("pets-egg-v1")))&&apparatusBefore.SequenceEqual(File.ReadAllBytes(RoyalPostmanArtPaths.Texture("pets-incubator-empty-v1"))),"Approved sprites changed");
            if(refineIncubationActions||refineItemTooltip||refineTabStates||refineCurrentProfile)PetsIncubationChecks.Run(kit,Output);
            if(refineItemTooltip||refineTabStates||refineCurrentProfile)PetsTooltipChecks.Run(kit,Output);
            if(refineTabStates||refineCurrentProfile)PetsTabStateChecks.Run(kit,Output);
            if(refineCurrentProfile)PetsCurrentProfileChecks.Run(kit,Output);
            Check(errors==0,"Unity errors: "+errors);
            File.WriteAllText(Output+"/validation.txt","PASS: "+(refineCurrentProfile?"V16 painted sprites, rounded progress with reserved value region, rarity shields, subtabs with separate icons/locks, complete top-aligned collection; default Current pet profile, separate provisional pets, per-pet skills/stats/portraits, selector/arrows/cards/sort/info/equip preview, locked secondary tabs, shared header/tabs/arrows/divider preserved. ":refineTabStates?"V14 four painted tab states; gold follows current selection, inactive tab is blue; runtime/editor click routing, correct content, persisted default incubation tab. ":refineItemTooltip?"V13 fitted Incubadora caption, click item tooltip with title/description/quantity, icon, clamped placement, outside/close dismissal, cross-item click-through, editor click preview, transient-state cleanup. ":refineIncubationActions?"V12 single contextual action; real egg counts, consumption, locks, 1h/3h/12h timers, 30/60/90/120m items, progressive 30m gold prices capped at 8000, failed-payment guards, JSON persistence. ":refineHeaderSpacing?"V11 taller header with clearance below circular buttons. ":refineInventoryPolish?"V10 smaller divider, lower inventory rows, gold page dots matching arrow states. ":refineDividerOnly?"V9 one native gold source for both mirrored finials and tiled bar; approved V3 center retained. ":"V8 ")+"Clickable page dots below both rows, independent dot/arrow navigation, pointer enter/exit hover without changing selection, editor hover picking and transient-state reset on save; complete six-slot pages; authored screen persistence; actual editor/runtime equality 0 differing pixels; captures 360x800, 720x1280, 1080x1920, 2160x3840 detail; Home, Navigation and approved egg/incubator PNGs unchanged; zero Unity errors. Reference stock quantities remain illustrative.\n");EditorApplication.Exit(0);
        }catch(Exception e){File.WriteAllText(Output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);}finally{Application.logMessageReceived-=handler;}
    }
}
