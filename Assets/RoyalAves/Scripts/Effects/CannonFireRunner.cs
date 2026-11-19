using System;
using System.Collections.Generic;
using RoyalAves.Meta;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Level;
using UnityEngine;

namespace RoyalAves.Effects
{
    // Executa a sequência do canhão: entrada com quique e brilho, pavio queimando, disparo com coice, bala de canhão em
    // chamas mágicas atravessando a coluna (cada peça estoura quando a bala chega) e saída. Criado pelo CannonFireEffect e
    // reaproveitado entre usos: os sprites e as malhas do rastro ficam guardados e são só reposicionados.
    //
    // Regras desta classe:
    // - Não decide o que é destruído: só chama, na hora certa, a ação que o jogo passou para cada casa.
    // - O tempo vem de Time.deltaTime (igual em 30 e 60 FPS; para junto com o jogo se ele pausar o tempo).
    // - Se for interrompida (objeto desligado ou destruído com a fase em andamento), as casas que faltavam são processadas
    //   na hora, para o power-up gasto não ficar sem efeito; ao trocar de fase, voltar ao mapa ou fechar a cena, só limpa.
    // - Sempre devolve a câmera, solta a trava de queda e libera a tela escura do power-up.
    [AddComponentMenu("")]
    public sealed class CannonFireRunner : MonoBehaviour
    {
        // Ordem de desenho (somada a PowerUpEffectOrder.Value), de baixo para cima.
        const int OrderDim = -2, OrderTrail = 0, OrderEmber = 4, OrderImpact = 5, OrderAura = 6, OrderAppear = 7,
            OrderCannon = 8, OrderFuse = 9, OrderCharge = 10, OrderGlint = 11, OrderFuseSpark = 12, OrderBallFringe = 13,
            OrderBallFlame = 14, OrderBall = 15, OrderBallCore = 16, OrderFlash = 17, OrderFlashTop = 18, OrderSpark = 19;

        const int RibbonSegments = 24;
        const float EndBurstSeconds = 0.35f;

        static bool quitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            quitting = false;
            Application.quitting -= MarkQuitting;
            Application.quitting += MarkQuitting;
        }

        static void MarkQuitting() => quitting = true;

        // ------------------------------------------------------------------------------------------ estado

        CannonFireEffectData d;
        readonly List<Square> cells = new List<Square>();
        Action<Square> onHit;
        float[] hitTime = new float[0];
        bool[] hitDone = new bool[0];
        Color[] hitColor = new Color[0];
        Vector3[] hitPos = new Vector3[0];

        bool running, fired, ballEnded;
        float age, cell, unit, speed;
        float tFuse, tFire, tBallEnd, tCannonFade, tDimEnd, total, lastHit;
        float cx, z, topY, endY, emberAcc, fuseAcc;
        bool blocked, fuseBurning;
        Vector3 muzzleRest, pivotRest, fusePoint;
        LevelData ownerLevel;
        Camera cam;
        Vector3 camBase;
        bool shaking;
        GameObject fallHold;

        public bool IsRunning => running;

        // Preview do editor: sem tabuleiro de verdade. As casas são só posições, o tempo vem de Step e nada do jogo é avisado.
        bool previewMode;
        Action<int> onHitIndex;
        Camera viewCamera;
        readonly List<Vector3> path = new List<Vector3>();

