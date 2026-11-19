// Vento de giro do seeker (o rotor dourado): arcos de vento correndo em volta dele no sentido do giro, um aro que
// abre e pontinhos lançados para fora. Usado no giro rápido de tempos em tempos (peça parada no tabuleiro) e,
// em laço, durante o voo. Parte do PowerUpVfxPlayer (mesmo pool, mesmos materiais).
using System;
using UnityEngine;

namespace RoyalAves.Effects
{
    public sealed partial class PowerUpVfxPlayer
    {
        [Serializable]
        public sealed class WhirlSettings
        {
            [Header("Giro do seeker parado no tabuleiro")]
            [Tooltip("Intervalo entre um giro e outro, em segundos (sorteado entre os dois valores).")]
            public Vector2 idleEvery = new Vector2(3.5f, 7f);
            [Min(0.1f)] public float idleSeconds = 0.7f;
            [Tooltip("Voltas em cada giro.")]
            [Min(1)] public int idleTurns = 2;
            [Tooltip("Força do vento no giro de tempos em tempos (1 = igual ao do voo). Mais baixo = mais suave.")]
            [Range(0f, 1f)] public float idleWind = 0.38f;
            [Tooltip("Força do vento quando o seeker chega no tabuleiro.")]
            [Range(0f, 1f)] public float arrivalWind = 0.55f;
            [Header("Giro durante o voo")]
            [Tooltip("Graus por segundo.")]
            public float flightSpin = 1080f;
            [Header("Vento em volta do rotor")]
            [Range(0f, 1.5f)] public float windAlpha = 0.8f;
            [Tooltip("Raio dos arcos de vento, em casas.")]
            [Min(0)] public float windRadius = 0.52f;
            [Tooltip("Espessura dos arcos, em casas.")]
            [Min(0)] public float windWidth = 0.1f;
            [Tooltip("Voltas por segundo dos arcos de vento.")]
            public float windSpeed = 2.6f;
            [Min(0)] public int arcs = 3;
            [Min(0)] public int specks = 9;
            public Color windColor = new Color(1f, 1f, 1f);
            public Color windTail = new Color(0.8f, 0.9f, 1f);
        }

        [Header("Seeker: giro e vento")]
        public WhirlSettings whirl = new WhirlSettings();

        /// Vento de giro em volta de "follow". seconds &lt;= 0: em laço, até "follow" sumir (voo).
        /// strength: 1 = vento cheio (voo); abaixo disso fica mais suave (menos aro, menos pontinhos, arcos mais lentos e finos).
        public void PlaySeekerWhirl(Transform follow, float seconds, float cell, float strength = 1f)
        {
            if (follow == null) return;
            Launch(new SeekerWhirlEffect { follow = follow, seconds = seconds, cell = cell, strength = Mathf.Clamp01(strength) });
        }

        sealed class SeekerWhirlEffect : Effect
        {
            public Transform follow; public float seconds, cell, strength = 1f;
            WhirlSettings s;
            Ribbon[] arcs;
            SpriteRenderer ring;
            SpriteRenderer[] specks;
            Vector3 center;
            bool loop;
            float endAt = -1f;

            public override void Begin()
            {
                s = player.whirl;
                loop = seconds <= 0f;
                duration = loop ? 30f : seconds + 0.2f;
                center = follow.position;
                ring = Add(BombRingLine, 5);
                arcs = new Ribbon[s.arcs];
                for (var i = 0; i < arcs.Length; i++) arcs[i] = player.MakeRibbon("Vento do giro", 6, objects);
                specks = new SpriteRenderer[s.specks];
                for (var i = 0; i < specks.Length; i++) specks[i] = Add(GlowDot, 6);
            }

            public override void Evaluate(float t)
            {
                var alive = follow != null && follow.gameObject.activeInHierarchy;
                if (alive) center = follow.position;
                else if (endAt < 0f) { endAt = t; duration = Mathf.Min(duration, t + 0.15f); }
                // envelope: sobe rápido, segura e some; em laço, fica aceso até o rotor sumir
                var bell = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / Mathf.Max(0.05f, seconds)));
                var env = loop ? Smooth(t / 0.15f) : bell * bell;                    // entra e sai bem devagar
                var soft = 1f - strength;                                             // 0 = vento cheio, 1 = o mais suave
                if (endAt >= 0f) env *= 1f - Smooth((t - endAt) / 0.15f);
                var I = s.windAlpha * env * strength;
                var radius = s.windRadius * cell;

                // arcos de vento correndo no sentido do giro (horário), em raios um pouco diferentes
                for (var i = 0; i < arcs.Length; i++)
                {
                    var lane = radius * (0.82f + 0.2f * i);
                    var head = -(t * s.windSpeed * (1f - 0.45f * soft) * (1f + 0.25f * i) + i / (float)Mathf.Max(1, arcs.Length)) * Mathf.PI * 2f;
                    var span = (110f + 60f * soft + 50f * (1f - soft) * Mathf.Sin(t * 5f + i)) * Mathf.Deg2Rad;
                    OrbitRibbon(arcs[i], center, lane, lane * 0.94f, 12f * i, head, span, -1f, s.windWidth * cell * (1f - 0.18f * i) * (1f - 0.35f * soft),
                        s.windColor, s.windTail, I * (0.95f - 0.2f * i));
                }

                // aro que abre a cada batida do giro
                var beat = Mathf.Repeat(t * 2.2f, 1f);
                var size = radius * 2f * (0.75f + 0.75f * beat) / BombRing;
                Place(ring, center, size, size, A(s.windTail, 0.5f * I * (1f - beat) * (1f - soft)));

                // pontinhos lançados para fora pela ponta das pás
                for (var j = 0; j < specks.Length; j++)
                {
                    var clock = t * (1.6f + 0.9f * Hash(j, 801)) + Hash(j, 802);
                    var cycle = Mathf.FloorToInt(clock); var phase = clock - cycle;
                    var angle = Hash(j, 803 + cycle * 5) * Mathf.PI * 2f - phase * 1.4f;
                    var reach = radius * (0.9f + 1.1f * phase);
                    var dot = cell * 0.07f * (0.6f + 0.8f * Hash(j, 804)) * (1f - 0.5f * phase);
                    Place(specks[j], center + new Vector3(Mathf.Cos(angle) * reach, Mathf.Sin(angle) * reach, 0f), dot, dot,
                        A(Color.Lerp(s.windColor, s.windTail, Hash(j, 805)), I * (1f - 0.6f * soft) * Mathf.Sin(Mathf.PI * phase)));
                }
            }
        }
    }
}
