using System;
using System.IO;
using System.Linq;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Blocks;
using UnityEditor;
using UnityEngine;
namespace RoyalAves.EditorTools {
public static class DynamiteBlastVerification {
 const string Key="MagicMail.DynamiteVerification";
 static int stage, blasts,hits,test;static double began;static float impact,scale,maxScale;static Item source;static string failure;
 [InitializeOnLoadMethod]static void Init(){EditorApplication.update-=Poll;EditorApplication.update+=Poll;}
 static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
 static Item Spawn(LevelManager m,int x,int y,ItemsTypes type){var c=m.field.GetSquare(x,y,true);foreach(var s in c.subSquares.ToArray()){if(s!=c){s.gameObject.SetActive(false);}}c.subSquares.Clear();c.type=SquareTypes.EmptySquare;if(c.Item!=null)c.Item.Hide(true);c.item=null;var item=c.GenItem(false,type,noAnim:true);c.Item=item;return item;}
 static void Blast(Square c,int radius,bool combined){Require(Resources.Load<Material>("Effects/DynamiteBlast")?.shader.isSupported==true,"Blast shader missing/unsupported");blasts++;impact=Time.time;if(radius!=(test==1?4:3)||combined!=(test==1))failure="Wrong combined radius";ScreenCapture.CaptureScreenshot("Logs/bomb-impact-"+test+".png");}
 static void HitStone(Square cell){if(stage==2){if(blasts==0)failure="Stone broke before blast";hits++;}}
 static void HitItem(Item item){if(stage==2 && item.currentType==ItemsTypes.NONE){if(blasts==0)failure="Piece broke before blast";hits++;}}
 static void Finish(string result){Time.maximumDeltaTime=SessionState.GetFloat(Key+".MaxDelta",.3333333f);SessionState.SetBool(Key,false);ItemDestroyAnimation.BlastApplied-=Blast;RoyalAves.Gameplay.EnchantedStone.HitApplied-=HitStone;ItemMulticolor.ActualExplosion-=HitItem;File.AppendAllText("Logs/bomb-native.result",result+"\n");StoneCampaign101Tools.Open();}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;try{
 if(File.Exists("Logs/bomb.request")){bool visual=File.ReadAllText("Logs/bomb.request").Trim()=="visual";File.Delete("Logs/bomb.request");SessionState.SetBool(Key+".Visual",visual);SessionState.SetFloat(Key+".MaxDelta",Time.maximumDeltaTime);test=visual?1:0;SessionState.SetInt(Key+".Test",test);stage=0;began=EditorApplication.timeSinceStartup;SessionState.SetBool(Key,true);File.WriteAllText("Logs/bomb-native.result","RUNNING\n");ItemDestroyAnimation.BlastApplied+=Blast;RoyalAves.Gameplay.EnchantedStone.HitApplied+=HitStone;ItemMulticolor.ActualExplosion+=HitItem;StoneCampaign101Tools.Open();return;}
 if(!SessionState.GetBool(Key,false))return;
 EditorApplication.QueuePlayerLoopUpdate();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
 var m=LevelManager.THIS;if(!EditorApplication.isPlaying||m?.field==null||m.gameStatus!=GameState.Playing)return;
 if(stage==0 && !m.StopFall&&!m.DragBlocked){
Time.maximumDeltaTime=1f/30;test=SessionState.GetInt(Key+".Test",0);ItemDestroyAnimation.BlastApplied-=Blast;ItemDestroyAnimation.BlastApplied+=Blast;RoyalAves.Gameplay.EnchantedStone.HitApplied-=HitStone;RoyalAves.Gameplay.EnchantedStone.HitApplied+=HitStone;ItemMulticolor.ActualExplosion-=HitItem;ItemMulticolor.ActualExplosion+=HitItem;
 var hold=new GameObject("Bomb test setup");hold.AddComponent<GameBlocker>();
 // Alternate normal pieces and runes; retain the bottom rune outside both blast areas to prevent a win.
 for(int y=0;y<9;y++)for(int x=0;x<9;x++)if((x+y)%2==0){var item=Spawn(m,x,y,ItemsTypes.NONE);item.color=(x+2*y)%5;}
 foreach(var item in m.field.GetItems().ToArray())if(item.currentType!=ItemsTypes.NONE)Spawn(m,item.square.col,item.square.row,ItemsTypes.NONE);
 source=Spawn(m,4,4,ItemsTypes.PACKAGE);if(test==1)Spawn(m,3,4,ItemsTypes.PACKAGE);
 UnityEngine.Object.Destroy(hold);stage=1;began=EditorApplication.timeSinceStartup;return;}
 if(stage==1 && !m.StopFall){blasts=hits=0;impact=0;failure=null;scale=source.GetSpriteRenderer().transform.localScale.x;maxScale=1;stage=2;began=EditorApplication.timeSinceStartup;if(test==1)source.SwitchDirection(Vector3.right);else source.ActivateFromSwap();return;}
 if(stage==2){Require(failure==null,failure);if(source!=null&&source.gameObject.activeInHierarchy&&blasts==0){maxScale=Mathf.Max(maxScale,source.GetSpriteRenderer().transform.localScale.x/scale);if(maxScale>1.9f)ScreenCapture.CaptureScreenshot("Logs/bomb-merge.png");}
 if(blasts>0&&Time.time-impact>.07f&&Time.time-impact<.16f)ScreenCapture.CaptureScreenshot("Logs/bomb-fire-"+test+".png");
 if(blasts>0&&Time.time-impact<.20f)Require(m.StopFall,"Gravity released during impact emphasis");
 if(blasts>0&&Time.time-impact>.85f){Require(blasts==1,"Multiple blasts for one activation");Require(hits>=(test==1?65:40),"Insufficient affected cells: "+hits);Require(test!=1||maxScale>1.8f,"Merged package did not grow");Require(!UnityEngine.Object.FindObjectsByType<RoyalAves.Effects.DynamiteBlastEffect>().Any(),"Effect did not clean up");File.AppendAllText("Logs/bomb-native.result",$"PASS case {test}: one blast; {hits} stone/piece hits; radius {(test==1?4:3)}; max scale {maxScale:F2}; gravity held at impact.\n");test++;SessionState.SetInt(Key+".Test",test);if(test==3 || SessionState.GetBool(Key+".Visual",false)){Finish(SessionState.GetBool(Key+".Visual",false)?"PASS: final visual, actual swapped pair, one larger blast, supported shader and automatic cleanup. No physical pointer/audio/full victory-flow claim.":"PASS: single, actual swapped pair, repeated single. No physical pointer, audio or full victory-flow claim.");return;}stage=0;began=EditorApplication.timeSinceStartup;StoneCampaign101Tools.Open();}
 else if(EditorApplication.timeSinceStartup-began>20)Finish("FAIL timeout, blasts="+blasts+", hits="+hits+", StopFall="+m.StopFall);}
 }catch(Exception ex){Finish("FAIL "+ex);Debug.LogException(ex);}}
}}
