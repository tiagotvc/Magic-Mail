using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEditor;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class GlossyBuildingsV74Validation
{
 public static void Run(){const string output="../glossy-buildings-v74";Directory.CreateDirectory(output);File.WriteAllText(output+"/validation.txt","RUNNING");RoyalAvesLevelMapPreview preview=null;int errors=0;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{
   ShaderUtil.allowAsyncCompilation=false;var config=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);
   string[] paths={"christmas-cottage-glossy-v74","christmas-depot-glossy-v74","stamp-workshop-glossy-v74"};
   foreach(var name in paths){var asset=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/GlossyBuildingsV74/"+name+".png");if(asset==null||!config.recipes.Any(r=>r.buildings.Contains(asset)))throw new Exception("Glossy building missing: "+name);}
   const BindingFlags f=BindingFlags.NonPublic|BindingFlags.Instance;var type=typeof(RoyalAvesLevelMapPreview);
   preview=(RoyalAvesLevelMapPreview)type.GetMethod("CreateScenePreview",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{720,1280});
   var map=(CurvedLevelMap)type.GetField("map",f).GetValue(preview);map.World.TimeOverride=0;map.PreviewAt(1);
   var first=JsonUtility.ToJson(new AutoIslandsBaselineV70.Props{entries=map.World.Props.Where(x=>map.World.Route.LevelAt(x.s)<15.9f).Select(AutoIslandsBaselineV70.Signature).ToArray()},true);
   if(first!=File.ReadAllText("../auto-islands-v70/first-props-before.json"))throw new Exception("Authored first island changed");
   foreach(var name in paths)if(!map.World.Props.Any(p=>p.sprite!=null&&p.sprite.name==name))throw new Exception("Glossy building not placed: "+name);
   foreach(int level in new[]{1,19,32,77,92}){type.GetField("level",f).SetValue(preview,(float)level);map.PreviewAt(level);type.GetMethod("Save",f).Invoke(preview,new object[]{output+"/level-"+level+".png",false});}
   if(!config.staticSky||!config.christmasBackdrop.name.Contains("shadow"))throw new Exception("Shadow sky changed");
   if(errors!=0)throw new Exception(errors+" Unity errors");
   File.WriteAllText(output+"/validation.txt","PASS: three glossy building sprites imported and used by shared recipes and map placements; five native map captures; authored first-island placements and static shadow sky retained; zero Unity errors.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=logger;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}EditorApplication.Exit(0);
 }
}
