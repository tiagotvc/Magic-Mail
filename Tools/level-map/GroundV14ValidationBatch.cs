using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using RoyalAves.EditorTools;

public static class GroundV14ValidationBatch
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/v14";
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void Execute()
    {
        Directory.CreateDirectory(Output);
        var kit=RoyalAvesWorldMapSetup.Ensure(true);
        Check(kit.groundDetails.Length==2&&kit.groundDetails.All(x=>x!=null),"Separate ground Sprites not bound");
        var source=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);
        var edited=UnityEngine.Object.Instantiate(source);
        var layout=Resources.Load<WorldMapLayout>(WorldMapLayout.SimpleResourcePath);
        var layoutJson=EditorJsonUtility.ToJson(layout);
        try
        {
            edited.islands[0].groundStyle=WorldMapGroundStyle.Volcanic;
            edited.islands[1].groundStyle=WorldMapGroundStyle.Lavender;
            const string saved="Assets/GroundV14Roundtrip.asset";
            AssetDatabase.CreateAsset(edited,saved);AssetDatabase.SaveAssets();
            var json=EditorJsonUtility.ToJson(edited);AssetDatabase.ImportAsset(saved,ImportAssetOptions.ForceUpdate);
            edited=AssetDatabase.LoadAssetAtPath<WorldMapIslands>(saved);
            Check(EditorJsonUtility.ToJson(edited)==json,"Ground settings failed save/reload");
            var route=new WorldMapRoute(edited,WorldMapLevelCatalog.LastLevel);
            Check(route.LavaAt(route.Pos(1))&&!route.LavaAt(route.Pos(21)),"Ground selection is still forced green");
            foreach(var gap in route.Gaps)Check(Mathf.Abs(route.CenterX(gap.start)-route.CenterX(gap.end))<.0002f,"Bridge no longer joins both banks");
            Check(WorldMapTerrain.WaterY<-.15f&&WorldMapTerrain.Floor<WorldMapTerrain.WaterY,"Channel has no visible bank face");
            var window=ScriptableObject.CreateInstance<RoyalAvesLevelMapPreview>();var t=typeof(RoyalAvesLevelMapPreview);
            try
            {
                t.GetField("captureIslands",Private).SetValue(window,edited);
                foreach(WorldMapGroundStyle style in Enum.GetValues(typeof(WorldMapGroundStyle)))
                {
                    edited.islands[0].groundStyle=style;edited.islands[0].customColors=false;
                    Capture(window,"floor-"+style.ToString().ToLowerInvariant(),1,1,720,1600);
                }
                edited.islands[0].groundStyle=WorldMapGroundStyle.Green;
                Capture(window,"crossing",19,19,720,1600);
                Capture(window,"compact",1,1,540,960);
                // Real editor entries: independently selectable, movable and removable.
                var chapter=layout.Find(0);
                if(chapter==null){chapter=new WorldMapLayout.Chapter{index=0,manual=true};layout.chapters.Add(chapter);}
                chapter.manual=true;
                for(int i=0;i<2;i++)chapter.entries.Add(new WorldMapLayout.Entry{id="ground-detail-proof-"+i,
                    sprite=kit.groundDetails[i],keepSprite=true,customSize=true,height=.72f,
                    level=i==0?1.7f:3.5f,lateral=i==0?-1.65f:.73f,kind=PropKind.GroundDetail,shadow=false});
                Capture(window,"details",1,1,720,1600);
                var view=((CurvedLevelMap)t.GetField("map",Private).GetValue(window)).World;
                var detailProps=view.Props.Where(p=>p.id!=null&&p.id.StartsWith("ground-detail-proof-")).ToArray();
                Check(detailProps.Length==2,"Ground details do not use saved layout entries");
                foreach(var p in detailProps)
                {
                    int index=Enumerable.Range(0,view.Props.Count).First(i=>view.Props[i].id==p.id);
                    Check(view.ProjectProp(index,.45f,out var bounds,out _)&&bounds.height>.008f,"Ground Sprite selection bounds are wrong");
                    Check(view.ProjectPropAnchor(index,.45f,out var uv)&&view.GroundAtViewport(uv,1,.45f,out var phase,out var side)&&
                        Mathf.Abs(side-p.offset)<.04f&&Mathf.Abs(phase-view.Route.LevelAt(p.s))<.04f,"Ground Sprite drag anchor is wrong");
                }
                Check(view.GetComponentsInChildren<MeshFilter>().Count(m=>m.sharedMesh!=null&&m.sharedMesh.name=="GroundDetailConformed"&&
                    kit.groundDetails.Any(sprite=>sprite.texture==m.GetComponent<MeshRenderer>().sharedMaterial.mainTexture))==2,"Ground Sprites are not independent conformed meshes");
                chapter.entries.RemoveAll(e=>e.id!=null&&e.id.StartsWith("ground-detail-proof-"));view.ReloadScenery();view.Tick(1,1,false);
                Check(!view.Props.Any(p=>p.id!=null&&p.id.StartsWith("ground-detail-proof-")),"Removed ground Sprite still rendered");
                // Shared runtime materials must retain distinct island floors simultaneously.
                edited.islands[0].groundStyle=WorldMapGroundStyle.Volcanic;
                Capture(window,"crossing-lava",19,19,720,1600);
                view=((CurvedLevelMap)t.GetField("map",Private).GetValue(window)).World;
                var mats=view.transform.Find("Chao").GetComponent<MeshRenderer>().sharedMaterials;
                Check(mats[0].GetFloat("_LavaTerrain")==1&&mats[1].GetFloat("_LavaTerrain")==0,"Island materials leaked");
                Check(mats[0].GetVector("_SimpleLawnColor")!=mats[1].GetVector("_SimpleLawnColor"),"Island floor colors were lost");
                edited.islands[0].customColors=true;edited.islands[0].ground=new Color(.22f,.63f,.62f);edited.islands[0].path=new Color(.84f,.70f,.96f);
                Capture(window,"custom",1,1,720,1600);
                view=((CurvedLevelMap)t.GetField("map",Private).GetValue(window)).World;
                Check(view.KitAt(view.Route.Pos(1)).simpleLawnColor==edited.islands[0].ground,"Custom colors ignored");
            }
            finally{t.GetMethod("Cleanup",Private).Invoke(window,null);UnityEngine.Object.DestroyImmediate(window);}
        }
        finally{EditorJsonUtility.FromJsonOverwrite(layoutJson,layout);}
        File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: eight illustrated floor styles and custom colors; per-island save/reload; separate simultaneous runtime materials; smooth bridge joins; deeper water/bank geometry; separate transparent puddle/lava Sprites through real layout entries; selectable projected bounds; drag anchor roundtrip; removal. Native Unity captures at 720x1600 and 540x960. Source authoring layouts were not written; phone performance and mouse interaction were not measured.");
    }
    static void Capture(RoyalAvesLevelMapPreview window,string name,float level,int reached,int width,int height)
    {
        var t=typeof(RoyalAvesLevelMapPreview);t.GetField("level",Private).SetValue(window,level);
        t.GetMethod("Build",Private).Invoke(window,new object[]{width,height,reached});
        ((CurvedLevelMap)t.GetField("map",Private).GetValue(window)).World.TimeOverride=1;
        t.GetMethod("Save",Private).Invoke(window,new object[]{Path.Combine(Output,name+".png"),false});
    }
}
