using System.Collections.Generic;
using System.Linq;
using RoyalAves.Effects;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    /// Menu Royal Aves/VFX/Flecha/Preview. Mostra a flecha atravessando uma linha de um tabuleiro de mentira, com o mesmo
    /// código do jogo (escurecimento, cauda, fitas, estrelinhas, fragmentos, impactos). Dá para pausar, arrastar o tempo,
    /// ver em câmera lenta e dar zoom. À direita ficam os parâmetros do prefab ArrowFlight: o que você muda é gravado nele
    /// e o preview recomeça na hora.
    public sealed class ArrowFlightPreviewWindow : EditorWindow
    {
        const int Cols = 9, Rows = 5, LineRow = 2;

        PreviewRenderUtility preview;
        GameObject root;
        readonly List<SpriteRenderer> pieces = new List<SpriteRenderer>();
        Sprite pieceSprite, tileSprite;
        Material spriteMaterial;
        ArrowFlightEffect prefab;
        ArrowFlightEffect instance;
        UnityEditor.Editor editor;

        float time;
        float speed = 0.5f;
        bool paused, loop = true, freeze;
        float zoom = 1f;
        double lastTime;
        double restartAt;
        Vector2 scroll;
        bool needsRestart = true;
        Vector2 lastSize;

        [MenuItem("Royal Aves/VFX/Flecha/Preview")]
        static void Open()
        {
            var window = GetWindow<ArrowFlightPreviewWindow>("Flecha");
            window.minSize = new Vector2(900, 520);
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
            if (instance != null) DestroyImmediate(instance.gameObject);
            instance = null;
            if (preview != null) { preview.Cleanup(); preview = null; }
            root = null;
            pieces.Clear();
        }

        // ------------------------------------------------------------------------------------------ montagem

        bool EnsureReady()
        {
            if (prefab == null)
            {
                if (!System.IO.File.Exists(ArrowFlightBuilder.PrefabPath)) ArrowFlightBuilder.Build();
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowFlightBuilder.PrefabPath);
                prefab = go != null ? go.GetComponent<ArrowFlightEffect>() : null;
                if (prefab == null) return false;
            }
            if (preview != null && root != null) return true;

            Teardown();
            preview = new PreviewRenderUtility();
            var camera = preview.camera;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.06f, 0.14f, 1f);
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;

            if (pieceSprite == null) pieceSprite = MakeRounded(64, 0.28f);
            if (tileSprite == null) tileSprite = MakeRounded(64, 0.18f);
            if (spriteMaterial == null) spriteMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave };

            root = new GameObject("Tabuleiro de teste");
            preview.AddSingleGO(root);
            var colors = new[]
            {
                new Color(0.93f, 0.22f, 0.30f), new Color(0.22f, 0.78f, 0.38f), new Color(0.26f, 0.52f, 0.98f),
                new Color(1f, 0.80f, 0.16f), new Color(1f, 0.58f, 0.14f),
            };
            var rng = new System.Random(7);
            for (var c = 0; c < Cols; c++)
                for (var r = 0; r < Rows; r++)
                {
                    Make("Casa", tileSprite, new Color(0.36f, 0.27f, 0.55f), new Vector3(c, r, 0), 0.93f, 0);
                    var piece = Make("Peça", pieceSprite, colors[rng.Next(colors.Length)], new Vector3(c, r, 0), 0.74f, 1);
                    if (r == LineRow) pieces.Add(piece);
                }
            needsRestart = true;
            return true;
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

        void Restart()
        {
            if (instance != null) DestroyImmediate(instance.gameObject);
            instance = null;
            foreach (var piece in pieces) piece.enabled = true;
            if (prefab == null || preview == null) return;
            var positions = Enumerable.Range(0, Cols).Select(c => new Vector3(c, LineRow, 0)).ToList();
            instance = ArrowFlightEffect.CreatePreview(prefab, positions, 1f, i => { if (i >= 0 && i < pieces.Count) pieces[i].enabled = false; }, preview.camera);
            instance.freezeX = (Cols - 1) * 0.5f;
            preview.AddSingleGO(instance.gameObject);
            time = 0f;
            needsRestart = false;
        }

        // ------------------------------------------------------------------------------------------ laço

        void OnUpdate()
        {
            var now = EditorApplication.timeSinceStartup;
            var dt = (float)(now - lastTime);
            lastTime = now;
            if (preview == null) return;
            if (!paused && instance != null)
            {
                time += Mathf.Min(dt, 0.1f) * speed;
                if (freeze) time = Mathf.Repeat(time, 1000f);
                else if (time > instance.Duration)
                {
                    if (loop)
                    {
                        if (restartAt == 0) restartAt = now + 0.6;
                        if (now >= restartAt) { restartAt = 0; needsRestart = true; }
                    }
                    else time = instance.Duration;
                }
            }
            Repaint();
        }

        // ------------------------------------------------------------------------------------------ interface

        void OnGUI()
        {
            if (!EnsureReady())
            {
                EditorGUILayout.HelpBox("Não achei o prefab do efeito. Use o menu Royal Aves/VFX/Flecha/Recriar prefab.", MessageType.Warning);
                return;
            }
            if (needsRestart && Event.current.type == EventType.Layout) { ConfigureCamera(lastSize); Restart(); }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                paused = GUILayout.Toggle(paused, paused ? "▶ Continuar" : "❚❚ Pausar", EditorStyles.toolbarButton, GUILayout.Width(90));
                if (GUILayout.Button("Reiniciar", EditorStyles.toolbarButton, GUILayout.Width(70))) needsRestart = true;
                loop = GUILayout.Toggle(loop, "Repetir", EditorStyles.toolbarButton, GUILayout.Width(60));
                var wasFrozen = freeze;
                freeze = GUILayout.Toggle(freeze, "Flecha parada", EditorStyles.toolbarButton, GUILayout.Width(95));
                if (freeze != wasFrozen) { needsRestart = true; foreach (var piece in pieces) piece.enabled = true; }
                GUILayout.Label("Velocidade", GUILayout.Width(70));
                speed = GUILayout.HorizontalSlider(speed, 0.05f, 1.5f, GUILayout.Width(100));
                GUILayout.Label("Zoom", GUILayout.Width(40));
                var oldZoom = zoom;
                zoom = GUILayout.HorizontalSlider(zoom, 0.4f, 2.5f, GUILayout.Width(100));
                if (!Mathf.Approximately(oldZoom, zoom)) { ConfigureCamera(lastSize); needsRestart = true; }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Selecionar prefab", EditorStyles.toolbarButton, GUILayout.Width(110))) Selection.activeObject = prefab.gameObject;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    var area = GUILayoutUtility.GetRect(10, 10, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    DrawPreview(area);
                    if (instance != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        var t = freeze ? time : EditorGUILayout.Slider("Tempo", time, 0f, instance.Duration);
                        if (EditorGUI.EndChangeCheck())
                        {
                            paused = true;
                            if (t < time) { Restart(); }
                            time = t;
                        }
                    }
                }
                using (var scrollView = new EditorGUILayout.ScrollViewScope(scroll, GUILayout.Width(380)))
                {
                    scroll = scrollView.scrollPosition;
                    DrawSettings();
                }
            }
        }

        void DrawSettings()
        {
            EditorGUILayout.HelpBox("Campos do prefab ArrowFlight: o que você muda é gravado nele e vale no jogo.", MessageType.None);
            if (editor == null || editor.target != prefab) { if (editor != null) DestroyImmediate(editor); editor = UnityEditor.Editor.CreateEditor(prefab); }
            EditorGUI.BeginChangeCheck();
            editor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssetIfDirty(prefab);
                needsRestart = true;
            }
        }

        /// Enquadra a câmera do preview como a câmera do jogo: as bordas dela são as "bordas da tela" onde a flecha nasce e bate.
        void ConfigureCamera(Vector2 size)
        {
            if (preview == null || size.x < 20 || size.y < 20) return;
            var camera = preview.camera;
            var viewWidth = Cols + 1.2f;
            camera.orthographicSize = Mathf.Max(1.5f, viewWidth * 0.5f * size.y / size.x) / zoom;
            camera.aspect = size.x / size.y;
            camera.transform.position = new Vector3((Cols - 1) * 0.5f, LineRow, -10f);
            camera.transform.rotation = Quaternion.identity;
        }

        void DrawPreview(Rect area)
        {
            // Se a janela ou o zoom mudaram, o enquadramento (e portanto as bordas da tela) mudou: recomeça o efeito.
            if (area.width > 20 && (Mathf.Abs(lastSize.x - area.width) > 4f || Mathf.Abs(lastSize.y - area.height) > 4f) && Event.current.type == EventType.Repaint)
            {
                lastSize = area.size;
                needsRestart = true;
            }
            EditorGUI.DrawRect(area, new Color(0.06f, 0.05f, 0.1f));
            if (preview == null || instance == null || area.width < 20 || area.height < 20) return;
            if (Event.current.type != EventType.Repaint) return;
            instance.freezeArrow = freeze;
            instance.Evaluate(time);

            var camera = preview.camera;
            var shake = instance != null && !freeze ? instance.CameraShakeOffset(time) : Vector3.zero;
            camera.transform.position += shake;
            preview.BeginPreview(area, GUIStyle.none);
            camera.Render();
            var texture = preview.EndPreview();
            camera.transform.position -= shake;
            GUI.DrawTexture(area, texture, ScaleMode.StretchToFill, false);
        }
    }
}
