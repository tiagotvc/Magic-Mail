// Seeker parado no tabuleiro: de tempos em tempos dá um giro rápido no próprio plano, com o vento de giro em volta
// (PowerUpVfxPlayer.PlaySeekerWhirl). Só visual: não decide nada do jogo.
using SweetSugar.Scripts.Items;
using UnityEngine;

namespace RoyalAves.Effects
{
    public sealed class SeekerIdleSpin : MonoBehaviour
    {
        ItemMarmalade item;
        MarmaladeFly fly;
        Transform view;
        Quaternion rest;
        bool spinning;
        float startedAt, nextAt, seconds;
        int turns;

        public static void Attach(ItemMarmalade item)
        {
            if (item == null) return;
            var component = item.GetComponent<SeekerIdleSpin>();
            if (component == null) component = item.gameObject.AddComponent<SeekerIdleSpin>();
            component.item = item;
            component.Cancel();
            component.Schedule();
        }

        void Schedule()
        {
            var every = PowerUpVfx.Whirl.idleEvery;
            nextAt = Time.time + Random.Range(Mathf.Min(every.x, every.y), Mathf.Max(every.x, every.y));
        }

        void Cancel()
        {
            if (spinning && view != null) view.localRotation = rest;
            spinning = false;
        }

        void LateUpdate()
        {
            if (item == null) return;
            if (fly == null || !fly.gameObject.activeInHierarchy) { Cancel(); fly = item.GetComponentInChildren<MarmaladeFly>(false); view = fly != null ? fly.transform : null; }
            // só gira parado: não durante a chegada, o uso, a destruição ou com quadros de giro próprios (arte antiga)
            var arrival = item.GetComponent<PowerUpArrivalMotion>();
            var busy = fly == null || fly.IsFlying || item.destroying || (arrival != null && arrival.IsPlaying) || (fly.SpinFrames != null && fly.SpinFrames.Length > 0);
            if (busy) { Cancel(); Schedule(); return; }

            if (!spinning)
            {
                if (Time.time < nextAt || !PowerUpVfx.IsAvailable) return;
                var s = PowerUpVfx.Whirl;
                spinning = true; startedAt = Time.time; seconds = s.idleSeconds; turns = s.idleTurns;
                rest = view.localRotation;
                PowerUpVfx.PlaySeekerWhirl(view, seconds, s.idleWind);
                return;
            }
            var u = Mathf.Clamp01((Time.time - startedAt) / Mathf.Max(0.05f, seconds));
            var eased = u * u * (3f - 2f * u);                        // arranca e para suave
            view.localRotation = rest * Quaternion.Euler(0f, 0f, -360f * turns * eased);
            if (u >= 1f) { Cancel(); Schedule(); }
        }

        void OnDisable() => Cancel();
    }
}
