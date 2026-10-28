using System;
using System.IO;
using RoyalAves.UI.RoyalPostman2026;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    // Uses the shipped prefabs and textures, rather than the obsolete level-map preview.
    public sealed class RoyalPostmanSimulatorRenderer : ScriptableObject
    {
        Scene scene;Camera camera;RenderTexture capture;RectTransform home,modal;
        public Texture SceneTexture=>capture;
        public static RoyalPostmanSimulatorRenderer Create(int width,int height)
        {
            var renderer=CreateInstance<RoyalPostmanSimulatorRenderer>();renderer.hideFlags=HideFlags.HideAndDontSave;
            try{renderer.Build(width,height);return renderer;}catch{DestroyImmediate(renderer);throw;}
        }
        void Build(int width,int height)
        {
            var kit=RoyalPostmanRuntimeKit.Load();if(kit==null||kit.home==null||kit.playModal==null)throw new InvalidOperationException("Royal Postman RuntimeKit missing.");
            scene=EditorSceneManager.NewPreviewScene();
            var cameraObject=new GameObject("RoyalPostmanPreviewCamera",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraObject,scene);
            camera=cameraObject.GetComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);
            capture=new RenderTexture(width,height,24){antiAliasing=4};capture.Create();camera.targetTexture=capture;camera.aspect=width/(float)height;
            var root=new GameObject("RoyalPostmanPreviewCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));SceneManager.MoveGameObjectToScene(root,scene);
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=0;
            home=(RectTransform)kit.InstantiateScreen("home",(RectTransform)root.transform).transform;
            modal=(RectTransform)Instantiate(kit.playModal,root.transform,false).transform;modal.gameObject.SetActive(false);
            ValidateImages(home);ValidateImages(modal);Render();ValidateNavigation();
        }
        void ValidateNavigation()
        {
            var nav=(RectTransform)home.Find("Navigation");var bar=(RectTransform)nav.Find("ContinuousBar");
            if(Mathf.Abs(nav.rect.width-home.rect.width)>.1f||Mathf.Abs(bar.rect.width-nav.rect.width)>.1f)
                throw new InvalidOperationException("Navigation background does not fill the home width.");
            foreach(var divider in nav.GetComponentsInChildren<RoyalPostmanNavigationDivider>(true))
            {
                var seam=divider.rectTransform;
                float top=seam.anchoredPosition.y+seam.rect.yMax,bottom=seam.anchoredPosition.y+seam.rect.yMin;
                float redTop=nav.rect.yMax-RoyalPostmanNavigation.GoldRailHeight,redHeight=nav.rect.height-RoyalPostmanNavigation.GoldRailHeight;
                if(Mathf.Abs(top-redTop)>.01f||bottom<=nav.rect.yMin||Mathf.Abs(seam.rect.height/redHeight-RoyalPostmanNavigation.DividerRedFraction)>.001f)
                    throw new InvalidOperationException("Navigation divider does not match the marked red-area extent.");
            }
        }
        static void ValidateImages(RectTransform root)
        {
            int textures=0;
            foreach(var image in root.GetComponentsInChildren<Image>(true))
            {
                if(image.sprite==null)continue;
                if(image.sprite.texture==null)throw new InvalidOperationException("Unresolved texture: "+image.name);
                var bounds=image.sprite.rect;var texture=image.sprite.texture;
                if(bounds.xMin<0||bounds.yMin<0||bounds.xMax>texture.width+.1f||bounds.yMax>texture.height+.1f)
                    throw new InvalidOperationException("Sprite rectangle exceeds imported texture: "+image.name+" ("+bounds+" on "+texture.width+"x"+texture.height+")");
                textures++;
            }
            if(textures<20)throw new InvalidOperationException("Royal Postman prefab lost painted sprites: "+root.name+" ("+textures+")");
        }
        public void Render()
        {
            Canvas.ForceUpdateCanvases();
            home.GetComponent<RoyalPostmanViewport>().Fit();
            var request=new RenderPipeline.StandardRequest{destination=capture};
            if(RenderPipeline.SupportsRenderRequest(camera,request))RenderPipeline.SubmitRenderRequest(camera,request);else camera.Render();
        }
        public bool Input(EventType type,int button,Vector2 position,float width,float height)
        {
            if(button!=0||width<=0||height<=0)return false;
            var pixel=new Vector2(position.x/width*capture.width,(1-position.y/height)*capture.height);
            var active=modal.gameObject.activeSelf?modal:home;
            string[] buttons=modal.gameObject.activeSelf?new[]{"PlayButton","FreeButton","CloseButton"}:new[]{"LevelButton","PetButton"};
            foreach(string name in buttons)
            {
                var rect=active.Find(name) as RectTransform;
                if(rect==null||!RectTransformUtility.RectangleContainsScreenPoint(rect,pixel,camera))continue;
                rect.GetComponent<RoyalPostmanPaintedButton>()?.SetPressed(type==EventType.MouseDown);
                if(type==EventType.MouseUp){if(name=="LevelButton")modal.gameObject.SetActive(true);else if(name=="CloseButton")modal.gameObject.SetActive(false);}
                Render();return true;
            }
            if(type==EventType.MouseUp){foreach(var press in active.GetComponentsInChildren<RoyalPostmanPaintedButton>())press.SetPressed(false);Render();}
            return false;
        }
        void OnDisable()
        {
            if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);
            if(capture!=null){capture.Release();DestroyImmediate(capture);}capture=null;camera=null;
        }
        [MenuItem("Royal Postman/2026/Verificar importação do projeto e capturar")]
        public static void CaptureInstalled()
        {
            RoyalPostman2026KitBuilder.RepairSpriteReferences();
            var renderer=Create(900,1600);Texture2D image=null;var previous=RenderTexture.active;
            try
            {
                image=new Texture2D(900,1600,TextureFormat.RGBA32,false);RenderTexture.active=(RenderTexture)renderer.SceneTexture;
                image.ReadPixels(new Rect(0,0,900,1600),0,0);image.Apply();
                string folder=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Tools/royal-postman/integration");Directory.CreateDirectory(folder);
                File.WriteAllBytes(folder+"/installed-home.png",image.EncodeToPNG());
                renderer.modal.gameObject.SetActive(true);renderer.Render();image.ReadPixels(new Rect(0,0,900,1600),0,0);image.Apply();
                File.WriteAllBytes(folder+"/installed-play-modal.png",image.EncodeToPNG());
                File.WriteAllText(folder+"/SPRITE-IMPORT-CHECK.txt","PASS: installed RuntimeKit prefabs resolve at least 20 painted sprites per view, with live texture references.\nProject: "+Application.dataPath+"\n");
                Debug.Log("ROYAL_POSTMAN_INSTALLED_SPRITES_OK");
            }
            finally{RenderTexture.active=previous;if(image!=null)DestroyImmediate(image);DestroyImmediate(renderer);}
        }
    }
}
