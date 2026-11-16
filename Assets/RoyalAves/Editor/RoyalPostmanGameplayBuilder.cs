// Troca a tela de gameplay pelo layout do mock "Royal Postman" (Assets/Royal Postman Art/Screens/Gameplay/mockup.png):
// HUD do alto (placas Objetivos/Jogadas sobre a base roxa, avatar sextavado à direita), barra de reforços com os
// quadros roxo/verde e a engrenagem, fundo novo e tabuleiro xadrez (via RoyalPostmanGameplayKit em Resources).
// Contadores de jogadas e ícones de metas do Sweet Sugar continuam os mesmos: só mudam de lugar e de moldura.
// Só a orientação vertical ("Vertical" do OrientationPanel) é refeita, como no builder anterior.
// Menu: Royal Aves > Aplicar tela de jogo Royal Postman. Rodar de novo só atualiza o que ele criou.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoyalAves.Meta;
using SweetSugar.Scripts.GUI.Boost;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    public static class RoyalPostmanGameplayBuilder
    {
        const string ArtRoot = "Assets/Royal Postman Art/";
        const string KitPath = "Assets/RoyalAves/Resources/RoyalPostmanGameplayKit.asset";
        const string OrientationPrefab = "Assets/SweetSugar/Scripts/System/Orientation/OrientationPanel.prefab";
        static readonly string[] ScenePaths = { "Assets/SweetSugar/Scenes/game.unity", "Assets/SweetSugar/Scenes/gameStatic.unity" };

        const string HudName = "HudRoyalPostman";
        const string AvatarName = "AvatarRoyalPostman";
        const string ObjectivesLabelName = "PlacaObjetivos";
        const string MovesLabelName = "PlacaJogadas";
        const string SelectedName = "Selecionado";

        // Medidas em pixels de superior hud/base.png (1007x231), origem no canto superior esquerdo, lidas do mock.
        const float BaseW = 1007f, BaseH = 231f;
        static readonly Rect GoalsSlot = Rect.MinMaxRect(52, 58, 440, 176);
        static readonly Rect MovesSlot = Rect.MinMaxRect(540, 59, 669, 176);
        static readonly Vector2 PlaqueSize = new Vector2(257, 101);
        static readonly Vector2 GoalsPlaqueCenter = new Vector2(235, -6);
        static readonly Vector2 MovesPlaqueCenter = new Vector2(604, -6);
        static readonly Vector2 AvatarCenter = new Vector2(916, 100);
        static readonly Vector2 AvatarSize = new Vector2(282, 302);
        // A placa e o avatar passam do topo da base; o quadro desce esse tanto para nada sair da tela.
        const float TopOverhang = 58f;
        const float HudWidthOfPanel = 0.94f;

        // Barra de reforços, em pixels do mock (1082 de largura) levados ao canvas de 1536: fator 1.42.
        const float FrameW = 290f, FrameH = 269f;          // base_frame.png 245x227
        const float OverlayRatio = 0.80f;                  // center_frame.png (196x169) dentro de base_frame.png
        const float ButtonStep = 312f, GearX = 633f, RowY = 150f, GearSize = 163f;

        class Art
        {
            public Sprite bg, hudBase, plaque, avatarContainer, avatarFace, frame, frameSelected,
                square, squareLight, squareDark, gear;
        }

        [MenuItem("Royal Aves/Aplicar tela de jogo Royal Postman")]
        static void ApplyFromMenu()
        {
            if (!RoyalAvesTools.CanRunTool()) return;
            var log = new List<string>();
            ConfigureSprites(log);
            var art = LoadArt();
            var missing = new[] { art.bg, art.hudBase, art.plaque, art.avatarContainer, art.avatarFace, art.frame,
                art.frameSelected, art.squareLight, art.squareDark }.Any(s => s == null);
            if (missing)
            {
                EditorUtility.DisplayDialog("Tela de jogo Royal Postman",
                    "Alguma imagem de " + ArtRoot + " não virou sprite. Veja o Console e rode de novo.", "OK");
                return;
            }

            BuildKit(art, log);

            var root = PrefabUtility.LoadPrefabContents(OrientationPrefab);
            try
            {
                if (ApplyToHuds(root.transform, art, "OrientationPanel.prefab", log) > 0)
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
                    var done = scene.GetRootGameObjects().Sum(g => ApplyToHuds(g.transform, art, scene.name, log));
                    if (done == 0) { log.Add($"{scene.name}: nenhum HUD vertical encontrado"); continue; }
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) log.Add($"{scene.name}: NÃO consegui gravar a cena");
                }
            }
            finally
            {
                if (setup.Length > 0 && setup.All(x => !string.IsNullOrEmpty(x.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Tela de jogo Royal Postman", "Aplicado:\n- " + string.Join("\n- ", log), "OK");
        }

        // ---------- imagens ----------

        /// As imagens vieram em Multiple sem fatias (não geram sprite): viram Single, sem compressão nem mipmaps.
        static void ConfigureSprites(List<string> log)
        {
            var changed = 0;
            var dir = Path.GetFullPath(ArtRoot);
            foreach (var file in Directory.GetFiles(dir, "*.png", SearchOption.AllDirectories))
            {
                var path = ArtRoot + file.Substring(dir.Length).Replace('\\', '/');
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
                if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single
                    && !importer.mipmapEnabled) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
                changed++;
            }
            if (changed > 0) log.Add($"{changed} imagens de Royal Postman Art importadas como sprite");
        }

        static Sprite S(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + file);

        static Art LoadArt() => new Art
        {
            bg = S("Screens/Gameplay/Background/bg.png"),
            hudBase = S("Screens/Gameplay/Frames/superior hud/base.png"),
            plaque = S("Screens/Gameplay/Frames/superior hud/pannel.png"),
            avatarContainer = S("Screens/Gameplay/Frames/superior hud/avatar_container.png"),
            avatarFace = S("Screens/Gameplay/Frames/avatar/happy.png"),
            frame = S("Screens/Gameplay/Frames/Power ups/base_frame.png"),
            frameSelected = S("Screens/Gameplay/Frames/Power ups/center_frame.png"),
            squareLight = S("chess/square_light.png"),
            squareDark = S("chess/square_dark.png"),
            square = S("chess/square_bg.png"),
            gear = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/UI/Buttons/engrenagem-plana.png"),
        };

        static void BuildKit(Art art, List<string> log)
        {
            var kit = AssetDatabase.LoadAssetAtPath<RoyalPostmanGameplayKit>(KitPath);
            if (kit == null)
            {
                kit = ScriptableObject.CreateInstance<RoyalPostmanGameplayKit>();
                AssetDatabase.CreateAsset(kit, KitPath);
            }
            kit.background = art.bg;
            kit.squareLight = art.squareLight;
            kit.squareDark = art.squareDark;
            kit.squareUnder = art.square;
            EditorUtility.SetDirty(kit);
            log.Add("kit de runtime (fundo e casas xadrez) em " + KitPath);
        }

        // ---------- HUD ----------

        static int ApplyToHuds(Transform root, Art art, string where, List<string> log)
        {
            var done = 0;
            foreach (var vertical in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Vertical").ToList())
            {
                var top = Descendant(vertical, "TopPanel");
                if (top == null) continue;                    // outra coisa chamada Vertical
                log.Add($"— {where}: {PathOf(vertical)}");
                BuildTop(top, art, log);
                var bottom = Descendant(vertical, "BottomPanel (1)");
                if (bottom != null) BuildBottom(bottom, art, log);
                else log.Add("barra de reforços: \"BottomPanel (1)\" não encontrado");
                done++;
            }
            return done;
        }

        static void BuildTop(RectTransform top, Art art, List<string> log)
        {
            var width = top.rect.width * HudWidthOfPanel;
            var s = width / BaseW;

            var hud = Child(top, HudName);
            hud.SetAsFirstSibling();
            hud.anchorMin = hud.anchorMax = new Vector2(0.5f, 1f);
            hud.pivot = new Vector2(0.5f, 1f);
            hud.sizeDelta = new Vector2(width, BaseH * s);
            hud.anchoredPosition = new Vector2(0f, -TopOverhang * s);
            hud.localScale = Vector3.one;
            Picture(hud, art.hudBase, false);

            // Contadores: mesmos objetos, agora dentro das duas caixas creme da base.
            var moves = Descendant(top, "Moves");
            var goals = Descendant(top, "Target");
            if (goals != null) FitGoals(goals, hud, s);
            else log.Add("Target (objetivos) não encontrado no TopPanel");
            if (moves != null) FitMoves(moves, hud, s);
            else log.Add("Moves (jogadas) não encontrado no TopPanel");

            Plaque(hud, ObjectivesLabelName, "Objetivos", GoalsPlaqueCenter, art.plaque, s);
            Plaque(hud, MovesLabelName, "Jogadas", MovesPlaqueCenter, art.plaque, s);

            var avatar = Child(hud, AvatarName);
            Place(avatar, AvatarCenter, AvatarSize, s);
            Picture(avatar, art.avatarContainer, false).preserveAspect = true;
            var face = Child(avatar, "Rosto");
            face.anchorMin = Vector2.zero;
            face.anchorMax = Vector2.one;
            face.offsetMin = face.offsetMax = Vector2.zero;
            Picture(face, art.avatarFace, false).preserveAspect = true;
            avatar.SetAsLastSibling();

            // Moldura, título e retrato da versão anterior: ficam desligados (os contadores já saíram de dentro deles).
            foreach (var oldName in new[] { "MolduraFase", "AnelRetrato" })
                if (Descendant(top, oldName) is RectTransform old && old != hud) old.gameObject.SetActive(false);
            if (top.Find("Image") is Transform portrait && portrait.parent == top) portrait.gameObject.SetActive(false);

            log.Add($"HUD do alto: base {width:0}x{BaseH * s:0}, placas Objetivos/Jogadas e avatar sextavado");
        }

        static void FitGoals(RectTransform goals, RectTransform hud, float s)
        {
            var size = new Vector2(GoalsSlot.width, GoalsSlot.height) * s;
            goals.SetParent(hud, false);
            goals.anchorMin = goals.anchorMax = new Vector2(0.5f, 0.5f);
            goals.pivot = new Vector2(0.5f, 0.5f);
            goals.anchoredPosition = SlotSpot(GoalsSlot, s);
            var own = goals.sizeDelta;
            var fit = own.x > 0 && own.y > 0 ? Mathf.Min(size.x / own.x, size.y / own.y) : 1f;
            goals.localScale = Vector3.one * fit;
            foreach (var label in goals.GetComponentsInChildren<TextMeshProUGUI>(true))
                RoyalAvesFonts.ApplyMail(label, numbers: label.name != "Description");
            EditorUtility.SetDirty(goals);
        }

        static void FitMoves(RectTransform moves, RectTransform hud, float s)
        {
            var size = new Vector2(MovesSlot.width, MovesSlot.height) * s;
            moves.SetParent(hud, false);
            moves.anchorMin = moves.anchorMax = new Vector2(0.5f, 0.5f);
            moves.pivot = new Vector2(0.5f, 0.5f);
            moves.anchoredPosition = SlotSpot(MovesSlot, s);
            moves.sizeDelta = size;
            moves.localScale = Vector3.one;
            // A palavra "Moves" sai: a placa "Jogadas" faz esse papel. O número ocupa a caixa inteira.
            if (Descendant(moves, "label") is RectTransform word) word.gameObject.SetActive(false);
            if (Descendant(moves, "Limit") is RectTransform number)
            {
                number.anchorMin = Vector2.zero;
                number.anchorMax = Vector2.one;
                number.offsetMin = number.offsetMax = Vector2.zero;
                number.localScale = Vector3.one;
                if (number.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI text)
                {
                    RoyalAvesFonts.ApplyMail(text, numbers: true);
                    text.alignment = TextAlignmentOptions.Center;
                    text.enableAutoSizing = true;
                    text.fontSizeMin = 20;
                    text.fontSizeMax = 220;
                    text.margin = Vector4.zero;
                    EditorUtility.SetDirty(text);
                }
            }
            EditorUtility.SetDirty(moves);
        }

        static void Plaque(RectTransform hud, string name, string text, Vector2 center, Sprite plaque, float s)
        {
            var rect = Child(hud, name);
            Place(rect, center, PlaqueSize, s);
            Picture(rect, plaque, false);
            var label = Child(rect, "Texto");
            label.anchorMin = Vector2.zero;
            label.anchorMax = Vector2.one;
            label.offsetMin = new Vector2(PlaqueSize.x * s * 0.08f, PlaqueSize.y * s * 0.14f);
            label.offsetMax = new Vector2(-PlaqueSize.x * s * 0.08f, -PlaqueSize.y * s * 0.14f);
            var tmp = label.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = label.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 10;
            tmp.fontSizeMax = 200;
            tmp.raycastTarget = false;
            RoyalAvesFonts.ApplyMail(tmp, numbers: true);   // creme com contorno escuro, como no mock
            EditorUtility.SetDirty(tmp);
        }

        static Vector2 SlotSpot(Rect slot, float s) => new Vector2(slot.center.x - BaseW * 0.5f, BaseH * 0.5f - slot.center.y) * s;

        static void Place(RectTransform rect, Vector2 artCenter, Vector2 artSize, float s)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = artSize * s;
            rect.anchoredPosition = new Vector2(artCenter.x - BaseW * 0.5f, BaseH * 0.5f - artCenter.y) * s;
            rect.localScale = Vector3.one;
        }

        // ---------- barra de reforços ----------

        static void BuildBottom(RectTransform panel, Art art, List<string> log)
        {
            var bar = Descendant(panel, "BarraReforcos");
            if (bar == null)
            {
                log.Add("barra de reforços: \"BarraReforcos\" não encontrada (rode antes Aplicar moldura da tela de jogo)");
                return;
            }
            // A faixa da versão anterior some; os botões e a engrenagem ficam e ganham os quadros do mock.
            if (bar.GetComponent<Image>() is Image strip) { strip.enabled = false; EditorUtility.SetDirty(strip); }

            var boosts = bar.GetComponentsInChildren<BoostIcon>(true)
                .Where(b => b.gameObject.activeSelf && b.transform.parent == bar)
                .OrderBy(b => ((RectTransform)b.transform).anchoredPosition.x)
                .ToList();
            if (boosts.Count == 0) { log.Add("barra de reforços: nenhum reforço ativo encontrado"); return; }

            for (var i = 0; i < boosts.Count; i++)
            {
                var x = GearX - ButtonStep * (boosts.Count - i);
                StyleBooster(boosts[i], new Vector2(x, RowY), art);
            }

            if (bar.Find("BotaoConfiguracoes") is RectTransform gear)
            {
                gear.anchorMin = gear.anchorMax = new Vector2(0.5f, 0f);
                gear.pivot = new Vector2(0.5f, 0.5f);
                gear.sizeDelta = new Vector2(GearSize, GearSize);
                gear.anchoredPosition = new Vector2(GearX, RowY);
                gear.localScale = Vector3.one;
                EditorUtility.SetDirty(gear);
            }
            log.Add($"barra de reforços: {boosts.Count} reforços em quadros roxos (verde quando ativo) + engrenagem");
        }

        static void StyleBooster(BoostIcon boost, Vector2 spot, Art art)
        {
            var rect = (RectTransform)boost.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(FrameW, FrameH);
            rect.anchoredPosition = spot;
            rect.localScale = Vector3.one;

            var background = Picture(rect, art.frame, true);
            background.preserveAspect = false;

            // Quadro verde por cima do roxo, só enquanto este é o reforço ativo.
            var overlay = Child(rect, SelectedName);
            overlay.anchorMin = overlay.anchorMax = new Vector2(0.5f, 0.5f);
            overlay.pivot = new Vector2(0.5f, 0.5f);
            overlay.sizeDelta = new Vector2(FrameW * OverlayRatio, FrameW * OverlayRatio * art.frameSelected.rect.height / art.frameSelected.rect.width);
            overlay.anchoredPosition = new Vector2(0f, FrameH * 0.03f);
            Picture(overlay, art.frameSelected, false).preserveAspect = true;
            overlay.SetSiblingIndex(0);
            overlay.gameObject.SetActive(false);

            // A figura do reforço fica sobre os dois quadros.
            var icon = rect.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("Icon")) as RectTransform;
            if (icon != null)
            {
                icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 0.5f);
                icon.pivot = new Vector2(0.5f, 0.5f);
                icon.sizeDelta = new Vector2(FrameW * 0.66f, FrameW * 0.66f);
                icon.anchoredPosition = new Vector2(0f, FrameH * 0.03f);
                icon.localScale = Vector3.one;
                icon.SetSiblingIndex(1);
                if (icon.GetComponent<Image>() is Image iconImage) iconImage.preserveAspect = true;
                EditorUtility.SetDirty(icon);
            }

            // Reforço bloqueado (outro está ativo): o mesmo quadro roxo, escurecido.
            if (Descendant(rect, "Lock") is RectTransform locked)
            {
                locked.anchorMin = locked.anchorMax = new Vector2(0.5f, 0.5f);
                locked.pivot = new Vector2(0.5f, 0.5f);
                locked.sizeDelta = new Vector2(FrameW, FrameH);
                locked.anchoredPosition = Vector2.zero;
                var dim = Picture(locked, art.frame, false);
                dim.color = new Color(0.32f, 0.3f, 0.34f, 0.82f);
                foreach (var extra in locked.GetComponentsInChildren<Image>(true))
                    if (extra != dim) extra.enabled = false;
            }

            // Número de usos: bolinha no canto de baixo à direita, saindo um pouco do quadro como no mock.
            foreach (var badge in new[] { boost.counter, boost.plus })
            {
                if (badge == null) continue;
                var badgeRect = (RectTransform)badge.transform;
                badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(0.5f, 0.5f);
                badgeRect.pivot = new Vector2(0.5f, 0.5f);
                badgeRect.anchoredPosition = new Vector2(FrameW * 0.36f, -FrameH * 0.44f);
                badgeRect.sizeDelta = Vector2.one * 96f;
                EditorUtility.SetDirty(badgeRect);
            }

            var look = boost.GetComponent<BoostSelectedLook>();
            if (look == null) look = boost.gameObject.AddComponent<BoostSelectedLook>();
            var serialized = new SerializedObject(look);
            serialized.FindProperty("selectedOverlay").objectReferenceValue = overlay.gameObject;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(boost);
        }

        // ---------- helpers ----------

        static RectTransform Descendant(Transform root, string name) =>
            root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == name);

        static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (var c = t; c != null; c = c.parent) parts.Insert(0, c.name);
            return string.Join("/", parts);
        }

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
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = raycast;
            image.enabled = true;
            EditorUtility.SetDirty(image);
            return image;
        }
    }
}
