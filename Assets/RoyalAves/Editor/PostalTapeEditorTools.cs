using System;
using System.IO;
using RoyalAves.Gameplay;
using SweetSugar.Scripts.Blocks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace RoyalAves.EditorTools
{
    public static class PostalTapeEditorTools
    {
        public const string PrefabPath="Assets/SweetSugar/Resources/Blocks/PostalTape.prefab";
        public const string IconPath="Assets/RoyalAves/Art/Obstacles/PostalTape/TapeCross.png";
        [MenuItem("Royal Aves/Elementos/Fita postal/Preparar pincel do Level Editor")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before preparing the tape brush.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset("Assets/RoyalAves/Scripts/Gameplay/PostalTapePlacement.cs",ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            var strip=AssetDatabase.LoadAssetAtPath<Sprite>(PostalTapeLaboratory.SpritePath);
            if(strip==null)throw new InvalidOperationException("Missing approved tape strip sprite.");
            if(!File.Exists(IconPath))CreateIcon(strip);
            AssetDatabase.ImportAsset(IconPath,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(IconPath);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=384/1.95f;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var icon=AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
            var preview=EditorSceneManager.NewPreviewScene();var root=new GameObject("PostalTape");SceneManager.MoveGameObjectToScene(root,preview);
            try
            {
                var renderer=root.AddComponent<SpriteRenderer>();renderer.sprite=icon;
                var square=root.AddComponent<Square>();square.type=SquareTypes.PostalTape;square.canMoveIn=true;square.canMoveOut=false;square.aboveItem=false;square.score=0;square.sizeInSquares=Vector2Int.one;square.mainRenderer=renderer;
                root.AddComponent<BindLayer>().order=6;root.AddComponent<PostalTapePlacement>().strip=strip;
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally{EditorSceneManager.ClosePreviewScene(preview);}
            AssetDatabase.SaveAssets();
        }
        static void CreateIcon(Sprite strip)
        {
            // Native composition of the existing two strips; no new painted artwork.
            var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("TapeIconComposition");SceneManager.MoveGameObjectToScene(root,scene);
            var rt=new RenderTexture(384,384,24);var pixels=new Texture2D(384,384,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
            try
            {
                for(int i=0;i<2;i++)
                {
                    var child=new GameObject("Strip");child.transform.SetParent(root.transform,false);child.transform.localRotation=Quaternion.Euler(0,0,i==0?45:-45);child.transform.localScale=new Vector3(1.18f/strip.bounds.size.x,.15f/strip.bounds.size.y,1);
                    var view=child.AddComponent<SpriteRenderer>();view.sprite=strip;view.color=new Color(1,1,1,.60f);view.sortingOrder=i;
                }
                var cameraObject=new GameObject("Camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.orthographic=true;camera.orthographicSize=.5f;camera.transform.position=Vector3.back*10;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.targetTexture=rt;
                camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,384,384),0,0);pixels.Apply();File.WriteAllBytes(IconPath,pixels.EncodeToPNG());
            }
            finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(scene);}
        }
        [InitializeOnLoadMethod] static void Listen()=>EditorApplication.update+=Poll;
        static void Poll()
        {
            const string request="Logs/postal-tape-editor.request";
            if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete(request);try{Build();File.WriteAllText("Logs/postal-tape-editor.result","READY: Fita postal available as a production blocker brush; no tape goal.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/postal-tape-editor.result",e.ToString());}
        }
    }
}
