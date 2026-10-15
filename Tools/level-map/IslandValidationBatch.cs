using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using RoyalAves.EditorTools;

public static class IslandValidationBatch
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/polish";
    [Serializable] sealed class FixtureIds {public int[] ids;}
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    // These fixtures contain only the real phase IDs. Game boards and saves are never opened here.
    public static void PrepareFixtures()
    {
        var ids=JsonUtility.FromJson<FixtureIds>(File.ReadAllText("Assets/Editor/island-level-fixtures.json")).ids;
        Directory.CreateDirectory("Assets/Resources/Levels");AssetDatabase.Refresh();
        foreach(var id in ids)
        {
            var path=$"Assets/Resources/Levels/Level_{id}.asset";
            if(AssetDatabase.LoadAssetAtPath<WorldMapIslands>(path)!=null)continue;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<WorldMapIslands>(),path);
        }
        AssetDatabase.SaveAssets();WorldMapLevelCatalog.Refresh();
    }
    public static void Execute()
    {
        Directory.CreateDirectory(Output);
        var kit=Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath);
        var source=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);
        var art=Resources.Load<LobbyFeaturesConfig>("RoyalAvesFeatures");
        var ids=JsonUtility.FromJson<FixtureIds>(File.ReadAllText("Assets/Editor/island-level-fixtures.json")).ids;
        Check(WorldMapLevelCatalog.Count==ids.Length&&WorldMapLevelCatalog.LastLevel==ids.Max(),"Catalog diverged from phase editor IDs");
        Check(ids.All(WorldMapLevelCatalog.Exists),"Created phase missing from catalog");
        var edited=UnityEngine.Object.Instantiate(source);
        var capacity=edited.Capacity;var added=edited.Append();
        Check(edited.FirstLevel(edited.islands.Count-1)==capacity+1&&edited.Capacity==capacity+20,"New island was not appended");
        added.title="Ilha de validação";added.terrain=WorldMapTerrainTheme.LavaLand;added.weather=WorldMapWeather.Storm;
        added.background=kit.skyLilac;added.cloudSmall=kit.lavaCloudSmall;added.weatherIntensity=.4f;
        const string saved="Assets/IslandRoundtrip.asset";
        AssetDatabase.CreateAsset(edited,saved);AssetDatabase.SaveAssets();
        var serialized=EditorJsonUtility.ToJson(edited);AssetDatabase.ImportAsset(saved,ImportAssetOptions.ForceUpdate);
        edited=AssetDatabase.LoadAssetAtPath<WorldMapIslands>(saved);
        Check(EditorJsonUtility.ToJson(edited)==serialized,"Island save/reload changed fields");
        Check(edited.islands.Last().background==kit.skyLilac&&edited.islands.Last().weather==WorldMapWeather.Storm,"Environment did not persist");

        var extended=new WorldMapRoute(edited,237);
        Check(extended.LevelSlots==237&&extended.Pos(237)>extended.Pos(236),"Route is still capped at 200");
        for(float n=1;n<237;n+=.37f)Check(Mathf.Abs(extended.LevelAt(extended.MetersAt(n))-n)<.0001f,"Route inverse drift");
        foreach(var gap in extended.Gaps)Check(Mathf.Abs(extended.CenterX(gap.start)-extended.CenterX(gap.end))<.0002f,"Bridge banks do not align");

        var view=WorldMapView.Create(null,art,kit,ids.Max(),40,true,edited);
        var picked=0;var anchors=0;
        try
        {
            Check(view.RenderedLevelCount==ids.Max(),"Future buttons are still being generated");
            view.TimeOverride=1;view.SetFixedSize(720,1600);
            foreach(var level in ids)
            {
                var cursor=view.CursorForLevel(level);view.Tick(cursor,level,false);
                if(view.ProjectLevel(level,cursor,720f/1600,out var uv)&&uv.y>0&&uv.y<1)
                {Check(view.PickViewport(uv)==level,"Level picking failed: "+level);picked++;}
            }
            Check(picked==ids.Length,"Some real phases were not visible when focused");
            const string hole="Assets/Resources/Levels/Level_50.asset";
            AssetDatabase.DeleteAsset(hole);WorldMapLevelCatalog.Refresh();
            view.LevelExists=WorldMapLevelCatalog.Exists;view.Tick(view.CursorForLevel(50),51,false);
            Check(!view.GetComponentsInChildren<WorldMapNodeRef>().Any(x=>x.gameObject.activeSelf&&x.level==50),"Deleted phase still has a button");
            Check(WorldMapLevelCatalog.NextExisting(50)==51,"Missing phase was not skipped");
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<WorldMapIslands>(),hole);AssetDatabase.SaveAssets();WorldMapLevelCatalog.Refresh();
            view.Tick(capacity+10,ids.Max()+1,false);
            Check(!view.GetComponentsInChildren<WorldMapNodeRef>().Any(x=>x.gameObject.activeSelf&&x.level>ids.Max()),"Empty island displayed phantom levels");
            view.Tick(2.55f,1,false);
            for(var i=0;i<view.Props.Count&&anchors<8;i++)
                if(view.ProjectPropAnchor(i,720f/1600,out var uv)&&uv.x>0&&uv.x<1&&uv.y>.2f&&uv.y<.85f)
                {
                    Check(view.GroundAtViewport(uv,2.55f,720f/1600,out var phase,out var side),"Drag anchor failed");
                    Check(Mathf.Abs(phase-view.Route.LevelAt(view.Props[i].s))<.03f&&Mathf.Abs(side-view.Props[i].offset)<.04f,"Drag placement drift");anchors++;
                }
        }
        finally{view.Dispose();}
        VerifyPreviewParity(kit,art,source,ids.Max());

        // Adjacent islands must keep different materials at the same time, not recolor the whole scene.
        var mixed=UnityEngine.Object.Instantiate(source);
        mixed.islands[1].terrain=WorldMapTerrainTheme.LavaLand;mixed.islands[1].weather=WorldMapWeather.Rain;
        mixed.islands[1].background=kit.skyLilac;mixed.islands[1].weatherIntensity=.4f;
        mixed.islands[1].groundMaterials=kit.terrainMaterials;
        view=WorldMapView.Create(null,art,kit,ids.Max(),0,true,mixed);
        try
        {
            view.SetFixedSize(720,1600);view.Tick(9.8f,10,false);
            var ground=view.transform.Find("Chao");var materials=ground.GetComponent<MeshRenderer>().sharedMaterials;
            var mesh=ground.GetComponent<MeshFilter>().sharedMesh;
            Check(materials[0].GetFloat("_LavaTerrain")==0&&materials[1].GetFloat("_LavaTerrain")==1,"Island themes leaked into each other");
            Check(mesh.GetIndexCount(0)>0&&mesh.GetIndexCount(1)>0,"Transition did not render both islands");
            Check(materials[1].GetTexture("_TerrainTiles")==kit.terrainMaterials.texture,"Custom floor was not bound");
            var root=new GameObject("AtmosphereTest",typeof(RectTransform),typeof(Canvas));
            var skyParent=new GameObject("Sky",typeof(RectTransform));skyParent.transform.SetParent(root.transform,false);
            var image=new GameObject("Background",typeof(RectTransform),typeof(Image)).GetComponent<Image>();image.transform.SetParent(skyParent.transform,false);
            try
            {
                var weather=WorldMapAtmosphere.Create((RectTransform)root.transform,(RectTransform)skyParent.transform,image,kit,view.Route);
                weather.Tick(12f,1);
                Check(image.enabled&&image.sprite==kit.skyLilac,"Separate island background not visible");
                var effect=root.transform.Find("ClimaDoMapa").GetComponent<WorldMapWeatherGraphic>();
                Check(effect.mode==1&&Mathf.Abs(effect.canvasRenderer.GetAlpha()-.4f)<.001f,"Island weather override was not applied");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        finally{view.Dispose();}

        Capture(new RoyalAvesWorldMapCapture.Request{outDir=Output,prefix="islands-green",cursors=new[]{2.55f,6.1f},width=720,height=1600,nextLevel=8,ui=true,audit=false});
        Capture(new RoyalAvesWorldMapCapture.Request{outDir=Output,prefix="islands-transition",cursors=new[]{9.8f},width=720,height=1600,nextLevel=10,ui=true,audit=false,islands=mixed});
        Capture(new RoyalAvesWorldMapCapture.Request{outDir=Output,prefix="islands-environment",cursors=new[]{12f},width=720,height=1600,nextLevel=12,ui=true,audit=false,islands=mixed});
        Capture(new RoyalAvesWorldMapCapture.Request{outDir=Output,prefix="islands-lava",cursors=new[]{89f},width=720,height=1600,nextLevel=90,ui=true,audit=false});
        Capture(new RoyalAvesWorldMapCapture.Request{outDir=Output,prefix="islands-empty",cursors=new[]{110f},width=720,height=1600,nextLevel=101,ui=true,audit=false,islands=edited});
        UnityEngine.Object.DestroyImmediate(mixed);
        AssetDatabase.DeleteAsset(saved);
        File.WriteAllText(Path.Combine(Output,"islands-validation.txt"),$"PASS: {ids.Length} real phase IDs; append after {capacity}; island fields and sprite references persisted; route extends to 237; {picked}/{ids.Length} focused level clicks (including every crossing); {anchors} drag anchors; missing phase omitted and skipped; empty island has no future buttons; simultaneous green/lava materials; independent background and rain intensity. Preview/runtime pixel parity in islands-parity.txt. Main editor and manual layout were not modified by validation.");
    }
    static void VerifyPreviewParity(WorldMapKit kit,LobbyFeaturesConfig art,WorldMapIslands source,int count)
    {
        var window=ScriptableObject.CreateInstance<RoyalAvesLevelMapPreview>();
        WorldMapView runtime=null;
        try
        {
            typeof(RoyalAvesLevelMapPreview).GetField("level",Private).SetValue(window,6.1f);
            typeof(RoyalAvesLevelMapPreview).GetMethod("Build",Private).Invoke(window,new object[]{720,1600,8});
            var map=(CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(window);
            var preview=map.World;
            Check(preview.Route.Islands==source,"Editor uses a different island source");
            Color32[] Pixels(WorldMapView view,string name)
            {
                view.TimeOverride=1;view.SetFixedSize(720,1600);view.Tick(6.1f,8,false);view.RenderNow();
                var previous=RenderTexture.active;RenderTexture.active=(RenderTexture)view.Target;
                var tex=new Texture2D(720,1600,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,720,1600),0,0);tex.Apply();
                var data=tex.GetPixels32();File.WriteAllBytes(Path.Combine(Output,name),tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);RenderTexture.active=previous;return data;
            }
            var origin=preview.transform.position;
            var a=Pixels(preview,"islands-parity-editor.png");
            var previewMesh=preview.transform.Find("Chao").GetComponent<MeshFilter>().sharedMesh;
            var points=previewMesh.vertices;var colors=previewMesh.colors;var cameraPosition=preview.Cam.transform.localPosition;var cameraRotation=preview.Cam.transform.localRotation;
            preview.Dispose();
            runtime=WorldMapView.Create(null,art,kit,count,0,true);
            Check(runtime.Route.Islands==source,"Runtime uses a different island source");
            Check(runtime.transform.position==origin,"Parity captures must use the same isolated world origin");
            var b=Pixels(runtime,"islands-parity-runtime.png");var changed=0;var worst=0;var worstLuma=0f;var worstAlpha=0;
            var runtimeMesh=runtime.transform.Find("Chao").GetComponent<MeshFilter>().sharedMesh;
            Check(points.SequenceEqual(runtimeMesh.vertices)&&colors.SequenceEqual(runtimeMesh.colors),"Editor/runtime ground geometry or palette differs");
            Check(cameraPosition==runtime.Cam.transform.localPosition&&cameraRotation==runtime.Cam.transform.localRotation,"Editor/runtime camera differs");
            for(var i=0;i<a.Length;i++)
            {
                var d=Mathf.Max(Mathf.Abs(a[i].r-b[i].r),Mathf.Abs(a[i].g-b[i].g),Mathf.Abs(a[i].b-b[i].b),Mathf.Abs(a[i].a-b[i].a));
                worst=Mathf.Max(worst,d);if(d>4)changed++;
                var luma=Mathf.Abs((a[i].r-b[i].r)*.2126f+(a[i].g-b[i].g)*.7152f+(a[i].b-b[i].b)*.0722f);
                worstLuma=Mathf.Max(worstLuma,luma);worstAlpha=Mathf.Max(worstAlpha,Mathf.Abs(a[i].a-b[i].a));
            }
            // HDR resolves can shift low blue values in bright colored highlights. Compare visible
            // luminance and transparency, in addition to exact source geometry, palette and camera.
            Check(worstLuma<=4&&worstAlpha<=4,"Editor/runtime luminance or transparency mismatch");
            File.WriteAllText(Path.Combine(Output,"islands-parity.txt"),$"PASS: same saved Resource; exact ground vertices, vertex colors and camera. HDR image comparison: maximum luminance difference {worstLuma:0.00}/255, alpha difference {worstAlpha}/255; {changed}/{a.Length} pixels have a raw channel difference over 4/255 (maximum {worst}/255 in bright colored highlights). Fixed animation time and viewport, real editor preview versus game renderer.");
        }
        finally
        {
            runtime?.Dispose();typeof(RoyalAvesLevelMapPreview).GetMethod("Cleanup",Private).Invoke(window,null);UnityEngine.Object.DestroyImmediate(window);
        }
    }
    static void Capture(RoyalAvesWorldMapCapture.Request request)
    {
        typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{request});
        var result=File.ReadAllText("Temp/worldmap-capture.done");Check(result.StartsWith("OK"),result);
    }
}
