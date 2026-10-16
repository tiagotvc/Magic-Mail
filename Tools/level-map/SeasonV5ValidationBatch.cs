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
public static class SeasonV5ValidationBatch
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/v5";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void Execute()
    {
        Directory.CreateDirectory(Output);
        var kit=RoyalAvesWorldMapSetup.Ensure(true);
        var islands=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);
        var art=Resources.Load<LobbyFeaturesConfig>("RoyalAvesFeatures");
        Check(islands.islands.Count>=10&&islands.Capacity==islands.islands.Count*20,"Uniform island catalogue missing");
        Check(islands.islands.Select(x=>x.terrain).Distinct().Count()>=10,"Fewer than ten distinct biomes");
        Check(WorldMapLevelCatalog.Count==100,"Existing phase catalogue changed");
        foreach(WorldMapTerrainTheme theme in Enum.GetValues(typeof(WorldMapTerrainTheme)))
        {
            var copy=UnityEngine.Object.Instantiate(kit);copy.terrainTheme=theme;
            if(!copy.Lava)Check(copy.Family.Length==6&&copy.Family.All(x=>x!=null),"Incomplete family "+theme);
            UnityEngine.Object.DestroyImmediate(copy);
        }
        Check(kit.geyserBase!=null&&kit.geyserWater!=null&&kit.geyserSteam!=null&&kit.meteorSprite!=null&&kit.vapor!=null&&kit.snowTerrainMaterials!=null,"Layered effect sprites/shader missing");
        var view=WorldMapView.Create(null,art,kit,100,0,true,islands);
        try
        {
            view.SetFixedSize(540,1200);view.TimeOverride=0;view.Tick(90,88,false);
            var geyser=view.Props.First(x=>x.kind==PropKind.Geyser&&Mathf.Abs(view.Route.LevelAt(x.s)-92)<5);
            var mesh=WorldMapCards.Get(kit.geyserBase,geyser.height*.34f,geyser.bottomPivot);
            var basal=view.GetComponentsInChildren<MeshFilter>().FirstOrDefault(x=>x.sharedMesh==mesh);
            Check(basal!=null,"Geyser fixed base not visible");
            var matrix=basal.transform.localToWorldMatrix;
            var water=basal.transform.Find("JatoIndependente");Check(water!=null,"Water layer missing");
            var heights=new System.Collections.Generic.HashSet<int>();
            for(var time=0f;time<6;time+=.25f)
            {
                view.TimeOverride=time;view.Tick(90,88,false);
                Check(basal.sharedMesh==mesh&&basal.transform.localToWorldMatrix==matrix,"Geyser rocks moved or changed frame");
                heights.Add(Mathf.RoundToInt(water.localScale.y*1000));
            }
            Check(heights.Count>3,"Independent water layer is not animated");
            Check(view.RenderedLevelCount==100,"Planned empty islands created playable levels");
            var picks=0;
            foreach(var level in new[]{1,19,20,21,39,40,41,60,61,80,81,99,100})
            {
                var cursor=view.CursorForLevel(level);view.Tick(cursor,level,false);
                Check(view.ProjectLevel(level,cursor,.45f,out var uv)&&view.PickViewport(uv)==level,"Phase picking failed: "+level);picks++;
            }
            view.Tick(85,85,false);
            var ground=view.GetComponentsInChildren<MeshFilter>().First(x=>x.gameObject.name=="Chao").sharedMesh;
            Check(ground.colors.Any(x=>x.r>1.5f),"No molten lava in trail geometry");
            var seasons=SeasonConfig.Instance;var oldActive=seasons.activeSeasonId;
            var testSeason=new Season{id="validation-season-v5",map=new MapSeasonLook{snow=false}};
            testSeason.map.overlays.Add(new MapSeasonOverlay{category=PropKind.Landmark,sprite=kit.snowRoof,relativeHeight=.30f,position=new Vector2(0,.64f)});
            seasons.seasons.Add(testSeason);
            try
            {
                seasons.activeSeasonId=testSeason.id;view.Tick(85,85,false);
                Check(view.GetComponentsInChildren<MeshFilter>().Any(x=>x.name.StartsWith("CoberturaDeNeve")&&x.gameObject.activeInHierarchy),"Custom overlay needs snow incorrectly");
                var before=view.Props.Select(x=>x.sprite).ToArray();testSeason.map.snow=true;view.Tick(85,85,false);
                Check(view.Props.Select(x=>x.sprite).SequenceEqual(before),"Winter replaced base scenery sprites");
                Check(view.GetComponentsInChildren<MeshRenderer>().First(x=>x.name=="Chao").sharedMaterials.Any(x=>x.GetFloat("_SnowCoverage")>.5f),"Season change did not update ground on a live map");
                var roundtrip=ScriptableObject.CreateInstance<SeasonConfig>();roundtrip.seasons.Add(testSeason);
                const string saved="Assets/SeasonV5Roundtrip.asset";AssetDatabase.CreateAsset(roundtrip,saved);AssetDatabase.SaveAssets();AssetDatabase.ImportAsset(saved,ImportAssetOptions.ForceUpdate);
                var restored=AssetDatabase.LoadAssetAtPath<SeasonConfig>(saved).seasons[0].map;
                Check(restored.snow&&restored.overlays[0].sprite==kit.snowRoof&&restored.overlays[0].category==PropKind.Landmark,"Season overlay did not persist");AssetDatabase.DeleteAsset(saved);
            }
            finally{seasons.activeSeasonId=oldActive;seasons.seasons.Remove(testSeason);}
            Debug.Log("V5 phase picking checks: "+picks+"; fixed geyser base, animated water and HDR lava verified");
        }
        finally{view.Dispose();}
        var root=new GameObject("AtmosphereTest",typeof(RectTransform),typeof(Canvas));
        var sky=new GameObject("Sky",typeof(RectTransform));sky.transform.SetParent(root.transform,false);((RectTransform)sky.transform).sizeDelta=new Vector2(720,1600);
        var back=new GameObject("Back",typeof(RectTransform),typeof(Image));back.transform.SetParent(sky.transform,false);
        try
        {
            var route=new WorldMapRoute(islands,100);var a=WorldMapAtmosphere.Create((RectTransform)root.transform,(RectTransform)sky.transform,back.GetComponent<Image>(),kit,route);
            float Focus(int n)=>route.LevelAt(route.Pos(n)-kit.focusAhead);
            a.Tick(Focus(81),1);a.Tick(Focus(81),1.3f);
            Check(root.GetComponentsInChildren<Image>().Any(x=>x.name.StartsWith("MeteoroSprite")&&x.enabled&&x.sprite==kit.meteorSprite),"Meteor is not a sprite");
            a.Tick(Focus(41),2);a.Tick(Focus(41),3);
            Check(root.GetComponentsInChildren<WorldMapWeatherGraphic>().Any(x=>x.mode==11),"Submerged water light missing");
            Check(!root.GetComponentsInChildren<WorldMapWeatherGraphic>().Any(x=>x.mode==8),"Procedural meteor still active");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
        var window=ScriptableObject.CreateInstance<RoyalAvesLevelMapPreview>();
        var type=typeof(RoyalAvesLevelMapPreview);
        try
        {
            for(var i=0;i<10;i++)
            {
                type.GetField("level",Private).SetValue(window,i*20+5f);
                type.GetMethod("Build",Private).Invoke(window,new object[]{540,1200,Mathf.Min(100,i*20+1)});
                var map=(CurvedLevelMap)type.GetField("map",Private).GetValue(window);map.World.TimeOverride=1;
                type.GetMethod("Save",Private).Invoke(window,new object[]{Path.Combine(Output,"area-"+(i+1).ToString("00")+".png"),false});
            }
            type.GetField("seasonPreview",Private).SetValue(window,2);
            type.GetField("level",Private).SetValue(window,85f);
            type.GetMethod("Build",Private).Invoke(window,new object[]{720,1600,81});
            var snowMap=(CurvedLevelMap)type.GetField("map",Private).GetValue(window);snowMap.World.TimeOverride=1;
            type.GetMethod("Save",Private).Invoke(window,new object[]{Path.Combine(Output,"lava-natal.png"),false});
            Check(snowMap.World.GetComponentsInChildren<MeshFilter>().Any(x=>x.name.StartsWith("CoberturaDeNeve")&&x.gameObject.activeInHierarchy),"Snow overlay missing on volcanic objects");
            var ground=snowMap.World.GetComponentsInChildren<MeshRenderer>().First(x=>x.name=="Chao");
            Check(ground.sharedMaterials.Any(x=>x.GetFloat("_SnowCoverage")>.5f),"Whole seasonal snowy ground missing");
            Check(ground.sharedMaterials.All(x=>x.GetFloat("_HasSnowTex")>.5f),"Painted winter material missing");
            type.GetField("seasonPreview",Private).SetValue(window,1);
            type.GetField("level",Private).SetValue(window,90f);
            type.GetMethod("Build",Private).Invoke(window,new object[]{540,1200,88});
            var motion=(CurvedLevelMap)type.GetField("map",Private).GetValue(window);
            for(var i=0;i<4;i++)
            {
                motion.World.TimeOverride=i*1.4f;
                type.GetMethod("Save",Private).Invoke(window,new object[]{Path.Combine(Output,"geyser-"+i+".png"),false});
            }
        }
        finally{type.GetMethod("Cleanup",Private).Invoke(window,null);UnityEngine.Object.DestroyImmediate(window);}
        File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: ten distinct equal-size areas; nine complete six-sprite families plus Lava; actual 100 phases preserved, planned 200 slots; 13 real picking checks across boundaries; geyser base mesh and transform unchanged over 24 times while liquid animates; sprite meteor; submerged lighting/bubbles; HDR lava trail cracks; snow overlay on volcanic props and whole-ground winter treatment. Native Unity captures: ten areas, Christmas Lava, four geyser times. Physical device performance and mouse gestures remain unmeasured.");
    }
}
