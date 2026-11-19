// Movimento dos power-ups que o jogo aplica seguindo os roteiros do PowerUpVfxPlayer (bloco "Presence" do prefab):
//  - PowerUpArrivalMotion: a chegada no tabuleiro (bomba gira e para, globo gira, hélice do girocóptero vai parando,
//    line cleaner pulsa), com a luz e as fitas do efeito.
//  - PowerUpMerge: a junção de dois power-ups (brilho atrás, giro no próprio eixo, giro de um em volta do outro). No fim
//    os dois estão de volta nas casas, e a animação própria de cada combinação continua dali.
// Só visual: não decide destruição, pontuação nem tabuleiro.
using System.Collections;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using UnityEngine;

namespace RoyalAves.Effects
{
    public sealed class PowerUpArrivalMotion : MonoBehaviour
    {
        Item item;
        PowerUpKind kind;
        SpriteRenderer view;
        MarmaladeFly fly;
        Sprite[] frames;
        Sprite restSprite;
        Vector3 restScale;
        Quaternion restRotation;
        float time;
        bool running, ready;
        PowerUpVfxPlayer.PresenceSettings settings;

        static bool KindOf(Item item, out PowerUpKind kind)
        {
            kind = PowerUpKind.Bomb;
            if (item is ItemPackage) return true;
            if (item is ItemMulticolor) { kind = PowerUpKind.Ball; return true; }
            if (item is ItemMarmalade) { kind = PowerUpKind.Seeker; return true; }
            if (item is ItemStriped) { kind = PowerUpKind.Line; return true; }
            return false;
        }

        /// Toca a chegada de um power-up recém-criado. Só durante a partida (não na montagem da fase) e só com o prefab dos efeitos.
        /// false = não tocou (quem chamou usa a entrada antiga).
        public bool IsPlaying => running;

        public static bool Play(Item item)
        {
            if (item == null || !KindOf(item, out var kind)) return false;
            var manager = LevelManager.THIS;
            if (manager == null || (manager.gameStatus != GameState.Playing && manager.gameStatus != GameState.Tutorial)) return false;
            if (!PowerUpVfx.IsAvailable) return false;
            var motion = item.GetComponent<PowerUpArrivalMotion>();
            if (motion == null) motion = item.gameObject.AddComponent<PowerUpArrivalMotion>();
            motion.Begin(item, kind);
            return true;
        }

        /// Termina a chegada na hora e devolve a pose de repouso (antes de outra animação guardar a escala da peça).
        public static void Stop(Item item)
        {
            if (item == null) return;
            var motion = item.GetComponent<PowerUpArrivalMotion>();
            if (motion != null) motion.End();
        }

        void Begin(Item owner, PowerUpKind type)
        {
            End();
            item = owner; kind = type; time = 0f; running = true; ready = false;
            settings = PowerUpVfx.Presence;
            Resolve();
        }

        void Resolve()
        {
            fly = null; frames = null;
            if (kind == PowerUpKind.Seeker)
            {
                fly = item.GetComponentInChildren<MarmaladeFly>(false);
                if (fly == null) return;                                  // os filhos ainda não foram ligados: tenta no próximo quadro
                view = fly.GetComponent<SpriteRenderer>();
                frames = fly.SpinFrames;
            }
            else
            {
                view = item.GetSpriteRenderer();
                if (item is ItemMulticolor ball) frames = ball.SpinFrames;
            }
            if (view == null) return;
            if (frames != null && frames.Length == 0) frames = null;
            restScale = view.transform.localScale;
            restRotation = view.transform.localRotation;
            restSprite = view.sprite;
            ready = true;
            PowerUpVfx.PlayArrival(view.transform, kind);
            if (kind == PowerUpKind.Seeker && frames == null) PowerUpVfx.PlaySeekerWhirl(view.transform, settings.seekerSeconds, PowerUpVfx.Whirl.arrivalWind);      // vento enquanto gira e pousa
            Apply();
        }

