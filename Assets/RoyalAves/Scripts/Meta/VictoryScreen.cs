// Level complete screen of Correio Mágico, as in the web prototype: the envelope drops in, wobbles, opens frame by
// frame and a star flies out of it. Every win shows it; only the first win of a level adds the star to the lobby.
// Timeline in seconds after the card appears, from royal-aves Data/animations.json (envelopeOpen).
using System;
using System.Collections;
using RoyalAves.Characters;
using SweetSugar.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public class VictoryScreen : MonoBehaviour
    {
        [SerializeField] RectTransform card;
        [SerializeField] TextMeshProUGUI levelTag;
        [SerializeField] RectTransform title;
        [SerializeField] RectTransform envelope;
        [SerializeField] Image envelopeImage;
        [Tooltip("Os 5 quadros do envelope abrindo, em ordem.")]
        [SerializeField] Sprite[] envelopeFrames;
        [SerializeField] CanvasGroup burst;
        [SerializeField] RectTransform star;
        [SerializeField] CanvasGroup starGroup;
        [SerializeField] RectTransform rays;
        [SerializeField] CanvasGroup raysGroup;
        [SerializeField] CanvasGroup sparkles;
        [SerializeField] TextMeshProUGUI rewardLine;
        [SerializeField] TextMeshProUGUI rewardTotal;
        [SerializeField] Button continueButton;
        [SerializeField] Button replayButton;
        [Tooltip("A gerente: aliviada se o nível foi vencido com ela nervosa, comemorando nos outros casos.")]
        [SerializeField] ManagerFace manager;
        [Tooltip("Quanto a estrela sobe de dentro do envelope até o lugar dela.")]
        [SerializeField] float starTravel = 290;
        [SerializeField] float envelopeDrop = 140;

        static readonly float[] FrameTimes = { 0f, 1f, 1.1f, 1.2f, 1.55f };
        const float ContinueAt = 1.5f;

        Action onContinue;
        Action onReplay;
        Vector2 envelopeRest;
        Vector2 starRest;

        void Awake()
        {
            envelopeRest = envelope.anchoredPosition;
            starRest = star.anchoredPosition;
            // "+1 estrela e 1600 moedas" is longer than the old line: shrink it to fit instead of overflowing the card.
            rewardLine.enableAutoSizing = true;
            rewardLine.fontSizeMax = rewardLine.fontSize;
            rewardLine.fontSizeMin = rewardLine.fontSize * 0.6f;
            continueButton.onClick.AddListener(() => Choose(onContinue));
            replayButton.onClick.AddListener(() => Choose(onReplay));
        }

        public void Show(int level, bool firstWin, int coins, int starsAvailable, bool wonTense, Action continueAction, Action replayAction)
        {
            onContinue = continueAction;
            onReplay = replayAction;
            if (manager != null) manager.Mood = wonTense ? ManagerMood.Relieved : ManagerMood.Victory;
            levelTag.text = $"NÍVEL {level}";
            var coinText = CoinFormat.Short(coins) + " moedas";
            rewardLine.text = firstWin
                ? (coins > 0 ? "+1 estrela e " + coinText : "+1 estrela")
                : (coins > 0 ? "+" + coinText : "Estrela já conquistada");
            rewardTotal.text = $"{starsAvailable} disponíveis para restauração";
            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(Animate());
        }

        void Choose(Action action)
        {
            if (SoundBase.Instance != null) SoundBase.Instance.PlayOneShot(SoundBase.Instance.click);
            continueButton.interactable = replayButton.interactable = false;
            action?.Invoke();
        }

        IEnumerator Animate()
        {
            continueButton.interactable = false;
            replayButton.interactable = true;
            var frame = -1;
            for (var t = 0f; ; t += Time.unscaledDeltaTime)
            {
                card.localScale = Vector3.one * Mathf.LerpUnclamped(0.82f, 1f, BackOut(Mathf.Clamp01(t / 0.48f)));
                title.localScale = Vector3.one * BackOut(Mathf.Clamp01((t - 0.2f) / 0.5f));

                // Envelope: drops in at 0.3 s, wobbles twice from 0.75 s, then opens frame by frame.
                var drop = Mathf.Clamp01((t - 0.3f) / 0.55f);
                envelopeImage.color = new Color(1, 1, 1, Mathf.Clamp01(drop * 4));
                envelope.anchoredPosition = envelopeRest + new Vector2(0, (1 - BackOut(drop)) * envelopeDrop);
                var wobble = t - 0.75f;
                var wobbling = wobble > 0 && wobble < 0.56f;
                var swing = wobbling ? Mathf.Sin(wobble / 0.28f * 2 * Mathf.PI) : 0;
                envelope.localRotation = Quaternion.Euler(0, 0, swing * 4);
                envelope.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, BackOut(drop)) * (1 + 0.03f * Mathf.Abs(swing));
                var current = FrameAt(t);
                if (current != frame && envelopeFrames.Length > 0)
                {
                    frame = current;
                    envelopeImage.sprite = envelopeFrames[Mathf.Min(frame, envelopeFrames.Length - 1)];
                }

                // Light burst at 1.18 s and the star flying out at 1.22 s.
                var b = (t - 1.18f) / 0.7f;
                burst.alpha = b <= 0 || b >= 1 ? 0 : b < 0.25f ? b / 0.25f : 1 - (b - 0.25f) / 0.75f;
                burst.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1.35f, Mathf.Clamp01(b));
                var s = Mathf.Clamp01((t - 1.22f) / 0.85f);
                starGroup.alpha = t < 1.22f ? 0 : Mathf.Clamp01(s / 0.14f);
                var rise = Mathf.Clamp01(s / 0.7f);
                var settle = Mathf.Clamp01((s - 0.7f) / 0.3f);
                var eased = 1 - (1 - rise) * (1 - rise);
                var offset = s < 0.7f ? Mathf.Lerp(-starTravel, 10, eased) : Mathf.Lerp(10, 0, settle);
                star.anchoredPosition = starRest + new Vector2(0, offset);
                var scale = s < 0.7f ? Mathf.Lerp(0.18f, 1.14f, eased) : Mathf.Lerp(1.14f, 1f, settle);
                if (t > 2.1f) scale *= 1 + 0.045f * (0.5f + 0.5f * Mathf.Sin((t - 2.1f) / 1.2f * Mathf.PI));
                star.localScale = Vector3.one * scale;
                star.localRotation = Quaternion.Euler(0, 0, s < 0.7f ? Mathf.Lerp(30, -8, rise) : Mathf.Lerp(-8, 0, settle));

                raysGroup.alpha = Mathf.Clamp01((t - 1.25f) / 0.5f);
                rays.localRotation = Quaternion.Euler(0, 0, -t / 7f * 360f);
                sparkles.alpha = t < 1.45f ? 0 : t < 1.85f ? (t - 1.45f) / 0.4f
                    : 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin((t - 1.85f) / 1.1f * Mathf.PI));

                // "Continuar" unlocks once the star has left the envelope.
                if (t >= ContinueAt && !continueButton.interactable && replayButton.interactable) continueButton.interactable = true;
                yield return null;
            }
        }

        static int FrameAt(float t)
        {
            var frame = 0;
            for (var i = 0; i < FrameTimes.Length; i++)
                if (t >= FrameTimes[i]) frame = i;
            return frame;
        }

        static float BackOut(float x)
        {
            const float c = 1.70158f;
            x -= 1;
            return 1 + (c + 1) * x * x * x + c * x * x;
        }
    }
}
