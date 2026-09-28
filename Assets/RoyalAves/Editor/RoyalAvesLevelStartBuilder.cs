// Restyles Sweet Sugar's level start window (CanvasGlobal/MenuPlay) after the user's mock-up:
// - the red mailbox frame with "Nível N" in its top band and a close button (red circle + white X) at its top right;
// - the beige panel with "Objetivo", the level goals (picture + amount), "Selecionar Reforços:" and the three boosters
//   in yellow buttons;
// - the green "Jogar".
// Sweet Sugar's objects and scripts stay (boosters at Image/Boosters/Boost1-3, Play, Close); they are only restyled and
// moved. Its goal banner, level badge and friends leaderboard are hidden.
// Menu: Royal Aves > Aplicar visual do início de nível. Running it again only updates what it made.
using System.Collections.Generic;
using System.Linq;
using RoyalAves.Meta;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesLevelStartBuilder
    {
        const string CanvasGlobalPath = "Assets/SweetSugar/Prefabs/CanvasGlobal.prefab";
        const string ArtFolder = RoyalAvesAssetImporter.LevelStartFolder;
        const string GreenButtonPath = "Assets/RoyalAves/Art/UI/Generated/btn-green.png";

        // Layout in the popup's units, around the window's centre. CanvasGlobal is 1536 x 2048, but a phone shows about
        // 1140 units across, so the frame is 1060 wide.
        const float FrameWidth = 1060, FrameHeight = 1340;
        const float TitleY = 526;
        static readonly Vector2 CloseCenter = new Vector2(455, 590);
        const float CloseSize = 150;
        const float BeigeWidth = 720, BeigeHeight = 578, BeigeCenterY = 14;
        const float PlayY = -405;
        static readonly Vector2 PlaySize = new Vector2(560, 180);

        // Inside the beige panel (units at its 720 width): goals slot from the top, booster slots from the bottom, and the
        // stretched strip between them for "Selecionar Reforços:".
        const float GoalLabelFromTop = 40, GoalsFromTop = 179, BoostLabelFromTop = 326;
        static readonly float[] BoostSlotX = { -223, -1, 222 };
        const float BoostSlotFromBottom = 123, BoostScale = 0.72f;

        static readonly Color Navy = new Color32(0x17, 0x2E, 0x67, 0xFF);
        static readonly Color Cream = new Color32(0xFF, 0xF8, 0xD5, 0xFF);

        [MenuItem("Royal Aves/Aplicar visual do início de nível")]
        static void ApplyFromMenu()
        {
            if (!RoyalAvesTools.CanRunTool()) return;
            RoyalAvesReskin.EnsureImportSettings();
            ReimportFrames();
            var frame = Load("moldura-metas-caixa-correio");
            var beige = Load("moldura-bege");
            var boostButton = Load("botao-power-ups");
            var closeCircle = Load("botao-fechar-circulo");
            var closeX = Load("botao-fechar-x");
            var green = AssetDatabase.LoadAssetAtPath<Sprite>(GreenButtonPath);
            var selected = new BoostSelection(Load("botao-verde-selecionado"), Load("selo-verde"), Load("check"));
            if (new[] { frame, beige, boostButton, closeCircle, closeX, green, selected.Button, selected.Badge, selected.Check }.Any(s => s == null))
            {
                EditorUtility.DisplayDialog("Início de nível", "Faltam imagens em " + ArtFolder + " (molduras, botão dos reforços ou o X).", "OK");
                return;
            }

            var log = new List<string>();
            var root = PrefabUtility.LoadPrefabContents(CanvasGlobalPath);
            try
            {
                var menu = root.transform.Find("MenuPlay");
                var window = menu != null ? menu.Find("Image") as RectTransform : null;
                if (window == null)
                {
                    EditorUtility.DisplayDialog("Início de nível", "Não achei CanvasGlobal/MenuPlay/Image.", "OK");
                    return;
                }
                Build(menu, window, frame, beige, boostButton, closeCircle, closeX, green, selected, log);
                PrefabUtility.SaveAsPrefabAsset(root, CanvasGlobalPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            EditorUtility.DisplayDialog("Início de nível", "Aplicado:\n- " + string.Join("\n- ", log) +
                "\n\nPosições e tamanhos podem ser ajustados no prefab CanvasGlobal > MenuPlay.", "OK");
        }

        // Pictures of a selected booster: the green button behind it and the green badge with a check on its corner.
        readonly struct BoostSelection
        {
            public readonly Sprite Button, Badge, Check;

            public BoostSelection(Sprite button, Sprite badge, Sprite check)
            {
                Button = button;
                Badge = badge;
                Check = check;
            }
        }

        static readonly Vector2 BadgeSpot = new Vector2(92, -92);

        static void Build(Transform menu, RectTransform window, Sprite frame, Sprite beige, Sprite boostButton,
            Sprite closeCircle, Sprite closeX, Sprite green, BoostSelection selected, List<string> log)
        {
            // Frame: Sweet Sugar's banner picture becomes the red mailbox, stretched in height only.
            var frameRect = (RectTransform)window.Find("Image");
            Sliced(frameRect.GetComponent<Image>(), frame, FrameWidth);
            Place(frameRect, Vector2.zero, new Vector2(FrameWidth, FrameHeight));
            log.Add("moldura vermelha");

            // "Nível N" in the band; Sweet Sugar's round level badge goes away.
            Hide(window.Find("Image (1)"));
            var title = TextOn(Child(window, "Titulo"));
            Place(title.rectTransform, new Vector2(0, TitleY), new Vector2(720, 150));
            RoyalAvesFonts.ApplyPostal(title);
            title.text = "Nível 1";
            title.alignment = TextAlignmentOptions.Center;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            AutoSize(title, 60, 110);
            if (title.GetComponent<LevelStartTitle>() == null) title.gameObject.AddComponent<LevelStartTitle>();
            log.Add("título \"Nível N\"");

            // Beige panel with its goal slot and three booster slots.
            var beigeRect = Child(window, "Bege");
            Place(beigeRect, new Vector2(0, BeigeCenterY), new Vector2(BeigeWidth, BeigeHeight));
            Sliced(ImageOn(beigeRect), beige, BeigeWidth);

            var goalLabel = TextOn(Child(beigeRect, "Objetivo"));
            FromTop(goalLabel.rectTransform, GoalLabelFromTop, new Vector2(600, 70));
            Style(goalLabel, "Objetivo", Navy, 30, 54);

            var goals = Child(beigeRect, "Metas");
            FromTop(goals, GoalsFromTop, new Vector2(620, 190));
            var row = goals.GetComponent<HorizontalLayoutGroup>();
            if (row == null) row = goals.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter;
            row.spacing = 28;
            row.childControlWidth = row.childControlHeight = false;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var template = Child(goals, "Modelo");
            template.sizeDelta = new Vector2(150, 150);
            var templateImage = ImageOn(template);
            templateImage.preserveAspect = true;
            var amount = TextOn(Child(template, "Quantidade"));
            var amountRect = amount.rectTransform;
            amountRect.anchorMin = new Vector2(0.35f, -0.12f);
            amountRect.anchorMax = new Vector2(1.25f, 0.42f);
            amountRect.offsetMin = amountRect.offsetMax = Vector2.zero;
            RoyalAvesFonts.ApplyPostal(amount);
            amount.text = "50";
            amount.alignment = TextAlignmentOptions.BottomRight;
            AutoSize(amount, 30, 64);
            template.gameObject.SetActive(false);
            var view = goals.GetComponent<LevelGoalsView>();
            if (view == null) view = goals.gameObject.AddComponent<LevelGoalsView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("template").objectReferenceValue = templateImage;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            log.Add("painel bege com \"Objetivo\" e as metas (figura + quantidade)");

            // Sweet Sugar's goal banner and the friends leaderboard under the window are not in the mock-up.
            Hide(window.Find("Target"));
            Hide(menu.Find("Leadboard"));
            Hide(menu.Find("Leadboard HD"));

            // "Selecionar Reforços:" in the strip between the slots (Sweet Sugar's "Select Boosters:" text, restyled).
            var boostLabel = window.Find("TextMeshPro Text (1)")?.GetComponent<TextMeshProUGUI>();
            if (boostLabel != null)
            {
                boostLabel.rectTransform.localScale = Vector3.one;
                Place(boostLabel.rectTransform, new Vector2(0, BeigeCenterY + BeigeHeight / 2 - BoostLabelFromTop), new Vector2(660, 80));
                Style(boostLabel, "Selecionar Reforços:", Navy, 28, 50);
            }

            // Boosters: the container covers the beige panel and each booster sits on its slot.
            var boosters = (RectTransform)window.Find("Boosters");
            foreach (var layout in boosters.GetComponents<LayoutGroup>()) layout.enabled = false;
            boosters.localScale = Vector3.one;
            Place(boosters, new Vector2(0, BeigeCenterY), new Vector2(BeigeWidth, BeigeHeight));
            for (var i = 0; i < 3; i++)
            {
                var boost = boosters.Find("Boost" + (i + 1)) as RectTransform;
                if (boost == null) continue;
                boost.anchorMin = boost.anchorMax = new Vector2(0.5f, 0);
                boost.pivot = new Vector2(0.5f, 0.5f);
                boost.anchoredPosition = new Vector2(BoostSlotX[i], BoostSlotFromBottom);
                boost.localScale = Vector3.one * BoostScale;
                Image background = null;
                foreach (Transform part in boost)
                    if (part.name.StartsWith("Image (") && part.GetComponent<Image>() is Image image)
                    {
                        background = image;
                        background.sprite = boostButton;
                        background.preserveAspect = true;
                    }
                if (background != null)
                {
                    background.transform.SetAsFirstSibling();
                    // Yellow normally, green while selected.
                    var look = boost.GetComponent<BoostSelectedLook>();
                    if (look == null) look = boost.gameObject.AddComponent<BoostSelectedLook>();
                    var lookData = new SerializedObject(look);
                    lookData.FindProperty("background").objectReferenceValue = background;
                    lookData.FindProperty("normal").objectReferenceValue = boostButton;
                    lookData.FindProperty("selected").objectReferenceValue = selected.Button;
                    lookData.ApplyModifiedPropertiesWithoutUndo();
                }
                // The amount in a red badge at the bottom right, as in the mock-up.
                if (boost.Find("Image") is RectTransform badge)
                {
                    badge.GetComponent<Image>().sprite = closeCircle;
                    badge.anchoredPosition = BadgeSpot;
                    badge.sizeDelta = new Vector2(92, 92);
                    var count = badge.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (count != null) count.color = Color.white;
                    badge.SetAsLastSibling();
                }
                // Sweet Sugar's plus (no boosters left) and check (selected) on the same spot; the check becomes the green
                // badge with a check mark, drawn over the amount.
                if (boost.Find("Indicator") is RectTransform indicator)
                {
                    indicator.anchorMin = indicator.anchorMax = new Vector2(0.5f, 0.5f);
                    indicator.anchoredPosition = Vector2.zero;
                    indicator.localScale = Vector3.one;
                    foreach (var spotName in new[] { "Image", "Plus" })
                        if (indicator.Find(spotName) is RectTransform spot)
                        {
                            spot.anchorMin = spot.anchorMax = new Vector2(0.5f, 0.5f);
                            spot.anchoredPosition = BadgeSpot;
                            spot.sizeDelta = new Vector2(100, 100);
                            spot.localScale = Vector3.one;
                        }
                    if (indicator.Find("Image") is RectTransform holder)
                    {
                        var holderImage = holder.GetComponent<Image>();
                        if (holderImage != null) holderImage.enabled = false; // its plus picture would show under the check
                        if (holder.Find("Check") is RectTransform check)
                        {
                            check.anchorMin = check.anchorMax = new Vector2(0.5f, 0.5f);
                            check.anchoredPosition = Vector2.zero;
                            check.sizeDelta = new Vector2(108, 108);
                            check.localScale = Vector3.one;
                            var checkImage = check.GetComponent<Image>();
                            checkImage.sprite = selected.Badge;
                            checkImage.preserveAspect = true;
                            var symbol = Child(check, "Simbolo");
                            symbol.anchorMin = Vector2.zero;
                            symbol.anchorMax = Vector2.one;
                            symbol.offsetMin = new Vector2(20, 24);
                            symbol.offsetMax = new Vector2(-20, -18);
                            var symbolImage = ImageOn(symbol);
                            symbolImage.sprite = selected.Check;
                            symbolImage.preserveAspect = true;
                        }
                    }
                    indicator.SetAsLastSibling();
                }
                if (boost.Find("Lock") is Transform locked) locked.SetAsLastSibling();
            }
            log.Add("reforços nos botões amarelos (verdes com check quando escolhidos), quantidade no selo vermelho");

            // Green "Jogar".
            var play = (RectTransform)window.Find("Play");
            play.localScale = Vector3.one;
            Place(play, new Vector2(0, PlayY), PlaySize);
            var playImage = play.GetComponent<Image>();
            playImage.sprite = green;
            playImage.type = Image.Type.Sliced;
            playImage.pixelsPerUnitMultiplier = 1;
            playImage.preserveAspect = false;
            var playText = play.GetComponentInChildren<TextMeshProUGUI>(true);
            if (playText != null)
            {
                var rect = playText.rectTransform;
                rect.localScale = Vector3.one;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(20, 18);
                rect.offsetMax = new Vector2(-20, -10);
                Style(playText, "Jogar", Cream, 50, 96);
            }
            log.Add("botão verde \"Jogar\"");

            // Close: red circle with the white X, at the frame's top right.
            var close = (RectTransform)window.Find("Close");
            Place(close, CloseCenter, new Vector2(CloseSize, CloseSize));
            var closeImage = close.GetComponent<Image>();
            closeImage.sprite = closeCircle;
            closeImage.preserveAspect = true;
            var x = Child(close, "X");
            x.anchorMin = Vector2.zero;
            x.anchorMax = Vector2.one;
            x.offsetMin = Vector2.one * 36;
            x.offsetMax = Vector2.one * -36;
            var xImage = ImageOn(x);
            xImage.sprite = closeX;
            xImage.preserveAspect = true;
            log.Add("botão de sair (X) no canto superior direito");

            // Drawing order: frame, beige, the boosters' label and buttons, the rest, the close button on top.
            frameRect.SetSiblingIndex(0);
            beigeRect.SetSiblingIndex(1);
            if (boostLabel != null) boostLabel.transform.SetSiblingIndex(2);
            boosters.SetSiblingIndex(3);
            close.SetAsLastSibling();
        }

        // Pictures imported before the importer knew the level start folder have no stretch borders yet.
        static void ReimportFrames()
        {
            foreach (var pair in RoyalAvesAssetImporter.LevelStartBorders)
            {
                var path = ArtFolder + pair.Key + ".png";
                if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.spriteBorder != pair.Value)
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        // A frame drawn at the rectangle's width: the border rows keep their proportions, the middle stretches in height.
        static void Sliced(Image image, Sprite sprite, float width)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.preserveAspect = false;
            image.pixelsPerUnitMultiplier = sprite.rect.width / width * (100f / sprite.pixelsPerUnit);
            image.raycastTarget = true;
        }

        static void Style(TextMeshProUGUI text, string value, Color color, float min, float max)
        {
            foreach (var localize in text.GetComponents<MonoBehaviour>().Where(c => c != null && c.GetType().Name == "LocalizeText"))
                localize.enabled = false; // Sweet Sugar's localization would put the English text back
            text.font = RoyalAvesFonts.Display;
            text.fontSharedMaterial = text.font.material;
            text.enableVertexGradient = false;
            text.text = value;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            AutoSize(text, min, max);
        }

        static void AutoSize(TextMeshProUGUI text, float min, float max)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
            text.fontSize = max;
            text.raycastTarget = false;
        }

        static void FromTop(RectTransform rect, float fromTop, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, -fromTop);
            rect.sizeDelta = size;
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

        static TextMeshProUGUI TextOn(RectTransform rect)
        {
            var text = rect.GetComponent<TextMeshProUGUI>();
            return text != null ? text : rect.gameObject.AddComponent<TextMeshProUGUI>();
        }

        static void Hide(Transform target)
        {
            if (target != null) target.gameObject.SetActive(false);
        }

        static Sprite Load(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + file + ".png");
    }
}
