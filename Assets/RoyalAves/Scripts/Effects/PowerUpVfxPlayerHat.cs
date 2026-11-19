// Chapéu de Mago (power-up de embaralhar): redemoinho mágico que embaralha as peças e, depois, um cajado que lança magias
// em vários pontos e troca a cor das peças atingidas. Parte do PowerUpVfxPlayer (mesmo pool, mesmos materiais).
// O jogo move as peças (HatShufflePlan.Pose) e troca as cores na hora em que cada magia chega (StaffCastPlan.HitTime);
// aqui ficam só as luzes, o chapéu e o cajado.
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace RoyalAves.Effects
{
    public sealed partial class PowerUpVfxPlayer
    {
        [Header("Chapéu de Mago: arte")]
        [Tooltip("Figura do chapéu (aparece no meio do redemoinho e na tela do power-up).")]
        public Sprite hatIcon;
        [Tooltip("Arte do cajado, em pé, com a ponta mágica em cima. Vazio = um cajado simples desenhado em código.")]
        public Sprite staffSprite;
        [Tooltip("Botão verde da tela do power-up: borda e miolo (os mesmos da modal de power-ups).")]
        public Sprite useButtonBorder;
        public Sprite useButtonCenter;
        public TMP_FontAsset useButtonFont;

        [Serializable]
        public sealed class HatSettings
        {
            [Header("Tela do power-up")]
            public string titlePt = "Chapéu de Mago";
            public string titleEn = "Wizard Hat";
            [TextArea(2, 3)] public string instructionPt = "Embaralhe os objetos e <hl>mude as cores</hl>!";
            [TextArea(2, 3)] public string instructionEn = "Shuffle the objects and <hl>change the colors</hl>!";
            public string useLabelPt = "Usar!";
            public string useLabelEn = "Use!";
            [Tooltip("Tamanho do botão em pixels do canvas (1080x1920).")]
            public Vector2 useButtonSize = new Vector2(560f, 250f);
            [Header("Tela escura durante a magia")]
            [Range(0f, 1f)] public float dim = 0.6f;
            [Header("Embaralhamento (redemoinho)")]
            [Min(0.2f)] public float shuffleSeconds = 1.3f;
            [Tooltip("Quantas voltas as peças dão em volta do centro enquanto trocam de lugar.")]
            public float swirlTurns = 0.75f;
            [Tooltip("Quanto as peças são puxadas para o centro no meio do caminho (0 = nada).")]
            [Range(0f, 0.9f)] public float gather = 0.45f;
            [Tooltip("As peças de fora saem um pouco depois das de dentro (fração do tempo).")]
            [Range(0f, 0.6f)] public float stagger = 0.22f;
            [Tooltip("Quanto as peças crescem no meio do voo.")]
            [Min(1f)] public float liftScale = 1.18f;
            [Range(0f, 1f)] public float pieceGlowAlpha = 0.55f;
            [Range(0f, 1f)] public float vortexAlpha = 0.8f;
            [Range(2, 8)] public int vortexArms = 4;
            [Min(0)] public float vortexWidth = 0.5f;
            [Min(0)] public int dustCount = 44;
            [Min(0)] public int glintCount = 12;
            [Tooltip("Raios saindo do chapéu durante o redemoinho.")]
            [Range(0, 8)] public int boltCount = 5;
            [Header("Chapéu no meio do redemoinho")]
            [Tooltip("Tamanho do chapéu, em casas.")]
            [Min(0)] public float hatSize = 2.3f;
            [Tooltip("Voltas que o chapéu dá sobre si mesmo.")]
            public float hatSpins = 1f;
            [Range(0f, 1f)] public float hatGlowAlpha = 0.7f;
            [Header("Cajado e troca de cores")]
            [Tooltip("Quantas peças mudam de cor.")]
            [Min(0)] public int recolorCount = 7;
            [Min(0.05f)] public float staffAppearSeconds = 0.4f;
            [Tooltip("Tempo entre uma magia e a próxima.")]
            [Min(0.02f)] public float castInterval = 0.13f;
            [Tooltip("Tempo de voo de cada magia até a peça.")]
            [Min(0.05f)] public float travelSeconds = 0.42f;
            [Tooltip("Altura do cajado, em casas.")]
            [Min(0)] public float staffSize = 3f;
            [Tooltip("Onde fica a ponta mágica, em fração da altura a partir do centro (0,5 = no topo).")]
            [Range(0f, 0.5f)] public float staffTip = 0.42f;
            [Tooltip("Quanto o cajado fica abaixo do tabuleiro, em casas.")]
            public float staffBelowBoard = 1.2f;
            [Tooltip("Curva do caminho de cada magia.")]
            [Range(0f, 0.6f)] public float castCurve = 0.28f;
            [Min(0)] public float orbSize = 0.55f;
            [Min(0)] public float trailWidth = 0.3f;
            [Min(0.05f)] public float burstSeconds = 0.42f;
            [Min(0)] public float burstSize = 1.6f;
            [Min(0)] public float staffLingerSeconds = 0.35f;
        }

        public HatSettings hat = new HatSettings();

        static float EaseOutBack(float x)
        {
            x = Mathf.Clamp01(x);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        /// Roteiro do embaralhamento: o jogo pergunta onde cada peça está em cada instante.
        public sealed class HatShufflePlan
        {
            internal HatSettings s;
            internal Vector3 center;
            internal float cell, maxDistance;

            public float Seconds => s.shuffleSeconds;

            /// Posição de uma peça que vai de start até target, "seconds" depois do início: gira em volta do centro do tabuleiro,
            /// é puxada um pouco para dentro no meio do caminho e cresce enquanto voa.
            public Vector3 Pose(Vector3 start, Vector3 target, float seconds, out float scale)
            {
                var from = start - center; var to = target - center;
                var r0 = new Vector2(from.x, from.y).magnitude; var r1 = new Vector2(to.x, to.y).magnitude;
                var delay = s.stagger * Mathf.Clamp01(r0 / Mathf.Max(0.001f, maxDistance));
                var f = Mathf.Clamp01((seconds / s.shuffleSeconds - delay) / Mathf.Max(0.05f, 1f - s.stagger));
                var e = Smooth(f);
                var a0 = Mathf.Atan2(from.y, from.x);
                var turn = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg) * Mathf.Deg2Rad + Mathf.PI * 2f * s.swirlTurns;
                if (r0 < 0.001f || r1 < 0.001f) turn = Mathf.PI * 2f * s.swirlTurns;
                var angle = a0 + turn * e;
                var radius = Mathf.Lerp(r0, r1, e) * (1f - s.gather * Mathf.Sin(Mathf.PI * e));
                scale = 1f + (s.liftScale - 1f) * Mathf.Sin(Mathf.PI * e);
                if (f >= 1f) return target;
                return new Vector3(center.x + Mathf.Cos(angle) * radius, center.y + Mathf.Sin(angle) * radius, Mathf.Lerp(start.z, target.z, e));
            }
        }

        /// Roteiro do cajado: quando cada magia chega na sua peça.
        public sealed class StaffCastPlan
        {
            internal HatSettings s;
            internal int count;
            /// Segundos, desde o início, até a magia de índice i chegar na peça.
            public float HitTime(int index) => s.staffAppearSeconds + index * s.castInterval + s.travelSeconds;
            public float Total => s.staffAppearSeconds + Mathf.Max(0, count - 1) * s.castInterval + s.travelSeconds + s.burstSeconds + s.staffLingerSeconds;
        }

        /// Redemoinho do embaralhamento. pieces: as peças que vão trocar de lugar (o efeito acompanha cada uma).
        public HatShufflePlan PlayHatShuffle(IList<Transform> pieces, IList<Color> colors, Vector3 center, float cell)
        {
            if (pieces == null || pieces.Count == 0) return null;
            var plan = new HatShufflePlan { s = hat, center = center, cell = cell };
            var far = cell;
            foreach (var piece in pieces)
            {
                if (piece == null) continue;
                var delta = piece.position - center; delta.z = 0f;
                far = Mathf.Max(far, delta.magnitude);
            }
            plan.maxDistance = far;
            var normalized = new List<Color>();
            for (var i = 0; i < pieces.Count; i++) normalized.Add(Normalize(colors != null && i < colors.Count ? colors[i] : Color.clear));
            Launch(new HatShuffleEffect { plan = plan, pieces = pieces, colors = normalized });
            return plan;
        }

        /// Cajado lançando uma magia em cada alvo. newColors: a cor que cada peça vai ganhar.
        public StaffCastPlan PlayStaffCast(Vector3 staffPosition, IList<Vector3> targets, IList<Color> newColors, float cell)
        {
            if (targets == null || targets.Count == 0) return null;
            var plan = new StaffCastPlan { s = hat, count = targets.Count };
            var normalized = new List<Color>();
            for (var i = 0; i < targets.Count; i++) normalized.Add(Normalize(newColors != null && i < newColors.Count ? newColors[i] : Color.clear));
            Launch(new StaffCastEffect { plan = plan, staffPosition = staffPosition, targets = targets, colors = normalized, cell = cell });
            return plan;
        }

        // ------------------------------------------------------------------------------------------ EMBARALHAMENTO

        sealed class HatShuffleEffect : Effect
        {
            public HatShufflePlan plan; public IList<Transform> pieces; public IList<Color> colors;
            HatSettings s;
            float cell, radius;
            Vector3 center;
            SpriteRenderer dim, hatSprite, hatGlow, hatHalo, settleMagenta, settleCyan, settleFlash;
            SpriteRenderer[] glows, dust, glints;
            Ribbon[] arms;
            BoltSet bolts;
            const float SettleSeconds = 0.35f;

            public override void Begin()
            {
                s = player.hat; cell = plan.cell; center = plan.center; radius = plan.maxDistance * 1.05f;
                duration = s.shuffleSeconds + SettleSeconds;
                dim = Add(Solid, -2, false);
                arms = new Ribbon[s.vortexArms];
                for (var i = 0; i < arms.Length; i++) arms[i] = player.MakeRibbon("Redemoinho do chapéu", 0, objects);
                glows = new SpriteRenderer[Mathf.Min(pieces.Count, 90)];
                for (var i = 0; i < glows.Length; i++) glows[i] = Add(GlowDot, 1);
                dust = new SpriteRenderer[player.Budget(s.dustCount, player.prism.dustDensity)];
                for (var i = 0; i < dust.Length; i++) dust[i] = Add(GlowDot, 2);
                glints = new SpriteRenderer[player.Budget(s.glintCount, 1f)];
                for (var i = 0; i < glints.Length; i++) glints[i] = Add(CrossStar, 3);
                settleMagenta = Add(BombRingMagenta, 3);
                settleCyan = Add(BombRingLine, 4);
                settleFlash = Add(GlowDot, 4);
                if (s.boltCount > 0) bolts = new BoltSet(player, objects, 5, s.boltCount * 2);
                hatHalo = Add(HollowGlow, 6);
                hatGlow = Add(GlowDot, 6);
                if (player.hatIcon != null) hatSprite = Add(player.hatIcon, 12, false);
            }

            public override void Evaluate(float t)
            {
                var p = player;
                var f = Mathf.Clamp01(t / s.shuffleSeconds);
                var env = Mathf.Sin(Mathf.PI * f);
                var settle = Mathf.Clamp01((t - s.shuffleSeconds) / SettleSeconds);
                var hdr = p.Hdr; var warm = 1f + 0.5f * (hdr - 1f);
                var rotation = Mathf.PI * 2f * s.swirlTurns * 1.6f * Smooth(f) + t * 0.6f;
                var off = A(Color.white, 0f);

                PlaceDim(dim, p.ViewCam, center.z, s.dim * Smooth(t / 0.2f) * (1f - Smooth(settle)));

                // braços do redemoinho: abrem do centro até a borda do tabuleiro e giram no sentido das peças
                for (var i = 0; i < arms.Length; i++)
                {
                    var arm = arms[i];
                    for (var k = 0; k <= RibbonSegments; k++)
                    {
                        var q = k / (float)RibbonSegments;
                        var angle = i / (float)arms.Length * Mathf.PI * 2f + rotation - q * 2.6f;
                        var next = angle - 0.05f;
                        var point = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (radius * (0.1f + 0.9f * q));
                        var ahead = center + new Vector3(Mathf.Cos(next), Mathf.Sin(next), 0f) * (radius * (0.1f + 0.9f * Mathf.Min(1f, q + 0.02f)));
                        var tangent = ahead - point; tangent.z = 0f;
                        tangent = tangent.sqrMagnitude > 1e-12f ? tangent.normalized : Vector3.right;
                        var normal = new Vector3(-tangent.y, tangent.x, 0f);
                        var shape = Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Clamp01(q * 0.92f + 0.04f)), 0.7f);
                        var half = normal * (s.vortexWidth * cell * shape * 0.5f);
                        arm.v[2 * k] = point - half; arm.v[2 * k + 1] = point + half;
                        var col = X(p.prism.ribbonGradient.Evaluate(q), warm);
                        col.a = Mathf.Clamp01(s.vortexAlpha * env * shape * (0.7f + 0.3f * Mathf.Sin(q * 9f - t * 14f + i)));
                        arm.c[2 * k] = col; arm.c[2 * k + 1] = col;
                    }
                    arm.mesh.vertices = arm.v; arm.mesh.colors = arm.c;
                }

                // cada peça voa com um brilho na própria cor
                for (var i = 0; i < glows.Length; i++)
                {
                    var piece = pieces[i];
                    if (piece == null || !piece.gameObject.activeInHierarchy) { glows[i].color = off; continue; }
                    Place(glows[i], piece.position, cell * 1.3f, cell * 1.3f, A(colors[i], s.pieceGlowAlpha * env));
                }

                // poeira mágica girando com o redemoinho e brilhos de 4 pontas alternados
                for (var j = 0; j < dust.Length; j++)
                {
                    var phase = Mathf.Repeat(t * (0.7f + 0.5f * Hash(j, 401)) + Hash(j, 402), 1f);
                    var r = radius * (0.12f + 0.95f * phase);
                    var angle = rotation * 0.7f + Hash(j, 403) * Mathf.PI * 2f + phase * 3.2f;
                    var size = cell * 0.1f * (0.5f + Hash(j, 404));
                    Place(dust[j], center + new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f), size, size, A(p.Prism(j % 5, Hash(j, 405)), env * Mathf.Sin(Mathf.PI * phase)));
                }
                for (var j = 0; j < glints.Length; j++)
                {
                    var clock = t * (1.2f + 0.8f * Hash(j, 411)) + Hash(j, 412);
                    var cycle = Mathf.FloorToInt(clock); var phase = clock - cycle;
                    if (phase > 0.4f) { glints[j].color = off; continue; }
                    var e = Mathf.Sin(Mathf.PI * phase / 0.4f);
                    var angle = Hash(j, cycle * 3 + 1) * Mathf.PI * 2f;
                    var r = radius * (0.2f + 0.8f * Hash(j, cycle * 3 + 2));
                    var size = cell * 0.42f * (0.45f + 0.75f * Hash(j, 413)) * e;
                    Place(glints[j], center + new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f), size, size,
                        A(j % 3 == 0 ? p.prismCyan : Color.Lerp(p.prismGold, Color.white, 0.5f), env * e));
                }

                // raios saindo do chapéu
                if (bolts != null)
                {
                    bolts.Begin();
                    var slot = Mathf.FloorToInt(t * 6f);
                    for (var j = 0; j < s.boltCount; j++)
                    {
                        var angle = Hash(j, slot) * Mathf.PI * 2f;
                        var reach = radius * (0.45f + 0.5f * Hash(j, slot + 7));
                        bolts.Arc(center, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * reach, t, 1000 + j, env, cell, 0.15f * (j % 2 == 0 ? 1f : -1f));
                    }
                    bolts.End();
                }

                // chapéu no centro: entra com um quique, gira sobre si mesmo e some quando as peças assentam
                var pop = EaseOutBack(t / 0.3f) * (1f - Smooth(settle));
                var hatSize = s.hatSize * cell * pop * (1f + 0.04f * Mathf.Sin(t * 10f));
                var hatAngle = 360f * s.hatSpins * Smooth(f) + 7f * Mathf.Sin(t * 8f);
                Place(hatGlow, center, hatSize * 1.5f, hatSize * 1.5f, A(p.prismGold, 0.6f * s.hatGlowAlpha * pop));
                Place(hatHalo, center, hatSize * 2.4f, hatSize * 2.4f, A(X(p.prismMagenta, warm), s.hatGlowAlpha * pop));
                if (hatSprite != null)
                {
                    var bounds = hatSprite.sprite.bounds.size;
                    var k = hatSize / Mathf.Max(0.001f, Mathf.Max(bounds.x, bounds.y));
                    Place(hatSprite, center, bounds.x * k, bounds.y * k, A(Color.white, pop), hatAngle);
                }

                // assentamento: uma onda sai do centro quando as peças chegam nos novos lugares
                if (settle > 0f)
                {
                    var ring = radius * 2f * (0.25f + 0.95f * EaseOut(settle)) / BombRing;
                    Place(settleMagenta, center, ring, ring, A(X(p.prismMagenta, warm), 0.8f * (1f - settle)));
                    Place(settleCyan, center, ring * 0.92f, ring * 0.92f, A(X(p.prismCyan, hdr), 0.8f * (1f - settle)));
                    Place(settleFlash, center, cell * (2f + 2f * settle), cell * (2f + 2f * settle), A(p.prismGold, 0.5f * (1f - settle) * (1f - settle)));
                }
                else { settleMagenta.color = off; settleCyan.color = off; settleFlash.color = off; }
            }
        }

        // ------------------------------------------------------------------------------------------ CAJADO

        sealed class StaffCastEffect : Effect
        {
            public StaffCastPlan plan; public Vector3 staffPosition; public IList<Vector3> targets; public IList<Color> colors; public float cell;
            HatSettings s;
            SpriteRenderer dim, staff, stick, orbGlow, orbCore, orbStar;
            SpriteRenderer[] headGlow, headCore, headStar, flash, ring, ringLine, pop, sparkles;
            Ribbon[] trail, trailGlow;
            float[] tilt;
            const int SparklesPerHit = 4;

            public override void Begin()
            {
                s = player.hat;
                duration = plan.Total;
                var n = targets.Count;
                dim = Add(Solid, -2, false);
                if (player.staffSprite != null) staff = Add(player.staffSprite, 11, false);
                else stick = Add(Solid, 11, false);
                orbGlow = Add(GlowDot, 12);
                orbStar = Add(CrossStar, 13);
                orbCore = Add(BombCoreWhite, 13);
                headGlow = new SpriteRenderer[n]; headCore = new SpriteRenderer[n]; headStar = new SpriteRenderer[n];
                flash = new SpriteRenderer[n]; ring = new SpriteRenderer[n]; ringLine = new SpriteRenderer[n]; pop = new SpriteRenderer[n];
                sparkles = new SpriteRenderer[n * SparklesPerHit];
                trail = new Ribbon[n]; trailGlow = new Ribbon[n]; tilt = new float[n];
                for (var i = 0; i < n; i++)
                {
                    trailGlow[i] = player.MakeRibbon("Magia (halo)", 5, objects);
                    trail[i] = player.MakeRibbon("Magia", 6, objects);
                    flash[i] = Add(GlowDot, 7);
                    ring[i] = Add(BombRingMagenta, 7);
                    ringLine[i] = Add(BombRingLine, 8);
                    pop[i] = Add(CrossStar, 8);
                    for (var k = 0; k < SparklesPerHit; k++) sparkles[i * SparklesPerHit + k] = Add(CrossStar, 9);
                    headGlow[i] = Add(GlowDot, 9);
                    headStar[i] = Add(CrossStar, 10);
                    headCore[i] = Add(BombCoreWhite, 10);
                    // inclinação do cajado para mirar em cada alvo (limitada, para não deitar)
                    var aim = targets[i] - staffPosition;
                    tilt[i] = Mathf.Clamp(-Mathf.Atan2(aim.x, Mathf.Max(0.001f, aim.y)) * Mathf.Rad2Deg, -32f, 32f);
                }
            }

            Vector3 Tip(float angleDegrees, float height)
            {
                var a = angleDegrees * Mathf.Deg2Rad;
                return staffPosition + new Vector3(-Mathf.Sin(a), Mathf.Cos(a), 0f) * (height * s.staffTip);
            }

            public override void Evaluate(float t)
            {
                var p = player;
                var n = targets.Count;
                var hdr = p.Hdr; var warm = 1f + 0.5f * (hdr - 1f);
                var off = A(Color.white, 0f);
                var total = plan.Total;
                var leave = 1f - Smooth((t - (total - s.staffLingerSeconds)) / Mathf.Max(0.05f, s.staffLingerSeconds));
                PlaceDim(dim, p.ViewCam, staffPosition.z, s.dim * Smooth(t / 0.35f) * (1f - Smooth((t - (total - 0.3f)) / 0.3f)));

                // cajado: sobe com um quique, mira em cada alvo e dá uma sacudida a cada magia
                var appear = EaseOutBack(t / s.staffAppearSeconds);
                var current = Mathf.Clamp(Mathf.FloorToInt((t - s.staffAppearSeconds) / s.castInterval), -1, n - 1);
                var angle = 0f; var kick = 0f;
                if (current >= 0)
                {
                    var k = (t - s.staffAppearSeconds - current * s.castInterval) / Mathf.Min(0.18f, s.castInterval);
                    var previous = current > 0 ? tilt[current - 1] : 0f;
                    angle = Mathf.Lerp(previous, tilt[current], Smooth(k)) - 7f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(k));
                    kick = Mathf.Sin(Mathf.PI * Mathf.Clamp01(k));
                }
                var height = s.staffSize * cell * appear * leave;
                var basePosition = staffPosition - new Vector3(0f, 0.6f * cell * (1f - Mathf.Clamp01(t / s.staffAppearSeconds)), 0f);
                if (staff != null)
                {
                    var bounds = staff.sprite.bounds.size;
                    var scale = height / Mathf.Max(0.001f, bounds.y);
                    Place(staff, basePosition, bounds.x * scale, height, A(Color.white, Mathf.Clamp01(appear) * leave), angle);
                }
                else Place(stick, basePosition, height * 0.07f, height * 0.9f, A(new Color(0.55f, 0.34f, 0.2f), Mathf.Clamp01(appear) * leave), angle);
                var tip = Tip(angle, height) + (basePosition - staffPosition);
                var orb = cell * s.orbSize * (1.5f + 0.9f * kick) * Mathf.Clamp01(appear) * leave;
                Place(orbGlow, tip, orb * 2.2f, orb * 2.2f, A(X(p.prismPink, warm), 0.85f * leave));
                Place(orbStar, tip, orb * 2.6f, orb * 2.6f, A(X(Color.Lerp(p.prismGold, Color.white, 0.5f), hdr), (0.6f + 0.4f * kick) * leave), t * 90f);
                Place(orbCore, tip, orb * 0.8f, orb * 0.8f, A(X(Color.white, hdr), leave));

                for (var i = 0; i < n; i++)
                {
                    var launch = s.staffAppearSeconds + i * s.castInterval;
                    var u = (t - launch) / s.travelSeconds;
                    var from = Tip(tilt[i], s.staffSize * cell);
                    var to = targets[i];
                    var delta = to - from; delta.z = 0f;
                    var length = Mathf.Max(0.001f, delta.magnitude);
                    var side = new Vector3(-delta.y, delta.x, 0f) / length * (i % 2 == 0 ? 1f : -1f);
                    var control = from + delta * 0.5f + side * (length * s.castCurve) + Vector3.up * (length * 0.12f);
                    var color = colors[i];

                    // magia em voo: cabeça luminosa na cor nova e rastro que afina (branco → cor nova → magenta)
                    var head = Mathf.Clamp01(u);
                    var tailLength = 0.42f * (1f - Smooth((u - 1f) / 0.5f));
                    var visible = u > 0f && u < 1.5f;
                    for (var k = 0; k <= RibbonSegments; k++)
                    {
                        var q = k / (float)RibbonSegments;
                        var w = Mathf.Max(0f, head - q * tailLength);
                        var point = (1f - w) * (1f - w) * from + 2f * (1f - w) * w * control + w * w * to;
                        var tangent = 2f * (1f - w) * (control - from) + 2f * w * (to - control); tangent.z = 0f;
                        tangent = tangent.sqrMagnitude > 1e-10f ? tangent.normalized : delta / length;
                        var normal = new Vector3(-tangent.y, tangent.x, 0f);
                        var width = s.trailWidth * cell * Mathf.Pow(1f - q, 0.8f);
                        trail[i].v[2 * k] = point - normal * (width * 0.5f); trail[i].v[2 * k + 1] = point + normal * (width * 0.5f);
                        trailGlow[i].v[2 * k] = point - normal * (width * 1.6f); trailGlow[i].v[2 * k + 1] = point + normal * (width * 1.6f);
                        var col = q < 0.15f ? Color.Lerp(Color.white, color, q / 0.15f) : Color.Lerp(color, p.prismMagenta, (q - 0.15f) / 0.85f);
                        col = X(col, warm); col.a = visible ? Mathf.Clamp01(Mathf.Pow(1f - q, 0.6f)) : 0f;
                        trail[i].c[2 * k] = col; trail[i].c[2 * k + 1] = col;
                        col = Color.Lerp(color, p.prismViolet, q); col.a = visible ? Mathf.Clamp01(0.5f * Mathf.Pow(1f - q, 0.7f)) : 0f;
                        trailGlow[i].c[2 * k] = col; trailGlow[i].c[2 * k + 1] = col;
                    }
                    trail[i].mesh.vertices = trail[i].v; trail[i].mesh.colors = trail[i].c;
                    trailGlow[i].mesh.vertices = trailGlow[i].v; trailGlow[i].mesh.colors = trailGlow[i].c;
                    if (u > 0f && u < 1f)
                    {
                        var at = (1f - u) * (1f - u) * from + 2f * (1f - u) * u * control + u * u * to;
                        var size = cell * s.orbSize;
                        Place(headGlow[i], at, size * 2f, size * 2f, A(X(color, warm), 0.9f));
                        Place(headStar[i], at, size * 2.2f, size * 2.2f, A(X(Color.Lerp(color, Color.white, 0.6f), hdr), 0.9f), t * 200f);
                        Place(headCore[i], at, size * 0.7f, size * 0.7f, A(X(Color.white, hdr), 1f));
                    }
                    else { headGlow[i].color = off; headStar[i].color = off; headCore[i].color = off; }

                    // chegada: a peça ganha a cor nova num clarão dessa cor, com anel magenta e brilhos
                    var a = (t - launch - s.travelSeconds) / s.burstSeconds;
                    if (a < 0f || a > 1f)
                    {
                        flash[i].color = off; ring[i].color = off; ringLine[i].color = off; pop[i].color = off;
                        for (var k = 0; k < SparklesPerHit; k++) sparkles[i * SparklesPerHit + k].color = off;
                        continue;
                    }
                    var grow = EaseOut(Mathf.Clamp01(a / 0.5f)); var fade = 1f - a;
                    var burst = s.burstSize * cell;
                    Place(flash[i], to, burst * (0.7f + 0.5f * grow), burst * (0.7f + 0.5f * grow), A(color, 0.85f * fade * fade));
                    var shock = burst * (0.45f + 0.55f * grow) / BombRing;
                    Place(ring[i], to, shock, shock, A(X(p.prismMagenta, warm), 0.9f * fade));
                    Place(ringLine[i], to, shock, shock, A(X(Color.Lerp(color, Color.white, 0.5f), hdr), fade));
                    var star = burst * 0.9f * (0.5f + 0.5f * grow);
                    Place(pop[i], to, star, star, A(X(Color.white, hdr), fade * fade), Hash(i, 421) * 90f);
                    for (var k = 0; k < SparklesPerHit; k++)
                    {
                        var id = i * 5 + k;
                        var angleK = (k + Hash(id, 422)) / SparklesPerHit * Mathf.PI * 2f;
                        var dist = cell * (0.25f + 0.65f * EaseOut(a)) * (0.7f + 0.6f * Hash(id, 423));
                        var e = Mathf.Sin(Mathf.PI * Mathf.Pow(a, 0.6f));
                        var sparkle = cell * 0.3f * (0.5f + Hash(id, 424)) * (0.5f + 0.5f * e);
                        Place(sparkles[i * SparklesPerHit + k], to + new Vector3(Mathf.Cos(angleK) * dist, Mathf.Sin(angleK) * dist, 0f), sparkle, sparkle,
                            A(k % 2 == 0 ? Color.Lerp(color, Color.white, 0.5f) : p.prismGold, e));
                    }
                }
            }
        }
    }
}
