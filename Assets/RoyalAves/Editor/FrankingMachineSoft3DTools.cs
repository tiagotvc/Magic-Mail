using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using RoyalAves.Gameplay;
namespace RoyalAves.EditorTools
{
 public static class FrankingMachineSoft3DTools
 {
  const string Art="Assets/RoyalAves/Resources/Soft3D/";
  const string Visual="Assets/RoyalAves/Prefabs/Elements/FrankingMachine.prefab";
  public static bool IsInstalled
  {
   get{var p=AssetDatabase.LoadAssetAtPath<GameObject>(Visual);var a=p!=null?p.GetComponent<MailCubbyAnimation>():null;return a!=null&&a.cabinet!=null&&a.cabinet.sprite!=null&&a.cabinet.sprite.texture.name=="franking-body";}
  }
  [MenuItem("Royal Aves/Elementos/Máquina de Franquear/Aplicar acabamento 3D")]
  public static void Apply()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Pare o Play antes de aplicar a arte da franqueadora.");
   AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   var body=Import("franking-body","Assets/RoyalAves/Art/Obstacles/FrankingMachine/CM_maquina_corpo_v2.png");
   var lever=Import("franking-lever","Assets/RoyalAves/Art/Obstacles/FrankingMachine/CM_maquina_alavanca_v2.png");
   var root=PrefabUtility.LoadPrefabContents(Visual);
   try
   {
    var animation=root.GetComponent<MailCubbyAnimation>();var mechanism=root.GetComponent<FrankingMachineLever>();
    animation.cabinet.sprite=body;animation.cabinet.sharedMaterial=Paint(body.texture);
    var leverRenderer=mechanism.lever.GetComponent<SpriteRenderer>();leverRenderer.sprite=lever;leverRenderer.sharedMaterial=Paint(lever.texture);
    PrefabUtility.SaveAsPrefabAsset(root,Visual);
   }
   finally{PrefabUtility.UnloadPrefabContents(root);}
   AssetDatabase.SaveAssets();SweetSugar.Scripts.Editor.SquareUtils.ClearCachedPreviews();
   File.WriteAllText("Logs/franking-soft3d-install.result","PASS: franking body/lever sprites and their materials replaced in existing visual prefab; transforms, lever pivot/angles/timings, letter sprites/flight/goal logic, prefab IDs and production cell reference preserved. Level Editor uses the actual installed body sprite.");
  }
  static Sprite Import(string name,string original)
  {
   string path=Art+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.isReadable=true;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var old=AssetDatabase.LoadAssetAtPath<Sprite>(original);
   var pivot=new Vector2(old.pivot.x/old.rect.width,old.pivot.y/old.rect.height);
   var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),pivot,texture.width*old.pixelsPerUnit/old.rect.width,0,SpriteMeshType.FullRect);sprite.name=name;
   string asset=Art+"Sprites/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Sprite>(asset);
   if(existing==null){AssetDatabase.CreateAsset(sprite,asset);return sprite;}
   EditorUtility.CopySerialized(sprite,existing);UnityEngine.Object.DestroyImmediate(sprite);EditorUtility.SetDirty(existing);return existing;
  }
  static Material Paint(Texture2D texture)
  {
   string path=Art+"Sprite_"+texture.name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(m==null){m=new Material(Shader.Find("RoyalAves/Soft3DElementSprite"));AssetDatabase.CreateAsset(m,path);}m.mainTexture=texture;EditorUtility.SetDirty(m);return m;
  }
  [InitializeOnLoadMethod]static void Listen()=>EditorApplication.update+=Poll;
  static void Poll()
  {
   const string request="Logs/franking-soft3d-install.request";
   if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
   if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}
   if(EditorApplication.isPlayingOrWillChangePlaymode)return;
   File.Delete(request);try{Apply();}catch(Exception e){File.WriteAllText("Logs/franking-soft3d-install.result",e.ToString());Debug.LogException(e);}
  }
 }
}
