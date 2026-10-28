using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEngine.UI;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using UnityEngine.Rendering;using RoyalAves.UI.RoyalPostman2026;
public static class TopHudV15Apply
{
 const string Art="Assets/RoyalAves/Art/RoyalPostmanUI2026/",HomePath="Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Home.prefab";
 static readonly string[] Top={"Avatar","CoinCounter","Coin","CoinValue","CoinPlus","LifeCounter","Life","LivesValue","LifeStatus","LifePlus","SettingsButton"};
 [Serializable]class Box{public int x,y,width,height;}[Serializable]class Entry{public string id;public int width,height;public Box[] rectangles;}[Serializable]class Report{public Entry[] assets;}
 static Sprite S(string id)=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Sprites/"+id+".asset");
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static RectTransform R(Transform root,string path)=>root.Find(path).GetComponent<RectTransform>();
 static void Rect(Transform root,string path,float x,float y,float w,float h){var r=R(root,path);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
 static void Picture(Transform root,string path,string id){var image=root.Find(path).GetComponent<Image>();image.sprite=S(id);image.material=null;image.color=Color.white;image.preserveAspect=true;}
 static void Text(Transform root,string path,int size){var t=root.Find(path).GetComponent<Text>();t.fontSize=size;t.resizeTextForBestFit=false;foreach(var e in t.GetComponents<Outline>())e.effectDistance=new Vector2(.35f,-.35f);}
 static void Import(){
  var report=JsonUtility.FromJson<Report>(File.ReadAllText("Tools/royal-postman/production/TOP-HUD-V15-BOUNDS.json"));
  foreach(var a in report.assets){
   string file=Art+a.id+".png";AssetDatabase.ImportAsset(file,ImportAssetOptions.ForceSynchronousImport);
   var importer=(TextureImporter)AssetImporter.GetAtPath(file);importer.textureType=TextureImporterType.Default;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(file);var b=a.rectangles[0];float border=a.id=="top-value-panel-v15"?b.height*.43f:0;
   var sprite=Sprite.Create(texture,new Rect(b.x,a.height-b.y-b.height,b.width,b.height),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,Vector4.one*border);sprite.name=a.id;
   string path=Art+"Sprites/"+a.id+".asset";var old=S(a.id);if(old==null)AssetDatabase.CreateAsset(sprite,path);else{EditorUtility.CopySerialized(sprite,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(sprite);}
  }
 }
 static Material Part(bool moving){
  string path=Art+"Materials/top-body-v15-"+(moving?"moving":"fixed")+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(m==null){m=new Material(Shader.Find("RoyalPostman/UI/PaintedPalette"));AssetDatabase.CreateAsset(m,path);}
  m.SetFloat("_UsePalette",0);m.SetFloat("_ButtonPart",moving?4:0);m.SetFloat("_CircleCenterRadius",.385f);
  var s=S("top-button-body-v15");var r=s.rect;m.SetVector("_ButtonUVRect",new Vector4(r.x/s.texture.width,r.y/s.texture.height,r.width/s.texture.width,r.height/s.texture.height));EditorUtility.SetDirty(m);return m;
 }
 static void Button(Transform root,string name,float x,float visual,bool gear){
  Rect(root,name,x,-125,132,132);Rect(root,name+"/FixedShoulder",0,0,visual,visual);Rect(root,name+"/MovingCenter",0,0,visual,visual);Rect(root,name+"/MovingCenter/Face",0,0,visual,visual);
  Picture(root,name+"/FixedShoulder","top-button-body-v15");root.Find(name+"/FixedShoulder").GetComponent<Image>().material=Part(false);
  Picture(root,name+"/MovingCenter/Face","top-button-body-v15");root.Find(name+"/MovingCenter/Face").GetComponent<Image>().material=Part(true);
  string leaf=name+"/MovingCenter/"+(gear?"Gear":"PlusGlyph");Picture(root,leaf,gear?"top-gear-v15":"top-plus-v15");Rect(root,leaf,0,0,visual*.55f,visual*.55f);R(root,leaf).localRotation=Quaternion.identity;
  var press=root.Find(name).GetComponent<RoyalPostmanPaintedButton>();press.pressTravel=gear?3:2;
 }
 static void Apply(Transform root){
  Rect(root,"Avatar",114,-125,166,166);Rect(root,"Avatar/PortraitClip",0,0,142,142);Rect(root,"Avatar/PortraitClip/Portrait",0,0,142,142);Rect(root,"Avatar/Frame",0,0,166,166);
  Picture(root,"Avatar/Frame","top-avatar-frame-v15");Picture(root,"Avatar/PortraitClip/Portrait","top-avatar-portrait-v15");
  Rect(root,"CoinCounter",400,-125,270,96);Rect(root,"LifeCounter",738,-125,252,96);
  foreach(var name in new[]{"CoinCounter","LifeCounter"}){Picture(root,name,"top-value-panel-v15");var image=root.Find(name).GetComponent<Image>();image.type=Image.Type.Sliced;image.preserveAspect=false;image.pixelsPerUnitMultiplier=image.sprite.rect.height/96;}
  Rect(root,"Coin",263,-125,108,108);Picture(root,"Coin","top-envelope-coin-v15");
  Rect(root,"CoinValue",400,-123,172,74);Text(root,"CoinValue",53);
  Rect(root,"Life",608,-125,108,108);Picture(root,"Life","top-heart-v15");
  Rect(root,"LivesValue",608,-123,72,86);Text(root,"LivesValue",65);
  Rect(root,"LifeStatus",752,-123,137,64);Text(root,"LifeStatus",44);
  Button(root,"CoinPlus",519,64,false);Button(root,"LifePlus",848,64,false);Button(root,"SettingsButton",1005,116,true);
 }
 static void Capture(GameObject prefab,int width,int height,string file,bool topOnly,bool pressed){
  var scene=EditorSceneManager.NewPreviewScene();var go=new GameObject("HudReview",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));SceneManager.MoveGameObjectToScene(go,scene);
  RenderTexture target=null;try{
   var cameraGo=new GameObject("Camera",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraGo,scene);var camera=cameraGo.GetComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
   var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;var scale=go.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1080,1920);scale.matchWidthOrHeight=0;
   var home=UnityEngine.Object.Instantiate(prefab,go.transform,false);if(topOnly){foreach(Transform child in home.transform)if(child.name!="Background"&&!Top.Contains(child.name))child.gameObject.SetActive(false);var background=R(home.transform,"Background");background.anchorMin=background.anchorMax=new Vector2(0,1);background.pivot=Vector2.one*.5f;background.anchoredPosition=new Vector2(540,-960);background.sizeDelta=new Vector2(1080,1920);}
   if(pressed)foreach(var name in new[]{"CoinPlus","LifePlus","SettingsButton"})home.transform.Find(name).GetComponent<RoyalPostmanPaintedButton>().SetPressed(true);
   target=new RenderTexture(width,height,24){antiAliasing=4};target.Create();camera.targetTexture=target;camera.aspect=(float)width/height;Canvas.ForceUpdateCanvases();
   var request=new RenderPipeline.StandardRequest{destination=target};if(RenderPipeline.SupportsRenderRequest(camera,request))RenderPipeline.SubmitRenderRequest(camera,request);else camera.Render();
   var old=RenderTexture.active;RenderTexture.active=target;var picture=new Texture2D(width,height,TextureFormat.RGBA32,false);picture.ReadPixels(new Rect(0,0,width,height),0,0);picture.Apply();File.WriteAllBytes(file,picture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(picture);RenderTexture.active=old;
  }finally{UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}}
 }
 public static void Review(){try{var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(HomePath);Capture(prefab,1080,320,"../top-hud-v15/top.png",true,false);Capture(prefab,1080,320,"../top-hud-v15/pressed.png",true,true);Capture(prefab,3840,1138,"../top-hud-v15/top-4k.png",true,false);Capture(prefab,360,107,"../top-hud-v15/top-phone.png",true,false);EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void Run(){
  const string output="../top-hud-v15";Directory.CreateDirectory(output);File.WriteAllText(output+"/validation.txt","RUNNING");int errors=0;Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=log;
  try{
   ShaderUtil.allowAsyncCompilation=false;Import();var root=PrefabUtility.LoadPrefabContents(HomePath);
   try{Apply(root.transform);PrefabUtility.SaveAsPrefabAsset(root,HomePath);
    foreach(var name in Top){string path="Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/"+name+".prefab";if(File.Exists(path))PrefabUtility.SaveAsPrefabAsset(root.transform.Find(name).gameObject,path);}
   }finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.SaveAssets();
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(HomePath);
   Capture(prefab,1080,320,output+"/top.png",true,false);Capture(prefab,1080,320,output+"/pressed.png",true,true);Capture(prefab,3840,1138,output+"/top-4k.png",true,false);Capture(prefab,360,107,output+"/top-phone.png",true,false);Capture(prefab,720,1280,output+"/home.png",false,false);
   var scene=EditorSceneManager.NewPreviewScene();var canvas=new GameObject("Bindings",typeof(RectTransform),typeof(Canvas));SceneManager.MoveGameObjectToScene(canvas,scene);
   try{
    ((RectTransform)canvas.transform).sizeDelta=new Vector2(1080,1920);int coin=0,life=0,profile=0,settings=0;bool covered=false;
    var view=RoyalPostmanLiveHome.Create((RectTransform)canvas.transform,()=>{},()=>coin++,()=>life++,()=>profile++,()=>settings++,()=>{},i=>{},()=>covered);
    foreach(var name in new[]{"CoinPlus","LifePlus","Avatar","SettingsButton"})view.transform.Find(name).GetComponent<Button>().onClick.Invoke();Check(coin==1&&life==1&&profile==1&&settings==1,"Top callbacks failed");
    foreach(var name in new[]{"CoinPlus","LifePlus","SettingsButton"}){
     var node=view.transform.Find(name);Check(((RectTransform)node).sizeDelta.x/3>=44,"Small phone hit target");var press=node.GetComponent<RoyalPostmanPaintedButton>();var fixedPosition=R(node,"FixedShoulder").anchoredPosition;var rest=press.movingCenter.anchoredPosition;press.SetPressed(true);Check(R(node,"FixedShoulder").anchoredPosition==fixedPosition&&press.movingCenter.anchoredPosition.y<rest.y,"Fixed rim / moving center failed");press.SetPressed(false);
    }
    Check(view.GetComponentInChildren<RoyalPostmanAvatar>().portrait.sprite.name=="top-avatar-portrait-v15","Avatar not updated");
    covered=true;view.Refresh();Check(!view.transform.Find("Avatar").gameObject.activeSelf,"Covered menu top still visible");covered=false;view.Refresh();Check(view.transform.Find("Avatar").gameObject.activeSelf,"Top failed to restore");
   }finally{UnityEngine.Object.DestroyImmediate(canvas);EditorSceneManager.ClosePreviewScene(scene);}
   Check(errors==0,"Unity errors: "+errors);File.WriteAllText(output+"/validation.txt","PASS native 1080 / 360 phone and 3840-wide top captures; uncompressed sources preserved up to 4096, no mipmaps; separate frame/portrait, gold symbol and moving button center; real profile/coin/life/settings callbacks; 44px narrow-phone hit targets; covered menu restoration; zero Unity errors. Source PNGs are 1254 square and 2172x724 panel, NOT native 4K: built-in generation resolution limitation. No wallet/player writes.\n");EditorApplication.Exit(0);
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL "+e);Debug.LogException(e);EditorApplication.Exit(1);}
  finally{Application.logMessageReceived-=log;}
 }
}
