using System;
using System.IO;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

public static class MoonIceV39Validation
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/moon-ice-v39";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int errors;
    public static void Run()
    {
        Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        RoyalAvesLevelMapPreview preview=null;
        try
        {
            Directory.CreateDirectory(Output);preview=RoyalAvesLevelMapPreview.CreateScenePreview(1080,1920);
            typeof(RoyalAvesLevelMapPreview).GetField("seasonPreview",Private).SetValue(preview,2);
            typeof(RoyalAvesLevelMapPreview).GetMethod("Build",Private).Invoke(preview,new object[]{1080,1920,1});
            var map=(CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview);var world=map.World;
            world.TimeOverride=3;world.ConfigureDayNight(false,4,0);Save(preview,"day");
            world.ConfigureDayNight(false,4,1);Save(preview,"night-3s");
            var water=(Material)typeof(WorldMapView).GetField("matWater",Private).GetValue(world);
            if(water.GetFloat("_IceNight")!=1||!world.IceNeedsFrames)throw new Exception("Night ice illumination or animated preview missing");
            world.TimeOverride=7;Save(preview,"night-7s");
            if(errors!=0)throw new Exception("Unity logged errors");
            File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: production shaders compiled and rendered in native Unity at 1080x1920; saved map day/night captures at two animation times; night reflection bindings and editor animation checked; zero Unity errors. Source scene, layout and active season were not saved. Physical-device performance was not measured.");
            UnityEngine.Object.DestroyImmediate(preview);preview=null;EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);EditorApplication.Exit(1);}
    }
    static void Save(RoyalAvesLevelMapPreview preview,string name)
    {
        preview.RenderScenePreview();var rt=(RenderTexture)preview.SceneTexture;var old=RenderTexture.active;var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),texture.EncodeToPNG());}
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(texture);}
    }
}
