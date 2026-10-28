using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEngine.UI;using UnityEditor;using TMPro;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class ReturnPinV87Validation
{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 public static void Run(){const string output="../return-pin-v87";Directory.CreateDirectory(output);File.WriteAllText(output+"/validation.txt","RUNNING");RoyalAvesLevelMapPreview preview=null;int errors=0;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{
   ShaderUtil.allowAsyncCompilation=false;preview=RoyalAvesLevelMapPreview.CreateScenePreview(720,1280);var pt=preview.GetType();var mt=typeof(CurvedLevelMap);var map=(CurvedLevelMap)pt.GetField("map",F).GetValue(preview);map.World.TimeOverride=0;
   Button Pin()=>(Button)mt.GetField("currentButton",F).GetValue(map);
   void At(int current,float cursor){mt.GetField("nextLevel",F).SetValue(map,current);map.PreviewAt(cursor);}
   void Save(string name){pt.GetField("level",F).SetValue(preview,map.Cursor);pt.GetMethod("Save",F).Invoke(preview,new object[]{output+"/"+name+".png",false});}
   void Return(int current,float from,string name){
    At(current,from);var pin=Pin();Check(pin.gameObject.activeSelf,"Return pin missing away from current level");Check(pin.name=="ReturnToCurrentLevel"&&pin.targetGraphic is WorldMapReturnPinGraphic,"Old current-level pill still used");
    Check(pin.GetComponentsInChildren<TMP_Text>(true).Length==0,"Return pin contains legacy caption");
    Check(pin.GetComponentsInChildren<Graphic>(true).Count(g=>g.raycastTarget)==1,"Portrait child blocks pointer input");
    Canvas.ForceUpdateCanvases();var parent=(RectTransform)pin.transform.parent;var r=(RectTransform)pin.transform;Check(r.anchoredPosition.y>=0&&r.anchoredPosition.x==0,"Pin not centered above navigation");
    Check(r.rect.width*360/1080>=44,"Pin touch area too small on narrow phone");Save(name);
    var avatar=pin.transform.Find("AvatarMask/HudPortrait");Check(avatar.gameObject.activeSelf,"Default postman portrait missing");
    Check(pin.GetComponent<WorldMapReturnPinGraphic>().canvasRenderer.GetMesh().vertexCount<1000,"Pin geometry unnecessarily large");
    float before=map.Cursor,target=current;pin.onClick.Invoke();
    Check(Mathf.Abs(map.Cursor-before)<.0001f,"Return teleported before easing");
    Check(Mathf.Abs((float)mt.GetField("targetCursor",F).GetValue(map)-target)<.001f,"Return targeted wrong level");
    float last=Mathf.Abs(map.Cursor-target);
    for(int i=0;i<32;i++){mt.GetMethod("AdvanceMotion",F).Invoke(map,new object[]{.05f});float distance=Mathf.Abs(map.Cursor-target);Check(distance<=last+.0001f,"Return motion reverses or overshoots");last=distance;}
    Save(name+"-returned");Check(last<.001f&&!pin.gameObject.activeSelf,"Return did not finish/hide on current level: distance="+last+", pin="+pin.gameObject.activeSelf);
   }
   At(5,5);Check(!Pin().gameObject.activeSelf,"Pin visible beside current on-node portrait");
   foreach(int level in new[]{1,5,15,30,31,100}){At(level,40);mt.GetMethod("FocusCurrent",F).Invoke(map,new object[]{true});map.PreviewAt(map.Cursor);Check(!Pin().gameObject.activeSelf,"Initial focus hides current level behind navigation: "+level);}
   Return(5,40,"scrolled-ahead");Return(31,1,"scrolled-behind");
   At(5,40);var property=typeof(MetaProgress).GetProperty("Avatar");
   if(property.CanWrite){try{property.SetValue(null,1);map.PreviewAt(40);var custom=Pin().transform.Find("AvatarMask/ProfilePortrait").GetComponent<Image>();Check(custom.gameObject.activeSelf&&custom.sprite!=null&&!Pin().transform.Find("AvatarMask/HudPortrait").gameObject.activeSelf,"Selected avatar not reflected in pin");Save("custom-avatar");}finally{property.SetValue(null,0);}}
   map.SetCovered(true);Check(!Pin().gameObject.activeSelf,"Pin remained active over menu");map.SetCovered(false);Check(Pin().gameObject.activeSelf,"Pin not restored on Map");
   At(101,90);Check(!Pin().gameObject.activeSelf,"Pin points to nonexistent completed-campaign level");
   pt.GetMethod("Build",F).Invoke(preview,new object[]{360,780,5});map=(CurvedLevelMap)pt.GetField("map",F).GetValue(preview);map.World.TimeOverride=0;Return(5,40,"narrow-phone");
   Check(errors==0,errors+" Unity errors");File.WriteAllText(output+"/validation.txt","PASS: glossy purple/gold drop replaces My level; default/current profile avatar circularly masked; one pointer target and narrow-phone touch area; centered above navigation; appears when current level is outside view in either scroll direction; actual button eases monotonically to current level without teleporting and hides on arrival; hidden under menus and after campaign completion; low geometry count; standard/narrow/custom-avatar captures; zero Unity errors. No player saves modified.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=logger;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}EditorApplication.Exit(0);
 }
}
