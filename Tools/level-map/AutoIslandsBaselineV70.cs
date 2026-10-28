using System;using System.IO;using System.Linq;using System.Reflection;using UnityEditor;using UnityEngine;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class AutoIslandsBaselineV70{
 [Serializable]public class Props{public string[] entries;}
 public static string Signature(PropPlacement x)=>string.Join("|",x.id??"",x.s.ToString("R",System.Globalization.CultureInfo.InvariantCulture),x.offset.ToString("R",System.Globalization.CultureInfo.InvariantCulture),x.height.ToString("R",System.Globalization.CultureInfo.InvariantCulture),x.kind,AssetDatabase.GetAssetPath(x.sprite),x.sprite!=null?x.sprite.name:"",x.bottomPivot,x.flipX,x.theme,x.shadow,x.groundPinned,x.groundDepth.ToString("R",System.Globalization.CultureInfo.InvariantCulture),x.supportedLevel,x.seed);
 public static void Run(){RoyalAvesLevelMapPreview p=null;try{
  Directory.CreateDirectory("../auto-islands-v70");ShaderUtil.allowAsyncCompilation=false;
  var t=typeof(RoyalAvesLevelMapPreview);const BindingFlags f=BindingFlags.Instance|BindingFlags.NonPublic;
  p=(RoyalAvesLevelMapPreview)t.GetMethod("CreateScenePreview",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{720,1280});
  var map=(CurvedLevelMap)t.GetField("map",f).GetValue(p);map.World.TimeOverride=0;t.GetField("level",f).SetValue(p,1f);map.PreviewAt(1);
  File.WriteAllText("../auto-islands-v70/first-props-before.json",JsonUtility.ToJson(new Props{entries=map.World.Props.Where(x=>map.World.Route.LevelAt(x.s)<15.9f).Select(Signature).ToArray()},true));
  t.GetMethod("Save",f).Invoke(p,new object[]{"../auto-islands-v70/first-before.png",false});
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);return;}finally{if(p!=null)UnityEngine.Object.DestroyImmediate(p);}EditorApplication.Exit(0);}
}
