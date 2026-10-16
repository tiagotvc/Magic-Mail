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

public static class WinterV35Validation
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/winter-v35";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int errors;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static WorldMapEffectPose Settings(float angle,Vector2 offset,Vector2 scale)=>new WorldMapEffectPose{rotation=angle,offset=offset,scale=scale};
    static object Slot(WorldMapView world,string id)
    {
        var index=Enumerable.Range(0,world.Props.Count).Single(i=>world.Props[i].id==id);
        return ((IDictionary)typeof(WorldMapView).GetField("activeSlots",Private).GetValue(world))[index];
    }
    static Transform Card(object slot)=>(Transform)slot.GetType().GetField("card").GetValue(slot);
    static void Same(WorldMapEffectPose a,WorldMapEffectPose b,string message)=>Check(a.visibility==b.visibility&&a.offset==b.offset&&a.scale==b.scale&&a.rotation==b.rotation,message);
    static void TransformMatches(Transform t,WorldMapEffectPose pose,float height,Vector2 pivot)
    {
        Check(Vector2.Distance(t.localPosition,(pivot+pose.offset)*height)<.001f,"Effect position lost");
        Check(Mathf.Abs(Mathf.DeltaAngle(t.localEulerAngles.z,pose.rotation))<.01f,"Effect rotation lost");
        Check(Vector2.Distance(t.localScale,pose.EffectiveScale)<.001f,"Effect scale lost");
    }
    public static void Run()
    {
        Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        try
        {
            Directory.CreateDirectory(Output);RoundtripAndRuntime();CaptureSavedMap(1080,1920);CaptureSavedMap(1080,2280);
            Check(errors==0,"Unity logged errors");
            File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: per-object snow/light/ice position, rotation, nonuniform scale and visibility survive asset save/reload and Entry/Placement conversion; legacy zero scales resolve to unit; independent scene transforms and unchanged original card anchors; snow-only contour overlay and registered painted overlay remain separate from original art; enabling lights on a shrub works; reset and theme opt-out clear pooled transforms/effects; visible bulb blink range and animated sharp ice shader bindings checked; native saved-layout captures at 1080x1920 and 1080x2280; zero Unity errors. Source scene/layouts and active season were never saved by tests. Physical-device performance and manual mouse interaction were not measured.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void RoundtripAndRuntime()
    {
        var kit=Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath);
        var layout=Resources.Load<WorldMapLayout>(WorldMapLayout.SimpleResourcePath);var original=EditorJsonUtility.ToJson(layout);
        var chapter=layout.Find(0);if(chapter==null){chapter=new WorldMapLayout.Chapter{index=0};layout.chapters.Add(chapter);}
        var tree=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/Arvores/green-rounded-tree-v27.png");
        var shrub=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/Props/shrub-coral-v15.png");
        var entry=new WorldMapLayout.Entry{id="edited-tree",sprite=tree,keepSprite=true,customSize=true,height=1.4f,kind=PropKind.Tree,
            groundPinned=true,level=3,lateral=-1.6f,theme=WorldMapObjectTheme.Ice,
            snowEffect=Settings(17,new Vector2(.08f,.03f),new Vector2(.8f,1.25f)),
            lightsEffect=Settings(-12,new Vector2(-.05f,.06f),new Vector2(1.2f,.8f)),
            iceEffect=Settings(23,new Vector2(.06f,.04f),new Vector2(.7f,1.3f))};
        var other=new WorldMapLayout.Entry{id="default-tree",sprite=tree,keepSprite=true,customSize=true,height=1.4f,kind=PropKind.Tree,
            groundPinned=true,level=4,lateral=1.6f,theme=WorldMapObjectTheme.Ice};
        var bush=new WorldMapLayout.Entry{id="painted-shrub",sprite=shrub,keepSprite=true,customSize=true,height=.65f,kind=PropKind.Small,
            groundPinned=true,level=5,lateral=-1.5f,theme=WorldMapObjectTheme.Ice,snowEffect=Settings(-9,new Vector2(.03f,.02f),new Vector2(.9f,1.1f))};
        chapter.manual=true;chapter.entries.Clear();chapter.entries.AddRange(new[]{entry,other,bush});
        RoyalAvesLevelMapPreview preview=null;const string fixture="Assets/WinterV35Roundtrip.asset";
        try
        {
            var copy=UnityEngine.Object.Instantiate(layout);AssetDatabase.CreateAsset(copy,fixture);AssetDatabase.SaveAssetIfDirty(copy);AssetDatabase.ImportAsset(fixture,ImportAssetOptions.ForceUpdate);
            var saved=AssetDatabase.LoadAssetAtPath<WorldMapLayout>(fixture).Find(0).entries[0];
            Same(saved.snowEffect,entry.snowEffect,"Snow settings did not persist");Same(saved.lightsEffect,entry.lightsEffect,"Light settings did not persist");Same(saved.iceEffect,entry.iceEffect,"Ice settings did not persist");
            var route=new WorldMapRoute(Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath),100);
            var from=WorldMapLayout.Entry.From(saved.Placement(route,kit),route);
            Same(from.snowEffect,entry.snowEffect,"Snow Entry conversion lost settings");Same(from.lightsEffect,entry.lightsEffect,"Light Entry conversion lost settings");Same(from.iceEffect,entry.iceEffect,"Ice Entry conversion lost settings");
            Check(default(WorldMapEffectPose).EffectiveScale==Vector2.one,"Legacy data changed original dimensions");
            AssetDatabase.DeleteAsset(fixture);
            preview=RoyalAvesLevelMapPreview.CreateScenePreview(1080,1920);var map=Map(preview);var world=map.World;world.TimeOverride=1;preview.RenderScenePreview();
            var card=Card(Slot(world,entry.id));var anchor=card.parent.localPosition;
            TransformMatches(card.Find("NeveDoObjeto"),entry.snowEffect,entry.height,new Vector2(0,.55f));
            TransformMatches(card.Find("NatalLuzesEGelo/LuzesDoObjeto"),entry.lightsEffect,entry.height,new Vector2(0,.6f));
            TransformMatches(card.Find("NatalLuzesEGelo/GeloDoObjeto"),entry.iceEffect,entry.height,new Vector2(0,.28f));
            Check(card.parent.localPosition==anchor,"Effects moved the original object anchor");
            TransformMatches(Card(Slot(world,other.id)).Find("NeveDoObjeto"),default,other.height,new Vector2(0,.55f));
            var renderer=card.Find("NeveDoObjeto").GetComponentInChildren<MeshRenderer>();var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
            Check(block.GetFloat("_SnowOnly")==1,"Contour snow cannot be moved independently from the original sprite");
            var painted=Card(Slot(world,bush.id)).Find("NeveDoObjeto");TransformMatches(painted,bush.snowEffect,bush.height,new Vector2(0,.55f));
            Check(painted.GetComponentInChildren<MeshFilter>().sharedMesh.name.StartsWith("RegisteredSnow_"),"Painted shrub overlay was replaced");
            var bulb=card.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.StartsWith("LuzDeNatal_"));float low=1,high=0;
            for(int i=0;i<12;i++){world.TimeOverride=i*.14f;preview.RenderScenePreview();bulb.GetPropertyBlock(block);float b=block.GetColor("_Tint").r;low=Mathf.Min(low,b);high=Mathf.Max(high,b);}
            Check(high-low>.6f,"Bulbs do not visibly blink");
            var ice=card.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.StartsWith("GeloGalhoBaixo_"));ice.GetPropertyBlock(block);
            Check(block.GetFloat("_IceGlint")==1&&Mathf.Abs(block.GetFloat("_IceTime")-world.Clock)<.001f,"Animated sharp ice highlights missing");
            Save(preview,"independent-effects.png");
            bush.lightsEffect.visibility=WorldMapEffectVisibility.Show;world.ReloadScenery();preview.RenderScenePreview();
            Check(Card(Slot(world,bush.id)).Find("NatalLuzesEGelo/LuzesDoObjeto").gameObject.activeInHierarchy,"Explicit light toggle on a shrub does nothing");
            entry.snowEffect=entry.lightsEffect=entry.iceEffect=default;world.ReloadScenery();preview.RenderScenePreview();card=Card(Slot(world,entry.id));
            TransformMatches(card.Find("NeveDoObjeto"),default,entry.height,new Vector2(0,.55f));
            TransformMatches(card.Find("NatalLuzesEGelo/LuzesDoObjeto"),default,entry.height,new Vector2(0,.6f));
            entry.snowEffect.visibility=entry.lightsEffect.visibility=entry.iceEffect.visibility=WorldMapEffectVisibility.Hide;
            world.ReloadScenery();preview.RenderScenePreview();card=Card(Slot(world,entry.id));
            Check(!card.Find("NeveDoObjeto").gameObject.activeInHierarchy&&!card.Find("NatalLuzesEGelo/LuzesDoObjeto").gameObject.activeInHierarchy&&!card.Find("NatalLuzesEGelo/GeloDoObjeto").gameObject.activeInHierarchy,"Independent visibility toggles leave an effect visible");
            entry.theme=WorldMapObjectTheme.None;world.ReloadScenery();preview.RenderScenePreview();
            Check(!Card(Slot(world,entry.id)).Find("NatalLuzesEGelo").gameObject.activeSelf,"Theme opt-out leaves holiday decorations visible");
        }
        finally{if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);if(AssetDatabase.LoadAssetAtPath<WorldMapLayout>(fixture)!=null)AssetDatabase.DeleteAsset(fixture);EditorJsonUtility.FromJsonOverwrite(original,layout);}
    }
    static CurvedLevelMap Map(RoyalAvesLevelMapPreview preview)=>(CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview);
    static void CaptureSavedMap(int width,int height)
    {
        var preview=RoyalAvesLevelMapPreview.CreateScenePreview(width,height);
        try
        {
            typeof(RoyalAvesLevelMapPreview).GetField("seasonPreview",Private).SetValue(preview,2);
            typeof(RoyalAvesLevelMapPreview).GetMethod("Build",Private).Invoke(preview,new object[]{width,height,1});
            Map(preview).World.TimeOverride=2.3f;Save(preview,"map-"+width+"x"+height+".png");
        }
        finally{UnityEngine.Object.DestroyImmediate(preview);}
    }
    static void Save(RoyalAvesLevelMapPreview preview,string name)
    {
        preview.RenderScenePreview();var rt=(RenderTexture)preview.SceneTexture;var old=RenderTexture.active;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name),image.EncodeToPNG());}
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}
    }
}
