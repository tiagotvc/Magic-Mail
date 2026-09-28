// Puts the Correio Mágico game-screen art over Sweet Sugar's: the leather HUD frame at the top (round portrait hole,
// the moves panel and the goals panel), the manager inside that hole cut by the circle instead of by the straight
// bottom edge of her picture, the booster bar at the foot of the screen and the teal frame around the board.
//
// Only the portrait orientation ("Vertical" of OrientationPanel) is rebuilt: that is the one the game ships in.
// Moves and Target keep their own rectangles and children, and are only moved and scaled onto the panels of the frame,
// so Sweet Sugar's counters and goal icons go on working untouched.
// Menu: Royal Aves > Aplicar moldura da tela de jogo. Running it again only updates what it made.
using System.Collections.Generic;
using System.Linq;
using RoyalAves.Meta;
using SweetSugar.Scripts.GUI.Boost;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesGameScreenBuilder
    {
        const string Art = RoyalAvesAssetImporter.GameFolder;
        const string OrientationPrefab = "Assets/SweetSugar/Scripts/System/Orientation/OrientationPanel.prefab";
        static readonly string[] ScenePaths = { "Assets/SweetSugar/Scenes/game.unity", "Assets/SweetSugar/Scenes/gameStatic.unity" };

        const string FrameName = "MolduraFase";
        const string BarName = "BarraReforcos";
        const string RingName = "AnelRetrato";
        const string GearName = "BotaoConfiguracoes";
        const string GoalsTitleName = "TituloMetas";
        static readonly string FrameNewLine = "\n- ";
        static readonly string FrameBlankLine = "\n\n";

        // Sweet Sugar paints its HUD backgrounds on the panels themselves, not on a child: the pink striped banner on
        // TopPanel, the pink box behind the moves, the cream bar behind the goals and the banner under the boosters.
        // Correio Mágico's frame and bar take their place, so these are switched off — the Image component, not the
        // object, so the counters and the goal icons inside them go on working.
        static readonly string[] SweetSugarBackdrops = { "ver_top_banner", "moves", "target_bar", "ver_foot_banner" };

        // moldura-fase.png is 2172x724. The round hole and the two beige panels were measured on the picture and are
        // kept in its own pixels, so moving a slot means changing one number here.
        const float ArtW = 2172f, ArtH = 724f;
        static readonly Rect HoleArt = Rect.MinMaxRect(180, 130, 602, 554);
        static readonly Rect MovesArt = Rect.MinMaxRect(736, 180, 1172, 542);
        static readonly Rect GoalsArt = Rect.MinMaxRect(1209, 176, 2023, 544);

        // The HUD fills the width of the screen and its height follows the picture's own proportion — it is never
        // squashed, because the portrait hole has to stay round. To land in the strip the mock-up marks (about 12% of
        // the screen height) the picture has to be roughly 4.5:1; moldura-fase.png is 3:1, so it comes out half as tall
        // again. The dialog reports the height it ended up with.
        const float TargetRatio = 4.5f;

        // moldura-fase.png has transparent margin around the leather (it starts at x=39 and ends at x=2128 of 2172), so
        // the frame is made wider than the panel by exactly that much: what shows then touches both edges of the screen.
        const float ContentLeft = 39f, ContentRight = 2128f;

        // barra-inferior-boosters.png is 1672 wide; its band and its five buttons are measured in those pixels.
        const float BarStripTop = 322f, BarStripBottom = 606f;   // the strip cut out as barra-reforcos.png
        const float ButtonArtSize = 192f, ButtonArtY = 474.5f;   // a button's diameter and its centre in the picture
        static readonly float[] ButtonArtX = { 329.5f, 582f, 834.5f, 1086.5f, 1340f };

        [MenuItem("Royal Aves/Aplicar moldura da tela de jogo")]
        static void ApplyFromMenu()
        {
            if (!RoyalAvesTools.CanRunTool()) return;
            RoyalAvesReskin.EnsureImportSettings();

            var frame = Load("moldura-fase");
            var portraitBack = Load("fundo-retrato");
            var bar = Load("barra-reforcos");
            var button = Load("botao-reforco");
            var gear = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/UI/Buttons/configuracoes.png");
            if (new[] { frame, portraitBack, bar, button, gear }.Any(s => s == null))
            {
                EditorUtility.DisplayDialog("Tela de jogo", "Faltam imagens em " + Art +
                    " (moldura-fase, fundo-retrato, barra-reforcos, botao-reforco) " +
                    "ou Art/UI/Buttons/configuracoes.", "OK");
                return;
            }

            var log = new List<string>();

            // The game scenes hold their own copy of the HUD — they are NOT instances of OrientationPanel.prefab — and
            // their hierarchy differs (Vertical/SafeArea/TopPanel against Vertical/TopPanel). So the scenes are what
            // matter, the prefab is done as well in case something still instantiates it, and both are found by
            // searching for the panels by name instead of by a fixed path.
            var root = PrefabUtility.LoadPrefabContents(OrientationPrefab);
            try
            {
                if (ApplyToHuds(root.transform, frame, portraitBack, bar, button, gear, "OrientationPanel.prefab", log) > 0)
                    PrefabUtility.SaveAsPrefabAsset(root, OrientationPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var path in ScenePaths)
                {
                    if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var done = scene.GetRootGameObjects()
                        .Sum(g => ApplyToHuds(g.transform, frame, portraitBack, bar, button, gear, scene.name, log));
                    if (done == 0)
                    {
                        log.Add($"{scene.name}: nenhum HUD vertical encontrado");
                        continue;
                    }
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) log.Add($"{scene.name}: NAO consegui gravar a cena");
                }
            }
            finally
            {
                if (setup.Length > 0 && setup.All(x => !string.IsNullOrEmpty(x.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            AddBoardFrame(log);

            EditorUtility.DisplayDialog("Tela de jogo", "Aplicado:\n- " + string.Join("\n- ", log), "OK");
        }

        /// Every portrait HUD under this root: the "Vertical" branch of an OrientationPanel, wherever it sits.
        static int ApplyToHuds(Transform root, Sprite frame, Sprite back, Sprite bar, Sprite button,
                               Sprite gear, string where, List<string> log)
        {
            var done = 0;
            foreach (var vertical in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Vertical").ToList())
            {
                var top = Descendant(vertical, "TopPanel");
                var bottom = Descendant(vertical, "BottomPanel (1)");
                if (top == null) continue;                     // not a HUD, just something else called Vertical
                log.Add($"— {where}: {Path(vertical)}");
                HideBackdrops(vertical, log);
                BuildTopFrame(top, frame, back, log);
                if (bottom != null) BuildBoosterBar(bottom, bar, button, gear, log);
                else log.Add("barra de reforços: \"BottomPanel (1)\" não encontrado");
                done++;
            }
            return done;
        }

        static RectTransform Descendant(Transform root, string name) =>
            root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == name);

        static string Path(Transform t)
        {
            var parts = new List<string>();
            for (var c = t; c != null; c = c.parent) parts.Insert(0, c.name);
            return string.Join("/", parts);
        }

        // ---------- top HUD ----------

        static void BuildTopFrame(RectTransform top, Sprite frameSprite, Sprite back, List<string> log)
        {
            var scaler = top.GetComponentInParent<CanvasScaler>();
            var canvasHeight = scaler != null ? scaler.referenceResolution.y : 2048f;
            var width = top.rect.width * ArtW / (ContentRight - ContentLeft);
            var height = width * ArtH / ArtW;
            var scale = width / ArtW;

            var frame = Child(top, FrameName);
            frame.SetAsFirstSibling();
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 1f);
            frame.pivot = new Vector2(0.5f, 1f);
            frame.sizeDelta = new Vector2(width, height);
            frame.anchoredPosition = Vector2.zero;
            Picture(frame, frameSprite, raycast: false);
            log.Add($"moldura da fase no alto: {width:0}x{height:0}, ou seja {height / canvasHeight:P0} da altura da tela" +
                    (Mathf.Abs(ArtW / ArtH - TargetRatio) > 0.5f
                        ? $" — para caber na faixa de ~12% a moldura-fase.png precisa de ser {TargetRatio:0.0}:1 e é {ArtW / ArtH:0.0}:1"
                        : ""));

            // The portrait circle, the moves panel and the goals panel keep their own contents; they are only moved
            // onto the frame and scaled to the slot painted for them.
            var portrait = top.Find("Image") as RectTransform;
            if (portrait != null)
            {
                PlaceInSlot(portrait, frame, HoleArt, scale, stretch: true);
                BuildPortrait(portrait, frame, HoleArt, scale, back);
                log.Add("retrato da gerente no furo redondo, recortado pelo círculo");
            }
            else log.Add("retrato: Vertical/TopPanel/Image não encontrado");

            foreach (var pair in new[] { ("Moves", MovesArt), ("Target", GoalsArt) })
            {
                var slot = Descendant(top, pair.Item1);
                if (slot == null) { log.Add($"{pair.Item1}: não encontrado no TopPanel"); continue; }
                PlaceInSlot(slot, frame, pair.Item2, scale, stretch: false);
                // The goals drop a little so the heading above them has room.
                if (pair.Item1 == "Target")
                    slot.anchoredPosition -= new Vector2(0, GoalsArt.height * scale * 0.13f);
                // As in the mock-up: the words ("Moves", "Metas") are solid dark red — the "texts" sheet — and only the
                // figures are the cream face with the dark red outline — the "numbers" sheet.
                foreach (var label in slot.GetComponentsInChildren<TMP_Text>(true))
                    Ink(label, IsWord(label.name));
            }
            BuildGoalsTitle(frame, scale, log);
            log.Add("movimentos e objetivos: palavras em vinho sólido, números em creme com contorno");
        }

        /// The heading over the goal icons, matching the "MOVES" of the panel beside it.
        static void BuildGoalsTitle(RectTransform frame, float scale, List<string> log)
        {
            var title = Child(frame, GoalsTitleName);
            title.anchorMin = title.anchorMax = new Vector2(0.5f, 0.5f);
            title.pivot = new Vector2(0.5f, 1f);
            title.sizeDelta = new Vector2(GoalsArt.width * scale, GoalsArt.height * scale * 0.26f);
            title.anchoredPosition = new Vector2((GoalsArt.center.x - ArtW * 0.5f) * scale,
                                                 (ArtH * 0.5f - GoalsArt.yMin) * scale - GoalsArt.height * scale * 0.06f);
            var label = title.GetComponent<TextMeshProUGUI>();
            if (label == null) label = title.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = "Metas";
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10;
            label.fontSizeMax = 200;
            label.raycastTarget = false;
            Ink(label, word: true);
            EditorUtility.SetDirty(label);
            log.Add("título \"Metas\" sobre os objetivos");
        }

        /// Sweet Sugar's names for what is a word rather than a figure: "label" is the word over the moves counter and
        /// "Description" the line under the goals; everything else in these panels is a number.
        static bool IsWord(string name) =>
            name.Equals("label", System.StringComparison.OrdinalIgnoreCase) || name == "Description";

        /// The Magic Mail alphabet: words in solid dark red, figures in the cream face with the dark red outline.
        static void Ink(TMP_Text label, bool word)
        {
            if (label is TextMeshProUGUI ugui) RoyalAvesFonts.ApplyMail(ugui, numbers: !word);
            EditorUtility.SetDirty(label);
        }

        /// Moves a top-panel child onto the frame. "stretch" resizes it to the slot; otherwise it keeps its own
        /// rectangle and is scaled to fit, which leaves Sweet Sugar's counters and goal icons laid out as they were.
        static void PlaceInSlot(RectTransform rect, RectTransform frame, Rect art, float scale, bool stretch)
        {
            var size = new Vector2(art.width, art.height) * scale;
            var spot = new Vector2(art.center.x - ArtW * 0.5f, ArtH * 0.5f - art.center.y) * scale;
            rect.SetParent(frame, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = spot;
            if (stretch)
            {
                rect.sizeDelta = size;
                rect.localScale = Vector3.one;
            }
            else
            {
                var own = rect.sizeDelta;
                var fit = own.x > 0 && own.y > 0 ? Mathf.Min(size.x / own.x, size.y / own.y) : 1f;
                rect.localScale = Vector3.one * fit;
            }
        }

        /// The circle is a mask: the manager is cut by its curve, not by the straight bottom edge of her picture.
        /// The gold ring is a sibling drawn over it, so the mask does not eat it.
        static void BuildPortrait(RectTransform circle, RectTransform frame, Rect art, float scale, Sprite back)
        {
            var disc = Picture(circle, back, raycast: false);
            disc.type = Image.Type.Simple;
            disc.preserveAspect = true;
            var mask = circle.GetComponent<Mask>();
            if (mask == null) mask = circle.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            // Sweet Sugar's confectioner rig is off (Aplicar gerente ruiva) but still inside; it must not be masked in.
            if (circle.Find("character_main") is Transform old) old.gameObject.SetActive(false);

            var manager = circle.Find("Gerente") as RectTransform;
            if (manager != null)
            {
                // Framed like the mock-up: head large but not filling everything, shoulders showing and cut by the
                // circle. In her picture the hair starts at 0.01 of the height and the neck — the narrowest point — is
                // at 0.65, so a bust the width of the circle, dropped a tenth of it, puts the head across the top two
                // thirds and carries the shoulders past the bottom edge, where the mask cuts them on the curve.
                manager.anchorMin = new Vector2(0f, -0.10f);
                manager.anchorMax = new Vector2(1f, 0.90f);
                manager.offsetMin = manager.offsetMax = Vector2.zero;
                manager.pivot = new Vector2(0.5f, 0.5f);
                manager.localScale = Vector3.one;
                manager.SetAsLastSibling();
            }

            // The user wants her straight in the leather: the gold ring the art came with is not drawn, and an older
            // run that did draw it gets it removed.
            if (frame.Find(RingName) is Transform ring) Object.DestroyImmediate(ring.gameObject);
        }

        static void HideBackdrops(Transform vertical, List<string> log)
        {
            var off = 0;
            foreach (var image in vertical.GetComponentsInChildren<Image>(true))
            {
                if (!image.enabled || image.sprite == null) continue;
                if (!SweetSugarBackdrops.Contains(image.sprite.name)) continue;
                image.enabled = false;
                EditorUtility.SetDirty(image);
                off++;
            }
            if (off > 0) log.Add($"fundos do Sweet Sugar desligados: {off} (faixa rosa, caixa dos movimentos, barra das metas, rodapé)");
        }

        // ---------- booster bar ----------

        static void BuildBoosterBar(RectTransform panel, Sprite barSprite, Sprite buttonSprite, Sprite gearIcon, List<string> log)
        {
            // The band is drawn at the size it was painted and only repeats sideways: stretching it in height would
            // squash the gold lip, and a tiled sprite clips whatever does not fit a whole tile.
            const float scale = 1f;
            var bar = Child(panel, BarName);
            bar.SetAsFirstSibling();
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.sizeDelta = new Vector2(0f, (BarStripBottom - BarStripTop) * scale);
            bar.anchoredPosition = Vector2.zero;
            var barImage = Picture(bar, barSprite, raycast: false);
            barImage.type = Image.Type.Tiled;   // the stitched middle repeats instead of smearing
            barImage.preserveAspect = false;

            var buttons = panel.GetComponentsInChildren<BoostIcon>(true)
                .Where(b => b.gameObject.activeSelf)
                .Select(b => (RectTransform)b.transform)
                .ToList();
            if (buttons.Count == 0)
            {
                log.Add("barra de reforços posta, mas nenhum botão de reforço ativo foi encontrado");
                return;
            }

            // The five buttons of the picture are evenly spaced. The settings gear takes the last one, as in the
            // mock-up, so the row holds the active boosters plus it; fewer boosters simply leave the row centred.
            var size = ButtonArtSize * scale;
            var step = (ButtonArtX[1] - ButtonArtX[0]) * scale;
            var y = (BarStripBottom - ButtonArtY) * scale;
            var slots = buttons.Count + 1;
            var first = -step * (slots - 1) * 0.5f;
            for (var i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                button.SetParent(bar, false);
                Round(button, new Vector2(first + step * i, y), size);
                if (button.GetComponent<Image>() is Image background)
                {
                    background.sprite = buttonSprite;
                    background.type = Image.Type.Simple;
                    background.preserveAspect = true;
                    EditorUtility.SetDirty(background);
                }
                // Choosing one booster locks the others: Sweet Sugar shows their "Lock" child, which still carried the
                // old button art. It becomes the same round button, darkened, so a locked one reads as itself dimmed.
                if (Descendant(button, "Lock") is RectTransform padlock)
                {
                    padlock.anchorMin = padlock.anchorMax = new Vector2(0.5f, 0.5f);
                    padlock.pivot = new Vector2(0.5f, 0.5f);
                    padlock.sizeDelta = new Vector2(size, size);
                    padlock.anchoredPosition = Vector2.zero;
                    var dim = Picture(padlock, buttonSprite, raycast: false);
                    dim.preserveAspect = true;
                    dim.color = new Color(0.32f, 0.34f, 0.32f, 0.82f);
                    foreach (var extra in padlock.GetComponentsInChildren<Image>(true))
                        if (extra != dim) extra.enabled = false;   // the old padlock drawing on top of it
                }
            }
            BuildGearButton(bar, buttonSprite, gearIcon, new Vector2(first + step * buttons.Count, y), size);
            log.Add($"barra de reforços com {buttons.Count} reforços + a engrenagem no último círculo");
        }

        /// The gear of the bar. It only fires Sweet Sugar's own gear, which stays in CanvasGlobal (SettingsGearProxy).
        static void BuildGearButton(RectTransform bar, Sprite buttonSprite, Sprite gearIcon, Vector2 spot, float size)
        {
            var gear = Child(bar, GearName);
            gear.SetAsLastSibling();
            Round(gear, spot, size);
            var background = Picture(gear, buttonSprite, raycast: true);
            background.preserveAspect = true;

            var icon = Child(gear, "Icon");
            icon.anchorMin = new Vector2(0.2f, 0.2f);
            icon.anchorMax = new Vector2(0.8f, 0.8f);
            icon.offsetMin = icon.offsetMax = Vector2.zero;
            icon.pivot = new Vector2(0.5f, 0.5f);
            Picture(icon, gearIcon, raycast: false).preserveAspect = true;

            var button = gear.GetComponent<Button>();
            if (button == null) button = gear.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            if (gear.GetComponent<SettingsGearProxy>() == null) gear.gameObject.AddComponent<SettingsGearProxy>();
            EditorUtility.SetDirty(gear);
        }

        static void Round(RectTransform rect, Vector2 spot, float size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = spot;
            rect.localScale = Vector3.one;
        }

        // ---------- board ----------

        [MenuItem("Royal Aves/Aplicar moldura do tabuleiro")]
        static void BoardOnlyFromMenu()
        {
            if (!RoyalAvesTools.CanRunTool()) return;
            RoyalAvesReskin.EnsureImportSettings();
            var log = new List<string>();
            AddBoardFrame(log);
            EditorUtility.DisplayDialog("Moldura do tabuleiro", "- " + string.Join(FrameNewLine, log) +
                FrameBlankLine + "Espessura, folga e profundidade ficam no proprio asset, no Inspector. " +
                "Os cantos alternativos (-b) estao na mesma pasta, e para os trocar basta arrasta-los para os campos.", "OK");
        }

        // The pieces the user drew, all normalised to the same 256 px band by the import step.
        static readonly string[] FramePieces =
            { "canto-cima-esq", "canto-cima-dir", "canto-baixo-esq", "canto-baixo-dir", "faixa-horizontal", "faixa-vertical" };

        // The frame is not put in game.unity and gameStatic.unity: it assembles itself when the game starts, from
        // this asset (BoardFrame.Spawn). One place to configure, both scenes served, Sweet Sugar scenes untouched.
        static void AddBoardFrame(List<string> log)
        {
            var art = FramePieces.Select(n => AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Moldura/" + n + ".png")).ToArray();
            if (art.Any(a => a == null))
            {
                log.Add("moldura do tabuleiro: faltam pecas em " + Art + "Moldura (" + string.Join(", ", FramePieces) + ")");
                return;
            }
            var path = "Assets/RoyalAves/Resources/" + BoardFrameSettings.ResourcePath + ".asset";
            var settings = AssetDatabase.LoadAssetAtPath<BoardFrameSettings>(path);
            var made = settings == null;
            if (made)
            {
                settings = ScriptableObject.CreateInstance<BoardFrameSettings>();
                AssetDatabase.CreateAsset(settings, path);
            }
            // Only the pieces are set; the fitting stays as the user left it in the Inspector.
            settings.cornerTopLeft = art[0];
            settings.cornerTopRight = art[1];
            settings.cornerBottomLeft = art[2];
            settings.cornerBottomRight = art[3];
            settings.edgeHorizontal = art[4];
            settings.edgeVertical = art[5];
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            log.Add((made ? "moldura do tabuleiro criada em " : "moldura do tabuleiro actualizada em ") + path +
                    " (4 cantos + 2 faixas, montada sozinha ao entrar no jogo)");
            RemoveStrayFrames(log);
        }

        // An earlier version put the frame in the scene, and a run that failed half way could leave the bare object
        // behind. It has no business in a scene now, so any that is open gets cleared out.
        static void RemoveStrayFrames(List<string> log)
        {
            var removed = 0;
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var stray in scene.GetRootGameObjects().Where(g => g.name == "MolduraTabuleiro").ToList())
                {
                    Object.DestroyImmediate(stray);
                    EditorSceneManager.MarkSceneDirty(scene);
                    removed++;
                }
            }
            if (removed > 0) log.Add($"objectos MolduraTabuleiro soltos removidos das cenas abertas: {removed} (grave a cena)");
        }

        // ---------- helpers ----------

        static Sprite Load(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + file + ".png");

        static RectTransform Child(RectTransform parent, string name)
        {
            if (parent.Find(name) is RectTransform found) return found;
            var made = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            made.gameObject.layer = parent.gameObject.layer;
            made.SetParent(parent, false);
            made.localScale = Vector3.one;
            return made;
        }

        static Image Picture(RectTransform rect, Sprite sprite, bool raycast)
        {
            var image = rect.GetComponent<Image>();
            if (image == null) image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;   // callers that stretch or tile say so themselves
            image.preserveAspect = false;
            image.raycastTarget = raycast;
            EditorUtility.SetDirty(image);
            return image;
        }
    }
}
