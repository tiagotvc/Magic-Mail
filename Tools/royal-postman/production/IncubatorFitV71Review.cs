using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RoyalAves.EditorTools;
using RoyalAves.UI.RoyalPostman2026;
using RoyalAves.Meta;

public static class IncubatorFitV71Review
{
    const string Output=IncubatorFitV71Build.Output;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        File.WriteAllText(Output+"/review.txt","RUNNING");
        try
        {
            var gold=AssetDatabase.LoadAssetAtPath<Material>(IncubatorFitV71Build.MaterialPath);
            var red=AssetDatabase.LoadAssetAtPath<Material>(IncubatorFitV71Build.RedMaterialPath);
            Check(gold!=null&&gold.GetFloat("_UseCustomPalette")==1,"Matched native gold palette missing");
            Check(red!=null&&red.GetFloat("_UseCustomPalette")==1,"Standard red face material missing");
            using(var d=new RoyalPostmanScreenDocument(RoyalPostmanRuntimeKit.Load(),"pets",720,1280))
            {
                long now=1700000000;int starts=0,saves=0;
                var stock=new PetsIncubationState{commonEggs=2,rareEggs=1,epicEggs=1,legendaryEggs=1};
                var view=d.Root.GetComponent<RoyalPostmanPetsScreen>();view.Initialize(()=>{},i=>{});
                view.UseItemRequested+=(i,id)=>starts++;
                Action bind=()=>{view.BindInventory(stock,()=>saves++,n=>false,()=>0,()=>now);view.RefreshAvailability(100);};
                bind();view.ShowCurrent(false);
                for(int i=0;i<4;i++)
                {
                    view.SelectIncubator(i);d.Render();var slot=view.incubators[i];
                    var body=(RectTransform)slot.root.transform.Find("Machine/BaseFront");
                    var rim=(RectTransform)slot.useItem.transform;
                    Check(rim.parent==body,"Button is not attached to metal body");
                    Check(Vector3.Distance(rim.position,IncubatorFitV71Build.BodyCenterWorld(body))<.1f,"Button is not centered in metal body: "+i);
                    var bodySize=body.sizeDelta;var bodyPosition=body.anchoredPosition;
                    body.anchoredPosition+=new Vector2(12,25);body.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,body.rect.height+45);Canvas.ForceUpdateCanvases();
                    Check(Vector3.Distance(rim.position,IncubatorFitV71Build.BodyCenterWorld(body))<.1f,"Button does not follow moved/resized body");
                    body.sizeDelta=bodySize;body.anchoredPosition=bodyPosition;Canvas.ForceUpdateCanvases();
                    Check(Mathf.Abs(rim.rect.width-rim.rect.height)<.01f&&rim.localScale==Vector3.one,"Button is distorted: "+i);
                    var corners=new Vector3[4];rim.GetWorldCorners(corners);
                    float low=body.rect.yMin+body.rect.height*(IncubatorFitV71Build.BodyCenter-IncubatorFitV71Build.BodyHeight*.5f);
                    float high=body.rect.yMin+body.rect.height*(IncubatorFitV71Build.BodyCenter+IncubatorFitV71Build.BodyHeight*.5f);
                    Check(body.InverseTransformPoint(corners[0]).y>=low-.1f&&body.InverseTransformPoint(corners[1]).y<=high+.1f,"Rim crosses body bands: "+i);
                    Check(slot.useItem.GetComponent<Image>().material==gold&&rim.Find("MovingCenter/RaisedFace").GetComponent<Image>().material==red,"Mismatched rim/face materials");
                    Check(rim.Find("MovingCenter/PawIcon").GetComponent<Image>().color==Color.white,"Paw does not contrast with red center");
                    Check(slot.useItem.GetComponent<Shadow>()!=null,"Contact shadow missing");
                    var bar=(RectTransform)slot.progress.transform.parent;bar.GetWorldCorners(corners);float bottom=corners[0].y;
                    slot.remaining.rectTransform.GetWorldCorners(corners);Check(corners[1].y<bottom,"Status overlaps timer: "+i);
                    d.Export(Output+"/rarity-"+i+".png");
                }
                view.SelectIncubator(0);d.Render();
                var first=view.incubators[0];var press=first.useItem.GetComponent<RoyalPostmanPaintedButton>();
                var rest=press.movingCenter.anchoredPosition;var rootPosition=first.useItem.transform.position;
                var eventsGO=new GameObject("V71Events",typeof(EventSystem));
                try
                {
                    var data=new PointerEventData(eventsGO.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=d.Camera.WorldToScreenPoint(rootPosition)};
                    var hits=new System.Collections.Generic.List<RaycastResult>();d.Root.GetComponentInParent<GraphicRaycaster>().Raycast(data,hits);
                    Check(hits.Exists(h=>h.gameObject==first.useItem.gameObject),"Fitted button lost raycast");
                    press.OnPointerDown(data);
                    Check(press.movingCenter.anchoredPosition==rest+Vector2.down*press.pressTravel&&first.useItem.transform.position==rootPosition,"Separate fixed rim/moving face broken");
                    d.Export(Output+"/pressed.png");press.OnPointerUp(data);
                    Check(press.movingCenter.anchoredPosition==rest,"Press release did not restore face");
                    first.useItem.onClick.Invoke();Check(stock.commonEggs==1&&starts==1&&saves==1&&stock.Occupied(0),"Incubation action changed");
                }
                finally{UnityEngine.Object.DestroyImmediate(eventsGO);}
                now+=1800;bind();d.Render();Check(first.progress.text.Contains("00:30:00"),"Remaining time changed");
                d.Export(Output+"/running.png");
                d.Resize(390,844);d.Render();d.Export(Output+"/running-390.png");
            }
            File.WriteAllText(Output+"/review.txt","PASS: four gold-matched mounted controls, centered and fully contained in metal body; circular geometry; contact shadow; separate fixed rim/moving face press+release; native raycast; one start/save/egg per click; timer/status separation; remaining time;720x1280 and390x844 captures.\n");
            EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Output+"/review.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
