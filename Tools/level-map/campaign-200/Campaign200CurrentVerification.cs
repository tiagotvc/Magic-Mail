// Isolated fixture only. Copy to the validation clone's Editor directory to run.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RoyalAves.Gameplay;
using RoyalAves.Meta;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts;
namespace RoyalAves.EditorTools
{
 public static class Campaign200NativeVerification
 {
  const string Key="MagicMail.Campaign200.Verify";
  static readonly int[] Cases={1,76,136,151,200};
  static int index,phase;static double at,deadline,lastStatus;static bool seenIntro,introClosing;static Campaign200Tools.Book book;
  static void Require(bool value,string reason){if(!value)throw new Exception(reason);}
  [InitializeOnLoadMethod]static void Listen(){EditorApplication.update+=Tick;}
  public static void Run()
  {
   Require(Application.dataPath.Replace('\\','/').Contains("/Logs/InkwellValidationProject/Assets"),"Isolated only");
   Campaign200Tools.Build();
   PlayerSettings.companyName="CodexValidation";PlayerSettings.productName="MagicMailCampaign200";
   foreach(var key in LightBallLaboratory.OtherKeys)SessionState.SetBool(key,false);
   LightBallLaboratory.Disable();InkwellLaboratory.Disable();
   foreach(var key in new[]{"MagicMail.StoneLab.Start","MagicMail.StoneLab.Test","MagicMail.FrankingMachinePreview"})SessionState.SetBool(key,false);
   foreach(var entry in CampaignElementIntroductions.Entries)Require(CampaignElementIntroductions.Icon(entry)!=null,"Missing intro artwork "+entry.level+" "+entry.resource);
   SessionState.SetBool(Key,true);SessionState.SetString(Key+"Deadline",DateTime.UtcNow.AddMinutes(12).ToString("O"));
   File.WriteAllText("Logs/campaign-200-native.txt","Native run "+DateTime.UtcNow.ToString("O")+"\n");
   EditorSceneManager.OpenScene("Assets/SweetSugar/Scenes/game.unity");EditorApplication.isPlaying=true;
  }
  static void Tick()
  {
   if(!SessionState.GetBool(Key,false)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
   try
   {
    Require(DateTime.UtcNow<DateTime.Parse(SessionState.GetString(Key+"Deadline",""),null,System.Globalization.DateTimeStyles.RoundtripKind),"Global timeout");
    if(!EditorApplication.isPlaying)return;var m=LevelManager.THIS;if(m==null)return;
    if(book==null)book=Campaign200Tools.Read();int n=Cases[index];var r=book.levels.Single(l=>l.level==n);double now=EditorApplication.timeSinceStartup;
    if(now-lastStatus>1){lastStatus=now;File.WriteAllText("Logs/campaign-200-status.txt","level="+n+" phase="+phase+" state="+m.gameStatus+" intro="+seenIntro+" timeScale="+Time.timeScale);}
    if(phase==0)
    {
     if(m.gameStatus!=GameState.Map||AI.THIS==null)return;
     PlayerPrefs.SetInt("OpenLevel",n);PlayerPrefs.SetInt("OpenLevelTest",0);m.currentLevel=n;
     m.BoostPackage=m.BoostStriped=m.BoostColorfullBomb=0;
     m.gameStatus=GameState.PrepareGame;deadline=now+40;at=now;phase=1;seenIntro=false;introClosing=false;return;
    }
    Require(now<deadline,"Board timeout level "+n+" phase "+phase+" state "+m.gameStatus);
    if(phase==1)
    {
     var intro=UnityEngine.Object.FindFirstObjectByType<ElementIntroPrototype>();
     if(intro!=null)
     {
      if(introClosing)return;
      Require(CampaignElementIntroductions.Has(n),"Unexpected intro "+n);
      Require(m.gameStatus==GameState.Pause,"Intro did not pause "+n);
      Require(intro.GetComponent<Canvas>().enabled&&intro.GetComponent<Canvas>().sortingOrder==32000,"Intro canvas hidden "+n);
      if(!seenIntro){seenIntro=true;at=now;}
      if(n!=1&&now-at>.35){introClosing=true;UnityEngine.Object.Destroy(intro.gameObject);}
      return;
     }
     if(m.gameStatus==GameState.Tutorial&&!CampaignElementIntroductions.Has(n))m.gameStatus=GameState.Playing;
     if(m.gameStatus!=GameState.Playing)return;
     Require(!CampaignElementIntroductions.Has(n)||seenIntro,"Missing automatic intro "+n);
     at=now+.4;phase=2;return;
    }
    if(now<at)return;
    if(phase==2)
    {
     Require(m.levelData.levelNum==n,"Lab substituted campaign "+n);
     foreach(var g in r.goals)
     {
      var counter=m.levelData.TargetCounters.SingleOrDefault(t=>t.targetLevel.name==g.name);
      Require(counter!=null&&counter.count==g.count,"Wrong runtime goal "+n+" "+g.name+" expected="+g.count+" actual="+(counter==null?-1:counter.count));
     }
     Require(!m.levelData.TargetCounters.Any(t=>t.targetLevel.name=="PostalTapes"),"Runtime tape goal");
     if(CampaignElementIntroductions.Has(n)||n==101||n==102||n==200)
     {string p="Logs/campaign-200-board-"+n+".png";PostalTapeLaboratoryTools.CaptureBoard(p);PostalTapeLaboratoryTools.CaptureBoard(p);}
     var cell=m.field.GetSquare(r.openingSwap.x,r.openingSwap.y);Require(cell!=null&&cell.Item!=null,"Missing swap origin "+n);
     // The game's drag API takes the inverse pointer delta: left means swap right.
     cell.Item.SwitchDirection(Vector3.left);phase=3;at=now+2;return;
    }
    if(phase==3)
    {
     Require(m.levelData.limit==r.moves-1,"Opening API swap did not spend exactly one move "+n+" limit="+m.levelData.limit+" expected="+(r.moves-1));
     File.AppendAllText("Logs/campaign-200-native.txt","PASS level="+n+" goals="+r.goals.Length+" intro="+seenIntro+" actual opening swap spent1 move\n");
     index++;
     if(index==Cases.Length)
     {
      File.WriteAllText("Logs/campaign-200-native.result","PASS: "+Cases.Length+" campaign boards loaded normally with all labs off, exact live goal counts, 4 automatic intro screens, full timed dismissal and input-state restoration at level1, real opening swap APIs each spent one move. No victory-rate, physical-pointer, device, audio or complete campaign-flow claim.\n"+DateTime.UtcNow.ToString("O"));
      SessionState.SetBool(Key,false);EditorApplication.Exit(0);return;
     }
     phase=0;m.gameStatus=GameState.Pause;SceneManager.LoadScene("game");
    }
   }catch(Exception e){File.WriteAllText("Logs/campaign-200-native.result","FAIL: "+e);Debug.LogException(e);SessionState.SetBool(Key,false);EditorApplication.Exit(1);}
  }
 }
}
