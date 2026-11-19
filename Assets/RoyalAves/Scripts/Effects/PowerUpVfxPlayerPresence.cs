// Presença dos power-ups no tabuleiro: a chegada (giro, pulso, luz), a junção de dois (brilho atrás, giro no próprio eixo,
// giro de um em volta do outro) e a combinação de duas bombas (carga lado a lado e explosão dupla).
// Só visual: quem move as peças é o jogo, seguindo os roteiros daqui (ArrivalPose, MergePlan).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Effects
{
    public enum PowerUpKind { Bomb, Ball, Seeker, Line }

    public sealed partial class PowerUpVfxPlayer
    {
        [Serializable]
        public sealed class PresenceSettings
        {
            [Header("Chegada no tabuleiro (\"olha, cheguei\")")]
            [Tooltip("Tempo do salto de entrada (cresce, passa um pouco do tamanho e assenta).")]
            [Min(0.05f)] public float popSeconds = 0.26f;
            [Min(1f)] public float popOvershoot = 1.14f;
            [Tooltip("Bomba: quica quando chega (não gira: a arte não tem quadros de giro).")]
            [Min(0.1f)] public float bombSeconds = 0.6f;
            [Min(1)] public int bombBounces = 2;
            [Range(0f, 0.6f)] public float bombBounce = 0.2f;
            [Tooltip("Color bomb: gira como um medalhão (ou troca os quadros de giro, se houver) e para.")]
            [Min(0.1f)] public float ballSeconds = 0.8f;
            [Min(0)] public int ballTurns = 1;
            [Tooltip("Girocóptero: a hélice gira e vai parando devagar, como se tivesse pousado.")]
            [Min(0.1f)] public float seekerSeconds = 1.15f;
            [Min(0)] public int seekerTurns = 4;
            [Tooltip("Line cleaner: pulsos de tamanho quando chega.")]
            [Min(0.1f)] public float lineSeconds = 0.6f;
            [Min(1)] public int linePulses = 2;
            [Range(0f, 0.6f)] public float linePulse = 0.22f;
            [Tooltip("Luz que acende em cima da peça na chegada.")]
            [Range(0f, 1.5f)] public float arrivalGlow = 0.85f;
            [Tooltip("Tamanho do brilho e do anel da chegada, em casas.")]
            [Min(0)] public float arrivalSize = 2.1f;
            [Tooltip("Fitas de luz girando em volta (disfarçam a falta de quadros de giro).")]
            [Range(0f, 1.5f)] public float swirlAlpha = 0.9f;
            [Min(0)] public int sparkleCount = 6;

            [Header("Globo ativo (girando)")]
            [Tooltip("Giro do medalhão enquanto está ativo, em graus por segundo (usado quando não há quadros de giro).")]
            public float ballSpin = 300f;
            [Tooltip("Quanto o globo encolhe e volta enquanto gira.")]
            [Range(0f, 0.3f)] public float ballBreath = 0.07f;
            [Tooltip("Pulsos de tamanho por segundo.")]
            [Min(0)] public float ballBreathFrequency = 2.4f;
            [Tooltip("Quanto o próprio desenho do globo acende.")]
            [Range(0f, 1.5f)] public float ballLight = 0.45f;

            [Header("Junção de dois power-ups")]
            [Tooltip("Primeiro os dois acendem, parados (brilho atrás e fitas de luz em volta).")]
            [Min(0.05f)] public float mergeSpinSeconds = 0.22f;
            [Tooltip("Depois giram um em volta do outro.")]
            [Min(0.05f)] public float mergeOrbitSeconds = 0.55f;
            [Tooltip("Voltas de um em volta do outro.")]
            [Min(1)] public int mergeOrbitTurns = 1;
            [Tooltip("Órbita deitada: 0 = círculo de frente, 1 = vista de lado.")]
            [Range(0f, 1f)] public float mergeTilt = 0.62f;
            [Tooltip("O que passa na frente cresce, o de trás diminui.")]
            [Range(0f, 0.6f)] public float mergeDepth = 0.2f;
            [Min(1f)] public float mergeScale = 1.15f;
            [Tooltip("Brilho forte atrás de cada um (como o da flecha).")]
            [Range(0f, 1.5f)] public float mergeGlow = 0.95f;
            [Min(0)] public float mergeGlowSize = 2.3f;

            [Header("Duas bombas")]
            [Tooltip("Tempo total da carga: as duas se unem no meio, a bomba cresce acesa e explode.")]
            [Min(0.1f)] public float doubleChargeSeconds = 0.9f;
            [Tooltip("Parte da carga usada para as duas se unirem numa só.")]
            [Range(0.05f, 0.9f)] public float doubleJoin = 0.3f;
            [Tooltip("Tamanho da bomba unida no fim da carga.")]
            [Min(1f)] public float doubleChargeScale = 2f;
            [Tooltip("Quanto o próprio desenho da bomba acende enquanto cresce (emissivo).")]
            [Range(0f, 2f)] public float doubleEmissive = 1.2f;
            [Tooltip("Tremor das bombas no fim da carga, em casas.")]
            [Min(0)] public float doubleShake = 0.035f;
            [Range(0f, 1.5f)] public float doubleChargeGlow = 0.9f;
            [Header("Duas bombas: explosão dupla")]
            [Min(0.2f)] public float doubleBurstSeconds = 1.35f;
            [Range(0f, 1.5f)] public float doubleFlash = 1f;
            [Range(0f, 1.5f)] public float doubleRingAlpha = 0.9f;
            [Tooltip("Fitas magenta que cruzam a explosão.")]
            [Range(0f, 1.5f)] public float doubleSwirlAlpha = 0.85f;
            [Min(0)] public int doubleRays = 16;
            [Tooltip("Brilhos que ficam em roda enquanto a explosão se dissipa.")]
            [Min(0)] public int doubleLingerSparkles = 22;

            public float ArrivalSeconds(PowerUpKind kind) =>
                kind == PowerUpKind.Bomb ? bombSeconds : kind == PowerUpKind.Ball ? ballSeconds : kind == PowerUpKind.Seeker ? seekerSeconds : lineSeconds;

            float Pop(float seconds)
            {
                var u = Mathf.Clamp01(seconds / Mathf.Max(0.01f, popSeconds));
                return u < 0.62f ? Mathf.Lerp(0.14f, popOvershoot, 1f - Mathf.Pow(1f - u / 0.62f, 3f)) : Mathf.Lerp(popOvershoot, 1f, Smooth((u - 0.62f) / 0.38f));
            }

            /// Pose da peça que acabou de chegar, "seconds" depois. scale multiplica a escala de repouso (x e y);
            /// phase são as voltas já dadas (quem tem quadros de giro mostra o quadro phase * quadros).
            /// spin = false: só o salto de entrada (peça que já foi reservada por outra combinação).
            public void ArrivalPose(PowerUpKind kind, float seconds, out Vector2 scale, out float phase, bool spin = true)
            {
                var u = Mathf.Clamp01(seconds / Mathf.Max(0.01f, ArrivalSeconds(kind)));
                var e = 1f - (1f - u) * (1f - u) * (1f - u);
                var pop = Pop(seconds);
                scale = new Vector2(pop, pop); phase = 0f;
                if (!spin) return;
                switch (kind)
                {
                    case PowerUpKind.Bomb:
                        // quica: estica para cima e achata, cada vez menos
                        var hop = u * Mathf.Max(1, bombBounces);
                        var bounce = bombBounce * Mathf.Sin(Mathf.PI * (hop - Mathf.Floor(hop))) * (1f - 0.55f * u) * Smooth(u / 0.1f);
                        scale = new Vector2(pop * (1f - 0.5f * bounce), pop * (1f + bounce));
                        break;
                    case PowerUpKind.Ball:
                        phase = ballTurns * e;
                        break;
                    case PowerUpKind.Seeker:
                        phase = seekerTurns * e;
                        var settle = Mathf.Sin(Mathf.PI * Mathf.Clamp01((u - 0.78f) / 0.22f));     // assenta no fim, como quem pousou
                        scale = new Vector2(pop * (1f + 0.05f * settle), pop * (1f - 0.07f * settle));
                        break;
                    default:
                        var beat = u * Mathf.Max(1, linePulses);
                        var pulse = linePulse * Mathf.Sin(Mathf.PI * (beat - Mathf.Floor(beat))) * (1f - 0.45f * u) * Smooth(u / 0.12f);
                        scale *= 1f + pulse;
                        break;
                }
            }
        }

        public PresenceSettings presence = new PresenceSettings();

        /// Camada e ordem de desenho para os dois power-ups durante a junção (o brilho fica atrás deles).
        public int MergeFrontOrder => sortingBase - 1;
        public int MergeBackOrder => sortingBase - 2;

        /// Roteiro da junção: os dois acendem e depois giram um em volta do outro, voltando para as casas de origem
        /// (a animação de cada combinação continua dali).
        public sealed class MergePlan
        {
            internal PresenceSettings s;
            internal Vector3 homeA, homeB;
            internal Action cancel;

            public float Seconds => s.mergeSpinSeconds + s.mergeOrbitSeconds;

            public void Pose(float seconds, out Vector3 a, out Vector3 b, out Vector2 scaleA, out Vector2 scaleB, out bool aInFront)
            {
                var u = Mathf.Clamp01(seconds / Mathf.Max(0.01f, Seconds));
                var p = Mathf.Clamp01((seconds - s.mergeSpinSeconds) / Mathf.Max(0.01f, s.mergeOrbitSeconds));
                var center = (homeA + homeB) * 0.5f;
                var arm = homeA - center; arm.z = 0f;
                var angle = Mathf.Atan2(arm.y, arm.x) + Mathf.PI * 2f * s.mergeOrbitTurns * Smooth(p);
                var lay = Mathf.Sin(Mathf.PI * p);                       // deita no meio da volta; começa e termina nas casas
                var sin = Mathf.Sin(angle);
                var offset = new Vector3(Mathf.Cos(angle), sin * (1f - s.mergeTilt * lay), 0f) * (arm.magnitude * (1f + 0.22f * lay));
                a = center + offset; b = center - offset;
                a.z = homeA.z; b.z = homeB.z;
                var depth = s.mergeDepth * lay * sin;                    // em cima = longe
                var grow = 1f + (s.mergeScale - 1f) * Mathf.Sin(Mathf.PI * u);
                scaleA = Vector2.one * (grow * (1f - depth));          // sem giro no próprio eixo: só o globo e a hélice têm quadros de giro
                scaleB = Vector2.one * (grow * (1f + depth));
                aInFront = sin < 0f;
            }

            public void Cancel() => cancel?.Invoke();
        }

        /// Chegada de um power-up: luz em cima da peça, anel, fitas girando e brilhos. Acompanha "follow".
        public float PlayArrival(Transform follow, PowerUpKind kind, float cell)
        {
            if (follow == null) return 0f;
            var effect = new ArrivalEffect { follow = follow, kind = kind, cell = cell };
            Launch(effect);
            return effect.duration;
        }

        /// Junção de dois power-ups: brilho forte atrás de cada um e fitas girando em volta. Devolve o roteiro do movimento.
        public MergePlan PlayMerge(Transform a, Transform b, float cell)
        {
            if (a == null || b == null) return null;
            var plan = new MergePlan { s = presence, homeA = a.position, homeB = b.position };
            var effect = new MergeAuraEffect { a = a, b = b, cell = cell, seconds = plan.Seconds };
            plan.cancel = () => effect.Cancel();
            Launch(effect);
            return plan;
        }

        /// Duas bombas, carga: anel magenta girando em volta das duas, luz crescendo entre elas e faíscas sendo puxadas.
        public void PlayDoubleBombCharge(Transform a, Transform b, float seconds, float cell)
        {
            if (a == null || b == null) return;
            Launch(new DoubleBombChargeEffect { a = a, b = b, cell = cell, seconds = seconds });
        }

        /// Duas bombas, explosão dupla: por cima da explosão normal, clarão, anéis em sequência, fitas magenta cruzando,
        /// raios e brilhos que ficam em roda na dissipação.
        public float PlayDoubleBombBurst(Vector3 center, float radiusCells, float cell)
        {
            var effect = new DoubleBombBurstEffect { center = center, radius = Mathf.Max(1f, radiusCells), cell = cell };
            Launch(effect);
            return effect.duration;
        }

        /// Cópia do próprio desenho, somada por cima dele: faz a peça acender.
        void PlaceLit(SpriteRenderer lit, SpriteRenderer source, Color tint, float alpha)
        {
            if (source == null || source.sprite == null || !source.enabled || alpha <= 0.001f) { lit.color = A(Color.white, 0f); return; }
            if (lit.sprite != source.sprite) { lit.sprite = source.sprite; lit.sharedMaterial = MaterialFor(source.sprite.texture); }
            lit.flipX = source.flipX; lit.flipY = source.flipY;
            lit.transform.position = source.transform.position;
            lit.transform.rotation = source.transform.rotation;
            lit.transform.localScale = source.transform.lossyScale;
            lit.color = A(tint, alpha);
        }

        static Color KindHot(PowerUpVfxPlayer p, PowerUpKind kind) =>
            kind == PowerUpKind.Bomb ? Color.Lerp(p.prismGold, new Color(1f, 0.55f, 0.2f), 0.35f) : kind == PowerUpKind.Ball ? Color.Lerp(p.prismPink, Color.white, 0.25f)
            : kind == PowerUpKind.Seeker ? Color.Lerp(p.prismCyan, Color.white, 0.25f) : p.prismGold;

        static Color KindCool(PowerUpVfxPlayer p, PowerUpKind kind) =>
            kind == PowerUpKind.Bomb ? p.prismMagenta : kind == PowerUpKind.Ball ? p.prismViolet : kind == PowerUpKind.Seeker ? p.prismViolet : p.prismPink;

        // ------------------------------------------------------------------------------------------ CHEGADA

        sealed class ArrivalEffect : Effect
        {
            public Transform follow; public PowerUpKind kind; public float cell;
            PresenceSettings s;
            SpriteRenderer source, lit, glow, ring, ringLine, ring2, star;
            SpriteRenderer[] sparkles;
            Ribbon[] swirls;
            Vector3 center;
            int seed;

            public override void Begin()
            {
                s = player.presence;
                duration = s.ArrivalSeconds(kind) + 0.12f;
                center = follow.position;
                seed = Mathf.RoundToInt(center.x * 37f) + Mathf.RoundToInt(center.y * 91f);
                source = follow.GetComponent<SpriteRenderer>();
                glow = Add(GlowDot, 0);
                ring = Add(BombRingMagenta, 1);
                ring2 = Add(BombRingMagenta, 1);
                ringLine = Add(BombRingLine, 2);
                lit = Add(GlowDot, 3);
                star = Add(CrossStar, 4);
                sparkles = new SpriteRenderer[s.sparkleCount];
                for (var i = 0; i < sparkles.Length; i++) sparkles[i] = Add(CrossStar, 5);
                swirls = new Ribbon[kind == PowerUpKind.Line ? 0 : 2];
                for (var i = 0; i < swirls.Length; i++) swirls[i] = player.MakeRibbon("Giro da chegada", 4, objects);
            }

            public override void Evaluate(float t)
            {
                var p = player;
                var alive = follow != null && follow.gameObject.activeInHierarchy;
                if (alive) center = follow.position;
                var total = s.ArrivalSeconds(kind);
                var u = Mathf.Clamp01(t / total);
                var fade = (alive ? 1f : 0f) * (1f - Smooth((t - total) / 0.12f));
                var hot = KindHot(p, kind); var cool = KindCool(p, kind);
                var hdr = p.Hdr;
                s.ArrivalPose(kind, t, out _, out var phase);

                // luz: clarão atrás e o próprio desenho aceso, forte no começo
                var burst = Mathf.Pow(1f - u, 1.6f);
                var size = cell * s.arrivalSize * (0.55f + 0.45f * EaseOut(u * 2.2f));
                Place(glow, center, size, size, A(Color.Lerp(cool, hot, 0.45f), 0.55f * s.arrivalGlow * burst * fade));
                p.PlaceLit(lit, source, Color.Lerp(hot, Color.white, 0.5f), 0.75f * s.arrivalGlow * Mathf.Pow(1f - u, 1.2f) * fade);
                var flash = 1f - Smooth(t / 0.22f);
                var starSize = cell * 1.5f * (0.5f + 0.5f * EaseOut(t / 0.22f));
                Place(star, center, starSize, starSize, A(X(Color.Lerp(Color.white, hot, 0.3f), hdr), flash * s.arrivalGlow * fade), 45f * Hash(seed, 3));

                // anel que abre; o line cleaner solta um anel a cada pulso
                var beats = kind == PowerUpKind.Line ? Mathf.Max(1, s.linePulses) : 1;
                for (var i = 0; i < 2; i++)
                {
                    var sr = i == 0 ? ring : ring2;
                    if (i >= beats) { sr.color = A(Color.white, 0f); continue; }
                    var a = Mathf.Clamp01((t - i * total / beats) / (kind == PowerUpKind.Line ? total / beats : 0.45f));
                    var diameter = cell * s.arrivalSize * (0.35f + 0.6f * EaseOut(a)) / BombRing;
                    var alpha = (a <= 0f ? 0f : 1f - a) * 0.85f * s.arrivalGlow * fade;
                    Place(sr, center, diameter, diameter, A(cool, alpha));
                    if (i == 0) Place(ringLine, center, diameter, diameter, A(X(Color.Lerp(hot, Color.white, 0.5f), hdr), alpha));
                }

                // fitas girando em volta, no ritmo do giro da peça, e parando com ela
                for (var i = 0; i < swirls.Length; i++)
                {
                    // o seeker agora é um rotor inteiro (antes era só a hélice em cima do corpo): fitas centradas nele
                    var flat = 0.42f;
                    var lift = 0f;
                    var rx = cell * 0.6f * (0.85f + 0.15f * EaseOut(u * 2f));
                    var head = phase * Mathf.PI * 2f + i * Mathf.PI;
                    var speed = 1f - u;                                       // mais curta conforme desacelera
                    OrbitRibbon(swirls[i], center + Vector3.up * lift, rx, rx * flat, i == 0 ? -10f : 10f, head, (60f + 170f * speed) * Mathf.Deg2Rad, 1f, cell * 0.13f,
                        X(Color.Lerp(hot, Color.white, 0.55f), hdr), cool, s.swirlAlpha * Mathf.Pow(1f - u, 0.7f) * fade);
                }

                for (var k = 0; k < sparkles.Length; k++)
                {
                    var a = Mathf.Clamp01((t - 0.03f * k) / (total * 0.8f));
                    var angle = (k + Hash(seed + k, 5)) / sparkles.Length * Mathf.PI * 2f + phase * 0.6f;
                    var dist = cell * (0.3f + 0.55f * EaseOut(a)) * (0.8f + 0.4f * Hash(seed + k, 6));
                    var env = a <= 0f ? 0f : Mathf.Sin(Mathf.PI * Mathf.Pow(a, 0.6f));
                    var sparkle = cell * 0.24f * (0.5f + Hash(seed + k, 7)) * (0.5f + 0.5f * env);
                    Place(sparkles[k], center + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0f), sparkle, sparkle,
                        A(k % 2 == 0 ? Color.Lerp(hot, Color.white, 0.5f) : cool, env * fade), 45f * Hash(seed + k, 8));
                }
            }
        }

        // ------------------------------------------------------------------------------------------ JUNÇÃO

        sealed class MergeAuraEffect : Effect
        {
            public Transform a, b; public float cell, seconds;
            PresenceSettings s;
            readonly SpriteRenderer[] source = new SpriteRenderer[2], wide = new SpriteRenderer[2], hot = new SpriteRenderer[2], lit = new SpriteRenderer[2];
            readonly Ribbon[] swirls = new Ribbon[4];
            SpriteRenderer[] sparkles;
            readonly Vector3[] at = new Vector3[2];
            float cancelledAt = -1f;

            public void Cancel() { if (cancelledAt < 0f) { cancelledAt = age; duration = Mathf.Min(duration, age + 0.14f); } }

            public override void Begin()
            {
                s = player.presence;
                duration = seconds + 0.16f;
                for (var i = 0; i < 2; i++)
                {
                    var t = i == 0 ? a : b;
                    at[i] = t.position;
                    source[i] = t.GetComponent<SpriteRenderer>();
                    wide[i] = Add(GlowDot, -4);
                    hot[i] = Add(GlowDot, -3);
                    lit[i] = Add(GlowDot, 0);
                    swirls[i * 2] = player.MakeRibbon("Giro da junção", 1, objects);
                    swirls[i * 2 + 1] = player.MakeRibbon("Giro da junção", 1, objects);
                }
                sparkles = new SpriteRenderer[8];
                for (var i = 0; i < sparkles.Length; i++) sparkles[i] = Add(CrossStar, 2);
            }

            public override void Evaluate(float t)
            {
                var p = player;
                var hdr = p.Hdr;
                var u = Mathf.Clamp01(t / seconds);
                var fade = Smooth(t / 0.1f) * (cancelledAt < 0f ? 1f - Smooth((t - seconds) / 0.16f) : 1f - Smooth((t - cancelledAt) / 0.14f));
                var kick = 1f + 0.8f * (1f - Smooth(t / 0.3f));
                var pulse = 0.5f + 0.5f * Mathf.Sin(t * 14f);
                var turns = 2f * Smooth(u);
                for (var i = 0; i < 2; i++)
                {
                    var follow = i == 0 ? a : b;
                    var alive = follow != null && follow.gameObject.activeInHierarchy;
                    if (alive) at[i] = follow.position;
                    var live = alive ? fade : 0f;
                    var cool = i == 0 ? p.prismMagenta : p.prismViolet;
                    var warm = i == 0 ? p.prismGold : Color.Lerp(p.prismGold, p.prismPink, 0.4f);
                    var size = cell * s.mergeGlowSize * (0.92f + 0.08f * pulse);
                    Place(wide[i], at[i], size, size, A(cool, 0.7f * s.mergeGlow * live * kick));
                    Place(hot[i], at[i], size * 0.6f, size * 0.6f, A(X(Color.Lerp(warm, Color.white, 0.4f), hdr), 0.8f * s.mergeGlow * live * kick));
                    p.PlaceLit(lit[i], source[i], Color.Lerp(warm, Color.white, 0.5f), 0.3f * s.mergeGlow * live * (0.6f + 0.4f * pulse));
                    for (var j = 0; j < 2; j++)
                    {
                        var rx = cell * 0.62f;
                        var head = (turns * 1.5f + t * 1.2f) * Mathf.PI * 2f * (i == 0 ? 1f : -1f) + j * Mathf.PI;
                        OrbitRibbon(swirls[i * 2 + j], at[i], rx, rx * 0.4f, i == 0 ? -12f : 12f, head, 190f * Mathf.Deg2Rad, i == 0 ? 1f : -1f, cell * 0.12f,
                            X(Color.Lerp(warm, Color.white, 0.6f), hdr), cool, 0.9f * s.swirlAlpha * live);
                    }
                }
                for (var k = 0; k < sparkles.Length; k++)
                {
                    var i = k % 2;
                    var angle = t * (3.5f + Hash(k, 21)) * (i == 0 ? 1f : -1f) + k * 0.9f;
                    var dist = cell * (0.55f + 0.2f * Mathf.Sin(t * 5f + k));
                    var twinkle = 0.5f + 0.5f * Mathf.Sin(t * 18f + k * 2.1f);
                    var sparkle = cell * 0.2f * (0.6f + 0.6f * Hash(k, 22));
                    Place(sparkles[k], at[i] + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist * 0.5f, 0f), sparkle, sparkle,
                        A(k % 3 == 0 ? p.prismCyan : Color.Lerp(p.prismGold, Color.white, 0.5f), twinkle * fade), 45f * Hash(k, 23));
                }
            }
        }

        // ------------------------------------------------------------------------------------------ DUAS BOMBAS: CARGA

        sealed class DoubleBombChargeEffect : Effect
        {
            public Transform a, b; public float cell, seconds;
            PresenceSettings s;
            readonly SpriteRenderer[] source = new SpriteRenderer[2], back = new SpriteRenderer[2], lit = new SpriteRenderer[2];
            SpriteRenderer aura, ringHalo, ringLine, between, betweenHot, spark;
            SpriteRenderer[] motes;
            readonly Ribbon[] comets = new Ribbon[3];
            readonly Vector3[] at = new Vector3[2];

            public override void Begin()
            {
                s = player.presence;
                duration = seconds + 0.08f;
                aura = Add(GlowDot, -4);
                for (var i = 0; i < 2; i++)
                {
                    var t = i == 0 ? a : b;
                    at[i] = t.position;
                    source[i] = t.GetComponent<SpriteRenderer>();
                    back[i] = Add(GlowDot, -3);
                    lit[i] = Add(GlowDot, 0);
                }
                ringHalo = Add(BombRingMagenta, 1);
                ringLine = Add(BombRingLine, 2);
                between = Add(GlowDot, 2);
                betweenHot = Add(BombCoreWhite, 3);
                spark = Add(CrossStar, 4);
                motes = new SpriteRenderer[14];
                for (var i = 0; i < motes.Length; i++) motes[i] = Add(GlowDot, 3);
                for (var i = 0; i < comets.Length; i++) comets[i] = player.MakeRibbon("Carga das duas bombas", 2, objects);
            }

            public override void Evaluate(float t)
            {
                var p = player;
                var hdr = p.Hdr;
                var u = Mathf.Clamp01(t / seconds);
                var fade = Smooth(t / 0.08f) * (1f - Smooth((t - seconds) / 0.08f));
                var build = Smooth(u);
                var pulse = 0.5f + 0.5f * Mathf.Sin(t * (10f + 16f * u));
                for (var i = 0; i < 2; i++)
                {
                    var follow = i == 0 ? a : b;
                    if (follow != null && follow.gameObject.activeInHierarchy) at[i] = follow.position;
                }
                var center = (at[0] + at[1]) * 0.5f;
                var reach = Mathf.Max(cell * 0.5f, (at[0] - at[1]).magnitude * 0.5f) + cell * (0.55f + 0.35f * build);
                var G = s.doubleChargeGlow;

                Place(aura, center, reach * 3.4f, reach * 3.4f, A(p.prismViolet, 0.55f * G * fade * (0.6f + 0.4f * build)));
                for (var i = 0; i < 2; i++)
                {
                    var size = cell * (1.9f + 0.7f * build) * (0.94f + 0.06f * pulse);
                    Place(back[i], at[i], size, size, A(Color.Lerp(p.prismMagenta, p.prismGold, 0.25f + 0.3f * build), 0.75f * G * fade));
                    p.PlaceLit(lit[i], source[i], X(Color.Lerp(p.prismGold, Color.white, 0.45f), hdr), (0.2f + 0.8f * build * (0.75f + 0.25f * pulse)) * s.doubleEmissive * fade);
                }
                var ringSize = 2f * reach / BombRing;
                Place(ringHalo, center, ringSize, ringSize, A(X(p.prismMagenta, 1f + 0.5f * (hdr - 1f)), 0.85f * G * fade * (0.5f + 0.5f * build)));
                Place(ringLine, center, ringSize, ringSize, A(X(Color.Lerp(p.prismPink, Color.white, 0.6f), hdr), 0.9f * G * fade * (0.4f + 0.6f * build)));
                for (var i = 0; i < comets.Length; i++)
                {
                    var head = (t * (1.6f + 2.4f * u) + i / (float)comets.Length) * Mathf.PI * 2f;
                    OrbitRibbon(comets[i], center, reach, reach * (0.86f + 0.1f * Mathf.Sin(t * 3f + i)), i * 40f, head, 150f * Mathf.Deg2Rad, 1f, cell * (0.14f + 0.08f * build),
                        X(Color.Lerp(p.prismPink, Color.white, 0.6f), hdr), p.prismViolet, 0.95f * G * fade);
                }
                // luz crescendo entre as duas
                var light = cell * (0.5f + 1.5f * build) * (0.9f + 0.1f * pulse);
                Place(between, center, light * 1.8f, light * 1.8f, A(Color.Lerp(p.prismGold, p.prismPink, 0.3f), 0.8f * G * fade * build));
                Place(betweenHot, center, light * 0.7f, light * 0.7f, A(X(Color.white, hdr), 0.95f * G * fade * build));
                Place(spark, center, light * 2.2f, light * 2.2f, A(X(Color.Lerp(Color.white, p.prismGold, 0.3f), hdr), 0.85f * G * fade * build * (0.6f + 0.4f * pulse)), t * 80f);
                // faíscas puxadas para o meio
                for (var k = 0; k < motes.Length; k++)
                {
                    var cycle = t * (1.3f + 0.9f * Hash(k, 31)) + Hash(k, 32);
                    var f = cycle - Mathf.Floor(cycle);
                    var angle = Hash(k + (int)Mathf.Floor(cycle) * 7, 33) * Mathf.PI * 2f + f * 1.6f;
                    var dist = reach * 1.25f * (1f - f * f);
                    var size = cell * 0.11f * (0.6f + 0.8f * Hash(k, 34));
                    Place(motes[k], center + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0f), size, size,
                        A(k % 3 == 0 ? p.prismCyan : k % 3 == 1 ? p.prismPink : Color.Lerp(p.prismGold, Color.white, 0.5f), Mathf.Sin(Mathf.PI * f) * G * fade));
                }
            }
        }

        // ------------------------------------------------------------------------------------------ DUAS BOMBAS: EXPLOSÃO DUPLA

        sealed class DoubleBombBurstEffect : Effect
        {
            public Vector3 center; public float radius, cell;
            PresenceSettings s;
            SpriteRenderer flash, flashCore, bloomPink, bloomViolet, star;
            readonly SpriteRenderer[] ringHalo = new SpriteRenderer[3], ringLine = new SpriteRenderer[3];
            SpriteRenderer[] rays, linger;
            readonly Ribbon[] swirls = new Ribbon[4];

            public override void Begin()
            {
                s = player.presence;
                duration = s.doubleBurstSeconds;
                bloomViolet = Add(GlowDot, 1);
                bloomPink = Add(GlowDot, 2);
                for (var i = 0; i < 3; i++) { ringHalo[i] = Add(BombRingMagenta, 3); ringLine[i] = Add(BombRingLine, 4); }
                for (var i = 0; i < swirls.Length; i++) swirls[i] = player.MakeRibbon("Explosão dupla", 5, objects);
                rays = new SpriteRenderer[s.doubleRays];
                for (var i = 0; i < rays.Length; i++) rays[i] = Add(GlowDot, 6);
                star = Add(CrossStar, 7);
                flash = Add(GlowDot, 8);
                flashCore = Add(BombCoreWhite, 9);
                linger = new SpriteRenderer[s.doubleLingerSparkles];
                for (var i = 0; i < linger.Length; i++) linger[i] = Add(CrossStar, 10);
            }

            public override void Evaluate(float t)
            {
                var p = player;
                var hdr = p.Hdr;
                var f = t / duration;
                var R = Mathf.Min(radius, 5.5f) * cell;                       // tamanho "visual" da explosão (a área pode ser o tabuleiro todo)
                var full = (radius + 0.5f) * cell;

                // clarão branco-dourado e as duas nuvens de luz (rosa por dentro, violeta por fora)
                var hot = Smooth(t / 0.03f) * (1f - Smooth((t - 0.1f) / 0.4f));
                var coreSize = R * (0.7f + 0.9f * EaseOut(t / 0.2f));
                Place(flash, center, coreSize * 2.2f, coreSize * 2.2f, A(X(Color.Lerp(p.prismGold, Color.white, 0.55f), hdr), 0.9f * s.doubleFlash * hot));
                Place(flashCore, center, coreSize, coreSize, A(X(Color.white, hdr), s.doubleFlash * hot));
                var cloud = Smooth(t / 0.05f) * (1f - Smooth((t - 0.25f) / (duration * 0.6f)));
                var cloudSize = R * 2f * (0.6f + 0.6f * EaseOut(t / 0.35f));
                Place(bloomPink, center, cloudSize * 1.3f, cloudSize * 1.3f, A(p.prismPink, 0.55f * s.doubleFlash * cloud));
                Place(bloomViolet, center, cloudSize * 2f, cloudSize * 2f, A(p.prismViolet, 0.5f * s.doubleFlash * cloud));
                var starSize = R * 2.6f * (0.5f + 0.5f * EaseOut(t / 0.16f));
                Place(star, center, starSize, starSize, A(X(Color.Lerp(Color.white, p.prismGold, 0.25f), hdr), s.doubleFlash * (1f - Smooth((t - 0.05f) / 0.35f)) * Smooth(t / 0.03f)), 45f + t * 30f);

                // três anéis em sequência (magenta, dourado, violeta) abrindo até a borda da área
                for (var i = 0; i < 3; i++)
                {
                    var a = Mathf.Clamp01((t - 0.09f * i) / (0.55f + 0.1f * i));
                    var radiusNow = Mathf.Lerp(R * 0.2f, Mathf.Lerp(R, full, 0.5f) * (1f + 0.12f * i), EaseOut(a));
                    var size = 2f * radiusNow / BombRing;
                    var alpha = a <= 0f ? 0f : s.doubleRingAlpha * Mathf.Pow(1f - a, 1.3f);
                    var tint = i == 0 ? p.prismMagenta : i == 1 ? p.prismGold : p.prismViolet;
                    Place(ringHalo[i], center, size, size, A(X(tint, 1f + 0.5f * (hdr - 1f)), 0.8f * alpha));
                    Place(ringLine[i], center, size, size, A(X(Color.Lerp(tint, Color.white, 0.6f), hdr), alpha));
                }

                // fitas magenta cruzando a explosão (duas elipses inclinadas, cada uma com duas pontas)
                var sweep = EaseOut(Mathf.Clamp01(t / 0.7f));
                var swirlFade = Smooth(t / 0.05f) * (1f - Smooth((t - 0.35f) / (duration * 0.55f)));
                for (var i = 0; i < swirls.Length; i++)
                {
                    var second = i >= 2;
                    var rx = R * (0.5f + 0.75f * sweep) * (second ? 1.12f : 1f);
                    var head = (0.15f + 1.1f * sweep) * Mathf.PI * 2f * (second ? -1f : 1f) + (i % 2) * Mathf.PI;
                    OrbitRibbon(swirls[i], center, rx, rx * 0.36f, second ? -24f : 20f, head, 230f * Mathf.Deg2Rad, second ? -1f : 1f, cell * (0.5f - 0.2f * sweep),
                        X(Color.Lerp(p.prismPink, Color.white, 0.55f), hdr), second ? p.prismViolet : p.prismMagenta, s.doubleSwirlAlpha * swirlFade);
                }

                // raios saindo do centro
                for (var i = 0; i < rays.Length; i++)
                {
                    var a = Mathf.Clamp01((t - 0.01f * (i % 5)) / (0.4f + 0.2f * Hash(i, 41)));
                    var angle = (i + 0.5f * Hash(i, 42)) / rays.Length * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                    var length = R * (0.5f + 0.8f * Hash(i, 43)) * (0.4f + 0.6f * EaseOut(a));
                    var dist = R * (0.15f + 0.95f * EaseOut(a)) * (0.7f + 0.3f * Hash(i, 44));
                    PlaceStreak(rays[i], center + dir * dist, dir, length, cell * (0.1f + 0.1f * Hash(i, 45)),
                        A(X(i % 3 == 0 ? Color.Lerp(p.prismPink, Color.white, 0.5f) : Color.Lerp(p.prismGold, Color.white, 0.55f), hdr), (a <= 0f ? 0f : 0.9f * Mathf.Pow(1f - a, 1.2f)) * s.doubleFlash));
                }

                // dissipação: brilhos em roda que piscam e somem
                for (var k = 0; k < linger.Length; k++)
                {
                    var a = Mathf.Clamp01((t - 0.28f - 0.3f * Hash(k, 51)) / (duration - 0.6f));
                    var angle = (k + Hash(k, 52)) / linger.Length * Mathf.PI * 2f + 0.5f * t;
                    var dist = R * (0.8f + 0.35f * Hash(k, 53)) * (0.9f + 0.1f * a);
                    var twinkle = 0.5f + 0.5f * Mathf.Sin(t * 16f + k * 1.7f);
                    var size = cell * 0.3f * (0.5f + Hash(k, 54)) * (0.6f + 0.4f * twinkle);
                    Place(linger[k], center + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0f), size, size,
                        A(k % 3 == 0 ? p.prismCyan : k % 3 == 1 ? Color.Lerp(p.prismPink, Color.white, 0.4f) : Color.Lerp(p.prismGold, Color.white, 0.5f),
                            (a <= 0f ? 0f : Mathf.Sin(Mathf.PI * Mathf.Pow(a, 0.5f))) * (0.4f + 0.6f * twinkle)), 45f * Hash(k, 55));
                }
            }
        }
    }
}
