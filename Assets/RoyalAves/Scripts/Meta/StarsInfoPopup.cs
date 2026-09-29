// "Ganhe Estrelas": the window the lobby opens when the stars counter is tapped, after the user's Royal Match-style
// reference. The blue frame with the wax seal, a 3x3 board of the game's pieces, an arrow to a big star, "Vença níveis
// para ganhar estrelas" and a green "Continuar"; the red X or "Continuar" closes it.
// Built once at runtime over the lobby, from the pictures in Resources/RoyalAvesFeatures (Janela das estrelas).
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public class StarsInfoPopup : MonoBehaviour
    {
        // Lobby canvas units (1080 x 1920). The frame's band with the seal takes the top 400 px of its picture, so the
        // title sits right under it, at the top of the blue area.
        const float FrameWidth = 880, FrameHeight = 1260;
        const float TitleY = 220, ArrowY = 90, PicturesY = -40, PictureSize = 280, PictureX = 200;
        const float TextY = -275, ButtonY = -440;
        static readonly Vector2 TextSize = new Vector2(740, 110);
        static readonly Vector2 ButtonSize = new Vector2(520, 170);
        static readonly Vector2 CloseCenter = new Vector2(FrameWidth / 2 - 40, FrameHeight / 2 - 60);
        const float CloseSize = 150;

        static readonly Color Dim = new Color(0, 0, 0, 0.6f);
        static readonly Color Cream = new Color32(0xFF, 0xF4, 0xC9, 0xFF);
        static readonly Color Strip = new Color32(0x0B, 0x3F, 0x8F, 0xE6);
        static readonly Color PostalTop = new Color32(0xFB, 0xFC, 0xE8, 0xFF);
        static readonly Color PostalBottom = new Color32(0xEE, 0x9A, 0x10, 0xFF);

        Action onClick;

        public static StarsInfoPopup Create(RectTransform parent, LobbyFeaturesConfig art, Action onClick)
        {
            var root = Node("JanelaEstrelas", parent);
            Stretch(root);
            var popup = root.gameObject.AddComponent<StarsInfoPopup>();
            popup.onClick = onClick;

            var dim = root.gameObject.AddComponent<Image>();
            dim.color = Dim;
            dim.raycastTarget = true; // the lobby behind does not take taps

            var frame = Node("Moldura", root);
            Place(frame, Vector2.zero, new Vector2(FrameWidth, FrameHeight));
            var frameImage = Picture(frame, art.starsFrame, false);
            if (art.starsFrame != null)
            {
                frameImage.type = Image.Type.Sliced;
                frameImage.pixelsPerUnitMultiplier = art.starsFrame.rect.width / FrameWidth * (100f / art.starsFrame.pixelsPerUnit);
            }
            frameImage.raycastTarget = true;

            var title = Text(frame, "Ganhe Estrelas", 96, Color.white, art.postalFont != null ? art.postalFont : art.displayFont);
            if (art.postalFont != null)
            {
                if (art.postalMaterial != null) title.fontSharedMaterial = art.postalMaterial;
                title.enableVertexGradient = true;
                title.colorGradient = new VertexGradient(PostalTop, PostalTop, PostalBottom, PostalBottom);
            }
            Place(title.rectTransform, new Vector2(0, TitleY), new Vector2(FrameWidth - 120, 130));

            // A little board of the game's pieces on a light card.
            var board = Node("Tabuleiro", frame);
            Place(board, new Vector2(-PictureX, PicturesY), new Vector2(PictureSize, PictureSize));
            var boardImage = Picture(board, art.card, false);
            if (art.card != null && art.card.border != Vector4.zero) boardImage.type = Image.Type.Sliced;
            var grid = board.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(18, 18, 18, 18);
            grid.cellSize = Vector2.one * ((PictureSize - 36 - 12) / 3);
            grid.spacing = Vector2.one * 6;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            var pieces = art.starsBoardPieces;
            for (var i = 0; i < 9 && pieces.Count > 0; i++)
                Picture(Node("Peca", board), pieces[(i * 2) % pieces.Count], true); // mixed, not in rainbow order

            var arrow = Node("Seta", frame);
            Place(arrow, new Vector2(0, ArrowY), new Vector2(230, 142));
            Picture(arrow, art.starsArrow, true);

            var star = Node("Estrela", frame);
            Place(star, new Vector2(PictureX, PicturesY), new Vector2(PictureSize, PictureSize));
            Picture(star, art.star, true);

            var strip = Node("Faixa", frame);
            Place(strip, new Vector2(0, TextY), TextSize);
            var stripImage = Picture(strip, art.darkBox, false);
            stripImage.color = Strip;
            if (art.darkBox != null && art.darkBox.border != Vector4.zero) stripImage.type = Image.Type.Sliced;
            var copy = Text(strip, "Vença níveis para ganhar estrelas", 52, Cream, art.displayFont);
            Stretch(copy.rectTransform);
            copy.rectTransform.offsetMin = new Vector2(24, 8);
            copy.rectTransform.offsetMax = new Vector2(-24, -8);

            var go = Node("Continuar", frame);
            Place(go, new Vector2(0, ButtonY), ButtonSize);
            var goImage = Picture(go, art.buttonGreen, false);
            if (art.buttonGreen != null && art.buttonGreen.border != Vector4.zero) goImage.type = Image.Type.Sliced;
            goImage.raycastTarget = true;
            var goButton = go.gameObject.AddComponent<Button>();
            goButton.targetGraphic = goImage;
            goButton.onClick.AddListener(popup.Close);
            var goText = Text(go, "Continuar", 80, Cream, art.displayFont);
            Stretch(goText.rectTransform);
            goText.rectTransform.offsetMin = new Vector2(24, 22);
            goText.rectTransform.offsetMax = new Vector2(-24, -12);

            var close = Node("Fechar", frame);
            Place(close, CloseCenter, Vector2.one * CloseSize);
            var closeImage = Picture(close, art.closeCircle, true);
            closeImage.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(popup.Close);
            var x = Node("X", close);
            Stretch(x);
            x.offsetMin = Vector2.one * 36;
            x.offsetMax = Vector2.one * -36;
            Picture(x, art.closeX, true);

            root.gameObject.SetActive(false);
            return popup;
        }

        public void Open()
        {
            onClick?.Invoke();
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        void Close()
        {
            onClick?.Invoke();
            gameObject.SetActive(false);
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

        static Image Picture(RectTransform rect, Sprite sprite, bool keepAspect)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = keepAspect;
            image.raycastTarget = false;
            return image;
        }

        static TextMeshProUGUI Text(Transform parent, string value, float size, Color color, TMP_FontAsset font)
        {
            var text = Node("Texto", parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = value;
            text.color = color;
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
