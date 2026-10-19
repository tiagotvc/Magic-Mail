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

            EnsureSeasonUi(loading);
            var config = AssetDatabase.LoadAssetAtPath<SeasonConfig>(SeasonConfigPath);
            if (config != null && config.Active != null) loading.ApplySeason(config.Active);
        }

        // ---- season: name, subtitle, bar and shade ----------------------------------------------------------------------

        public const string SeasonConfigPath = "Assets/RoyalAves/Resources/RoyalAvesSeasons.asset";
        const string ScrimPath = "Assets/RoyalAves/Art/UI/Generated/loading-scrim.png";

        // Puts the loading screen in step with the active season: writes it into the Loading scene and saves, so the scene
        // itself shows it (at run time LoadingScreen applies the active season again, so both always agree). Returns an
        // error message, or null when it worked.
        public static string ApplySeasonToScene(Season season)
        {
            if (!RoyalAvesTools.CanRunTool()) return "Cancelado.";
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var loading = Object.FindFirstObjectByType<LoadingScreen>();
                if (loading == null) return "A cena Loading não tem o componente LoadingScreen (menu Royal Aves > Criar tela de loading).";
                EnsureSeasonUi(loading);
                loading.ApplySeason(season);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                return null;
            }
            catch (System.Exception e)
            {
                return "Não consegui abrir/salvar " + ScenePath + ": " + e.Message;
            }
            finally
            {
                if (setup.Length > 0 && setup.All(s => !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        // Creates, only where missing, what a season needs on the loading screen — the shade, the name, the subtitle and the
        // progress bar — and wires it into LoadingScreen. Safe to run again: what exists is kept as the user left it. The
        // first time, "Carregando..." moves down to make room.
        public static void EnsureSeasonUi(LoadingScreen loading)
        {
            var root = (RectTransform)loading.transform;
            var firstTime = root.Find("SeasonTitle") == null;
            var so = new SerializedObject(loading);

            // The picture behind everything and the separate logo over it. The builder names them "Art" and none; the scene as
            // it was edited by hand calls them "title" and "logo-magic-mail", so both spellings are understood.
            var art = root.Find("Art") ?? root.Find("title");
            if (art == null)
                art = Enumerable.Range(0, root.childCount).Select(root.GetChild).FirstOrDefault(c => c.GetComponent<Image>() != null);
            if (art != null) so.FindProperty("art").objectReferenceValue = art.GetComponent<Image>();
            var logo = root.Find("logo-magic-mail") ?? root.Find("Logo");
            if (logo != null) so.FindProperty("logo").objectReferenceValue = logo.gameObject;

            var scrim = root.Find("Scrim") as RectTransform;
            if (scrim == null)
            {
                scrim = Child("Scrim", root);
                scrim.anchorMin = Vector2.zero;
                scrim.anchorMax = new Vector2(1f, 0.5f);
                scrim.offsetMin = scrim.offsetMax = Vector2.zero;
                var shade = scrim.gameObject.AddComponent<Image>();
                shade.sprite = EnsureScrimSprite();
                shade.raycastTarget = false;
            }
            so.FindProperty("scrim").objectReferenceValue = scrim.GetComponent<Image>();

            var title = EnsureText(root, "SeasonTitle", new Vector2(0, 325), new Vector2(960, 130), t =>
            {
                RoyalAvesFonts.ApplyPostal(t);               // the game's gold lettering with its outline
                t.fontStyle = FontStyles.UpperCase;
                t.enableAutoSizing = true;
                t.fontSizeMin = 40;
                t.fontSizeMax = 96;
            });
            var subtitle = EnsureText(root, "SeasonSubtitle", new Vector2(0, 250), new Vector2(960, 64), t =>
            {
                t.font = RoyalAvesFonts.Display;
                t.color = new Color32(0xFF, 0xF1, 0xD0, 0xFF);
                t.enableAutoSizing = true;
                t.fontSizeMin = 24;
                t.fontSizeMax = 44;
            });
            so.FindProperty("seasonTitle").objectReferenceValue = title;
            so.FindProperty("seasonSubtitle").objectReferenceValue = subtitle;

            // The progress bar is the reusable Magic Mail component (prefab "Barra de progresso", with its card and sparkles).
            // A simple bar from an earlier version is swapped for it, keeping its place on the screen.
            var bar = root.Find("Bar") as RectTransform;
            var barPrefab = MagicMail.EditorTools.MagicProgressBarBuilder.EnsurePrefab();
            if (barPrefab != null && (bar == null || bar.GetComponent<MagicMail.MagicProgressBar>() == null))
            {
                // The new bar is taller than the old one: the lettering above it and "Carregando..." below move to make room.
                if (root.Find("SeasonTitle") is RectTransform t) t.anchoredPosition = new Vector2(0, 325);
                if (root.Find("SeasonSubtitle") is RectTransform st) st.anchoredPosition = new Vector2(0, 250);
                if (root.Find("Label") is RectTransform lb) lb.anchoredPosition = new Vector2(0, 70);
                var place = new Vector2(0, 165);
                if (bar != null) Object.DestroyImmediate(bar.gameObject);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(barPrefab, root);
                instance.name = "Bar";
                bar = (RectTransform)instance.transform;
                bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0f);
                bar.pivot = new Vector2(0.5f, 0.5f);
                bar.sizeDelta = new Vector2(880f, 880f * 107f / 1108f);
                bar.anchoredPosition = place;
                bar.localScale = Vector3.one;
            }
            else if (bar == null)   // the prefab could not be built: a plain bar so the screen still has progress
            {
                bar = Child("Bar", root);
                bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0f);
                bar.anchoredPosition = new Vector2(0, 175);
                bar.sizeDelta = new Vector2(820, 38);
                Sliced(bar, "bar-bg");
                var fill = Child("Fill", bar);
                fill.anchorMin = Vector2.zero;
                fill.anchorMax = new Vector2(0f, 1f);       // LoadingScreen grows its right edge with the progress
                fill.offsetMin = fill.offsetMax = Vector2.zero;
                Sliced(fill, "bar-fill");
            }
            var magicBar = bar.GetComponent<MagicMail.MagicProgressBar>();
            so.FindProperty("progressBar").objectReferenceValue = magicBar;
            so.FindProperty("barFill").objectReferenceValue = magicBar != null ? null : bar.Find("Fill");
            if (magicBar != null) magicBar.ApplyPose(2.2f, 0.6f);   // a still frame, so the scene shows the bar laid out

            if (firstTime && root.Find("Label") is RectTransform label)
            {
                label.anchorMin = label.anchorMax = new Vector2(0.5f, 0f);
                label.anchoredPosition = new Vector2(0, 70);
            }

            // The groups the season's props live in, and the component that fills them: one group right above the art (behind the
            // logo, the lettering and the bar), one over everything. The props themselves are made at run time from the season.
            var behindProps = EnsureGroup(root, "AdereçosFundo");
            var frontProps = EnsureGroup(root, "AdereçosFrente");
            var host = root.GetComponent<SeasonProps>();
            if (host == null) host = root.gameObject.AddComponent<SeasonProps>();
            var hostSo = new SerializedObject(host);
            hostSo.FindProperty("screen").enumValueIndex = (int)SeasonScreen.Loading;
            hostSo.FindProperty("behind").objectReferenceValue = behindProps;
            hostSo.FindProperty("front").objectReferenceValue = frontProps;
            hostSo.ApplyModifiedPropertiesWithoutUndo();

            // Back to front: art, props behind, shade, logo, name, subtitle, bar, "Carregando...", props in front.
            var order = 0;
            foreach (var child in new[] { art, behindProps, scrim, logo, title.transform, subtitle.transform, bar, root.Find("Label"), frontProps })
                if (child != null) child.SetSiblingIndex(order++);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // A group covering the whole screen, for a layer of props.
        static RectTransform EnsureGroup(RectTransform root, string name)
        {
            var group = root.Find(name) as RectTransform;
            if (group != null) return group;
            group = Child(name, root);
            group.anchorMin = Vector2.zero;
            group.anchorMax = Vector2.one;
            group.offsetMin = group.offsetMax = Vector2.zero;
            return group;
        }

        static TextMeshProUGUI EnsureText(RectTransform root, string name, Vector2 position, Vector2 size, System.Action<TextMeshProUGUI> style)
        {
            var existing = root.Find(name);
            if (existing != null) return existing.GetComponent<TextMeshProUGUI>();
            var rect = Child(name, root);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = RoyalAvesFonts.Display;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.text = "";
            style(text);
            return text;
        }

        // A dark-to-clear fade for the bottom of the art, so the name and the bar read over any picture.
        public static Sprite EnsureScrimSprite()
        {
            var disk = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, ScrimPath);
            if (!System.IO.File.Exists(disk))
            {
                const int height = 256;
                var pixels = new Color32[4 * height];
                for (var y = 0; y < height; y++)
                {
                    var alpha = Mathf.Pow(1f - y / (height - 1f), 1.7f) * 0.88f;   // row 0 is the bottom
                    for (var x = 0; x < 4; x++) pixels[y * 4 + x] = new Color32(20, 5, 56, (byte)Mathf.RoundToInt(alpha * 255f));
                }
                var tex = new Texture2D(4, height, TextureFormat.RGBA32, false);
                tex.SetPixels32(pixels);
                System.IO.File.WriteAllBytes(disk, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(ScrimPath, ImportAssetOptions.ForceUpdate);
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(ScrimPath);
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
