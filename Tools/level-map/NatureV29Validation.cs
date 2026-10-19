using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

public static class NatureV29Validation
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/nature-v29";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int errors;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        try{Execute();Check(errors==0,"Unity logged errors");EditorApplication.Exit(0);}
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static WorldMapLibraryItem Item(string folder,string file)=>AssetDatabase.LoadAssetAtPath<WorldMapLibraryItem>("Assets/RoyalAves/Resources/MapLibrary/"+folder+"/arte-"+file+".asset");
    static WorldMapLayout.Entry Entry(string id,WorldMapLibraryItem item,float level,float side)=>new WorldMapLayout.Entry
    {id=id,sprite=item.sprite,keepSprite=true,customSize=true,height=item.size,kind=item.kind,level=level,lateral=side,groundPinned=true};
    static object Slot(WorldMapView world,string id)
    {
        var index=Enumerable.Range(0,world.Props.Count).First(i=>world.Props[i].id==id);
        var slots=(IDictionary)typeof(WorldMapView).GetField("activeSlots",Private).GetValue(world);
        Check(slots.Contains(index),"Test object is not rendered: "+id);return slots[index];
    }
    static Transform Card(object slot)=>(Transform)slot.GetType().GetField("card").GetValue(slot);
    static int Caps(object slot)=>((MeshFilter[])slot.GetType().GetField("caps").GetValue(slot)).Count(c=>c.gameObject.activeSelf);
    static void Pose(WorldMapView world,object slot,PropPlacement p)=>typeof(WorldMapView).GetMethod("Pose",Private).Invoke(world,new object[]{slot,p,world.Route.MetersAt(1),true});
    static void Execute()
    {
        Directory.CreateDirectory(Output);
        var kit=RoyalAvesWorldMapSetup.Ensure(false);RoyalAvesMapLibrary.Ensure(kit);
        var pine=Item("Arvores","pine-classic-glossy-v29");
        var green=Item("Props","shrub-green-glossy-v29");var orange=Item("Props","shrub-orange-glossy-v29");
        Check(pine!=null&&pine.sprite!=null&&pine.kind==PropKind.Tree,"Classic repainted pine missing");
        Check(new[]{green,orange}.All(i=>i!=null&&i.sprite!=null&&i.kind==PropKind.Small),"Recolored shrub library items missing");
        foreach(var item in new[]{pine,green,orange})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(item.sprite));
            Check(importer.textureCompression==TextureImporterCompression.Uncompressed&&item.sprite.texture.width>1000,"New art was compressed or shrunk");
        }
        var layout=Resources.Load<WorldMapLayout>(WorldMapLayout.SimpleResourcePath);var original=EditorJsonUtility.ToJson(layout);
        var chapter=layout.Find(0);if(chapter==null){chapter=new WorldMapLayout.Chapter{index=0};layout.chapters.Add(chapter);}
        chapter.manual=true;chapter.entries.Clear();
        var left=Entry("green-left",green,2,-1.6f);var right=Entry("orange-right",orange,2.2f,1.3f);right.flipX=true;
        var clearPine=Entry("pine-clear",pine,4,-1.3f);clearPine.theme=WorldMapObjectTheme.None;
        var icePine=Entry("pine-ice",pine,4.2f,1.3f);icePine.theme=WorldMapObjectTheme.Ice;
        chapter.entries.AddRange(new[]{left,right,clearPine,icePine});
        RoyalAvesLevelMapPreview preview=null;
        try
        {
            // Test actual save/reload, including references, without saving the source layout.
            const string fixture="Assets/NatureV29Roundtrip.asset";
            var copy=UnityEngine.Object.Instantiate(layout);AssetDatabase.CreateAsset(copy,fixture);AssetDatabase.SaveAssetIfDirty(copy);
            AssetDatabase.ImportAsset(fixture,ImportAssetOptions.ForceUpdate);
            var restored=AssetDatabase.LoadAssetAtPath<WorldMapLayout>(fixture);
            Check(restored.Find(0).entries[1].flipX&&restored.Find(0).entries[3].theme==WorldMapObjectTheme.Ice,"Facing/theme did not persist");
            Check(restored.Find(0).entries[0].groundPinned&&Mathf.Abs(restored.Find(0).entries[0].groundDepth-.06f)<.0001f,"Ground pin/depth did not persist");
            var route=new WorldMapRoute(Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath),100);
            var placed=restored.Find(0).entries[1].Placement(route,kit);var from=WorldMapLayout.Entry.From(placed,route);
            Check(placed.flipX&&from.flipX&&from.theme==placed.theme&&from.groundPinned&&from.groundDepth==placed.groundDepth,"Converting between editor and runtime loses facing/theme/ground pin");
            AssetDatabase.DeleteAsset(fixture);

            preview=RoyalAvesLevelMapPreview.CreateScenePreview(1080,1920);
            var map=(CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview);
            var world=map.World;world.TimeOverride=1;preview.RenderScenePreview();
            var l=Slot(world,left.id);var r=Slot(world,right.id);
            Check(Card(l).localScale.x>0&&Card(r).localScale.x<0,"Manual shrub mirroring is not rendered");
            var grounded=world.Props.First(p=>p.id==left.id);var anchor=Card(l).parent.localPosition;
            var sunk=Card(l).localPosition.y;grounded.groundPinned=false;Pose(world,l,grounded);
            Check(Mathf.Abs(Card(l).localPosition.y-sunk-grounded.height*grounded.groundDepth)<.0001f,"Pin depth did not lower the artwork by its chosen amount");
            Check(Card(l).parent.localPosition==anchor,"Pinning moved the saved ground anchor");
            grounded.groundPinned=true;Pose(world,l,grounded);
            var contact=(Transform)l.GetType().GetField("shadowT").GetValue(l);
            Check(contact.localPosition==new Vector3(0,.008f,0),"Contact shadow does not stay at the planted foot");
            var mirrored=world.Props.First(p=>p.id==right.id);var originalAnchor=Card(r).parent.localPosition;
            mirrored.flipX=false;Pose(world,r,mirrored);Check(Card(r).localScale.x>0,"Pooled card retained stale mirroring");
            mirrored.flipX=true;Pose(world,r,mirrored);
            Check(Card(r).parent.localPosition==originalAnchor,"Mirroring moved the object anchor");
            var a=Slot(world,clearPine.id);var b=Slot(world,icePine.id);
            Check(Caps(a)==0&&Caps(b)==1,"Per-object ice must dress only the chosen pine using its registered overlay");
            var theme=world.Props.First(p=>p.id==icePine.id);var sourceSprite=theme.sprite;
            theme.theme=WorldMapObjectTheme.None;Pose(world,b,theme);Check(Caps(b)==0,"Disabling theme leaves stale ice on a pooled slot");
            theme.theme=WorldMapObjectTheme.Ice;Pose(world,b,theme);Check(Caps(b)==1&&theme.sprite==sourceSprite,"Ice changed the base sprite");
            var auto=world.Props.First(p=>p.id==left.id);auto.theme=WorldMapObjectTheme.Season;
            Check(!WorldMapScenery.HasIceTheme(auto,false)&&WorldMapScenery.HasIceTheme(auto,true),"Automatic theme does not follow seasonal snow");
            auto.theme=WorldMapObjectTheme.None;Check(!WorldMapScenery.HasIceTheme(auto,true),"Theme opt-out ignored during Christmas");
            var reflected=world.Props.First(p=>p.id==right.id);reflected.kind=PropKind.GroundDetail;
            reflected.flipX=false;Pose(world,r,reflected);
            var detail=Card(r).Find("DetalheDoChao").GetComponent<MeshFilter>();var firstUV=detail.sharedMesh.uv[0].x;
            reflected.flipX=true;Pose(world,r,reflected);Check(detail.sharedMesh.uv[0].x>firstUV,"Ground props ignored manual mirroring");
            preview.RenderScenePreview();Save(preview,"normal-1080x1920.png");

            // Snow preview changes the season, while manual per-object opt-out remains effective.
            UnityEngine.Object.DestroyImmediate(preview);preview=null;
            preview=RoyalAvesLevelMapPreview.CreateScenePreview(1080,2280);
            typeof(RoyalAvesLevelMapPreview).GetField("seasonPreview",Private).SetValue(preview,2);
            typeof(RoyalAvesLevelMapPreview).GetMethod("Build",Private).Invoke(preview,new object[]{1080,2280,1});
            map=(CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview);world=map.World;world.TimeOverride=1;
            preview.RenderScenePreview();
            Check(Caps(Slot(world,left.id))==1&&Caps(Slot(world,right.id))==1,"Christmas theme did not reach both shrubs");
            Check(Caps(Slot(world,clearPine.id))==0,"Christmas overrides object opt-out");
            Check(Caps(Slot(world,icePine.id))==1,"Classic pine lost its registered snow layer");
            Save(preview,"christmas-1080x2280.png");
        }
        finally{if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);EditorJsonUtility.FromJsonOverwrite(original,layout);}
        File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: native 1080x1920 and 1080x2280 captures; classic pine and green/orange shrubs in correct library categories; uncompressed full-size art; facing/theme/ground pin/depth survive asset save/reload and placement conversion; pinning lowers artwork by chosen fraction, keeps anchor fixed and centers contact shadow; actual card mirrors without moving anchor, resets on pooled slot and also mirrors ground-prop UVs; explicit ice only on selected object; registered pine snow layer; toggling off clears overlays; seasonal snow follows Christmas while opt-out remains clear; base sprites unchanged; main editor layouts never saved by validation.");
    }
    static void Save(RoyalAvesLevelMapPreview preview,string name)
    {
        var rt=(RenderTexture)preview.SceneTexture;var previous=RenderTexture.active;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name),image.EncodeToPNG());}
        finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}
    }
}
