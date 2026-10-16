// First screen of the app: the art of the active season (name and subtitle included) with a progress bar while the game
// scene (with the agency lobby) loads in the background. The season comes from SeasonConfig.
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public class LoadingScreen : MonoBehaviour
    {
        [SerializeField] string sceneName = "game";
        [Tooltip("Tempo mínimo na tela, para a arte não piscar em aparelhos rápidos.")]
        [SerializeField, Min(0)] float minimumSeconds = 2.5f;
        [Tooltip("A barra de progresso mágica (prefab Barra de progresso). Tem prioridade sobre Bar Fill.")]
        [SerializeField] MagicMail.MagicProgressBar progressBar;
        [Tooltip("Opcional: barra simples, só se não houver a barra mágica.")]
        [SerializeField] RectTransform barFill;
        [SerializeField] TextMeshProUGUI label;

        [Header("Temporada (preenchido pelo Editor de temporadas)")]
        [SerializeField] Image art;
        [Tooltip("Logo separado por cima da arte (a temporada pode desligar).")]
        [SerializeField] GameObject logo;
        [Tooltip("Sombra na base da arte, atrás do nome e da barra.")]
        [SerializeField] Image scrim;
        [SerializeField] TextMeshProUGUI seasonTitle;
        [SerializeField] TextMeshProUGUI seasonSubtitle;

        IEnumerator Start()
        {
            var config = SeasonConfig.Instance;
            if (config != null) ApplySeason(config.Active);

            var load = SceneManager.LoadSceneAsync(sceneName);
            load.allowSceneActivation = false;
            var start = Time.unscaledTime;
            while (true)
            {
                var elapsed = Time.unscaledTime - start;
                // Unity stops reporting at 0.9 until the scene is allowed to activate.
                var progress = Mathf.Min(Mathf.Clamp01(load.progress / 0.9f), elapsed / Mathf.Max(0.01f, minimumSeconds));
                if (progressBar != null) progressBar.SetProgress(progress);
                else if (barFill != null) barFill.anchorMax = new Vector2(progress, 1);
                if (label != null) label.text = "Carregando" + new string('.', 1 + (int)(elapsed * 2) % 3);
                if (load.progress >= 0.9f && elapsed >= minimumSeconds) break;
                yield return null;
            }
            if (progressBar != null) progressBar.SetProgress(1f);
            else if (barFill != null) barFill.anchorMax = Vector2.one;
            // Let the bar reach the end and show it for a moment before the game scene takes over.
            if (progressBar != null) yield return new WaitForSecondsRealtime(0.45f);
            load.allowSceneActivation = true;
        }

        /// Dresses the screen with a season: its art (covering the screen whatever its proportion), its name and subtitle
        /// (hidden when empty) and the shade behind them. The area editor also calls this to keep the Loading scene in step.
        public void ApplySeason(Season season)
        {
            if (season == null) return;
            // What the screen really shows: the base theme with whatever this season replaces (its art, its positions).
            var config = SeasonConfig.Instance;
            var look = config != null ? config.ResolveLoading(season) : new LoadingLook
            {
                art = season.loadingArt, zoom = season.artZoom, offset = season.artOffset, showLogo = season.showLogo,
                bottomShade = season.bottomShade, title = season.titleLayout, subtitle = season.subtitleLayout,
                bar = season.barLayout, label = season.labelLayout,
            };

            if (art != null && look.art != null)
            {
                art.sprite = look.art;
                // The art is sized here (FitArt), not by an AspectRatioFitter, which would overrule the zoom and the offset.
                var fitter = art.GetComponent<AspectRatioFitter>();
                if (fitter != null)
                {
                    if (Application.isPlaying) Destroy(fitter); else DestroyImmediate(fitter);
                }
                artZoom = Mathf.Max(0.1f, look.zoom);
                artOffset = look.offset;
                seasonApplied = true;
                FitArt();
            }
            if (logo != null) logo.SetActive(look.showLogo);
            SetText(seasonTitle, season.title);
            SetText(seasonSubtitle, season.subtitle);
            if (scrim != null) scrim.gameObject.SetActive(look.bottomShade);

            // Where each element stands and how big it is.
            Place(seasonTitle != null ? seasonTitle.rectTransform : null, look.title);
            Place(seasonSubtitle != null ? seasonSubtitle.rectTransform : null, look.subtitle);
            Place(progressBar != null ? (RectTransform)progressBar.transform : null, look.bar);
            Place(label != null ? label.rectTransform : null, look.label);

            // The props of the season (a SeasonProps on this screen shows them at run time; here the editor also asks for them).
            var props = GetComponent<SeasonProps>();
            if (props != null && Application.isPlaying) props.Apply(season);
        }

        // Anchored to the middle of the bottom edge, so the numbers mean the same on any screen shape.
        static void Place(RectTransform rect, ElementLayout layout)
        {
            if (rect == null || layout == null) return;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = layout.position;
            rect.localScale = Vector3.one * Mathf.Max(0.1f, layout.scale);
        }

        float artZoom = 1f;
        Vector2 artOffset;
        bool seasonApplied;   // until a season is applied the art keeps the placement the scene has

        // Covers the whole screen with the art, whatever its proportion (the excess is cropped), then zooms and shifts it.
        // Done every frame so a different screen shape never leaves bars.
        void FitArt()
        {
            if (art == null || art.sprite == null) return;
            var screen = ((RectTransform)transform).rect.size;
            if (screen.x <= 0f || screen.y <= 0f) return;
            var aspect = art.sprite.rect.width / art.sprite.rect.height;
            var cover = screen.x / screen.y > aspect ? new Vector2(screen.x, screen.x / aspect) : new Vector2(screen.y * aspect, screen.y);
            var rect = art.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = cover;
            rect.anchoredPosition = artOffset;
            rect.localScale = Vector3.one * artZoom;
        }

        void LateUpdate()
        {
            if (seasonApplied) FitArt();
        }

        static void SetText(TextMeshProUGUI target, string value)
        {
            if (target == null) return;
            target.text = value ?? "";
            target.gameObject.SetActive(!string.IsNullOrWhiteSpace(value));
        }
    }
}
