// The progress bar section of the Temporadas tab: the real prefab of the bar, rendered in the editor with its own code
// (MagicProgressBar.Evaluate) while a loading is simulated, so what is seen here is what the loading screen plays.
// A transport (play, pause, scrub, step, slow motion) and the bar's own settings sit under the picture; changes show at once
// and "Salvar no prefab" writes them to the prefab, which every screen that uses the bar shares.
//
// There are two pictures — the bar alone, and the whole loading screen (art + shade + bar, drawn by DrawScreenPreview) — and
// each one has its own preview scene, because a preview scene recreates its render texture whenever the size changes, and
// two sizes in one frame fight over it. The screen's copy of the bar takes its settings from the bar's, so editing the bar
// is seen in both.
using System;
using System.Collections.Generic;
using RoyalAves.Meta;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MagicMail.EditorTools
{
    public static class MagicProgressBarPreview
    {
        /// Width, in units of the 1080-wide screen, of the bar on the loading screen at scale 1 (RoyalAvesLoadingBuilder uses the same).
        public const float ScreenBarWidth = 880f;
        const float Hold = 1.2f;             // seconds the bar stays full at the end of the simulated loading
        const float FrameStep = 1f / 60f;

        // One preview scene: a camera, a canvas and a copy of the prefab. `screenLayers` adds the art and the shade behind the bar.
        class Stage
        {
            public PreviewRenderUtility pru;
            public RectTransform canvasRect;
            public GameObject instance;
            public MagicProgressBar bar;
            public Image art, shade;
            public readonly List<Image> propImages = new List<Image>();   // one per prop of the season being shown

            public bool Ready => pru != null && bar != null;

            // Lays the props of the season out as the game does (SeasonProps) and puts every layer in drawing order: the art,
            // the props behind, the shade, the bar, the props in front.
            public void SyncProps(IReadOnlyList<SeasonProp> props, Vector2 screen, float k, float now)
            {
                while (propImages.Count < props.Count) propImages.Add(Layer("Adereço", canvasRect));
                for (var i = 0; i < propImages.Count; i++)
                {
                    var image = propImages[i];
                    if (i >= props.Count || props[i].sprite == null) { image.enabled = false; continue; }
                    var p = props[i];
                    SeasonPropMotion.Evaluate(p, now, out var offset, out var scale, out var rotation);
                    image.enabled = true;
                    image.sprite = p.sprite;
                    image.color = p.tint;
                    var rect = image.rectTransform;
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = p.sprite.rect.size * k;
                    rect.anchoredPosition = new Vector2((p.position.x + offset.x) * k, -screen.y / 2f + (p.position.y + offset.y) * k);
                    rect.localScale = Vector3.one * Mathf.Max(0.05f, p.scale * scale);
                    rect.localEulerAngles = new Vector3(0f, 0f, p.rotation + rotation);
                }

                var index = 0;
                art.transform.SetSiblingIndex(index++);
                for (var i = 0; i < props.Count; i++)
                    if (!props[i].inFront) propImages[i].transform.SetSiblingIndex(index++);
                shade.transform.SetSiblingIndex(index++);
                instance.transform.SetSiblingIndex(index++);
                for (var i = 0; i < props.Count; i++)
                    if (props[i].inFront) propImages[i].transform.SetSiblingIndex(index++);
            }

            public bool Load(GameObject prefab, bool screenLayers)
            {
                Dispose();
                pru = new PreviewRenderUtility();
                var cam = pru.camera;
                cam.orthographic = true;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 100f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.cullingMask = -1;

                // A world-space canvas at scale 1 and a camera that sees one world unit per pixel of its texture (see Begin).
                var canvasObject = new GameObject("PreviewCanvas", typeof(Canvas));
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = cam;
                canvasRect = (RectTransform)canvasObject.transform;
                canvasRect.pivot = new Vector2(0.5f, 0.5f);
                pru.AddSingleGO(canvasObject);

                instance = pru.InstantiatePrefabInScene(prefab);
                instance.transform.SetParent(canvasObject.transform, false);
                bar = instance.GetComponent<MagicProgressBar>();
                if (bar == null) { Dispose(); return false; }

                if (screenLayers)
                {
                    art = Layer("Arte", canvasObject.transform);
                    shade = Layer("Sombra", canvasObject.transform);
                    art.transform.SetSiblingIndex(0);       // behind the bar, art first, then the shade over it
                    shade.transform.SetSiblingIndex(1);
                }
                return true;
            }

            static Image Layer(string name, Transform parent)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
                var image = go.GetComponent<Image>();
                image.raycastTarget = false;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = Color.white;
                return image;
            }

            public void Dispose()
            {
                if (pru != null) pru.Cleanup();
                pru = null;
                canvasRect = null;
                instance = null;
                bar = null;
                art = shade = null;
                propImages.Clear();
            }

            /// Starts drawing into `rect`: the camera sees exactly the pixels of the texture the preview made for it (read from
            /// that texture, never guessed), one world unit per pixel, and the canvas is as big as the texture.
            public Vector2 Begin(Rect rect, Color background)
            {
                pru.BeginPreview(rect, GUIStyle.none);
                var cam = pru.camera;
                cam.backgroundColor = background;
                var texture = cam.targetTexture;
                float w = texture != null ? texture.width : cam.pixelWidth;
                float h = texture != null ? texture.height : cam.pixelHeight;
                cam.aspect = w / h;
                cam.orthographicSize = h * 0.5f;
                cam.transform.position = new Vector3(0f, 0f, -10f);
                cam.transform.rotation = Quaternion.identity;
                canvasRect.sizeDelta = new Vector2(w, h);
                canvasRect.localScale = Vector3.one;
                canvasRect.position = Vector3.zero;
                return new Vector2(w, h);
            }

            public void Finish(Rect rect)
            {
                bar.Evaluate(time, ProgressAt);
                Canvas.ForceUpdateCanvases();
                pru.Render(true, false);
                GUI.DrawTexture(rect, pru.EndPreview(), ScaleMode.StretchToFill, false);
            }
        }

        static readonly Stage barStage = new Stage();      // the bar alone (and the one whose settings are edited)
        static readonly Stage screenStage = new Stage();   // the whole loading screen
        static Sprite shadeSprite;
        static bool refreshed;
        static UnityEditor.Editor barEditor;

        static float time;
        static bool playing = true, loop = true;
        static float duration = 4f;          // seconds of the simulated loading, 0 to full
        static double lastTick, lastDrawn;
        static int speedIndex = 4;
        static readonly float[] SpeedValues = { 0.05f, 0.1f, 0.25f, 0.5f, 1f };
        static readonly string[] SpeedLabels = { "0,05x", "0,1x", "0,25x", "0,5x", "1x" };
        static int backgroundIndex;
        static readonly string[] BackgroundNames = { "Roxo da tela", "Escuro", "Cinza", "Claro" };
        static readonly Color[] BackgroundColors =
        {
            new Color(0.16f, 0.07f, 0.34f), new Color(0.07f, 0.07f, 0.09f), new Color(0.45f, 0.45f, 0.48f), new Color(0.85f, 0.85f, 0.88f),
        };
        static bool dirty;
        static Action repaintCb;

        static MagicProgressBarPreview()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
        }

        static float TotalLength => duration + Hold;
        static float ProgressAt(float t) => t <= 0f ? 0f : Mathf.Clamp01(t / Mathf.Max(0.1f, duration));

        // ---- the bar alone ---------------------------------------------------------------------------------------------

        public static void Draw(float width, Action repaint)
        {
            repaintCb = repaint;
            lastDrawn = EditorApplication.timeSinceStartup;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(MagicProgressBarBuilder.PrefabPath) == null)
            {
                EditorGUILayout.HelpBox("O prefab da barra ainda não foi montado (ele nasce das imagens em Assets/Magic Mail/GUI).", MessageType.Info);
                if (GUILayout.Button("Montar o prefab da barra de progresso", GUILayout.Height(28))) Later(() => MagicProgressBarBuilder.EnsurePrefab());
                return;
            }
            if (!EnsureLoaded(barStage, false))
            {
                EditorGUILayout.HelpBox("Não consegui abrir o prefab na prévia (ele precisa ter o componente MagicProgressBar).", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Fundo", GUILayout.Width(36));
            backgroundIndex = EditorGUILayout.Popup(backgroundIndex, BackgroundNames, GUILayout.Width(100));
            GUILayout.Label("Carregamento simulado", GUILayout.Width(130));
            duration = Mathf.Max(0.5f, EditorGUILayout.FloatField(duration, GUILayout.Width(44)));
            GUILayout.Label("s", GUILayout.Width(12));
            EditorGUILayout.EndHorizontal();

            var rect = GUILayoutUtility.GetRect(width, Mathf.Clamp(width * 0.27f, 110f, 260f), GUILayout.ExpandWidth(true));
            DrawBar(rect);
            DrawTransport();
            DrawSettings();
        }

        static void DrawBar(Rect rect)
        {
            if (Event.current.type != EventType.Repaint || !barStage.Ready) return;
            var size = barStage.Begin(rect, BackgroundColors[backgroundIndex]);
            var barRect = (RectTransform)barStage.instance.transform;
            var barWidth = size.x * 0.86f;
            barRect.anchorMin = barRect.anchorMax = barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.sizeDelta = new Vector2(barWidth, barWidth * 107f / 1108f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.localScale = Vector3.one;
            barStage.Finish(rect);
        }

        // ---- the whole loading screen ------------------------------------------------------------------------------------

        /// Draws the loading screen's picture the way the game composes it — the art (covering the screen, zoomed and shifted),
        /// the shade on its base and the real progress bar — with the same simulated loading as the section below. The
        /// lettering is not drawn here: the caller puts it on top. Returns false when the bar cannot be shown (then the
        /// caller draws its own approximation). Units are those of the 1080-wide screen; barPosition is the bar's centre
        /// measured from the middle of the bottom edge, and barScale multiplies its size.
        public static bool DrawScreenPreview(Rect rect, LoadingLook look, IReadOnlyList<SeasonProp> props, Action repaint)
        {
            var art = look.art;
            var zoom = look.zoom;
            var offset = look.offset;
            var shade = look.bottomShade;
            var barPosition = look.bar.position;
            var barScale = look.bar.scale;
            repaintCb = repaint;
            lastDrawn = EditorApplication.timeSinceStartup;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MagicProgressBarBuilder.PrefabPath) == null) return false;
            if (!EnsureLoaded(barStage, false) || !EnsureLoaded(screenStage, true)) return false;
            if (Event.current.type != EventType.Repaint) return true;
            if (shadeSprite == null) shadeSprite = RoyalAves.EditorTools.RoyalAvesLoadingBuilder.EnsureScrimSprite();

            SyncSettings(screenStage.bar, barStage.bar);     // edits of the bar's settings show here too
            var screen = screenStage.Begin(rect, new Color(0.04f, 0.12f, 0.23f));
            var k = screen.x / 1080f;                        // texture pixels per unit of the 1080-wide screen

            screenStage.art.enabled = art != null;
            if (art != null)
            {
                screenStage.art.sprite = art;
                var aspect = art.rect.width / art.rect.height;
                var cover = screen.x / screen.y > aspect ? new Vector2(screen.x, screen.x / aspect) : new Vector2(screen.y * aspect, screen.y);
                Place(screenStage.art, cover * Mathf.Max(0.1f, zoom), offset * k);
            }
            screenStage.shade.enabled = shade && shadeSprite != null;
            if (screenStage.shade.enabled)
            {
                screenStage.shade.sprite = shadeSprite;
                Place(screenStage.shade, new Vector2(screen.x, screen.y / 2f), new Vector2(0f, -screen.y / 4f));
            }

            // As the game places it (LoadingScreen.Place): the bar is 880 units wide at scale 1, its centre `barPosition` from
            // the middle of the bottom edge, and the scale multiplies the whole bar.
            var barRect = (RectTransform)screenStage.instance.transform;
            var barWidth = ScreenBarWidth * k;
            barRect.anchorMin = barRect.anchorMax = barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.sizeDelta = new Vector2(barWidth, barWidth * 107f / 1108f);
            barRect.anchoredPosition = new Vector2(barPosition.x * k, -screen.y / 2f + barPosition.y * k);
            barRect.localScale = Vector3.one * Mathf.Max(0.1f, barScale);

            screenStage.SyncProps(props ?? new List<SeasonProp>(), screen, k, time);
            screenStage.Finish(rect);
            return true;
        }

        static void Place(Image image, Vector2 size, Vector2 position)
        {
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
        }

        // Copies the bar's settings (everything the inspector shows; the parts it is built from are hidden and left alone).
        static void SyncSettings(MagicProgressBar to, MagicProgressBar from)
        {
            var source = new SerializedObject(from);
            var target = new SerializedObject(to);
            var it = source.GetIterator();
            for (var enter = true; it.NextVisible(enter); enter = false)
            {
                if (it.propertyPath == "m_Script") continue;
                target.CopyFromSerializedProperty(it);
            }
            target.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- loading the prefab into the preview scenes -----------------------------------------------------------------

        // The prefab in a preview scene, loaded (or reloaded) when needed. The first time in a session it also brings the
        // bar's pictures up to date with the builder (see MagicProgressBarBuilder.RefreshAssets).
        static bool EnsureLoaded(Stage stage, bool screenLayers)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MagicProgressBarBuilder.PrefabPath);
            if (prefab == null) return false;
            if (!refreshed)
            {
                refreshed = true;
                if (MagicProgressBarBuilder.RefreshAssets())
                {
                    Cleanup();
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MagicProgressBarBuilder.PrefabPath);
                }
            }
            if (stage.Ready) return true;
            if (!stage.Load(prefab, screenLayers)) return false;
            if (stage == barStage)
            {
                if (barEditor != null) Object.DestroyImmediate(barEditor);
                barEditor = UnityEditor.Editor.CreateEditor(stage.bar);
                time = 0f;
                playing = true;
                dirty = false;
            }
            lastTick = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            return true;
        }

        public static void Cleanup()
        {
            EditorApplication.update -= Tick;
            if (barEditor != null) Object.DestroyImmediate(barEditor);
            barEditor = null;
            barStage.Dispose();
            screenStage.Dispose();
        }

        static void Tick()
        {
            var now = EditorApplication.timeSinceStartup;
            var dt = Mathf.Min(0.1f, (float)(now - lastTick));
            lastTick = now;
            if (now - lastDrawn > 1.0) { Cleanup(); return; }    // the tab is not on screen: free the previews
            if (!playing) return;
            time += dt * SpeedValues[speedIndex];
            if (time >= TotalLength)
            {
                if (loop) time %= TotalLength;
                else { time = TotalLength; playing = false; }
            }
            repaintCb?.Invoke();
        }

        // ---- transport and settings -------------------------------------------------------------------------------------

        static void DrawTransport()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("⏮", GUILayout.Width(30))) { time = 0f; playing = false; }
            if (GUILayout.Button("◀|", GUILayout.Width(34))) { playing = false; time = Mathf.Max(0f, time - FrameStep); }
            if (GUILayout.Button(playing ? "⏸ Pausar" : "▶ Tocar", GUILayout.Width(80)))
            {
                if (!playing && !loop && time >= TotalLength) time = 0f;
                playing = !playing;
                lastTick = EditorApplication.timeSinceStartup;
            }
            if (GUILayout.Button("|▶", GUILayout.Width(34))) { playing = false; time = Mathf.Min(TotalLength, time + FrameStep); }
            loop = GUILayout.Toggle(loop, "Repetir", GUILayout.Width(62));
            GUILayout.Label("Velocidade", GUILayout.Width(65));
            speedIndex = EditorGUILayout.Popup(speedIndex, SpeedLabels, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            var v = EditorGUILayout.Slider($"{time:0.000}s  ({ProgressAt(time) * 100f:0}%)", time, 0f, TotalLength);
            if (EditorGUI.EndChangeCheck()) { time = v; playing = false; }
        }

        // The bar's own settings (trail, sparkles, card, glow, margins): they change both pictures at once.
        static void DrawSettings()
        {
            if (barEditor == null) return;
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Ajustes da barra (valem para todas as telas que a usam)", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            barEditor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck()) dirty = true;

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!dirty))
            {
                GUI.backgroundColor = dirty ? new Color(0.6f, 1f, 0.6f) : Color.white;
                if (GUILayout.Button(dirty ? "● Salvar no prefab" : "Salvar no prefab", GUILayout.Height(26))) Later(Save);
                GUI.backgroundColor = Color.white;
                if (GUILayout.Button("Reverter", GUILayout.Height(26), GUILayout.Width(80))) Later(Cleanup);   // reloads from the prefab
            }
            if (GUILayout.Button("Selecionar o prefab", GUILayout.Height(26), GUILayout.Width(140)))
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(MagicProgressBarBuilder.PrefabPath));
            EditorGUILayout.EndHorizontal();
            if (dirty)
                EditorGUILayout.HelpBox("Alterações só na prévia por enquanto. \"Salvar no prefab\" grava no arquivo; as telas que já usam a barra passam a ter os novos valores.", MessageType.None);
        }

        // Only the settings of the component are written: the preview copy also carries the size and the pose the bar was
        // given to be drawn, which must not end up in the prefab.
        static void Save()
        {
            if (!barStage.Ready) return;
            try
            {
                PrefabUtility.ApplyObjectOverride(barStage.bar, MagicProgressBarBuilder.PrefabPath, InteractionMode.AutomatedAction);
                AssetDatabase.SaveAssets();
                dirty = false;
                Debug.Log("[Barra de progresso] ajustes salvos em " + MagicProgressBarBuilder.PrefabPath);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Salvar no prefab", "Não consegui salvar: " + e.Message, "OK");
            }
        }

        static void Later(Action action) => EditorApplication.delayCall += () => { action(); repaintCb?.Invoke(); };
    }
}
