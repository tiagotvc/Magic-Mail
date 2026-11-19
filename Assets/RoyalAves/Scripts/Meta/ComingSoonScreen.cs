// Tela provisória "Coming soon" para as opções do menu inicial que ainda não têm tela própria.
//
// - Uma tela só, reaproveitada por todas as opções: quem abre passa o título (Show).
// - Mostra um fundo em tela cheia, o título da opção, o texto "Coming soon" e um botão de voltar.
// - Fundo, textos e cores são editáveis no Inspector (no prefab Resources/UI/ComingSoonScreen).
// - A HomeScreen cria a tela sozinha na primeira vez que uma aba sem tela é tocada (Create): usa o prefab e,
//   se ele ainda não existir, monta uma versão simples por código (BuildDefault), sem arte.
//
// Prefab: menu Royal Aves > Tela inicial > Em breve (criado sozinho pelo ComingSoonScreenBuilder).
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Royal Aves/Coming Soon Screen")]
    public class ComingSoonScreen : MonoBehaviour
    {
        /// Caminho do prefab dentro de uma pasta Resources.
        public const string ResourcePath = "UI/ComingSoonScreen";
        public const string ObjectName = "ComingSoon";

        [Header("Fundo")]
        [Tooltip("Image de tela cheia que recebe o fundo.")]
        [SerializeField] Image background;
        [Tooltip("Arte do fundo. Troque aqui. Vazio = só a cor abaixo.")]
        [SerializeField] Sprite backgroundSprite;
        [Tooltip("Cor do fundo (multiplica a arte; sem arte, é a cor chapada).")]
        [SerializeField] Color backgroundColor = Color.white;
        [Tooltip("Cor usada quando não há arte de fundo.")]
        [SerializeField] Color emptyBackgroundColor = new Color(0.33f, 0.2f, 0.5f, 1f);
        [Tooltip("Película por cima do fundo, para o texto ficar legível.")]
        [SerializeField] Image dim;
        [SerializeField] Color dimColor = new Color(0.12f, 0.04f, 0.24f, 0.35f);

        [Header("Título da opção")]
        [SerializeField] TMP_Text titleText;
        [SerializeField] Color titleColor = new Color(1f, 0.87f, 0.35f, 1f);

        [Header("Mensagem")]
        [SerializeField] TMP_Text messageText;
        [SerializeField] string messagePt = "Coming soon";
        [SerializeField] string messageEn = "Coming soon";
        [SerializeField] Color messageColor = Color.white;

        [Header("Voltar")]
        [SerializeField] Button backButton;
        [SerializeField] TMP_Text backLabel;
        [SerializeField] string backPt = "Voltar";
        [SerializeField] string backEn = "Back";
        [SerializeField] Color backLabelColor = Color.white;

        /// Disparado quando o jogador toca em Voltar (a HomeScreen volta para a aba inicial).
        public event Action Closed;

        string titlePt = "", titleEn = "";
        GameLanguage shownLanguage;
        bool wired;

        public bool IsOpen => gameObject.activeSelf;

        void Awake() => Wire();

        void Wire()
        {
            // Montada por código, o Awake roda antes de o botão existir: tenta de novo no Show.
            if (wired || backButton == null) return;
            wired = true;
            backButton.onClick.AddListener(Close);
        }

        void OnEnable() => ApplyLook();

        void Update()
        {
            if (LocalizedImage.Current != shownLanguage) ApplyTexts();
        }

#if UNITY_EDITOR
        // Mudou algo no Inspector: mostra na hora (fora do Play também). Adiado um passo porque o Unity não deixa
        // mexer em outros componentes de dentro do OnValidate.
        void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () => { if (this != null) ApplyColors(); };
        }
