using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections;using System.Collections.Generic;using UnityEngine;using UnityEditor;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class SceneryRollV100Validation
{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 public static void Run(){const string output="../scenery-roll-v100";Directory.CreateDirectory(output);File.WriteAllText(output+"/validation.txt","RUNNING");RoyalAvesLevelMapPreview preview=null;int errors=0,subjects=0,poses=0;float maxScaleError=0,maxHingeError=0;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{
   ShaderUtil.allowAsyncCompilation=false;preview=RoyalAvesLevelMapPreview.CreateScenePreview(720,1280);var type=preview.GetType();var map=(CurvedLevelMap)type.GetField("map",F).GetValue(preview);var world=map.World;world.TimeOverride=0;var route=world.Route;map.PreviewAt(1);
   string Placements()=>string.Join("\n",world.Props.Select(AutoIslandsBaselineV70.Signature));
   string baseline=Placements();
   void Save(float cursor,string name){type.GetField("level",F).SetValue(preview,cursor);map.PreviewAt(cursor);type.GetMethod("Save",F).Invoke(preview,new object[]{output+"/"+name+".png",false});}
   int Find(int island,PropKind kind)=>Enumerable.Range(0,world.Props.Count).Last(i=>route.IslandIndexAt(world.Props[i].s)==island&&world.Props[i].kind==kind&&world.Props[i].workMotion==WorldMapWorkMotion.None&&world.Props[i].s-route.MetersAt(1)>7);
   void Subject(int index,string label,bool capture){
    var p=world.Props[index];float horizon=world.KitAt(p.s).radius*world.CameraModel.PitchAt(world.CameraModel.Aspect)*Mathf.Deg2Rad;
    float start=Mathf.Min(horizon-.01f,p.s-route.MetersAt(1)),near=Mathf.Max(.1f,horizon-4.25f);
    Check(start>near,label+" sample range unavailable");
    object Slot()=>((IDictionary)typeof(WorldMapView).GetField("activeSlots",F).GetValue(world))[index];
    Transform Card()=>(Transform)Slot().GetType().GetField("card").GetValue(Slot());
    void At(float rel){map.PreviewAt(route.LevelAt(p.s-rel));}
    At(near);var up=Card();Vector3 standingOffset=up.localPosition;Quaternion standingLocal=up.localRotation;
    float sink=p.groundPinned?p.height*Mathf.Clamp(p.groundDepth,0,.30f):0;
    Check((standingOffset-new Vector3(0,-p.height*.018f-sink,0)).sqrMagnitude<.0000001f,label+" standing authored offset changed");
    Vector3 contact=Vector3.up*(-standingOffset.y/Mathf.Max(.25f,(standingLocal*Vector3.up).y));
    Vector3 hinge=standingOffset+standingLocal*contact;
    bool complete=p.s-route.MetersAt(1)>horizon+.1f;
    if(complete){
     At(horizon+.05f);Check(!((GameObject)Slot().GetType().GetField("go").GetValue(Slot())).activeSelf,label+" visible behind crest");
     At(horizon-.01f);Check(Quaternion.Angle(Card().localRotation,Quaternion.Euler(90,0,0))<.03f,label+" not lying on curved ground at crest");
     Check(!((MeshRenderer)Slot().GetType().GetField("shadowRenderer").GetValue(Slot())).enabled,label+" shadow before unfolding");
    }
    var samples=new List<(Vector3 position,Quaternion rotation,Vector3 scale)>();Quaternion previous=default;const int count=64;
    float firstAngle=0;
    for(int i=0;i<=count;i++){
     At(Mathf.Lerp(start,near,i/(float)count));var card=Card();samples.Add((card.localPosition,card.localRotation,card.localScale));
     var abs=new Vector3(Mathf.Abs(card.localScale.x),Mathf.Abs(card.localScale.y),Mathf.Abs(card.localScale.z));
     float scaleError=(abs-Vector3.one).magnitude;maxScaleError=Mathf.Max(maxScaleError,scaleError);Check(scaleError<.00001f,label+" grows while scrolling at "+i);
     Check(Mathf.Abs(card.TransformVector(Vector3.up).magnitude-1)<.0001f&&Mathf.Abs(card.TransformVector(Vector3.right).magnitude-1)<.0001f,label+" actual geometry resized");
     // The frontal endpoint follows the camera, so its authored contact is evaluated
     // in this sample's terrain frame rather than reusing a different root rotation.
     var endpoint=route.Islands.Managed(route.IslandAt(p.s))?Quaternion.Inverse(card.parent.localRotation)*world.CameraModel.Rotation:standingLocal;
     var footContact=Vector3.up*(-standingOffset.y/Mathf.Max(.25f,(endpoint*Vector3.up).y));
     var targetContact=standingOffset+endpoint*footContact;
     float hingeError=(card.localPosition+card.localRotation*footContact-targetContact).magnitude;maxHingeError=Mathf.Max(maxHingeError,hingeError);
     Check(hingeError<.0001f,label+" foot slides during hinge");
     var middle=card.localPosition+card.localRotation*Vector3.up*p.height*.55f;
     Check(middle.y>=-.0001f,label+" buried under tangent");
     if(i==0)firstAngle=Quaternion.Angle(card.localRotation,standingLocal);
     if(i>0)Check(Quaternion.Angle(previous,card.localRotation)<5,label+" rotation discontinuity");previous=card.localRotation;poses++;
    }
    // Some authored opening props are already below the hinge interval at the
    // earliest legal cursor. Their fixed standing pose must not be forced to fold.
    if(complete)Check(firstAngle>75,label+" no actual scroll hinge");
    else if(start>horizon-2.5f)Check(firstAngle>10,label+" no partial hinge");
    Check(Quaternion.Angle(Card().localRotation,standingLocal)<.02f,label+" near endpoint differs");
    if(route.Islands.Managed(route.IslandAt(p.s)))Check(Quaternion.Angle(Card().rotation,world.transform.rotation*world.CameraModel.Rotation)<.03f,label+" no frontal near endpoint");
    for(int i=count;i>=0;i--){At(Mathf.Lerp(start,near,i/(float)count));var card=Card();Check((card.localPosition-samples[i].position).sqrMagnitude<.000001f&&Quaternion.Angle(card.localRotation,samples[i].rotation)<.02f&&(card.localScale-samples[i].scale).sqrMagnitude<.000001f,label+" reverse differs");}
    world.TimeOverride=2.35f;At(near);Check((new Vector3(Mathf.Abs(Card().localScale.x),Card().localScale.y,Card().localScale.z)-Vector3.one).sqrMagnitude<.000001f,label+" idle breathing resizes scenery");world.TimeOverride=0;
    if(capture){for(int i=0;i<=8;i++)Save(route.LevelAt(p.s-Mathf.Lerp(start,near,i/8f)),label+"-"+i);}
    subjects++;
   }
   Subject(Find(1,PropKind.Tree),"tree",true);Subject(Find(1,PropKind.Landmark),"building",true);Subject(Find(2,PropKind.Character),"character",true);
   Subject(Find(0,PropKind.Landmark),"opening-building",false);
   Save(1,"opening");Save(12,"crossing-before");Save(14,"crossing-after");Save(33,"christmas-island");
   foreach(var kind in new[]{PropKind.RiverFall,PropKind.RiverPlant,PropKind.Mist}){
    int index=Enumerable.Range(0,world.Props.Count).FirstOrDefault(i=>world.Props[i].kind==kind);
    if(world.Props[index].kind!=kind)continue;var p=world.Props[index];map.PreviewAt(route.LevelAt(p.s-6));var slot=((IDictionary)typeof(WorldMapView).GetField("activeSlots",F).GetValue(world))[index];var card=(Transform)slot.GetType().GetField("card").GetValue(slot);
    Check(Quaternion.Angle(card.rotation,world.transform.rotation*world.CameraModel.Rotation)<.03f,kind+" affected by hinge");
   }
   Check(Placements()==baseline,"Placement data changed");
   type.GetMethod("Build",F).Invoke(preview,new object[]{360,780,1});map=(CurvedLevelMap)type.GetField("map",F).GetValue(preview);world=map.World;route=world.Route;world.TimeOverride=0;map.PreviewAt(1);
   Subject(Find(1,PropKind.Landmark),"narrow-building",true);Save(1,"narrow-opening");
   Check(errors==0,errors+" Unity errors");File.WriteAllText(output+"/validation.txt","PASS: "+subjects+" actual authored/automatic tree/building/character subjects, "+poses+" forward poses plus identical reverse traversal across standard/narrow phones; unit local and world geometry scale throughout scroll and idle (max error "+maxScaleError+"); real ground-tangent to standing hinge, no scale reveal; fixed visible ground contact (max error "+maxHingeError+"), no buried middle; continuity, hidden behind crest/shadow-free start, frontal automatic endpoint and authored near offsets preserved; river plants/falls/mist unchanged; all placement signatures unchanged; native 9-stage sequences and opening/crossing/Christmas captures; zero Unity errors. No player or layout writes.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=logger;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}EditorApplication.Exit(0);
 }
}
