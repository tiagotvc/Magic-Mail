using System;using System.IO;using System.Reflection;using UnityEngine;using UnityEditor;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
// Read-only visual comparison: the isolated Resource is modified in memory and never saved.
public static class LateralArcV96Preview
{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 public static void Run()
 {
  const string output="../lateral-arc-v96";Directory.CreateDirectory(output);RoyalAvesLevelMapPreview preview=null;WorldMapIslands data=null;float original=0;int errors=0;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{
   ShaderUtil.allowAsyncCompilation=false;preview=RoyalAvesLevelMapPreview.CreateScenePreview(720,1280);var type=preview.GetType();var map=(CurvedLevelMap)type.GetField("map",F).GetValue(preview);map.World.TimeOverride=0;
   data=map.World.Route.Islands;original=data.lateralCurvature;
   void Save(float level,string name){type.GetField("level",F).SetValue(preview,level);map.PreviewAt(level+.01f);map.PreviewAt(level);type.GetMethod("Save",F).Invoke(preview,new object[]{output+"/"+name+".png",false});}
   Save(1,"current");data.lateralCurvature=1.95f;Save(1,"stronger-arc");Save(5,"stronger-arc-scroll");Save(18,"stronger-arc-island");
   data.lateralCurvature=original;map.PreviewAt(1.01f);map.PreviewAt(1);
   if(errors!=0)throw new Exception(errors+" Unity errors");File.WriteAllText(output+"/validation.txt","PASS: native comparison of current cross-map arc and 1.95 arc; all changes in isolated Resource memory restored, no asset or layout writes; zero Unity errors.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{if(data!=null)data.lateralCurvature=original;Application.logMessageReceived-=logger;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}EditorApplication.Exit(0);
 }
}
