using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEditor;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class GroundRollV97Validation
{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 [Serializable]class Items{public Item[] items;}
 [Serializable]class Item{public string id,sprite;public float s,x,height;}
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 public static void Run()
 {
  const string output="../ground-roll-v97";Directory.CreateDirectory(output);RoyalAvesLevelMapPreview preview=null;WorldMapKit source=null;float r=0,p=0,h=0;int errors=0,picks=0,groundPicks=0;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{
   ShaderUtil.allowAsyncCompilation=false;source=Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath);r=source.radius;p=source.pitch;h=source.cameraHeight;
   Check(Mathf.Abs(r-20)<.001f,"Wrong production radius");var islands=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);Check(islands.pathSway==1,"Unwanted S-curve increase remains");
   source.radius=26;source.pitch=32;source.cameraHeight=4.9f;
   CurvedLevelMap map=null;WorldMapView world=null;Type type=null;
   void Create(int width,int height){if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);preview=RoyalAvesLevelMapPreview.CreateScenePreview(width,height);type=preview.GetType();map=(CurvedLevelMap)type.GetField("map",F).GetValue(preview);world=map.World;world.TimeOverride=0;map.PreviewAt(1);}
   string First()=>JsonUtility.ToJson(new Items{items=world.Props.Where(x=>world.Route.IslandIndexAt(x.s)==0).Select(x=>new Item{id=x.id,sprite=x.sprite.name,s=x.s,x=x.offset,height=x.height}).ToArray()});
   void Save(int level,string name){type.GetField("level",F).SetValue(preview,(float)level);map.PreviewAt(level);type.GetMethod("Save",F).Invoke(preview,new object[]{output+"/"+name+".png",false});}
   Vector2 Near(){world.CameraModel.Project(0,0,0,0,9f/16,out var uv,out _);return uv;}
   Vector2 Crest(){world.CameraModel.Project(0,world.CameraModel.Kit.radius*world.CameraModel.PitchAt(9f/16)*Mathf.Deg2Rad,0,0,9f/16,out var uv,out _);return uv;}
   Create(720,1280);var first=First();var near=Near();var crest=Crest();Save(1,"before-opening");Save(5,"before-scroll");
   source.SetGroundCurveStrength(1.3f);Check(Mathf.Abs(source.radius-20)<.001f,"Curvature control failed");Create(720,1280);
   Check(Vector2.Distance(near,Near())<.0001f&&Vector2.Distance(crest,Crest())<.0001f,"Curvature changed near ground/horizon framing");
   Check(MapCurve.Point(8,0,0,20).y<MapCurve.Point(8,0,0,26).y-.25f,"Ground does not bend downward more strongly");
   Check(First()==first,"Authored opening object source data changed");
   void Interactions(int level){
    for(int target=level+1;target<=Mathf.Min(100,level+4);target++){
     if(!world.ProjectLevelBounds(target,level,world.Cam.aspect,out var bounds)||bounds.center.y<.12f||bounds.center.y>.78f)continue;
     Check(world.PickViewport(bounds.center)==target,"Level tap missed after stronger roll: "+target);picks++;
    }
    float cursor=world.Route.MetersAt(level),s=cursor+2;
    if(world.Route.InGap(s,.5f))return;
    foreach(float side in new[]{-1.8f,1.8f}){
     float x=world.Route.CenterX(s)+WorldMapTerrain.Lateral(side,2),height=WorldMapTerrain.SurfaceHeight(world.Route,s,side);
     if(!world.CameraModel.Project(cursor,s,x,height,world.Cam.aspect,out var uv,out _)||uv.x<.05f||uv.x>.95f||uv.y<.12f||uv.y>.8f)continue;
     Check(world.GroundAtViewport(uv,level,world.Cam.aspect,out var phase,out var lateral),"Editor cannot pick rolled ground");
     Check(Mathf.Abs(phase-world.Route.LevelAt(s))<.12f&&Mathf.Abs(lateral-side)<.12f,"Editor ground pick drifts after stronger roll");groundPicks++;
    }
   }
   foreach(int level in new[]{1,5,14,18,29,33,63,95}){Save(level,"level-"+level);Interactions(level);}
   Create(360,780);foreach(int level in new[]{1,18}){Save(level,"narrow-"+level);Interactions(level);}
   Check(picks>=25&&groundPicks>=8,"Insufficient interaction checks");Check(errors==0,errors+" Unity errors");
   File.WriteAllText(output+"/validation.txt","PASS: actual downward terrain roll strengthened from radius 26 to 20 (1.3 strength), with camera pitch/height refitted to preserve near ground and crest framing; S-curve restored to 1; authored first-island object source anchors/art/heights unchanged; "+picks+" token hit tests and "+groundPicks+" editor ground picks pass across two phone ratios; normal/seasonal/crossing native renders; zero Unity errors. No layout, progress or purchase writes.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{if(source!=null){source.radius=r;source.pitch=p;source.cameraHeight=h;}Application.logMessageReceived-=logger;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}EditorApplication.Exit(0);
 }
}
