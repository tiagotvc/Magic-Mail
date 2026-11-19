// Tela inicial do jogo, dentro do CanvasGlobal (objeto "Home"): barra de cima (avatar, moedas, vidas,
// configurações), botão de Play e menu inferior. Substitui o antigo canvas RoyalAvesLobby.
//
// - Aparece só no estado de mapa (GameState.Map) e some quando a fase começa.
// - Play abre o pré-nível do Sweet Sugar na fase atual do jogador (a mais alta liberada no mapa).
// - Configurações abre o menu Settings do CanvasGlobal.
// - Menu inferior: troca a aba selecionada e move o destaque; aba sem tela ligada (fora a inicial) abre a tela
//   provisória "Coming soon" (ComingSoonScreen), com o título da aba. Voltar = aba inicial.
//
// O objeto "Home" fica sempre ativo (MenuReference.HideAll o poupa pelo nome) e liga/desliga o filho "Content".
// Montagem: menu Royal Aves > Tela inicial > Criar no CanvasGlobal. Todos os campos são opcionais.
using System;
using System.Linq;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.MapScripts;
using SweetSugar.Scripts.System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Royal Aves/Home Screen")]
    public class HomeScreen : MonoBehaviour
    {
        public const string ObjectName = "Home";

        [Serializable]
        public class Tab
        {
            public Button button;
            [Tooltip("Ícone da aba (sobe e cresce um pouco quando selecionada).")]
            public RectTransform icon;
            [Tooltip("Opcional: tela que esta aba mostra. Vazio = abre a tela provisória \"Coming soon\" (menos na aba inicial).")]
            public GameObject screen;
            [Tooltip("Título mostrado na tela \"Coming soon\" quando a aba não tem tela. Vazio = deduzido do nome do botão (TabEvents = Eventos).")]
            public string comingSoonTitlePt;
            public string comingSoonTitleEn;
            [Tooltip("Texto embaixo da aba, visível só quando ela está selecionada.")]
            public TMP_Text label;
            [Tooltip("Vazio = deduzido do nome do botão (TabHome = Início / Home).")]
            public string labelPt;
            public string labelEn;
        }

        [Header("Raiz do conteúdo (ligada só no mapa)")]
        [SerializeField] GameObject content;

        [Header("Barra de cima")]
        [SerializeField] TMP_Text coinsText;
        [SerializeField] TMP_Text livesText;
        [SerializeField] Button settingsButton;
        [Tooltip("Botão + das moedas: abre a loja de moedas.")]
        [SerializeField] Button coinsPlusButton;
        [Tooltip("Botão + das vidas: abre a loja de vidas.")]
        [SerializeField] Button livesPlusButton;
        [Tooltip("Moedas passam a \"100k\" a partir deste valor.")]
        [SerializeField] int coinsAbbreviateFrom = 100000;

        [Header("Play")]
        [SerializeField] Button playButton;
        [Tooltip("Opcional: mostra o número da fase atual.")]
        [SerializeField] TMP_Text levelText;
        [Tooltip("Texto do número da fase; {0} = número.")]
        [SerializeField] string levelFormatPt = "Nível {0}", levelFormatEn = "Level {0}";
        [Tooltip("Miolo verde do botão normal. Some nas fases Hard / Super Hard (o sprite delas já vem completo).")]
        [SerializeField] Image playCenter;

        [Header("Play por dificuldade da fase (campo Dificuldade do Level editor)")]
        [Tooltip("Image que troca de sprite conforme a dificuldade. Vazio = a Image do próprio botão de Play.")]
        [SerializeField] Image playFace;
        [SerializeField] Sprite playNormal;
        [SerializeField] Sprite playHard;
        [SerializeField] Sprite playSuperHard;
        [Tooltip("Opcional: texto no rebaixo de cima do botão (\"Difícil\" / \"Super difícil\"). Some na fase normal.")]
        [SerializeField] TMP_Text difficultyLabel;
        [SerializeField] string hardPt = "Difícil", hardEn = "Hard";
        [SerializeField] string superHardPt = "Super difícil", superHardEn = "Super Hard";

        [Header("Menu inferior")]
        [SerializeField] Tab[] tabs = new Tab[0];
        [Tooltip("Destaque da aba selecionada (selected_menu). Fica atrás dos ícones.")]
        [SerializeField] RectTransform indicator;
        [SerializeField] int startTab;
        [Tooltip("Quanto o ícone da aba selecionada sobe, em pixels do canvas.")]
        [SerializeField] float iconLift = 55f;
        [SerializeField] float iconGrow = 1.3f;

        [Header("Abas sem tela (\"Coming soon\")")]
        [Tooltip("Aba sem tela ligada (fora a inicial) abre a tela provisória \"Coming soon\". Desligue para a aba só trocar o visual.")]
        [SerializeField] bool comingSoonForEmptyTabs = true;
        [Tooltip("Opcional: tela provisória já colocada na cena. Vazio = criada sozinha a partir de Resources/UI/ComingSoonScreen.")]
        [SerializeField] ComingSoonScreen comingSoon;

        bool comingSoonWired;
        int selected = -1;
        Vector2[] iconHome;
        Vector3[] iconScale;
        int shownCoins = int.MinValue, shownLives = int.MinValue, shownLevel = int.MinValue;
        GameLanguage shownLanguage;

        [Header("Artes padrão (preenchidas sozinhas no editor)")]
        [SerializeField] Sprite backgroundSprite;
        [SerializeField] Sprite playCenterSprite;

#if UNITY_EDITOR
        void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null || Application.isPlaying) return;
                FillDefaults();
            };
        }

        void FillDefaults()
        {
            var changed = false;
            const string art = "Assets/Royal Postman Art/";
            changed |= Fill(ref backgroundSprite, art + "Screens/Gameplay/Background/background_ready_01.png", false);
            changed |= Fill(ref playCenterSprite, art + "Screens/Modal/button_center.png", false);
            changed |= Fill(ref playNormal, art + "Screens/Modal/button_border.png", true);
            if (changed) UnityEditor.EditorUtility.SetDirty(this);
        }

        static bool Fill(ref Sprite field, string path, bool replaceBlue)
        {
            if (field != null && !(replaceBlue && field.name.StartsWith("btn_blue"))) return false;
            var sprite = System.Linq.Enumerable.FirstOrDefault(System.Linq.Enumerable.OfType<Sprite>(UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path)));
            if (sprite == null || sprite == field) return false;
            field = sprite;
            return true;
        }
