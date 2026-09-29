// Builds the lobby's settings screen inside the lobby prefab, so it can be arranged by hand in the Inspector like the
// rest of the lobby. Only Content/TelaConfiguracoes is touched: running it again throws that one object away and makes
// it afresh, leaving everything else in the prefab as it was.
// Menu: Royal Aves > Criar tela de configurações.
using RoyalAves.Meta;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesSettingsScreenBuilder
    {
        const string LobbyPath = "Assets/RoyalAves/Prefabs/RoyalAvesLobby.prefab";
        const string ScreenName = "TelaConfiguracoes";

        // Lobby canvas units (1080 wide). Starting point only: from here it is arranged in the Inspector.
        const float HeaderWidth = 940, HeaderTop = -150;
        const float PlateWidthShare = 0.58f;   // the gold plate inside the header picture
        const float RowWidth = 900, RowHeight = 168, RowGap = 26, RowsTop = -420;
        const float LabelLeft = 56, ControlSize = 124, ControlRight = 44;
        static readonly Vector2 PillSize = new Vector2(268, 104);
        const float CloseSize = 132;

        static readonly Color Cream = new Color32(0xFF, 0xF4, 0xC9, 0xFF);
        static readonly Color PostalTop = new Color32(0xFB, 0xFC, 0xE8, 0xFF);
        static readonly Color PostalBottom = new Color32(0xEE, 0x9A, 0x10, 0xFF);

        [MenuItem("Royal Aves/Criar tela de configurações", false, 30)]
        static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Tela de configurações", "Pare o modo Play antes de usar este comando.", "OK");
                return;
            }

            var art = LobbyFeaturesConfig.Instance;
            if (art == null)
            {
                EditorUtility.DisplayDialog("Tela de configurações",
                    "Não achei Resources/" + LobbyFeaturesConfig.ResourcePath + ".", "OK");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(LobbyPath);
            if (root == null)
            {
                EditorUtility.DisplayDialog("Tela de configurações", "Não achei " + LobbyPath + ".", "OK");
                return;
            }

            try
            {
                var content = root.transform.Find("Content") as RectTransform;
                if (content == null)
                {
                    EditorUtility.DisplayDialog("Tela de configurações", "Não achei o Content dentro do prefab do lobby.", "OK");
                    return;
                }

                var old = content.Find(ScreenName);
                if (old != null) Object.DestroyImmediate(old.gameObject);

                var screenRect = Node(ScreenName, content);
                Stretch(screenRect);
                var screen = screenRect.gameObject.AddComponent<SettingsScreen>();

                var background = screenRect.gameObject.AddComponent<Image>();
                background.sprite = art.settingsBackground;
                background.raycastTarget = true; // the lobby behind must not take taps

                BuildHeader(screenRect, art);
                var close = BuildClose(screenRect, art);

                var y = RowsTop;
                var music = IconRow(screenRect, ref y, "Musica", "Música", art, art.gameMenuMusicOn);
                var sound = IconRow(screenRect, ref y, "Som", "Som", art, art.gameMenuSoundOn);
                var vibration = IconRow(screenRect, ref y, "Vibracao", "Vibração", art, art.gameMenuVibrationOn);
                var hint = PillRow(screenRect, ref y, "Dica", "Dica", art);
                var notifications = PillRow(screenRect, ref y, "Notificacoes", "Notificações", art);

                var serialized = new SerializedObject(screen);
                Set(serialized, "closeButton", close);
                Set(serialized, "musicButton", music.button);
                Set(serialized, "musicIcon", music.picture);
                Set(serialized, "soundButton", sound.button);
                Set(serialized, "soundIcon", sound.picture);
                Set(serialized, "vibrationButton", vibration.button);
                Set(serialized, "vibrationIcon", vibration.picture);
                Set(serialized, "hintButton", hint.button);
                Set(serialized, "hintPill", hint.picture);
                Set(serialized, "hintText", hint.state);
                Set(serialized, "notificationsButton", notifications.button);
                Set(serialized, "notificationsPill", notifications.picture);
                Set(serialized, "notificationsText", notifications.state);
                Set(serialized, "musicOn", art.gameMenuMusicOn);
                Set(serialized, "musicOff", art.gameMenuMusicOff);
                Set(serialized, "soundOn", art.gameMenuSoundOn);
                Set(serialized, "soundOff", art.gameMenuSoundOff);
                Set(serialized, "vibrationOn", art.gameMenuVibrationOn);
                Set(serialized, "vibrationOff", art.gameMenuVibrationOff);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                // It is opened by the gear, so it starts out of the way.
                screenRect.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, LobbyPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            EditorUtility.DisplayDialog("Tela de configurações",
                "Tela criada em Content/" + ScreenName + ", dentro do prefab do lobby.\n\n" +
                "Agora ela aparece na hierarquia fora do Play e pode ser arrumada à mão. " +
                "Rodar este comando de novo refaz a tela do zero e desfaz esses ajustes.", "OK");
        }

        static void BuildHeader(RectTransform parent, LobbyFeaturesConfig art)
        {
            var header = Node("Cabecalho", parent);
            var sprite = art.settingsHeader;
            var height = sprite != null ? HeaderWidth * sprite.rect.height / sprite.rect.width : 150;
            TopCentre(header, HeaderTop, new Vector2(HeaderWidth, height));
            Picture(header, sprite, true);

            var title = Text(header, "Titulo", "Configurações", 78, Color.white,
                art.postalFont != null ? art.postalFont : art.displayFont);
            if (art.postalFont != null)
            {
                if (art.postalMaterial != null) title.fontSharedMaterial = art.postalMaterial;
                title.enableVertexGradient = true;
                title.colorGradient = new VertexGradient(PostalTop, PostalTop, PostalBottom, PostalBottom);
            }
            Place(title.rectTransform, Vector2.zero, new Vector2(HeaderWidth * PlateWidthShare, height * 0.52f));
        }

        static Button BuildClose(RectTransform parent, LobbyFeaturesConfig art)
        {
            var close = Node("Fechar", parent);
            TopCentre(close, HeaderTop, new Vector2(CloseSize, CloseSize));
            close.anchoredPosition = new Vector2(HeaderWidth / 2 + 10, close.anchoredPosition.y);
            var picture = Picture(close, art.closeCircle, true);
            picture.raycastTarget = true;
            var button = close.gameObject.AddComponent<Button>();
            button.targetGraphic = picture;
            var x = Node("X", close);
            Stretch(x);
            x.offsetMin = Vector2.one * 32;
            x.offsetMax = Vector2.one * -32;
            Picture(x, art.closeX, true);
            return button;
        }

        struct Control
        {
            public Button button;
            public Image picture;
            public TextMeshProUGUI state;
        }

        /// Music, sound and vibration: the state is a picture that changes.
        static Control IconRow(RectTransform parent, ref float y, string name, string label, LobbyFeaturesConfig art, Sprite icon)
        {
            var row = Row(parent, ref y, name, label, art);
            var holder = Node("Botao", row);
            RightCentre(holder, ControlRight, new Vector2(ControlSize, ControlSize));
            var picture = Picture(holder, icon, true);
            picture.raycastTarget = true;
            var button = holder.gameObject.AddComponent<Button>();
            button.targetGraphic = picture;
            return new Control { button = button, picture = picture };
        }

        /// Hint and notifications: the state is written out on a pill.
        static Control PillRow(RectTransform parent, ref float y, string name, string label, LobbyFeaturesConfig art)
        {
            var row = Row(parent, ref y, name, label, art);
            var holder = Node("Botao", row);
            RightCentre(holder, ControlRight, PillSize);
            var pill = holder.gameObject.AddComponent<Image>();
            pill.sprite = art.pill;
            if (pill.sprite != null) pill.type = Image.Type.Sliced;
            pill.raycastTarget = true;
            var button = holder.gameObject.AddComponent<Button>();
            button.targetGraphic = pill;
            var state = Text(holder, "Estado", "Ligado", 52, Cream, art.displayFont);
            Stretch(state.rectTransform);
            return new Control { button = button, picture = pill, state = state };
        }

        static RectTransform Row(RectTransform parent, ref float y, string name, string label, LobbyFeaturesConfig art)
        {
            var row = Node(name, parent);
            TopCentre(row, y, new Vector2(RowWidth, RowHeight));
            y -= RowHeight + RowGap;

            var box = row.gameObject.AddComponent<Image>();
            box.sprite = art.darkBox;
            if (box.sprite != null) box.type = Image.Type.Sliced;
            box.color = box.sprite != null ? Color.white : new Color(0, 0, 0, 0.32f);
            box.raycastTarget = false;

            var text = Text(row, "Nome", label, 62, Cream, art.displayFont);
            text.alignment = TextAlignmentOptions.Left;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 0.5f);
            rect.pivot = new Vector2(0, 0.5f);
            rect.anchoredPosition = new Vector2(LabelLeft, 0);
            rect.sizeDelta = new Vector2(RowWidth - LabelLeft - ControlSize - ControlRight - 40, RowHeight * 0.6f);
            return row;
        }

        static void Set(SerializedObject serialized, string field, Object value)
        {
            var property = serialized.FindProperty(field);
            if (property != null) property.objectReferenceValue = value;
            else Debug.LogWarning($"Tela de configurações: o campo {field} não existe mais no SettingsScreen.");
        }

        static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = parent.gameObject.layer };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// Hung from the top, so the rows follow one another whatever the screen's height.
        static void TopCentre(RectTransform rect, float y, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = size;
        }

        static void RightCentre(RectTransform rect, float margin, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1, 0.5f);
            rect.pivot = new Vector2(1, 0.5f);
            rect.anchoredPosition = new Vector2(-margin, 0);
            rect.sizeDelta = size;
        }

        static Image Picture(RectTransform rect, Sprite sprite, bool keepAspect)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = keepAspect;
            image.raycastTarget = false;
            return image;
        }

        static TextMeshProUGUI Text(Transform parent, string name, string value, float size, Color colour, TMP_FontAsset font)
        {
            var text = Node(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = value;
            text.color = colour;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMin = size * 0.5f;
            text.fontSizeMax = size;
            text.fontSize = size;
            text.raycastTarget = false;
            return text;
        }
    }
}
