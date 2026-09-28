// Applies the Magic Mail look to Sweet Sugar's title scene (main.unity) and makes it the first scene of the app: a plain
// dark background with the pile of letters and parcels at the bottom, the official logo (pulsing slightly, with Sweet
// Sugar's glow behind it) and the wax-seal Play button, 30% smaller, which shows its pressed version when tapped. The
// manager and Sweet Sugar's "Candy SMITH" badge are hidden.
// Sweet Sugar's loading overlay (Resources/Loading and its copies in main) shows the loading art with the logo and only
// the animated "Carregando...". The loading scene stays in the build list, turned off, for the mobile build.
// Menu: Royal Aves > Aplicar visual da tela inicial.
using System.Collections.Generic;
using System.Linq;
using RoyalAves.Meta;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesTitleBuilder
    {
        const string ScenePath = "Assets/SweetSugar/Scenes/main.unity";
        const string LoadingScenePath = "Assets/RoyalAves/Scenes/Loading.unity";
        const string TransitionPrefabPath = "Assets/SweetSugar/Resources/Loading.prefab";
        const string ArtRoot = "Assets/RoyalAves/Art/";
        const string LoadingArtName = "LoadingArt";

        // Plain, sober background (dark navy); the logo and the pile bring the colour.
        static readonly Color BackgroundColor = new Color32(0x1C, 0x2A, 0x4A, 0xFF);
        // The wax seal was 586 units; 30% smaller.
        static readonly Vector2 PlaySize = new Vector2(410, 410);

        [MenuItem("Royal Aves/Aplicar visual da tela inicial")]
        static void ApplyFromMenu()
        {
            if (!RoyalAvesTools.CanRunTool()) return;
            RoyalAvesReskin.EnsureImportSettings();
            var loadingArt = LoadSprite("Backgrounds/loading");
            var pile = LoadSprite("UI/Title/decor-pile"); // pilhas-de-cartas-pacotes
            var logo = LoadSprite("Branding/logo-magic-mail");
            var play = LoadSprite("UI/Title/play-seal");
            var playPressed = LoadSprite("UI/Title/play-seal-pressed");
            if (new[] { loadingArt, pile, logo, play, playPressed }.Any(s => s == null))
            {
                EditorUtility.DisplayDialog("Tela inicial", "Faltam imagens em Assets/RoyalAves/Art (loading, decor-pile, logo, play-seal ou play-seal-pressed).", "OK");
                return;
            }

            var log = new List<string>();

            // The loading overlay prefab first, so its copy in main already has the art when the scene opens.
            var transition = PrefabUtility.LoadPrefabContents(TransitionPrefabPath);
            try
            {
                if (ApplyLoadingArt(transition.transform, loadingArt))
                {
                    PrefabUtility.SaveAsPrefabAsset(transition, TransitionPrefabPath);
                    log.Add("loading entre cenas: arte com logo e \"Carregando...\"");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(transition);
            }

            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var rects = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RectTransform>(true)).ToList();
                RectTransform Find(string path) => rects.FirstOrDefault(t => PathOf(t) == path);

                // Background: one plain colour covering the screen.
                if (Find("CanvasBack/Image")?.GetComponent<Image>() is Image background)
                {
                    background.sprite = null;
                    background.color = BackgroundColor;
                    Record(background);
                    log.Add("fundo de cor única");
                }

                // The pile of letters and parcels, once, at the bottom centre.
                if (Find("CanvasBack/main_menu_part_1") is RectTransform pileRect)
                {
                    SetSprite(pileRect, pile, true);
                    pileRect.anchorMin = pileRect.anchorMax = new Vector2(0.5f, 0);
                    pileRect.pivot = new Vector2(0.5f, 0);
                    pileRect.anchoredPosition = new Vector2(0, -40);
                    pileRect.sizeDelta = new Vector2(700, 700);
                    Record(pileRect);
                    log.Add("pilha de cartas e pacotes embaixo");
                }
                Hide(Find("CanvasBack/main_menu_part_2"));
                if (Hide(Find("CanvasBack/main_menu_part_3"))) log.Add("selo Candy SMITH escondido");

                foreach (var orientation in new[] { "OrientationHandler/Vertical", "OrientationHandler/Horrizontal" })
                {
                    var portrait = orientation.EndsWith("Vertical");
                    var side = portrait ? "em pé" : "deitado";
                    if (Hide(Find(orientation + "/Gerente"))) log.Add($"gerente escondida ({side})");

                    // The logo, pulsing slightly; Sweet Sugar's own logo animation is turned off so it does not fight the pulse.
                    if (Find(orientation + "/Logo") is RectTransform logoRect)
                    {
                        Show(logoRect);
                        SetSprite(logoRect, logo, true);
                        logoRect.sizeDelta = new Vector2(780, 780);
                        if (portrait) logoRect.anchoredPosition = new Vector2(logoRect.anchoredPosition.x, 300);
                        Record(logoRect);
                        foreach (var animator in logoRect.GetComponents<Animator>())
                        {
                            animator.enabled = false;
                            Record(animator);
                        }
                        if (logoRect.GetComponent<LogoPulse>() == null) logoRect.gameObject.AddComponent<LogoPulse>();
                        log.Add($"logo pulsando ({side})");
                    }
                    // The glow behind the logo stays.
                    if (Find(orientation + "/white_wheel") is RectTransform wheel)
                    {
                        Show(wheel);
                        if (portrait) wheel.anchoredPosition = new Vector2(wheel.anchoredPosition.x, 300);
                        Record(wheel);
                    }

                    // Smaller Play; the pressed seal shows while it is held.
                    if (Find(orientation + "/Play") is RectTransform playRect)
                    {
                        SetSprite(playRect, play, true);
                        playRect.sizeDelta = PlaySize;
                        Record(playRect);
                        if (playRect.GetComponent<Button>() is Button button)
                        {
                            button.transition = Selectable.Transition.SpriteSwap;
                            var state = button.spriteState;
                            state.pressedSprite = playPressed;
                            state.highlightedSprite = null;
                            state.selectedSprite = null;
                            button.spriteState = state;
                            Record(button);
                        }
                        log.Add($"botão Play menor, com clique ({side})");
                    }
                }

                // Loading overlays inside main (one comes from the prefab above, the landscape one is a plain copy).
                foreach (var loading in rects.Where(t => t.name == "Loading"))
                    if (ApplyLoadingArt(loading, loadingArt)) log.Add("loading: " + PathOf(loading));

                foreach (var text in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TextMeshProUGUI>(true)))
                    if (Translate(text)) log.Add("texto Carregando");

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (setup.Length > 0 && setup.All(s => !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            // main opens the app; the loading scene with the logo stays in the list, off, for the mobile build.
            var scenes = EditorBuildSettings.scenes.ToList();
            var main = scenes.FirstOrDefault(s => s.path == ScenePath) ?? new EditorBuildSettingsScene(ScenePath, true);
            main.enabled = true;
            scenes.Remove(main);
            scenes.Insert(0, main);
            foreach (var s in scenes.Where(s => s.path == LoadingScenePath)) s.enabled = false;
            EditorBuildSettings.scenes = scenes.ToArray();
            log.Add("main é a primeira cena do app (loading com logo guardado e desligado)");

            EditorUtility.DisplayDialog("Tela inicial", "Aplicado:\n- " + string.Join("\n- ", log.Distinct()) +
                "\n\nA cor do fundo fica no objeto CanvasBack/Image; o pulso do logo, no componente LogoPulse do Logo.", "OK");
        }

        // Sweet Sugar's loading overlay: the loading art (with the logo) covering it, the character off and only the
        // animated "Carregando..." at the bottom, in the postal font.
        static bool ApplyLoadingArt(Transform loading, Sprite art)
        {
            var text = loading.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault();
            if (text == null) return false;

            var rect = loading.Find(LoadingArtName) as RectTransform;
            if (rect == null)
            {
                var go = new GameObject(LoadingArtName, typeof(RectTransform)) { layer = loading.gameObject.layer };
                rect = (RectTransform)go.transform;
                rect.SetParent(loading, false);
            }
            rect.SetAsFirstSibling();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            var image = rect.GetComponent<Image>();
            if (image == null) image = rect.gameObject.AddComponent<Image>();
            image.sprite = art;
            image.color = Color.white;
            image.preserveAspect = false;
            image.raycastTarget = false;
            // The overlay is a 5000 x 5000 square: fitting the art to it zoomed the art far past the screen. The first
            // version of this tool did that; LoadingOverlayLayout sizes the art to the screen instead.
            var fitter = rect.GetComponent<AspectRatioFitter>();
            if (fitter != null && !PrefabUtility.IsPartOfPrefabInstance(fitter)) Object.DestroyImmediate(fitter, true);
            else if (fitter != null) fitter.enabled = false;
            Record(rect);
            Record(image);

            foreach (var character in new[] { "Gerente", "character_main" })
                Hide(loading.Find(character));

            var label = text.rectTransform;
            label.SetAsLastSibling();
            label.anchorMin = new Vector2(0.1f, 0.05f);
            label.anchorMax = new Vector2(0.9f, 0.11f);
            label.offsetMin = label.offsetMax = Vector2.zero;
            RoyalAvesFonts.ApplyPostal(text);
            text.text = "Carregando...";
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18;
            text.fontSizeMax = 90;
            // The dots animate the text; Sweet Sugar's localization would put "Loading" back.
            foreach (var localize in text.GetComponents<MonoBehaviour>().Where(c => c != null && c.GetType().Name == "LocalizeText"))
            {
                localize.enabled = false;
                Record(localize);
            }
            if (text.GetComponent<LoadingDots>() == null) text.gameObject.AddComponent<LoadingDots>();
            Record(label);
            Record(text);

            // Art over the whole screen and the text near its bottom, measured on the canvas at run time.
            var layout = loading.GetComponent<LoadingOverlayLayout>();
            if (layout == null) layout = loading.gameObject.AddComponent<LoadingOverlayLayout>();
            var serialized = new SerializedObject(layout);
            serialized.FindProperty("art").objectReferenceValue = rect;
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Record(layout);
            return true;
        }

        static bool Hide(Transform target)
        {
            if (target == null || !target.gameObject.activeSelf) return false;
            target.gameObject.SetActive(false);
            Record(target.gameObject);
            return true;
        }

        static void Show(Transform target)
        {
            if (target.gameObject.activeSelf) return;
            target.gameObject.SetActive(true);
            Record(target.gameObject);
        }

        static void SetSprite(RectTransform rect, Sprite sprite, bool preserveAspect)
        {
            var image = rect.GetComponent<Image>();
            if (image == null) return;
            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            Record(image);
        }

        static bool Translate(TextMeshProUGUI text)
        {
            if (!text.text.StartsWith("Loading")) return false;
            text.text = "Carregando...";
            Record(text);
            return true;
        }

        static void Record(Object target)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }

        static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (; t != null; t = t.parent) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }

        static Sprite LoadSprite(string relativePathWithoutExtension) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + relativePathWithoutExtension + ".png");
    }
}
