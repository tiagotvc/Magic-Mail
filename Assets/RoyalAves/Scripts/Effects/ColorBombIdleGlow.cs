// Liga e desliga o brilho permanente da color bomb (PowerUpVfxPlayer.PlayLightBallIdle) conforme o estado dela:
// aceso enquanto está parada no tabuleiro; apagado quando é usada (a carga mágica assume) ou destruída.
// Só visual: não decide nada do jogo.
using SweetSugar.Scripts.Items;
using UnityEngine;

namespace RoyalAves.Effects
{
    public sealed class ColorBombIdleGlow : MonoBehaviour
    {
        ItemMulticolor ball;
        SpriteRenderer view;
        PowerUpVfxPlayer.IdleGlow glow;

        public static void Attach(ItemMulticolor ball)
        {
            if (ball == null) return;
            var component = ball.GetComponent<ColorBombIdleGlow>();
            if (component == null) component = ball.gameObject.AddComponent<ColorBombIdleGlow>();
            component.ball = ball;
        }

        void LateUpdate()
        {
            if (ball == null) return;
            var show = !ball.destroying && !ball.IsSpinning && PowerUpVfx.IsAvailable;
            if (show)
            {
                if (glow != null && glow.Alive) return;
                if (view == null) view = ball.GetSpriteRenderer();
                glow = view != null ? PowerUpVfx.PlayLightBallIdle(view.transform) : null;
            }
            else Stop();
        }

        void Stop()
        {
            if (glow != null) glow.Stop();
            glow = null;
        }

        void OnDisable() => Stop();
    }
}
