using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
public static class RiverPropsV67Validation
{
 public static void Run()
 {
  RoyalAvesLevelMapPreview preview=null;int errors=0;
  Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=log;
  try{
   Directory.CreateDirectory("../river-props-v67");File.WriteAllText("../river-props-v67/validation.txt","RUNNING\n");
   ShaderUtil.allowAsyncCompilation=false;
   var config=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);
   if(config.stoneLeft==null||config.stoneRight==null||config.bankPlant==null||config.inkFall==null||config.waterFall==null||config.lavaFall==null)throw new Exception("Missing river sprites");
   var type=typeof(RoyalAvesLevelMapPreview);const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
   preview=(RoyalAvesLevelMapPreview)type.GetMethod("CreateScenePreview",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{720,1280});
   var map=(CurvedLevelMap)type.GetField("map",flags).GetValue(preview);Directory.CreateDirectory("../river-props-v67");
   foreach(int level in new[]{27,72,162,12})
   {
    type.GetField("level",flags).SetValue(preview,(float)level);map.PreviewAt(level);
    type.GetMethod("Save",flags).Invoke(preview,new object[]{"../river-props-v67/level-"+level+".png",false});
   }
   type.GetField("level",flags).SetValue(preview,27f);map.PreviewAt(27);var world=map.World;
   var debug=new System.Text.StringBuilder();var settings=config.islands[1];debug.AppendLine("Waterfall enabled="+settings.sideWaterfall+" sprite="+config.inkFall+" override="+settings.waterfallSprite);
   var slots=(System.Collections.IDictionary)typeof(WorldMapView).GetField("activeSlots",flags).GetValue(world);
   for(int i=0;i<world.Props.Count;i++)if(world.Props[i].kind==PropKind.RiverFall&&world.Props[i].s>world.Route.Pos(26)&&world.Props[i].s<world.Route.Pos(31)){
    var prop=world.Props[i];bool visible=world.ProjectProp(i,720f/1280,out var rect,out var depth);debug.AppendLine(prop.id+" lateral="+prop.offset+" s="+prop.s+" sprite="+prop.sprite+" projected="+visible+" rect="+rect);
    if(slots.Contains(i)){var slot=slots[i];var renderer=(MeshRenderer)slot.GetType().GetField("cardRenderer").GetValue(slot);var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);debug.AppendLine("enabled="+renderer.enabled+" active="+renderer.gameObject.activeInHierarchy+" bounds="+renderer.bounds+" clip="+block.GetFloat("_ClipBelow")+" material="+renderer.sharedMaterial);}
   }
   File.WriteAllText("../river-props-v67/fall-debug.txt",debug.ToString());
   if(!world.Props.Any(p=>p.kind==PropKind.RiverFall))throw new Exception("No waterfall placements");
   var platforms=world.Props.Where(p=>p.kind==PropKind.RiverPlatform&&p.supportedLevel>=28&&p.supportedLevel<=30).OrderBy(p=>p.supportedLevel).ToArray();
   if(platforms.Length!=3||platforms[0].offset>=0||platforms[1].offset<=0||platforms[2].offset>=0)throw new Exception("Platforms do not alternate sides");
   var plants=world.Props.Where(p=>p.kind==PropKind.RiverPlant).ToArray();if(plants.Length==0||plants.Any(p=>!p.bottomPivot||!p.groundPinned||p.groundDepth<.3f))throw new Exception("Plants are not frontal/submerged sprites");
   var entry=WorldMapLayout.Entry.From(platforms[0],world.Route);entry.id=platforms[0].id;entry.lateral-=.30f;
   world.ProjectLevel(28,map.Cursor,720f/1280,out var before);if(!world.RefreshPlacement(entry))throw new Exception("Platform edit failed");world.Tick(map.Cursor,1,false);
   world.ProjectLevel(28,map.Cursor,720f/1280,out var after);if(after.x>=before.x-.005f)throw new Exception("Level does not follow edited stone");
   var restored=JsonUtility.FromJson<WorldMapLayout.Entry>(JsonUtility.ToJson(entry));if(restored.supportedLevel!=28)throw new Exception("Platform-level link is not saved");
   if(errors!=0)throw new Exception(errors+" Unity errors");
   File.WriteAllText("../river-props-v67/validation.txt","PASS: six imported sprites; separate alternating platforms; frontal partially submerged plants; 4 native captures; editing a stone moves its level; saved platform-level link; zero Unity errors.\n");
  }catch(Exception e){File.WriteAllText("../river-props-v67/validation.txt","FAIL: "+e.Message);Debug.LogException(e);EditorApplication.Exit(1);return;}finally{Application.logMessageReceived-=log;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}EditorApplication.Exit(0);
 }
}
