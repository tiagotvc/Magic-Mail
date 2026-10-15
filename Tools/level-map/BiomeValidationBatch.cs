using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using RoyalAves.EditorTools;
public static class BiomeValidationBatch
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/polish";
    static void Check(bool b,string why){if(!b)throw new Exception(why);}
    public static void Execute()
    {
        Directory.CreateDirectory(Output);
        var source=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);
        var kit=Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath);
        kit=RoyalAvesWorldMapSetup.Ensure(true);
        foreach(var file in new[]{"GreenLand/garden-vivid-v4","ForestLand/forest-postal-v4","CoastLand/coast-postal-v4","AuroraLand/aurora-postal-v4","LavaLand/thermal-geyser-v4"})
            Debug.Log("BIOME ASSETS "+file+": "+string.Join(", ",AssetDatabase.LoadAllAssetsAtPath("Assets/RoyalAves/Art/MapKitV3/"+file+".png").Select(x=>x.name+" ("+x.GetType().Name+")")));
        var art=Resources.Load<LobbyFeaturesConfig>("RoyalAvesFeatures");
        Check(source.islands.Count==5&&source.Capacity==100,"Expected five equal islands for 100 phases");
        Check(source.islands.All(x=>!x.volcanoes),"Volcano backdrop was not removed");
        for(var i=0;i<5;i++)Check(source.FirstLevel(i)==i*20+1,"Unequal island spacing");
        foreach(var f in new[]{kit.gardenFamily,kit.forestFamily,kit.coastFamily,kit.auroraFamily})Check(f.Length==6&&f.All(s=>s!=null),"Biome family missing");
        Check(kit.geyserFrames.Length==4&&kit.geyserFrames.All(x=>x!=null),"Geyser animation missing");
        var bounds=kit.geyserFrames.Select(WorldMapCards.VisibleBounds).ToArray();
        Check(bounds.All(x=>Mathf.Abs(x.height-bounds[0].height)<.001f&&Mathf.Abs(x.width-bounds[0].width)<.001f),"Geyser animation frame bounds drift");
        var config=UnityEngine.Object.Instantiate(source);var clicks=0;var drags=0;
        foreach(var curvature in new[]{.6f,1.45f,2.2f})
        {
            config.lateralCurvature=curvature;
            var view=WorldMapView.Create(null,art,kit,100,0,true,config);
            try
            {
                view.TimeOverride=1;view.SetFixedSize(540,1200);
                foreach(var level in Enumerable.Range(1,100))
                {
                    var cursor=view.CursorForLevel(level);view.Tick(cursor,level,false);
                    Check(view.ProjectLevel(level,cursor,.45f,out var uv)&&uv.y>0&&uv.y<1,"Focused level outside viewport");
                    Check(view.PickViewport(uv)==level,"Picking failed at curvature "+curvature+" level "+level);clicks++;
                }
                view.Tick(3,3,false);
                for(var i=0;i<view.Props.Count&&drags<(curvature==.6f?8:curvature==1.45f?16:24);i++)
                    if(view.ProjectPropAnchor(i,.45f,out var uv)&&uv.x>.03f&&uv.x<.97f&&uv.y>.2f&&uv.y<.8f)
                    {
                        Check(view.GroundAtViewport(uv,3,.45f,out var phase,out var side),"Curved ground picker missed");
                        Check(Mathf.Abs(phase-view.Route.LevelAt(view.Props[i].s))<.035f&&Mathf.Abs(side-view.Props[i].offset)<.05f,"Curved drag anchor drift");drags++;
                    }
                foreach(var x in new[]{-2f,2f})
                {
                    var n=MapCurve.Normal(0,kit.radius,x,curvature);var up=MapCurve.Orientation(0,kit.radius,x,curvature)*Vector3.up;
                    Check(Vector3.Dot(n,up)>.9999f&&Mathf.Sign(n.x)==Mathf.Sign(x),"Assets do not follow the correct side angle");
                }
                Check(view.Cam.allowHDR&&view.Cam.GetUniversalAdditionalCameraData().renderPostProcessing,"Map post-processing is off");
                view.Tick(85,85,false);view.RenderNow();
                var previous=RenderTexture.active;RenderTexture.active=(RenderTexture)view.Target;
                var tex=new Texture2D(540,1200,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,540,1200),0,0);tex.Apply();
                Check(tex.GetPixel(270,1190).a<.02f,"Post processing made the sky opaque");UnityEngine.Object.DestroyImmediate(tex);RenderTexture.active=previous;
                var geysers=view.Props.Where(x=>x.kind==PropKind.Geyser).ToArray();Check(geysers.Length>=3,"Lava has no geysers");
                var pose=typeof(WorldMapView).GetMethod("PoseSprite",BindingFlags.Instance|BindingFlags.NonPublic);var frames=new HashSet<Sprite>();
                for(var time=0f;time<6;time+=.2f){view.TimeOverride=time;frames.Add((Sprite)pose.Invoke(view,new object[]{geysers[0]}));}
                Check(frames.Count==4,"Geyser does not animate through its four frames");
            }
            finally{view.Dispose();}
        }
        config.lateralCurvature=1.73f;const string saved="Assets/CurvatureRoundtrip.asset";AssetDatabase.CreateAsset(config,saved);AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(saved,ImportAssetOptions.ForceUpdate);Check(Mathf.Abs(AssetDatabase.LoadAssetAtPath<WorldMapIslands>(saved).lateralCurvature-1.73f)<.0001f,"Curvature did not persist");AssetDatabase.DeleteAsset(saved);
        var extraLayout=ScriptableObject.CreateInstance<WorldMapLayout>();var extra=new WorldMapLayout.Entry{sourceId="thermal-test"};
        extraLayout.chapters.Add(new WorldMapLayout.Chapter{manual=true});extraLayout.chapters[0].entries.Add(extra);
        Check(extraLayout.ExtraClaimed("thermal-test"),"Edited geyser would duplicate");extraLayout.chapters[0].entries.Clear();extraLayout.hiddenExtras.Add("thermal-test");
        Check(extraLayout.ExtraClaimed("thermal-test"),"Removed geyser would return");UnityEngine.Object.DestroyImmediate(extraLayout);
        var root=new GameObject("AtmosphereTest",typeof(RectTransform),typeof(Canvas));var skyParent=new GameObject("Sky",typeof(RectTransform));skyParent.transform.SetParent(root.transform,false);
        var back=new GameObject("Back",typeof(RectTransform),typeof(Image));back.transform.SetParent(skyParent.transform,false);
        try
        {
            ((RectTransform)skyParent.transform).sizeDelta=new Vector2(720,1600);
            var route=new WorldMapRoute(source,100);var weather=WorldMapAtmosphere.Create((RectTransform)root.transform,(RectTransform)skyParent.transform,back.GetComponent<Image>(),kit,route);
            float Focus(int level)=>route.LevelAt(route.Pos(level)-kit.focusAhead);
            weather.Tick(Focus(1),1);weather.Tick(Focus(21),2);Check(weather.TransitionProgress<.01f,"Sky transition did not begin");
            weather.Tick(Focus(21),2.4f);Check(weather.TransitionProgress>.4f&&weather.TransitionProgress<.6f,"Sky transition does not interpolate");
            weather.Tick(Focus(21),2.9f);Check(weather.TransitionProgress>.99f,"Sky transition did not finish");
            weather.Tick(Focus(81),3);weather.Tick(Focus(81),4);
            Check(root.GetComponentsInChildren<WorldMapWeatherGraphic>().Any(g=>g.mode==8),"Lava meteors are missing");
            Check(!root.GetComponentsInChildren<Transform>().Any(t=>t.name.Contains("Vulcao")),"Volcano objects remain in the horizon");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
        var scenes=new[]{("garden",3f,1),("forest",25f,21),("coast",45f,41),("aurora",65f,61),("lava",85f,81)};
        foreach(var scene in scenes)Capture(new RoyalAvesWorldMapCapture.Request{outDir=Output,prefix="biome-"+scene.Item1,cursors=new[]{scene.Item2},nextLevel=scene.Item3,width=720,height=1600,ui=true,audit=false,time=1});
        for(var i=0;i<4;i++)Capture(new RoyalAvesWorldMapCapture.Request{outDir=Output,prefix="biome-geyser-"+i,cursors=new[]{90f},nextLevel=88,width=540,height=1200,ui=true,audit=false,time=i*1.4f});
        File.WriteAllText(Path.Combine(Output,"biome-validation.txt"),$"PASS: five equal 20-phase islands, four complete six-sprite families, four geyser frames with stable bounds, {clicks} focused phase clicks at three curvature values, {drags} drag anchors, correct left/right asset normals, curvature save/reload, map-only HDR/post processing with transparent sky, animated geysers, sliding sky transitions and lava meteors; no volcano backdrop. Real Unity UI captures saved for all five areas. Manual layout preserved.");
    }
    static void Capture(RoyalAvesWorldMapCapture.Request r)
    {
        typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{r});
        var result=File.ReadAllText("Temp/worldmap-capture.done");Check(result.StartsWith("OK"),result);
    }
    public static void Motion()
    {
        var kit=RoyalAvesWorldMapSetup.Ensure(false);var art=Resources.Load<LobbyFeaturesConfig>("RoyalAvesFeatures");var islands=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);
        typeof(IslandValidationBatch).GetMethod("VerifyPreviewParity",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{kit,art,islands,100});
        var times=new[]{0f,2.06f,2.66f,3.56f};
        for(var i=0;i<4;i++)Capture(new RoyalAvesWorldMapCapture.Request{outDir=Output,prefix="biome-geyser-"+i,cursors=new[]{90f},nextLevel=88,width=540,height=1200,ui=true,audit=false,time=times[i]});
        var window=ScriptableObject.CreateInstance<RoyalAvesLevelMapPreview>();var type=typeof(RoyalAvesLevelMapPreview);const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        try
        {
            type.GetField("level",flags).SetValue(window,19.5f);type.GetMethod("Build",flags).Invoke(window,new object[]{540,1200,21});
            var map=(CurvedLevelMap)type.GetField("map",flags).GetValue(window);map.World.TimeOverride=0;
            type.GetMethod("Render",flags).Invoke(window,null);
            type.GetField("level",flags).SetValue(window,23f);
            for(var i=0;i<4;i++)
            {
                map.World.TimeOverride=1+i*.27f;
                type.GetMethod("Save",flags).Invoke(window,new object[]{Path.Combine(Output,"biome-transition-"+i+".png"),false});
            }
        }
        finally{type.GetMethod("Cleanup",flags).Invoke(window,null);UnityEngine.Object.DestroyImmediate(window);}
    }
    public static void EffectsCheck()
    {
        var kit=RoyalAvesWorldMapSetup.Ensure(false);var art=Resources.Load<LobbyFeaturesConfig>("RoyalAvesFeatures");
        var view=WorldMapView.Create(null,art,kit,100,0,true);
        try
        {
            view.SetFixedSize(540,1200);view.TimeOverride=1;view.Tick(85,85,false);view.RenderNow();
            var bloom=VolumeManager.instance.stack.GetComponent<Bloom>().intensity.value;
            var saturation=VolumeManager.instance.stack.GetComponent<ColorAdjustments>().saturation.value;
            Debug.Log("EFFECTS STACK before sync: bloom="+bloom+" saturation="+saturation);
            Physics.SyncTransforms();view.RenderNow();
            var after=VolumeManager.instance.stack.GetComponent<Bloom>().intensity.value;
            Debug.Log("EFFECTS STACK after sync: bloom="+after);
            Check(bloom>.17f&&saturation>6,"Map camera is not receiving the configured post-processing Volume");
            File.WriteAllText(Path.Combine(Output,"biome-effects.txt"),$"PASS: rendered camera stack uses bloom {bloom:0.00} and saturation {saturation:0.0}; scoped Volume is active without an external physics step.");
        }
        finally{view.Dispose();}
    }
}
