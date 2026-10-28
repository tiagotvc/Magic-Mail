using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
public static class ChristmasSkyV66Validation
{
 public static void Run()
 {
  RoyalAvesLevelMapPreview preview=null;int errors=0;
  Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=log;
  try{
   ShaderUtil.allowAsyncCompilation=false;var config=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);
   if(config.christmasBackdrop==null||config.christmasVillain==null)throw new Exception("Missing production sky sprites");
   var type=typeof(RoyalAvesLevelMapPreview);const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
   preview=(RoyalAvesLevelMapPreview)type.GetMethod("CreateScenePreview",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{720,1280});
   var map=(CurvedLevelMap)type.GetField("map",flags).GetValue(preview);
   var atmosphere=(WorldMapAtmosphere)typeof(CurvedLevelMap).GetField("atmosphere",flags).GetValue(map);
   Directory.CreateDirectory("../christmas-v66");
   foreach(int level in new[]{1,16,31,61})
   {
    type.GetField("level",flags).SetValue(preview,(float)level);map.PreviewAt(level);
    type.GetMethod("Save",flags).Invoke(preview,new object[]{"../christmas-v66/level-"+level+".png",false});
    var images=atmosphere.GetComponentsInChildren<Image>(true);
    foreach(var image in images.Where(i=>i.name.StartsWith("Nuvem_")))if(image.enabled)throw new Exception("Animated cloud remains enabled");
    foreach(var image in images.Where(i=>i.name=="BackgroundSeparado"))if(!image.enabled||image.sprite!=config.christmasBackdrop)throw new Exception("Seasonal sky is inconsistent between islands");
   }
   atmosphere.Tick(1,0);var villain=atmosphere.GetComponentsInChildren<Image>(true).First(i=>i.name=="RibbonRookSilhouette");
   var start=villain.rectTransform.anchorMin;atmosphere.Tick(1,1);if(start==villain.rectTransform.anchorMin)throw new Exception("Villain does not jump");
   config.christmasSky=false;atmosphere.Tick(1,2);if(villain.enabled)throw new Exception("Season toggle leaves villain visible");config.christmasSky=true;atmosphere.Tick(1,3);
   // New sky animation must still respect the editor's idle rendering gate.
   type.GetField("scenePreview",flags).SetValue(preview,false);var render=type.GetMethod("RenderWindow",flags);var stamp=type.GetField("lastPreviewFrame",flags);
   render.Invoke(preview,null);var before=stamp.GetValue(preview);for(int i=0;i<20;i++)render.Invoke(preview,null);if(!Equals(before,stamp.GetValue(preview)))throw new Exception("Sky animation causes idle redraws");
   if(ShaderUtil.ShaderHasError(Shader.Find("RoyalAves/WorldMap/Surface"))||errors>0)throw new Exception("Unity render errors: "+errors);
   File.WriteAllText("../christmas-v66/validation.txt","PASS: sky and silhouette sprites imported; clouds disabled; common backdrop across 4 islands; villain jumps; theme toggle restores normal sky; 20 idle requests trigger no extra renders; native captures; zero Unity errors.\n");
  }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=log;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}
  EditorApplication.Exit(0);
 }
}