#endif

        /// Abre a tela com o título da opção (em português e em inglês; vazio = sem título).
        public void Show(string optionTitlePt, string optionTitleEn)
        {
            Wire();
            titlePt = optionTitlePt ?? "";
            titleEn = string.IsNullOrEmpty(optionTitleEn) ? titlePt : optionTitleEn;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            ApplyLook();
        }

        /// Fecha sem avisar ninguém (quem chama já sabe para onde ir).
        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        /// Botão Voltar: fecha e avisa (Closed).
        public void Close()
        {
            Hide();
            Closed?.Invoke();
        }

        /// Reaplica fundo, cores e textos a partir dos campos do Inspector.
        public void ApplyLook()
        {
            ApplyColors();
            ApplyTexts();
        }

        void ApplyColors()
        {
            if (background != null)
            {
                background.sprite = backgroundSprite;
                background.color = backgroundSprite != null ? backgroundColor : emptyBackgroundColor;
                // A arte cobre a tela inteira sem deformar (corta a sobra dos lados ou de cima/baixo).
                var fitter = background.GetComponent<AspectRatioFitter>();
                if (fitter != null && backgroundSprite != null && backgroundSprite.rect.height > 0f)
                    fitter.aspectRatio = backgroundSprite.rect.width / backgroundSprite.rect.height;
            }
            if (dim != null) dim.color = dimColor;
            if (titleText != null) titleText.color = titleColor;
            if (messageText != null) messageText.color = messageColor;
            if (backLabel != null) backLabel.color = backLabelColor;
        }

        void ApplyTexts()
        {
            shownLanguage = LocalizedImage.Current;
            var pt = shownLanguage == GameLanguage.Portuguese;
            if (titleText != null)
            {
                var title = pt ? titlePt : titleEn;
                titleText.text = title;
                titleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
            }
            if (messageText != null) messageText.text = pt ? messagePt : messageEn;
            if (backLabel != null) backLabel.text = pt ? backPt : backEn;
        }

        // ------------------------------------------------------------------------------------------ criação

        /// Cria a tela (fechada) dentro de "parent": instancia o prefab de Resources ou, se ele não existir,
        /// monta a versão simples por código. "font" só é usado nessa versão simples.
        public static ComingSoonScreen Create(Transform parent, TMP_FontAsset font = null)
        {
            ComingSoonScreen screen;
            var prefab = Resources.Load<ComingSoonScreen>(ResourcePath);
            if (prefab != null)
            {
                screen = Instantiate(prefab, parent, false);
                screen.name = ObjectName;
            }
            else
            {
                Debug.LogWarning("Royal Aves: prefab Resources/" + ResourcePath + " não encontrado; a tela \"Coming soon\" foi montada por código, sem arte. Use o menu Royal Aves > Tela inicial > Em breve > Criar prefab da tela.");
                screen = BuildDefault(parent, font);
            }
            var rect = (RectTransform)screen.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            screen.gameObject.SetActive(false);
            return screen;
        }

        /// Monta a hierarquia padrão da tela (medidas para o CanvasGlobal, 1536 de largura de referência), sem arte.
        /// O ComingSoonScreenBuilder usa este mesmo método e depois coloca a arte e a fonte para salvar o prefab.
        public static ComingSoonScreen BuildDefault(Transform parent, TMP_FontAsset font = null)
        {
            var layer = parent != null ? parent.gameObject.layer : LayerMask.NameToLayer("UI");
            if (layer < 0) layer = 0;

            var root = NewRect(ObjectName, parent, layer);
            Stretch(root);
            var screen = root.gameObject.AddComponent<ComingSoonScreen>();

            // Fundo: bloqueia os toques no que está atrás (mapa, Play).
            var backgroundRect = NewRect("Background", root, layer);
            Stretch(backgroundRect);
            screen.background = backgroundRect.gameObject.AddComponent<Image>();
            screen.background.raycastTarget = true;
            var fitter = backgroundRect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 9f / 16f;

            var dimRect = NewRect("Dim", root, layer);
            Stretch(dimRect);
            screen.dim = dimRect.gameObject.AddComponent<Image>();
            screen.dim.raycastTarget = false;

            screen.titleText = NewText("Title", root, layer, font, "", 130f, new Vector2(1380f, 190f), new Vector2(0f, 300f));
            screen.messageText = NewText("Message", root, layer, font, screen.messageEn, 150f, new Vector2(1380f, 230f), new Vector2(0f, 90f));

            var buttonRect = NewRect("BackButton", root, layer);
            buttonRect.sizeDelta = new Vector2(520f, 196f);
            buttonRect.anchoredPosition = new Vector2(0f, -230f);
            var face = buttonRect.gameObject.AddComponent<Image>();
            face.color = new Color(0.16f, 0.36f, 1f, 1f); // sem arte: azul chapado (o builder troca pelo botão azul)
            screen.backButton = buttonRect.gameObject.AddComponent<Button>();
            screen.backButton.transition = Selectable.Transition.None;
            screen.backLabel = NewText("Label", buttonRect, layer, font, screen.backEn, 84f, new Vector2(440f, 150f), new Vector2(0f, 4f));

            screen.ApplyColors();
            return screen;
        }

        static RectTransform NewRect(string name, Transform parent, int layer)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = layer };
            var rect = (RectTransform)go.transform;
            if (parent != null) rect.SetParent(parent, false);
            return rect;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static TextMeshProUGUI NewText(string name, Transform parent, int layer, TMP_FontAsset font, string value, float size, Vector2 bounds, Vector2 position)
        {
            var rect = NewRect(name, parent, layer);
            rect.sizeDelta = bounds;
            rect.anchoredPosition = position;
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = value;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            // Encolhe sozinho se o título for comprido.
            text.enableAutoSizing = true;
            text.fontSizeMin = size * 0.4f;
            text.fontSizeMax = size;
            text.fontSize = size;
            return text;
        }
    }
}
