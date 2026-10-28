using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.UI.RoyalPostman2026;

public static class HatcheryV88Build
{
    public const string Prefab="Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab";
    public const string Output="../hatchery-v88";
    public static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static Text Label(Text template,Transform parent,string name,string text,Vector2 position,Vector2 size,int point)
    {
        var label=UnityEngine.Object.Instantiate(template,parent,false);label.name=name;label.text=text;label.fontSize=point;
        var r=label.rectTransform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size;r.localScale=Vector3.one;
        label.resizeTextForBestFit=false;label.raycastTarget=false;label.gameObject.SetActive(true);return label;
    }
    static Button Round(Button template,string name,float x)
    {
        var button=UnityEngine.Object.Instantiate(template,template.transform.parent,false);button.name=name;
        const float scale=144f/216f;
        foreach(var r in button.GetComponentsInChildren<RectTransform>(true))
        {
            if(r==button.transform)continue;
            r.anchoredPosition*=scale;r.sizeDelta*=scale;
        }
        var rect=(RectTransform)button.transform;rect.anchoredPosition=new Vector2(x,((RectTransform)template.transform).anchoredPosition.y);rect.sizeDelta=Vector2.one*144;
        button.onClick=new Button.ButtonClickedEvent();button.GetComponent<RoyalPostmanPaintedButton>().pressTravel*=scale;
        return button;
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        try
        {
            AssetDatabase.Refresh();var root=PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var h=root.GetComponent<RoyalPostmanPetsScreen>().hatchery;Check(h!=null&&h.cancel==null,"Not a fresh V87 source");
                h.transform.Find("AcceleratorDisplay").gameObject.SetActive(false);
                var cancel=Round(h.incubate,"Cancel",-285);
                var speed=Round(h.incubate,"Accelerate",285);
                var cancelIcon=cancel.GetComponent<RoyalPostmanPaintedButton>().movingCenter.Find("Icon").GetComponent<Image>();
                cancelIcon.sprite=root.GetComponent<RoyalPostmanPetsScreen>().helpButton.GetComponent<RoyalPostmanPaintedButton>().movingCenter.Find("Icon").GetComponent<Image>().sprite;
                Check(cancelIcon.sprite!=null,"User cancel PNG missing");
                var speedCenter=speed.GetComponent<RoyalPostmanPaintedButton>().movingCenter;
                speedCenter.Find("Icon").gameObject.SetActive(false);
                Label(h.actionLabel,speedCenter,"FastForwardIcon",">>",new Vector2(-2,2),new Vector2(102,84),70);
                h.cancel=cancel;h.accelerate=speed;
                h.cancelLabel=Label(h.actionLabel,cancel.transform,"Label","Cancelar",new Vector2(0,-142),new Vector2(248,42),34);
                h.accelerateLabel=Label(h.actionLabel,speed.transform,"Label","Acelerar",new Vector2(0,-142),new Vector2(248,42),34);
                h.accelerateQuantity=Label(h.actionLabel,speed.transform,"Quantity","x0",new Vector2(0,-185),new Vector2(248,40),30);
                h.accelerateQuantity.resizeTextForBestFit=true;h.accelerateQuantity.resizeTextMinSize=20;h.accelerateQuantity.resizeTextMaxSize=30;
                h.actionLabel.rectTransform.sizeDelta=new Vector2(300,h.actionLabel.rectTransform.sizeDelta.y);
                var timer=(RectTransform)h.transform.Find("DurationDisplay");timer.anchoredPosition=new Vector2(0,timer.anchoredPosition.y);timer.sizeDelta=new Vector2(760,timer.sizeDelta.y);
                h.Refresh();PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();File.Copy(Prefab,Output+"/assembled.prefab",true);
            File.WriteAllText(Output+"/build.txt","PASS: two smaller user round controls beside unchanged Incubate; lower accelerator hidden, digital timer centered.\n");EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Output+"/build.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
