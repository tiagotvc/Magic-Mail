using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using RoyalAves.EditorTools;

public static class MapLibraryV16ValidationBatch
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/v16";
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void Execute()
    {
        Directory.CreateDirectory(Output);var kit=RoyalAvesWorldMapSetup.Ensure(true);RoyalAvesMapLibrary.Ensure(kit);
        var dock=RoyalAvesMapLibrary.Items(WorldMapAssetCategory.Buildings).FirstOrDefault(i=>i.title=="Cais postal");
        var stones=RoyalAvesMapLibrary.Items(WorldMapAssetCategory.Props).FirstOrDefault(i=>i.title=="Pedras baixas");
        Check(dock!=null&&stones!=null&&kit.waterRocks==stones.sprite,"New art missing or assigned to wrong category");
        Check(stones.kind==PropKind.GroundDetail,"Stones will stand vertically");
        foreach(WorldMapAssetCategory category in Enum.GetValues(typeof(WorldMapAssetCategory)))
            foreach(var item in RoyalAvesMapLibrary.Items(category))
                Check(item.sprite.rect.width>0&&item.sprite.rect.height>0&&!string.IsNullOrEmpty(item.Label),"Invalid thumbnail or empty item label");
        var blank=ScriptableObject.CreateInstance<WorldMapLibraryItem>();blank.name="Sem título";
        Check(blank.Label=="Sem título","Untitled library item cannot be searched");UnityEngine.Object.DestroyImmediate(blank);
        var layout=Resources.Load<WorldMapLayout>(WorldMapLayout.SimpleResourcePath);var original=EditorJsonUtility.ToJson(layout);
        var edited=UnityEngine.Object.Instantiate(Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath));
        var chapter=layout.Find(0);if(chapter==null){chapter=new WorldMapLayout.Chapter{index=0};layout.chapters.Add(chapter);}
        chapter.manual=true;chapter.entries.Clear();
        chapter.entries.Add(new WorldMapLayout.Entry{id="resize-proof",sprite=kit.simplePostal[0],keepSprite=true,customSize=true,height=.90f,level=2.5f,lateral=1.8f,kind=PropKind.Landmark});
        try
        {
            var view=WorldMapView.Create(null,Resources.Load<LobbyFeaturesConfig>("RoyalAvesFeatures"),kit,WorldMapLevelCatalog.LastLevel,0,true,edited);
            try
            {
                view.SetFixedSize(720,1600);view.TimeOverride=1;view.Tick(1,1,false);
                var index=Enumerable.Range(0,view.Props.Count).First(i=>view.Props[i].id=="resize-proof");
                var slot=((IDictionary)typeof(WorldMapView).GetField("activeSlots",Private).GetValue(view))[index];
                var filter=(MeshFilter)slot.GetType().GetField("cardMesh").GetValue(slot);
                var before=filter.sharedMesh;var height=before.bounds.size.y;var placement=view.Props[index];placement.height=1.35f;
                typeof(WorldMapView).GetMethod("Pose",Private).Invoke(view,new object[]{slot,placement,view.Route.MetersAt(1),true});
                Check(filter.sharedMesh!=before&&Mathf.Abs(filter.sharedMesh.bounds.size.y/height-1.5f)<.005f,"Resizing same Sprite on pooled slot keeps stale mesh");
                placement.kind=PropKind.Steam;
                typeof(WorldMapView).GetMethod("Pose",Private).Invoke(view,new object[]{slot,placement,view.Route.MetersAt(1),true});
                var renderer=filter.GetComponent<MeshRenderer>();Check(renderer.sharedMaterial.shader==kit.vapor,"Changing animation kind keeps stale material");
            }
            finally{view.Dispose();}
            chapter.entries.Clear();chapter.entries.Add(new WorldMapLayout.Entry{id="dock-proof",sprite=dock.sprite,keepSprite=true,customSize=true,height=1.45f,level=3,lateral=-1.9f,kind=PropKind.Landmark});
            chapter.entries.Add(new WorldMapLayout.Entry{id="stone-proof",sprite=stones.sprite,keepSprite=true,customSize=true,height=.32f,level=1.8f,lateral=1.7f,kind=PropKind.GroundDetail,shadow=false});
            edited.islands[0].channelRocks=true;edited.islands[0].customChannelColor=true;edited.islands[0].channelColor=new Color32(40,167,218,255);
            edited.islands[1].groundStyle=WorldMapGroundStyle.Lavender;
            var window=ScriptableObject.CreateInstance<RoyalAvesLevelMapPreview>();var t=typeof(RoyalAvesLevelMapPreview);
            try
            {
                t.GetField("captureIslands",Private).SetValue(window,edited);
                Capture(window,"dock",1,1,720,1600);Capture(window,"river",19,19,720,1600);
                var viewMap=((CurvedLevelMap)t.GetField("map",Private).GetValue(window)).World;
                Check(viewMap.Props.Where(p=>p.kind==PropKind.WaterRock).All(p=>p.sprite==kit.waterRocks),"River still uses jewel-like old pebbles");
                Capture(window,"compact",1,1,540,960);
            }
            finally{t.GetMethod("Cleanup",Private).Invoke(window,null);UnityEngine.Object.DestroyImmediate(window);}
        }
        finally{EditorJsonUtility.FromJsonOverwrite(original,layout);UnityEngine.Object.DestroyImmediate(edited);}
        File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: categorized dock and low stones; valid Sprite thumbnail regions and fallback labels; same-Sprite resize on the SAME pooled slot rebuilds its mesh; animation-kind change replaces material; new rocks used in river; native dock/river/compact captures. Thumbnail GUI compiles; manual mouse interaction and phone performance not measured. Source authoring layouts not saved.");
    }
    static void Capture(RoyalAvesLevelMapPreview window,string name,float level,int reached,int width,int height)
    {
        var t=typeof(RoyalAvesLevelMapPreview);t.GetField("level",Private).SetValue(window,level);
        t.GetMethod("Build",Private).Invoke(window,new object[]{width,height,reached});
        ((CurvedLevelMap)t.GetField("map",Private).GetValue(window)).World.TimeOverride=1;
        t.GetMethod("Save",Private).Invoke(window,new object[]{Path.Combine(Output,name+".png"),false});
    }
}
