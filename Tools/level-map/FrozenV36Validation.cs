using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

public static class FrozenV36Validation
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/frozen-v36";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int errors;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static CurvedLevelMap Map(RoyalAvesLevelMapPreview p)=>(CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(p);
    static void Build(RoyalAvesLevelMapPreview p,int season,float level,int width=1080,int height=1920)
    {
        typeof(RoyalAvesLevelMapPreview).GetField("seasonPreview",Private).SetValue(p,season);
        typeof(RoyalAvesLevelMapPreview).GetField("level",Private).SetValue(p,level);
        typeof(RoyalAvesLevelMapPreview).GetMethod("Build",Private).Invoke(p,new object[]{width,height,Mathf.CeilToInt(level)});
        Map(p).World.TimeOverride=2.3f;p.RenderScenePreview();
    }
    static void Save(RoyalAvesLevelMapPreview p,string name)
    {
        p.RenderScenePreview();var rt=(RenderTexture)p.SceneTexture;var old=RenderTexture.active;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());}
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}
    }
    public static void Run()
    {
        Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        try{Execute();Check(errors==0,"Unity logged errors");File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: native winter saved-layout captures in 1080x1920 and 1080x2280; separate snow and reflective blue ice on ground; frozen first water passage material and native capture; water returns to normal outside winter; puddles independently honor Ice and None object themes; pooled ground detail clears frozen property after theme opt-out; no animated wakes on winter crossings; zero Unity errors. Reflection is stylized painted sky, without a real-time scene reflection pass. Main scene, saved authoring layouts and active season were not written. Physical device performance was not measured.");EditorApplication.Exit(0);}
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Execute()
    {
        Directory.CreateDirectory(Output);var p=RoyalAvesLevelMapPreview.CreateScenePreview(1080,1920);
        var layout=Resources.Load<WorldMapLayout>(WorldMapLayout.SimpleResourcePath);var original=EditorJsonUtility.ToJson(layout);
        try
        {
            Build(p,2,1);Save(p,"map-1080x1920");Build(p,2,1,1080,2280);Save(p,"map-1080x2280");
            var world=Map(p).World;var first=world.Route.Gaps[0];var level=world.Route.LevelAt((first.start+first.end)*.5f);
            Build(p,2,level);world=Map(p).World;
            var water=(Material)typeof(WorldMapView).GetField("matWater",Private).GetValue(world);
            Check(water.GetFloat("_Frozen")==1,"Winter water passage is not frozen");
            foreach(var r in world.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="DetalheDoChao"&&r.sharedMaterial.shader.name.Contains("Effect")))
                Check(!r.gameObject.activeInHierarchy,"A moving wake remains above frozen water");
            Save(p,"crossing-frozen");Build(p,1,level);world=Map(p).World;
            water=(Material)typeof(WorldMapView).GetField("matWater",Private).GetValue(world);
            Check(water.GetFloat("_Frozen")==0,"Water remained frozen after leaving winter");Save(p,"crossing-normal");
            var chapter=layout.Find(0);if(chapter==null){chapter=new WorldMapLayout.Chapter{index=0};layout.chapters.Add(chapter);}
            chapter.manual=true;chapter.entries.Clear();var kit=Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath);
            var frozen=new WorldMapLayout.Entry{id="frozen-puddle",sprite=kit.groundDetails[0],keepSprite=true,customSize=true,height=1.3f,kind=PropKind.GroundDetail,level=2.6f,lateral=-1.65f,theme=WorldMapObjectTheme.Ice};
            chapter.entries.Add(frozen);chapter.entries.Add(new WorldMapLayout.Entry{id="normal-puddle",sprite=kit.groundDetails[0],keepSprite=true,customSize=true,height=1.3f,kind=PropKind.GroundDetail,level=2.6f,lateral=1.65f,theme=WorldMapObjectTheme.None});
            Build(p,2,1);world=Map(p).World;var block=new MaterialPropertyBlock();
            var details=world.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="DetalheDoChao"&&r.sharedMaterial.mainTexture==kit.groundDetails[0].texture).ToArray();
            Check(details.Length==2,"Puddle fixtures missing");int iced=0,liquid=0;
            foreach(var r in details){r.GetPropertyBlock(block);if(block.GetFloat("_FrozenPuddle")>0)iced++;else liquid++;}
            Check(iced==1&&liquid==1,"Puddle theme isolation failed");Save(p,"puddles-ice-and-water");
            frozen.theme=WorldMapObjectTheme.None;world.ReloadScenery();p.RenderScenePreview();
            foreach(var r in world.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="DetalheDoChao")){r.GetPropertyBlock(block);Check(block.GetFloat("_FrozenPuddle")==0,"Pooled puddle retained frost");}
        }
        finally{UnityEngine.Object.DestroyImmediate(p);EditorJsonUtility.FromJsonOverwrite(original,layout);}
    }
}
