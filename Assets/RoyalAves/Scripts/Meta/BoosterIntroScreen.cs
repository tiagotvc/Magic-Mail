// Tela intermediária de uso de power-up (estilo Royal Match). Quando o jogador escolhe martelo, flecha ou canhão, o jogo
// escurece tudo MENOS o tabuleiro; em cima aparecem a figura do power-up com brilho dourado atrás, o nome e o texto de
// como usar; embaixo só o botão selecionado continua visível, com partículas atrás. Some quando o power-up é usado ou
// cancelado. Não bloqueia o toque no tabuleiro (as faixas escuras não recebem toque), então usar o power-up funciona
// como antes; tocar no botão em destaque cancela a seleção.
//
// É o prefab Resources/UI/BoosterIntroScreen: abra e edite qualquer pedaço. O conteúdo de cada power-up vem do
// BoosterIntroCatalog. Os textos usam LocalizedImage / LocalizedTextTMP (português e inglês, idioma global do jogo).
using System.Collections;
using RoyalAves.Effects;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.GUI.Boost;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public sealed class BoosterIntroScreen : MonoBehaviour
    {
        public const string PrefabPath = "UI/BoosterIntroScreen";

        [Header("Partes da tela")]
        [SerializeField] CanvasGroup group;
        [Tooltip("Imagem preta que cobre a tela toda, com um buraco no formato do tabuleiro (material BoosterDimHole).")]
        [SerializeField] Image dim;
        [SerializeField] Material dimMaterial;
        [SerializeField] RectTransform header;
        [SerializeField] Image icon;
        [Tooltip("Brilho dourado atrás da figura do power-up.")]
        [SerializeField] UIMagicGlow headerGlow;
        [SerializeField] Image nameImage;
        [SerializeField] LocalizedImage nameLocalized;
        [Tooltip("Texto mostrado no lugar da imagem de nome quando o power-up ainda não tem uma.")]
        [SerializeField] TMP_Text nameFallback;
        [SerializeField] LocalizedTextTMP nameFallbackText;
        [SerializeField] TMP_Text instruction;
        [SerializeField] LocalizedTextTMP instructionText;
        [Tooltip("Onde a cópia do botão selecionado é desenhada (cobre a tela toda).")]
        [SerializeField] RectTransform buttonLayer;
        [Tooltip("Partículas e brilho atrás do botão selecionado.")]
        [SerializeField] UIMagicGlow buttonGlow;

        [Header("Escurecimento")]
        [Range(0f, 1f)] [SerializeField] float dimAlpha = 0.93f;
        [Tooltip("Liga: o tabuleiro de verdade é desenhado por cima do fundo escuro (uma câmera extra copia só o mundo do jogo), " +
                 "então ele fica perfeito, com moldura e tudo. Desligado: usa o buraco recortado (aproximado).")]
        [SerializeField] bool useBoardCamera = true;
        [Tooltip("Quanto o buraco passa das casas, em casas (cobre a moldura dourada e arredonda os cantos).")]
        [SerializeField] float boardMargin = 0.11f;
        [Tooltip("Raio dos cantos de fora da moldura, em casas (somado à folga).")]
        [SerializeField] float boardCornerRadius = 0.14f;
        [Tooltip("Suavidade da borda do buraco, em casas.")]
        [SerializeField] float boardFeather = 0.012f;
        [Tooltip("Texels por casa na máscara do tabuleiro.")]
        [SerializeField] int holeResolution = 20;

        [Header("Tempo (segundos)")]
        [SerializeField] float fadeIn = 0.2f;
        [SerializeField] float fadeOut = 0.18f;
        [Tooltip("Entrada da figura do power-up (cresce com um quique).")]
        [SerializeField] float iconPopSeconds = 0.45f;
        [SerializeField] float buttonPopSeconds = 0.35f;
        [SerializeField] float buttonPopScale = 1.07f;

        [Header("Botão selecionado")]
        [Tooltip("Tamanho das partículas atrás do botão, em múltiplos do botão.")]
        [SerializeField] float buttonGlowScale = 1.8f;
        [Header("Brilho no contorno da figura do power-up (o mesmo da flecha em voo)")]
        [SerializeField] bool iconRim = true;
        [SerializeField] Color iconRimColor = new Color(1f, 0.66f, 0.18f, 1f);
        [Tooltip("Alcance do brilho em coordenadas da imagem: a camada de dentro (fina e forte) e a de fora (larga e fraca).")]
        [SerializeField] Vector2 iconRimRadius = new Vector2(0.018f, 0.04f);
        [Range(0f, 1f)] [SerializeField] float iconRimAlpha = 1f;

        [Header("Contorno luminoso do botão (desligado: só o brilho atrás)")]
        [SerializeField] bool useOutline = false;
        [SerializeField] Color outlineColor = new Color(1f, 0.86f, 0.22f, 1f);
        [Tooltip("Quanto o contorno passa da borda do botão (fração do botão). Pequeno = quase uma linha.")]
        [SerializeField] float outlinePad = 0.04f;
        [Range(0f, 1f)] [SerializeField] float outlineAlpha = 0.95f;
        [Tooltip("Segunda camada, mais larga e fraca, que dá o brilho em volta da linha.")]
        [SerializeField] float outlineGlowPad = 0.15f;
        [Range(0f, 1f)] [SerializeField] float outlineGlowAlpha = 0.6f;
        [SerializeField] float outlinePulseSpeed = 0.8f;

        BoostIcon boost;
        BoosterIntroCatalog.Entry entry;
        RectTransform root;
        Canvas canvas;
        RectTransform snapshot;
        readonly System.Collections.Generic.List<Image> outlines = new System.Collections.Generic.List<Image>();
        Vector2 frameCenter, frameSize;
        Camera boardCamera;
        RenderTexture boardTexture;
        RawImage boardView;
        Material dimInstance;
        Image rimInner, rimOuter;
        Material rimInnerMaterial, rimOuterMaterial;
        Texture2D holeTexture;
        Vector2 snapshotCenter;
        Vector2Int lastScreen;
        bool closing;
        float age;
        bool basesCaptured;
        Vector3 iconBaseScale, nameBaseScale;
        Vector2 iconBasePosition;

        /// Botão "Usar!" no meio do tabuleiro, para power-ups que não precisam de toque numa peça (o Chapéu de Mago).
        /// Com ele a tela cobre o tabuleiro também; tocar fora do botão cancela o power-up sem gastar.
        public sealed class UseButton
        {
            public Sprite border, center;
            public TMP_FontAsset font;
            public string labelPt = "Usar!", labelEn = "Use!";
            public Vector2 size = new Vector2(560f, 250f);
            public System.Action onUse;
        }

        UseButton use;
        RectTransform useRect;
        bool used;

        public static BoosterIntroScreen Current { get; private set; }

        /// Instante (Time.unscaledTime) até o qual a tela fica escura mesmo depois que o power-up foi usado: o efeito da
        /// flecha atravessando a linha marca aqui quando termina. Só o fundo escuro e o tabuleiro ficam; título e botão somem.
        public static float HoldUntil;

        public static bool IsOpen => Current != null;

        // ------------------------------------------------------------------------------------------ abrir / fechar

        /// Abre a tela para o power-up escolhido. false = sem prefab/entrada (o jogo segue como antes, sem a tela).
        public static bool Show(BoostIcon boost)
        {
            if (boost == null) return false;
            var entry = BoosterIntroCatalog.Find(boost.type);
            var prefab = Resources.Load<BoosterIntroScreen>(PrefabPath);
            if (entry == null || prefab == null) return false;
            Hide(true);
            var view = Instantiate(prefab);
            view.name = "Apresentação de power-up - " + entry.DisplayName;
            view.boost = boost;
            view.entry = entry;
            Current = view;
            view.Setup();
            view.StartCoroutine(view.Run());
            return true;
        }

        /// Abre a tela com o botão "Usar!". entry: conteúdo usado se o catálogo não tiver uma entrada para este power-up.
        public static bool ShowUse(BoostIcon boost, BoosterIntroCatalog.Entry entry, UseButton use)
        {
            if (boost == null || use == null || use.border == null) return false;
            var found = BoosterIntroCatalog.Find(boost.type);
            if (found != null) entry = found;
            var prefab = Resources.Load<BoosterIntroScreen>(PrefabPath);
            if (entry == null || prefab == null) return false;
            Hide(true);
            var view = Instantiate(prefab);
            view.name = "Apresentação de power-up - " + entry.DisplayName;
            view.boost = boost;
            view.entry = entry;
            view.use = use;
            Current = view;
            view.Setup();
            if (LevelManager.THIS != null) LevelManager.THIS.DragBlocked = true;      // nada de arrastar peça com a tela aberta
            view.StartCoroutine(view.Run());
            return true;
        }

        void BuildUseButton()
        {
            if (use == null || useRect != null) return;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            // tocar no escuro cancela (o mesmo que tocar de novo no power-up da barra)
            if (dim != null)
            {
                dim.raycastTarget = true;
                var cancel = dim.GetComponent<Button>();
                if (cancel == null) cancel = dim.gameObject.AddComponent<Button>();
                cancel.transition = Selectable.Transition.None;
                cancel.onClick.AddListener(() =>
                {
                    if (closing || used) return;
                    var lm = LevelManager.THIS;
                    if (lm != null && lm.activatedBoost == boost) { lm.activatedBoost = null; lm.UnLockBoosts(); }
                    BeginClose();
                });
            }

            var go = new GameObject("Usar", typeof(RectTransform), typeof(Image), typeof(Button));
            useRect = (RectTransform)go.transform;
            useRect.SetParent(root, false);
            useRect.SetAsLastSibling();
            useRect.anchorMin = useRect.anchorMax = new Vector2(0.5f, 0.5f);
            useRect.sizeDelta = use.size;
            useRect.anchoredPosition = BoardCenterInRoot();
            var borderImage = go.GetComponent<Image>();
            borderImage.sprite = use.border; borderImage.preserveAspect = false;

            if (use.center != null)
            {
                var core = new GameObject("Miolo", typeof(RectTransform), typeof(Image));
                var coreRect = (RectTransform)core.transform;
                coreRect.SetParent(useRect, false);
                coreRect.anchorMin = new Vector2(0.035f, 0.09f); coreRect.anchorMax = new Vector2(0.965f, 0.93f);
                coreRect.offsetMin = coreRect.offsetMax = Vector2.zero;
                var coreImage = core.GetComponent<Image>();
                coreImage.sprite = use.center; coreImage.raycastTarget = false;
            }

            var label = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRect = (RectTransform)label.transform;
            labelRect.SetParent(useRect, false);
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(30f, 24f); labelRect.offsetMax = new Vector2(-30f, -12f);
            var text = label.GetComponent<TextMeshProUGUI>();
            if (use.font != null) text.font = use.font;
            else if (nameFallback != null) text.font = nameFallback.font;
            text.text = LocalizedImage.Current == GameLanguage.Portuguese ? use.labelPt : use.labelEn;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true; text.fontSizeMin = 40f; text.fontSizeMax = 130f;
            text.color = Color.white;
            text.outlineColor = new Color32(30, 90, 10, 255); text.outlineWidth = 0.22f;
            text.raycastTarget = false;

            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (closing || used) return;
                used = true;
                var action = use.onUse;
                BeginClose();
                action?.Invoke();
            });
        }

        Vector2 BoardCenterInRoot()
        {
            var lm = LevelManager.THIS; var cam = Camera.main;
            if (lm == null || lm.field == null || cam == null || root == null) return Vector2.zero;
            var squares = lm.field.GetSquares(true);
            if (squares == null || squares.Count == 0) return Vector2.zero;
            Vector3 lo = new Vector3(float.MaxValue, float.MaxValue), hi = new Vector3(float.MinValue, float.MinValue);
            foreach (var square in squares)
            {
                if (square == null || square.IsNone()) continue;
                lo = Vector3.Min(lo, square.transform.position); hi = Vector3.Max(hi, square.transform.position);
            }
            if (lo.x > hi.x) return Vector2.zero;
            return ScreenToRoot(cam.WorldToScreenPoint((lo + hi) * 0.5f));
        }

        public static void Hide(bool immediate = false)
        {
            if (Current == null) return;
            var view = Current;
            Current = null;
            if (immediate) Destroy(view.gameObject);
            else view.BeginClose();
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            if (holeTexture != null) Destroy(holeTexture);
            if (dimInstance != null) Destroy(dimInstance);
            if (rimInnerMaterial != null) Destroy(rimInnerMaterial);
            if (rimOuterMaterial != null) Destroy(rimOuterMaterial);
            ReleaseBoardCamera();
        }

        void ReleaseBoardCamera()
        {
            if (boardCamera != null) Destroy(boardCamera.gameObject);
            if (boardTexture != null) { boardTexture.Release(); Destroy(boardTexture); }
            boardCamera = null; boardTexture = null;
        }

        // Acompanha a câmera do jogo (ela se mexe ao montar a fase).
        void LateUpdate()
        {
            var main = Camera.main;
            if (boardCamera == null || main == null) return;
            boardCamera.transform.SetPositionAndRotation(main.transform.position, main.transform.rotation);
            boardCamera.orthographic = main.orthographic;
            boardCamera.orthographicSize = main.orthographicSize;
            boardCamera.fieldOfView = main.fieldOfView;
        }

        void Setup()
        {
            root = (RectTransform)transform;
            canvas = GetComponent<Canvas>();
            CaptureBases();
            Fill(entry);
            if (group != null) { group.alpha = 0f; group.interactable = true; group.blocksRaycasts = true; }
        }

        void CaptureBases()
        {
            if (basesCaptured) return;
            basesCaptured = true;
            if (icon != null) { iconBaseScale = icon.rectTransform.localScale; iconBasePosition = icon.rectTransform.anchoredPosition; }
            if (nameImage != null) nameBaseScale = nameImage.rectTransform.localScale;
        }

        /// Aplica o conteúdo da entrada nas partes da tela (também usado pelo editor).
        public void Fill(BoosterIntroCatalog.Entry e)
        {
            CaptureBases();
            if (e == null) return;
            if (icon != null)
            {
                icon.sprite = e.icon;
                icon.enabled = e.icon != null;
                icon.preserveAspect = true;
                icon.rectTransform.localScale = iconBaseScale * Mathf.Max(0.05f, e.iconScale);
                icon.rectTransform.anchoredPosition = iconBasePosition + e.iconOffset;
            }
            RefreshRim();
            var hasName = e.nameSpritePt != null || e.nameSpriteEn != null;
            if (nameLocalized != null)
            {
                nameLocalized.SetSprite(GameLanguage.Portuguese, e.nameSpritePt != null ? e.nameSpritePt : e.nameSpriteEn);
                nameLocalized.SetSprite(GameLanguage.English, e.nameSpriteEn != null ? e.nameSpriteEn : e.nameSpritePt);
            }
            if (nameImage != null)
            {
                nameImage.preserveAspect = true;
                nameImage.gameObject.SetActive(hasName);
                nameImage.rectTransform.localScale = nameBaseScale * Mathf.Max(0.05f, e.nameScale);
            }
            if (nameFallback != null) nameFallback.gameObject.SetActive(!hasName);
            if (!hasName && nameFallbackText != null) nameFallbackText.SetTexts(e.titlePt, e.titleEn);
            if (instructionText != null) instructionText.SetTexts(e.instructionPt, e.instructionEn);
        }

        /// Brilho só no contorno da figura: duas cópias da imagem, filhas do ícone (por isso crescem junto com ele), com o shader
        /// RoyalAves/ArrowRim, que só desenha luz por FORA da silhueta. O material vem do prefab do efeito da flecha.
        void RefreshRim()
        {
            if (icon == null) return;
            var source = Resources.Load<ArrowFlightEffect>(ArrowFlightEffect.ResourcePath);
            var rim = source != null ? source.rimMaterial : null;
            var show = iconRim && rim != null && icon.sprite != null;
            if (rimInner == null && show)
            {
                rimOuterMaterial = new Material(rim); rimInnerMaterial = new Material(rim);
                rimOuter = MakeRim("Contorno luminoso (fora)", rimOuterMaterial);
                rimInner = MakeRim("Contorno luminoso (dentro)", rimInnerMaterial);
            }
            if (rimInner == null) return;
            rimInnerMaterial.SetColor("_Color", Color.white); rimOuterMaterial.SetColor("_Color", Color.white);
            rimInnerMaterial.SetFloat("_Radius", iconRimRadius.x); rimOuterMaterial.SetFloat("_Radius", iconRimRadius.y);
            foreach (var image in new[] { rimInner, rimOuter })
            {
                image.sprite = icon.sprite;
                image.preserveAspect = true;
                image.gameObject.SetActive(show);
            }
        }

        Image MakeRim(string label, Material material)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(icon.transform, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.material = material;
            image.raycastTarget = false;
            image.color = new Color(iconRimColor.r, iconRimColor.g, iconRimColor.b, 0f);
            return image;
        }

        // ------------------------------------------------------------------------------------------ laço

        IEnumerator Run()
        {
            yield return null;   // dá um quadro para o botão mostrar o visual de "selecionado"
            if (boost == null || closing) yield break;
            Layout();
            BuildUseButton();
            while (age < fadeIn && !closing)
            {
                age += Time.unscaledDeltaTime;
                if (group != null) group.alpha = Mathf.Clamp01(age / Mathf.Max(0.01f, fadeIn));
                Animate(age);
                yield return null;
            }
            if (group != null) group.alpha = 1f;

            while (!closing && StillSelected())
            {
                age += Time.unscaledDeltaTime;
                Animate(age);
                if (lastScreen.x != Screen.width || lastScreen.y != Screen.height) Layout();
                yield return null;
            }
            if (!closing && Time.unscaledTime < HoldUntil)
            {
                if (header != null) header.gameObject.SetActive(false);
                if (buttonLayer != null) buttonLayer.gameObject.SetActive(false);
                if (buttonGlow != null) buttonGlow.gameObject.SetActive(false);
                while (!closing && Time.unscaledTime < HoldUntil) yield return null;
            }
            BeginClose();
        }

        bool StillSelected()
        {
            var lm = LevelManager.THIS;
            return boost != null && boost.isActiveAndEnabled && lm != null && lm.activatedBoost == boost &&
                   (lm.gameStatus == GameState.Playing || lm.gameStatus == GameState.Tutorial);
        }

        void BeginClose()
        {
            if (closing) return;
            closing = true;
            // modo "Usar!": se fechou sem usar, devolve o arrastar; se usou, o embaralhamento cuida disso
            if (use != null && !used && LevelManager.THIS != null) LevelManager.THIS.DragBlocked = false;
            if (Current == this) Current = null;
            StartCoroutine(FadeOutAndDestroy());
        }

        IEnumerator FadeOutAndDestroy()
        {
            var t = 0f;
            var from = group != null ? group.alpha : 1f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                if (group != null) group.alpha = Mathf.Lerp(from, 0f, t / Mathf.Max(0.01f, fadeOut));
                yield return null;
            }
            Destroy(gameObject);
        }

        static float EaseOutBack(float x)
        {
            x = Mathf.Clamp01(x);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        void Animate(float t)
        {
            if (icon != null && entry != null)
            {
                var k = iconPopSeconds > 0f ? EaseOutBack(t / iconPopSeconds) : 1f;
                icon.rectTransform.localScale = iconBaseScale * Mathf.Max(0.05f, entry.iconScale) * Mathf.Max(0.01f, k);
            }
            if (rimInner != null && rimInner.gameObject.activeSelf)
            {
                var pulse = 0.9f + 0.1f * Mathf.Sin(t * 5f);
                rimInner.color = new Color(iconRimColor.r, iconRimColor.g, iconRimColor.b, iconRimAlpha * pulse);
                rimOuter.color = new Color(iconRimColor.r, iconRimColor.g, iconRimColor.b, iconRimAlpha * 0.55f * pulse);
            }
            for (var i = 0; i < outlines.Count; i++)
            {
                if (outlines[i] == null) continue;
                var baseAlpha = i == 0 ? outlineAlpha : outlineGlowAlpha;
                var pulse = 0.85f + 0.15f * Mathf.Sin(t * outlinePulseSpeed * 6.2831853f + i);
                var c = outlineColor; c.a = baseAlpha * pulse;
                outlines[i].color = c;
            }
            if (useRect != null)
            {
                // botão "Usar!": entra com um quique e fica pulsando de leve
                var enter = EaseOutBack((t - 0.12f) / 0.35f);
                useRect.localScale = Vector3.one * Mathf.Max(0.01f, enter) * (1f + 0.035f * Mathf.Sin(t * 4.5f));
            }
            if (snapshot != null)
            {
                var k = buttonPopSeconds > 0f ? Mathf.Clamp01(t / buttonPopSeconds) : 1f;
                var pop = 1f + (buttonPopScale - 1f) * Mathf.Sin(k * Mathf.PI);
                snapshot.localScale = Vector3.one * pop;
            }
        }

        // ------------------------------------------------------------------------------------------ layout

        void Layout()
        {
            lastScreen = new Vector2Int(Screen.width, Screen.height);
            BuildDim();
            BuildSnapshot();
        }

        Vector2 ScreenToRoot(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var local);
            return local;
        }

        /// Escurece a tela toda menos o tabuleiro, com o buraco no formato real dele (cantos arredondados e entradas).
        void BuildDim()
        {
            if (dim == null)
            {
                Debug.LogWarning("Power-up intro: o prefab BoosterIntroScreen está na versão antiga (sem o campo Dim). Pare o Play e use Royal Aves > Introdução de power-up > Recriar prefab da tela.");
                return;
            }
            var r = dim.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            dim.raycastTarget = false;
            var c = dim.color; c.a = dimAlpha; dim.color = c;

            if (dimInstance != null) Destroy(dimInstance);
            if (holeTexture != null) Destroy(holeTexture);
            if (use != null)
            {
                dim.material = null;   // modo "Usar!": preto liso por cima de tudo, tabuleiro inclusive
                return;
            }
            if (useBoardCamera && SetupBoardCamera())
            {
                dim.material = null;   // preto liso; o tabuleiro vem por cima, desenhado de verdade
                return;
            }
            dimInstance = dimMaterial != null ? new Material(dimMaterial) : null;
            holeTexture = null;
            if (dimInstance == null) return;
            holeTexture = BuildHoleTexture(out var uvRect);
            if (holeTexture == null) { dimInstance.SetVector("_HoleRect", new Vector4(0, 0, 0, 0)); }
            else
            {
                dimInstance.SetTexture("_HoleTex", holeTexture);
                dimInstance.SetVector("_HoleRect", uvRect);
            }
            dim.material = dimInstance;
        }

        /// Uma câmera extra desenha só o mundo do jogo (tabuleiro, peças, moldura) numa textura transparente, que fica por
        /// cima do fundo escuro. O fundo e a interface (camada UI) ficam de fora, então só o tabuleiro aparece claro.
        bool SetupBoardCamera()
        {
            var main = Camera.main;
            if (main == null || LevelManager.THIS == null) return false;
            ReleaseBoardCamera();
            boardTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32) { name = "Tabuleiro (power-up)" };
            var go = new GameObject("Câmera do tabuleiro (power-up)");
            boardCamera = go.AddComponent<Camera>();
            boardCamera.CopyFrom(main);
            boardCamera.clearFlags = CameraClearFlags.SolidColor;
            boardCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            var ui = LayerMask.NameToLayer("UI");
            boardCamera.cullingMask = ui >= 0 ? main.cullingMask & ~(1 << ui) : main.cullingMask;
            boardCamera.targetTexture = boardTexture;
            boardCamera.depth = main.depth - 1f;
            boardCamera.allowMSAA = false;
            LateUpdate();

            if (boardView == null)
            {
                var view = new GameObject("Tabuleiro (por cima do escuro)", typeof(RectTransform), typeof(RawImage));
                var rect = (RectTransform)view.transform;
                rect.SetParent(root, false);
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                rect.SetSiblingIndex(dim.transform.GetSiblingIndex() + 1);
                boardView = view.GetComponent<RawImage>();
                boardView.raycastTarget = false;
            }
            boardView.texture = boardTexture;
            boardView.color = Color.white;
            return true;
        }

        /// Máscara do tabuleiro: 1 dentro das casas (mais a folga da moldura), 0 fora. uvRect = onde ela cai na tela (0..1).
        Texture2D BuildHoleTexture(out Vector4 uvRect)
        {
            uvRect = Vector4.zero;
            var lm = LevelManager.THIS;
            var cam = Camera.main;
            if (lm == null || lm.field == null || cam == null || !HintMoveFinder.TryGrid(out var grid)) return null;
            var squares = lm.field.GetSquares(true);
            if (squares == null || squares.Count == 0) return null;

            int minC = int.MaxValue, minR = int.MaxValue, maxC = int.MinValue, maxR = int.MinValue;
            var filled = new System.Collections.Generic.HashSet<Vector2Int>();
            foreach (var sq in squares)
            {
                if (sq == null || sq.IsNone()) continue;
                filled.Add(new Vector2Int(sq.col, sq.row));
                minC = Mathf.Min(minC, sq.col); maxC = Mathf.Max(maxC, sq.col);
                minR = Mathf.Min(minR, sq.row); maxR = Mathf.Max(maxR, sq.row);
            }
            if (filled.Count == 0) return null;

            var pad = boardMargin + boardCornerRadius + boardFeather + 0.05f;
            float u0 = minC - 0.5f - pad, u1 = maxC + 0.5f + pad, v0 = minR - 0.5f - pad, v1 = maxR + 0.5f + pad;
            Vector3 W(float u, float v) => grid.origin + grid.ex * u + grid.ey * v;
            var corners = new[] { W(u0, v0), W(u1, v0), W(u0, v1), W(u1, v1) };
            var lo = new Vector2(float.MaxValue, float.MaxValue); var hi = new Vector2(float.MinValue, float.MinValue);
            foreach (var corner in corners) { lo = Vector2.Min(lo, corner); hi = Vector2.Max(hi, corner); }

            var width = Mathf.Clamp(Mathf.CeilToInt((u1 - u0) * Mathf.Max(4, holeResolution)), 8, 1024);
            var height = Mathf.Clamp(Mathf.CeilToInt((v1 - v0) * Mathf.Max(4, holeResolution)), 8, 1024);
            var pixels = new Color32[width * height];
            var exLen2 = Vector3.Dot(grid.ex, grid.ex); var eyLen2 = Vector3.Dot(grid.ey, grid.ey);
            var reach = Mathf.CeilToInt(boardMargin + boardCornerRadius + boardFeather) + 1;
            var shrink = Mathf.Clamp(boardCornerRadius, 0f, 0.45f);
            var feather = Mathf.Max(0.005f, boardFeather);
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    var world = new Vector3(Mathf.Lerp(lo.x, hi.x, (x + 0.5f) / width), Mathf.Lerp(lo.y, hi.y, (y + 0.5f) / height), 0f);
                    var rel = world - grid.origin; rel.z = 0f;
                    var u = Vector3.Dot(rel, grid.ex) / exLen2; var v = Vector3.Dot(rel, grid.ey) / eyLen2;
                    var cu = Mathf.RoundToInt(u); var cv = Mathf.RoundToInt(v);
                    var best = float.MaxValue;
                    for (var dc = -reach; dc <= reach; dc++)
                        for (var dr = -reach; dr <= reach; dr++)
                        {
                            if (!filled.Contains(new Vector2Int(cu + dc, cv + dr))) continue;
                            var dx = Mathf.Max(Mathf.Abs(u - (cu + dc)) - (0.5f - shrink), 0f);
                            var dy = Mathf.Max(Mathf.Abs(v - (cv + dr)) - (0.5f - shrink), 0f);
                            best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dy * dy));
                        }
                    var a = Mathf.Clamp01((boardMargin + shrink - best) / feather + 0.5f);
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            var sLo = cam.WorldToScreenPoint(new Vector3(lo.x, lo.y, 0f));
            var sHi = cam.WorldToScreenPoint(new Vector3(hi.x, hi.y, 0f));
            uvRect = new Vector4(Mathf.Min(sLo.x, sHi.x) / Screen.width, Mathf.Min(sLo.y, sHi.y) / Screen.height,
                Mathf.Max(sLo.x, sHi.x) / Screen.width, Mathf.Max(sLo.y, sHi.y) / Screen.height);
            return texture;
        }

        // ------------------------------------------------------------------------------------------ cópia do botão

        void BuildSnapshot()
        {
            if (buttonLayer == null || boost == null) return;
            for (var i = buttonLayer.childCount - 1; i >= 0; i--) Destroy(buttonLayer.GetChild(i).gameObject);
            snapshot = null;
            outlines.Clear();

            var rootRect = root.rect;
            var holder = new GameObject("Botão em destaque", typeof(RectTransform));
            snapshot = (RectTransform)holder.transform;
            snapshot.SetParent(buttonLayer, false);
            snapshot.anchorMin = snapshot.anchorMax = Vector2.one * 0.5f;
            snapshot.sizeDelta = rootRect.size;

            var bounds = CopyGraphics(boost.transform, snapshot, out var any);
            if (!any) { Destroy(holder); snapshot = null; return; }

            snapshotCenter = bounds.center;
            // Escala em volta do centro do botão: ajusta o pivô e compensa a posição para a cópia não andar.
            var pivot = new Vector2((snapshotCenter.x - rootRect.xMin) / rootRect.width, (snapshotCenter.y - rootRect.yMin) / rootRect.height);
            snapshot.pivot = pivot;
            snapshot.anchoredPosition = new Vector2((pivot.x - 0.5f) * rootRect.width, (pivot.y - 0.5f) * rootRect.height);

            // Área de toque: cancelar a seleção.
            var hit = new GameObject("Toque", typeof(RectTransform), typeof(Image), typeof(Button));
            var hitRect = (RectTransform)hit.transform;
            hitRect.SetParent(snapshot, false);
            hitRect.anchorMin = hitRect.anchorMax = hitRect.pivot = Vector2.one * 0.5f;
            hitRect.anchoredPosition = snapshotCenter;
            hitRect.sizeDelta = bounds.size;
            var hitImage = hit.GetComponent<Image>();
            hitImage.color = new Color(1, 1, 1, 0);
            var button = hit.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            var target = boost;
            button.onClick.AddListener(() => { if (target != null) target.ActivateBoost(); });

            if (buttonGlow != null)
            {
                var gr = (RectTransform)buttonGlow.transform;
                gr.anchorMin = gr.anchorMax = gr.pivot = Vector2.one * 0.5f;
                gr.anchoredPosition = frameSize.sqrMagnitude > 0f ? frameCenter : snapshotCenter;   // centro do quadro, sem a bolinha do número
                var side = (frameSize.sqrMagnitude > 0f ? Mathf.Max(frameSize.x, frameSize.y) : Mathf.Max(bounds.width, bounds.height)) * buttonGlowScale;
                gr.sizeDelta = new Vector2(side, side);
                buttonGlow.Rebuild();
            }
        }

        /// Copia as imagens e textos do botão para esta tela, na mesma posição da tela do jogador.
        Rect CopyGraphics(Transform source, RectTransform target, out bool any)
        {
            any = false;
            var min = new Vector2(float.MaxValue, float.MaxValue); var max = new Vector2(float.MinValue, float.MinValue);
            var rootCanvasScale = canvas != null ? Mathf.Max(0.0001f, canvas.scaleFactor) : 1f;
            foreach (var g in source.GetComponentsInChildren<Graphic>(true))
            {
                if (g == null || !g.enabled || g.color.a < 0.01f) continue;
                var selectedOverlay = g.transform.name == "Selecionado" || g.transform.parent != null && g.transform.parent.name == "Selecionado";
                if (!g.gameObject.activeInHierarchy && !selectedOverlay) continue;
                var srcCanvas = g.canvas != null ? g.canvas.rootCanvas : null;
                var cam = srcCanvas != null && srcCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? srcCanvas.worldCamera : null;
                var corners = new Vector3[4];
                g.rectTransform.GetWorldCorners(corners);
                var lo = new Vector2(float.MaxValue, float.MaxValue); var hi = new Vector2(float.MinValue, float.MinValue);
                foreach (var corner in corners)
                {
                    var p = ScreenToRoot(RectTransformUtility.WorldToScreenPoint(cam, corner));
                    lo = Vector2.Min(lo, p); hi = Vector2.Max(hi, p);
                }
                var size = hi - lo;
                if (size.x < 0.5f || size.y < 0.5f) continue;

                // Contorno luminoso: duas cópias douradas e aditivas do quadro, um pouco maiores, ficam atrás do botão
                // e só a borda aparece (quase uma linha em volta do quadro, como no Royal Match).
                if (g.transform == source && g is Image frameImage)
                {
                    frameCenter = (lo + hi) * 0.5f;
                    frameSize = size;
                }
                if (useOutline && g.transform == source && g is Image frame && buttonGlow != null && buttonGlow.additive != null)
                {
                    foreach (var layerPad in new[] { outlineGlowPad, outlinePad })
                    {
                        var outline = new GameObject("Contorno", typeof(RectTransform), typeof(Image));
                        var outlineRect = (RectTransform)outline.transform;
                        outlineRect.SetParent(target, false);
                        outlineRect.anchorMin = outlineRect.anchorMax = outlineRect.pivot = Vector2.one * 0.5f;
                        outlineRect.anchoredPosition = (lo + hi) * 0.5f;
                        outlineRect.sizeDelta = size * (1f + layerPad * 2f);
                        var outlineImage = outline.GetComponent<Image>();
                        outlineImage.sprite = frame.sprite;
                        outlineImage.type = frame.type;
                        outlineImage.preserveAspect = frame.preserveAspect;
                        outlineImage.material = buttonGlow.additive;
                        outlineImage.raycastTarget = false;
                        outlineImage.color = new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0f);
                        outlines.Add(outlineImage);
                    }
                    outlines.Reverse();   // [0] = linha fina, [1] = brilho largo
                }

                var go = new GameObject(g.name, typeof(RectTransform));
                var rect = (RectTransform)go.transform;
                rect.SetParent(target, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
                rect.anchoredPosition = (lo + hi) * 0.5f;
                rect.sizeDelta = size;

                if (g is Image img)
                {
                    var copy = go.AddComponent<Image>();
                    copy.sprite = img.sprite;
                    copy.type = img.type;
                    copy.preserveAspect = img.preserveAspect;
                    copy.fillMethod = img.fillMethod;
                    copy.fillAmount = img.fillAmount;
                    copy.fillCenter = img.fillCenter;
                    copy.color = img.color;
                    if (img.material != null && img.material != Canvas.GetDefaultCanvasMaterial()) copy.material = img.material;
                    copy.raycastTarget = false;
                }
                else if (g is TMP_Text tmp)
                {
                    var copy = go.AddComponent<TextMeshProUGUI>();
                    copy.font = tmp.font;
                    copy.fontSharedMaterial = tmp.fontSharedMaterial;
                    copy.text = tmp.text;
                    var ratio = tmp.rectTransform.rect.width > 0.01f ? size.x / tmp.rectTransform.rect.width : rootCanvasScale;
                    copy.enableAutoSizing = tmp.enableAutoSizing;
                    copy.fontSize = tmp.fontSize * ratio;
                    copy.fontSizeMin = tmp.fontSizeMin * ratio;
                    copy.fontSizeMax = tmp.fontSizeMax * ratio;
                    copy.fontStyle = tmp.fontStyle;
                    copy.color = tmp.color;
                    copy.alignment = tmp.alignment;
                    copy.textWrappingMode = tmp.textWrappingMode;
                    copy.overflowMode = tmp.overflowMode;
                    copy.richText = tmp.richText;
                    copy.raycastTarget = false;
                }
                else { Destroy(go); continue; }

                any = true;
                min = Vector2.Min(min, lo); max = Vector2.Max(max, hi);
            }
            return any ? Rect.MinMaxRect(min.x, min.y, max.x, max.y) : default;
        }
    }
}
