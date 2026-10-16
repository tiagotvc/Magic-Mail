using System;
using System.IO;
using System.Linq;
using RoyalAves.Meta;
using SweetSugar.Scripts.GUI.Boost;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SnowModalValidation
{
    public static void Run()
    {
        RoyalAves.EditorTools.RoyalAvesSnowLevelStartBuilder.Apply();
        var setup=EditorSceneManager.GetSceneManagerSetup();
        var oldLevel=PlayerPrefs.GetInt("OpenLevel",1);
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/SweetSugar/Scenes/game.unity",OpenSceneMode.Single);
            var menu=scene.GetRootGameObjects().First(g=>g.name=="CanvasGlobal").transform.Find("MenuPlay");
            var window=menu.Find("Image")??menu.Find("MenuPlayBg");
            var boosts=window.Find("Boosters").GetComponentsInChildren<BoostIcon>(true).Where(b=>b.gameObject.activeSelf).ToArray();
            Check(boosts.Length==3,"Three original boosters must remain");
            foreach(var boost in boosts)
            {
                Check(boost.check!=null && boost.plus!=null && boost.counter!=null && boost.boostCount!=null,"Booster references lost");
                var buttons=boost.GetComponentsInChildren<Button>(true);
                Check(buttons.Any(b=>Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentTarget(i)==boost)),"Booster click disconnected");
            }
            var play=window.Find("Play").GetComponent<Button>();
            Check(Enumerable.Range(0,play.onClick.GetPersistentEventCount()).Any(i=>play.onClick.GetPersistentMethodName(i)=="Play"),"Play callback lost");
            var close=window.Find("Close").GetComponent<Button>();
            Check(Enumerable.Range(0,close.onClick.GetPersistentEventCount()).Any(i=>close.onClick.GetPersistentMethodName(i)=="CloseMenu"),"Close callback lost");
            var free=menu.GetComponentsInChildren<FreeAdBoosterButton>(true).Where(f=>f.gameObject.activeSelf).ToArray();
            var activeFree=free.Where(f=>{var b=(Button)new SerializedObject(f).FindProperty("button").objectReferenceValue;return b!=null && b.gameObject.activeSelf;}).ToArray();
            Check(activeFree.Length==1,"Expected exactly one enabled rewarded-ad controller");
            var data=new SerializedObject(activeFree[0]);
            Check(data.FindProperty("boostShop").objectReferenceValue!=null,"Reward shop reference lost");
            var ad=(Button)data.FindProperty("button").objectReferenceValue;
            Check(Enumerable.Range(0,ad.onClick.GetPersistentEventCount()).Any(i=>ad.onClick.GetPersistentTarget(i)==activeFree[0] && ad.onClick.GetPersistentMethodName(i)=="WatchAdForFreeBooster"),"Reward callback missing");
            Directory.CreateDirectory("Tools/level-start/previews");
            Capture(menu.gameObject,1,720,1280);
            Capture(menu.gameObject,20,720,1280);
            Capture(menu.gameObject,20,720,960);
            File.WriteAllText("Tools/level-start/previews/validation.txt","PASS: Unity compilation, three original booster controls and count/plus/check references, Play and Close callbacks, exactly one enabled free-ad controller with original shop and rewarded-ad callback. Rendered actual scene modal at levels 1 and 20 in 720x1280 and 720x960. No live ad playback or physical device input tested.");
            Debug.Log("SNOW_MODAL_VALIDATION_PASS");
        }
        finally {PlayerPrefs.SetInt("OpenLevel",oldLevel);if(setup.Any(s=>s.isLoaded)&&setup.Any(s=>s.isActive))EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Capture(GameObject source,int level,int width,int height)
    {
        PlayerPrefs.SetInt("OpenLevel",level);
        var scene=EditorSceneManager.NewPreviewScene();
        RenderTexture rt=null;Texture2D texture=null;
        Canvas[] outsideCanvases=Array.Empty<Canvas>();
        try
        {
            var camGo=new GameObject("PreviewCamera",typeof(Camera));SceneManager.MoveGameObjectToScene(camGo,scene);
            var cam=camGo.GetComponent<Camera>();cam.scene=scene;cam.orthographic=true;cam.orthographicSize=1024;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.015f,.2f);cam.transform.position=new Vector3(0,0,-10);
            rt=new RenderTexture(width,height,24);rt.Create();cam.targetTexture=rt;
            var canvasGo=new GameObject("PreviewCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));SceneManager.MoveGameObjectToScene(canvasGo,scene);
            var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;
            var scaler=canvasGo.GetComponent<CanvasScaler>();EditorUtility.CopySerialized(source.GetComponentInParent<CanvasScaler>(),scaler);
            var modal=UnityEngine.Object.Instantiate(source,canvasGo.transform);modal.SetActive(true);
            foreach(var name in new[]{"Leadboard","Leadboard HD"})
                if(modal.transform.Find(name) is Transform leaderboard)leaderboard.gameObject.SetActive(false);
            foreach(var anim in modal.GetComponentsInChildren<Animation>(true))anim.enabled=false;
            modal.transform.localScale=Vector3.one;
            var view=modal.GetComponentInChildren<LevelGoalsView>(true);view.Refresh();
            var boosters=(modal.transform.Find("Image")??modal.transform.Find("MenuPlayBg")).Find("Boosters");
            for(var i=0;i<3;i++)
            {
                var boost=boosters.GetComponentsInChildren<BoostIcon>(true).Where(b=>b.gameObject.activeSelf).ToArray()[i];
                boost.check.SetActive(false);boost.plus.SetActive(false);boost.counter.SetActive(true);boost.boostCount.text=new[]{"2","7","5"}[i];
                var locked=boost.transform.Find("Lock");if(locked!=null)locked.gameObject.SetActive(false);
                var indicator=boost.transform.Find("Indicator");if(indicator!=null)indicator.gameObject.SetActive(true);
            }
            Canvas.ForceUpdateCanvases();
            // A tall popup must also fit 4:3 devices, which have less reference-canvas width.
            var rect=(RectTransform)canvas.transform;
            var window=(RectTransform)(modal.transform.Find("Image")??modal.transform.Find("MenuPlayBg"));
            window.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
            Canvas.ForceUpdateCanvases();
            var frame=(RectTransform)(window.Find("Image")??window.Find("MenuPlayBg"));
            var frameCorners=new Vector3[4];frame.GetWorldCorners(frameCorners);
            foreach(var corner in frameCorners)
            {
                var p=rect.InverseTransformPoint(corner);var b=rect.rect;
                Check(p.x>=b.xMin-.1f&&p.x<=b.xMax+.1f&&p.y>=b.yMin-.1f&&p.y<=b.yMax+.1f,
                    "Modal frame extends outside viewport: "+p+", "+b);
            }
            Debug.Log($"MODAL_CAPTURE {width}x{height}: canvas {rect.rect.size}, window {window.rect.size}");
            // Screen-space overlay canvases in the open Simulator can leak into a live-editor render.
            outsideCanvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c=>c.enabled && c.gameObject.scene!=scene).ToArray();
            foreach(var c in outsideCanvases)c.enabled=false;
            cam.Render();Canvas.ForceUpdateCanvases();cam.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;
            texture=new Texture2D(width,height,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();RenderTexture.active=previous;
            File.WriteAllBytes($"Tools/level-start/previews/level-{level}-{width}x{height}.png",texture.EncodeToPNG());
        }
        finally{foreach(var c in outsideCanvases)if(c!=null)c.enabled=true;if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);foreach(var c in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()))c.targetTexture=null;if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}EditorSceneManager.ClosePreviewScene(scene);}
    }
}



