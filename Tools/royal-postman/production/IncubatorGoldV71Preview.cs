using UnityEditor;
using UnityEngine;
using RoyalAves.EditorTools;
using RoyalAves.UI.RoyalPostman2026;

public static class IncubatorGoldV71Preview
{
    public static void Run()
    {
        var gold=AssetDatabase.LoadAssetAtPath<Material>(IncubatorFitV71Build.MaterialPath);
        gold.SetColor("_PaletteShadow",new Color32(178,84,0,255));
        gold.SetColor("_PaletteMid",new Color32(255,174,0,255));
        gold.SetColor("_PaletteHighlight",new Color32(255,249,64,255));
        EditorUtility.SetDirty(gold);AssetDatabase.SaveAssets();
        using(var d=new RoyalPostmanScreenDocument(RoyalPostmanRuntimeKit.Load(),"pets",720,1280))
        {
            d.Root.GetComponent<RoyalPostmanPetsScreen>().ShowCurrent(false);
            d.Export(IncubatorFitV71Build.Output+"/gold-refined.png");
        }
        EditorApplication.Exit(0);
    }
}
