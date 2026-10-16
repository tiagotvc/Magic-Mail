using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using RoyalAves.EditorTools;
public static class SimpleV6ValidationBatch
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/v6";
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void SkyCheck()
    {
        Directory.CreateDirectory(Output);var source=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);var islands=UnityEngine.Object.Instantiate(source);
        var root=new GameObject("SkyCheck",typeof(RectTransform),typeof(Canvas));
        var sky=new GameObject("Sky",typeof(RectTransform));sky.transform.SetParent(root.transform,false);((RectTransform)sky.transform).sizeDelta=new Vector2(720,1600);
        var back=new GameObject("Background",typeof(RectTransform),typeof(Image));back.transform.SetParent(sky.transform,false);
        try
        {
            var kit=RoyalAvesWorldMapSetup.Ensure(false);var route=new WorldMapRoute(islands,100);
            var a=WorldMapAtmosphere.Create((RectTransform)root.transform,(RectTransform)sky.transform,back.GetComponent<Image>(),kit,route);
            float Focus(int n)=>route.LevelAt(route.Pos(n)-kit.focusAhead);
            a.Tick(Focus(1),1);a.Tick(Focus(21),2);Check(a.TransitionProgress>.99f,"Identical sky moved between islands");
            a.Tick(Focus(41),3);islands.islands[1].skyTop=Color.magenta;a.Tick(Focus(21),4);
            Check(a.TransitionProgress<.01f,"Authored sky change did not transition");a.Tick(Focus(21),4.9f);Check(a.TransitionProgress>.99f,"Sky transition did not finish");
            File.WriteAllText(Path.Combine(Output,"sky-validation.txt"),"PASS: identical base sky stays still at island boundaries; authored sky changes retain their sliding transition. Final editor and runtime sources compile.");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(islands);}
    }
    public static void Execute()
    {
        Directory.CreateDirectory(Output);
        var source=RoyalAvesWorldMapSetup.Ensure(true);var islands=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);
        Check(islands.unifiedBase&&islands.islands.All(x=>x.terrain==WorldMapTerrainTheme.GreenLand&&!x.meteors&&x.weather==WorldMapWeather.Clear),"Base is not unified");
        Check(source.simpleNature.Length==6&&source.simplePostal.Length==6&&source.simpleNature.Concat(source.simplePostal).All(x=>x!=null),"Simple collection missing");
        Check(source.simpleBridge!=null&&source.simpleCrown!=null&&source.simpleStar!=null&&source.simpleHard!=null&&source.simpleExpress!=null,"Bridge or badges missing");
        Check(WorldMapLevelCatalog.Count==100,"Authored level catalogue changed");
        var allowed=source.simpleNature.Concat(source.simplePostal).ToArray();
        var view=WorldMapView.Create(null,Resources.Load<LobbyFeaturesConfig>("RoyalAvesFeatures"),source,100,0,true,islands);
        var clicks=0;var anchors=0;
        try
        {
            Check(view.CameraModel.Kit!=source&&view.CameraModel.Kit.simpleBase,"View did not isolate its presentation kit");
            Check(view.Props.All(x=>allowed.Contains(x.sprite)),"Legacy prop escaped reduced collection");
            Check(view.Props.Count(x=>view.Route.LevelAt(x.s)<21)<=20,"Opening composition too dense");
            Check(!view.Props.Any(x=>x.kind==PropKind.Geyser||x.kind==PropKind.FloatingMail||x.kind==PropKind.Dog),"Unrelated props in base");
            view.SetFixedSize(540,1200);view.TimeOverride=0;view.Tick(1.8f,5,false);
            var courier=view.GetComponentsInChildren<MeshFilter>().FirstOrDefault(x=>x.sharedMesh==WorldMapCards.Get(source.simplePostal[3],1.05f));
            Check(courier!=null,"Courier not visible");var foot=courier.transform.parent.localPosition;var scale=courier.transform.localScale;
            view.TimeOverride=1.25f;view.Tick(1.8f,5,false);
            Check(Mathf.Abs(courier.transform.parent.localPosition.x-foot.x)>.005f,"Character does not travel");
            Check(courier.transform.localScale!=scale,"Character does not pulse");
            var house=view.GetComponentsInChildren<MeshFilter>().First(x=>x.sharedMesh==WorldMapCards.Get(source.simplePostal[0],1.42f));
            var before=house.transform.localScale;view.TimeOverride=2.5f;view.Tick(1.8f,5,false);Check(house.transform.localScale!=before,"Building does not pulse");
            foreach(var n in new[]{1,5,19,20,21,39,40,41,60,61,80,81,99,100})
            {var cursor=view.CursorForLevel(n);view.Tick(cursor,n,false);Check(view.ProjectLevel(n,cursor,.45f,out var uv)&&view.PickViewport(uv)==n,"Pick failed "+n);clicks++;}
            view.Tick(5,5,false);
            for(var i=0;i<view.Props.Count&&anchors<8;i++)
                if(view.ProjectPropAnchor(i,.45f,out var uv)&&uv.x>.03f&&uv.x<.97f&&uv.y>.2f&&uv.y<.8f)
                {Check(view.GroundAtViewport(uv,5,.45f,out var phase,out var side)&&Mathf.Abs(phase-view.Route.LevelAt(view.Props[i].s))<.035f&&Mathf.Abs(side-view.Props[i].offset)<.05f,"Drag anchor drift");anchors++;}
            view.Tick(20.8f,21,false);view.RenderNow();
            var water=view.GetComponentsInChildren<MeshFilter>().First(x=>x.name=="Agua").sharedMesh.colors;
            Check(water.Any(x=>x.a<.05f)&&water.Any(x=>x.a>.9f),"Water shore gradient missing");
            var ground=view.GetComponentsInChildren<MeshRenderer>().First(x=>x.name=="Chao");
            Check(ground.sharedMaterials.All(x=>x.GetFloat("_SimpleBase")>.5f),"Illustrated gradient ground not active");
            Check(view.RenderedLevelCount==100,"Planned islands generated fictitious phases");
        }
        finally{view.Dispose();}
        var authored=UnityEngine.Object.Instantiate(islands);authored.islands[0].customColors=true;authored.islands[0].ground=new Color(.3f,.5f,.8f);authored.islands[0].path=new Color(.7f,.4f,.2f);
        var custom=WorldMapView.Create(null,Resources.Load<LobbyFeaturesConfig>("RoyalAvesFeatures"),source,100,0,true,authored);
        try
        {
            custom.SetFixedSize(360,800);custom.TimeOverride=1;custom.Tick(3,3,false);
            Check(custom.Route.PaletteAt(custom.Route.Pos(1)).outerA==authored.islands[0].ground,"Authored ground colors ignored");
            Check(custom.GetComponentsInChildren<MeshRenderer>().First(x=>x.name=="Chao").sharedMaterials[0].GetFloat("_SimpleGrade")<.5f,"Default grade overwrote authored ground");
        }
        finally{custom.Dispose();UnityEngine.Object.DestroyImmediate(authored);}
        var t=typeof(RoyalAvesLevelMapPreview);var window=ScriptableObject.CreateInstance<RoyalAvesLevelMapPreview>();
        try
        {
            var shots=new[]{("intro",1.8f,5),("route",8f,8),("bridge",20.8f,21),("grove",25f,25),("later",85f,85)};
            foreach(var shot in shots)
            {
                t.GetField("level",Private).SetValue(window,shot.Item2);t.GetMethod("Build",Private).Invoke(window,new object[]{720,1600,shot.Item3});
                var map=(CurvedLevelMap)t.GetField("map",Private).GetValue(window);map.World.TimeOverride=1;
                t.GetMethod("Save",Private).Invoke(window,new object[]{Path.Combine(Output,shot.Item1+".png"),false});
            }
            t.GetField("level",Private).SetValue(window,1.8f);t.GetMethod("Build",Private).Invoke(window,new object[]{540,1200,5});
            var motion=(CurvedLevelMap)t.GetField("map",Private).GetValue(window);
            for(var i=0;i<3;i++){motion.World.TimeOverride=i*.8f;t.GetMethod("Save",Private).Invoke(window,new object[]{Path.Combine(Output,"motion-"+i+".png"),false});}
            t.GetField("seasonPreview",Private).SetValue(window,2);t.GetMethod("Build",Private).Invoke(window,new object[]{540,1200,5});
            var winter=(CurvedLevelMap)t.GetField("map",Private).GetValue(window);winter.World.TimeOverride=1;
            t.GetMethod("Save",Private).Invoke(window,new object[]{Path.Combine(Output,"winter.png"),false});
            Check(winter.World.Props.All(x=>allowed.Contains(x.sprite)),"Season replaced base props");
            Check(winter.World.GetComponentsInChildren<MeshFilter>().Any(x=>x.name.StartsWith("CoberturaDeNeve")),"Snow overlay missing");
        }
        finally{t.GetMethod("Cleanup",Private).Invoke(window,null);UnityEngine.Object.DestroyImmediate(window);}
        File.WriteAllText(Path.Combine(Output,"validation.txt"),$"PASS: unified green base; only 12 scenery sprites plus log bridge and four emblems; sparse opening <=20 props per20 levels; {clicks} phase picks, {anchors} curved drag anchors; character travel and pulse; building pulse; illustrated ground shader; shore gradient; actual100 phases unchanged; winter keeps original scenery. Native Unity captures saved. Mouse gestures and phone performance not automatically tested.");
    }
}
