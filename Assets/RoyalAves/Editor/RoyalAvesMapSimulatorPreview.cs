using System;
using System.Collections.Generic;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RoyalAves.UI.RoyalPostman2026;

namespace RoyalAves.EditorTools
{
    // A disposable image of the real map replaces the legacy room in Game/Simulator edit mode.
    // Both its canvas and the renderer's preview scene are excluded from scene saves and builds.
    [InitializeOnLoad]
    public static class RoyalAvesMapSimulatorPreview
    {
        static GameObject overlay;
        static Canvas lobby;
        static readonly List<(Canvas canvas,bool enabled)> coveredCanvases=new List<(Canvas,bool)>();
        static RoyalAvesLevelMapPreview renderer;
        static RoyalPostmanSimulatorRenderer postman;
        static double nextFrame;
        static bool failed;
        static Vector2Int size;
        static bool dirty,moving,dragging;
        static bool moved;
        static Vector2 pressPosition;
        static double lastFrame,lastDrag;

        static RoyalAvesMapSimulatorPreview()
        {
            EditorApplication.update+=Update;
            EditorApplication.playModeStateChanged+=_=>Cleanup();
            AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;
            EditorApplication.quitting+=Cleanup;
            EditorApplication.projectChanged+=Invalidate;
            Undo.undoRedoPerformed+=Invalidate;
            EditorSceneManager.activeSceneChangedInEditMode+=(_,__)=>Invalidate();
            EditorApplication.delayCall+=CleanupOrphanedPreviews;
        }

        static void CleanupOrphanedPreviews()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            foreach(var view in Resources.FindObjectsOfTypeAll<WorldMapView>())
            {
                if(view.Route!=null||view.name!="WorldMap3D"||(view.gameObject.hideFlags&HideFlags.DontSaveInEditor)==0)continue;
                // Old previews could lose their managed owner on assembly reload and leave a live camera behind.
                var materials=new HashSet<Material>();var textures=new HashSet<RenderTexture>();
                foreach(var drawing in view.GetComponentsInChildren<Renderer>(true))
                    foreach(var material in drawing.sharedMaterials)
                        if(material!=null&&(material.hideFlags&HideFlags.DontSaveInEditor)!=0&&!AssetDatabase.Contains(material))materials.Add(material);
                foreach(var camera in view.GetComponentsInChildren<Camera>(true))
                    if(camera.targetTexture!=null&&(camera.targetTexture.hideFlags&HideFlags.DontSaveInEditor)!=0)textures.Add(camera.targetTexture);
                UnityEngine.Object.DestroyImmediate(view.gameObject);
                foreach(var material in materials)if(material!=null)UnityEngine.Object.DestroyImmediate(material);
                foreach(var texture in textures)if(texture!=null){texture.Release();UnityEngine.Object.DestroyImmediate(texture);}
            }
        }

        const string AnimationKey="RoyalAves.MapPreview.Animate";
        const string AnimationMenu="Royal Aves/Mapa de n\u00edveis/Animate editor preview";
        internal static bool AnimatePreview
        {
            get=>SessionState.GetBool(AnimationKey,false);
            set
            {
                if(value==AnimatePreview)return;
                SessionState.SetBool(AnimationKey,value);
                dirty=true;nextFrame=0;
            }
        }
        [MenuItem(AnimationMenu)]
        static void ToggleAnimation()=>AnimatePreview=!AnimatePreview;
        [MenuItem(AnimationMenu,true)]
        static bool ValidateAnimation(){Menu.SetChecked(AnimationMenu,AnimatePreview);return true;}

        static void Invalidate(){Cleanup();failed=false;}

        static Canvas FindLobby()
        {
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
                if(canvas.name=="RoyalAvesLobby"&&canvas.gameObject.scene.IsValid()
                    &&!EditorSceneManager.IsPreviewScene(canvas.gameObject.scene))return canvas;
            return null;
        }

        static Vector2Int Resolution()
        {
            return ResolutionFor(Handles.GetMainGameViewSize());
        }

        internal static Vector2Int ResolutionFor(Vector2 view)
        {
            var aspect=view.y/Mathf.Max(1,view.x);
            if(aspect<=1)aspect=16f/9f; // Portrait game, including headless/editor startup before the Simulator has a size.
            // The complete UI is flattened into this texture. A 540px target discarded half
            // the authored HUD detail before Scene/Simulator could display or enlarge it.
            // Keep a phone-sized baseline even when the docked Game view is small.
            var width=Mathf.Clamp(Mathf.RoundToInt(view.y>view.x?view.x:1080),1080,2160);
            return new Vector2Int(width,Mathf.Clamp(Mathf.RoundToInt(width*aspect),width,4096));
        }

