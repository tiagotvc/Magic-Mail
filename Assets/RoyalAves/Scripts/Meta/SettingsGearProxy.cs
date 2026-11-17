// A engrenagem de ajustes da barra de power-ups durante a fase, e o menu que ela abre: a tela escurece, a engrenagem fica
// vermelha e os botões redondos de música, som e vibração SAEM DA ENGRENAGEM para a esquerda, cada um com uma pulsação ao
// chegar. Tocar de novo na engrenagem (ou no fundo escuro) faz os botões voltarem para a engrenagem, da esquerda para a
// direita, e a fase volta ao normal. Cada botão liga e desliga a sua opção; quando está desligado aparece um risco por cima
// do ícone (sprite).
//
// Tudo que dá para configurar fica no asset Resources/Settings/GameplaySettingsMenu (GameplaySettingsMenuConfig): quadro do
// botão, ícones, sprite do risco (tem um placeholder), tamanhos, cores e tempos. Sem o asset, usa as imagens padrão do menu
// (Resources/RoyalAvesFeatures).
//
// A engrenagem original do Sweet Sugar (CanvasGlobal/SettingsButton) fica onde está: MenuReference.HideAll a mantém pelo
// nome e o LobbyController a mostra no mapa. Aqui ela só fica fora de vista enquanto o HUD do jogo está na tela.
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
        static readonly Color Navy = new Color32(0x17, 0x2E, 0x67, 0xFF);
        static readonly Color Cream = new Color32(0xFF, 0xF4, 0xC9, 0xFF);
        static readonly Color PostalTop = new Color32(0xFB, 0xFC, 0xE8, 0xFF);
        static readonly Color PostalBottom = new Color32(0xEE, 0x9A, 0x10, 0xFF);
        const float ConfirmWidth = 0.84f;   // of the screen's width
        const float ConfirmAspect = 0.95f;  // height / width

        sealed class Round
        {
            public string key;
            public RectTransform rect;
            public CanvasGroup group;
            public Image frame, icon, slash;
            public System.Action onClick;
            public bool isToggle;
            public Sprite iconSprite;
        }

        CanvasGroup hidden;
        LobbyFeaturesConfig art;
        GameplaySettingsMenuConfig config;
        RectTransform overlay;
        RectTransform gearSpot;
        RectTransform confirm;
        Image dimImage;
        Image confirmBox, stayButton, leaveButton;
        readonly List<Round> buttons = new List<Round>();
        bool paused, closing, built;
        Coroutine running;
        static Sprite placeholderSlash;

        void Awake() => GetComponent<Button>().onClick.AddListener(OpenMenu);

        void OnEnable() => ShowOriginal(false);

        void OnDisable()
        {
            ShowOriginal(true);
            CloseNow();
        }

        // ------------------------------------------------------------------------------------------ abrir / fechar

        void OpenMenu()
        {
            art = LobbyFeaturesConfig.Instance;
            config = Resources.Load<GameplaySettingsMenuConfig>(GameplaySettingsMenuConfig.ResourcePath);
            if (art == null && config == null)
            {
                Debug.LogWarning("Correio Mágico: faltam as imagens do menu da tela de jogo em Resources/RoyalAvesFeatures.");
                return;
            }
            if (overlay != null && overlay.gameObject.activeSelf)
            {
                // Tocar de novo na engrenagem fecha
                CloseMenu();
                return;
            }
            if (overlay == null || !built) { if (overlay != null) Destroy(overlay.gameObject); Build(); }
            Click();
            Place();
            ShowButtons(true);
            RefreshIcons();
            overlay.gameObject.SetActive(true);
            // The board takes no swipes while the menu is up, as with Sweet Sugar's popups.
            if (LevelManager.THIS != null && LevelManager.THIS.gameStatus == GameState.Playing)
            {
                LevelManager.THIS.gameStatus = GameState.Pause;
                paused = true;
            }
            closing = false;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Slide(true));
        }

        /// Fecha com a animação (os botões voltam para a engrenagem, da esquerda para a direita).
        void CloseMenu()
        {
            if (overlay == null || !overlay.gameObject.activeSelf || closing) return;
            if (!isActiveAndEnabled) { CloseNow(); return; }
            closing = true;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Slide(false));
        }

        void CloseNow()
        {
            if (running != null) StopCoroutine(running);
            running = null;
            closing = false;
            if (overlay != null && overlay.gameObject.activeSelf) overlay.gameObject.SetActive(false);
            if (paused && LevelManager.THIS != null && LevelManager.THIS.gameStatus == GameState.Pause)
                LevelManager.THIS.gameStatus = GameState.Playing; // (Sweet Sugar's WaitAfterClose is never handled)
            paused = false;
        }

        // ------------------------------------------------------------------------------------------ construção

        // O menu é um canvas próprio por cima da tela de jogo: desenha acima do HUD e do tabuleiro.
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

            dimImage = overlay.gameObject.AddComponent<Image>();
            dimImage.color = config != null ? config.dimColor : new Color(0, 0, 0, 0.7f);
            AddButton(overlay.gameObject, dimImage, () => { Click(); CloseMenu(); }).transition = Selectable.Transition.None;

            buttons.Clear();
            var musicFirst = config == null || config.musicSoundVibrationOrder;
            var music = MakeRound("Musica", "Music", MusicSprite(), () => Toggle("Music", MusicBase.Instance != null ? MusicBase.Instance.audioMixer : null, "MusicVolume"));
            var sound = MakeRound("Som", "Sound", SoundSprite(), () => Toggle("Sound", SoundBase.Instance != null ? SoundBase.Instance.audioMixer : null, "SoundVolume"));
            var vibration = MakeRound("Vibracao", VibrationKey, VibrationSprite(), () => Toggle(VibrationKey, null, null));
            // Ordem da esquerda para a direita; a engrenagem fica à direita do último.
            if (musicFirst) { buttons.Add(music); buttons.Add(sound); buttons.Add(vibration); }
            else { buttons.Add(vibration); buttons.Add(sound); buttons.Add(music); }
            if (config != null && config.showExitButton && (config.exitIcon != null || (art != null && art.gameMenuExit != null)))
            {
                var exit = MakeRound("Sair", null, config.exitIcon != null ? config.exitIcon : art.gameMenuExit, AskToLeave);
                exit.isToggle = false;
                buttons.Insert(0, exit);
            }

            // A engrenagem vermelha fica em cima da engrenagem da barra e fecha o menu.
            gearSpot = Node("Engrenagem", overlay);
            var gearSprite = art != null ? art.gameMenuGearOpen : null;
            var gear = Picture(gearSpot, gearSprite);
            AddButton(gearSpot.gameObject, gear, () => { Click(); CloseMenu(); });
            BuildConfirm();
            overlay.gameObject.SetActive(false);
            built = true;
        }

        Sprite MusicSprite() => config != null && config.musicIcon != null ? config.musicIcon : art != null ? art.gameMenuMusicOn : null;
        Sprite SoundSprite() => config != null && config.soundIcon != null ? config.soundIcon : art != null ? art.gameMenuSoundOn : null;
        Sprite VibrationSprite() => config != null && config.vibrationIcon != null ? config.vibrationIcon : art != null ? art.gameMenuVibrationOn : null;

        Round MakeRound(string name, string key, Sprite iconSprite, System.Action onClick)
        {
            var round = new Round { key = key, isToggle = key != null, onClick = onClick, iconSprite = iconSprite };
            round.rect = Node(name, overlay);
            round.group = round.rect.gameObject.AddComponent<CanvasGroup>();

            var frameSprite = config != null && config.buttonFrame != null ? config.buttonFrame : null;
            round.frame = Picture(round.rect, frameSprite);
            round.frame.preserveAspect = false;
            round.frame.color = config != null ? config.buttonTint : Color.white;
            if (frameSprite == null) round.frame.color = new Color(0.45f, 0.2f, 0.8f, 1f);   // sem arte: roxo liso

            var iconRect = Node("Icone", round.rect);
            round.icon = Picture(iconRect, iconSprite);
            round.icon.raycastTarget = false;

            var slashRect = Node("Risco", round.rect);
            round.slash = Picture(slashRect, null);
            round.slash.raycastTarget = false;

            AddButton(round.rect.gameObject, round.frame, () => { Click(); round.onClick(); });
            return round;
        }

        // ------------------------------------------------------------------------------------------ posição e animação

        // Cada botão tem um tamanho (o da engrenagem × multiplicador) e um "lugar" final; começa na engrenagem.
        Vector2 gearCenter, slotStep;
        float buttonSize;

        void Place()
        {
            var gearRect = (RectTransform)transform;
            var centre = (Vector2)overlay.InverseTransformPoint(gearRect.TransformPoint(gearRect.rect.center));
            var size = gearRect.rect.size.x * gearRect.lossyScale.x / overlay.lossyScale.x;
            gearCenter = centre - overlay.rect.center;
            var multiplier = config != null ? config.sizeMultiplier : 1f;
            buttonSize = size * multiplier;
            var spacing = config != null ? config.spacing : 1.12f;
            var column = config != null && config.layout == GameplaySettingsMenuConfig.Layout.ColumnAboveGear;
            slotStep = column ? Vector2.up * (buttonSize * spacing) : Vector2.left * (buttonSize * spacing);

            gearSpot.anchorMin = gearSpot.anchorMax = gearSpot.pivot = new Vector2(0.5f, 0.5f);
            gearSpot.sizeDelta = Vector2.one * size;
            gearSpot.anchoredPosition = gearCenter;

            foreach (var round in buttons)
            {
                round.rect.anchorMin = round.rect.anchorMax = round.rect.pivot = new Vector2(0.5f, 0.5f);
                round.rect.sizeDelta = Vector2.one * buttonSize;
                round.rect.anchoredPosition = gearCenter;
                Stretch(round.frame.rectTransform);
                var scale = config != null ? config.iconScale : 0.62f;
                round.icon.rectTransform.anchorMin = round.icon.rectTransform.anchorMax = Vector2.one * 0.5f;
                round.icon.rectTransform.sizeDelta = Vector2.one * buttonSize * scale;
                round.icon.rectTransform.anchoredPosition = (config != null ? config.iconOffset : Vector2.zero) * buttonSize;
                round.icon.color = config != null ? config.iconTint : Color.white;
                var slashSize = config != null ? config.slashSize : new Vector2(0.78f, 0.78f);
                round.slash.rectTransform.anchorMin = round.slash.rectTransform.anchorMax = Vector2.one * 0.5f;
                round.slash.rectTransform.sizeDelta = new Vector2(slashSize.x, slashSize.y) * buttonSize;
                round.slash.rectTransform.anchoredPosition = (config != null ? config.slashOffset : Vector2.zero) * buttonSize;
                round.slash.sprite = config != null && config.slash != null ? config.slash : PlaceholderSlash();
                round.slash.color = config != null ? config.slashColor : new Color(0.95f, 0.25f, 0.3f, 1f);
            }
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static float EaseOut(float k) => 1f - (1f - k) * (1f - k) * (1f - k);

        /// Abre (os botões saem da engrenagem para a esquerda, com pulsação) ou fecha (voltam da esquerda para a engrenagem).
        IEnumerator Slide(bool open)
        {
            var slide = config != null ? config.slideSeconds : 0.3f;
            var close = config != null ? config.closeSeconds : 0.22f;
            var stagger = config != null ? config.stagger : 0.07f;
            var overshoot = config != null ? config.popOvershoot : 0.16f;
            var fade = config != null ? config.dimFadeSeconds : 0.18f;
            var dimTarget = config != null ? config.dimColor : new Color(0, 0, 0, 0.7f);
            var duration = open ? slide : close;
            var count = buttons.Count;
            var total = duration + stagger * Mathf.Max(0, count - 1);

            if (open) foreach (var round in buttons) { round.rect.localScale = Vector3.zero; round.group.alpha = 0f; }
            for (float t = 0; ; t += Time.unscaledDeltaTime)
            {
                var finished = true;
                for (var i = 0; i < count; i++)
                {
                    // Na abertura o primeiro da direita sai primeiro; no fechamento o da esquerda volta primeiro (esquerda -> direita).
                    var order = open ? count - 1 - i : i;
                    var k = Mathf.Clamp01((t - order * stagger) / duration);
                    if (k < 1f) finished = false;
                    var progress = open ? k : 1f - k;             // 1 = no lugar, 0 = na engrenagem
                    var eased = EaseOut(progress);
                    var round = buttons[i];
                    round.rect.anchoredPosition = gearCenter + slotStep * (count - i) * eased;
                    // Pulsação: passa um pouco do tamanho e assenta (só na abertura); no fechamento só encolhe.
                    var pop = open ? 1f + overshoot * Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI) : 1f;
                    round.rect.localScale = Vector3.one * Mathf.Lerp(0.35f, 1f, eased) * pop;
                    round.group.alpha = Mathf.Clamp01(progress * 1.6f);
                }
                var dimK = fade > 0f ? Mathf.Clamp01(open ? t / fade : 1f - t / fade) : (open ? 1f : 0f);
                if (dimImage != null) dimImage.color = new Color(dimTarget.r, dimTarget.g, dimTarget.b, dimTarget.a * dimK);
                if (finished && t >= (open ? 0f : fade) ) break;
                if (t > total + fade + 1f) break;
                yield return null;
            }
            running = null;
            if (!open) CloseNow();
        }

        // ------------------------------------------------------------------------------------------ "Sair da fase?"

        // "Sair da fase?": the life shop's cream box with the heart, "Continuar" to go back to the level and a red "Sair".
        // Laid out in fractions of the box, which is sized from the screen when it opens.
        void BuildConfirm()
        {
            confirm = Node("AvisoSair", overlay);
            confirm.anchorMin = confirm.anchorMax = confirm.pivot = new Vector2(0.5f, 0.5f);
            if (art == null) { confirm.gameObject.SetActive(false); return; }
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
            if (art == null || confirm == null || confirmBox == null) { LeaveLevel(); return; }
            ShowButtons(false);
            var width = overlay.rect.width * ConfirmWidth;
            confirm.sizeDelta = new Vector2(width, width * ConfirmAspect);
            confirm.anchoredPosition = Vector2.zero;
            // The borders keep the pictures' proportions at this size.
            if (art.confirmBox != null) confirmBox.pixelsPerUnitMultiplier = art.confirmBox.rect.width / (width * 0.3f);
            var buttonHeight = width * ConfirmAspect * 0.16f;
            foreach (var button in new[] { stayButton, leaveButton })
                if (button.sprite != null) button.pixelsPerUnitMultiplier = button.sprite.rect.height / buttonHeight;
            confirm.gameObject.SetActive(true);
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Pop(confirm));
        }

        void ShowButtons(bool visible)
        {
            foreach (var round in buttons) round.rect.gameObject.SetActive(visible);
            gearSpot.gameObject.SetActive(visible);
            if (confirm != null && visible) confirm.gameObject.SetActive(false);
        }

        static IEnumerator Pop(RectTransform rect)
        {
            const float seconds = 0.18f;
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                var k = t / seconds;
                rect.localScale = Vector3.one * Mathf.Lerp(0.6f, 1, 1 - (1 - k) * (1 - k));
                yield return null;
            }
            rect.localScale = Vector3.one;
        }

        // ------------------------------------------------------------------------------------------ ajustes

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

        /// Ligado: ícone normal. Desligado: ícone mais escuro com o risco (sprite) por cima.
        void RefreshIcons()
        {
            var dark = config != null ? config.offIconBrightness : 0.55f;
            foreach (var round in buttons)
            {
                if (!round.isToggle || round.key == null) { round.slash.gameObject.SetActive(false); continue; }
                var on = IsOn(round.key);
                round.slash.gameObject.SetActive(!on);
                var tint = config != null ? config.iconTint : Color.white;
                round.icon.color = on ? tint : new Color(tint.r * dark, tint.g * dark, tint.b * dark, tint.a);
            }
        }

        // Sweet Sugar leaves a level by closing its settings popup in game (AnimationEventManager.BackToMap).
        void LeaveLevel()
        {
            CloseNow();
            var settings = MenuReference.THIS != null ? MenuReference.THIS.Settings : null;
            var events = settings != null ? settings.GetComponent<AnimationEventManager>() : null;
            if (events != null) events.BackToMap();
            else Debug.LogWarning("Correio Mágico: não achei o menu Settings do Sweet Sugar para sair da fase.");
        }

        static void Click()
        {
            if (SoundBase.Instance != null) SoundBase.Instance.PlayOneShot(SoundBase.Instance.click);
        }

        /// Risco diagonal de reserva (gerado em código) para quando o asset de configuração ainda não tem o sprite.
        static Sprite PlaceholderSlash()
        {
            if (placeholderSlash != null) return placeholderSlash;
            const int n = 128;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    // linha do canto inferior esquerdo ao superior direito, com ponta arredondada e contorno escuro
                    var d = Mathf.Abs((x - y) / 1.4142f);
                    var along = (x + y) / 2f / n;
                    var inside = along > 0.06f && along < 0.94f;
                    var core = inside ? Mathf.Clamp01((7f - d) / 2f) : 0f;
                    var edge = inside ? Mathf.Clamp01((11f - d) / 2f) : 0f;
                    var color = Color.Lerp(new Color(0.18f, 0.05f, 0.08f, 1f), Color.white, core);
                    texture.SetPixel(x, y, new Color(color.r, color.g, color.b, edge));
                }
            texture.Apply();
            placeholderSlash = Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            placeholderSlash.hideFlags = HideFlags.HideAndDontSave;
            return placeholderSlash;
        }

        // ------------------------------------------------------------------------------------------ utilidades

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
