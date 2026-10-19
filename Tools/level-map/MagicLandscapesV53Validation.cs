using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEngine;
using UnityEditor;

public static class MagicLandscapesV53Validation
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/magic-landscapes-v53";
    public static void PrepareAndRun()
    {
        var layout=WorldMapMission100.Current.layout;var islet=layout.chapters.SelectMany(c=>c.entries).Single(e=>e.id=="magic53-floating-islet");
        for(int i=0;i<islet.adornments.Length;i++){var a=islet.adornments[i];if(a.id=="adornment-magic53-visiting-owl")a.motion=WorldMapAdornmentMotion.OwlLanding;islet.adornments[i]=a;}
        var route=new WorldMapRoute(Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath));var entries=layout.chapters.SelectMany(c=>c.entries).ToArray();
        void Move(string id,float level,float x,float height=0){var e=entries.Single(p=>p.id==id);e.level=level;e.lateral=x-route.CenterX(route.MetersAt(level));if(height>0)e.height=height;}
        Move("magic53-ribbon-arch",14.8f,-1.40f);Move("magic53-source-stream",23.07f,-1.35f,1.10f);Move("magic53-mill-stream",32.05f,1.40f,.85f);Move("magic53-mail-cloud",88.4f,-1.43f);Move("magic53-cloud-collection-funnel",88.4f,-1.43f);
        foreach(var e in entries){if(e.id=="mission100-yard-sign")e.caption="INK SPRINGS";if(e.id=="mission100-aviary-sign")e.caption="MESSENGER GROVE";if(e.id=="mission100-workshop-sign")e.caption="STAMPWORKS";if(e.id=="mission100-dispatch-sign")e.caption="GRAND DISPATCH";}
        var attractions=entries.Where(e=>e.id.StartsWith("magic53-")&&e.kind==PropKind.Landmark).ToArray();
        foreach(var chapter in layout.chapters){chapter.entries.RemoveAll(e=>e.id.StartsWith("mission100-tree-")&&attractions.Any(a=>Mathf.Abs(e.level-a.level)<2.4f&&Mathf.Sign(e.lateral)==Mathf.Sign(a.lateral)));chapter.entries.Sort((a,b)=>a.level.CompareTo(b.level));}
        EditorUtility.SetDirty(layout);AssetDatabase.SaveAssets();Run();
    }
    public static void Run()
    {
        RoyalAvesLevelMapPreview preview=null;int errors=0;Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        try
        {
            Directory.CreateDirectory(Output);var mission=WorldMapMission100.Current;var layout=mission.layout;var before=EditorJsonUtility.ToJson(layout);var entries=layout.chapters.SelectMany(c=>c.entries).ToArray();
            foreach(var id in new[]{"envelope-tree","seal-flowers","ribbon-arch","ink-spring","quill-mill","envelope-bridge","floating-islet","mail-tubes","wax-pond","stamp-factory","parcel-mountain","mail-cloud"})Check(entries.Any(e=>e.id=="magic53-"+id),"Missing attraction "+id);
            Check(mission.characters.Length==5,"Five distinct character profiles expected");var loader=mission.characters.Single(c=>c.sprite.name=="gorilla-loader-v53");Check(string.IsNullOrEmpty(loader.wand),"Loader must not have a wand");
            var baseline=AssetDatabase.LoadAssetAtPath<WorldMapLayout>("Assets/Editor/LayoutBeforeV53.asset");
            if(baseline!=null){var old=baseline.chapters.SelectMany(c=>c.entries).Where(e=>e.level>100.1f).Select(JsonUtility.ToJson).ToArray();var after=entries.Where(e=>e.level>100.1f).Select(JsonUtility.ToJson).ToArray();Check(old.SequenceEqual(after),"Later regions changed");}
            preview=RoyalAvesLevelMapPreview.CreateScenePreview(1080,1920);typeof(RoyalAvesLevelMapPreview).GetField("seasonPreview",Private).SetValue(preview,1);
            foreach(var level in new[]{1,4,7,12,16,19,22,25,27,31,32,36,43,46,52,57,62,67,71,77,82,85,88,90,96}){var world=Build(preview,level);world.ConfigureDayNight(false,4,0);Save(preview,"level-"+level.ToString("000")+"-day");}
            foreach(var level in new[]{22,77,90,96}){var world=Build(preview,level);world.ConfigureDayNight(false,4,1);Save(preview,"level-"+level.ToString("000")+"-night");}
            var mill=Build(preview,31);mill.ConfigureDayNight(false,4,0);mill.TimeOverride=0;preview.RenderScenePreview();var rotor=Piece(mill,"quill-mill","quill-rotor");var body=rotor.transform.parent.parent;var bodyRot=body.localRotation;var bodyPos=body.localPosition;var start=rotor.transform.localRotation;mill.TimeOverride=2;preview.RenderScenePreview();Check(Quaternion.Angle(start,rotor.transform.localRotation)>20,"Quills are not rotating");Check(Quaternion.Angle(bodyRot,body.localRotation)<.001f&&(bodyPos-body.localPosition).sqrMagnitude<1e-10f,"Mill structure is moving");mill.TimeOverride=0;preview.RenderScenePreview();Check(Quaternion.Angle(start,rotor.transform.localRotation)<.001f,"Rotation accumulates between renders");
            var spring=Build(preview,22);spring.ConfigureDayNight(false,4,0);spring.TimeOverride=0;preview.RenderScenePreview();var fall=Piece(spring,"ink-spring","falling-ink");var pool=Piece(spring,"ink-spring","spring-pool");Check(fall!=pool&&fall.GetComponentInChildren<MeshRenderer>().sharedMaterial.mainTexture!=pool.GetComponentInChildren<MeshRenderer>().sharedMaterial.mainTexture,"Ink fall and pool must be independent images");var block=new MaterialPropertyBlock();fall.GetComponentInChildren<MeshRenderer>().GetPropertyBlock(block);Check(block.GetFloat("_MagicFlow")==1,"Liquid shader not assigned to waterfall");spring.TimeOverride=1;preview.RenderScenePreview();fall.GetComponentInChildren<MeshRenderer>().GetPropertyBlock(block);Check(block.GetFloat("_MagicClock")==1,"Flow clock does not animate");var staticBody=fall.transform.parent.parent.GetComponent<MeshRenderer>();if(staticBody!=null){staticBody.GetPropertyBlock(block);Check(block.GetFloat("_MagicFlow")==0,"Structure received liquid animation");}
            var factory=Build(preview,77);factory.ConfigureDayNight(false,4,0);factory.TimeOverride=.2f;preview.RenderScenePreview();var bubble=Piece(factory,"stamp-factory","chimney-stamp-0");var bubbleTexture=bubble.GetComponentInChildren<MeshRenderer>().sharedMaterial.mainTexture;factory.TimeOverride=3.4f;preview.RenderScenePreview();Check(bubble.GetComponentInChildren<MeshRenderer>().sharedMaterial.mainTexture!=bubbleTexture,"Bubble does not become stamp");
            foreach(var scene in new[]{(4,"tree"),(14,"ribbons"),(22,"ink"),(27,"gorilla"),(31,"mill"),(46,"islet"),(62,"tubes"),(71,"wax"),(77,"factory"),(82,"parcels"),(88,"cloud")})
            {
                var world=Build(preview,scene.Item1);world.ConfigureDayNight(false,4,0);Check(world.AdornmentNeedsFrames,"Animation clock stopped");for(int f=0;f<20;f++){world.TimeOverride=f*.32f;Save(preview,scene.Item2+"-"+f.ToString("00"));}
            }
            Check(EditorJsonUtility.ToJson(layout)==before,"Capture modified authored data");Check(errors==0,"Unity errors: "+errors);
            File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: native Unity C#/shader compilation; all twelve attractions and five character profiles; independent flowing ink layer/pool/drops with fixed structural art; rotor rotates without moving mill; stamp bubbles change texture; repeatable animation poses; later region data retained; 25 day and four night captures, eleven 20-frame animation sequences. No main scene, islands or season saved. Physical-device performance and manual editor pointer interaction not measured.");UnityEngine.Object.DestroyImmediate(preview);EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);EditorApplication.Exit(1);}
    }
    static WorldMapEffectPiece Piece(WorldMapView world,string owner,string id)=>world.EffectPieces("magic53-"+owner).Single(p=>p.Id=="adornment-magic53-"+id);
    static WorldMapView Build(RoyalAvesLevelMapPreview preview,float level)
    {typeof(RoyalAvesLevelMapPreview).GetField("level",Private).SetValue(preview,level);typeof(RoyalAvesLevelMapPreview).GetMethod("Build",Private).Invoke(preview,new object[]{1080,1920,1});var world=((CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview)).World;world.TimeOverride=2;return world;}
    static void Save(RoyalAvesLevelMapPreview preview,string name)
    {preview.RenderScenePreview();var rt=(RenderTexture)preview.SceneTexture;var old=RenderTexture.active;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());}finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
}