        static void Update()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)
            {if(overlay!=null)Cleanup();return;}
            if(failed||EditorApplication.timeSinceStartup<nextFrame)return;
            var now=EditorApplication.timeSinceStartup;
            nextFrame=now+(dirty||moving?1d/30:.5);
            RoyalAvesMapPreviewInput.Handler=HandleInput; // estatico: some no domain reload
            var current=FindLobby();
            if(current==null){if(overlay!=null)Cleanup();return;}
            var dimensions=Resolution();
            try
            {
                if(overlay==null||current!=lobby||dimensions!=size)
                {
                    Cleanup();lobby=current;size=dimensions;
                    if(RoyalPostmanRuntimeKit.Load()!=null)postman=RoyalPostmanSimulatorRenderer.Create(size.x,size.y);
                    else renderer=RoyalAvesLevelMapPreview.CreateScenePreview(size.x,size.y);
                    RoyalAvesMapPreviewInput.Handler=HandleInput;
                    overlay=new GameObject("Preview temporário do mapa",typeof(RectTransform),typeof(Canvas),typeof(RoyalAvesMapPreviewInput));
                    overlay.hideFlags=HideFlags.HideAndDontSave;
                    SceneManager.MoveGameObjectToScene(overlay,lobby.gameObject.scene);
                    var canvas=overlay.GetComponent<Canvas>();
                    canvas.renderMode=RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder=32700;
                    var picture=new GameObject("Mapa",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));
                    picture.hideFlags=HideFlags.HideAndDontSave;
                    picture.transform.SetParent(overlay.transform,false);
                    var rect=(RectTransform)picture.transform;
                    rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                    var image=picture.GetComponent<RawImage>();image.texture=postman!=null?postman.SceneTexture:renderer.SceneTexture;image.raycastTarget=false;
                    // The preview replaces the lobby image. Covered canvases otherwise still render.
                    foreach(var original in lobby.GetComponentsInChildren<Canvas>(true))
                    {
                        coveredCanvases.Add((original,original.enabled));original.enabled=false;
                    }
                    lastFrame=now;
                    EditorApplication.QueuePlayerLoopUpdate();
                }
                if(postman!=null)return;
                if(AnimatePreview&&renderer.NeedsSceneAnimation)dirty=true;
                if(dirty||moving)
                {
                    moving=renderer.RenderScenePreview((float)(now-lastFrame),AnimatePreview);
                    dirty=false;lastFrame=now;
                    nextFrame=now+1d/30;
                    EditorApplication.QueuePlayerLoopUpdate();
                }
            }
            catch(Exception error)
            {
                Cleanup();failed=true;
                Debug.LogException(error);
            }
        }

        internal static bool HandleInput(EventType type,int button,Vector2 position,Vector2 delta,float height,float width=0)
        {
            if(postman!=null&&!EditorApplication.isPlayingOrWillChangePlaymode)return postman.Input(type,button,position,width,height);
            if(renderer==null||EditorApplication.isPlayingOrWillChangePlaymode||height<1)return false;
            var inMap=position.y>height*.13f&&position.y<height*.90f;
            var now=EditorApplication.timeSinceStartup;
            if(type==EventType.MouseDown&&button==0&&inMap)
            {
                dragging=true;moved=false;pressPosition=position;moving=false;lastDrag=now;return true;
            }
            if(type==EventType.MouseDrag&&dragging)
            {
                if(!moved&&(position-pressPosition).sqrMagnitude<36)return true;
                if(!moved){renderer.BeginSceneDrag();moved=true;}
                renderer.DragScenePreview(-delta.y/height,(float)(now-lastDrag));
                lastDrag=now;dirty=true;nextFrame=0;return true;
            }
            if((type==EventType.MouseUp||type==EventType.Ignore)&&dragging)
            {
                dragging=false;
                if(moved)renderer.EndSceneDrag();
                else if(type==EventType.MouseUp&&width>0)renderer.ClickScenePreview(new Vector2(position.x/width,1-position.y/height));
                moving=moved;dirty=true;nextFrame=0;return true;
            }
            if(type==EventType.ScrollWheel&&inMap)
            {
                renderer.ScrollScenePreview(-delta.y);moving=true;nextFrame=0;return true;
            }
            return false;
        }

        static void Cleanup()
        {
            foreach(var original in coveredCanvases)if(original.canvas!=null)original.canvas.enabled=original.enabled;
            coveredCanvases.Clear();
            if(overlay!=null)UnityEngine.Object.DestroyImmediate(overlay);
            if(renderer!=null)UnityEngine.Object.DestroyImmediate(renderer);
            if(postman!=null)UnityEngine.Object.DestroyImmediate(postman);
            overlay=null;renderer=null;postman=null;lobby=null;
            dirty=moving=dragging=false;
        }
    }
}
