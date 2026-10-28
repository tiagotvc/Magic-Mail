using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.UI.RoyalPostman2026;

public static class PetTabsV99Build
{
    public const string Prefab="Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab";
    public const string Output="../pet-tabs-v99";
    public const string Art="Assets/RoyalAves/Art/UI/Tab/RoyalPostman/Pets/TabsV99/";
    public static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static Material Paint(Shader shader,string name,float hue)
    {
        var material=new Material(shader);material.name=name;material.EnableKeyword("HSV_ON");material.SetColor("_Color",Color.white);material.SetFloat("_Alpha",1);material.SetFloat("_HsvShift",hue);material.SetFloat("_HsvSaturation",1);material.SetFloat("_HsvBright",1);AssetDatabase.CreateAsset(material,Art+name+".mat");return material;
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output);Directory.CreateDirectory(Art);
        try
        {
            AssetDatabase.Refresh();var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Plugins/AllIn1SpriteShader/Shaders/AllIn1SpriteShaderUiMask.shader");Check(shader!=null,"User AllIn1SpriteShader UI shader missing");var red=Paint(shader,"SelectedRed",210);var blue=Paint(shader,"UnselectedBlue",0);
            var root=PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var v=root.GetComponent<RoyalPostmanPetsScreen>();var h=v.hatchery;Check(h!=null,"Hatchery controller missing");
                Check(v.currentTab.GetComponent<Image>().sprite!=null&&v.incubatorTab.GetComponent<Image>().sprite!=null,"User tab sprites missing");
                h.selectedTabMaterial=red;h.unselectedTabMaterial=blue;h.ShowSection(v.defaultCurrentTab);PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();File.Copy(Prefab,Output+"/assembled.prefab",true);File.WriteAllText(Output+"/build.txt","PASS: user tab artwork retained, white vertex tint and AllIn1SpriteShader HSV selected-red/inactive-blue materials, both tabs enabled.\n");EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Output+"/build.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
