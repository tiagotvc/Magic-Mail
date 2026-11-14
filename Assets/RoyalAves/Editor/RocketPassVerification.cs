using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    // Real Play diagnostics: actual projectile positions versus actual stone/item impacts.
    public static class RocketPassVerification
    {
        const string Request="Logs/rocket-pass.request";
        const string Key="MagicMail.RocketPassVerification";
        static readonly string[] Cases={"vertical-stones","horizontal-mixed","vertical-pieces","horizontal-pieces","two-rockets","rocket-dynamite","horizontal-offset","vertical-offset"};
        static readonly List<string> timeline=new List<string>();
        static readonly List<string> report=new List<string>();
        static readonly Dictionary<Item,Vector3> positions=new Dictionary<Item,Vector3>();
        static bool observing, released;
        static int caseIndex, hits;
        static float firstHit,lastHit,releaseAt;
        static double started;
        static string failure;

        [InitializeOnLoadMethod]
        static void Init()
        {
            EditorApplication.update-=Poll;EditorApplication.update+=Poll;
            started=EditorApplication.timeSinceStartup;
        }
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        static void HitStone(SweetSugar.Scripts.Blocks.Square cell)=>Impact(cell.transform.position,"stone",cell.col,cell.row);
        static void HitItem(Item item)
        {
            if(item.currentType==ItemsTypes.NONE && item.field==LevelManager.THIS?.field)
            {
                positions.Remove(item); // A pooled object reused during refill is not an original survivor.
                Impact(item.transform.position,"piece",item.square.col,item.square.row);
            }
        }
        static void Impact(Vector3 cell,string kind,int col,int row)
        {
            if(!observing || released || !LevelManager.THIS.StopFall)return;
            // Locate a real moving half on this cell's ray, not an estimated timer.
            bool reached=false;
            foreach(var head in UnityEngine.Object.FindObjectsByType<SpriteRenderer>())
            {
                // An arrived half is hidden while its trail fades, but its final position
                // still proves it crossed the cell (arrival and impact may share a frame).
                if(head.name!="RocketHalf" || head.transform.parent==null)continue;
                Vector2 origin=head.transform.parent.position;
                Vector2 delta=(Vector2)head.transform.position-origin;
                if(delta.sqrMagnitude<.00001f)continue;
                var axis=delta.normalized;
                var target=(Vector2)cell-origin;
                float along=Vector2.Dot(target,axis);
                float offLine=Mathf.Abs(target.x*axis.y-target.y*axis.x);
                if(offLine<.08f && along>=-.08f && delta.magnitude>=along-.08f){reached=true;break;}
            }
            if(!reached)failure=$"{Cases[caseIndex]}: {kind} {col},{row} exploded before a real rocket half reached it.";
            if(hits++==0)firstHit=Time.time;
            lastHit=Time.time;
            timeline.Add($"{Cases[caseIndex]},{Time.frameCount},{Time.time.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)},{kind},{col},{row},{reached}");
        }
        static Item Spawn(LevelManager manager,int col,int row,ItemsTypes type)
        {
            var cell=manager.field.GetSquare(col,row,true);
            if(cell.Item!=null)cell.Item.Hide(true);
            cell.item=null;
            return cell.GenItem(false,type,noAnim:true);
        }
        static void Begin(LevelManager manager)
        {
            RoyalAves.Gameplay.EnchantedStone.HitApplied-=HitStone;
            RoyalAves.Gameplay.EnchantedStone.HitApplied+=HitStone;
            ItemMulticolor.ActualExplosion-=HitItem;ItemMulticolor.ActualExplosion+=HitItem;
            hits=0;firstHit=lastHit=releaseAt=0;released=false;failure=null;
            positions.Clear();
            var setup=new GameObject("RocketVerificationSetup");setup.AddComponent<GameBlocker>();
            Item source,partner=null;
            if(caseIndex==0)source=Spawn(manager,0,0,ItemsTypes.VERTICAL_STRIPED);
            else if(caseIndex==1)source=Spawn(manager,8,0,ItemsTypes.HORIZONTAL_STRIPED);
            else if(caseIndex==2)source=Spawn(manager,0,0,ItemsTypes.VERTICAL_STRIPED);
            else if(caseIndex==3)source=Spawn(manager,8,0,ItemsTypes.HORIZONTAL_STRIPED);
            else if(caseIndex>=6)source=Spawn(manager,4,0,caseIndex==6?ItemsTypes.HORIZONTAL_STRIPED:ItemsTypes.VERTICAL_STRIPED);
            else
            {
                source=Spawn(manager,4,0,ItemsTypes.HORIZONTAL_STRIPED);
                partner=Spawn(manager,5,0,caseIndex==4?ItemsTypes.VERTICAL_STRIPED:ItemsTypes.PACKAGE);
                // Keep this test about rockets: remove the separate preplaced central bomb.
                if(caseIndex==4)Spawn(manager,4,2,ItemsTypes.NONE);
            }
            foreach(var item in manager.field.GetItems().Where(i=>i.currentType==ItemsTypes.NONE))positions[item]=item.transform.position;
            if(caseIndex>=6)source.transform.position+=new Vector3(.43f,.37f,2.1f);
            observing=true;started=EditorApplication.timeSinceStartup;
            UnityEngine.Object.Destroy(setup);
            if(partner==null)source.ActivateFromSwap();else source.Check(source,partner);
        }
        static void Finish(string result)
        {
            observing=false;SessionState.SetBool(Key,false);
            RoyalAves.Gameplay.EnchantedStone.HitApplied-=HitStone;ItemMulticolor.ActualExplosion-=HitItem;
            File.WriteAllLines("Logs/rocket-pass-timeline.csv",new[]{"case,frame,time,kind,col,row,headReached"}.Concat(timeline));
            File.WriteAllText("Logs/rocket-pass-native.result",result+"\n"+string.Join("\n",report));
            if(result.StartsWith("PASS"))StoneCampaign101Tools.Open();
        }
        static void Poll()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            try
            {
                if(File.Exists(Request))
                {
                    File.Delete(Request);
                    SessionState.SetBool(Key,true);observing=false;caseIndex=0;timeline.Clear();report.Clear();
                    started=EditorApplication.timeSinceStartup;
                    StoneCampaign101Tools.Open();return;
                }
                if(!SessionState.GetBool(Key,false))return;
                var manager=LevelManager.THIS;
                if(!EditorApplication.isPlaying || manager?.field==null || manager.levelData.levelNum!=101)
                {if(EditorApplication.timeSinceStartup-started>90)Finish("FAIL: level 101 did not open.");return;}
                EditorApplication.QueuePlayerLoopUpdate();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                if(!observing)
                {
                    if(manager.gameStatus==GameState.Playing && !manager.StopFall && !manager.DragBlocked &&
                        !manager.field.GetItems().Any(i=>i.falling || i.destroying))Begin(manager);
                    return;
                }
                Require(failure==null,failure);
                foreach(var head in UnityEngine.Object.FindObjectsByType<SpriteRenderer>())
                {
                    if(head.name!="RocketHalf" || head.transform.parent==null)continue;
                    Vector3 delta=head.transform.position-head.transform.parent.position;
                    Require(Mathf.Abs(delta.x)<.002f || Mathf.Abs(delta.y)<.002f,
                        Cases[caseIndex]+": diagonal rocket trajectory detected: "+delta);
                    Require(Mathf.Abs(delta.z)<.002f,Cases[caseIndex]+": rocket left the board plane.");
                }
                if(manager.StopFall && !released)
                {
                    foreach(var pair in positions)
                        if(pair.Key!=null && pair.Key.gameObject.activeInHierarchy && !pair.Key.destroying)
                            Require(Vector2.Distance(pair.Key.transform.position,pair.Value)<.01f,
                                $"{Cases[caseIndex]}: surviving {pair.Key.name}/{pair.Key.currentType} at {pair.Key.square.col},{pair.Key.square.row} moved {pair.Value} -> {pair.Key.transform.position}; falling={pair.Key.falling}, needFall={pair.Key.needFall}, hits={hits}, time={Time.time}.");
                }
                else if(hits>0 && !released)
                {
                    released=true;releaseAt=Time.time;
                    Require(releaseAt-lastHit>=.18f,Cases[caseIndex]+": no impact pause before gravity release.");
                }
                if(released && Time.time-releaseAt>1.5f && !manager.StopFall &&
                    !manager.field.GetItems().Any(i=>i.falling || i.destroying))
                {
                    Require(hits>1 && lastHit-firstHit>.12f,Cases[caseIndex]+": impacts were simultaneous.");
                    report.Add($"PASS {Cases[caseIndex]}: {hits} impacts over {lastHit-firstHit:F3}s; gravity held until {releaseAt-lastHit:F3}s after last impact.");
                    ScreenCapture.CaptureScreenshot("Logs/rocket-pass-"+Cases[caseIndex]+".png");
                    observing=false;caseIndex++;
                    if(caseIndex==Cases.Length)Finish("PASS: eight real Play cases including off-grid horizontal/vertical launches; no diagonal or depth movement; impacts follow actual moving halves, no falling during the sweep, impact pause before refill. No physical-pointer/full-victory/audio test.");
                }
                if(EditorApplication.timeSinceStartup-started>25)Finish("FAIL: "+Cases[caseIndex]+" timed out.");
            }
            catch(Exception ex){Finish("FAIL: "+ex);Debug.LogException(ex);}
        }
    }
}
