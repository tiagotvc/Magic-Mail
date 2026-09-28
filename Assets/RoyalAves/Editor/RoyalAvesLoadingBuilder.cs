// Builds the Magic Mail loading screen, the first scene of the app: the loading art, a progress bar and the
// LoadingScreen script, which opens the game scene (with the agency lobby). Also puts the scene first in Build Settings
// and uses the official logo as the app icon.
// Menu: Royal Aves > Criar tela de loading.
using System.Linq;
using RoyalAves.Meta;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesLoadingBuilder
    {
        const string ScenePath = "Assets/RoyalAves/Scenes/Loading.unity";
        const string ArtPath = "Assets/RoyalAves/Art/Backgrounds/loading.png";
        const string LogoPath = "Assets/RoyalAves/Art/Branding/logo-magic-mail.png";
        const float Width = 1080f;
        static float Cq(float value) => value * Width / 100f;

        [MenuItem("Royal Aves/Criar tela de loading")]
        static void BuildFromMenu()
        {
            if (!RoyalAvesTools.CanRunTool()) return;
            RoyalAvesReskin.EnsureImportSettings();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                BuildScene();
                if (!AssetDatabase.IsValidFolder("Assets/RoyalAves/Scenes")) AssetDatabase.CreateFolder("Assets/RoyalAves", "Scenes");
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally
            {
                if (setup.Length > 0 && setup.All(s => !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            // Loading first; the Sweet Sugar scenes stay in the build after it.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray();
            var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
            if (logo != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { logo }, IconKind.Any);

            EditorUtility.DisplayDialog("Tela de loading",
                "Cena criada em " + ScenePath + " e colocada como primeira do build.\n" +
                (logo != null ? "Logo oficial definido como ícone do app." : "Logo não encontrado; ícone não alterado.") +
                "\n\nPara testar, abra a cena Loading e aperte Play.", "OK");
        }

        static void BuildScene()
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0x0B, 0x1F, 0x3A, 0xFF);
            cameraObject.transform.position = new Vector3(0, 0, -10);

            var canvasObject = new GameObject("LoadingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.layer = LayerMask.NameToLayer("UI");
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Width, 1920);
            scaler.matchWidthOrHeight = 0;
            var root = (RectTransform)canvasObject.transform;

            // The art covers the screen whatever its proportion.
            var art = Child("Art", root);
            art.anchorMin = art.anchorMax = new Vector2(0.5f, 0.5f);
            var fitter = art.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = Width / 1920f;
            art.gameObject.AddComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);

            // Only the animated "Carregando..." at the bottom, over the art.
            var label = Child("Label", root);
            label.anchorMin = label.anchorMax = new Vector2(0.5f, 0);
            label.anchoredPosition = new Vector2(0, Cq(14));
            label.sizeDelta = new Vector2(Cq(80), Cq(8));
            var text = label.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = RoyalAvesFonts.Display;
            text.text = "Carregando...";
            text.fontSize = Cq(5.5f);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            RoyalAvesFonts.ApplyPostal(text);

            var loading = canvasObject.AddComponent<LoadingScreen>();
            var serialized = new SerializedObject(loading);
            serialized.FindProperty("label").objectReferenceValue = text;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static RectTransform Child(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        static void Sliced(RectTransform rect, string generatedSprite)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/UI/Generated/" + generatedSprite + ".png");
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
        }
    }
}
