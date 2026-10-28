using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.EditorTools;
using RoyalAves.UI.RoyalPostman2026;
public static class UISharpnessV65Audit
{
    const string Output="../ui-sharpness-v65";
    static readonly string[] Files={"rounded_total_close_border","rounded_total_close_center"};
    static void Capture(string tag)
    {
        using(var d=new RoyalPostmanScreenDocument(RoyalPostmanRuntimeKit.Load(),"pets",1080,1920))
        {
            d.Root.GetComponent<RoyalPostmanPetsScreen>().ShowCurrent(false);
            d.Export(Output+"/"+tag+"-1080.png");
            d.Resize(497,884);d.Export(Output+"/"+tag+"-497.png");
        }
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        try
        {
            var report="";
            foreach(var name in Files){string path="Assets/RoyalAves/Art/UI/UI assets/"+name+".png";var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var i=(TextureImporter)AssetImporter.GetAtPath(path);report+="BEFORE "+name+": "+t.width+"x"+t.height+" "+t.format+" mips="+t.mipmapCount+" filter="+t.filterMode+" type="+i.textureType+" compression="+i.textureCompression+"\n";}
            var knob=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");report+="Placeholder Knob: "+knob.texture.width+"x"+knob.texture.height+" rect="+knob.rect+"\n";
            Capture("before");
            foreach(var name in Files)
            {
                string path="Assets/RoyalAves/Art/UI/UI assets/"+name+".png";var i=(TextureImporter)AssetImporter.GetAtPath(path);
                i.textureCompression=TextureImporterCompression.Uncompressed;i.crunchedCompression=false;i.mipmapEnabled=true;i.mipmapFilter=TextureImporterMipFilter.BoxFilter;i.filterMode=FilterMode.Trilinear;i.maxTextureSize=8192;i.npotScale=TextureImporterNPOTScale.None;i.alphaIsTransparency=true;
                var p=i.GetDefaultPlatformTextureSettings();p.textureCompression=TextureImporterCompression.Uncompressed;p.maxTextureSize=8192;i.SetPlatformTextureSettings(p);
                foreach(var target in new[]{"Standalone","Android","iPhone","WebGL","VisionOS"})i.ClearPlatformTextureSettings(target);
                i.SaveAndReimport();
                var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);report+="AFTER "+name+": "+t.width+"x"+t.height+" "+t.format+" mips="+t.mipmapCount+" filter="+t.filterMode+" type="+i.textureType+" compression="+i.textureCompression+"\n";
            }
            Capture("after");
            File.WriteAllText(Output+"/audit.txt",report);EditorApplication.Exit(0);
        }catch(Exception e){File.WriteAllText(Output+"/audit.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}

