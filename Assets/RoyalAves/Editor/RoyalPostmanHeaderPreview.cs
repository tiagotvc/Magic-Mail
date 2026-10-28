using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using RoyalAves.UI.RoyalPostman2026;

namespace RoyalAves.EditorTools
{
    /// <summary>Read-only native rendering of the actual Pets header, without changing the open scene.</summary>
    [InitializeOnLoad]
    public sealed class RoyalPostmanHeaderPreview : EditorWindow
    {
        const string Request="Library/RoyalPostmanHeaderPreview.open";
        const string Receipt="Library/RoyalPostmanHeaderPreview.opened";
        RoyalPostmanScreenDocument document;
        int language;
        string error;
        static RoyalPostmanHeaderPreview(){EditorApplication.update+=OpenRequestedPreview;}
        static void OpenRequestedPreview()
        {
            // The request is created only for the user's explicit "show it in Unity" action.
            if(Application.isBatchMode||!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(RoyalPostmanNativeAuthoring.PetsPath);
            if(source==null||source.transform.Find("Header/Title")==null)return;
            File.Delete(Request);
            var window=Open();
            if(window.error==null)window.SaveHeaderCaptures();
            File.WriteAllText(Receipt,window.error??"OPEN: actual Pets.prefab header; read-only native PT/EN preview; user's scene and Play state preserved.");
        }
        [MenuItem("Royal Postman/Prévia do cabeçalho PT-EN",false,11)]
        public static RoyalPostmanHeaderPreview Open()
        {
            var window=GetWindow<RoyalPostmanHeaderPreview>(true,"Cabeçalho · PT / EN",true);
            window.minSize=new Vector2(700,230);window.Reload();window.Show();window.Focus();return window;
        }
        void OnEnable(){EditorApplication.projectChanged+=Reload;}
        void OnDisable(){EditorApplication.projectChanged-=Reload;document?.Dispose();document=null;}
        void Reload()
        {
            document?.Dispose();document=null;error=null;
            try{document=new RoyalPostmanScreenDocument(RoyalPostmanRuntimeKit.Load(),"pets",1440,2560);ApplyLanguage();}
            catch(Exception e){error=e.Message;}
            Repaint();
        }
        void ApplyLanguage()
        {
            var title=document?.Root.GetComponentInChildren<RoyalPostmanHeaderTitle>(true);
            if(title==null)return;
            title.editorLanguage=language==0?RoyalPostmanHeaderTitle.EditorLanguage.Portuguese:RoyalPostmanHeaderTitle.EditorLanguage.English;
            title.Refresh();document.Render();
        }
        void SaveHeaderCaptures()
        {
            const string folder="Library/MapValidation/header-v86";
            Directory.CreateDirectory(folder);int original=language;
            for(language=0;language<2;language++)
            {
                ApplyLanguage();var bounds=document.Bounds((RectTransform)document.Root.transform.Find("Header"));
                int bottom=Mathf.FloorToInt(bounds.yMin),height=document.Height-bottom;
                var prior=RenderTexture.active;var capture=new Texture2D(document.Width,height,TextureFormat.RGBA32,false);
                try{RenderTexture.active=document.Texture;capture.ReadPixels(new Rect(0,bottom,document.Width,height),0,0);capture.Apply();File.WriteAllBytes(folder+"/main-header-"+(language==0?"pt":"en")+".png",capture.EncodeToPNG());}
                finally{RenderTexture.active=prior;DestroyImmediate(capture);}
            }
            language=original;ApplyLanguage();
        }
        void OnGUI()
        {
            using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();language=GUILayout.Toolbar(language,new[]{"Português","English"},EditorStyles.toolbarButton,GUILayout.Width(220));
                if(EditorGUI.EndChangeCheck())ApplyLanguage();
                GUILayout.FlexibleSpace();
                if(GUILayout.Button("Recarregar",EditorStyles.toolbarButton,GUILayout.Width(90)))Reload();
                if(GUILayout.Button("Editar na Unity",EditorStyles.toolbarButton,GUILayout.Width(115)))RoyalPostmanNativeAuthoring.OpenPets();
            }
            if(error!=null){EditorGUILayout.HelpBox(error,MessageType.Error);return;}
            if(document==null){Reload();return;}
            var header=(RectTransform)document.Root.transform.Find("Header");
            var bounds=document.Bounds(header);float width=position.width-24,height=width*bounds.height/bounds.width;
            var area=GUILayoutUtility.GetRect(width,height,GUILayout.ExpandWidth(true));area.x+=12;area.width=width;
            var uv=new Rect(bounds.xMin/document.Width,bounds.yMin/document.Height,bounds.width/document.Width,bounds.height/document.Height);
            GUI.DrawTextureWithTexCoords(area,document.Texture,uv);
            EditorGUILayout.LabelField("Pets.prefab · render nativo do header aplicado",EditorStyles.centeredGreyMiniLabel);
        }
    }
}
