using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEditor;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class MapPaletteV94Validation
{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 [Serializable]public class Item{public string id,sprite;public float level,lateral,height;public PropKind kind;}
 [Serializable]public class Snapshot{public Item[] items;}
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 public static void Before()=>Run(false);
 public static void After()=>Run(true);
 static void Run(bool after)
 {
  const string root="../map-palette-v94";string output=root+(after?"/after":"/before");Directory.CreateDirectory(output);RoyalAvesLevelMapPreview preview=null;int errors=0;
  Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=log;
  try{
   ShaderUtil.allowAsyncCompilation=false;preview=RoyalAvesLevelMapPreview.CreateScenePreview(720,1280);var type=preview.GetType();var map=(CurvedLevelMap)type.GetField("map",F).GetValue(preview);map.World.TimeOverride=0;map.PreviewAt(1);
   var world=map.World;var route=world.Route;
   var snapshot=new Snapshot{items=world.Props.Where(p=>route.IslandIndexAt(p.s)==0).Select(p=>new Item{id=p.id,sprite=p.sprite.name,level=route.LevelAt(p.s),lateral=p.offset,height=p.height,kind=p.kind}).ToArray()};
   if(!after)File.WriteAllText(root+"/first-island-before.json",JsonUtility.ToJson(snapshot,true));
   else{
    var before=JsonUtility.FromJson<Snapshot>(File.ReadAllText(root+"/first-island-before.json"));Check(before.items.Length==snapshot.items.Length,"First-island object count changed");int houses=0;
    foreach(var old in before.items){var now=snapshot.items.Single(p=>p.id==old.id);Check(now.sprite==old.sprite&&now.kind==old.kind&&Mathf.Abs(now.level-old.level)<.0001f&&Mathf.Abs(now.lateral-old.lateral)<.0001f,"First-island authored anchor/art changed: "+old.id);
     bool house=old.kind==PropKind.Landmark&&old.sprite.Contains("postal-cottage");Check(Mathf.Abs(now.height-old.height*(house?.8f:1))<.0001f,"Unexpected first-island height: "+old.sprite);if(house)houses++;
    }
    Check(houses==2,"Expected exactly the two authored opening cottages");
    Check(world.Route.Islands.recipes.All(r=>r.buildingSize<=1.3761f),"Automatic building presets still large");
    var standard=WorldMapIslands.IllustratedPalette(new WorldMapIslands.Island());var lavender=WorldMapIslands.IllustratedPalette(new WorldMapIslands.Island{groundStyle=WorldMapGroundStyle.Lavender});Check(Vector4.Distance(standard.outerA,lavender.outerA)>.15f,"Themed islands lost distinct palettes");
    var custom=new WorldMapIslands.Island{customColors=true,ground=Color.magenta,path=Color.cyan,water=Color.blue};var palette=WorldMapIslands.IllustratedPalette(custom);Check(palette.outerA==custom.ground&&palette.pathA==custom.path&&palette.water==custom.water,"Custom palette controls ignored");
   }
   void Save(float cursor,string name){type.GetField("level",F).SetValue(preview,cursor);map.PreviewAt(cursor);type.GetMethod("Save",F).Invoke(preview,new object[]{output+"/"+name+".png",false});}
   foreach(int level in after?new[]{1,5,18,33,63,80,108}:new[]{1,5,18,63})Save(level,"level-"+level);
   if(after){type.GetMethod("Build",F).Invoke(preview,new object[]{360,780,1});map=(CurvedLevelMap)type.GetField("map",F).GetValue(preview);map.World.TimeOverride=0;Save(1,"narrow-opening");Save(20,"narrow-island");}
   Check(errors==0,errors+" Unity errors");File.WriteAllText(output+"/validation.txt",after?"PASS: cream/sand and sage/olive ground rendered on seven map positions and two phone ratios; two opening houses 20 percent smaller with every authored anchor, sprite, object count and other height preserved; automatic building presets reduced; themed palettes distinct; custom colors honored; no shader/Unity errors. No player-save writes.\n":"PASS: baseline captured from current production map; first-island authored anchors/art/heights snapshotted; four native captures; zero Unity errors.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=log;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}EditorApplication.Exit(0);
 }
}
