using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using RoyalAves.Gameplay;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Items;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
 public static class DynamiteCombinationsVerification
 {
  const string Key="MagicMail.DynamiteCombinationsVerification";
  static readonly int[] Modes={2,5,3,4,1};
  static int step,mode,blasts,delivered,marked,expectedColor,expectedMarks;
  static double began;
  static float activated;
  static Item source,package;
  static HashSet<Vector2Int> hits=new HashSet<Vector2Int>();
  static HashSet<Vector2Int> expected=new HashSet<Vector2Int>();
  static bool sawCargo,sawFuse,sourceFinished;
  static string failure;
  [InitializeOnLoadMethod]static void Init(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;}
  public static void Run()
  {
   if(!Application.dataPath.Replace('\\','/').Contains("/Logs/InkwellValidationProject/Assets"))
    throw new InvalidOperationException("Run only in the disposable InkwellValidationProject copy.");
   PlayerSettings.companyName="CodexValidation";PlayerSettings.productName="MagicMailDynamiteCombinations";
   Directory.CreateDirectory("Logs");File.WriteAllText("Logs/dynamite-combinations.result","RUNNING\n");
   SessionState.SetBool(Key,true);SessionState.SetInt(Key+".Index",0);
   SessionState.SetString(Key+".Deadline",DateTime.UtcNow.AddMinutes(8).ToString("O"));Open();
  }
  static void Open()
  {
   foreach(var k in LightBallLaboratory.OtherKeys)SessionState.SetBool(k,false);
   SessionState.SetBool(LightBallLaboratory.EnabledKey,false);
   SessionState.SetBool(ExplosivePackageLaboratory.EnabledKey,true);
   SessionState.SetInt(ExplosivePackageLaboratory.ModeKey,Modes[SessionState.GetInt(Key+".Index",0)]);
   step=0;ExplosivePackageLaboratoryTools.Open();
  }
  static void Require(bool ok,string reason){if(!ok)throw new Exception(reason);}
  static void Subscribe()
  {
   ItemStriped.DynamiteRocketCellHit-=RocketHit;ItemStriped.DynamiteRocketCellHit+=RocketHit;
   ItemDestroyAnimation.BlastApplied-=Blast;ItemDestroyAnimation.BlastApplied+=Blast;
   MarmaladeFly.DynamiteDelivered-=Delivered;MarmaladeFly.DynamiteDelivered+=Delivered;
   ItemMulticolor.ActualExplosion-=Hit;ItemMulticolor.ActualExplosion+=Hit;
   ItemMulticolor.AuditEvent-=Audit;ItemMulticolor.AuditEvent+=Audit;
  }
  static void Blast(Square cell,int radius,bool combined){blasts++;if(mode==3)failure="Package exploded at launch instead of delivery";}
  static void Delivered(Square cell,int count){delivered++;if(count<9)failure="Delivery area too small";}
  static void RocketHit(Square cell){if(step==1 && (mode==2||mode==5))hits.Add(new Vector2Int(cell.col,cell.row));}
  static void Hit(Item item)
  {
   if(step!=1 || item.currentType!=ItemsTypes.NONE || item.square==null)return;
   if((mode==2||mode==5) && Time.time-activated<.55f)failure="Rocket damaged before merge completed";
   if(!sourceFinished && mode!=2 && mode!=5)hits.Add(new Vector2Int(item.square.col,item.square.row));
  }
  static void Audit(ItemMulticolor owner,string action,Item target)
  {
   if(mode!=4 || step!=1)return;
   if(action=="Marked")
   {
    marked++;
    if(target.currentType!=ItemsTypes.PACKAGE)failure="Light Ball failed to convert target to dynamite";
   }
   if(action=="AllMarked" && marked!=expectedMarks)failure="Wrong number transformed: "+marked+" expected "+expectedMarks;
  }
  static void Tick()
  {
   if(!SessionState.GetBool(Key,false)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
   try
   {
    Require(DateTime.UtcNow<DateTime.Parse(SessionState.GetString(Key+".Deadline",""),null,System.Globalization.DateTimeStyles.RoundtripKind),"Total timeout");
    var m=LevelManager.THIS;
    if(!EditorApplication.isPlaying||m==null||!ExplosivePackageLaboratory.IsActive||m.gameStatus!=GameState.Playing)return;
    if(step==0)
    {
     if(m.StopFall||m.DragBlocked)return;
     mode=SessionState.GetInt(ExplosivePackageLaboratory.ModeKey,0);
     source=m.field.GetSquare(5,4,true).Item;package=m.field.GetSquare(4,4,true).Item;
     Require(source!=null && package!=null && package.currentType==ItemsTypes.PACKAGE,"Missing combo pair");
     Require(source.currentType==ExplosivePackageLaboratory.PartnerType(mode),"Wrong lab partner type");
     // Unequal color frequencies distinguish 'most common' from the package's own color.
     if(mode==4)
      foreach(var item in m.field.GetItems().Where(i=>i.currentType==ItemsTypes.NONE))
       item.color=(item.square.row+item.square.col)%2==0?2:item.square.col%2;
     hits.Clear();expected.Clear();blasts=delivered=marked=0;sawCargo=sawFuse=sourceFinished=false;failure=null;
     expectedColor=m.field.GetItems().Where(i=>i.currentType==ItemsTypes.NONE).GroupBy(i=>i.color)
      .OrderByDescending(g=>g.Count()).ThenBy(g=>g.Key).First().Key;
     expectedMarks=m.field.GetItems().Count(i=>i.currentType==ItemsTypes.NONE && i.color==expectedColor)+1;
     // Package lands at (5,4) after source swaps right to (4,4).
     foreach(var cell in m.field.squaresArray)
      if(cell!=null && (mode==1 || Mathf.Abs(cell.row-4)<=1 || Mathf.Abs(cell.col-5)<=1) && cell.Item!=source && cell.Item!=package)
       expected.Add(new Vector2Int(cell.col,cell.row));
     Subscribe();step=1;began=EditorApplication.timeSinceStartup;activated=Time.time;
     source.SwitchDirection(Vector3.right);return;
    }
    if(step!=1)return;
    Require(failure==null,failure);
    if(GameObject.Find("Dynamite burning fuse")!=null)sawFuse=true;
    foreach(var fly in UnityEngine.Object.FindObjectsByType<MarmaladeFly>())if(fly.IsFlying)
    {
     var renderer=fly.GetComponent<SpriteRenderer>();Require(renderer.sortingLayerName==RoyalAves.Effects.PowerUpEffectOrder.SortingLayer && renderer.sortingOrder>=100,"Propeller below safe");
     var cargo=fly.transform.Find("Carried dynamite");if(cargo!=null && cargo.gameObject.activeInHierarchy)sawCargo=true;
    }
    sourceFinished=source==null || !source.gameObject.activeInHierarchy;
    bool packageFinished=package==null || !package.gameObject.activeInHierarchy;
    if(sourceFinished && packageFinished && !m.StopFall)
    {
     if(mode==2||mode==5){Require(sawFuse,"No merge fuse observed");Require(hits.SetEquals(expected),"Cross coverage mismatch: "+hits.Count+" expected "+expected.Count);}
     if(mode==3){Require(sawCargo,"No carried dynamite");Require(delivered==1 && blasts==0,"Expected one delivered blast and no launch blast");}
     if(mode==4)Require(marked==expectedMarks && blasts>=expectedMarks,"Light Ball conversion/detonation incomplete");
     if(mode==1)Require(blasts==1 && hits.SetEquals(expected),"Double dynamite did not hit the full board once");
     File.AppendAllText("Logs/dynamite-combinations.result",$"PASS mode {mode}: hits={hits.Count}, blasts={blasts}, delivered={delivered}, marked={marked}, fuse={sawFuse}, cargo={sawCargo}; scripted real swap; gravity released.\n");
     int next=SessionState.GetInt(Key+".Index",0)+1;SessionState.SetInt(Key+".Index",next);
     if(next<Modes.Length){step=2;Open();}
     else{SessionState.SetBool(Key,false);File.AppendAllText("Logs/dynamite-combinations.result","PASS all modes; no physical pointer/audio/device/full victory-flow claim.\n");EditorApplication.Exit(0);}
    }
    else Require(EditorApplication.timeSinceStartup-began<60,"Mode timeout: "+mode+" stopFall="+m.StopFall);
   }
   catch(Exception e){SessionState.SetBool(Key,false);File.AppendAllText("Logs/dynamite-combinations.result","FAIL "+e+"\n");Debug.LogException(e);EditorApplication.Exit(1);}
  }
 }
}
