// Tela "Novo item desbloqueado!" Royal Postman. É o prefab Resources/UI/ElementIntroScreen: abra o prefab e edite
// qualquer pedaço (fundo, imagem do nome, subtítulo, brilho dourado, ícone, placa e texto). O conteúdo de cada
// introdução (nome, ícone, texto) vem do ElementIntroCatalog; os textos usam LocalizedTextTMP / LocalizedImage, então
// seguem o idioma global do jogo (português e inglês).
using System;
using System.Collections;
using SweetSugar.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public sealed class ElementIntroScreen : MonoBehaviour
    {
        public const string PrefabPath = "UI/ElementIntroScreen";

        public enum GlowMode
        {
            [Tooltip("Gira e pulsa devagar atrás do ícone.")] Steady,
            [Tooltip("Anel que nasce pequeno, cresce e some, em loop.")] Ring,
            [Tooltip("Um clarão só, na hora em que a tela abre.")] Burst,
            [Tooltip("Brilho que acende e apaga.")] Twinkle,
        }

        [Serializable]
        public class GlowLayer
        {
            public string name;
            [Tooltip("Imagem da camada (cor e transparência são animadas).")] public Image image;
            [Tooltip("Ou só um grupo (ex.: os raios), que gira e pulsa sem mudar a cor dos filhos.")] public RectTransform rect;
            public GlowMode mode = GlowMode.Steady;
            [Tooltip("Graus por segundo (negativo gira ao contrário).")] public float spinDegreesPerSecond = 12f;
            [Tooltip("Quanto a escala pulsa (0.05 = 5%).")] public float pulseScale = 0.04f;
            [Tooltip("Ciclos por segundo.")] public float speed = 0.6f;
            [Range(0f, 1f)] public float alpha = 0.6f;
            [Range(0f, 1f)] public float alphaPulse = 0.15f;
            public float phase;
            [HideInInspector] public Vector3 baseScale = Vector3.one;
        }

        [Header("Partes da tela")]
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform content;
        [SerializeField] Image dim;
        [SerializeField] Image nameImage;
        [SerializeField] LocalizedImage nameLocalized;
        [Tooltip("Texto mostrado no lugar da imagem de nome quando o elemento ainda não tem uma.")]
        [SerializeField] TMP_Text nameFallback;
        [SerializeField] LocalizedTextTMP nameFallbackText;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text instruction;
        [SerializeField] LocalizedTextTMP instructionText;

        [Header("Brilho dourado atrás do item")]
        [SerializeField] GlowLayer[] glow = new GlowLayer[0];

        [Header("Tempo (segundos)")]
        [SerializeField] float fadeIn = 0.18f;
        [Tooltip("0 = só fecha quando o jogador toca na tela; acima de 0 fecha sozinho depois desse tempo.")]
        [SerializeField] float autoCloseSeconds = 0f;
        [Tooltip("Tempo antes de aceitar o toque, para não fechar sem querer.")]
        [SerializeField] float tapDelay = 0.35f;
        [SerializeField] float fadeOut = 0.35f;
        [Tooltip("Quanto o ícone cresce ao entrar (pop).")]
        [SerializeField] float iconPopSeconds = 0.45f;

        [Header("Tamanho")]
        [Tooltip("Referência do Canvas Scaler; o conteúdo é reduzido para caber em telas estreitas.")]
        [SerializeField] Vector2 designSize = new Vector2(1080, 1920);

        LevelManager manager;
        SweetSugar.Scripts.Level.LevelData data;
        bool paused, closing;
        float age;
        Vector3 iconScale = Vector3.one;

        static RectTransform Target(GlowLayer layer) => layer.rect != null ? layer.rect : layer.image != null ? layer.image.rectTransform : null;

        public static bool IsOpen => FindFirstObjectByType<ElementIntroScreen>() != null;

        /// Abre a tela com o conteúdo da entrada. Retorna false quando o prefab não existe (usa-se a tela antiga).
        public static bool Show(ElementIntroCatalog.Entry entry, Sprite iconSprite)
        {
            var prefab = Resources.Load<ElementIntroScreen>(PrefabPath);
            if (prefab == null || entry == null) return false;
            if (IsOpen || FindFirstObjectByType<ElementIntroPrototype>() != null) return true;
            var view = Instantiate(prefab);
            view.name = "Apresentação de elemento - " + entry.DisplayName;
            view.Fill(entry, iconSprite);
            view.Begin();
            return true;
        }

        /// Aplica o conteúdo do catálogo nas partes da tela (também usado pelo preview do editor).
        public void Fill(ElementIntroCatalog.Entry entry, Sprite iconSprite)
        {
            if (icon != null)
            {
                icon.sprite = iconSprite != null ? iconSprite : entry.iconOverride;
                icon.enabled = icon.sprite != null;
            }
            var hasName = entry.nameSpritePt != null || entry.nameSpriteEn != null;
            if (nameLocalized != null)
            {
                nameLocalized.SetSprite(GameLanguage.Portuguese, entry.nameSpritePt ?? entry.nameSpriteEn);
                nameLocalized.SetSprite(GameLanguage.English, entry.nameSpriteEn ?? entry.nameSpritePt);
            }
            // A imagem do nome cabe na caixa sem esticar: cada nome tem uma proporção diferente.
            if (nameImage != null) { nameImage.preserveAspect = true; nameImage.gameObject.SetActive(hasName); }
            if (nameFallback != null) nameFallback.gameObject.SetActive(!hasName);
            if (!hasName && nameFallbackText != null) nameFallbackText.SetTexts(entry.titlePt, entry.titleEn);
            if (instructionText != null) instructionText.SetTexts(entry.instructionPt, entry.instructionEn);
        }

        void Begin()
        {
            foreach (var layer in glow)
                if (layer != null && Target(layer) != null) layer.baseScale = Target(layer).localScale;
            if (icon != null) iconScale = icon.rectTransform.localScale;

            if (group != null) { group.interactable = true; group.blocksRaycasts = true; group.alpha = 0; }
            FitToScreen();
            if (dim != null)
            {
                if (!dim.TryGetComponent<Button>(out var button)) button = dim.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(Close);
            }

            manager = LevelManager.THIS;
            data = manager != null ? manager.levelData : null;
            if (manager != null && (manager.gameStatus == GameState.Playing || manager.gameStatus == GameState.Tutorial))
            {
                paused = true;
                manager.gameStatus = GameState.Pause;
            }
            StartCoroutine(Run());
        }

        /// Reduz o conteúdo para caber em telas mais estreitas que a referência.
        public void FitToScreen()
        {
            Canvas.ForceUpdateCanvases();
            if (content == null) return;
            var area = (RectTransform)transform;
            var fit = Mathf.Min(1f, area.rect.width / designSize.x, area.rect.height / designSize.y);
            content.localScale = Vector3.one * Mathf.Max(0.3f, fit);
        }

        /// Fecha a tela (toque do jogador).
        public void Close()
        {
            if (age >= tapDelay) closing = true;
        }

        /// Mostra a tela parada, na pose do instante t, para editar no Game view fora do Play.
        public void PreviewPose(float t)
        {
            CaptureBase();
            if (group != null) group.alpha = 1f;
            FitToScreen();
            Animate(t);
        }

        /// Guarda as escalas de repouso (base da animação). Chamar uma vez, antes de animar com Animate(t).
        public void CaptureBase()
        {
            foreach (var layer in glow)
                if (layer != null && Target(layer) != null) layer.baseScale = Target(layer).localScale;
            if (icon != null) iconScale = icon.rectTransform.localScale;
        }

        IEnumerator Run()
        {
            for (float t = 0; t < fadeIn; t += Time.unscaledDeltaTime)
            {
                group.alpha = t / fadeIn;
                yield return null;
            }
            group.alpha = 1;
            var waited = 0f;
            while (!closing && (autoCloseSeconds <= 0f || waited < autoCloseSeconds))
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            for (float t = 0; t < fadeOut; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1 - t / fadeOut;
                yield return null;
            }
            Destroy(gameObject);
        }

        void Update()
        {
            age += Time.unscaledDeltaTime;
            Animate(age);
            if (manager != null && (manager.levelData != data || manager.gameStatus != GameState.Pause))
                Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (paused && manager != null && manager.levelData == data && manager.gameStatus == GameState.Pause)
                manager.gameStatus = GameState.Playing;
        }

        /// Pose da animação no instante t (segundos desde que a tela abriu). Público para o preview do editor.
        public void Animate(float t)
        {
            if (icon != null && iconPopSeconds > 0f)
            {
                var u = Mathf.Clamp01(t / iconPopSeconds);
                // Entra com um leve excesso e assenta (ease-out-back).
                const float c1 = 1.70158f, c3 = c1 + 1f;
                var pop = 1f + c3 * Mathf.Pow(u - 1f, 3f) + c1 * Mathf.Pow(u - 1f, 2f);
                icon.rectTransform.localScale = iconScale * Mathf.LerpUnclamped(0.5f, 1f, pop);
            }
            foreach (var layer in glow)
            {
                if (layer == null || Target(layer) == null) continue;
                var rect = Target(layer);
                var phase = layer.phase * Mathf.PI * 2f;
                var wave = Mathf.Sin(t * layer.speed * Mathf.PI * 2f + phase);
                float scale = 1f, alpha = layer.alpha;
                switch (layer.mode)
                {
                    case GlowMode.Steady:
                        scale = 1f + layer.pulseScale * wave;
                        alpha = layer.alpha * (1f + layer.alphaPulse * wave);
                        rect.localRotation = Quaternion.Euler(0f, 0f, layer.spinDegreesPerSecond * t);
                        break;
                    case GlowMode.Ring:
                        var u = Mathf.Repeat(t * layer.speed + layer.phase, 1f);
                        scale = Mathf.Lerp(0.55f, 1.25f, 1f - (1f - u) * (1f - u));
                        alpha = layer.alpha * Mathf.Pow(1f - u, 1.5f) * Mathf.Clamp01(u * 8f);
                        break;
                    case GlowMode.Burst:
                        var b = Mathf.Clamp01(t / 0.6f);
                        scale = Mathf.Lerp(0.3f, 1.2f, 1f - (1f - b) * (1f - b) * (1f - b));
                        alpha = layer.alpha * (1f - b) * Mathf.Clamp01(t * 20f);
                        break;
                    case GlowMode.Twinkle:
                        var tw = Mathf.Max(0f, wave);
                        scale = 0.6f + 0.5f * tw;
                        alpha = layer.alpha * tw;
                        rect.localRotation = Quaternion.Euler(0f, 0f, layer.spinDegreesPerSecond * t);
                        break;
                }
                rect.localScale = layer.baseScale * scale;
                if (layer.image == null) continue;   // grupo: só gira e pulsa
                var color = layer.image.color;
                color.a = Mathf.Clamp01(alpha);
                layer.image.color = color;
            }
        }
    }
}
