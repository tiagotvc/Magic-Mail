using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEngine.UI;using UnityEditor;using TMPro;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class PostalPigV93Validation
{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 public static void Run(){const string output="../postal-pig-v93";Directory.CreateDirectory(output);File.WriteAllText(output+"/validation.txt","RUNNING");RoyalAvesLevelMapPreview preview=null;int errors=0;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{
   ShaderUtil.allowAsyncCompilation=false;preview=RoyalAvesLevelMapPreview.CreateScenePreview(720,1280);var pt=preview.GetType();var mt=typeof(CurvedLevelMap);var map=(CurvedLevelMap)pt.GetField("map",F).GetValue(preview);map.World.TimeOverride=0;
   WorldMapLetterVault Vault()=>(WorldMapLetterVault)mt.GetField("letterVault",F).GetValue(map);
   var sprite=Resources.Load<Sprite>(WorldMapLetterVault.ResourcePath);Check(sprite!=null,"Vault sprite missing");
   var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));Check(importer.maxTextureSize==512&&!importer.mipmapEnabled&&importer.textureType==TextureImporterType.Sprite,"Vault UI import settings wrong");
   var probe=new Texture2D(2,2,TextureFormat.RGBA32,false);try{ImageConversion.LoadImage(probe,File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));Check(probe.GetPixel(0,0).a<.01f&&probe.GetPixel(probe.width-1,probe.height-1).a<.01f&&probe.GetPixel(probe.width/2,probe.height/2).a>.9f,"Vault icon does not have real transparent background");}finally{UnityEngine.Object.DestroyImmediate(probe);}
   var apprentice=Resources.Load<WorldMapLibraryItem>("MapLibrary/Personagens/postal-apprentice-v93");
   Check(apprentice!=null&&apprentice.sprite!=null&&apprentice.category==WorldMapAssetCategory.Characters&&apprentice.kind==PropKind.Character,"Apprentice is not registered in Characters");
   var courier=Resources.Load<WorldMapLibraryItem>("MapLibrary/Personagens/courier-wizard-v50");
   Check(apprentice.size>.5f&&apprentice.size<courier.size*.6f,"Apprentice default size is not smaller than Courier");
   Check(RoyalAvesMapLibrary.Items(WorldMapAssetCategory.Characters).Contains(apprentice),"Apprentice absent from editor character picker");
   var apprenticeImporter=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(apprentice.sprite));Check(apprenticeImporter.DoesSourceTextureHaveAlpha(),"Apprentice lacks alpha");
   Check(sprite.name=="postal-pig","The old safe icon is still in use");
   void Capture(string name){map.PreviewMotion(0,false);Canvas.ForceUpdateCanvases();var previewDialog=map.transform.Find("LetterVaultPreview");foreach(var text in map.GetComponentsInChildren<TMP_Text>(false)){text.ForceMeshUpdate();if(previewDialog!=null&&text.transform.IsChildOf(previewDialog))Check(!text.isTextOverflowing,"Vault dialog text overflows: "+text.text);}pt.GetMethod("RenderCanvas",F).Invoke(preview,null);
    var target=(RenderTexture)pt.GetField("capture",F).GetValue(preview);var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(output+"/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=old;
   }
   void Layout(){Canvas.ForceUpdateCanvases();var r=(RectTransform)Vault().Button.transform;Check(r.anchorMin==Vector2.one&&r.pivot==Vector2.one&&-r.anchoredPosition.y>=WorldMapTopHud.Height+WorldMapTopHud.TopInset+10,"Vault overlaps top HUD");Check(r.sizeDelta.x*360/1080>=44&&r.anchoredPosition.x<0,"Vault tap area/clamp wrong");Check(Vault().Button.GetComponent<Image>().preserveAspect,"Vault sprite stretched");}
   map.PreviewAt(1);Check(Vault().Visible,"Vault shortcut not visible on Map");Layout();Capture("map-shortcut");
   map.PreviewAt(33);Check(Vault().Visible,"Permanent shortcut disappeared on another island");Capture("another-island");
   map.SetCovered(true);Check(!Vault().Visible,"Vault visible over Events/menu");map.SetCovered(false);Check(Vault().Visible,"Vault did not return on Map");
   map.SetChromeVisible(false);Check(!Vault().Visible,"Vault ignores global chrome hide");map.SetChromeVisible(true);Check(Vault().Visible,"Vault did not restore with chrome");
   var content=map.transform.parent;var header=content.Find("HUDDoMapa");var nav=content.Find("NavegacaoDoMapa");
   Vault().Button.onClick.Invoke();Check(Vault().DialogVisible&&!Vault().Visible&&!map.World.gameObject.activeSelf&&!header.gameObject.activeSelf&&!nav.gameObject.activeSelf,"Vault preview did not cover/pause Map");Capture("vault-preview");
   var dialog=map.transform.Find("LetterVaultPreview");Check(dialog.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="EM BREVE")&&!dialog.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("R$")||t.text.Contains("$")),"Preview contains misleading store price/readiness");
   dialog.Find("LetterVaultWindow/CloseLetterVault").GetComponent<Button>().onClick.Invoke();Check(!Vault().DialogVisible&&Vault().Visible&&map.World.gameObject.activeSelf&&header.gameObject.activeSelf&&nav.gameObject.activeSelf,"Close did not restore Map");
   Vault().Open();dialog.Find("Dimmer").GetComponent<Button>().onClick.Invoke();Check(!Vault().DialogVisible&&Vault().Visible,"Outside dismiss failed");
   pt.GetMethod("Build",F).Invoke(preview,new object[]{360,780,1});map=(CurvedLevelMap)pt.GetField("map",F).GetValue(preview);map.World.TimeOverride=0;map.PreviewAt(1);Layout();Capture("narrow-phone");Vault().Open();Capture("narrow-preview");Vault().Close();
   Check(errors==0,errors+" Unity errors");File.WriteAllText(output+"/validation.txt","PASS: glossy purple postal pig replaces safe in map shortcut and dialog; apprentice registered in editor Characters library at default 0.82 vs Courier 1.6, with alpha; transparent glossy pig sprite, 512 UI import without mipmaps; top-right shortcut below HUD with narrow-phone touch size; permanent across islands; hidden under menus/chrome; actual tap opens dedicated coming-soon explanation; map/HUD pause and restore on button/outside close; no fake price/balance or purchase action; standard/narrow native captures without text overflow; zero Unity errors. No store requests, wallet or player-save writes.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=logger;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}EditorApplication.Exit(0);
 }
}
