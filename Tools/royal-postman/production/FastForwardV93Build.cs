using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.UI.RoyalPostman2026;

public static class FastForwardV93Build
{
    public const string Prefab="Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab";
    public const string Output="../fast-forward-v93";
    public const string SpritePath="Assets/RoyalAves/Art/UI/Button/RoyalPostman/Pets/Sprites/fast-forward-chevron-v93.asset";
    public const string MaterialPath="Assets/RoyalAves/Art/UI/Button/RoyalPostman/Pets/fast-forward-cream-v93.mat";
    public static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        try
        {
            AssetDatabase.Refresh();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RoyalAves/Art/UI/Button/RoyalPostman/Pets/pets-page-arrow-v6.png");Check(texture!=null,"Existing rounded chevron PNG missing");
            var sprite=Sprite.Create(texture,new Rect(144,225,732,1260),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);sprite.name="fast-forward-chevron-v93";AssetDatabase.CreateAsset(sprite,SpritePath);
            var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/RoyalAves/Art/UI/Button/RoyalPostman/Shaders/RoyalPostmanNeutralPalette.shader");Check(shader!=null,"Existing UI palette shader missing");
            var material=new Material(shader);material.SetFloat("_UseCustomPalette",1);material.SetColor("_Color",Color.white);
            material.SetColor("_PaletteShadow",new Color(.72f,.43f,.20f));material.SetColor("_PaletteMid",new Color(1,.94f,.75f));material.SetColor("_PaletteHighlight",new Color(1,1,.94f));AssetDatabase.CreateAsset(material,MaterialPath);
            var root=PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var h=root.GetComponent<RoyalPostmanPetsScreen>().hatchery;var parent=h.accelerate.GetComponent<RoyalPostmanPaintedButton>().movingCenter.Find("FastForwardIcon");Check(parent!=null&&parent.childCount==0,"Unexpected icon structure");
                parent.GetComponent<Text>().enabled=false;
                foreach(var effect in parent.GetComponents<BaseMeshEffect>())effect.enabled=false;
                float height=72,width=height*sprite.rect.width/sprite.rect.height;
                for(int i=0;i<2;i++)
                {
                    var go=new GameObject(i==0?"ChevronLeft":"ChevronRight",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));var rect=(RectTransform)go.transform;rect.SetParent(parent,false);
                    rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.anchoredPosition=new Vector2(i==0?-23:23,0);rect.sizeDelta=new Vector2(width,height);
                    var image=go.GetComponent<Image>();image.sprite=sprite;image.material=material;image.preserveAspect=true;image.raycastTarget=false;
                    var shadow=go.AddComponent<Shadow>();shadow.effectColor=new Color(.23f,.045f,.012f,1);shadow.effectDistance=new Vector2(0,-2);
                    var outline=go.AddComponent<Outline>();outline.effectColor=new Color(.20f,.035f,.012f,1);outline.effectDistance=new Vector2(1,-1);
                }
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();File.Copy(Prefab,Output+"/assembled.prefab",true);
            File.WriteAllText(Output+"/build.txt","PASS: thin font >> disabled; two broad rounded PNG chevrons, cream palette and brown relief, under existing moving center.\n");EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Output+"/build.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
