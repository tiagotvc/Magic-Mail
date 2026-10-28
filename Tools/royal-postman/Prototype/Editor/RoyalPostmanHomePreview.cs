using System;
using System.IO;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    // This renders the SAME home component used by LobbyController, in a disposable scene.
    public sealed class RoyalPostmanHomePreview : ScriptableObject
    {
        Scene scene;
        Camera camera;
        RenderTexture capture;
        public RoyalPostmanHome Home { get; private set; }
        public Texture SceneTexture=>capture;
        public int PlaySelections { get; private set; }
        public static RoyalPostmanHomePreview CreateScenePreview(int width,int height)
        {
            var preview=CreateInstance<RoyalPostmanHomePreview>();preview.hideFlags=HideFlags.HideAndDontSave;
            try{preview.Build(width,height);return preview;}
            catch{DestroyImmediate(preview);throw;}
        }
        void Build(int width,int height)
        {
            scene=EditorSceneManager.NewPreviewScene();
            var cameraObject=new GameObject("RoyalPostmanPreviewCamera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
            camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.orthographic=true;
            camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            capture=new RenderTexture(width,height,24){name="RoyalPostmanHomePreview",hideFlags=HideFlags.HideAndDontSave,antiAliasing=4};capture.Create();
            camera.targetTexture=capture;camera.aspect=width/(float)height;
            var root=new GameObject("RoyalPostmanPreviewCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(root,scene);
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=0;
            Home=RoyalPostmanHome.Create((RectTransform)root.transform,Resources.Load<LobbyFeaturesConfig>(LobbyFeaturesConfig.ResourcePath),
                Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath),()=>PlaySelections++,()=>{},()=>{},()=>{},()=>{},tab=>Home.SelectTab(tab));
            Home.Refresh(PlayerPrefs.GetInt("Gems",0),PlayerPrefs.GetInt("Lifes",5),"Cheio",Mathf.Max(1,MetaProgress.NextLevel),true);
            RenderScenePreview();
        }
        public bool RenderScenePreview(float delta=0,bool animate=false)
        {
            if(camera==null||capture==null)return false;
            Canvas.ForceUpdateCanvases();
            var request=new RenderPipeline.StandardRequest{destination=capture};
            if(RenderPipeline.SupportsRenderRequest(camera,request))RenderPipeline.SubmitRenderRequest(camera,request);else camera.Render();
            return false;
        }
        public void PressAt(Vector2 uv,bool down)
        {
            if(Home==null)return;
            var rect=(RectTransform)Home.PlayButton.transform;
            bool hit=RectTransformUtility.RectangleContainsScreenPoint(rect,new Vector2(uv.x*capture.width,uv.y*capture.height),camera);
            Home.PlayPress.SetPressed(down&&hit);RenderScenePreview();
        }
        public void Save(string path)
        {
            RenderScenePreview();var previous=RenderTexture.active;var picture=new Texture2D(capture.width,capture.height,TextureFormat.RGBA32,false);
            try{RenderTexture.active=capture;picture.ReadPixels(new Rect(0,0,capture.width,capture.height),0,0);picture.Apply();Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,picture.EncodeToPNG());}
            finally{RenderTexture.active=previous;DestroyImmediate(picture);}
        }
        void OnDisable()
        {
            // Destroy explicitly before scene close so owned label materials and the separate nav are disposed.
            if(Home!=null)DestroyImmediate(Home.gameObject);Home=null;
            if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);camera=null;
            if(capture!=null){capture.Release();DestroyImmediate(capture);capture=null;}
        }

        [MenuItem("Royal Postman/Capturar tela principal")]
        static void Capture()
        {
            var file=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Tools/royal-postman/previews/home-live-editor.png");
            var p=CreateScenePreview(720,1600);try{p.Save(file);}finally{DestroyImmediate(p);}Debug.Log("Royal Postman: "+file);
        }
    }
}
