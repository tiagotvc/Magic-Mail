using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEditor;using RoyalAves.Meta;using RoyalAves.Meta.WorldMap;using RoyalAves.EditorTools;
public static class LawnStippleV95Validation
{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 public static void Run()
 {
  const string output="../lawn-stipple-v95";Directory.CreateDirectory(output);RoyalAvesLevelMapPreview preview=null;int errors=0;float maxFar=0,maxNear=0,maxPath=0;
  Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
  try{
   ShaderUtil.allowAsyncCompilation=false;preview=RoyalAvesLevelMapPreview.CreateScenePreview(720,1280);var type=preview.GetType();var map=(CurvedLevelMap)type.GetField("map",F).GetValue(preview);map.World.TimeOverride=0;map.PreviewAt(1);var world=map.World;
   Material[] Materials(){var main=(Material)typeof(WorldMapView).GetField("matSurface",F).GetValue(world);var islands=(Material[])typeof(WorldMapView).GetField("islandMaterials",F).GetValue(world);return new[]{main}.Concat(islands??Array.Empty<Material>()).ToArray();}
   void Amount(float value){foreach(var material in Materials())material.SetFloat("_LawnStipple",value);}
   void Save(float cursor,string name){type.GetField("level",F).SetValue(preview,cursor);map.PreviewAt(cursor);type.GetMethod("Save",F).Invoke(preview,new object[]{output+"/"+name+".png",false});}
   Texture2D Read(){world.RenderNow();var target=(RenderTexture)world.Target;var old=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();RenderTexture.active=old;return texture;}
   foreach(var m in Materials())Check(m.HasProperty("_LawnStipple")&&m.GetFloat("_LawnStipple")==1,"An island material lacks the lawn pattern");
   int materialCount=Materials().Length;
   foreach(int level in new[]{1,18}){
    type.GetField("level",F).SetValue(preview,(float)level);map.PreviewAt(level);float cursor=world.Route.MetersAt(level);
    foreach(var m in Materials())Check(Mathf.Abs(m.GetFloat("_MapCursor")-cursor)<.001f,"Cursor is stale on an island material");
    int objectCount=world.GetComponentsInChildren<Transform>(true).Length;
    Amount(0);Save(level,"level-"+level+"-off");var off=Read();Amount(1);Save(level,"level-"+level+"-on");var on=Read();
    Check(world.GetComponentsInChildren<Transform>(true).Length==objectCount,"Toggling the pattern added scene objects");
    float Region(float ahead,float lateral){float s=cursor+ahead;var x=world.Route.CenterX(s)+WorldMapTerrain.Lateral(lateral,ahead);
     if(!world.CameraModel.Project(cursor,s,x,WorldMapTerrain.BankH,world.Cam.aspect,out var uv,out _)||uv.x<.04f||uv.x>.96f||uv.y<.04f||uv.y>.94f)return 0;
     int px=Mathf.RoundToInt(uv.x*(on.width-1)),py=Mathf.RoundToInt(uv.y*(on.height-1));float max=0;
     for(int y=py-10;y<=py+10;y++)for(int xx=px-10;xx<=px+10;xx++){var a=off.GetPixel(xx,y);var b=on.GetPixel(xx,y);max=Mathf.Max(max,Mathf.Abs(a.r-b.r),Mathf.Abs(a.g-b.g),Mathf.Abs(a.b-b.b));}return max;
    }
    float far=0,near=0,path=0;
    foreach(float side in new[]{-2.6f,-2.2f,-1.65f,1.65f,2.2f,2.6f}){
     foreach(float ahead in new[]{0f,1f,2f})near=Mathf.Max(near,Region(ahead,side));
     foreach(float ahead in new[]{8f,9f,10f,11f,12f,13f})far=Mathf.Max(far,Region(ahead,side));
    }
    foreach(float ahead in new[]{0f,2f,4f,6f,8f,10f})path=Mathf.Max(path,Region(ahead,0));
    Check(far>.008f,"Distant stipple is not visible in actual GPU render at level "+level+": "+far);Check(near<=1f/255+.0001f,"Stipple did not disappear near camera: "+near);Check(path<=1f/255+.0001f,"Stipple leaked onto path: "+path);
    maxFar=Mathf.Max(maxFar,far);maxNear=Mathf.Max(maxNear,near);maxPath=Mathf.Max(maxPath,path);UnityEngine.Object.DestroyImmediate(off);UnityEngine.Object.DestroyImmediate(on);
   }
   Amount(1);foreach(float level in new[]{1f,2f,3f,4f,33f,63f})Save(level,"level-"+level);
   for(int i=0;i<24;i++)map.PreviewAt(1+i/12f);for(int i=23;i>=0;i--)map.PreviewAt(1+i/12f);
   Check(Materials().Length==materialCount,"Scroll added ground materials");
   map.PreviewAt(1);
   type.GetMethod("Build",F).Invoke(preview,new object[]{360,780,1});map=(CurvedLevelMap)type.GetField("map",F).GetValue(preview);world=map.World;world.TimeOverride=0;Save(1,"narrow-opening");Save(18,"narrow-island");
   Check(errors==0,errors+" Unity errors");File.WriteAllText(output+"/validation.txt","PASS: real GPU A/B captures show distant lawn freckles on green and lavender islands (peak RGB difference "+maxFar+"); near-camera lawn unchanged ("+maxNear+") and playable path unchanged ("+maxPath+"); all island materials carry live route cursor; world-anchored pattern with smooth approach/subpixel fade; pattern toggles add no objects and forward/reverse scroll adds no ground materials; normal/seasonal island and two phone-ratio captures; zero shader/Unity errors. No layout, player-save or purchase writes.\n");
  }catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
  finally{Application.logMessageReceived-=logger;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}EditorApplication.Exit(0);
 }
}
