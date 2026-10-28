using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEngine;
using UnityEditor;
public static class BeamEditorValidation
{
 public static void Run()
 {
  RoyalAvesLevelMapPreview preview=null;
  try {
   ShaderUtil.allowAsyncCompilation=false;
   const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
   preview=(RoyalAvesLevelMapPreview)typeof(RoyalAvesLevelMapPreview).GetMethod("CreateScenePreview",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{540,960});
   var map=(CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",flags).GetValue(preview);
   var world=map.World;
   var p=world.Props[world.Props.Count/2];
   var entry=WorldMapLayout.Entry.From(p,world.Route);entry.id=p.id;entry.raySpacing=.83f;
   var copy=JsonUtility.FromJson<WorldMapLayout.Entry>(JsonUtility.ToJson(entry));
   if(Mathf.Abs(copy.Placement(world.Route,world.CameraModel.Kit).raySpacing-.83f)>.0001f)throw new Exception("Spacing did not persist");
   if(!world.RefreshPlacement(copy))throw new Exception("Single object refresh failed");
   var sw=Stopwatch.StartNew();for(int i=0;i<100;i++)world.RefreshPlacement(copy);sw.Stop();double fast=sw.Elapsed.TotalMilliseconds/100;
   sw.Restart();for(int i=0;i<5;i++)world.ReloadScenery();sw.Stop();double full=sw.Elapsed.TotalMilliseconds/5;
   var beam=Shader.Find("RoyalAves/WorldMap/Beam");if(beam==null)throw new Exception("Missing beam shader");
   if(ShaderUtil.ShaderHasError(beam))throw new Exception("Beam shader compilation failed");
   var mat=new Material(beam);var sparkle=new Material(Shader.Find("RoyalAves/WorldMap/Sparkle"));
   var host=new GameObject("BeamValidation");var fx=new WorldMapLightFx(host.transform,0,mat,sparkle);
   var shaft=new WorldMapLightSet.Shaft{raySpacing=.83f,sparkles=0};fx.TickShaft(shaft,0,1);
   var block=new MaterialPropertyBlock();fx.Root.Find("Feixe").GetComponent<MeshRenderer>().GetPropertyBlock(block);
   if(Mathf.Abs(block.GetFloat("_RaySpacing")-.83f)>.0001f)throw new Exception("Spacing missing on material");
   fx.Dispose();UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(mat);UnityEngine.Object.DestroyImmediate(sparkle);
   world.Tick(map.Cursor,1,false);world.RenderNow();
   File.WriteAllText("../beam-editor-validation.txt",$"PASS: spacing JSON/placement/material roundtrip; beam shader compiled; native map render.\nSingle object refresh: {fast:F4} ms. Full scenery reload: {full:F4} ms (isolated editor CPU).\n");
  } catch(Exception e){UnityEngine.Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}
  EditorApplication.Exit(0);
 }
}
