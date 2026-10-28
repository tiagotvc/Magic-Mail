using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.Meta;
using RoyalAves.UI.RoyalPostman2026;

public static class MenuV162Build
{
    public const string Prefab="Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/MenuWindowV162.prefab";
    public const string SettingsPrefab="Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/SettingsWindowV161.prefab";
    public const string Art="Assets/RoyalAves/Art/UI/Menu/RoyalPostman/MenuV162/Sprites/";
    public const string Output="../menu-v162";
    [Serializable] public class Info {public string prefabGuid,rootFileId,frameTextureGuid,headerGuid,bodyGuid;public int newPngCount;}
    static readonly string Tab="Assets/RoyalAves/Art/UI/Tab/RoyalPostman/Sprites/",Icon="Assets/RoyalAves/Art/UI/Icon/RoyalPostman/Sprites/";
    public static readonly string[] Names={"Ranking","Equipes","Colecao","Eventos","Perfil","Inventario","Loja","Configuracoes"};
    static readonly string[] Labels={"Ranking","Equipes","Coleção","Eventos","Meu perfil","Inventário","Loja","Configurações"};
    static readonly string[] Icons={Tab+"nav-ranking-v22.asset",Tab+"navigation-teams-individual-v3.asset",Tab+"navigation-album-individual-v3.asset",Tab+"navigation-clipboard-individual-v3.asset",Icon+"courier-portrait.asset",Icon+"booster-bomb.asset",Icon+"coin-envelope.asset",Icon+"gear-cream.asset"};
    public static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Place(RectTransform rt,Vector2 at,Vector2 size){rt.anchorMin=rt.anchorMax=rt.pivot=Vector2.one*.5f;rt.anchoredPosition=at;rt.sizeDelta=size;}
    static RectTransform Node(Transform parent,string name,Vector2 at,Vector2 size){var go=new GameObject(name,typeof(RectTransform)){layer=parent.gameObject.layer};var rt=(RectTransform)go.transform;rt.SetParent(parent,false);Place(rt,at,size);return rt;}
    static Sprite Slice(Texture2D texture,string name,Rect area,Vector4 border)
    {
        var s=Sprite.Create(texture,area,Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border);s.name=name;
        string path=Art+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(existing==null)AssetDatabase.CreateAsset(s,path);else{EditorUtility.CopySerialized(s,existing);UnityEngine.Object.DestroyImmediate(s);EditorUtility.SetDirty(existing);}
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Image Picture(Transform parent,string name,Sprite sprite,Vector2 at,Vector2 size){var rt=Node(parent,name,at,size);var im=rt.gameObject.AddComponent<Image>();im.sprite=sprite;im.raycastTarget=false;im.preserveAspect=true;return im;}
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        try
        {
            Directory.CreateDirectory(Art);AssetDatabase.Refresh();
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPrefab);Check(source!=null,"Existing Settings window missing");
            var sourcePanel=source.transform.Find("Janela");var frame=sourcePanel.Find("Moldura").GetComponent<Image>().sprite;
            Check(frame!=null&&frame.texture!=null,"Existing painted frame missing");
            foreach(string iconPath in Icons){var existingIcon=AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);Check(existingIcon!=null,"Icon absent");AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(existingIcon.texture),ImportAssetOptions.ForceUpdate);}
            // Native sprite regions of the SAME PNG: keep the approved arch pixel-for-pixel;
            // use the blank lower frame for a tall menu, without inheriting the sound-control tray.
            const float headerPixels=312;
            var header=Slice(frame.texture,"postal-header",new Rect(0,frame.texture.height-headerPixels,frame.texture.width,headerPixels),Vector4.zero);
            var body=Slice(frame.texture,"postal-body",new Rect(0,0,frame.texture.width,800),new Vector4(100,100,100,0));
            var root=UnityEngine.Object.Instantiate(source);root.name="MenuWindowV162";root.SetActive(false);
            try
            {
                if(PrefabUtility.IsPartOfPrefabInstance(root))PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                UnityEngine.Object.DestroyImmediate(root.GetComponent<SettingsScreen>());
                var panel=(RectTransform)root.transform.Find("Janela");var fit=root.GetComponent<RoyalPostmanSettingsFit>();fit.designSize=new Vector2(1000,1600);panel.sizeDelta=fit.designSize;
                for(int i=panel.childCount-1;i>=0;i--){var child=panel.GetChild(i);if(child.name!="Titulo"&&child.name!="Fechar"&&child.name!="FecharArte")UnityEngine.Object.DestroyImmediate(child.gameObject);}
                float height=1600;
                Vector2 At(float x,float top)=>new Vector2(x,height*.5f-top);
                float headerHeight=1000*headerPixels/frame.texture.width,bodyHeight=height-headerHeight;
                var bodyPaint=Picture(panel,"Corpo",body,At(0,headerHeight+bodyHeight*.5f),new Vector2(1000,bodyHeight));bodyPaint.type=Image.Type.Sliced;bodyPaint.preserveAspect=false;bodyPaint.pixelsPerUnitMultiplier=frame.texture.width/1000f;bodyPaint.transform.SetAsFirstSibling();
                var headerPaint=Picture(panel,"Cabecalho",header,At(0,headerHeight*.5f),new Vector2(1000,headerHeight));headerPaint.transform.SetSiblingIndex(1);
                var title=panel.Find("Titulo").GetComponent<TextMeshProUGUI>();title.text="Menu";title.rectTransform.anchoredPosition=At(0,1000*frame.rect.height/frame.rect.width*.157f);
                var closePaint=panel.Find("FecharArte").GetComponent<Image>();closePaint.rectTransform.anchoredPosition=At(445,1000*frame.rect.height/frame.rect.width*.185f);
                var close=panel.Find("Fechar").GetComponent<Button>();((RectTransform)close.transform).anchoredPosition=closePaint.rectTransform.anchoredPosition;
                var view=root.AddComponent<RoyalPostmanMenuWindow>();var so=new SerializedObject(view);so.FindProperty("closeButton").objectReferenceValue=close;
                var entries=so.FindProperty("entries");entries.arraySize=Names.Length;
                for(int i=0;i<Names.Length;i++)
                {
                    var row=Node(panel,Names[i],At(0,355+i*157),new Vector2(820,144));
                    // Clone the working V161 control components, retaining their art, native 9-slice,
                    // text material, press colours and hit target; only labels/icons/positions differ.
                    var paint=UnityEngine.Object.Instantiate(sourcePanel.Find("VibracaoArte").gameObject,row,false).GetComponent<Image>();paint.name="Arte";Place(paint.rectTransform,Vector2.zero,new Vector2(820,128));
                    var button=UnityEngine.Object.Instantiate(sourcePanel.Find("Vibracao").gameObject,row,false).GetComponent<Button>();button.name="Botao";Place((RectTransform)button.transform,Vector2.zero,new Vector2(820,144));button.targetGraphic=paint;
                    var label=UnityEngine.Object.Instantiate(sourcePanel.Find("VibracaoNome").gameObject,row,false).GetComponent<TextMeshProUGUI>();label.name="Nome";label.text=Labels[i];Place(label.rectTransform,new Vector2(49,0),new Vector2(610,110));
                    var icon=AssetDatabase.LoadAssetAtPath<Sprite>(Icons[i]);Check(icon!=null,"Existing icon missing: "+Icons[i]);Picture(row,"Icone",icon,new Vector2(-316,0),new Vector2(90,94));
                    var entry=entries.GetArrayElementAtIndex(i);entry.FindPropertyRelative("destination").enumValueIndex=i;entry.FindPropertyRelative("button").objectReferenceValue=button;
                }
                panel.Find("FecharArte").SetAsLastSibling();panel.Find("Fechar").SetAsLastSibling();
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);AssetDatabase.SaveAssets();
                var saved=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);AssetDatabase.TryGetGUIDAndLocalFileIdentifier(saved,out string guid,out long id);
                var info=new Info{prefabGuid=guid,rootFileId=id.ToString(),frameTextureGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(frame.texture)),headerGuid=AssetDatabase.AssetPathToGUID(Art+"postal-header.asset"),bodyGuid=AssetDatabase.AssetPathToGUID(Art+"postal-body.asset"),newPngCount=0};
                File.WriteAllText(Output+"/import.json",JsonUtility.ToJson(info,true));
                File.WriteAllText(Output+"/build.txt","PASS: cloned V161 settings view; same dimmer, fitter, title typography, close, capsule art and press controls; existing individual route icons; two native Sprite regions sharing the original frame PNG; no new PNG or duplicate textures.\n");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            MenuV162Check.Run();
        }
        catch(Exception e){File.WriteAllText(Output+"/build.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
