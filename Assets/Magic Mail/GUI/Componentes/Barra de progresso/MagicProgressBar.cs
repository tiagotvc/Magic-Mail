// Magic Mail progress bar: a golden frame, a purple fill that grows with the progress, a trail of magic sparkles left behind
// the front, a warm glow and a postal card riding on the front edge. Reusable: drop the prefab "Barra de progresso" under any
// Canvas and call SetProgress(0..1). Everything it draws is Images, so it works in any UI.
//
// All the motion is a pure function of (time, progress history): Evaluate(time, progressAt) places every part for that
// instant, with no state of its own. At run time Update feeds it the real clock and the progress it has been given; the
// editor (Level editor > Temporadas > Barra de progresso) feeds it a simulated loading, so what it previews is exactly what
// the game plays.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MagicMail
{
    [DisallowMultipleComponent]
    public class MagicProgressBar : MonoBehaviour
    {
        // Parts: made by MagicProgressBarBuilder (menu Magic Mail > Componentes > Montar barra de progresso).
        [SerializeField, HideInInspector] Image fill;
        [SerializeField, HideInInspector] Image glow;
        [SerializeField, HideInInspector] Image trail;
        [SerializeField, HideInInspector] Image card;
        [SerializeField, HideInInspector] Image[] sparklePool = new Image[0];
        [SerializeField, HideInInspector] Image[] haloPool = new Image[0];   // one soft halo per sparkle, same slot
        [SerializeField, HideInInspector] Sprite[] sparkleSprites = new Sprite[0];

        [Header("Preenchimento")]
        [Tooltip("Progresso de 0 a 1. No jogo quem manda é SetProgress; aqui é só a posição inicial.")]
        [SerializeField, Range(0f, 1f)] float progress = 0f;
        [Tooltip("Suavização do avanço (0 = acompanha na hora; maior = mais lento para alcançar).")]
        [SerializeField, Min(0f)] float smoothing = 8f;
        [Tooltip("Onde o preenchimento fica dentro da moldura, como fração do tamanho da barra: esquerda, direita, topo, base.")]
        [SerializeField] Vector4 fillInset = new Vector4(0.0235f, 0.0190f, 0.0935f, 0.0374f);
        [Tooltip("Por onde a frente (carta e brilho) anda, como fração da largura: começo e fim do canal escuro.")]
        [SerializeField] Vector2 trackInset = new Vector2(0.029f, 0.027f);

        [Header("Rastro mágico")]
        [Tooltip("Quanto do comprimento da barra o rastro alcança atrás da frente.")]
        [SerializeField, Range(0.05f, 1f)] float trailLength = 0.6f;
        [Tooltip("Segundos entre uma fagulha e a próxima.")]
        [SerializeField, Min(0.005f)] float sparkleInterval = 0.016f;
        [Tooltip("Quanto cada fagulha dura, em segundos (mínimo e máximo).")]
        [SerializeField] Vector2 sparkleLife = new Vector2(0.35f, 0.75f);
        [Tooltip("Tamanho das fagulhas como fração da altura da barra (mínimo e máximo).")]
        [SerializeField] Vector2 sparkleSize = new Vector2(0.04f, 0.14f);
        [Tooltip("Espalhamento vertical em torno do centro, como fração da altura.")]
        [SerializeField, Range(0f, 0.6f)] float sparkleSpread = 0.05f;
        [Tooltip("Quanto as fagulhas sobem enquanto somem, como fração da altura.")]
        [SerializeField, Range(0f, 1f)] float sparkleRise = 0.06f;
        [Tooltip("Cor das fagulhas (misturada com branco). Com a mistura aditiva, o dourado sobre o roxo vira rosa-claro.")]
        [SerializeField] Color sparkleColor = new Color(1f, 0.85f, 0.3f, 1f);
        [Tooltip("Muda o sorteio das fagulhas.")]
        [SerializeField] int seed = 7;
        [Tooltip("Parte das fagulhas que são estrelas de 4 pontas; as outras são pontinhos redondos.")]
        [SerializeField, Range(0f, 1f)] float starChance = 0.15f;

        [Header("Glow das fagulhas (auréola suave atrás de cada uma)")]
        [SerializeField] Color haloColor = new Color(1f, 0.8f, 0.25f, 1f);
        [Tooltip("Tamanho da auréola em relação à fagulha.")]
        [SerializeField, Range(0f, 8f)] float haloSize = 3.4f;
        [Tooltip("Intensidade da auréola (0 = sem glow).")]
        [SerializeField, Range(0f, 1f)] float haloAlpha = 0.55f;

        [Header("Faixa luminosa (o fio amarelo logo atrás da frente)")]
        [SerializeField] Color trailColor = new Color(1f, 0.82f, 0.3f, 0.55f);
        [Tooltip("Altura da faixa, como fração da altura da barra.")]
        [SerializeField, Range(0f, 1f)] float trailGlowHeight = 0.5f;
        [Tooltip("Comprimento da faixa, como fração do rastro.")]
        [SerializeField, Range(0f, 1f)] float trailGlowLength = 0.85f;

        [Header("Frente: carta e brilho")]
        [Tooltip("Altura da carta como fração da altura da barra.")]
        [SerializeField, Range(0.3f, 2.5f)] float cardHeight = 1.05f;
        [Tooltip("Inclinação da carta, em graus.")]
        [SerializeField, Range(-45f, 45f)] float cardTilt = -8f;
        [Tooltip("Quanto a carta balança, em graus, e a que velocidade.")]
        [SerializeField] Vector2 cardWobble = new Vector2(3f, 3.2f);
        [Tooltip("Deslocamento da carta em relação à frente, como fração da altura da barra (x, y).")]
        [SerializeField] Vector2 cardOffset = new Vector2(0.05f, 0f);
        [Tooltip("Tamanho do brilho atrás da carta, como fração da altura da barra.")]
        [SerializeField, Range(0f, 5f)] float glowSize = 2.0f;
        [SerializeField] Color glowColor = new Color(1f, 0.8f, 0.35f, 0.85f);

        // ---- run time ---------------------------------------------------------------------------------------------

        float startTime, target, shown;
        readonly List<Vector2> history = new List<Vector2>();   // (time, progress), oldest first

        public float Progress => target;

        /// Sets the progress the bar moves towards (0..1).
        public void SetProgress(float value) => target = Mathf.Clamp01(value);

        void OnEnable()
        {
            startTime = Time.unscaledTime;
            target = shown = progress;
            history.Clear();
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            var now = Time.unscaledTime - startTime;
            shown = smoothing <= 0f ? target : Mathf.Lerp(shown, target, 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));
            history.Add(new Vector2(now, shown));
            // Only what the sparkles can still ask for is kept: their longest life plus a little.
            var keepFrom = now - sparkleLife.y - 0.25f;
            var drop = 0;
            while (drop < history.Count - 1 && history[drop + 1].x < keepFrom) drop++;
            if (drop > 0) history.RemoveRange(0, drop);
            progressFn = progressFn ?? ProgressFromHistory;   // one delegate, not one per frame
            Evaluate(now, progressFn);
        }

        Func<float, float> progressFn;
        bool[] usedSlots = new bool[0];

        float ProgressFromHistory(float t)
        {
            if (history.Count == 0) return shown;
            if (t <= history[0].x) return history[0].y > 0f && t < 0f ? 0f : history[0].y;
            for (var i = history.Count - 1; i > 0; i--)
            {
                if (history[i - 1].x > t) continue;
                var a = history[i - 1];
                var b = history[i];
                return Mathf.Lerp(a.y, b.y, Mathf.InverseLerp(a.x, b.x, t));
            }
            return history[history.Count - 1].y;
        }

        // ---- the effect: a pure function of the instant -----------------------------------------------------------

        /// Puts the bar in a still pose (used by the builder, so the prefab and the scene show a pleasant frame).
        public void ApplyPose(float time, float value) => Evaluate(time, _ => value);

        /// Places every part for the instant `time` (seconds since the bar started). progressAt(t) is how far the bar
        /// was at instant t: the sparkles ask for it at the moment they were born.
        public void Evaluate(float time, Func<float, float> progressAt)
        {
            if (fill == null) return;
            var rect = ((RectTransform)transform).rect;
            var width = rect.width;
            var height = rect.height;
            if (width <= 0f || height <= 0f) return;

            var p = Mathf.Clamp01(progressAt(time));

            // The fill: its rectangle fixed inside the frame, revealed from the left by fillAmount.
            var fillRect = Rect.MinMaxRect(
                rect.xMin + width * fillInset.x, rect.yMin + height * fillInset.w,
                rect.xMax - width * fillInset.y, rect.yMax - height * fillInset.z);
            Place(fill.rectTransform, fillRect);
            fill.fillAmount = p;

            var left = rect.xMin + width * trackInset.x;
            var right = rect.xMax - width * trackInset.y;
            var centreY = rect.center.y;
            var front = Mathf.Lerp(left, right, p);

            if (glow != null)
            {
                var size = height * glowSize;
                Place(glow.rectTransform, new Rect(front - size / 2f, centreY - size / 2f, size, size));
                var pulse = 0.85f + 0.15f * Mathf.Sin(time * 5.5f);
                glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, glowColor.a * pulse);
                glow.enabled = glowSize > 0f;
            }

            // The luminous streak: a soft yellow thread along the centre line that ends at the front and fades towards the left.
            if (trail != null)
            {
                var length = Mathf.Min((right - left) * trailLength * trailGlowLength, front - left);
                trail.enabled = length > 4f && trailGlowHeight > 0f;
                if (trail.enabled)
                {
                    var thickness = height * trailGlowHeight;
                    Place(trail.rectTransform, new Rect(front - length, centreY - thickness / 2f, length, thickness));
                    var fade = Mathf.Clamp01(p * 10f);   // grows in with the first bit of progress
                    trail.color = new Color(trailColor.r, trailColor.g, trailColor.b, trailColor.a * fade * (0.9f + 0.1f * Mathf.Sin(time * 7f)));
                }
            }

            if (card != null)
            {
                var h = height * cardHeight;
                var aspect = card.sprite != null ? card.sprite.rect.width / card.sprite.rect.height : 1.4f;
                Place(card.rectTransform, new Rect(front + cardOffset.x * height - h * aspect / 2f, centreY + cardOffset.y * height - h / 2f, h * aspect, h));
                card.rectTransform.localEulerAngles = new Vector3(0f, 0f, cardTilt + Mathf.Sin(time * cardWobble.y) * cardWobble.x);
            }

            EvaluateSparkles(time, progressAt, left, right, centreY, height);
        }

        struct Sparkle
        {
            public float back, offsetY, rise, life, size, spin, tint;
            public int sprite;
        }

        // Sparkle n is born at n * sparkleInterval, behind the front of that moment; its look comes from (seed, n).
        Sparkle MakeSparkle(int n)
        {
            var r = new System.Random(unchecked(seed * 7919 + n * 104729 + 11));
            float F() => (float)r.NextDouble();
            var back = F();
            return new Sparkle
            {
                back = back * back,                       // most are born close to the front, a few far back
                offsetY = F() - F(),                      // mostly along the centre line
                rise = Mathf.Lerp(0.2f, 1f, F()),
                life = Mathf.Lerp(sparkleLife.x, sparkleLife.y, F()),
                size = F(),
                spin = (F() - 0.5f) * 160f,
                tint = F(),
                // [0] is the star, [1] the round dot: mostly dots, like the fine glitter of the original.
                sprite = sparkleSprites.Length > 1 && F() >= starChance ? 1 : 0,
            };
        }

        void EvaluateSparkles(float time, Func<float, float> progressAt, float left, float right, float centreY, float height)
        {
            var pool = sparklePool;
            if (pool == null || pool.Length == 0) return;
            if (usedSlots.Length != pool.Length) usedSlots = new bool[pool.Length];
            var used = usedSlots;
            Array.Clear(used, 0, used.Length);

            var interval = Mathf.Max(0.005f, sparkleInterval);
            var last = Mathf.FloorToInt(time / interval);
            var first = Mathf.Max(0, Mathf.FloorToInt((time - sparkleLife.y) / interval));
            first = Mathf.Max(first, last - pool.Length + 1);   // the pool is the most that can be alive at once
            var span = (right - left) * trailLength;

            for (var n = first; n <= last; n++)
            {
                var born = n * interval;
                var age = time - born;
                if (age < 0f) continue;
                var s = MakeSparkle(n);
                if (age >= s.life) continue;
                var bornAt = Mathf.Clamp01(progressAt(born));
                if (bornAt <= 0.002f) continue;

                var x = Mathf.Lerp(left, right, bornAt) - s.back * span;
                if (x < left) continue;
                var u = age / s.life;
                var y = centreY + s.offsetY * height * sparkleSpread + s.rise * u * height * sparkleRise;

                var slot = n % pool.Length;
                var img = pool[slot];
                if (img == null) continue;
                used[slot] = true;
                img.enabled = true;
                if (sparkleSprites.Length > 0) img.sprite = sparkleSprites[Mathf.Clamp(s.sprite, 0, sparkleSprites.Length - 1)];
                var size = Mathf.Lerp(sparkleSize.x, sparkleSize.y, s.size) * height * (1f - 0.4f * u);
                Place(img.rectTransform, new Rect(x - size / 2f, y - size / 2f, size, size));
                img.rectTransform.localEulerAngles = new Vector3(0f, 0f, s.spin * age);
                var twinkle = 0.75f + 0.25f * Mathf.Sin(age * 20f + n);
                // Brighter the closer to the front it was born, as in the original.
                var alpha = Mathf.Clamp01(u / 0.12f) * (1f - u) * twinkle * Mathf.Lerp(1f, 0.5f, s.back);
                var tint = Color.Lerp(Color.white, sparkleColor, s.tint);
                img.color = new Color(tint.r, tint.g, tint.b, alpha);

                // The glow: a larger, soft halo behind the dot (added to the light behind it, so it reads as a bloom).
                if (haloPool != null && slot < haloPool.Length && haloPool[slot] != null)
                {
                    var halo = haloPool[slot];
                    halo.enabled = haloAlpha > 0f;
                    var haloExtent = size * haloSize;
                    Place(halo.rectTransform, new Rect(x - haloExtent / 2f, y - haloExtent / 2f, haloExtent, haloExtent));
                    halo.color = new Color(haloColor.r, haloColor.g, haloColor.b, alpha * haloAlpha);
                }
            }

            for (var i = 0; i < pool.Length; i++)
            {
                if (used[i]) continue;
                if (pool[i] != null) pool[i].enabled = false;
                if (haloPool != null && i < haloPool.Length && haloPool[i] != null) haloPool[i].enabled = false;
            }
        }

        // Centres a part on `local` (a rectangle in this bar's own coordinates), whatever its anchors were.
        static void Place(RectTransform part, Rect local)
        {
            part.anchorMin = part.anchorMax = part.pivot = new Vector2(0.5f, 0.5f);
            part.sizeDelta = local.size;
            part.localPosition = new Vector3(local.center.x, local.center.y, 0f);
        }
    }
}
