using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoyalAves.Meta;
using SweetSugar.Scripts.GUI.Boost;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    // Changes presentation in place: BoostIcon, AnimationEventManager and all existing OnClick calls survive.
    public static class RoyalAvesSnowLevelStartBuilder
    {
        const string Art = "Assets/RoyalAves/Art/modal/";
        const string Prefab = "Assets/SweetSugar/Prefabs/CanvasGlobal.prefab";
        const string Game = "Assets/SweetSugar/Scenes/game.unity";
        const float ActionWidth = 448, ActionHeight = 208, ActionX = 234, ActionY = -468;
        static readonly Color Purple = new Color32(57, 7, 76, 255);
        static readonly Dictionary<string, Sprite> Pieces = new Dictionary<string, Sprite>();
        static readonly Dictionary<Sprite, Sprite> TrimmedIcons = new Dictionary<Sprite, Sprite>();

        [MenuItem("Royal Aves/Aplicar modal de nível roxa e dourada")]
        static void FromMenu()
        {
            if (!RoyalAvesTools.CanRunTool()) return;
            Apply();
        }

        public static void Apply()
        {
            ImportPieces();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                Build(root.transform.Find("MenuPlay"), root.GetComponentInChildren<BoostShop>(true));
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            try
            {
                // The game scene has intentional overrides and its own rewarded-ad button.
                // Apply to the instance too, without reverting unrelated overrides or replacing its controls.
                var scene = EditorSceneManager.OpenScene(Game, OpenSceneMode.Single);
                var canvas = scene.GetRootGameObjects().First(g => g.name == "CanvasGlobal");
                Build(canvas.transform.Find("MenuPlay"), canvas.GetComponentInChildren<BoostShop>(true));
                foreach (var t in canvas.transform.Find("MenuPlay").GetComponentsInChildren<Transform>(true))
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(t.gameObject))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
                    foreach (var c in t.GetComponents<Component>())
                        if (c != null && PrefabUtility.IsPartOfPrefabInstance(c))
                            PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                }
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Snow level modal applied to CanvasGlobal and game scene; existing level/boost/ad callbacks preserved.");
            }
            finally
            {
                if (setup.Any(s => s.isLoaded) && setup.Any(s => s.isActive))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
            AssetDatabase.SaveAssets();
        }

        static void ImportPieces()
        {
            Pieces.Clear();
            Import("Corpo principal vazio do modal-2.png", "Body");
            Import("Placa roxa e dourada-1.png", "Title");
            Import("Plaqueta roxa e dourada sem texto-4.png", "Label");
            Import("Contêiner roxo de boosters-5.png", "Boosters");
            Import("Botão verde com borda dourada-3.png", "Green");
            Import("Botão vermelho de fechar-7.png", "Close");
            Import("Soquete roxo dourado isolado-6.png", "Socket");
            Import("Neve e pingentes de gelo recortados-13.png", "Snow", true);
        }

        static void Import(string file, string name, bool corners = false)
        {
            var path = Art + file;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException(path);
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(path));
            var pixels = texture.GetPixels32();
            var w = texture.width; var h = texture.height;
            var regions = corners
                ? new[] { new RectInt(0, h - 400, 240, 190), new RectInt(750, h - 400, w - 750, 190),
                    new RectInt(0, 0, w / 2, h / 2), new RectInt(w / 2, 0, w - w / 2, h / 2),
                    new RectInt(240, h - 220, 272, 220), new RectInt(512, h - 210, 288, 210) }
                : new[] { new RectInt(0, 0, w, h) };
            var sprites = new List<SpriteMetaData>();
            for (var i = 0; i < regions.Length; i++)
            {
                var region = regions[i]; var minX = region.xMax; var minY = region.yMax; var maxX = region.x; var maxY = region.y;
                for (var y = region.y; y < region.yMax; y++)
                    for (var x = region.x; x < region.xMax; x++)
                        if (pixels[y * w + x].a > 12)
                        { minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y); }
                sprites.Add(new SpriteMetaData { name = corners ? name + i : name,
                    rect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1), pivot = Vector2.one * .5f, alignment = 0,
                    border = name == "Green" ? new Vector4(280, 250, 280, 250) : Vector4.zero });
            }
            UnityEngine.Object.DestroyImmediate(texture);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            // These buttons are reduced substantially on phones. Mips avoid jagged gold edges at that size.
            importer.mipmapEnabled = name == "Green";
            importer.filterMode = name == "Green" ? FilterMode.Trilinear : FilterMode.Bilinear;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
