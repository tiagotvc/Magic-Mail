using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEditor;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class PathSwayV96Validation
{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 [Serializable]class Items{public Item[] items;}
 [Serializable]class Item{public string id,sprite;public float s,x,height;}
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 public static void Run()
 {
  const string output="../path-sway-v96";Directory.CreateDirectory(output);RoyalAvesLevelMapPreview preview=null;WorldMapIslands data=null;float original=1;int errors=0,picks=0;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{
   ShaderUtil.allowAsyncCompilation=false;preview=RoyalAvesLevelMapPreview.CreateScenePreview(720,1280);var type=preview.GetType();var map=(CurvedLevelMap)type.GetField("map",F).GetValue(preview);var world=map.World;world.TimeOverride=0;data=world.Route.Islands;original=data.pathSway;
   Check(Mathf.Abs(original-1.35f)<.0001f&&Mathf.Abs(data.lateralCurvature-1.55f)<.0001f,"Wrong route sway or unrelated ground arc changed");
   string First()=>JsonUtility.ToJson(new Items{items=world.Props.Where(p=>world.Route.IslandIndexAt(p.s)==0).Select(p=>new Item{id=p.id,sprite=p.sprite.name,s=p.s,x=p.offset,height=p.height}).ToArray()});
   var first=First();data.pathSway=1;var baseline=Enumerable.Range(0,400).Select(i=>world.Route.CenterX(8+i*.8f)).ToArray();data.pathSway=1.35f;
   for(int i=0;i<baseline.Length;i++)Check(Mathf.Abs(world.Route.CenterX(8+i*.8f)-baseline[i]*1.35f)<.0001f,"Route phase/continuity changed");
   foreach(var gap in world.Route.Gaps)Check(Mathf.Abs(world.Route.CenterX(gap.start)-world.Route.CenterX(gap.end))<.0001f,"Bridge crossing bends inside gap");
   void Save(float level,string name){type.GetField("level",F).SetValue(preview,level);map.PreviewAt(level+.01f);map.PreviewAt(level);type.GetMethod("Save",F).Invoke(preview,new object[]{output+"/"+name+".png",false});}
   data.pathSway=1;Save(1,"before-opening");Save(5,"before-scroll");data.pathSway=1.35f;
   foreach(int level in new[]{1,5,14,18,29,33,63,95}){
    Save(level,"level-"+level);for(int target=level+1;target<=Mathf.Min(100,level+4);target++){
     if(!world.ProjectLevelBounds(target,level,world.Cam.aspect,out var bounds)||bounds.center.y<.12f||bounds.center.y>.78f)continue;
     Check(world.PickViewport(bounds.center)==target,"Level tap misses displaced trail: "+target);picks++;
    }
   }
   Check(picks>=15,"Insufficient token hit tests");Check(First()==first,"Authored opening scenery data changed");
   type.GetMethod("Build",F).Invoke(preview,new object[]{360,780,1});map=(CurvedLevelMap)type.GetField("map",F).GetValue(preview);world=map.World;world.TimeOverride=0;Save(1,"narrow-opening");Save(18,"narrow-island");
   Check(errors==0,errors+" Unity errors");File.WriteAllText(output+"/validation.txt","PASS: path left/right sway 1.35, 35 percent stronger, independently editable from unchanged 1.55 cross-map ground arc; 400 route samples retain phase and smooth scaling; bridge gaps remain straight; "+picks+" actual projected token hit tests pass; first-island IDs, sprites, source anchors and heights unchanged; normal/seasonal/crossing and two phone-ratio captures; zero Unity errors. No layout, progress or purchase writes.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{if(data!=null)data.pathSway=original;Application.logMessageReceived-=logger;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}EditorApplication.Exit(0);
 }
}
