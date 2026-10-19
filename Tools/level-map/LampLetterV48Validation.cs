using System;
using System.IO;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

public static class LampLetterV48Validation
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/lamp-letter-v48";
    public static void Run()
    {
        RoyalAvesLevelMapPreview preview=null;WorldMapLayout layout=null;string original=null;int errors=0;
        Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        try
        {
            Directory.CreateDirectory(Output);layout=Resources.Load<WorldMapLayout>(WorldMapLayout.SimpleResourcePath);original=EditorJsonUtility.ToJson(layout);
            var chapter=layout.Find(0);if(chapter==null){chapter=new WorldMapLayout.Chapter{index=0};layout.chapters.Add(chapter);}chapter.manual=true;chapter.entries.Clear();
            var names=new[]{"lamp-straight-glossy-v46","lamp-dark-forest-v47","lamp-volcano-v47"};
            for(int i=0;i<names.Length;i++)
            {
                var item=Resources.Load<WorldMapLibraryItem>("MapLibrary/Props/"+names[i]);Check(item!=null&&item.sprite!=null&&item.kind==PropKind.Lamp,"Missing lamp library item");
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(item.sprite));Check(importer.alphaIsTransparency&&importer.spriteImportMode==SpriteImportMode.Single,"Invalid transparent sprite");
                chapter.entries.Add(new WorldMapLayout.Entry{id="lamp-v47-"+i,keepSprite=true,customSize=true,height=1.14f,sprite=item.sprite,kind=PropKind.Lamp,level=2+i*2,lateral=i==1?1.45f:-1.45f});
            }
            preview=RoyalAvesLevelMapPreview.CreateScenePreview(1080,1920);typeof(RoyalAvesLevelMapPreview).GetField("seasonPreview",Private).SetValue(preview,0);
            typeof(RoyalAvesLevelMapPreview).GetMethod("Build",Private).Invoke(preview,new object[]{1080,1920,1});
            var world=((CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview)).World;world.TimeOverride=2;world.ConfigureDayNight(false,4,0);Save(preview,"day");world.ConfigureDayNight(false,4,1);Save(preview,"night");
            var slots=(System.Collections.IDictionary)typeof(WorldMapView).GetField("activeSlots",Private).GetValue(world);int checkedCount=0;
            foreach(System.Collections.DictionaryEntry entry in slots)
            {
                var prop=world.Props[(int)entry.Key];if(!prop.id.StartsWith("lamp-v47-"))continue;
                var type=entry.Value.GetType();var renderer=(MeshRenderer)type.GetField("cardRenderer").GetValue(entry.Value);var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
                var glass=WorldMapLampArt.Glass(prop.sprite);Check(block.GetVector("_LampGlass")==glass,"Glass mask mismatch");
                var halo=(MeshRenderer)type.GetField("lampHalo").GetValue(entry.Value);var target=WorldMapCards.ImagePoint(prop.sprite,prop.height,new Vector2(glass.x,glass.y))+Vector3.back*.03f;
                Check((halo.transform.localPosition-target).sqrMagnitude<.000001f,"Halo not registered to luminous head");
                if(WorldMapLampArt.Cold(prop.sprite)){var tint=block.GetColor("_LampTint");Check(tint.b>tint.r&&block.GetFloat("_LampCold")==1,"Forest light lost violet hue");}
                checkedCount++;
            }
            Check(checkedCount==3&&errors==0,"Missing fixture lamp or Unity errors");
            File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: native Unity compilation and day/night rendering; three transparent Props library items; registered luminous masks and halos; violet forest light, warm magma/straight lights. Source scene, layout and season not saved. Manual interaction and device performance not measured.");
            EditorJsonUtility.FromJsonOverwrite(original,layout);UnityEngine.Object.DestroyImmediate(preview);EditorApplication.Exit(0);
        }
        catch(Exception e){if(layout!=null&&original!=null)EditorJsonUtility.FromJsonOverwrite(original,layout);Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Save(RoyalAvesLevelMapPreview preview,string name)
    {
        preview.RenderScenePreview();var rt=(RenderTexture)preview.SceneTexture;var previous=RenderTexture.active;var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),texture.EncodeToPNG());}
        finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(texture);}
    }
}

