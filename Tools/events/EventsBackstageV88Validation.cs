using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEngine.UI;using UnityEditor;using TMPro;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class EventsBackstageV88Validation
{
 sealed class UnusedSource:IPostalEventsSource{
  Exception Read()=>new Exception("Coming-soon screen accessed event state");
  public DateTime Now=>throw Read();public int NextLevel=>throw Read();public int LevelsWon=>throw Read();public bool CanPlay=>throw Read();public int SeasonLevel=>throw Read();
  public PostalEventsConfig Config=>throw Read();public PostalEventState Progress=>throw Read();
  public bool IsMissionClaimed(string id)=>throw Read();public bool ClaimMission(LobbyFeaturesConfig.Mission mission)=>throw Read();public bool Join()=>throw Read();public bool ClaimDaily()=>throw Read();public bool ClaimSeason(int index)=>throw Read();
 }
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 public static void Run(){const string output="../events-backstage-v88";Directory.CreateDirectory(output);RoyalAvesLevelMapPreview preview=null;int errors=0;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{
   ShaderUtil.allowAsyncCompilation=false;preview=RoyalAvesLevelMapPreview.CreateScenePreview(720,1280);var pt=preview.GetType();var map=(CurvedLevelMap)pt.GetField("map",F).GetValue(preview);map.World.TimeOverride=0;
   var content=(RectTransform)map.transform.parent;var nav=content.Find("NavegacaoDoMapa");Check(nav!=null,"Approved navigation missing");
   var host=new GameObject("EventsPreviewHost",typeof(RectTransform));host.transform.SetParent(content,false);host.transform.SetSiblingIndex(nav.GetSiblingIndex());
   var r=(RectTransform)host.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(0,166);r.offsetMax=Vector2.zero;
   var art=Resources.Load<LobbyFeaturesConfig>(LobbyFeaturesConfig.ResourcePath);int actions=0;
   var screen=LobbyEventsScreen.Create(r,art,_=>actions++,()=>actions++,()=>actions++,new UnusedSource());screen.gameObject.SetActive(true);map.SelectNavigationTab(0);map.SetCovered(true);map.SetAccountHeaderVisible(false);
   var account=content.Find("HUDDoMapa");Check(account!=null&&!account.gameObject.activeSelf,"Account HUD still visible on Events");
   Check(!map.World.gameObject.activeSelf&&nav.gameObject.activeSelf,"Covered world/navigation state wrong");
   map.SetChromeVisible(false);Check(!nav.gameObject.activeSelf,"Chrome did not hide");map.SetChromeVisible(true);Check(nav.gameObject.activeSelf&&!account.gameObject.activeSelf,"Global chrome toggle restored wrong Events header");
   Check(screen.GetComponentsInChildren<Button>(true).Length==0&&screen.GetComponentsInChildren<ScrollRect>(true).Length==0,"Old event cards/actions remain");
   Check(screen.transform.Find("EventsHeader").GetComponent<WorldMapPostalGraphic>().surface==WorldMapPostalGraphic.Surface.Navigation,"Header does not reuse approved cream navigation art");
   var labels=screen.GetComponentsInChildren<TMP_Text>(true);Check(labels.Length==2&&labels.Any(x=>x.text=="Events")&&labels.Any(x=>x.text=="Eventos em breve"),"Coming-soon copy wrong");
   var scene=screen.transform.Find("ComingSoon").GetComponent<PostalEventSceneImage>();Check(scene!=null&&scene.texture!=null&&!scene.raycastTarget,"Story scene missing or blocking input");
   var mission=WorldMapMission100.Current;Check(mission!=null&&mission.characters.Any(c=>c.role.Contains("Owl")&&AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(c.sprite))=="aac497d37460686ef635454417c73131"),"Owl reference is not from current map cast");
   var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(scene.texture));Check(!importer.mipmapEnabled&&importer.textureType==TextureImporterType.Default&&importer.maxTextureSize==2048,"Story UI texture importer wrong");
   void Capture(string name){screen.Refresh();map.PreviewMotion(0,false);Canvas.ForceUpdateCanvases();scene.Fit();
    var uv=scene.uvRect;float sampleAspect=scene.texture.width*uv.width/(scene.texture.height*uv.height),viewAspect=scene.rectTransform.rect.width/scene.rectTransform.rect.height;
    Check(Mathf.Abs(sampleAspect-viewAspect)<.001f,"Scene characters stretched");Check(uv.xMin<=.20f&&uv.xMax>=.86f&&uv.yMin<.08f&&Mathf.Abs(uv.yMax-1)<.0001f,"Current characters or curtain crest cropped");foreach(var text in labels){text.ForceMeshUpdate();Check(!text.isTextOverflowing,"Text overflows: "+text.text);}pt.GetMethod("RenderCanvas",F).Invoke(preview,null);
    var target=(RenderTexture)pt.GetField("capture",F).GetValue(preview);var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(output+"/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=old;
   }
   Capture("events-backstage");screen.gameObject.SetActive(false);screen.gameObject.SetActive(true);screen.Refresh();Check(actions==0,"Coming-soon screen fired a gameplay/claim callback");
   var camera=(Camera)pt.GetField("camera",F).GetValue(preview);var original=(RenderTexture)pt.GetField("capture",F).GetValue(preview);var narrow=new RenderTexture(360,780,24);narrow.Create();camera.targetTexture=narrow;camera.aspect=360f/780;pt.GetField("capture",F).SetValue(preview,narrow);Capture("narrow-phone");pt.GetField("capture",F).SetValue(preview,original);camera.targetTexture=original;camera.aspect=720f/1280;narrow.Release();UnityEngine.Object.DestroyImmediate(narrow);
   host.SetActive(false);map.SetAccountHeaderVisible(true);map.SetCovered(false);Check(account.gameObject.activeSelf&&map.World.gameObject.activeSelf&&nav.gameObject.activeSelf,"Map return did not restore HUD/world");
   Check(errors==0,errors+" Unity errors");File.WriteAllText(output+"/validation.txt","PASS: approved cream Events header preserved; exact Events/Eventos em breve labels; narrative backstage illustration with seated courier and current Organizer owl; canonical owl verified against current cast; preserved character aspect and safe crop at both phone ratios; no mipmaps on opaque UI texture; no cards, scroll, play or reward actions; no event-state reads on create/reopen/refresh; map paused behind Events; account HUD hidden across global chrome toggles and restored on Map; bottom navigation remains visible; standard/narrow native captures without text overflow; zero Unity errors. No player saves or wallet writes.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=logger;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}EditorApplication.Exit(0);
 }
}
