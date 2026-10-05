// Lobby of the Correio Mágico agency, laid out like the web prototype (royal-aves/src): HUD with avatar, coins, lives,
// stars and settings; the room of the current area with its restored upgrades; "Nível N" and the area button; the bottom
// navigation; and one shared window for the upgrades, the area chest, its reward, the next area and the settings.
// It replaces the Sweet Sugar path map: LevelManager's Map state shows it and "Nível N" opens Sweet Sugar's pre-level
// popup, so the levels themselves are still Sweet Sugar's.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RoyalAves.Characters;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.System;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    [RequireComponent(typeof(Canvas))]
    public class LobbyController : MonoBehaviour
    {
        public static LobbyController Instance { get; private set; }

        [Header("Sala")]
        [SerializeField] GameObject content;
        [SerializeField] Image background;
        [SerializeField] AspectRatioFitter roomFitter;
        [SerializeField] RectTransform layersRoot;

        [Header("HUD")]
        [SerializeField] Button avatarButton;
        [SerializeField] Button coinsButton;
        [SerializeField] TextMeshProUGUI coinsText;
        [SerializeField] Button livesButton;
        [SerializeField] TextMeshProUGUI livesText;
        [SerializeField] TextMeshProUGUI livesLabel;
        [SerializeField] TextMeshProUGUI starsText;
        [SerializeField] Button settingsButton;

        [Header("Ações")]
        [SerializeField] Button playButton;
        [SerializeField] TextMeshProUGUI playText;
        [SerializeField] Button areaButton;
        [SerializeField] TextMeshProUGUI areaLabel;
        [SerializeField] TextMeshProUGUI areaProgress;

        [Header("Menu inferior: Eventos, Ranking, Agência, Equipes, Coleção")]
        [SerializeField] Button[] navButtons;

        [Header("Janela")]
        [SerializeField] GameObject modal;
        [SerializeField] RectTransform modalWindow;
        [SerializeField] GameObject modalArt;
        [SerializeField] TextMeshProUGUI modalTag;
        [SerializeField] TextMeshProUGUI modalTitle;
        [SerializeField] TextMeshProUGUI modalCopy;
        [SerializeField] Button modalPrimary;
        [SerializeField] TextMeshProUGUI modalPrimaryText;
        [SerializeField] Button modalSecondary;
        [SerializeField] TextMeshProUGUI modalSecondaryText;
        [SerializeField] Button modalClose;

        [Header("Conteúdos da janela")]
        [SerializeField] GameObject decorationsExtra;
        [SerializeField] RectTransform meterFill;
        [SerializeField] TextMeshProUGUI meterText;
        [SerializeField] LobbyTaskRow[] taskRows;
        [SerializeField] GameObject chestExtra;
        [SerializeField] Button chestTapButton;
        [SerializeField] GameObject rewardExtra;
        [SerializeField] TextMeshProUGUI rewardText;
        [SerializeField] GameObject settingsExtra;
        [SerializeField] Button soundButton;
        [SerializeField] TextMeshProUGUI soundText;
        [SerializeField] Button musicButton;
        [SerializeField] TextMeshProUGUI musicText;
        [SerializeField] GameObject lockedAreaExtra;

        [Header("Restauração")]
        [SerializeField] GameObject restoreEffect;
        [SerializeField] CanvasGroup restoreGlow;
        [SerializeField] RectTransform restoreToken;
        [SerializeField] Image restoreIcon;
        [SerializeField] RectTransform restoreLabelBox;
        [SerializeField] TextMeshProUGUI restoreLabel;
        [SerializeField] AudioClip restoreSound;
        [SerializeField, Min(0.1f)] float restoreSeconds = 1.25f;

        [Header("Moedas voando até o contador (depois de vencer um nível)")]
        [Tooltip("Quantas moedas voam, no máximo.")]
        [SerializeField, Range(1, 20)] int flyingCoins = 10;
        [Tooltip("Tempo de voo de cada moeda, em segundos.")]
        [SerializeField, Min(0.2f)] float coinFlightSeconds = 0.75f;

        [Header("Fundo do jogo (a sala desfocada)")]
        [Tooltip("Resolução da captura da sala em relação à tela (menor = mais desfocado).")]
        [SerializeField, Range(0.05f, 0.5f)] float backdropScale = 0.25f;
        [Tooltip("Quantas vezes a captura é reduzida pela metade (mais = mais desfocado).")]
        [SerializeField, Range(0, 4)] int backdropBlurSteps = 1;

        [Header("Diagnóstico")]
        [Tooltip("Escreve no Console cada toque (o que está sob o dedo) e cada mudança de estado do jogo.")]
        [SerializeField] bool logDiagnostics = true;

        static readonly string[] NavTitles = { "Eventos", "Ranking", "Agência", "Equipes", "Coleção" };
        const int HomeTab = 2;

        // The prototype's bottom-menu screens, in the order of the navigation buttons (Agência is the lobby itself).
        static readonly LobbyMenus.Route[] TabRoutes =
            { LobbyMenus.Route.Events, LobbyMenus.Route.Records, LobbyMenus.Route.Home, LobbyMenus.Route.Teams, LobbyMenus.Route.Collection };

        // Dark track and green fill inside the area button's progress pill (made by "Arrumar botão da área", or here).
        const string AreaTrackName = "Trilho";
        const string AreaFillName = "Preenchimento";
        const float AreaBarInset = 6; // inside the pill's gold border
        RectTransform areaFill;

        [Tooltip("Partícula dourada usada no efeito de \"construção\" dos estágios de área (Content/areas/<id>/Estagio_*).")]
        [SerializeField] Sprite stageSparkle;
        [Tooltip("Textura de fogo (AllIn1SpriteShader/Textures/fire2.png) para a borda do \"dissolve reverso\" dos estágios.")]
        [SerializeField] Texture2D stageBurnTexture;

        LobbyMenus menus;
        StarsInfoPopup starsPopup;
        SettingsScreen settingsScreen;
        LobbyFeaturesConfig features;
        Vector2? managerAvatarPosition, managerAvatarSize;

        readonly List<Image> layers = new List<Image>();
        GameObject[] extras;
        AreaProgressionConfig config;
        int builtAreaIndex = -1;
        bool restoring;
        Action primaryAction;
        Action secondaryAction;
        GameObject sweetSugarGear;
        GameState? lastStatus;

        // Coins won in the level just played. The scene reloads between the level and the lobby, so the amount waits
        // here (static) until the lobby opens and flies them into the counter.
        static int pendingCoins;
        bool coinsFlying;

        void Awake()
        {
            Instance = this;
            config = AreaProgressionConfig.Instance;
            var canvas = GetComponent<Canvas>();
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
                canvas.worldCamera = Camera.main;
            extras = new[] { decorationsExtra, chestExtra, rewardExtra, settingsExtra, lockedAreaExtra };

            playButton.onClick.AddListener(Play);
            areaButton.onClick.AddListener(OpenArea);
            if (settingsButton == null) settingsButton = GearFromScene();
            if (settingsButton != null)
                settingsButton.onClick.AddListener(ToggleSettings);
            coinsButton.onClick.AddListener(OpenCoinShop);
            var coinIcon = SiblingButton(coinsButton, "Coin");
            if (coinIcon != null) coinIcon.onClick.AddListener(OpenCoinShop);
            livesButton.onClick.AddListener(() =>
            {
                if (InitScript.lifes < InitScript.Instance.CapOfLife)
                    OpenSweetSugarMenu(MenuReference.THIS != null ? MenuReference.THIS.LiveShop : null);
            });
            avatarButton.onClick.AddListener(OpenProfile);
            for (var i = 0; i < navButtons.Length; i++)
            {
                var tab = i;
                navButtons[i].onClick.AddListener(() => OpenTab(tab));
            }
            modalPrimary.onClick.AddListener(() => CloseModalThen(primaryAction));
            modalSecondary.onClick.AddListener(() => CloseModalThen(secondaryAction));
            modalClose.onClick.AddListener(() => CloseModalThen(modalSecondary.gameObject.activeSelf ? secondaryAction : null));
            chestTapButton.onClick.AddListener(() => CloseModalThen(primaryAction));
            soundButton.onClick.AddListener(() => Toggle("Sound"));
            musicButton.onClick.AddListener(() => Toggle("Music"));
            for (var i = 0; i < taskRows.Length; i++)
            {
                var row = i;
                taskRows[i].Button.onClick.AddListener(() => BuyShownTask(row));
            }

            // Bottom-menu screens (Eventos, Recordes, Coleção, Equipes, Inventário, Perfil), built over the lobby.
            features = LobbyFeaturesConfig.Instance;
            if (features != null && navButtons.Length > 0 && navButtons[0] != null)
            {
                menus = LobbyMenus.Create((RectTransform)content.transform, (RectTransform)navButtons[0].transform.parent, features,
                    PlayLevelFromMenu, (title, copy) => ShowModal(null, title, copy, primary: "Voltar"), Refresh);
            }
            else if (features == null)
                Debug.LogWarning("Royal Aves: falta Resources/" + LobbyFeaturesConfig.ResourcePath + " (menu Royal Aves > Criar telas do menu inferior).");

            // Stars counter (its frame and the star beside it) opens "Ganhe Estrelas".
            var starsButton = starsText.GetComponentInParent<Button>(true);
            if (features != null && starsButton != null)
            {
                starsPopup = StarsInfoPopup.Create((RectTransform)content.transform, features, PlayClick, Play);
                starsButton.onClick.AddListener(starsPopup.Open);
                var starIcon = SiblingButton(starsButton, "Star");
                if (starIcon != null) starIcon.onClick.AddListener(starsPopup.Open);
            }

            // Settings as a screen of its own, in place of Sweet Sugar's little window. It lives inside the prefab
            // (menu Royal Aves > Criar tela de configurações), so here it is only found and given the tap sound.
            settingsScreen = content.GetComponentInChildren<SettingsScreen>(true);
            if (settingsScreen != null) settingsScreen.OnTap = PlayClick;

            modal.SetActive(false);
            restoreEffect.SetActive(false);
            content.SetActive(false); // shown when LevelManager enters the Map state
        }

        void OpenCoinShop() => OpenSweetSugarMenu(MenuReference.THIS != null ? MenuReference.THIS.GemsShop : null);

        // A picture drawn beside a HUD button (the coin beside the coins frame) becomes a button with the same click look.
        static Button SiblingButton(Button button, string pictureName)
        {
            var picture = button.transform.parent.Find(pictureName)?.GetComponent<Image>();
            if (picture == null) return null;
            picture.raycastTarget = true;
            var copy = picture.GetComponent<Button>();
            if (copy == null) copy = picture.gameObject.AddComponent<Button>();
            copy.transition = button.transition;
            copy.colors = button.colors;
            copy.spriteState = button.spriteState;
            copy.targetGraphic = picture;
            return copy;
        }

        // game.unity swaps the prefab's Gear for a plain picture of the gear (config-tela-inicial); it becomes the button.
        Button GearFromScene()
        {
            var picture = GetComponentsInChildren<Image>(true).FirstOrDefault(i => i.name.StartsWith("config-tela-inicial"));
            if (picture == null) return null;
            picture.raycastTarget = true;
            var button = picture.GetComponent<Button>();
            if (button == null) button = picture.gameObject.AddComponent<Button>();
            button.targetGraphic = picture;
            return button;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            LevelManager.OnMapState += Show;
            LevelManager.OnEnterGame += Hide;
            MetaProgress.Changed += Refresh;
        }

        void OnDisable()
        {
            LevelManager.OnMapState -= Show;
            LevelManager.OnEnterGame -= Hide;
            MetaProgress.Changed -= Refresh;
        }

        void Update()
        {
            if (content.activeSelf) RefreshLives();
            if (!logDiagnostics) return;
            GameState? status = LevelManager.THIS != null ? LevelManager.THIS.gameStatus : (GameState?)null;
            if (status != lastStatus)
            {
                Log($"estado do jogo: {(lastStatus.HasValue ? lastStatus.ToString() : "-")} -> {status}");
                lastStatus = status;
            }
            if (Input.GetMouseButtonDown(0)) LogPointerTargets(Input.mousePosition);
        }

        // Sweet Sugar's popups (pre-level, shops, settings) live on CanvasGlobal. The lobby must draw and take taps behind
        // them: same camera and sorting layer, a lower order and a farther plane. Done at runtime because a prefab canvas
        // has no camera, and without one Unity neither keeps its sorting layer nor draws it behind the popups.
        void PlaceBehindSweetSugarPopups()
        {
            var canvas = GetComponent<Canvas>();
            var popups = FindPopupsCanvas();
            // Without a camera a Screen Space - Camera canvas falls back to Overlay, which draws after every camera: the
            // lobby would cover Sweet Sugar's popups while still losing to them in the raycast (they sit on a higher
            // sorting layer), so the pre-level window would take the taps without ever being seen.
            var camera = popups != null && popups.worldCamera != null ? popups.worldCamera : AnyCamera();
            if (camera != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
            }
            if (popups != null)
            {
                canvas.sortingLayerID = popups.sortingLayerID;
                canvas.sortingOrder = popups.sortingOrder - 2;
                canvas.planeDistance = popups.planeDistance + 50;
            }
            Log($"canvas do lobby: {canvas.renderMode}, câmera {NameOf(canvas.worldCamera)}, camada {canvas.sortingLayerName} {canvas.sortingOrder}, distância {canvas.planeDistance}" +
                (popups == null ? "; CanvasGlobal não encontrado"
                    : $"; popups: câmera {NameOf(popups.worldCamera)}, camada {popups.sortingLayerName} {popups.sortingOrder}, distância {popups.planeDistance}"));
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                Debug.LogWarning("Royal Aves: o lobby ficou sem câmera (Screen Space - Overlay) e vai cobrir os popups do " +
                                 "Sweet Sugar. Confira se a cena tem uma câmera com a tag MainCamera e o CanvasGlobal.", this);
        }

        // MenuReference may not be awake yet the first time the lobby is shown, so CanvasGlobal is also looked up by name.
        static Canvas FindPopupsCanvas()
        {
            var menus = MenuReference.THIS != null ? MenuReference.THIS : FindAnyObjectByType<MenuReference>(FindObjectsInactive.Include);
            var parent = menus != null ? menus.GetComponentInParent<Canvas>() : null;
            if (parent != null) return parent.rootCanvas;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (canvas.name == "CanvasGlobal") return canvas.rootCanvas;
            return null;
        }

        // Camera.main is null while no enabled camera carries the MainCamera tag; any camera that draws to the screen serves.
        static Camera AnyCamera()
        {
            if (Camera.main != null) return Camera.main;
            foreach (var camera in Camera.allCameras)
                if (camera.isActiveAndEnabled && camera.targetTexture == null) return camera;
            return null;
        }

        static string NameOf(UnityEngine.Object target) => target != null ? target.name : "nenhuma";

        void Show()
        {
            PlaceBehindSweetSugarPopups();
            if (config == null || config.areas.Count == 0)
            {
                Debug.LogError($"Royal Aves: falta Resources/{AreaProgressionConfig.ResourcePath} com pelo menos uma área " +
                               "(menu Royal Aves > Criar lobby da agência).");
                return;
            }
            content.SetActive(true);
            // A restoration cut short by the level (or by a reload) would leave this full-screen effect on, swallowing
            // every tap in the lobby: the lobby always opens with it off.
            restoring = false;
            restoreEffect.SetActive(false);
            // The lobby has its own settings button; Sweet Sugar's map gear would sit over "Nível N".
            if (MenuReference.THIS != null)
            {
                var gear = MenuReference.THIS.transform.Find("SettingsButton");
                if (gear != null && gear.gameObject.activeSelf)
                {
                    sweetSugarGear = gear.gameObject;
                    sweetSugarGear.SetActive(false);
                }
            }
            Refresh();
            if (pendingCoins > 0)
            {
                StartCoroutine(FlyCoins(pendingCoins));
                pendingCoins = 0;
            }
            var module = EventSystem.current != null && EventSystem.current.currentInputModule != null
                ? EventSystem.current.currentInputModule.GetType().Name : "nenhum";
            Log($"lobby aberto; próximo nível {MetaProgress.NextLevel}, vidas {InitScript.lifes}, módulo de entrada {module}");
            if (MetaProgress.IsAreaComplete(config) && !MetaProgress.ChestClaimed) OpenAreaComplete();
        }

        void Hide()
        {
            // The lobby canvas sits on the UI sorting layer, in front of the board: it is switched off FIRST, so that a
            // failure in anything below can no longer leave it covering the level. RenderRoom turns the room back on by
            // itself while it captures, so the blurred backdrop still works from here.
            content.SetActive(false);
            modal.SetActive(false);
            if (sweetSugarGear != null) sweetSugarGear.SetActive(true);
            sweetSugarGear = null;
            Log("lobby escondido; entrando no nível");
            try
            {
                if (menus != null) menus.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning("Royal Aves: falha ao fechar as telas do menu inferior (" + e.Message + ").", this);
            }
            ShowRoomBehindLevel();
            HideScoreUi();
        }

        // Correio Mágico levels are worth one star, not a 1-3 star score: the in-game score and its star bar are hidden.
        static void HideScoreUi()
        {
            if (LevelManager.THIS == null || LevelManager.THIS.Level == null) return;
            foreach (var t in LevelManager.THIS.Level.GetComponentsInChildren<Transform>(true))
                if (t.name == "ProgressBar" || t.name == "Score")
                    t.gameObject.SetActive(false);
        }

        // The level's background is this room, blurred (AreaBackdrop): drawn once as the level starts.
        void ShowRoomBehindLevel()
        {
            if (config == null || config.areas.Count == 0 || LevelManager.THIS == null || LevelManager.THIS.Level == null) return;
            var canvasBack = LevelManager.THIS.Level.transform.Find("CanvasBack");
            if (canvasBack == null)
            {
                Log("fundo do jogo: CanvasBack não encontrado; fica o fundo do Sweet Sugar");
                return;
            }
            try
            {
                AreaBackdrop.Show(canvasBack, RenderRoom());
                Log("fundo do jogo: sala desfocada");
            }
            catch (Exception e)
            {
                Debug.LogWarning("Royal Aves: não consegui desenhar a sala no fundo do jogo (" + e.Message + "); fica o fundo do Sweet Sugar.");
            }
        }

        // Only the room (background + restored upgrades) is shown, and a camera far from the scene draws this canvas into
        // a small texture; Blur then halves it and scales it back up, which softens it.
        RenderTexture RenderRoom()
        {
            var canvas = GetComponent<Canvas>();
            var wasShown = content.activeSelf;
            content.SetActive(true);
            var area = MetaProgress.CurrentArea(config);
            if (builtAreaIndex != MetaProgress.AreaIndex) BuildArea(area);
            var done = Mathf.Min(MetaProgress.TasksDone, area.tasks.Count);
            for (var i = 0; i < layers.Count; i++)
                layers[i].gameObject.SetActive(i < done && layers[i].sprite != null);
            var room = background.transform.parent;
            var hidden = new List<GameObject>();
            foreach (Transform child in content.transform)
                if (child != room && child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                    hidden.Add(child.gameObject);
                }

            var capture = RenderTexture.GetTemporary(Mathf.Max(32, Mathf.RoundToInt(Screen.width * backdropScale)),
                Mathf.Max(32, Mathf.RoundToInt(Screen.height * backdropScale)), 24);
            var cameraObject = new GameObject("Captura da sala");
            cameraObject.transform.position = new Vector3(0, 10000, 0);
            var captureCamera = cameraObject.AddComponent<Camera>();
            captureCamera.enabled = false;
            captureCamera.orthographic = true;
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = Color.black;
            captureCamera.cullingMask = 1 << gameObject.layer;
            var oldMode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            var oldDistance = canvas.planeDistance;
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = captureCamera;
                canvas.planeDistance = 10;
                Canvas.ForceUpdateCanvases();
                var request = new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = capture };
                if (UnityEngine.Rendering.RenderPipeline.SupportsRenderRequest(captureCamera, request))
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(captureCamera, request);
                else
                {
                    captureCamera.targetTexture = capture;
                    captureCamera.Render();
                    captureCamera.targetTexture = null;
                }
            }
            finally
            {
                canvas.renderMode = oldMode;
                canvas.worldCamera = oldCamera;
                canvas.planeDistance = oldDistance;
                foreach (var go in hidden) go.SetActive(true);
                content.SetActive(wasShown);
                Destroy(cameraObject);
            }
            return Blur(capture);
        }

        RenderTexture Blur(RenderTexture source)
        {
            var current = source;
            for (var i = 0; i < backdropBlurSteps; i++)
            {
                var smaller = RenderTexture.GetTemporary(Mathf.Max(8, current.width / 2), Mathf.Max(8, current.height / 2), 0);
                smaller.filterMode = FilterMode.Bilinear;
                Graphics.Blit(current, smaller);
                RenderTexture.ReleaseTemporary(current);
                current = smaller;
            }
            // Back up once: the bilinear filter softens the small picture instead of showing blocks.
            var result = new RenderTexture(current.width * 2, current.height * 2, 0) { name = "Sala desfocada", filterMode = FilterMode.Bilinear };
            Graphics.Blit(current, result);
            RenderTexture.ReleaseTemporary(current);
            return result;
        }

        [Header("Vitória")]
        [SerializeField] VictoryScreen victory;
        int victoryLevel;

        /// Called by LevelManager when a level is won: gives the star (first win only) and shows the envelope screen.
        public void ShowVictory(int level)
        {
            victoryLevel = level;
            var firstWin = config != null && MetaProgress.RecordWin(level, config) > 0;
            // Coins follow the level's points (RoyalAvesProgression > Moedas); a replay can give a smaller share.
            var coins = config == null ? 0
                : Mathf.RoundToInt(LevelManager.Score * config.coinsPerPoint * (firstWin ? 1f : config.replayCoinShare));
            if (coins > 0 && InitScript.Instance != null)
            {
                InitScript.Instance.AddGems(coins);
                pendingCoins += coins;
            }
            MetaProgress.RecordResult(level, LevelManager.Score); // Recordes screen
            Log($"nível {level} vencido; estrela nova: {firstWin}; pontos {LevelManager.Score}; moedas +{coins}");
            // Above the game board and its HUD, on the same camera.
            var canvas = victory.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 10;
            victory.Show(level, firstWin, coins, MetaProgress.Stars, ManagerFace.LastWinWasTense, ContinueToLobby, ReplayLevel);
        }

        // Same as Sweet Sugar's "Next" on its complete popup: reload the game scene, which opens in the lobby.
        void ContinueToLobby()
        {
            PlayerPrefs.SetInt("OpenLevel", victoryLevel + 1);
            CrosssceneData.openNextLevel = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        // Same as Sweet Sugar's "Again": reload and start the same level.
        void ReplayLevel()
        {
            PlayerPrefs.SetInt("OpenLevel", victoryLevel);
            new GameObject("RestartLevel").AddComponent<RestartLevel>();
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        /// Redraws the HUD and the room now (coins given from outside, for example).
        public void RefreshNow() => Refresh();

        void Refresh()
        {
            if (!content.activeSelf) return;
            var area = MetaProgress.CurrentArea(config);
            if (builtAreaIndex != MetaProgress.AreaIndex) BuildArea(area);
            var done = Mathf.Min(MetaProgress.TasksDone, area.tasks.Count);
            for (var i = 0; i < layers.Count; i++)
                layers[i].gameObject.SetActive(i < done && layers[i].sprite != null);
            SyncAreaStages(area, done);

            if (!coinsFlying) coinsText.text = CoinText(InitScript.Gems); // FlyCoins counts it up
            ApplyAvatar();
            starsText.text = MetaProgress.Stars.ToString();
            RefreshLives();
            var level = MetaProgress.NextLevel;
            var levelExists = level <= LevelCount();
            playButton.interactable = levelExists;
            playText.text = levelExists ? $"Nível {level}" : "Em breve";
            areaLabel.text = MetaProgress.IsAreaComplete(config)
                ? (MetaProgress.ChestClaimed ? "Restaurada" : "Concluída!")
                : $"Área {MetaProgress.AreaIndex + 1}";
            areaProgress.text = $"{done}/{area.tasks.Count}";
            // The pill fills with green as the area's upgrades are restored.
            if (AreaFill() is RectTransform fill)
            {
                var share = area.tasks.Count == 0 ? 1f : done / (float)area.tasks.Count;
                fill.anchorMax = new Vector2(share, 1);
                fill.gameObject.SetActive(share > 0);
            }
        }

        // The bar inside the area button's pill: when the prefab does not have it yet, it is made here, with the art of the
        // area window's progress bar (RoyalAvesFeatures: barBackground and barFill).
        RectTransform AreaFill()
        {
            if (areaFill != null) return areaFill;
            if (!(areaProgress.transform.parent is RectTransform box)) return null;
            if (box.Find(AreaTrackName) == null && features != null && features.barBackground != null)
                BarPart(box, AreaTrackName, features.barBackground).anchorMax = Vector2.one;
            areaFill = box.Find(AreaFillName) as RectTransform;
            if (areaFill == null && features != null && features.barFill != null)
                areaFill = BarPart(box, AreaFillName, features.barFill);
            if (box.Find(AreaTrackName) is Transform track) track.SetSiblingIndex(0);
            if (areaFill != null) areaFill.SetSiblingIndex(1);
            areaProgress.transform.SetAsLastSibling(); // the "0/5" over the bar
            return areaFill;
        }

        static RectTransform BarPart(RectTransform box, string name, Sprite sprite)
        {
            var rect = new GameObject(name, typeof(RectTransform)) { layer = box.gameObject.layer }.GetComponent<RectTransform>();
            rect.SetParent(box, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 0.5f);
            rect.offsetMin = new Vector2(AreaBarInset, AreaBarInset);
            rect.offsetMax = new Vector2(-AreaBarInset, -AreaBarInset);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;
            return rect;
        }

        void RefreshLives()
        {
            if (InitScript.Instance == null) return;
            var lives = InitScript.lifes;
            livesText.text = lives.ToString();
            if (lives >= InitScript.Instance.CapOfLife)
            {
                livesLabel.text = "Cheio";
                return;
            }
            var seconds = Mathf.Max(0, Mathf.CeilToInt(InitScript.RestLifeTimer));
            livesLabel.text = $"{seconds / 60}:{seconds % 60:00}";
        }

        // The coins won rise from "Nível N" to the coin counter one by one: the counter counts up as they land, the coin
        // icon bumps and Sweet Sugar's cash sound plays.
        IEnumerator FlyCoins(int amount)
        {
            coinsFlying = true;
            var end = InitScript.Gems;
            var start = Mathf.Max(0, end - amount);
            coinsText.text = CoinText(start);
            var target = coinsButton.transform.parent.Find("Coin") as RectTransform;
            if (target == null) target = coinsText.rectTransform;
            var targetImage = target.GetComponent<Image>();
            var layer = (RectTransform)content.transform;
            var toLayer = layer.lossyScale.x > 0 ? target.lossyScale.x / layer.lossyScale.x : 1;
            var size = target.rect.size * toLayer * 0.8f;
            var iconScale = target.localScale;
            yield return new WaitForSecondsRealtime(0.35f);

            var count = Mathf.Clamp(Mathf.CeilToInt(amount / 150f), 3, flyingCoins);
            var landed = 0;
            for (var i = 0; i < count; i++)
            {
                var spread = new Vector3(UnityEngine.Random.Range(-140f, 140f), UnityEngine.Random.Range(-60f, 80f), 0) * layer.lossyScale.x;
                StartCoroutine(FlyCoin(targetImage != null ? targetImage.sprite : null, size, layer,
                    playButton.transform.position + spread, target, i * 0.07f, () =>
                    {
                        landed++;
                        coinsText.text = CoinText(Mathf.RoundToInt(Mathf.Lerp(start, end, landed / (float)count)));
                        if (landed % 2 == 1 && SoundBase.Instance != null && SoundBase.Instance.cash != null)
                            SoundBase.Instance.PlayOneShot(SoundBase.Instance.cash);
                        StartCoroutine(Bump(target, iconScale));
                    }));
            }
            while (landed < count) yield return null;
            target.localScale = iconScale;
            coinsFlying = false;
            coinsText.text = CoinText(InitScript.Gems);
        }

        static string CoinText(int coins) => CoinFormat.Short(coins);

        IEnumerator FlyCoin(Sprite sprite, Vector2 size, RectTransform layer, Vector3 from, RectTransform target, float delay, Action landed)
        {
            yield return new WaitForSecondsRealtime(delay);
            var coin = new GameObject("Moeda voando", typeof(RectTransform), typeof(Image)) { layer = layer.gameObject.layer };
            var rect = (RectTransform)coin.transform;
            rect.SetParent(layer, false);
            rect.SetAsLastSibling();
            rect.sizeDelta = size;
            var image = coin.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            rect.position = from;
            // A curve that first swings up and to one side, then speeds into the counter.
            var control = from + new Vector3(UnityEngine.Random.Range(-220f, 220f), 260f, 0) * layer.lossyScale.x;
            for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / coinFlightSeconds)
            {
                var e = t * t;
                rect.position = Vector3.Lerp(Vector3.Lerp(from, control, e), Vector3.Lerp(control, target.position, e), e);
                rect.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.8f, e);
                yield return null;
            }
            Destroy(coin);
            landed?.Invoke();
        }

        static IEnumerator Bump(Transform icon, Vector3 baseScale)
        {
            for (var t = 0f; t < 0.16f; t += Time.unscaledDeltaTime)
            {
                icon.localScale = baseScale * (1 + 0.18f * Mathf.Sin(t / 0.16f * Mathf.PI));
                yield return null;
            }
            icon.localScale = baseScale;
        }

        void BuildArea(AreaDefinition area)
        {
            builtAreaIndex = MetaProgress.AreaIndex;
            background.sprite = area.background;
            if (roomFitter != null && area.background != null)
                roomFitter.aspectRatio = area.background.rect.width / area.background.rect.height;
            foreach (var layer in layers) Destroy(layer.gameObject);
            layers.Clear();
            foreach (var task in area.tasks)
            {
                var layer = new GameObject(task.id, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                var rect = layer.rectTransform;
                rect.SetParent(layersRoot, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                layer.sprite = task.layer;
                layer.raycastTarget = false;
                layers.Add(layer);
            }
        }

        // Hand-placed decoration stages for richer areas (Content/areas/<id>/Estagio_*), alongside the
        // single-sprite "layers" above. Areas without a matching folder (old-style, sprite-only areas)
        // just skip all of this — AreaStageRoot returns null and every call below no-ops.
        Transform AreaStageRoot(AreaDefinition area) =>
            area == null ? null : content.transform.Find($"areas/{area.id}");

        // Instantly matches every stage's visibility to how many tasks are already done — for when the lobby
        // opens or the area changes, so progress already made doesn't replay its build-up animation.
        void SyncAreaStages(AreaDefinition area, int done)
        {
            var root = AreaStageRoot(area);
            if (root == null) return;
            for (var i = 0; i < root.childCount; i++)
                SetStageState(root.GetChild(i), i < done);
        }

        // The animated counterpart of SyncAreaStages: plays only for the stage that was just unlocked.
        // Hides the "before" pieces instantly (nothing to animate there), but leaves activating the
        // "after" piece(s) entirely to AreaStageReveal.Play — doing it here first would flash them at
        // full visibility for a frame before the reveal coroutine resets them to animate in from zero.
        void RevealAreaStage(AreaDefinition area, int index)
        {
            var root = AreaStageRoot(area);
            if (root == null || index < 0 || index >= root.childCount) return;
            var stage = root.GetChild(index);
            for (var i = 0; i < stage.childCount; i++)
            {
                var child = stage.GetChild(i);
                if (child.name == "quebrado" || child.name.StartsWith("seta_")) child.gameObject.SetActive(false);
            }
            var fixedPiece = stage.Find("consertado");
            var pieces = fixedPiece != null
                ? new[] { (RectTransform)fixedPiece }
                : AllChildren(stage);
            StartCoroutine(AreaStageReveal.Play(this, (RectTransform)stage, pieces, stageSparkle, stageBurnTexture));
        }

        // Prefers the currently visible pieces (excluding "consertado", which a before/after stage parks
        // off to the side until it's needed — averaging it in would skew the position badly). But a plain
        // stage has EVERY piece inactive until it's bought, so when nothing is active yet, falls back to
        // averaging all of them anyway — a piece's position is still valid even while inactive.
        static Vector3 StageCenter(Transform stage)
        {
            if (stage.childCount == 0) return stage.position;
            var activeSum = Vector3.zero;
            var activeCount = 0;
            var allSum = Vector3.zero;
            for (var i = 0; i < stage.childCount; i++)
            {
                var child = stage.GetChild(i);
                allSum += child.position;
                if (!child.gameObject.activeSelf || child.name == "consertado") continue;
                activeSum += child.position;
                activeCount++;
            }
            return activeCount > 0 ? activeSum / activeCount : allSum / stage.childCount;
        }

        // A stage can be a plain "hidden until bought" group, or hold its own before/after pieces: a
        // "quebrado" (broken) piece plus "seta_*" hint arrows shown before buying, swapped for a
        // "consertado" (fixed) piece once bought — e.g. the broken clock with arrows pointing at it.
        // Anything else inside (like "suporte", the wall bracket) is left alone either way.
        static void SetStageState(Transform stage, bool unlocked)
        {
            var hasBeforeAfter = false;
            for (var i = 0; i < stage.childCount; i++)
            {
                var child = stage.GetChild(i);
                if (child.name == "consertado") { child.gameObject.SetActive(unlocked); hasBeforeAfter = true; }
                else if (child.name == "quebrado" || child.name.StartsWith("seta_"))
                {
                    child.gameObject.SetActive(!unlocked);
                    hasBeforeAfter = true;
                }
            }
            stage.gameObject.SetActive(hasBeforeAfter || unlocked);
        }

        static RectTransform[] AllChildren(Transform parent)
        {
            var result = new RectTransform[parent.childCount];
            for (var i = 0; i < result.Length; i++) result[i] = (RectTransform)parent.GetChild(i);
            return result;
        }

        void Play()
        {
            if (restoring) return;
            PlayClick();
            var level = MetaProgress.NextLevel;
            Log($"'Nível {level}' tocado; abrindo o pré-nível do Sweet Sugar");
            OpenLevelPopup(level);
        }

        // Sweet Sugar's pre-level popup. LoadLevel() inside OpenMenuPlay touches a lot of the template, so a failure there
        // used to stop the tap silently: it is caught and reported, and the state of the popup is always logged.
        void OpenLevelPopup(int level)
        {
            if (MenuReference.THIS == null)
            {
                Debug.LogError("Royal Aves: não há MenuReference na cena (CanvasGlobal); o pré-nível não abre.");
                return;
            }
            PlaceBehindSweetSugarPopups(); // CanvasGlobal may have come up after the lobby was first shown
            try
            {
                InitScript.OpenMenuPlay(level);
            }
            catch (Exception e)
            {
                Debug.LogError("Royal Aves: o pré-nível do Sweet Sugar falhou ao abrir (" + e.GetType().Name + ": " + e.Message + ")" + Environment.NewLine + e.StackTrace, this);
                return;
            }
            var menuPlay = MenuReference.THIS != null ? MenuReference.THIS.MenuPlay : null;
            if (menuPlay == null)
            {
                Debug.LogError("Royal Aves: MenuReference está na cena, mas o campo MenuPlay está vazio.", MenuReference.THIS);
                return;
            }
            Log($"MenuPlay '{PathOf(menuPlay.transform)}': ligado {menuPlay.activeSelf}, visível {menuPlay.activeInHierarchy}" +
                (menuPlay.activeInHierarchy ? "" : $"; desligado por '{FirstInactiveAncestor(menuPlay.transform)}'"));
            if (!logDiagnostics) return;
            LogDrawing(menuPlay);
            StartCoroutine(CheckPopupLater(menuPlay));
        }

        // The state at the moment of the tap is not the whole story: MenuPlay has a legacy Animation that plays on enable
        // and the template may hide the popup again later. This looks once on the next frame and once half a second in.
        IEnumerator CheckPopupLater(GameObject menuPlay)
        {
            yield return null;
            ReportPopup(menuPlay, "no frame seguinte");
            yield return new WaitForSecondsRealtime(0.5f);
            ReportPopup(menuPlay, "meio segundo depois");
        }

        void ReportPopup(GameObject menuPlay, string when)
        {
            if (menuPlay == null)
            {
                Log($"MenuPlay {when}: foi destruído");
                return;
            }
            if (!menuPlay.activeInHierarchy)
            {
                Log($"MenuPlay {when}: foi DESLIGADO (ligado {menuPlay.activeSelf}, desligado por '{FirstInactiveAncestor(menuPlay.transform)}')");
                return;
            }
            var rect = (RectTransform)menuPlay.transform;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var canvas = menuPlay.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
            var a = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            var b = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            var window = rect.childCount > 0 ? rect.GetChild(rect.childCount - 1) : null;
            Log($"MenuPlay {when}: ligado, escala {rect.localScale.x:0.###}, de {a} até {b}" +
                (window == null ? "" : $"; último filho '{window.name}' ativo {window.gameObject.activeSelf}, escala {window.localScale.x:0.###}"));
            LogPointerTargets(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        }

        // An active popup that nobody sees: this says who draws it (canvas, camera, sorting), whether that camera still
        // renders its layer, and how transparent and how big on screen the popup ended up.
        void LogDrawing(GameObject menuPlay)
        {
            var canvas = menuPlay.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Royal Aves: MenuPlay não está dentro de nenhum Canvas; nada o desenha.", menuPlay);
                return;
            }
            var root = canvas.rootCanvas;
            var cam = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
            Log($"desenho do MenuPlay: canvas '{PathOf(root.transform)}' {root.renderMode}, ligado {root.isActiveAndEnabled}, " +
                $"camada {SortingLayer.IDToName(canvas.sortingLayerID)} {canvas.sortingOrder}, distância {root.planeDistance}, " +
                $"câmera {NameOf(cam)}");
            if (root.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                if (cam == null)
                    Debug.LogWarning("Royal Aves: o canvas dos popups está em Screen Space - Camera sem câmera; ele não é desenhado.", root);
                else
                {
                    if ((cam.cullingMask & (1 << menuPlay.layer)) == 0)
                        Debug.LogWarning($"Royal Aves: a câmera '{cam.name}' não desenha a camada '{LayerMask.LayerToName(menuPlay.layer)}' do MenuPlay.", cam);
                    if (root.planeDistance > cam.farClipPlane || root.planeDistance < cam.nearClipPlane)
                        Debug.LogWarning($"Royal Aves: o canvas está a {root.planeDistance} da câmera '{cam.name}', fora do alcance dela " +
                                         $"({cam.nearClipPlane} a {cam.farClipPlane}); ele não aparece.", cam);
                    if (!cam.isActiveAndEnabled)
                        Debug.LogWarning($"Royal Aves: a câmera '{cam.name}' dos popups está desligada.", cam);
                }
            }
            var mine = GetComponent<Canvas>();
            Log($"canvas do lobby agora: {mine.renderMode}, ligado {mine.isActiveAndEnabled}, camada {mine.sortingLayerName} {mine.sortingOrder}, " +
                $"distância {mine.planeDistance}, câmera {NameOf(mine.worldCamera)}");
            // A camera with a higher depth that clears the colour buffer wipes out what the cameras before it drew: the
            // popup would vanish from the screen while still taking the taps.
            foreach (var c in Camera.allCameras)
                Log($"câmera '{c.name}': ligada {c.isActiveAndEnabled}, profundidade {c.depth}, limpeza {c.clearFlags}, " +
                    $"alvo {(c.targetTexture == null ? "tela" : c.targetTexture.name)}, máscara {c.cullingMask}, near {c.nearClipPlane}, far {c.farClipPlane}");
            var alpha = 1f;
            foreach (var group in menuPlay.GetComponentsInParent<CanvasGroup>()) alpha *= group.alpha;
            var corners = new Vector3[4];
            ((RectTransform)menuPlay.transform).GetWorldCorners(corners);
            var a = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            var b = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            Log($"MenuPlay na tela: de {a} até {b} (tela {Screen.width}x{Screen.height}), escala {menuPlay.transform.localScale.x:0.###}, " +
                $"opacidade dos CanvasGroups {alpha:0.##}");
        }

        // Which object in the chain is switched off: an active MenuPlay under an inactive CanvasGlobal shows nothing.
        static string FirstInactiveAncestor(Transform t)
        {
            for (; t != null; t = t.parent)
                if (!t.gameObject.activeSelf) return PathOf(t);
            return "nenhum";
        }

        void OpenArea()
        {
            if (restoring) return;
            PlayClick();
            if (MetaProgress.IsAreaComplete(config)) OpenAreaComplete();
            else if (AreaStageRoot(MetaProgress.CurrentArea(config)) != null) OpenAreaHotspot();
            else OpenDecorations();
        }

        // As in the prototype: the next upgrade plus one preview; the rest appear one per purchase.
        // Used as a fallback for areas with no hand-placed Estagio_* stages (OpenAreaHotspot needs those).
        void OpenDecorations()
        {
            var area = MetaProgress.CurrentArea(config);
            var done = Mathf.Min(MetaProgress.TasksDone, area.tasks.Count);
            meterFill.anchorMax = new Vector2(area.tasks.Count == 0 ? 1 : done / (float)area.tasks.Count, 1);
            meterText.text = $"{done}/{area.tasks.Count}";
            for (var i = 0; i < taskRows.Length; i++)
            {
                var index = done + i;
                var visible = index < area.tasks.Count;
                taskRows[i].gameObject.SetActive(visible);
                if (visible) taskRows[i].Show(area.tasks[index], i == 0, MetaProgress.Stars >= area.tasks[index].starCost);
            }
            ShowModal(decorationsExtra, area.title, primary: "Fechar");
            SetLobbyChromeVisible(false);
            StartCoroutine(RestoreChromeWhenModalCloses());
        }

        // The real area view: no modal at all. HUD/nav get out of the way (like OpenDecorations) and a
        // freshly built "Restaurar ★ n" tag floats in world space right over the piece it unlocks, taken
        // from the hand-placed Estagio_* stage. Built from scratch (not the old list's LobbyTaskRow) so it
        // never depends on a specific prefab object surviving whatever the area's hierarchy gets rebuilt into.
        GameObject hotspotClose, hotspotTag;
        TextMeshProUGUI hotspotTagText;

        void OpenAreaHotspot()
        {
            SetLobbyChromeVisible(false);
            if (content.transform.Find("Actions") is Transform actions) actions.gameObject.SetActive(false);
            ShowHotspotCloseButton();
            if (!ShowHotspotTag()) CloseAreaHotspot(); // area already complete somehow — nothing to show
        }

        // Positions the tag over the next unbought stage; returns false if there is none left.
        bool ShowHotspotTag()
        {
            var area = MetaProgress.CurrentArea(config);
            var root = AreaStageRoot(area);
            var done = Mathf.Min(MetaProgress.TasksDone, area.tasks.Count);
            if (root == null || done >= area.tasks.Count || done >= root.childCount) return false;
            var stage = root.GetChild(done);
            var task = area.tasks[done];
            if (hotspotTag == null) BuildHotspotTag();
            hotspotTag.SetActive(true);
            hotspotTagText.text = $"{task.title}\n★ {task.starCost}";
            var rect = (RectTransform)hotspotTag.transform;
            rect.SetAsLastSibling();
            // .position is world space, where 1 unit is NOT 1 pixel on a ScreenSpaceCamera canvas (it's
            // tiny) — landing on the stage's world position is fine, but nudging it up needs to happen in
            // anchoredPosition (pixel space) instead, or "80" would fling it far off-screen.
            rect.position = StageCenter(stage);
            rect.anchoredPosition += new Vector2(0f, 80f);
            return true;
        }

        // Without enough stars, buying would just silently fail — instead, same as the prototype's "Ganhe
        // diamantes": open the explainer that sends the player into a level, since that's how stars are earned.
        void HotspotTagClicked()
        {
            var area = MetaProgress.CurrentArea(config);
            var done = Mathf.Min(MetaProgress.TasksDone, area.tasks.Count);
            if (done < area.tasks.Count && MetaProgress.Stars < area.tasks[done].starCost)
            {
                PlayClick();
                starsPopup?.Open();
                return;
            }
            BuyShownTask(0);
        }

        void BuildHotspotTag()
        {
            hotspotTag = new GameObject("HotspotTag", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rect = (RectTransform)hotspotTag.transform;
            rect.SetParent(content.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(260f, 100f);
            var background = hotspotTag.GetComponent<Image>();
            background.color = new Color(0.2f, 0.65f, 0.25f, 0.95f);
            var button = hotspotTag.GetComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(HotspotTagClicked);

            var textGo = new GameObject("Text", typeof(RectTransform));
            var textRect = (RectTransform)textGo.transform;
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 6f);
            textRect.offsetMax = new Vector2(-10f, -6f);
            hotspotTagText = textGo.AddComponent<TextMeshProUGUI>();
            hotspotTagText.alignment = TextAlignmentOptions.Center;
            hotspotTagText.fontSize = 26f;
            hotspotTagText.color = Color.white;
            hotspotTagText.raycastTarget = false;
        }

        void ShowHotspotCloseButton()
        {
            if (hotspotClose != null) { hotspotClose.SetActive(true); return; }
            hotspotClose = new GameObject("HotspotClose", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rect = (RectTransform)hotspotClose.transform;
            rect.SetParent(content.transform, false);
            rect.SetAsLastSibling();
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(84, 84);
            rect.anchoredPosition = new Vector2(24, -24);
            var image = hotspotClose.GetComponent<Image>();
            image.sprite = modalClose.image.sprite;
            image.preserveAspect = true;
            var button = hotspotClose.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => { PlayClick(); CloseAreaHotspot(); });
        }

        void CloseAreaHotspot()
        {
            SetLobbyChromeVisible(true);
            if (content.transform.Find("Actions") is Transform actions) actions.gameObject.SetActive(true);
            if (hotspotClose != null) hotspotClose.SetActive(false);
            if (hotspotTag != null) hotspotTag.SetActive(false);
        }

        // Like the prototype's area view: the top HUD and bottom nav get out of the way while picking an
        // upgrade, so the room itself (with the window over it) is what's on screen. Restored the moment the
        // modal closes, however it closes ("Fechar" or the X) — both just set modal inactive.
        void SetLobbyChromeVisible(bool visible)
        {
            if (content.transform.Find("HUD") is Transform hud) hud.gameObject.SetActive(visible);
            if (content.transform.Find("Nav") is Transform nav) nav.gameObject.SetActive(visible);
        }

        IEnumerator RestoreChromeWhenModalCloses()
        {
            yield return new WaitUntil(() => !modal.activeSelf);
            SetLobbyChromeVisible(true);
        }

        void BuyShownTask(int row)
        {
            if (row != 0 || restoring) return; // only the next upgrade can be bought
            var area = MetaProgress.CurrentArea(config);
            var index = MetaProgress.TasksDone;
            if (!MetaProgress.BuyNextTask(config)) return; // Changed refreshes the lobby and turns the new layer on
            modal.SetActive(false);
            StartCoroutine(Restore(area, area.tasks[index], index));
        }

        // The prototype's arrival: a flash, the upgrade's token drops in with its name and its layer fades into the
        // room. Skipped in the area hotspot view (SetLobbyChromeVisible/ShowHotspotTag) — there the piece builds
        // itself right where it stands (AreaStageReveal), so the full-screen token-drop toast is redundant noise.
        IEnumerator Restore(AreaDefinition area, AreaTask task, int index)
        {
            var inHotspot = hotspotClose != null && hotspotClose.activeSelf;
            restoring = true;
            if (restoreSound != null && SoundBase.Instance != null) SoundBase.Instance.PlayOneShot(restoreSound);
            var layer = index < layers.Count ? layers[index] : null;
            // The effect covers the whole lobby and takes every tap, so it is turned off even if the coroutine is cut
            // short (the lobby hidden, the scene reloaded): otherwise nothing in the lobby would answer again.
            if (!inHotspot)
            {
                try
                {
                    restoreIcon.sprite = task.icon;
                    restoreIcon.enabled = task.icon != null;
                    restoreLabel.text = task.title;
                    restoreEffect.SetActive(true);
                    for (var t = 0f; t < restoreSeconds; t += Time.unscaledDeltaTime)
                    {
                        var p = t / restoreSeconds;
                        var drop = Mathf.Clamp01(p / 0.6f);
                        restoreGlow.alpha = Mathf.Sin(p * Mathf.PI);
                        restoreToken.localScale = Vector3.one * BackOut(drop);
                        restoreToken.anchoredPosition = new Vector2(0, Mathf.Lerp(420, 120, 1 - (1 - drop) * (1 - drop)));
                        restoreLabelBox.localScale = Vector3.one * BackOut(Mathf.Clamp01((p - 0.2f) / 0.5f));
                        if (layer != null) layer.color = new Color(1, 1, 1, Mathf.Clamp01(p * 1.5f));
                        yield return null;
                    }
                }
                finally
                {
                    if (restoreEffect != null) restoreEffect.SetActive(false);
                    restoring = false;
                }
            }
            else restoring = false;
            if (layer != null) layer.color = Color.white;
            // Before Refresh(): Refresh -> SyncAreaStages would otherwise instantly show the just-bought
            // stage at full visibility (done already counts it), flashing it before the reveal plays.
            RevealAreaStage(area, index);
            Refresh();
            if (inHotspot)
            {
                if (MetaProgress.IsAreaComplete(config)) CloseAreaHotspot();
                else ShowHotspotTag(); // moves the tag to the next stage
            }
            if (MetaProgress.IsAreaComplete(config)) OpenAreaComplete();
        }

        static float BackOut(float x)
        {
            const float c = 1.70158f;
            x -= 1;
            return 1 + (c + 1) * x * x * x + c * x * x;
        }

        void OpenAreaComplete()
        {
            if (MetaProgress.ChestClaimed)
            {
                ShowNextArea();
                return;
            }
            var area = MetaProgress.CurrentArea(config);
            ShowModal(chestExtra, $"{area.title} concluído!", "Seu baú está pronto!", "AGÊNCIA RESTAURADA", true,
                "Toque para abrir", OpenChest, "Mais tarde");
        }

        void OpenChest()
        {
            if (!MetaProgress.ClaimChest(config)) return;
            rewardText.text = RewardText(MetaProgress.CurrentArea(config).reward);
            ShowModal(rewardExtra, "Baú de Correspondências", tag: "ÁREA RESTAURADA", art: true, primary: "Recolher", onPrimary: ShowNextArea);
        }

        void ShowNextArea()
        {
            var area = MetaProgress.CurrentArea(config);
            if (MetaProgress.HasNextArea(config))
            {
                var next = config.areas[MetaProgress.AreaIndex + 1];
                ShowModal(lockedAreaExtra, "Nova área!", next.title, "NOVA RESTAURAÇÃO", true, "Desbloquear",
                    () => MetaProgress.GoToNextArea(config));
                return;
            }
            ShowModal(null, $"{area.title} restaurado!", "Continue jogando para guardar estrelas.", "BAÚ RECOLHIDO", true,
                "Jogar", Play, "Ficar na agência");
        }

        static string RewardText(AreaReward reward)
        {
            var parts = new List<string>();
            if (reward.coins > 0) parts.Add($"+{CoinFormat.Short(reward.coins)} moedas");
            parts.AddRange(reward.boosters.Select(b => $"+{b.count} {b.type}"));
            return parts.Count > 0 ? string.Join("\n", parts) : "Área restaurada!";
        }

        void OpenSettings()
        {
            PlayClick();
            RefreshAudioTexts();
            ShowModal(settingsExtra, "Configurações", tag: "CORREIO MÁGICO", art: true, primary: "Voltar");
        }

        // The same switches as Sweet Sugar's settings popup: mixer volume 1 (on) or -80 (off), saved in PlayerPrefs.
        void Toggle(string key)
        {
            PlayClick();
            var volume = IsOn(key) ? -80 : 1;
            AudioMixer mixer = null;
            if (key == "Sound" && SoundBase.Instance != null) mixer = SoundBase.Instance.audioMixer;
            if (key == "Music" && MusicBase.Instance != null) mixer = MusicBase.Instance.audioMixer;
            if (mixer != null) mixer.SetFloat(key + "Volume", volume);
            PlayerPrefs.SetInt(key, volume);
            PlayerPrefs.Save();
            RefreshAudioTexts();
        }

        static bool IsOn(string key) => PlayerPrefs.GetInt(key, 1) > -80;

        void RefreshAudioTexts()
        {
            soundText.text = "Som: " + (IsOn("Sound") ? "ligado" : "desligado");
            musicText.text = "Música: " + (IsOn("Music") ? "ligada" : "desligada");
        }

        void OpenTab(int tab)
        {
            PlayClick();
            modal.SetActive(false);
            if (menus != null && tab < TabRoutes.Length)
            {
                menus.Open(TabRoutes[tab]);
                return;
            }
            if (tab != HomeTab) ComingSoon(tab < NavTitles.Length ? NavTitles[tab] : "Menu");
        }

        void OpenInventory()
        {
            if (menus == null)
            {
                OpenSweetSugarMenu(MenuReference.THIS != null ? MenuReference.THIS.GemsShop : null);
                return;
            }
            PlayClick();
            modal.SetActive(false);
            menus.Open(LobbyMenus.Route.Inventory);
        }

        void OpenProfile()
        {
            if (menus == null)
            {
                ComingSoon("Perfil");
                return;
            }
            PlayClick();
            modal.SetActive(false);
            menus.Open(LobbyMenus.Route.Profile);
        }

        void PlayLevelFromMenu(int level)
        {
            if (menus != null) menus.Close();
            if (level < 1 || level > LevelCount()) return;
            Log($"nível {level} escolhido no menu; abrindo o pré-nível do Sweet Sugar");
            OpenLevelPopup(level);
        }

        // The avatar chosen in Perfil: the first is the animated manager, the others a picture in her place.
        void ApplyAvatar()
        {
            if (features == null) return;
            var face = avatarButton.transform.Find("Face/Gerente") as RectTransform;
            if (face == null) return;
            var manager = face.GetComponent<ManagerFace>();
            var image = face.GetComponent<Image>();
            managerAvatarPosition ??= face.anchoredPosition;
            managerAvatarSize ??= face.sizeDelta;
            var index = MetaProgress.Avatar;
            var custom = index > 0 && index < features.avatars.Count && features.avatars[index] != null;
            if (manager != null) manager.enabled = !custom;
            if (custom)
            {
                image.sprite = features.avatars[index];
                face.anchoredPosition = Vector2.zero;
                face.sizeDelta = ((RectTransform)face.parent).rect.size * 0.8f;
            }
            else
            {
                face.anchoredPosition = managerAvatarPosition.Value;
                face.sizeDelta = managerAvatarSize.Value;
            }
        }

        void ComingSoon(string title)
        {
            ShowModal(null, title, "Em breve no Correio Mágico.", art: true, primary: "Voltar");
        }

        // The lobby gear works as a switch: one tap opens Sweet Sugar's settings window, the next one closes it.
        // Closing is a plain SetActive(false) on purpose. Sweet Sugar's own AnimationEventManager.CloseMenu sends the
        // player back to the title screen when a window named "Settings" is closed in the Map state, which is exactly
        // what the lobby must not do.
        void ToggleSettings()
        {
            if (settingsScreen != null)
            {
                if (settingsScreen.IsOpen) settingsScreen.Close();
                else settingsScreen.Open();
                return;
            }

            var menu = MenuReference.THIS != null ? MenuReference.THIS.Settings : null;
            if (menu != null && menu.activeInHierarchy)
            {
                PlayClick();
                menu.SetActive(false);
                return;
            }
            OpenSweetSugarMenu(menu);
        }

        void OpenSweetSugarMenu(GameObject menu)
        {
            PlayClick();
            PlaceBehindSweetSugarPopups();
            if (menu != null) menu.SetActive(true);
        }

        void ShowModal(GameObject extra, string title, string copy = "", string tag = "", bool art = false,
            string primary = "OK", Action onPrimary = null, string secondary = null, Action onSecondary = null)
        {
            foreach (var e in extras) e.SetActive(e == extra);
            modalArt.SetActive(art);
            modalTag.gameObject.SetActive(!string.IsNullOrEmpty(tag));
            modalTag.text = tag;
            modalTitle.text = title;
            modalCopy.gameObject.SetActive(!string.IsNullOrEmpty(copy));
            modalCopy.text = copy;
            modalPrimaryText.text = primary;
            primaryAction = onPrimary;
            modalSecondary.gameObject.SetActive(secondary != null);
            modalSecondaryText.text = secondary ?? "";
            secondaryAction = onSecondary;
            modal.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(modalWindow);
        }

        void CloseModalThen(Action next)
        {
            PlayClick();
            modal.SetActive(false);
            next?.Invoke();
        }

        void Log(string message)
        {
            if (logDiagnostics) Debug.Log("[Correio Mágico] " + message, this);
        }

        // Lists the UI objects under the pointer, front first: shows whether anything covers the lobby buttons.
        void LogPointerTargets(Vector2 position)
        {
            if (EventSystem.current == null)
            {
                Log("toque, mas a cena não tem EventSystem");
                return;
            }
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
            var top = hits.Take(3).Select(h => $"{PathOf(h.gameObject.transform)} [{SortingLayer.IDToName(h.sortingLayer)} {h.sortingOrder}]");
            Log($"toque em {position}: " + (hits.Count == 0 ? "nenhum objeto de UI" : string.Join("  |  ", top)));
        }

        static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (; t != null; t = t.parent) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }

        static int LevelCount()
        {
            if (CrosssceneData.totalLevels == 0) CrosssceneData.totalLevels = LoadingManager.GetLastLevelNum();
            return CrosssceneData.totalLevels;
        }

        static void PlayClick()
        {
            if (SoundBase.Instance != null) SoundBase.Instance.PlayOneShot(SoundBase.Instance.click);
        }
    }
}
