using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

// Render library additions without saving the author's layout.
public static class MapBuildingsV17PreviewBatch
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    public static void Execute()=>Execute("v17",new[]{"postman-cottage-v17","parcel-shelter-v17","postal-signal-tower-v17","stamp-workshop-v17"});
    public static void ExecuteV18()=>Execute("v18",new[]{"delivery-station-v18","postal-kiosk-v18","postman-garage-v18","mail-lighthouse-v18"});
    [Serializable] class ArtJob { public string id; }
    [Serializable] class ArtJobs { public ArtJob[] jobs; }
    public static void ExecuteV19()
    {
        var manifest=File.ReadAllText("C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/map-library-v19-assets.json");
        Execute("v19",JsonUtility.FromJson<ArtJobs>(manifest).jobs.Select(j=>j.id).ToArray());
    }
    static void Execute(string version,string[] ids)
    {
        var Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/"+version;
        Directory.CreateDirectory(Output);
        var kit=RoyalAvesWorldMapSetup.Ensure(true);RoyalAvesMapLibrary.Ensure(kit);
        var items=ids.Select(id=>RoyalAvesMapLibrary.Items(WorldMapAssetCategory.Buildings).Single(i=>i.name=="arte-"+id)).ToArray();
        foreach(var item in items)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(item.sprite));
            if(item.kind!=PropKind.Landmark||item.size<=0||importer.spriteImportMode!=SpriteImportMode.Single||!importer.alphaIsTransparency)
                throw new Exception("Invalid building catalog/import: "+item.name);
        }
        var layout=Resources.Load<WorldMapLayout>(WorldMapLayout.SimpleResourcePath);
        var original=EditorJsonUtility.ToJson(layout);
        var chapter=layout.Find(0);if(chapter==null){chapter=new WorldMapLayout.Chapter{index=0};layout.chapters.Add(chapter);}
        var islands=UnityEngine.Object.Instantiate(Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath));
        var window=ScriptableObject.CreateInstance<RoyalAvesLevelMapPreview>();var t=typeof(RoyalAvesLevelMapPreview);
        try
        {
            chapter.manual=true;chapter.entries.Clear();
            // Two pieces per capture, on opposite banks, leaving the lane unobstructed.
            for(int pair=0;pair<(items.Length+1)/2;pair++)
            {
                chapter.entries.Clear();
                for(int j=0;j<2&&pair*2+j<items.Length;j++)
                {
                    var item=items[pair*2+j];
                    chapter.entries.Add(new WorldMapLayout.Entry{id="preview-"+item.name,sprite=item.sprite,keepSprite=true,
                        customSize=true,height=item.size,level=j==0?3:5,lateral=j==0?(version=="v17"?-2.15f:-1.9f):2.15f,kind=PropKind.Landmark});
                }
                t.GetField("captureIslands",Private).SetValue(window,islands);
                t.GetField("level",Private).SetValue(window,1f);
                t.GetMethod("Build",Private).Invoke(window,new object[]{720,1600,1});
                ((CurvedLevelMap)t.GetField("map",Private).GetValue(window)).World.TimeOverride=1;
                t.GetMethod("Save",Private).Invoke(window,new object[]{Path.Combine(Output,"buildings-"+(pair+1)+".png"),false});
            }
        }
        finally
        {
            t.GetMethod("Cleanup",Private).Invoke(window,null);UnityEngine.Object.DestroyImmediate(window);
            EditorJsonUtility.FromJsonOverwrite(original,layout);UnityEngine.Object.DestroyImmediate(islands);
        }
        File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: "+items.Length+" separate transparent Single sprites imported; all registered under Buildings with Landmark kind and individual default sizes; "+((items.Length+1)/2)+" native map captures rendered. Authoring layouts not saved. No runtime code changes.");
    }
}
