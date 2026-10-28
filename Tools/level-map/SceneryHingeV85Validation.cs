using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections;using System.Collections.Generic;using UnityEngine;using UnityEditor;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class SceneryHingeV85Validation
{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 public static void Run(){const string output="../scenery-hinge-v85";Directory.CreateDirectory(output);RoyalAvesLevelMapPreview preview=null;int errors=0,subjects=0;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{
   ShaderUtil.allowAsyncCompilation=false;preview=RoyalAvesLevelMapPreview.CreateScenePreview(720,1280);var type=preview.GetType();var map=(CurvedLevelMap)type.GetField("map",F).GetValue(preview);map.World.TimeOverride=0;map.PreviewAt(1);
   var world=map.World;var route=world.Route;
   var first=JsonUtility.ToJson(new AutoIslandsBaselineV70.Props{entries=world.Props.Where(x=>route.LevelAt(x.s)<15.9f).Select(AutoIslandsBaselineV70.Signature).ToArray()},true);
   Check(first==File.ReadAllText("../auto-islands-v70/first-props-before.json"),"First-island placement data changed");
   void Save(float cursor,string name){type.GetField("level",F).SetValue(preview,cursor);map.PreviewAt(cursor);type.GetMethod("Save",F).Invoke(preview,new object[]{output+"/"+name+".png",false});}
   void Subject(int index,string label,bool capture){
    var p=world.Props[index];float horizon=world.KitAt(p.s).radius*world.CameraModel.PitchAt(world.CameraModel.Aspect)*Mathf.Deg2Rad;
    float far=Mathf.Min(18,horizon-1.25f),near=Mathf.Min(5.5f,far*.5f);
    bool fullRange=p.s-route.MetersAt(1)>horizon+.08f;
    far=Mathf.Min(far,p.s-route.MetersAt(1));
    var samples=new Dictionary<int,(Quaternion rotation,Vector3 position)>();
    object Slot()=>((IDictionary)typeof(WorldMapView).GetField("activeSlots",F).GetValue(world))[index];
    Transform Card(object slot)=>(Transform)slot.GetType().GetField("card").GetValue(slot);
    void At(float rel){float cursor=route.LevelAt(p.s-rel);Check(cursor>=1,"Invalid test cursor");map.PreviewAt(cursor);}
    if(fullRange){
     At(horizon+.08f);var hidden=Slot();Check(hidden!=null&&!((GameObject)hidden.GetType().GetField("go").GetValue(hidden)).activeSelf,label+" visible behind horizon");
     At(horizon-.03f);var born=Slot();Check(((GameObject)born.GetType().GetField("go").GetValue(born)).activeSelf,label+" not active at crest");
     Check(Quaternion.Angle(Card(born).localRotation,Quaternion.Euler(90,0,0))<.02f,label+" spawned upright at crest");
     Check(!((MeshRenderer)born.GetType().GetField("shadowRenderer").GetValue(born)).enabled,label+" shadow appears before unfolding");
    }
    Quaternion previous=default;int count=48;
    for(int i=0;i<=count;i++){
     float rel=Mathf.Lerp(far,near,i/(float)count);At(rel);var card=Card(Slot());
     samples.Add(i,(card.localRotation,card.localPosition));
     // Physical regression: the middle/top of a standing sprite must stay above its
     // terrain tangent during unfolding, rather than the whole card sinking below it.
     var middle=card.localPosition+card.localRotation*Vector3.up*(p.height*.55f*card.localScale.y);
     var top=card.localPosition+card.localRotation*Vector3.up*(p.height*card.localScale.y);
     Check(middle.y>=-.0001f&&top.y>=-.0001f,label+" visible upper artwork buried below terrain at sample "+i);
     if(i==0&&fullRange)Check(Mathf.Abs(middle.y)<.0001f&&Mathf.Abs(top.y)<.0001f,label+" flat artwork not resting on terrain tangent");
     if(i>0)Check(Quaternion.Angle(previous,card.localRotation)<6,label+" hinge jumps while scrolling");previous=card.localRotation;
    }
    Check(Quaternion.Angle(samples[0].rotation,samples[count].rotation)>(fullRange?45:15),label+" no lift animation");
    var up=Card(Slot());
    if(route.Islands.Managed(route.IslandAt(p.s)))Check(Quaternion.Angle(up.rotation,world.transform.rotation*world.CameraModel.Rotation)<.03f,label+" lost frontal standing orientation");
    float sink=p.groundPinned?p.height*Mathf.Clamp(p.groundDepth,0,.30f):0;
    Check((up.localPosition-new Vector3(0,-p.height*.018f-sink,0)).sqrMagnitude<.0000001f,label+" authored upright position changed");
    for(int i=count;i>=0;i--){At(Mathf.Lerp(far,near,i/(float)count));var card=Card(Slot());Check(Quaternion.Angle(card.localRotation,samples[i].rotation)<.02f&&(card.localPosition-samples[i].position).sqrMagnitude<.000001f,label+" differs when reversing scroll");}
    if(capture){for(int i=0;i<=4;i++)Save(route.LevelAt(p.s-Mathf.Lerp(far,near,i/4f)),label+"-"+i);}
    subjects++;
   }
   int Find(int island,PropKind kind)=>Enumerable.Range(0,world.Props.Count).Last(i=>route.IslandIndexAt(world.Props[i].s)==island&&world.Props[i].kind==kind&&world.Props[i].workMotion==WorldMapWorkMotion.None&&world.Props[i].s-route.MetersAt(1)>5.5f);
   Subject(Find(1,PropKind.Tree),"tree",true);Subject(Find(1,PropKind.Landmark),"building",true);Subject(Find(2,PropKind.Character),"character",true);
   Subject(Find(0,PropKind.Landmark),"opening-building",true);
   Save(1,"opening");Save(33,"christmas-island");
   // Ground props retain conformed geometry; river decorations retain their frontal pose.
   var fall=Enumerable.Range(0,world.Props.Count).FirstOrDefault(i=>world.Props[i].kind==PropKind.RiverFall);
   if(world.Props[fall].kind==PropKind.RiverFall){var p=world.Props[fall];map.PreviewAt(route.LevelAt(p.s-6));var slot=((IDictionary)typeof(WorldMapView).GetField("activeSlots",F).GetValue(world))[fall];var card=(Transform)slot.GetType().GetField("card").GetValue(slot);Check(Quaternion.Angle(card.rotation,world.transform.rotation*world.CameraModel.Rotation)<.03f,"Riverfall tilted by scenery lift");}
   type.GetMethod("Build",F).Invoke(preview,new object[]{360,780,1});map=(CurvedLevelMap)type.GetField("map",F).GetValue(preview);world=map.World;route=world.Route;world.TimeOverride=0;map.PreviewAt(1);
   Subject(Find(1,PropKind.Landmark),"narrow-building",true);
   Check(errors==0,errors+" Unity errors");File.WriteAllText(output+"/validation.txt","PASS: "+subjects+" actual manual/automatic tree/building/character subjects across phone ratios; artwork center and top never buried below local terrain during 48 forward/reverse samples; fully folded art rests on ground tangent; authored upright art offsets unchanged; continuous reversible rotation; frontal automatic standing pose; horizon activation folded/shadow-free; riverfall unchanged; first-island placement signatures unchanged; native five-stage capture sequences; zero Unity errors. No layout or player save writes.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=logger;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}EditorApplication.Exit(0);
 }
}
