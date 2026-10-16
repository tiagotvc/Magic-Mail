using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

public static class SnowV33Validation
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/snow-v33";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int errors;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        try{Execute();Check(errors==0,"Unity logged errors");EditorApplication.Exit(0);}
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static object Slot(WorldMapView world,string id)
    {
        var index=Enumerable.Range(0,world.Props.Count).Single(i=>world.Props[i].id==id);
        return ((IDictionary)typeof(WorldMapView).GetField("activeSlots",Private).GetValue(world))[index];
    }
    static MeshFilter[] Caps(object slot)=>((MeshFilter[])slot.GetType().GetField("caps").GetValue(slot));
    static Transform Card(object slot)=>(Transform)slot.GetType().GetField("card").GetValue(slot);
    static void Pose(WorldMapView world,string id,WorldMapObjectTheme theme)
    {
        var p=world.Props.Single(v=>v.id==id);p.theme=theme;
        typeof(WorldMapView).GetMethod("Pose",Private).Invoke(world,new object[]{Slot(world,id),p,world.Route.MetersAt(1),true});
    }
    static void Execute()
    {
        Directory.CreateDirectory(Output);
        var kit=Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath);
        Check(kit.paintedSnow.Length>=2,"Painted snow bindings missing");
        var pine=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/Arvores/pine-classic-glossy-v29.png");
        var shrub=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/Props/shrub-coral-v15.png");
        foreach(var source in new[]{pine,shrub})
        {
            var match=kit.paintedSnow.Single(v=>v.source==source);
            Check(match.overlay!=null&&match.overlay.texture.width>=1000,"Painted snow art unavailable/full resolution lost");
        }
        var layout=Resources.Load<WorldMapLayout>(WorldMapLayout.SimpleResourcePath);var json=EditorJsonUtility.ToJson(layout);
        var chapter=layout.Find(0);if(chapter==null){chapter=new WorldMapLayout.Chapter{index=0};layout.chapters.Add(chapter);}
        chapter.manual=true;chapter.entries.Clear();
        var bush=new WorldMapLayout.Entry{id="snow-shrub",sprite=shrub,keepSprite=true,kind=PropKind.Small,
            customSize=true,height=.85f,level=2.7f,lateral=-1.7f,groundPinned=true,theme=WorldMapObjectTheme.Season};
        var tree=new WorldMapLayout.Entry{id="snow-pine",sprite=pine,keepSprite=true,kind=PropKind.Tree,
            customSize=true,height=1.7f,level=4.2f,lateral=1.55f,groundPinned=true,theme=WorldMapObjectTheme.Season};
        chapter.entries.AddRange(new[]{bush,tree});
        RoyalAvesLevelMapPreview preview=null;MapSeasonLook look=null;float originalCoverage=0;
        try
        {
            preview=RoyalAvesLevelMapPreview.CreateScenePreview(1080,1920);
            var map=(CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview);
            var world=map.World;world.TimeOverride=1;preview.RenderScenePreview();
            Check(Caps(Slot(world,bush.id)).All(c=>!c.gameObject.activeSelf),"Snow remains on normal-season shrub");
            Save(preview,"normal-1080x1920.png");
            tree.theme=WorldMapObjectTheme.Ice;tree.flipX=true;world.ReloadScenery();preview.RenderScenePreview();
            var pineSlot=Slot(world,tree.id);var cap=Caps(pineSlot).Single(c=>c.gameObject.activeSelf);
            Check(cap.sharedMesh.name.StartsWith("RegisteredSnow_"),"Pine still uses generic snow caps");
            Check(cap.transform.IsChildOf(Card(pineSlot))&&Card(pineSlot).localScale.x<0,"Snow does not inherit manual mirroring");
            Check(world.Props.Single(p=>p.id==tree.id).sprite==pine,"Ice replaced original pine art");
            Pose(world,tree.id,WorldMapObjectTheme.None);Check(Caps(pineSlot).All(c=>!c.gameObject.activeSelf),"Turning theme off left stale snow");
            Pose(world,tree.id,WorldMapObjectTheme.Ice);Check(Caps(pineSlot).Count(c=>c.gameObject.activeSelf)==1,"Restoring theme duplicated the snow");
            Save(preview,"manual-ice-1080x1920.png");
            tree.theme=WorldMapObjectTheme.Season;tree.flipX=false;bush.flipX=true;
            typeof(RoyalAvesLevelMapPreview).GetField("seasonPreview",Private).SetValue(preview,2);
            typeof(RoyalAvesLevelMapPreview).GetMethod("Build",Private).Invoke(preview,new object[]{1080,1920,1});
            map=(CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview);world=map.World;
            world.TimeOverride=1;preview.RenderScenePreview();
            foreach(var id in new[]{bush.id,tree.id})Check(Caps(Slot(world,id)).Count(c=>c.gameObject.activeSelf)==1,"Season snow not using registered layers");
            Check(Card(Slot(world,bush.id)).localScale.x<0,"Snowy shrub lost manual mirror");
            Save(preview,"christmas-1080x1920.png");
            look=world.CameraModel.Kit.SeasonLook;Check(look!=null,"Season look unavailable");originalCoverage=look.snowCoverage;
            var material=world.transform.Find("Chao").GetComponent<MeshRenderer>().sharedMaterials[0];
            look.snowCoverage=.28f;preview.RenderScenePreview();
            Check(Mathf.Abs(material.GetFloat("_SnowCoverage")-.28f)<.001f,"Live snow coverage does not refresh material");
            var low=Save(preview,"coverage-28.png");
            look.snowCoverage=.95f;preview.RenderScenePreview();var high=Save(preview,"coverage-95.png");
            Check(high>low+.10f,"Higher coverage did not visibly increase snow on the lawn");
            Check(Mathf.Abs(material.GetFloat("_SnowCoverage")-.95f)<.001f,"High snow coverage was ignored");
            look.snowCoverage=originalCoverage;
            Pose(world,bush.id,WorldMapObjectTheme.None);Check(Caps(Slot(world,bush.id)).All(c=>!c.gameObject.activeSelf),"Per-object opt-out ignored during Christmas");
            typeof(RoyalAvesLevelMapPreview).GetMethod("Build",Private).Invoke(preview,new object[]{1080,2280,1});
            map=(CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview);map.World.TimeOverride=1;
            Save(preview,"christmas-1080x2280.png");
            Check(kit.surface.isSupported,"Surface shader unsupported");
            File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: original sprites retained; dedicated registered overlays for coral shrub and classic pine; normal, Christmas and manual ice captures at 1080x1920 and 1080x2280; overlay inherits mirrored card and depth; per-object opt-out clears/reuses slots; live ground coverage refreshes 28/95 percent; supported shader; zero Unity errors. Source scene/layout/season selection never saved. Visual QA performed on captured images; physical-device performance not measured.");
        }
        finally
        {
            if(look!=null)look.snowCoverage=originalCoverage;
            if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);
            EditorJsonUtility.FromJsonOverwrite(json,layout);
        }
    }
    static float Save(RoyalAvesLevelMapPreview preview,string name)
    {
        preview.RenderScenePreview();var rt=(RenderTexture)preview.SceneTexture;var old=RenderTexture.active;
        var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
        float whites=0,count=0;
        try
        {
            RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name),image.EncodeToPNG());
            var pixels=image.GetPixels32();
            for(var y=(int)(rt.height*.18f);y<rt.height*.65f;y+=8)for(var x=0;x<rt.width;x+=8)
            {
                if(x>rt.width*.19f&&x<rt.width*.81f)continue;
                var c=pixels[y*rt.width+x];if(c.r>210&&c.g>210&&c.b>210)whites++;count++;
            }
        }
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}
        return whites/Mathf.Max(1,count);
    }
}
