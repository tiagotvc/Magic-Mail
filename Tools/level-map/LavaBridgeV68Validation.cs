using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
public static class LavaBridgeV68Validation
{
 public static void Run()
 {
  const string output="../lava-bridge-v68";Directory.CreateDirectory(output);File.WriteAllText(output+"/validation.txt","RUNNING");
  RoyalAvesLevelMapPreview preview=null;int errors=0;
  Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=log;
  try{
   ShaderUtil.allowAsyncCompilation=false;
   var config=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);
   if(config.lavaStoneLeft==null||config.lavaStoneRight==null||config.lavaSurface==null||config.lavaFall==null||config.bridgeDeck==null)throw new Exception("Missing V68 art");
   var type=typeof(RoyalAvesLevelMapPreview);const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
   preview=(RoyalAvesLevelMapPreview)type.GetMethod("CreateScenePreview",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{720,1280});
   var map=(CurvedLevelMap)type.GetField("map",flags).GetValue(preview);
   foreach(var level in new[]{72,12,42,147,27}){
    type.GetField("level",flags).SetValue(preview,(float)level);map.PreviewAt(level);
    type.GetMethod("Save",flags).Invoke(preview,new object[]{output+"/level-"+level+".png",false});
   }
   var world=map.World;
   var stones=world.Props.Where(p=>p.kind==PropKind.RiverPlatform&&p.supportedLevel>=73&&p.supportedLevel<=75).OrderBy(p=>p.supportedLevel).ToArray();
   if(stones.Length!=3||stones[0].sprite!=config.lavaStoneLeft||stones[1].sprite!=config.lavaStoneRight||stones[2].sprite!=config.lavaStoneLeft)throw new Exception("Lava platforms not themed");
   if(world.Props.Any(p=>p.kind==PropKind.RiverPlant&&world.Route.InGap(p.s)&&world.Route.CrossingAt(world.Route.Gaps.First(g=>p.s>g.start&&p.s<g.end))?.liquid==WorldMapLiquid.Lava))throw new Exception("Green default plant in lava");
   var material=(Material)typeof(WorldMapView).GetField("matWater",flags).GetValue(world);
   if(material.GetFloat("_HasLavaArt")<.5f||material.GetTexture("_LavaTex")!=config.lavaSurface.texture)throw new Exception("Painted river not bound");
   var ground=(Material)typeof(WorldMapView).GetField("matSurface",flags).GetValue(world);
   if(ground.GetTexture("_BridgeTex")!=config.bridgeDeck.texture)throw new Exception("Bridge texture not bound");
   // Deck center follows route center without the former alternating artificial shear.
   type.GetField("level",flags).SetValue(preview,12f);map.PreviewAt(12);
   var terrain=(WorldMapTerrain)typeof(WorldMapView).GetField("terrain",flags).GetValue(world);
   var mesh=terrain.ground;var colors=mesh.colors;var points=mesh.vertices;var uv=mesh.uv;
   int deckVertices=0;float maxWidth=0;
   var radius=world.CameraModel.Kit.radius;var lateralRadius=radius/world.Route.Curvature;
   var cursor=(float)typeof(WorldMapTerrain).GetField("cursor",flags).GetValue(terrain);
   float Across(Vector3 p)=>Mathf.Atan2(p.x,Mathf.Sqrt(p.z*p.z+(p.y+radius)*(p.y+radius))-radius+lateralRadius)*lateralRadius;
   int centeredRows=0;
   for(int i=0;i<points.Length;i++)if(colors[i].a>1.5f){
    deckVertices++;
    if(float.IsNaN(points[i].x)||float.IsInfinity(points[i].x))throw new Exception("Invalid bridge vertex");
    maxWidth=Mathf.Max(maxWidth,uv[i].x);
    if(uv[i].x<.001f)for(int j=0;j<points.Length;j++)if(colors[j].a>1.5f&&uv[j].x>.999f&&Mathf.Abs(uv[j].y-uv[i].y)<.0001f){
     float s=cursor+Mathf.Atan2(points[i].z,points[i].y+radius)*radius;
     if(Mathf.Abs((Across(points[i])+Across(points[j]))*.5f-world.Route.CenterX(s))>.002f)throw new Exception("Bridge is sheared away from route center");
     centeredRows++;break;
    }
   }
   if(deckVertices<20||maxWidth<.99f||centeredRows<3)throw new Exception("Missing bridge mesh");
   if(errors>0)throw new Exception(errors+" Unity errors");
   File.WriteAllText(output+"/validation.txt","PASS: 5 imported assets; lava uses distinct left/right stones; painted lava bound to river shader; lava cascade; no default green aquatic plants in lava; new bridge deck bound and valid; 5 native captures; zero Unity errors.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=log;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}
  EditorApplication.Exit(0);
 }
}
