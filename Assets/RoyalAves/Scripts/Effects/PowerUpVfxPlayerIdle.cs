// Brilho permanente da color bomb parada no tabuleiro: raios de várias cores e o halo amarelo quente (o mesmo da flecha).
// Fica ligado o tempo todo, atrás das peças; quando a color bomb é usada, a carga mágica (LightBallEffect) assume.
// Parte do PowerUpVfxPlayer (mesmo pool, mesmos materiais), mas fora da lista de efeitos ativos: não conta no limite
// "maxActive" e não é derrubado por outros efeitos.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Effects
{
    public sealed partial class PowerUpVfxPlayer
    {
        [Serializable]
        public sealed class IdleGlowSettings
        {
            [Tooltip("Força geral do brilho parado (0 = desligado).")]
            [Range(0f, 1.5f)] public float strength = 0.5f;
            [Tooltip("Comprimento dos raios parados, em relação aos da ativação.")]
            [Range(0.2f, 1.5f)] public float rayLength = 0.6f;
            [Tooltip("Tamanho do clarão quente atrás da color bomb parada, em relação ao da ativação.")]
            [Range(0.2f, 1.5f)] public float haloSize = 0.75f;
            [Tooltip("Espera antes de acender (deixa o salto de chegada acontecer primeiro).")]
            [Min(0)] public float delay = 0.2f;
            [Min(0.05f)] public float fadeIn = 0.45f;
            [Tooltip("Desenha o brilho atrás das peças do tabuleiro (camada e ordem abaixo). Desligado, fica por cima delas.")]
            public bool behindPieces = true;
            public string sortingLayer = "Default";
            public int sortingOrder = 1;
        }

        [Header("Color bomb parada: brilho permanente")]
        public IdleGlowSettings idleGlow = new IdleGlowSettings();

        /// Controle do brilho permanente de uma color bomb.
        public sealed class IdleGlow
        {
            internal bool stopped;
            public bool Alive { get; internal set; }
            public void Stop() => stopped = true;
        }

        readonly List<Effect> idleEffects = new List<Effect>();

        /// Liga o brilho permanente em volta de "follow" (o desenho da color bomb). behind = false: por cima das peças (preview).
        public IdleGlow PlayLightBallIdle(Transform follow, float cell, bool behind = true)
        {
            if (follow == null) return null;
            var handle = new IdleGlow { Alive = true };
            var effect = new LightBallIdleEffect { player = this, follow = follow, cell = cell, handle = handle, behind = behind };
            effect.Begin();
            idleEffects.Add(effect);
            effect.Evaluate(0f);
            return handle;
        }

        void TickIdle(float dt)
        {
            for (var i = idleEffects.Count - 1; i >= 0; i--)
            {
                var effect = idleEffects[i];
                effect.age += dt;
                if (effect.age >= effect.duration) { effect.Release(); idleEffects.RemoveAt(i); continue; }
                effect.Evaluate(effect.age);
            }
        }

        void StopIdle()
        {
            foreach (var effect in idleEffects) effect.Release();
            idleEffects.Clear();
        }

        sealed class LightBallIdleEffect : Effect
        {
            public Transform follow; public float cell; public IdleGlow handle; public bool behind;
            LightBallSettings s; IdleGlowSettings g;
            SpriteRenderer source, rimTight, rimWide, warmHalo;
            SpriteRenderer[] rainbow;
            Vector3 center;
            float endAt = -1f;
            const float FadeOut = 0.2f;

            public override void Begin()
            {
                s = player.lightBall; g = player.idleGlow;
                duration = float.MaxValue;
                center = follow.position;
                source = follow.GetComponent<SpriteRenderer>();
                rainbow = new SpriteRenderer[s.rainbowRays * 2];
                for (var i = 0; i < rainbow.Length; i++) rainbow[i] = Add(GlowDot, -6);
                warmHalo = Add(GlowDot, -6);
                rimWide = Add(RimHaloWide, -5);
                rimTight = Add(RimHalo, -5);
                if (behind && g.behindPieces)
                    foreach (var sr in sprites) { sr.sortingLayerName = g.sortingLayer; sr.sortingOrder = g.sortingOrder; }
            }

            protected override void OnRelease() { handle.Alive = false; }

            public override void Evaluate(float t)
            {
                var alive = follow != null && follow.gameObject.activeInHierarchy && (source == null || source.enabled);
                if (alive) center = follow.position;
                if ((!alive || handle.stopped) && endAt < 0f) endAt = t;
                if (endAt >= 0f && t - endAt >= FadeOut) { duration = age; handle.Alive = false; }

                var p = player;
                var fade = Smooth((t - g.delay) / g.fadeIn) * (endAt < 0f ? 1f : 1f - Smooth((t - endAt) / FadeOut));
                var I = g.strength * fade;
                var pulse = 0.5f + 0.5f * Mathf.Sin(t * 3.2f);
                var hdr = p.Hdr; var warm = 1f + 0.5f * (hdr - 1f);

                // halo amarelo quente colado na borda e clarão quente atrás
                var rimRadius = RoundRadius(source, cell * 0.43f);
                Place(rimTight, center, rimRadius * 4f, rimRadius * 4f, A(X(Color.Lerp(s.warmGlow, Color.white, 0.35f), hdr), 0.85f * I * (0.85f + 0.15f * pulse)));
                Place(rimWide, center, rimRadius * 4f, rimRadius * 4f, A(X(s.warmGlow, warm), s.warmGlowAlpha * I));
                var warmSize = s.warmGlowSize * g.haloSize * cell * (0.9f + 0.1f * pulse);
                Place(warmHalo, center, warmSize, warmSize, A(Color.Lerp(s.warmGlow, Color.white, 0.25f), 0.55f * s.warmGlowAlpha * I));

                // raios de várias cores girando devagar e pulsando
                var count = rainbow.Length / 2;
                for (var j = 0; j < count; j++)
                {
                    var angle = (j + 0.5f) / count * Mathf.PI * 2f + t * s.rainbowSpin * Mathf.Deg2Rad;
                    var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                    var tint = Color.HSVToRGB(Mathf.Repeat(j / (float)count + 0.04f * t, 1f), 0.82f, 1f);
                    var beat = 0.78f + 0.22f * Mathf.Sin(t * 2.6f + j * 1.7f);
                    var length = s.rainbowLength * g.rayLength * cell * (0.6f + 0.4f * Hash(j, 611)) * beat;
                    var width = s.rainbowWidth * cell * (0.7f + 0.6f * Hash(j, 612));
                    var at = center + direction * (rimRadius * 0.8f + length * 0.5f);
                    PlaceStreak(rainbow[2 * j], at, direction, length, width, A(tint, 0.6f * s.rainbowAlpha * I));
                    PlaceStreak(rainbow[2 * j + 1], at, direction, length * 0.8f, width * 0.26f, A(X(Color.Lerp(tint, Color.white, 0.6f), warm), 0.75f * s.rainbowAlpha * I * beat));
                }
            }
        }
    }
}
