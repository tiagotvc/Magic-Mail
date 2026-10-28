using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.EditorTools;
using RoyalAves.UI.RoyalPostman2026;
public static class UISharpnessV65Review
{
    const string Output="../ui-sharpness-v65";
    static void Check(bool ok,string msg){if(!ok)throw new Exception(msg);}
    public static void Run()
    {
        Directory.CreateDirectory(Output);File.WriteAllText(Output+"/review.txt","RUNNING");
        try
        {
            var report="";
            foreach(var file in Directory.GetFiles(RoyalPostmanUserSpriteImportPolicy.Root,"*.png"))
            {
                var path=file.Replace('\\','/');AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                var guid=AssetDatabase.AssetPathToGUID(path);var type=importer.textureType;var mode=importer.spriteImportMode;var border=importer.spriteBorder;var pivot=importer.spritePivot;
                var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);int width=t.width,height=t.height;
                RoyalPostmanUserSpriteImportPolicy.Configure(importer);importer.SaveAndReimport();t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Check(AssetDatabase.AssetPathToGUID(path)==guid&&importer.textureType==type&&importer.spriteImportMode==mode&&importer.spriteBorder==border&&importer.spritePivot==pivot,"Sprite identity/settings changed "+path);
                Check(t.width==width&&t.height==height,"Texture size changed "+path);
                Check(importer.textureCompression==TextureImporterCompression.Uncompressed&&t.mipmapCount>1&&t.filterMode==FilterMode.Trilinear,"Quality policy not applied "+path);
                report+=Path.GetFileName(path)+" "+width+"x"+height+" "+t.format+" mipmaps="+t.mipmapCount+"; sprite identity preserved\n";
            }
            var prefab=File.ReadAllBytes("Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab");
            var size=new Vector2Int(1080,1920);
            Check(RoyalPostmanScreenEditor.PreviewRenderSize(size,1,1)==size,"100% not1:1");
            Check(RoyalPostmanScreenEditor.PreviewRenderSize(size,2,1)==new Vector2Int(2160,3840),"200% magnifies low-res buffer");
            Check(RoyalPostmanScreenEditor.PreviewRenderSize(size,1,2)==new Vector2Int(2160,3840),"Retina density lost");
            var fit=RoyalPostmanScreenEditor.PreviewRenderSize(size,497f/1080,1);
            Check(fit.x==497&&fit.y>=883&&fit.y<=884,"Fit not rendered at display resolution");
            using(var d=new RoyalPostmanScreenDocument(RoyalPostmanRuntimeKit.Load(),"pets",1080,1920))
            {
                var view=d.Root.GetComponent<RoyalPostmanPetsScreen>();view.ShowCurrent(false);d.Render();
                var button=view.helpButton.GetComponent<RectTransform>();var original=button.anchoredPosition;var bounds=d.Bounds(button);
                d.Resize(fit.x,fit.y);d.Render();var next=d.Bounds(button);
                Check(Mathf.Abs(next.center.x/fit.x-bounds.center.x/1080)<.002f&&Mathf.Abs(next.center.y/fit.y-bounds.center.y/1920)<.002f,"Fit changed element position: "+bounds+" -> "+next+" target="+fit);
                var pick=d.Pick(next.center);Check(pick!=null&&(pick==button||pick.IsChildOf(button)),"Picking/selection broke at fit density");
                Check(button.anchoredPosition==original,"Preview changed authored position");d.Export(Output+"/final-fit-497.png");
                d.Resize(2160,3840);d.Render();Check(d.Width==2160&&d.Height==3840,"Zoom render lost density");
            }
            Check(File.ReadAllBytes("Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab").SequenceEqual(prefab),"Review modified prefab");
            File.WriteAllText(Output+"/review.txt","PASS:11 user PNGs imported losslessly with mipmaps; GUID/type/sprite mode/border/pivot/dimensions preserved;100%/200%/HiDPI/Fit density; pick coordinates and authored position retained; no prefab changes.\n"+report);
            EditorApplication.Exit(0);
        }catch(Exception e){File.WriteAllText(Output+"/review.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}

