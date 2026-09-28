// Builds the Correio Mágico agency lobby, laid out after the web prototype (royal-aves/src/styles.css, whose cqw unit is
// 1% of the screen width), replacing the Sweet Sugar path map. It also holds the level complete screen (envelope + star).
// Menu: Royal Aves > Criar lobby da agência. It creates the area configuration from Data/progression.json (only when it
// does not exist yet), builds the lobby prefab and places it in the game scene with the path map turned off.
// Afterwards edit the prefab and Resources/RoyalAvesProgression freely; rebuilding the prefab discards prefab edits.
using System;
using System.Linq;
using RoyalAves.Characters;
using RoyalAves.Meta;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.GUI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesLobbyBuilder
    {
        const string ArtRoot = "Assets/RoyalAves/Art/";
        const string ConfigFolder = "Assets/RoyalAves/Resources";
        const string ConfigPath = ConfigFolder + "/" + AreaProgressionConfig.ResourcePath + ".asset";
        const string PrefabFolder = "Assets/RoyalAves/Prefabs";
        const string PrefabPath = PrefabFolder + "/RoyalAvesLobby.prefab";
        const string GameScenePath = "Assets/SweetSugar/Scenes/game.unity";

        // The canvas is 1080 units wide (it matches the screen width), so 1 prototype cqw = 10.8 units.
        const float Width = 1080f;
        static float Cq(float value) => value * Width / 100f;

        static readonly Color Cream = new Color32(0xFF, 0xF8, 0xD5, 0xFF);
        static readonly Color Navy = new Color32(0x17, 0x2E, 0x67, 0xFF);
        static readonly Color TitleBlue = new Color32(0x07, 0x5B, 0x9B, 0xFF);
        static readonly Color CopyGrey = new Color32(0x52, 0x64, 0x76, 0xFF);
        static readonly Color TagBrown = new Color32(0x96, 0x70, 0x45, 0xFF);
        static readonly Color RowText = new Color32(0x3A, 0x60, 0x80, 0xFF);
        static readonly Color MeterText = new Color32(0x12, 0x45, 0x65, 0xFF);
        static readonly Color Divider = new Color32(0xE7, 0xC8, 0x78, 0xFF);
        static readonly Color PaleBlue = new Color32(0xD9, 0xEF, 0xFF, 0xFF);
        static readonly Color Dim = new Color(0, 0, 0, 0.6f);

        static TMP_FontAsset font;

        [MenuItem("Royal Aves/Criar lobby da agência")]
        static void BuildFromMenu()
        {
            var rebuild = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null ||
                          EditorUtility.DisplayDialog("Lobby da agência",
                              "Recriar o prefab do lobby no layout do protótipo? Ajustes feitos no prefab serão perdidos.\n\n" +
                              "A configuração das áreas (RoyalAvesProgression) não é alterada.", "Recriar", "Manter o prefab");
            if (!RoyalAvesTools.CanRunTool()) return;

            RoyalAvesReskin.EnsureImportSettings();
            RoyalAvesManagerBuilder.EnsureExpressionSet();
            var configCreated = EnsureConfig();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            bool placed;
            try
            {
                var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
                var prefab = rebuild ? BuildPrefab() : AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                placed = PlaceInScene(scene, prefab);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (setup.Length > 0 && setup.All(s => !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            EditorUtility.DisplayDialog("Lobby da agência",
                (configCreated ? "Configuração das áreas criada: " : "Configuração das áreas mantida: ") + ConfigPath + "\n" +
                (rebuild ? "Prefab do lobby criado: " : "Prefab do lobby mantido: ") + PrefabPath + "\n" +
                (placed ? "Lobby colocado na cena game; o mapa de caminho foi desligado." : "A cena game já tinha o lobby (atualizado pelo prefab)."), "OK");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<AreaProgressionConfig>(ConfigPath);
        }

        [MenuItem("Royal Aves/Zerar progresso da agência")]
        static void ResetProgress()
        {
            if (EditorUtility.DisplayDialog("Zerar progresso da agência",
                    "Apaga estrelas, níveis vencidos, melhorias e baús da agência neste computador.", "Zerar", "Cancelar"))
                MetaProgress.ResetAll();
        }

        static bool EnsureConfig()
        {
            if (AssetDatabase.LoadAssetAtPath<AreaProgressionConfig>(ConfigPath) != null) return false;
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/RoyalAves/Data/progression.json");
            var file = JsonUtility.FromJson<Data.ProgressionFile>(json.text);
            var config = ScriptableObject.CreateInstance<AreaProgressionConfig>();
            foreach (var area in file.areas)
            {
                // Only the central hall has art so far; other areas start without background and layers.
                var hasArt = area.id == "central";
                config.areas.Add(new AreaDefinition
                {
                    id = area.id,
                    title = area.name,
                    background = hasArt ? LoadSprite("Backgrounds/area-central-stage-0") : null,
                    tasks = area.tasks.Select(task => new AreaTask
                    {
                        id = task.id,
                        title = task.name,
                        starCost = task.cost,
                        layer = hasArt ? LoadSprite("Room/AreaCentral/" + task.sprite) : null,
                        icon = LoadSprite("UI/Area/deco-icon-" + task.id, optional: true)
                    }).ToList(),
                    // The web boosters (canhão, boné, arco, martelo) do not exist in Sweet Sugar yet: coins only.
                    reward = new AreaReward { coins = area.reward.coins }
                });
            }
            EnsureFolder(ConfigFolder);
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            return true;
        }

        static bool PlaceInScene(Scene scene, GameObject prefab)
        {
            var roots = scene.GetRootGameObjects();
            // The lobby replaces the path map; LevelManager also keeps the map off at runtime while a lobby exists.
            var levelManager = roots.SelectMany(r => r.GetComponentsInChildren<LevelManager>(true)).FirstOrDefault();
            if (levelManager != null && levelManager.LevelsMap != null) levelManager.LevelsMap.SetActive(false);
            if (roots.Any(r => r.GetComponentInChildren<LobbyController>(true) != null)) return false;
            PrefabUtility.InstantiatePrefab(prefab, scene);
            return true;
        }

        static GameObject BuildPrefab()
        {
            font = RoyalAvesFonts.Display;
            var root = new GameObject("RoyalAvesLobby", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                root.layer = LayerMask.NameToLayer("UI");
                // Camera, sorting layer and order are set by LobbyController at runtime, behind Sweet Sugar's popups.
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.planeDistance = 150;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(Width, 1920);
                scaler.matchWidthOrHeight = 0; // match width, like the prototype's cqw units

                var content = Stretch(Node("Content", root.transform));

                // Room: background and upgrade layers share one rectangle that covers the screen, so the layers stay aligned.
                var room = Stretch(Node("Room", content));
                var roomFitter = room.gameObject.AddComponent<AspectRatioFitter>();
                roomFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                roomFitter.aspectRatio = 941f / 1672f;
                var background = AddImage(Stretch(Node("Background", room)), LoadSprite("Backgrounds/area-central-stage-0"));
                background.preserveAspect = false;
                var layers = Stretch(Node("Layers", room));

                // HUD, from the prototype grid: 14.5cqw | 1fr | 1.12fr | 0.9fr | 11.5cqw, gap 1.5cqw, margins 3.3cqw.
                var hud = Node("HUD", content);
                hud.anchorMin = new Vector2(0, 1);
                hud.anchorMax = Vector2.one;
                hud.pivot = new Vector2(0.5f, 1);
                hud.anchoredPosition = new Vector2(0, -Cq(3.2f));
                hud.sizeDelta = new Vector2(0, Cq(14));
                var x = Cq(3.3f);
                var gap = Cq(1.5f);
                var unit = (Width - 2 * x - Cq(14.5f) - Cq(11.5f) - 4 * gap) / 3.02f;
                RectTransform Column(string name, float columnWidth)
                {
                    var column = Node(name, hud);
                    column.anchorMin = column.anchorMax = new Vector2(0, 0.5f);
                    column.pivot = new Vector2(0, 0.5f);
                    column.anchoredPosition = new Vector2(x, 0);
                    column.sizeDelta = new Vector2(columnWidth, Cq(14));
                    x += columnWidth + gap;
                    return column;
                }

                var avatar = Column("Avatar", Cq(14.5f));
                RoyalAvesManagerBuilder.AddAvatar(Place(Node("Face", avatar), new Vector2(0.5f, 0.5f), new Vector2(0, -Cq(0.65f)), new Vector2(Cq(8.4f), Cq(8.4f))));
                var avatarFrame = AddImage(Stretch(Node("Frame", avatar)), LoadSprite("UI/Profile/avatar-frame"), raycast: true);
                var avatarButton = avatar.gameObject.AddComponent<Button>();
                avatarButton.targetGraphic = avatarFrame;

                var coins = Column("Coins", unit);
                var coinsButton = HudFrame(coins, "Value", Cq(5.2f), out var coinsText);
                AddImage(Place(Node("Coin", coins), new Vector2(0, 0.5f), new Vector2(Cq(2.4f), 0), new Vector2(Cq(7.2f), Cq(9.5f))), LoadSprite("UI/Icons/moeda"));

                var lives = Column("Lives", unit * 1.12f);
                var livesButton = HudFrame(lives, "Label", Cq(4.2f), out var livesLabel);
                livesLabel.text = "Cheio";
                var heart = Place(Node("Heart", lives), new Vector2(0, 0.5f), new Vector2(Cq(1.5f), Cq(0.8f)), new Vector2(Cq(10.8f), Cq(11.2f)));
                AddImage(heart, LoadSprite("UI/Icons/vida_ativa"));
                var livesText = AddPostalText(Stretch(Node("Count", heart)), "5", Cq(4.8f));
                // Sweet Sugar refills lives with LIFESAddCounter (it used to live on the map HUD); it runs here, invisible,
                // and the label above shows "Cheio" or the time to the next life.
                AddText(Stretch(Node("LifeRefill", lives)), "", 10, Color.clear).gameObject.AddComponent<LIFESAddCounter>();

                var stars = Column("Stars", unit * 0.9f);
                HudFrame(stars, "Value", Cq(5.2f), out var starsText).interactable = false;
                AddImage(Place(Node("Star", stars), new Vector2(0, 0.5f), new Vector2(Cq(1.5f), Cq(0.6f)), new Vector2(Cq(10), Cq(11))), LoadSprite("UI/Icons/estrela"));

                var gear = Column("Settings", Cq(11.5f));
                var settingsButton = AddButton(Place(Node("Gear", gear), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Cq(10.5f), Cq(11.8f))), LoadSprite("UI/Buttons/configuracoes"));

                // "Nível N" and the area button: margins 5cqw, gap 5cqw, above the navigation.
                var actions = Node("Actions", content);
                actions.anchorMin = Vector2.zero;
                actions.anchorMax = new Vector2(1, 0);
                actions.pivot = new Vector2(0.5f, 0);
                actions.anchoredPosition = new Vector2(0, Cq(22));
                actions.sizeDelta = new Vector2(0, Cq(27));
                var buttonWidth = (Width - 3 * Cq(5)) / 2;
                var playRect = Place(Node("PlayButton", actions), new Vector2(0, 0.5f), new Vector2(Cq(5) + buttonWidth / 2, 0), new Vector2(buttonWidth, Cq(18)));
                var playButton = AddSlicedButton(playRect, "btn-play");
                var playText = AddPostalText(Stretch(Node("Label", playRect)), "Nível 1", Cq(8));

                var areaRect = Place(Node("AreaButton", actions), new Vector2(0, 0.5f), new Vector2(2 * Cq(5) + 1.5f * buttonWidth, 0), new Vector2(buttonWidth, Cq(27)));
                var areaArt = AddImage(RectIn(Node("Art", areaRect), new Vector2(0, 1), Vector2.one, new Vector2(Cq(4), -Cq(21)), new Vector2(-Cq(4), 0)), LoadSprite("UI/Menu/menu_mapa"), raycast: true);
                var areaButton = areaRect.gameObject.AddComponent<Button>();
                areaButton.targetGraphic = areaArt;
                var labelBox = RectIn(Node("LabelBox", areaRect), Vector2.zero, new Vector2(0.53f, 0), Vector2.zero, new Vector2(0, Cq(6.3f)));
                AddSliced(labelBox, "darkbox");
                var areaLabel = AddPostalText(Stretch(Node("Text", labelBox)), "Área 1", Cq(4));
                var progressBox = RectIn(Node("ProgressBox", areaRect), new Vector2(0.57f, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, Cq(6.3f)));
                AddSliced(progressBox, "darkbox");
                var areaProgress = AddPostalText(Stretch(Node("Text", progressBox)), "0/5", Cq(4));

                var navButtons = BuildNav(content);
                var modal = BuildModal(content, out var window, out var modalParts, out var taskRows);
                var restore = BuildRestoreEffect(content, out var restoreParts);
                var victory = BuildVictory(root.transform);

                var lobby = root.AddComponent<LobbyController>();
                Wire(lobby,
                    ("content", content.gameObject), ("background", background), ("roomFitter", roomFitter), ("layersRoot", layers),
                    ("avatarButton", avatarButton), ("coinsButton", coinsButton), ("coinsText", coinsText),
                    ("livesButton", livesButton), ("livesText", livesText), ("livesLabel", livesLabel), ("starsText", starsText),
                    ("settingsButton", settingsButton), ("playButton", playButton), ("playText", playText),
                    ("areaButton", areaButton), ("areaLabel", areaLabel), ("areaProgress", areaProgress),
                    ("modal", modal), ("modalWindow", window), ("restoreEffect", restore), ("victory", victory),
                    ("restoreSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SweetSugar/Audio/wav/Star_win_03.wav")));
                Wire(lobby, modalParts);
                Wire(lobby, restoreParts);
                WireArray(lobby, "navButtons", navButtons);
                WireArray(lobby, "taskRows", taskRows);

                modal.SetActive(false);
                restore.SetActive(false);
                victory.gameObject.SetActive(false);
                EnsureFolder(PrefabFolder);
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // Bottom navigation: Eventos, Ranking, Agência (the lobby, raised), Equipes, Coleção.
        static Object[] BuildNav(RectTransform content)
        {
            string[] labels = { "Eventos", "Ranking", "Agência", "Equipes", "Coleção" };
            string[] icons = { "UI/Menu/menu_correio_pequeno", "UI/Menu/menu_trofeu", "UI/Menu/menu_agencia", "UI/Menu/menu_amigos", "UI/Menu/menu_selos" };
            var nav = RectIn(Node("Nav", content), Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, Cq(18.5f)));
            AddSliced(nav, "nav", raycast: true);
            var buttons = new Object[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                var active = i == 2;
                var cell = RectIn(Node(labels[i], nav), new Vector2(i / 5f, 0), new Vector2((i + 1) / 5f, 1), Vector2.zero, new Vector2(0, -Cq(1.5f)));
                var target = active ? AddSliced(cell, "nav-active", raycast: true) : AddImage(cell, null, new Color(1, 1, 1, 0), raycast: true);
                var button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = target;
                var iconSize = active ? new Vector2(Cq(17), Cq(16)) : new Vector2(Cq(13), Cq(12));
                AddImage(Place(Node("Icon", cell), new Vector2(0.5f, 1), new Vector2(0, active ? -Cq(3) : -Cq(6.5f)), iconSize), LoadSprite(icons[i]));
                AddText(RectIn(Node("Label", cell), Vector2.zero, new Vector2(1, 0), new Vector2(0, Cq(1.2f)), new Vector2(0, Cq(4.4f))), labels[i], Cq(2.7f), Cream);
                buttons[i] = button;
            }
            return buttons;
        }

        // One window for every lobby dialog, like the prototype's <dialog>: art, tag, title, copy, a content slot and
        // two buttons. It grows with its content.
        static GameObject BuildModal(RectTransform content, out RectTransform window, out (string field, Object value)[] parts, out Object[] taskRows)
        {
            var modal = Stretch(Node("Modal", content));
            AddImage(modal, null, Dim, raycast: true); // blocks taps on the lobby behind it
            window = Place(Node("Window", modal), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Cq(90), 0));
            AddSliced(window, "panel", raycast: true);
            var layout = window.gameObject.AddComponent<VerticalLayoutGroup>();
            var padding = (int)Cq(6);
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = Cq(2);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            window.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var art = Element(window, "Art", Cq(22));
            RoyalAvesManagerBuilder.AddPortrait(art, ManagerMood.Idle);
            var tag = AddText(Node("Tag", window), "", Cq(2.8f), TagBrown);
            var title = AddText(Node("Title", window), "", Cq(7), TitleBlue);
            title.textWrappingMode = TextWrappingModes.Normal;
            var copy = AddText(Node("Copy", window), "", Cq(3.8f), CopyGrey);
            copy.textWrappingMode = TextWrappingModes.Normal;

            var extra = Node("Extra", window);
            var extraLayout = extra.gameObject.AddComponent<VerticalLayoutGroup>();
            extraLayout.childControlWidth = extraLayout.childControlHeight = true;
            extraLayout.childForceExpandWidth = true;
            extraLayout.childForceExpandHeight = false;

            var decorations = BuildDecorations(extra, out var meterFill, out var meterText, out var rows);
            var chest = Element(extra, "Chest", Cq(44));
            AddImage(Place(Node("StarLeft", chest), new Vector2(0.3f, 1), new Vector2(0, -Cq(4)), new Vector2(Cq(7), Cq(7.7f))), LoadSprite("UI/Icons/estrela"));
            AddImage(Place(Node("StarRight", chest), new Vector2(0.7f, 1), new Vector2(0, -Cq(4)), new Vector2(Cq(7), Cq(7.7f))), LoadSprite("UI/Icons/estrela"));
            var chestTap = AddButton(Place(Node("ChestButton", chest), new Vector2(0.5f, 0.5f), new Vector2(0, -Cq(3)), new Vector2(Cq(36), Cq(32.7f))), LoadSprite("UI/Chest/bau_fechado"));
            var reward = Element(extra, "Reward", Cq(46));
            AddImage(Place(Node("OpenChest", reward), new Vector2(0.5f, 1), new Vector2(0, -Cq(17)), new Vector2(Cq(28), Cq(33))), LoadSprite("UI/Chest/bau_aberto"));
            var rewardText = AddText(RectIn(Node("Text", reward), Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, Cq(11))), "", Cq(4.4f), TitleBlue);
            rewardText.textWrappingMode = TextWrappingModes.Normal;
            var settings = Element(extra, "Settings", Cq(12));
            var soundButton = AddSlicedButton(RectIn(Node("Sound", settings), Vector2.zero, new Vector2(0.48f, 1), Vector2.zero, Vector2.zero), "btn-blue");
            var soundText = AddText(Stretch(Node("Label", soundButton.transform)), "Som: ligado", Cq(3.4f), Cream);
            var musicButton = AddSlicedButton(RectIn(Node("Music", settings), new Vector2(0.52f, 0), Vector2.one, Vector2.zero, Vector2.zero), "btn-blue");
            var musicText = AddText(Stretch(Node("Label", musicButton.transform)), "Música: ligada", Cq(3.4f), Cream);
            var locked = Element(extra, "LockedArea", Cq(22));
            AddImage(Place(Node("Lock", locked), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Cq(20), Cq(20))), LoadSprite("UI/Area/area_bloqueada"));

            var primaryRect = Element(window, "Primary", Cq(14));
            var primary = AddSlicedButton(primaryRect, "btn-green");
            var primaryText = AddText(Stretch(Node("Label", primaryRect)), "OK", Cq(4.8f), Cream);
            var secondaryRect = Element(window, "Secondary", Cq(10.5f));
            var secondary = AddSlicedButton(secondaryRect, "btn-red");
            var secondaryText = AddText(Stretch(Node("Label", secondaryRect)), "", Cq(3.8f), Color.white);
            var close = AddButton(Place(Node("Close", window), Vector2.one, new Vector2(-Cq(3), -Cq(3)), new Vector2(Cq(10), Cq(10))), LoadSprite("UI/Buttons/fechar"));
            close.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            foreach (var hidden in new[] { decorations, chest.gameObject, reward.gameObject, settings.gameObject, locked.gameObject })
                hidden.SetActive(false);

            parts = new (string, Object)[]
            {
                ("modalArt", art.gameObject), ("modalTag", tag), ("modalTitle", title), ("modalCopy", copy),
                ("modalPrimary", primary), ("modalPrimaryText", primaryText), ("modalSecondary", secondary),
                ("modalSecondaryText", secondaryText), ("modalClose", close),
                ("decorationsExtra", decorations), ("meterFill", meterFill), ("meterText", meterText),
                ("chestExtra", chest.gameObject), ("chestTapButton", chestTap),
                ("rewardExtra", reward.gameObject), ("rewardText", rewardText),
                ("settingsExtra", settings.gameObject), ("soundButton", soundButton), ("soundText", soundText),
                ("musicButton", musicButton), ("musicText", musicText), ("lockedAreaExtra", locked.gameObject)
            };
            taskRows = rows.Cast<Object>().ToArray();
            return modal.gameObject;
        }

        // The area window: progress meter with the chest, then the next upgrade and a preview of the one after it.
        static GameObject BuildDecorations(RectTransform extra, out RectTransform meterFill, out TextMeshProUGUI meterText, out LobbyTaskRow[] rows)
        {
            var decorations = Element(extra, "Decorations", Cq(60));
            var meter = RectIn(Node("Meter", decorations), new Vector2(0, 1), Vector2.one, new Vector2(0, -Cq(20)), Vector2.zero);
            AddSliced(meter, "meter");
            AddText(RectIn(Node("Caption", meter), new Vector2(0, 1), new Vector2(0.75f, 1), new Vector2(Cq(3), -Cq(7)), new Vector2(0, -Cq(1.5f))), "Progresso na Área", Cq(3.8f), MeterText, TextAlignmentOptions.Left);
            var bar = RectIn(Node("Bar", meter), new Vector2(0, 0.5f), new Vector2(0.75f, 0.5f), new Vector2(Cq(3), -Cq(1.8f)), new Vector2(0, Cq(1.2f)));
            AddSliced(bar, "bar-bg");
            meterFill = RectIn(Node("Fill", bar), Vector2.zero, new Vector2(0.2f, 1), Vector2.zero, Vector2.zero);
            AddSliced(meterFill, "bar-fill");
            meterText = AddText(RectIn(Node("Count", meter), Vector2.zero, new Vector2(0.75f, 0), new Vector2(Cq(3), Cq(1)), new Vector2(0, Cq(6))), "0/5", Cq(4.2f), MeterText);
            AddImage(Place(Node("Chest", meter), new Vector2(1, 0.5f), new Vector2(-Cq(10), 0), new Vector2(Cq(15), Cq(13.6f))), LoadSprite("UI/Chest/bau_fechado"));
            var divider = RectIn(Node("Divider", decorations), new Vector2(0, 1), Vector2.one, new Vector2(0, -Cq(23)), new Vector2(0, -Cq(22.5f)));
            AddImage(divider, null, Divider);
            rows = new[] { BuildTaskRow(decorations, "NextTask", Cq(25)), BuildTaskRow(decorations, "PreviewTask", Cq(42.5f)) };
            return decorations.gameObject;
        }

        static LobbyTaskRow BuildTaskRow(RectTransform parent, string name, float top)
        {
            var row = RectIn(Node(name, parent), new Vector2(0, 1), Vector2.one, new Vector2(0, -top - Cq(15.5f)), new Vector2(0, -top));
            AddSliced(row, "card");
            var group = row.gameObject.AddComponent<CanvasGroup>();
            AddImage(Place(Node("Token", row), new Vector2(0, 0.5f), new Vector2(Cq(8), 0), new Vector2(Cq(12.5f), Cq(12.5f))), LoadSprite("UI/Generated/token"));
            var icon = AddImage(Place(Node("Icon", row), new Vector2(0, 0.5f), new Vector2(Cq(8), 0), new Vector2(Cq(10), Cq(10))), null);
            var title = AddText(RectIn(Node("Title", row), Vector2.zero, Vector2.one, new Vector2(Cq(16), 0), new Vector2(-Cq(26), 0)), "Melhoria", Cq(3.8f), RowText, TextAlignmentOptions.Left);
            title.textWrappingMode = TextWrappingModes.Normal;
            var buttonRect = Place(Node("Button", row), new Vector2(1, 0.5f), new Vector2(-Cq(13), 0), new Vector2(Cq(22), Cq(12)));
            var button = AddSlicedButton(buttonRect, "btn-green");
            var caption = AddText(RectIn(Node("Caption", buttonRect), new Vector2(0, 0.5f), Vector2.one, new Vector2(0, -Cq(0.5f)), new Vector2(0, -Cq(1))), "Restaurar", Cq(2.6f), Cream);
            AddImage(Place(Node("Star", buttonRect), new Vector2(0.38f, 0.3f), Vector2.zero, new Vector2(Cq(4.2f), Cq(4.6f))), LoadSprite("UI/Icons/estrela"));
            var cost = AddText(RectIn(Node("Cost", buttonRect), new Vector2(0.48f, 0), new Vector2(0.9f, 0.55f), Vector2.zero, Vector2.zero), "1", Cq(4), Cream);
            var component = row.gameObject.AddComponent<LobbyTaskRow>();
            Wire(component, ("icon", icon), ("title", title), ("button", button), ("caption", caption), ("cost", cost), ("group", group));
            return component;
        }

        // The prototype's restoration arrival: a glow, the upgrade token and its name in a blue banner.
        static GameObject BuildRestoreEffect(RectTransform content, out (string field, Object value)[] parts)
        {
            var effect = Stretch(Node("RestoreEffect", content));
            AddImage(effect, null, new Color(0, 0, 0, 0.25f), raycast: true);
            var glow = Place(Node("Glow", effect), new Vector2(0.5f, 0.5f), new Vector2(0, Cq(8)), new Vector2(Cq(95), Cq(95)));
            AddImage(glow, LoadSprite("UI/Generated/glow"));
            var glowGroup = glow.gameObject.AddComponent<CanvasGroup>();
            var token = Place(Node("Token", effect), new Vector2(0.5f, 0.5f), new Vector2(0, Cq(8)), new Vector2(Cq(28), Cq(28)));
            AddImage(token, LoadSprite("UI/Generated/token"));
            var icon = AddImage(Place(Node("Icon", token), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Cq(21), Cq(21))), null);
            var labelBox = Place(Node("Label", effect), new Vector2(0.5f, 0.5f), new Vector2(0, -Cq(16)), new Vector2(Cq(86), Cq(12)));
            AddSliced(labelBox, "btn-blue");
            var label = AddText(Stretch(Node("Text", labelBox)), "", Cq(5.4f), Cream);
            parts = new (string, Object)[]
            {
                ("restoreGlow", glowGroup), ("restoreToken", token), ("restoreIcon", icon), ("restoreLabelBox", labelBox), ("restoreLabel", label)
            };
            return effect.gameObject;
        }

        // Level complete, as in the prototype (#modal[data-kind=won]): level tag, "Muito bem!", the envelope that opens and
        // gives a star, the reward lines, "Continuar" and "Jogar esta fase de novo". A nested canvas draws it above the board.
        static VictoryScreen BuildVictory(Transform root)
        {
            var panel = Stretch(Node("Victory", root));
            var canvas = panel.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10;
            panel.gameObject.AddComponent<GraphicRaycaster>();
            AddImage(panel, null, new Color32(6, 16, 32, 0xB8), raycast: true);

            var card = Place(Node("Card", panel), new Vector2(0.5f, 0.5f), new Vector2(0, Cq(4)), new Vector2(Cq(88), Cq(132)));
            AddSliced(card, "victory-card", raycast: true);
            var banner = Place(Node("Tag", card), new Vector2(0.5f, 1), new Vector2(0, Cq(2)), new Vector2(Cq(96), Cq(15)));
            AddSliced(banner, "banner");
            var levelTag = AddText(Stretch(Node("Text", banner)), "NÍVEL 1", Cq(7.5f), Cream);
            var title = AddPostalText(Place(Node("Title", card), new Vector2(0.5f, 1), new Vector2(0, -Cq(19)), new Vector2(Cq(80), Cq(12))), "Muito bem!", Cq(10));

            var stage = Place(Node("Stage", card), new Vector2(0.5f, 1), new Vector2(0, -Cq(55)), new Vector2(Cq(80), Cq(56)));
            var rays = Place(Node("Rays", stage), new Vector2(0.5f, 0.5f), new Vector2(0, Cq(6)), new Vector2(Cq(60), Cq(60)));
            AddImage(rays, LoadSprite("UI/Generated/rays"));
            var raysGroup = rays.gameObject.AddComponent<CanvasGroup>();
            var sparkles = Place(Node("Sparkles", stage), new Vector2(0.5f, 1), new Vector2(0, -Cq(6)), new Vector2(Cq(64), Cq(8)));
            var sparklesGroup = sparkles.gameObject.AddComponent<CanvasGroup>();
            foreach (var sparkleX in new[] { -Cq(27), -Cq(14), Cq(14), Cq(27) })
                AddImage(Place(Node("Sparkle", sparkles), new Vector2(0.5f, 0.5f), new Vector2(sparkleX, 0), new Vector2(Cq(4.5f), Cq(5))), LoadSprite("UI/Icons/estrela"));
            var envelope = Place(Node("Envelope", stage), new Vector2(0.5f, 0), new Vector2(0, Cq(19)), new Vector2(Cq(46), Cq(35)));
            var envelopeImage = AddImage(envelope, LoadSprite("Effects/EnvelopeOpen/envelope-open-1"));
            var burst = Place(Node("Burst", stage), new Vector2(0.5f, 0), new Vector2(0, Cq(30)), new Vector2(Cq(50), Cq(50)));
            AddImage(burst, LoadSprite("UI/Generated/glow"));
            var burstGroup = burst.gameObject.AddComponent<CanvasGroup>();
            var star = Place(Node("Star", stage), new Vector2(0.5f, 1), new Vector2(0, -Cq(14)), new Vector2(Cq(24), Cq(26.5f)));
            AddImage(star, LoadSprite("UI/Icons/estrela"));
            var starGroup = star.gameObject.AddComponent<CanvasGroup>();

            var rewardLine = AddText(Place(Node("Reward", card), new Vector2(0.5f, 0), new Vector2(0, Cq(44)), new Vector2(Cq(80), Cq(8))), "+1 estrela", Cq(6), Cream);
            var rewardTotal = AddText(Place(Node("Total", card), new Vector2(0.5f, 0), new Vector2(0, Cq(36)), new Vector2(Cq(80), Cq(6))), "", Cq(3.8f), PaleBlue);
            var continueRect = Place(Node("Continue", card), new Vector2(0.5f, 0), new Vector2(0, Cq(22)), new Vector2(Cq(56), Cq(13)));
            var continueButton = AddSlicedButton(continueRect, "btn-green");
            AddText(Stretch(Node("Label", continueRect)), "Continuar", Cq(6), Cream);
            var replayRect = Place(Node("Replay", card), new Vector2(0.5f, 0), new Vector2(0, Cq(8)), new Vector2(Cq(64), Cq(8)));
            var replayButton = AddButton(replayRect, null);
            replayButton.targetGraphic.color = new Color(1, 1, 1, 0);
            AddText(Stretch(Node("Label", replayRect)), "Jogar esta fase de novo", Cq(3.8f), PaleBlue);
            // The manager peeks over the card's bottom-left corner, clear of the buttons.
            var manager = RoyalAvesManagerBuilder.AddVictoryPortrait(panel, Cq(30), Cq(16));

            var screen = panel.gameObject.AddComponent<VictoryScreen>();
            Wire(screen,
                ("card", card), ("levelTag", levelTag), ("title", title.rectTransform), ("envelope", envelope), ("envelopeImage", envelopeImage),
                ("burst", burstGroup), ("star", star), ("starGroup", starGroup), ("rays", rays), ("raysGroup", raysGroup),
                ("sparkles", sparklesGroup), ("rewardLine", rewardLine), ("rewardTotal", rewardTotal),
                ("continueButton", continueButton), ("replayButton", replayButton), ("manager", manager));
            WireArray(screen, "envelopeFrames", Enumerable.Range(1, 5).Select(i => (Object)LoadSprite("Effects/EnvelopeOpen/envelope-open-" + i)).ToArray());
            return screen;
        }

        // Coins, lives and stars sit in the prototype's cream frames; the value uses the postal font.
        static Button HudFrame(RectTransform column, string textName, float fontSize, out TextMeshProUGUI text)
        {
            var frame = RectIn(Node("Frame", column), new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -Cq(4.6f)), new Vector2(0, Cq(4.6f)));
            var button = AddSlicedButton(frame, "hud-frame");
            text = AddPostalText(RectIn(Node(textName, frame), Vector2.zero, Vector2.one, new Vector2(Cq(7), 0), new Vector2(-Cq(2.3f), Cq(0.5f))), "0", fontSize, TextAlignmentOptions.Right);
            return button;
        }

        static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        static RectTransform Stretch(RectTransform rect) => RectIn(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        static RectTransform RectIn(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        // A node inside a layout group, with a fixed preferred height.
        static RectTransform Element(RectTransform parent, string name, float height)
        {
            var rect = Node(name, parent);
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = element.minHeight = height;
            return rect;
        }

        static Image AddImage(RectTransform rect, Sprite sprite, Color? color = null, bool raycast = false)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color ?? Color.white;
            image.preserveAspect = true;
            image.raycastTarget = raycast;
            return image;
        }

        static Image AddSliced(RectTransform rect, string generatedSprite, bool raycast = false)
        {
            var image = AddImage(rect, LoadSprite("UI/Generated/" + generatedSprite), raycast: raycast);
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            return image;
        }

        static Button AddSlicedButton(RectTransform rect, string generatedSprite)
        {
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = AddSliced(rect, generatedSprite, raycast: true);
            return button;
        }

        static Button AddButton(RectTransform rect, Sprite sprite)
        {
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = AddImage(rect, sprite, raycast: true);
            return button;
        }

        static TextMeshProUGUI AddText(RectTransform rect, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        // The prototype's "postal-text": the CorreioMagico font with its cream-to-orange letters and blue outline.
        static TextMeshProUGUI AddPostalText(RectTransform rect, string text, float size,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var label = AddText(rect, text, size, Color.white, alignment);
            RoyalAvesFonts.ApplyPostal(label);
            return label;
        }

        static void Wire(Object target, params (string field, Object value)[] references)
        {
            var serialized = new SerializedObject(target);
            foreach (var (field, value) in references)
            {
                var property = serialized.FindProperty(field) ??
                               throw new InvalidOperationException($"{target.GetType().Name} não tem o campo {field}");
                property.objectReferenceValue = value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireArray(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field) ??
                           throw new InvalidOperationException($"{target.GetType().Name} não tem o campo {field}");
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Sprite LoadSprite(string relativePathWithoutExtension, bool optional = false)
        {
            var path = ArtRoot + relativePathWithoutExtension;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path + ".png");
            if (sprite == null) sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path + ".jpg");
            if (sprite == null && !optional) Debug.LogWarning($"Royal Aves: sprite não encontrado em {path}");
            return sprite;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
