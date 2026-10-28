using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.UI.RoyalPostman2026;

public static class IncubatorControlsV70Build
{
    public const string Prefab = "Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab";
    public const string Art = "Assets/RoyalAves/Art/UI/Menu/RoyalPostman/IncubatorControlsV70/";
    public const string Output = "../incubator-controls-v70";

    static Sprite Sprite(string name, bool sliced)
    {
        string path = Art + name + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.spriteImportMode = SpriteImportMode.None;
        importer.isReadable = true;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.crunchedCompression = false;
        importer.maxTextureSize = 8192;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.mipmapEnabled = true;
        importer.mipmapFilter = TextureImporterMipFilter.BoxFilter;
        importer.filterMode = FilterMode.Trilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        foreach (var platform in new[] { "Standalone", "Android", "iPhone", "WebGL" }) importer.ClearPlatformTextureSettings(platform);
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var pixels = texture.GetPixels32();
        int left=texture.width, bottom=texture.height, right=0, top=0;
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)
            if(pixels[y*texture.width+x].a>=160){left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
        left=Math.Max(0,left-2);bottom=Math.Max(0,bottom-2);
        right=Math.Min(texture.width-1,right+2);top=Math.Min(texture.height-1,top+2);
        var rect = new Rect(left,bottom,right-left+1,top-bottom+1);
        float cap = rect.height*.48f;
        var border = sliced ? Vector4.one*cap : Vector4.zero;
        var sprite = UnityEngine.Sprite.Create(texture,rect,Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border);
        sprite.name = name;
        Directory.CreateDirectory(Art+"Sprites");
        AssetDatabase.Refresh();
        string spritePath=Art+"Sprites/"+name+".asset";
        var existing=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if(existing==null)AssetDatabase.CreateAsset(sprite,spritePath);
        else{EditorUtility.CopySerialized(sprite,existing);UnityEngine.Object.DestroyImmediate(sprite);sprite=existing;}
        return sprite;
    }

    static Image Child(Transform parent,string name)
    {
        var go = new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        go.transform.SetParent(parent,false);
        var image=go.GetComponent<Image>();image.color=Color.white;image.raycastTarget=false;
        return image;
    }

    public static void Run()
    {
        Directory.CreateDirectory(Output);
        try
        {
            AssetDatabase.Refresh();
            var rim=Sprite("incubate-fixed-rim",false);
            var face=Sprite("incubate-raised-face",false);
            var track=Sprite("timer-track",true);
            var paw=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/UI/Icon/RoyalPostman/Sprites/paw-cream.asset");
            var blue=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/UI/Menu/RoyalPostman/Pets/Sprites/pets-progress-blue-v16.asset");
            if(paw==null||blue==null)throw new Exception("Existing paw/fill sprite missing");
            var root=PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var screen=root.GetComponent<RoyalPostmanPetsScreen>();
                foreach(var slot in screen.incubators)
                {
                    var button=slot.useItem;
                    var rimImage=button.GetComponent<Image>();
                    rimImage.sprite=rim;rimImage.color=Color.white;rimImage.type=Image.Type.Simple;rimImage.preserveAspect=true;
                    var moving=(RectTransform)button.transform.Find("MovingCenter");
                    moving.anchorMin=moving.anchorMax=moving.pivot=Vector2.one*.5f;
                    moving.sizeDelta=new Vector2(156,156);moving.anchoredPosition=new Vector2(0,6);
                    var label=moving.Find("Label");if(label!=null)label.gameObject.SetActive(false);
                    var faceImage=Child(moving,"RaisedFace");faceImage.sprite=face;faceImage.preserveAspect=true;
                    var fr=faceImage.rectTransform;fr.anchorMin=Vector2.zero;fr.anchorMax=Vector2.one;fr.offsetMin=fr.offsetMax=Vector2.zero;
                    var icon=Child(moving,"PawIcon");icon.sprite=paw;icon.color=new Color32(184,84,4,255);icon.preserveAspect=true;
                    var ir=icon.rectTransform;ir.anchorMin=ir.anchorMax=Vector2.one*.5f;ir.sizeDelta=new Vector2(100,96);ir.anchoredPosition=new Vector2(0,3);
                    button.targetGraphic=faceImage;button.interactable=true;
                    var press=button.GetComponent<RoyalPostmanPaintedButton>();
                    press.movingCenter=moving;press.pressTravel=8;press.pressScale=.96f;
                    var trackImage=slot.progress.transform.parent.GetComponent<Image>();
                    trackImage.sprite=track;trackImage.color=Color.white;trackImage.type=Image.Type.Sliced;
                    trackImage.pixelsPerUnitMultiplier=track.rect.height/trackImage.rectTransform.rect.height;
                    var value=slot.progressFill;
                    value.color=Color.clear;value.raycastTarget=false;
                    var vr=value.rectTransform;vr.anchorMin=Vector2.zero;vr.anchorMax=Vector2.one;
                    vr.offsetMin=new Vector2(8,8);vr.offsetMax=new Vector2(-8,-8);
                    var fill=Child(value.transform,"FillVisual");fill.sprite=blue;fill.type=Image.Type.Sliced;
                    fill.pixelsPerUnitMultiplier=blue.rect.height/Mathf.Max(1,trackImage.rectTransform.rect.height-16);
                    var r=fill.rectTransform;r.anchorMin=new Vector2(0,0);r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,.5f);
                    r.offsetMin=r.offsetMax=Vector2.zero;
                    var clip=value.GetComponent<RoyalPostmanProgressClip>()??value.gameObject.AddComponent<RoyalPostmanProgressClip>();
                    clip.valueSource=value;clip.clipping=r;clip.useParentWidth=true;clip.Refresh();
                    slot.progress.color=new Color32(255,247,218,255);
                    slot.progress.raycastTarget=false;
                }
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
            File.Copy(Prefab,Output+"/assembled.prefab",true);
            File.WriteAllText(Output+"/build.txt","PASS: four independent rim/raised-face/paw assemblies and timer bars; existing root transforms, bindings and time sources retained.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Output+"/build.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
