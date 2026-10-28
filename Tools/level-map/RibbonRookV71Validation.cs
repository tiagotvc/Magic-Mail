using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEngine.UI;using UnityEditor;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class RibbonRookV71Validation
{
 public static void Run(){const string output="../ribbon-rook-v71";Directory.CreateDirectory(output);File.WriteAllText(output+"/validation.txt","RUNNING");RoyalAvesLevelMapPreview p=null;int errors=0;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{
   ShaderUtil.allowAsyncCompilation=false;var config=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);
   if(!config.staticSky||config.christmasBackdrop==null||config.christmasVillain==null||config.christmasVillainCharacter==null||config.villainSize<200)throw new Exception("New skyline art/settings missing");
   const BindingFlags f=BindingFlags.NonPublic|BindingFlags.Instance;var type=typeof(RoyalAvesLevelMapPreview);
   p=(RoyalAvesLevelMapPreview)type.GetMethod("CreateScenePreview",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{720,1280});
   var map=(CurvedLevelMap)type.GetField("map",f).GetValue(p);map.World.TimeOverride=0;map.PreviewAt(1);
   var first=JsonUtility.ToJson(new AutoIslandsBaselineV70.Props{entries=map.World.Props.Where(x=>map.World.Route.LevelAt(x.s)<15.9f).Select(AutoIslandsBaselineV70.Signature).ToArray()},true);
   if(first!=File.ReadAllText("../auto-islands-v70/first-props-before.json"))throw new Exception("First island placements changed");
   var sky=(WorldMapAtmosphere)typeof(CurvedLevelMap).GetField("atmosphere",f).GetValue(map);
   var current=typeof(WorldMapAtmosphere).GetField("current",f).GetValue(sky);var layer=current.GetType();
   var shadow=(Image)layer.GetField("villain").GetValue(current);var position=shadow.rectTransform.anchoredPosition;var anchor=shadow.rectTransform.anchorMin;var rotation=shadow.rectTransform.localRotation;
   map.World.TimeOverride=137;map.PreviewAt(1);
   if(sky.NeedsAnimation||shadow.rectTransform.anchoredPosition!=position||shadow.rectTransform.anchorMin!=anchor||shadow.rectTransform.localRotation!=rotation)throw new Exception("Sky villain still animates");
   foreach(Image image in (Image[])layer.GetField("clouds").GetValue(current))if(image.enabled)throw new Exception("Sky cloud still visible");
   foreach(Image image in (Image[])layer.GetField("meteorSprites").GetValue(current))if(image.enabled)throw new Exception("Sky meteor still visible");
   var weather=(Behaviour)typeof(WorldMapAtmosphere).GetField("effects",f).GetValue(sky);if(weather.enabled)throw new Exception("Sky weather still animates");
   int villains=0;
   for(int i=1;i<config.ActiveIslandCount;i++){
    bool christmas=config.Recipe(config.islands[i]).christmas;
    var actors=map.World.Props.Where(x=>map.World.Route.IslandIndexAt(x.s)==i&&x.sprite==config.christmasVillainCharacter).ToArray();
    if(actors.Length!=(christmas?1:0))throw new Exception("Villain island distribution mismatch: "+(i+1));
    foreach(var actor in actors){villains++;if(!actor.shadow||actor.height<1.4f||map.World.Props.Any(x=>x.id==actor.id+"-cover"))throw new Exception("Colored villain body hidden or too small");}
   }
   foreach(int level in new[]{1,12,19,32,77,122,167}){
    type.GetField("level",f).SetValue(p,(float)level);map.World.TimeOverride=0;map.PreviewAt(level);
    type.GetMethod("Save",f).Invoke(p,new object[]{output+"/level-"+level+".png",false});
   }
   if(errors>0)throw new Exception(errors+" Unity errors");
   File.WriteAllText(output+"/validation.txt","PASS: imported shadow skyline, matching silhouette and full-color character; sky fixed across 137 seconds; clouds/meteors/weather disabled; "+villains+" full-body villains on Christmas islands only; first-island placements unchanged; seven native captures; zero Unity errors.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=logger;if(p!=null)UnityEngine.Object.DestroyImmediate(p);}EditorApplication.Exit(0);
 }
}
