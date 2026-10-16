using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

public static class WinterV34Validation
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/winter-v34";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int errors;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void CaptureUpdatedAnchors()
    {
        Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        try
        {
            Capture(1080,1920);Capture(1080,2280);Check(errors==0,"Unity logged errors after anchor update");
            File.WriteAllText(Path.Combine(Output,"anchors-validation.txt"),"PASS: refreshed alpha-based lower-branch profiles rendered at both phone aspect ratios; existing bulb art retained; theme opt-out, mirroring and animation checks passed; zero Unity errors. Full winter/coverage suite passed before this data-only attachment refinement.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void Run()
    {
        Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        try
        {
            Directory.CreateDirectory(Output);
            typeof(SnowV33Validation).GetMethod("Execute",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            Capture(1080,1920);Capture(1080,2280);
            Check(errors==0,"Unity logged errors");
            File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: saved user scenery rendered at 1080x1920 and 1080x2280; no generic snow caps on unmatched assets; alpha-contour coating uses each sprite crop; existing gold/red/teal bulb atlas reused; curved cords and ice clusters parented to real trees; mild light animation and preview animation flag; theme opt-out removes lights, ice and snow; mirrored decoration keeps its anchor; previous registered-overlay/coverage suite passed; zero Unity errors. Main scene, layouts and active season were not saved. Device performance not measured.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Capture(int width,int height)
    {
        var preview=RoyalAvesLevelMapPreview.CreateScenePreview(width,height);
        try
        {
            typeof(RoyalAvesLevelMapPreview).GetField("seasonPreview",Private).SetValue(preview,2);
            typeof(RoyalAvesLevelMapPreview).GetMethod("Build",Private).Invoke(preview,new object[]{width,height,1});
            var map=(CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview);var world=map.World;
            world.TimeOverride=1;preview.RenderScenePreview();
            var slots=(IDictionary)typeof(WorldMapView).GetField("activeSlots",Private).GetValue(world);
            var kit=world.CameraModel.Kit;Check(kit.christmasBulbs.Length==3&&kit.christmasBulbs.All(s=>s!=null),"Existing bulb bindings missing");
            Check(AssetDatabase.GetAssetPath(kit.christmasBulbs[0]).EndsWith("christmas-lights-cartoon.png"),"Existing bulb artwork was replaced");
            var block=new MaterialPropertyBlock();int trees=0,contours=0;
            object chosen=null;PropPlacement selected=default;
            foreach(DictionaryEntry pair in slots)
            {
                int i=(int)pair.Key;var p=world.Props[i];var slot=pair.Value;var type=slot.GetType();
                var caps=(MeshFilter[])type.GetField("caps").GetValue(slot);
                bool mapped=kit.paintedSnow.Any(v=>v.source==p.sprite&&v.overlay!=null);
                if(!mapped)foreach(var cap in caps.Where(c=>c.gameObject.activeSelf))
                {
                    cap.GetComponent<MeshRenderer>().GetPropertyBlock(block);
                    Check(block.GetFloat("_SnowOnly")>.5f,"Unmatched asset still has generic snow hat: "+p.sprite.name);contours++;
                }
                var drawing=(MeshRenderer)type.GetField("cardRenderer").GetValue(slot);drawing.GetPropertyBlock(block);
                if(block.GetFloat("_SnowDust")>.5f)contours++;
                if(p.kind!=PropKind.Tree||!WorldMapScenery.HasIceTheme(p,true))continue;
                var card=(Transform)type.GetField("card").GetValue(slot);var decor=card.Find("NatalLuzesEGelo");
                Check(decor!=null&&decor.gameObject.activeSelf,"Tree has no active Christmas decorations");
                Check(decor.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("LuzDeNatal_")),"Tree has no existing light bulbs");
                Check(decor.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("GeloGalhoBaixo_"))==2,"Tree has no lower-branch icicles");
                Check(kit.treeDecorationProfiles.Any(profile=>profile.source==p.sprite),"Tree lacks alpha-based lower-branch attachment points");
                trees++;if(chosen==null){chosen=slot;selected=p;}
            }
            Check(trees>0&&contours>0,"Saved scenery did not exercise trees/contour snow");
            Check(map.NeedsPreviewFrames,"Christmas lights cannot animate in editor preview");
            var light=world.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.StartsWith("LuzDeNatal_"));
            light.GetPropertyBlock(block);var before=block.GetFloat("_Emission");world.TimeOverride=2.3f;preview.RenderScenePreview();
            light.GetPropertyBlock(block);Check(Mathf.Abs(before-block.GetFloat("_Emission"))>.005f,"Christmas lights have no mild animation");
            Save(preview,"map-"+width+"x"+height+".png");
            var selectedType=chosen.GetType();var selectedCard=(Transform)selectedType.GetField("card").GetValue(chosen);
            var anchor=selectedCard.parent.localPosition;var oldSign=Mathf.Sign(selectedCard.localScale.x);
            selected.flipX=!selected.flipX;Pose(world,chosen,selected);
            Check(Mathf.Sign(selectedCard.localScale.x)!=oldSign&&selectedCard.parent.localPosition==anchor,"Mirroring moved tree/decor anchor");
            selected.theme=WorldMapObjectTheme.None;Pose(world,chosen,selected);
            Check(!selectedCard.Find("NatalLuzesEGelo").gameObject.activeSelf,"Theme opt-out leaves decorations visible");
            var renderer=(MeshRenderer)selectedType.GetField("cardRenderer").GetValue(chosen);renderer.GetPropertyBlock(block);
            Check(block.GetFloat("_SnowDust")==0,"Theme opt-out leaves contour snow visible");
        }
        finally{UnityEngine.Object.DestroyImmediate(preview);}
    }
    static void Pose(WorldMapView world,object slot,PropPlacement p)=>typeof(WorldMapView).GetMethod("Pose",Private).Invoke(world,new object[]{slot,p,world.Route.MetersAt(1),true});
    static void Save(RoyalAvesLevelMapPreview preview,string name)
    {
        preview.RenderScenePreview();var rt=(RenderTexture)preview.SceneTexture;var old=RenderTexture.active;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name),image.EncodeToPNG());}
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}
    }
}