#pragma warning disable 0618
            importer.spritesheet = sprites.ToArray();
#pragma warning restore 0618
            importer.SaveAndReimport();
            foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()) Pieces[sprite.name] = sprite;
        }

        static void Build(Transform menu, BoostShop shop)
        {
            if (menu == null) throw new InvalidOperationException("CanvasGlobal/MenuPlay missing");
            var menuRect = (RectTransform)menu;
            menuRect.anchorMin = Vector2.zero; menuRect.anchorMax = Vector2.one;
            menuRect.offsetMin = menuRect.offsetMax = Vector2.zero;
            menuRect.localScale = Vector3.one;
            var window = (RectTransform)(menu.Find("Image") ?? menu.Find("MenuPlayBg"));
            Place(window, 0, 0, 1060, 1440);
            if (window.GetComponent<LevelStartLayout>() == null) window.gameObject.AddComponent<LevelStartLayout>();
            var backdrop = menu.GetComponent<Image>();
            if (backdrop != null) { backdrop.sprite = null; backdrop.color = new Color(.08f, 0, .13f, .78f); backdrop.raycastTarget = true; }
            var frame = (RectTransform)(window.Find("Image") ?? window.Find("MenuPlayBg"));
            Picture(frame, "Body"); Place(frame, 0, -25, 1060, 1280); frame.SetAsFirstSibling();
            // Goals remain under Bege to keep LevelGoalsView and any scene references valid.
            var beige = Child(window, "Bege");
            Place(beige, 0, 0, 1060, 1440);
            if (beige.GetComponent<Image>() is Image beigeImage) beigeImage.enabled = false;
            Hide(beige.Find("Objetivo")); Hide(window.Find("Target")); Hide(window.Find("Image (1)"));
            Hide(menu.Find("Leadboard")); Hide(menu.Find("Leadboard HD"));
            Hide(window.Find("BoosterText (1)")); Hide(window.Find("TitleText (1)")); Hide(window.Find("Info"));
            var goals = Child(beige, "Metas");
            Place(goals, 0, 275, 760, 300);
            var row = goals.GetComponent<HorizontalLayoutGroup>() ?? goals.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 80; row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlHeight = row.childControlWidth = false;
            row.childScaleWidth = row.childScaleHeight = true;
            row.childForceExpandHeight = row.childForceExpandWidth = false;
            var model = Child(goals, "Modelo"); Place(model, 0, 0, 230, 230);
            var modelImage = model.GetComponent<Image>() ?? model.gameObject.AddComponent<Image>();
            modelImage.preserveAspect = true; modelImage.raycastTarget = false;
            var amountRect = Child(model, "Quantidade");
            var quantity = amountRect.GetComponent<TextMeshProUGUI>() ?? amountRect.gameObject.AddComponent<TextMeshProUGUI>();
            Place(quantity.rectTransform, 0, -160, 250, 90); Text(quantity, "20", Purple, 80);
            model.gameObject.SetActive(false);
            var goalsView = goals.GetComponent<LevelGoalsView>() ?? goals.gameObject.AddComponent<LevelGoalsView>();
            var goalsData = new SerializedObject(goalsView); goalsData.FindProperty("template").objectReferenceValue = modelImage;
            goalsData.ApplyModifiedPropertiesWithoutUndo();
            var titlePlate = Child(window, "PlacaNivel"); Picture(titlePlate, "Title"); Place(titlePlate, 0, 590, 665, 222);
            var title = (window.Find("Titulo") ?? window.Find("TitleText")).GetComponent<TextMeshProUGUI>();
            foreach (Transform child in title.transform) child.gameObject.SetActive(false);
            Place(title.rectTransform, 0, 592, 550, 155);
            title.raycastTarget = false; title.enableAutoSizing = true; title.fontSizeMin = 55; title.fontSizeMax = 112;
            title.spriteAsset = null;
            RoyalAvesFonts.ApplyPostal(title);
            // Keep the localized level number; the postal font supplies a gold gradient and outline.
            var panel = Child(window, "PainelBoosters"); Picture(panel, "Boosters"); Place(panel, 0, -177, 920, 332);
            panel.SetSiblingIndex(beige.GetSiblingIndex() + 1);
            var plaque = Child(window, "PlacaBoosters"); Picture(plaque, "Label"); Place(plaque, 0, 17, 520, 100);
            var boosterLabel = (window.Find("TextMeshPro Text (1)") ?? window.Find("BoosterText")).GetComponent<TextMeshProUGUI>();
            Place(boosterLabel.rectTransform, 0, 17, 420, 80); Text(boosterLabel, "Boosters", new Color32(255, 227, 94, 255), 64);
            var boosters = (RectTransform)window.Find("Boosters");
            foreach (var layout in boosters.GetComponents<LayoutGroup>()) layout.enabled = false;
            Place(boosters, 0, -190, 900, 280);
            var activeBoosts = boosters.Cast<Transform>().Where(t => t.gameObject.activeSelf && t.GetComponent<BoostIcon>() != null).ToArray();
            var animationEvents = menu.GetComponents<MonoBehaviour>().FirstOrDefault(c => c != null && c.GetType().Name == "AnimationEventManager");
            if (animationEvents != null)
            {
                var eventData = new SerializedObject(animationEvents);
                for (var i = 0; i < Math.Min(3, activeBoosts.Length); i++)
                    eventData.FindProperty("boostIcon" + (i + 1)).objectReferenceValue = activeBoosts[i].GetComponent<BoostIcon>();
                eventData.ApplyModifiedPropertiesWithoutUndo();
            }
            for (var i = 0; i < activeBoosts.Length; i++)
            {
                var boost = (RectTransform)activeBoosts[i];
                Place(boost, (i - 1) * 285, 0, 250, 250);
                var bg = boost.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("Image (") && t.GetComponent<Image>() != null);
                if (bg != null)
                {
                    bg.gameObject.SetActive(true);
                    Picture((RectTransform)bg, "Socket"); Place((RectTransform)bg, 0, 0, 250, 250); bg.SetAsFirstSibling();
                    var look = boost.GetComponent<BoostSelectedLook>();
                    if (look != null)
                    {
                        var so = new SerializedObject(look);
                        so.FindProperty("normal").objectReferenceValue = Pieces["Socket"];
                        // Keep the already configured green selection and shader values.
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                Hide(boost.Find("Border"));
                if (boost.Find("Center") is RectTransform center) Place(center, 0, 0, 216, 216);
                // Keep each boost's icon, counter, lock, plus, check, type and click configuration.
                if (boost.Find("Image") is RectTransform badge)
                {
                    Place(badge, 86, -88, 135, 135); badge.SetAsLastSibling();
                    var badgeImage = badge.GetComponent<Image>();
                    badgeImage.material = null; badgeImage.color = Color.white; badgeImage.preserveAspect = true;
                    var counter = boost.GetComponent<BoostIcon>().boostCount;
                    Fill(counter.rectTransform, 12, 12, 12, 12); Text(counter, counter.text, Color.white, 72);
                }
            }
            var play = (RectTransform)window.Find("Play");
            ButtonFace(play); Place(play, -225, -480, ActionWidth, ActionHeight);
            foreach (Transform child in play) child.gameObject.SetActive(false);
            var playLabel = Child(play, "TextoJogar"); playLabel.gameObject.SetActive(true);
            var playText = playLabel.GetComponent<TextMeshProUGUI>() ?? playLabel.gameObject.AddComponent<TextMeshProUGUI>();
            Fill(playText.rectTransform, 24, 16, 24, 16); Text(playText, "Jogar", Color.white, 92);
            var freeButtons = menu.GetComponentsInChildren<FreeAdBoosterButton>(true);
            var free = freeButtons.FirstOrDefault(c => !PrefabUtility.IsPartOfPrefabInstance(c) || PrefabUtility.IsAddedComponentOverride(c))
                ?? freeButtons.FirstOrDefault();
            foreach (var duplicate in freeButtons.Where(c => c != free))
            {
                var duplicateButton = new SerializedObject(duplicate).FindProperty("button").objectReferenceValue as Button;
                if (duplicateButton != null) duplicateButton.gameObject.SetActive(false);
            }
            Button ad;
            if (free != null) ad = (Button)new SerializedObject(free).FindProperty("button").objectReferenceValue;
            else
            {
                var rect = Child(window, "Gratis"); ad = rect.gameObject.AddComponent<Button>();
                free = rect.gameObject.AddComponent<FreeAdBoosterButton>();
                var icon = Child(rect, "Booster").gameObject.AddComponent<Image>();
                var so = new SerializedObject(free);
                so.FindProperty("button").objectReferenceValue = ad; so.FindProperty("icon").objectReferenceValue = icon;
                so.FindProperty("boostShop").objectReferenceValue = shop; so.ApplyModifiedPropertiesWithoutUndo();
            }
            if (ad == null) throw new InvalidOperationException("Free booster button missing");
            var adRect = (RectTransform)ad.transform;
            // The pre-existing button can live in another container; retain it and its callback targets.
            adRect.SetParent(window, false); adRect.gameObject.SetActive(true);
            ButtonFace(adRect); Place(adRect, 225, -480, ActionWidth, ActionHeight);
            var freeData = new SerializedObject(free);
            var rotationIcon = freeData.FindProperty("icon").objectReferenceValue as Image;
            foreach (Transform t in adRect) t.gameObject.SetActive(false);
            if (free.transform != adRect)
            {
                free.gameObject.SetActive(true);
                foreach (Transform t in free.transform) t.gameObject.SetActive(false);
            }
            // The rotation continues to choose the reward; its old oversized ornamental art is hidden.
            if (rotationIcon != null) rotationIcon.gameObject.SetActive(false);
            var freeLabel = Child(adRect, "TextoGratis"); freeLabel.gameObject.SetActive(true);
            var label = freeLabel.GetComponent<TextMeshProUGUI>() ?? freeLabel.gameObject.AddComponent<TextMeshProUGUI>();
            Place(freeLabel, -45, 0, 270, 160); Text(label, "Grátis", Color.white, 86);
            var video = Child(adRect, "Video"); video.gameObject.SetActive(true); Place(video, 143, 0, 69, 69);
            if (video.GetComponent<CanvasRenderer>() == null) video.gameObject.AddComponent<CanvasRenderer>();
            var glyph = video.GetComponent<RewardedVideoGlyph>() ?? video.gameObject.AddComponent<RewardedVideoGlyph>();
            glyph.color = new Color32(255, 248, 209, 255); glyph.raycastTarget = false;
            // The scene's previous button only had a click sound. Connect the existing reward method once.
            if (!Enumerable.Range(0, ad.onClick.GetPersistentEventCount()).Any(i =>
                    ad.onClick.GetPersistentTarget(i) == free && ad.onClick.GetPersistentMethodName(i) == nameof(FreeAdBoosterButton.WatchAdForFreeBooster)))
                UnityEventTools.AddPersistentListener(ad.onClick, free.WatchAdForFreeBooster);
            var close = (RectTransform)window.Find("Close");
            foreach (Transform child in close) child.gameObject.SetActive(false);
            Picture(close, "Close", true); Place(close, 414, 516, 145, 150); Hide(close.Find("X"));
            var snowSpots = new[] { new Vector4(-373, 522, 210, 134), new Vector4(373, 522, 210, 134),
                new Vector4(-369, -638, 210, 220), new Vector4(369, -638, 210, 220) };
            for (var i = 0; i < 4; i++)
            {
                var snow = Child(window, "Neve" + i); Picture(snow, "Snow" + i);
                var p = snowSpots[i]; Place(snow, p.x, p.y, p.z, p.w); snow.SetAsLastSibling();
            }
            titlePlate.SetAsLastSibling(); title.transform.SetAsLastSibling();
            for (var i = 0; i < 2; i++)
            {
                var snow = Child(window, "NeveTitulo" + i); Picture(snow, "Snow" + i);
                Place(snow, i == 0 ? -242 : 242, 689, 155, 99); snow.SetAsLastSibling();
            }
            plaque.SetAsLastSibling(); boosterLabel.transform.SetAsLastSibling(); close.SetAsLastSibling();
            PolishActions(menu);
        }

        static void ButtonFace(RectTransform rect)
        {
            Picture(rect, "Green", true);
            var image = rect.GetComponent<Image>();
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            // Preserve the artwork's border thickness and rounded end caps as the middle becomes taller.
            image.pixelsPerUnitMultiplier = Pieces["Green"].rect.height / ActionHeight;
            var button = rect.GetComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.transition = Selectable.Transition.ColorTint;
            var colors = ColorBlock.defaultColorBlock; colors.pressedColor = new Color(.8f, .9f, .8f); button.colors = colors;
        }

        // A focused update for an already customized scene: touch only Jogar and Grátis.
        public static GameObject ApplyActionButtonPolish()
        {
            ImportPieces();
            var prefab = PrefabUtility.LoadPrefabContents(Prefab);
            try { PolishActions(prefab.transform.Find("MenuPlay")); PrefabUtility.SaveAsPrefabAsset(prefab, Prefab); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(Game);
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(Game, OpenSceneMode.Additive);
            var menu = scene.GetRootGameObjects().First(g => g.name == "CanvasGlobal").transform.Find("MenuPlay");
            Undo.RegisterFullObjectHierarchyUndo(menu.gameObject, "Engrossar botões da modal");
            var actions = PolishActions(menu);
            foreach (var action in actions)
                foreach (var t in action.GetComponentsInChildren<Transform>(true))
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(t.gameObject)) PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
                    foreach (var component in t.GetComponents<Component>())
                        if (component != null && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return menu.gameObject;
        }

        static RectTransform[] PolishActions(Transform menu)
        {
            var menuRect = (RectTransform)menu;
            menuRect.anchorMin = Vector2.zero; menuRect.anchorMax = Vector2.one;
            menuRect.offsetMin = menuRect.offsetMax = Vector2.zero; menuRect.localScale = Vector3.one;
            var window = menu.Find("Image") ?? menu.Find("MenuPlayBg");
            Place((RectTransform)window, 0, 0, 1060, 1440);
            var frame = (RectTransform)(window.Find("Image") ?? window.Find("MenuPlayBg"));
            Picture(frame, "Body"); Place(frame, 0, -25, 1060, 1280); frame.SetAsFirstSibling();
            foreach (var oldGoals in menu.GetComponentsInChildren<SweetSugar.Scripts.GUI.MenuTargetIcon>(true))
                oldGoals.gameObject.SetActive(false);
            Hide(window.Find("Target"));
            Hide(menu.Find("Leadboard")); Hide(menu.Find("Leadboard HD"));
            var goals = window.GetComponentInChildren<LevelGoalsView>(true);
            var goalRect = (RectTransform)goals.transform;
            Place(goalRect, 0, 285, 860, 380);
            foreach (var layout in goals.GetComponents<LayoutGroup>()) layout.enabled = false;
            // Every goal is fitted explicitly. A mask clips stale geometry during the opening animation.
            foreach (var mask in goals.GetComponents<RectMask2D>()) UnityEngine.Object.DestroyImmediate(mask);
            var model = (Image)new SerializedObject(goals).FindProperty("template").objectReferenceValue;
            Place(model.rectTransform, 0, 52.5f, 260, 260);
            model.preserveAspect = true; model.raycastTarget = false; model.overrideSprite = null;
            var amount = model.GetComponentInChildren<TextMeshProUGUI>(true);
            Place(amount.rectTransform, 0, -180, 260, 110); ModalText(amount, amount.text, "Purple", 126);
            model.gameObject.SetActive(false);
            // Generated previews never belong in the saved scene; OnEnable creates the selected level atomically.
            foreach (var child in goals.transform.Cast<Transform>().Where(t => t.name.StartsWith("Meta ")).ToArray())
                UnityEngine.Object.DestroyImmediate(child.gameObject);

            var plate = Child(window, "PlacaNivel"); Picture(plate, "Title"); Place(plate, 0, 615, 665, 222);
            var title = (window.Find("Titulo") ?? window.Find("TitleText")).GetComponent<TextMeshProUGUI>();
            Place(title.rectTransform, 0, 622, 565, 178); ModalText(title, "Nível 1", "Gold", 166);
            title.rectTransform.localScale = new Vector3(.94f, 1, 1); title.spriteAsset = null;
            title.GetComponent<LevelStartTitle>().Refresh();
            var panel = Child(window, "PainelBoosters"); Picture(panel, "Boosters");
            Place(panel, 0, -147, 958, 400);
            // The original loose asset is a shallow strip. Fit the reference's taller well explicitly.
            panel.GetComponent<Image>().preserveAspect = false;
            panel.SetSiblingIndex(goalRect.parent.GetSiblingIndex() + 1);
            var plaque = Child(window, "PlacaBoosters"); Picture(plaque, "Label"); Place(plaque, 0, 53, 530, 106);
            var label = (window.Find("TextMeshPro Text (1)") ?? window.Find("BoosterText")).GetComponent<TextMeshProUGUI>();
            Place(label.rectTransform, 0, 57, 420, 96); ModalText(label, "Boosters", "Gold", 96);
            var boosts = (RectTransform)window.Find("Boosters"); Place(boosts, 0, -167, 900, 280);
            boosts.SetAsLastSibling();
            foreach (var layout in boosts.GetComponents<LayoutGroup>()) layout.enabled = false;
            var activeBoosts = boosts.GetComponentsInChildren<BoostIcon>(true).Where(b => b.gameObject.activeSelf).ToArray();
            for (var i = 0; i < activeBoosts.Length; i++)
            {
                var boost = activeBoosts[i]; var r = (RectTransform)boost.transform;
                Place(r, (i - (activeBoosts.Length - 1) * .5f) * 290, 0, 250, 250);
                var look = boost.GetComponent<BoostSelectedLook>();
                var data = look != null ? new SerializedObject(look) : null;
                var background = data?.FindProperty("background").objectReferenceValue as Image;
                if (background != null)
                {
                    background.gameObject.SetActive(true); Picture(background.rectTransform, "Socket", true);
                    Place(background.rectTransform, 0, 0, 250, 250); background.transform.SetAsFirstSibling();
                    data.FindProperty("normal").objectReferenceValue = Pieces["Socket"];
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                Hide(r.Find("Center")); Hide(r.Find("Border"));
                if (r.Find("Icon") is RectTransform icon)
                {
                    Place(icon, 0, 12, 222, 222);
                    var image = icon.GetComponent<Image>();
                    if (image != null)
                    {
                        image.sprite = TrimIcon(image.sprite);
                        image.preserveAspect = true; image.material = null; image.color = Color.white;
                    }
                }
                if (r.Find("Image") is RectTransform badge)
                {
                    Place(badge, 86, -88, 158, 158); badge.SetAsLastSibling();
                    var image = badge.GetComponent<Image>(); image.material = null; image.color = Color.white; image.preserveAspect = true;
                    Fill(boost.boostCount.rectTransform, 20, 20, 20, 20);
                }
                ModalText(boost.boostCount, boost.boostCount.text, "Badge", 85);
            }
            var play = (RectTransform)window.Find("Play");
            Place(play, -ActionX, ActionY, ActionWidth, ActionHeight); ButtonFace(play);
            var playText = play.Find("TextoJogar").GetComponent<TextMeshProUGUI>();
            Fill(playText.rectTransform, 12, 10, 12, 10); ModalText(playText, "Jogar", "Cream", 115);
            playText.rectTransform.localScale = new Vector3(.94f, 1, 1);
            var ad = menu.GetComponentsInChildren<FreeAdBoosterButton>(true)
                .Select(c => (Button)new SerializedObject(c).FindProperty("button").objectReferenceValue)
                .First(b => b != null && b.gameObject.activeSelf);
            var adRect = (RectTransform)ad.transform;
            Place(adRect, ActionX, ActionY, ActionWidth, ActionHeight); ButtonFace(adRect);
            var freeText = adRect.Find("TextoGratis").GetComponent<TextMeshProUGUI>();
            Place(freeText.rectTransform, -46, 0, 300, 168); ModalText(freeText, "Grátis", "Cream", 108);
            freeText.rectTransform.localScale = new Vector3(.94f, 1, 1);
            if (adRect.Find("Video") is RectTransform video) Place(video, 150, -1, 78, 78);
            var close = (RectTransform)window.Find("Close"); Picture(close, "Close", true); Place(close, 414, 516, 145, 150);
            var spots = new[] { new Vector4(-400, 557, 210, 145), new Vector4(400, 557, 210, 145),
                new Vector4(-397, -628, 255, 245), new Vector4(397, -628, 255, 245),
                new Vector4(-242, 724, 174, 95), new Vector4(242, 724, 174, 95) };
            for (var i = 0; i < spots.Length; i++)
            {
                var snow = Child(window, i < 4 ? "Neve" + i : "NeveTitulo" + (i - 4));
                Picture(snow, "Snow" + i); var p = spots[i]; Place(snow, p.x, p.y, p.z, p.w);
                snow.SetAsLastSibling();
            }
            plaque.SetAsLastSibling(); label.transform.SetAsLastSibling(); close.SetAsLastSibling();
            return new[] { menuRect };
        }

        static void ModalText(TextMeshProUGUI label, string value, string look, float size)
        {
            Text(label, value, Color.white, size);
            var font = RoyalAvesFonts.LevelModal;
            label.font = font;
            label.fontStyle = FontStyles.Normal;
            label.fontWeight = FontWeight.Regular;
            label.characterSpacing = label.wordSpacing = 0;
            label.margin = Vector4.zero;
            label.extraPadding = true;
            var path = "Assets/RoyalAves/Fonts/LevelModal " + look + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(font.material) { name = "LevelModal " + look };
                AssetDatabase.CreateAsset(material, path);
            }
            var purple = look == "Purple";
            material.SetColor(ShaderUtilities.ID_FaceColor, purple ? Purple : look == "Gold" ? Color.white : new Color32(255, 250, 224, 255));
            material.SetTexture(ShaderUtilities.ID_MainTex, font.atlasTexture);
            material.SetFloat(ShaderUtilities.ID_GradientScale, font.material.GetFloat(ShaderUtilities.ID_GradientScale));
            material.SetFloat(ShaderUtilities.ID_FaceDilate, .04f);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, purple ? 0 : .12f);
            material.SetColor(ShaderUtilities.ID_OutlineColor, look == "Gold" ? new Color32(112, 43, 8, 255) :
                look == "Badge" ? new Color32(115, 14, 8, 255) : new Color32(14, 65, 21, 255));
            if (purple) { material.DisableKeyword(ShaderUtilities.Keyword_Outline); material.DisableKeyword(ShaderUtilities.Keyword_Underlay); }
            else
            {
                material.EnableKeyword(ShaderUtilities.Keyword_Outline); material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.08f, .015f, .035f, .85f));
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, .45f); material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -.8f);
                material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, .12f);
            }
            label.fontSharedMaterial = material;
            label.enableVertexGradient = look == "Gold";
            label.colorGradient = new VertexGradient(new Color32(255, 254, 190, 255), new Color32(255, 254, 190, 255),
                new Color32(255, 172, 13, 255), new Color32(255, 172, 13, 255));
            EditorUtility.SetDirty(material);
        }

        // UI icons have very different transparent margins. A modal-only sprite rect fits the visible art
        // inside the socket without changing source textures, shared gameplay sprites or the button's hit area.
        static Sprite TrimIcon(Sprite source)
        {
            if (source == null) return null;
            if (TrimmedIcons.TryGetValue(source, out var cached)) return cached;
            var path = AssetDatabase.GetAssetPath(source);
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return source;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long localId);
            var destination = Art + "Booster-" + guid + "-" + localId + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(destination);
            if (existing != null) return TrimmedIcons[source] = existing;
            var texture = new Texture2D(2, 2);
            try
            {
                texture.LoadImage(File.ReadAllBytes(path));
                var pixels = texture.GetPixels32(); var original = source.rect;
                var ratio = texture.width / (float)source.texture.width;
                var region = new RectInt(Mathf.RoundToInt(original.x * ratio), Mathf.RoundToInt(original.y * ratio),
                    Mathf.RoundToInt(original.width * ratio), Mathf.RoundToInt(original.height * ratio));
                var minX = region.xMax; var minY = region.yMax; var maxX = region.xMin; var maxY = region.yMin;
                for (var y = region.yMin; y < region.yMax; y++)
                    for (var x = region.xMin; x < region.xMax; x++)
                        if (pixels[y * texture.width + x].a > 32)
                        { minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y); }
                if (maxX <= minX || maxY <= minY) return source;
                var rect = Rect.MinMaxRect(Mathf.Max(original.xMin, minX / ratio - 3), Mathf.Max(original.yMin, minY / ratio - 3),
                    Mathf.Min(original.xMax, (maxX + 1) / ratio + 3), Mathf.Min(original.yMax, (maxY + 1) / ratio + 3));
                var sprite = Sprite.Create(source.texture, rect, Vector2.one * .5f, source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                sprite.name = source.name + " modal";
                AssetDatabase.CreateAsset(sprite, destination);
                return TrimmedIcons[source] = sprite;
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }
        static void Picture(RectTransform rect, string key, bool raycast = false)
        {
            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.enabled = true; image.sprite = Pieces[key]; image.overrideSprite = null; image.color = Color.white;
            image.material = null; image.type = Image.Type.Simple; image.preserveAspect = true; image.raycastTarget = raycast;
        }
        static void Text(TextMeshProUGUI label, string value, Color color, float size)
        {
            foreach (var behaviour in label.GetComponents<MonoBehaviour>())
                if (behaviour != null && (behaviour.GetType().Name == "LocalizeText" || behaviour is SpriteWordText)) behaviour.enabled = false;
            label.font = RoyalAvesFonts.Display; label.fontSharedMaterial = label.font.material;
            label.text = value; label.color = color; label.enableVertexGradient = false;
            label.enableAutoSizing = true; label.fontSizeMin = size * .65f; label.fontSizeMax = size; label.fontSize = size;
            label.textWrappingMode = TextWrappingModes.NoWrap; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        }
        static RectTransform Child(Transform parent, string name)
        {
            if (parent.Find(name) is RectTransform existing) return existing;
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.gameObject.layer = parent.gameObject.layer; r.SetParent(parent, false); return r;
        }
        static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(w, h);
        }
        static void Fill(RectTransform r, float left, float bottom, float right, float top)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.localScale = Vector3.one;
            r.offsetMin = new Vector2(left, bottom); r.offsetMax = new Vector2(-right, -top);
        }
        static void Hide(Transform t) { if (t != null) t.gameObject.SetActive(false); }
    }
}
