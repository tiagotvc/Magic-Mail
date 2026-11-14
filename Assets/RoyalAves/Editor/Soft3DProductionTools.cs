using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using RoyalAves.Gameplay;
using RoyalAves.Effects;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
namespace RoyalAves.EditorTools
{
 public static class Soft3DProductionTools
 {
  const string Art="Assets/RoyalAves/Resources/Soft3D/";
  static Material spriteMaterial,lensMaterial;
  static Sprite[] crate,ink,bag,scroll;static Sprite rune,potionMaster,rack,flasks,signal;
  public static bool IsInstalled=>Resources.Load<GameObject>("Blocks/EnchantedStone")?.GetComponent<Soft3DElementStyle>()!=null;
  [MenuItem("Royal Aves/Elementos/Remodelagem 3D/Aplicar seis elementos na partida")]
  public static void Apply()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Pare o Play antes de aplicar os prefabs.");
   Directory.CreateDirectory(Art+"Sprites");AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   spriteMaterial=Material("Sprite","RoyalAves/Soft3DElementSprite");lensMaterial=Material("Lens","RoyalAves/Soft3DElementSprite");Material("Solid","RoyalAves/Soft3DElementSolid");Material("Drop","Sprites/Default");
   crate=Sheet("crate-states",3,1,1.85f);ink=Sheet("inkwell-states",5,1,1.85f);bag=Sheet("bag-opening",4,4,2);scroll=Sheet("scroll-opening",4,1,1.2f);
   rune=Sheet("rune",1,1,1.85f)[0];potionMaster=Sheet("potions",1,1,2)[0];rack=Sheet("potion-rack",1,1,4)[0];flasks=Sheet("potion-bottles",1,1,4)[0];signal=Sheet("signal",1,1,1.85f)[0];
   foreach(var texture in new[]{crate[0].texture,ink[0].texture,bag[0].texture,scroll[0].texture,rune.texture,potionMaster.texture,rack.texture,flasks.texture,signal.texture}){var m=Material("Sprite_"+texture.name,"RoyalAves/Soft3DElementSprite");m.mainTexture=texture;EditorUtility.SetDirty(m);}
   lensMaterial.mainTexture=signal.texture;EditorUtility.SetDirty(lensMaterial);
   Edit("Assets/SweetSugar/Resources/Blocks/SolidBlock.prefab",root=>
   {
    foreach(var layer in root.GetComponentsInChildren<ParcelStackLayer>(true)){Style(layer.gameObject);layer.visualScale=Vector2.one;layer.visualOffset=Vector2.zero;layer.GetComponent<SpriteRenderer>().sprite=crate[layer.remainingLayers-1];}
   });
   EditFamily("EnchantedStone",EnchantedStoneLaboratory.PrefabPath,root=>root.GetComponent<SpriteRenderer>().sprite=rune);
   EditFamily("Inkwell",InkwellLaboratory.PrefabPath,root=>{root.GetComponent<SpriteRenderer>().sprite=ink[0];root.GetComponent<InkwellCell>().stages=ink;});
   EditFamily("RoutePotions",RouteCentralLaboratory.CellPath,root=>{var cell=root.GetComponent<RouteCentralCell>();cell.artwork=potionMaster;cell.standArtwork=rack;cell.flaskArtwork=flasks;cell.soft3D=true;root.GetComponent<SpriteRenderer>().sprite=potionMaster;});
   EditFamily("BagOpenings",MailbagLaboratory.CellPath,root=>{var cell=root.GetComponent<BagOpeningStation>();cell.frames=bag;cell.scrollFrames=Enumerable.Range(0,16).Select(i=>scroll[Mathf.Min(3,i/4)]).ToArray();root.GetComponent<SpriteRenderer>().sprite=bag[0];});
   EditFamily("SortingSignals",SortingSignalLaboratory.PrefabPath,root=>{root.GetComponent<SpriteRenderer>().sprite=signal;root.GetComponent<SortingSignalCell>().lensTemplate=lensMaterial;});
   var config=Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");
   foreach(var pair in new[]{("SolidBlock",crate[0]),("EnchantedStone",rune),("Inkwell",ink[0]),("RoutePotions",potionMaster),("BagOpenings",scroll[3]),("SortingSignals",signal)})
   {
    var target=config.targets.Single(t=>t.name==pair.Item1);target.defaultSprites=new SprArrays{new SprArray{sprites=new SpriteList{new SpriteObject{icon=pair.Item2,uiSprite=true}}}};
   }
   EditorUtility.SetDirty(config);AssetDatabase.SaveAssetIfDirty(config);AssetDatabase.SaveAssets();SweetSugar.Scripts.Editor.SquareUtils.ClearCachedPreviews();Validate();
   File.WriteAllText("Logs/soft3d-production.result","PASS: six production families and existing lab prefabs updated in place; old prefab IDs retained; normal Level Editor and goal default icons refreshed; no campaign levels rewritten.");
  }
  static Material Material(string name,string shader)
  {
   var sh=Shader.Find(shader);if(sh==null)throw new Exception("Missing shader "+shader);string path=Art+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(sh);AssetDatabase.CreateAsset(material,path);}else material.shader=sh;EditorUtility.SetDirty(material);return material;
  }
  static Sprite[] Sheet(string name,int cols,int rows,float size)
  {
   string path=Art+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Default;imp.isReadable=true;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.npotScale=TextureImporterNPOTScale.None;imp.maxTextureSize=4096;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.filterMode=FilterMode.Bilinear;imp.SaveAndReimport();var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);int w=tex.width/cols,h=tex.height/rows;var result=new Sprite[cols*rows];
   // Frame-registration measures only opaque rows, ignoring generated low-alpha fringe.
   int low=h,high=0,left=w,right=0;
   for(int y=0;y<h;y++){int count=0;for(int x=0;x<w;x++)if(tex.GetPixel(x,(rows-1)*h+y).a>.96f)count++;if(count>w/5){low=Math.Min(low,y);high=Math.Max(high,y);}}
   for(int x=0;x<w;x++){int count=0;for(int y=0;y<h;y++)if(tex.GetPixel(x,(rows-1)*h+y).a>.96f)count++;if(count>h/5){left=Math.Min(left,x);right=Math.Max(right,x);}}
   if(name=="scroll-opening"){low=0;high=h-1;left=0;right=w-1;}
   if(high<=low||right<=left)throw new Exception("Invalid opaque art bounds "+name);
   float ppu=Mathf.Max(right-left+1,high-low+1)/size;
   for(int i=0;i<result.Length;i++)
   {
    var rect=new Rect(i%cols*w,(rows-1-i/cols)*h,w,h);var pivot=new Vector2((left+right+1f)*.5f/w,(low+high+1f)*.5f/h);
    if(name=="bag-opening")
    {
     int baseY=0;for(int y=0;y<h;y++){int count=0;for(int x=w/5;x<w*4/5;x++)if(tex.GetPixel((int)rect.x+x,(int)rect.y+y).a>.96f)count++;if(count>w/3){baseY=y;break;}}pivot=new Vector2(.5f,(baseY+2f)/h);ppu=w/2f;
    }
    if(name=="scroll-opening"){pivot=Vector2.one*.5f;ppu=w/1.2f;}
    if(name=="potion-rack"||name=="potion-bottles"||name=="potions"){pivot=Vector2.one*.5f;ppu=w/size;}
    var sprite=Sprite.Create(tex,rect,pivot,ppu,0,SpriteMeshType.FullRect);sprite.name=name+"-"+i.ToString("00");var destination=Art+"Sprites/"+sprite.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Sprite>(destination);if(old==null){AssetDatabase.CreateAsset(sprite,destination);result[i]=sprite;}else{EditorUtility.CopySerialized(sprite,old);UnityEngine.Object.DestroyImmediate(sprite);EditorUtility.SetDirty(old);result[i]=old;}
   }
   return result;
  }
  static void Style(GameObject root){if(root.GetComponent<Soft3DElementStyle>()==null)root.AddComponent<Soft3DElementStyle>();var r=root.GetComponent<SpriteRenderer>();if(r!=null){r.sharedMaterial=spriteMaterial;r.color=Color.white;}}
  static void EditFamily(string name,string lab,Action<GameObject> action){Edit("Assets/SweetSugar/Resources/Blocks/"+name+".prefab",r=>{Style(r);action(r);});if(File.Exists(lab))Edit(lab,r=>{Style(r);action(r);});}
  static void Edit(string path,Action<GameObject> action){var root=PrefabUtility.LoadPrefabContents(path);try{action(root);foreach(var marker in root.GetComponentsInChildren<Soft3DElementStyle>(true)){var r=marker.GetComponent<SpriteRenderer>();if(r!=null&&r.sprite!=null)r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"Sprite_"+r.sprite.texture.name+".mat");}PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
  public static void Validate()
  {
   foreach(var name in new[]{"SolidBlock","EnchantedStone","Inkwell","RoutePotions","BagOpenings","SortingSignals"}){var p=Resources.Load<GameObject>("Blocks/"+name);if(p==null||p.GetComponentInChildren<Soft3DElementStyle>(true)==null)throw new Exception("Missing production style "+name);foreach(var r in p.GetComponentsInChildren<SpriteRenderer>(true))if(r.sprite==null)throw new Exception("Missing sprite "+name);}
  }
  [InitializeOnLoadMethod]static void Listen(){EditorApplication.update+=Poll;}
  static void Poll(){const string path="Logs/soft3d-production.request";if(!File.Exists(path)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete(path);try{Apply();}catch(Exception e){File.WriteAllText("Logs/soft3d-production.result",e.ToString());Debug.LogException(e);}}
 }
}
