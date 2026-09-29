// Restyles Sweet Sugar's life shop (CanvasGlobal/LiveShop, opened from the lives counter): a plain box as in Sweet
// Sugar, in the lobby's look (cream panel, gold border, blue outline) with the title at its top, a light card with the
// Correio Mágico heart, the refill text and the life timer, the coin price on a blue button and the video on a green one,
// and the round red close button on the box's corner. Sweet Sugar's objects and scripts (LifeShop, BuyLife, Video, Close, Timer) stay; they are only restyled and
// moved. Its pink banner, blue ribbon and white circle are hidden.
// Menu: Royal Aves > Aplicar visual da loja de vidas. Running it again only updates what it made.
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesLifeShopBuilder
    {
        const string CanvasGlobalPath = "Assets/SweetSugar/Prefabs/CanvasGlobal.prefab";
        const string ArtFolder = RoyalAvesAssetImporter.LevelStartFolder;
        const string BoxPath = "Assets/RoyalAves/Art/UI/Generated/panel.png";
        const string CardPath = "Assets/RoyalAves/Art/UI/Generated/card.png";
        const string HeartPath = "Assets/RoyalAves/Art/UI/Icons/vida_ativa.png";
        const string CoinPath = "Assets/RoyalAves/Art/UI/Icons/moeda-correio.png";
        const string GreenButtonPath = "Assets/RoyalAves/Art/UI/Generated/btn-green.png";
        const string BlueButtonPath = "Assets/RoyalAves/Art/UI/Generated/btn-blue.png";

        // Popup units around the window's centre (a phone shows about 1140 across). The box's border is drawn at 0.6 of
        // the panel's pixels, so it reads as thick as Sweet Sugar's.
        const float BoxWidth = 1000, BoxHeight = 1150, BoxBorderScale = 0.6f;
        const float TitleY = BoxHeight / 2 - 110;
        static readonly Vector2 CloseCenter = new Vector2(BoxWidth / 2 - 30, BoxHeight / 2 - 30);
        const float CloseSize = 150;
        const float CardWidth = 780, CardHeight = 560, CardCenterY = 30;
        const float HeartY = 180, HeartSize = 230;
        const float RefillY = -20, TimerY = -150;
        const float ButtonsY = -400;
        static readonly Vector2 ButtonSize = new Vector2(380, 170);
        const float ButtonX = 210;

        static readonly Color Navy = new Color32(0x17, 0x2E, 0x67, 0xFF);
        static readonly Color Cream = new Color32(0xFF, 0xF8, 0xD5, 0xFF);

        [MenuItem("Royal Aves/Aplicar visual da loja de vidas")]
        static void ApplyFromMenu()
        {
            if (!RoyalAvesTools.CanRunTool()) return;
            RoyalAvesReskin.EnsureImportSettings();
            var art = new Art
            {
                Box = Load(BoxPath),
                Card = Load(CardPath),
                CloseCircle = Load(ArtFolder + "botao-fechar-circulo.png"),
                CloseX = Load(ArtFolder + "botao-fechar-x.png"),
                Heart = Load(HeartPath),
                Coin = Load(CoinPath),
                Green = Load(GreenButtonPath),
                Blue = Load(BlueButtonPath),
            };
            if (art.Missing)
            {
                EditorUtility.DisplayDialog("Loja de vidas", "Faltam imagens (caixa, cartão, X, coração, moeda ou botões).", "OK");
                return;
            }

            var log = new List<string>();
            var root = PrefabUtility.LoadPrefabContents(CanvasGlobalPath);
            try
            {
                var window = root.transform.Find("LiveShop/Image") as RectTransform;
                if (window == null)
                {
                    EditorUtility.DisplayDialog("Loja de vidas", "Não achei CanvasGlobal/LiveShop/Image.", "OK");
                    return;
                }
                Build(window, art, log);
                PrefabUtility.SaveAsPrefabAsset(root, CanvasGlobalPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            EditorUtility.DisplayDialog("Loja de vidas", "Aplicado:\n- " + string.Join("\n- ", log) +
                "\n\nPosições e tamanhos podem ser ajustados no prefab CanvasGlobal > LiveShop.", "OK");
        }

        class Art
        {
            public Sprite Box, Card, CloseCircle, CloseX, Heart, Coin, Green, Blue;

            public bool Missing => new[] { Box, Card, CloseCircle, CloseX, Heart, Coin, Green, Blue }.Any(s => s == null);
        }

        static void Build(RectTransform window, Art art, List<string> log)
        {
            // Box: the pink banner is switched off and the lobby's panel is drawn behind everything.
            var banner = window.GetComponent<Image>();
            if (banner != null) banner.enabled = false;
            var frame = Child(window, "Moldura");
            Place(frame, Vector2.zero, new Vector2(BoxWidth, BoxHeight));
            var frameImage = ImageOn(frame);
            frameImage.sprite = art.Box;
            frameImage.type = Image.Type.Sliced;
            frameImage.fillCenter = true;
            frameImage.preserveAspect = false;
            frameImage.pixelsPerUnitMultiplier = BoxBorderScale;
            frameImage.raycastTarget = true; // taps on the window must not reach the dim behind it
            log.Add("caixa creme com borda dourada");

            // Title at the top of the box, in the postal lettering of "Nível N".
            var title = window.Find("TextMeshPro Text (1)")?.GetComponent<TextMeshProUGUI>();
            if (title != null)
            {
                title.rectTransform.localScale = Vector3.one;
                Place(title.rectTransform, new Vector2(0, TitleY), new Vector2(720, 150));
                StopLocalization(title);
                RoyalAvesFonts.ApplyPostal(title);
                title.text = "Sem vidas?";
                title.alignment = TextAlignmentOptions.Center;
                title.textWrappingMode = TextWrappingModes.NoWrap;
                AutoSize(title, 60, 110);
                log.Add("título \"Sem vidas?\"");
            }

            // Card: Sweet Sugar's scalloped white card, redrawn; its blue ribbon goes away.
            var beige = (RectTransform)window.Find("Image");
            beige.localScale = Vector3.one;
            Place(beige, new Vector2(0, CardCenterY), new Vector2(CardWidth, CardHeight));
            var cardImage = beige.GetComponent<Image>();
            cardImage.sprite = art.Card;
            cardImage.type = Image.Type.Sliced;
            cardImage.fillCenter = true;
            cardImage.preserveAspect = false;
            cardImage.pixelsPerUnitMultiplier = 0.5f;
            Hide(window.Find("Image (1)"));

            // The heart: the white circle stays as a holder with its picture switched off.
            var heartHolder = (RectTransform)window.Find("Image (2)");
            heartHolder.localScale = Vector3.one;
            Place(heartHolder, new Vector2(0, HeartY), new Vector2(HeartSize, HeartSize));
            var holderImage = heartHolder.GetComponent<Image>();
            if (holderImage != null) holderImage.enabled = false;
            var heart = (RectTransform)heartHolder.Find("Image");
            heart.localScale = Vector3.one;
            heart.anchorMin = Vector2.zero;
            heart.anchorMax = Vector2.one;
            heart.offsetMin = heart.offsetMax = Vector2.zero;
            var heartImage = heart.GetComponent<Image>();
            heartImage.sprite = art.Heart;
            heartImage.preserveAspect = true;
            log.Add("cartão claro com o coração do Correio Mágico");

            var refill = window.Find("TextMeshPro Text")?.GetComponent<TextMeshProUGUI>();
            if (refill != null)
            {
                refill.rectTransform.localScale = Vector3.one;
                Place(refill.rectTransform, new Vector2(0, RefillY), new Vector2(640, 90));
                Style(refill, "Recarregar todas as vidas!", Navy, 36, 62);
            }
            // The timer's sentence is written every frame by LifeTimerDouble; only its look is set here.
            var timer = window.Find("Timer")?.GetComponent<TextMeshProUGUI>();
            if (timer != null)
            {
                timer.rectTransform.localScale = Vector3.one;
                Place(timer.rectTransform, new Vector2(0, TimerY), new Vector2(640, 70));
                Style(timer, "+1 vida em 12:50", Navy, 28, 46);
            }
            log.Add("textos na fonte do projeto, em português");

            // Buttons: coins on blue, video on green, side by side under the panel.
            var buttons = (RectTransform)window.Find("Buttons");
            foreach (var layout in buttons.GetComponents<LayoutGroup>()) layout.enabled = false;
            buttons.localScale = Vector3.one;
            Place(buttons, new Vector2(0, ButtonsY), new Vector2(BoxWidth, ButtonSize.y));

            var buy = (RectTransform)buttons.Find("BuyLife");
            SetButton(buy, art.Blue, new Vector2(-ButtonX, 0));
            var price = buy.Find("Price")?.GetComponent<TextMeshProUGUI>();
            if (price != null)
            {
                price.rectTransform.localScale = Vector3.one;
                price.rectTransform.anchorMin = new Vector2(0.42f, 0);
                price.rectTransform.anchorMax = new Vector2(1, 1);
                price.rectTransform.offsetMin = new Vector2(0, 22);
                price.rectTransform.offsetMax = new Vector2(-24, -14);
                RoyalAvesFonts.ApplyMail(price, true);
                price.alignment = TextAlignmentOptions.Center;
                price.textWrappingMode = TextWrappingModes.NoWrap;
                AutoSize(price, 40, 90);
            }
            if (buy.Find("Image") is RectTransform coin)
            {
                coin.localScale = Vector3.one;
                coin.anchorMin = coin.anchorMax = new Vector2(0, 0.5f);
                coin.pivot = new Vector2(0.5f, 0.5f);
                coin.anchoredPosition = new Vector2(95, 4);
                coin.sizeDelta = new Vector2(115, 115);
                var coinImage = coin.GetComponent<Image>();
                coinImage.sprite = art.Coin;
                coinImage.preserveAspect = true;
            }

            var video = (RectTransform)buttons.Find("Video");
            SetButton(video, art.Green, new Vector2(ButtonX, 0));
            if (video.Find("watch_video_icon") is RectTransform icon)
            {
                icon.localScale = Vector3.one;
                icon.anchorMin = icon.anchorMax = new Vector2(0, 0.5f);
                icon.pivot = new Vector2(0.5f, 0.5f);
                icon.anchoredPosition = new Vector2(90, 4);
                icon.sizeDelta = new Vector2(100, 104);
            }
            var videoText = video.GetComponentInChildren<TextMeshProUGUI>(true);
            if (videoText != null)
            {
                var rect = videoText.rectTransform;
                rect.localScale = Vector3.one;
                rect.anchorMin = new Vector2(0.38f, 0);
                rect.anchorMax = new Vector2(1, 1);
                rect.offsetMin = new Vector2(0, 22);
                rect.offsetMax = new Vector2(-20, -14);
                Style(videoText, "Ver vídeo", Cream, 30, 58);
            }
            log.Add("preço em moedas no botão azul e vídeo no botão verde");

            // Close: red circle with the white X, on the box's top right corner.
            var close = (RectTransform)window.Find("Close");
            close.localScale = Vector3.one;
            Place(close, CloseCenter, new Vector2(CloseSize, CloseSize));
            var closeImage = close.GetComponent<Image>();
            closeImage.sprite = art.CloseCircle;
            closeImage.preserveAspect = true;
            var x = Child(close, "X");
            x.anchorMin = Vector2.zero;
            x.anchorMax = Vector2.one;
            x.offsetMin = Vector2.one * 36;
            x.offsetMax = Vector2.one * -36;
            var xImage = ImageOn(x);
            xImage.sprite = art.CloseX;
            xImage.preserveAspect = true;
            log.Add("botão de fechar (X) no canto superior direito");

            // Drawing order: box, card, then the rest, the close button on top.
            frame.SetSiblingIndex(0);
            beige.SetSiblingIndex(1);
            close.SetAsLastSibling();
        }

        static void SetButton(RectTransform button, Sprite sprite, Vector2 position)
        {
            button.localScale = Vector3.one;
            button.anchorMin = button.anchorMax = new Vector2(0.5f, 0.5f);
            button.pivot = new Vector2(0.5f, 0.5f);
            button.anchoredPosition = position;
            button.sizeDelta = ButtonSize;
            var image = button.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1;
            image.preserveAspect = false;
        }


        static void Style(TextMeshProUGUI text, string value, Color color, float min, float max)
        {
            StopLocalization(text);
            text.font = RoyalAvesFonts.Display;
            text.fontSharedMaterial = text.font.material;
            text.enableVertexGradient = false;
            text.text = value;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            AutoSize(text, min, max);
        }

        // Sweet Sugar's localization would put the English text back.
        static void StopLocalization(TextMeshProUGUI text)
        {
            foreach (var localize in text.GetComponents<MonoBehaviour>().Where(c => c != null && c.GetType().Name == "LocalizeText"))
                localize.enabled = false;
        }

        static void AutoSize(TextMeshProUGUI text, float min, float max)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
            text.fontSize = max;
            text.raycastTarget = false;
        }

        static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static RectTransform Child(Transform parent, string name)
        {
            if (parent.Find(name) is RectTransform existing) return existing;
            var go = new GameObject(name, typeof(RectTransform)) { layer = parent.gameObject.layer };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        static Image ImageOn(RectTransform rect)
        {
            var image = rect.GetComponent<Image>();
            if (image == null) image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        static void Hide(Transform target)
        {
            if (target != null) target.gameObject.SetActive(false);
        }

        static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
