using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RoyalAves.Characters;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesRibbonRookRigBuilder
    {
        public const string Atlas="Assets/RoyalAves/Art/MapKitV3/RibbonRookV73/ribbon-rook-cutout-parts-v73.png";
        public const string Folder="Assets/RoyalAves/Characters/RibbonRookV73";
        public const string Prefab=Folder+"/RibbonRookV73.prefab";
        static readonly string[] Names={"Body","LeftArm","RightArm","MouthClosed","MouthOpen","GiftSack"};
        static readonly Rect[] Regions={new Rect(45,0,470,700),new Rect(550,140,455,430),new Rect(1030,140,500,430),new Rect(90,710,410,300),new Rect(600,710,420,300),new Rect(1030,580,506,444)};

        [MenuItem("Royal Aves/Personagens/Build Ribbon Rook cutout")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(Atlas);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.isReadable=true;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
            importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas);
            var pixels=texture.GetPixels32();float sx=texture.width/1536f,sy=texture.height/1024f;
            var regions=new SpriteMetaData[Names.Length];
            for(int i=0;i<Names.Length;i++)
            {
                var r=Regions[i];int x0=Mathf.RoundToInt(r.x*sx),x1=Mathf.RoundToInt(r.xMax*sx);
                int y0=Mathf.RoundToInt(texture.height-r.yMax*sy),y1=Mathf.RoundToInt(texture.height-r.y*sy);
                int left=x1,right=x0,bottom=y1,top=y0;
                for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(pixels[y*texture.width+x].a>24){left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
                if(right<=left||top<=bottom)throw new Exception("Empty cutout: "+Names[i]);
                var rect=new Rect(Math.Max(x0,left-2),Math.Max(y0,bottom-2),Math.Min(x1,right+3)-Math.Max(x0,left-2),Math.Min(y1,top+3)-Math.Max(y0,bottom-2));
                var pivot=new Vector2(.5f,0);
                if(i==3||i==4){var hinge=i==3?new Vector2(157,786):new Vector2(675,780);pivot=new Vector2((hinge.x*sx-rect.x)/rect.width,(texture.height-hinge.y*sy-rect.y)/rect.height);}
                regions[i]=new SpriteMetaData{name=Names[i],rect=rect,alignment=(int)SpriteAlignment.Custom,pivot=pivot};
            }
            importer.spritesheet=regions;importer.isReadable=false;importer.SaveAndReimport();
            var sprites=AssetDatabase.LoadAllAssetsAtPath(Atlas).OfType<Sprite>().ToDictionary(s=>s.name);
            var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/RibbonRook.mat");
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")??Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(material,Folder+"/RibbonRook.mat");}
            var root=new GameObject("RibbonRookV73");
            try
            {
                var rig=root.AddComponent<RibbonRookCutout>();
                float scale=6/sprites["Body"].rect.height;var bodyRect=sprites["Body"].rect;
                Vector2 Point(float x,float y)=>new Vector2((x*sx-bodyRect.center.x)*scale,(texture.height-y*sy-bodyRect.y)*scale);
                Transform Joint(string name,Vector2 at){var t=new GameObject(name).transform;t.SetParent(root.transform,false);t.localPosition=at;return t;}
                SpriteRenderer Art(Transform parent,string part,float height,Vector2 anchor,int order)
                {
                    var s=sprites[part];float zoom=height/s.bounds.size.y;
                    var t=new GameObject("Art").transform;t.SetParent(parent,false);t.localScale=Vector3.one*zoom;
                    t.localPosition=new Vector3((.5f-anchor.x)*s.bounds.size.x*zoom,-anchor.y*height,0);
                    var sr=t.gameObject.AddComponent<SpriteRenderer>();sr.sprite=s;sr.sharedMaterial=material;sr.sortingOrder=order;return sr;
                }
                Art(Joint("Body",Vector2.zero),"Body",6,new Vector2(.5f,0),3);
                rig.leftArm=Joint("LeftArm",Point(228,258));Art(rig.leftArm,"LeftArm",2.45f,new Vector2(.91f,.92f),4);
                rig.rightArm=Joint("RightArm",Point(371,261));Art(rig.rightArm,"RightArm",2.45f,new Vector2(.09f,.92f),4);
                var mouth=Joint("Mouth",Point(303,192));
                var mouthArt=new GameObject("Art");mouthArt.transform.SetParent(mouth,false);
                mouthArt.transform.localScale=Vector3.one*(1.46f/sprites["MouthClosed"].bounds.size.x);
                rig.mouth=mouthArt.AddComponent<SpriteRenderer>();rig.mouth.sprite=sprites["MouthClosed"];rig.mouth.sharedMaterial=material;rig.mouth.sortingOrder=5;
                rig.closedMouth=sprites["MouthClosed"];rig.openMouth=sprites["MouthOpen"];
                Art(Joint("GiftSack",new Vector2(-2.05f,0)),"GiftSack",2.3f,new Vector2(.5f,0),1);
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);AssetDatabase.SaveAssets();
                Debug.Log("Ribbon Rook: standing cutout with shoulder pivots and replaceable mouth saved.");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
