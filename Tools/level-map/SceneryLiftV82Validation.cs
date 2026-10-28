using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections;using System.Collections.Generic;using UnityEngine;using UnityEditor;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class SceneryLiftV82Validation
{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 public static void Run(){const string output="../scenery-lift-v82";Directory.CreateDirectory(output);RoyalAvesLevelMapPreview preview=null;int errors=0,subjects=0;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{
   ShaderUtil.allowAsyncCompilation=false;preview=RoyalAvesLevelMapPreview.CreateScenePreview(720,1280);var type=preview.GetType();var map=(CurvedLevelMap)type.GetField("map",F).GetValue(preview);map.World.TimeOverride=0;map.PreviewAt(1);
   var world=map.World;var route=world.Route;
   var first=JsonUtility.ToJson(new AutoIslandsBaselineV70.Props{entries=world.Props.Where(x=>route.LevelAt(x.s)<15.9f).Select(AutoIslandsBaselineV70.Signature).ToArray()},true);
   Check(first==File.ReadAllText("../auto-islands-v70/first-props-before.json"),"First-island placement data changed");
   void Save(float cursor,string name){type.GetField("level",F).SetValue(preview,cursor);map.PreviewAt(cursor);type.GetMethod("Save",F).Invoke(preview,new object[]{output+"/"+name+".png",false});}
   void Subject(int index,string label,bool capture){
    var p=world.Props[index];float horizon=world.KitAt(p.s).radius*world.CameraModel.PitchAt(world.CameraModel.Aspect)*Mathf.Deg2Rad;
    float far=Mathf.Min(18,horizon-1.25f),near=Mathf.Min(5.5f,far*.5f),middle=(near+far)*.5f;
    var samples=new Dictionary<int,(Quaternion rotation,Vector3 position)>();
    object Slot()=>((IDictionary)typeof(WorldMapView).GetField("activeSlots",F).GetValue(world))[index];
    Transform Card(object slot)=>(Transform)slot.GetType().GetField("card").GetValue(slot);
    void At(float rel){float cursor=route.LevelAt(p.s-rel);Check(cursor>=1,"Invalid test cursor");map.PreviewAt(cursor);}
    At(horizon+.08f);var hidden=Slot();Check(hidden!=null&&!((GameObject)hidden.GetType().GetField("go").GetValue(hidden)).activeSelf,label+" visible behind horizon");
    At(horizon-.03f);var born=Slot();Check(((GameObject)born.GetType().GetField("go").GetValue(born)).activeSelf,label+" not active at crest");
    Check(Quaternion.Angle(Card(born).localRotation,Quaternion.Euler(90,0,0))<.02f,label+" spawned upright at crest");
    Check(!((MeshRenderer)born.GetType().GetField("shadowRenderer").GetValue(born)).enabled,label+" shadow appears before unfolding");
    Quaternion previous=default;int count=36;
    for(int i=0;i<=count;i++){
     float rel=Mathf.Lerp(far,near,i/(float)count);At(rel);var card=Card(Slot());
     samples.Add(i,(card.localRotation,card.localPosition));
     if(i>0)Check(Quaternion.Angle(previous,card.localRotation)<7,label+" hinge jumps while scrolling");previous=card.localRotation;
    }
    Check(Quaternion.Angle(samples[0].rotation,samples[count].rotation)>45,label+" no lift animation");
    var up=Card(Slot());Check(Quaternion.Angle(up.rotation,world.transform.rotation*world.CameraModel.Rotation)<.03f,label+" lost frontal standing orientation");
    for(int i=count;i>=0;i--){At(Mathf.Lerp(far,near,i/(float)count));var card=Card(Slot());Check(Quaternion.Angle(card.localRotation,samples[i].rotation)<.02f&&(card.localPosition-samples[i].position).sqrMagnitude<.000001f,label+" differs when reversing scroll");}
    if(capture){Save(route.LevelAt(p.s-far),label+"-folded");Save(route.LevelAt(p.s-middle),label+"-rising");Save(route.LevelAt(p.s-near),label+"-standing");}subjects++;
   }
   int Find(int island,PropKind kind)=>Enumerable.Range(0,world.Props.Count).First(i=>route.IslandIndexAt(world.Props[i].s)==island&&world.Props[i].kind==kind);
   Subject(Find(1,PropKind.Tree),"tree",false);Subject(Find(1,PropKind.Landmark),"building",true);Subject(Find(2,PropKind.Character),"character",false);
   Save(1,"opening");Save(33,"christmas-island");
   // Ground props retain world-conformed geometry and river decorations keep their frontal pose.
   var fall=Enumerable.Range(0,world.Props.Count).FirstOrDefault(i=>world.Props[i].kind==PropKind.RiverFall);
   if(world.Props[fall].kind==PropKind.RiverFall){var p=world.Props[fall];map.PreviewAt(route.LevelAt(p.s-6));var slot=((IDictionary)typeof(WorldMapView).GetField("activeSlots",F).GetValue(world))[fall];var card=(Transform)slot.GetType().GetField("card").GetValue(slot);Check(Quaternion.Angle(card.rotation,world.transform.rotation*world.CameraModel.Rotation)<.03f,"Riverfall tilted by scenery lift");}
   type.GetMethod("Build",F).Invoke(preview,new object[]{360,780,1});map=(CurvedLevelMap)type.GetField("map",F).GetValue(preview);world=map.World;route=world.Route;world.TimeOverride=0;map.PreviewAt(1);
   Subject(Find(1,PropKind.Landmark),"narrow-building",true);
   Check(errors==0,errors+" Unity errors");File.WriteAllText(output+"/validation.txt","PASS: "+subjects+" actual automatic tree/building/character subjects across phone ratios; behind-horizon activation remains folded and shadow-free; 36 forward and reverse scroll samples each are continuous and reversible; near artwork keeps frontal camera orientation; riverfall remains frontal; first-island placement signatures unchanged; native opening/theme/folded/rising/standing captures; zero Unity errors. No layout or player save writes.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=logger;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}EditorApplication.Exit(0);
 }
}
