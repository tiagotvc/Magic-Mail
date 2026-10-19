using System;
using System.IO;
using RoyalAves.Meta;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SnowOpeningTask
{
    const string Key = "Snow.OpeningAudit";
    static double since;
    static SnowOpeningTask() { EditorApplication.update += Tick; }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (File.Exists("Temp/snow-opening.request") && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            File.Delete("Temp/snow-opening.request");
            SessionState.SetInt(Key + "Level", PlayerPrefs.GetInt("OpenLevel", 1));
            SessionState.SetBool(Key + "Start", EditorPrefs.GetBool("RoyalAves.PlayModeStartScene", true));
            EditorPrefs.SetBool("RoyalAves.PlayModeStartScene", false); EditorSceneManager.playModeStartScene = null;
            SessionState.SetInt(Key, 1); EditorApplication.EnterPlaymode(); return;
        }
        var stage = SessionState.GetInt(Key, 0);
        if (stage == 0) return;
        if (!EditorApplication.isPlaying)
        {
            if (stage == 3)
            {
                PlayerPrefs.SetInt("OpenLevel", SessionState.GetInt(Key + "Level", 1));
                EditorPrefs.SetBool("RoyalAves.PlayModeStartScene", SessionState.GetBool(Key + "Start", true));
                SessionState.SetInt(Key, 0); File.WriteAllText("Temp/snow-opening.done", "done");
            }
            return;
        }
        if (stage == 1)
        {
            if (since == 0) since = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - since < 7 || MenuReference.THIS == null || LevelManager.THIS == null) return;
            SessionState.SetInt(Key, 2); new GameObject("Snow opening validation").AddComponent<SnowOpeningProbe>();
        }
        if (stage == 2 && SnowOpeningProbe.Done)
        {
            Debug.Log(SnowOpeningProbe.Result); SessionState.SetInt(Key, 3); EditorApplication.ExitPlaymode();
        }
    }
}
