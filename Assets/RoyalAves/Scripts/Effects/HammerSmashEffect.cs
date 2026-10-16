using System;
using System.Linq;
using SweetSugar.LeanTween.Framework;
using UnityEngine;

namespace RoyalAves.Effects
{
    // Correio Mágico: o martelo (boost Bomb) agora "marreta" a peça em vez de só encolher parado em cima dela -
    // aparece na posição da peça, balança um instante, gira pra trás pegando impulso e desce golpeando, com o
    // impacto disparando a destruição da peça (onImpact) no exato instante em que o quadro de explosão aparece.
    // Sem deslocamento de verdade (igual ao canhão): a sensação de movimento vem só da troca de quadros do
    // spritesheet, não de interpolar a posição.
    public static class HammerSmashEffect
    {
        static HammerSmashEffectData _data;

        static HammerSmashEffectData Data
        {
            get
            {
                if (_data == null) _data = Resources.Load<HammerSmashEffectData>("Effects/HammerSmashEffectData");
                return _data;
            }
        }

        // Board items render at RestScale (0.42 * ~0.88, Square.cs) times their raw sprite size; this prop isn't
        // spawned through that pipeline, so it sets its own - deliberately a bit bigger than one square so the hit
        // reads clearly over the piece it's crushing. 0.45 was the first guess; +15% after seeing it in the editor.
        const float HammerScale = 0.45f * 1.15f;

        const float IdleFrameTime = 0.06f; // ~3 idle frames before the wind-up starts
        const float WindupFrameTime = 0.07f;
        const float ImpactHoldTime = 0.09f; // onImpact fires the instant this frame appears
        const float AftermathHoldTime = 0.12f;
        const float SettleHoldTime = 0.12f;

        const float HalfPixelsPerUnit = 128f; // matches hammer-*.png's import setting

        // SwingFrames play order: windup-0, windup-1, impact, aftermath, settle (see HammerSmashEffectData). Only
        // the first 5 entries get a named hold time; anything beyond that (shouldn't happen) reuses SettleHoldTime.
        static readonly float[] SwingHoldTimes = { WindupFrameTime, WindupFrameTime, ImpactHoldTime, AftermathHoldTime, SettleHoldTime };
        const int ImpactFrameIndex = 2;

        // position: where the target square sits (world space) - the hammer appears centered right on top of it.
        // onImpact: called once, timed to the impact frame, so the piece pops exactly on the hit instead of after
        // the whole animation finishes. Returns how long the whole effect takes, or 0 when the art isn't set up
        // (caller then has to call onImpact itself).
        public static float Play(Vector3 position, Action onImpact)
        {
            var data = Data;
            if (data == null)
            {
                onImpact?.Invoke();
                return 0f;
            }

            var idleSprites = ToSprites(data.IdleFrames);
            var swingSprites = ToSprites(data.SwingFrames);
            if (idleSprites.Length == 0 && swingSprites.Length == 0)
            {
                onImpact?.Invoke();
                return 0f;
            }

            var root = new GameObject("HammerSmashFX");
            root.transform.position = position;
            root.transform.localScale = Vector3.one * HammerScale;

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            sr.sortingOrder = PowerUpEffectOrder.Value;

            var idleDuration = idleSprites.Length * IdleFrameTime;
            PlayFrames(sr, idleSprites, Enumerable.Repeat(IdleFrameTime, idleSprites.Length).ToArray());

            var swingHoldTimes = swingSprites.Select((_, i) => i < SwingHoldTimes.Length ? SwingHoldTimes[i] : SettleHoldTime).ToArray();
            var swingDuration = swingHoldTimes.Sum();
            LeanTween.delayedCall(idleDuration, () => PlayFrames(sr, swingSprites, swingHoldTimes));

            if (swingSprites.Length > ImpactFrameIndex)
            {
                var impactAt = idleDuration + swingHoldTimes.Take(ImpactFrameIndex).Sum();
                LeanTween.delayedCall(impactAt, () => onImpact?.Invoke());
            }
            else
            {
                onImpact?.Invoke();
            }

            var total = idleDuration + swingDuration;
            UnityEngine.Object.Destroy(root, total);
            return total;
        }

        static Sprite[] ToSprites(Texture2D[] textures)
        {
            if (textures == null) return Array.Empty<Sprite>();
            return textures.Where(t => t != null)
                .Select(t => Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), HalfPixelsPerUnit))
                .ToArray();
        }

        // Cycles a SpriteRenderer through a frame sequence once, one LeanTween.delayedCall per frame, each frame
        // held for its own duration (holdTimes[i]) instead of a fixed frame time - the impact/aftermath/settle
        // frames need to linger longer than the quick wind-up spin.
        static void PlayFrames(SpriteRenderer sr, Sprite[] frames, float[] holdTimes)
        {
            if (sr == null || frames.Length == 0) return;
            var t = 0f;
            for (var i = 0; i < frames.Length; i++)
            {
                var sprite = frames[i];
                LeanTween.delayedCall(t, () =>
                {
                    if (sr != null) sr.sprite = sprite;
                });
                t += holdTimes[i];
            }
        }
    }
}
