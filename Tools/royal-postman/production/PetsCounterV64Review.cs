using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.UI.RoyalPostman2026;
public static class PetsCounterV64Review
{
    const string Art="Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsLayeredV63/";
    const string Output="../pets-counter-v64";
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        try
        {
            string path=Art+"counter-supported-v64.png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.isReadable=true;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=8192;importer.SaveAndReimport();
            var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var px=t.GetPixels32();int minX=t.width,minY=t.height,maxX=0,maxY=0;
            for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++)if(px[y*t.width+x].a>=200){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}
            var sprite=Sprite.Create(t,new Rect(minX,minY,maxX-minX+1,maxY-minY+1),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);sprite.name="counter-supported-v64";
            string target=Art+"Sprites/counter-supported-v64.asset";var old=AssetDatabase.LoadAssetAtPath<Sprite>(target);if(old==null)AssetDatabase.CreateAsset(sprite,target);else{EditorUtility.CopySerialized(sprite,old);UnityEngine.Object.DestroyImmediate(sprite);sprite=old;}AssetDatabase.SaveAssets();
            File.WriteAllText(Output+"/sprite-guid.txt",AssetDatabase.AssetPathToGUID(target));
            foreach(int y in new[]{1020,1045,1065})
            using(var d=new RoyalPostmanScreenDocument(RoyalPostmanRuntimeKit.Load(),"pets",720,1280))
            {
                var view=d.Root.GetComponent<RoyalPostmanPetsScreen>();view.Initialize(()=>{},i=>{});view.ShowCurrent(false);view.SelectIncubator(3);
                var im=d.Root.transform.Find("Body/Workshop/Counter").GetComponent<Image>();im.sprite=sprite;im.rectTransform.sizeDelta=new Vector2(1650,235);im.rectTransform.anchoredPosition=new Vector2(0,-y);
                d.Export(Output+"/counter-y"+y+".png");
            }
            File.WriteAllText(Output+"/review.txt","PASS: supported full-width counter sprite imported and native placement previews exported; main prefab untouched.\n");
            EditorApplication.Exit(0);
        }catch(Exception e){File.WriteAllText(Output+"/review.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
