using System.Collections.Generic;
using System.Linq;
using RoyalAves.Effects;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
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
        Material spriteMaterial;
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
                    var color = colors[rng.Next(colors.Length)];
                    pieceColors[c, r] = color;
                    pieces[c, r] = Make("Peça", pieceSprite, color, new Vector3(c, r, 0), 0.74f, 1);
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
            sr.sharedMaterial = spriteMaterial;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        // ------------------------------------------------------------------------------------------ disparos

        void ResetBoard()
        {
            for (var c = 0; c < Cols; c++) for (var r = 0; r < Rows; r++) pieces[c, r].enabled = true;
            hides.Clear(); queued.Clear(); queuedTimes.Clear();
            foreach (var flight in flights) if (flight.go != null) DestroyImmediate(flight.go);
            flights.Clear();
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
        sealed class MockFlight { public GameObject go; public Vector3 start, control, end; public float startAt, time; public Vector2Int target; public bool marked; }
        readonly List<MockFlight> flights = new List<MockFlight>();
        const float MockWindUp = 0.35f;

        void LaunchSeeker(Vector2Int target)
        {
            var from = target.x >= Cols / 2 ? new Vector2Int(1, 1) : new Vector2Int(Cols - 2, 1);
            var start = W(from.x, from.y); var end = W(target.x, target.y);
            var distance = Vector3.Distance(start, end);
            var apex = Vector3.Lerp(start, end, 0.5f) + Vector3.up * Mathf.Max(1f, distance * 0.35f);
            var copter = Make("Girocóptero", pieceSprite, new Color(0.62f, 0.3f, 0.9f), start, 0.62f, 6);
            copter.sortingLayerName = player.sortingLayer; copter.sortingOrder = player.sortingBase + 20;
            flights.Add(new MockFlight { go = copter.gameObject, start = start, end = end, control = 2f * apex - (start + end) * 0.5f, startAt = clock, time = Mathf.Clamp(distance / 5f, 0.5f, 1.4f), target = target });
            player.PlaySeekerFlight(copter.transform, 1f);
        }

        void StepFlights()
        {
            for (var i = flights.Count - 1; i >= 0; i--)
            {
                var flight = flights[i];
                var age = clock - flight.startAt - MockWindUp;
                if (age < 0f) continue;
                if (!flight.marked) { flight.marked = true; player.PlaySeekerMarker(flight.end, flight.time, 1f); }
                var u = Mathf.Clamp01(age / flight.time);
                var e = u * u * (3f - 2f * u);
                flight.go.transform.position = (1f - e) * (1f - e) * flight.start + 2f * (1f - e) * e * flight.control + e * e * flight.end;
                if (u < 1f) continue;
                player.PlaySeekerImpact(flight.end, pieceColors[flight.target.x, flight.target.y], 1f);
                HideAt(flight.target.x, flight.target.y, 0.04f);
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
                var o = W(origin.x, origin.y);
                var first = horizontal ? W(0, origin.y) : W(origin.x, 0);
                var last = horizontal ? W(Cols - 1, origin.y) : W(origin.x, Rows - 1);
                // Só um sentido: o outro "extremo" fica na própria origem
                var endA = toNegative ? first : o; var endB = toPositive ? last : o;
                player.PlayLine(o, horizontal, endA, endB, false, false, cells, colors, 1f);
                var speed = player.line.speed;
                for (var i = 0; i < length; i++)
                {
                    var c = horizontal ? i : origin.x; var r = horizontal ? origin.y : i;
                    var pos = horizontal ? i : i;
                    var along = Mathf.Abs((horizontal ? c - origin.x : r - origin.y));
                    var negative = (horizontal ? c < origin.x : r < origin.y);
                    if ((negative && !toNegative) || (!negative && !toPositive && along > 0)) continue;
                    HideAt(c, r, along / speed + 0.04f);
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
                        default: continue;
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