        static void Kill(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target); else DestroyImmediate(target);
        }

        // ------------------------------------------------------------------------------------------ início

        /// Monta e começa a sequência. ordered: casas da coluna de baixo para cima. Devolve o tempo até a última casa ser atingida.
        public float Begin(CannonFireEffectData data, List<Square> ordered, Action<Square> callback)
        {
            if (running) Stop(false, false);
            previewMode = false; onHitIndex = null; viewCamera = null;
            onHit = callback;
            cells.Clear();
            cells.AddRange(ordered);
            path.Clear();
            foreach (var square in cells) path.Add(square.transform.position);
            return Launch(data, null);
        }

        /// Só para a janela de preview do editor: a mesma sequência num tabuleiro de mentira. positions: centro de cada casa
        /// da coluna, de baixo para cima. O tempo é avançado por Step; a câmera não treme e o jogo não é avisado.
        public float BeginPreview(CannonFireEffectData data, IList<Vector3> positions, IList<Color> colors, Action<int> callback, Camera camera)
        {
            if (running) Stop(false, false);
            previewMode = true; onHit = null; onHitIndex = callback; viewCamera = camera;
            cells.Clear();
            path.Clear();
            path.AddRange(positions);
            return Launch(data, colors);
        }

        /// Preview do editor: avança a sequência "dt" segundos.
        public void Step(float dt)
        {
            if (running && previewMode) Advance(dt);
        }

        float Launch(CannonFireEffectData data, IList<Color> colors)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            ReleaseVisuals();   // sobras de uma execução interrompida

            d = data;
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;
            var count = path.Count;

            var lm = previewMode ? null : LevelManager.THIS;
            cell = lm != null ? lm.squareWidth : 1.2f;
            // Passo real entre as casas (o tabuleiro pode estar em outra escala).
            var step = float.MaxValue;
            for (var i = 1; i < count; i++)
            {
                var dy = path[i].y - path[i - 1].y;
                if (dy > 0.01f) step = Mathf.Min(step, dy);
            }
            if (step < float.MaxValue) cell = step;
            cell = Mathf.Max(0.05f, cell);

            var first = path[0];
            var last = path[count - 1];
            cx = first.x; z = first.z; topY = last.y;
            unit = d.cannonScale * cell / 1.2f;
            var aim = Aim;
            muzzleRest = new Vector3(cx, first.y - cell * 0.5f + d.muzzleIntoBoardCells * cell, z);
            pivotRest = muzzleRest - aim * (d.muzzleOffsetPx / d.pixelsPerUnit * unit);

            // Coluna livre até o topo: a bala segue voando e some. Barrada (caixote, bloco, casa acima que o canhão não
            // alcança): a bala estoura na borda da última casa atingida.
            blocked = !previewMode && ColumnContinuesAbove(cells[count - 1]);
            endY = blocked ? topY + cell * 0.4f : topY + cell * (0.5f + d.exitCells);
            endY = Mathf.Max(endY, muzzleRest.y + cell * 0.2f);
            speed = Mathf.Max(0.5f, d.ballSpeedCells) * cell;

            tFuse = Mathf.Max(0f, d.entranceSeconds);
            tFire = tFuse + Mathf.Max(0f, d.fuseSeconds);
            tBallEnd = tFire + Mathf.Max(0.05f, (endY - muzzleRest.y) / speed);

            if (hitTime.Length != count)
            {
                hitTime = new float[count]; hitDone = new bool[count]; hitColor = new Color[count]; hitPos = new Vector3[count];
            }
            lastHit = tFire;
            for (var i = 0; i < count; i++)
            {
                hitPos[i] = path[i];
                var distance = Mathf.Max(0f, hitPos[i].y - d.hitLeadCells * cell - muzzleRest.y);
                hitTime[i] = Mathf.Min(tFire + distance / speed, tBallEnd);
                hitDone[i] = false;
                // Lê a cor de cada peça ANTES de ela ser destruída.
                var color = previewMode ? (colors != null && i < colors.Count ? colors[i] : d.impactFallbackColor) : PieceColors.Of(cells[i].Item);
                hitColor[i] = color.a > 0.5f ? color : d.impactFallbackColor;
                lastHit = Mathf.Max(lastHit, hitTime[i]);
            }

            tCannonFade = tFire + Mathf.Max(0f, d.cannonLingerSeconds);
            tDimEnd = Mathf.Max(tBallEnd, lastHit) + d.dimHoldAfterBall + d.dimFadeSeconds;
            total = Mathf.Max(tBallEnd + d.afterglowSeconds, Mathf.Max(tCannonFade + d.cannonFadeSeconds, tDimEnd)) + 0.05f;

            Build(count);

            age = 0f; emberAcc = 0f; fuseAcc = 0f;
            fired = false; ballEnded = false; fuseBurning = false;
            ownerLevel = lm != null ? lm.levelData : null;
            running = true;

            // Segura a queda das peças enquanto a bala atravessa (mesmo mecanismo do foguete): sem isso as peças de cima
            // começariam a cair no buraco antes de a bala chegar nelas.
            if (lm != null)
            {
                fallHold = new GameObject("CannonFallHold");
                fallHold.transform.SetParent(transform, false);
                fallHold.AddComponent<GameBlocker>();
            }
            if (!previewMode && d.suppressMatchEffect) PowerUpVfx.SuppressMatchFor(lastHit + 0.3f);   // as peças já têm o impacto do canhão
            if (!previewMode) BoosterIntroScreen.HoldUntil = Time.unscaledTime + tDimEnd;             // tela de power-up fica escura até a bala sair

            cam = previewMode ? viewCamera : Camera.main;
            shaking = !previewMode && d.cameraShake && cam != null && (d.fireShakeCells > 0f || d.hitShakeCells > 0f);
            if (shaking) camBase = cam.transform.position;

            PlaySound(d.entranceSound);
            Evaluate(0f);
            return lastHit;
        }

        Vector3 Aim => Quaternion.Euler(0f, 0f, d.artAngle) * Vector3.right;

        bool ColumnContinuesAbove(Square top)
        {
            try
            {
                var field = top != null ? top.field : null;
                if (field == null) return false;
                var limit = top.transform.position.y + cell * 0.5f;
                foreach (var square in field.GetSquares(true))
                {
                    if (square == null || square.IsNone() || square.col != top.col) continue;
                    if (square.transform.position.y > limit) return true;
                }
            }
            catch (Exception) { /* sem essa informação a bala só segue voando */ }
            return false;
        }

        // ------------------------------------------------------------------------------------------ ciclo

        void Update()
        {
            if (!running || previewMode) return;
            var lm = LevelManager.THIS;
            if (lm == null || lm.levelData != ownerLevel || lm.gameStatus == GameState.Map || lm.gameStatus == GameState.PrepareGame)
            {
                Stop(false, false);   // a fase acabou ou mudou: só limpa
                return;
            }
            Advance(Time.deltaTime);
        }

        void Advance(float deltaTime)
        {
            var previous = age;
            age += deltaTime;

            if (!fired && age >= tFire) { fired = true; OnFire(); }
            for (var i = 0; i < hitDone.Length && i < path.Count; i++)
                if (!hitDone[i] && age >= hitTime[i]) Hit(i, true);
            if (!running) return;   // a ação de alguma casa pode ter encerrado a fase
            if (!ballEnded && age >= tBallEnd) { ballEnded = true; OnBallEnd(); }
            if (fallHold != null && age >= lastHit + d.fallHoldAfterLastHit) ReleaseFallHold();

            EmitContinuous(previous, age, deltaTime);
            Evaluate(age);
            UpdateParticles(deltaTime);
            if (age >= total) Stop(false, false);
        }

        void LateUpdate()
        {
            if (!running || !shaking || cam == null) return;
            cam.transform.position = camBase + ShakeOffset(age);
        }

        Vector3 ShakeOffset(float t)
        {
            var amp = 0f;
            var sinceFire = t - tFire;
            if (d.fireShakeCells > 0f && sinceFire >= 0f && sinceFire <= d.fireShakeSeconds)
            {
                var e = 1f - sinceFire / d.fireShakeSeconds;
                amp += d.fireShakeCells * cell * e * e;
            }
            if (d.hitShakeCells > 0f)
                for (var i = 0; i < hitTime.Length; i++)
                {
                    var since = t - hitTime[i];
                    if (since >= 0f && since <= 0.1f) { var e = 1f - since / 0.1f; amp += d.hitShakeCells * cell * e * e; }
                }
            if (amp <= 0f) return Vector3.zero;
            return new Vector3((Mathf.PerlinNoise(t * 38f, 1.7f) - 0.5f) * 2f, (Mathf.PerlinNoise(3.1f, t * 41f) - 0.5f) * 2f, 0f) * amp;
        }

        void Hit(int index, bool withVisual)
        {
            hitDone[index] = true;
            if (previewMode)
            {
                try { onHitIndex?.Invoke(index); }
                catch (Exception e) { Debug.LogException(e); }
            }
            else
            {
                var square = cells[index];
                if (square != null && onHit != null)
                {
                    try { onHit(square); }
                    catch (Exception e) { Debug.LogException(e); }
                }
            }
            if (!withVisual || !running) return;
            var color = hitColor[index];
            var bright = Color.Lerp(color, Color.white, 0.35f);
            for (var k = 0; k < d.impactSparks; k++)
            {
                var angle = UnityEngine.Random.value * Mathf.PI * 2f;
                var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) + 0.35f) * (UnityEngine.Random.Range(2f, 5f) * cell);
                Emit(Art.SoftDot, OrderSpark, hitPos[index], velocity, UnityEngine.Random.Range(0.25f, 0.45f),
                    cell * UnityEngine.Random.Range(0.07f, 0.13f), bright, color, -4f * cell, 3f, 0f);
            }
        }

        void OnFire()
        {
            fuseBurning = false;
            if (d.shotSound != null) PlaySound(d.shotSound);
            else if (SoundBase.Instance != null) PlaySound(SoundBase.Instance.strippedExplosion);
            var origin = muzzleRest + Vector3.up * (cell * 0.1f);
            for (var k = 0; k < d.muzzleSparks; k++)
            {
                var angle = (90f + UnityEngine.Random.Range(-55f, 55f)) * Mathf.Deg2Rad;
                var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (UnityEngine.Random.Range(3f, 8f) * cell);
                var magic = UnityEngine.Random.value < d.emberMagicShare;
                Emit(k % 3 == 0 ? Art.Star : Art.SoftDot, OrderSpark, origin, velocity, UnityEngine.Random.Range(0.3f, 0.6f),
                    cell * UnityEngine.Random.Range(0.07f, 0.15f), magic ? d.emberMagicColor : d.emberHotColor,
                    magic ? d.trailFringeColor : d.emberColdColor, -6f * cell, 3f, UnityEngine.Random.Range(-360f, 360f));
            }
        }

        void OnBallEnd()
        {
            if (!blocked) return;
            var origin = new Vector3(cx, endY, z);
            for (var k = 0; k < d.endBurstSparks; k++)
            {
                var angle = UnityEngine.Random.value * Mathf.PI * 2f;
                var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) - 0.3f) * (UnityEngine.Random.Range(2.5f, 6f) * cell);
                var magic = UnityEngine.Random.value < d.emberMagicShare;
                Emit(k % 3 == 0 ? Art.Star : Art.SoftDot, OrderSpark, origin, velocity, UnityEngine.Random.Range(0.3f, 0.55f),
                    cell * UnityEngine.Random.Range(0.07f, 0.15f), magic ? d.emberMagicColor : d.emberHotColor,
                    magic ? d.trailFringeColor : d.emberColdColor, -5f * cell, 2.5f, UnityEngine.Random.Range(-360f, 360f));
            }
        }

        void EmitContinuous(float previous, float now, float dt)
        {
            // Faíscas do pavio
            if (fuseBurning && d.fuseSparksPerSecond > 0f)
            {
                fuseAcc += d.fuseSparksPerSecond * dt;
                while (fuseAcc >= 1f)
                {
                    fuseAcc -= 1f;
                    var angle = UnityEngine.Random.value * Mathf.PI * 2f;
                    var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) + 0.4f) * (UnityEngine.Random.Range(1f, 3f) * cell);
                    Emit(Art.SoftDot, OrderFuseSpark, fusePoint, velocity, UnityEngine.Random.Range(0.18f, 0.35f),
                        cell * UnityEngine.Random.Range(0.05f, 0.09f), d.fuseSparkColor, d.emberColdColor, -5f * cell, 2f, 0f);
                }
            }

            // Brasas soltas pela bala, proporcionais à distância percorrida (igual em qualquer FPS)
            var from = Mathf.Max(previous, tFire);
            var to = Mathf.Min(now, tBallEnd);
            if (to <= from || d.embersPerCell <= 0f) return;
            emberAcc += d.embersPerCell * (to - from) * speed / cell;
            var headY = muzzleRest.y + (to - tFire) * speed;
            while (emberAcc >= 1f)
            {
                emberAcc -= 1f;
                var position = new Vector3(cx + UnityEngine.Random.Range(-0.22f, 0.22f) * cell, headY - UnityEngine.Random.Range(0f, 0.5f) * cell, z);
                if (position.y < muzzleRest.y) position.y = muzzleRest.y;
                var velocity = new Vector2(UnityEngine.Random.Range(-1.4f, 1.4f), UnityEngine.Random.Range(-2.2f, -0.3f)) * cell;
                var magic = UnityEngine.Random.value < d.emberMagicShare;
                Emit(UnityEngine.Random.value < 0.3f ? Art.Star : Art.SoftDot, OrderEmber, position, velocity,
                    d.emberLifeSeconds * UnityEngine.Random.Range(0.6f, 1.2f), d.emberSizeCells * cell * UnityEngine.Random.Range(0.5f, 1.3f),
                    magic ? d.emberMagicColor : d.emberHotColor, magic ? d.trailFringeColor : d.emberColdColor,
                    -1.5f * cell, 1.5f, UnityEngine.Random.Range(-300f, 300f));
            }
        }

        void PlaySound(AudioClip clip)
        {
            if (previewMode) return;
            if (clip != null && SoundBase.Instance != null) SoundBase.Instance.PlayOneShot(clip);
        }

        // ------------------------------------------------------------------------------------------ fim e limpeza

        /// Interrompe sem destruir as casas que faltavam.
        public void Cancel() => Stop(false, false);

        void Stop(bool flushPending, bool fromDisable)
        {
            if (!running) return;
            running = false;
            if (flushPending)
                for (var i = 0; i < hitDone.Length && i < path.Count; i++)
                    if (!hitDone[i]) Hit(i, false);
            RestoreCamera();
            ReleaseFallHold();
            // Interrompido antes da hora: a tela de power-up não fica esperando o resto.
            if (!previewMode && age < tDimEnd && BoosterIntroScreen.HoldUntil > Time.unscaledTime) BoosterIntroScreen.HoldUntil = Time.unscaledTime;
            onHit = null; onHitIndex = null;
            cells.Clear();
            path.Clear();
            ownerLevel = null;
            if (fromDisable) return;   // não dá para mexer nos filhos enquanto o objeto desliga; o próximo Begin limpa
            ReleaseVisuals();
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        void RestoreCamera()
        {
            if (shaking && cam != null) cam.transform.position = camBase;
            shaking = false;
        }

        void ReleaseFallHold()
        {
            if (fallHold != null) Destroy(fallHold);
            fallHold = null;
        }

        void OnApplicationQuit() => quitting = true;

        void OnDisable()
        {
            if (!running) return;
            // Desligado no meio da sequência. Se a fase continua valendo, as casas que faltavam são destruídas na hora
            // (o power-up já foi gasto); se a cena está fechando ou a fase mudou, só limpa.
            var lm = LevelManager.THIS;
            var gameAlive = !quitting && gameObject.scene.isLoaded && lm != null && lm.levelData == ownerLevel &&
                            lm.gameStatus != GameState.Map && lm.gameStatus != GameState.PrepareGame;
            Stop(gameAlive, true);
        }

        void OnDestroy()
        {
            RestoreCamera();
            foreach (var material in additiveByTexture.Values)
                Kill(material);
            additiveByTexture.Clear();
            Kill(ownedAdditive);
            foreach (var ribbon in ribbons)
                if (ribbon != null) Kill(ribbon.mesh);
        }

        // ------------------------------------------------------------------------------------------ pool de sprites

        readonly Stack<SpriteRenderer> free = new Stack<SpriteRenderer>();
        readonly List<SpriteRenderer> fixedRenderers = new List<SpriteRenderer>();
        Material defaultMaterial;

        // Um material por textura: com um material só, a Unity junta os sprites num lote e desenha todos com a textura do primeiro.
        readonly Dictionary<Texture, Material> additiveByTexture = new Dictionary<Texture, Material>();
        Material additiveBase, ownedAdditive;
        bool additiveSearched;

        Material AdditiveBase
        {
            get
            {
                if (d != null && d.additiveMaterial != null) return d.additiveMaterial;
                if (additiveBase != null || additiveSearched) return additiveBase;
                additiveSearched = true;
                var shader = Shader.Find("RoyalAves/ImpactSprite");
                if (shader == null)
                {
                    Debug.LogWarning("Canhão: shader RoyalAves/ImpactSprite não encontrado; os brilhos saem com mistura normal. " +
                                     "Rode Royal Aves/Efeitos/Canhão/Aplicar configuração do efeito.");
                    return null;
                }
                ownedAdditive = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                ownedAdditive.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                ownedAdditive.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                additiveBase = ownedAdditive;
                return additiveBase;
            }
        }

        Material AdditiveFor(Texture texture)
        {
            var source = AdditiveBase;
            if (source == null) return null;
            if (texture == null) return source;
            if (additiveByTexture.TryGetValue(texture, out var material) && material != null && material.shader == source.shader) return material;
            material = new Material(source) { hideFlags = HideFlags.HideAndDontSave, mainTexture = texture };
            additiveByTexture[texture] = material;
            return material;
        }

        SpriteRenderer Rent(Sprite sprite, int order, bool additive)
        {
            SpriteRenderer sr = null;
            while (free.Count > 0 && sr == null) sr = free.Pop();
            if (sr == null)
            {
                var go = new GameObject("VFX");
                go.transform.SetParent(transform, false);
                sr = go.AddComponent<SpriteRenderer>();
                if (defaultMaterial == null) defaultMaterial = sr.sharedMaterial;
            }
            sr.gameObject.SetActive(true);
            sr.sprite = sprite;
            var material = additive ? AdditiveFor(sprite != null ? sprite.texture : null) : null;
            sr.sharedMaterial = material != null ? material : defaultMaterial;
            sr.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            sr.sortingOrder = PowerUpEffectOrder.Value + order;
            sr.color = new Color(1f, 1f, 1f, 0f);
            sr.transform.localScale = Vector3.one;
            sr.transform.rotation = Quaternion.identity;
            return sr;
        }

        SpriteRenderer Fixed(Sprite sprite, int order, bool additive = true)
        {
            var sr = Rent(sprite, order, additive);
            fixedRenderers.Add(sr);
            return sr;
        }

        void Release(SpriteRenderer sr)
        {
            if (sr == null) return;
            sr.sprite = null;
            sr.gameObject.SetActive(false);
            free.Push(sr);
        }

        void ReleaseVisuals()
        {
            foreach (var sr in fixedRenderers) Release(sr);
            fixedRenderers.Clear();
            for (var i = 0; i < particles.Count; i++) Release(particles[i].sr);
            particles.Clear();
            foreach (var ribbon in ribbons)
                if (ribbon != null && ribbon.renderer != null) ribbon.renderer.enabled = false;
            ReleaseFallHold();
        }

        // ------------------------------------------------------------------------------------------ partículas

        struct Particle
        {
            public SpriteRenderer sr;
            public Vector2 position, velocity;
            public float age, life, size, gravity, drag, spin, angle;
            public Color from, to;
        }

        readonly List<Particle> particles = new List<Particle>();

        void Emit(Sprite sprite, int order, Vector3 position, Vector2 velocity, float life, float size, Color from, Color to, float gravity, float drag, float spin)
        {
            if (particles.Count >= d.maxParticles || life <= 0f) return;
            var sr = Rent(sprite, order, true);
            sr.transform.position = position;
            particles.Add(new Particle
            {
                sr = sr, position = position, velocity = velocity, life = life, size = size, gravity = gravity, drag = drag,
                spin = spin, angle = UnityEngine.Random.value * 360f, from = from, to = to
            });
        }

        void UpdateParticles(float dt)
        {
            for (var i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                p.age += dt;
                if (p.age >= p.life || p.sr == null)
                {
                    Release(p.sr);
                    particles[i] = particles[particles.Count - 1];
                    particles.RemoveAt(particles.Count - 1);
                    continue;
                }
                p.velocity.y += p.gravity * dt;
                p.velocity *= Mathf.Exp(-p.drag * dt);
                p.position += p.velocity * dt;
                p.angle += p.spin * dt;
                var k = p.age / p.life;
                var color = Color.Lerp(p.from, p.to, k);
                color.a *= (1f - k) * Mathf.Sqrt(1f - k);
                var size = p.size * (1f - 0.5f * k);
                Place(p.sr, new Vector3(p.position.x, p.position.y, z), size, size, color, p.angle);
                particles[i] = p;
            }
        }

        // ------------------------------------------------------------------------------------------ rastro (malhas)

        sealed class Ribbon
        {
            public Mesh mesh;
            public MeshRenderer renderer;
            public Vector3[] v = new Vector3[2 * (RibbonSegments + 1)];
            public Color[] c = new Color[2 * (RibbonSegments + 1)];
            public Vector2[] uv = new Vector2[2 * (RibbonSegments + 1)];
        }

        readonly Ribbon[] ribbons = new Ribbon[4];

        Ribbon MakeRibbon(string label, int order, Texture2D texture)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            renderer.sortingOrder = PowerUpEffectOrder.Value + order;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var ribbon = new Ribbon { mesh = new Mesh { name = "CannonTrail" }, renderer = renderer };
            ribbon.mesh.MarkDynamic();
            var triangles = new int[RibbonSegments * 6];
            for (var i = 0; i < RibbonSegments; i++)
            {
                var a = i * 2;
                triangles[i * 6] = a; triangles[i * 6 + 1] = a + 1; triangles[i * 6 + 2] = a + 2;
                triangles[i * 6 + 3] = a + 1; triangles[i * 6 + 4] = a + 3; triangles[i * 6 + 5] = a + 2;
            }
            ribbon.mesh.vertices = ribbon.v;
            ribbon.mesh.triangles = triangles;
            filter.sharedMesh = ribbon.mesh;
            renderer.enabled = false;
            ApplyRibbonMaterial(ribbon, texture);
            return ribbon;
        }

        void ApplyRibbonMaterial(Ribbon ribbon, Texture2D texture)
        {
            var material = AdditiveFor(texture);
            ribbon.renderer.sharedMaterial = material;
            if (material == null) return;
            var block = new MaterialPropertyBlock();
            block.SetTexture("_MainTex", texture);
            ribbon.renderer.SetPropertyBlock(block);
        }

        /// Faixa reta ao longo da coluna, larga na bala e afunilando até a ponta de trás, com labaredas ondulando.
        void UpdateRibbon(Ribbon ribbon, float headY, float length, float width, Color color, float alpha, float phase, float scroll)
        {
            if (ribbon == null || ribbon.renderer == null) return;
            if (ribbon.renderer.sharedMaterial == null || alpha <= 0.002f || length <= 0.002f || width <= 0.002f)
            {
                ribbon.renderer.enabled = false;
                return;
            }
            ribbon.renderer.enabled = true;
            var sway = d.trailSwayCells * cell;
            for (var i = 0; i <= RibbonSegments; i++)
            {
                var s = i / (float)RibbonSegments;
                var y = headY - s * length;
                var w = width * Mathf.Pow(1f - s, d.trailTaper) * (1f + d.trailWobble * Mathf.Sin(s * 11f - age * 24f + phase));
                if (s < 0.1f) w *= 0.6f + 0.4f * (s / 0.1f);   // arredonda a ponta da frente (a bala cobre)
                var x = cx + sway * s * Mathf.Sin(s * 6.5f - age * 13f + phase * 1.7f);
                var vertexColor = color;
                vertexColor.a = alpha * Mathf.Pow(1f - s, 1.3f);
                var u = s * 1.6f + scroll;
                ribbon.v[i * 2] = new Vector3(x - w * 0.5f, y, z);
                ribbon.v[i * 2 + 1] = new Vector3(x + w * 0.5f, y, z);
                ribbon.c[i * 2] = vertexColor; ribbon.c[i * 2 + 1] = vertexColor;
                ribbon.uv[i * 2] = new Vector2(u, 0f); ribbon.uv[i * 2 + 1] = new Vector2(u, 1f);
            }
            ribbon.mesh.vertices = ribbon.v;
            ribbon.mesh.colors = ribbon.c;
            ribbon.mesh.uv = ribbon.uv;
            ribbon.mesh.bounds = new Bounds(new Vector3(cx, headY - length * 0.5f, z), new Vector3(width * 2f + sway * 2f + cell, length + cell, 1f));
        }

        // ------------------------------------------------------------------------------------------ montagem

        SpriteRenderer dim, aura, appearGlow, appearRing, cannon, charge, fuseGlow, fuseStar, fuseCore, muzzleCharge;
        SpriteRenderer ballFringe, ballFlame, ballBody, ballCore, flashGlow, flashBurst, flashRing, flashCore, endGlow, endRing;
        SpriteRenderer[] fuseSegments = new SpriteRenderer[0], sparkles = new SpriteRenderer[0], motes = new SpriteRenderer[0];
        SpriteRenderer[] hitFlash = new SpriteRenderer[0], hitRing = new SpriteRenderer[0], hitCore = new SpriteRenderer[0];
        Sprite[] idleSprites = new Sprite[0], fireSprites = new Sprite[0];
        Texture chargeTexture;

        void Build(int count)
        {
            idleSprites = Art.CannonSprites(d.IdleFrames, d.pixelsPerUnit);
            fireSprites = Art.CannonSprites(d.FireFrames, d.pixelsPerUnit);
            var frame = idleSprites.Length > 0 ? idleSprites[0] : null;

            dim = Fixed(Art.Solid, OrderDim, false);
            if (ribbons[0] == null)
            {
                ribbons[0] = MakeRibbon("Rastro: franja mágica", OrderTrail, Art.RibbonProfile);
                ribbons[1] = MakeRibbon("Rastro: labaredas", OrderTrail + 1, Art.FlameNoise);
                ribbons[2] = MakeRibbon("Rastro: faixa dourada", OrderTrail + 2, Art.RibbonProfile);
                ribbons[3] = MakeRibbon("Rastro: miolo quente", OrderTrail + 3, Art.RibbonProfile);
            }
            else
            {
                // O material aditivo pode ter sido trocado no asset entre um uso e outro.
                ApplyRibbonMaterial(ribbons[0], Art.RibbonProfile); ApplyRibbonMaterial(ribbons[1], Art.FlameNoise);
                ApplyRibbonMaterial(ribbons[2], Art.RibbonProfile); ApplyRibbonMaterial(ribbons[3], Art.RibbonProfile);
            }

            hitFlash = new SpriteRenderer[count]; hitRing = new SpriteRenderer[count]; hitCore = new SpriteRenderer[count];
            for (var i = 0; i < count; i++)
            {
                hitFlash[i] = Fixed(Art.SoftDot, OrderImpact);
                hitRing[i] = Fixed(Art.Ring, OrderImpact);
                hitCore[i] = Fixed(Art.CoreDot, OrderImpact);
            }

            aura = Fixed(Art.SoftDot, OrderAura);
            appearGlow = Fixed(Art.SoftDot, OrderAura);
            appearRing = Fixed(Art.Ring, OrderAppear);
            cannon = Fixed(frame, OrderCannon, false);
            fuseSegments = new SpriteRenderer[Mathf.Clamp(d.fuseSegments, 1, 24)];
            for (var i = 0; i < fuseSegments.Length; i++)
                fuseSegments[i] = Fixed(d.fuseSegmentSprite != null ? d.fuseSegmentSprite : Art.Solid, OrderFuse, false);
            charge = Fixed(frame, OrderCharge);
            chargeTexture = frame != null ? frame.texture : null;
            sparkles = new SpriteRenderer[Mathf.Max(0, d.entranceSparkles)];
            for (var i = 0; i < sparkles.Length; i++) sparkles[i] = Fixed(Art.Star, OrderGlint);
            muzzleCharge = Fixed(Art.SoftDot, OrderGlint);
            motes = new SpriteRenderer[Mathf.Max(0, d.chargeMotes)];
            for (var i = 0; i < motes.Length; i++) motes[i] = Fixed(Art.SoftDot, OrderGlint);
            fuseGlow = Fixed(Art.SoftDot, OrderGlint);
            fuseStar = Fixed(d.fuseSparkSprite != null ? d.fuseSparkSprite : Art.Star, OrderFuseSpark);
            fuseCore = Fixed(Art.CoreDot, OrderFuseSpark);

            ballFringe = Fixed(Art.SoftDot, OrderBallFringe);
            ballFlame = Fixed(Art.SoftDot, OrderBallFlame);
            ballBody = Fixed(d.ballSprite != null ? d.ballSprite : Art.Ball, OrderBall, false);
            ballCore = Fixed(Art.CoreDot, OrderBallCore);

            flashGlow = Fixed(Art.SoftDot, OrderFlash);
            flashBurst = Fixed(Art.Sunburst, OrderFlash);
            flashRing = Fixed(Art.Ring, OrderFlashTop);
            flashCore = Fixed(Art.CoreDot, OrderFlashTop);
            endGlow = Fixed(Art.SoftDot, OrderFlash);
            endRing = Fixed(Art.Ring, OrderFlashTop);
        }

        // ------------------------------------------------------------------------------------------ animação

        static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
        static float EaseOut(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x); }

        static float EaseOutBack(float x)
        {
            x = Mathf.Clamp01(x);
            const float c1 = 2.2f, c3 = c1 + 1f;   // passa do tamanho final e volta (quique)
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        static Color A(Color c, float a) { c.a = Mathf.Clamp01(a); return c; }
        static float Sw(Sprite s) => s == null ? 1f : Mathf.Max(0.01f, s.bounds.size.x);
        static float Sh(Sprite s) => s == null ? 1f : Mathf.Max(0.01f, s.bounds.size.y);

        static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            var u = 1f - t;
            return a * (u * u) + b * (2f * u * t) + c * (t * t);
        }

        /// Posiciona um sprite com largura/altura em unidades de mundo (independe do pixels-per-unit).
        static void Place(SpriteRenderer sr, Vector3 position, float width, float height, Color color, float angle = 0f)
        {
            if (sr == null) return;
            sr.transform.position = position;
            sr.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            sr.transform.localScale = new Vector3(Mathf.Max(0.0005f, width) / Sw(sr.sprite), Mathf.Max(0.0005f, height) / Sh(sr.sprite), 1f);
            sr.color = color;
        }

        static void Hide(SpriteRenderer sr)
        {
            if (sr != null) sr.color = new Color(1f, 1f, 1f, 0f);
        }

        /// Posiciona tudo no instante t (segundos desde o início).
        void Evaluate(float t)
        {
            var up = Vector3.up;
            var aim = Aim;
            var white = Color.white;

            // ---- tela escura: entra rápido, sai logo depois que a bala some
            if (cam != null && cam.orthographic)
            {
                var fade = Mathf.Max(0.01f, d.dimFadeSeconds);
                var dimK = Smooth(t / fade) * (1f - Smooth((t - (tDimEnd - fade)) / fade));
                var height = cam.orthographicSize * 2f * 1.4f;
                var width = height * Mathf.Max(1f, cam.aspect) * 1.1f;
                var center = shaking ? camBase : cam.transform.position;
                Place(dim, new Vector3(center.x, center.y, z), width, height, A(Color.black, d.dimAlpha * dimK));
            }
            else Hide(dim);

            // ---- canhão: entrada, carga, coice, saída
            var beforeFire = t < tFire;
            var sinceFire = t - tFire;
            var enter = tFuse > 0f ? Mathf.Clamp01(t / tFuse) : 1f;
            var burn = t < tFuse ? 0f : beforeFire ? Mathf.Clamp01((t - tFuse) / Mathf.Max(0.001f, tFire - tFuse)) : 1f;
            var chargeK = beforeFire ? burn * burn : Mathf.Exp(-sinceFire * 9f);
            var pop = Mathf.Max(0.02f, EaseOutBack(enter));
            var slide = d.entranceSlideCells * cell * (1f - EaseOut(enter));
            var swell = 1f + d.fuseSwell * (beforeFire ? burn * burn : Mathf.Exp(-sinceFire * 14f));
            var tremble = beforeFire ? d.fuseTrembleCells * cell * burn * burn : 0f;
            float back = 0f, along = 1f, across = 1f, tilt = 0f;
            if (!beforeFire)
            {
                var r = sinceFire / Mathf.Max(0.05f, d.recoilSeconds);
                var curve = d.recoilCurve != null && d.recoilCurve.length > 0 ? d.recoilCurve.Evaluate(Mathf.Clamp01(r)) : Mathf.Exp(-r * 5f);
                back = d.recoilCells * cell * curve;
                var kick = Mathf.Exp(-r * 6f);
                along = 1f - d.recoilSquash * kick * Mathf.Cos(r * 16f);
                across = 1f + d.recoilSquash * 0.6f * kick * Mathf.Cos(r * 16f);
                tilt = d.recoilTiltDegrees * kick * Mathf.Sin(r * 20f);
            }
            var leave = Mathf.Clamp01((t - tCannonFade) / Mathf.Max(0.01f, d.cannonFadeSeconds));
            var cannonAlpha = Smooth(enter * 2.5f) * (1f - Smooth(leave));
            var size = unit * pop * swell * (1f - 0.18f * Smooth(leave));
            var rotation = Quaternion.Euler(0f, 0f, d.artAngle + tilt);
            var barrel = rotation * Vector3.right;
            var pivot = pivotRest - aim * (slide + back) + new Vector3(Mathf.Sin(t * 83f), Mathf.Sin(t * 71f + 1.3f), 0f) * tremble;
            var pxToWorld = size / Mathf.Max(1f, d.pixelsPerUnit);
            var muzzle = pivot + barrel * (d.muzzleOffsetPx * pxToWorld * along);
            var breech = pivot - barrel * (d.breechOffsetPx * pxToWorld * along);
            var cannonLength = (d.muzzleOffsetPx + d.breechOffsetPx) / Mathf.Max(1f, d.pixelsPerUnit) * unit;

            Sprite frame = null;
            if (d.useFireFrames && fireSprites.Length > 0 && !beforeFire && sinceFire < fireSprites.Length * d.fireFrameTime)
                frame = fireSprites[Mathf.Clamp((int)(sinceFire / d.fireFrameTime), 0, fireSprites.Length - 1)];
            else if (idleSprites.Length > 0)
                frame = idleSprites[(int)(t / Mathf.Max(0.01f, d.idleFrameTime)) % idleSprites.Length];

            if (cannon != null)
            {
                cannon.sprite = frame;
                cannon.transform.SetPositionAndRotation(pivot, rotation);
                cannon.transform.localScale = new Vector3(size * along, size * across, 1f);
                cannon.color = new Color(1f, 1f, 1f, cannonAlpha);
            }
            if (charge != null)
            {
                // Cópia aditiva da própria arte: o canhão "acende" na sua silhueta conforme carrega.
                charge.sprite = frame;
                var texture = frame != null ? frame.texture : null;
                if (texture != chargeTexture)
                {
                    chargeTexture = texture;
                    var material = AdditiveFor(texture);
                    if (material != null) charge.sharedMaterial = material;
                }
                charge.transform.SetPositionAndRotation(pivot, rotation);
                charge.transform.localScale = new Vector3(size * along, size * across, 1f);
                charge.color = AdditiveBase != null ? A(d.chargeColor, d.chargeAlpha * chargeK * cannonAlpha) : new Color(1f, 1f, 1f, 0f);
            }

            var auraSize = cannonLength * 1.7f * pop;
            Place(aura, pivot, auraSize, auraSize, A(d.auraColor, d.auraAlpha * cannonAlpha * (0.7f + 0.6f * chargeK) * (0.9f + 0.1f * Mathf.Sin(t * 9f))));

            if (t < tFuse)
            {
                var ringSize = cannonLength * (0.5f + 1.5f * EaseOut(enter));
                Place(appearRing, pivot, ringSize, ringSize, A(d.entranceRingColor, d.entranceGlowAlpha * (1f - enter)));
                Place(appearGlow, pivot, cannonLength * 2.2f, cannonLength * 2.2f, A(d.entranceGlowColor, d.entranceGlowAlpha * (1f - enter) * (1f - enter)));
            }
            else if (leave > 0f && leave < 1f)
            {
                // Some num sopro de luz
                var ringSize = cannonLength * (0.6f + 1f * EaseOut(leave));
                Place(appearRing, pivot, ringSize, ringSize, A(d.entranceGlowColor, 0.6f * d.entranceGlowAlpha * (1f - leave) * Mathf.Min(1f, leave * 6f)));
                Hide(appearGlow);
            }
            else { Hide(appearRing); Hide(appearGlow); }

            for (var i = 0; i < sparkles.Length; i++)
            {
                var local = enter * 1.6f - i * 0.09f;
                if (t >= tFuse + 0.25f || local <= 0f || local >= 1f) { Hide(sparkles[i]); continue; }
                var angle = Hash(i, 11) * Mathf.PI * 2f;
                var position = pivotRest + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (cannonLength * (0.45f + 0.35f * Hash(i, 12)));
                var sparkleSize = cell * 0.34f * (0.6f + 0.8f * Hash(i, 13)) * Mathf.Sin(local * Mathf.PI);
                Place(sparkles[i], position, sparkleSize, sparkleSize, A(i % 2 == 0 ? d.entranceRingColor : d.entranceGlowColor, 0.9f * Mathf.Sin(local * Mathf.PI)), t * 120f);
            }

            // ---- pavio: gomos entre a ponta de trás do canhão e o ponto que está queimando
            var fuseLength = d.fuseLengthCells * cell * pop;
            var fuseOn = beforeFire && fuseLength > 0.001f && d.fuseWidthCells > 0f;
            var side = Vector3.right * (d.fuseSide >= 0f ? 1f : -1f);
            var f0 = breech;
            var f1 = breech + side * (fuseLength * 0.7f) - aim * (fuseLength * 0.1f);
            var f2 = breech + side * (fuseLength * 0.9f) + aim * (fuseLength * 0.55f);
            var remain = 1f - burn;
            var fuseWidth = d.fuseWidthCells * cell * pop;
            for (var j = 0; j < fuseSegments.Length; j++)
            {
                var a0 = j / (float)fuseSegments.Length;
                var a1 = Mathf.Min((j + 1f) / fuseSegments.Length, remain);
                if (!fuseOn || a1 <= a0 + 0.001f) { Hide(fuseSegments[j]); continue; }
                var q0 = Bezier(f0, f1, f2, a0);
                var q1 = Bezier(f0, f1, f2, a1);
                var delta = q1 - q0;
                Place(fuseSegments[j], (q0 + q1) * 0.5f, delta.magnitude + fuseWidth * 0.6f, fuseWidth,
                    A(j % 2 == 0 ? d.fuseColorA : d.fuseColorB, cannonAlpha), Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }

            fuseBurning = fuseOn && t >= tFuse;
            if (fuseBurning)
            {
                fusePoint = Bezier(f0, f1, f2, remain);
                var flicker = 0.82f + 0.12f * Mathf.Sin(t * 47f) + 0.06f * Mathf.Sin(t * 91f + 0.7f);
                var sparkSize = d.fuseSparkSizeCells * cell;
                Place(fuseGlow, fusePoint, sparkSize * flicker, sparkSize * flicker, A(d.fuseSparkColor, 0.75f * flicker));
                Place(fuseStar, fusePoint, sparkSize * 0.9f * flicker, sparkSize * 0.9f * flicker, A(Color.Lerp(d.fuseSparkColor, white, 0.3f), 0.9f), t * 300f);
                Place(fuseCore, fusePoint, sparkSize * 0.3f, sparkSize * 0.3f, A(d.ballCoreColor, 0.9f));
            }
            else { Hide(fuseGlow); Hide(fuseStar); Hide(fuseCore); }

            // ---- carga na boca do cano
            if (beforeFire && t >= tFuse)
            {
                var chargeSize = cell * (0.35f + 0.75f * burn);
                Place(muzzleCharge, muzzle, chargeSize, chargeSize, A(d.muzzleGlowColor, 0.75f * burn * burn * (0.85f + 0.15f * Mathf.Sin(t * 31f))));
                for (var i = 0; i < motes.Length; i++)
                {
                    var phase = Mathf.Repeat(burn * 2.2f + i / (float)motes.Length, 1f);
                    var angle = i * 2.399f + phase * 4f;
                    var position = muzzle + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (cell * 0.95f * (1f - phase));
                    var moteSize = cell * 0.13f * (1f - 0.5f * phase);
                    Place(motes[i], position, moteSize, moteSize, A(i % 2 == 0 ? d.muzzleGlowColor : d.ballFringeColor, 0.8f * Mathf.Sin(phase * Mathf.PI) * Mathf.Clamp01(burn * 4f)));
                }
            }
            else
            {
                Hide(muzzleCharge);
                for (var i = 0; i < motes.Length; i++) Hide(motes[i]);
            }

            // ---- clarão do disparo (sem fumaça: luz dourada, raios laranja e um anel mágico)
            var flashK = sinceFire / Mathf.Max(0.05f, d.muzzleFlashSeconds);
            if (flashK >= 0f && flashK < 1f)
            {
                var origin = muzzleRest + up * (cell * 0.1f);
                var flashSize = d.muzzleFlashSizeCells * cell;
                var grow = EaseOut(flashK);
                Place(flashGlow, origin, flashSize * (0.35f + 0.65f * grow), flashSize * (0.35f + 0.65f * grow), A(d.muzzleGlowColor, 0.9f * (1f - flashK) * (1f - flashK)));
                Place(flashBurst, origin, flashSize * (0.45f + 0.6f * grow), flashSize * (0.45f + 0.6f * grow), A(d.muzzleBurstColor, 0.8f * Mathf.Pow(1f - flashK, 1.5f)), 25f * flashK);
                Place(flashRing, origin, flashSize * (0.2f + 0.9f * grow), flashSize * (0.2f + 0.9f * grow), A(d.muzzleRingColor, 0.7f * (1f - flashK)));
                Place(flashCore, origin, flashSize * 0.3f * (1f - 0.5f * flashK), flashSize * 0.3f * (1f - 0.5f * flashK), A(d.ballCoreColor, 1f - flashK * 2.5f));
            }
            else { Hide(flashGlow); Hide(flashBurst); Hide(flashRing); Hide(flashCore); }

            // ---- bala de canhão e rastro
            if (!beforeFire)
            {
                var travelled = Mathf.Min(sinceFire * speed, endY - muzzleRest.y);
                var headY = muzzleRest.y + travelled;
                var after = t - tBallEnd;
                float ballAlpha;
                if (blocked) ballAlpha = after >= 0f ? 0f : 1f;
                else
                {
                    // Sai do tabuleiro sumindo aos poucos
                    var fadeStart = topY + cell * 0.5f;
                    ballAlpha = endY - fadeStart > 0.01f ? 1f - Smooth((headY - fadeStart) / (endY - fadeStart)) : (after >= 0f ? 0f : 1f);
                }
                var ballSize = d.ballSizeCells * cell * (0.5f + 0.5f * Mathf.Clamp01(sinceFire / 0.08f));
                var flicker = 1f + 0.08f * Mathf.Sin(t * 41f) + 0.05f * Mathf.Sin(t * 67f + 1f);
                var head = new Vector3(cx, headY, z);
                Place(ballFringe, head, ballSize * 2.7f * flicker, ballSize * 2.7f * flicker, A(d.ballFringeColor, d.ballFringeAlpha * ballAlpha));
                Place(ballFlame, head - up * (ballSize * 0.12f), ballSize * 1.9f * flicker, ballSize * 2.2f * flicker, A(d.ballFlameColor, d.ballFlameAlpha * ballAlpha));
                Place(ballBody, head, ballSize, ballSize, new Color(1f, 1f, 1f, ballAlpha), d.ballSpinDegrees * sinceFire);
                Place(ballCore, head + up * (ballSize * 0.22f), ballSize * 0.75f, ballSize * 0.6f, A(d.ballCoreColor, d.ballCoreAlpha * ballAlpha));

                var fadeK = after > 0f ? Smooth(after / Mathf.Max(0.01f, d.trailFadeSeconds)) : 0f;
                var trailAlpha = (1f - fadeK) * (0.35f + 0.65f * ballAlpha);
                var length = Mathf.Min(d.trailLengthCells * cell, travelled) * (1f - 0.6f * fadeK);
                var trailWidth = d.trailWidthCells * cell;
                // Do mais largo e frio (franja violeta) ao mais fino e quente (miolo), para o branco ficar só no centro.
                UpdateRibbon(ribbons[0], headY, length, trailWidth * 1.35f, d.trailFringeColor, d.trailFringeAlpha * trailAlpha, 0f, 0f);
                UpdateRibbon(ribbons[1], headY, length * 0.88f, trailWidth, d.trailFlameColor, d.trailFlameAlpha * trailAlpha, 1.3f, -t * 2.4f);
                UpdateRibbon(ribbons[2], headY, length * 0.66f, trailWidth * 0.55f, d.trailGoldColor, d.trailGoldAlpha * trailAlpha, 2.1f, 0f);
                UpdateRibbon(ribbons[3], headY, length * 0.42f, trailWidth * 0.24f, d.trailCoreColor, d.trailCoreAlpha * trailAlpha, 3.4f, 0f);

                // Coluna barrada: a bala estoura na última casa
                var burst = after / EndBurstSeconds;
                if (blocked && burst >= 0f && burst < 1f)
                {
                    var origin = new Vector3(cx, endY, z);
                    var burstSize = d.endBurstSizeCells * cell;
                    Place(endGlow, origin, burstSize * (0.3f + 0.7f * EaseOut(burst)), burstSize * (0.3f + 0.7f * EaseOut(burst)), A(d.ballFlameColor, 0.85f * (1f - burst) * (1f - burst)));
                    Place(endRing, origin, burstSize * (0.25f + 0.75f * EaseOut(burst)), burstSize * (0.25f + 0.75f * EaseOut(burst)), A(d.ballFringeColor, 0.7f * (1f - burst)));
                }
                else { Hide(endGlow); Hide(endRing); }
            }
            else
            {
                Hide(ballFringe); Hide(ballFlame); Hide(ballBody); Hide(ballCore); Hide(endGlow); Hide(endRing);
                for (var i = 0; i < ribbons.Length; i++)
                    if (ribbons[i] != null && ribbons[i].renderer != null) ribbons[i].renderer.enabled = false;
            }

            // ---- impacto em cada peça, na cor dela
            var impactSize = d.impactSizeCells * cell;
            var impactTime = Mathf.Max(0.05f, d.impactSeconds);
            for (var i = 0; i < hitFlash.Length && i < hitDone.Length; i++)
            {
                var k = (t - hitTime[i]) / impactTime;
                if (!hitDone[i] || k < 0f || k >= 1f) { Hide(hitFlash[i]); Hide(hitRing[i]); Hide(hitCore[i]); continue; }
                var grow = EaseOut(k);
                Place(hitFlash[i], hitPos[i], impactSize * (0.4f + 0.6f * grow), impactSize * (0.4f + 0.6f * grow), A(hitColor[i], d.impactAlpha * (1f - k) * (1f - k)));
                Place(hitRing[i], hitPos[i], impactSize * (0.3f + 0.7f * grow), impactSize * (0.3f + 0.7f * grow), A(Color.Lerp(hitColor[i], white, 0.3f), d.impactAlpha * 0.8f * (1f - k)));
                Place(hitCore[i], hitPos[i], cell * 0.4f * (1f - 0.4f * k), cell * 0.4f * (1f - 0.4f * k), A(d.ballCoreColor, 0.7f * (1f - k * 3f)));
            }
        }

        static float Hash(int a, int b)
        {
            unchecked
            {
                var x = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ 0x9E3779B9u;
                x ^= x >> 13; x *= 0x5bd1e995u; x ^= x >> 15;
                return (x & 0xFFFFFF) / 16777216f;
            }
        }

        // ------------------------------------------------------------------------------------------ arte feita em código

        /// Sprites e texturas gerados uma vez e compartilhados por todos os canhões (FullRect, sem recorte em polígono).
        static class Art
        {
            static Sprite solid, softDot, coreDot, ring, star, sunburst, ball;
            static Texture2D ribbonProfile, flameNoise;
            static readonly Dictionary<Texture2D, Sprite> cannonSprites = new Dictionary<Texture2D, Sprite>();
            static float cannonSpritesPpu;

            public static Sprite Solid => solid != null ? solid : solid = Make(4, (x, y) => Color.white, FilterMode.Point);

            /// Brilho redondo macio.
            public static Sprite SoftDot => softDot != null ? softDot : softDot = Make(96, (x, y) =>
            {
                var q = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y));
                return White(q * q * q * (3f - 2f * q));
            });

            /// Miolo quente, pequeno e concentrado.
            public static Sprite CoreDot => coreDot != null ? coreDot : coreDot = Make(64, (x, y) =>
            {
                var r = Mathf.Sqrt(x * x + y * y);
                return White(Mathf.Exp(-r * r * 10f) * Mathf.Clamp01((1f - r) * 4f));
            });

            /// Anel fino com brilho.
            public static Sprite Ring => ring != null ? ring : ring = Make(128, (x, y) =>
            {
                var r = Mathf.Sqrt(x * x + y * y);
                var band = (r - 0.78f) / 0.075f;
                return White(Mathf.Exp(-band * band) * Mathf.Clamp01((1f - r) * 8f));
            });

            /// Estrelinha de 4 pontas.
            public static Sprite Star => star != null ? star : star = Make(96, (x, y) =>
            {
                var r = Mathf.Sqrt(x * x + y * y);
                if (r >= 1f) return White(0f);
                return White(Mathf.Exp(-Mathf.Abs(x * y) * 70f) * Mathf.Pow(1f - r, 1.6f) * 0.9f + Mathf.Exp(-r * r * 30f));
            });

            /// Raios do clarão do disparo.
            public static Sprite Sunburst => sunburst != null ? sunburst : sunburst = Make(160, (x, y) =>
            {
                var r = Mathf.Sqrt(x * x + y * y);
                if (r >= 1f) return White(0f);
                var rays = Mathf.Pow(Mathf.Abs(Mathf.Cos(Mathf.Atan2(y, x) * 5f)), 10f);
                return White(Mathf.Pow(1f - r, 1.4f) * (0.25f + 0.75f * rays) + Mathf.Exp(-r * r * 18f) * 0.5f);
            });

            /// Bala de canhão: esfera de ferro escuro com a borda em brasa e um reflexo.
            public static Sprite Ball => ball != null ? ball : ball = Make(96, (x, y) =>
            {
                var r = Mathf.Sqrt(x * x + y * y);
                if (r >= 1f) return new Color(1f, 0.5f, 0.14f, 0f);
                var color = Color.Lerp(new Color(0.17f, 0.11f, 0.16f), new Color(1f, 0.5f, 0.14f), Mathf.Pow(r, 3f) * 0.85f);
                var highlight = Mathf.Exp(-((x + 0.3f) * (x + 0.3f) + (y - 0.35f) * (y - 0.35f)) * 14f) * 0.55f;
                color.r = Mathf.Clamp01(color.r + highlight); color.g = Mathf.Clamp01(color.g + highlight * 0.85f); color.b = Mathf.Clamp01(color.b + highlight * 0.7f);
                color.a = Mathf.Clamp01((1f - r) * 10f);
                return color;
            });

            /// Perfil macio de uma faixa (transparente nas bordas).
            public static Texture2D RibbonProfile
            {
                get
                {
                    if (ribbonProfile != null) return ribbonProfile;
                    const int h = 64;
                    ribbonProfile = new Texture2D(4, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                    for (var y = 0; y < h; y++)
                    {
                        var v = (y + 0.5f) / h * 2f - 1f;
                        var a = Mathf.Exp(-v * v * 4.5f) * Mathf.Clamp01((1f - Mathf.Abs(v)) * 5f);
                        for (var x = 0; x < 4; x++) ribbonProfile.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                    ribbonProfile.Apply();
                    return ribbonProfile;
                }
            }

            /// Labaredas em faixas (repete no comprimento): desliza pelo rastro para o fogo parecer vivo.
            public static Texture2D FlameNoise
            {
                get
                {
                    if (flameNoise != null) return flameNoise;
                    const int w = 128, h = 32;
                    flameNoise = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
                    flameNoise.wrapModeU = TextureWrapMode.Repeat; flameNoise.wrapModeV = TextureWrapMode.Clamp;
                    var pixels = new Color[w * h];
                    for (var y = 0; y < h; y++)
                        for (var x = 0; x < w; x++)
                        {
                            var u = x / (float)w; var v = (y + 0.5f) / h;
                            var n = 0.55f * ValueNoise(u * 4f, v * 3f, 4, 301) + 0.3f * ValueNoise(u * 8f, v * 6f, 8, 302) + 0.15f * ValueNoise(u * 16f, v * 10f, 16, 303);
                            var vv = v * 2f - 1f;
                            var profile = Mathf.Exp(-vv * vv * 2.2f) * Mathf.Clamp01((1f - Mathf.Abs(vv)) * 3f);
                            pixels[y * w + x] = new Color(1f, 1f, 1f, Smooth((n - 0.3f) / 0.36f) * profile);
                        }
                    flameNoise.SetPixels(pixels);
                    flameNoise.Apply();
                    return flameNoise;
                }
            }

            /// Um sprite por quadro do canhão, criado uma vez (centro da imagem como pivô).
            public static Sprite[] CannonSprites(Texture2D[] textures, float pixelsPerUnit)
            {
                if (textures == null) return new Sprite[0];
                pixelsPerUnit = Mathf.Max(1f, pixelsPerUnit);
                if (!Mathf.Approximately(cannonSpritesPpu, pixelsPerUnit))
                {
                    foreach (var old in cannonSprites.Values)
                        if (old != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(old); else UnityEngine.Object.DestroyImmediate(old); }
                    cannonSprites.Clear();
                    cannonSpritesPpu = pixelsPerUnit;
                }
                var result = new List<Sprite>(textures.Length);
                foreach (var texture in textures)
                {
                    if (texture == null) continue;
                    if (!cannonSprites.TryGetValue(texture, out var sprite) || sprite == null)
                    {
                        sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
                        sprite.hideFlags = HideFlags.HideAndDontSave;
                        cannonSprites[texture] = sprite;
                    }
                    result.Add(sprite);
                }
                return result.ToArray();
            }

            static Color White(float alpha) => new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));

            static Sprite Make(int n, Func<float, float, Color> pixel, FilterMode filter = FilterMode.Bilinear)
            {
                var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = filter };
                var pixels = new Color[n * n];
                for (var y = 0; y < n; y++)
                    for (var x = 0; x < n; x++)
                        pixels[y * n + x] = pixel((x + 0.5f) / n * 2f - 1f, (y + 0.5f) / n * 2f - 1f);
                texture.SetPixels(pixels);
                texture.Apply();
                var sprite = Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n, 0, SpriteMeshType.FullRect);
                sprite.hideFlags = HideFlags.HideAndDontSave;
                return sprite;
            }

            static float ValueNoise(float x, float y, int periodX, int seed)
            {
                var xi = Mathf.FloorToInt(x); var yi = Mathf.FloorToInt(y);
                var fx = x - xi; var fy = y - yi;
                fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
                int Wrap(int value) => ((value % periodX) + periodX) % periodX;
                var a = Hash(Wrap(xi) + yi * 57, seed); var b = Hash(Wrap(xi + 1) + yi * 57, seed);
                var c = Hash(Wrap(xi) + (yi + 1) * 57, seed); var e = Hash(Wrap(xi + 1) + (yi + 1) * 57, seed);
                return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, e, fx), fy);
            }
        }
    }
}
