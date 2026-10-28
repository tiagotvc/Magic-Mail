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

public static class FastForwardV93Review
{
    public static void Run()
    {
        const string output=FastForwardV93Build.Output;
        try
        {
            using(var d=new RoyalPostmanScreenDocument(RoyalPostmanRuntimeKit.Load(),"pets",720,1280))
            {
                var view=d.Root.GetComponent<RoyalPostmanPetsScreen>();view.Initialize(()=>{},i=>{});var h=view.hatchery;var p=h.accelerate.GetComponent<RoyalPostmanPaintedButton>();var icon=p.movingCenter.Find("FastForwardIcon");
                FastForwardV93Build.Check(!icon.GetComponent<Text>().enabled&&icon.childCount==2,"Font icon still active or missing chevron");
                var arrows=icon.GetComponentsInChildren<Image>();FastForwardV93Build.Check(arrows.Length==2&&arrows.All(a=>a.sprite.rect.height==1260&&a.material.shader.name=="RoyalPostman/UI/NeutralPalette"&&a.preserveAspect&&!a.raycastTarget),"PNG/cream material/raycast contract broken");
                int saves=0,boosts=0;long now=1700000000;var stock=new PetsIncubationState{commonEggs=1,accelerationItems=new[]{1,0,0,0}};
                view.BindInventory(stock,()=>saves++,n=>false,()=>0,()=>now);view.RefreshAvailability(100);view.ShowCurrent(false);view.AccelerateRequested+=i=>boosts++;h.incubate.onClick.Invoke();h.accelerate.onClick.Invoke();
                FastForwardV93Build.Check(saves==2&&boosts==1&&stock.accelerationItems[0]==0&&stock.Remaining(0,now)==1800,"Acceleration action changed");
                d.Render();
                var events=new GameObject("ChevronReview",typeof(EventSystem));
                try
                {
                    h.accelerate.interactable=true;var data=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=d.Camera.WorldToScreenPoint(h.accelerate.transform.position)};
                    var hits=new System.Collections.Generic.List<RaycastResult>();d.Root.GetComponentInParent<GraphicRaycaster>().Raycast(data,hits);FastForwardV93Build.Check(hits.Any(x=>x.gameObject==h.accelerate.gameObject),"Icon intercepts accelerator hit");
                    var fixedRim=h.accelerate.transform.Find("FixedGold");var rim=fixedRim.position;var rest=p.movingCenter.anchoredPosition;
                    p.OnPointerDown(data);FastForwardV93Build.Check(p.movingCenter.anchoredPosition!=rest&&fixedRim.position==rim,"Press/fixed rim broken");p.OnPointerUp(data);FastForwardV93Build.Check(p.movingCenter.anchoredPosition==rest,"Pressed glyph does not restore");
                }
                finally{UnityEngine.Object.DestroyImmediate(events);}
                foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(720,1280)})
                {
                    d.Resize(size.x,size.y);d.Render();var left=d.Bounds(arrows[0].rectTransform);var right=d.Bounds(arrows[1].rectTransform);var container=d.Bounds((RectTransform)icon);
                    FastForwardV93Build.Check(left.xMax<right.xMin&&left.xMin>=container.xMin&&right.xMax<=container.xMax&&left.yMin>=container.yMin&&left.yMax<=container.yMax,"Chevrons overlap or overflow the authored icon area");
                }
            }
            File.WriteAllText(output+"/review.txt","PASS: native compile/render; two separate thick rounded PNG chevrons with shared cream material/brown contour; aspect/spacing/bounds at390/720; nonblocking raycast; original press/fixed rim and exactly-one accelerator/save/item action preserved.\n");EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(output+"/review.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
