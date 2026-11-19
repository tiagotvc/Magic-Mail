// Monta o prefab da tela de configurações (Resources/UI/ConfigurationsScreen.prefab) só com as artes de
// Assets/Royal Postman Art/Screens/Configurations/Pngs, seguindo o mock.png e as modais (mock_modal_*.png).
//   Royal Aves > Tela de configurações > Montar prefab   (apaga e refaz; ajustes feitos à mão no prefab se perdem)
// Se o prefab não existir, ele é montado sozinho quando o projeto abre. Os números abaixo estão nas medidas do mock
// (1080 de largura); K converte para o CanvasGlobal (1536 de referência). O comportamento está em ConfigScreen.cs.
using System.IO;
using System.Linq;
using RoyalAves.Meta;
using RoyalAves.Save;
using SweetSugar.Scripts.System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    [InitializeOnLoad]
    public static class ConfigScreenBuilder
    {
        const string Pngs = "Assets/Royal Postman Art/Screens/Configurations/Pngs/";
        const string PrefabPath = "Assets/Royal Postman Art/Resources/UI/ConfigurationsScreen.prefab";
        const string FontPath = "Assets/RoyalAves/Fonts/LilitaOne SDF.asset";
        const float K = 1536f / 1080f;
        const float MockHeight = 1980f;

        static TMP_FontAsset font;

        static ConfigScreenBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!File.Exists(PrefabPath)) Build(false);
                PlaceInScene(false);
            };
        }

        [MenuItem("Royal Aves/Tela de configurações/Colocar no CanvasGlobal da cena")]
        static void PlaceFromMenu() => PlaceInScene(true);

        // Põe uma instância do prefab dentro do CanvasGlobal da cena aberta (desligada, logo depois do Home), para ela
        // aparecer na hierarquia e poder ser ajustada ali. O jogo abre essa mesma instância ao tocar na engrenagem.
        static void PlaceInScene(bool verbose)
        {
            var menus = Object.FindFirstObjectByType<MenuReference>(FindObjectsInactive.Include);
            if (menus == null) { if (verbose) EditorUtility.DisplayDialog("Tela de configurações", "Não achei o CanvasGlobal (MenuReference) na cena aberta. Abra a cena game.", "OK"); return; }
            var canvas = menus.transform;
            if (canvas.Find(ConfigScreen.ObjectName) != null) { if (verbose) Selection.activeTransform = canvas.Find(ConfigScreen.ObjectName); return; }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
            go.name = ConfigScreen.ObjectName;
            go.SetActive(false);
            var home = canvas.Find(HomeScreen.ObjectName);
            go.transform.SetSiblingIndex(home != null ? home.GetSiblingIndex() + 1 : 0);
            Undo.RegisterCreatedObjectUndo(go, "Colocar tela de configurações");
            EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeGameObject = go;
            Debug.Log("Royal Aves: tela de configurações colocada no CanvasGlobal. Salve a cena (Ctrl+S).");
        }

        [MenuItem("Royal Aves/Tela de configurações/Montar prefab")]
        static void BuildFromMenu() => Build(true);

        static void Build(bool verbose)
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            var root = new GameObject(ConfigScreen.ObjectName, typeof(RectTransform));
            var rootRt = (RectTransform)root.transform;
            Stretch(rootRt);
            var screen = root.AddComponent<ConfigScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("selectedOptionSprite").objectReferenceValue = LoadArt("Buttons/Idioma Modal/button_idioma_selected.png");
            so.FindProperty("otherOptionSprite").objectReferenceValue = LoadArt("Buttons/Idioma Modal/button_idioma_options.png");
            so.ApplyModifiedPropertiesWithoutUndo();

            BuildBackground(rootRt);
            var main = Container("Main", rootRt);
            BuildMain(main);
            BuildLanguageModal(rootRt);
            BuildTermsModal(rootRt);

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            Debug.Log("Royal Aves: prefab da tela de configurações montado em " + PrefabPath);
            if (verbose) EditorUtility.DisplayDialog("Tela de configurações", "Prefab montado em:\n" + PrefabPath + "\n\nPreencha os links (Facebook, Instagram, e-mail, termos) no componente Config Screen do prefab.", "OK");
        }

        // ---------------------------------------------------------------------------------------------- telas

        static void BuildBackground(RectTransform root)
        {
            var bg = Picture("Background", root, "paper_background.png", 1095, 1988);
            bg.anchorMin = bg.anchorMax = new Vector2(0.5f, 0.5f);
            bg.anchoredPosition = Vector2.zero;
            var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1095f / 1988f;
            bg.GetComponent<Image>().raycastTarget = true;
        }

        static void BuildMain(RectTransform main)
        {
            // Cabeçalho: a barra roxa ocupa a largura toda; título e fechar ficam em cima dela.
            var header = Picture("Header", main, "barra_superior.png", 1218, 293);
            header.anchorMin = new Vector2(0f, 1f); header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = new Vector2(0f, 293f * (1080f / 1218f) * K);
            header.GetComponent<Image>().type = Image.Type.Simple;
            header.GetComponent<Image>().preserveAspect = false;
            var title = Picture("Title", header, "title_PT.png", 718, 133); Top(title, 515, 120);
            var localized = title.gameObject.AddComponent<LocalizedImage>();
            localized.SetSprite(GameLanguage.Portuguese, LoadArt("title_PT.png"));
            localized.SetSprite(GameLanguage.English, LoadArt("title_EN.png"));
            CloseButton("Close", header, 970, 105, 1f);

            Picture("FrameTop", main, "Frames/frame_superior.png", 1007, 375, 537, 497);
            Picture("FrameBottom", main, "Frames/frame_inferior.png", 1007, 426, 535, 905);

            RoundToggle("Music", main, 220, 515, "Icons/music icon.png", 95, 86);
            RoundToggle("Sounds", main, 540, 515, "Icons/sound_icon.png", 104, 104);
            RoundToggle("Hints", main, 837, 515, "Icons/idea_icon.png", 84, 96);

            var notifications = Container("Notifications", main);
            Picture("Bell", notifications, "Icons/notification_icon.png", 84, 84, 140, 805);
            Label("Label", notifications, 336, 810, 300, 80, 46);
            var sw = Picture("Switch", notifications, "Buttons/Switch/switch_container.png", 398, 124, 714, 813);
            Plain(sw);
            var knob = Picture("Knob", sw, "Buttons/Switch/switch_button.png", 186, 96); Top(knob, 540, 0); knob.anchorMin = knob.anchorMax = new Vector2(0.5f, 0.5f); knob.anchoredPosition = Vector2.zero;
            knob.GetComponent<Image>().raycastTarget = false;
            var swLabel = Label("Label", sw, 540, 0, 190, 80, 56); swLabel.anchorMin = swLabel.anchorMax = new Vector2(0.5f, 0.5f); swLabel.anchoredPosition = Vector2.zero;
            var divider = New("Divider", notifications);
            var line = divider.gameObject.AddComponent<Image>(); line.color = new Color(0.66f, 0.56f, 0.46f, 1f); line.raycastTarget = false;
            divider.sizeDelta = new Vector2(860f * K, 6f * K); Top(divider, 533, 906);

            var language = Picture("Language", main, "Buttons/Idioma e Supporte/btn_idioma_and_support.png", 374, 105, 315, 975);
            Plain(language); Label("Label", language, 0, 0, 340, 90, 50, true);
            var support = Picture("Support", main, "Buttons/Idioma e Supporte/btn_idioma_and_support.png", 374, 105, 745, 975);
            Plain(support);
            var supportIcon = Picture("Icon", support, "Icons/support_icon.png", 78, 78); Mid(supportIcon, -118, 4);
            Label("Label", support, 34, 0, 250, 90, 50, true);

            LongButton("Save", main, 525, 1250, "Buttons/Longed/longed_button_center_blue.png", 738, 226, 670, 160, "Icons/save_icon.png", 72, 58, 56);
            var status = Label("SaveStatus", main, 525, 1372, 760, 50, 32);
            status.GetComponent<TMP_Text>().text = "";
            LongButton("Terms", main, 525, 1495, "Buttons/Longed/longed_button_center_green.png", 738, 226, 670, 160, null, 0, 0, 58);

            FollowButton("FollowFacebook", main, 290, 1815, "Icons/facebook_icon.png", 102, 97, -128);
            FollowButton("FollowInstagram", main, 785, 1815, "Icons/instagram.png", 104, 93, -125);

            // A moeda "+100" do botão: só aparece como arte; o texto fica por cima.
            AddCoin((RectTransform)main.Find("FollowFacebook"));
            AddCoin((RectTransform)main.Find("FollowInstagram"));

            var cloud = main.Find("Save").gameObject.AddComponent<CloudSaveButton>();
            var cloudSo = new SerializedObject(cloud);
            cloudSo.FindProperty("statusText").objectReferenceValue = status.GetComponent<TMP_Text>();
            cloudSo.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildLanguageModal(RectTransform root)
        {
            var modal = Modal("LanguageModal", root, "Modals/modal_idioma.png", 1031, 1027, 983, 975, 548, 815);
            Ribbon(modal);
            OptionButton("Portuguese", modal, 548, 662);
            OptionButton("English", modal, 548, 873);
            CloseButton("Close", modal, 965, 397, 1.25f);
        }

        static void BuildTermsModal(RectTransform root)
        {
            var modal = Modal("TermsModal", root, "Modals/modal_termos_e_privacidade.png", 1031, 1265, 985, 1208, 548, 935);
            Ribbon(modal);
            LongButton("TermsOfService", modal, 545, 693, "Buttons/Longed/longed_button_center_blue.png", 650, 199, 590, 141, null, 0, 0, 62);
            LongButton("PrivacyPolicy", modal, 545, 912, "Buttons/Longed/longed_button_center_blue.png", 650, 199, 590, 141, null, 0, 0, 58);
            LongButton("PrivacyPrefs", modal, 545, 1137, "Buttons/Longed/longed_button_center_blue.png", 650, 199, 590, 141, null, 0, 0, 52);
            var prefs = (RectTransform)modal.Find("PrivacyPrefs/Center/Label");
            prefs.sizeDelta = new Vector2(500f * K, 130f * K);
            prefs.GetComponent<TMP_Text>().textWrappingMode = TextWrappingModes.Normal;
            var delete = New("DeleteAccount", modal); delete.sizeDelta = new Vector2(480f * K, 80f * K); Top(delete, 550, 1360);
            var deleteImage = delete.gameObject.AddComponent<Image>(); deleteImage.color = new Color(1, 1, 1, 0);
            Plain(delete);
            Label("Label", delete, 0, 0, 480, 80, 50, true);
            CloseButton("Close", modal, 965, 397, 1.25f);
        }

        // ---------------------------------------------------------------------------------------------- peças

        static RectTransform Modal(string name, RectTransform root, string panelPath, float artW, float artH, float w, float h, float x, float y)
        {
            var modal = Container(name, root);
            var dim = New("Dim", modal); Stretch(dim);
            var dimImage = dim.gameObject.AddComponent<Image>(); dimImage.color = new Color(0f, 0f, 0f, 0.62f);
            Plain(dim);
            var panel = Picture("Panel", modal, panelPath, artW, artH, x, y);
            panel.sizeDelta = new Vector2(w * K, h * K);
            panel.GetComponent<Image>().raycastTarget = true; // clicar na moldura não fecha a modal
            modal.gameObject.SetActive(false);
            return modal;
        }

        static void Ribbon(RectTransform modal)
        {
            var ribbon = Picture("Ribbon", modal, "Ribbons.png", 2172, 724, 540, 372);
            ribbon.sizeDelta = new Vector2(970f * K, 323f * K);
            var title = Label("Title", ribbon, 0, 0, 760, 120, 72, true);
            title.anchoredPosition = new Vector2(0f, 60f * K);
            var arc = title.gameObject.AddComponent<TextArc>();
            arc.height = 38f * K;
        }

        static void OptionButton(string name, RectTransform parent, float x, float y)
        {
            var button = Picture(name, parent, "Buttons/Idioma Modal/button_idioma_options.png", 771, 204, x, y);
            Plain(button);
            Label("Label", button, 0, 0, 700, 150, 92, true);
        }

        /// Botão redondo verde que liga/desliga: moldura dourada, miolo verde (encolhe no clique), ícone e a barra vermelha.
        static void RoundToggle(string name, RectTransform parent, float x, float y, string iconPath, float iconW, float iconH)
        {
            var border = Picture(name, parent, "Buttons/Rounde Green/border_close.png", 122, 122, x, y);
            border.sizeDelta = new Vector2(170f * K, 170f * K);
            var center = Picture("Center", border, "Buttons/Rounde Green/center_green.png", 152, 152);
            center.anchorMin = center.anchorMax = new Vector2(0.5f, 0.5f); center.anchoredPosition = Vector2.zero;
            center.sizeDelta = new Vector2(140f * K, 140f * K);
            var icon = Picture("Icon", center, iconPath, iconW, iconH); Mid(icon, 0, 0);
            var off = Picture("Off", center, "desactivated.png", 120, 128); Mid(off, 0, 0);
            off.sizeDelta = new Vector2(128f * K, 136f * K);
            var label = Label("Label", border, 0, 0, 300, 80, 50);
            label.anchorMin = label.anchorMax = new Vector2(0.5f, 0.5f);
            label.anchoredPosition = new Vector2(0f, 135f * K);
            MakeButton(border, center);
        }

        /// Botão comprido: moldura dourada de 738x226, miolo colorido que encolhe no clique, ícone e texto.
        static void LongButton(string name, RectTransform parent, float x, float y, string centerPath,
            float borderW, float borderH, float centerW, float centerH, string iconPath, float iconW, float iconH, float fontSize)
        {
            var border = Picture(name, parent, "Buttons/Longed/longed_button_border.png", 738, 226, x, y);
            border.sizeDelta = new Vector2(borderW * K, borderH * K);
            var center = Picture("Center", border, centerPath, 670, 160);
            center.anchorMin = center.anchorMax = new Vector2(0.5f, 0.5f); center.anchoredPosition = Vector2.zero;
            center.sizeDelta = new Vector2(centerW * K, centerH * K);
            var hasIcon = iconPath != null;
            if (hasIcon) { var icon = Picture("Icon", center, iconPath, iconW, iconH); Mid(icon, -250, 4); }
            var label = Label("Label", center, hasIcon ? 40 : 0, 0, centerW - 40, centerH - 20, fontSize, true);
            MakeButton(border, center);
        }

        /// Botão de uma imagem só ("Seguir"): sem miolo, então sem o efeito de encolher; só o som.
        static void FollowButton(string name, RectTransform parent, float x, float y, string iconPath, float iconW, float iconH, float iconX)
        {
            var button = Picture(name, parent, "Buttons/Follow/btn_follow.png", 403, 142, x, y);
            Plain(button);
            var icon = Picture("Icon", button, iconPath, iconW, iconH); Mid(icon, iconX, 0);
            Label("Label", button, 12, 0, 200, 90, 50, true);
        }

        static void AddCoin(RectTransform button)
        {
            var coin = Picture("Coin", button, "Icons/coin icon.png", 101, 101); Mid(coin, 140, 5);
            var amount = Label("Amount", button, 0, 0, 110, 40, 34, true);
            amount.anchoredPosition = new Vector2(140f * K, -28f * K);
            amount.GetComponent<TMP_Text>().text = "+100";
        }

        static void CloseButton(string name, RectTransform parent, float x, float y, float scale)
        {
            var border = Picture(name, parent, "Buttons/Close/border_close.png", 122, 122, x, y);
            border.sizeDelta = new Vector2(122f * scale * K, 122f * scale * K);
            var center = Picture("Center", border, "Buttons/Close/center_close.png", 101, 102);
            center.anchorMin = center.anchorMax = new Vector2(0.5f, 0.5f); center.anchoredPosition = Vector2.zero;
            center.sizeDelta = new Vector2(101f * scale * K, 102f * scale * K);
            var icon = Picture("Icon", center, "Buttons/Close/icone_close.png", 64, 61); Mid(icon, 0, 0);
            icon.sizeDelta = new Vector2(64f * scale * K, 61f * scale * K);
            MakeButton(border, center);
        }

        // ---------------------------------------------------------------------------------------------- base

        static RectTransform New(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return rt;
        }

        static RectTransform Container(string name, Transform parent)
        {
            var rt = New(name, parent); Stretch(rt); return rt;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// Coordenada do mock (centro do objeto, y de cima para baixo) -> posição no canvas.
        static void Top(RectTransform rt, float x, float y)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2((x - 540f) * K, -y * K);
        }

        static void Mid(RectTransform rt, float dx, float dy)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(dx * K, dy * K);
        }

        static RectTransform Picture(string name, Transform parent, string path, float w, float h, float x = float.NaN, float y = float.NaN)
        {
            var rt = New(name, parent);
            rt.sizeDelta = new Vector2(w * K, h * K);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = LoadArt(path);
            image.preserveAspect = true;
            image.raycastTarget = false;
            if (!float.IsNaN(x)) Top(rt, x, y);
            return rt;
        }

        static RectTransform Label(string name, Transform parent, float x, float y, float w, float h, float size, bool relativeToCenter = false)
        {
            var rt = New(name, parent);
            rt.sizeDelta = new Vector2(w * K, h * K);
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = size * K;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.text = name;
            if (relativeToCenter) Mid(rt, x, y); else Top(rt, x, y);
            return rt;
        }

        /// Botão sem transição de cor, só para receber o clique.
        static void Plain(RectTransform rt)
        {
            var image = rt.GetComponent<Image>();
            if (image != null) image.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            if (image != null) button.targetGraphic = image;
        }

        /// Botão na moldura, com o efeito de clique encolhendo só o miolo.
        static void MakeButton(RectTransform frame, RectTransform center)
        {
            Plain(frame);
            var pop = frame.gameObject.AddComponent<ButtonPopPress>();
            var so = new SerializedObject(pop);
            so.FindProperty("target").objectReferenceValue = center;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Sprite LoadArt(string relative)
        {
            var path = Pngs + relative;
            var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sprite != null) return sprite;
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            }
            if (sprite == null) Debug.LogWarning("Royal Aves: não achei o sprite " + path);
            return sprite;
        }
    }
}
