using System;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.EditorTools;
using RoyalAves.UI.RoyalPostman2026;

public static class PetsTabStateChecks
{
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    static void State(RoyalPostmanPetsScreen view,bool current)
    {
        var paw=view.currentTab.transform.Find("Face").GetComponent<Image>();var egg=view.incubatorTab.transform.Find("Face").GetComponent<Image>();
        Check(paw.sprite==(current?view.currentTabGold:view.currentTabBlue)&&egg.sprite==(current?view.incubatorTabBlue:view.incubatorTabGold),"Selected tab does not own the gold state");
        Check(paw.color==Color.white&&egg.color==Color.white,"Tab states must use painted colors");
        Check(view.currentPanel.activeSelf==current&&view.workshop.activeSelf!=current,"Tab content does not match gold state");
    }
    public static void Run(RoyalPostmanRuntimeKit kit,string output)
    {
        using(var document=new RoyalPostmanScreenDocument(kit,"pets",720,1280)){
            var view=document.Root.GetComponent<RoyalPostmanPetsScreen>();view.Initialize(()=>{},i=>{});
            Check(view.currentTabBlue!=null&&view.currentTabGold!=null&&view.incubatorTabBlue!=null&&view.incubatorTabGold!=null,"Four tab states missing");
            view.ShowCurrent(false);State(view,false);document.Export(output+"/pets-tab-incubator-selected-720.png");
            view.currentTab.onClick.Invoke();State(view,true);document.Export(output+"/pets-tab-current-selected-720.png");
            view.incubatorTab.onClick.Invoke();State(view,false);
            document.PreviewClick(view.currentTab.GetComponent<RectTransform>());State(view,true);
            document.PreviewClick(view.incubatorTab.GetComponent<RectTransform>());State(view,false);
        }
        // The authoring editor previews tabs without initializing runtime availability/state.
        using(var document=new RoyalPostmanScreenDocument(kit,"pets",720,1280)){
            var view=document.Root.GetComponent<RoyalPostmanPetsScreen>();
            document.PreviewClick(view.currentTab.GetComponent<RectTransform>());document.Save();
            State(view,true); // Saving defaults must not change the editor's current preview.
            using(var reopened=new RoyalPostmanScreenDocument(kit,"pets",720,1280))State(reopened.Root.GetComponent<RoyalPostmanPetsScreen>(),reopened.Root.GetComponent<RoyalPostmanPetsScreen>().defaultCurrentTab);
        }
    }
}
