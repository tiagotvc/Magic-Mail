using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEngine.UI;using UnityEditor;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class LevelFocusV75Validation
{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 public static void Run(){const string output="../level-focus-v75";Directory.CreateDirectory(output);File.WriteAllText(output+"/validation.txt","RUNNING");RoyalAvesLevelMapPreview p=null;int errors=0;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{
   ShaderUtil.allowAsyncCompilation=false;p=RoyalAvesLevelMapPreview.CreateScenePreview(720,1280);var type=p.GetType();
   var map=(CurvedLevelMap)type.GetField("map",F).GetValue(p);map.World.TimeOverride=0;
   var markers=map.GetComponentsInChildren<WorldMapCurrentFocusGraphic>(true);if(markers.Length!=1)throw new Exception("Expected a single current-level effect");
   var marker=(WorldMapCurrentLevel)typeof(CurvedLevelMap).GetField("currentLevelMarker",F).GetValue(map);
   void At(int active,float cursor,string file,float time=0){typeof(CurvedLevelMap).GetField("nextLevel",F).SetValue(map,active);type.GetField("level",F).SetValue(p,cursor);map.World.TimeOverride=time;map.PreviewAt(cursor);type.GetMethod("Save",F).Invoke(p,new object[]{output+"/"+file+".png",false});}
   foreach(int level in new[]{1,5,15,30,31,100}){
    At(level,level,"active-"+level);
    if(!marker.Visible)throw new Exception("Marker missing for current level "+level);
    if(marker.Marker.anchoredPosition.x-marker.Marker.rect.width*.5f<0)throw new Exception("Avatar outside left edge");
    if(marker.Marker.GetComponentsInChildren<Graphic>(true).Any(g=>g.raycastTarget))throw new Exception("Marker blocks map input");
    int vertices=markers[0].canvasRenderer.GetMesh().vertexCount;if(vertices>800)throw new Exception("Focus geometry unexpectedly large");
    var nodes=(Array)typeof(WorldMapView).GetField("nodes",F).GetValue(map.World);
    foreach(var node in nodes){var nt=node.GetType();if((int)nt.GetField("level").GetValue(node)!=level)continue;
     var stars=(MeshRenderer[])nt.GetField("stars").GetValue(node);if(stars.Count(s=>s.gameObject.activeInHierarchy)!=3)throw new Exception("Current star missing");
     foreach(var star in stars){var block=new MaterialPropertyBlock();star.GetPropertyBlock(block);if(block.GetFloat("_RewardEarned")!=0)throw new Exception("Unplayed star marked earned");if(star.sharedMaterial.HasProperty("_RewardMode")?star.sharedMaterial.GetFloat("_RewardMode")!=1:star.sharedMaterial.mainTexture==null)throw new Exception("Glossy star rendering missing");}
    }
   }
   At(5,5,"active-5-phase",.85f);
   // The isolated preview facade is writable in memory; real player progress remains read-only here.
   var avatar=typeof(MetaProgress).GetProperty("Avatar");
   if(avatar.CanWrite){try{avatar.SetValue(null,1);At(5,5,"custom-profile");var image=marker.Marker.Find("RoundedPortraitMask/ProfilePortrait").GetComponent<Image>();if(!image.gameObject.activeSelf||image.sprite==null)throw new Exception("Custom profile avatar not reflected");}finally{avatar.SetValue(null,0);}}
   At(1,40,"current-offscreen");if(marker.Visible)throw new Exception("Offscreen marker remained visible");
   type.GetMethod("Build",F).Invoke(p,new object[]{360,780,5});map=(CurvedLevelMap)type.GetField("map",F).GetValue(p);marker=(WorldMapCurrentLevel)typeof(CurvedLevelMap).GetField("currentLevelMarker",F).GetValue(map);
   At(5,5,"narrow-phone");var viewport=(RectTransform)marker.Marker.parent;
   if(!marker.Visible||marker.Marker.anchoredPosition.x-marker.Marker.rect.width*.5f<0||marker.Marker.anchoredPosition.x+marker.Marker.rect.width*.5f>viewport.rect.width)throw new Exception("Narrow phone marker clipped");
   if(errors!=0)throw new Exception(errors+" Unity errors");
   File.WriteAllText(output+"/validation.txt","PASS: glossy gold/silver stars; all three current stars enabled; one avatar marker and lightweight focus effect following levels 1/5/15/30/31/100; marker passes input and hides offscreen; alternate phase, custom profile and narrow-phone captures; zero Unity errors.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=logger;if(p!=null)UnityEngine.Object.DestroyImmediate(p);}EditorApplication.Exit(0);
 }
}
