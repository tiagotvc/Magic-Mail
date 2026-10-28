using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
public static class GlossyCrossingsValidation
{
 public static void Run(){RoyalAvesLevelMapPreview preview=null;int errors=0;Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=log;
 try{
 ShaderUtil.allowAsyncCompilation=false;var islands=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);var route=new WorldMapRoute(islands,150);
 if(islands.levelsPerIsland!=15||islands.specialLevelEvery!=30||islands.Capacity<500)throw new Exception("Island pacing/capacity");
 for(int i=0;i<islands.islands.Count;i++){if(islands.FirstLevel(i)!=i*15+1)throw new Exception("Wrong island boundary");}
 if(route.Gaps[0].afterLevel!=15)throw new Exception("First mission still blocks crossings");
 for(float meters=0;meters<route.Length;meters+=.37f){
 float expected=meters-(route.Pos(1)-1.4f);bool gapHit=false;
 foreach(var g in route.Gaps){expected=Mathf.Min(expected,meters<=g.start?g.start-1-meters:meters-g.end-1);gapHit|=meters>g.start-.3f&&meters<g.end+.3f;}
 if(Mathf.Abs(expected-route.DistanceFromLaneEnd(meters))>.0001f||gapHit!=route.InGap(meters,.3f))throw new Exception("Optimized shore lookup changed geometry");
 }
 var authored=new WorldMapIslands.Island{liquid=WorldMapLiquid.Symphony,musicNotes=6,noteSpeed=.24f,lakeLevels=3};authored.stones.Add(new WorldMapIslands.CrossingStone{levelOffset=-1,lateral=.45f,size=1.2f});authored.plants.Add(new WorldMapIslands.CrossingPlant{progress=.7f,lateral=-2.3f,size=.8f});
 var restored=JsonUtility.FromJson<WorldMapIslands.Island>(JsonUtility.ToJson(authored));if(restored.stones[0].lateral!=.45f||restored.musicNotes!=6||restored.plants[0].progress!=.7f)throw new Exception("Crossing controls did not persist");
 var t=typeof(RoyalAvesLevelMapPreview);const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
 preview=(RoyalAvesLevelMapPreview)t.GetMethod("CreateScenePreview",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{720,1280});
 var map=(CurvedLevelMap)t.GetField("map",flags).GetValue(preview);Directory.CreateDirectory("../glossy-crossings");
 foreach(int level in new[]{12,27,30,42,57,72,87,102,147}){
 t.GetField("level",flags).SetValue(preview,(float)level);map.PreviewAt(level);t.GetMethod("Save",flags).Invoke(preview,new object[]{"../glossy-crossings/level-"+level+".png",false});
 foreach(var mf in map.World.GetComponentsInChildren<MeshFilter>())if(mf.sharedMesh!=null)foreach(var v in mf.sharedMesh.vertices)if(float.IsNaN(v.x)||float.IsInfinity(v.y))throw new Exception("Invalid mesh");
 }
 foreach(var name in new[]{"Surface","Water"})if(ShaderUtil.ShaderHasError(Shader.Find("RoyalAves/WorldMap/"+name)))throw new Exception("Shader compilation failed "+name);
 if(errors>0)throw new Exception(errors+" Unity errors");
 File.WriteAllText("../glossy-crossings/validation.txt","PASS: 15 levels per island; special interval 30; capacity >=500; crossings from first island; 9 native renders; finite meshes; water/surface shaders compile; zero Unity errors.\n");
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);return;}finally{Application.logMessageReceived-=log;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}EditorApplication.Exit(0);}
}