#endif

        // Liga sozinho o que a cena já tem (sem precisar rodar menu): texto de vidas do HeartText, botões "+",
        // miolo verde do Play, rótulos das abas e fundo.
        void AutoWire()
        {
#if UNITY_EDITOR
            FillDefaults(); // garante já no Play: moldura laranja no lugar do azul, fundo e miolo
#endif
            var all = GetComponentsInChildren<Transform>(true);
            Transform Find(string n) { foreach (var t in all) if (t.name == n) return t; return null; }

            var heart = Find("HeartText")?.GetComponent<TMP_Text>();
            if (heart != null && heart != livesText)
            {
                if (livesText != null) livesText.gameObject.SetActive(false);
                livesText = heart;
            }
            if (coinsPlusButton == null && coinsText != null) coinsPlusButton = PlusButton(coinsText.transform.parent);
            if (livesPlusButton == null && livesText != null) livesPlusButton = PlusButton(livesText.transform.parent);

            AvoidCoinIcon();
            var font = levelText != null ? levelText.font : coinsText != null ? coinsText.font : null;
            if (playButton != null)
            {
                if (playCenter == null)
                {
                    var center = playButton.transform.Find("Center");
                    if (center == null && playCenterSprite != null)
                    {
                        var go = new GameObject("Center", typeof(RectTransform), typeof(Image)) { layer = playButton.gameObject.layer };
                        var rt = (RectTransform)go.transform;
                        rt.SetParent(playButton.transform, false);
                        rt.anchorMin = new Vector2(0.047f, 0.11f); rt.anchorMax = new Vector2(0.953f, 0.89f);
                        rt.offsetMin = rt.offsetMax = Vector2.zero;
                        rt.SetAsFirstSibling();
                        var image = go.GetComponent<Image>();
                        image.sprite = playCenterSprite; image.raycastTarget = false;
                        center = rt;
                    }
                    if (center != null) playCenter = center.GetComponent<Image>();
                }
                // Só a moldura do botão e o miolo desenham: qualquer outra Image dentro do botão (sobra de teste) some.
                foreach (var image in playButton.GetComponentsInChildren<Image>(true))
                    if (image.gameObject != playButton.gameObject && image != playCenter) image.enabled = false;
                // Só o número da fase e o texto de dificuldade ficam no botão (some texto provisório, como "Level 100").
                foreach (var text in playButton.GetComponentsInChildren<TMP_Text>(true))
                    if (text != levelText && text != difficultyLabel) text.gameObject.SetActive(false);
                if (levelText != null)
                {
                    levelText.transform.SetAsLastSibling();
                    levelText.alignment = TextAlignmentOptions.Center;
                    var lr = levelText.rectTransform;
                    lr.anchorMin = lr.anchorMax = lr.pivot = new Vector2(0.5f, 0.5f);
                    lr.anchoredPosition = new Vector2(0f, lr.anchoredPosition.y);
                }
                FitCenter();
            }

            for (var i = 0; i < tabs.Length; i++)
            {
                var tab = tabs[i];
                if (tab.label != null || tab.button == null || tab.icon == null) continue;
                var existing = tab.button.transform.Find("Label");
                if (existing != null) { tab.label = existing.GetComponent<TMP_Text>(); if (tab.label != null) continue; }
                var go = new GameObject("Label", typeof(RectTransform)) { layer = tab.button.gameObject.layer };
                var rt = (RectTransform)go.transform;
                rt.SetParent(tab.button.transform, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(300f, 70f);
                rt.anchoredPosition = new Vector2(0f, 45f);
                var label = go.AddComponent<TextMeshProUGUI>();
                if (font != null) label.font = font;
                label.color = Color.white; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.enableAutoSizing = true; label.fontSizeMin = 18; label.fontSizeMax = 52;
                tab.label = label;
            }

            if (content != null && backgroundSprite != null && content.transform.Find("Background") == null)
            {
                var go = new GameObject("Background", typeof(RectTransform), typeof(Image)) { layer = content.layer };
                var rt = (RectTransform)go.transform;
                rt.SetParent(content.transform, false);
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
                rt.SetAsFirstSibling();
                var image = go.GetComponent<Image>();
                image.sprite = backgroundSprite; image.raycastTarget = false;
                image.preserveAspect = false;
                var fitter = go.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = backgroundSprite.rect.width / backgroundSprite.rect.height;
            }
        }

        // Miolo verde centralizado e dentro da moldura: a moldura é desenhada com a proporção do sprite
        // (preserveAspect), então o miolo usa o tamanho REAL desenhado, não o retângulo do botão.
        // Proporção da arte: moldura 604x274, miolo 547x214.
        // O texto das moedas não pode ficar atrás da moeda: margem esquerda = parte do texto coberta pelo CoinIcon.
        void AvoidCoinIcon()
        {
            if (coinsText == null) return;
            var icon = coinsText.transform.parent != null ? coinsText.transform.parent.Find("CoinIcon") as RectTransform : null;
            if (icon == null) return;
            var t = coinsText.rectTransform;
            var iconRight = icon.anchoredPosition.x + icon.rect.width * (1f - icon.pivot.x);
            var textLeft = t.anchoredPosition.x - t.rect.width * t.pivot.x;
            var cover = Mathf.Max(0f, iconRight - textLeft + 10f);
            coinsText.alignment = TextAlignmentOptions.Center;
            var m = coinsText.margin; m.x = cover; coinsText.margin = m;
        }

        void FitCenter()
        {
            var face = playFace != null ? playFace : playButton != null ? playButton.GetComponent<Image>() : null;
            if (playCenter == null || face == null || playNormal == null) return;
            var rect = face.rectTransform.rect;
            var aspect = playNormal.rect.width / playNormal.rect.height;
            var w = rect.width; var h = rect.height;
            // proporção do sprite
            {
                if (w / h > aspect) w = h * aspect; else h = w / aspect;
            }
            var rt = playCenter.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w * 547f / 604f, h * 214f / 274f);
            rt.anchoredPosition = Vector2.zero;
            playCenter.preserveAspect = false;
            face.preserveAspect = true;
        }

        static Button PlusButton(Transform group)
        {
            var plus = group != null ? group.Find("Plus") : null;
            if (plus == null) return null;
            var image = plus.GetComponent<Image>();
            if (image != null) image.raycastTarget = true;
            var button = plus.GetComponent<Button>();
            if (button == null)
            {
                button = plus.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = image;
            }
            if (plus.GetComponent<ButtonPopPress>() == null) plus.gameObject.AddComponent<ButtonPopPress>();
            return button;
        }

        void Awake()
        {
            AutoWire();
            iconHome = new Vector2[tabs.Length];
            iconScale = new Vector3[tabs.Length];
            for (var i = 0; i < tabs.Length; i++)
            {
                var index = i;
                if (tabs[i].icon != null) { iconHome[i] = tabs[i].icon.anchoredPosition; iconScale[i] = tabs[i].icon.localScale; }
                if (tabs[i].button != null) tabs[i].button.onClick.AddListener(() => Select(index));
            }
            if (playButton != null) playButton.onClick.AddListener(Play);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (coinsPlusButton != null) coinsPlusButton.onClick.AddListener(OpenCoinShop);
            if (livesPlusButton != null) livesPlusButton.onClick.AddListener(OpenLifeShop);
        }

        static void ClickSound()
        {
            if (SoundBase.Instance != null) SoundBase.Instance.PlayOneShot(SoundBase.Instance.click);
        }

        public void OpenCoinShop()
        {
            if (MenuReference.THIS == null || MenuReference.THIS.GemsShop == null) return;
            ClickSound();
            MenuReference.THIS.GemsShop.SetActive(true);
        }

        public void OpenLifeShop()
        {
            if (MenuReference.THIS == null || MenuReference.THIS.LiveShop == null) return;
            ClickSound();
            // Abre sempre (mesmo com as vidas cheias, para o jogador ver a loja).
            MenuReference.THIS.LiveShop.SetActive(true);
        }

        void Start()
        {
            if (tabs.Length > 0) Select(Mathf.Clamp(startTab, 0, tabs.Length - 1));
            Refresh();
        }

        void Update() => Refresh();

        void Refresh()
        {
            var onMap = LevelManager.THIS != null && LevelManager.GetGameStatus() == GameState.Map;
            if (content != null && content.activeSelf != onMap) content.SetActive(onMap);
            if (!onMap) return;

            if (coinsText != null && InitScript.Gems != shownCoins)
            {
                shownCoins = InitScript.Gems;
                coinsText.text = CoinFormat.Short(shownCoins, coinsAbbreviateFrom);
            }
            if (livesText != null && InitScript.lifes != shownLives)
            {
                shownLives = InitScript.lifes;
                livesText.text = shownLives.ToString();
            }
            // Vidas cheias: o "+" some (não há o que comprar).
            if (livesPlusButton != null && InitScript.Instance != null)
            {
                var notFull = InitScript.lifes < InitScript.Instance.CapOfLife;
                if (livesPlusButton.gameObject.activeSelf != notFull) livesPlusButton.gameObject.SetActive(notFull);
            }
            var level = CurrentLevel();
            var language = LocalizedImage.Current;
            if (level == shownLevel && language == shownLanguage) return;
            shownLevel = level;
            shownLanguage = language;
            if (levelText != null)
                levelText.text = string.Format(language == GameLanguage.Portuguese ? levelFormatPt : levelFormatEn, level);
            ShowDifficulty(DifficultyOf(level), language);
            RefreshTabLabels();
        }

        void RefreshTabLabels()
        {
            var pt = LocalizedImage.Current == GameLanguage.Portuguese;
            for (var i = 0; i < tabs.Length; i++)
            {
                var tab = tabs[i];
                if (tab.label == null) continue;
                TabLabel(tab, out var labelPt, out var labelEn);
                tab.label.text = pt ? labelPt : labelEn;
                tab.label.gameObject.SetActive(i == selected);
            }
        }

        static void TabLabel(Tab tab, out string pt, out string en)
        {
            pt = tab.labelPt; en = tab.labelEn;
            if (!string.IsNullOrEmpty(pt) || !string.IsNullOrEmpty(en))
            {
                if (string.IsNullOrEmpty(pt)) pt = en;
                if (string.IsNullOrEmpty(en)) en = pt;
                return;
            }
            var name = tab.button != null ? tab.button.name : "";
            if (name.StartsWith("Tab", StringComparison.Ordinal)) name = name.Substring(3);
            switch (name)
            {
                case "Events": pt = "Eventos"; en = "Events"; break;
                case "Collection": pt = "Coleção"; en = "Collection"; break;
                case "Home": pt = "Início"; en = "Home"; break;
                case "Ranking": pt = "Ranking"; en = "Ranking"; break;
                default: pt = en = ""; break;
            }
        }

        void ShowDifficulty(LevelDifficulty difficulty, GameLanguage language)
        {
            var face = playFace != null ? playFace : playButton != null ? playButton.GetComponent<Image>() : null;
            var sprite = difficulty == LevelDifficulty.SuperHard ? playSuperHard : difficulty == LevelDifficulty.Hard ? playHard : playNormal;
            if (face != null && sprite != null) face.sprite = sprite;
            if (playCenter != null) { playCenter.enabled = difficulty == LevelDifficulty.Normal; FitCenter(); }
            if (difficultyLabel == null) return;
            var pt = language == GameLanguage.Portuguese;
            difficultyLabel.gameObject.SetActive(difficulty != LevelDifficulty.Normal);
            if (difficulty == LevelDifficulty.Hard) difficultyLabel.text = pt ? hardPt : hardEn;
            else if (difficulty == LevelDifficulty.SuperHard) difficultyLabel.text = pt ? superHardPt : superHardEn;
        }

        /// Dificuldade gravada na fase (Resources/Levels/Level_N). Fase sem arquivo vale Normal.
        public static LevelDifficulty DifficultyOf(int level)
        {
            var container = Resources.Load<LevelContainer>("Levels/Level_" + level);
            return container != null && container.levelData != null ? container.levelData.difficulty : LevelDifficulty.Normal;
        }

        /// Fase que o Play abre: a mais alta liberada no mapa do Sweet Sugar.
        public static int CurrentLevel()
        {
            try
            {
                var levels = LevelsMap.GetMapLevels();
                if (levels != null && levels.Any(l => !l.IsLocked)) return Mathf.Max(1, LevelsMap.GetLastestReachedLevel());
            }
            catch (Exception) { /* mapa ainda não montado: cai no progresso salvo */ }
            return Mathf.Max(1, MetaProgress.NextLevel);
        }

        public void Play()
        {
            if (LevelManager.THIS == null || LevelManager.GetGameStatus() != GameState.Map) return;
            try { InitScript.OpenMenuPlay(CurrentLevel()); }
            catch (Exception e) { Debug.LogError("Royal Aves: a tela inicial não conseguiu abrir o pré-nível: " + e, this); }
        }

        public void OpenSettings()
        {
            if (ConfigScreen.Open()) return; // tela nova (Resources/UI/ConfigurationsScreen); sem o prefab, cai na antiga
            var settings = MenuReference.THIS != null ? MenuReference.THIS.Settings : null;
            if (settings != null) settings.SetActive(true);
        }

        public void Select(int index)
        {
            if (index < 0 || index >= tabs.Length) return;
            selected = index;
            for (var i = 0; i < tabs.Length; i++)
            {
                var on = i == index;
                var tab = tabs[i];
                if (tab.icon != null)
                {
                    tab.icon.anchoredPosition = iconHome[i] + (on ? new Vector2(0f, iconLift) : Vector2.zero);
                    tab.icon.localScale = iconScale[i] * (on ? iconGrow : 1f);
                }
                if (tab.screen != null) tab.screen.SetActive(on);
            }
            var target = tabs[index].button != null ? (RectTransform)tabs[index].button.transform : null;
            if (indicator != null && target != null)
            {
                indicator.gameObject.SetActive(true);
                var p = indicator.position;
                indicator.position = new Vector3(target.position.x, p.y, p.z);
            }
            RefreshTabLabels();
            ShowComingSoon(index);
        }

        // ---------- Abas sem tela: tela provisória "Coming soon" ----------

        int HomeTab => Mathf.Clamp(startTab, 0, Mathf.Max(0, tabs.Length - 1));

        void ShowComingSoon(int index)
        {
            var tab = tabs[index];
            if (!comingSoonForEmptyTabs || tab.screen != null || index == HomeTab)
            {
                if (comingSoon != null) comingSoon.Hide();
                return;
            }
            try
            {
                if (comingSoon == null)
                    comingSoon = ComingSoonScreen.Create(content != null ? content.transform : transform, levelText != null ? levelText.font : null);
                if (!comingSoonWired)
                {
                    comingSoonWired = true;
                    comingSoon.Closed += () => Select(HomeTab); // Voltar = aba inicial
                }
                // A tela cobre o mapa e o Play; a barra de cima e o menu inferior continuam por cima dela.
                if (content != null && comingSoon.transform.parent == content.transform)
                {
                    comingSoon.transform.SetAsLastSibling();
                    BringToFront(coinsText != null ? coinsText.transform : null);
                    BringToFront(tab.button != null ? tab.button.transform : null);
                }
                TabTitle(tab, out var pt, out var en);
                comingSoon.Show(pt, en);
            }
            catch (Exception e) { Debug.LogError("Royal Aves: a tela inicial não conseguiu abrir a tela \"Coming soon\": " + e, this); }
        }

        /// Põe por último (desenhado por cima) o filho direto de Content que contém "inner".
        void BringToFront(Transform inner)
        {
            if (inner == null || content == null) return;
            var root = content.transform;
            while (inner.parent != null && inner.parent != root) inner = inner.parent;
            if (inner.parent == root) inner.SetAsLastSibling();
        }

        /// Título da aba para a tela provisória: o do Inspector ou, vazio, deduzido do nome do botão.
        static void TabTitle(Tab tab, out string pt, out string en)
        {
            pt = tab.comingSoonTitlePt; en = tab.comingSoonTitleEn;
            if (!string.IsNullOrEmpty(pt) || !string.IsNullOrEmpty(en))
            {
                if (string.IsNullOrEmpty(pt)) pt = en;
                if (string.IsNullOrEmpty(en)) en = pt;
                return;
            }
            var name = tab.button != null ? tab.button.name : "";
            if (name.StartsWith("Tab", StringComparison.Ordinal)) name = name.Substring(3);
            switch (name)
            {
                case "Events": pt = "Eventos"; en = "Events"; break;
                case "Collection": pt = "Coleção"; en = "Collection"; break;
                case "Ranking": pt = "Ranking"; en = "Ranking"; break;
                case "Shop": pt = "Loja"; en = "Shop"; break;
                case "Team": case "Teams": pt = "Equipes"; en = "Teams"; break;
                case "Empty": case "": pt = en = ""; break; // aba ainda sem nome: só "Coming soon"
                default: pt = en = name; break;
            }
        }
    }
}
