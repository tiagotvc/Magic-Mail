using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.UI.RoyalPostman2026;

public static class AcceleratorBadgeV98Build
{
    public const string Prefab="Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab";
    public const string Art="Assets/RoyalAves/Art/UI/Menu/RoyalPostman/AcceleratorBadgeV98/";
    public const string Output="../accelerator-badge-v98";
    public static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static RectTransform Node(Transform parent,string name,Vector2 position,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size;return r;
    }
    static Image Image(Transform parent,string name,Sprite sprite,Vector2 position,Vector2 size)
    {
        var r=Node(parent,name,position,size);r.gameObject.AddComponent<CanvasRenderer>();var i=r.gameObject.AddComponent<Image>();i.sprite=sprite;i.preserveAspect=true;i.raycastTarget=false;return i;
    }
    static Text Label(Text template,Transform parent,string name,Vector2 position,Vector2 size,int point,int minimum)
    {
        var text=UnityEngine.Object.Instantiate(template,parent,false);text.name=name;text.gameObject.SetActive(true);var r=text.rectTransform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size;r.localScale=Vector3.one;r.localRotation=Quaternion.identity;
        text.alignment=TextAnchor.MiddleCenter;text.fontSize=point;text.resizeTextForBestFit=true;text.resizeTextMinSize=minimum;text.resizeTextMaxSize=point;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;text.raycastTarget=false;return text;
    }
    static Material Part(Shader shader,string name,int part)
    {
        var material=new Material(shader);material.SetFloat("_Part",part);AssetDatabase.CreateAsset(material,Art+name+".mat");return material;
    }
    static Sprite Import()
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(Art+"badge.png");importer.isReadable=true;importer.SaveAndReimport();var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"badge.png");var pixels=texture.GetPixels32();var seen=new bool[pixels.Length];var queue=new int[pixels.Length];int largest=0,left=0,right=0,bottom=0,top=0;
        for(int seed=0;seed<pixels.Length;seed++)
        {
            if(seen[seed]||pixels[seed].a<96)continue;int head=0,tail=1,l=texture.width,r=0,b=texture.height,t=0;seen[seed]=true;queue[0]=seed;
            while(head<tail){int at=queue[head++],x=at%texture.width,y=at/texture.width;l=Math.Min(l,x);r=Math.Max(r,x);b=Math.Min(b,y);t=Math.Max(t,y);for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int nx=x+dx,ny=y+dy;if(nx<0||nx>=texture.width||ny<0||ny>=texture.height)continue;int next=ny*texture.width+nx;if(seen[next]||pixels[next].a<96)continue;seen[next]=true;queue[tail++]=next;}}
            if(tail>largest){largest=tail;left=l;right=r;bottom=b;top=t;}
        }
        Check(largest>10000,"Main badge alpha component missing");var rect=Rect.MinMaxRect(Math.Max(0,left-2),Math.Max(0,bottom-2),Math.Min(texture.width,right+3),Math.Min(texture.height,top+3));
        var sprite=Sprite.Create(texture,rect,Vector2.one*.5f,100,0,SpriteMeshType.FullRect);sprite.name="accelerator-badge-v98";Directory.CreateDirectory(Art+"Sprites");AssetDatabase.Refresh();AssetDatabase.CreateAsset(sprite,Art+"Sprites/badge.asset");
        File.WriteAllText(Output+"/sprite.txt",texture.width+"x"+texture.height+" original PNG; largest-alpha native crop "+rect+"\n");return sprite;
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        try
        {
            AssetDatabase.Refresh();var sprite=Import();var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/RoyalAves/Art/UI/Button/RoyalPostman/Shaders/RoyalPostmanBadgeParts.shader");Check(shader!=null,"Badge split shader missing");var gold=Part(shader,"BadgeGold",1);var red=Part(shader,"BadgeRed",2);
            var root=PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var h=root.GetComponent<RoyalPostmanPetsScreen>().hatchery;Check(h.accelerate.transform.Find("ResourceBadge")==null,"Badge already exists");var oldCount=h.accelerateQuantity;var oldIcon=h.accelerate.transform.Find("Image");var hourglass=oldIcon!=null?oldIcon.GetComponent<Image>().sprite:AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/UI/Icon/RoyalPostman/Pets/Sprites/pets-hourglass-v2.asset");Check(hourglass!=null,"Existing hourglass missing");
                var badge=Node(h.accelerate.transform,"ResourceBadge",new Vector2(86,55),new Vector2(94,94));badge.gameObject.AddComponent<CanvasRenderer>();var hit=badge.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
                Image(badge,"GoldBorder",sprite,Vector2.zero,new Vector2(94,94)).material=gold;
                Image(badge,"RedFace",sprite,Vector2.zero,new Vector2(94,94)).material=red;
                Image(badge,"Hourglass",hourglass,new Vector2(0,16),new Vector2(31,40));
                var count=Label(oldCount,badge,"Quantity",new Vector2(0,-18),new Vector2(68,30),34,6);count.text="x0";
                var cost=Label(oldCount,h.accelerate.transform,"GoldCost",new Vector2(0,-136),new Vector2(248,34),26,18);cost.text="";cost.gameObject.SetActive(false);
                oldCount.gameObject.SetActive(false);if(oldIcon!=null)oldIcon.gameObject.SetActive(false);
                h.accelerateQuantity=count;h.accelerateCost=cost;h.separateAcceleratorStock=true;h.Refresh();
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);
                var reusable=UnityEngine.Object.Instantiate(badge.gameObject);reusable.name="AcceleratorStockBadgeV98";try{PrefabUtility.SaveAsPrefabAsset(reusable,"Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Components/AcceleratorStockBadgeV98.prefab");}finally{UnityEngine.Object.DestroyImmediate(reusable);}
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();File.Copy(Prefab,Output+"/assembled.prefab",true);File.WriteAllText(Output+"/build.txt","PASS: original painted PNG split into aligned gold/red Image layers, existing hourglass and live stock count; badge attached upper-right; old detached resource hidden; optional gold fallback cost remains below label.\n");EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Output+"/build.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
