using System;using System.IO;using UnityEditor;using UnityEngine;using UnityEngine.UI;using RoyalAves.Meta;
// Runs only in the isolated preview project. Real Play lifecycle verifies pause/reopen behavior.
public static class EventsSurpriseV92PlayValidation
{
 static GameObject host;static LobbyEventsScreen screen;static PostalEventSurpriseMotion motion;
 static double deadline;static int phase;static float previous;static int errors;
 static bool oldOptionsEnabled;static EnterPlayModeOptions oldOptions;
 public static void Run()
 {
  oldOptionsEnabled=EditorSettings.enterPlayModeOptionsEnabled;oldOptions=EditorSettings.enterPlayModeOptions;
  EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload|EnterPlayModeOptions.DisableSceneReload;
  Application.logMessageReceived+=Log;EditorApplication.playModeStateChanged+=State;
  EditorApplication.EnterPlaymode();
 }
 static void Log(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;}
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void State(PlayModeStateChange state)
 {
  if(state==PlayModeStateChange.EnteredPlayMode)
  {
   try{
    host=new GameObject("IsolatedEventsPlayTest",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
    host.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
    var scaler=host.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);
    screen=LobbyEventsScreen.Create((RectTransform)host.transform,Resources.Load<LobbyFeaturesConfig>(LobbyFeaturesConfig.ResourcePath),null,null,null);
    screen.gameObject.SetActive(true);motion=screen.GetComponentInChildren<PostalEventSurpriseMotion>();
    Time.timeScale=0;phase=0;deadline=EditorApplication.timeSinceStartup+.65;
    EditorApplication.update+=Tick;
   }catch(Exception e){Finish(e);}
  }
  if(state==PlayModeStateChange.EnteredEditMode)
  {
   EditorSettings.enterPlayModeOptionsEnabled=oldOptionsEnabled;EditorSettings.enterPlayModeOptions=oldOptions;
   EditorApplication.playModeStateChanged-=State;Application.logMessageReceived-=Log;
   EditorApplication.Exit(errors==0?0:1);
  }
 }
 static void Tick()
 {
  if(EditorApplication.timeSinceStartup<deadline)return;
  try{
   if(phase==0){Check(motion.Elapsed>.25f,"Animation did not run with timeScale zero");previous=motion.Elapsed;screen.gameObject.SetActive(false);phase=1;deadline=EditorApplication.timeSinceStartup+.4;return;}
   if(phase==1){Check(motion.Elapsed==previous,"Hidden Events kept animating");screen.gameObject.SetActive(true);Check(motion.Elapsed==0,"Real OnEnable did not reset cycle");phase=2;deadline=EditorApplication.timeSinceStartup+.5;return;}
   Check(motion.Elapsed>.2f&&motion.Elapsed<previous+1,"Animation failed to restart");Check(errors==0,"Unity runtime errors: "+errors);Finish(null);
  }catch(Exception e){Finish(e);}
 }
 static void Finish(Exception failure)
 {
  EditorApplication.update-=Tick;Time.timeScale=1;if(host!=null)UnityEngine.Object.DestroyImmediate(host);
  Directory.CreateDirectory("../events-surprise-v92");File.WriteAllText("../events-surprise-v92/play-validation.txt",failure==null?"PASS: real Play Mode animation advances with timeScale zero, stops while Events is hidden, resets on reopen and resumes; zero Unity errors. Isolated test does not write player progress.\n":"FAIL: "+failure);
  if(failure!=null){errors++;Debug.LogException(failure);}EditorApplication.ExitPlaymode();
 }
}
