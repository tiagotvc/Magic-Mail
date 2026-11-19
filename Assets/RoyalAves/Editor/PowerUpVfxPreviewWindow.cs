using System.Collections.Generic;
using System.Linq;
using RoyalAves.Effects;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items._Interfaces;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    /// Menu Royal Aves/VFX/Power-ups/Preview. Dispara os efeitos mágicos (match 3/4/5/L, bombas de raio 1-3 e encadeadas, line
    /// cleaner nos 4 sentidos) num tabuleiro de mentira, quantas vezes quiser, com câmera lenta, zoom e os parâmetros do prefab
    /// ao lado (gravados na hora). No Play, a parte de baixo dispara os efeitos NO JOGO REAL, com as animações originais
    /// (cacos das peças, metades do foguete) funcionando junto.
    public sealed class PowerUpVfxPreviewWindow : EditorWindow
    {
        const int Cols = 9, Rows = 7;

        PreviewRenderUtility preview;
        GameObject root;
        PowerUpVfxPlayer prefab, player;
        readonly SpriteRenderer[,] pieces = new SpriteRenderer[Cols, Rows];
        readonly Color[,] pieceColors = new Color[Cols, Rows];
        Sprite pieceSprite, tileSprite;
        // Arte real do jogo: sprites das peças (Resources/Items/Item) e ícones dos power-ups (prefabs de Resources/Items).
        readonly int[,] pieceKinds = new int[Cols, Rows];
        readonly Sprite[,] baseSprites = new Sprite[Cols, Rows];
        Sprite[] realPieces;
        Sprite iconLightBall, iconBomb, iconRocketH, iconRocketV, iconSeeker;
        Sprite[] ballFrames, seekerFrames;      // quadros de giro reais (globo e hélice)
        const float PieceSize = 0.78f;
        Material spriteMaterial;
        // Um material por textura: no preview a Unity junta os sprites num lote e desenha todos com a textura do primeiro.
        readonly Dictionary<Texture, Material> materialByTexture = new Dictionary<Texture, Material>();

        Material MaterialFor(Sprite sprite)
        {
            var texture = sprite != null ? sprite.texture : null;
            if (texture == null) return spriteMaterial;
            if (materialByTexture.TryGetValue(texture, out var material) && material != null) return material;
            material = new Material(spriteMaterial) { hideFlags = HideFlags.HideAndDontSave, mainTexture = texture };
            materialByTexture[texture] = material;
            return material;
        }
        UnityEditor.Editor editor;

        sealed class Pending { public float time; public Vector2Int cell; }
        readonly List<Pending> hides = new List<Pending>();
        readonly List<System.Action> queued = new List<System.Action>();
        readonly List<float> queuedTimes = new List<float>();

        float clock, speed = 0.6f, zoom = 1f;
        bool paused, loop;
        System.Action lastAction;
        double lastTime, nextLoopAt;
        Vector2 scroll;
        bool needsRebuild;

        [MenuItem("Royal Aves/VFX/Power-ups/Preview")]
        static void Open()
        {
            var window = GetWindow<PowerUpVfxPreviewWindow>("Power-ups VFX");
            window.minSize = new Vector2(1000, 620);
        }

        void OnEnable()
        {
            EditorApplication.update += OnUpdate;
            lastTime = EditorApplication.timeSinceStartup;
        }

        void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
            Teardown();
            if (editor != null) DestroyImmediate(editor);
        }

        void Teardown()
        {
            if (player != null) DestroyImmediate(player.gameObject);
            player = null;
            if (preview != null) { preview.Cleanup(); preview = null; }
            foreach (var material in materialByTexture.Values) if (material != null) DestroyImmediate(material);
            materialByTexture.Clear();
            root = null;
        }

        // ------------------------------------------------------------------------------------------ montagem

        bool EnsureReady()
        {
            if (prefab == null)
            {
                if (!System.IO.File.Exists(PowerUpVfxBuilder.PrefabPath)) PowerUpVfxBuilder.Build();
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(PowerUpVfxBuilder.PrefabPath);
                prefab = go != null ? go.GetComponent<PowerUpVfxPlayer>() : null;
                if (prefab == null) return false;
            }
            if (preview != null && root != null && player != null && !needsRebuild) return true;

            Teardown();
            needsRebuild = false;
            preview = new PreviewRenderUtility();
            var camera = preview.camera;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.05f, 0.13f, 1f);
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;

            if (pieceSprite == null) pieceSprite = MakeRounded(64, 0.28f);
            LoadGameArt();
            if (tileSprite == null) tileSprite = MakeRounded(64, 0.18f);
            if (spriteMaterial == null) spriteMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave };

            root = new GameObject("Tabuleiro de teste");
            preview.AddSingleGO(root);
            BuildBoard();

            player = Instantiate(prefab);
            player.name = "Player (preview)";
            player.manual = true;
            player.viewCamera = preview.camera;
            preview.AddSingleGO(player.gameObject);
            return true;
        }

        void LoadGameArt()
        {
            var item = Resources.Load<GameObject>("Items/Item");
            var colorable = item != null ? item.GetComponentInChildren<IColorableComponent>(true) : null;
            realPieces = colorable != null && colorable.Sprites.Count > 0 && colorable.Sprites[0].Sprites != null
                ? colorable.Sprites[0].Sprites.Where(sprite => sprite != null).Take(5).ToArray() : new Sprite[0];
            iconLightBall = IconOf("MULTICOLOR");
            iconBomb = IconOf("PACKAGE");
            iconRocketH = IconOf("HORIZONTAL_STRIPED");
            iconRocketV = IconOf("VERTICAL_STRIPED");
            iconSeeker = IconOf("MARMALADE");
            var ballPrefab = Resources.Load<GameObject>("Items/MULTICOLOR");
            var ballItem = ballPrefab != null ? ballPrefab.GetComponentInChildren<SweetSugar.Scripts.Items.ItemMulticolor>(true) : null;
            ballFrames = ballItem != null && ballItem.SpinFrames != null && ballItem.SpinFrames.Length > 0 ? ballItem.SpinFrames : null;
            var seekerPrefab = Resources.Load<GameObject>("Items/MARMALADE");
            var seekerFly = seekerPrefab != null ? seekerPrefab.GetComponentInChildren<SweetSugar.Scripts.Items.MarmaladeFly>(true) : null;
            seekerFrames = seekerFly != null && seekerFly.SpinFrames != null && seekerFly.SpinFrames.Length > 0 ? seekerFly.SpinFrames : null;
        }

        /// O sprite principal de um prefab de item: o filho "Sprite" se existir, senão o maior; por último, a primeira cor do item.
        static Sprite IconOf(string resource)
        {
            var go = Resources.Load<GameObject>("Items/" + resource);
            if (go == null) return null;
            var renderer = go.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.sprite != null)
                .OrderByDescending(r => r.name == "Sprite").ThenByDescending(r => r.sprite.bounds.size.x * r.sprite.bounds.size.y).FirstOrDefault();
            if (renderer != null) return renderer.sprite;
            var colorable = go.GetComponentInChildren<IColorableComponent>(true);
            return colorable != null && colorable.Sprites.Count > 0 && colorable.Sprites[0].Sprites.Length > 0 ? colorable.Sprites[0].Sprites[0] : null;
        }

        /// Troca o desenho de uma casa (por exemplo, pelo ícone do power-up que vai disparar); ResetBoard devolve a peça.
        void SetPiece(int c, int r, Sprite sprite, float size = PieceSize)
        {
            if (sprite == null || c < 0 || c >= Cols || r < 0 || r >= Rows) return;
            var piece = pieces[c, r];
            piece.sprite = sprite;
            piece.sharedMaterial = MaterialFor(sprite);
            piece.color = sprite == pieceSprite ? pieceColors[c, r] : Color.white;
            piece.transform.localScale = Vector3.one * (size / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y));
        }

        /// Um objeto solto com o ícone real (ou o quadrado colorido, se o ícone não foi achado).
        SpriteRenderer MakeIcon(string label, Sprite icon, Color fallbackTint, Vector3 position, float size, int order)
        {
            var made = Make(label, icon != null ? icon : pieceSprite, icon != null ? Color.white : fallbackTint, position, size, order);
            if (icon != null) made.transform.localScale = Vector3.one * (size / Mathf.Max(icon.bounds.size.x, icon.bounds.size.y));
            return made;
        }

        void BuildBoard()
        {
            var colors = new[]
            {
                new Color(0.93f, 0.22f, 0.30f), new Color(0.22f, 0.78f, 0.38f), new Color(0.26f, 0.52f, 0.98f),
                new Color(1f, 0.80f, 0.16f), new Color(1f, 0.58f, 0.14f),
            };
            var rng = new System.Random(11);
            for (var c = 0; c < Cols; c++)
                for (var r = 0; r < Rows; r++)
                {
                    Make("Casa", tileSprite, new Color(0.36f, 0.27f, 0.55f), new Vector3(c, r, 0), 0.93f, 0);
                    var kind = rng.Next(colors.Length);
                    var color = colors[kind];
                    var real = realPieces != null && realPieces.Length > 0 ? realPieces[kind % realPieces.Length] : null;
                    if (real != null)
                    {
                        kind %= realPieces.Length;
                        var sampled = PieceColors.Of(real);
                        if (sampled.a > 0.5f) color = sampled;
                    }
                    pieceKinds[c, r] = kind;
                    pieceColors[c, r] = color;
                    baseSprites[c, r] = real != null ? real : pieceSprite;
                    pieces[c, r] = Make("Peça", pieceSprite, color, new Vector3(c, r, 0), 0.74f, 1);
                    SetPiece(c, r, baseSprites[c, r]);
                }
        }

        static Sprite MakeRounded(int size, float radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
            var r = radius * size;
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Max(Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - r), 0f);
                    var dy = Mathf.Max(Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - r), 0f);
                    var a = Mathf.Clamp01(0.5f - (Mathf.Sqrt(dx * dx + dy * dy) - r));
                    var shade = Mathf.Lerp(0.82f, 1f, y / (float)size);
                    texture.SetPixel(x, y, new Color(shade, shade, shade, a));
                }
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        SpriteRenderer Make(string label, Sprite sprite, Color color, Vector3 position, float size, int order)
        {
            var go = new GameObject(label);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = position;
            go.transform.localScale = Vector3.one * (size / sprite.bounds.size.x);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = MaterialFor(sprite);
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        // ------------------------------------------------------------------------------------------ disparos

        void ResetBoard()
        {
            for (var c = 0; c < Cols; c++)
                for (var r = 0; r < Rows; r++)
                {
                    pieces[c, r].enabled = true;
                    pieces[c, r].transform.localPosition = new Vector3(c, r, 0);
                    SetPiece(c, r, baseSprites[c, r]);
                }
            hatPlan = null; staffPlan = null;
            ClearCombos();
            ClearBoosters();
            hides.Clear(); queued.Clear(); queuedTimes.Clear();
            foreach (var flight in flights) if (flight.go != null) DestroyImmediate(flight.go);
            flights.Clear();
            if (mockBall != null) DestroyImmediate(mockBall);
            if (mockA != null) DestroyImmediate(mockA.gameObject);
            if (mockB != null) DestroyImmediate(mockB.gameObject);
            mockPlan = null;
            player?.StopAll();
            clock = 0f;
        }

        static Vector3 W(int c, int r) => new Vector3(c, r, 0f);

        void Fire(System.Action action)
        {
            lastAction = action;
            ResetBoard();
            action();
        }

        void HideAt(int c, int r, float time)
        {
            if (c < 0 || c >= Cols || r < 0 || r >= Rows) return;
            hides.Add(new Pending { time = clock + time, cell = new Vector2Int(c, r) });
        }

        void FireMatch(IList<Vector2Int> cells)
        {
            Fire(() =>
            {
                foreach (var cell in cells)
                {
                    player.PlayMatch(W(cell.x, cell.y), pieceColors[cell.x, cell.y], 1f);
                    HideAt(cell.x, cell.y, 0.02f);
                }
            });
        }

        void FireBomb(int radius, int cx = Cols / 2, int cy = Rows / 2, bool keepBoard = false)
        {
            System.Action fire = () =>
            {
                SetPiece(cx, cy, iconBomb, 0.86f);
                var cells = new List<Vector3>(); var colors = new List<Color>();
                for (var c = 0; c < Cols; c++)
                    for (var r = 0; r < Rows; r++)
                        if (Mathf.Abs(c - cx) <= radius && Mathf.Abs(r - cy) <= radius)
                        {
                            cells.Add(W(c, r)); colors.Add(pieceColors[c, r]);
                            HideAt(c, r, 0.05f + 0.025f * Mathf.Max(Mathf.Abs(c - cx), Mathf.Abs(r - cy)));
                        }
                player.PlayBomb(W(cx, cy), radius, 1f, cells, colors);
            };
            if (keepBoard) fire(); else Fire(fire);
        }

        // Girocóptero de mentira: espera parado (decolagem), voa em arco até o alvo (rastro + marcador) e bate (impacto).
        sealed class MockFlight { public GameObject go; public Vector3 start, control, end; public float startAt, time; public Vector2Int target; public bool marked; public System.Action onArrive; }
        readonly List<MockFlight> flights = new List<MockFlight>();
        const float MockWindUp = 0.35f;

        /// from: de onde decola (vazio = um canto); cargo: power-up que ele carrega; onArrive: o que acontece na chegada em vez do impacto normal.
        void LaunchSeeker(Vector2Int target, Vector3? origin = null, Sprite cargo = null, System.Action onArrive = null)
        {
            var from = target.x >= Cols / 2 ? new Vector2Int(1, 1) : new Vector2Int(Cols - 2, 1);
            var start = origin ?? W(from.x, from.y); var end = W(target.x, target.y);
            var distance = Vector3.Distance(start, end);
            var apex = Vector3.Lerp(start, end, 0.5f) + Vector3.up * Mathf.Max(1f, distance * 0.35f);
            var copter = MakeIcon("Girocóptero", iconSeeker, new Color(0.62f, 0.3f, 0.9f), start, 0.8f, 6);
            copter.sortingLayerName = player.sortingLayer; copter.sortingOrder = player.sortingBase + 20;
            flights.Add(new MockFlight { go = copter.gameObject, start = start, end = end, control = 2f * apex - (start + end) * 0.5f, startAt = clock, time = Mathf.Clamp(distance / 5f, 0.5f, 1.4f), target = target, onArrive = onArrive });
            if (cargo != null)
            {
                var load = MakeIcon("Carga", cargo, Color.white, start + Vector3.down * 0.32f, 0.5f, 6);
                load.sortingLayerName = player.sortingLayer; load.sortingOrder = player.sortingBase + 19;
                load.transform.SetParent(copter.transform, true);
            }
            player.PlaySeekerFlight(copter.transform, 1f);
            if (seekerFrames == null) player.PlaySeekerWhirl(copter.transform, 0f, 1f);      // vento de giro em volta, o voo inteiro
        }

        // ------------------------------------------------------------------------------------------ power-ups de uso
        // Flecha, martelo e canhão: os efeitos de verdade do jogo, com o tempo controlado por esta janela.

        ArrowFlightEffect arrowFx; float arrowStart;
        CannonFireRunner cannonFx; float cannonLast;
        SpriteRenderer hammerSr; float hammerStart; Vector2Int hammerCell; bool hammerHit;

        void ClearBoosters()
        {
            if (arrowFx != null) DestroyImmediate(arrowFx.gameObject);
            if (cannonFx != null) DestroyImmediate(cannonFx.gameObject);
            if (hammerSr != null) DestroyImmediate(hammerSr.gameObject);
            arrowFx = null; cannonFx = null; hammerSr = null;
        }

        /// Flecha: atravessa a linha do meio, da esquerda até a borda direita da tela.
        void FireArrow()
        {
            Fire(() =>
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowFlightBuilder.PrefabPath);
                var effect = source != null ? source.GetComponent<ArrowFlightEffect>() : null;
                if (effect == null) { Debug.LogWarning("Prefab da flecha não encontrado: " + ArrowFlightBuilder.PrefabPath); return; }
                const int row = 3;
                var positions = Enumerable.Range(0, Cols).Select(c => W(c, row)).ToList();
                arrowFx = ArrowFlightEffect.CreatePreview(effect, positions, 1f, i => { if (i >= 0) HideAt(i, row, 0f); }, preview.camera);
                preview.AddSingleGO(arrowFx.gameObject);
                arrowStart = clock;
            });
        }

        /// Canhão: entra embaixo da coluna do meio, acende o pavio e a bala sobe quebrando as peças.
        void FireCannon()
        {
            Fire(() =>
            {
                const int col = 4;
                var rows = Enumerable.Range(0, Rows).OrderBy(r => W(col, r).y).ToList();
                cannonFx = CannonFireEffect.CreatePreview(rows.Select(r => W(col, r)).ToList(), rows.Select(r => pieceColors[col, r]).ToList(),
                    i => { if (i >= 0 && i < rows.Count) HideAt(col, rows[i], 0f); }, preview.camera);
                if (cannonFx == null) { Debug.LogWarning("Arte do canhão não configurada (Resources/Effects/CannonFireEffectData)."); return; }
                preview.AddSingleGO(cannonFx.gameObject);
                cannonLast = clock;
            });
        }

        /// Martelo: aparece em cima da peça do meio, pega impulso e golpeia.
        void FireHammer()
        {
            Fire(() =>
            {
                hammerCell = new Vector2Int(4, 3);
                if (!HammerSmashEffect.PreviewFrame(0f, out var first, out _, out _)) { Debug.LogWarning("Arte do martelo não configurada (Resources/Effects/HammerSmashEffectData)."); return; }
                hammerSr = Make("Martelo", first, Color.white, W(hammerCell.x, hammerCell.y), 1f, 6);
                hammerSr.sortingLayerName = player.sortingLayer; hammerSr.sortingOrder = player.sortingBase + 20;
                hammerStart = clock; hammerHit = false;
                StepBoosters();
            });
        }

        void StepBoosters()
        {
            if (arrowFx != null)
            {
                var t = clock - arrowStart;
                arrowFx.Evaluate(Mathf.Min(t, arrowFx.Duration));
                if (t > arrowFx.Duration) { DestroyImmediate(arrowFx.gameObject); arrowFx = null; }
            }
            if (cannonFx != null)
            {
                var dt = Mathf.Max(0f, clock - cannonLast); cannonLast = clock;
                if (cannonFx.IsRunning) cannonFx.Step(dt);
                else { DestroyImmediate(cannonFx.gameObject); cannonFx = null; }
            }
            if (hammerSr != null)
            {
                var t = clock - hammerStart;
                if (HammerSmashEffect.PreviewFrame(t, out var sprite, out var impactAt, out var scale))
                {
                    hammerSr.sprite = sprite; hammerSr.sharedMaterial = MaterialFor(sprite);
                    hammerSr.transform.localScale = Vector3.one * (scale / 1.2f);          // no jogo a casa tem 1,2; aqui tem 1
                    if (!hammerHit && t >= impactAt)
                    {
                        hammerHit = true;
                        player.PlayMatch(W(hammerCell.x, hammerCell.y), pieceColors[hammerCell.x, hammerCell.y], 1f);
                        HideAt(hammerCell.x, hammerCell.y, 0f);
                    }
                }
                else { DestroyImmediate(hammerSr.gameObject); hammerSr = null; }
            }
        }

        void StepFlights()
        {
            StepBoosters();
            StepDouble();
            StepHat();
            StepCombos();
            for (var i = flights.Count - 1; i >= 0; i--)
            {
                var flight = flights[i];
                var age = clock - flight.startAt - MockWindUp;
                // rotor novo: gira no próprio plano desde a decolagem (com carga pendurada fica parado, para a carga não rodar junto)
                if (seekerFrames == null && flight.go.transform.childCount == 0)
                    flight.go.transform.localRotation = Quaternion.Euler(0f, 0f, -player.whirl.flightSpin * (clock - flight.startAt));
                if (age < 0f) continue;
                if (!flight.marked) { flight.marked = true; player.PlaySeekerMarker(flight.end, flight.time, 1f); }
                var u = Mathf.Clamp01(age / flight.time);
                var e = u * u * (3f - 2f * u);
                flight.go.transform.position = (1f - e) * (1f - e) * flight.start + 2f * (1f - e) * e * flight.control + e * e * flight.end;
                if (u < 1f) continue;
                if (flight.onArrive != null) flight.onArrive();
                else
                {
                    player.PlaySeekerImpact(flight.end, pieceColors[flight.target.x, flight.target.y], 1f);
                    HideAt(flight.target.x, flight.target.y, 0.04f);
                }
                DestroyImmediate(flight.go);
                flights.RemoveAt(i);
            }
        }

        void FireSeeker(Vector2Int cell)
        {
            Fire(() => LaunchSeeker(cell));
        }

        void FireSeekers()
        {
            Fire(() =>
            {
                var targets = new[] { new Vector2Int(2, 4), new Vector2Int(5, 2), new Vector2Int(7, 5) };
                for (var i = 0; i < targets.Length; i++)
                {
                    var target = targets[i];
                    queuedTimes.Add(clock + 0.3f * i);
                    queued.Add(() => LaunchSeeker(target));
                }
            });
        }

        // Light ball de mentira: carrega, conecta peça por peça (todas de uma cor, ou o tabuleiro todo) e explode tudo junto.
        GameObject mockBall;

        void FireLightBall(bool wholeBoard)
        {
            Fire(() =>
            {
                var origin = new Vector2Int(4, 3);
                var ball = MakeIcon("Light ball", iconLightBall, Color.white, W(origin.x, origin.y), 0.86f, 6);
                mockBall = ball.gameObject;
                Spin(ball);
                pieces[origin.x, origin.y].enabled = false;
                var owner = ball.transform;
                player.PlayLightBallCharge(owner, owner, 1f);
                var kind = pieceKinds[5, 3];
                var targets = new List<Vector2Int>();
                for (var c = 0; c < Cols; c++)
                    for (var r = 0; r < Rows; r++)
                        if ((c != origin.x || r != origin.y) && (wholeBoard || pieceKinds[c, r] == kind)) targets.Add(new Vector2Int(c, r));
                var step = wholeBoard ? 0.012f : 0.08f;
                for (var i = 0; i < targets.Count; i++)
                {
                    var target = targets[i];
                    queuedTimes.Add(clock + 0.3f + step * i);
                    queued.Add(() => player.PlayLightBallBeam(owner, pieces[target.x, target.y].transform, pieceColors[target.x, target.y]));
                }
                queuedTimes.Add(clock + 0.3f + step * targets.Count + 0.3f);
                queued.Add(() =>
                {
                    player.DetonateLightBall(owner);
                    ball.enabled = false;
                    foreach (var target in targets) HideAt(target.x, target.y, 0.03f);
                });
            });
        }

        // Combinação de dois light balls: dois globos de mentira seguem o roteiro (órbita e colapso) e as peças somem quando a onda chega.
        PowerUpVfxPlayer.DoubleBallPlan mockPlan;
        SpriteRenderer mockA, mockB;
        float mockStart;

        void FireDoubleBall()
        {
            Fire(() =>
            {
                var a = new Vector2Int(4, 3); var b = new Vector2Int(5, 3);
                mockA = MakeIcon("Light ball A", iconLightBall, Color.white, W(a.x, a.y), 0.86f, 6);
                mockB = MakeIcon("Light ball B", iconLightBall, new Color(1f, 0.9f, 0.95f), W(b.x, b.y), 0.86f, 6);
                foreach (var ball in new[] { mockA, mockB }) { ball.sortingLayerName = player.sortingLayer; ball.sortingOrder = player.sortingBase - 1; }
                pieces[a.x, a.y].enabled = false; pieces[b.x, b.y].enabled = false;
                var cells = new List<Vector3>(); var colors = new List<Color>();
                for (var c = 0; c < Cols; c++)
                    for (var r = 0; r < Rows; r++)
                        if (!(c == a.x && r == a.y) && !(c == b.x && r == b.y)) { cells.Add(W(c, r)); colors.Add(pieceColors[c, r]); }
                Spin(mockA, false); Spin(mockB, false);
                mockPlan = player.PlayDoubleLightBall(mockA.transform, mockB.transform, cells, colors, 1f);
                mockStart = clock;
                for (var c = 0; c < Cols; c++)
                    for (var r = 0; r < Rows; r++)
                        if (!(c == a.x && r == a.y) && !(c == b.x && r == b.y)) HideAt(c, r, mockPlan.ExplodeAt + mockPlan.HitTime(W(c, r)));
            });
        }

        void StepDouble()
        {
            if (mockPlan == null || mockA == null || mockB == null) return;
            var t = clock - mockStart;
            if (t >= mockPlan.ExplodeAt) { mockA.enabled = false; mockB.enabled = false; return; }
            mockPlan.Pose(t, out var a, out var b, out var scaleA, out var scaleB, out var aInFront);
            mockA.transform.position = a; mockB.transform.position = b;
            var unit = 0.86f / Mathf.Max(mockA.sprite.bounds.size.x, mockA.sprite.bounds.size.y);
            mockA.transform.localScale = Vector3.one * (unit * scaleA); mockB.transform.localScale = Vector3.one * (unit * scaleB);
            mockA.sortingOrder = aInFront ? mockPlan.FrontOrder : mockPlan.BackOrder;
            mockB.sortingOrder = aInFront ? mockPlan.BackOrder : mockPlan.FrontOrder;
        }

        // Chapéu de Mago de mentira: as peças do tabuleiro seguem o roteiro do redemoinho; depois o cajado troca a cor de algumas.
        PowerUpVfxPlayer.HatShufflePlan hatPlan;
        PowerUpVfxPlayer.StaffCastPlan staffPlan;
        readonly List<SpriteRenderer> hatPieces = new List<SpriteRenderer>();
        readonly List<Vector3> hatStart = new List<Vector3>(), hatTarget = new List<Vector3>(), hatScale = new List<Vector3>();
        readonly List<SpriteRenderer> staffPieces = new List<SpriteRenderer>();
        readonly List<Sprite> staffSprites = new List<Sprite>();
        float hatStartTime, staffStartTime;
        int staffNext;

        void FireHat()
        {
            Fire(() =>
            {
                hatPieces.Clear(); hatStart.Clear(); hatTarget.Clear(); hatScale.Clear();
                var colors = new List<Color>();
                for (var c = 0; c < Cols; c++)
                    for (var r = 0; r < Rows; r++)
                    {
                        hatPieces.Add(pieces[c, r]); hatStart.Add(W(c, r)); hatScale.Add(pieces[c, r].transform.localScale); colors.Add(pieceColors[c, r]);
                    }
                hatTarget.AddRange(hatStart);
                var rng = new System.Random(5);
                for (var i = hatTarget.Count - 1; i > 0; i--)
                {
                    var j = rng.Next(i + 1);
                    (hatTarget[i], hatTarget[j]) = (hatTarget[j], hatTarget[i]);
                }
                hatPlan = player.PlayHatShuffle(hatPieces.Select(piece => piece.transform).ToList(), colors, new Vector3((Cols - 1) * 0.5f, (Rows - 1) * 0.5f, 0f), 1f);
                hatStartTime = clock;
            });
        }

        void StepHat()
        {
            if (hatPlan != null)
            {
                var t = clock - hatStartTime;
                for (var i = 0; i < hatPieces.Count; i++)
                {
                    hatPieces[i].transform.position = hatPlan.Pose(hatStart[i], hatTarget[i], t, out var scale);
                    hatPieces[i].transform.localScale = hatScale[i] * scale;
                }
                if (t < hatPlan.Seconds) return;
                for (var i = 0; i < hatPieces.Count; i++) { hatPieces[i].transform.position = hatTarget[i]; hatPieces[i].transform.localScale = hatScale[i]; }
                hatPlan = null;

                // cajado: troca a figura de algumas peças pela de outra cor
                staffPieces.Clear(); staffSprites.Clear(); staffNext = 0;
                var targets = new List<Vector3>(); var tints = new List<Color>();
                var rng = new System.Random(9);
                var kinds = realPieces != null ? realPieces.Length : 0;
                for (var n = 0; n < player.hat.recolorCount && kinds > 1; n++)
                {
                    var index = rng.Next(hatPieces.Count);
                    if (staffPieces.Contains(hatPieces[index])) continue;
                    var sprite = realPieces[rng.Next(kinds)];
                    if (sprite == hatPieces[index].sprite) continue;
                    staffPieces.Add(hatPieces[index]); staffSprites.Add(sprite);
                    targets.Add(hatTarget[index]); tints.Add(PieceColors.Of(sprite));
                }
                staffPlan = targets.Count > 0 ? player.PlayStaffCast(new Vector3((Cols - 1) * 0.5f, -player.hat.staffBelowBoard, 0f), targets, tints, 1f) : null;
                staffStartTime = clock;
            }
            if (staffPlan == null) return;
            var elapsed = clock - staffStartTime;
            while (staffNext < staffPieces.Count && staffPlan.HitTime(staffNext) <= elapsed)
            {
                var piece = staffPieces[staffNext]; var sprite = staffSprites[staffNext];
                piece.sprite = sprite; piece.sharedMaterial = MaterialFor(sprite); piece.color = Color.white;
                piece.transform.localScale = Vector3.one * (PieceSize / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y));
                staffNext++;
            }
            if (staffNext >= staffPieces.Count) staffPlan = null;
        }

        // ------------------------------------------------------------------------------------------ combinações de dois power-ups
        // Reproduz o que o jogo faz hoje em cada combinação (mesmos tempos e mesmas chamadas ao PowerUpVfxPlayer), com peças de
        // mentira. O que não é do PowerUpVfxPlayer (pavio da bomba, metades do foguete voando) não aparece aqui.
        sealed class ComboTween { public Transform target; public Vector3 from, to, scale; public float toMul, start, seconds; public bool hide; }
        readonly List<GameObject> comboObjects = new List<GameObject>();
        readonly List<ComboTween> tweens = new List<ComboTween>();
        static readonly Vector2Int ComboA = new Vector2Int(4, 3), ComboB = new Vector2Int(5, 3);
        const int BlastRadius = 2;          // ItemDestroyAnimation.BlastRadius
        const float BeamStep = 0.08f;       // ItemMulticolor.BeamStepDelay

        void After(float delay, System.Action action) { queuedTimes.Add(clock + delay); queued.Add(action); }

        void ClearCombos()
        {
            foreach (var go in comboObjects) if (go != null) DestroyImmediate(go);
            comboObjects.Clear(); tweens.Clear(); spins.Clear(); arrivals.Clear(); merges.Clear();
        }

        static void Drop(SpriteRenderer sr) { if (sr != null) DestroyImmediate(sr.gameObject); }

        /// Ícone solto no lugar da peça de uma casa (para poder mover e crescer).
        SpriteRenderer ComboIcon(Sprite icon, Vector2Int cell, bool raise = true)
        {
            pieces[cell.x, cell.y].enabled = false;
            var sr = MakeIcon("Combinação", icon, Color.white, W(cell.x, cell.y), 0.86f, 6);
            if (raise) { sr.sortingLayerName = player.sortingLayer; sr.sortingOrder = player.sortingBase + 18; }
            comboObjects.Add(sr.gameObject);
            return sr;
        }

        void Tween(SpriteRenderer sr, Vector3 to, float toMul, float delay, float seconds, bool hide = false)
        {
            tweens.Add(new ComboTween { target = sr.transform, from = sr.transform.position, to = to, scale = sr.transform.localScale, toMul = toMul, start = clock + delay, seconds = seconds, hide = hide });
        }

        void StepCombos()
        {
            for (var i = tweens.Count - 1; i >= 0; i--)
            {
                var tween = tweens[i];
                if (tween.target == null) { tweens.RemoveAt(i); continue; }
                if (clock < tween.start) continue;
                var u = Mathf.Clamp01((clock - tween.start) / Mathf.Max(0.001f, tween.seconds));
                var e = u * u * (3f - 2f * u);
                tween.target.position = Vector3.Lerp(tween.from, tween.to, e);
                tween.target.localScale = tween.scale * Mathf.Lerp(1f, tween.toMul, e);
                if (u < 1f) continue;
                if (tween.hide) tween.target.GetComponent<SpriteRenderer>().enabled = false;
                tweens.RemoveAt(i);
            }
            StepSpins(); StepArrivals(); StepMerges();
        }

        // Globo ativo: os quadros de giro reais, o aperto do começo e a respiração (como ItemMulticolor.SpinCor).
        sealed class MockSpin { public SpriteRenderer sr; public Vector3 scale; public float last, age, phase; public bool scaleToo; }
        readonly List<MockSpin> spins = new List<MockSpin>();

        void Spin(SpriteRenderer sr, bool scaleToo = true)
        {
            if (sr == null) return;
            spins.Add(new MockSpin { sr = sr, scale = sr.transform.localScale, last = clock, scaleToo = scaleToo });
        }

        void StepSpins()
        {
            for (var i = spins.Count - 1; i >= 0; i--)
            {
                var spin = spins[i];
                if (spin.sr == null || !spin.sr.enabled) { spins.RemoveAt(i); continue; }
                var dt = clock - spin.last; spin.last = clock; spin.age += dt;
                var period = ballFrames != null ? ballFrames.Length * 0.018f : 360f / Mathf.Max(1f, Mathf.Abs(player.presence.ballSpin));
                spin.phase += dt * Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(spin.age / 0.16f)) / period;
                if (ballFrames != null)
                {
                    var frame = ballFrames[Mathf.FloorToInt(spin.phase * ballFrames.Length) % ballFrames.Length];
                    spin.sr.sprite = frame; spin.sr.sharedMaterial = MaterialFor(frame);
                }
                else spin.sr.transform.localRotation = Quaternion.Euler(0f, 0f, -spin.phase * 360f);      // medalhão: gira no próprio plano
                if (!spin.scaleToo) continue;
                var pose = SweetSugar.Scripts.Items.ItemMulticolor.SpinScaleAt(spin.age);
                if (ballFrames == null) { var round = (pose.x + pose.y) * 0.5f; pose = new Vector2(round, round); }      // medalhão: sem achatar
                pose *= 1f - player.presence.ballBreath * (0.5f - 0.5f * Mathf.Cos(spin.age * Mathf.PI * 2f * player.presence.ballBreathFrequency));
                spin.sr.transform.localScale = Vector3.Scale(spin.scale, new Vector3(pose.x, pose.y, 1f));
            }
        }

        // Chegada de um power-up no tabuleiro (como PowerUpArrivalMotion no jogo).
        sealed class MockArrival { public SpriteRenderer sr; public PowerUpKind kind; public float start; public Vector3 scale; public Sprite rest; public Sprite[] frames; }
        readonly List<MockArrival> arrivals = new List<MockArrival>();

        void Arrive(PowerUpKind kind, Vector2Int cell)
        {
            var icon = kind == PowerUpKind.Bomb ? iconBomb : kind == PowerUpKind.Ball ? iconLightBall : kind == PowerUpKind.Seeker ? iconSeeker : iconRocketH;
            var sr = ComboIcon(icon, cell, false);
            arrivals.Add(new MockArrival { sr = sr, kind = kind, start = clock, scale = sr.transform.localScale, rest = sr.sprite,
                frames = kind == PowerUpKind.Ball ? ballFrames : kind == PowerUpKind.Seeker ? seekerFrames : null });
            player.PlayArrival(sr.transform, kind, 1f);
            if (kind == PowerUpKind.Seeker && seekerFrames == null)
            {
                player.PlaySeekerWhirl(sr.transform, player.presence.seekerSeconds, 1f, player.whirl.arrivalWind);
                // depois de pousar, o giro de tempos em tempos (como SeekerIdleSpin no jogo); o primeiro vem logo, para dar para ver
                idleSpins.Add(new MockIdleSpin { sr = sr, nextAt = clock + player.presence.seekerSeconds + 1f });
            }
            if (kind == PowerUpKind.Ball) player.PlayLightBallIdle(sr.transform, 1f, false);      // o brilho permanente fica depois da chegada
            StepArrivals();
        }

        // Seeker parado: giro rápido de tempos em tempos, com o vento suave em volta.
        sealed class MockIdleSpin { public SpriteRenderer sr; public float nextAt, startedAt = -1f; }
        readonly List<MockIdleSpin> idleSpins = new List<MockIdleSpin>();

        void StepIdleSpins()
        {
            var s = player.whirl;
            for (var i = idleSpins.Count - 1; i >= 0; i--)
            {
                var idle = idleSpins[i];
                if (idle.sr == null) { idleSpins.RemoveAt(i); continue; }
                if (idle.startedAt < 0f)
                {
                    if (clock < idle.nextAt) continue;
                    idle.startedAt = clock;
                    player.PlaySeekerWhirl(idle.sr.transform, s.idleSeconds, 1f, s.idleWind);
                }
                var u = Mathf.Clamp01((clock - idle.startedAt) / Mathf.Max(0.05f, s.idleSeconds));
                idle.sr.transform.localRotation = Quaternion.Euler(0f, 0f, -360f * s.idleTurns * (u * u * (3f - 2f * u)));
                if (u < 1f) continue;
                idle.sr.transform.localRotation = Quaternion.identity;
                idle.startedAt = -1f;
                idle.nextAt = clock + Mathf.Min(s.idleEvery.x, s.idleEvery.y);      // no preview usa o intervalo mais curto
            }
        }

        void StepArrivals()
        {
            StepIdleSpins();
            for (var i = arrivals.Count - 1; i >= 0; i--)
            {
                var arrival = arrivals[i];
                if (arrival.sr == null) { arrivals.RemoveAt(i); continue; }
                var t = clock - arrival.start;
                var ended = t >= player.presence.ArrivalSeconds(arrival.kind);
                player.presence.ArrivalPose(arrival.kind, t, out var scale, out var phase);
                arrival.sr.transform.localScale = ended ? arrival.scale : Vector3.Scale(arrival.scale, new Vector3(scale.x, scale.y, 1f));
                if (arrival.frames != null)
                {
                    var sprite = ended ? arrival.rest : arrival.frames[Mathf.FloorToInt(phase * arrival.frames.Length) % arrival.frames.Length];
                    arrival.sr.sprite = sprite; arrival.sr.sharedMaterial = MaterialFor(sprite);
                }
                else if (arrival.kind == PowerUpKind.Ball || arrival.kind == PowerUpKind.Seeker) arrival.sr.transform.localRotation = ended ? Quaternion.identity : Quaternion.Euler(0f, 0f, -phase * 360f);
                if (ended) arrivals.RemoveAt(i);
            }
        }

        void FireArrival(PowerUpKind kind) { Fire(() => Arrive(kind, ComboA)); }

        void FireArrivalAll()
        {
            Fire(() =>
            {
                Arrive(PowerUpKind.Bomb, new Vector2Int(1, 3));
                After(0.15f, () => Arrive(PowerUpKind.Ball, new Vector2Int(3, 3)));
                After(0.3f, () => Arrive(PowerUpKind.Seeker, new Vector2Int(5, 3)));
                After(0.45f, () => Arrive(PowerUpKind.Line, new Vector2Int(7, 3)));
            });
        }

        // Junção de dois power-ups (como PowerUpMerge no jogo): gira cada um no eixo, depois um em volta do outro; "then" é a
        // animação própria da combinação, que continua com os dois de volta nas casas.
        sealed class MockMerge { public SpriteRenderer a, b; public PowerUpVfxPlayer.MergePlan plan; public float start; public Vector3 posA, posB, scaleA, scaleB; public int orderA, orderB; public System.Action then; }
        readonly List<MockMerge> merges = new List<MockMerge>();

        void Merge(SpriteRenderer a, SpriteRenderer b, System.Action then)
        {
            var plan = player.PlayMerge(a.transform, b.transform, 1f);
            a.sortingLayerName = player.sortingLayer; b.sortingLayerName = player.sortingLayer;
            merges.Add(new MockMerge { a = a, b = b, plan = plan, start = clock, posA = a.transform.position, posB = b.transform.position,
                scaleA = a.transform.localScale, scaleB = b.transform.localScale, orderA = a.sortingOrder, orderB = b.sortingOrder, then = then });
        }

        void StepMerges()
        {
            for (var i = merges.Count - 1; i >= 0; i--)
            {
                var merge = merges[i];
                if (merge.a == null || merge.b == null) { merges.RemoveAt(i); continue; }
                var t = clock - merge.start;
                if (t < merge.plan.Seconds)
                {
                    merge.plan.Pose(t, out var pa, out var pb, out var sa, out var sb, out var aInFront);
                    merge.a.transform.position = pa; merge.b.transform.position = pb;
                    merge.a.transform.localScale = Vector3.Scale(merge.scaleA, new Vector3(sa.x, sa.y, 1f));
                    merge.b.transform.localScale = Vector3.Scale(merge.scaleB, new Vector3(sb.x, sb.y, 1f));
                    merge.a.sortingOrder = aInFront ? player.MergeFrontOrder : player.MergeBackOrder;
                    merge.b.sortingOrder = aInFront ? player.MergeBackOrder : player.MergeFrontOrder;
                    continue;
                }
                merge.a.transform.position = merge.posA; merge.b.transform.position = merge.posB;
                merge.a.transform.localScale = merge.scaleA; merge.b.transform.localScale = merge.scaleB;
                merge.a.sortingOrder = merge.orderA; merge.b.sortingOrder = merge.orderB;
                merge.plan.Cancel();
                merges.RemoveAt(i);
                merge.then?.Invoke();
            }
        }

        static bool Inside(int c, int r) => c >= 0 && c < Cols && r >= 0 && r < Rows;

        /// Line cleaner para os dois lados a partir da casa, sem escurecer a tela (como no jogo fora de power-up).
        void LineAt(int c, int r, bool horizontal)
        {
            if (!Inside(c, r)) return;
            var length = horizontal ? Cols : Rows;
            var cells = new List<Vector3>(); var colors = new List<Color>();
            for (var i = 0; i < length; i++)
            {
                var cc = horizontal ? i : c; var rr = horizontal ? r : i;
                cells.Add(W(cc, rr)); colors.Add(pieceColors[cc, rr]);
                var distance = horizontal ? cc - c : rr - r;
                var side = distance < 0 ? (horizontal ? c : r) : (horizontal ? Cols - 1 - c : Rows - 1 - r);
                HideAt(cc, rr, player.LineTimeAt(Mathf.Abs(distance), side, 1f) + 0.04f);
            }
            player.PlayLine(W(c, r), horizontal, horizontal ? W(0, r) : W(c, 0), horizontal ? W(Cols - 1, r) : W(c, Rows - 1), false, false, cells, colors, 1f, false, true);
        }

        void BombAt(int cx, int cy, int radius)
        {
            var cells = new List<Vector3>(); var colors = new List<Color>();
            for (var c = 0; c < Cols; c++)
                for (var r = 0; r < Rows; r++)
                    if (Mathf.Abs(c - cx) <= radius && Mathf.Abs(r - cy) <= radius)
                    {
                        cells.Add(W(c, r)); colors.Add(pieceColors[c, r]);
                        HideAt(c, r, 0.05f + 0.025f * Mathf.Max(Mathf.Abs(c - cx), Mathf.Abs(r - cy)));
                    }
            player.PlayBomb(W(cx, cy), radius, 1f, cells, colors, false);
        }

        void MatchAt(int c, int r)
        {
            if (!Inside(c, r) || !pieces[c, r].enabled) return;
            player.PlayMatch(W(c, r), pieceColors[c, r], 1f);
            HideAt(c, r, 0.02f);
        }

        // Line + Line: junção e depois um feixe na linha e outro na coluna da casa onde a troca terminou.
        void ComboLineLine()
        {
            Fire(() =>
            {
                var first = ComboIcon(iconRocketH, ComboA); var second = ComboIcon(iconRocketV, ComboB);
                Merge(first, second, () =>
                {
                    Drop(first); Drop(second);
                    LineAt(ComboA.x, ComboA.y, true); LineAt(ComboA.x, ComboA.y, false);
                });
            });
        }

        // Line + Bomba: junção; o foguete é puxado para a bomba, que cresce (0,7 s); depois 3 linhas e 3 colunas.
        void ComboLineBomb()
        {
            Fire(() =>
            {
                var bomb = ComboIcon(iconBomb, ComboA); var rocket = ComboIcon(iconRocketH, ComboB);
                Merge(bomb, rocket, () =>
                {
                    Tween(rocket, W(ComboA.x, ComboA.y), 0.12f, 0f, 0.70f * 0.55f, true);
                    Tween(bomb, W(ComboA.x, ComboA.y), 1.55f, 0f, 0.70f);
                    After(0.70f, () =>
                    {
                        Drop(bomb); Drop(rocket);
                        for (var d = -1; d <= 1; d++) { LineAt(ComboA.x - 1, ComboA.y + d, true); LineAt(ComboA.x + d, ComboA.y + 1, false); }
                    });
                });
            });
        }

        // Bomba + Bomba: as duas se unem no meio, a bomba unida cresce acesa e a explosão dupla limpa o tabuleiro inteiro.
        void ComboBombBomb()
        {
            Fire(() =>
            {
                var main = ComboIcon(iconBomb, ComboA); var other = ComboIcon(iconBomb, ComboB);
                {
                    var s = player.presence;
                    var middle = (W(ComboA.x, ComboA.y) + W(ComboB.x, ComboB.y)) * 0.5f;
                    var join = s.doubleChargeSeconds * s.doubleJoin;
                    player.PlayDoubleBombCharge(main.transform, other.transform, s.doubleChargeSeconds, 1f);
                    Tween(main, middle, 1f, 0f, join);
                    Tween(other, middle, 0.7f, 0f, join, true);
                    After(join * 0.8f, () => Tween(main, middle, s.doubleChargeScale, 0f, s.doubleChargeSeconds - join * 0.8f));
                    After(s.doubleChargeSeconds, () =>
                    {
                        Drop(main); Drop(other);
                        var radius = Mathf.Max(Mathf.Max(ComboA.x, Cols - 1 - ComboA.x), Mathf.Max(ComboA.y, Rows - 1 - ComboA.y));
                        var cells = new List<Vector3>(); var colors = new List<Color>();
                        for (var c = 0; c < Cols; c++)
                            for (var r = 0; r < Rows; r++)
                            {
                                cells.Add(W(c, r)); colors.Add(pieceColors[c, r]);
                                HideAt(c, r, 0.05f + 0.025f * Mathf.Max(Mathf.Abs(c - ComboA.x), Mathf.Abs(r - ComboA.y)));
                            }
                        player.PlayBomb(middle, radius, 1f, cells, colors, false);
                        player.PlayDoubleBombBurst(middle, radius, 1f);
                    });
                }
            });
        }

        // Globo + outro power-up: o globo carrega, cada raio transforma uma peça da cor no power-up parceiro e, na explosão,
        // todos disparam juntos. mode: 0 = bomba (cor mais comum do tabuleiro), 1 = line cleaner (direção sorteada), 2 = giro.
        void ComboGlobe(int mode)
        {
            Fire(() =>
            {
                var ball = ComboIcon(iconLightBall, ComboA, false);
                var owner = ball.transform;
                Spin(ball);
                var partnerIcon = mode == 0 ? iconBomb : mode == 1 ? iconRocketH : iconSeeker;
                SetPiece(ComboB.x, ComboB.y, partnerIcon, 0.86f);
                player.PlayLightBallCharge(owner, owner, 1f);
                var kind = pieceKinds[ComboB.x, ComboB.y];
                if (mode == 0)
                {
                    var counts = new Dictionary<int, int>();
                    for (var c = 0; c < Cols; c++)
                        for (var r = 0; r < Rows; r++)
                        {
                            var cell = new Vector2Int(c, r);
                            if (cell == ComboA || cell == ComboB) continue;
                            counts.TryGetValue(pieceKinds[c, r], out var n); counts[pieceKinds[c, r]] = n + 1;
                        }
                    kind = counts.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).First().Key;
                }
                var targets = new List<Vector2Int>();
                for (var c = 0; c < Cols; c++)
                    for (var r = 0; r < Rows; r++)
                    {
                        var cell = new Vector2Int(c, r);
                        if (cell != ComboA && cell != ComboB && pieceKinds[c, r] == kind) targets.Add(cell);
                    }
                targets.Add(ComboB);                       // o parceiro é o último a ser marcado
                var rng = new System.Random(3);
                var horizontal = targets.Select(cell => cell == ComboB || rng.Next(2) == 0).ToList();
                for (var i = 0; i < targets.Count; i++)
                {
                    var target = targets[i]; var across = horizontal[i];
                    After(0.3f + BeamStep * i, () =>
                    {
                        if (target != ComboB) SetPiece(target.x, target.y, mode == 1 ? (across ? iconRocketH : iconRocketV) : partnerIcon, 0.86f);
                        player.PlayLightBallBeam(owner, pieces[target.x, target.y].transform, pieceColors[target.x, target.y]);
                    });
                }
                After(0.3f + BeamStep * targets.Count + 0.3f, () =>
                {
                    player.DetonateLightBall(owner);
                    ball.enabled = false;
                    var taken = new HashSet<Vector2Int>(targets) { ComboA };
                    for (var i = 0; i < targets.Count; i++)
                    {
                        var target = targets[i]; var across = horizontal[i];
                        if (mode == 0) After(0.32f, () => BombAt(target.x, target.y, BlastRadius));      // 0,32 s de pavio de cada bomba
                        else if (mode == 1) LineAt(target.x, target.y, across);
                        else
                        {
                            var goal = target;
                            for (var tries = 0; tries < 40 && !taken.Add(goal); tries++) goal = new Vector2Int(rng.Next(Cols), rng.Next(Rows));
                            pieces[target.x, target.y].enabled = false;
                            LaunchSeeker(goal, W(target.x, target.y));
                        }
                    }
                });
            });
        }

        // Giro + Line ou Giro + Bomba: junção; o giro leva o parceiro como carga, as casas vizinhas estouram na decolagem e o
        // parceiro dispara onde ele pousa (line: 0,12 s depois; bomba: 0,32 s de pavio).
        void ComboSeekerCargo(bool bomb)
        {
            Fire(() =>
            {
                var seeker = ComboIcon(iconSeeker, ComboA); var partner = ComboIcon(bomb ? iconBomb : iconRocketH, ComboB);
                Merge(seeker, partner, () =>
                {
                    Drop(seeker); Drop(partner);
                    MatchAt(ComboA.x - 1, ComboA.y); MatchAt(ComboA.x, ComboA.y + 1); MatchAt(ComboA.x, ComboA.y - 1);
                    var target = new Vector2Int(7, 5);
                    LaunchSeeker(target, W(ComboA.x, ComboA.y), bomb ? iconBomb : iconRocketH, () =>
                    {
                        if (!bomb) { After(0.12f, () => LineAt(target.x, target.y, true)); return; }
                        var icon = ComboIcon(iconBomb, target);
                        After(0.32f, () => { Drop(icon); BombAt(target.x, target.y, BlastRadius); });
                    });
                });
            });
        }

        // Giro + Giro: junção; os dois se fundem no meio (0,12 s), saem três giros e as casas em cruz estouram.
        void ComboSeekerSeeker()
        {
            Fire(() =>
            {
                var main = ComboIcon(iconSeeker, ComboA); var other = ComboIcon(iconSeeker, ComboB);
                Merge(main, other, () =>
                {
                    var merge = (W(ComboA.x, ComboA.y) + W(ComboB.x, ComboB.y)) * 0.5f;
                    Tween(other, merge, 0.22f, 0f, 0.12f, true);
                    Tween(main, merge, 1f, 0f, 0.12f);
                    After(0.12f, () =>
                    {
                        Drop(main); Drop(other);
                        foreach (var target in new[] { new Vector2Int(1, 5), new Vector2Int(7, 5), new Vector2Int(6, 0) }) LaunchSeeker(target, merge);
                        MatchAt(ComboA.x - 1, ComboA.y); MatchAt(ComboA.x, ComboA.y + 1); MatchAt(ComboA.x, ComboA.y - 1);
                    });
                });
            });
        }

        bool FireCombo(string kind)
        {
            switch (kind)
            {
                case "c_lineline": ComboLineLine(); return true;
                case "c_linebomb": ComboLineBomb(); return true;
                case "c_bombbomb": ComboBombBomb(); return true;
                case "c_globebomb": ComboGlobe(0); return true;
                case "c_globeline": ComboGlobe(1); return true;
                case "c_globeseeker": ComboGlobe(2); return true;
                case "c_seekerline": ComboSeekerCargo(false); return true;
                case "c_seekerbomb": ComboSeekerCargo(true); return true;
                case "c_seekerseeker": ComboSeekerSeeker(); return true;
                case "c_arrive": FireArrivalAll(); return true;
            }
            return false;
        }

        void FireChain()
        {
            Fire(() =>
            {
                var centers = new[] { new Vector2Int(2, 3), new Vector2Int(4, 3), new Vector2Int(6, 3) };
                for (var i = 0; i < centers.Length; i++)
                {
                    var center = centers[i];
                    queuedTimes.Add(clock + 0.28f * i);
                    queued.Add(() => FireBomb(1, center.x, center.y, true));
                }
            });
        }

        void FireLine(Vector2Int origin, bool horizontal, bool toNegative, bool toPositive)
        {
            Fire(() =>
            {
                var length = horizontal ? Cols : Rows;
                var cells = new List<Vector3>(); var colors = new List<Color>();
                for (var i = 0; i < length; i++)
                {
                    var c = horizontal ? i : origin.x; var r = horizontal ? origin.y : i;
                    cells.Add(W(c, r)); colors.Add(pieceColors[c, r]);
                }
                SetPiece(origin.x, origin.y, horizontal ? iconRocketH : iconRocketV, 0.86f);
                var o = W(origin.x, origin.y);
                var first = horizontal ? W(0, origin.y) : W(origin.x, 0);
                var last = horizontal ? W(Cols - 1, origin.y) : W(origin.x, Rows - 1);
                // Só um sentido: o outro "extremo" fica na própria origem
                var endA = toNegative ? first : o; var endB = toPositive ? last : o;
                player.PlayLine(o, horizontal, endA, endB, false, false, cells, colors, 1f, true, true);      // com as duas cabeças, como no jogo
                for (var i = 0; i < length; i++)
                {
                    var c = horizontal ? i : origin.x; var r = horizontal ? origin.y : i;
                    var pos = horizontal ? i : i;
                    var along = Mathf.Abs((horizontal ? c - origin.x : r - origin.y));
                    var negative = (horizontal ? c < origin.x : r < origin.y);
                    if ((negative && !toNegative) || (!negative && !toPositive && along > 0)) continue;
                    var side = negative ? (horizontal ? origin.x : origin.y) : (horizontal ? Cols - 1 - origin.x : Rows - 1 - origin.y);
                    HideAt(c, r, player.LineTimeAt(along, side, 1f) + 0.04f);
                }
            });
        }

        // ------------------------------------------------------------------------------------------ laço

        void OnUpdate()
        {
            var now = EditorApplication.timeSinceStartup;
            var dt = (float)(now - lastTime);
            lastTime = now;
            if (preview == null || player == null) return;
            if (!paused)
            {
                var step = Mathf.Min(dt, 0.1f) * speed;
                clock += step;
                StepFlights();
                player.Tick(step);
                for (var i = queued.Count - 1; i >= 0; i--)
                    if (clock >= queuedTimes[i]) { var action = queued[i]; queued.RemoveAt(i); queuedTimes.RemoveAt(i); action(); }
                for (var i = hides.Count - 1; i >= 0; i--)
                    if (clock >= hides[i].time) { pieces[hides[i].cell.x, hides[i].cell.y].enabled = false; hides.RemoveAt(i); }
                if (loop && lastAction != null && player.ActiveCount == 0 && queued.Count == 0)
                {
                    if (nextLoopAt == 0) nextLoopAt = now + 0.6;
                    if (now >= nextLoopAt) { nextLoopAt = 0; Fire(lastAction); }
                }
            }
            Repaint();
        }

        // ------------------------------------------------------------------------------------------ capturas automáticas

        // Para conferir o efeito sem depender de captura de tela manual: se existir Temp/vfx_capture_request.txt (uma linha por
        // pedido: "bomb1 0.05,0.14,0.26,0.42"), renderiza cada instante no mesmo tabuleiro do preview e grava PNGs em Temp/VfxShots.
        const string CaptureRequest = "Temp/vfx_capture_request.txt";
        const string CaptureFolder = "Temp/VfxShots";
        static double nextCapturePoll;

        [InitializeOnLoadMethod]
        static void CaptureHook() => EditorApplication.update += PollCapture;

        static void PollCapture()
        {
            if (EditorApplication.timeSinceStartup < nextCapturePoll) return;
            nextCapturePoll = EditorApplication.timeSinceStartup + 1.0;
            if (!System.IO.File.Exists(CaptureRequest) || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var text = System.IO.File.ReadAllText(CaptureRequest);
            System.IO.File.Delete(CaptureRequest);
            System.IO.Directory.CreateDirectory(CaptureFolder);
            try
            {
                var window = GetWindow<PowerUpVfxPreviewWindow>("Power-ups VFX");
                window.needsRebuild = true;
                window.Capture(text);
                System.IO.File.WriteAllText(CaptureFolder + "/done.txt", System.DateTime.Now.ToString("o"));
            }
            catch (System.Exception e) { System.IO.File.WriteAllText(CaptureFolder + "/error.txt", e.ToString()); }
        }

        void Capture(string text)
        {
            foreach (var raw in text.Split('\n'))
            {
                var parts = raw.Trim().Split(' ');
                if (parts.Length < 2 || !EnsureReady()) continue;
                var kind = parts[0];
                var solo = -1;
                if (kind.Contains(":")) { solo = int.Parse(kind.Split(':')[1]); kind = kind.Split(':')[0]; }
                PowerUpVfxPlayer.DebugSolo = solo;
                foreach (var stamp in parts[1].Split(','))
                {
                    var time = float.Parse(stamp, System.Globalization.CultureInfo.InvariantCulture);
                    switch (kind)
                    {
                        case "bomb1": FireBomb(1); break;
                        case "bomb2": FireBomb(2); break;
                        case "bomb3": FireBomb(3); break;
                        case "match": FireMatch(new[] { new Vector2Int(3, 3), new Vector2Int(4, 3), new Vector2Int(5, 3) }); break;
                        case "seeker": FireSeeker(new Vector2Int(4, 3)); break;
                        case "line": FireLine(new Vector2Int(4, 3), true, true, true); break;
                        case "lightball": FireLightBall(false); break;
                        case "lightball2": FireLightBall(true); break;
                        case "double": FireDoubleBall(); break;
                        case "hat": FireHat(); break;
                        case "arrow": FireArrow(); break;
                        case "cannon": FireCannon(); break;
                        case "hammer": FireHammer(); break;
                        default: if (!FireCombo(kind)) continue; break;
                    }
                    for (var elapsed = 0f; elapsed < time; elapsed += 1f / 120f)
                    {
                        clock += 1f / 120f;
                        StepFlights();
                        player.Tick(1f / 120f);
                        for (var i = queued.Count - 1; i >= 0; i--)
                            if (clock >= queuedTimes[i]) { var action = queued[i]; queued.RemoveAt(i); queuedTimes.RemoveAt(i); action(); }
                        for (var i = hides.Count - 1; i >= 0; i--)
                            if (clock >= hides[i].time) { pieces[hides[i].cell.x, hides[i].cell.y].enabled = false; hides.RemoveAt(i); }
                    }
                    var area = new Rect(0, 0, 900, 700);
                    var camera = preview.camera;
                    camera.orthographicSize = Mathf.Max((Cols + 1.5f) * 0.5f * area.height / area.width, (Rows + 1f) * 0.5f);
                    camera.transform.position = new Vector3((Cols - 1) * 0.5f, (Rows - 1) * 0.5f, -10f);
                    camera.transform.rotation = Quaternion.identity;
                    preview.BeginStaticPreview(area);
                    camera.Render();
                    var shot = preview.EndStaticPreview();
                    System.IO.File.WriteAllBytes(CaptureFolder + "/" + kind + (solo >= 0 ? "_solo" + solo : "") + "_" + stamp.Replace(".", "_") + ".png", shot.EncodeToPNG());
                    DestroyImmediate(shot);
                }
            }
            PowerUpVfxPlayer.DebugSolo = -1;
            ResetBoard();
        }

        // ------------------------------------------------------------------------------------------ interface

        void OnGUI()
        {
            if (!EnsureReady())
            {
                EditorGUILayout.HelpBox("Não achei o prefab dos efeitos. Use o menu Royal Aves/VFX/Power-ups/Recriar prefab.", MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                paused = GUILayout.Toggle(paused, paused ? "▶ Continuar" : "❚❚ Pausar", EditorStyles.toolbarButton, GUILayout.Width(90));
                if (GUILayout.Button("Limpar", EditorStyles.toolbarButton, GUILayout.Width(60))) ResetBoard();
                loop = GUILayout.Toggle(loop, "Repetir", EditorStyles.toolbarButton, GUILayout.Width(60));
                GUILayout.Label("Velocidade", GUILayout.Width(70));
                speed = GUILayout.HorizontalSlider(speed, 0.05f, 1.5f, GUILayout.Width(100));
                GUILayout.Label("Zoom", GUILayout.Width(40));
                zoom = GUILayout.HorizontalSlider(zoom, 0.5f, 2.5f, GUILayout.Width(100));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Selecionar prefab", EditorStyles.toolbarButton, GUILayout.Width(110))) Selection.activeObject = prefab.gameObject;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    var area = GUILayoutUtility.GetRect(10, 10, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    DrawPreview(area);
                    DrawButtons();
                }
                using (var scrollView = new EditorGUILayout.ScrollViewScope(scroll, GUILayout.Width(400)))
                {
                    scroll = scrollView.scrollPosition;
                    DrawSettings();
                }
            }
        }

        void DrawButtons()
        {
            EditorGUILayout.LabelField("Match", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Match 3")) FireMatch(Enumerable.Range(3, 3).Select(c => new Vector2Int(c, 3)).ToList());
                if (GUILayout.Button("Match 4")) FireMatch(Enumerable.Range(2, 4).Select(c => new Vector2Int(c, 3)).ToList());
                if (GUILayout.Button("Match 5")) FireMatch(Enumerable.Range(2, 5).Select(c => new Vector2Int(c, 3)).ToList());
                if (GUILayout.Button("Match em L"))
                    FireMatch(new[] { new Vector2Int(3, 2), new Vector2Int(3, 3), new Vector2Int(3, 4), new Vector2Int(4, 2), new Vector2Int(5, 2) });
            }
            EditorGUILayout.LabelField("Bomba", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Raio 1 (3x3)")) FireBomb(1);
                if (GUILayout.Button("Raio 2 (5x5)")) FireBomb(2);
                if (GUILayout.Button("Raio 3 (7x7)")) FireBomb(3);
                if (GUILayout.Button("3 encadeadas")) FireChain();
            }
            EditorGUILayout.LabelField("Seeker (impacto na peça-alvo)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Seeker no centro")) FireSeeker(new Vector2Int(4, 3));
                if (GUILayout.Button("Seeker na borda")) FireSeeker(new Vector2Int(7, 5));
                if (GUILayout.Button("Seeker em 3 alvos")) FireSeekers();
            }
            EditorGUILayout.LabelField("Light Ball (carga → conexões → explosão)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Light ball + cor")) FireLightBall(false);
                if (GUILayout.Button("Combinação de dois (órbita → vórtice → ondas)")) FireDoubleBall();
            }
            EditorGUILayout.LabelField("Chegada no tabuleiro (gira, pulsa e acende)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Bomba")) FireArrival(PowerUpKind.Bomb);
                if (GUILayout.Button("Globo")) FireArrival(PowerUpKind.Ball);
                if (GUILayout.Button("Giro")) FireArrival(PowerUpKind.Seeker);
                if (GUILayout.Button("Line")) FireArrival(PowerUpKind.Line);
                if (GUILayout.Button("Os quatro")) FireArrivalAll();
            }
            EditorGUILayout.LabelField("Combinações de dois power-ups (junção + efeito)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Line + Line")) ComboLineLine();
                if (GUILayout.Button("Line + Bomba")) ComboLineBomb();
                if (GUILayout.Button("Bomba + Bomba")) ComboBombBomb();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Globo + Bomba")) ComboGlobe(0);
                if (GUILayout.Button("Globo + Line")) ComboGlobe(1);
                if (GUILayout.Button("Globo + Giro")) ComboGlobe(2);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Giro + Line")) ComboSeekerCargo(false);
                if (GUILayout.Button("Giro + Bomba")) ComboSeekerCargo(true);
                if (GUILayout.Button("Giro + Giro")) ComboSeekerSeeker();
            }
            EditorGUILayout.LabelField("Power-ups de uso (efeito completo de cada um)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Flecha (linha)")) FireArrow();
                if (GUILayout.Button("Canhão (coluna)")) FireCannon();
                if (GUILayout.Button("Martelo (uma peça)")) FireHammer();
                if (GUILayout.Button("Chapéu de Mago")) FireHat();
            }
            EditorGUILayout.LabelField("Line Cleaner", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("→ Direita")) FireLine(new Vector2Int(0, 3), true, false, true);
                if (GUILayout.Button("← Esquerda")) FireLine(new Vector2Int(Cols - 1, 3), true, true, false);
                if (GUILayout.Button("↑ Cima")) FireLine(new Vector2Int(4, 0), false, false, true);
                if (GUILayout.Button("↓ Baixo")) FireLine(new Vector2Int(4, Rows - 1), false, true, false);
                if (GUILayout.Button("↔ Foguete (dois lados)")) FireLine(new Vector2Int(4, 3), true, true, true);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("No jogo real (Play) — com as animações originais", EditorStyles.boldLabel);
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Entre no Play e comece uma fase: os botões abaixo disparam no tabuleiro de verdade, com os cacos das peças e as metades do foguete originais.", MessageType.None);
                return;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Quebrar 3 peças")) GameBreak(3);
                if (GUILayout.Button("Bomba (raio 1)")) GameBomb(1);
                if (GUILayout.Button("Bomba (raio 2)")) GameBomb(2);
                if (GUILayout.Button("Foguete na linha")) GameRocket();
            }
        }

        // ---- disparos no jogo real
        static Square MiddleSquare()
        {
            var lm = LevelManager.THIS;
            if (lm == null || lm.field == null) return null;
            return lm.field.GetSquares().Where(s => s != null && s.Item != null && !s.IsNone())
                .OrderBy(s => Mathf.Abs(s.col - lm.levelData.maxCols / 2) + Mathf.Abs(s.row - lm.levelData.maxRows / 2)).FirstOrDefault();
        }

        void GameBreak(int count)
        {
            var origin = MiddleSquare();
            if (origin == null) return;
            var row = LevelManager.THIS.GetRowSquare(origin.row).Where(s => s.Item != null).OrderBy(s => Mathf.Abs(s.col - origin.col)).Take(count).ToList();
            foreach (var square in row) square.Item.DestroyItem(true);
        }

        void GameBomb(int radius)
        {
            var origin = MiddleSquare();
            if (origin == null) return;
            var lm = LevelManager.THIS;
            DynamiteBlastEffect.Play(origin.transform.position, lm.squareWidth, radius, false);
            foreach (var square in lm.field.GetSquares())
                if (square != null && square.Item != null && Mathf.Abs(square.col - origin.col) <= radius && Mathf.Abs(square.row - origin.row) <= radius)
                    square.Item.DestroyItem(true);
        }

        void GameRocket()
        {
            var origin = MiddleSquare();
            if (origin == null) return;
            var lm = LevelManager.THIS;
            var line = lm.GetRowSquare(origin.row);
            var items = line.ToDictionary(s => s, s => s.Item);
            lm.StripedShowPass(origin.Item.gameObject, true, line, sq => { if (items.TryGetValue(sq, out var item) && item != null) item.DestroyItem(true); });
        }

        void DrawSettings()
        {
            EditorGUILayout.HelpBox("Campos do prefab PowerUpVfx: o que você muda é gravado nele e vale no jogo. Os efeitos disparam de novo para você ver.", MessageType.None);
            if (editor == null || editor.target != prefab) { if (editor != null) DestroyImmediate(editor); editor = UnityEditor.Editor.CreateEditor(prefab); }
            EditorGUI.BeginChangeCheck();
            editor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssetIfDirty(prefab);
                needsRebuild = true;
                var again = lastAction;
                EditorApplication.delayCall += () => { if (this != null && again != null && EnsureReady()) Fire(again); };
            }
        }

        void DrawPreview(Rect area)
        {
            EditorGUI.DrawRect(area, new Color(0.06f, 0.05f, 0.1f));
            if (preview == null || area.width < 20 || area.height < 20 || Event.current.type != EventType.Repaint) return;
            var camera = preview.camera;
            var viewWidth = Cols + 1.5f;
            camera.orthographicSize = Mathf.Max(1.5f, Mathf.Max(viewWidth * 0.5f * area.height / area.width, (Rows + 1f) * 0.5f)) / zoom;
            camera.transform.position = new Vector3((Cols - 1) * 0.5f, (Rows - 1) * 0.5f, -10f);
            camera.transform.rotation = Quaternion.identity;
            preview.BeginPreview(area, GUIStyle.none);
            camera.Render();
            var texture = preview.EndPreview();
            GUI.DrawTexture(area, texture, ScaleMode.StretchToFill, false);
        }
    }
}
