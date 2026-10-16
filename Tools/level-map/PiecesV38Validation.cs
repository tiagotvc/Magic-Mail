using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

public static class PiecesV38Validation
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/pieces-v38";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int errors;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static WorldMapView World(RoyalAvesLevelMapPreview p)=>((CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(p)).World;
    static WorldMapEffectPiece Piece(WorldMapView w,string owner,string id)=>w.EffectPieces(owner).Single(p=>p.Id==id);
    public static void Run()
    {
        Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        var layout=Resources.Load<WorldMapLayout>(WorldMapLayout.SimpleResourcePath);var original=EditorJsonUtility.ToJson(layout);
        RoyalAvesLevelMapPreview preview=null;const string fixture="Assets/PiecesV38Roundtrip.asset";
        try
        {
            Directory.CreateDirectory(Output);var chapter=layout.Find(0);chapter.manual=true;chapter.entries.Clear();
            var tree=new WorldMapLayout.Entry{id="piece-tree",sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/Arvores/green-rounded-tree-v27.png"),keepSprite=true,customSize=true,height=1.4f,kind=PropKind.Tree,level=3,lateral=-1.6f,theme=WorldMapObjectTheme.Ice};
            var bush=new WorldMapLayout.Entry{id="piece-bush",sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/Props/shrub-coral-v15.png"),keepSprite=true,customSize=true,height=.7f,kind=PropKind.Small,level=5,lateral=1.65f,theme=WorldMapObjectTheme.Ice};
            var kit=Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath);
            chapter.entries.AddRange(new[]{tree,bush,new WorldMapLayout.Entry{id="piece-puddle",sprite=kit.groundDetails[0],keepSprite=true,customSize=true,height=1,kind=PropKind.GroundDetail,level=1.4f,lateral=1.6f,theme=WorldMapObjectTheme.Ice}});
            preview=RoyalAvesLevelMapPreview.CreateScenePreview(1080,1920);
            typeof(RoyalAvesLevelMapPreview).GetField("seasonPreview",Private).SetValue(preview,2);
            typeof(RoyalAvesLevelMapPreview).GetMethod("Build",Private).Invoke(preview,new object[]{1080,1920,1});
            var world=World(preview);world.TimeOverride=1;world.ConfigureDayNight(false,4,0);preview.RenderScenePreview();
            Check(world.EffectPieces(tree.id).Count(p=>p.Id.StartsWith("snow-"))==3,"Rounded tree snow is still grouped");
            Check(world.EffectPieces(bush.id).Count(p=>p.Id.StartsWith("snow-"))==4,"Painted shrub snow is still grouped");
            Check(world.EffectPieces(tree.id).Count(p=>p.Id.StartsWith("ice-"))==6,"Individual icicle tips are not independent");
            var left=Piece(world,tree.id,"ice-left-0");var leftHome=left.transform.localPosition;
            var rightHome=Piece(world,tree.id,"ice-right-0").transform.localPosition;var snowHome=Piece(world,tree.id,"snow-1").transform.localPosition;
            Check(world.ProjectEffectPiece(left,out var region)&&region.width>0&&region.height>0,"Independent piece cannot be selected by its projected bounds");
            Save(preview,"separate-default");
            var pose=new WorldMapEffectPose{offset=new Vector2(.12f,-.04f),rotation=32,scale=new Vector2(.65f,1.25f)};
            tree.effectPieces=WorldMapPiecePose.Set(tree.effectPieces,"ice-left-0",pose);
            tree.effectPieces=WorldMapPiecePose.Set(tree.effectPieces,"snow-0",new WorldMapEffectPose{offset=new Vector2(.08f,.025f),rotation=-12,scale=new Vector2(.8f,1.1f)});
            var copy=UnityEngine.Object.Instantiate(layout);AssetDatabase.CreateAsset(copy,fixture);AssetDatabase.SaveAssetIfDirty(copy);AssetDatabase.ImportAsset(fixture,ImportAssetOptions.ForceUpdate);
            var saved=AssetDatabase.LoadAssetAtPath<WorldMapLayout>(fixture).Find(0).entries[0];
            Check(WorldMapPiecePose.Find(saved.effectPieces,"ice-left-0").rotation==32,"Per-piece asset save/reload lost settings");
            var converted=WorldMapLayout.Entry.From(saved.Placement(world.Route,kit),world.Route);
            Check(WorldMapPiecePose.Find(converted.effectPieces,"ice-left-0").offset==pose.offset&&!ReferenceEquals(converted.effectPieces,saved.effectPieces),"Entry/Placement conversion lost or shared mutable piece data");
            world.ReloadScenery();preview.RenderScenePreview();left=Piece(world,tree.id,"ice-left-0");
            Check(Vector3.Distance(left.transform.localPosition,leftHome+new Vector3(.12f,-.04f,0)*tree.height)<.001f,"Individual ice offset failed");
            Check(Mathf.Abs(Mathf.DeltaAngle(left.transform.localEulerAngles.z,32))<.01f&&Vector2.Distance(left.transform.localScale,pose.EffectiveScale)<.001f,"Individual ice rotation/scale failed");
            Check(Vector3.Distance(Piece(world,tree.id,"ice-right-0").transform.localPosition,rightHome)<.001f&&Vector3.Distance(Piece(world,tree.id,"snow-1").transform.localPosition,snowHome)<.001f,"Moving one piece changed its neighbor");
            var stable=left.transform.localPosition;for(int i=0;i<8;i++)preview.RenderScenePreview();Check(left.transform.localPosition==stable,"Per-piece transform accumulates on repaint");Save(preview,"separate-edited");
            tree.effectPieces=WorldMapPiecePose.Set(tree.effectPieces,"snow-0",new WorldMapEffectPose{visibility=WorldMapEffectVisibility.Hide});world.ReloadScenery();preview.RenderScenePreview();
            Check(!Piece(world,tree.id,"snow-0").gameObject.activeSelf&&Piece(world,tree.id,"snow-1").gameObject.activeSelf,"Individual visibility does not isolate snow pieces");
            int puddle=Enumerable.Range(0,world.Props.Count).Single(i=>world.Props[i].id=="piece-puddle");
            Check(world.ProjectPropAnchor(puddle,world.Cam.aspect,out var center),"Puddle reflection fixture not projected");
            world.TimeOverride=1;preview.RenderScenePreview();var a=Save(preview,"reflection-1s");
            world.TimeOverride=5;preview.RenderScenePreview();var b=Save(preview,"reflection-5s");
            int changed=0,cx=Mathf.RoundToInt(center.x*1080),cy=Mathf.RoundToInt(center.y*1920);
            for(int y=Mathf.Max(0,cy-20);y<Mathf.Min(1920,cy+20);y++)for(int x=Mathf.Max(0,cx-20);x<Mathf.Min(1080,cx+20);x++){int i=y*1080+x;if(Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>8)changed++;}
            Check(changed>20,"Ground ice reflection has no visible animation");
            Check(world.IceNeedsFrames,"Editor stops repainting animated ground ice");
            EditorJsonUtility.FromJsonOverwrite(original,layout);typeof(RoyalAvesLevelMapPreview).GetMethod("Build",Private).Invoke(preview,new object[]{1080,1920,1});world=World(preview);world.TimeOverride=3;world.ConfigureDayNight(false,4,0);Save(preview,"map-day");world.ConfigureDayNight(false,4,1);Save(preview,"map-night");
            Check(errors==0,"Unity logged errors");File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: rounded tree has 3 separate snow pieces and 6 individual ice pieces; painted red shrub has 4 separate snow pieces; per-piece position, rotation, nonuniform scale, visibility and unchanged neighbors; no transform drift; geometry bounds can be projected for selection; save/reimport and cloned Entry/Placement data; "+changed+" changing pixels in a fixed 40x40 ground-puddle region prove rendered reflection animation; animated ice requests editor frames; native saved-map day/night captures; zero Unity errors. Main scene, layout and season were not saved. Manual mouse interaction and physical-device performance were not measured.");
            UnityEngine.Object.DestroyImmediate(preview);preview=null;AssetDatabase.DeleteAsset(fixture);EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);if(AssetDatabase.LoadAssetAtPath<WorldMapLayout>(fixture)!=null)AssetDatabase.DeleteAsset(fixture);EditorJsonUtility.FromJsonOverwrite(original,layout);EditorApplication.Exit(1);}
    }
    static Color32[] Save(RoyalAvesLevelMapPreview p,string name)
    {
        p.RenderScenePreview();var rt=(RenderTexture)p.SceneTexture;var old=RenderTexture.active;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());return image.GetPixels32();}
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}
    }
}

