// Celebration on the "Jogar" button for when the lobby reopens right after winning a level: the button's
// own glow flashes bright for a beat — like it's building up to burst — then a particle explosion fires
// outward from behind it. Hooked from LobbyController.Show() in the same spot where won coins start flying
// in (both only happen when pendingCoins > 0, i.e. we just came back from a level actually won).
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public class PlayButtonCelebration : MonoBehaviour
    {
        [Tooltip("Imagem de brilho atrás/sobre o botão (ex.: um glow dourado), do tamanho dele. Começa invisível.")]
        [SerializeField] Image glow;
        [Tooltip("Sistema de partículas atrás do botão — Shape no tamanho/forma dele, emissão por Burst (não contínua). Play On Awake desligado, quem dispara é este script.")]
        [SerializeField] ParticleSystem burst;
        [SerializeField] float glowBuildUp = 0.18f;
        [SerializeField] float glowHold = 0.05f;
        [SerializeField] float glowFade = 0.25f;
        [SerializeField] float glowMaxScale = 1.25f;

        Vector3 glowRestScale = Vector3.one;
        Coroutine routine;

        void Awake()
        {
            if (glow == null) return;
            glowRestScale = glow.rectTransform.localScale;
            var c = glow.color;
            glow.color = new Color(c.r, c.g, c.b, 0f);
        }

        [ContextMenu("Testar")]
        public void Play()
        {
            if (!isActiveAndEnabled) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Routine());
        }

        IEnumerator Routine()
        {
            if (glow != null)
            {
                yield return Fade(0f, 1f, glowBuildUp, glowRestScale, glowRestScale * glowMaxScale);
                yield return new WaitForSecondsRealtime(glowHold);
            }
            if (burst != null) burst.Play(true);
            if (glow != null) yield return Fade(1f, 0f, glowFade, glow.rectTransform.localScale, glowRestScale);
            routine = null;
        }

        IEnumerator Fade(float fromAlpha, float toAlpha, float duration, Vector3 fromScale, Vector3 toScale)
        {
            var c = glow.color;
            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var f = Mathf.Clamp01(t / Mathf.Max(duration, 0.0001f));
                glow.color = new Color(c.r, c.g, c.b, Mathf.Lerp(fromAlpha, toAlpha, f));
                glow.rectTransform.localScale = Vector3.Lerp(fromScale, toScale, f);
                yield return null;
            }
            glow.color = new Color(c.r, c.g, c.b, toAlpha);
            glow.rectTransform.localScale = toScale;
        }
    }
}
