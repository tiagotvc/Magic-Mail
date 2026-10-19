using System;
using System.IO;
using System.Collections.Generic;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class MailboxV45Validation
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/mailbox-v45";
    public static void Run()
    {
        WorldMapMailbox mailbox=null;var materials=new List<Material>();GameObject root=null,cameraGo=null;RenderTexture rt=null;
        int errors=0;Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        try
        {
            Directory.CreateDirectory(Output);var kit=Resources.Load<WorldMapKit>("RoyalAvesWorldMapKit");var profile=Resources.Load<WorldMapMailboxRig>("WorldMapMailboxAnimation");
            Check(profile!=null&&WorldMapMailbox.Supports(profile.source),"Rig profile missing");
            root=new GameObject("MailboxFixture");root.transform.position=new Vector3(0,-3000,0);
            mailbox=new WorldMapMailbox(root.transform,30,texture=>{var m=new Material(kit.card);m.SetTexture("_MainTex",texture);m.SetFloat("_GroundedFoot",0);materials.Add(m);return m;});
            cameraGo=new GameObject("FixtureCamera",typeof(Camera));var cam=cameraGo.GetComponent<Camera>();
            cameraGo.transform.position=root.transform.position+new Vector3(0,.53f,-3);cam.orthographic=true;cam.orthographicSize=.76f;cam.nearClipPlane=.1f;cam.farClipPlane=10;cam.cullingMask=1<<30;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.25f,.58f,.30f);
            cameraGo.AddComponent<UniversalAdditionalCameraData>().SetRenderer(kit.rendererIndex);
            rt=new RenderTexture(768,768,24);rt.Create();cam.targetTexture=rt;
            mailbox.Tick(1,0,0);var body=root.transform.Find("CaixaCorreioAnimada/CorpoEPosteFixos");var flag=root.transform.Find("CaixaCorreioAnimada/Bandeirinha");var door=root.transform.Find("CaixaCorreioAnimada/Portinha");
            Vector3 bodyPosition=body.position;Quaternion bodyRotation=body.rotation;var fixedVertices=body.GetComponent<MeshFilter>().sharedMesh.vertices;
            Save(cam,rt,"closed");mailbox.Tick(1,.65f,0);Check(Quaternion.Angle(flag.localRotation,Quaternion.identity)>50,"Flag does not rotate");Check(Quaternion.Angle(door.localRotation,Quaternion.identity)<.001f,"Door moved during flag-only interval");Save(cam,rt,"flag");
            mailbox.Tick(1,3.2f,0);Check(Quaternion.Angle(flag.localRotation,Quaternion.identity)<.001f,"Flag did not return");Check(Quaternion.Angle(door.localRotation,Quaternion.identity)>95,"Door does not open");Save(cam,rt,"open");
            Check(body.position==bodyPosition&&Quaternion.Angle(body.rotation,bodyRotation)<.001f,"Body/support moved");
            var current=body.GetComponent<MeshFilter>().sharedMesh.vertices;Check(current.Length==fixedVertices.Length,"Body geometry changed");for(int i=0;i<current.Length;i++)Check(current[i]==fixedVertices[i],"Body geometry animated");
            mailbox.Tick(1,6,0);Check(Quaternion.Angle(door.localRotation,Quaternion.identity)<.001f&&Quaternion.Angle(flag.localRotation,Quaternion.identity)<.001f,"Hinges did not close");
            Check(errors==0,"Unity logged errors");File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: native Unity compilation and production shader renders. Flag and door rotate independently about hinges; body and post transform/vertices stay fixed; both parts return to rest. Profile resolves approved source and backing. Scene/layout/season not saved. Manual editor interaction and device performance not measured.");
            mailbox.Dispose();mailbox=null;foreach(var m in materials)UnityEngine.Object.DestroyImmediate(m);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraGo);rt.Release();UnityEngine.Object.DestroyImmediate(rt);EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Check(bool test,string message){if(!test)throw new Exception(message);}
    static void Save(Camera camera,RenderTexture rt,string name)
    {
        camera.Render();var previous=RenderTexture.active;var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),texture.EncodeToPNG());}
        finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(texture);}
    }
}
