using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RoyalAves.Meta;
using RoyalAves.EditorTools;
using RoyalAves.UI.RoyalPostman2026;

public static class PetsLayeredV63Review
{
    const string Output="../pets-layered-v63";
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Inside(RoyalPostmanScreenDocument d,RectTransform rect,Rect safe)
    {
        var b=d.Bounds(rect);Check(b.xMin>=safe.xMin-1&&b.yMin>=safe.yMin-1&&b.xMax<=safe.xMax+1&&b.yMax<=safe.yMax+1,"Outside safe area: "+rect.name+" "+b+" vs "+safe);
    }
    static void Hit(RoyalPostmanScreenDocument d,Button button,EventSystem events)
    {
        var hits=new List<RaycastResult>();d.Root.GetComponentInParent<GraphicRaycaster>().Raycast(new PointerEventData(events){position=d.Camera.WorldToScreenPoint(button.transform.position)},hits);
        Check(hits.Count>0&&(hits[0].gameObject.transform==button.transform||hits[0].gameObject.transform.IsChildOf(button.transform)),"Tap blocked: "+button.name+" first="+(hits.Count>0?hits[0].gameObject.name:"none"));
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output);File.WriteAllText(Output+"/review.txt","RUNNING");
        try
        {
            ShaderUtil.allowAsyncCompilation=false;
            var kit=RoyalPostmanRuntimeKit.Load();Check(kit!=null,"Kit missing");
            Check(RoyalPostmanPetsScreen.FormatIncubationTime(172800)=="48:00:00","48h wrapped at24h");
            Check(RoyalPostmanPetsScreen.FormatIncubationTime(-1)=="00:00:00","Negative timer");
            foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(390,844),new Vector2Int(360,640)})
            foreach(bool notch in new[]{false,true})
            using(var d=new RoyalPostmanScreenDocument(kit,"pets",size.x,size.y))
            {
                var view=d.Root.GetComponent<RoyalPostmanPetsScreen>();Check(view.layeredIncubatorLayout,"New layout flag missing");
                Check(d.Root.transform.Find("Room").GetSiblingIndex()>d.Root.transform.Find("Background").GetSiblingIndex(),"New scenery hidden by old background");
                var safe=notch?new Rect(0,size.x*34f/390,size.x,size.y-size.x*81f/390):new Rect(0,0,size.x,size.y);
                d.Root.GetComponent<RoyalPostmanViewport>().SafeAreaOverride=safe;
                int backs=0,saves=0,incubations=0,accelerations=0;long now=1700000000;int gold=10000;
                view.Initialize(()=>backs++,i=>{});
                view.UseItemRequested+=(i,id)=>incubations++;view.AccelerateRequested+=i=>accelerations++;
                var stock=new PetsIncubationState{commonEggs=3,rareEggs=1,epicEggs=1,legendaryEggs=1,accelerationItems=new[]{1,0,0,0}};
                view.BindInventory(stock,()=>saves++,cost=>{if(gold<cost)return false;gold-=cost;return true;},()=>gold,()=>now);
                view.ShowCurrent(false);d.Render();
                Check(view.incubators.Select(s=>s.unlockLevel).SequenceEqual(new[]{0,30,100,0}),"Unlock levels changed");
                Check(view.incubationRules.EggSeconds(0)==3600&&view.incubationRules.EggSeconds(1)==10800&&view.incubationRules.EggSeconds(2)==43200&&view.incubationRules.EggSeconds(3)==172800,"Durations changed");
                Check(!view.incubators[0].accelerate.gameObject.activeSelf&&!view.incubators[0].particles.IsRunning,"Idle speedup/effects visible");
                Check(view.incubators[0].progress.text=="Duração · 01:00:00","Idle time label");
                string suffix=size.x+"x"+size.y+(notch?"-notch":"");
                d.Export(Output+"/idle-"+suffix+".png");
                var eventGO=new GameObject("V63Events",typeof(EventSystem));try
                {
                    var events=eventGO.GetComponent<EventSystem>();
                    foreach(var b in new[]{view.backButton,view.helpButton,view.currentTab,view.incubatorTab,view.previousIncubator,view.nextIncubator,view.incubators[0].useItem}){Inside(d,b.GetComponent<RectTransform>(),safe);Hit(d,b,events);}
                    foreach(var item in view.items){Inside(d,item.button.GetComponent<RectTransform>(),safe);Hit(d,item.button,events);}
                    view.currentTab.onClick.Invoke();Check(view.currentPanel.activeSelf&&!view.workshop.activeSelf,"Mascote navigation broken");
                    if(size.x==720&&!notch)d.Export(Output+"/current-preserved.png");
                    view.incubatorTab.onClick.Invoke();Check(!view.currentPanel.activeSelf&&view.workshop.activeSelf,"Incubator navigation broken");
                    view.backButton.onClick.Invoke();view.helpButton.onClick.Invoke();Check(backs==2,"Back/close binding broken");
                    var slot=view.incubators[0];slot.useItem.onClick.Invoke();
                    Check(stock.commonEggs==2&&stock.Occupied(0)&&saves==1&&incubations==1,"Paw didn't start exactly once");
                    slot.useItem.onClick.Invoke();Check(stock.commonEggs==2&&saves==1,"Occupied paw consumed another egg");
                    Check(slot.egg.gameObject.activeSelf&&slot.particles.IsRunning&&slot.accelerate.gameObject.activeSelf,"Active layers/state missing");
                    now+=60;view.BindInventory(stock,()=>saves++,c=>{gold-=c;return true;},()=>gold,()=>now);d.Render();
                    Check(slot.progress.text=="Tempo restante · 00:59:00"&&Mathf.Abs(slot.progressFill.fillAmount-1f/60)<.001,"Countdown/fill not temporal");
                    Hit(d,slot.accelerate,events);Inside(d,slot.accelerate.GetComponent<RectTransform>(),safe);
                    var egg=slot.egg.transform;var glass=egg.parent.Find("GlassFront");var front=egg.parent.Find("BaseFront");
                    Check(egg.GetSiblingIndex()<glass.GetSiblingIndex()&&egg.GetSiblingIndex()<front.GetSiblingIndex(),"Egg not inside glass/front lip");
                    Check(slot.useItem.transform.parent!=egg.parent,"Paw baked into machine hierarchy");
                    Check(slot.particles.rearLayer.GetComponent<Mask>()!=null&&slot.particles.frontLayer.GetComponent<Mask>()!=null,"Particle masks missing");
                    slot.particles.Sample(10);var pos=slot.particles.motes[0].rectTransform.anchoredPosition;slot.particles.Sample(11);Check(slot.particles.motes[0].rectTransform.anchoredPosition!=pos,"Particles static");
                    d.Export(Output+"/active-"+suffix+".png");
                    slot.accelerate.onClick.Invoke();Check(stock.accelerationItems[0]==0&&stock.Remaining(0,now)==1740&&saves==2&&accelerations==1,"Acceleration stock/rule changed");
                    int oldGold=gold;slot.accelerate.onClick.Invoke();Check(gold==oldGold-500&&stock.Remaining(0,now)==0&&saves==3,"Gold acceleration changed");
                    Check(slot.progress.text=="Pronto"&&!slot.accelerate.gameObject.activeSelf&&!slot.particles.IsRunning,"Finished timer/effects state broken");
                    for(int i=1;i<4;i++)
                    {
                        var s=view.incubators[i];s.unlockLevel=0;view.SelectIncubator(i);view.BindInventory(stock,()=>saves++,c=>false,()=>0,()=>now);d.Render();
                        Check(s.useItem.interactable,"Rarity didn't become ready");s.useItem.onClick.Invoke();
                        Check(stock.Occupied(i)&&s.egg.gameObject.activeSelf&&s.particles.IsRunning,"Rarity incubation layers missing");
                        Check(s.progress.text=="Tempo restante · "+RoyalPostmanPetsScreen.FormatIncubationTime(view.incubationRules.EggSeconds(i)),"Rarity time mismatch");
                        Inside(d,s.useItem.GetComponent<RectTransform>(),safe);Hit(d,s.useItem,events);
                        if(size.x==720&&!notch)d.Export(Output+"/"+new[]{"common","rare","epic","legendary"}[i]+".png");
                    }
                    view.SelectIncubator(0);view.BindInventory(new PetsIncubationState(),()=>{},c=>false,()=>0,()=>now);
                    Check(!view.incubators[0].useItem.interactable&&!view.incubators[0].accelerate.gameObject.activeSelf,"Empty-stock guard missing");
                }finally{UnityEngine.Object.DestroyImmediate(eventGO);}
            }
            // Exercise actual layout editor roundtrip on an isolated prefab only.
            using(var d=new RoyalPostmanScreenDocument(kit,"pets",720,1280))
            {
                var view=d.Root.GetComponent<RoyalPostmanPetsScreen>();view.ShowCurrent(false);var rect=view.incubators[0].useItem.GetComponent<RectTransform>();float width=rect.rect.width;
                var time=view.incubators[0].progress.rectTransform;var track=time.parent as RectTransform;Check(track.name=="TimeBar_PLACEHOLDER","Timer doesn't move with its bar");track.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,700);d.Render();Check(Mathf.Abs(time.rect.width-680)<.1,"Timer doesn't resize with its bar");
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width+12);d.Save();
            }
            using(var d=new RoyalPostmanScreenDocument(kit,"pets",720,1280)){Check(Mathf.Abs(d.Root.GetComponent<RoyalPostmanPetsScreen>().incubators[0].useItem.GetComponent<RectTransform>().rect.width-206)<.1,"Placeholder resize/save failed");}
            File.WriteAllText(Output+"/review.txt","PASS: four layered machines; true transparent glass; egg/front lip ordering; animated masked particles; editable independent paw/button/time placeholders; all configured durations and unlocks; back/close/current navigation; stock and gold acceleration; 48h formatting; idle/running/finished states; raycasts and safe areas at720x1280,390x844,360x640 with/without notch; editor resize/save roundtrip.\n");
            EditorApplication.Exit(0);
        }catch(Exception e){File.WriteAllText(Output+"/review.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}

