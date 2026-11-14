using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.Meta;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    // Runs the bonus itself, deliberately excluding persistence and reward/victory dispatch.
    public static class CampaignBonusSkipVerification
    {
        const string Request="Logs/bonus-skip.request", Key="MagicMail.BonusSkipVerification";
        static CampaignBonusSkip view;
        static GameObject hold;
        static double began;
        static int stage, gems, reached, levelsWon, metaStars, savedScore;
        static float scale;
        static bool mute, bonusDone;
        static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        static void PrepareArt()
        {
            var config=LobbyFeaturesConfig.Instance;
            if(config.bonusSkipButton!=null)return;
            config.bonusSkipButton=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/UI/Menu/RoyalPostman/SettingsV161/Sprites/button.asset");
            Require(config.bonusSkipButton!=null,"Shared settings button sprite missing.");
            EditorUtility.SetDirty(config);AssetDatabase.SaveAssetIfDirty(config);
        }
        static void SetState(LevelManager manager,GameState state)=>typeof(LevelManager).GetField("GameStatus",Flags).SetValue(manager,state);
        [InitializeOnLoadMethod]
        static void Init(){EditorApplication.update-=Poll;EditorApplication.update+=Poll;began=EditorApplication.timeSinceStartup;}
        static IEnumerator Bonus(LevelManager manager)
        {
            var routine=(IEnumerator)typeof(LevelManager).GetMethod("CampaignWinBonus",Flags).Invoke(manager,null);
            yield return routine;bonusDone=true;
        }
        static void Click(CampaignBonusSkip target)
        {
            Canvas.ForceUpdateCanvases();
            var button=target.SkipButton;
            var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
            var center=(Vector2)((corners[0]+corners[2])*.5f);
            var pointer=new PointerEventData(EventSystem.current){position=center,button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Require(hits.Count>0 && hits[0].gameObject==button.gameObject,"Skip is not reachable by the UI raycaster.");
            Require((corners[2].y-corners[0].y)>=43.9f,"Skip target is smaller than 44px.");
            Require(Screen.safeArea.Contains(corners[0]) && Screen.safeArea.Contains(corners[2]),"Skip is outside the safe area.");
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        static void CheckProgressUnchanged()
        {
            Require(InitScript.Gems==gems && PlayerPrefs.GetInt("ReachedLevel")==reached &&
                MetaProgress.LevelsWon==levelsWon && MetaProgress.Stars==metaStars &&
                PlayerPrefs.GetInt("Score101")==savedScore,"Bonus-only test changed progress or awarded currency.");
        }
        static void Finish(string result)
        {
            SessionState.SetBool(Key,false);
            if(hold!=null)UnityEngine.Object.Destroy(hold);
            if(view!=null)view.Complete();
            File.WriteAllText("Logs/bonus-skip-native.result",result);
            StoneCampaign101Tools.Open();
        }
        static void Poll()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            try
            {
                if(File.Exists(Request))
                {
                    File.Delete(Request);SessionState.SetBool(Key,true);stage=0;began=EditorApplication.timeSinceStartup;
                    PrepareArt();
                    File.WriteAllText("Logs/bonus-skip-native.result","RUNNING");StoneCampaign101Tools.Open();return;
                }
                if(!SessionState.GetBool(Key,false))return;
                var manager=LevelManager.THIS;
                if(!EditorApplication.isPlaying || manager?.field==null || manager.levelData.levelNum!=101)return;
                EditorApplication.QueuePlayerLoopUpdate();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                if(stage==0 && manager.gameStatus==GameState.Playing && !manager.StopFall && !manager.DragBlocked)
                {
                    PrepareArt();
                    gems=InitScript.Gems;reached=PlayerPrefs.GetInt("ReachedLevel");levelsWon=MetaProgress.LevelsWon;
                    metaStars=MetaProgress.Stars;savedScore=PlayerPrefs.GetInt("Score101");scale=Time.timeScale;
                    var source=SoundBase.Instance.GetComponent<AudioSource>();mute=source.mute;
                    // Prevent normal CheckWinLose calls from starting persistence/life/reward code in this fixture.
                    typeof(LevelManager).GetField("resolvingWin",Flags).SetValue(manager,true);
                    typeof(LevelManager).GetField("winResolved",Flags).SetValue(manager,true);
                    SetState(manager,GameState.PreWinAnimations);
                    foreach(var goal in manager.levelData.TargetCounters)goal.count=0;
                    manager.levelData.limit=4;
                    hold=new GameObject("BonusSkipVerificationHold");hold.AddComponent<GameBlocker>();
                    view=CampaignBonusSkip.Begin(manager);bonusDone=false;manager.StartCoroutine(Bonus(manager));
                    Require(!view.ButtonVisible,"Skip appeared before its delay.");
                    view.RequestSkip();Require(!view.Requested && Time.timeScale==scale,"Skip accepted an early request.");
                    began=EditorApplication.timeSinceStartup;stage=1;
                }
                else if(stage==1 && EditorApplication.timeSinceStartup-began>CampaignBonusSkip.RevealDelay+.15)
                {
                    Require(view.ButtonVisible,"Skip did not appear during a long bonus.");
                    Require(view.SkipButton.GetComponent<Image>().sprite==LobbyFeaturesConfig.Instance.bonusSkipButton,
                        $"Shared button art was not reused: actual={view.SkipButton.GetComponent<Image>().sprite}, configured={LobbyFeaturesConfig.Instance.bonusSkipButton}.");
                    ScreenCapture.CaptureScreenshot("Logs/bonus-skip-button.png");
                    began=EditorApplication.timeSinceStartup;stage=6;
                }
                else if(stage==6 && EditorApplication.timeSinceStartup-began>.15)
                {
                    Click(view);view.RequestSkip();
                    Require(view.Requested && !view.ButtonVisible && Time.timeScale==CampaignBonusSkip.FinishSpeed,"Skip did not start exactly one fast finish.");
                    UnityEngine.Object.Destroy(hold);hold=null;
                    began=EditorApplication.timeSinceStartup;stage=2;
                }
                else if(stage==2 && bonusDone)
                {
                    Require(manager.levelData.limit==0 && LevelManager.Score>0,"Real bonus did not account for remaining moves/points.");
                    CheckProgressUnchanged();view.Complete();view.Complete();
                    Require(Time.timeScale==scale && SoundBase.Instance.GetComponent<AudioSource>().mute==mute,"Speed or audio was not restored.");
                    // A short sequence exits before revealing Skip.
                    view=CampaignBonusSkip.Begin(manager);SetState(manager,GameState.Pause);
                    began=EditorApplication.timeSinceStartup;stage=3;
                }
                else if(stage==3 && EditorApplication.timeSinceStartup-began>.15)
                {
                    Require(view==null && Time.timeScale==scale,"Short-bonus UI did not clean up.");
                    SetState(manager,GameState.PreWinAnimations);view=CampaignBonusSkip.Begin(manager);
                    began=EditorApplication.timeSinceStartup;stage=4;
                }
                else if(stage==4 && (view.Requested || view.ButtonVisible) && EditorApplication.timeSinceStartup-began>CampaignBonusSkip.RevealDelay+.15)
                {
                    // The pointer route was checked above; this case isolates interruption cleanup.
                    if(!view.Requested)view.RequestSkip();
                    Require(view.Requested,"Interruption fixture did not start fast finish.");
                    UnityEngine.Object.Destroy(view.gameObject);began=EditorApplication.timeSinceStartup;stage=5;
                }
                else if(stage==5 && EditorApplication.timeSinceStartup-began>.15)
                {
                    Require(Time.timeScale==scale && SoundBase.Instance.GetComponent<AudioSource>().mute==mute,"Interruption leaked speed/audio settings.");
                    CheckProgressUnchanged();
                    Finish("PASS: delayed visible button, real GraphicRaycaster/pointer-event route, shared art, 44px/safe-area target, double request guard, actual campaign bonus consumed four moves and calculated points, cleanup before reveal, normal completion/interruption restored time/audio. Progress, currency and persisted score unchanged in this bonus-only fixture. No full victory/reward/Continue flow or physical input claim.");
                }
                if(stage>0 && EditorApplication.timeSinceStartup-began>35)throw new TimeoutException("Bonus skip stage "+stage);
            }
            catch(Exception ex){Finish("FAIL: "+ex);Debug.LogException(ex);}
        }
    }
}
