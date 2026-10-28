using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RoyalAves.EditorTools;
using RoyalAves.UI.RoyalPostman2026;

public static class HeaderV86Build
{
    public const string Prefab = "Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab";
    public const string Art = "Assets/RoyalAves/Art/UI/Menu/RoyalPostman/HeaderV86/";
    public const string Output = "../header-v86";
    public const string Reusable = "Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Components/HatcheryHeaderV86.prefab";
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    static void Rect(RectTransform rect,Vector2 anchor,Vector2 position,Vector2 size)
    {
        rect.anchorMin=rect.anchorMax=anchor;rect.pivot=Vector2.one*.5f;
        rect.anchoredPosition=position;rect.sizeDelta=size;rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;
    }
    static Image Image(Transform parent,string name)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);
        var image=go.GetComponent<Image>();image.color=Color.white;image.raycastTarget=false;return image;
    }
    static Sprite Sprite(string name,bool sliced)
    {
        string file=Art+name+".png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(file);
        importer.textureType=TextureImporterType.Default;importer.isReadable=true;importer.alphaIsTransparency=true;
        importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=8192;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.crunchedCompression=false;
        importer.mipmapEnabled=true;importer.mipmapFilter=TextureImporterMipFilter.BoxFilter;
        importer.filterMode=FilterMode.Trilinear;importer.wrapMode=TextureWrapMode.Clamp;
        foreach(var p in new[]{"Standalone","Android","iPhone","WebGL"}) importer.ClearPlatformTextureSettings(p);
        importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(file);var pixels=texture.GetPixels32();
        int left=texture.width,bottom=texture.height,right=-1,top=-1;
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>=160)
        {left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
        Check(right>left&&top>bottom,"Empty generated source: "+name);
        // Native Sprite metadata crops transparent padding; original PNG pixels are never rewritten.
        var region=new Rect(left,bottom,right-left+1,top-bottom+1);
        var border=sliced?new Vector4(16,64,16,12):Vector4.zero;
        var sprite=UnityEngine.Sprite.Create(texture,region,Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border);
        sprite.name=name;Directory.CreateDirectory(Art+"Sprites");AssetDatabase.Refresh();
        string target=Art+"Sprites/"+name+".asset";
        var existing=AssetDatabase.LoadAssetAtPath<Sprite>(target);
        if(existing==null) AssetDatabase.CreateAsset(sprite,target);
        else {EditorUtility.CopySerialized(sprite,existing);UnityEngine.Object.DestroyImmediate(sprite);sprite=existing;}
        File.AppendAllText(Output+"/sprites.txt",name+" source="+texture.width+"x"+texture.height+" crop="+region+" border="+border+"\n");
        return sprite;
    }
    static RectTransform Copy(Transform source,Transform parent,string name)
    {
        var copy=UnityEngine.Object.Instantiate(source.gameObject,parent,false);copy.name=name;return (RectTransform)copy.transform;
    }
    static void AssembleButtons(Transform header,RoyalPostmanPetsScreen view)
    {
        var close=view.helpButton;var back=view.backButton;
        var moving=(RectTransform)close.transform.Find("MovingCenter");
        var oldRim=moving.Find("rounded_total_close_border");var face=moving.Find("rounded_total_close_center");
        var oldIcon=close.transform.Find("icon");Check(oldRim!=null&&face!=null&&oldIcon!=null,"User close painting missing");
        var rim=Copy(oldRim,close.transform,"FixedGold");rim.SetAsFirstSibling();oldRim.gameObject.SetActive(false);
        var icon=Copy(oldIcon,moving,"Icon");oldIcon.gameObject.SetActive(false);
        // The user's original Image sprites, face proportions and icon painting stay intact.
        foreach(var graphic in close.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
        var hit=close.GetComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;close.targetGraphic=hit;
        close.transition=Selectable.Transition.None;close.interactable=true;
        var press=close.GetComponent<RoyalPostmanPaintedButton>();press.movingCenter=moving;press.pressTravel=3;press.pressScale=.94f;
        foreach(Transform child in back.transform)child.gameObject.SetActive(false);
        var backRim=Copy(rim,back.transform,"FixedGold");var backMoving=Copy(moving,back.transform,"PaintedCenter");
        var arrow=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/UI/UI assets/seta_voltar_creme_contorno.png");
        Check(arrow!=null,"Existing matching arrow PNG sprite missing");
        backMoving.Find("Icon").GetComponent<Image>().sprite=arrow;
        foreach(var graphic in back.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
        var backHit=back.GetComponent<Image>();backHit.color=Color.clear;backHit.raycastTarget=true;back.targetGraphic=backHit;
        back.transition=Selectable.Transition.None;back.interactable=true;
        var backPress=back.GetComponent<RoyalPostmanPaintedButton>();backPress.movingCenter=backMoving;backPress.pressTravel=3;backPress.pressScale=.94f;
        foreach(var button in new[]{back,close})
        {
            button.gameObject.SetActive(true);
            Rect((RectTransform)button.transform,Vector2.one*.5f,new Vector2(button==back?-438:438,5),new Vector2(128,128));
        }
    }
    static void Capture(string suffix)
    {
        using(var d=new RoyalPostmanScreenDocument(RoyalPostmanRuntimeKit.Load(),"pets",720,1280))
        {
            var title=d.Root.GetComponentInChildren<RoyalPostmanHeaderTitle>(true);
            d.Root.GetComponent<RoyalPostmanPetsScreen>().ShowCurrent(false);
            foreach(var english in new[]{false,true})
            {
                title.editorLanguage=english?RoyalPostmanHeaderTitle.EditorLanguage.English:RoyalPostmanHeaderTitle.EditorLanguage.Portuguese;title.Refresh();
                string language=english?"en":"pt";d.Export(Output+"/unity-"+language+suffix+".png");
                var prior=RenderTexture.active;var crop=new Texture2D(720,150,TextureFormat.RGBA32,false);
                try{RenderTexture.active=d.Texture;crop.ReadPixels(new Rect(0,1130,720,150),0,0);crop.Apply();File.WriteAllBytes(Output+"/header-"+language+suffix+".png",crop.EncodeToPNG());}
                finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(crop);}
            }
        }
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output);File.WriteAllText(Output+"/sprites.txt","");
        try
        {
            AssetDatabase.Refresh();var background=Sprite("background",true);var pt=Sprite("title-pt",false);var en=Sprite("title-en",false);
            var root=PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var header=(RectTransform)root.transform.Find("Header");
                Rect(header,new Vector2(.5f,1),new Vector2(0,-100),new Vector2(1080,200));
                var paint=Image(header,"Background");paint.sprite=background;paint.type=UnityEngine.UI.Image.Type.Sliced;paint.pixelsPerUnitMultiplier=2;
                Rect(paint.rectTransform,Vector2.one*.5f,Vector2.zero,new Vector2(1080,200));paint.transform.SetAsFirstSibling();
                var title=Image(header,"Title");Rect(title.rectTransform,Vector2.one*.5f,new Vector2(0,5),new Vector2(500,110));
                var localized=title.gameObject.AddComponent<RoyalPostmanHeaderTitle>();localized.portuguese=pt;localized.english=en;localized.titleHeight=110;localized.maximumWidth=640;localized.Refresh();
                AssembleButtons(header,root.GetComponent<RoyalPostmanPetsScreen>());
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);
                Directory.CreateDirectory(Path.GetDirectoryName(Reusable));AssetDatabase.Refresh();
                var component=UnityEngine.Object.Instantiate(header.gameObject);component.name="HatcheryHeaderV86";
                try{PrefabUtility.SaveAsPrefabAsset(component,Reusable);}finally{UnityEngine.Object.DestroyImmediate(component);}
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();File.Copy(Prefab,Output+"/assembled.prefab",true);Capture("-built");
            File.WriteAllText(Output+"/build.txt","PASS: one combined red/gold sliced Sprite; native reusable header; separate PT/EN painted titles; original user close PNG layers reused for close/back.\n");
            EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Output+"/build.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void Review()
    {
        Directory.CreateDirectory(Output);
        try
        {
            AssetDatabase.Refresh();Capture("");
            using(var d=new RoyalPostmanScreenDocument(RoyalPostmanRuntimeKit.Load(),"pets",720,1280))
            {
                int callbacks=0;var view=d.Root.GetComponent<RoyalPostmanPetsScreen>();view.Initialize(()=>callbacks++,i=>{});view.ShowCurrent(false);
                var title=d.Root.GetComponentInChildren<RoyalPostmanHeaderTitle>(true);Check(title.portuguese!=null&&title.english!=null,"Missing bilingual title");
                title.editorLanguage=RoyalPostmanHeaderTitle.EditorLanguage.GameLanguage;
                foreach(var code in new[]{"Portuguese (Brazil)","English"})
                {
                    SweetSugar.Scripts.Level.CrosssceneData.selectedLanguage=code;title.Refresh();
                    Check(title.GetComponent<Image>().sprite==(code.StartsWith("Portuguese")?title.portuguese:title.english),"Game-language selection mismatch");
                }
                var events=new GameObject("HeaderReviewEvents",typeof(EventSystem));
                try
                {
                    foreach(var button in new[]{view.backButton,view.helpButton})
                    {
                        Check(button.gameObject.activeInHierarchy&&button.IsInteractable(),"Header control inactive");
                        var pos=d.Camera.WorldToScreenPoint(button.transform.position);
                        var data=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=pos};
                        var hits=new System.Collections.Generic.List<RaycastResult>();d.Root.GetComponentInParent<GraphicRaycaster>().Raycast(data,hits);
                        Check(hits.Exists(h=>h.gameObject==button.gameObject),"Header control raycast missing");
                        var press=button.GetComponent<RoyalPostmanPaintedButton>();var rest=press.movingCenter.anchoredPosition;
                        var rim=button.transform.Find("FixedGold");var fixedPosition=rim.position;var fixedScale=rim.localScale;
                        press.OnPointerDown(data);Check(press.movingCenter.anchoredPosition!=rest,"Center did not press");
                        Check(rim.position==fixedPosition&&rim.localScale==fixedScale,"Gold rim moved with face");
                        press.OnPointerUp(data);Check(press.movingCenter.anchoredPosition==rest,"Face did not restore");
                        button.onClick.Invoke();
                    }
                    Check(callbacks==2,"Back and close callbacks changed");
                }
                finally{UnityEngine.Object.DestroyImmediate(events);}
                var paint=d.Root.transform.Find("Header/Background").GetComponent<Image>();
                Check(paint.type==UnityEngine.UI.Image.Type.Sliced&&paint.sprite.border.y>0,"Header is not a reusable sliced combined sprite");
                title.editorLanguage=RoyalPostmanHeaderTitle.EditorLanguage.Portuguese;title.Refresh();
                d.Resize(390,844);d.Root.GetComponent<RoyalPostmanViewport>().SafeAreaOverride=new Rect(0,34,390,770);d.Render();
                foreach(var button in new[]{view.backButton,view.helpButton}){var b=d.Bounds((RectTransform)button.transform);Check(b.xMin>=0&&b.xMax<=390&&b.yMin>=34&&b.yMax<=804,"Header control outside safe area");}
                d.Export(Output+"/unity-pt-390-safe.png");
            }
            File.WriteAllText(Output+"/review.txt","PASS: native PT/EN renders, live game-language switch, exact user PNG controls, both callbacks, native raycasts, independent fixed rim/moving face press+release, combined sliced background,720x1280 and390x844/notch safe area.\n");
            EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Output+"/review.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
