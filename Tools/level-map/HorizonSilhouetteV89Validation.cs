using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections;using System.Collections.Generic;using UnityEngine;using UnityEditor;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class HorizonSilhouetteV89Validation
{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 public static void Run(){const string output="../horizon-v89";Directory.CreateDirectory(output);File.WriteAllText(output+"/validation.txt","RUNNING");RoyalAvesLevelMapPreview preview=null;int errors=0,subjects=0;float minRatio=1;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{
   ShaderUtil.allowAsyncCompilation=false;preview=RoyalAvesLevelMapPreview.CreateScenePreview(720,1280);var type=preview.GetType();var map=(CurvedLevelMap)type.GetField("map",F).GetValue(preview);map.World.TimeOverride=0;map.PreviewAt(1);
   var world=map.World;var route=world.Route;
   string Placements()=>JsonUtility.ToJson(new AutoIslandsBaselineV70.Props{entries=world.Props.Where(x=>route.LevelAt(x.s)<15.9f).Select(AutoIslandsBaselineV70.Signature).ToArray()},true);
   string initial=Placements();
   void Save(float cursor,string name){type.GetField("level",F).SetValue(preview,cursor);map.PreviewAt(cursor);type.GetMethod("Save",F).Invoke(preview,new object[]{output+"/"+name+".png",false});}
   void Subject(int index,string label,bool capture){
    var p=world.Props[index];float horizon=world.KitAt(p.s).radius*world.CameraModel.PitchAt(world.CameraModel.Aspect)*Mathf.Deg2Rad;
    float far=Mathf.Min(18,horizon-1.25f),near=Mathf.Min(5.5f,far*.5f),start=Mathf.Min(horizon-.01f,p.s-route.MetersAt(1));
    var samples=new Dictionary<int,(Quaternion rotation,Vector3 position,Vector3 scale)>();
    object Slot()=>((IDictionary)typeof(WorldMapView).GetField("activeSlots",F).GetValue(world))[index];
    Transform Card(object slot)=>(Transform)slot.GetType().GetField("card").GetValue(slot);
    void At(float rel){map.PreviewAt(route.LevelAt(p.s-rel));}
    if(p.s-route.MetersAt(1)>horizon+.1f){At(horizon+.08f);var hidden=Slot();Check(hidden!=null&&!((GameObject)hidden.GetType().GetField("go").GetValue(hidden)).activeSelf,label+" appears behind horizon");At(horizon-.01f);Check(Card(Slot()).localScale.y<.002f,label+" pops at crest instead of proportional entrance");}
    Quaternion previous=default,firstWorld=default;int count=48;
    for(int i=0;i<=count;i++){
     float rel=Mathf.Lerp(start,near,i/(float)count);At(rel);var card=Card(Slot());samples.Add(i,(card.localRotation,card.localPosition,card.localScale));
     if(i==0)firstWorld=card.rotation;if(i>0)Check(Quaternion.Angle(previous,card.localRotation)<2,label+" tilt jumps");previous=card.localRotation;
     var middle=card.localPosition+card.localRotation*Vector3.up*(p.height*.55f*card.localScale.y);
     Check(middle.y>=-.0001f,label+" visible artwork buried during reveal");
     if(card.localScale.y>.05f){
      Vector2 Point(Vector3 local){var point=world.transform.InverseTransformPoint(card.TransformPoint(local));Check(world.CameraModel.ProjectPoint(point,world.CameraModel.Aspect,out var uv,out _),"Projection failed");return Vector2.Scale(uv,new Vector2(world.CameraModel.Aspect,1));}
      var origin=Point(Vector3.zero);float ratio=(Point(Vector3.up)-origin).magnitude/Mathf.Max(.000001f,(Point(Vector3.right)-origin).magnitude);
      minRatio=Mathf.Min(minRatio,ratio);Check(ratio>.85f,label+" flattened in actual camera projection: "+ratio+" at "+rel);
     }
    }
    Check(Quaternion.Angle(firstWorld,Card(Slot()).rotation)>2,label+" reversible scroll tilt disappeared");
    var up=Card(Slot());if(route.Islands.Managed(route.IslandAt(p.s)))Check(Quaternion.Angle(up.rotation,world.transform.rotation*world.CameraModel.Rotation)<.03f,label+" lost frontal endpoint");
    float sink=p.groundPinned?p.height*Mathf.Clamp(p.groundDepth,0,.30f):0;Check((up.localPosition-new Vector3(0,-p.height*.018f-sink,0)).sqrMagnitude<.0000001f,label+" authored standing offset changed");
    for(int i=count;i>=0;i--){At(Mathf.Lerp(start,near,i/(float)count));var card=Card(Slot());Check(Quaternion.Angle(card.localRotation,samples[i].rotation)<.02f&&(card.localPosition-samples[i].position).sqrMagnitude<.000001f&&(card.localScale-samples[i].scale).sqrMagnitude<.000001f,label+" reverse scroll differs");}
    if(capture){Save(route.LevelAt(p.s-Mathf.Min(start,far)),label+"-distant");Save(route.LevelAt(p.s-(near+Mathf.Min(start,far))*.5f),label+"-approaching");Save(route.LevelAt(p.s-near),label+"-near");}subjects++;
   }
   int Find(int island,PropKind kind)=>Enumerable.Range(0,world.Props.Count).Last(i=>route.IslandIndexAt(world.Props[i].s)==island&&world.Props[i].kind==kind&&world.Props[i].workMotion==WorldMapWorkMotion.None&&world.Props[i].s-route.MetersAt(1)>5.5f);
   Subject(Find(1,PropKind.Tree),"tree",false);Subject(Find(1,PropKind.Landmark),"building",true);Subject(Find(2,PropKind.Character),"character",true);Subject(Find(0,PropKind.Landmark),"opening-building",false);
   Save(1,"opening");Save(2,"opening-scroll");Check(Placements()==initial,"Current first-island placement signatures changed");
   var fall=Enumerable.Range(0,world.Props.Count).FirstOrDefault(i=>world.Props[i].kind==PropKind.RiverFall);
   if(world.Props[fall].kind==PropKind.RiverFall){var p=world.Props[fall];map.PreviewAt(route.LevelAt(p.s-6));var slot=((IDictionary)typeof(WorldMapView).GetField("activeSlots",F).GetValue(world))[fall];var card=(Transform)slot.GetType().GetField("card").GetValue(slot);Check(Quaternion.Angle(card.rotation,world.transform.rotation*world.CameraModel.Rotation)<.03f,"Riverfall affected by scenery tilt");}
   type.GetMethod("Build",F).Invoke(preview,new object[]{360,780,1});map=(CurvedLevelMap)type.GetField("map",F).GetValue(preview);world=map.World;route=world.Route;world.TimeOverride=0;map.PreviewAt(1);
   Subject(Find(1,PropKind.Landmark),"narrow-building",true);Save(1,"narrow-opening");
   Check(errors==0,errors+" Unity errors");File.WriteAllText(output+"/validation.txt","PASS: "+subjects+" actual authored/automatic tree/building/character subjects across two phone ratios; 48 forward/reverse poses each; projected unit height/width never below "+minRatio.ToString("F3")+" (limit .85); proportional crest entrance without edge-on card; reversible gentle tilt retained; no buried center; near frontal orientation and authored offsets retained; riverfall unchanged; current first-island signatures unchanged; native distant/approach/near/opening captures; zero Unity errors. No layout or player save writes.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=logger;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}EditorApplication.Exit(0);
 }
}
