// The lobby's settings screen: the postal background, the standard leather header with the title on its gold plate,
// and one row per setting — music, sound, vibration, hint and notifications. The gear opens and closes it; so does the
// red X.
//
// The screen lives inside the lobby prefab (Content/TelaConfiguracoes), so it can be moved and resized by hand in the
// Inspector like the rest of the lobby. Build it once with the menu Royal Aves > Criar tela de configurações; from
// then on this script only reads and writes the settings and swaps the on/off pictures.
//
// Every setting is stored the way Sweet Sugar already stores sound and music: PlayerPrefs with 1 for on and -80 for
// off (the volume in decibels), so a value written here reads the same everywhere else in the game.
using SweetSugar.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public class SettingsScreen : MonoBehaviour
    {
        public const string MusicKey = "Music";
        public const string SoundKey = "Sound";
        public const string VibrationKey = "Vibration";
        public const string HintKey = "Hint";
        public const string NotificationsKey = "Notifications";

        [Header("Fechar")]
        [SerializeField] Button closeButton;

        [Header("Música, som e vibração: o botão troca de figura")]
        [SerializeField] Button musicButton;
        [SerializeField] Image musicIcon;
        [SerializeField] Button soundButton;
        [SerializeField] Image soundIcon;
        [SerializeField] Button vibrationButton;
        [SerializeField] Image vibrationIcon;

        [Header("Dica e notificações: o botão escreve o estado")]
        [SerializeField] Button hintButton;
        [SerializeField] Image hintPill;
        [SerializeField] TextMeshProUGUI hintText;
        [SerializeField] Button notificationsButton;
        [SerializeField] Image notificationsPill;
        [SerializeField] TextMeshProUGUI notificationsText;

        [Header("Textos e cores do estado")]
        [SerializeField] string onWord = "Ligado";
        [SerializeField] string offWord = "Desligado";
        [SerializeField] Color onColour = new Color32(0x3E, 0xA9, 0x3E, 0xFF);
        [SerializeField] Color offColour = new Color32(0x8A, 0x2A, 0x22, 0xFF);

        [Header("Figuras de ligado/desligado (Resources/RoyalAvesFeatures se ficar vazio)")]
        [SerializeField] Sprite musicOn, musicOff, soundOn, soundOff, vibrationOn, vibrationOff;

        /// Sound of a tap, given by the lobby.
        public System.Action OnTap;

        public bool IsOpen => gameObject.activeSelf;

        void Awake()
        {
            UseFeaturesArtWhenEmpty();
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (musicButton != null)
                musicButton.onClick.AddListener(() =>
                    Toggle(MusicKey, MusicBase.Instance != null ? MusicBase.Instance.audioMixer : null, "MusicVolume"));
            if (soundButton != null)
                soundButton.onClick.AddListener(() =>
                    Toggle(SoundKey, SoundBase.Instance != null ? SoundBase.Instance.audioMixer : null, "SoundVolume"));
            if (vibrationButton != null) vibrationButton.onClick.AddListener(() => Toggle(VibrationKey, null, null));
            if (hintButton != null) hintButton.onClick.AddListener(() => Toggle(HintKey, null, null));
            if (notificationsButton != null) notificationsButton.onClick.AddListener(() => Toggle(NotificationsKey, null, null));
        }

        // The screen sits inactive inside the prefab, so Awake only runs the first time the gear opens it: turning it
        // on comes before reading the settings, or the first open would draw the pictures of an unbuilt screen.
        public void Open()
        {
            OnTap?.Invoke();
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            OnTap?.Invoke();
            gameObject.SetActive(false);
        }

        public static bool IsOn(string key) => PlayerPrefs.GetInt(key, 1) > -80;

        void Toggle(string key, AudioMixer mixer, string parameter)
        {
            var volume = IsOn(key) ? -80 : 1;
            if (mixer != null && !string.IsNullOrEmpty(parameter)) mixer.SetFloat(parameter, volume);
            PlayerPrefs.SetInt(key, volume);
            PlayerPrefs.Save();
            OnTap?.Invoke();
            Refresh();
        }

        public void Refresh()
        {
            if (musicIcon != null) musicIcon.sprite = IsOn(MusicKey) ? musicOn : musicOff;
            if (soundIcon != null) soundIcon.sprite = IsOn(SoundKey) ? soundOn : soundOff;
            if (vibrationIcon != null) vibrationIcon.sprite = IsOn(VibrationKey) ? vibrationOn : vibrationOff;
            SetState(hintPill, hintText, IsOn(HintKey));
            SetState(notificationsPill, notificationsText, IsOn(NotificationsKey));
        }

        void SetState(Image pill, TextMeshProUGUI label, bool on)
        {
            if (pill != null) pill.color = on ? onColour : offColour;
            if (label != null) label.text = on ? onWord : offWord;
        }

        /// The builder fills these in, but a screen assembled by hand can leave them empty and still work.
        void UseFeaturesArtWhenEmpty()
        {
            var art = LobbyFeaturesConfig.Instance;
            if (art == null) return;
            if (musicOn == null) musicOn = art.gameMenuMusicOn;
            if (musicOff == null) musicOff = art.gameMenuMusicOff;
            if (soundOn == null) soundOn = art.gameMenuSoundOn;
            if (soundOff == null) soundOff = art.gameMenuSoundOff;
            if (vibrationOn == null) vibrationOn = art.gameMenuVibrationOn;
            if (vibrationOff == null) vibrationOff = art.gameMenuVibrationOff;
        }
    }
}
