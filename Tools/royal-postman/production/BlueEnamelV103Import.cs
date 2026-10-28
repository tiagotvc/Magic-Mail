using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BlueEnamelV103Import
{
    [Serializable] public class Result {public string name,guid;public float width,height;}
    [Serializable] public class Results {public Result[] sprites;}
    const string Art="Assets/BlueEnamelV103/";
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static Result Import(string name,bool crop)
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(Art+name+".png");importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=8192;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;importer.isReadable=true;var platform=importer.GetDefaultPlatformTextureSettings();platform.maxTextureSize=8192;platform.textureCompression=TextureImporterCompression.Uncompressed;importer.SetPlatformTextureSettings(platform);importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+name+".png");var pixels=texture.GetPixels32();int left=texture.width,bottom=texture.height,right=-1,top=-1;
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++){var alpha=pixels[y*texture.width+x].a;if(!crop)Check(alpha==255,"Background transparency");if(alpha<32)continue;left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
        Check(right>left&&top>bottom,"Empty texture");
        if(crop)Check(left>0&&bottom>0&&right<texture.width-1&&top<texture.height-1,"Title touches PNG edge");
        else{var reflection=texture.GetPixelBilinear(.1f,.65f);var center=texture.GetPixelBilinear(.5f,.65f);Check(reflection.grayscale>center.grayscale+.04f&&center.b>center.r*1.5f,"Reflection/navy contrast missing");}
        var rect=crop?Rect.MinMaxRect(Math.Max(0,left-3),Math.Max(0,bottom-3),Math.Min(texture.width,right+4),Math.Min(texture.height,top+4)):new Rect(0,0,texture.width,texture.height);
        var sprite=Sprite.Create(texture,rect,Vector2.one*.5f,100,0,SpriteMeshType.FullRect);sprite.name=name=="title-pt"?"Incubadora":name=="title-en"?"Incubator":"Blue enamel background";var path=Art+"Sprites/"+name+".asset";AssetDatabase.CreateAsset(sprite,path);
        importer.isReadable=false;importer.SaveAndReimport();AssetDatabase.SaveAssets();var saved=AssetDatabase.LoadAssetAtPath<Sprite>(path);Check(saved!=null&&saved.texture!=null&&saved.rect==rect,"Native sprite serialization failed");
        return new Result{name=name,guid=AssetDatabase.AssetPathToGUID(path),width=rect.width,height=rect.height};
    }
    public static void Run()
    {
        const string output="../blue-enamel-v103";
        try{Directory.CreateDirectory(Art+"Sprites");AssetDatabase.Refresh();var data=new Results{sprites=new[]{Import("background",false),Import("title-pt",true),Import("title-en",true)}};File.WriteAllText(output+"/import.json",JsonUtility.ToJson(data,true));File.WriteAllText(output+"/review.txt","PASS: Unity native import/serialization of three original PNGs; opaque blue background, reflection/navy contrast; transparent title margins and native alpha-bounds sprites; textures retain dimensions, lossless compression, mipmaps and trilinear filtering.\n");EditorApplication.Exit(0);}
        catch(Exception e){File.WriteAllText(output+"/review.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
