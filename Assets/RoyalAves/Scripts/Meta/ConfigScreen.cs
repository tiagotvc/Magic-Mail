// Tela de configurações do Correio Mágico (arte de Screens/Configurations). O prefab Resources/UI/ConfigurationsScreen é
// montado por Royal Aves > Tela de configurações > Montar prefab (e sozinho, na primeira vez que o projeto abre sem ele).
// Este script acha os elementos pelo NOME (a hierarquia é a do builder) e liga tudo:
//   - Música / Sons / Dicas: botões redondos; a barra vermelha (Off) aparece quando está desligado. Salvo no
//     PlayerPrefs do mesmo jeito do Sweet Sugar (1 ligado, -80 desligado), então o resto do jogo lê igual.
//   - Notificações: o switch SIM/NÃO.
//   - Idioma: abre a modal com Português e Inglês; a escolha vale na hora e fica salva.
//   - Termos & Privacidade: abre a modal com os links. Suporte, Seguir e Salvar progresso também.
//   - Todo clique toca um som suave gerado por código (sem o "click" do Sweet Sugar) e respeita o botão Sons.
//   - Todo texto é branco com uma sombra preta pequena e sai em português ou inglês.
using System;
using System.Collections.Generic;
using RoyalAves.Save;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.System;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public class ConfigScreen : MonoBehaviour
    {
        public const string ResourcePath = "UI/ConfigurationsScreen";
        public const string ObjectName = "ConfigurationsScreen";
        public const string LanguageKey = "RoyalAves.Language";

        [Header("Links (vazio = o botão só avisa no Console)")]
        [SerializeField] string facebookUrl = "";
        [SerializeField] string instagramUrl = "";
        [SerializeField] string supportEmail = "";
        [SerializeField] string termsUrl = "";
        [SerializeField] string privacyUrl = "";
        [SerializeField] string deleteAccountUrl = "";
        [SerializeField] int followReward = 100;

        [Header("Modal de idioma: botão da opção escolhida e das outras")]
        [SerializeField] Sprite selectedOptionSprite;
        [SerializeField] Sprite otherOptionSprite;

        /// Quem tratar "Preferências de Privacidade" (ex.: o formulário de consentimento de anúncios) assina aqui.
        /// Sem ninguém, o botão abre a política de privacidade.
        public static event Action PrivacyPreferencesRequested;

        static AudioClip clickClip;
        AudioSource source;

        GameObject languageModal, termsModal;
        Image portugueseImage, englishImage;
        RectTransform switchKnob, switchRoot;
        Image switchContainer;
        float knobTarget;
        readonly Dictionary<string, GameObject> offBars = new Dictionary<string, GameObject>();
        readonly List<(string path, string pt, string en)> labels = new List<(string, string, string)>
        {
            ("Main/Music/Label", "Música", "Music"),
            ("Main/Sounds/Label", "Sons", "Sounds"),
            ("Main/Hints/Label", "Dicas", "Hints"),
            ("Main/Notifications/Label", "Notificações", "Notifications"),
            ("Main/Language/Label", "Idioma", "Language"),
            ("Main/Support/Label", "Suporte", "Support"),
            ("Main/Save/Center/Label", "Salvar progresso", "Save progress"),
            ("Main/Terms/Center/Label", "Termos & Privacidade", "Terms & Privacy"),
            ("Main/FollowFacebook/Label", "Seguir", "Follow"),
            ("Main/FollowInstagram/Label", "Seguir", "Follow"),
            ("LanguageModal/Ribbon/Title", "Idioma", "Language"),
            ("LanguageModal/Portuguese/Label", "Português", "Portuguese"),
            ("LanguageModal/English/Label", "Inglês", "English"),
            ("TermsModal/Ribbon/Title", "Termos e Privacidade", "Terms and Privacy"),
            ("TermsModal/TermsOfService/Center/Label", "Termos de Serviço", "Terms of Service"),
            ("TermsModal/PrivacyPolicy/Center/Label", "Política de Privacidade", "Privacy Policy"),
            ("TermsModal/PrivacyPrefs/Center/Label", "Preferências de Privacidade", "Privacy Preferences"),
            ("TermsModal/DeleteAccount/Label", "Excluir conta", "Delete account"),
        };

        public static bool IsPortuguese => LocalizedImage.Current == GameLanguage.Portuguese;
        public static bool NotificationsOn => SettingsScreen.IsOn(SettingsScreen.NotificationsKey);

        // O idioma escolhido volta sozinho a cada abertura do jogo, antes de qualquer cena.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RestoreLanguage()
        {
            var saved = PlayerPrefs.GetString(LanguageKey, "");
            if (!string.IsNullOrEmpty(saved)) CrosssceneData.selectedLanguage = saved;
        }

        /// Abre a tela (cria a partir do prefab na primeira vez). False se o prefab ainda não existe.
        public static bool Open()
        {
            var menus = MenuReference.THIS;
            if (menus == null) return false;
            var parent = menus.transform;
            var existing = parent.Find(ObjectName);
            if (existing == null)
            {
                var prefab = Resources.Load<GameObject>(ResourcePath);
                if (prefab == null) return false;
                var created = Instantiate(prefab, parent, false);
                created.name = ObjectName;
                existing = created.transform;
            }
            existing.SetAsLastSibling();
            existing.gameObject.SetActive(true);
            return true;
        }

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            languageModal = Child("LanguageModal")?.gameObject;
            termsModal = Child("TermsModal")?.gameObject;
            portugueseImage = Child("LanguageModal/Portuguese")?.GetComponent<Image>();
            englishImage = Child("LanguageModal/English")?.GetComponent<Image>();
            switchRoot = Child("Main/Notifications/Switch") as RectTransform;
            switchKnob = Child("Main/Notifications/Switch/Knob") as RectTransform;
            switchContainer = switchRoot != null ? switchRoot.GetComponent<Image>() : null;

            foreach (var text in GetComponentsInChildren<TMP_Text>(true)) ApplyShadow(text);

            Wire("Main/Header/Close", Close);
            Wire("Main/Music", () => Toggle(SettingsScreen.MusicKey, MusicBase.Instance != null ? MusicBase.Instance.audioMixer : null, "MusicVolume"));
            Wire("Main/Sounds", () => Toggle(SettingsScreen.SoundKey, SoundBase.Instance != null ? SoundBase.Instance.audioMixer : null, "SoundVolume"));
            Wire("Main/Hints", () => Toggle(SettingsScreen.HintKey, null, null));
            Wire("Main/Notifications/Switch", () => Toggle(SettingsScreen.NotificationsKey, null, null));
            Wire("Main/Language", () => ShowModal(languageModal, true));
            Wire("Main/Terms", () => ShowModal(termsModal, true));
            Wire("Main/Support", () => OpenMail("Suporte / Support"));
            Wire("Main/Save", () => { }); // só o som: quem salva é o CloudSaveButton no mesmo botão
            Wire("Main/FollowFacebook", () => Follow("Facebook", facebookUrl));
            Wire("Main/FollowInstagram", () => Follow("Instagram", instagramUrl));

            Wire("LanguageModal/Close", () => ShowModal(languageModal, false));
            Wire("LanguageModal/Dim", () => ShowModal(languageModal, false), silent: true);
            Wire("LanguageModal/Portuguese", () => SetLanguage("Portuguese"));
            Wire("LanguageModal/English", () => SetLanguage("English"));

            Wire("TermsModal/Close", () => ShowModal(termsModal, false));
            Wire("TermsModal/Dim", () => ShowModal(termsModal, false), silent: true);
            Wire("TermsModal/TermsOfService", () => OpenUrl(termsUrl, "termos de serviço"));
            Wire("TermsModal/PrivacyPolicy", () => OpenUrl(privacyUrl, "política de privacidade"));
            Wire("TermsModal/PrivacyPrefs", () =>
            {
                if (PrivacyPreferencesRequested != null) PrivacyPreferencesRequested();
                else OpenUrl(privacyUrl, "política de privacidade");
            });
            Wire("TermsModal/DeleteAccount", () =>
            {
                if (!string.IsNullOrEmpty(deleteAccountUrl)) OpenUrl(deleteAccountUrl, "exclusão de conta");
                else OpenMail(IsPortuguese ? "Excluir minha conta" : "Delete my account");
            });

            foreach (var name in new[] { "Music", "Sounds", "Hints" })
            {
                var off = Child("Main/" + name + "/Center/Off");
                if (off != null) offBars[name] = off.gameObject;
            }
        }

        void OnEnable()
        {
            if (languageModal != null) languageModal.SetActive(false);
            if (termsModal != null) termsModal.SetActive(false);
            Refresh();
            if (switchKnob != null) switchKnob.anchoredPosition = new Vector2(knobTarget, switchKnob.anchoredPosition.y);
        }

        void Update()
        {
            if (switchKnob != null && !Mathf.Approximately(switchKnob.anchoredPosition.x, knobTarget))
            {
                var p = switchKnob.anchoredPosition;
                p.x = Mathf.MoveTowards(p.x, knobTarget, 900f * Time.unscaledDeltaTime);
                switchKnob.anchoredPosition = p;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (languageModal != null && languageModal.activeSelf) ShowModal(languageModal, false);
                else if (termsModal != null && termsModal.activeSelf) ShowModal(termsModal, false);
                else Close();
            }
        }

        public void Close() => gameObject.SetActive(false);

        // ------------------------------------------------------------------------------------------------ estado

        void Refresh()
        {
            SetOff("Music", !SettingsScreen.IsOn(SettingsScreen.MusicKey));
            SetOff("Sounds", !SettingsScreen.IsOn(SettingsScreen.SoundKey));
            SetOff("Hints", !SettingsScreen.IsOn(SettingsScreen.HintKey));

            var notifications = NotificationsOn;
            var pt = IsPortuguese;
            if (switchRoot != null)
            {
                var half = switchRoot.rect.width * 0.23f;
                knobTarget = notifications ? half : -half;
                var text = Child("Main/Notifications/Switch/Label") as RectTransform;
                if (text != null)
                {
                    var tmp = text.GetComponent<TMP_Text>();
                    if (tmp != null) tmp.text = notifications ? (pt ? "SIM" : "YES") : (pt ? "NÃO" : "NO");
                    text.anchoredPosition = new Vector2(notifications ? -half : half, text.anchoredPosition.y);
                }
                if (switchContainer != null) switchContainer.color = notifications ? Color.white : new Color(.62f, .62f, .62f, 1f);
            }

            foreach (var (path, ptText, enText) in labels)
            {
                var t = Child(path);
                var tmp = t != null ? t.GetComponent<TMP_Text>() : null;
                if (tmp != null) tmp.text = pt ? ptText : enText;
            }

            var portuguese = LocalizedImage.Current == GameLanguage.Portuguese;
            if (portugueseImage != null && selectedOptionSprite != null) portugueseImage.sprite = portuguese ? selectedOptionSprite : otherOptionSprite;
            if (englishImage != null && selectedOptionSprite != null) englishImage.sprite = portuguese ? otherOptionSprite : selectedOptionSprite;
        }

        void SetOff(string name, bool off)
        {
            if (offBars.TryGetValue(name, out var bar) && bar != null) bar.SetActive(off);
        }

        void Toggle(string key, AudioMixer mixer, string parameter)
        {
            var volume = SettingsScreen.IsOn(key) ? -80 : 1;
            if (mixer != null && !string.IsNullOrEmpty(parameter)) mixer.SetFloat(parameter, volume);
            PlayerPrefs.SetInt(key, volume);
            PlayerPrefs.Save();
            Refresh();
        }

        void SetLanguage(string name)
        {
            CrosssceneData.selectedLanguage = name;
            PlayerPrefs.SetString(LanguageKey, name);
            PlayerPrefs.Save();
            LocalizedImage.RefreshAll();
            Refresh();
        }

        void ShowModal(GameObject modal, bool show)
        {
            if (modal == null) return;
            if (show) { Refresh(); modal.transform.SetAsLastSibling(); }
            modal.SetActive(show);
        }

        // ------------------------------------------------------------------------------------------------ ações

        void Follow(string network, string url)
        {
            if (string.IsNullOrEmpty(url)) { Debug.LogWarning("Royal Aves: link do " + network + " ainda não foi preenchido em ConfigurationsScreen."); return; }
            Application.OpenURL(url);
            var key = "Follow." + network;
            if (PlayerPrefs.GetInt(key, 0) == 1 || followReward <= 0 || InitScript.Instance == null) return;
            PlayerPrefs.SetInt(key, 1);
            InitScript.Instance.AddGems(followReward);
        }

        void OpenUrl(string url, string what)
        {
            if (string.IsNullOrEmpty(url)) { Debug.LogWarning("Royal Aves: link de " + what + " ainda não foi preenchido em ConfigurationsScreen."); return; }
            Application.OpenURL(url);
        }

        void OpenMail(string subject)
        {
            if (string.IsNullOrEmpty(supportEmail)) { Debug.LogWarning("Royal Aves: e-mail de suporte ainda não foi preenchido em ConfigurationsScreen."); return; }
            Application.OpenURL("mailto:" + supportEmail + "?subject=" + Uri.EscapeDataString(subject));
        }

        // ------------------------------------------------------------------------------------------------ apoio

        Transform Child(string path) => transform.Find(path);

        void Wire(string path, Action action, bool silent = false)
        {
            var target = Child(path);
            var button = target != null ? target.GetComponent<Button>() : null;
            if (button == null) { Debug.LogWarning("Royal Aves: tela de configurações sem o botão " + path + ". Monte o prefab de novo.", this); return; }
            button.onClick.AddListener(() => { if (!silent) PlayClick(); action(); });
        }

        void PlayClick()
        {
            if (source == null || !SettingsScreen.IsOn(SettingsScreen.SoundKey)) return;
            if (clickClip == null) clickClip = MakeClickClip();
            source.PlayOneShot(clickClip, 0.8f);
        }

        // Um "toc" curto e macio: seno que desce de tom com decaimento rápido e um pouquinho de ruído no ataque.
        static AudioClip MakeClickClip()
        {
            const int rate = 44100;
            var samples = new float[(int)(rate * 0.085f)];
            var random = new System.Random(7);
            var phase = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)rate;
                phase += 2f * Mathf.PI * (260f + 380f * Mathf.Exp(-t * 38f)) / rate;
                var body = Mathf.Sin(phase) * Mathf.Exp(-t * 48f);
                var air = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 420f) * 0.10f;
                var attack = Mathf.Clamp01(i / 24f);
                samples[i] = (body * 0.55f + air) * attack;
            }
            var clip = AudioClip.Create("RoyalUiClick", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// Texto branco com sombra preta pequena (o "underlay" do TextMeshPro), dá o ar de relevo.
        public static void ApplyShadow(TMP_Text text)
        {
            if (text == null) return;
            text.color = Color.white;
            var material = text.fontMaterial;
            if (material == null || !material.HasProperty("_UnderlayColor")) return;
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.9f));
            material.SetFloat("_UnderlayOffsetX", 0.55f);
            material.SetFloat("_UnderlayOffsetY", -0.55f);
            material.SetFloat("_UnderlayDilate", 0.15f);
            material.SetFloat("_UnderlaySoftness", 0.1f);
            text.UpdateMeshPadding();
        }
    }
}
