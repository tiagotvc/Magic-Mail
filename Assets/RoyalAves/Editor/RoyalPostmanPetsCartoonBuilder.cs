using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.UI.RoyalPostman2026;

namespace RoyalAves.EditorTools
{
    /// <summary>V24 authoring migration. Run against the staged copy, then validate before selective installation.</summary>
    public static partial class RoyalPostmanPetsCartoonBuilder
    {
        [Serializable] public class AssetEntry { public string id, path, status; }
        [Serializable] public class Manifest { public AssetEntry[] assets; }
        static Manifest manifest;
        static Font font;
        static Material paintMaterial;
        static readonly string[] Ids={"common","rare","epic","legendary"};
        static readonly string[] Names={"Comum","Raro","Épico","Lendário"};
        static readonly Color Cream=new Color32(255,246,217,255), Plum=new Color32(68,17,49,255);
        static readonly Color[] RarityColors={new Color(.05f,.95f,1),new Color(.72f,.22f,1),new Color(1,.22f,.15f),new Color(1,.72f,.06f)};
        const string MaterialFolder="Assets/RoyalAves/Art/UI/Button/RoyalPostman/PetsCartoon/Materials";

        public static void Build()
        {
            manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText("Tools/royal-postman/production/PETS-CARTOON-V24.json"));
            foreach(var a in manifest.assets.Where(a=>!string.IsNullOrEmpty(a.path)&&(a.status=="generated"||a.status.StartsWith("restored"))))Import(a);
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/RoyalAves/Fonts/LilitaOne-Regular.ttf");
            if(font==null)throw new InvalidOperationException("Missing approved Home font");
            Directory.CreateDirectory(MaterialFolder);AssetDatabase.Refresh();
            string matPath=MaterialFolder+"/pets-cartoon-v24-surface.mat";
            paintMaterial=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(paintMaterial==null){paintMaterial=new Material(Shader.Find("RoyalPostman/UI/SymmetricPaint"));AssetDatabase.CreateAsset(paintMaterial,matPath);}
            CreateRailSprite();
            var root=PrefabUtility.LoadPrefabContents(RoyalPostmanPetsBuilder.PrefabPath);
            try
            {
                var r=(RectTransform)root.transform;var view=root.GetComponent<RoyalPostmanPetsScreen>();
                int[] unlocks=view.incubators.Select(i=>i.unlockLevel).ToArray();
                foreach(string path in new[]{"Room","Body","Inventory","Divider","Header","CurrentTab","IncubatorTab"}){var old=r.Find(path);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
                view.cartoonLayout=true;view.mobileCarousel=true;view.inventory=null;view.inventoryRows=Array.Empty<RoyalPostmanPetsScreen.InventoryRow>();
                view.cartoonSelectedTabPaint=Paint("action-teal-symmetric");view.cartoonInactiveTabPaint=Paint("action-purple");
                view.currentTabBlue=view.currentTabGold=view.incubatorTabBlue=view.incubatorTabGold=null;
                view.incubationRules.legendaryEggSeconds=172800;
                var backgroundRoot=r.Find("Background");foreach(var graphic in backgroundRoot.GetComponents<Graphic>())UnityEngine.Object.DestroyImmediate(graphic);
                var background=backgroundRoot.gameObject.AddComponent<Image>();background.color=new Color32(148,74,28,255);background.raycastTarget=false;
                BuildWorkshop(r,view,unlocks);
                BuildShelf(r,view);
                BuildHeader(r,view);
                var tooltip=view.tooltipCard;
                if(tooltip!=null){tooltip.sizeDelta=new Vector2(580,430);view.hintTitle.rectTransform.anchoredPosition=new Vector2(20,126);view.hintDescription.rectTransform.anchoredPosition=new Vector2(0,-40);view.hintDescription.rectTransform.sizeDelta=new Vector2(496,260);view.dismissItemTooltip.GetComponent<RectTransform>().anchoredPosition=new Vector2(247,150);}
                var outside=view.itemTooltip.transform.Find("Outside").GetComponent<RoyalPostmanTooltipSurface>();outside.passThrough=(RectTransform)r.Find("Inventory");
                var helpCopy=view.helpPanel.transform.Find("Card/Copy")?.GetComponent<Text>();
                if(helpCopy!=null)helpCopy.text="Use as setas ou toque nos ovos para escolher a raridade.\nCada incubadora recebe seu próprio ovo.\n\nA ampulheta no alto mostra os aceleradores disponíveis.\nLendária: 48 horas.\nCustos em gold aparecem antes da ação.";
                view.itemTooltip.transform.SetAsLastSibling();view.helpPanel.transform.SetAsLastSibling();
                view.SelectIncubator(0);view.RefreshAvailability(RoyalAves.Meta.MetaProgress.NextLevel);view.ShowCurrent(view.defaultCurrentTab);
                PrefabUtility.SaveAsPrefabAsset(root,RoyalPostmanPetsBuilder.PrefabPath);AssetDatabase.SaveAssets();
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }

        static AssetEntry Asset(string name)=>manifest.assets.First(a=>a.id=="pets-cartoon-v24-"+name);
        static Texture2D Paint(string name)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Asset(name).path);
        static string SpritePath(AssetEntry a)=>Path.GetDirectoryName(a.path).Replace('\\','/')+"/Sprites/"+a.id+".asset";
        static Sprite S(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath(Asset(name)))??throw new Exception("Missing cartoon sprite "+name);
        static Sprite Existing(string id)=>AssetDatabase.LoadAssetAtPath<Sprite>(RoyalPostmanArtPaths.Sprite(id))??throw new Exception("Missing approved sprite "+id);
        static Rect OpaqueBounds(Texture2D read)
        {
            var pixels=read.GetPixels32();int w=read.width,h=read.height;var seen=new bool[pixels.Length];var queue=new int[pixels.Length];int largest=0,l=0,r=w-1,b=0,t=h-1;
            for(int seed=0;seed<pixels.Length;seed++)
            {
                if(seen[seed]||pixels[seed].a<192)continue;int head=0,tail=1;queue[0]=seed;seen[seed]=true;int x0=w,x1=0,y0=h,y1=0;
                while(head<tail)
                {
                    int at=queue[head++],x=at%w,y=at/w;x0=Math.Min(x0,x);x1=Math.Max(x1,x);y0=Math.Min(y0,y);y1=Math.Max(y1,y);
                    foreach(int next in new[]{x>0?at-1:-1,x+1<w?at+1:-1,y>0?at-w:-1,y+1<h?at+w:-1})if(next>=0&&!seen[next]&&pixels[next].a>=192){seen[next]=true;queue[tail++]=next;}
                }
                if(tail>largest){largest=tail;l=x0;r=x1;b=y0;t=y1;}
            }
            return Rect.MinMaxRect(Math.Max(0,l-2),Math.Max(0,b-2),Math.Min(w,r+3),Math.Min(h,t+3));
        }
        static void Import(AssetEntry a)
        {
            AssetDatabase.ImportAsset(a.path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            var importer=(TextureImporter)AssetImporter.GetAtPath(a.path);importer.textureType=TextureImporterType.Default;importer.maxTextureSize=8192;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(a.path);var read=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(read,File.ReadAllBytes(a.path));
            Rect bounds=a.id.EndsWith("workshop")||a.id.EndsWith("egg-shelf")||a.id.EndsWith("egg-glow")?new Rect(0,0,read.width,read.height):OpaqueBounds(read);
            UnityEngine.Object.DestroyImmediate(read);
            var border=a.id.EndsWith("control-panel")?Vector4.one*170:a.id.EndsWith("egg-shelf")?new Vector4(0,80,0,90):Vector4.zero;
            var sprite=Sprite.Create(texture,bounds,Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border);sprite.name=a.id;
            string file=SpritePath(a);Directory.CreateDirectory(Path.GetDirectoryName(file));AssetDatabase.Refresh();SaveSprite(sprite,file);
        }
        static void SaveSprite(Sprite sprite,string path)
        {
            var previous=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(previous==null)AssetDatabase.CreateAsset(sprite,path);else{EditorUtility.CopySerialized(sprite,previous);EditorUtility.SetDirty(previous);UnityEngine.Object.DestroyImmediate(sprite);}
        }
        static Sprite rail;
        static void CreateRailSprite()
        {
            var texture=Paint("action-rim");var rect=new Rect(texture.width*.3f,texture.height*.82f,texture.width*.4f,texture.height*.047f);
            rail=Sprite.Create(texture,rect,Vector2.one*.5f,100,0,SpriteMeshType.FullRect);rail.name="pets-cartoon-v24-straight-gold";
            string path=Path.GetDirectoryName(SpritePath(Asset("action-rim")))+"/pets-cartoon-v24-straight-gold.asset";SaveSprite(rail,path);rail=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static RectTransform Box(RectTransform parent,string name,float x,float y,float w,float h)
        {
            var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;
        }
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static void Bottom(RectTransform r,float x,float y){r.anchorMin=r.anchorMax=new Vector2(.5f,0);r.anchoredPosition=new Vector2(x,y);}
        static void Top(RectTransform r,float x,float y){r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.anchoredPosition=new Vector2(x,y);}
        static Image Picture(RectTransform parent,string name,Sprite sprite,float x,float y,float w,float h,bool aspect=false)
        {
            var image=Box(parent,name,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=aspect;image.raycastTarget=false;return image;
        }
        static RoyalPostmanSymmetricButtonFace Face(RectTransform parent,string name,Texture2D texture,float x,float y,float w,float h,float radius,float bevel,bool center=true,bool gold=false)
        {
            var face=Box(parent,name,x,y,w,h).gameObject.AddComponent<RoyalPostmanSymmetricButtonFace>();face.paint=texture??Texture2D.whiteTexture;face.material=paintMaterial;face.cornerRadius=radius;face.bevelWidth=bevel;face.drawCenter=center;face.raycastTarget=false;face.antialiasWidth=.65f;
            if(texture==null){face.sourceOuter=face.sourceFace=new Rect(0,0,1,1);face.sourceOuterRadius=face.sourceFaceRadius=0;}
            else if(gold){face.sourceOuter=new Rect(100,121,1723,578);face.sourceFace=new Rect(140,152,1645,504);face.sourceOuterRadius=184;face.sourceFaceRadius=166;}
            else{face.sourceOuter=new Rect(texture.width*.04995f,texture.height*.1345f,texture.width*.9001f,texture.height*.7384f);face.sourceFace=new Rect(texture.width*.112f,texture.height*.30f,texture.width*.776f,texture.height*.367f);face.sourceOuterRadius=texture.height*.22f;face.sourceFaceRadius=texture.height*.10f;}
            return face;
        }
        static Text Label(RectTransform parent,string name,string value,float x,float y,float w,float h,int size,bool light=true)
        {
            var text=Box(parent,name,x,y,w,h).gameObject.AddComponent<Text>();text.font=font;text.text=value;text.fontSize=size;text.color=light?Cream:Plum;text.alignment=TextAnchor.MiddleCenter;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Overflow;text.raycastTarget=false;
            if(light){var outline=text.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.13f,.025f,.09f,1);outline.effectDistance=new Vector2(1.8f,-1.8f);var shadow=text.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.12f,.02f,.06f,.8f);shadow.effectDistance=new Vector2(0,-2.4f);}
            return text;
        }
        static RectTransform Plaque(RectTransform parent,string name,string paint,string value,float x,float y,float w,float h,int textSize,out Text label)
        {
            var root=Box(parent,name,x,y,w,h);Face(root,"FixedGold",Paint("action-rim"),0,0,w,h,h*.42f,5,false,true);Face(root,"Face",Paint(paint),0,0,w-14,h-14,h*.42f-7,6);label=Label(root,"Label",value,0,1,w-28,h-10,textSize);return root;
        }
        static Button PaintedControl(RectTransform parent,string name,string paint,float x,float y,float w,float h,float radius,bool inset,out RectTransform moving)
        {
            var root=Box(parent,name,x,y,w,h);var hit=root.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            if(inset){var socket=Face(root,"Inset",null,0,0,w+4,h+4,radius+2,2,false);socket.color=new Color(.58f,.37f,.16f,.48f);}
            Face(root,"FixedGold",Paint("action-rim"),0,0,w,h,radius,9,false,true);
            moving=Box(root,"MovingCenter",0,0,w,h);var face=Face(moving,"Face",Paint(paint),0,0,w-24,h-24,radius-12,Mathf.Min(20,(h-24)*.15f));
            var button=root.gameObject.AddComponent<Button>();button.targetGraphic=face;button.transition=Selectable.Transition.None;
            var press=root.gameObject.AddComponent<RoyalPostmanPaintedButton>();press.movingCenter=moving;press.pressTravel=1.5f;press.pressScale=.94f;
            var visual=root.gameObject.AddComponent<RoyalPostmanActionVisual>();visual.tintedWhenDisabled=new Graphic[]{face};visual.disabledTint=new Color(.62f,.62f,.66f,1);
            return button;
        }
    }
}
