using System;using System.IO;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.Rendering;using UnityEngine.SceneManagement;using UnityEngine.UI;using RoyalAves.UI.RoyalPostman2026;
namespace RoyalAves.EditorTools
{
 public enum RoyalPostmanButtonPreview { Game, Active, Inactive }
 public sealed class RoyalPostmanScreenDocument : IDisposable
 {
  public readonly RoyalPostmanRuntimeKit Kit;public readonly string Id;
  public GameObject Root{get;private set;}public Camera Camera{get;private set;}public RenderTexture Texture{get;private set;}
  public int Width{get;private set;}public int Height{get;private set;}public bool Dirty;
  public RoyalPostmanButtonPreview ButtonPreview { get; set; }
  public bool ReadOnly { get; set; }
  Scene scene;GameObject canvas;string sourceFingerprint;public GameObject Source=>Kit.ScreenPrefab(Id);
  string SourceFingerprint()=>Hash128.Compute(File.ReadAllText(AssetDatabase.GetAssetPath(Source))).ToString();
  public bool SourceChanged=>Source!=null&&sourceFingerprint!=SourceFingerprint();
  public RoyalPostmanScreenDocument(RoyalPostmanRuntimeKit kit,string id,int width,int height){
   Kit=kit;Id=id;scene=EditorSceneManager.NewPreviewScene();canvas=new GameObject("ScreenEditorCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));SceneManager.MoveGameObjectToScene(canvas,scene);
   try{
   var camera=new GameObject("ScreenEditorCamera",typeof(Camera));SceneManager.MoveGameObjectToScene(camera,scene);Camera=camera.GetComponent<Camera>();Camera.enabled=false;Camera.scene=scene;Camera.orthographic=true;Camera.transform.position=new Vector3(0,0,-10);Camera.clearFlags=CameraClearFlags.SolidColor;Camera.backgroundColor=Color.black;
   var c=canvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=Camera;c.planeDistance=5;
   var s=canvas.GetComponent<CanvasScaler>();s.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;s.referenceResolution=new Vector2(1080,1920);s.matchWidthOrHeight=0;
   Root=kit.InstantiateScreen(id,(RectTransform)canvas.transform);if(Root==null)throw new InvalidOperationException("Tela sem prefab: "+id);sourceFingerprint=SourceFingerprint();Resize(width,height);
   }catch{Dispose();throw;}
  }
  public bool Locked(Transform node){var nav=Root.transform.Find("Navigation");return Id!="home"&&node!=null&&nav!=null&&(node==nav||node.IsChildOf(nav));}
  public void Resize(int width,int height){
   Width=Mathf.Clamp(width,240,8192);Height=Mathf.Clamp(height,240,8192);Camera.targetTexture=null;if(Texture!=null){Texture.Release();UnityEngine.Object.DestroyImmediate(Texture);}
   Texture=new RenderTexture(Width,Height,24){antiAliasing=4,hideFlags=HideFlags.HideAndDontSave};Texture.Create();Camera.targetTexture=Texture;Camera.aspect=(float)Width/Height;
   Canvas.ForceUpdateCanvases();Root.GetComponent<RoyalPostmanViewport>().Fit();Render();
  }
  public void Render(){
   Canvas.ForceUpdateCanvases();Root.GetComponent<RoyalPostmanViewport>().Fit();Canvas.ForceUpdateCanvases();
   // The editor does not run ScrollRect.LateUpdate; settle its bounds and thumb after fitting.
   foreach(var scroll in canvas.GetComponentsInChildren<ScrollRect>())scroll.Rebuild(CanvasUpdate.PostLayout);
   bool? interactable=ButtonPreview==RoyalPostmanButtonPreview.Game?(bool?)null:ButtonPreview==RoyalPostmanButtonPreview.Active;
   foreach(var visual in Root.GetComponentsInChildren<RoyalPostmanActionVisual>(true))visual.Refresh(interactable);
   Canvas.ForceUpdateCanvases();
   var request=new RenderPipeline.StandardRequest{destination=Texture};if(RenderPipeline.SupportsRenderRequest(Camera,request))RenderPipeline.SubmitRenderRequest(Camera,request);else Camera.Render();
  }
  public Rect Bounds(RectTransform node){var corners=new Vector3[4];node.GetWorldCorners(corners);var lo=Vector2.positiveInfinity;var hi=Vector2.negativeInfinity;foreach(var c in corners){var p=(Vector2)Camera.WorldToScreenPoint(c);lo=Vector2.Min(lo,p);hi=Vector2.Max(hi,p);}return Rect.MinMaxRect(lo.x,lo.y,hi.x,hi.y);}
  public RectTransform Pick(Vector2 pixel){var graphics=Root.GetComponentsInChildren<Graphic>(false);for(int i=graphics.Length-1;i>=0;i--){var g=graphics[i];if(g.color.a<=.001f||Locked(g.transform))continue;if(RectTransformUtility.RectangleContainsScreenPoint(g.rectTransform,pixel,Camera))return g.rectTransform;}return null;}
  public void PreviewClick(RectTransform pick){
   var view=Root.GetComponent<RoyalPostmanPetsScreen>();if(view==null)return;
   if(pick!=null){
    if(pick==view.currentTab.transform||pick.IsChildOf(view.currentTab.transform)){view.ShowCurrent(true);Render();return;}
    if(pick==view.incubatorTab.transform||pick.IsChildOf(view.incubatorTab.transform)){view.ShowCurrent(false);Render();return;}
    if(view.previousIncubator!=null&&(pick==view.previousIncubator.transform||pick.IsChildOf(view.previousIncubator.transform))){view.StepIncubator(-1);Render();return;}
    if(view.nextIncubator!=null&&(pick==view.nextIncubator.transform||pick.IsChildOf(view.nextIncubator.transform))){view.StepIncubator(1);Render();return;}
    var profile=view.currentPanel.GetComponent<RoyalPostmanCurrentPet>();if(profile!=null&&profile.PreviewClick(pick)){Render();return;}
    for(int i=0;i<view.items.Length;i++)if(pick==view.items[i].button.transform||pick.IsChildOf(view.items[i].button.transform)){view.SelectItem(i);Render();return;}
    if(view.dismissItemTooltip!=null&&(pick==view.dismissItemTooltip.transform||pick.IsChildOf(view.dismissItemTooltip.transform))){view.HideItemTooltip();Render();return;}
    if(view.tooltipCard!=null&&(pick==view.tooltipCard||pick.IsChildOf(view.tooltipCard)))return;
   }
   view.HideItemTooltip();Render();
  }
  static void RestoreRoot(RectTransform target,RectTransform original){target.anchorMin=original.anchorMin;target.anchorMax=original.anchorMax;target.pivot=original.pivot;target.anchoredPosition=original.anchoredPosition;target.sizeDelta=original.sizeDelta;target.localScale=original.localScale;target.localRotation=original.localRotation;}
  static void Unpack(GameObject go){if(PrefabUtility.IsPartOfPrefabInstance(go)&&PrefabUtility.IsAnyPrefabInstanceRoot(go))PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);}
  public void Save(){
   if(ReadOnly)throw new InvalidOperationException("A simulação usa dados temporários e não salva alterações no prefab.");
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Edite e salve fora do Play.");
   if(sourceFingerprint!=SourceFingerprint())throw new InvalidOperationException("A tela foi atualizada no projeto. Clique em Recarregar antes de continuar, para preservar a atualização.");
   foreach(var hover in Root.GetComponentsInChildren<RoyalPostmanInventoryHover>(true))hover.PreviewHover(false);
   Root.GetComponent<RoyalPostmanPetsScreen>()?.ResetTooltipPreview();
   var copy=UnityEngine.Object.Instantiate(Root);copy.name=Source.name;
   try{
    var petsCopy=copy.GetComponent<RoyalPostmanPetsScreen>();if(petsCopy!=null){foreach(var item in petsCopy.items)item.selection.SetActive(false);petsCopy.currentPanel.GetComponent<RoyalPostmanCurrentPet>()?.ResetPreview();petsCopy.SelectIncubator(0);petsCopy.ShowCurrent(petsCopy.defaultCurrentTab);}
    Unpack(copy);if(Source.GetComponent<RoyalPostmanViewport>()==null){var viewport=copy.GetComponent<RoyalPostmanViewport>();if(viewport!=null)UnityEngine.Object.DestroyImmediate(viewport);}
    RestoreRoot((RectTransform)copy.transform,(RectTransform)Source.transform);
    // Full-bleed paint dimensions belong to the current device, not the authored prefab.
    if(Id=="pets")foreach(var path in new[]{"Background","Header/Background","Header/GoldRail","Inventory/Background","Room","Room/WorkshopBackground","Body/Workshop/ControlPanel"}){
     var target=copy.transform.Find(path) as RectTransform;var authored=Source.transform.Find(path) as RectTransform;
     if(target!=null&&authored!=null)RestoreRoot(target,authored);
    }
    var nav=copy.transform.Find("Navigation");
    // Preview colors/materials never become the authored button state when a layout edit is saved.
    foreach(var visual in copy.GetComponentsInChildren<RoyalPostmanActionVisual>(true))visual.Refresh();
    if(nav!=null&&Kit.navigation!=null){
     if(Id=="home"){
      var navCopy=UnityEngine.Object.Instantiate(nav.gameObject);navCopy.name="Navigation";
      try{Unpack(navCopy);PrefabUtility.SaveAsPrefabAsset(navCopy,AssetDatabase.GetAssetPath(Kit.navigation));}finally{UnityEngine.Object.DestroyImmediate(navCopy);}
      var rect=(RectTransform)nav;var anchors=new[]{rect.anchorMin,rect.anchorMax,rect.pivot,rect.anchoredPosition,rect.sizeDelta};var scale=rect.localScale;int sibling=nav.GetSiblingIndex();UnityEngine.Object.DestroyImmediate(nav.gameObject);
      var shared=(GameObject)PrefabUtility.InstantiatePrefab(Kit.navigation,copy.transform);shared.name="Navigation";shared.transform.SetSiblingIndex(sibling);
      var r=(RectTransform)shared.transform;r.anchorMin=anchors[0];r.anchorMax=anchors[1];r.pivot=anchors[2];r.anchoredPosition=anchors[3];r.sizeDelta=anchors[4];r.localScale=scale;
     }else UnityEngine.Object.DestroyImmediate(nav.gameObject);
    }
    PrefabUtility.SaveAsPrefabAsset(copy,AssetDatabase.GetAssetPath(Source));AssetDatabase.SaveAssets();sourceFingerprint=SourceFingerprint();Dirty=false;
   }finally{UnityEngine.Object.DestroyImmediate(copy);}
  }
  public void Export(string path){Render();var prior=RenderTexture.active;var image=new Texture2D(Width,Height,TextureFormat.RGBA32,false);try{RenderTexture.active=Texture;image.ReadPixels(new Rect(0,0,Width,Height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(image);}}
  public void Dispose(){if(Camera!=null)Camera.targetTexture=null;if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);if(Texture!=null){Texture.Release();UnityEngine.Object.DestroyImmediate(Texture);}Root=null;Texture=null;}
 }
}

