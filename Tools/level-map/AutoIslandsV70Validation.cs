using System;using System.IO;using System.Linq;using System.Reflection;using UnityEditor;using UnityEngine;using RoyalAves.EditorTools;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;
public static class AutoIslandsV70Validation
{
 public static void Run(){const string output="../auto-islands-v70";RoyalAvesLevelMapPreview p=null;int errors=0;
  Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=log;
  Directory.CreateDirectory(output);File.WriteAllText(output+"/validation.txt","RUNNING");
  try{
   ShaderUtil.allowAsyncCompilation=false;var config=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);
   if(!config.automaticSequence||config.Capacity!=200||config.ActiveIslandCount!=14||config.recipes.Count!=7)throw new Exception("Invalid campaign configuration");
   if(config.Managed(config.islands[0]))throw new Exception("First island is managed");
   var firstSettings=JsonUtility.ToJson(config.islands[0]);
   var layout=Resources.Load<WorldMapLayout>(WorldMapMission100.LayoutResource);var firstLayout=JsonUtility.ToJson(layout.Find(0));
   RoyalAvesMapAutomaticControls.Recompose(config);
   if(JsonUtility.ToJson(config.islands[0])!=firstSettings||JsonUtility.ToJson(layout.Find(0))!=firstLayout)throw new Exception("Recompose changed authored island 1");
   for(int i=1;i<14;i++)if(config.Recipe(config.islands[i]).christmas!=(i%3==2))throw new Exception("Christmas cadence incorrect at "+(i+1));
   var type=typeof(RoyalAvesLevelMapPreview);const BindingFlags f=BindingFlags.Instance|BindingFlags.NonPublic;
   p=(RoyalAvesLevelMapPreview)type.GetMethod("CreateScenePreview",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{720,1280});
   var map=(CurvedLevelMap)type.GetField("map",f).GetValue(p);map.World.TimeOverride=0;type.GetField("level",f).SetValue(p,1f);map.PreviewAt(1);
   if(map.World.Route.ChapterAtMeters(map.World.Route.Pos(16))!=1)throw new Exception("Manual layout still uses 20-level boundaries");
   var first=JsonUtility.ToJson(new AutoIslandsBaselineV70.Props{entries=map.World.Props.Where(x=>map.World.Route.LevelAt(x.s)<15.9f).Select(AutoIslandsBaselineV70.Signature).ToArray()},true);
   File.WriteAllText(output+"/first-props-after.json",first);
   if(first!=File.ReadAllText(output+"/first-props-before.json"))throw new Exception("First island placements changed");
   type.GetMethod("Save",f).Invoke(p,new object[]{output+"/first-after.png",false});
   var report=new System.Text.StringBuilder();
   for(int i=1;i<14;i++){
    int firstLevel=config.FirstLevel(i),last=Mathf.Min(200,firstLevel+14);var recipe=config.Recipe(config.islands[i]);
    var props=map.World.Props.Where(x=>map.World.Route.IslandIndexAt(x.s)==i&&x.id!=null&&x.id.StartsWith("auto-island-")).ToArray();
    if(!props.Any(x=>x.kind==PropKind.Tree)||props.Count(x=>x.kind==PropKind.Landmark)<2||!props.Any(x=>x.kind==PropKind.GroundDetail))throw new Exception("Sparse automatic island "+(i+1));
    foreach(var prop in props){
     if(map.World.Route.InGap(prop.s,.5f))throw new Exception("Land object in water: "+prop.id);
     if(recipe.christmas!=(prop.theme==WorldMapObjectTheme.Ice))throw new Exception("Season decoration leaked");
     if(prop.kind==PropKind.Landmark&&!recipe.buildings.Contains(prop.sprite))throw new Exception("Unapproved building perspective");
     if(prop.kind==PropKind.GroundDetail&&prop.shadow)throw new Exception("Flat prop has standing shadow");
    }
    if(!recipe.christmas&&props.Any(x=>x.kind==PropKind.Character))throw new Exception("Season actor on normal island");
    if(recipe.christmas&&props.Count(x=>x.kind==PropKind.Character)!=Mathf.Min(2,recipe.story.Length))throw new Exception("Season story lost its reserved place");
    int level=Mathf.Min(last-1,firstLevel+3);type.GetField("level",f).SetValue(p,(float)level);map.World.TimeOverride=i*3;map.PreviewAt(level);
    // Let the sky transition settle for a deterministic native capture.
    map.World.TimeOverride=i*3+2;map.PreviewAt(level);
    type.GetMethod("Save",f).Invoke(p,new object[]{output+"/island-"+(i+1)+".png",false});
    if(map.World.KitAt(map.World.Route.Pos(firstLevel)).Christmas!=recipe.christmas)throw new Exception("Per-island snow mismatch");
    if(map.World.SeasonEventsAllowed!=recipe.christmas)throw new Exception("Season event escaped its island");
    report.AppendLine($"Island {i+1}: {firstLevel}-{last}, {recipe.title}, {props.Length} props; buildings={props.Count(x=>x.kind==PropKind.Landmark)}, trees={props.Count(x=>x.kind==PropKind.Tree)}, overhead={props.Count(x=>x.kind==PropKind.GroundDetail)}, story={props.Count(x=>x.kind==PropKind.Character)}");
   }
   // The same recipe repeats with a different stable sprite selection.
   if(config.islands[1].recipeIndex!=config.islands[9].recipeIndex||config.islands[1].sceneryVariant==config.islands[9].sceneryVariant)throw new Exception("Recipe variation missing");
   if(map.World.Route.LevelSlots!=200)throw new Exception("Unexpected planned route length");
   if(map.World.RenderedLevelCount!=200)throw new Exception("Editor does not show future positions");
   if(errors>0)throw new Exception(errors+" Unity errors");
   File.WriteAllText(output+"/composition.txt",report.ToString());
   File.WriteAllText(output+"/validation.txt","PASS: first-island placement signature unchanged; 200 planned positions, 14 islands; five repeating normal recipes and two alternating Christmas recipes; seasonal snow/actors isolated; every island populated with correctly sized buildings, trees and overhead props; no land props in water; 14 native captures; zero Unity errors.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=log;if(p!=null)UnityEngine.Object.DestroyImmediate(p);}EditorApplication.Exit(0);
 }
}
