using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RoyalAves.EditorTools;

public static class PostmanRigValidationBatch
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/postman-v8";
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void Execute()
    {
        Directory.CreateDirectory(Output);
        RoyalAvesPostmanRigBuilder.Build();
        string folder=RoyalAvesPostmanRigBuilder.Folder;
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/PostmanV8.prefab");
        Check(prefab!=null,"Prefab was not saved");
        var actor=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var cameraObject=new GameObject("PostmanPreviewCamera");
        var camera=cameraObject.AddComponent<Camera>();
        var target=new RenderTexture(540,810,24,RenderTextureFormat.ARGB32);
        try
        {
            actor.GetComponent<Animator>().enabled=false;
            var parts=actor.GetComponentsInChildren<SpriteRenderer>();
            Check(parts.Length==18&&parts.All(x=>x.sprite!=null&&x.sharedMaterial!=null),"Missing part or material");
            Check(parts.All(x=>x.sprite.rect.width>20&&x.sprite.rect.height>20),"Invalid crop");
            camera.transform.position=new Vector3(.55f,3.55f,-10);
            camera.orthographic=true;camera.orthographicSize=4.55f;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.05f,.10f,.16f,0);
            camera.allowHDR=false;camera.allowMSAA=false;
            camera.GetUniversalAdditionalCameraData().SetRenderer(0);
            target.Create();camera.targetTexture=target;
            foreach(var mode in new[]{"Idle","Wave","Walk"})
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"/"+mode+".anim");
                Check(clip!=null&&clip.length>1,"Missing motion "+mode);
                var elbow=actor.transform.Find("Hips/Body/RightArm/RightForearm");
                var hips=actor.transform.Find("Hips");
                clip.SampleAnimation(actor,0);var start=hips.localPosition;
                var initial=elbow.localRotation;
                clip.SampleAnimation(actor,clip.length*.11f);var moved=elbow.localRotation;
                if(mode=="Wave")Check(Quaternion.Angle(initial,moved)>1,"Wave elbow stays still");
                clip.SampleAnimation(actor,clip.length);
                Check(Vector3.Distance(start,hips.localPosition)<.0001f,"Loop position seam "+mode);
                for(int i=0;i<16;i++)
                {
                    clip.SampleAnimation(actor,i*clip.length/16);
                    Capture(camera,target,Path.Combine(Output,mode.ToLowerInvariant()+"-"+i.ToString("00")+".png"));
                }
            }
            var controller=(AnimatorController)actor.GetComponent<Animator>().runtimeAnimatorController;
            Check(controller.layers[0].stateMachine.states.Length==3,"Three states missing");
            Check(controller.parameters.Any(x=>x.name=="Motion"),"Motion selector missing");
            File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: saved prefab;18 sprites and complete material references;3 looping clips;Wave elbow rotates;loop position continuity;Motion states;48 native Unity frames. Cutout transform rig, no mesh skinning or world locomotion. Visual review required for extreme poses.");
        }
        finally{camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(cameraObject);target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
    static void Capture(Camera camera,RenderTexture target,string file)
    {
        var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
        RenderPipeline.SubmitRenderRequest(camera,request);
        var previous=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
        try
        {
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
            var pixels=image.GetPixels32();int visible=pixels.Count(x=>x.a>200);
            Check(visible>5000,"Empty native render: "+file);
            File.WriteAllBytes(file,image.EncodeToPNG());
        }
        finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}
    }
}
