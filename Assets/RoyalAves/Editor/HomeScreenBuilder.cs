// Monta a tela inicial nova DENTRO do CanvasGlobal da cena aberta, só com as artes de
// Assets/Royal Postman Art/Screens/Home (+ botão de configurações de Buttons/Rounded e botão verde de Screens/Modal):
//
//   Royal Aves > Tela inicial > Criar no CanvasGlobal
//       cria CanvasGlobal/Home (primeiro filho, atrás das modais) com barra de cima, Play e menu inferior, já
//       ligados ao componente HomeScreen. É um ponto de partida: ajuste posições/tamanhos na cena e salve.
//   Royal Aves > Tela inicial > Remover RoyalAvesLobby da cena
//       apaga da cena o canvas antigo do lobby (o prefab e os scripts continuam no projeto).
using System.Linq;
using RoyalAves.Meta;
using SweetSugar.Scripts.System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    public static class HomeScreenBuilder
    {
        const string Art = "Assets/Royal Postman Art/";
        const string HomeArt = Art + "Screens/Home/";
        const string FontPath = "Assets/RoyalAves/Fonts/LilitaOne SDF.asset";
        // As artes foram desenhadas para ~1075 px de largura útil; o CanvasGlobal tem 1536 de referência.
        const float K = 1536f / 1075f;

        [MenuItem("Royal Aves/Tela inicial/Criar no CanvasGlobal")]
        static void Build()
        {
            var menus = Object.FindFirstObjectByType<MenuReference>(FindObjectsInactive.Include);
            if (menus == null) { EditorUtility.DisplayDialog("Tela inicial", "Não achei o CanvasGlobal (MenuReference) na cena aberta. Abra a cena game.", "OK"); return; }
            var canvas = (RectTransform)menus.transform;

            var old = canvas.Find(HomeScreen.ObjectName);
            if (old != null)
            {
                if (!EditorUtility.DisplayDialog("Tela inicial", "Já existe um objeto Home no CanvasGlobal. Recriar apaga os ajustes feitos nele.", "Recriar", "Cancelar")) return;
                Undo.DestroyObjectImmediate(old.gameObject);
            }

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            var home = New(HomeScreen.ObjectName, canvas); Stretch(home);
            Undo.RegisterCreatedObjectUndo(home.gameObject, "Criar tela inicial");
            home.SetSiblingIndex(0);
            var screen = home.gameObject.AddComponent<HomeScreen>();
            var content = New("Content", home); Stretch(content);

            // ---------- Barra de cima ----------
            var top = Picture("TopHud", content, HomeArt + "superior_hud.png", 991, 154);
            Anchor(top, new Vector2(0.5f, 1f), new Vector2(0f, -(60f + 154f * K * 0.5f)));
            var avatar = Picture("Avatar", top, HomeArt + "avatar_hud.png", 193, 190); Place(avatar, -415, 0);
            var coinIcon = Picture("CoinIcon", top, Art + "Screens/Modal/coin.png", 90, 90); Place(coinIcon, -250, 0);
            var coins = Text("CoinsText", top, font, "0", 62, 220, 100); Place(coins.rectTransform, -120, 0);
            var lives = Text("LivesText", top, font, "5", 62, 160, 100); Place(lives.rectTransform, 120, 0);
            var settings = Picture("Settings", top, Art + "Buttons/Rounded/border_global_rounded.png", 121, 111); Place(settings, 400, 0);
            var settingsCenter = Picture("Center", settings, Art + "Buttons/Rounded/center_golden.png", 101, 92);
            Picture("Icon", settingsCenter, Art + "Buttons/Rounded/config icon.png", 71, 66);
            var settingsButton = MakeButton(settings, settingsCenter);

            // ---------- Menu inferior ----------
            var bar = Picture("BottomBar", content, HomeArt + "inferior_bar.png", 1165, 231);
            Anchor(bar, new Vector2(0.5f, 0f), new Vector2(0f, 231f * K * 0.5f));
            var indicator = Picture("Indicator", bar, HomeArt + "selected_menu.png", 286, 320);
            indicator.pivot = new Vector2(0.5f, 0f); indicator.anchorMin = indicator.anchorMax = new Vector2(0.5f, 0f);
            indicator.anchoredPosition = Vector2.zero;
            indicator.GetComponent<Image>().raycastTarget = false;

            // A arte da barra tem 5 divisões; a pasta Home tem 4 ícones. A 5ª aba fica sem ícone.
            var slots = new[] { -430f, -217f, 0f, 215f, 430f };
            var icons = new[] { "events_icon.png", "collection_icon.png", "home_icon.png", "ranking_icon.png", null };
            var sizes = new[] { new Vector2(141, 150), new Vector2(178, 134), new Vector2(170, 174), new Vector2(154, 154), Vector2.zero };
            var names = new[] { "TabEvents", "TabCollection", "TabHome", "TabRanking", "TabEmpty" };
            var tabButtons = new Button[5]; var tabIcons = new RectTransform[5];
            for (var i = 0; i < 5; i++)
            {
                var tab = New(names[i], bar);
                tab.sizeDelta = new Vector2(212f * K, 215f * K);
                Place(tab, slots[i], -8);
                var hit = tab.gameObject.AddComponent<Image>(); hit.color = new Color(1, 1, 1, 0); // área de toque invisível
                tabButtons[i] = tab.gameObject.AddComponent<Button>();
                tabButtons[i].transition = Selectable.Transition.None;
                if (icons[i] == null) continue;
                tabIcons[i] = Picture("Icon", tab, HomeArt + icons[i], sizes[i].x * 0.8f, sizes[i].y * 0.8f);
                tabIcons[i].GetComponent<Image>().raycastTarget = false;
            }

            // ---------- Play ----------
            // Botão único de Buttons/squared (865x326), que troca de sprite conforme a dificuldade da fase.
            // Normal: moldura laranja + miolo verde (criado por "Ligar Play por dificuldade" no fim).
            // Hard / Super Hard: sprite completo de Buttons/squared no lugar da moldura.
            var play = Picture("PlayButton", content, ModalArt + "button_border.png", 604 * 0.75f, 274 * 0.75f);
            Anchor(play, new Vector2(0.5f, 0f), new Vector2(0f, 231f * K + 60f + 274f * 0.75f * K * 0.5f));
            var level = Text("LevelText", play, font, "1", 80, 520, 140); Place(level.rectTransform, 0, -10);
            var playButton = MakeButton(play, play);
            MakeDifficultyLabel(play, font);

            // ---------- Liga no HomeScreen ----------
            var so = new SerializedObject(screen);
            so.FindProperty("content").objectReferenceValue = content.gameObject;
            so.FindProperty("coinsText").objectReferenceValue = coins;
            so.FindProperty("livesText").objectReferenceValue = lives;
            so.FindProperty("settingsButton").objectReferenceValue = settingsButton;
            so.FindProperty("playButton").objectReferenceValue = playButton;
            so.FindProperty("levelText").objectReferenceValue = level;
            so.FindProperty("indicator").objectReferenceValue = indicator;
            so.FindProperty("startTab").intValue = 2; // Home, no centro
            var tabs = so.FindProperty("tabs");
            tabs.arraySize = 5;
            for (var i = 0; i < 5; i++)
            {
                var element = tabs.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("button").objectReferenceValue = tabButtons[i];
                element.FindPropertyRelative("icon").objectReferenceValue = tabIcons[i];
                element.FindPropertyRelative("screen").objectReferenceValue = null;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            WireDifficulty(screen, play.GetComponent<Image>(), play.Find(DifficultyLabelName)?.GetComponent<TMP_Text>());
            WireDifficultyOnExisting(); // miolo verde, botões + e rótulos do menu

            EditorSceneManager.MarkSceneDirty(home.gameObject.scene);
            Selection.activeGameObject = home.gameObject;
            Debug.Log("Royal Aves: tela inicial criada em CanvasGlobal/Home. Ajuste o layout na cena e salve (Ctrl+S).", home);
        }

        // Para uma tela inicial que JÁ existe (e já foi ajustada): só liga os três sprites de Play por dificuldade e
        // cria o texto "Difícil / Super difícil", sem recriar nem mover nada.
        [MenuItem("Royal Aves/Tela inicial/Ligar Play por dificuldade (sem recriar)")]
        static void WireDifficultyOnExisting()
        {
            var screen = Object.FindFirstObjectByType<HomeScreen>(FindObjectsInactive.Include);
            if (screen == null) { EditorUtility.DisplayDialog("Tela inicial", "Não há tela inicial (HomeScreen) na cena. Use \"Criar no CanvasGlobal\" antes.", "OK"); return; }
            var so = new SerializedObject(screen);
            var button = so.FindProperty("playButton").objectReferenceValue as Button;
            if (button == null) { EditorUtility.DisplayDialog("Tela inicial", "O campo Play Button do HomeScreen está vazio.", "OK"); return; }

            var face = button.GetComponent<Image>();
            // Botão normal = moldura laranja (button_border) + miolo verde (button_center, filho "Center").
            // Hard / Super Hard trocam a moldura por um sprite completo e o miolo some.
            var center = button.transform.Find("Center");
            if (center == null)
            {
                var c = Picture("Center", button.transform, ModalArt + "button_center.png", 547, 214);
                c.anchorMin = new Vector2(0.047f, 0.11f); c.anchorMax = new Vector2(0.953f, 0.89f);
                c.offsetMin = c.offsetMax = Vector2.zero;
                c.SetSiblingIndex(0);
                c.GetComponent<Image>().raycastTarget = false;
                Undo.RegisterCreatedObjectUndo(c.gameObject, "Play por dificuldade");
                center = c;
            }
            playCenterRef = center.GetComponent<Image>();
            var pop = button.GetComponent<ButtonPopPress>();
            if (pop != null)
            {
                var popSo = new SerializedObject(pop);
                popSo.FindProperty("target").objectReferenceValue = button.transform;
                popSo.ApplyModifiedProperties();
            }

            var label = button.transform.Find(DifficultyLabelName)?.GetComponent<TMP_Text>();
            if (label == null)
            {
                label = MakeDifficultyLabel((RectTransform)button.transform, AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath));
                Undo.RegisterCreatedObjectUndo(label.gameObject, "Play por dificuldade");
            }
            if (face != null)
            {
                Undo.RecordObject(face, "Play por dificuldade");
                var border = LoadSprite(ModalArt + "button_border.png");
                if (border != null) face.sprite = border; // prévia do botão normal no modo de edição
            }
            WireDifficulty(screen, face, label);
            var playSo = new SerializedObject(screen);
            playSo.FindProperty("playCenter").objectReferenceValue = playCenterRef;
            playSo.ApplyModifiedProperties();
            // O número da fase fica por cima do miolo.
            var levelText = playSo.FindProperty("levelText").objectReferenceValue as TMP_Text;
            if (levelText != null && levelText.transform.parent == button.transform) levelText.transform.SetAsLastSibling();

            WireHud(screen);
            EditorSceneManager.MarkSceneDirty(screen.gameObject.scene);
            Selection.activeGameObject = button.gameObject;
            Debug.Log("Royal Aves: Play por dificuldade, botões + e rótulos do menu ligados. Ajuste posições/tamanhos e salve a cena.", button);
        }

        static Image playCenterRef;

        // Texto de vidas, botões "+" (moedas/vidas) e rótulos das abas, sobre o que já existe na cena.
        static void WireHud(HomeScreen screen)
        {
            var so = new SerializedObject(screen);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var root = screen.transform;

            // Vidas: o texto que vale é o HeartText do grupo do coração; o LivesText antigo (inativo) sai de cena.
            var heartText = FindDeep(root, "HeartText")?.GetComponent<TMP_Text>();
            if (heartText != null)
            {
                var oldLives = so.FindProperty("livesText").objectReferenceValue as TMP_Text;
                if (oldLives != null && oldLives != heartText) { Undo.RecordObject(oldLives.gameObject, "HUD"); oldLives.gameObject.SetActive(false); }
                so.FindProperty("livesText").objectReferenceValue = heartText;
                var plus = heartText.transform.parent.Find("Plus");
                so.FindProperty("livesPlusButton").objectReferenceValue = MakePlusButton(plus);
            }
            var coinsText = so.FindProperty("coinsText").objectReferenceValue as TMP_Text;
            if (coinsText != null)
                so.FindProperty("coinsPlusButton").objectReferenceValue = MakePlusButton(coinsText.transform.parent.Find("Plus"));

            // Rótulo embaixo de cada aba (aparece só na selecionada).
            var tabs = so.FindProperty("tabs");
            for (var i = 0; i < tabs.arraySize; i++)
            {
                var element = tabs.GetArrayElementAtIndex(i);
                var tabButton = element.FindPropertyRelative("button").objectReferenceValue as Button;
                if (tabButton == null || element.FindPropertyRelative("icon").objectReferenceValue == null) continue;
                var label = element.FindPropertyRelative("label").objectReferenceValue as TMP_Text;
                if (label == null)
                {
                    var existing = tabButton.transform.Find("Label");
                    label = existing != null ? existing.GetComponent<TMP_Text>() : null;
                }
                if (label == null)
                {
                    label = Text("Label", tabButton.transform, font, "", 34, 260, 60);
                    var rt = label.rectTransform;
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                    rt.anchoredPosition = new Vector2(0f, 38f);
                    label.enableAutoSizing = true; label.fontSizeMin = 14; label.fontSizeMax = 52;
                    Undo.RegisterCreatedObjectUndo(label.gameObject, "HUD");
                }
                element.FindPropertyRelative("label").objectReferenceValue = label;
            }
            so.ApplyModifiedProperties();
        }

        static Button MakePlusButton(Transform plus)
        {
            if (plus == null) return null;
            var image = plus.GetComponent<Image>();
            if (image != null) image.raycastTarget = true;
            var button = plus.GetComponent<Button>();
            if (button == null)
            {
                button = Undo.AddComponent<Button>(plus.gameObject);
                button.transition = Selectable.Transition.None;
                button.targetGraphic = image;
            }
            if (plus.GetComponent<ButtonPopPress>() == null) Undo.AddComponent<ButtonPopPress>(plus.gameObject);
            return button;
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        const string SquaredArt = Art + "Buttons/squared/";
        const string ModalArt = Art + "Screens/Modal/";
        const string DifficultyLabelName = "DifficultyLabel";

        static TMP_Text MakeDifficultyLabel(RectTransform play, TMP_FontAsset font)
        {
            // O rebaixo da arte fica no topo do botão, centralizado (~18% da largura, ~10% da altura).
            var label = Text(DifficultyLabelName, play, font, "Hard", 30, 10, 10);
            var rt = label.rectTransform;
            rt.anchorMin = new Vector2(0.37f, 0.80f); rt.anchorMax = new Vector2(0.63f, 0.93f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            label.enableAutoSizing = true; label.fontSizeMin = 10; label.fontSizeMax = 60;
            return label;
        }

        static void WireDifficulty(HomeScreen screen, Image face, TMP_Text label)
        {
            var so = new SerializedObject(screen);
            so.FindProperty("playFace").objectReferenceValue = face;
            so.FindProperty("playNormal").objectReferenceValue = LoadSprite(ModalArt + "button_border.png");
            so.FindProperty("playHard").objectReferenceValue = LoadSprite(SquaredArt + "hard_level.png");
            so.FindProperty("playSuperHard").objectReferenceValue = LoadSprite(SquaredArt + "ultra_hard_level.png");
            so.FindProperty("difficultyLabel").objectReferenceValue = label;
            so.ApplyModifiedProperties();
        }

        [MenuItem("Royal Aves/Tela inicial/Remover RoyalAvesLobby da cena")]
        static void RemoveLobby()
        {
            var lobbies = Object.FindObjectsByType<LobbyController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(l => l.gameObject).Where(g => g.scene.IsValid()).Distinct().ToList();
            if (lobbies.Count == 0) { EditorUtility.DisplayDialog("Tela inicial", "Não há RoyalAvesLobby na cena aberta.", "OK"); return; }
            if (!EditorUtility.DisplayDialog("Tela inicial", "Remover " + lobbies.Count + " objeto(s) RoyalAvesLobby da cena? (O prefab continua no projeto; dá para desfazer com Ctrl+Z.)", "Remover", "Cancelar")) return;
            foreach (var lobby in lobbies)
            {
                var scene = lobby.scene;
                Undo.DestroyObjectImmediate(lobby);
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        // ---------- utilitários ----------
        static RectTransform New(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = parent.gameObject.layer };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void Anchor(RectTransform rt, Vector2 anchor, Vector2 position)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = position;
        }

        /// Posição em pixels da ARTE (multiplicada pela escala do canvas), relativa ao centro do pai.
        static void Place(RectTransform rt, float x, float y) => rt.anchoredPosition = new Vector2(x * K, y * K);

        /// Image com o sprite do caminho, no tamanho da arte (em pixels) multiplicado pela escala do canvas.
        static RectTransform Picture(string name, Transform parent, string path, float width, float height)
        {
            var rt = New(name, parent);
            rt.sizeDelta = new Vector2(width * K, height * K);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(path);
            image.preserveAspect = true;
            if (image.sprite == null) Debug.LogWarning("Royal Aves: não achei o sprite " + path, rt);
            return rt;
        }

        static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, string value, float size, float width, float height)
        {
            var rt = New(name, parent);
            rt.sizeDelta = new Vector2(width * K, height * K);
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = value; text.fontSize = size * K; text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        /// Botão no objeto da moldura, com o efeito de clique (15%) encolhendo só o centro.
        static Button MakeButton(RectTransform frame, RectTransform center)
        {
            var button = frame.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var pop = frame.gameObject.AddComponent<ButtonPopPress>();
            var so = new SerializedObject(pop);
            so.FindProperty("target").objectReferenceValue = center;
            so.ApplyModifiedPropertiesWithoutUndo();
            return button;
        }

        /// Carrega o sprite; se a textura ainda não foi importada como sprite único, ajusta o import.
        static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sprite != null) return sprite;
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return null;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }
    }
}
