using System;
using System.IO;
using System.Reflection;
using RoyalAves.Meta;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SnowModalPlayValidation
{
    const string Key="SnowModal.PlayValidation";
    static double since;
    static int stage;
    static bool finished;
    static SnowModalPlayValidation(){if(SessionState.GetInt(Key+"Exit",0)>0)EditorApplication.delayCall+=RestoreAndExit;if(SessionState.GetBool(Key,false)){since=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}}
    public static void PrepareRun() { SnowModalValidation.Run(); Run(); }
    public static void Run()
    {
        SessionState.SetInt(Key+"Level",PlayerPrefs.GetInt("OpenLevel",1));
        SessionState.SetInt(Key+"Lives",PlayerPrefs.GetInt("Lifes",5));
        SessionState.SetBool(Key+"Start",EditorPrefs.GetBool("RoyalAves.PlayModeStartScene",true));
        EditorPrefs.SetBool("RoyalAves.PlayModeStartScene",false);
        EditorSceneManager.playModeStartScene=null;
        EditorSceneManager.OpenScene("Assets/SweetSugar/Scenes/game.unity",OpenSceneMode.Single);
        SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(finished)return;
        try
        {
            if(EditorApplication.timeSinceStartup-since>100)throw new Exception("Timed out waiting for runtime flow at stage "+stage);
            if(!EditorApplication.isPlaying)return;
            var map=UnityEngine.Object.FindAnyObjectByType<CurvedLevelMap>();
            var menu=MenuReference.THIS!=null?MenuReference.THIS.MenuPlay:null;
            if(stage==0)
            {
                if(map==null||menu==null||LevelManager.THIS==null||EditorApplication.timeSinceStartup-since<6)return;
                typeof(CurvedLevelMap).GetMethod("Select",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(map,new object[]{1});
                stage=1;since=EditorApplication.timeSinceStartup;return;
            }
            if(EditorApplication.timeSinceStartup-since<1)return;
            var window=menu.transform.Find("Image")??menu.transform.Find("MenuPlayBg");
            if(stage==1)
            {
                if(!menu.activeInHierarchy)throw new Exception("Selecting map level did not open MenuPlay");
                var goals=menu.GetComponentInChildren<LevelGoalsView>();
                if(goals==null||goals.transform.childCount<2)throw new Exception("Runtime level goals missing");
                window.Find("Close").GetComponent<Button>().onClick.Invoke();
                stage=2;since=EditorApplication.timeSinceStartup;return;
            }
            if(stage==2)
            {
                if(menu.activeSelf)throw new Exception("Close button did not close modal");
                typeof(CurvedLevelMap).GetMethod("Select",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(map,new object[]{1});
                stage=3;since=EditorApplication.timeSinceStartup;return;
            }
            if(stage==3)
            {
                if(!menu.activeInHierarchy)throw new Exception("Map could not reopen modal");
                // Grant no inventory and play no ads. Use the existing life balance for the start-game check.
                if(InitScript.lifes>0)
                {
                    window.Find("Play").GetComponent<Button>().onClick.Invoke();
                    stage=4;since=EditorApplication.timeSinceStartup;return;
                }
                Finish("PASS: runtime map selection opens modal with real goals, Close closes it, map reopens it. Start game skipped: player has no lives.",0);
            }
            if(stage==4)
            {
                if(LevelManager.THIS.gameStatus==GameState.Map)throw new Exception("Play button did not start level");
                Finish("PASS: Play Mode map selection opens the existing modal with real level goals; Close closes it; map reopens it; Play starts the selected level. No real ad playback tested.",0);
            }
        }
        catch(Exception e){Debug.LogException(e);Finish("FAIL: "+e,1);}
    }
    static void RestoreAndExit()
    {
        var code=SessionState.GetInt(Key+"Exit",1)-1;SessionState.SetInt(Key+"Exit",0);
        PlayerPrefs.SetInt("OpenLevel",SessionState.GetInt(Key+"Level",1));PlayerPrefs.SetInt("Lifes",SessionState.GetInt(Key+"Lives",5));PlayerPrefs.Save();EditorApplication.Exit(code);
    }
    static void Finish(string message,int code)
    {
        finished=true;SessionState.SetBool(Key,false);SessionState.SetInt(Key+"Exit",code+1);
        File.WriteAllText("Tools/level-start/previews/play-validation.txt",message);
        Debug.Log("SNOW_MODAL_PLAY_VALIDATION: "+message);
        EditorPrefs.SetBool("RoyalAves.PlayModeStartScene",SessionState.GetBool(Key+"Start",true));
        // Exit Play before restoring preferences, so OnDisable save hooks cannot spend a life again.
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredEditMode)return;
            PlayerPrefs.SetInt("OpenLevel",SessionState.GetInt(Key+"Level",1));
            PlayerPrefs.SetInt("Lifes",SessionState.GetInt(Key+"Lives",5));PlayerPrefs.Save();EditorApplication.Exit(code);
        };
        EditorApplication.ExitPlaymode();
    }
}

