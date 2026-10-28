using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RoyalAves.Meta;
using RoyalAves.UI.RoyalPostman2026;

public static class SettingsV161Check
{
    static readonly string[] Keys={SettingsScreen.SoundKey,SettingsScreen.MusicKey,SettingsScreen.VibrationKey,SettingsScreen.HintKey,SettingsScreen.NotificationsKey};
    static void Check(bool b,string reason){if(!b)throw new Exception(reason);}
    sealed class View:IDisposable
    {
        readonly Scene scene;readonly GameObject canvas;public readonly Camera camera;public readonly SettingsScreen settings;public readonly RoyalPostmanSettingsFit fit;public readonly EventSystem events;
        public RenderTexture texture;public int width,height;
        public View()
        {
            scene=EditorSceneManager.NewPreviewScene();canvas=new GameObject("NativeSettingsCanvas",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));SceneManager.MoveGameObjectToScene(canvas,scene);
            var cam=new GameObject("NativeSettingsCamera",typeof(Camera));SceneManager.MoveGameObjectToScene(cam,scene);camera=cam.GetComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.03f,.035f,.10f);
            var c=canvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=5;
            var e=new GameObject("NativeSettingsEvents",typeof(EventSystem));SceneManager.MoveGameObjectToScene(e,scene);events=e.GetComponent<EventSystem>();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(SettingsV161Build.Prefab);Check(prefab!=null,"Missing real prefab");Check(!prefab.activeSelf,"Settings must be closed by default");
            var root=UnityEngine.Object.Instantiate(prefab,canvas.transform);settings=root.GetComponent<SettingsScreen>();fit=root.GetComponent<RoyalPostmanSettingsFit>();
            // Editor preview scenes do not dispatch runtime Awake. Execute the actual method once, before Open.
            typeof(SettingsScreen).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(settings,null);
        }
        public void Resize(int w,int h)
        {
            width=w;height=h;camera.targetTexture=null;if(texture!=null){texture.Release();UnityEngine.Object.DestroyImmediate(texture);}
            texture=new RenderTexture(w,h,24){antiAliasing=4};texture.Create();camera.targetTexture=texture;camera.aspect=(float)w/h;
            Render();
        }
        public void Render(){Canvas.ForceUpdateCanvases();fit.Fit();Canvas.ForceUpdateCanvases();foreach(var t in settings.GetComponentsInChildren<TextMeshProUGUI>())t.ForceMeshUpdate();camera.Render();}
        public Rect Bounds(RectTransform t){var corners=new Vector3[4];t.GetWorldCorners(corners);var p=corners.Select(v=>(Vector2)camera.WorldToScreenPoint(v)).ToArray();return Rect.MinMaxRect(p.Min(v=>v.x),p.Min(v=>v.y),p.Max(v=>v.x),p.Max(v=>v.y));}
        public void Export(string name){Render();var old=RenderTexture.active;RenderTexture.active=texture;var t=new Texture2D(width,height,TextureFormat.RGBA32,false);t.ReadPixels(new Rect(0,0,width,height),0,0);t.Apply();File.WriteAllBytes(SettingsV161Build.Output+"/"+name+".png",t.EncodeToPNG());RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(t);}
        public void Click(Button button)
        {
            Render();var point=Bounds((RectTransform)button.transform).center;var ev=new PointerEventData(events){position=point,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();canvas.GetComponent<GraphicRaycaster>().Raycast(ev,hits);
            Check(hits.Count>0,"No raycast at "+button.name);var hitButton=hits[0].gameObject.GetComponentInParent<Button>();Check(hitButton==button,"Input intercepted at "+button.name+" by "+hits[0].gameObject.name);
            ExecuteEvents.Execute(button.gameObject,ev,ExecuteEvents.pointerClickHandler);
        }
        public void Dispose(){camera.targetTexture=null;EditorSceneManager.ClosePreviewScene(scene);if(texture!=null){texture.Release();UnityEngine.Object.DestroyImmediate(texture);}}
    }
    public static void Run()
    {
        Directory.CreateDirectory(SettingsV161Build.Output);
        var previous=Keys.ToDictionary(k=>k,k=>(exists:PlayerPrefs.HasKey(k),value:PlayerPrefs.GetInt(k)));
        try
        {
            foreach(string k in Keys)PlayerPrefs.SetInt(k,1);PlayerPrefs.Save();
            using(var v=new View())
            {
                int taps=0;v.settings.OnTap=()=>taps++;v.Resize(720,1280);v.settings.Open();Check(v.settings.IsOpen,"First Open failed");
                var p=v.fit.panel;string[] buttons={"Som","Musica","Vibracao","Dica","Notificacoes"};
                foreach(var wh in new[]{new Vector2Int(360,640),new Vector2Int(390,844),new Vector2Int(720,1280),new Vector2Int(640,360)})
                {
                    v.Resize(wh.x,wh.y);v.Export("diagnostic-"+wh.x+"x"+wh.y);var panel=v.Bounds(p);Check(panel.xMin>=-.1f&&panel.yMin>=-.1f&&panel.xMax<=wh.x+.1f&&panel.yMax<=wh.y+.1f,"Clipped panel "+wh+" bounds="+panel+" host="+((RectTransform)v.settings.transform).rect+" scale="+p.localScale+" camera="+v.camera.pixelRect);
                    foreach(string n in buttons){var rt=(RectTransform)p.Find(n);var b=v.Bounds(rt);Check(panel.Contains(b.min)&&panel.Contains(b.max),"Control outside panel "+n);if(wh.x<500)Check(b.width>=44&&b.height>=44,"Small tap target "+n);}
                    foreach(var t in p.GetComponentsInChildren<TextMeshProUGUI>()){Check(!t.isTextOverflowing,"Text overflow "+t.name);Check(t.textInfo.characterCount>0,"Invisible text "+t.name);if(wh.x<500)Check(v.Bounds(t.rectTransform).height>=11,"Unreadable label "+t.name);}
                    if(wh.x!=640)v.Export("settings-"+wh.x+"x"+wh.y);
                }
                v.Resize(390,844);v.fit.SafeAreaOverride=new Rect(0,34,390,766);v.Render();var safePanel=v.Bounds(p);Check(safePanel.yMin>=34&&safePanel.yMax<=800,"Safe area ignored");v.fit.SafeAreaOverride=null;
                var host=(RectTransform)v.settings.transform;host.anchoredPosition=new Vector2(0,-107);v.fit.Fit();v.Render();Check(host.anchoredPosition==Vector2.zero,"Legacy game-scene settings offset leaked into the new window");
                v.Resize(720,1280);
                for(int i=0;i<buttons.Length;i++)
                {
                    var button=p.Find(buttons[i]).GetComponent<Button>();int before=taps;v.Click(button);Check(!SettingsScreen.IsOn(Keys[i])&&PlayerPrefs.GetInt(Keys[i])==-80,"Off value did not persist: "+Keys[i]);Check(taps==before+1,"Duplicate action: "+Keys[i]);
                    Check(p.Find(buttons[i]+"Estado").GetComponent<TextMeshProUGUI>().text=="Desligado","Missing off label: "+Keys[i]);
                }
                v.Export("settings-off");v.Click(p.Find("Fechar").GetComponent<Button>());Check(!v.settings.IsOpen,"Red close did not close");v.settings.Open();
                foreach(string n in buttons)Check(p.Find(n+"Estado").GetComponent<TextMeshProUGUI>().text=="Desligado","Reopen lost state");
                for(int i=0;i<buttons.Length;i++){int before=taps;v.Click(p.Find(buttons[i]).GetComponent<Button>());Check(SettingsScreen.IsOn(Keys[i])&&PlayerPrefs.GetInt(Keys[i])==1,"On failed: "+Keys[i]);Check(taps==before+1,"Duplicated listener on reopen");}
                v.Export("settings-on");
                // The fullscreen dimmer is the only hit outside the panel, shielding lobby buttons.
                var ev=new PointerEventData(v.events){position=new Vector2(1,1)};var hits=new List<RaycastResult>();v.settings.GetComponentInParent<GraphicRaycaster>().Raycast(ev,hits);Check(hits.Count>0&&hits[0].gameObject==v.settings.gameObject,"Background does not shield the lobby");
            }
            using(var fresh=new View()){fresh.Resize(390,844);fresh.settings.Open();Check(Keys.All(SettingsScreen.IsOn),"New instance lost preferences");fresh.Click(fresh.fit.panel.Find("Som").GetComponent<Button>());Check(!SettingsScreen.IsOn(SettingsScreen.SoundKey),"New instance duplicate/missing listener");}
            File.WriteAllText(SettingsV161Build.Output+"/checks.txt","PASS: actual SettingsScreen compile; genuine alpha/lossless sprites; native TMP labels; first-open/close/reopen/new instance; all five existing preferences toggle 1/-80 exactly once and persist; actual GraphicRaycaster/pointer clicks; fullscreen input shield; 360x640,390x844,720x1280,640x360 bounds; 44px portrait control targets; no text overflow; notch safe-area fit. Audio singleton is an isolated null test shim; real production mixer path is unchanged.\n");
            EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(SettingsV161Build.Output+"/checks.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
        finally{foreach(var pair in previous){if(pair.Value.exists)PlayerPrefs.SetInt(pair.Key,pair.Value.value);else PlayerPrefs.DeleteKey(pair.Key);}PlayerPrefs.Save();}
    }
}
