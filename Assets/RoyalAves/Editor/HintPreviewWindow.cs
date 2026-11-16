using System.Collections.Generic;
using System.Linq;
using RoyalAves.Effects;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    /// Menu Royal Aves/VFX/Hint Preview: mostra o efeito de dica rodando em um exemplo de cada tipo de match
    /// (3 horizontal/vertical, 4, 5, L, T) com o mesmo código do jogo. Clique num quadro para ampliar. À direita ficam os
    /// parâmetros do prefab: o que você mexe é gravado no prefab e aparece na hora no preview.
    public sealed class HintPreviewWindow : EditorWindow
    {
        const int Cols = 8, Rows = 5;
        const float BoardSpacing = 20f;

        sealed class Case
        {
            public string name;
            public Vector2Int[] cells;
            public Vector2Int from, to;
        }

        static readonly Case[] Cases =
        {
            new Case { name = "Match 3 horizontal", cells = H(3, 2, 3), from = new Vector2Int(5, 1), to = new Vector2Int(5, 2) },
            new Case { name = "Match 3 vertical", cells = V(3, 1, 3), from = new Vector2Int(4, 3), to = new Vector2Int(3, 3) },
            new Case { name = "Match 4 horizontal", cells = H(2, 2, 4), from = new Vector2Int(5, 1), to = new Vector2Int(5, 2) },
            new Case { name = "Match 4 vertical", cells = V(2, 1, 4), from = new Vector2Int(1, 3), to = new Vector2Int(2, 3) },
            new Case { name = "Match 5 horizontal", cells = H(1, 2, 5), from = new Vector2Int(3, 3), to = new Vector2Int(3, 2) },
            new Case { name = "Match 5 vertical", cells = V(6, 0, 5), from = new Vector2Int(5, 2), to = new Vector2Int(6, 2) },
            new Case { name = "Formato L", cells = new[] { new Vector2Int(2, 1), new Vector2Int(3, 1), new Vector2Int(4, 1), new Vector2Int(2, 2), new Vector2Int(2, 3) },
                from = new Vector2Int(1, 1), to = new Vector2Int(2, 1) },
            new Case { name = "Formato T", cells = new[] { new Vector2Int(2, 3), new Vector2Int(3, 3), new Vector2Int(4, 3), new Vector2Int(3, 2), new Vector2Int(3, 1) },
                from = new Vector2Int(3, 4), to = new Vector2Int(3, 3) },
        };

        static Vector2Int[] H(int c, int r, int n) => Enumerable.Range(0, n).Select(i => new Vector2Int(c + i, r)).ToArray();
        static Vector2Int[] V(int c, int r, int n) => Enumerable.Range(0, n).Select(i => new Vector2Int(c, r + i)).ToArray();

        static readonly Color[] PieceColors =
        {
            new Color(0.93f, 0.22f, 0.30f), new Color(0.22f, 0.78f, 0.38f), new Color(0.26f, 0.52f, 0.98f),
            new Color(1f, 0.80f, 0.16f), new Color(1f, 0.58f, 0.14f),
        };

        sealed class Board
        {
            public GameObject root;
            public HintEffect effect;
            public List<HintPiece> pieces = new List<HintPiece>();
            public int moverIndex;
            public Vector3 direction;
            public bool playing;
        }

        PreviewRenderUtility preview;
        readonly List<Board> boards = new List<Board>();
        Sprite pieceSprite, tileSprite;
        Material spriteMaterial;
        GameObject prefab;
        UnityEditor.Editor effectEditor, directorEditor;
        int focus = -1;
        float speed = 1f;
        bool paused;
        double lastTime;
        float time;
        Vector2 scroll;
        bool settingsDirty;

        [MenuItem("Royal Aves/VFX/Hint Preview")]
        static void Open()
        {
            var window = GetWindow<HintPreviewWindow>("Hint Preview");
            window.minSize = new Vector2(980, 560);
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
            if (effectEditor != null) DestroyImmediate(effectEditor);
            if (directorEditor != null) DestroyImmediate(directorEditor);
        }

        void Teardown()
        {
            foreach (var board in boards)
            {
                if (board.effect != null && board.effect.IsPlaying) board.effect.Stop();
            }
            boards.Clear();
            if (preview != null) { preview.Cleanup(); preview = null; }
        }

        // ------------------------------------------------------------------------------------------ montagem

        bool EnsureReady()
        {
            if (prefab == null)
            {
                if (!System.IO.File.Exists(HintEffectBuilder.PrefabPath)) HintEffectBuilder.Build();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HintEffectBuilder.PrefabPath);
                if (prefab == null) return false;
            }
            if (preview != null && boards.Count == Cases.Length && boards.All(b => b.root != null)) return true;

            Teardown();
            preview = new PreviewRenderUtility();
            var camera = preview.camera;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.07f, 0.17f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 3f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;

            EnsureSprites();
            for (var i = 0; i < Cases.Length; i++) boards.Add(BuildBoard(i));
            settingsDirty = true;
            return true;
        }

        void EnsureSprites()
        {
            if (pieceSprite == null) pieceSprite = MakeRounded(64, 0.28f, 0.0f);
            if (tileSprite == null) tileSprite = MakeRounded(64, 0.18f, 0.0f);
            if (spriteMaterial == null)
            {
                spriteMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave };
            }
        }

        static Sprite MakeRounded(int size, float radius, float unused)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
            var r = radius * size;
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Max(Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - r), 0f);
                    var dy = Mathf.Max(Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - r), 0f);
                    var d = Mathf.Sqrt(dx * dx + dy * dy) - r;
                    var a = Mathf.Clamp01(0.5f - d);
                    var shade = Mathf.Lerp(0.82f, 1f, y / (float)size);
                    texture.SetPixel(x, y, new Color(shade, shade, shade, a));
                }
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        SpriteRenderer MakeSprite(Transform parent, string label, Sprite sprite, Color color, Vector3 position, float scale, int order)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = Vector3.one * scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = spriteMaterial;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        Board BuildBoard(int index)
        {
            var data = Cases[index];
            var board = new Board();
            var root = new GameObject("Board " + data.name);
            root.transform.position = new Vector3(index * BoardSpacing, 0, 0);
            preview.AddSingleGO(root);
            board.root = root;

            var rng = new System.Random(1000 + index);
            var matchColor = PieceColors[0];
            var cellSet = new HashSet<Vector2Int>(data.cells);
            var pieceAt = new Dictionary<Vector2Int, SpriteRenderer>();
            for (var c = 0; c < Cols; c++)
                for (var r = 0; r < Rows; r++)
                {
                    var cell = new Vector2Int(c, r);
                    var pos = new Vector3(c, r, 0);
                    MakeSprite(root.transform, "Casa", tileSprite, new Color(0.36f, 0.27f, 0.55f, 1f), pos, 0.93f, 0);
                    Color color;
                    if (cell == data.to) color = PieceColors[1];
                    else if (cell == data.from || cellSet.Contains(cell)) color = matchColor;
                    else color = PieceColors[1 + rng.Next(PieceColors.Length - 1)];
                    pieceAt[cell] = MakeSprite(root.transform, "Peça", pieceSprite, color, pos, 0.74f, 1);
                }

            var stationary = data.cells.Where(c => c != data.to).Select(c => pieceAt[c]).ToList();
            foreach (var renderer in stationary)
                board.pieces.Add(new HintPiece { nudge = renderer.transform, sources = new[] { renderer } });
            var mover = pieceAt[data.from];
            board.pieces.Add(new HintPiece { nudge = mover.transform, sources = new[] { mover } });
            board.moverIndex = board.pieces.Count - 1;
            board.direction = new Vector3(data.to.x - data.from.x, data.to.y - data.from.y, 0).normalized;

            var effectGo = new GameObject("Efeito da dica");
            effectGo.transform.SetParent(root.transform, false);
            board.effect = effectGo.AddComponent<HintEffect>();
            return board;
        }

        void SyncSettings()
        {
            var source = prefab != null ? prefab.GetComponent<HintEffect>() : null;
            if (source == null) return;
            foreach (var board in boards)
            {
                if (board.effect == null) continue;
                var wasPlaying = board.effect.IsPlaying;
                if (wasPlaying) board.effect.Stop();
                EditorUtility.CopySerialized(source, board.effect);
                board.playing = false;
            }
            settingsDirty = false;
        }

        // ------------------------------------------------------------------------------------------ laço

        float Show => prefab != null && prefab.GetComponent<HintDirector>() != null ? prefab.GetComponent<HintDirector>().showSeconds : 3.2f;
        float Pause => prefab != null && prefab.GetComponent<HintDirector>() != null ? prefab.GetComponent<HintDirector>().pauseSeconds : 0.8f;

        void OnUpdate()
        {
            var now = EditorApplication.timeSinceStartup;
            var dt = (float)(now - lastTime);
            lastTime = now;
            if (preview == null) return;
            if (!paused) time += Mathf.Min(dt, 0.1f) * speed;
            Repaint();
        }

        void Drive()
        {
            if (settingsDirty) SyncSettings();
            var show = Show; var cycle = show + Pause;
            var local = Mathf.Repeat(time, cycle);
            for (var i = 0; i < boards.Count; i++)
            {
                var board = boards[i];
                if (board.effect == null) continue;
                if (local < show)
                {
                    if (!board.effect.IsPlaying)
                        board.effect.Play(Cases[i].cells, new HintGrid { origin = board.root.transform.position, ex = Vector3.right, ey = Vector3.up },
                            board.pieces, board.moverIndex, board.direction, show);
                    board.effect.Evaluate(local);
                }
                else if (board.effect.IsPlaying) board.effect.Stop();
            }
        }

        // ------------------------------------------------------------------------------------------ interface

        void OnGUI()
        {
            if (!EnsureReady())
            {
                EditorGUILayout.HelpBox("Não achei o prefab do efeito. Use o menu Royal Aves/VFX/Hint/Recriar prefab.", MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                paused = GUILayout.Toggle(paused, paused ? "▶ Continuar" : "❚❚ Pausar", EditorStyles.toolbarButton, GUILayout.Width(90));
                if (GUILayout.Button("Reiniciar", EditorStyles.toolbarButton, GUILayout.Width(70))) time = 0f;
                GUILayout.Label("Velocidade", GUILayout.Width(70));
                speed = GUILayout.HorizontalSlider(speed, 0.1f, 1.5f, GUILayout.Width(120));
                if (focus >= 0 && GUILayout.Button("← Ver todos", EditorStyles.toolbarButton, GUILayout.Width(90))) focus = -1;
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Selecionar prefab", EditorStyles.toolbarButton, GUILayout.Width(110))) Selection.activeObject = prefab;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var previewArea = GUILayoutUtility.GetRect(10, 10, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                DrawPreviews(previewArea);
                using (var scope = new EditorGUILayout.ScrollViewScope(scroll, GUILayout.Width(360)))
                {
                    scroll = scope.scrollPosition;
                    DrawSettings();
                }
            }
        }

        void DrawSettings()
        {
            EditorGUILayout.HelpBox("Estes campos são do prefab HintEffect: o que você muda é gravado nele e vale no jogo. " +
                                    "Cada dica fica 'Show Seconds' na tela; depois de passar por todas as jogadas possíveis, recomeça.", MessageType.None);
            var effect = prefab.GetComponent<HintEffect>();
            var director = prefab.GetComponent<HintDirector>();
            if (effectEditor == null || effectEditor.target != effect) { if (effectEditor != null) DestroyImmediate(effectEditor); effectEditor = UnityEditor.Editor.CreateEditor(effect); }
            if (directorEditor == null || directorEditor.target != director) { if (directorEditor != null) DestroyImmediate(directorEditor); directorEditor = UnityEditor.Editor.CreateEditor(director); }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Ciclo de dicas", EditorStyles.boldLabel);
            directorEditor.OnInspectorGUI();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Efeito", EditorStyles.boldLabel);
            effectEditor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                settingsDirty = true;
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssetIfDirty(prefab);
            }
        }

        void DrawPreviews(Rect area)
        {
            EditorGUI.DrawRect(area, new Color(0.07f, 0.05f, 0.13f));
            Drive();
            if (focus >= 0)
            {
                DrawTile(focus, Fit(area, 1.375f));
                return;
            }

            const int columns = 3;
            var count = Cases.Length;
            var rowsNeeded = Mathf.CeilToInt(count / (float)columns);
            var gap = 8f;
            var cellW = (area.width - gap * (columns + 1)) / columns;
            var cellH = (area.height - gap * (rowsNeeded + 1)) / rowsNeeded;
            var size = FitSize(cellW, cellH, 1.375f);
            for (var i = 0; i < count; i++)
            {
                var col = i % columns; var row = i / columns;
                var rect = new Rect(area.x + gap + col * (cellW + gap) + (cellW - size.x) * 0.5f,
                    area.y + gap + row * (cellH + gap) + (cellH - size.y) * 0.5f, size.x, size.y);
                DrawTile(i, rect);
            }
        }

        static Vector2 FitSize(float w, float h, float aspect)
        {
            var width = Mathf.Min(w, h * aspect);
            return new Vector2(width, width / aspect);
        }

        static Rect Fit(Rect area, float aspect)
        {
            var size = FitSize(area.width - 16f, area.height - 16f, aspect);
            return new Rect(area.x + (area.width - size.x) * 0.5f, area.y + (area.height - size.y) * 0.5f, size.x, size.y);
        }

        void DrawTile(int index, Rect rect)
        {
            if (rect.width < 10 || rect.height < 10) return;
            if (Event.current.type == EventType.Repaint)
            {
                var camera = preview.camera;
                camera.orthographicSize = 3.2f;
                camera.transform.position = new Vector3(index * BoardSpacing + 3.5f, 2f, -10f);
                camera.transform.rotation = Quaternion.identity;
                preview.BeginPreview(rect, GUIStyle.none);
                camera.Render();
                var texture = preview.EndPreview();
                GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
            }
            var label = new Rect(rect.x + 6, rect.y + 4, rect.width - 12, 18);
            EditorGUI.DropShadowLabel(label, Cases[index].name);
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                focus = focus == index ? -1 : index;
                Event.current.Use();
            }
        }
    }
}
