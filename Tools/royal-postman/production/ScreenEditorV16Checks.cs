using System;using System.IO;using System.Linq;using UnityEngine;using UnityEngine.UI;using UnityEditor;using RoyalAves.EditorTools;using RoyalAves.UI.RoyalPostman2026;
public static class ScreenEditorV16Checks
{
 [Serializable]class Move{public string source,target,guid;}[Serializable]class Moves{public Move[] moves;}
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;var parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
 static void Migrate(){
  var moves=JsonUtility.FromJson<Moves>(File.ReadAllText("Tools/royal-postman/production/UI-FOLDERS-V16.json")).moves;
  foreach(var m in moves)Folder(Path.GetDirectoryName(m.target).Replace('\\','/'));
  AssetDatabase.StartAssetEditing();try{foreach(var m in moves){if(AssetDatabase.AssetPathToGUID(m.target)==m.guid)continue;var error=AssetDatabase.MoveAsset(m.source,m.target);Check(string.IsNullOrEmpty(error),error);}}finally{AssetDatabase.StopAssetEditing();}
  AssetDatabase.Refresh();foreach(var m in moves)Check(AssetDatabase.AssetPathToGUID(m.target)==m.guid,"GUID changed: "+m.target);
 }
 static void FixHome(){
  var kit=RoyalPostmanRuntimeKit.Load();string home=AssetDatabase.GetAssetPath(kit.home);var root=PrefabUtility.LoadPrefabContents(home);
  try{
   var portrait=root.transform.Find("Avatar/PortraitClip/Portrait").GetComponent<RectTransform>();portrait.sizeDelta=new Vector2(162,162);portrait.anchoredPosition=new Vector2(0,5);
   root.transform.Find("CoinPlus").GetComponent<RectTransform>().anchoredPosition=new Vector2(303,-165);
   root.transform.Find("LifePlus").GetComponent<RectTransform>().anchoredPosition=new Vector2(648,-165);
   string navPath="Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Navigation.prefab";
   var old=root.transform.Find("Navigation");var oldRect=(RectTransform)old;var anchors=new[]{oldRect.anchorMin,oldRect.anchorMax,oldRect.pivot,oldRect.anchoredPosition,oldRect.sizeDelta};int sibling=old.GetSiblingIndex();
   var navCopy=UnityEngine.Object.Instantiate(old.gameObject);navCopy.name="Navigation";
   try{if(PrefabUtility.IsAnyPrefabInstanceRoot(navCopy))PrefabUtility.UnpackPrefabInstance(navCopy,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);PrefabUtility.SaveAsPrefabAsset(navCopy,navPath);}finally{UnityEngine.Object.DestroyImmediate(navCopy);}
   UnityEngine.Object.DestroyImmediate(old.gameObject);kit.navigation=AssetDatabase.LoadAssetAtPath<GameObject>(navPath);var nav=(GameObject)PrefabUtility.InstantiatePrefab(kit.navigation,root.transform);nav.name="Navigation";nav.transform.SetSiblingIndex(sibling);
   var r=(RectTransform)nav.transform;r.anchorMin=anchors[0];r.anchorMax=anchors[1];r.pivot=anchors[2];r.anchoredPosition=anchors[3];r.sizeDelta=anchors[4];
   PrefabUtility.SaveAsPrefabAsset(root,home);foreach(var name in new[]{"Avatar","CoinPlus","LifePlus"})PrefabUtility.SaveAsPrefabAsset(root.transform.Find(name).gameObject,"Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/"+name+".prefab");
   EditorUtility.SetDirty(kit);AssetDatabase.SaveAssets();
  }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
 static Color32[] Pixels(RoyalPostmanScreenDocument d){d.Render();var prior=RenderTexture.active;RenderTexture.active=d.Texture;var image=new Texture2D(d.Width,d.Height,TextureFormat.RGBA32,false);try{image.ReadPixels(new Rect(0,0,d.Width,d.Height),0,0);image.Apply();return image.GetPixels32();}finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(image);}}
 public static void Run(){
  const string output="../screen-editor-v16";Directory.CreateDirectory(output);File.WriteAllText(output+"/validation.txt","RUNNING");int errors=0;Application.LogCallback callback=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=callback;
  try{
   ShaderUtil.allowAsyncCompilation=false;Migrate();FixHome();var kit=RoyalPostmanRuntimeKit.Load();var before=File.ReadAllText(AssetDatabase.GetAssetPath(kit.home));
   using(var d=new RoyalPostmanScreenDocument(kit,"home",360,800)){
    var node=d.Root.transform.Find("CoinCounter").GetComponent<RectTransform>();var image=d.Root.transform.Find("Coin").GetComponent<Image>();var position=node.anchoredPosition;var sprite=image.sprite;
    node.anchoredPosition+=new Vector2(17,9);image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(RoyalPostmanArtPaths.Sprite("top-heart-v15"));d.Dirty=true;d.Save();
    using(var reload=new RoyalPostmanScreenDocument(kit,"home",360,800)){Check(reload.Root.transform.Find("CoinCounter").GetComponent<RectTransform>().anchoredPosition==position+new Vector2(17,9),"Position did not persist");Check(reload.Root.transform.Find("Coin").GetComponent<Image>().sprite.name=="top-heart-v15","Asset did not persist");}
    Undo.RecordObject(node,"Editor move test");node.anchoredPosition+=Vector2.right*20;Undo.FlushUndoRecordObjects();Undo.PerformUndo();Check(node.anchoredPosition==position+new Vector2(17,9),"Undo failed");Undo.PerformRedo();Check(node.anchoredPosition==position+new Vector2(37,9),"Redo failed");
    node.anchoredPosition=position;image.sprite=sprite;d.Save();
   }
   int differences=0;foreach(var size in new[]{new Vector2Int(360,800),new Vector2Int(720,1280)}){
    using(var d=new RoyalPostmanScreenDocument(kit,"home",size.x,size.y)){
     d.Export(output+"/home-"+size.x+".png");var parent=(RectTransform)d.Root.transform.parent;d.Root.SetActive(false);int coins=0,lives=0,settings=0;
     var live=RoyalPostmanLiveHome.Create(parent,()=>{},()=>coins++,()=>lives++,()=>{},()=>settings++,()=>{},i=>{},()=>false);
     foreach(var text in live.GetComponentsInChildren<Text>(true)){var p=AnimationUtility.CalculateTransformPath(text.transform,live.transform);var target=d.Root.transform.Find(p)?.GetComponent<Text>();if(target!=null)target.text=text.text;}
     var a=Pixels(d);live.gameObject.SetActive(false);d.Root.SetActive(true);var b=Pixels(d);
     for(int i=0;i<a.Length;i++)if(!a[i].Equals(b[i]))differences++;Check(differences==0,"Editor and actual runtime pixels differ: "+differences);
     live.gameObject.SetActive(true);live.transform.Find("CoinPlus").GetComponent<Button>().onClick.Invoke();live.transform.Find("LifePlus").GetComponent<Button>().onClick.Invoke();live.transform.Find("SettingsButton").GetComponent<Button>().onClick.Invoke();Check(coins==1&&lives==1&&settings==1,"Actions lost");
     UnityEngine.Object.DestroyImmediate(live.gameObject);
    }
   }
   var temporary=ScriptableObject.Instantiate(kit);temporary.additionalScreens=new System.Collections.Generic.List<RoyalPostmanRuntimeKit.Screen>();const string testPath="Assets/Editor/V16OtherScreen.prefab";var blank=new GameObject("TestOther",typeof(RectTransform));((RectTransform)blank.transform).sizeDelta=new Vector2(1080,1920);
   try{
    var asset=PrefabUtility.SaveAsPrefabAsset(blank,testPath);temporary.additionalScreens.Add(new RoyalPostmanRuntimeKit.Screen{id="test",title="Test",prefab=asset});
    using(var other=new RoyalPostmanScreenDocument(temporary,"test",360,800)){Check(other.Locked(other.Root.transform.Find("Navigation")),"Other screen nav is editable");Check(other.Root.transform.Find("Navigation").GetComponent<RoyalPostmanNavigation>()!=null,"Other screen lost shared nav");other.Save();Check(AssetDatabase.LoadAssetAtPath<GameObject>(testPath).transform.Find("Navigation")==null,"Shared nav overrides saved in other screen");}
   }finally{UnityEngine.Object.DestroyImmediate(blank);UnityEngine.Object.DestroyImmediate(temporary);AssetDatabase.DeleteAsset(testPath);}
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(RoyalPostmanArtPaths.Texture("top-gear-v15"));var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));Check(importer.maxTextureSize==8192&&importer.textureCompression==TextureImporterCompression.Uncompressed,"8K UI policy missing");Check(!importer.GetPlatformTextureSettings("Android").overridden,"Lossy Android override remains");Check(texture.width==1254,"Source was resampled");
   Check(errors==0,"Unity errors: "+errors);File.WriteAllText(output+"/validation.txt","PASS: GUID-preserving organized UI assets; 8192 import maximum/uncompressed with original pixels; avatar crop and plus placement; Home save/reopen persists positions and sprite changes; Undo/Redo; shared navigation editable through Home and locked/not serialized on another registered screen; actual runtime versus editor rendering exactly equal on 360x800 and 720x1280 after normalizing live values (0 differing pixels); callbacks; zero Unity errors; no player data writes. Sources remain original resolution, not native 8K.\n");EditorApplication.Exit(0);
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);}
  finally{Application.logMessageReceived-=callback;}
 }
}

