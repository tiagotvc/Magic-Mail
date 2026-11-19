// Cria o prefab da tela provisória "Coming soon" (Resources/UI/ComingSoonScreen.prefab), usada pelas abas do menu
// inicial que ainda não têm tela (RoyalAves.Meta.HomeScreen abre sozinha; nada muda na cena nem no CanvasGlobal).
//
//   Royal Aves > Tela inicial > Em breve > Criar prefab da tela
//       cria o prefab se ele não existir (não mexe num prefab que já existe).
//   Royal Aves > Tela inicial > Em breve > Recriar prefab da tela (apaga edições)
//   Royal Aves > Tela inicial > Em breve > Abrir prefab da tela
//       para trocar o fundo, os textos e as cores no Inspector (componente Coming Soon Screen na raiz).
//
// Roda sozinho uma vez depois de compilar, se o prefab faltar. Depois o prefab é seu: não é sobrescrito.
using System.IO;
using System.Linq;
using RoyalAves.Meta;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    public static class ComingSoonScreenBuilder
    {
        const string Art = "Assets/Royal Postman Art/";
        const string BackgroundPath = Art + "Screens/Gameplay/Background/background_ready_01.png";
        const string ButtonPath = Art + "Buttons/squared/btn_blue.png";
        const string FontPath = "Assets/RoyalAves/Fonts/LilitaOne SDF.asset";
        const string Folder = Art + "Resources/UI";
        public const string PrefabPath = Folder + "/ComingSoonScreen.prefab";
        const string VersionKey = "RoyalAves.ComingSoonScreen.BuilderVersion";
        const int Version = 1;

        [InitializeOnLoadMethod]
        static void AutoBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                if (EditorPrefs.GetInt(VersionKey, 0) >= Version && File.Exists(PrefabPath)) return;
                try
                {
                    if (File.Exists(PrefabPath))
                    {
                        // Prefab já existe (veio do repositório ou foi editado): não sobrescreve, só marca como feito.
                        EditorPrefs.SetInt(VersionKey, Version);
                        return;
                    }
                    BuildPrefab();
                }
                catch (System.Exception e) { Debug.LogError("Royal Aves: não consegui criar o prefab da tela \"Coming soon\": " + e); }
            };
        }

        [MenuItem("Royal Aves/Tela inicial/Em breve/Criar prefab da tela")]
        static void CreateMenu()
        {
            if (File.Exists(PrefabPath))
            {
                Debug.Log("Royal Aves: o prefab da tela \"Coming soon\" já existe em " + PrefabPath + "; nada foi alterado.");
                Select();
                return;
            }
            BuildPrefab();
            Select();
        }

        [MenuItem("Royal Aves/Tela inicial/Em breve/Recriar prefab da tela (apaga edições)")]
        static void RebuildMenu()
        {
            if (File.Exists(PrefabPath) && !EditorUtility.DisplayDialog("Recriar prefab",
                    "Isto sobrescreve ComingSoonScreen.prefab e perde as edições feitas nele. Continuar?", "Recriar", "Cancelar")) return;
            BuildPrefab();
            Select();
        }

        [MenuItem("Royal Aves/Tela inicial/Em breve/Abrir prefab da tela")]
        static void OpenPrefab()
        {
            if (!File.Exists(PrefabPath)) BuildPrefab();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Selection.activeObject = prefab;
            AssetDatabase.OpenAsset(prefab);
        }

        static void Select() => Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        public static void BuildPrefab()
        {
            EnsureFolder(Folder);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var backgroundSprite = LoadSprite(BackgroundPath);
            var buttonSprite = LoadSprite(ButtonPath);
            if (backgroundSprite == null) Debug.LogWarning("Royal Aves: não achei o fundo " + BackgroundPath + "; a tela \"Coming soon\" fica com cor chapada até você escolher um fundo no prefab.");

            // Mesma hierarquia que o jogo monta por código quando o prefab falta; aqui entram a arte e a fonte.
            var screen = ComingSoonScreen.BuildDefault(null, font);
            var root = screen.gameObject;
            try
            {
                var so = new SerializedObject(screen);
                so.FindProperty("backgroundSprite").objectReferenceValue = backgroundSprite;
                so.ApplyModifiedPropertiesWithoutUndo();

                var button = so.FindProperty("backButton").objectReferenceValue as Button;
                if (button != null)
                {
                    var face = button.GetComponent<Image>();
                    if (face != null && buttonSprite != null)
                    {
                        face.sprite = buttonSprite;
                        face.color = Color.white;
                        face.preserveAspect = true;
                    }
                    // Mesmo efeito de clique dos botões da tela inicial (o botão inteiro encolhe).
                    var pop = button.gameObject.AddComponent<ButtonPopPress>();
                    var popSo = new SerializedObject(pop);
                    popSo.FindProperty("target").objectReferenceValue = button.transform;
                    popSo.ApplyModifiedPropertiesWithoutUndo();
                }

                screen.ApplyLook();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
            EditorPrefs.SetInt(VersionKey, Version);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Royal Aves: prefab da tela \"Coming soon\" criado em " + PrefabPath + ". As abas do menu inicial sem tela (Eventos, Coleção, Ranking e a 5ª aba) abrem essa tela. Para trocar fundo, textos e cores: Royal Aves > Tela inicial > Em breve > Abrir prefab da tela.");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// Primeiro sprite da textura (as artes do projeto são importadas como sprite único ou múltiplo de uma fatia só).
        static Sprite LoadSprite(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }
}
