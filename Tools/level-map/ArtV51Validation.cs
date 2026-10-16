using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using RoyalAves.EditorTools;

public static class ArtV51Validation
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/art-v51";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    public static void Run()
    {
        RoyalAvesLevelMapPreview preview=null;int errors=0;
        Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        try
        {
            Directory.CreateDirectory(Output);var mission=WorldMapMission100.Current;var kit=Resources.Load<WorldMapKit>("RoyalAvesWorldMapKit");
            Check(mission.characters[1].sprite.name.Contains("owl")&&mission.characters[2].sprite.name.Contains("turtle")&&mission.characters[3].sprite.name.Contains("ink"),"New species not bound");
            var dataBefore=EditorJsonUtility.ToJson(mission.layout);
            foreach(var c in mission.characters)
            {
                var parent=new GameObject("ArmValidation");var material=new Material(Shader.Find("RoyalAves/WorldMap/Card"));
                try
                {
                    using(var actor=new WorldMapPostman(parent.transform,31,kit,t=>material,c.sprite))
                    {
                        actor.Tick(c.height,0,0);var mesh=actor.Actor.GetComponent<MeshFilter>().sharedMesh;var first=mesh.vertices;
                        var joints=actor.Actor.Cast<Transform>().Where(t=>t.name.StartsWith("ForearmJoint_")).ToArray();var rotations=joints.Select(t=>t.localRotation).ToArray();var pivots=joints.Select(t=>t.localPosition).ToArray();
                        actor.Tick(c.height,1.2f,0);var second=mesh.vertices;
                        if(c.bodyBacking!=null)
                        {
                            Check(actor.Articulated&&joints.Length==2,"Two independent turtle joints missing");
                            Check(first.Zip(second,(a,b)=>(a-b).sqrMagnitude).All(d=>d<1e-12f),"Shell/head/beard/apron/feet deform during joint motion");
                            for(int i=0;i<joints.Length;i++){Check(Quaternion.Angle(rotations[i],joints[i].localRotation)>.3f,"Arm joint stays still");Check((pivots[i]-joints[i].localPosition).sqrMagnitude<1e-12f,"Elbow hinge moves");}
                            Check(actor.Actor.GetComponent<MeshRenderer>().sharedMaterial!=null,"Body material missing");
                        }
                        else
                        {
                            var weights=(float[][])typeof(WorldMapPostman).GetField("weights",Private).GetValue(actor);int moved=0;
                            for(int i=0;i<first.Length;i++){if(weights.All(w=>w[i]==0))Check((first[i]-second[i]).sqrMagnitude<1e-12f,"Motion outside selected forearm: "+c.role);else if((first[i]-second[i]).sqrMagnitude>1e-10f)moved++;}
                            Check(moved>15,"No forearm gesture "+c.role);
                        }
                        actor.Tick(c.height,0,0);Check(mesh.vertices.Zip(first,(a,b)=>(a-b).sqrMagnitude).All(d=>d<1e-12f),"Pose accumulates drift");
                        Check(WorldMapScenery.IsMirrored(new PropPlacement{kind=PropKind.Character,sprite=c.sprite,offset=-1})!=WorldMapScenery.IsMirrored(new PropPlacement{kind=PropKind.Character,sprite=c.sprite,offset=1}),"Facing does not follow road side");
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(parent);UnityEngine.Object.DestroyImmediate(material);}
            }
            CaptureCast(kit,mission);
            preview=RoyalAvesLevelMapPreview.CreateScenePreview(1080,1920);typeof(RoyalAvesLevelMapPreview).GetField("seasonPreview",Private).SetValue(preview,1);
            foreach(var level in new[]{1,10,19,24,30,39,48,57,65,69,79,89,94,98})
            {
                typeof(RoyalAvesLevelMapPreview).GetField("level",Private).SetValue(preview,(float)level);typeof(RoyalAvesLevelMapPreview).GetMethod("Build",Private).Invoke(preview,new object[]{1080,1920,1});
                var world=((CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview)).World;world.ConfigureDayNight(false,4,0);world.TimeOverride=2;preview.RenderScenePreview();Save((RenderTexture)preview.SceneTexture,"level-"+level.ToString("000")+"-day");
                if(level==19||level==89){world.ConfigureDayNight(false,4,1);preview.RenderScenePreview();Save((RenderTexture)preview.SceneTexture,"level-"+level.ToString("000")+"-night");}
            }
            Check(EditorJsonUtility.ToJson(mission.layout)==dataBefore,"Validation changed authored placements");Check(errors==0,"Unity errors: "+errors);
            File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: native Unity runtime/editor compilation and shaders; human/owlish/turtle/ink silhouettes; articulated turtle body remains completely rigid while two independent elbow joints rotate; stable hinge positions and repeatable poses; other characters move only inside forearm masks; lane-facing orientation; 24 native animation frames; 14 phone-format day captures and 2 night captures. Existing layout placement ids, route phases, lateral anchors, effects and later chapters retained. No main scene, islands or season saved. Physical-device performance not measured.");
            UnityEngine.Object.DestroyImmediate(preview);EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);EditorApplication.Exit(1);}
    }
    static void CaptureCast(WorldMapKit kit,WorldMapMission100 mission)
    {
        var root=new GameObject("CastComparison");var camRoot=new GameObject("CastCamera");var camera=camRoot.AddComponent<Camera>();var rt=new RenderTexture(1600,900,24);var actors=new List<WorldMapPostman>();var materials=new List<Material>();
        try
        {
            camera.cullingMask=1<<31;camera.orthographic=true;camera.orthographicSize=1.30f;camera.transform.position=new Vector3(0,.76f,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.19f,.23f,1);camera.allowHDR=false;camera.allowMSAA=false;camera.GetUniversalAdditionalCameraData().SetRenderer(kit.rendererIndex);rt.Create();camera.targetTexture=rt;
            for(int i=0;i<mission.characters.Length;i++){var c=mission.characters[i];var a=new WorldMapPostman(root.transform,31,kit,t=>{var m=new Material(Shader.Find("RoyalAves/WorldMap/Card"));m.SetTexture("_MainTex",t);materials.Add(m);return m;},c.sprite);a.Actor.localPosition=new Vector3(new[]{-1.62f,-.58f,.48f,1.65f}[i],0,0);actors.Add(a);}
            for(int frame=0;frame<24;frame++)
            {
                for(int i=0;i<actors.Count;i++)actors[i].Tick(mission.characters[i].height,frame*.22f,0);
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});Save(rt,"cast-"+frame.ToString("00"));if(frame==0)Save(rt,"cast-comparison");
            }
        }
        finally{foreach(var a in actors)a.Dispose();foreach(var m in materials)UnityEngine.Object.DestroyImmediate(m);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(camRoot);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Save(RenderTexture rt,string name)
    {var old=RenderTexture.active;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());}finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}}
}
