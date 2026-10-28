using System;
using System.IO;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using UnityEditor;
using UnityEngine;

public static class EditorIdlePreviewValidation
{
    const BindingFlags Instance=BindingFlags.Instance|BindingFlags.NonPublic;
    const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
    public static void Run()
    {
        RoyalAvesLevelMapPreview preview=null;
        GameObject lobby=null;
        var simulator=typeof(RoyalAvesMapSimulatorPreview);
        var animation=simulator.GetProperty("AnimatePreview",Static);
        var saved=(bool)animation.GetValue(null);
        int errors=0;
        Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        Application.logMessageReceived+=log;
        try
        {
            ShaderUtil.allowAsyncCompilation=false;
            animation.SetValue(null,false);
            var type=typeof(RoyalAvesLevelMapPreview);
            preview=(RoyalAvesLevelMapPreview)type.GetMethod("CreateScenePreview",Static).Invoke(null,new object[]{540,960});
            // Exercise the standalone window's draw scheduler without opening another window.
            type.GetField("scenePreview",Instance).SetValue(preview,false);
            var render=type.GetMethod("RenderWindow",Instance);
            var stamp=type.GetField("lastPreviewFrame",Instance);
            render.Invoke(preview,null);
            var idleStamp=stamp.GetValue(preview);
            for(int i=0;i<20;i++)render.Invoke(preview,null);
            Check(Equals(idleStamp,stamp.GetValue(preview)),"Idle window keeps rendering");

            var map=(CurvedLevelMap)type.GetField("map",Instance).GetValue(preview);
            map.PreviewAt(20);type.GetField("level",Instance).SetValue(preview,20f);
            type.GetMethod("BeginSceneDrag",Instance).Invoke(preview,null);
            type.GetMethod("DragScenePreview",Instance).Invoke(preview,new object[]{-.18f,.1f});
            type.GetMethod("EndSceneDrag",Instance).Invoke(preview,null);
            float before=map.Cursor;
            for(int i=0;i<10;i++)
            {
                stamp.SetValue(preview,EditorApplication.timeSinceStartup-.033);
                render.Invoke(preview,null);
            }
            Check(map.Cursor>before+.1f,"Paused animations broke drag inertia");
            bool moving=true;
            for(int i=0;i<400&&moving;i++)moving=map.PreviewMotion(.05f,false);
            Check(!moving,"Navigation never returns to idle with animated props present");
            Check(map.PreviewMotion(0,true)==map.NeedsPreviewFrames,"Animation opt-in no longer requests frames");
            UnityEngine.Object.DestroyImmediate(preview);preview=null;

            // Exercise the real edit-mode Simulator scheduler and its disposable overlay.
            simulator.GetMethod("Cleanup",Static).Invoke(null,null);
            lobby=new GameObject("RoyalAvesLobby",typeof(RectTransform),typeof(Canvas));
            var original=lobby.GetComponent<Canvas>();
            var nested=new GameObject("NestedCanvas",typeof(Canvas)).GetComponent<Canvas>();nested.transform.SetParent(lobby.transform,false);
            var disabled=new GameObject("DisabledCanvas",typeof(Canvas)).GetComponent<Canvas>();disabled.transform.SetParent(lobby.transform,false);disabled.enabled=false;
            var update=simulator.GetMethod("Update",Static);
            var next=simulator.GetField("nextFrame",Static);
            var frame=simulator.GetField("lastFrame",Static);
            next.SetValue(null,0d);update.Invoke(null,null);
            Check(simulator.GetField("renderer",Static).GetValue(null)!=null,"Simulator preview missing");
            Check(!original.enabled&&!nested.enabled&&!disabled.enabled,"Covered legacy canvases still render");
            var overlay=(GameObject)simulator.GetField("overlay",Static).GetValue(null);
            Check(overlay.GetComponent<Canvas>().isActiveAndEnabled,"New map overlay was disabled");
            next.SetValue(null,0d);update.Invoke(null,null);
            var simulatorStamp=frame.GetValue(null);
            for(int i=0;i<20;i++){next.SetValue(null,0d);update.Invoke(null,null);}
            Check(Equals(simulatorStamp,frame.GetValue(null)),"Idle Simulator keeps rendering");
            animation.SetValue(null,true);next.SetValue(null,0d);update.Invoke(null,null);
            Check(!Equals(simulatorStamp,frame.GetValue(null)),"Animation toggle does not wake Simulator");
            animation.SetValue(null,false);next.SetValue(null,0d);update.Invoke(null,null);
            Check(!(bool)simulator.GetField("moving",Static).GetValue(null),"Animation toggle leaves Simulator spinning");
            simulator.GetMethod("Cleanup",Static).Invoke(null,null);
            Check(original.enabled&&nested.enabled&&!disabled.enabled,"Original canvas states were not restored");
            next.SetValue(null,0d);update.Invoke(null,null);
            Check(!original.enabled&&!nested.enabled,"Reopened preview leaves legacy canvases rendering");
            simulator.GetMethod("Cleanup",Static).Invoke(null,null);
            Check(original.enabled&&nested.enabled&&!disabled.enabled,"Canvas states lost after reopen/cleanup");
            Check(errors==0,"Unity logged errors");
            File.WriteAllText("../editor-idle-validation.txt","PASS: 20 idle window redraw requests and 20 Simulator updates cause zero extra preview renders; animation opt-in wakes rendering and opt-out stops it; drag inertia advances and settles with animated props present; covered legacy canvases disabled, new overlay stays enabled, original nested/enabled/disabled states restored across two preview lifecycles; zero Unity errors. No gameplay assets or source scene saved.");
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);return;}
        finally
        {
            simulator.GetMethod("Cleanup",Static).Invoke(null,null);
            if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);
            if(lobby!=null)UnityEngine.Object.DestroyImmediate(lobby);
            animation.SetValue(null,saved);
            Application.logMessageReceived-=log;
        }
        EditorApplication.Exit(0);
    }
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
}
