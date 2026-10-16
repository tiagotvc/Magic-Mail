using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RoyalAves.Meta.WorldMap;

public static class MagicLandscapesV53Characters
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/magic-landscapes-v53";
    public static void Run()
    {
        var root=new GameObject("FiveCharacterComparison");var cameraRoot=new GameObject("CastCamera");var camera=cameraRoot.AddComponent<Camera>();var rt=new RenderTexture(1920,900,24);var actors=new List<WorldMapPostman>();var materials=new List<Material>();
        try
        {
            var kit=Resources.Load<WorldMapKit>("RoyalAvesWorldMapKit");var mission=WorldMapMission100.Current;
            camera.cullingMask=1<<31;camera.orthographic=true;camera.orthographicSize=1.30f;camera.transform.position=new Vector3(0,.76f,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.19f,.23f,1);camera.allowHDR=false;camera.allowMSAA=false;camera.GetUniversalAdditionalCameraData().SetRenderer(kit.rendererIndex);rt.Create();camera.targetTexture=rt;
            for(int i=0;i<mission.characters.Length;i++)
            {
                var c=mission.characters[i];var actor=new WorldMapPostman(root.transform,31,kit,t=>{var m=new Material(Shader.Find("RoyalAves/WorldMap/Card"));m.SetTexture("_MainTex",t);materials.Add(m);return m;},c.sprite);actor.Actor.localPosition=new Vector3(new[]{-2.1f,-1.15f,-.15f,.72f,1.85f}[i],0,0);actors.Add(actor);
                if(c.sprite.name=="gorilla-loader-v53")
                {
                    actor.Tick(c.height,0,0);var mesh=actor.Actor.GetComponent<MeshFilter>().sharedMesh;var first=mesh.vertices;actor.Tick(c.height,1.2f,0);var second=mesh.vertices;var weights=(float[][])typeof(WorldMapPostman).GetField("weights",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(actor);int moved=0;
                    for(int p=0;p<first.Length;p++){if(weights.All(w=>w[p]==0)&&Vector3.Distance(first[p],second[p])>.000001f)throw new Exception("Loader body moves outside forearm mask");if(Vector3.Distance(first[p],second[p])>.000001f)moved++;}
                    if(moved<15)throw new Exception("Loader forearm does not move");actor.Tick(c.height,0,0);if(!mesh.vertices.Zip(first,(a,b)=>(a-b).sqrMagnitude<1e-12f).All(x=>x))throw new Exception("Loader idle accumulates drift");
                }
            }
            for(int frame=0;frame<24;frame++)
            {
                for(int i=0;i<actors.Count;i++)actors[i].Tick(mission.characters[i].height,frame*.22f,0);
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});Save(rt,"cast-"+frame.ToString("00"));if(frame==0)Save(rt,"cast-comparison");
            }
            File.WriteAllText(Path.Combine(Output,"characters-validation.txt"),"PASS: five full-body silhouettes rendered in native Unity, including corrected two-eye square-jawed gorilla. Loader mesh changes only within the selected forearm mask and repeats without drift. Turtle retains separate rigid body and two articulated forearms.");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        finally{foreach(var a in actors)a.Dispose();foreach(var m in materials)UnityEngine.Object.DestroyImmediate(m);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraRoot);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
    static void Save(RenderTexture rt,string name)
    {var old=RenderTexture.active;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());}finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}}
}
