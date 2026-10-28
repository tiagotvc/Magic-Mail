using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;using UnityEngine.UI;using UnityEditor;using TMPro;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class EventsValidation
{
 sealed class Source:IPostalEventsSource{
  public DateTime Now{get;set;}=new DateTime(2026,12,5,12,30,0,DateTimeKind.Utc);public int Won;public int NextLevel=>Won+1;public int LevelsWon=>Won;public bool CanPlay=>true;
  public PostalEventsConfig Config{get;set;}public PostalEventState Progress{get;set;}=new PostalEventState();public HashSet<string> claimed=new HashSet<string>();public int coins;
  public int SeasonLevel=>NextLevel>=31?31:0;
  public bool Award(int n){coins+=n;return true;}public bool IsMissionClaimed(string id)=>claimed.Contains(id);
  public bool ClaimMission(LobbyFeaturesConfig.Mission m){if(m==null||Won<m.goal||!claimed.Add(m.id))return false;return Award(m.coins);}
  public bool Join()=>Progress.Join(NextLevel,Config,Now);public bool ClaimDaily()=>Progress.ClaimDaily(Config,Now,Award);public bool ClaimSeason(int index)=>Progress.ClaimSeason(index,Config,Now,Award);
 }
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void Rules(PostalEventsConfig cfg){var s=new Source{Config=cfg};var state=s.Progress;state.Observe(cfg,s.Now);
  Check(!s.ClaimDaily()&&s.coins==0,"Premature daily reward");
  Check(state.RecordWin(1,false,cfg,s.Now)&&!state.RecordWin(1,false,cfg,s.Now),"Replay added progress");state.RecordWin(2,false,cfg,s.Now);state.RecordWin(3,false,cfg,s.Now);
  Check(state.dailyWins==3&&s.ClaimDaily()&&!s.ClaimDaily()&&s.coins==cfg.dailyCoins,"Daily reward duplicated/lost");
  state=JsonUtility.FromJson<PostalEventState>(JsonUtility.ToJson(state));s.Progress=state;Check(!s.ClaimDaily(),"Reload allowed duplicate claim");
  s.Now=s.Now.Date.AddDays(1);state.Observe(cfg,s.Now);Check(state.dailyWins==0&&!state.dailyClaimed&&!state.RecordWin(3,false,cfg,s.Now),"Daily rollover/replay failed");
  Check(!state.ClaimDaily(cfg,s.Now,_=>throw new Exception("Unready award")),"Empty next-day claim");
  Check(!state.Join(30,cfg,s.Now)&&state.Join(31,cfg,s.Now),"Season unlock/join failed");state.RecordWin(4,false,cfg,s.Now);Check(state.seasonWins==0,"Normal island advanced Christmas story");
  for(int n=5;n<=7;n++)state.RecordWin(n,true,cfg,s.Now);Check(state.seasonWins==3&&s.ClaimSeason(0)&&!s.ClaimSeason(0),"Season milestone duplicated/lost");
  state=JsonUtility.FromJson<PostalEventState>(JsonUtility.ToJson(state));Check(state.joined&&state.seasonWins==3&&state.seasonClaims.Contains(0),"Season persistence failed");
  Check(!state.ClaimSeason(1,cfg,s.Now,_=>true),"Premature season milestone");
  int daily=state.dailyWins;Check(state.RecordSeasonWin(31,true,cfg,s.Now)&&!state.RecordSeasonWin(31,true,cfg,s.Now)&&state.seasonWins==4&&state.dailyWins==daily,"Season replay eligibility/deduplication failed");
  var unavailable=ScriptableObject.Instantiate(cfg);unavailable.seasonId="next-season";state.Observe(unavailable,s.Now);Check(!state.joined&&state.seasonWins==0&&state.seasonClaims.Count==0&&state.lastCreditedLevel==7,"New season rollover failed");
  unavailable.endsUtc="2026-01-01T00:00:00Z";Check(!unavailable.IsActive(s.Now)&&!state.Join(31,unavailable,s.Now),"Expired event joined");unavailable.endsUtc="invalid date";Check(!unavailable.IsActive(s.Now),"Invalid schedule activated");UnityEngine.Object.DestroyImmediate(unavailable);
  var denied=new PostalEventState{day=s.Now.ToString("yyyy-MM-dd"),season=cfg.seasonId,dailyWins=3};Check(!denied.ClaimDaily(cfg,s.Now,_=>false)&&!denied.dailyClaimed,"Unavailable wallet consumed reward");
  Check(PostalEvents.IsChristmasLevel(31)&&!PostalEvents.IsChristmasLevel(16)&&!PostalEvents.IsChristmasLevel(46),"Wrong Christmas island attribution");
 }
 public static void Run(){const string output="../events-v81";Directory.CreateDirectory(output);RoyalAvesLevelMapPreview preview=null;PostalEventsConfig cfg=null;int errors=0;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{
   ShaderUtil.allowAsyncCompilation=false;cfg=UnityEngine.Object.Instantiate(Resources.Load<PostalEventsConfig>(PostalEventsConfig.ResourcePath));Check(cfg!=null,"Events configuration missing");Check(!cfg.seasonEnabled,"Season must be disabled for the delivery-only release");
   var rules=UnityEngine.Object.Instantiate(cfg);rules.seasonEnabled=true;Rules(rules);UnityEngine.Object.DestroyImmediate(rules);
   preview=RoyalAvesLevelMapPreview.CreateScenePreview(720,1280);var pt=preview.GetType();var map=(CurvedLevelMap)pt.GetField("map",F).GetValue(preview);map.World.TimeOverride=0;
   var content=(RectTransform)map.transform.parent;var nav=content.Find("Nav");var host=new GameObject("EventsPreviewHost",typeof(RectTransform));host.transform.SetParent(content,false);host.transform.SetSiblingIndex(nav.GetSiblingIndex());
   var r=(RectTransform)host.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(0,166);r.offsetMax=new Vector2(0,-232);
   var art=Resources.Load<LobbyFeaturesConfig>(LobbyFeaturesConfig.ResourcePath);var source=new Source{Config=cfg};int played=0,closed=0;
   var screen=LobbyEventsScreen.Create(r,art,n=>played=n,()=>closed++,()=>{},source);screen.gameObject.SetActive(true);map.SelectNavigationTab(0);map.SetCovered(true);
   Check(!map.World.gameObject.activeSelf,"Covered map is still active");
   Button Find(string name)=>screen.GetComponentsInChildren<Button>(true).First(x=>x.name==name);
   void Capture(string name){screen.Refresh();var coinSource=content.Find("HUD/Coins")?.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();if(coinSource!=null)coinSource.text=source.coins.ToString();map.PreviewMotion(0,false);foreach(var text in screen.GetComponentsInChildren<TMP_Text>(true))text.ForceMeshUpdate();Canvas.ForceUpdateCanvases();pt.GetMethod("RenderCanvas",F).Invoke(preview,null);
    var target=(RenderTexture)pt.GetField("capture",F).GetValue(preview);var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(output+"/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=old;
   }
   Capture("fresh");Check(!screen.GetComponentsInChildren<Button>(true).Any(x=>x.name=="SeasonAction")&&!screen.GetComponentsInChildren<TMP_Text>(true).Any(x=>x.text.Contains("Christmas")),"Season content still appears");
   Find("DailyAction").onClick.Invoke();Check(played==1,"Play button did not select the next level");Find("DeliveryAction").onClick.Invoke();Check(played==1,"Featured delivery selected the wrong level");Find("BackToMap").onClick.Invoke();Check(closed==1,"Map button did not close events");
   source.Won=3;source.Progress.dailyWins=3;Capture("rewards-ready");Find("DailyAction").onClick.Invoke();Find("DeliveryAction").onClick.Invoke();Check(source.coins==cfg.dailyCoins+100&&source.Progress.dailyClaimed,"UI did not award daily/featured delivery coins");
   Find("Claim-first").onClick.Invoke();Check(source.coins==cfg.dailyCoins+100,"Featured and journey cards duplicated the same reward");
   Check(!Find("DailyAction").interactable&&!Find("Claim-first").interactable,"Claim button not disabled");Capture("collected");
   // Check phone layout separately in the same isolated scene, using real Canvas scaling.
   var camera=(Camera)pt.GetField("camera",F).GetValue(preview);var original=(RenderTexture)pt.GetField("capture",F).GetValue(preview);var narrow=new RenderTexture(360,780,24);narrow.Create();camera.targetTexture=narrow;camera.aspect=360f/780;pt.GetField("capture",F).SetValue(preview,narrow);Capture("narrow-phone");pt.GetField("capture",F).SetValue(preview,original);camera.targetTexture=original;camera.aspect=720f/1280;narrow.Release();UnityEngine.Object.DestroyImmediate(narrow);
   source.Won=10;foreach(var mission in art.missions)source.claimed.Add(mission.id);Capture("journey-completed");Check(!Find("DeliveryAction").interactable,"Completed delivery can still be claimed");
   source.Now=source.Now.Date.AddDays(1);source.Progress.Observe(cfg,source.Now);screen.Refresh();Check(!source.Progress.dailyClaimed&&source.Progress.dailyWins==0,"UI daily reset failed");
   map.SetCovered(false);Check(map.World.gameObject.activeSelf,"Map did not resume");if(errors!=0)throw new Exception(errors+" Unity errors");
   File.WriteAllText(output+"/validation.txt","PASS: delivery-only configuration with no Christmas content; featured delivery progression, Play and Collect; shared reward deduplication with journey milestones; daily rollover and JSON persistence; missing-wallet rollback; disabled seasonal rules retained; native fresh/ready/collected/completed/narrow-phone UI; actual Play/Map/Collect callbacks; map suspend/resume; zero Unity errors. Test wallet and state are in memory; no player saves or currency modified.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=logger;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);if(cfg!=null)UnityEngine.Object.DestroyImmediate(cfg);}EditorApplication.Exit(0);
 }
}
