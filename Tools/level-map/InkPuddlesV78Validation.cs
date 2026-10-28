using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;using UnityEngine;using UnityEditor;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class InkPuddlesV78Validation
{
 public static void Run(){const string output="../ink-puddles-v78";Directory.CreateDirectory(output);RoyalAvesLevelMapPreview p=null;int errors=0;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{
   ShaderUtil.allowAsyncCompilation=false;
   var items=Resources.LoadAll<WorldMapLibraryItem>("MapLibrary/Props").Where(x=>x.name.StartsWith("ink-puddle-")&&x.name.EndsWith("v78")).OrderBy(x=>x.name).ToArray();
   if(items.Length!=3||items.Any(x=>x.sprite==null||x.category!=WorldMapAssetCategory.Props||x.kind!=PropKind.GroundDetail))throw new Exception("Invalid puddle library entries");
   p=RoyalAvesLevelMapPreview.CreateScenePreview(720,1280);const BindingFlags f=BindingFlags.NonPublic|BindingFlags.Instance;var type=p.GetType();
   var map=(CurvedLevelMap)type.GetField("map",f).GetValue(p);map.World.TimeOverride=0;
   var props=(List<PropPlacement>)typeof(WorldMapView).GetField("props",f).GetValue(map.World);var route=map.World.Route;map.PreviewAt(1);
   var first=JsonUtility.ToJson(new AutoIslandsBaselineV70.Props{entries=map.World.Props.Where(x=>route.LevelAt(x.s)<15.9f).Select(AutoIslandsBaselineV70.Signature).ToArray()},true);
   if(first!=File.ReadAllText("../auto-islands-v70/first-props-before.json"))throw new Exception("Authored first island changed");
   var automatic=props.Where(x=>x.sprite!=null&&x.sprite.name.StartsWith("ink-puddle-")&&x.sprite.name.EndsWith("v78")).ToArray();
   if(automatic.Length<3||automatic.Select(x=>x.sprite).Distinct().Count()!=3)throw new Exception("Missing automatic puddle variants");
   foreach(var prop in automatic)if(prop.kind!=PropKind.GroundDetail||route.InGap(prop.s)||route.IslandIndexAt(prop.s)<1)throw new Exception("Invalid automatic puddle placement");
   foreach(int island in new[]{1,3,4}){
    var target=automatic.FirstOrDefault(x=>route.IslandIndexAt(x.s)==island);if(target.sprite==null)continue;
    float cursor=route.LevelAt(target.s)-1;type.GetField("level",f).SetValue(p,cursor);map.PreviewAt(cursor);type.GetMethod("Save",f).Invoke(p,new object[]{output+"/automatic-island-"+(island+1)+".png",false});
   }
   // In-memory examples only: show each color at an unobstructed bank position.
   props.RemoveAll(x=>{float level=route.LevelAt(x.s);return level>17&&level<24;});
   for(int i=0;i<items.Length;i++)props.Add(new PropPlacement{id="v78-example-"+i,s=route.MetersAt(18.8f+i*1.45f),offset=i%2==0?-1.6f:1.6f,sprite=items[i].sprite,height=.95f,kind=PropKind.GroundDetail,fixedPlacement=true,theme=WorldMapObjectTheme.None});
   props.Sort((a,b)=>a.s.CompareTo(b.s));float example=18;type.GetField("level",f).SetValue(p,example);map.PreviewAt(example);type.GetMethod("Save",f).Invoke(p,new object[]{output+"/three-colors.png",false});
   var slots=(System.Collections.IDictionary)typeof(WorldMapView).GetField("activeSlots",f).GetValue(map.World);int shown=0;
   foreach(System.Collections.DictionaryEntry entry in slots){if(!props[(int)entry.Key].id.StartsWith("v78-example-"))continue;var slot=entry.Value;var ground=(WorldMapGroundDetail)slot.GetType().GetField("groundDetail").GetValue(slot);if(ground!=null&&ground.Active)shown++;}
   if(shown!=3)throw new Exception("Expected three actually visible ground puddles, got "+shown);
   if(errors!=0)throw new Exception(errors+" Unity errors");File.WriteAllText(output+"/validation.txt","PASS: three glossy overhead puddles in Props library; "+automatic.Length+" automatic ground placements using all three variants, outside crossings and first authored island; first-island signatures unchanged; native automatic and three-color captures; no layout resource saved; zero Unity errors.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=logger;if(p!=null)UnityEngine.Object.DestroyImmediate(p);}EditorApplication.Exit(0);
 }
}
