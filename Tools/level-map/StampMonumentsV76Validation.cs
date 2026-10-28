using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;using UnityEngine;using UnityEditor;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class StampMonumentsV76Validation
{
 public static void Run(){const string output="../stamp-monuments-v76";Directory.CreateDirectory(output);File.WriteAllText(output+"/validation.txt","RUNNING");RoyalAvesLevelMapPreview p=null;int errors=0;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{
   ShaderUtil.allowAsyncCompilation=false;
   var items=Resources.LoadAll<WorldMapLibraryItem>("MapLibrary/Props").Where(x=>x.name.StartsWith("stamp-monument-")).OrderBy(x=>x.name).ToArray();
   if(items.Length!=3||items.Any(x=>x.sprite==null||x.category!=WorldMapAssetCategory.Props||x.kind!=PropKind.Landmark||x.size<1||WorldMapWindowLight.IsBuilding(x.sprite)))throw new Exception("Invalid stamp library items");
   p=RoyalAvesLevelMapPreview.CreateScenePreview(720,1280);const BindingFlags f=BindingFlags.NonPublic|BindingFlags.Instance;var type=p.GetType();
   var map=(CurvedLevelMap)type.GetField("map",f).GetValue(p);map.World.TimeOverride=0;
   var props=(List<PropPlacement>)typeof(WorldMapView).GetField("props",f).GetValue(map.World);var route=map.World.Route;
   map.PreviewAt(1);
   var first=JsonUtility.ToJson(new AutoIslandsBaselineV70.Props{entries=map.World.Props.Where(x=>route.LevelAt(x.s)<15.9f).Select(AutoIslandsBaselineV70.Signature).ToArray()},true);
   if(first!=File.ReadAllText("../auto-islands-v70/first-props-before.json"))throw new Exception("First authored island changed");
   var asset=route.Islands;int automatic=0;
   for(int i=1;i<asset.ActiveIslandCount;i++){
    var recipe=asset.Recipe(asset.islands[i]);var stamps=props.Where(x=>route.IslandIndexAt(x.s)==i&&x.sprite!=null&&x.sprite.name.StartsWith("stamp-monument-")).ToArray();
    if(stamps.Length!=1)throw new Exception("Expected one automatic stamp on island "+(i+1)+", got "+stamps.Length);
    if(stamps[0].sprite.name.Contains("christmas")!=recipe.christmas||!stamps[0].groundPinned)throw new Exception("Automatic stamp theme/ground mismatch");automatic++;
    if(i==1||i==2||i==6){float cursor=route.LevelAt(stamps[0].s)-2;type.GetField("level",f).SetValue(p,cursor);map.PreviewAt(cursor);type.GetMethod("Save",f).Invoke(p,new object[]{output+"/automatic-island-"+(i+1)+".png",false});}
   }
   // Only this disposable preview composition is changed; no layout resource is saved.
   props.RemoveAll(x=>{float level=route.LevelAt(x.s);return level>18&&level<23||level>33&&level<37&&x.offset<0;});
   foreach(var item in items){bool winter=item.name.Contains("christmas"),curved=item.name.Contains("curved");
    props.Add(new PropPlacement{id="v76-example-"+item.name,s=route.MetersAt(winter?35:curved?21.8f:19.8f),offset=curved?1.72f:-1.72f,
     sprite=item.sprite,height=item.size,kind=item.kind,shadow=true,bottomPivot=true,groundPinned=true,groundDepth=.08f,fixedPlacement=true,theme=WorldMapObjectTheme.None});
   }
   props.Sort((a,b)=>a.s.CompareTo(b.s));
   foreach(int level in new[]{19,32}){type.GetField("level",f).SetValue(p,(float)level);map.PreviewAt(level);type.GetMethod("Save",f).Invoke(p,new object[]{output+"/example-"+level+".png",false});
    var slots=(System.Collections.IDictionary)typeof(WorldMapView).GetField("activeSlots",f).GetValue(map.World);int shown=0;
    foreach(System.Collections.DictionaryEntry entry in slots){var placement=props[(int)entry.Key];if(!placement.id.StartsWith("v76-example-"))continue;var go=(GameObject)entry.Value.GetType().GetField("go").GetValue(entry.Value);if(go.activeInHierarchy)shown++;}
    if(shown!=(level==19?2:1))throw new Exception("Stamp examples not actually visible: "+shown);
   }
   if(errors!=0)throw new Exception(errors+" Unity errors");
   File.WriteAllText(output+"/validation.txt","PASS: three transparent frontal glossy stamp monuments in Props library; "+automatic+" automatic placements with correct normal/Christmas variants and ground pinning; first-island placements unchanged; native automatic and temporary 8%-sink examples; no layout resource saved; zero Unity errors.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=logger;if(p!=null)UnityEngine.Object.DestroyImmediate(p);}EditorApplication.Exit(0);
 }
}
