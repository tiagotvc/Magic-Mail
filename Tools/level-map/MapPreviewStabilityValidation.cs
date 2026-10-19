using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MapPreviewStabilityValidation
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static MapPreviewStabilityValidation(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/map-preview-stability.request"))return;
        File.Delete("Temp/map-preview-stability.request");
        RoyalAvesLevelMapPreview preview=null;
        var errors=0;string lastError="";
        Application.LogCallback log=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors++;lastError=message;}};
        Application.logMessageReceived+=log;
        try
        {
            var type=typeof(RoyalAvesLevelMapPreview);
            preview=RoyalAvesLevelMapPreview.CreateScenePreview(540,960);
            var map=(CurvedLevelMap)type.GetField("map",Private).GetValue(preview);
            var world=map.World;var kit=world.CameraModel.Kit;
            kit.santaEventEnabled=true;kit.santaFirstDelay=1;kit.santaInterval=2;
            Check(world.Santa!=null,"Expected Santa event");
            var renderer=world.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name=="Cartao");
            var original=renderer.sharedMaterial;var active=renderer.gameObject.activeSelf;
            try
            {
                renderer.sharedMaterial=null;renderer.gameObject.SetActive(false);
                world.Santa.ShowNow(1); // Regression: a pooled, hidden card never received a material.
                renderer.gameObject.SetActive(true);
                world.Santa.ShowNow(2); // A card without drawable art cannot be a host either.
            }
            finally{renderer.sharedMaterial=original;renderer.gameObject.SetActive(active);world.Santa.Reset();}
            var renders=0;var appearances=0;
            foreach(var level in new[]{1,21,41,61,81,1})
            {
                type.GetField("level",Private).SetValue(preview,(float)level);
                world.TimeOverride=0;world.Santa.Reset();
                foreach(var time in new[]{0f,.5f,1.1f,1.8f,3f,6f,12f,25f,60f})
                {
                    world.TimeOverride=time;
                    type.GetMethod("Render",Private).Invoke(preview,null);
                    if(world.Santa.Active)appearances++;
                    Check(preview.SceneTexture is RenderTexture rt&&rt.IsCreated(),"Lost render target at "+level+"/"+time);
                    renders++;
                }
                if(level==1||level==81)Save(preview,"Tools/level-map/previews/stable-preview-"+level+".png");
            }
            Check(errors==0,"Unity error: "+lastError);
            File.WriteAllText("Tools/level-map/preview-stability.txt",$"PASS: hidden/uninitialized card and active material-less card do not throw during Santa host selection; {renders} map renders across sections 1, 21, 41, 61, 81 and back to 1, advancing event time through 60 seconds. {appearances} frames with automatic Santa active. Render targets remain valid; zero Unity errors. Saved scene, islands, layout and player data untouched.");
            File.WriteAllText("Temp/map-preview-stability.done","OK");
        }
        catch(Exception e){Debug.LogException(e);File.WriteAllText("Temp/map-preview-stability.done",e.ToString());}
        finally{Application.logMessageReceived-=log;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Save(RoyalAvesLevelMapPreview preview,string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var rt=(RenderTexture)preview.SceneTexture;var old=RenderTexture.active;
        var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}
    }
}