        void Apply()
        {
            // peça já reservada por outra combinação (o globo transformou esta peça): só o salto de entrada, sem giro
            var spin = !item.IsMulticolorReserved;
            settings.ArrivalPose(kind, time, out var scale, out var phase, spin);
            view.transform.localScale = Vector3.Scale(restScale, new Vector3(scale.x, scale.y, 1f));
            if (frames != null && spin) view.sprite = frames[Mathf.FloorToInt(phase * frames.Length) % frames.Length];
            // color bomb sem quadros de giro: o medalhão gira no próprio plano e para na posição de repouso
            else if ((kind == PowerUpKind.Ball || kind == PowerUpKind.Seeker) && spin) view.transform.localRotation = restRotation * Quaternion.Euler(0f, 0f, -phase * 360f);
        }

        void LateUpdate()
        {
            if (!running) return;
            if (item == null) { running = false; return; }
            if (!ready) { Resolve(); if (!ready) return; }
            if (view == null || (item.destroying && !item.IsMulticolorReserved) || (fly != null && fly.IsFlying)) { End(); return; }
            time += Time.deltaTime;
            var total = item.IsMulticolorReserved ? settings.popSeconds : settings.ArrivalSeconds(kind);
            if (time >= total) { End(); return; }
            Apply();
        }

        void End()
        {
            if (running && ready && view != null)
            {
                view.transform.localScale = restScale;
                if ((kind == PowerUpKind.Ball || kind == PowerUpKind.Seeker) && frames == null) view.transform.localRotation = restRotation;
                // quem já decolou troca os próprios quadros; não devolve o desenho parado por cima
                if (frames != null && restSprite != null && (fly == null || !fly.IsFlying)) view.sprite = restSprite;
            }
            running = false; ready = false;
        }

        void OnDisable() => End();
    }

    public static class PowerUpMerge
    {
        /// Junção de dois power-ups antes da combinação: brilho atrás de cada um, giro no próprio eixo e uma volta de um em
        /// volta do outro. Termina com os dois exatamente como estavam (posição, escala, ordem de desenho).
        public static IEnumerator Play(Item first, Item second)
        {
            if (first == null || second == null || !PowerUpVfx.IsAvailable) yield break;
            PowerUpArrivalMotion.Stop(first); PowerUpArrivalMotion.Stop(second);
            var a = first.GetSpriteRenderer(); var b = second.GetSpriteRenderer();
            if (a == null || b == null) yield break;
            var player = PowerUpVfx.Player;
            var plan = PowerUpVfx.PlayMerge(a.transform, b.transform);
            if (player == null || plan == null) yield break;

            Vector3 positionA = a.transform.position, positionB = b.transform.position;
            Vector3 scaleA = a.transform.localScale, scaleB = b.transform.localScale;
            int layerA = a.sortingLayerID, layerB = b.sortingLayerID, orderA = a.sortingOrder, orderB = b.sortingOrder;
            bool animA = first.anim != null && first.anim.enabled, animB = second.anim != null && second.anim.enabled;
            if (first.anim != null) first.anim.enabled = false;
            if (second.anim != null) second.anim.enabled = false;
            a.sortingLayerName = player.sortingLayer; b.sortingLayerName = player.sortingLayer;
            try
            {
                for (var t = 0f; t < plan.Seconds; t += Time.deltaTime)
                {
                    if (a == null || b == null || !a.gameObject.activeInHierarchy || !b.gameObject.activeInHierarchy) break;
                    plan.Pose(t, out var pa, out var pb, out var sa, out var sb, out var aInFront);
                    a.transform.position = pa; b.transform.position = pb;
                    a.transform.localScale = Vector3.Scale(scaleA, new Vector3(sa.x, sa.y, 1f));
                    b.transform.localScale = Vector3.Scale(scaleB, new Vector3(sb.x, sb.y, 1f));
                    a.sortingOrder = aInFront ? player.MergeFrontOrder : player.MergeBackOrder;
                    b.sortingOrder = aInFront ? player.MergeBackOrder : player.MergeFrontOrder;
                    yield return null;
                }
            }
            finally
            {
                plan.Cancel();
                if (a != null)
                {
                    a.transform.position = positionA; a.transform.localScale = scaleA;
                    a.sortingLayerID = layerA; a.sortingOrder = orderA;
                }
                if (b != null)
                {
                    b.transform.position = positionB; b.transform.localScale = scaleB;
                    b.sortingLayerID = layerB; b.sortingOrder = orderB;
                }
                if (first != null && first.anim != null) first.anim.enabled = animA;
                if (second != null && second.anim != null) second.anim.enabled = animB;
            }
        }
    }
}
