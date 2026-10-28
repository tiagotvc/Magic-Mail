using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using RoyalAves.EditorTools;
public static class ActiveSeasonSaveValidation
{
 static Type Controls=>typeof(RoyalAvesLevelMapPreview).Assembly.GetType("RoyalAves.EditorTools.RoyalAvesMapSeasonControls");
 static void Save(int mode)=>Controls.GetMethod("SaveActive",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{mode});
 public static void Run()
 {
  try{
   var config=Resources.Load<SeasonConfig>(SeasonConfig.ResourcePath);int count=config.seasons.Count;
   Save(2);Check(config.Active.map.snow,"Christmas was not activated");string id=config.activeSeasonId;
   Save(2);Check(config.activeSeasonId==id&&config.seasons.Count<=count+1,"Repeated save duplicates season");
   string yaml=File.ReadAllText("Assets/RoyalAves/Resources/RoyalAvesSeasons.asset");Check(yaml.Contains("activeSeasonId: "+id),"Season not written to disk");
   var kit=Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath);Check(kit.Christmas,"Runtime map does not use saved Christmas season");
   Save(1);Check(!config.Active.map.snow&&!kit.Christmas,"Normal theme did not persist");
   Save(2);Save(0);Check(config.Active.map.snow,"Saving active selection changed season");
   Directory.CreateDirectory("../active-season-save");File.WriteAllText("../active-season-save/first-pass.txt","PASS: Christmas and normal selection, repeated saves, runtime lookup and YAML persistence.\n");
  }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);return;}EditorApplication.Exit(0);
 }
 public static void Reload()
 {
  RoyalAvesLevelMapPreview preview=null;
  try{
   var config=Resources.Load<SeasonConfig>(SeasonConfig.ResourcePath);Check(config.Active.map.snow,"Active Christmas season lost after editor restart");
   var kit=Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath);Check(kit.Christmas&&kit.seasonPreview==0&&!kit.christmasPreview,"Runtime depends on temporary preview override");
   ShaderUtil.allowAsyncCompilation=false;preview=(RoyalAvesLevelMapPreview)typeof(RoyalAvesLevelMapPreview).GetMethod("CreateScenePreview",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{540,960});
   Check((int)typeof(RoyalAvesLevelMapPreview).GetField("seasonPreview",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(preview)==0,"Reopened editor overrides active season");
   File.WriteAllText("../active-season-save/reload-pass.txt","PASS: fresh Unity process reads saved Christmas season; runtime lookup and new preview use it without temporary flags.\n");
  }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);return;}finally{if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}EditorApplication.Exit(0);
 }
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
}
