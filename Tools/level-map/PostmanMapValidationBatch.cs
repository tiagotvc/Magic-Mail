using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using RoyalAves.EditorTools;
public static class PostmanMapValidationBatch
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/v12";
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void Execute()
    {
        Directory.CreateDirectory(Output);var source=RoyalAvesWorldMapSetup.Ensure(true);
        Check(source.postmanSprite!=null,"Approved illustration binding missing");
        var view=WorldMapView.Create(null,Resources.Load<LobbyFeaturesConfig>("RoyalAvesFeatures"),source,WorldMapLevelCatalog.Count,0,true);
        try
        {
            view.SetFixedSize(540,1200);view.TimeOverride=0;view.Tick(1,5,false);
            Check(!view.Props.Any(x=>x.sprite==source.simplePostal[3]),"Old chibi still in map");
            int index=Enumerable.Range(0,view.Props.Count).First(i=>view.Props[i].sprite==source.postmanSprite&&view.Route.LevelAt(view.Props[i].s)<10);
            Check(view.ProjectProp(index,.45f,out var bounds,out _)&&bounds.height>.10f&&bounds.yMax<1,"Character scale or editor picking is wrong");
            var actor=view.GetComponentsInChildren<Transform>().FirstOrDefault(x=>x.name=="CarteiroAprovado");
            Check(actor!=null,"Approved illustration is not visible");
            Check(actor.GetComponentsInChildren<MeshRenderer>().Length==1,"Character has cutout seams again");
            Check(!actor.parent.GetComponent<MeshRenderer>().enabled,"Proxy image overlaps the character");
            Check(actor.parent.localScale.x<0,"Right-side courier does not face road");
            var characterMesh=actor.GetComponent<MeshFilter>().sharedMesh;var before=characterMesh.vertices;
            view.TimeOverride=1.2f;view.Tick(1,5,false);var after=characterMesh.vertices;
            Check(before.Zip(after,(a,b)=>Vector3.Distance(a,b)).Max()>.002f,"Character has no idle motion");
            Check(Vector3.Distance(before[0],after[0])<.0001f,"Feet float during idle");
            Check(actor.GetComponent<MeshRenderer>().sharedMaterial.mainTexture==source.postmanSprite.texture,"Character no longer uses approved full illustration");
            var old=new WorldMapLayout.Entry{sprite=source.simplePostal[3],keepSprite=true,customSize=true,height=1.05f,level=5.0189786f,lateral=2.006f,kind=PropKind.Character};
            var migrated=old.Placement(view.Route,view.CameraModel.Kit);
            Check(migrated.sprite==source.postmanSprite&&Mathf.Abs(migrated.height-1.5225f)<.001f&&migrated.offset==old.lateral,"Saved courier migration incorrect");
            int count=view.GetComponentsInChildren<Transform>(true).Count(x=>x.name=="CarteiroAprovado");
            for(int i=0;i<4;i++){view.Tick(85,85,false);view.Tick(1,5,false);}
            Check(view.GetComponentsInChildren<Transform>().Any(x=>x.name=="CarteiroAprovado"),"Character lost after pool reuse");
            Check(view.GetComponentsInChildren<Transform>(true).Count(x=>x.name=="CarteiroAprovado")<=count+3,"Character pool leaks instances");
            view.TimeOverride=1;view.Tick(1,5,false);view.RenderNow();
            SaveTarget(view.Target,Path.Combine(Output,"map-renderer.png"));
        }
        finally{view.Dispose();}
        var t=typeof(RoyalAvesLevelMapPreview);var window=ScriptableObject.CreateInstance<RoyalAvesLevelMapPreview>();
        try
        {
            t.GetField("level",Private).SetValue(window,1f);t.GetMethod("Build",Private).Invoke(window,new object[]{720,1600,5});
            var map=(CurvedLevelMap)t.GetField("map",Private).GetValue(window);
            for(int i=0;i<20;i++)
            {
                map.World.TimeOverride=i*.5f;
                t.GetMethod("Save",Private).Invoke(window,new object[]{Path.Combine(Output,"map-"+i.ToString("00")+".png"),false});
            }
            t.GetField("level",Private).SetValue(window,8f);t.GetMethod("Build",Private).Invoke(window,new object[]{720,1600,8});
            map=(CurvedLevelMap)t.GetField("map",Private).GetValue(window);map.World.TimeOverride=1;
            t.GetMethod("Save",Private).Invoke(window,new object[]{Path.Combine(Output,"later.png"),false});
            t.GetField("level",Private).SetValue(window,1f);t.GetMethod("Build",Private).Invoke(window,new object[]{540,960,5});
            map=(CurvedLevelMap)t.GetField("map",Private).GetValue(window);map.World.TimeOverride=1;
            t.GetMethod("Save",Private).Invoke(window,new object[]{Path.Combine(Output,"compact.png"),false});
        }
        finally{t.GetMethod("Cleanup",Private).Invoke(window,null);UnityEngine.Object.DestroyImmediate(window);}
        File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: approved full illustration replaces chibi and broken cutout rig; one continuous mesh, no joints; restrained idle motion with fixed feet; road-facing orientation; actual character bounds used by editor picking; saved lateral/phase preserved with adult scale; proxy hidden; pooled instances; tall and compact native Unity captures. Original illustration and standalone prototype preserved. Asset picker compiles; mouse interaction not automated. Phone performance not measured.");
    }
    static void SaveTarget(Texture target,string file)
    {
        var old=RenderTexture.active;RenderTexture.active=(RenderTexture)target;
        var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
        try{image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(file,image.EncodeToPNG());}
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}
    }
}
