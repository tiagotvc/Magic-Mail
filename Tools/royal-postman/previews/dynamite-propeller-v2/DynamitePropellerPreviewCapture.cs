using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using RoyalAves.Gameplay;

namespace RoyalAves.EditorTools {
 public static class DynamitePropellerPreviewCapture {
  const string Key="MagicMail.DynamitePropellerPreviewCapture";
  static readonly int[] Modes={3,6,8};
  static bool recording;
  [InitializeOnLoadMethod] static void Init(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;}
  public static void Run(){
   if(!Application.dataPath.Replace('\\','/').Contains("/Logs/InkwellValidationProject/Assets"))throw new Exception("Isolated copy only");
   PlayerSettings.companyName="CodexValidation";PlayerSettings.productName="MagicMailDynamitePreviews";
   Directory.CreateDirectory("Logs/dynamite-propeller-v2");
   SessionState.SetBool(Key,true);SessionState.SetInt(Key+".Index",0);
   SessionState.SetString(Key+".Deadline",DateTime.UtcNow.AddMinutes(12).ToString("O"));
   File.AppendAllText("Logs/dynamite-propeller-v2/result.txt","RECAPTURE corrected cargo sprite\n");Open();
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
    root.AddComponent<DynamitePropellerPreviewFrames>().Begin(m,source,mode);
   }catch(Exception e){Fail(e);}
  }
  public static void Done(int mode,int frames){
   File.AppendAllText("Logs/dynamite-propeller-v2/result.txt",$"CAPTURED mode {mode}: {frames} frames, 24 fps, native live board; scripted swap.\n");
   int next=SessionState.GetInt(Key+".Index",0)+1;SessionState.SetInt(Key+".Index",next);
   if(next<Modes.Length)Open();else{SessionState.SetBool(Key,false);File.AppendAllText("Logs/dynamite-propeller-v2/result.txt","PASS requested captures. Board-only camera; no audio.\n");EditorApplication.Exit(0);}
  }
  public static void Fail(Exception e){SessionState.SetBool(Key,false);File.AppendAllText("Logs/dynamite-propeller-v2/result.txt","FAIL "+e+"\n");Debug.LogException(e);EditorApplication.Exit(1);}
 }
 [DefaultExecutionOrder(32000)]
 public sealed class DynamitePropellerPreviewFrames:MonoBehaviour {
  Camera camera;RenderTexture target;Texture2D pixels;Item source;
  int mode,frame,total;string folder;bool complete;
  int deliveries,expectedHits,dynamiteDeliveries;bool sawCargo;
  int orbitChanges;float previousOrbit;
  int combinedStarted,arrivals,maxGroup;bool sawSpread;
  global::System.Collections.Generic.HashSet<Vector2Int> arrivalCells=new global::System.Collections.Generic.HashSet<Vector2Int>();
  void GroupStarted(SweetSugar.Scripts.Blocks.Square cell,int count){combinedStarted++;if(count!=3)throw new Exception("Expected three propellers");}
  void GroupArrived(SweetSugar.Scripts.Blocks.Square cell){arrivals++;if(cell==null || !arrivalCells.Add(new Vector2Int(cell.col,cell.row)))throw new Exception("Repeated group target");}


  global::System.Collections.Generic.HashSet<Vector2Int> hitCells=new global::System.Collections.Generic.HashSet<Vector2Int>();
  void Delivered(SweetSugar.Scripts.Blocks.Square origin,bool horizontal,int count){
   if(horizontal!=(mode==6))throw new Exception("Wrong delivered rocket orientation");
   deliveries++;expectedHits=count;
  }
  void Hit(SweetSugar.Scripts.Blocks.Square origin,SweetSugar.Scripts.Blocks.Square cell,bool horizontal){
   if(horizontal?cell.row!=origin.row:cell.col!=origin.col)throw new Exception("Hit outside landing axis");
   if(!hitCells.Add(new Vector2Int(cell.col,cell.row)))throw new Exception("Duplicate cell hit");
  }
  void DynamiteDelivered(SweetSugar.Scripts.Blocks.Square origin,int count){dynamiteDeliveries++;if(mode==3 && count!=25)throw new Exception("Expected complete 5x5 landing blast, got "+count);}
  void Blast(SweetSugar.Scripts.Blocks.Square cell,int radius,bool combined){if(mode==3 && dynamiteDeliveries==0)throw new Exception("Dynamite exploded before landing");}

  public void Begin(LevelManager m,Item item,int selected){
   source=item;mode=selected;frame=0;
   ItemDestroyAnimation.BlastApplied+=Blast;
   ItemMarmalade.CombinedPropellersStarted+=GroupStarted;MarmaladeFly.CombinedPropellerArrived+=GroupArrived;
   MarmaladeFly.RocketDelivered+=Delivered;MarmaladeFly.RocketDeliveryCellHit+=Hit;MarmaladeFly.DynamiteDelivered+=DynamiteDelivered;
total=(mode==4?12:mode==3?8:6)*24;
   folder="Logs/dynamite-propeller-v2/mode-"+mode;Directory.CreateDirectory(folder);
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
      foreach(var r in package.GetComponentsInChildren<SpriteRenderer>(true))File.AppendAllText("Logs/dynamite-propeller-v2/cargo-diagnostic.txt",$"PACKAGE {r.name} sprite={r.sprite?.name} enabled={r.enabled} order={r.sortingOrder} scale={r.transform.lossyScale}\n");
      source.SwitchDirection(Vector3.right);
    }
    var group=UnityEngine.Object.FindObjectsByType<MarmaladeFly>().Where(f=>f.IsCombinedPropeller && f.IsFlying).ToArray();
    maxGroup=Mathf.Max(maxGroup,group.Length);
    if(group.Length==3 && Vector3.Distance(group[0].transform.position,group[1].transform.position)>.3f)sawSpread=true;
    foreach(var g in group)if(g.GetComponent<SpriteRenderer>().sortingOrder<100)throw new Exception("Group below safe");
    foreach(var flyer in UnityEngine.Object.FindObjectsByType<MarmaladeFly>())if(flyer.IsFlying){
     var load=flyer.transform.Find((mode==6 || mode==7)?"Carried rocket":"Carried dynamite");
     if(load!=null && load.GetComponent<SpriteRenderer>().enabled){sawCargo=true;
      if((mode==3 || mode==6 || mode==7)){
       float orbit=load.localPosition.x;
       if(previousOrbit*orbit<0)orbitChanges++;
       previousOrbit=orbit;
       var rotor=flyer.transform.Find("Orbiting combo propeller");
       if(rotor==null || rotor.GetComponent<SpriteRenderer>().sortingOrder<100)throw new Exception("Missing front-layer orbital propeller");
      }
      if(load.GetComponent<SpriteRenderer>().sortingOrder<100)throw new Exception("Cargo below safe");}
    }
    if(frame==35)foreach(var fly in UnityEngine.Object.FindObjectsByType<MarmaladeFly>())if(fly.IsFlying)
      foreach(var r in fly.GetComponentsInChildren<SpriteRenderer>(true))File.AppendAllText("Logs/dynamite-propeller-v2/cargo-diagnostic.txt",$"FLY {r.name} sprite={r.sprite?.name} enabled={r.enabled} color={r.color} order={r.sortingOrder} layer={r.sortingLayerName} position={r.transform.position} bounds={r.bounds} scale={r.transform.lossyScale}\n");
    var previous=RenderTexture.active;
    try{camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,640,640),0,0);pixels.Apply();File.WriteAllBytes(folder+"/"+frame.ToString("D4")+".png",pixels.EncodeToPNG());}
    finally{RenderTexture.active=previous;}
    frame++;
    if(frame>=total){
      if((mode==3 || mode==6 || mode==7) && orbitChanges<4)throw new Exception("Combo did not orbit during flight: "+orbitChanges);
      if((mode==6 || mode==7) && (deliveries!=1 || expectedHits!=9 || hitCells.Count!=9 || !sawCargo))throw new Exception($"Delivery mismatch mode={mode} deliveries={deliveries} hits={hitCells.Count}/{expectedHits} cargo={sawCargo}");
      if(mode==8 && (combinedStarted!=1 || arrivals!=3 || maxGroup!=3 || !sawSpread))throw new Exception($"Group mismatch: {combinedStarted}, arrivals={arrivals}, max={maxGroup}, spread={sawSpread}");
      if(mode==8 && UnityEngine.Object.FindObjectsByType<MarmaladeFly>().Any(f=>f.IsCombinedPropeller && f.IsFlying))throw new Exception("Group failed to clean up");
      if(mode==3 && (dynamiteDeliveries!=1 || !sawCargo))throw new Exception("Dynamite regression");
      if(LevelManager.THIS.StopFall)throw new Exception("Gravity still held after sequence");
      File.AppendAllText("Logs/dynamite-propeller-v2/behavior.result",$"PASS mode {mode}: rocket deliveries={deliveries}, unique axis hits={hitCells.Count}, dynamite deliveries={dynamiteDeliveries}, groups={combinedStarted}, arrivals={arrivals}, maxGroup={maxGroup}, spread={sawSpread}, cargo={sawCargo}, orbit side changes={orbitChanges}, gravity released.\n");
      complete=true;Time.captureFramerate=0;EditorApplication.delayCall+=()=>DynamitePropellerPreviewCapture.Done(mode,frame);}
   }catch(Exception e){complete=true;DynamitePropellerPreviewCapture.Fail(e);}
  }
  void OnDestroy(){ItemDestroyAnimation.BlastApplied-=Blast;ItemMarmalade.CombinedPropellersStarted-=GroupStarted;MarmaladeFly.CombinedPropellerArrived-=GroupArrived;MarmaladeFly.RocketDelivered-=Delivered;MarmaladeFly.RocketDeliveryCellHit-=Hit;MarmaladeFly.DynamiteDelivered-=DynamiteDelivered;Time.captureFramerate=0;if(target!=null){target.Release();DestroyImmediate(target);}if(pixels!=null)DestroyImmediate(pixels);}
 }
}
