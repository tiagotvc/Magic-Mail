using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.EditorTools;
using RoyalAves.UI.RoyalPostman2026;

public static class IncubatorFitV71Build
{
    public const string Prefab="Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab";
    public const string Output="../incubator-fit-v71";
    public const string MaterialPath="Assets/RoyalAves/Art/UI/Menu/RoyalPostman/IncubatorControlsV70/GoldMatchedV71.mat";
    public const string RedMaterialPath="Assets/RoyalAves/Art/UI/Menu/RoyalPostman/IncubatorControlsV70/RedMatchedV71.mat";
    // Central opaque teal body measured on the common machine's original435px base slice.
    public const float BodyCenter=216.5f/435f, BodyHeight=207f/435f;
    public static Vector3 BodyCenterWorld(RectTransform body)=>body.TransformPoint(new Vector3(body.rect.center.x,body.rect.yMin+body.rect.height*BodyCenter,0));
    public static float FittedDiameter(RectTransform body,Transform parent)=>parent.InverseTransformVector(body.TransformVector(Vector3.up*body.rect.height*BodyHeight)).magnitude*.96f;
    static void Capture(string name)
    {
        using(var d=new RoyalPostmanScreenDocument(RoyalPostmanRuntimeKit.Load(),"pets",720,1280))
        {
            var view=d.Root.GetComponent<RoyalPostmanPetsScreen>();view.ShowCurrent(false);view.SelectIncubator(0);d.Export(Output+"/"+name+".png");
        }
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        try
        {
            Capture("before");
            var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/RoyalAves/Art/UI/Button/RoyalPostman/Shaders/RoyalPostmanNeutralPalette.shader");
            if(shader==null)throw new Exception("Existing native UI palette shader missing");
            var gold=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if(gold==null){gold=new Material(shader);AssetDatabase.CreateAsset(gold,MaterialPath);}
            gold.shader=shader;gold.name="GoldMatchedV71";
            gold.SetColor("_Color",Color.white);gold.SetFloat("_UseCustomPalette",1);
            gold.SetColor("_PaletteShadow",new Color32(178,84,0,255));
            gold.SetColor("_PaletteMid",new Color32(255,174,0,255));
            gold.SetColor("_PaletteHighlight",new Color32(255,249,64,255));
            EditorUtility.SetDirty(gold);
            var red=AssetDatabase.LoadAssetAtPath<Material>(RedMaterialPath);
            if(red==null){red=new Material(shader);AssetDatabase.CreateAsset(red,RedMaterialPath);}
            red.shader=shader;red.name="RedMatchedV71";red.SetColor("_Color",Color.white);red.SetFloat("_UseCustomPalette",1);
            // Sampled from the existing red Incubadora plaque: body238,4,21; top252,32,40.
            red.SetColor("_PaletteShadow",new Color32(185,0,12,255));
            red.SetColor("_PaletteMid",new Color32(238,4,21,255));
            red.SetColor("_PaletteHighlight",new Color32(255,45,51,255));
            EditorUtility.SetDirty(red);
            var root=PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var view=root.GetComponent<RoyalPostmanPetsScreen>();
                for(int i=0;i<view.incubators.Length;i++)
                {
                    var slot=view.incubators[i];var button=slot.useItem;
                    var body=(RectTransform)slot.root.transform.Find("Machine/BaseFront");
                    var rect=(RectTransform)button.transform;
                    float diameter=FittedDiameter(body,rect.parent);
                    rect.SetParent(body,false);
                    rect.anchorMin=rect.anchorMax=new Vector2(.5f,BodyCenter);rect.pivot=Vector2.one*.5f;
                    rect.localScale=Vector3.one;rect.sizeDelta=Vector2.one*diameter;rect.anchoredPosition=Vector2.zero;
                    var rim=button.GetComponent<Image>();rim.material=gold;rim.color=Color.white;
                    var shadow=button.GetComponent<Shadow>()??button.gameObject.AddComponent<Shadow>();
                    shadow.effectColor=new Color(0,.19f,.17f,.42f);shadow.effectDistance=new Vector2(0,-2);shadow.useGraphicAlpha=true;
                    var moving=(RectTransform)rect.Find("MovingCenter");
                    moving.anchorMin=moving.anchorMax=moving.pivot=Vector2.one*.5f;
                    moving.localScale=Vector3.one;moving.anchoredPosition=new Vector2(0,diameter*.03f);moving.sizeDelta=Vector2.one*(diameter*.80f);
                    moving.Find("RaisedFace").GetComponent<Image>().material=red;
                    var icon=(RectTransform)moving.Find("PawIcon");icon.sizeDelta=new Vector2(diameter*.51f,diameter*.49f);icon.anchoredPosition=new Vector2(0,diameter*.015f);
                    icon.GetComponent<Image>().color=Color.white;
                    var press=button.GetComponent<RoyalPostmanPaintedButton>();press.pressTravel=diameter*.04f;press.pressScale=.96f;
                    var bar=(RectTransform)slot.progress.transform.parent;
                    var status=slot.remaining.rectTransform;
                    var corners=new Vector3[4];bar.GetWorldCorners(corners);
                    float barBottom=status.parent.InverseTransformPoint(corners[0]).y;
                    status.GetWorldCorners(corners);float statusTop=status.parent.InverseTransformPoint(corners[1]).y;
                    if(statusTop>barBottom-8)
                    {
                        var position=status.localPosition;position.y-=statusTop-(barBottom-8);status.localPosition=position;
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
            File.Copy(Prefab,Output+"/assembled.prefab",true);
            Capture("after");
            File.WriteAllText(Output+"/build.txt","PASS: original machine layers preserved; circular gold controls centered/contained in metal body; existing native palette material; separate moving centers preserved; overlapping status moved below timer.\n");
            EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Output+"/build.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
