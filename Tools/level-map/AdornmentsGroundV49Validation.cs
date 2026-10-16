using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

public static class AdornmentsGroundV49Validation
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/adornments-ground-v49";
    public static void Run()
    {
        RoyalAvesLevelMapPreview preview=null;WorldMapLayout layout=null;WorldMapIslands islands=null;string oldLayout=null,oldIslands=null;int errors=0;
        Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        try
        {
            Directory.CreateDirectory(Output);layout=Resources.Load<WorldMapLayout>(WorldMapLayout.SimpleResourcePath);islands=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);
            oldLayout=EditorJsonUtility.ToJson(layout);oldIslands=EditorJsonUtility.ToJson(islands);var kit=Resources.Load<WorldMapKit>("RoyalAvesWorldMapKit");
            var chapter=layout.Find(0);if(chapter==null){chapter=new WorldMapLayout.Chapter{index=0};layout.chapters.Add(chapter);}chapter.manual=true;chapter.entries.Clear();
            var letter=WorldMapAdornment.Create(kit.envelopeCream,"Carta de teste");letter.id="adornment-proof-letter";letter.size=.16f;
            var lamp=WorldMapAdornment.Create(kit.christmasBulbs[0],"Lâmpada de teste",true);lamp.id="adornment-proof-lamp";lamp.blink=true;
            var tree=new WorldMapLayout.Entry{id="adorned-tree-proof",keepSprite=true,customSize=true,height=1.25f,kind=PropKind.Tree,level=4,lateral=1.45f,flipX=true,customAdornments=true,
                sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/Arvores/green-rounded-tree-v27.png"),adornments=new[]{letter,lamp},
                effectPieces=new[]{new WorldMapPiecePose{id=letter.id,pose=new WorldMapEffectPose{offset=new Vector2(.19f,-.12f),rotation=18,scale=new Vector2(1.1f,.85f)}},new WorldMapPiecePose{id=lamp.id,pose=new WorldMapEffectPose{offset=new Vector2(-.18f,-.02f),rotation=180}}}};
            Check(tree.sprite!=null,"Missing tree fixture");chapter.entries.Add(tree);
            var post=Resources.Load<WorldMapLibraryItem>("MapLibrary/Props/lamp-straight-glossy-v46");chapter.entries.Add(new WorldMapLayout.Entry{id="post-proof",keepSprite=true,customSize=true,height=1.14f,kind=PropKind.Lamp,sprite=post.sprite,level=2,lateral=-1.45f});
            islands.islands[0].groundStyle=WorldMapGroundStyle.Green;islands.islands[0].customColors=false;islands.islands[0].customPathColor=false;
            preview=RoyalAvesLevelMapPreview.CreateScenePreview(1080,1920);typeof(RoyalAvesLevelMapPreview).GetField("seasonPreview",Private).SetValue(preview,0);
            var world=Build(preview,1);world.ConfigureDayNight(false,4,0);Save(preview,"island-start-day");
            Check(world.AdornmentNeedsFrames,"Blinking adornment does not keep preview animating");
            var pieces=world.EffectPieces(tree.id);var letterPiece=pieces.First(p=>p.Id==letter.id);var lampPiece=pieces.First(p=>p.Id==lamp.id);
            Check(pieces.Length==2,"Custom adornments not independent");Check(letterPiece.transform.parent.parent.localScale.x<0,"Adornments lost owner mirroring");
            var pos=letterPiece.transform.localPosition;for(int i=0;i<8;i++)preview.RenderScenePreview();Check((letterPiece.transform.localPosition-pos).sqrMagnitude<.000001f,"Adornment transform drifts");
            var prop=world.Props.First(p=>p.id==tree.id);var copy=WorldMapLayout.Entry.From(prop,world.Route);Check(!ReferenceEquals(copy.adornments,prop.adornments),"Adornments not copied independently");
            copy.adornments[0].size=.3f;Check(prop.adornments[0].size==letter.size,"Adornment copies share mutable data");
            var fixture=UnityEngine.Object.Instantiate(layout);AssetDatabase.CreateAsset(fixture,"Assets/AdornmentsV49Roundtrip.asset");EditorUtility.SetDirty(fixture);AssetDatabase.SaveAssets();AssetDatabase.ImportAsset("Assets/AdornmentsV49Roundtrip.asset",ImportAssetOptions.ForceUpdate);
            var saved=AssetDatabase.LoadAssetAtPath<WorldMapLayout>("Assets/AdornmentsV49Roundtrip.asset").Find(0).entries.First(e=>e.id==tree.id);
            Check(saved.customAdornments&&saved.adornments.Length==2&&saved.adornments[1].blink&&saved.effectPieces[0].pose.rotation==18,"Adornment save/reimport failed");AssetDatabase.DeleteAsset("Assets/AdornmentsV49Roundtrip.asset");
            world.ConfigureDayNight(false,4,1);world.TimeOverride=3;Save(preview,"adornments-night");var block=new MaterialPropertyBlock();lampPiece.GetComponentInChildren<MeshRenderer>().GetPropertyBlock(block);float emission=block.GetFloat("_Emission");world.TimeOverride=4;preview.RenderScenePreview();lampPiece.GetComponentInChildren<MeshRenderer>().GetPropertyBlock(block);Check(Mathf.Abs(block.GetFloat("_Emission")-emission)>.01f,"Adornment lamp is not blinking");
            world=Build(preview,17);world.ConfigureDayNight(false,4,0);Save(preview,"island-end-day");
            var material=((Material[])typeof(WorldMapView).GetField("islandMaterials",Private).GetValue(world))[0];Check(material.GetFloat("_GreenIslandGradient")==1,"Island grass gradient missing");
            var span=material.GetVector("_IslandSpan");Check(span.y>span.x+10,"Island gradient is not bound to island length");
            islands.islands[0].customPathColor=true;islands.islands[0].path=new Color32(255,180,38,255);
            world=Build(preview,1);world.ConfigureDayNight(false,4,0);Save(preview,"independent-gold-road");
            material=((Material[])typeof(WorldMapView).GetField("islandMaterials",Private).GetValue(world))[0];Check(material.GetFloat("_IndependentPathColor")==1&&material.GetFloat("_GreenIslandGradient")==1,"Independent road color changed grass mode");
            Check(errors==0,"Unity logged errors");File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: native Unity runtime/editor C# and shaders; two independent adornments, pose stability, owner mirroring, independent array copies, asset save/reimport, blinking lamp, island-length grass gradient, independent gold road with grass preserved. Native day/night and island-start/end captures. Main source scene/layout/islands/season were not saved. Manual pointer interaction and physical-device performance not measured.");
            EditorJsonUtility.FromJsonOverwrite(oldLayout,layout);EditorJsonUtility.FromJsonOverwrite(oldIslands,islands);UnityEngine.Object.DestroyImmediate(preview);EditorApplication.Exit(0);
        }
        catch(Exception e){if(layout!=null&&oldLayout!=null)EditorJsonUtility.FromJsonOverwrite(oldLayout,layout);if(islands!=null&&oldIslands!=null)EditorJsonUtility.FromJsonOverwrite(oldIslands,islands);Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static WorldMapView Build(RoyalAvesLevelMapPreview preview,float level)
    {typeof(RoyalAvesLevelMapPreview).GetField("level",Private).SetValue(preview,level);typeof(RoyalAvesLevelMapPreview).GetMethod("Build",Private).Invoke(preview,new object[]{1080,1920,1});var world=((CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview)).World;world.TimeOverride=2;return world;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Save(RoyalAvesLevelMapPreview preview,string name)
    {preview.RenderScenePreview();var rt=(RenderTexture)preview.SceneTexture;var old=RenderTexture.active;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());}finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}}
}
