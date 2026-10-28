using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RoyalAves.EditorTools;
using RoyalAves.UI.RoyalPostman2026;

public static class PetsTooltipChecks
{
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    public static void Run(RoyalPostmanRuntimeKit kit,string output)
    {
        foreach(var size in new[]{new Vector2Int(360,800),new Vector2Int(720,1280),new Vector2Int(1080,1920)})using(var d=new RoyalPostmanScreenDocument(kit,"pets",size.x,size.y)){
            var view=d.Root.GetComponent<RoyalPostmanPetsScreen>();view.Initialize(()=>{},i=>{});view.ShowCurrent(false);d.Render();
            var caption=d.Root.transform.Find("IncubatorTab/Label").GetComponent<Text>();
            Check(caption.resizeTextForBestFit&&caption.cachedTextGenerator.lineCount<=1,"Incubadora caption wrapped");
            var vertices=caption.cachedTextGenerator.verts;float lo=float.MaxValue,hi=float.MinValue;
            for(int i=0;i<Math.Max(0,vertices.Count-4);i++){lo=Mathf.Min(lo,vertices[i].position.x);hi=Mathf.Max(hi,vertices[i].position.x);}
            Check(hi-lo<=caption.rectTransform.rect.width*caption.pixelsPerUnit+1,"Incubadora text overflows its inset");
            var events=new GameObject("TooltipEvents",typeof(EventSystem));
            try{
                foreach(int index in new[]{0,1,7,11}){
                    view.SelectItem(index);d.Render();
                    Check(view.itemTooltip.activeSelf&&view.hintTitle.text==view.items[index].title&&view.hintDescription.text.Contains(view.items[index].description)&&view.hintDescription.text.Contains("Quantidade:"),"Tooltip metadata incorrect");
                    Check(view.tooltipIcon.sprite==view.items[index].button.transform.Find("Icon").GetComponent<Image>().sprite,"Tooltip icon does not match item");
                    var bounds=d.Bounds(view.tooltipCard);Check(bounds.xMin>=0&&bounds.yMin>=0&&bounds.xMax<=size.x&&bounds.yMax<=size.y,"Tooltip clipped by screen");
                    Check(view.hintDescription.preferredHeight<=view.hintDescription.rectTransform.rect.height+1,"Tooltip description clipped");
                }
                view.SelectItem(1);d.Export(output+"/pets-tooltip-"+size.x+".png");
                var hits=new List<RaycastResult>();var raycaster=d.Root.GetComponentInParent<GraphicRaycaster>();
                raycaster.Raycast(new PointerEventData(events.GetComponent<EventSystem>()){position=d.Camera.WorldToScreenPoint(view.items[0].button.transform.position)},hits);
                Check(hits.Count>0&&(hits[0].gameObject.transform==view.items[0].button.transform||hits[0].gameObject.transform.IsChildOf(view.items[0].button.transform)),"Open tooltip blocks another inventory item");
                view.items[0].button.onClick.Invoke();Check(view.hintTitle.text==view.items[0].title,"Item click did not replace tooltip");
                view.dismissItemTooltip.onClick.Invoke();Check(!view.itemTooltip.activeSelf,"Tooltip close button failed");
                view.SelectItem(0);var outside=view.itemTooltip.transform.Find("Outside");ExecuteEvents.Execute(outside.gameObject,new PointerEventData(events.GetComponent<EventSystem>()),ExecuteEvents.pointerClickHandler);Check(!view.itemTooltip.activeSelf,"Outside click failed");
                view.SelectItem(0);view.SetInventoryPage(0,1);Check(!view.itemTooltip.activeSelf,"Page change kept obsolete tooltip");view.SetInventoryPage(0,0);
                d.PreviewClick(view.items[1].button.GetComponent<RectTransform>());Check(view.itemTooltip.activeSelf,"Editor click does not open same tooltip");
                d.PreviewClick(view.dismissItemTooltip.GetComponent<RectTransform>());Check(!view.itemTooltip.activeSelf,"Editor tooltip close failed");
            }finally{UnityEngine.Object.DestroyImmediate(events);}
        }
        using(var d=new RoyalPostmanScreenDocument(kit,"pets",720,1280)){
            var view=d.Root.GetComponent<RoyalPostmanPetsScreen>();view.ShowCurrent(false);view.SelectItem(0);d.Save();Check(!view.itemTooltip.activeSelf&&view.hintTitle.text=="","Save retained tooltip state");
            using(var reopened=new RoyalPostmanScreenDocument(kit,"pets",720,1280)){var fresh=reopened.Root.GetComponent<RoyalPostmanPetsScreen>();Check(!fresh.itemTooltip.activeSelf,"Reopened screen has a stale tooltip");foreach(var item in fresh.items)Check(!item.selection.activeSelf,"Editor save retained transient item selection");}
        }
    }
}
