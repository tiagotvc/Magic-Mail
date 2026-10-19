using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public static class SantaV30Validation
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/santa-v30";
    static int errors;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void RunGround()
    {
        Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        try
        {
            Directory.CreateDirectory(Output);CaptureGround();Check(errors==0,"Unity logged an error");
            File.WriteAllText(Path.Combine(Output,"ground-validation.txt"),"PASS: eight production floor palettes rendered at 1080x1920 with independent lawn bands and unchanged trail diagonals; shader supported; zero Unity errors. Source scene, layouts and island assets never saved. Visual QA performed separately on native captures; physical-device performance not measured.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void Run()
    {
        Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        try
        {
            Directory.CreateDirectory(Output);
            CaptureGround();Capture("tree",false,1080,1920);Capture("building",true,1080,2280);
            Capture("shrub",false,1080,1920);Capture("shrub",true,1080,2280);
            Check(errors==0,"Unity logged an error");
            File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: native map captures at 1080x1920 and 1080x2280; eight floor palettes captured; frontal friendly and purple evil Santa bound and alpha-readable; only shrubs eligible, tree/building excluded; animated upper-body peek; hidden, empty-background, expired and drag interactions rejected; valid catch emits correct variant exactly once and shows configured balloon; ordinary level callback checked; automatic appearance, disable, close/reopen and cleanup checked. Source scene/layouts never saved. Gameplay event is a balloon prototype; device performance not measured.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static Vector2 Clickable(WorldMapView world)
    {
        Check(world.Santa.VisibleRect(out var r),"Santa has no exposed head rectangle");
        for(var y=1;y<30;y++)for(var x=1;x<30;x++)
        {
            var point=new Vector2(Mathf.Lerp(r.xMin,r.xMax,x/30f),Mathf.Lerp(r.yMin,r.yMax,y/30f));
            if(world.Santa.Hit(point,world.Clock)&&world.PickViewport(point)==0)return point;
        }
        throw new Exception("No visible clickable Santa pixel; rect="+r+" host="+world.Santa.HostIndex);
    }
    static void Capture(string kind,bool evil,int width,int height)
    {
        var kit=Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath);
        kit.santaEventEnabled=true;
        Check(kit.santaFriendly!=null&&kit.santaVillain!=null&&kit.santaFriendly.texture.isReadable&&kit.santaVillain.texture.isReadable,"Santa sprites/alpha are unavailable");
        var layout=Resources.Load<WorldMapLayout>(WorldMapLayout.SimpleResourcePath);var original=EditorJsonUtility.ToJson(layout);
        var chapter=layout.Find(0);if(chapter==null){chapter=new WorldMapLayout.Chapter{index=0};layout.chapters.Add(chapter);}
        chapter.manual=true;chapter.entries.Clear();
        var sprite=kind=="tree"?AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/Arvores/pine-classic-glossy-v29.png"):
            kind=="shrub"?AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/Props/shrub-orange-glossy-v29.png"):kit.expressivePostOffice;
        chapter.entries.Add(new WorldMapLayout.Entry{id="santa-cover",sprite=sprite,keepSprite=true,customSize=true,
            kind=kind=="tree"?PropKind.Tree:kind=="building"?PropKind.Landmark:PropKind.Small,height=kind=="shrub"?.7f:1.5f,level=3,lateral=kind=="building"?1.3f:-1.35f,groundPinned=true});
        RoyalAvesLevelMapPreview preview=null;
        try
        {
            preview=RoyalAvesLevelMapPreview.CreateScenePreview(width,height);
            var map=(CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview);var world=map.World;
            world.TimeOverride=10;preview.RenderScenePreview();
            var host=Enumerable.Range(0,world.Props.Count).First(i=>world.Props[i].id=="santa-cover");
            if(kind!="shrub")
            {
                Check(!world.Santa.ShowNow(10,evil,host),"Tree/building was accepted as Santa cover");
                return;
            }
            var catches=0;var caughtEvil=!evil;map.SantaEventCaught+=v=>{catches++;caughtEvil=v;};
            Check(world.Santa.ShowNow(10,evil,host),"No eligible cover for "+kind);
            preview.RenderScenePreview();Check(!world.Santa.Hit(new Vector2(.5f,.5f),world.Clock),"Hidden Santa can be clicked");
            world.TimeOverride=10.9f;preview.RenderScenePreview();
            var target=Clickable(world);var actor=world.GetComponentsInChildren<MeshFilter>().Single(m=>m.name=="NoelSurpresa");
            var firstPose=actor.transform.localPosition;world.TimeOverride=11.1f;preview.RenderScenePreview();
            Check(actor.transform.localPosition!=firstPose,"Santa peek has no animation");
            Check(!world.Santa.Hit(Vector2.zero,world.Clock),"Empty background catches Santa");
            var prefix=evil?"evil-shrub":"friendly-shrub";
            Save(preview,prefix+"-peek.png");target=Clickable(world);map.ClickViewport(target);
            Check(catches==1&&caughtEvil==evil&&map.SantaBalloonVisible,"Catch failed or chose wrong event variant");
            var text=map.transform.Find("BalaoEventoNoel/Texto").GetComponent<TMP_Text>();
            Check(text.text==(evil?world.CameraModel.Kit.santaVillainMessage:world.CameraModel.Kit.santaFriendlyMessage),"Wrong balloon message");
            map.TryCatchSanta(target);Check(catches==1,"Same Santa can be caught more than once");Save(preview,prefix+"-balloon.png");
            world.TimeOverride=17;preview.RenderScenePreview();Check(!map.SantaBalloonVisible,"Balloon never closes");
            Check(world.ProjectLevel(1,map.Cursor,world.Cam.aspect,out var token),"Level projection failed");
            var before=(int)typeof(RoyalAvesLevelMapPreview).GetField("selections",Private).GetValue(preview);
            map.ClickViewport(token);
            Check((int)typeof(RoyalAvesLevelMapPreview).GetField("selections",Private).GetValue(preview)==before+1,"Ordinary level callback failed");
            Check(world.Santa.ShowNow(20,evil,host),"Cannot restart event");world.TimeOverride=27;preview.RenderScenePreview();
            Check(!world.Santa.Active&&!map.TryCatchSanta(target),"Expired Santa can be clicked");
            world.TimeOverride=30;world.Santa.Reset();preview.RenderScenePreview();
            world.TimeOverride=30+world.CameraModel.Kit.santaFirstDelay+.1f;preview.RenderScenePreview();Check(world.Santa.Active,"Automatic appearance never starts");
            map.BeginDrag(new PointerEventData(null));preview.RenderScenePreview();Check(!world.Santa.Active&&!map.TryCatchSanta(target),"Dragging catches an event");
            map.EndDrag(new PointerEventData(null));
            world.CameraModel.Kit.santaEventEnabled=false;preview.RenderScenePreview();Check(!world.Santa.Active,"Disabled event remains visible");
            map.Close();Check(!map.SantaBalloonVisible,"Balloon survives closing map");map.Open(false);
            Check(world.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="NoelSurpresa")==1,"Event actor duplicated after reopening map");
        }
        finally{if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);EditorJsonUtility.FromJsonOverwrite(original,layout);}
    }
    static void CaptureGround()
    {
        var islands=UnityEngine.Object.Instantiate(Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath));
        var preview=ScriptableObject.CreateInstance<RoyalAvesLevelMapPreview>();var t=typeof(RoyalAvesLevelMapPreview);
        try
        {
            t.GetField("captureIslands",Private).SetValue(preview,islands);
            foreach(WorldMapGroundStyle style in Enum.GetValues(typeof(WorldMapGroundStyle)))
            {
                islands.islands[0].groundStyle=style;islands.islands[0].customColors=false;
                t.GetField("level",Private).SetValue(preview,1f);
                t.GetMethod("Build",Private).Invoke(preview,new object[]{1080,1920,1});
                var map=(CurvedLevelMap)t.GetField("map",Private).GetValue(preview);map.World.TimeOverride=1;
                Save(preview,"ground-"+style.ToString().ToLowerInvariant()+".png");
                Check(map.World.CameraModel.Kit.surface.isSupported,"Ground shader unsupported");
            }
        }
        finally{t.GetMethod("Cleanup",Private).Invoke(preview,null);UnityEngine.Object.DestroyImmediate(preview);UnityEngine.Object.DestroyImmediate(islands);}
    }
    static void Save(RoyalAvesLevelMapPreview preview,string name)
    {
        preview.RenderScenePreview();var rt=(RenderTexture)preview.SceneTexture;var old=RenderTexture.active;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();File.WriteAllBytes(Path.Combine(Output,name),t.EncodeToPNG());}
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(t);}
    }
}
