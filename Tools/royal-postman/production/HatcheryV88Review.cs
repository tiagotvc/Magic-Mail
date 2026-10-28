using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.UI.RoyalPostman2026;

public static class HatcheryV88Review
{
    const string Output=HatcheryV88Build.Output;
    static void Check(bool ok,string message)=>HatcheryV88Build.Check(ok,message);
    static void CancelChecks()
    {
        var rules=new PetsIncubationRules();long now=1700000000;
        for(int kind=0;kind<4;kind++)
        {
            var s=new PetsIncubationState{commonEggs=2,rareEggs=2,epicEggs=2,legendaryEggs=2,accelerationItems=new[]{2,0,0,0}};
            Check(s.IncubateSlot(0,kind,100,30,rules,now),"Cannot start selected egg");
            s.started[3]=now;s.finishes[3]=now+500;s.goldPurchases[3]=4;
            Check(s.Accelerate(0,100,30,rules,now,-1,n=>false),"Cannot boost before cancelling");
            Check(s.CancelIncubation(0,now),"Cannot cancel running job");
            Check(s.EggCount(kind)==2&&!s.Occupied(0)&&s.started[0]==0&&s.goldPurchases[0]==0,"Cancel did not restore exactly selected egg/reset timer");
            Check(s.accelerationItems[0]==1&&s.finishes[3]==now+500&&s.goldPurchases[3]==4,"Cancel refunded boost or changed another saved job");
            string json=JsonUtility.ToJson(s);Check(!s.CancelIncubation(0,now)&&!s.CancelIncubation(-1,now)&&!s.CancelIncubation(4,now)&&JsonUtility.ToJson(s)==json,"Repeated/invalid cancel changed stock");
            Check(s.IncubateSlot(0,kind,100,30,rules,now),"Cancel did not release space");
            Check(!s.CancelIncubation(0,s.finishes[0]),"Completed job can be cancelled/refunded");
            var copy=JsonUtility.FromJson<PetsIncubationState>(JsonUtility.ToJson(s));copy.Normalize();Check(copy.finishes.SequenceEqual(s.finishes),"Persistence changed another timer");
        }
        var full=new PetsIncubationState{commonEggs=int.MaxValue};full.finishes[0]=now+5;
        Check(!full.CancelIncubation(0,now)&&full.Occupied(0),"Overflow cancellation loses egg");
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        try
        {
            CancelChecks();
            using(var d=new RoyalPostmanScreenDocument(RoyalPostmanRuntimeKit.Load(),"pets",720,1280))
            {
                var v=d.Root.GetComponent<RoyalPostmanPetsScreen>();var h=v.hatchery;Check(h.cancel!=null,"Cancel button missing");
                int saves=0,starts=0,boosts=0,spent=0;long now=1700000000;
                v.Initialize(()=>{},i=>{});v.UseItemRequested+=(i,id)=>starts++;v.AccelerateRequested+=i=>boosts++;
                var state=new PetsIncubationState{commonEggs=2,legendaryEggs=2,accelerationItems=new[]{1,0,0,0}};
                v.BindInventory(state,()=>saves++,cost=>{spent+=cost;return true;},()=>10000,()=>now);v.RefreshAvailability(100);v.ShowCurrent(false);
                Check(!h.cancel.interactable&&!h.accelerate.interactable,"Empty slot enables running actions");
                v.items[3].button.onClick.Invoke();h.incubate.onClick.Invoke();h.incubate.onClick.Invoke();
                Check(starts==1&&saves==1&&state.legendaryEggs==1&&h.cancel.interactable&&h.accelerate.interactable,"Start/save/buttons eligibility regressed");
                h.accelerate.onClick.Invoke();Check(boosts==1&&saves==2&&state.accelerationItems[0]==0&&spent==0&&h.display.Value=="47:30:00","New speed button not connected to consumable pipeline");
                h.accelerate.onClick.Invoke();Check(boosts==2&&saves==3&&spent==500&&state.goldPurchases[0]==1,"New speed button changed gold pipeline");
                Check(h.accelerateQuantity.text.Contains("1000"),"New speed button hides next cost");
                h.cancel.onClick.Invoke();h.cancel.onClick.Invoke();
                Check(saves==4&&state.legendaryEggs==2&&!state.Occupied(0)&&!h.particles.IsRunning&&!h.cancel.interactable&&!h.accelerate.interactable&&h.incubate.interactable,"Cancel not wired or duplicate refund/save");
                Check(h.display.Value=="48:00:00"&&state.accelerationItems[0]==0&&spent==500,"Cancel reset duration or refunded spent boosts/gold incorrectly");
                v.RefreshAvailability(29);Check(!h.cancel.interactable&&!h.accelerate.interactable&&!h.incubate.interactable,"Locked job actions enabled");v.RefreshAvailability(100);
                h.incubate.onClick.Invoke();now=state.finishes[0];h.Refresh();Check(!h.cancel.interactable&&!h.accelerate.interactable,"Ready job actions enabled");h.cancel.onClick.Invoke();Check(state.Occupied(0),"Ready job lost by cancel click");
                Check(!h.transform.Find("AcceleratorDisplay").gameObject.activeSelf&&h.accelerate.name=="Accelerate","Old lower accelerator still active");
                Check(h.accelerate.GetComponent<RoyalPostmanPaintedButton>().movingCenter.Find("FastForwardIcon").GetComponent<Text>().text==">>","Wrong speed symbol");
                Check(h.cancel.GetComponent<RoyalPostmanPaintedButton>().movingCenter.Find("Icon").GetComponent<Image>().sprite==v.helpButton.GetComponent<RoyalPostmanPaintedButton>().movingCenter.Find("Icon").GetComponent<Image>().sprite,"Cancel did not reuse user X sprite");
                var events=new GameObject("HatcheryV88Events",typeof(EventSystem));
                try
                {
                    foreach(var button in new[]{h.cancel,h.accelerate})
                    {
                        Check(button.transform.Find("FixedGold").GetComponent<Image>().sprite==h.incubate.transform.Find("FixedGold").GetComponent<Image>().sprite,"User rim replaced");
                        button.interactable=true;d.Render();var data=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=d.Camera.WorldToScreenPoint(button.transform.position)};
                        var hits=new List<RaycastResult>();d.Root.GetComponentInParent<GraphicRaycaster>().Raycast(data,hits);Check(hits.Any(x=>x.gameObject==button.gameObject),"New control misses raycast");
                        var p=button.GetComponent<RoyalPostmanPaintedButton>();var rim=button.transform.Find("FixedGold");var rest=p.movingCenter.anchoredPosition;var fixedPosition=rim.position;
                        p.OnPointerDown(data);Check(p.movingCenter.anchoredPosition!=rest&&rim.position==fixedPosition,"New round does not press independently");p.OnPointerUp(data);Check(p.movingCenter.anchoredPosition==rest,"New round stays pressed");
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(events);}
                foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(390,844),new Vector2Int(360,640)})
                {
                    d.Resize(size.x,size.y);d.Root.GetComponent<RoyalPostmanViewport>().SafeAreaOverride=new Rect(0,24,size.x,size.y-48);d.Render();
                    var left=d.Bounds((RectTransform)h.cancel.transform);var middle=d.Bounds((RectTransform)h.incubate.transform);var right=d.Bounds((RectTransform)h.accelerate.transform);var timer=d.Bounds((RectTransform)h.transform.Find("DurationDisplay"));
                    Check(left.xMax<middle.xMin&&middle.xMax<right.xMin&&Mathf.Abs(left.center.y-middle.center.y)<.2f&&Mathf.Abs(right.center.y-middle.center.y)<.2f,"Action row not aligned/separated");
                    Check(left.width<middle.width&&right.width<middle.width&&left.width>=40,"Side buttons not smaller/tappable");
                    Check(Mathf.Abs(timer.center.x-size.x*.5f)<.2f,"Timer is not centered");
                    Check(d.Bounds(h.accelerateQuantity.rectTransform).yMin>timer.yMax,"Speed quantity overlaps digital display");
                    Check(d.Bounds(h.cancelLabel.rectTransform).xMax<d.Bounds(h.actionLabel.rectTransform).xMin&&d.Bounds(h.actionLabel.rectTransform).xMax<d.Bounds(h.accelerateLabel.rectTransform).xMin,"Action labels overlap");
                }
                h.headerTitle.editorLanguage=RoyalPostmanHeaderTitle.EditorLanguage.English;h.Refresh();Check(h.cancelLabel.text=="Cancel"&&h.accelerateLabel.text=="Speed up","Side actions not localized");
                v.currentTab.onClick.Invoke();Check(v.currentPanel.activeSelf&&!v.workshop.activeSelf,"Preserved profile navigation broken");v.incubatorTab.onClick.Invoke();Check(v.workshop.activeSelf,"Hatchery tab broken");
            }
            File.WriteAllText(Output+"/review.txt","PASS: native compile; smaller separated left Cancel/right >>; user PNG rim/center/X, independent press and raycast; only lower timer active/centered; label/cost spacing at360/390/720; existing boost/gold pipeline; cancellation returns exactly one original egg once, stops particles/timer and persists once; spent boosts/gold and other/legacy jobs preserved; ready/locked/invalid guards; PT/EN and profile navigation.\n");EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Output+"/review.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
