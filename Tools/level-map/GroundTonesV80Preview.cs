using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEditor;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class GroundTonesV80Preview
{
 public static void Before()=>Capture("before");
 public static void After()=>Capture("after");
 static void Capture(string stage){string output="../ground-tones-v80/"+stage;Directory.CreateDirectory(output);RoyalAvesLevelMapPreview p=null;int errors=0;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{ShaderUtil.allowAsyncCompilation=false;p=RoyalAvesLevelMapPreview.CreateScenePreview(720,1280);const BindingFlags f=BindingFlags.NonPublic|BindingFlags.Instance;var type=p.GetType();
   var map=(CurvedLevelMap)type.GetField("map",f).GetValue(p);map.World.TimeOverride=0;map.PreviewAt(1);var route=map.World.Route;
   var first=JsonUtility.ToJson(new AutoIslandsBaselineV70.Props{entries=map.World.Props.Where(x=>route.LevelAt(x.s)<15.9f).Select(AutoIslandsBaselineV70.Signature).ToArray()},true);
   if(first!=File.ReadAllText("../auto-islands-v70/first-props-before.json"))throw new Exception("First-island placements changed");
   foreach(int level in new[]{1,5,18,29,33,48,63,92,108}){type.GetField("level",f).SetValue(p,(float)level);map.PreviewAt(level);type.GetMethod("Save",f).Invoke(p,new object[]{output+"/level-"+level+".png",false});}
   if(errors!=0)throw new Exception(errors+" Unity errors");File.WriteAllText(output+"/validation.txt","PASS: nine native map captures, preserved first-island placement signatures, no layout or progress writes, zero Unity errors.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=logger;if(p!=null)UnityEngine.Object.DestroyImmediate(p);}EditorApplication.Exit(0);
 }
}
