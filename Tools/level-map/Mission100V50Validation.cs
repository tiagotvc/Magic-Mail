using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class Mission100V50Validation
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/mission100-v50";
    public static void Run()
    {
        int errors=0;RoyalAvesLevelMapPreview preview=null;
        Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        try
        {
            Directory.CreateDirectory(Output);
            var mission=WorldMapMission100.Current;Check(WorldMapMission100.Active,"Mission missing or inactive");
            var kit=Resources.Load<WorldMapKit>("RoyalAvesWorldMapKit");var islands=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);
            var original=Resources.Load<WorldMapLayout>(WorldMapLayout.SimpleResourcePath);var layout=mission.layout;
            // Repair the initial authoring grouping: layout chapters are editorial blocks of 20,
            // independent of the user's physical island capacity (currently 50).
            var authored=layout.chapters.SelectMany(c=>c.entries).Where(e=>e.id.StartsWith("mission100-")).ToArray();
            foreach(var chapter in layout.chapters)chapter.entries.RemoveAll(e=>e.id.StartsWith("mission100-"));
            foreach(var e in authored)
            {
                if(e.id=="mission100-final-scribble")e.level=99.9f;
                int index=Mathf.Max(0,(Mathf.RoundToInt(e.level)-1)/WorldMapEditorial.LevelsPerChapter);
                var chapter=layout.Find(index);if(chapter==null){chapter=new WorldMapLayout.Chapter{index=index};layout.chapters.Add(chapter);}chapter.manual=true;chapter.entries.Add(e);
            }
            Mission100V50Bake.Frame(layout,new WorldMapRoute(islands));
            foreach(var c in layout.chapters)c.entries.Sort((a,b)=>a.level.CompareTo(b.level));
            EditorUtility.SetDirty(layout);AssetDatabase.SaveAssets();
            Check(authored.Length==124,"Unexpected mission prop count");
            for(int i=0;i<5;i++)Check(layout.Find(i)?.manual==true,"Missing story section "+i);
            foreach(var c in original.chapters)foreach(var e in c.entries.Where(e=>e.level>100.1f))Check(layout.chapters.SelectMany(x=>x.entries).Any(x=>x.id==e.id&&x.sprite==e.sprite&&x.level==e.level&&x.lateral==e.lateral),"Later layout changed "+e.id);
            Check(mission.characters.Length==4&&mission.characters.Select(c=>c.sprite).Distinct().Count()==4,"Character identity reused");
            Check(mission.characters[0].height>mission.characters[1].height&&mission.characters[1].height>mission.characters[2].height&&mission.characters[2].height>mission.characters[3].height,"Height hierarchy missing");
            Check(mission.characters.Take(3).Select(c=>c.wand).Distinct().Count()==3,"Repeated wands");
            var route=new WorldMapRoute(islands,150);Check(route.Gaps.All(g=>g.afterLevel>=100),"Campus split by channel");
            foreach(var c in mission.characters)
            {
                var parent=new GameObject("ForearmFixture");var material=new Material(Shader.Find("RoyalAves/WorldMap/Card"));
                try
                {
                    using(var actor=new WorldMapPostman(parent.transform,0,kit,t=>material,c.sprite))
                    {
                        var mesh=actor.Actor.GetComponent<MeshFilter>().sharedMesh;actor.Tick(c.height,0,0);var a=mesh.vertices;actor.Tick(c.height,1.2f,0);var b=mesh.vertices;
                        var weights=(float[][])typeof(WorldMapPostman).GetField("weights",Private).GetValue(actor);int moved=0,fixedCount=0;
                        for(int i=0;i<a.Length;i++){if(weights.All(w=>w[i]==0)){Check((a[i]-b[i]).sqrMagnitude<1e-12f,"Body moves outside forearm masks "+c.role);fixedCount++;}else if((a[i]-b[i]).sqrMagnitude>1e-10f)moved++;}
                        Check(fixedCount>a.Length*.65f&&moved>15,"Forearm motion missing or too broad "+c.role);
                        actor.Tick(c.height,0,0);Check(mesh.vertices.Zip(a,(x,y)=>(x-y).sqrMagnitude).All(d=>d<1e-12f),"Animation accumulates drift "+c.role);
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(parent);UnityEngine.Object.DestroyImmediate(material);}
            }
            preview=RoyalAvesLevelMapPreview.CreateScenePreview(1080,1920);typeof(RoyalAvesLevelMapPreview).GetField("seasonPreview",Private).SetValue(preview,1);
            foreach(float level in new[]{1,10,19,24,30,39,48,57,65,69,79,89,94,98})
            {
                var world=Build(preview,level);world.ConfigureDayNight(false,4,0);Save(preview,"level-"+level.ToString("000")+"-day");
                Check(world.Props.Where(p=>world.Route.LevelAt(p.s)>=1&&world.Route.LevelAt(p.s)<99.5f).All(p=>p.id!=null&&p.id.StartsWith("mission100-")),"Automatic props leaked into authored campus");
                Check(world.CharacterNeedsFrames,"Character idle does not keep preview updating");
            }
            var night=Build(preview,19);night.ConfigureDayNight(false,4,1);Save(preview,"headquarters-night");
            CaptureCast(kit,mission);
            Check(errors==0,"Unity logged errors: "+errors);
            File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: native Unity C# and shaders; 124 authored props; five editorial sections independent of island capacity; continuous route through level 100; later authored entries preserved; four distinct sprites/heights; three distinct wands; fixed torso/head/feet outside arm masks; moving forearms; no accumulated animation drift; 14 phone-format day captures and one night capture. Special beats at 50/100 are narrative data, not new gameplay modes. Main scene/islands/season were not saved. Physical device performance and manual pointer input not measured.");
            UnityEngine.Object.DestroyImmediate(preview);EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);EditorApplication.Exit(1);}
    }
    static WorldMapView Build(RoyalAvesLevelMapPreview preview,float level)
    {typeof(RoyalAvesLevelMapPreview).GetField("level",Private).SetValue(preview,level);typeof(RoyalAvesLevelMapPreview).GetMethod("Build",Private).Invoke(preview,new object[]{1080,1920,1});var world=((CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview)).World;world.TimeOverride=2;return world;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void CastOnly()
    {try{CaptureCast(Resources.Load<WorldMapKit>("RoyalAvesWorldMapKit"),WorldMapMission100.Current);EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    static void CaptureCast(WorldMapKit kit,WorldMapMission100 mission)
    {
        var root=new GameObject("CastComparison");var camRoot=new GameObject("CastCamera");var camera=camRoot.AddComponent<Camera>();var rt=new RenderTexture(1600,900,24);var material=new Material(Shader.Find("RoyalAves/WorldMap/Card"));var actors=new System.Collections.Generic.List<WorldMapPostman>();var materials=new System.Collections.Generic.List<Material>();
        try
        {
            camera.cullingMask=1<<31;camera.orthographic=true;camera.orthographicSize=1.22f;camera.transform.position=new Vector3(0,.76f,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.19f,.23f,1);camera.allowHDR=false;camera.allowMSAA=false;
            UnityEngine.Rendering.Universal.UniversalAdditionalCameraData data=camera.GetUniversalAdditionalCameraData();data.SetRenderer(kit.rendererIndex);rt.Create();camera.targetTexture=rt;
            for(int i=0;i<mission.characters.Length;i++){var c=mission.characters[i];var a=new WorldMapPostman(root.transform,31,kit,t=>{var m=new Material(material);m.SetTexture("_MainTex",t);materials.Add(m);return m;},c.sprite);a.Actor.localPosition=new Vector3(new[]{-1.55f,-.55f,.50f,1.65f}[i],0,0);a.Tick(c.height,0,0);actors.Add(a);}
            var request=new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt};UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,request);
            var old=RenderTexture.active;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,"cast-comparison.png"),image.EncodeToPNG());}finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}
        }
        finally{foreach(var a in actors)a.Dispose();foreach(var m in materials)UnityEngine.Object.DestroyImmediate(m);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(camRoot);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
    static void Save(RoyalAvesLevelMapPreview preview,string name)
    {preview.RenderScenePreview();var rt=(RenderTexture)preview.SceneTexture;var old=RenderTexture.active;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());}finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}}
}
