using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using RoyalAves.Gameplay;

namespace RoyalAves.EditorTools {
 public static class DynamitePreviewCapture {
  const string Key="MagicMail.DynamitePreviewCapture";
  static readonly int[] Modes={1,2,5,3,4};
  static bool recording;
  [InitializeOnLoadMethod] static void Init(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;}
  public static void Run(){
   if(!Application.dataPath.Replace('\\','/').Contains("/Logs/InkwellValidationProject/Assets"))throw new Exception("Isolated copy only");
   PlayerSettings.companyName="CodexValidation";PlayerSettings.productName="MagicMailDynamitePreviews";
   Directory.CreateDirectory("Logs/dynamite-previews-v1");
   SessionState.SetBool(Key,true);SessionState.SetInt(Key+".Index",0);
   SessionState.SetString(Key+".Deadline",DateTime.UtcNow.AddMinutes(12).ToString("O"));
   File.AppendAllText("Logs/dynamite-previews-v1/result.txt","RECAPTURE corrected cargo sprite\n");Open();
  }
  static void Open(){
   recording=false;foreach(var k in LightBallLaboratory.OtherKeys)SessionState.SetBool(k,false);
   LightBallLaboratory.Disable();SessionState.SetBool(ExplosivePackageLaboratory.EnabledKey,true);
   SessionState.SetInt(ExplosivePackageLaboratory.ModeKey,Modes[SessionState.GetInt(Key+".Index",0)]);
   ExplosivePackageLaboratoryTools.Open();
  }
  static void Tick(){
   if(!SessionState.GetBool(Key,false)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
   try{
    if(DateTime.UtcNow>DateTime.Parse(SessionState.GetString(Key+".Deadline",""),null,System.Globalization.DateTimeStyles.RoundtripKind))throw new Exception("Preview timeout");
    var m=LevelManager.THIS;
    if(recording||!EditorApplication.isPlaying||m==null||!ExplosivePackageLaboratory.IsActive||m.gameStatus!=GameState.Playing||m.StopFall||m.DragBlocked)return;
    int mode=Modes[SessionState.GetInt(Key+".Index",0)];
    var source=m.field.GetSquare(5,4,true).Item;
    if(source==null||source.currentType!=ExplosivePackageLaboratory.PartnerType(mode))return;
    if(mode==4)foreach(var item in m.field.GetItems().Where(i=>i.currentType==ItemsTypes.NONE))item.color=(item.square.row+item.square.col)%2==0?2:item.square.col%2;
    recording=true;
    var root=new GameObject("Preview frame recorder");
    root.AddComponent<DynamitePreviewFrames>().Begin(m,source,mode);
   }catch(Exception e){Fail(e);}
  }
  public static void Done(int mode,int frames){
   File.AppendAllText("Logs/dynamite-previews-v1/result.txt",$"CAPTURED mode {mode}: {frames} frames, 24 fps, native live board; scripted swap.\n");
   int next=SessionState.GetInt(Key+".Index",0)+1;SessionState.SetInt(Key+".Index",next);
   if(next<Modes.Length)Open();else{SessionState.SetBool(Key,false);File.AppendAllText("Logs/dynamite-previews-v1/result.txt","PASS all five captures. Board-only camera; no audio.\n");EditorApplication.Exit(0);}
  }
  public static void Fail(Exception e){SessionState.SetBool(Key,false);File.AppendAllText("Logs/dynamite-previews-v1/result.txt","FAIL "+e+"\n");Debug.LogException(e);EditorApplication.Exit(1);}
 }
 [DefaultExecutionOrder(32000)]
 public sealed class DynamitePreviewFrames:MonoBehaviour {
  Camera camera;RenderTexture target;Texture2D pixels;Item source;
  int mode,frame,total;string folder;bool complete;
  public void Begin(LevelManager m,Item item,int selected){
   source=item;mode=selected;frame=0;total=(mode==4?12:mode==3?8:6)*24;
   folder="Logs/dynamite-previews-v1/mode-"+mode;Directory.CreateDirectory(folder);
   Time.captureFramerate=24;Time.maximumDeltaTime=1f/24;Time.timeScale=1;
   var first=m.field.GetSquare(0,0).transform.position;var last=m.field.GetSquare(8,8).transform.position;
   camera=gameObject.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;
   camera.orthographicSize=Mathf.Max(Mathf.Abs(first.x-last.x),Mathf.Abs(first.y-last.y))*.5f+.9f;
   camera.transform.position=(first+last)*.5f+Vector3.back*20;
   camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.045f,.06f,.10f);
   target=new RenderTexture(640,640,24);pixels=new Texture2D(640,640,TextureFormat.RGB24,false);camera.targetTexture=target;
  }
  void LateUpdate(){
   if(complete||target==null)return;
   try{
    if(frame==18){
      var package=LevelManager.THIS.field.GetSquare(4,4,true).Item;
      foreach(var r in package.GetComponentsInChildren<SpriteRenderer>(true))File.AppendAllText("Logs/dynamite-previews-v1/cargo-diagnostic.txt",$"PACKAGE {r.name} sprite={r.sprite?.name} enabled={r.enabled} order={r.sortingOrder} scale={r.transform.lossyScale}\n");
      source.SwitchDirection(Vector3.right);
    }
    if(frame==35)foreach(var fly in UnityEngine.Object.FindObjectsByType<MarmaladeFly>())if(fly.IsFlying)
      foreach(var r in fly.GetComponentsInChildren<SpriteRenderer>(true))File.AppendAllText("Logs/dynamite-previews-v1/cargo-diagnostic.txt",$"FLY {r.name} sprite={r.sprite?.name} enabled={r.enabled} color={r.color} order={r.sortingOrder} layer={r.sortingLayerName} position={r.transform.position} bounds={r.bounds} scale={r.transform.lossyScale}\n");
    var previous=RenderTexture.active;
    try{camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,640,640),0,0);pixels.Apply();File.WriteAllBytes(folder+"/"+frame.ToString("D4")+".png",pixels.EncodeToPNG());}
    finally{RenderTexture.active=previous;}
    frame++;
    if(frame>=total){complete=true;Time.captureFramerate=0;EditorApplication.delayCall+=()=>DynamitePreviewCapture.Done(mode,frame);}
   }catch(Exception e){complete=true;DynamitePreviewCapture.Fail(e);}
  }
  void OnDestroy(){Time.captureFramerate=0;if(target!=null){target.Release();DestroyImmediate(target);}if(pixels!=null)DestroyImmediate(pixels);}
 }
}
