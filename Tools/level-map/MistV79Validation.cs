using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;using UnityEngine;using UnityEditor;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class MistV79Validation
{
 public static void Run(){const string output="../mist-v79";Directory.CreateDirectory(output);RoyalAvesLevelMapPreview p=null;int errors=0;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{
   ShaderUtil.allowAsyncCompilation=false;
   if((int)PropKind.Mist!=18||(int)PropKind.RiverFall!=17)throw new Exception("Serialized kind compatibility changed");
   var items=Resources.LoadAll<WorldMapLibraryItem>("MapLibrary/Efeitos").Where(x=>x.name.StartsWith("mist-")&&x.name.EndsWith("v79")).OrderBy(x=>x.name).ToArray();
   if(items.Length!=3||items.Any(x=>x.sprite==null||x.category!=WorldMapAssetCategory.Effects||x.kind!=PropKind.Mist))throw new Exception("Invalid mist library entries");
   foreach(var item in items){var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(item.sprite));var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);if(settings.spriteMeshType!=SpriteMeshType.FullRect||!importer.alphaIsTransparency)throw new Exception("Mist fade may be clipped");}
   p=RoyalAvesLevelMapPreview.CreateScenePreview(720,1280);const BindingFlags f=BindingFlags.NonPublic|BindingFlags.Instance;var type=p.GetType();
   var map=(CurvedLevelMap)type.GetField("map",f).GetValue(p);map.World.TimeOverride=0;
   var props=(List<PropPlacement>)typeof(WorldMapView).GetField("props",f).GetValue(map.World);var route=map.World.Route;map.PreviewAt(1);
   var first=JsonUtility.ToJson(new AutoIslandsBaselineV70.Props{entries=map.World.Props.Where(x=>route.LevelAt(x.s)<15.9f).Select(AutoIslandsBaselineV70.Signature).ToArray()},true);
   if(first!=File.ReadAllText("../auto-islands-v70/first-props-before.json"))throw new Exception("First authored island changed");
   var mist=props.Where(x=>x.kind==PropKind.Mist).ToArray();
   if(mist.Length!=26||mist.Select(x=>x.sprite).Distinct().Count()!=3)throw new Exception("Expected 26 automatic mist ribbons and three variants, got "+mist.Length);
   foreach(var entry in mist){if(entry.shadow||entry.groundPinned||entry.theme!=WorldMapObjectTheme.None||entry.intensity<=0||route.IslandIndexAt(entry.s)<1)throw new Exception("Invalid mist placement");
    var saved=WorldMapLayout.Entry.From(entry,route).Placement(route,map.World.CameraModel.Kit);if(saved.kind!=PropKind.Mist||saved.intensity!=entry.intensity||saved.sprite!=entry.sprite)throw new Exception("Mist save roundtrip failed");}
   foreach(int island in new[]{1,2,3,6}){
    var target=mist.First(x=>route.IslandIndexAt(x.s)==island);float cursor=route.LevelAt(target.s)-1.4f;type.GetField("level",f).SetValue(p,cursor);map.PreviewAt(cursor);
    type.GetMethod("Save",f).Invoke(p,new object[]{output+"/automatic-island-"+(island+1)+".png",false});
   }
   // A temporary composition for comparing the three variants over visible ground.
   props.RemoveAll(x=>{float level=route.LevelAt(x.s);return level>17&&level<24;});
   for(int i=0;i<items.Length;i++)props.Add(new PropPlacement{id="v79-example-"+i,s=route.MetersAt(19+i*1.6f),offset=i%2==0?-1.9f:1.9f,sprite=items[i].sprite,height=.8f,intensity=.75f,kind=PropKind.Mist,fixedPlacement=true,bottomPivot=true,theme=WorldMapObjectTheme.None});
   props.Sort((a,b)=>a.s.CompareTo(b.s));float example=18;type.GetField("level",f).SetValue(p,example);map.PreviewAt(example);
   type.GetMethod("Save",f).Invoke(p,new object[]{output+"/three-variants.png",false});
   var slots=(System.Collections.IDictionary)typeof(WorldMapView).GetField("activeSlots",f).GetValue(map.World);
   var snapshots=new Dictionary<int,(Matrix4x4 matrix,Color tint)>();var block=new MaterialPropertyBlock();
   foreach(System.Collections.DictionaryEntry entry in slots){if(!props[(int)entry.Key].id.StartsWith("v79-example-"))continue;var slot=entry.Value;
    var renderer=(MeshRenderer)slot.GetType().GetField("cardRenderer").GetValue(slot);var card=(Transform)slot.GetType().GetField("card").GetValue(slot);
    if(!renderer.enabled||!renderer.gameObject.activeInHierarchy||renderer.sharedMaterial.shader.name!="RoyalAves/WorldMap/Vapor")throw new Exception("Mist not actually drawn with alpha blending");
    renderer.GetPropertyBlock(block);snapshots.Add((int)entry.Key,(card.localToWorldMatrix,block.GetColor("_Tint")));}
   if(snapshots.Count!=3)throw new Exception("Expected three visible mist examples");map.World.TimeOverride=97;map.PreviewAt(example);
   foreach(var entry in snapshots){var slot=slots[entry.Key];var renderer=(MeshRenderer)slot.GetType().GetField("cardRenderer").GetValue(slot);var card=(Transform)slot.GetType().GetField("card").GetValue(slot);renderer.GetPropertyBlock(block);
    if(card.localToWorldMatrix!=entry.Value.matrix||block.GetColor("_Tint")!=entry.Value.tint)throw new Exception("Mist moved/pulsed over time");}
   if(errors!=0)throw new Exception(errors+" Unity errors");File.WriteAllText(output+"/validation.txt","PASS: three translucent mist Effects entries with full-rect alpha fade; 26 automatic lateral ribbons across islands 2-14; all three colors used; no shadows/snow; opacity and sprite survive layout roundtrip; actual visible alpha-blended renderers remain identical at time 0 and 97; first-island signatures unchanged; native automatic/theme and three-variant captures; no layout resource saved; zero Unity errors.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=logger;if(p!=null)UnityEngine.Object.DestroyImmediate(p);}EditorApplication.Exit(0);
 }
}
