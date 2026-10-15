using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MapNavigationReferenceCapture
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/navigation-reference";
    const BindingFlags Instance=BindingFlags.Instance|BindingFlags.NonPublic;
    static int errors,materialWarnings;
    public static void Run()
    {
        Application.logMessageReceived+=(message,stack,type)=>
        {
            if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;
            if(message.Contains("Instantiating material due to calling renderer.material"))materialWarnings++;
        };
        try
        {
            Directory.CreateDirectory(Output);
            Capture(1080,1920,true);
            Capture(1080,2280,false);
            CompareReference();
            Check(errors==0&&materialWarnings==0,"Unity logged errors or instantiated materials during capture");
            File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: native Unity renders at 1080 x 1920 and 1080 x 2280; all seven navigation textures retain original source dimensions, uncompressed, mip-filtered with trilinear sampling and exempt from global mip limits; PNG hashes unchanged; five equal-width touching tabs; original order and five original sprite references preserved; each tab relays its original action exactly once; one active label; selected icon extends above the rail; painted backplates bound to active navigation shader; all five states captured; label and surface materials released on close; latest user reference and native capture compared at same menu width of 1080; zero Unity errors and zero material-instantiation warnings.");
            EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static void CheckTexture(Texture2D texture)
    {
        var path=AssetDatabase.GetAssetPath(texture);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        Check(importer.mipmapEnabled&&importer.ignoreMipmapLimit&&importer.textureCompression==TextureImporterCompression.Uncompressed&&importer.filterMode==FilterMode.Trilinear,"Navigation texture import reduces UI quality: "+path);
        var original=new Texture2D(2,2);
        try
        {
            original.LoadImage(File.ReadAllBytes(path));
            Check(texture.width==original.width&&texture.height==original.height,"Navigation texture is smaller than its source: "+path);
            var relative=Path.Combine("Assets/RoyalAves/Art/MapKitV3/MapUI",Path.GetFileName(path));
            var source=Path.Combine("C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail",relative);
            using(var hash=System.Security.Cryptography.SHA256.Create())
                Check(hash.ComputeHash(File.ReadAllBytes(path)).SequenceEqual(hash.ComputeHash(File.ReadAllBytes(source))),"Navigation PNG pixels changed: "+path);
        }
        finally{UnityEngine.Object.DestroyImmediate(original);}
    }
    static void CompareReference()
    {
        // A review render, not a modified game asset: the two supplied images are drawn at
        // the same menu width. Latest reference: x=71..1371, gold top y=76, bottom y=320.
        var reference=new Texture2D(2,2);reference.LoadImage(File.ReadAllBytes(Path.Combine(Output,"reference-latest-1.png")));
        var actual=new Texture2D(2,2);actual.LoadImage(File.ReadAllBytes(Path.Combine(Output,"tab-0.png")));
        reference.filterMode=actual.filterMode=FilterMode.Bilinear;
        var rt=RenderTexture.GetTemporary(2160,235,0,RenderTextureFormat.ARGB32);
        var previous=RenderTexture.active;
        var result=new Texture2D(2160,235,TextureFormat.RGB24,false);
        try
        {
            RenderTexture.active=rt;GL.Clear(true,true,new Color(.105f,.105f,.105f));
            GL.PushMatrix();GL.LoadPixelMatrix(0,2160,235,0);
            var source=new Rect(71f/1439,1-322.8704f/385,1300f/1439,282.8704f/385);
            Graphics.DrawTexture(new Rect(0,0,1080,235),reference,source,0,0,0,0);
            Graphics.DrawTexture(new Rect(1080,0,1080,235),actual,new Rect(0,0,1,235f/285),0,0,0,0);
            GL.PopMatrix();GL.Flush();
            result.ReadPixels(new Rect(0,0,2160,235),0,0);result.Apply();
            File.WriteAllBytes(Path.Combine(Output,"comparison-same-scale.png"),result.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(reference);UnityEngine.Object.DestroyImmediate(actual);UnityEngine.Object.DestroyImmediate(result);
        }
    }
    static void Capture(int width,int height,bool allTabs)
    {
        RoyalAvesLevelMapPreview preview=null;
        try
        {
            preview=RoyalAvesLevelMapPreview.CreateScenePreview(width,height);
            var map=(CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Instance).GetValue(preview);
            var content=map.transform.parent;
            var nav=(RectTransform)content.Find("NavegacaoDoMapa");
            var names=new[]{"Mapa","Eventos","Ranking","Equipes","Coleção"};
            var keys=new[]{2,0,1,3,4};
            var buttons=nav.GetComponentsInChildren<Button>().OrderBy(x=>x.transform.GetSiblingIndex()).ToArray();
            Check(buttons.Length==5,"Navigation no longer has five tabs");
            var originalIcons=map.World.CameraModel.Kit.mapNavIcons;
            foreach(var sprite in originalIcons)CheckTexture(sprite.texture);
            CheckTexture(map.World.CameraModel.Kit.navigationPaint);
            CheckTexture(map.World.CameraModel.Kit.navigationSelectedPaint);
            for(var i=0;i<buttons.Length;i++)
            {
                var button=buttons[i];var rect=(RectTransform)button.transform;
                Check(button.transform.Find("Label").GetComponent<TMP_Text>().text==names[i],"Tab order changed");
                Check(Mathf.Abs(rect.rect.width-nav.rect.width/5)<.01f,"Tabs are not equal width");
                Check(rect.offsetMin==Vector2.zero&&rect.offsetMax==Vector2.zero,"Navigation contains gaps");
                Check(button.transform.Find("Icon").GetComponent<Image>().sprite==originalIcons[keys[i]],"An icon was replaced");
                var deliveries=0;
                content.Find("Nav/"+button.name).GetComponent<Button>().onClick.AddListener(()=>deliveries++);
                button.onClick.Invoke();
                Check(deliveries==1,"Tab did not relay its original action once");
                Check(nav.GetComponentsInChildren<TMP_Text>().Length==1,"More than the selected label is visible");
                var icon=button.transform.Find("Icon") as RectTransform;
                Check(icon.anchoredPosition.y+icon.anchorMin.y*nav.rect.height+icon.rect.height/2>nav.rect.height,"Selected icon no longer rises over the rail");
                if(allTabs)
                {
                    preview.RenderScenePreview();
                    Save(preview,Path.Combine(Output,"tab-"+i+".png"),true);
                }
            }
            map.SelectNavigationTab(2);preview.RenderScenePreview();
            Save(preview,Path.Combine(Output,"phone-"+width+"x"+height+".png"),false);
            var shadow=nav.GetComponentsInChildren<TMP_Text>(true).First().fontSharedMaterial;
            var surface=nav.GetComponentsInChildren<WorldMapPanelGraphic>().Single().material;
            Check(surface.shader.name=="RoyalAves/UI/MapNavigation","The molded navigation shader is not bound");
            Check(surface.GetFloat("_UsePaintedNav")>.5f&&surface.GetTexture("_BasePaint")!=null&&surface.GetTexture("_ActivePaint")!=null,"Painted navigation assets are not bound");
            map.Close();
            Check(shadow==null,"Navigation label material leaked on close");
            Check(surface==null,"Navigation surface material leaked on close");
        }
        finally{if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}
    }
    static void Save(RoyalAvesLevelMapPreview preview,string path,bool footerOnly)
    {
        var capture=(RenderTexture)preview.SceneTexture;
        var cropHeight=footerOnly?Mathf.CeilToInt(capture.width*285f/1080):capture.height;
        var previous=RenderTexture.active;
        var image=new Texture2D(capture.width,cropHeight,TextureFormat.RGBA32,false);
        try
        {
            RenderTexture.active=capture;
            image.ReadPixels(new Rect(0,0,capture.width,cropHeight),0,0);image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG());
        }
        finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}
    }
}
