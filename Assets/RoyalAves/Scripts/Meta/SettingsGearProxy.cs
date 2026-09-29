// The settings gear as the last round button of the booster bar, and the menu it opens, after the user's Royal
// Match-style reference: the screen dims, the gear turns red, and a column of round buttons rises above it - vibration,
// sound, music and, on top, leaving the level. Tapping the red gear or the dim closes it. Leaving asks first: the life
// was spent when the level started and only comes back with a win, so quitting loses it.
//
// Sweet Sugar's own gear (CanvasGlobal/SettingsButton) has to stay where it is: MenuReference.HideAll keeps it by name
// and LobbyController shows it on the map. It is only kept out of sight while the game HUD is up.
// The pictures come from Resources/RoyalAvesFeatures (Menu da tela de jogo).
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SweetSugar.Scripts;
using TMPro;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.GUI;
using SweetSugar.Scripts.System;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    [RequireComponent(typeof(Button))]
    public class SettingsGearProxy : MonoBehaviour
    {
        const string VibrationKey = "Vibration";
        const float Spacing = 1.12f;     // distance between the buttons' centres, in button sizes
        const float RiseSeconds = 0.18f;
        const float Stagger = 0.04f;
        static readonly Color Dim = new Color(0, 0, 0, 0.6f);
        static readonly Color Navy = new Color32(0x17, 0x2E, 0x67, 0xFF);
        static readonly Color Cream = new Color32(0xFF, 0xF4, 0xC9, 0xFF);
        static readonly Color PostalTop = new Color32(0xFB, 0xFC, 0xE8, 0xFF);
        static readonly Color PostalBottom = new Color32(0xEE, 0x9A, 0x10, 0xFF);
        const float ConfirmWidth = 0.84f;   // of the screen's width
        const float ConfirmAspect = 0.95f;  // height / width

        CanvasGroup hidden;
        LobbyFeaturesConfig art;
        RectTransform overlay;
        RectTransform gearSpot;
        RectTransform confirm;
        Image confirmBox, stayButton, leaveButton;
        readonly List<RectTransform> column = new List<RectTransform>();
        Image music, sound, vibration;
        bool paused;

        void Awake() => GetComponent<Button>().onClick.AddListener(OpenMenu);

        void OnEnable() => ShowOriginal(false);

        void OnDisable()
        {
            ShowOriginal(true);
            CloseMenu();
        }

        void OpenMenu()
        {
            art = LobbyFeaturesConfig.Instance;
            if (art == null || art.gameMenuExit == null)
            {
                Debug.LogWarning("Correio Mágico: faltam as imagens do menu da tela de jogo em Resources/RoyalAvesFeatures.");
                return;
            }
            if (overlay == null) Build();
            Click();
            Place();
            ShowColumn(true);
            RefreshIcons();
            overlay.gameObject.SetActive(true);
            // The board takes no swipes while the menu is up, as with Sweet Sugar's popups.
            if (LevelManager.THIS != null && LevelManager.THIS.gameStatus == GameState.Playing)
            {
                LevelManager.THIS.gameStatus = GameState.Pause;
                paused = true;
            }
            StopAllCoroutines();
            StartCoroutine(Rise());
        }

        void CloseMenu()
        {
            if (overlay == null || !overlay.gameObject.activeSelf) return;
            overlay.gameObject.SetActive(false);
            if (paused && LevelManager.THIS != null && LevelManager.THIS.gameStatus == GameState.Pause)
                LevelManager.THIS.gameStatus = GameState.Playing; // (Sweet Sugar's WaitAfterClose is never handled)
            paused = false;
        }

        // The menu is its own canvas over the game screen, so it draws above the HUD and the board.
        void Build()
        {
            var root = GetComponentInParent<Canvas>().rootCanvas;
            overlay = Node("MenuDoJogo", root.transform);
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            var canvas = overlay.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingLayerID = root.sortingLayerID;
            canvas.sortingOrder = root.sortingOrder + 50;
            overlay.gameObject.AddComponent<GraphicRaycaster>();

            var dim = overlay.gameObject.AddComponent<Image>();
            dim.color = Dim;
            AddButton(overlay.gameObject, dim, () => { Click(); CloseMenu(); }).transition = Selectable.Transition.None;

            // Bottom to top: vibration, sound, music, leave the level.
            vibration = RoundButton("Vibracao", () => Toggle(VibrationKey, null, null));
            sound = RoundButton("Som", () => Toggle("Sound", SoundBase.Instance != null ? SoundBase.Instance.audioMixer : null, "SoundVolume"));
            music = RoundButton("Musica", () => Toggle("Music", MusicBase.Instance != null ? MusicBase.Instance.audioMixer : null, "MusicVolume"));
            var exit = RoundButton("Sair", AskToLeave);
            exit.sprite = art.gameMenuExit;

            // The red gear sits on the bar's gear and closes the menu.
            gearSpot = Node("Engrenagem", overlay);
            var gear = Picture(gearSpot, art.gameMenuGearOpen);
            AddButton(gearSpot.gameObject, gear, () => { Click(); CloseMenu(); });
            BuildConfirm();
            overlay.gameObject.SetActive(false);
        }

        // "Sair da fase?": the life shop's cream box with the heart, "Continuar" to go back to the level and a red "Sair".
        // Laid out in fractions of the box, which is sized from the screen when it opens.
        void BuildConfirm()
        {
            confirm = Node("AvisoSair", overlay);
            confirm.anchorMin = confirm.anchorMax = confirm.pivot = new Vector2(0.5f, 0.5f);
            confirmBox = Picture(confirm, art.confirmBox);
            confirmBox.preserveAspect = false;
            confirmBox.type = Image.Type.Sliced;

            var title = Label(confirm, "Sair da fase?", 0.08f, 0.8f, 0.92f, 0.94f, Color.white, art.postalFont);
            if (art.postalFont != null)
            {
                if (art.postalMaterial != null) title.fontSharedMaterial = art.postalMaterial;
                title.enableVertexGradient = true;
                title.colorGradient = new VertexGradient(PostalTop, PostalTop, PostalBottom, PostalBottom);
            }
            var heart = Node("Coracao", confirm);
            Span(heart, 0.36f, 0.47f, 0.64f, 0.77f);
            Picture(heart, art.heart).raycastTarget = false;
            var copy = Label(confirm, "Se sair agora, você perde uma vida.", 0.08f, 0.27f, 0.92f, 0.45f, Navy, art.displayFont);
            copy.textWrappingMode = TextWrappingModes.Normal;

            stayButton = TextButton("Continuar", art.buttonGreen, 0.07f, 0.48f, () => { Click(); CloseMenu(); });
            leaveButton = TextButton("Sair", art.buttonRed, 0.52f, 0.93f, () => { Click(); LeaveLevel(); });
            confirm.gameObject.SetActive(false);
        }

        Image TextButton(string label, Sprite sprite, float left, float right, UnityEngine.Events.UnityAction onClick)
        {
            var rect = Node(label, confirm);
            Span(rect, left, 0.07f, right, 0.23f);
            var image = Picture(rect, sprite);
            image.preserveAspect = false;
            image.type = Image.Type.Sliced;
            AddButton(rect.gameObject, image, onClick);
            var text = Label(rect, label, 0, 0, 1, 1, Cream, art.displayFont);
            text.rectTransform.offsetMin = new Vector2(20, 18);
            text.rectTransform.offsetMax = new Vector2(-20, -10);
            return image;
        }

        void AskToLeave()
        {
            ShowColumn(false);
            var width = overlay.rect.width * ConfirmWidth;
            confirm.sizeDelta = new Vector2(width, width * ConfirmAspect);
            confirm.anchoredPosition = Vector2.zero;
            // The borders keep the pictures' proportions at this size.
            if (art.confirmBox != null) confirmBox.pixelsPerUnitMultiplier = art.confirmBox.rect.width / (width * 0.3f);
            var buttonHeight = width * ConfirmAspect * 0.16f;
            foreach (var button in new[] { stayButton, leaveButton })
                if (button.sprite != null) button.pixelsPerUnitMultiplier = button.sprite.rect.height / buttonHeight;
            confirm.gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(Pop(confirm));
        }

        void ShowColumn(bool visible)
        {
            foreach (var rect in column) rect.gameObject.SetActive(visible);
            gearSpot.gameObject.SetActive(visible);
            if (confirm != null && visible) confirm.gameObject.SetActive(false);
        }

        static IEnumerator Pop(RectTransform rect)
        {
            for (float t = 0; t < RiseSeconds; t += Time.unscaledDeltaTime)
            {
                var k = t / RiseSeconds;
                rect.localScale = Vector3.one * Mathf.Lerp(0.6f, 1, 1 - (1 - k) * (1 - k));
                yield return null;
            }
            rect.localScale = Vector3.one;
        }

        Image RoundButton(string name, System.Action onClick)
        {
            var rect = Node(name, overlay);
            var image = Picture(rect, null);
            AddButton(rect.gameObject, image, () => { Click(); onClick(); });
            column.Add(rect);
            return image;
        }

        // Everything is laid out from where the bar's gear is on this screen.
        void Place()
        {
            var gearRect = (RectTransform)transform;
            var centre = (Vector2)overlay.InverseTransformPoint(gearRect.TransformPoint(gearRect.rect.center));
            var size = gearRect.rect.size.x * gearRect.lossyScale.x / overlay.lossyScale.x;
            foreach (var rect in column.Append(gearSpot))
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = Vector2.one * size;
                rect.anchoredPosition = centre - overlay.rect.center;
            }
        }

        IEnumerator Rise()
        {
            var start = gearSpot.anchoredPosition;
            var step = gearSpot.sizeDelta.y * Spacing;
            foreach (var rect in column) rect.localScale = Vector3.zero;
            for (float t = 0; ; t += Time.unscaledDeltaTime)
            {
                var done = true;
                for (var i = 0; i < column.Count; i++)
                {
                    var k = Mathf.Clamp01((t - i * Stagger) / RiseSeconds);
                    if (k < 1) done = false;
                    var eased = 1 - (1 - k) * (1 - k);
                    column[i].anchoredPosition = start + Vector2.up * (step * (i + 1) * eased);
                    column[i].localScale = Vector3.one * Mathf.Lerp(0.4f, 1, eased);
                }
                if (done) yield break;
                yield return null;
            }
        }

        // The same switches as Sweet Sugar's settings popup: mixer volume 1 (on) or -80 (off), saved in PlayerPrefs.
        // Vibration is only remembered for now; the game does not vibrate yet.
        void Toggle(string key, UnityEngine.Audio.AudioMixer mixer, string parameter)
        {
            var volume = IsOn(key) ? -80 : 1;
            if (mixer != null) mixer.SetFloat(parameter, volume);
            PlayerPrefs.SetInt(key, volume);
            PlayerPrefs.Save();
            RefreshIcons();
        }

        static bool IsOn(string key) => PlayerPrefs.GetInt(key, 1) > -80;

        void RefreshIcons()
        {
            music.sprite = IsOn("Music") ? art.gameMenuMusicOn : art.gameMenuMusicOff;
            sound.sprite = IsOn("Sound") ? art.gameMenuSoundOn : art.gameMenuSoundOff;
            vibration.sprite = IsOn(VibrationKey) ? art.gameMenuVibrationOn : art.gameMenuVibrationOff;
        }

        // Sweet Sugar leaves a level by closing its settings popup in game (AnimationEventManager.BackToMap).
        void LeaveLevel()
        {
            CloseMenu();
            var settings = MenuReference.THIS != null ? MenuReference.THIS.Settings : null;
            var events = settings != null ? settings.GetComponent<AnimationEventManager>() : null;
            if (events != null) events.BackToMap();
            else Debug.LogWarning("Correio Mágico: não achei o menu Settings do Sweet Sugar para sair da fase.");
        }

        static void Click()
        {
            if (SoundBase.Instance != null) SoundBase.Instance.PlayOneShot(SoundBase.Instance.click);
        }

        static void Span(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = new Vector2(left, bottom);
            rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static TextMeshProUGUI Label(RectTransform parent, string value, float left, float bottom, float right, float top,
            Color color, TMP_FontAsset font)
        {
            var rect = Node("Texto", parent);
            Span(rect, left, bottom, right, top);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = value;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10;
            text.fontSizeMax = 200;
            text.raycastTarget = false;
            return text;
        }

        static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = parent.gameObject.layer };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        static Image Picture(RectTransform rect, Sprite sprite)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            return image;
        }

        static Button AddButton(GameObject target, Graphic graphic, UnityEngine.Events.UnityAction onClick)
        {
            var button = target.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.onClick.AddListener(onClick);
            return button;
        }

        // Hidden with a CanvasGroup, not by turning it off: LobbyController turns the same object on and off around the
        // map, and the two would undo each other.
        void ShowOriginal(bool visible)
        {
            if (hidden == null)
            {
                var gear = Original();
                if (gear == null) return;
                hidden = gear.GetComponent<CanvasGroup>();
                if (hidden == null) hidden = gear.gameObject.AddComponent<CanvasGroup>();
            }
            hidden.alpha = visible ? 1f : 0f;
            hidden.blocksRaycasts = visible;
            hidden.interactable = visible;
        }

        static Transform Original() =>
            MenuReference.THIS != null ? MenuReference.THIS.transform.Find("SettingsButton") : null;
    }
}
