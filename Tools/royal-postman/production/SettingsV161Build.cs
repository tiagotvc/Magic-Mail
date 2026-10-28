using System;
using System.IO;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.Meta;
using RoyalAves.UI.RoyalPostman2026;

public static class SettingsV161Build
{
    public const string Art="Assets/RoyalAves/Art/UI/Menu/RoyalPostman/SettingsV161/";
    public const string Prefab="Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/SettingsWindowV161.prefab";
    public const string Output="../settings-v161";
    [Serializable] public class AssetInfo { public string name,guid; public int width,height; public float spriteWidth,spriteHeight; public int transparentPixels; }
    [Serializable] public class ImportReport { public AssetInfo[] assets; }
    static TMP_FontAsset bodyFont,titleFont;
    static bool checkAfterBuild;
    public static void BuildAndCheck(){checkAfterBuild=true;Run();}
    static readonly Color Ink=new Color32(91,43,22,255),Ivory=new Color32(255,239,182,255);
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static Sprite Import(string name,List<AssetInfo> report)
    {
        string path=Art+name+".png";var ti=(TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType=TextureImporterType.Default;ti.textureShape=TextureImporterShape.Texture2D;ti.npotScale=TextureImporterNPOTScale.None;ti.maxTextureSize=4096;
        ti.textureCompression=TextureImporterCompression.Uncompressed;ti.alphaIsTransparency=true;ti.isReadable=true;
        ti.mipmapEnabled=true;ti.mipmapFilter=TextureImporterMipFilter.BoxFilter;ti.filterMode=FilterMode.Trilinear;ti.wrapMode=TextureWrapMode.Clamp;
        foreach(var platform in new[]{"Standalone","Android","iPhone","WebGL"})ti.ClearPlatformTextureSettings(platform);
        var defaults=ti.GetDefaultPlatformTextureSettings();defaults.maxTextureSize=4096;defaults.textureCompression=TextureImporterCompression.Uncompressed;ti.SetPlatformTextureSettings(defaults);
        ti.SaveAndReimport();var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);Check(t!=null,name+": not a Texture2D after native import");var pixels=t.GetPixels32();
        Check(t.GetPixel(0,0).a==0&&t.GetPixel(t.width-1,t.height-1).a==0,name+": opaque corners");
        int loX=t.width,loY=t.height,hiX=0,hiY=0,transparent=0;
        for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++){byte a=pixels[y*t.width+x].a;if(a==0)transparent++;if(a<32)continue;loX=Math.Min(loX,x);loY=Math.Min(loY,y);hiX=Math.Max(hiX,x);hiY=Math.Max(hiY,y);}
        Check(transparent>1000&&hiX>loX&&hiY>loY,name+": missing alpha/art");
        Rect rect=name=="frame"?new Rect(0,0,t.width,t.height):Rect.MinMaxRect(Math.Max(0,loX-3),Math.Max(0,loY-3),Math.Min(t.width,hiX+4),Math.Min(t.height,hiY+4));
        var border=name=="button"?new Vector4(235,220,235,220):Vector4.zero;
        var sprite=Sprite.Create(t,rect,Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border);sprite.name=name;
        string sp=Art+"Sprites/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Sprite>(sp);
        if(existing==null)AssetDatabase.CreateAsset(sprite,sp);else{EditorUtility.CopySerialized(sprite,existing);UnityEngine.Object.DestroyImmediate(sprite);EditorUtility.SetDirty(existing);}
        report.Add(new AssetInfo{name=name,guid=AssetDatabase.AssetPathToGUID(sp),width=t.width,height=t.height,spriteWidth=rect.width,spriteHeight=rect.height,transparentPixels=transparent});
        ti.isReadable=false;ti.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(sp);
    }
    static RectTransform Node(Transform parent,string name,Vector2 at,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform)){layer=5};var rt=(RectTransform)go.transform;rt.SetParent(parent,false);
        rt.anchorMin=rt.anchorMax=rt.pivot=Vector2.one*.5f;rt.anchoredPosition=at;rt.sizeDelta=size;return rt;
    }
    static Image Picture(Transform parent,string name,Sprite sprite,Vector2 at,Vector2 size)
    {
        var rt=Node(parent,name,at,size);var im=rt.gameObject.AddComponent<Image>();im.sprite=sprite;im.raycastTarget=false;im.preserveAspect=true;return im;
    }
    static TextMeshProUGUI Text(Transform parent,string name,string value,Vector2 at,Vector2 size,float fontSize,Color color,bool title=false)
    {
        size.y=Mathf.Max(size.y,fontSize*1.8f);
        var rt=Node(parent,name,at,size);var text=rt.gameObject.AddComponent<TextMeshProUGUI>();text.font=title?titleFont:bodyFont;
        text.text=value;text.fontSize=fontSize;text.alignment=TextAlignmentOptions.Center;text.color=color;text.raycastTarget=false;
        text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Overflow;text.margin=Vector4.zero;
        if(title){var shadow=rt.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.19f,.06f,.015f,.85f);shadow.effectDistance=new Vector2(0,-2);}
        return text;
    }
    static Button Hit(Transform parent,string name,Vector2 at,Vector2 size,Image paint=null)
    {
        var rt=Node(parent,name,at,size);var im=rt.gameObject.AddComponent<Image>();im.color=Color.clear;im.raycastTarget=true;
        var b=rt.gameObject.AddComponent<Button>();b.targetGraphic=paint!=null?paint:im;
        var c=b.colors;c.highlightedColor=Color.white;c.pressedColor=new Color(.77f,.82f,.93f);c.fadeDuration=.08f;b.colors=c;
        var nav=b.navigation;nav.mode=Navigation.Mode.None;b.navigation=nav;return b;
    }
    static void Set(SerializedObject so,string name,UnityEngine.Object value)=>so.FindProperty(name).objectReferenceValue=value;
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        try
        {
            Directory.CreateDirectory(Art+"Sprites");Directory.CreateDirectory(Path.GetDirectoryName(Prefab));AssetDatabase.Refresh();
            var report=new List<AssetInfo>();var frame=Import("frame",report);var capsule=Import("button",report);var sound=Import("sound",report);var music=Import("music",report);var close=Import("close",report);
            bodyFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/RoyalAves/Fonts/BreeSerif SDF.asset");titleFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/RoyalAves/Fonts/Calistoga SDF.asset");Check(bodyFont!=null&&titleFont!=null,"Missing serif fonts");
            var root=new GameObject("TelaConfiguracoes",typeof(RectTransform)){layer=5};root.SetActive(false);
            try
            {
                var rr=(RectTransform)root.transform;rr.anchorMin=Vector2.zero;rr.anchorMax=Vector2.one;rr.sizeDelta=Vector2.zero;
                var dim=root.AddComponent<Image>();dim.color=new Color(.018f,.018f,.065f,.88f);dim.raycastTarget=true;
                var controller=root.AddComponent<SettingsScreen>();var so=new SerializedObject(controller);
                float width=1000,height=width*frame.rect.height/frame.rect.width;
                var panel=Node(rr,"Janela",Vector2.zero,new Vector2(width,height));var fit=root.AddComponent<RoyalPostmanSettingsFit>();fit.panel=panel;fit.designSize=panel.sizeDelta;
                Picture(panel,"Moldura",frame,Vector2.zero,panel.sizeDelta);
                Vector2 At(float x,float y)=>new Vector2((x-.5f)*width,(.5f-y)*height);
                Text(panel,"Titulo","Configurações",At(.5f,.157f),new Vector2(760,62),59,Ivory,true);
                var xPaint=Picture(panel,"FecharArte",close,At(.945f,.185f),Vector2.one*99);
                var xButton=Hit(panel,"Fechar",xPaint.rectTransform.anchoredPosition,Vector2.one*144,xPaint);Set(so,"closeButton",xButton);
                foreach(bool isSound in new[]{true,false})
                {
                    string name=isSound?"Som":"Musica",field=isSound?"sound":"music";float x=isSound?.30f:.70f;
                    var paint=Picture(panel,name+"Arte",isSound?sound:music,At(x,.294f),Vector2.one*160);
                    var b=Hit(panel,name,paint.rectTransform.anchoredPosition,Vector2.one*195,paint);
                    Text(panel,name+"Nome",isSound?"Som":"Música",At(x,.377f),new Vector2(280,58),46,Ink);
                    var state=Text(panel,name+"Estado","Ligado",At(x,.409f),new Vector2(280,46),37,Ink);
                    Set(so,field+"Button",b);Set(so,field+"Icon",paint);Set(so,field+"State",state);
                    Set(so,field+"On",paint.sprite);Set(so,field+"Off",paint.sprite);
                }
                string[] names={"Vibracao","Dica","Notificacoes"},labels={"Vibração","Dicas","Notificações"},fields={"vibration","hint","notifications"};
                for(int i=0;i<names.Length;i++)
                {
                    float y=.525f+i*.162f;float bh=128;
                    var paint=Picture(panel,names[i]+"Arte",capsule,At(.5f,y),new Vector2(760,bh));
                    paint.type=Image.Type.Sliced;paint.preserveAspect=false;paint.pixelsPerUnitMultiplier=capsule.rect.height/bh;
                    var b=Hit(panel,names[i],paint.rectTransform.anchoredPosition,new Vector2(760,144),paint);
                    Text(panel,names[i]+"Nome",labels[i],At(.5f,y+.001f),new Vector2(670,74),55,Ivory,true);
                    var state=Text(panel,names[i]+"Estado","Ligado",At(.5f,y+.070f),new Vector2(600,48),40,Ink);
                    Set(so,fields[i]+"Button",b);Set(so,i==0?"vibrationState":fields[i]+"Text",state);
                }
                so.FindProperty("useRpgStateLabels").boolValue=true;
                so.FindProperty("onColour").colorValue=new Color32(28,101,59,255);so.FindProperty("offColour").colorValue=new Color32(159,43,39,255);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);AssetDatabase.SaveAssets();
                File.WriteAllText(Output+"/import.json",JsonUtility.ToJson(new ImportReport{assets=report.ToArray()},true));
                File.WriteAllText(Output+"/build.txt","PASS: five original transparent raster sprites imported losslessly with mipmaps; native serif labels and real SettingsScreen bindings; serialized inactive prefab built.\n");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            if(checkAfterBuild)SettingsV161Check.Run();else EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Output+"/build.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
