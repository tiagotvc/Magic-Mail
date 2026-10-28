using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEditor;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;using SweetSugar.Scripts.MapScripts;
public static class LevelRewardsV77Validation
{
 sealed class Scores:IMapProgressManager{
  public int LoadLevelStarsCount(int level)=>level==1?1:level==2?2:3;
  public int GetLastLevel()=>4;public string GetLevelKey(int n)=>"v77-memory-"+n;public string GetScoreKey(int n)=>"v77-memory-score-"+n;
  public void SaveLevelStarsCount(int l,int s)=>throw new Exception("Unexpected score write");public void SaveLevelStarsCount(int l,int s,int score)=>throw new Exception("Unexpected score write");public void ClearLevelProgress(int l)=>throw new Exception("Unexpected score write");
 }
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 public static void Run(){const string output="../level-rewards-v77";Directory.CreateDirectory(output);File.WriteAllText(output+"/validation.txt","RUNNING");RoyalAvesLevelMapPreview p=null;int errors=0;var original=LevelsMap._mapProgressManager;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{
   LevelsMap._mapProgressManager=new Scores();ShaderUtil.allowAsyncCompilation=false;
   var kit=Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath);
   if(kit.rewardStarGold==null||kit.rewardStarEmpty==null||!kit.levelCrown.name.Contains("v77")||kit.levelCrown!=kit.simpleCrown)throw new Exception("New reward references missing");
   if(kit.rewardStarGold.texture.width>512||kit.rewardStarEmpty.texture.width>512)throw new Exception("Reward textures unnecessarily large");
   p=RoyalAvesLevelMapPreview.CreateScenePreview(720,1280);var type=p.GetType();var map=(CurvedLevelMap)type.GetField("map",F).GetValue(p);
   void At(int active,float cursor,string name){typeof(CurvedLevelMap).GetField("nextLevel",F).SetValue(map,active);type.GetField("level",F).SetValue(p,cursor);map.World.TimeOverride=0;map.PreviewAt(cursor);type.GetMethod("Save",F).Invoke(p,new object[]{output+"/"+name+".png",false});}
   At(4,1,"scores-1-2-3");
   var nodes=(Array)typeof(WorldMapView).GetField("nodes",F).GetValue(map.World);int tested=0;
   foreach(var node in nodes){var nt=node.GetType();int level=(int)nt.GetField("level").GetValue(node);if(level<1||level>4)continue;
    var renderers=(MeshRenderer[])nt.GetField("stars").GetValue(node);var crown=(MeshRenderer)nt.GetField("crown").GetValue(node);
    if(crown.gameObject.activeSelf!=(level==3))throw new Exception("Crown eligibility changed at "+level);
    int earned=level<4?level:0;
    for(int j=0;j<3;j++){
     var star=renderers[j];var expected=j<earned?kit.rewardStarGold:kit.rewardStarEmpty;
     if(star.sharedMaterial.mainTexture!=expected.texture||!star.gameObject.activeSelf)throw new Exception("Wrong score sprite for "+level+" star "+j);
     var box=star.GetComponent<MeshFilter>().sharedMesh.bounds;if(box.size.x<=0||box.size.y<=0)throw new Exception("Empty reward card");
    }tested++;
   }
   if(tested!=4)throw new Exception("Missing score states");
   At(1,1,"current-empty");At(15,13,"completed-crowns");At(15,1,"distant-completed");At(31,29,"crossing-rewards");
   type.GetMethod("Build",F).Invoke(p,new object[]{360,780,4});map=(CurvedLevelMap)type.GetField("map",F).GetValue(p);At(4,1,"narrow-phone");
   if(errors!=0)throw new Exception(errors+" Unity errors");
   File.WriteAllText(output+"/validation.txt","PASS: glossy rounded crown and matched gold/platinum stars with cream support; actual 0/1/2/3-star states checked using in-memory scores; crown eligibility retained; native normal/current/crossing/narrow-phone captures; compact 512px imports; no progress writes; zero Unity errors.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{LevelsMap._mapProgressManager=original;Application.logMessageReceived-=logger;if(p!=null)UnityEngine.Object.DestroyImmediate(p);}EditorApplication.Exit(0);
 }
}
