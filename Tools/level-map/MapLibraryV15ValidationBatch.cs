using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using RoyalAves.EditorTools;

public static class MapLibraryV15ValidationBatch
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/v15";
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void Execute()
    {
        GroundV14ValidationBatch.Execute();Directory.CreateDirectory(Output);
        var kit=RoyalAvesWorldMapSetup.Ensure(true);RoyalAvesMapLibrary.Ensure(kit);
        Check(kit.waterRipple!=null,"Water wake Sprite missing");
        foreach(WorldMapAssetCategory category in Enum.GetValues(typeof(WorldMapAssetCategory)))
        {
            var items=RoyalAvesMapLibrary.Items(category);
            Check(items.Length>0&&items.All(i=>i.category==category),"Category is empty or contains wrong items: "+category);
            if(category==WorldMapAssetCategory.Buildings)Check(items.All(i=>i.kind==PropKind.Landmark)&&items.Length>=5,"Building category mixed or new buildings missing");
            if(category==WorldMapAssetCategory.Trees)Check(items.All(i=>i.kind==PropKind.Tree),"Tree category contains props");
            if(category==WorldMapAssetCategory.Characters)Check(items.All(i=>i.kind==PropKind.Character),"Character category mixed");
        }
        var props=RoyalAvesMapLibrary.Items(WorldMapAssetCategory.Props);
        Check(props.Any(i=>i.title=="Graminha"&&i.kind==PropKind.GroundDetail)&&props.Count(i=>i.title.StartsWith("Arbusto"))>=3,"Independent grass or shrub variants missing");
        var edited=UnityEngine.Object.Instantiate(Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath));
        var first=edited.islands[0];first.channelWidth=2.5f;first.channelDepth=.35f;first.bankShape=WorldMapBankShape.Angular;first.channelRocks=true;
        first.customChannelColor=true;first.channelColor=new Color32(255,147,19,255);edited.islands[1].groundStyle=WorldMapGroundStyle.Lavender;
        const string saved="Assets/MapV15Crossings.asset";AssetDatabase.CreateAsset(edited,saved);AssetDatabase.SaveAssets();
        var serialized=EditorJsonUtility.ToJson(edited);AssetDatabase.ImportAsset(saved,ImportAssetOptions.ForceUpdate);
        edited=AssetDatabase.LoadAssetAtPath<WorldMapIslands>(saved);first=edited.islands[0];
        Check(EditorJsonUtility.ToJson(edited)==serialized,"Crossing settings failed save/reload");
        var route=new WorldMapRoute(edited,100);var gap=route.Gaps[0];
        Check(Mathf.Abs(gap.end-gap.start-2.5f)<.001f&&Mathf.Abs(route.WaterHeight((gap.start+gap.end)*.5f)+.35f)<.001f,"Channel width/depth ignored");
        var window=ScriptableObject.CreateInstance<RoyalAvesLevelMapPreview>();var t=typeof(RoyalAvesLevelMapPreview);
        var layout=Resources.Load<WorldMapLayout>(WorldMapLayout.SimpleResourcePath);var original=EditorJsonUtility.ToJson(layout);
        try
        {
            t.GetField("captureIslands",Private).SetValue(window,edited);
            Capture(window,"crossing-deep",19,19);
            var view=((CurvedLevelMap)t.GetField("map",Private).GetValue(window)).World;
            var wake=view.Props.FirstOrDefault(p=>p.kind==PropKind.WaterRipple&&p.id.StartsWith("river-20-"));
            Check(wake.sprite==kit.waterRipple,"Water animation is not using separate Sprite");
            var visible=view.GetComponentsInChildren<MeshFilter>().Where(m=>m.sharedMesh!=null&&m.sharedMesh.name=="GroundDetailConformed").ToArray();
            Check(visible.Length>=2,"River Sprites absent");
            var moving=visible.First(m=>m.GetComponent<MeshRenderer>().sharedMaterial.mainTexture==kit.waterRipple.texture);
            var before=moving.sharedMesh.vertices;view.TimeOverride=2;view.Tick(19,19,false);
            Check(before.Zip(moving.sharedMesh.vertices,(a,b)=>Vector3.Distance(a,b)).Max()>.01f,"River wakes do not move");
            for(int frame=0;frame<8;frame++)
            {
                view.TimeOverride=frame*.4f;t.GetMethod("Save",Private).Invoke(window,new object[]{Path.Combine(Output,"water-"+frame.ToString("00")+".png"),false});
            }
            first.channelWidth=1.45f;first.channelDepth=.14f;first.bankShape=WorldMapBankShape.Rounded;first.channelRocks=false;
            Capture(window,"crossing-narrow",19,19);
            first.channelWidth=2.0f;first.channelDepth=.12f;first.bankShape=WorldMapBankShape.Scalloped;first.crossing=WorldMapCrossing.Stones;first.waterMotion=WorldMapWaterMotion.Ripples;first.channelColor=new Color32(37,167,220,255);
            Capture(window,"crossing-stones",19,19);
            view=((CurvedLevelMap)t.GetField("map",Private).GetValue(window)).World;
            Check(!view.Props.Any(p=>p.kind==PropKind.WaterRipple&&p.id.StartsWith("river-20-")),"Wake animation ignored selected motion style");
            first.crossing=WorldMapCrossing.Planks;first.channelDepth=.26f;first.waterMotion=WorldMapWaterMotion.Calm;
            Capture(window,"crossing-planks",19,19);
            // Place new art through the same real entries used by the editor; no source layout save.
            var chapter=layout.Find(0);if(chapter==null){chapter=new WorldMapLayout.Chapter{index=0,manual=true};layout.chapters.Add(chapter);}
            chapter.manual=true;chapter.entries.Clear();
            var buildings=RoyalAvesMapLibrary.Items(WorldMapAssetCategory.Buildings).Where(i=>i.name.StartsWith("arte-")).ToArray();
            foreach(var building in buildings)
            {
                chapter.entries.Clear();chapter.entries.Add(new WorldMapLayout.Entry{id="proof-building",sprite=building.sprite,keepSprite=true,customSize=true,height=1.65f,level=3,lateral=-2.1f,kind=PropKind.Landmark});
                foreach(var shrub in props.Where(p=>p.title=="Arbusto coral"||p.title=="Arbusto violeta"))
                    chapter.entries.Add(new WorldMapLayout.Entry{id="proof-"+shrub.name,sprite=shrub.sprite,keepSprite=true,customSize=true,height=.58f,level=shrub.title.Contains("coral")?1.8f:4.4f,lateral=1.7f,kind=PropKind.Small});
                var grass=props.First(i=>i.title=="Graminha");chapter.entries.Add(new WorldMapLayout.Entry{id="proof-grass",sprite=grass.sprite,keepSprite=true,customSize=true,height=.25f,level=1.6f,lateral=-1.6f,kind=PropKind.GroundDetail});
                Capture(window,building.name,1,1);
                view=((CurvedLevelMap)t.GetField("map",Private).GetValue(window)).World;
                var shrubCard=view.GetComponentsInChildren<MeshFilter>().First(m=>m.sharedMesh!=null&&m.sharedMesh.name.StartsWith("Card_shrub-"));
                var scale=shrubCard.transform.localScale;view.TimeOverride=2.2f;view.Tick(1,1,false);
                Check(Vector3.Distance(scale,shrubCard.transform.localScale)>.001f,"Shrubs lost pulse animation");
            }
        }
        finally{EditorJsonUtility.FromJsonOverwrite(original,layout);t.GetMethod("Cleanup",Private).Invoke(window,null);UnityEngine.Object.DestroyImmediate(window);}
        File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: five filtered library folders; three new buildings; animated shrub color variants; separate grass prop; independent puddle/lava props; eight floor styles and custom colors; width/depth/shape/rocks/crossing/motion/channel color survive serialization; moving V Sprite wakes; alternate narrow/deep/stepping-stone/plank crossings; actual shared runtime/editor rendering. Source layouts restored in memory and never saved. Mouse interaction and phone performance were not measured.");
    }
    static void Capture(RoyalAvesLevelMapPreview window,string name,float level,int reached)
    {
        var t=typeof(RoyalAvesLevelMapPreview);t.GetField("level",Private).SetValue(window,level);
        t.GetMethod("Build",Private).Invoke(window,new object[]{720,1600,reached});
        ((CurvedLevelMap)t.GetField("map",Private).GetValue(window)).World.TimeOverride=1;
        t.GetMethod("Save",Private).Invoke(window,new object[]{Path.Combine(Output,name+".png"),false});
    }
}
