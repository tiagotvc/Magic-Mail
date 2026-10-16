using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
public static class EntranceV54Diagnose
{
 public static void Run()
 {
  var log=new System.Text.StringBuilder();const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;var mission=WorldMapMission100.Current;
  log.AppendLine("Layout "+AssetDatabase.GetAssetPath(mission.layout));
  foreach(var e in mission.layout.chapters.SelectMany(c=>c.entries).Where(e=>e.level<24))log.AppendLine(e.id+" "+e.level+" "+e.sprite.name);
  foreach(var state in new[]{false,true})
  {
   mission.polishedEntrance=state;var preview=RoyalAvesLevelMapPreview.CreateScenePreview(1080,1920);typeof(RoyalAvesLevelMapPreview).GetField("seasonPreview",flags).SetValue(preview,1);typeof(RoyalAvesLevelMapPreview).GetField("level",flags).SetValue(preview,1f);typeof(RoyalAvesLevelMapPreview).GetMethod("Build",flags).Invoke(preview,new object[]{1080,1920,1});var world=((CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",flags).GetValue(preview)).World;world.ConfigureDayNight(false,4,0);world.TimeOverride=2;preview.RenderScenePreview();
   foreach(var m in Resources.FindObjectsOfTypeAll<Material>().Where(m=>m.name.Contains("Ground")||m.name.Contains("Surface")||m.name.Contains("Token")))log.AppendLine(state+" mat="+m.name+" shader="+m.shader.name+" tint="+(m.HasProperty("_Tint")?m.GetColor("_Tint").ToString():"none")+" supported="+m.shader.isSupported);
   var rt=(RenderTexture)preview.SceneTexture;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes("C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/entrance-v54/diagnose-"+state+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(preview);
  }
  foreach(var shader in Resources.FindObjectsOfTypeAll<Shader>())foreach(var m in ShaderUtil.GetShaderMessages(shader))log.AppendLine(shader.name+": "+m.severity+" "+m.message);
  File.WriteAllText("C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/entrance-v54-diagnose.txt",log.ToString());EditorApplication.Exit(0);
 }
}
