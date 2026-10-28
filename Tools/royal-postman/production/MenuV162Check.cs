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

public static class MenuV162Check
{
    static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
    sealed class View:IDisposable
    {
        readonly Scene scene;public readonly GameObject canvas;public readonly Camera camera;public readonly EventSystem events;
        public readonly RoyalPostmanMenuWindow menu;public readonly RoyalPostmanSettingsFit fit;public readonly SettingsScreen settings;
        public RenderTexture texture;public int width,height;
        public View(bool throughRuntimeKit=false)
        {
            scene=EditorSceneManager.NewPreviewScene();canvas=new GameObject("NativeMenuCanvas",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));SceneManager.MoveGameObjectToScene(canvas,scene);
            var cam=new GameObject("NativeMenuCamera",typeof(Camera));SceneManager.MoveGameObjectToScene(cam,scene);camera=cam.GetComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.03f,.035f,.10f);
            var c=canvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=5;
            var e=new GameObject("NativeMenuEvents",typeof(EventSystem));SceneManager.MoveGameObjectToScene(e,scene);events=e.GetComponent<EventSystem>();
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(MenuV162Build.Prefab);Check(source!=null&&!source.activeSelf,"Menu prefab missing or open by default");
            var settingsSource=AssetDatabase.LoadAssetAtPath<GameObject>(MenuV162Build.SettingsPrefab);settings=UnityEngine.Object.Instantiate(settingsSource,canvas.transform).GetComponent<SettingsScreen>();
            typeof(SettingsScreen).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(settings,null);
            if(throughRuntimeKit)
            {
                var kit=ScriptableObject.CreateInstance<RoyalPostmanRuntimeKit>();kit.additionalScreens.Add(new RoyalPostmanRuntimeKit.Screen{id="menu",title="Menu",prefab=source,hideNavigation=true});
                menu=kit.InstantiateScreen("menu",(RectTransform)canvas.transform).GetComponent<RoyalPostmanMenuWindow>();UnityEngine.Object.DestroyImmediate(kit);
            }
            else menu=UnityEngine.Object.Instantiate(source,canvas.transform).GetComponent<RoyalPostmanMenuWindow>();
            fit=menu.GetComponent<RoyalPostmanSettingsFit>();
        }
        public void Resize(int w,int h){width=w;height=h;camera.targetTexture=null;if(texture!=null){texture.Release();UnityEngine.Object.DestroyImmediate(texture);}texture=new RenderTexture(w,h,24){antiAliasing=4};texture.Create();camera.targetTexture=texture;camera.aspect=(float)w/h;Render();}
        public void Render(){Canvas.ForceUpdateCanvases();fit.Fit();settings.GetComponent<RoyalPostmanSettingsFit>().Fit();Canvas.ForceUpdateCanvases();foreach(var t in canvas.GetComponentsInChildren<TextMeshProUGUI>())t.ForceMeshUpdate();camera.Render();}
        public Rect Bounds(RectTransform rt){var corners=new Vector3[4];rt.GetWorldCorners(corners);var p=corners.Select(v=>(Vector2)camera.WorldToScreenPoint(v)).ToArray();return Rect.MinMaxRect(p.Min(v=>v.x),p.Min(v=>v.y),p.Max(v=>v.x),p.Max(v=>v.y));}
        public void Export(string name){Render();var old=RenderTexture.active;RenderTexture.active=texture;var t=new Texture2D(width,height,TextureFormat.RGBA32,false);t.ReadPixels(new Rect(0,0,width,height),0,0);t.Apply();File.WriteAllBytes(MenuV162Build.Output+"/"+name+".png",t.EncodeToPNG());RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(t);}
        public void Click(Button button){Render();var ev=new PointerEventData(events){position=Bounds((RectTransform)button.transform).center,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();canvas.GetComponent<GraphicRaycaster>().Raycast(ev,hits);Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Tap intercepted: "+button.name);ExecuteEvents.Execute(button.gameObject,ev,ExecuteEvents.pointerClickHandler);}
        public void Dispose(){camera.targetTexture=null;EditorSceneManager.ClosePreviewScene(scene);if(texture!=null){texture.Release();UnityEngine.Object.DestroyImmediate(texture);}}
    }
    public static void Run()
    {
        Directory.CreateDirectory(MenuV162Build.Output);
        try
        {
            using(var v=new View())
            {
                var dispatch=new MenuV162DispatchHarness{menu=v.menu,settings=v.settings};int taps=0,visibility=0,oldCallback=0;
                v.menu.Bind(_=>oldCallback++,()=>taps++,()=>visibility++);
                // Rebinding callbacks must not multiply Button listeners or leave old handlers alive.
                for(int i=0;i<8;i++)v.menu.Bind(dispatch.Navigate,()=>taps++,()=>visibility++);
                v.Resize(720,1280);v.menu.Open();v.menu.Open();Check(taps==1&&visibility==1,"Open is not idempotent");
                var p=v.fit.panel;var settingsPanel=v.settings.transform.Find("Janela");
                var close=p.Find("Fechar").GetComponent<Button>();Check(close.targetGraphic==p.Find("FecharArte").GetComponent<Image>(),"Close target lost");
                Check(p.Find("FecharArte").GetComponent<Image>().sprite==settingsPanel.Find("FecharArte").GetComponent<Image>().sprite,"Close art was regenerated");
                var originalFrame=settingsPanel.Find("Moldura").GetComponent<Image>().sprite;
                Check(p.Find("Cabecalho").GetComponent<Image>().sprite.texture==originalFrame.texture&&p.Find("Corpo").GetComponent<Image>().sprite.texture==originalFrame.texture,"Header/body duplicate or substitute PNG");
                Check(p.Find("Titulo").GetComponent<TextMeshProUGUI>().fontSharedMaterial==settingsPanel.Find("Titulo").GetComponent<TextMeshProUGUI>().fontSharedMaterial,"Title material drift");
                foreach(var wh in new[]{new Vector2Int(360,640),new Vector2Int(390,844),new Vector2Int(720,1280),new Vector2Int(640,360)})
                {
                    v.Resize(wh.x,wh.y);v.Export("menu-"+wh.x+"x"+wh.y);var bounds=v.Bounds(p);Check(bounds.xMin>=-.1f&&bounds.yMin>=-.1f&&bounds.xMax<=wh.x+.1f&&bounds.yMax<=wh.y+.1f,"Panel clipped "+wh+" bounds="+bounds+" host="+((RectTransform)v.menu.transform).rect+" scale="+p.localScale);
                    var previous=new Rect();
                    for(int i=0;i<MenuV162Build.Names.Length;i++)
                    {
                        var row=p.Find(MenuV162Build.Names[i]);var b=v.Bounds((RectTransform)row.Find("Botao"));
                        Check(bounds.Contains(b.min)&&bounds.Contains(b.max),"Menu control outside frame");if(wh.x<500)Check(b.height>=44&&b.width>=44,"Tap target below 44px");
                        if(i>0)Check(!b.Overlaps(previous),"Neighbouring controls overlap");previous=b;
                        var paint=row.Find("Arte").GetComponent<Image>();var sourcePaint=settingsPanel.Find("VibracaoArte").GetComponent<Image>();Check(paint.sprite==sourcePaint.sprite&&paint.type==sourcePaint.type&&paint.pixelsPerUnitMultiplier==sourcePaint.pixelsPerUnitMultiplier,"Capsule style drift");
                        Check(row.Find("Botao").GetComponent<Button>().targetGraphic==paint,"Press feedback points outside cloned row");
                        var reusedIcon=row.Find("Icone").GetComponent<Image>().sprite;Check(reusedIcon!=null,"Missing reused icon");Check(reusedIcon.texture.mipmapCount>1&&reusedIcon.texture.filterMode==FilterMode.Trilinear,"Reused small icon has no minification filter");
                    }
                    foreach(var t in p.GetComponentsInChildren<TextMeshProUGUI>())Check(!t.isTextOverflowing&&t.textInfo.characterCount>0,"Text overflow/missing: "+t.name);
                }
                v.Resize(390,844);v.fit.SafeAreaOverride=new Rect(0,34,390,766);v.Render();var safe=v.Bounds(p);Check(safe.yMin>=34&&safe.yMax<=800,"Safe area ignored");v.fit.SafeAreaOverride=null;
                v.Click(close);v.menu.Close();Check(!v.menu.IsOpen&&taps==2&&visibility==2,"Close/repeated close action wrong");
                string[] expected={"tab:1","tab:3","tab:4","tab:0","profile","inventory","shop","settings"};
                for(int i=0;i<MenuV162Build.Names.Length;i++)
                {
                    v.menu.Open();int before=dispatch.Calls.Count,tapBefore=taps;var button=p.Find(MenuV162Build.Names[i]+"/Botao").GetComponent<Button>();v.Click(button);
                    Check(dispatch.Calls.Count==before+1&&dispatch.Calls.Last()==expected[i],"Wrong or duplicate existing destination: "+MenuV162Build.Names[i]);
                    Check(!v.menu.IsOpen&&dispatch.WasClosedBeforeNavigation,"Popup stayed over destination");Check(taps==tapBefore,"Menu duplicates the destination's click sound");
                    // A queued second Button invocation after dismissal must not navigate twice.
                    button.onClick.Invoke();Check(dispatch.Calls.Count==before+1,"Double tap navigated twice");
                    if(i==7){Check(v.settings.IsOpen,"Settings transition failed");v.Export("menu-to-settings");v.Click(settingsPanel.Find("Fechar").GetComponent<Button>());Check(!v.settings.IsOpen&&!v.menu.IsOpen,"Settings close left another popup active");}
                }
                Check(oldCallback==0,"Stale Bind callback survived");
                v.menu.Open();v.Render();var ev=new PointerEventData(v.events){position=new Vector2(1,1)};var hits=new List<RaycastResult>();v.canvas.GetComponent<GraphicRaycaster>().Raycast(ev,hits);Check(hits.Count>0&&hits[0].gameObject==v.menu.gameObject,"Dimmer does not block underlying lobby");
                int dismissTaps=taps;v.menu.Dismiss();v.menu.Dismiss();Check(!v.menu.IsOpen&&taps==dismissTaps,"Quiet dismissal plays duplicate sound");
                v.menu.Open();v.Export("menu-final");
            }
            using(var editorView=new View(true))
            {
                editorView.menu.Bind(_=>{},()=>{},()=>{});editorView.Resize(390,844);editorView.menu.Open();
                var viewport=editorView.menu.GetComponent<RoyalPostmanViewport>();Check(viewport!=null,"RuntimeKit did not attach its existing viewport");
                viewport.SafeAreaOverride=new Rect(0,34,390,766);viewport.Fit();editorView.Render();editorView.Render();
                var bounds=editorView.Bounds(editorView.fit.panel);Check(bounds.xMin>=0&&bounds.xMax<=390&&bounds.yMin>=34&&bounds.yMax<=800,"Editor/runtime fitter divergence");
                Check(editorView.fit.SafeAreaOverride.HasValue,"Editor safe area not passed to shared window fitter");editorView.Export("menu-via-runtime-kit");
                editorView.Click(editorView.fit.panel.Find("Fechar").GetComponent<Button>());Check(!editorView.menu.IsOpen,"RuntimeKit view close failed");
            }
            File.WriteAllText(MenuV162Build.Output+"/checks.txt","PASS: actual menu component and exact extracted LobbyController destination switch compile; eight real pointer/raycast routes exactly once; repeated Bind/open/close/dismiss and double-tap guard; menu closes before destination; existing Settings prefab opens/closes; fullscreen lobby input shield; reused title material, close sprite, capsule sprite/9-slice/press targets and original frame texture; 360x640/390x844/720x1280/640x360 bounds; portrait44px targets; no overlapping controls or text overflow; notch safe-area. Existing route handlers beyond the switch are observed test boundaries, not full-game/audio/device tests. No new PNGs.\n");EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(MenuV162Build.Output+"/checks.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void CheckInstalledRegistration()
    {
        try
        {
            AssetDatabase.Refresh();
            var kit=AssetDatabase.LoadAssetAtPath<RoyalPostmanRuntimeKit>("Assets/RoyalAves/Resources/RoyalPostmanUI2026/RuntimeKit.asset");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(MenuV162Build.Prefab);
            Check(kit!=null&&prefab!=null&&kit.ScreenPrefab("menu")==prefab,"Installed RuntimeKit does not resolve the real Menu prefab");
            Check(!kit.ShowsNavigation("menu")&&!prefab.activeSelf,"Menu registered with wrong footer/default visibility");
            Check(prefab.GetComponent<RoyalPostmanMenuWindow>()!=null&&prefab.GetComponent<RoyalPostmanSettingsFit>()!=null,"Installed view/controller/fitter missing");
            Check(prefab.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"Installed Menu has missing scripts");
            File.WriteAllText(MenuV162Build.Output+"/registration.txt","PASS: installed real RuntimeKit asset resolves the exact Menu prefab, hides legacy navigation, starts closed and contains the real controller/shared fitter with no missing scripts.\n");EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(MenuV162Build.Output+"/registration.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
