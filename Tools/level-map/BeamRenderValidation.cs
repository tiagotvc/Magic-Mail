using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using UnityEditor.SceneManagement;
using RoyalAves.Meta.WorldMap;
public static class BeamRenderValidation
{
 public static void Run(){try{
 ShaderUtil.allowAsyncCompilation=false;
 var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("BeamRender");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
 var cameraGo=new GameObject("Camera");cameraGo.transform.SetParent(root.transform);var cam=cameraGo.AddComponent<Camera>();cam.scene=scene;cam.enabled=false;cam.GetUniversalAdditionalCameraData().SetRenderer(Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath).rendererIndex);cam.orthographic=true;cam.orthographicSize=3.4f;cam.transform.position=new Vector3(0,3,-10);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.1f,.25f,.15f);var rt=new RenderTexture(640,800,24);rt.Create();cam.targetTexture=rt;
 var beam=new Material(Shader.Find("RoyalAves/WorldMap/Beam"));var sparkle=new Material(Shader.Find("RoyalAves/WorldMap/Sparkle"));var fx=new WorldMapLightFx(root.transform,0,beam,sparkle);var shaft=new WorldMapLightSet.Shaft{angle=0,length=6,width=4,sparkles=0,intensity=.6f};
 foreach(float spacing in new[]{0f,1f}){shaft.raySpacing=spacing;fx.TickShaft(shaft,0,1);UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(cam,new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;var image=new Texture2D(640,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,640,800),0,0);image.Apply();File.WriteAllBytes("../beam-spacing-"+spacing+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
 RenderTexture.active=null;fx.Dispose();UnityEngine.Object.DestroyImmediate(beam);UnityEngine.Object.DestroyImmediate(sparkle);rt.Release();UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(scene);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);return;}EditorApplication.Exit(0);}
}


