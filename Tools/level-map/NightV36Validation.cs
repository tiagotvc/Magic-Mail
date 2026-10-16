using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

public static class NightV36Validation
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/night-v36";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int errors;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        RoyalAvesLevelMapPreview p=null;
        try
        {
            Directory.CreateDirectory(Output);
            Check(WorldMapKit.NightAt(0,4)==0&&WorldMapKit.NightAt(120,4)==1&&WorldMapKit.NightAt(240,4)==0,"Cycle does not cover day/night/day");
            for(int t=1;t<120;t++)Check(WorldMapKit.NightAt(t,4)>=WorldMapKit.NightAt(t-1,4),"Cycle is not smooth/monotonic toward night");
            p=RoyalAvesLevelMapPreview.CreateScenePreview(1080,1920);
            typeof(RoyalAvesLevelMapPreview).GetField("seasonPreview",Private).SetValue(p,2);
            typeof(RoyalAvesLevelMapPreview).GetMethod("Build",Private).Invoke(p,new object[]{1080,1920,1});
            var kit=(WorldMapKit)typeof(RoyalAvesLevelMapPreview).GetField("previewKit",Private).GetValue(p);
            var map=(CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(p);var world=map.World;
            world.TimeOverride=2.3f;world.ConfigureDayNight(false,4,0);p.RenderScenePreview();Save(p,"day");
            var bulb=world.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.StartsWith("LuzDeNatal_"));var block=new MaterialPropertyBlock();bulb.GetPropertyBlock(block);var dayEmission=block.GetFloat("_Emission");
            world.ConfigureDayNight(false,4,.45f);p.RenderScenePreview();Check(Mathf.Abs(world.Night-.45f)<.001f,"Dusk ignored");Save(p,"dusk");
            world.ConfigureDayNight(false,4,1);p.RenderScenePreview();Check(world.Night==1&&map.NeedsPreviewFrames,"Night animation stopped");Save(p,"night");
            bulb.GetPropertyBlock(block);Check(block.GetFloat("_Emission")>dayEmission+.3f,"Christmas bulbs have no extra night emission");
            var halos=world.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("HaloDeNatal_")).ToArray();
            Check(halos.Length>0,"Christmas bulb halos missing");halos[0].GetPropertyBlock(block);Check(block.GetColor("_Color").a>.05f,"Christmas halos remain invisible at night");
            Check(world.GetComponentsInChildren<MeshRenderer>().Any(r=>r.name=="ReflexoDaJanela"),"No registered window rays in saved map: "+string.Join(",",world.Props.Where(v=>v.kind==PropKind.Landmark).Select(v=>((Sprite)typeof(WorldMapView).GetMethod("PoseSprite",Private).Invoke(world,new object[]{v})).name)));
            var atmosphere=(WorldMapAtmosphere)typeof(CurvedLevelMap).GetField("atmosphere",Private).GetValue(map);Check(atmosphere.Night==1,"Sky did not receive night transition");
            world.ConfigureDayNight(false,4,0);p.RenderScenePreview();foreach(var r in world.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("HaloDeNatal_"))){r.GetPropertyBlock(block);Check(block.GetColor("_Color").a==0,"Christmas halo remains on during day");}
            Check(errors==0,"Unity logged errors");
            File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: full day/night/day cycle endpoints, monotonic sunset transition, native saved-layout day/dusk/night screenshots, sky receives night blend, brighter Christmas bulb emission and colored halos, registered window light rays, night preview continues animating, daytime clears night halos; zero Unity errors. Native rendering is stylized illumination; no real-time raytraced reflections. Source scene, layout and season were not saved by this validation. Physical-device performance and manual editor interaction were not measured.");
            UnityEngine.Object.DestroyImmediate(p);p=null;EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);if(p!=null)UnityEngine.Object.DestroyImmediate(p);EditorApplication.Exit(1);}
    }
    static void Save(RoyalAvesLevelMapPreview p,string name)
    {
        p.RenderScenePreview();var rt=(RenderTexture)p.SceneTexture;var old=RenderTexture.active;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());}
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}
    }
}
