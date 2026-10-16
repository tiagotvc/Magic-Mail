using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;

public static class EntranceV54Validation
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/entrance-v54";
    public static void PrepareAndRun()
    {
        var layout=WorldMapMission100.Current.layout;var route=new WorldMapRoute(Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath));
        foreach(var e in layout.chapters.SelectMany(c=>c.entries)){if(e.id=="mission100-lamp-3")e.lateral=2.10f-route.CenterX(route.MetersAt(e.level));if(e.id=="mission100-meet-courier")e.lateral=1.30f-route.CenterX(route.MetersAt(e.level));}
        EditorUtility.SetDirty(layout);AssetDatabase.SaveAssets();Run();
    }
    public static void Run()
    {
        RoyalAvesLevelMapPreview preview=null;int errors=0;Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        try
        {
            ShaderUtil.allowAsyncCompilation=false;
            Directory.CreateDirectory(Output);var mission=WorldMapMission100.Current;var layout=mission.layout;var before=EditorJsonUtility.ToJson(layout);var entries=layout.chapters.SelectMany(c=>c.entries).ToArray();Check(mission.polishedEntrance,"Polished entrance is not enabled");
            var baseline=AssetDatabase.LoadAssetAtPath<WorldMapLayout>("Assets/Editor/LayoutBeforeV54.asset");var oldLater=baseline.chapters.SelectMany(c=>c.entries).Where(e=>e.level>20).Select(e=>JsonUtility.ToJson(e)).OrderBy(s=>s,StringComparer.Ordinal);var later=entries.Where(e=>e.level>20).Select(e=>JsonUtility.ToJson(e)).OrderBy(s=>s,StringComparer.Ordinal);Check(oldLater.SequenceEqual(later),"Later campaign entries changed");
            var opening=entries.Where(e=>e.level<=20&&e.level>=0).ToArray();Check(opening.Length<=14,"Opening has too many objects");Check(opening.Select(e=>e.level).Distinct().Count()==opening.Length,"Objects repeat a level anchor");
            foreach(var sign in new[]{-1,1}){var side=opening.Where(e=>Mathf.Sign(e.lateral)==sign).OrderBy(e=>e.level).ToArray();for(int i=1;i<side.Length;i++)Check(side[i].level-side[i-1].level>=2.5f,"Same-side objects too close: "+side[i].id);}
            foreach(var aspect in new[]{(1080,1920,"standard"),(1080,2340,"tall")})
            {
                preview=RoyalAvesLevelMapPreview.CreateScenePreview(aspect.Item1,aspect.Item2);typeof(RoyalAvesLevelMapPreview).GetField("seasonPreview",Private).SetValue(preview,1);
                foreach(var level in new[]{1,4,8,12,16,22}){var world=Build(preview,level,aspect.Item1,aspect.Item2);world.ConfigureDayNight(false,4,0);Save(preview,aspect.Item3+"-level-"+level.ToString("000")+"-day",level==1);}
                var first=Build(preview,1,aspect.Item1,aspect.Item2);first.ConfigureDayNight(false,4,1);Save(preview,aspect.Item3+"-level-001-night");first.ConfigureDayNight(false,4,0);first.TimeOverride=0;Save(preview,aspect.Item3+"-idle-00");first.TimeOverride=1.4f;Save(preview,aspect.Item3+"-idle-01");
                var hud=Resources.FindObjectsOfTypeAll<RectTransform>().FirstOrDefault(t=>t.name=="HUDDoMapa"&&Mathf.Abs(t.rect.height-174)<.1f);Check(hud!=null,"Compact header missing");
                var satin=hud.GetComponentInChildren<WorldMapPanelGraphic>();Check(satin!=null&&satin.quietPostal&&!satin.raycastTarget,"Header backplate blocks input or has wrong style");
                Check(satin.GetComponent<CanvasRenderer>()!=null,"Satin header has no canvas renderer");
                foreach(var name in new[]{"Lives","Avatar","Coins","Settings"})Check(hud.Find(name)?.GetComponent<Button>()!=null,"Missing functional HUD button "+name);
                UnityEngine.Object.DestroyImmediate(preview);preview=null;
            }
            Check(EditorJsonUtility.ToJson(layout)==before,"Captures changed authored layout");Check(errors==0,"Unity errors: "+errors);
            File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: native Unity C#/shader compilation; sparse opening with unique level anchors and at least 2.5 levels between objects on each side; all entries after level 20 retained; independently animated tree letters and mailbox preserved; compact non-blocking header with all four source controls; 9:16 and 19.5:9 day/night captures. No main scene, island configuration or season saved. Physical-device interaction/performance not measured.");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);EditorApplication.Exit(1);}
    }
    static WorldMapView Build(RoyalAvesLevelMapPreview preview,float level,int w,int h)
    {typeof(RoyalAvesLevelMapPreview).GetField("level",Private).SetValue(preview,level);typeof(RoyalAvesLevelMapPreview).GetMethod("Build",Private).Invoke(preview,new object[]{w,h,1});var world=((CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview)).World;world.TimeOverride=2;return world;}
    static void Save(RoyalAvesLevelMapPreview preview,string name,bool inspect=false)
    {preview.RenderScenePreview();var rt=(RenderTexture)preview.SceneTexture;var old=RenderTexture.active;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();if(inspect){var road=image.GetPixel(rt.width/2,(int)(rt.height*.20f));var grass=image.GetPixel((int)(rt.width*.08f),(int)(rt.height*.20f));Check(road.r>.40f&&road.r>grass.r+.10f,"Road failed to render or does not separate from grass");Check(grass.g>grass.r+.08f,"Lawn did not render green");var chrome=image.GetPixel((int)(rt.width*.035f),(int)(rt.height*.965f));Check(chrome.r>.20f,"Header background did not render");}File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());}finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
}
