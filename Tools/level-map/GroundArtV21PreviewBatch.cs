using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using RoyalAves.EditorTools;

// Native comparison captures; fixtures exist only in memory in the isolated project.
public static class GroundArtV21PreviewBatch
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Root="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/v21";
    static void Check(bool v,string message){if(!v)throw new Exception(message);}
    public static void Execute(bool before)
    {
        var output=Root+(before?"/before":"/after");Directory.CreateDirectory(output);
        var kit=RoyalAvesWorldMapSetup.Ensure(true);RoyalAvesMapLibrary.Ensure(kit);
        var ids=new[]{"crooked-canopy-v20","coral-fan-tree-v20","teal-wonky-pine-v20","crooked-mail-palm-v20"};
        var trees=ids.Select(id=>RoyalAvesMapLibrary.Items(WorldMapAssetCategory.Trees).Single(x=>x.name=="arte-"+id)).ToArray();
        foreach(var item in trees)Check(item.kind==PropKind.Tree&&item.sprite!=null,"Tree catalog/import invalid");
        var dock=RoyalAvesMapLibrary.Items(WorldMapAssetCategory.Buildings).Single(x=>x.title=="Cais postal");
        var islands=UnityEngine.Object.Instantiate(Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath));
        var layout=Resources.Load<WorldMapLayout>(WorldMapLayout.SimpleResourcePath);var original=EditorJsonUtility.ToJson(layout);
        var chapter=layout.Find(0);if(chapter==null){chapter=new WorldMapLayout.Chapter{index=0};layout.chapters.Add(chapter);}
        chapter.manual=true;chapter.entries.Clear();
        var levels=new[]{2.7f,4.5f,6.7f,8f};
        for(int i=0;i<trees.Length;i++)chapter.entries.Add(new WorldMapLayout.Entry{id="tree-v20-proof-"+i,sprite=trees[i].sprite,keepSprite=true,
            customSize=true,height=trees[i].size,level=levels[i],lateral=i%2==0?-1.95f:2.05f,kind=PropKind.Tree,shadow=true});
        chapter.entries.Add(new WorldMapLayout.Entry{id="dock-v21-proof",sprite=dock.sprite,keepSprite=true,customSize=true,height=1.0f,level=1.35f,lateral=1.9f,kind=PropKind.Landmark});
        var window=ScriptableObject.CreateInstance<RoyalAvesLevelMapPreview>();var t=typeof(RoyalAvesLevelMapPreview);
        try
        {
            t.GetField("captureIslands",Private).SetValue(window,islands);
            foreach(WorldMapGroundStyle style in Enum.GetValues(typeof(WorldMapGroundStyle)))
            {
                islands.islands[0].groundStyle=style;islands.islands[0].customColors=false;
                Capture(window,Path.Combine(output,"floor-"+style.ToString().ToLowerInvariant()+".png"),1,1,720,1600);
            }
            islands.islands[0].groundStyle=WorldMapGroundStyle.Green;islands.islands[1].groundStyle=WorldMapGroundStyle.Lavender;
            Capture(window,Path.Combine(output,"crossing.png"),19,19,720,1600);
            islands.islands[0].customColors=true;islands.islands[0].ground=new Color(.26f,.63f,.68f);islands.islands[0].path=new Color(.90f,.65f,.84f);
            Capture(window,Path.Combine(output,"custom.png"),1,1,720,1600);
            var view=((CurvedLevelMap)t.GetField("map",Private).GetValue(window)).World;
            Check(view.KitAt(view.Route.Pos(1)).simpleLawnColor==islands.islands[0].ground,"Custom palette lost");
            islands.islands[0].customColors=false;
            Capture(window,Path.Combine(output,"compact.png"),1,1,540,960);
            if(!before)
            {
                var hasPaint=typeof(WorldMapKit).GetField("groundBrushLawn");
                Check(hasPaint!=null&&hasPaint.GetValue(kit)!=null,"Ground paint missing");
                Check(kit.surface.isSupported,"Surface shader unsupported");
                File.WriteAllText(Path.Combine(Root,"validation.txt"),"PASS: four new Tree sprites and library records; eight floor palettes rendered with shared production code; custom colors, island crossing and compact viewport captured; supported surface shader and bound ground paint. Authoring layouts/islands were not saved. Mobile performance and manual editor interaction not measured.");
            }
        }
        finally
        {
            t.GetMethod("Cleanup",Private).Invoke(window,null);UnityEngine.Object.DestroyImmediate(window);
            EditorJsonUtility.FromJsonOverwrite(original,layout);UnityEngine.Object.DestroyImmediate(islands);
        }
    }
    static void Capture(RoyalAvesLevelMapPreview window,string file,float level,int reached,int width,int height)
    {
        var t=typeof(RoyalAvesLevelMapPreview);t.GetField("level",Private).SetValue(window,level);
        t.GetMethod("Build",Private).Invoke(window,new object[]{width,height,reached});
        ((CurvedLevelMap)t.GetField("map",Private).GetValue(window)).World.TimeOverride=1;
        t.GetMethod("Save",Private).Invoke(window,new object[]{file,false});
    }
}
