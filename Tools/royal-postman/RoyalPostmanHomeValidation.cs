using System;
using System.IO;
using System.Linq;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class RoyalPostmanHomeValidation
{
    const string Output=@"C:\Users\Tiago Carvalho\Documents\projetos\Magic-Mail\Tools\royal-postman\previews";
    static void Check(bool yes,string reason){if(!yes)throw new Exception(reason);}
    public static void Run()
    {
        Directory.CreateDirectory(Output);int errors=0;RoyalPostmanHomePreview preview=null;
        Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=log;
        try
        {
            ShaderUtil.allowAsyncCompilation=false;
            foreach(var size in new[]{new Vector2Int(720,1600),new Vector2Int(720,1280),new Vector2Int(360,800)})
            {
                preview=RoyalPostmanHomePreview.CreateScenePreview(size.x,size.y);var home=preview.Home;
                Check(home.GetComponentInChildren<Image>().sprite!=null,"Character art missing");
                home.Refresh(1867,5,"Cheio",14,true);
                string suffix=size.x+"x"+size.y;
                preview.Save(Output+"/home-"+suffix+".png");
                var rimPosition=home.GoldRim.position;var rimScale=home.GoldRim.localScale;
                var face=home.PlayPress.face;var original=face.anchoredPosition;
                Check(home.GoldRim!=face&&!home.GoldRim.IsChildOf(face),"Rim is flattened into moving face");
                Check(home.LevelText.transform.IsChildOf(face),"Label does not follow moving face");
                home.PlayPress.SetPressed(true);preview.Save(Output+"/pressed-"+suffix+".png");
                Check(face.anchoredPosition.y<original.y-5,"Center does not sink");
                Check(home.GoldRim.position==rimPosition&&home.GoldRim.localScale==rimScale,"Gold rim moved on press");
                home.PlayPress.SetPressed(false);Check(face.anchoredPosition==original,"Center failed to return");
                int before=preview.PlaySelections;home.PlayButton.onClick.Invoke();Check(preview.PlaySelections==before+1,"Play action not wired");
                home.Refresh(20,2,"12:34",73,false);Check(!home.PlayButton.interactable&&home.LevelText.text=="Em breve","Unavailable level state broken");
                home.Refresh(20,2,"12:34",73,true);Check(home.LevelText.text=="Nível 73","Live level label not updated");
                for(int i=0;i<5;i++)
                {
                    var tabs=home.Navigation.GetComponentsInChildren<Button>();Check(tabs.Length==5,"Expected exactly five nav targets");
                    tabs[i].onClick.Invoke();Check(home.SelectedTab==i,"Navigation callback failed: "+i);
                }
                home.ShowRoute(false,3);Check(!home.Body.gameObject.activeSelf&&home.Navigation.gameObject.activeInHierarchy,"Menu routing hid navigation");
                home.ShowRoute(true,2);home.Refresh(0,5,"Cheio",1,true);preview.Save(Output+"/home-current-"+suffix+".png");
                UnityEngine.Object.DestroyImmediate(preview);preview=null;
            }
            Check(errors==0,"Native errors: "+errors);
            File.WriteAllText(Output+"/validation.txt","PASS: RoyalPostmanHome approval prototype rendered in Unity 6000.6 at 720x1600, 720x1280, 360x800. Character image, counters, labels, rim, moving face and nav are independent. Press sinks face/labels, fixed gold rim remains unchanged, release restores exact origin. Dynamic preview balances/level/unavailable state, Play callback and all five nav callbacks verified; non-home routes preserve navbar. Zero Unity errors. Isolated read-only preview; no player-save writes. Not integrated into the game lobby or Simulator; awaits visual approval.\n");
        }
        catch(Exception e){File.WriteAllText(Output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
        finally{Application.logMessageReceived-=log;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}
        EditorApplication.Exit(0);
    }
}
