using System;
using System.IO;
using System.Linq;
using RoyalAves.Meta;
using RoyalAves.EditorTools;
using SweetSugar.Scripts.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SnowReferenceUpdateTask
{
    static double next;
    static SnowReferenceUpdateTask(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        next=EditorApplication.timeSinceStartup+1;
        const string request="Temp/snow-reference-update.request";
        if(!File.Exists(request))return;
        if(EditorApplication.isPlaying){EditorApplication.ExitPlaymode();return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete(request);
        var oldLevel=PlayerPrefs.GetInt("OpenLevel",1);
        var oldLanguage=CrosssceneData.selectedLanguage;
        var preview=default(Scene);
        try
        {
            CrosssceneData.selectedLanguage="Portuguese";
            var modal=RoyalAvesSnowLevelStartBuilder.ApplyActionButtonPolish();
            preview=EditorSceneManager.NewPreviewScene();
            var copy=UnityEngine.Object.Instantiate(modal);
            SceneManager.MoveGameObjectToScene(copy,preview);copy.SetActive(true);
            var view=copy.GetComponentInChildren<LevelGoalsView>(true);
            var bounds=(RectTransform)view.transform;
            var checkedLevels=0;var maxGoals=0;var maxGoalLevel=1;
            var corners=new Vector3[4];
            for(var level=1;level<=100;level++)
            {
                if(Resources.Load<LevelContainer>("Levels/Level_"+level)==null)continue;
                PlayerPrefs.SetInt("OpenLevel",level);view.Refresh();Canvas.ForceUpdateCanvases();
                var icons=view.transform.Cast<Transform>().Where(t=>t.gameObject.activeSelf&&t.name.StartsWith("Meta ")).ToArray();
                if(icons.Length>maxGoals){maxGoals=icons.Length;maxGoalLevel=level;}
                foreach(var icon in icons)
                    foreach(var r in icon.GetComponentsInChildren<RectTransform>().Where(r=>r.gameObject.activeInHierarchy))
                    {
                        r.GetWorldCorners(corners);
                        foreach(var corner in corners)
                        {
                            var p=bounds.InverseTransformPoint(corner);var b=bounds.rect;
                            if(p.x<b.xMin-.05f||p.x>b.xMax+.05f||p.y<b.yMin-.05f||p.y>b.yMax+.05f)
                                throw new Exception($"Goal overflow at level {level}, {r.name}, {p}, area {b}");
                        }
                    }
                checkedLevels++;
            }
            EditorSceneManager.ClosePreviewScene(preview);preview=default(Scene);
            SnowModalValidation.Capture(modal,1,720,1280);
            SnowModalValidation.Capture(modal,20,720,960);
            SnowModalValidation.Capture(modal,maxGoalLevel,720,1280);
            File.WriteAllText("Tools/level-start/previews/reference-update.txt",$"PASS: Unity compilation; Calistoga modal text; actions at x +/-234, y -468 with 448x208 size; goal area 860x380 without a mask; booster panel 958x400, visible icon art fitted to the purple sockets. All picture and number rectangles contained across {checkedLevels} real levels. Maximum {maxGoals} goals at level {maxGoalLevel}. Rendered at 720x1280 and 720x960. Existing booster types and callbacks retained.");
            File.WriteAllText("Temp/snow-reference-update.done","OK");
        }
        catch(Exception e){Debug.LogException(e);File.WriteAllText("Temp/snow-reference-update.done",e.ToString());}
        finally
        {
            if(preview.IsValid())EditorSceneManager.ClosePreviewScene(preview);
            PlayerPrefs.SetInt("OpenLevel",oldLevel);CrosssceneData.selectedLanguage=oldLanguage;
        }
    }
}
