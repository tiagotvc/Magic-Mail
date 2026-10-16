using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MapPreviewDragValidation
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static MapPreviewDragValidation(){EditorApplication.update+=Poll;}
    static RoyalAvesLevelMapPreview Window()=>Resources.FindObjectsOfTypeAll<RoyalAvesLevelMapPreview>().FirstOrDefault(p=>!(bool)typeof(RoyalAvesLevelMapPreview).GetField("scenePreview",Private).GetValue(p));
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(File.Exists("Temp/map-preview-drag.snapshot"))
        {
            File.Delete("Temp/map-preview-drag.snapshot");var p=Window();
            var map=p!=null?(CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(p):null;
            File.WriteAllText("Temp/map-preview-drag-position.txt",map!=null?map.Cursor.ToString(System.Globalization.CultureInfo.InvariantCulture):"No window");
        }
        if(!File.Exists("Temp/map-preview-drag.request"))return;
        File.Delete("Temp/map-preview-drag.request");RoyalAvesLevelMapPreview preview=null;
        try
        {
            var t=typeof(RoyalAvesLevelMapPreview);
            preview=RoyalAvesLevelMapPreview.CreateScenePreview(540,960);
            var map=(CurvedLevelMap)t.GetField("map",Private).GetValue(preview);
            var level=t.GetField("level",Private);var render=t.GetMethod("RenderWindow",Private);
            level.SetValue(preview,20f);map.PreviewAt(20);
            preview.BeginSceneDrag();preview.DragScenePreview(-.18f,.1f);
            Check(map.Cursor>21,"Downward mouse drag did not advance");var forward=map.Cursor;
            preview.EndSceneDrag();
            for(var i=0;i<10;i++)
            {t.GetField("lastPreviewFrame",Private).SetValue(preview,EditorApplication.timeSinceStartup-.016);render.Invoke(preview,null);}
            Check(map.Cursor>forward+.1f,"Repaint reset inertia");var momentum=map.Cursor;
            preview.BeginSceneDrag();preview.DragScenePreview(.18f,.1f);preview.EndSceneDrag();
            Check(map.Cursor<momentum-1,"Upward mouse drag did not go back");var backward=map.Cursor;
            level.SetValue(preview,81f);render.Invoke(preview,null);
            Check(Mathf.Abs(map.Cursor-81)<.001f,"Sidebar position no longer works");
            for(var i=0;i<5;i++)render.Invoke(preview,null);
            Check(Mathf.Abs(map.Cursor-81)<.001f,"Old momentum survived sidebar jump");
            File.WriteAllText("Tools/level-map/preview-drag-validation.txt",$"PASS: same game drag moves forward (20 to {forward:F3}) and backward ({momentum:F3} to {backward:F3}); inertia advances over 10 editor repaints; explicit sidebar jump to 81 stays stable. Scene, assets and player saves untouched.");
            File.WriteAllText("Temp/map-preview-drag.done","OK");
        }
        catch(Exception e){Debug.LogException(e);File.WriteAllText("Temp/map-preview-drag.done",e.ToString());}
        finally{if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
}
