// Area editor of Correio Mágico: the areas of RoyalAvesProgression.asset with each area's upgrades (layer, icon, cost in
// stars, order, build effect), the chest reward, and a preview of the background with the layers bought so far.
// It lives in the "Areas" tab of the Sweet Sugar Level editor (LevelMakerEditor calls RoyalAvesAreaPanel.DrawTab) and also
// opens alone from the menu Royal Aves > Editor de áreas.
//
// Flow, top to bottom: pick the area; pick the upgrade (etapa) and its build effect above the preview, press ▶ Testar;
// the timeline (play, scrub, step, slow motion) sits right under the preview; below are the compact list of upgrades and,
// for the selected one, its objects to place (drag in the preview, wheel = size) and the files it ends up with.
// "Importar pasta" builds an area from a folder of pictures under Assets/RoyalAves/Art: the file with "bg" or "fundo" in
// its name is the empty background, "icone-X" is the icon of the layer "X", and every other picture is a layer, in
// file-name order (the back-to-front draw order and the order they are bought in). Importing again into an existing
// area keeps the costs already set.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoyalAves.Meta;
using SweetSugar.Scripts.GUI.Boost;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public class RoyalAvesAreaEditor : EditorWindow
    {
        readonly RoyalAvesAreaPanel panel = new RoyalAvesAreaPanel();
        Vector2 scroll;

        [MenuItem("Royal Aves/Editor de áreas")]
        static void Open() => GetWindow<RoyalAvesAreaEditor>("Áreas").minSize = new Vector2(420, 480);

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            panel.Draw(position.width - 24, position.height, Repaint);
            EditorGUILayout.EndScrollView();
        }
    }

    // Static state so the selection and the effect test survive the Level editor redrawing itself.
    public class RoyalAvesAreaPanel
    {
        const string ConfigPath = "Assets/RoyalAves/Resources/" + AreaProgressionConfig.ResourcePath + ".asset";
        const string ArtRoot = "Assets/RoyalAves/Art/";
        const float PreviewMaxHeight = 460f;

        static AreaProgressionConfig config;
        static int areaIndex;

        // The selected upgrade (always one when the area has any) and the object of it that the mouse moves.
        static int activeTask, activeObj;
        static bool showAreaData, showFiles, showLater;
        static PlacementFile placements;
        static string placementsFolder;
        static Vector2 grabOffset;
        static System.Action repaintCb;

        // Build-up effect test: the same material, sprite and duration the game uses (AreaStageReveal), driven by time.
        const string LobbyPrefab = "Assets/RoyalAves/Prefabs/RoyalAvesLobby.prefab";
        const string FallbackBurn = "Assets/Plugins/AllIn1SpriteShader/Textures/fire2.png";
        const float EffectTail = 1.3f;      // keeps playing after the dissolve so the burst and the last sparkles fade out
        static int effectTask = -1;
        static AreaStageReveal.BuildEffect effectStyle;
        static float effectTotal = AreaStageReveal.Duration + EffectTail;
        static readonly string[] ShortEffectNames = { "Magia", "Pop elástico", "Varredura", "Raios + estrelas" };
        static float effectTime;            // the playhead, seconds into the effect
        static double lastTick;
        static bool effectPlaying;
        static int speedIndex = 4;
        static readonly float[] SpeedValues = { 0.05f, 0.1f, 0.25f, 0.5f, 1f };
        static readonly string[] SpeedLabels = { "0,05x", "0,1x", "0,25x", "0,5x", "1x" };
        const float FrameStep = 1f / 60f;
        static Material fxMaterial;
        static Texture2D fxBurn;
        static bool fxLoaded;

        // Call from inside an EditorWindow.OnGUI. `width` is the usable width, `repaint` redraws that window.
        public static void DrawTab(float width, System.Action repaint) => shared.Draw(width, 0f, repaint);
        public static void DrawTab(float width, float height, System.Action repaint) => shared.Draw(width, height, repaint);
        static readonly RoyalAvesAreaPanel shared = new RoyalAvesAreaPanel();

        // Wide window (and its height known): an editor layout — areas and upgrades on the left, the canvas in the middle,
        // the selected upgrade's objects/files, chest and checks on the right. Narrow: one readable column, stacked.
        const float MinWideWidth = 1000f, LeftWidth = 300f, MaxContentWidth = 760f;
        static Vector2 leftScroll, rightScroll;

        public void Draw(float width, System.Action repaint) => Draw(width, 0f, repaint);

        public void Draw(float width, float height, System.Action repaint)
        {
            if (width >= MinWideWidth && height > 0f)
            {
                DrawContent(width, height, true, repaint);
                return;
            }
            var usable = Mathf.Min(width, MaxContentWidth);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical(GUILayout.Width(usable));
            DrawContent(usable, height, false, repaint);
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        void DrawContent(float width, float height, bool wide, System.Action repaint)
        {
            if (config == null) config = AssetDatabase.LoadAssetAtPath<AreaProgressionConfig>(ConfigPath);
            if (config == null)
            {
                EditorGUILayout.HelpBox("Falta " + ConfigPath + " (menu Royal Aves > Criar lobby da agência).", MessageType.Warning);
                return;
            }
            areaIndex = Mathf.Clamp(areaIndex, 0, Mathf.Max(0, config.areas.Count - 1));
            repaintCb = repaint;

            DrawHeader(repaint);
            if (config.areas.Count == 0)
            {
                EditorGUILayout.HelpBox("Nenhuma área ainda. Use \"+ Nova\" ou \"Importar pasta…\".", MessageType.Info);
                return;
            }
            var area = config.areas[areaIndex];
            var folder = RoyalAvesAreaPlacer.AreaFolder(area);
            if (folder != placementsFolder)
            {
                placementsFolder = folder;
                placements = folder == null ? new PlacementFile() : RoyalAvesAreaPlacer.Load(folder);
                activeTask = 0;
                StopEffect();
            }
            activeTask = area.tasks.Count == 0 ? -1 : Mathf.Clamp(activeTask, 0, area.tasks.Count - 1);

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(config, "Editar área");

            if (wide)
            {
                var columnHeight = Mathf.Max(320f, height - 150f);   // below the Level editor's tabs and our header
                var rightWidth = Mathf.Clamp(width * 0.27f, 400f, 560f);   // the inspector gets the room the canvas does not need
                var centerWidth = Mathf.Max(320f, width - LeftWidth - rightWidth - 28f);
                EditorGUILayout.BeginHorizontal();

                // Left: the area's data and its list of upgrades.
                EditorGUILayout.BeginVertical(GUILayout.Width(LeftWidth));
                leftScroll = ColumnScroll(leftScroll, columnHeight);
                DrawAreaData(area);
                DrawTasks(area, true);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();

                // Middle: the canvas, with the stage/effect bar above it and the timeline below.
                EditorGUILayout.BeginVertical(GUILayout.Width(centerWidth));
                DrawPreview(area, centerWidth, columnHeight - 130f);
                EditorGUILayout.EndVertical();

                // Right: everything about the selected upgrade, then the chest and the checks.
                EditorGUILayout.BeginVertical(GUILayout.Width(rightWidth));
                rightScroll = ColumnScroll(rightScroll, columnHeight);
                DrawSelectedTask(area);
                DrawReward(area);
                DrawChecks(area);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                DrawAreaData(area);
                DrawPreview(area, width, PreviewMaxHeight);
                DrawSelectedTask(area);   // right under the preview, where the objects are moved
                DrawTasks(area, false);
                DrawReward(area);
                DrawChecks(area);
            }
            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(config);

            EditorGUILayout.Space();
            if (GUILayout.Button("Salvar (grava o asset)", GUILayout.Height(24))) AssetDatabase.SaveAssets();
        }

        // A column that scrolls up and down only: no horizontal bar, whatever the width of its content.
        static Vector2 ColumnScroll(Vector2 position, float height) =>
            EditorGUILayout.BeginScrollView(position, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView, GUILayout.Height(height));

        // Id, title and background: folded away once the area has a background.
        void DrawAreaData(AreaDefinition area)
        {
            var open = EditorGUILayout.Foldout(showAreaData || area.background == null, $"Dados da área — {area.title} ({area.id})", true);
            if (area.background != null) showAreaData = open;
            if (!open) return;
            area.id = EditorGUILayout.TextField("Id", area.id);
            area.title = EditorGUILayout.TextField("Título", area.title);
            area.background = (Sprite)EditorGUILayout.ObjectField("Fundo (sem móveis)", area.background, typeof(Sprite), false);
        }

        // ---- header: pick / add / import / order / delete --------------------------------------------------------

        void DrawHeader(System.Action repaint)
        {
            EditorGUILayout.BeginHorizontal();
            if (config.areas.Count > 0)
            {
                var names = config.areas.Select((a, i) => $"{i + 1}. {(string.IsNullOrEmpty(a.title) ? a.id : a.title)} ({a.tasks.Count} etapas, {a.tasks.Sum(t => t.starCost)}⭐)").ToArray();
                var picked = EditorGUILayout.Popup(areaIndex, names);
                if (picked != areaIndex) { areaIndex = picked; activeTask = 0; StopEffect(); }
            }
            else GUILayout.FlexibleSpace();

            if (GUILayout.Button("+ Nova", GUILayout.Width(60)))
                Mutate("Nova área", repaint, () =>
                {
                    config.areas.Add(new AreaDefinition { id = "area" + (config.areas.Count + 1), title = "Nova área" });
                    areaIndex = config.areas.Count - 1;
                });
            if (GUILayout.Button("Importar pasta…", GUILayout.Width(110))) ImportFolder(repaint);
            using (new EditorGUI.DisabledScope(config.areas.Count == 0))
            {
                if (GUILayout.Button("▲", GUILayout.Width(24))) MoveArea(-1, repaint);
                if (GUILayout.Button("▼", GUILayout.Width(24))) MoveArea(1, repaint);
                if (GUILayout.Button("✕", GUILayout.Width(24)) &&
                    EditorUtility.DisplayDialog("Excluir área", $"Excluir \"{config.areas[areaIndex].title}\" e as {config.areas[areaIndex].tasks.Count} etapas dela?\n(As imagens ficam na pasta.)", "Excluir", "Cancelar"))
                    Mutate("Excluir área", repaint, () => config.areas.RemoveAt(areaIndex));
            }
            EditorGUILayout.EndHorizontal();
        }

        static void MoveArea(int dir, System.Action repaint)
        {
            var to = areaIndex + dir;
            if (to < 0 || to >= config.areas.Count) return;
            Mutate("Mover área", repaint, () =>
            {
                (config.areas[areaIndex], config.areas[to]) = (config.areas[to], config.areas[areaIndex]);
                areaIndex = to;
            });
        }

        // ---- preview -----------------------------------------------------------------------------------------------

        void DrawPreview(AreaDefinition area, float width, float maxHeight)
        {
            if (area.background == null)
            {
                EditorGUILayout.HelpBox("Escolha o fundo da área em \"Dados da área\".", MessageType.Info);
                return;
            }
            EditorGUILayout.Space();
            var count = area.tasks.Count;
            if (count > 0) DrawStageToolbar(area);

            var aspect = area.background.rect.width / area.background.rect.height;
            var h = Mathf.Min(Mathf.Max(220f, maxHeight), Mathf.Max(200f, width) / aspect); // phone-shaped art: tall, so cap it
            var rect = GUILayoutUtility.GetRect(h * aspect, h, GUILayout.ExpandWidth(false));
            rect.x += Mathf.Max(0, (width - rect.width) / 2);

            var editing = activeTask >= 0 && activeTask < count;
            if (effectTask >= 0 && effectTask < count) DrawTimeline();
            if (count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                var hasObjects = editing && placements.Get(area.tasks[activeTask].id).objects.Count > 0;
                EditorGUILayout.LabelField(
                    !editing ? "" :
                    effectTask >= 0 ? "Teste de efeito aberto — clique ou arraste na prévia para voltar a editar" :
                    hasObjects ? "Arraste para mover · puxe um canto amarelo (ou use a roda do mouse) para redimensionar" :
                    "Esta etapa ainda não tem objetos para mover: veja os botões logo abaixo da prévia",
                    EditorStyles.miniLabel);
                showLater = GUILayout.Toggle(showLater, "Mostrar etapas seguintes", GUILayout.Width(170));
                EditorGUILayout.EndHorizontal();
            }
            if (editing) HandlePreviewInput(rect, area);

            // Clipped, so an object pushed past the edge of the background does not paint over the window.
            var playing = effectTask >= 0 && effectTask < count && area.tasks[effectTask].layer != null &&
                          (fxMaterial != null || !AreaStageReveal.UsesDissolve(effectStyle));
            GUI.BeginClip(rect);
            var local = new Rect(0, 0, rect.width, rect.height);
            DrawSprite(local, area.background);
            var current = playing ? effectTask : editing ? activeTask : count;
            for (var i = 0; i < count && i < current; i++)
                if (area.tasks[i].layer != null) DrawSprite(local, area.tasks[i].layer);
            if (playing) DrawEffect(local, area.tasks[effectTask]);
            else if (editing)
            {
                var placement = placements.Get(area.tasks[activeTask].id);
                // Nothing placed yet: show the picture already baked so the stage is not blank.
                if (placement.objects.Count == 0 && area.tasks[activeTask].layer != null) DrawSprite(local, area.tasks[activeTask].layer);
                for (var k = 0; k < placement.objects.Count; k++)
                {
                    var r = ObjectRect(placement.objects[k], local, out var tex);
                    if (tex == null) continue;
                    GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, true);
                    if (k == activeObj) { DrawOutline(r, Color.yellow); DrawHandles(r); }
                }
            }
            if (showLater && !playing)
                for (var i = current + 1; i < count; i++)
                    if (area.tasks[i].layer != null) DrawSprite(local, area.tasks[i].layer);
            GUI.EndClip();
        }

        // Above the preview: which upgrade, which effect, and the button that plays it.
        static void DrawStageToolbar(AreaDefinition area)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Etapa", GUILayout.Width(36));
            var names = area.tasks.Select((t, i) => $"{i + 1}. {t.title}").ToArray();
            var picked = EditorGUILayout.Popup(activeTask, names);
            if (picked != activeTask) { activeTask = picked; activeObj = 0; }
            var task = area.tasks[activeTask];

            GUILayout.Label("Efeito", GUILayout.Width(38));
            task.effect = (AreaStageReveal.BuildEffect)EditorGUILayout.Popup((int)task.effect, ShortEffectNames, GUILayout.Width(100));
            if (GUILayout.Button(new GUIContent("Todas", "Usar este efeito em todas as etapas da área"), GUILayout.Width(48)))
                foreach (var t in area.tasks) t.effect = task.effect;

            using (new EditorGUI.DisabledScope(!CanPlay(task)))
            {
                GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
                var index = activeTask;
                if (GUILayout.Button("▶ Testar efeito", GUILayout.Width(110))) Later(() => PlayEffect(area, index));
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();
        }

        // ---- build-up effect test ----------------------------------------------------------------------------------

        // Playable with a baked layer, or with placed objects (the layer is baked from them first).
        static bool CanPlay(AreaTask task) => task.layer != null || placements.Get(task.id).objects.Count > 0;

        static void PlayEffect(AreaDefinition area, int index)
        {
            var task = area.tasks[index];
            var placement = placements.Get(task.id);
            // Objects placed: bake them first, so the test shows the current positions (and the layer exists to play).
            if (placement.objects.Count > 0) BakeTask(area, task, placement);
            if (task.layer == null) return;
            if (!fxLoaded)
            {
                fxLoaded = true;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LobbyPrefab);
                var lobby = prefab != null ? prefab.GetComponentInChildren<LobbyController>(true) : null;
                if (lobby != null)
                {
                    var so = new SerializedObject(lobby); // the very assets the game plays the effect with
                    fxBurn = so.FindProperty("stageBurnTexture")?.objectReferenceValue as Texture2D;
                }
                if (fxBurn == null) fxBurn = AssetDatabase.LoadAssetAtPath<Texture2D>(FallbackBurn);
            }
            StopEffect();
            effectStyle = task.effect;
            if (AreaStageReveal.UsesDissolve(effectStyle))
            {
                fxMaterial = AreaStageReveal.CreateFadeMaterial(fxBurn, task.RevealBounds, effectStyle);
                if (fxMaterial == null)
                {
                    EditorUtility.DisplayDialog("Efeito de construção", "Não achei o shader AllIn1SpriteShader/AllIn1SpriteShader.", "OK");
                    return;
                }
            }
            effectTask = index;
            effectTotal = AreaStageReveal.DurationOf(effectStyle) + EffectTail;
            effectTime = 0f;
            effectPlaying = true;
            lastTick = EditorApplication.timeSinceStartup;
            EditorApplication.update -= EffectTick;
            EditorApplication.update += EffectTick;
        }

        // Advances the playhead at the chosen speed; at the end it stays there (paused) so it can be scrubbed.
        static void EffectTick()
        {
            var now = EditorApplication.timeSinceStartup;
            var dt = Mathf.Min(0.1f, (float)(now - lastTick));
            lastTick = now;
            if (!effectPlaying) return;
            effectTime += dt * SpeedValues[speedIndex];
            if (effectTime >= effectTotal) { effectTime = effectTotal; effectPlaying = false; }
            repaintCb?.Invoke();
        }

        // Play / pause, scrub and step frame by frame, with slow motion.
        static void DrawTimeline()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("⏮", GUILayout.Width(30))) { effectTime = 0f; effectPlaying = false; }
            if (GUILayout.Button("◀|", GUILayout.Width(34))) { effectPlaying = false; effectTime = Mathf.Max(0f, effectTime - FrameStep); }
            if (GUILayout.Button(effectPlaying ? "⏸ Pausar" : "▶ Tocar", GUILayout.Width(80)))
            {
                if (!effectPlaying && effectTime >= effectTotal) effectTime = 0f;
                effectPlaying = !effectPlaying;
                lastTick = EditorApplication.timeSinceStartup;
            }
            if (GUILayout.Button("|▶", GUILayout.Width(34))) { effectPlaying = false; effectTime = Mathf.Min(effectTotal, effectTime + FrameStep); }
            GUILayout.Label("Velocidade", GUILayout.Width(65));
            speedIndex = EditorGUILayout.Popup(speedIndex, SpeedLabels, GUILayout.Width(60));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕ Fechar", GUILayout.Width(70))) Later(StopEffect);
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            var v = EditorGUILayout.Slider($"{effectTime:0.000}s", effectTime, 0f, effectTotal);
            if (EditorGUI.EndChangeCheck()) { effectTime = v; effectPlaying = false; }
            EditorGUILayout.LabelField($"{AreaStageReveal.EffectNames[(int)effectStyle]} · efeito 0 a {AreaStageReveal.DurationOf(effectStyle):0.0}s · " +
                                       $"explosão a partir de {AreaStageReveal.BurstAtOf(effectStyle):0.0}s · 1 passo = 1/60 s", EditorStyles.miniLabel);
        }

        static void StopEffect()
        {
            EditorApplication.update -= EffectTick;
            if (fxMaterial != null) UnityEngine.Object.DestroyImmediate(fxMaterial);
            fxMaterial = null;
            effectTask = -1;
            repaintCb?.Invoke();
        }

        static void DrawEffect(Rect local, AreaTask task)
        {
            var t = effectTime;
            var d = AreaStageReveal.DurationOf(effectStyle);
            var p = Mathf.Clamp01(t / d);
            if (effectStyle == AreaStageReveal.BuildEffect.PopElastico)
            {
                // Scaled about the base of the object, which stays put; alpha comes in fast.
                var b = task.RevealBounds;
                var pivot = new Vector2(local.x + (b.x + b.width / 2f) * local.width, local.y + (1f - b.y) * local.height);
                var s = AreaStageReveal.PopScale(p);
                var oldMatrix = GUI.matrix;
                var oldColor = GUI.color;
                GUI.matrix = oldMatrix * Matrix4x4.Translate(pivot) * Matrix4x4.Scale(new Vector3(s, s, 1f)) * Matrix4x4.Translate(-pivot);
                GUI.color = new Color(1f, 1f, 1f, AreaStageReveal.PopAlpha(p));
                DrawSprite(local, task.layer);
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
            }
            else
            {
                fxMaterial.SetFloat("_FadeAmount", Mathf.Lerp(AreaStageReveal.FadeAmountHidden, AreaStageReveal.FadeAmountShown, p));
                EditorGUI.DrawPreviewTexture(local, task.layer.texture, fxMaterial, ScaleMode.StretchToFill);
            }
            DrawSparkles(local, task, t);
        }

        // The game sheds SparklesPerStep sparkles every SparkleInterval along the moving edge (not for the pop), and bursts
        // from the outline when the burst goes off. Each is made from the same recipe the game uses (MakeSparkle/MakeBurst)
        // and is a pure function of time, so the preview needs no state. Sizes are canvas pixels on a 1080-wide canvas.
        static void DrawSparkles(Rect local, AreaTask task, float t)
        {
            var style = effectStyle;
            var b = task.RevealBounds;
            var sprite = AreaStageReveal.GetSparkleSprite();
            var scale = local.width / 1080f;
            const int seed = 12345;
            var old = GUI.color;
            var oldMatrix = GUI.matrix;

            void Draw(AreaStageReveal.SparkleSpec s, float age, Vector2 origin01, bool burst)
            {
                if (age < 0f || age >= s.life) return;
                var u = age / s.life;
                var offset = AreaStageReveal.SparkleOffset(s, u, burst) * scale;
                var cx = local.x + origin01.x * local.width + offset.x;
                var cy = local.y + (1f - origin01.y) * local.height - offset.y; // y up in canvas space, down in the GUI
                var size = s.size * scale * AreaStageReveal.SparkleScale(u);
                if (size < 0.5f) return;
                GUI.color = new Color(s.color.r, s.color.g, s.color.b, AreaStageReveal.SparkleAlpha(u));
                GUI.matrix = oldMatrix;
                GUIUtility.RotateAroundPivot(-s.spin * age, new Vector2(cx, cy));
                DrawSprite(new Rect(cx - size / 2, cy - size / 2, size, size), sprite);
            }

            var d = AreaStageReveal.DurationOf(style);
            if (AreaStageReveal.HasTrail(style))
            {
                var spawnUntil = Mathf.Min(t, d);
                var step = 0;
                for (var spawn = 0f; spawn <= spawnUntil; spawn += AreaStageReveal.SparkleInterval, step++)
                for (var j = 0; j < AreaStageReveal.SparklesPerStep; j++)
                {
                    var s = AreaStageReveal.MakeSparkle(step * AreaStageReveal.SparklesPerStep + j, seed, style);
                    Draw(s, t - spawn, AreaStageReveal.TrailOrigin(style, s, b, spawn / d), false);
                }
            }

            // The burst starts on the object's outline and flies out of it.
            var region = task.Region;
            var burstAt = AreaStageReveal.BurstAtOf(style);

            // Streaks of light shooting out of the outline (drawn first, so the stars are over them).
            if (style == AreaStageReveal.BuildEffect.Raios)
            {
                var rayTint = GUI.color;
                for (var k = 0; k < AreaStageReveal.RayCount; k++)
                {
                    var ray = AreaStageReveal.MakeRay(k, seed);
                    var age = t - burstAt - ray.delay;
                    if (age < 0f || age >= ray.life) continue;
                    var u = age / ray.life;
                    AreaStageReveal.OutlinePoint(region, k, AreaStageReveal.RayCount, ray.spread, out var origin, out var angle);
                    var head = AreaStageReveal.RayHead(ray, u);
                    var tail = AreaStageReveal.RayTail(ray, u);
                    var length = Mathf.Max(1f, head - tail) * scale;
                    var centre = (head + tail) / 2f * scale;
                    var cx = local.x + origin.x * local.width + Mathf.Cos(angle) * centre;
                    var cy = local.y + (1f - origin.y) * local.height - Mathf.Sin(angle) * centre; // y up in canvas space
                    var thickness = Mathf.Max(1f, ray.width * scale);
                    GUI.matrix = oldMatrix;
                    GUIUtility.RotateAroundPivot(-angle * Mathf.Rad2Deg, new Vector2(cx, cy));
                    GUI.color = new Color(ray.color.r, ray.color.g, ray.color.b, AreaStageReveal.RayAlpha(u));
                    DrawSprite(new Rect(cx - length / 2f, cy - thickness / 2f, length, thickness), AreaStageReveal.GetRaySprite());
                }
                GUI.matrix = oldMatrix;
                GUI.color = rayTint;
            }

            for (var k = 0; k < AreaStageReveal.BurstCountOf(style); k++)
            {
                var s = AreaStageReveal.MakeBurst(k, seed, style);
                AreaStageReveal.BurstPoint(region, k, s, out var origin, out var angle);
                s.angle = angle;
                Draw(s, t - burstAt, origin, true);
            }

            // The pop's shockwave: a flat ring spreading on the ground from the base of the object.
            var ringAge = t - burstAt;
            if (style == AreaStageReveal.BuildEffect.PopElastico && ringAge >= 0f && ringAge < AreaStageReveal.RingLife)
            {
                var u = ringAge / AreaStageReveal.RingLife;
                var diameter = b.width * local.width * AreaStageReveal.RingWidthFactor * AreaStageReveal.RingScale(u);
                var cx = local.x + (b.x + b.width / 2f) * local.width;
                var cy = local.y + (1f - b.y) * local.height;
                var c = AreaStageReveal.RingColor;
                GUI.matrix = oldMatrix;
                GUI.color = new Color(c.r, c.g, c.b, AreaStageReveal.RingAlpha(u));
                var h = diameter * AreaStageReveal.RingFlatten;
                DrawSprite(new Rect(cx - diameter / 2f, cy - h / 2f, diameter, h), AreaStageReveal.GetRingSprite());
            }

            GUI.matrix = oldMatrix;
            GUI.color = old;
        }

        static void DrawOutline(Rect r, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            var white = Texture2D.whiteTexture;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2), white);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 2, r.width, 2), white);
            GUI.DrawTexture(new Rect(r.x, r.y, 2, r.height), white);
            GUI.DrawTexture(new Rect(r.xMax - 2, r.y, 2, r.height), white);
            GUI.color = old;
        }

        // Where an object is drawn inside `area` (a rect the size of the background in the window).
        static Rect ObjectRect(PlacedObject o, Rect area, out Texture2D tex)
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(o.source ?? "");
            if (tex == null) return default;
            var pw = o.w * area.width;
            var ph = pw * tex.height / tex.width;
            return new Rect(area.x + o.cx * area.width - pw / 2, area.y + o.cy * area.height - ph / 2, pw, ph);
        }

        static void HandlePreviewInput(Rect rect, AreaDefinition area)
        {
            var placement = placements.Get(area.tasks[activeTask].id);
            if (activeObj < 0 || activeObj >= placement.objects.Count) return;
            var o = placement.objects[activeObj];
            var e = Event.current;
            // The corner handles of the selected object: the cursor changes over them, and a drag that started on one keeps
            // working when the mouse leaves the canvas.
            var objectRect = ObjectRect(o, rect, out var objectTex);
            if (objectTex != null && dragHandle < 0 && e.type == EventType.Repaint)
            {
                var handles = HandleRects(objectRect);
                for (var i = 0; i < 4; i++)
                    EditorGUIUtility.AddCursorRect(handles[i], i == 0 || i == 3 ? MouseCursor.ResizeUpLeft : MouseCursor.ResizeUpRight);
            }
            if (dragHandle < 0 && !rect.Contains(e.mousePosition) && e.type != EventType.MouseUp) return;
            var mouse = new Vector2((e.mousePosition.x - rect.x) / rect.width, (e.mousePosition.y - rect.y) / rect.height);
            // While an effect test is open the preview shows the baked layer, not the live objects, so a move would be
            // invisible. Touching the preview closes the test and goes back to editing.
            if (effectTask >= 0 && (e.type == EventType.MouseDown || e.type == EventType.ScrollWheel)) StopEffect();
            switch (e.type)
            {
                case EventType.MouseDown when e.button == 0:
                    dragHandle = -1;
                    if (objectTex != null)
                    {
                        var handles = HandleRects(objectRect);
                        for (var i = 0; i < 4; i++)
                            if (handles[i].Contains(e.mousePosition))
                            {
                                // Resize: the opposite corner stays where it is, the aspect ratio is kept.
                                dragHandle = i;
                                resizeAnchor = Corner(objectRect, 3 - i);
                                resizeAspect = objectRect.width / Mathf.Max(1f, objectRect.height);
                                break;
                            }
                    }
                    grabOffset = new Vector2(o.cx, o.cy) - mouse; // keep the grab point, so the object does not jump
                    e.Use();
                    break;
                case EventType.MouseDrag when e.button == 0:
                    if (dragHandle >= 0)
                    {
                        // The new corner follows the mouse in the quadrant it started in; the bigger of the two pulls wins.
                        var delta = e.mousePosition - resizeAnchor;
                        var startsLeft = dragHandle == 0 || dragHandle == 2;   // the dragged corner is on the left
                        var startsTop = dragHandle == 0 || dragHandle == 1;    // ... and on the top
                        var sx = Mathf.Abs(delta.x) < 1f ? (startsLeft ? -1f : 1f) : Mathf.Sign(delta.x);
                        var sy = Mathf.Abs(delta.y) < 1f ? (startsTop ? -1f : 1f) : Mathf.Sign(delta.y);
                        var width = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y) * resizeAspect);
                        width = Mathf.Clamp(width, 0.01f * rect.width, 1.5f * rect.width);
                        var height = width / resizeAspect;
                        var centre = resizeAnchor + new Vector2(sx * width / 2f, sy * height / 2f);
                        o.w = width / rect.width;
                        o.cx = Mathf.Clamp01((centre.x - rect.x) / rect.width);
                        o.cy = Mathf.Clamp01((centre.y - rect.y) / rect.height);
                    }
                    else
                    {
                        o.cx = Mathf.Clamp01(mouse.x + grabOffset.x);
                        o.cy = Mathf.Clamp01(mouse.y + grabOffset.y);
                    }
                    e.Use(); repaintCb?.Invoke();
                    break;
                case EventType.ScrollWheel:
                    o.w = Mathf.Clamp(o.w * (1f - e.delta.y * 0.03f), 0.01f, 1.5f);
                    e.Use(); repaintCb?.Invoke();
                    SavePlacements();
                    break;
                case EventType.MouseUp when e.button == 0:
                    dragHandle = -1;
                    SavePlacements();
                    break;
            }
        }

        // The corner handles of an object's box, drawn and picked in the same size: 0 top-left, 1 top-right, 2 bottom-left,
        // 3 bottom-right (so 3 - i is always the opposite corner).
        const float HandleSize = 12f;
        static int dragHandle = -1;
        static Vector2 resizeAnchor;
        static float resizeAspect = 1f;

        static Vector2 Corner(Rect r, int i) => new Vector2(i % 2 == 0 ? r.xMin : r.xMax, i < 2 ? r.yMin : r.yMax);

        static Rect[] HandleRects(Rect r)
        {
            var result = new Rect[4];
            for (var i = 0; i < 4; i++)
            {
                var c = Corner(r, i);
                result[i] = new Rect(c.x - HandleSize / 2f, c.y - HandleSize / 2f, HandleSize, HandleSize);
            }
            return result;
        }

        static void DrawHandles(Rect r)
        {
            var old = GUI.color;
            foreach (var h in HandleRects(r))
            {
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(h.x - 1, h.y - 1, h.width + 2, h.height + 2), Texture2D.whiteTexture);
                GUI.color = Color.yellow;
                GUI.DrawTexture(h, Texture2D.whiteTexture);
            }
            GUI.color = old;
        }

        static void SavePlacements()
        {
            if (placementsFolder != null) RoyalAvesAreaPlacer.Save(placementsFolder, placements);
        }

        static void DrawSprite(Rect rect, Sprite sprite)
        {
            var tex = sprite.texture;
            var r = sprite.textureRect;
            var uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);
            GUI.DrawTextureWithTexCoords(rect, tex, uv, true);
        }

        // ---- upgrades: the compact list ----------------------------------------------------------------------------

        // One short row per upgrade: pick it (the number button), name, cost, effect, order. Everything else about the
        // selected one is in DrawSelectedTask.
        void DrawTasks(AreaDefinition area, bool compact)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(compact ? $"Etapas ({area.tasks.Sum(t => t.starCost)} ⭐)" : $"Etapas — de trás para a frente, compradas nesta ordem ({area.tasks.Sum(t => t.starCost)} ⭐ no total)", EditorStyles.boldLabel);
            int remove = -1, move = 0, moveFrom = -1;
            for (var i = 0; i < area.tasks.Count; i++)
            {
                var t = area.tasks[i];
                var selected = i == activeTask;
                EditorGUILayout.BeginHorizontal(selected ? (GUIStyle)"selectionRect" : GUIStyle.none, GUILayout.Height(30));
                var thumb = t.icon != null ? t.icon : t.layer;
                var box = GUILayoutUtility.GetRect(28, 28, GUILayout.Width(28));
                if (thumb != null) DrawSprite(box, thumb); else GUI.Box(box, GUIContent.none);
                if (GUILayout.Toggle(selected, (i + 1).ToString(), "Button", GUILayout.Width(28), GUILayout.Height(28)) && !selected)
                {
                    activeTask = i;
                    activeObj = 0;
                }
                t.title = EditorGUILayout.TextField(t.title);
                t.starCost = Mathf.Max(0, EditorGUILayout.IntField(t.starCost, GUILayout.Width(34)));
                GUILayout.Label("⭐", GUILayout.Width(18));
                if (!compact) GUILayout.Label(ShortEffectNames[(int)t.effect], EditorStyles.miniLabel, GUILayout.Width(70));
                if (GUILayout.Button("▲", GUILayout.Width(22))) { moveFrom = i; move = -1; }
                if (GUILayout.Button("▼", GUILayout.Width(22))) { moveFrom = i; move = 1; }
                if (GUILayout.Button("✕", GUILayout.Width(22))) remove = i;
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Etapa")) { area.tasks.Add(new AreaTask { id = "etapa" + (area.tasks.Count + 1), title = "Nova melhoria" }); activeTask = area.tasks.Count - 1; activeObj = 0; }
            if (remove >= 0) { area.tasks.RemoveAt(remove); activeTask = Mathf.Max(0, remove - 1); activeObj = 0; }
            if (moveFrom >= 0 && moveFrom + move >= 0 && moveFrom + move < area.tasks.Count)
            {
                (area.tasks[moveFrom], area.tasks[moveFrom + move]) = (area.tasks[moveFrom + move], area.tasks[moveFrom]);
                activeTask = moveFrom + move; // the selection follows the upgrade
            }
        }

        // The selected upgrade: its objects to place, then the files it ends up with.
        void DrawSelectedTask(AreaDefinition area)
        {
            if (activeTask < 0 || activeTask >= area.tasks.Count) return;
            var task = area.tasks[activeTask];
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Etapa {activeTask + 1}: {task.title}", EditorStyles.boldLabel);
            DrawPlacer(area, task);
            showFiles = EditorGUILayout.Foldout(showFiles, "Arquivos da etapa (id, camada, ícone) — preenchidos ao gerar a camada", true);
            if (showFiles)
            {
                task.id = EditorGUILayout.TextField("Id (nome dos arquivos)", task.id);
                task.layer = (Sprite)EditorGUILayout.ObjectField("Camada", task.layer, typeof(Sprite), false);
                task.icon = (Sprite)EditorGUILayout.ObjectField("Ícone", task.icon, typeof(Sprite), false);
            }
            EditorGUILayout.EndVertical();
        }

        // The objects of one upgrade: add (a PNG, or a whole sheet cut into pieces), place by dragging in the preview or with
        // the numbers, then bake into the layer + icon. Anything that touches files or the list runs after the GUI pass.
        void DrawPlacer(AreaDefinition area, AreaTask task)
        {
            var folder = placementsFolder;
            if (folder == null)
            {
                EditorGUILayout.HelpBox("Escolha o fundo da área (dentro de Assets/RoyalAves/Art/) para poder posicionar.", MessageType.Info);
                return;
            }
            var placement = placements.Get(task.id);
            if (activeObj >= placement.objects.Count) activeObj = Mathf.Max(0, placement.objects.Count - 1);

            if (placement.objects.Count == 0)
            {
                if (task.layer != null)
                {
                    // A layer that is already baked (from the old montar.py, or an earlier "Gerar camada") has no objects to
                    // move: turn it back into objects standing exactly where they stand now.
                    EditorGUILayout.HelpBox("Esta etapa já tem uma camada pronta, mas não tem objetos para mover. Converta a camada em objetos (ficam onde estão agora) para poder arrastar e redimensionar.", MessageType.Info);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Camada atual → objetos separados")) Later(() => ExtractLayer(task, placement, folder, true));
                    if (GUILayout.Button("Camada atual → 1 objeto só")) Later(() => ExtractLayer(task, placement, folder, false));
                    EditorGUILayout.EndHorizontal();
                }
                else
                    EditorGUILayout.HelpBox("Sem objetos ainda: adicione o PNG do móvel (ou uma folha com vários) e arraste na prévia.", MessageType.None);
            }

            EditorGUI.BeginChangeCheck();
            int remove = -1;
            for (var k = 0; k < placement.objects.Count; k++)
            {
                var o = placement.objects[k];
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(o.source ?? "");
                var current = k == activeObj;
                // Two lines per object, so it fits any column width: who it is, then where it stands.
                EditorGUILayout.BeginVertical(current ? (GUIStyle)"selectionRect" : GUIStyle.none);
                EditorGUILayout.BeginHorizontal();
                var box = GUILayoutUtility.GetRect(28, 28, GUILayout.Width(28));
                if (tex != null) GUI.DrawTexture(box, tex, ScaleMode.ScaleToFit, true); else GUI.Box(box, "?");
                if (GUILayout.Toggle(current, "Mover", "Button", GUILayout.Width(54), GUILayout.Height(24))) activeObj = k;
                GUILayout.Label(new GUIContent(Path.GetFileNameWithoutExtension(o.source ?? "(sem arquivo)"), o.source), EditorStyles.miniLabel, GUILayout.MinWidth(20));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("✕", GUILayout.Width(22))) remove = k;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                var oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 34f;
                o.cx = Mathf.Clamp01(EditorGUILayout.FloatField("X", (float)System.Math.Round(o.cx, 3)));
                o.cy = Mathf.Clamp01(EditorGUILayout.FloatField("Y", (float)System.Math.Round(o.cy, 3)));
                o.w = Mathf.Clamp(EditorGUILayout.FloatField("Larg.", (float)System.Math.Round(o.w, 3)), 0.01f, 1.5f);
                EditorGUIUtility.labelWidth = oldLabelWidth;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            var dirty = EditorGUI.EndChangeCheck();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ PNG do objeto…")) Later(() => AddPngs(folder, placement, false));
            if (GUILayout.Button("+ Folha com vários objetos…")) Later(() => AddPngs(folder, placement, true));
            EditorGUILayout.EndHorizontal();
            using (new EditorGUI.DisabledScope(placement.objects.Count == 0))
            {
                GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
                if (GUILayout.Button("Gerar camada e ícone", GUILayout.Height(26))) Later(() => BakeTask(area, task, placement));
                GUI.backgroundColor = Color.white;
            }

            // One line (a Texture2D field in a layout call would draw a big square thumbnail).
            var dropRect = EditorGUILayout.GetControlRect();
            var dropped = (Texture2D)EditorGUI.ObjectField(dropRect, "…ou arraste um PNG do projeto", null, typeof(Texture2D), false);
            if (dropped != null) { var path = AssetDatabase.GetAssetPath(dropped); Later(() => AddObject(placement, path)); }

            if (remove >= 0) Later(() => { placement.objects.RemoveAt(remove); SavePlacements(); });
            if (dirty)
            {
                SavePlacements();
                if (effectTask >= 0) Later(StopEffect); // the open test shows the old layer; back to the live objects
            }
        }

        static void Later(System.Action action) => EditorApplication.delayCall += () => { action(); repaintCb?.Invoke(); };

        // Turns the baked layer of an upgrade back into movable objects, each exactly where it is drawn now.
        static void ExtractLayer(AreaTask task, TaskPlacement placement, string folder, bool split)
        {
            if (task.layer == null) return;
            var disk = RoyalAvesAreaPlacer.ToDisk(AssetDatabase.GetAssetPath(task.layer));
            var found = new List<PlacedObject>();
            if (split) RoyalAvesAreaPlacer.SplitSheet(disk, folder, found);
            else
            {
                var whole = RoyalAvesAreaPlacer.WholeObject(disk, folder, task.id);
                if (whole != null) found.Add(whole);
            }
            AssetDatabase.Refresh();
            if (found.Count == 0)
            {
                EditorUtility.DisplayDialog("Camada → objetos", "Não achei nada desenhado nesta camada.", "OK");
                return;
            }
            placement.objects.AddRange(found);
            activeObj = 0;
            SavePlacements();
            if (split && found.Count > 12)
                EditorUtility.DisplayDialog("Camada → objetos", $"A camada virou {found.Count} objetos (cada pedaço solto vira um). Se ficou picado demais, apague com ✕ e use \"Camada atual → 1 objeto só\".", "OK");
        }

        static void AddObject(TaskPlacement placement, string assetPath)
        {
            placement.objects.Add(new PlacedObject { source = assetPath });
            activeObj = placement.objects.Count - 1;
            SavePlacements();
        }

        static void AddPngs(string folder, TaskPlacement placement, bool split)
        {
            var file = EditorUtility.OpenFilePanel(split ? "Folha com vários objetos (fundo transparente)" : "PNG do objeto (fundo transparente)", "", "png");
            if (string.IsNullOrEmpty(file)) return;
            if (!split) { AddObject(placement, RoyalAvesAreaPlacer.ImportPng(file, folder)); return; }

            var pieces = RoyalAvesAreaPlacer.SplitSheet(file, folder);
            foreach (var p in pieces) AddObject(placement, p);
            EditorUtility.DisplayDialog("Separar folha",
                pieces.Count == 0 ? "Não achei nenhum objeto. A folha precisa ter fundo transparente."
                : $"{pieces.Count} objeto(s) separados em {folder}/objetos e adicionados, da esquerda para a direita.\nApague com ✕ os que forem lascas ou duplicados." +
                  (pieces.Count == 1 ? "\n\nSó achei 1: se a folha tinha vários, confira se o fundo é mesmo transparente." : ""), "OK");
        }

        static void BakeTask(AreaDefinition area, AreaTask task, TaskPlacement placement)
        {
            var error = RoyalAvesAreaPlacer.Bake(area, task, placement, out var layer, out var icon);
            if (error != null) { EditorUtility.DisplayDialog("Gerar camada", error, "OK"); return; }
            Undo.RecordObject(config, "Gerar camada");
            task.layer = layer;
            task.icon = icon;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Áreas] camada \"{task.id}\" gerada ({layer.rect.width}x{layer.rect.height}) com {placement.objects.Count} objeto(s).");
        }

        void DrawReward(AreaDefinition area)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Baú ao terminar a área", EditorStyles.boldLabel);
            area.reward.coins = Mathf.Max(0, EditorGUILayout.IntField("Moedas", area.reward.coins));
            int remove = -1;
            for (var i = 0; i < area.reward.boosters.Count; i++)
            {
                var b = area.reward.boosters[i];
                EditorGUILayout.BeginHorizontal();
                b.type = (BoostType)EditorGUILayout.EnumPopup("Reforço", b.type);
                b.count = Mathf.Max(1, EditorGUILayout.IntField(b.count, GUILayout.Width(36)));
                if (GUILayout.Button("✕", GUILayout.Width(22))) remove = i;
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Reforço")) area.reward.boosters.Add(new BoosterReward());
            if (remove >= 0) area.reward.boosters.RemoveAt(remove);
        }

        // Problems that would only show up in the game, as silent blank spots.
        void DrawChecks(AreaDefinition area)
        {
            var problems = new List<string>();
            if (string.IsNullOrWhiteSpace(area.id)) problems.Add("A área não tem id.");
            if (config.areas.Count(a => a.id == area.id) > 1) problems.Add("Há outra área com o mesmo id.");
            if (area.background == null) problems.Add("Falta o fundo.");
            foreach (var t in area.tasks)
            {
                if (t.layer == null) problems.Add($"\"{t.title}\": sem imagem de camada.");
                else if (area.background != null && t.layer.rect.size != area.background.rect.size)
                    problems.Add($"\"{t.title}\": a camada tem {t.layer.rect.width}x{t.layer.rect.height} e o fundo {area.background.rect.width}x{area.background.rect.height} (precisam ser iguais).");
                if (t.icon == null) problems.Add($"\"{t.title}\": sem ícone.");
            }
            if (area.tasks.Select(t => t.id).Distinct().Count() != area.tasks.Count) problems.Add("Há etapas com o mesmo id.");
            EditorGUILayout.Space();
            if (problems.Count == 0) EditorGUILayout.HelpBox("Tudo certo com esta área.", MessageType.Info);
            else EditorGUILayout.HelpBox(string.Join("\n", problems), MessageType.Warning);
        }

        // ---- import ------------------------------------------------------------------------------------------------

        static void ImportFolder(System.Action repaint)
        {
            var abs = EditorUtility.OpenFolderPanel("Pasta com o fundo e as camadas", Path.Combine(Application.dataPath, "RoyalAves/Art/Backgrounds"), "");
            if (string.IsNullOrEmpty(abs)) return;
            var data = Application.dataPath.Replace('\\', '/');
            var norm = abs.Replace('\\', '/');
            var folder = norm.StartsWith(data) ? "Assets" + norm.Substring(data.Length) : "";
            if (!folder.StartsWith(ArtRoot))
            {
                EditorUtility.DisplayDialog("Importar pasta", "A pasta precisa estar dentro de " + ArtRoot + ", onde as imagens viram sprites sozinhas. Copie as imagens para lá antes.", "OK");
                return;
            }
            AssetDatabase.Refresh();

            var sprites = Directory.GetFiles(abs, "*.png").Concat(Directory.GetFiles(abs, "*.jpg"))
                .Select(f => folder + "/" + Path.GetFileName(f)).OrderBy(p => p, System.StringComparer.OrdinalIgnoreCase)
                .Select(p => (path: p, name: Path.GetFileNameWithoutExtension(p), sprite: AssetDatabase.LoadAssetAtPath<Sprite>(p)))
                .Where(x => x.sprite != null).ToList();

            var bg = sprites.FirstOrDefault(x => x.name.ToLowerInvariant().Contains("bg") || x.name.ToLowerInvariant().Contains("fundo"));
            if (bg.sprite == null)
            {
                EditorUtility.DisplayDialog("Importar pasta", "Não achei o fundo: nomeie o arquivo com \"bg\" ou \"fundo\" (ex.: bg-sem-upgrades.png).", "OK");
                return;
            }
            var icons = sprites.Where(x => x.name.StartsWith("icone-", System.StringComparison.OrdinalIgnoreCase)).ToList();
            var layers = sprites.Where(x => x.path != bg.path && !icons.Contains(x)).ToList();

            var id = Path.GetFileName(abs).ToLowerInvariant();
            Mutate("Importar área", repaint, () =>
            {
                var area = config.areas.FirstOrDefault(a => a.id == id);
                if (area == null) { area = new AreaDefinition { id = id, title = Humanize(id) }; config.areas.Add(area); }
                area.background = bg.sprite;
                var tasks = new List<AreaTask>();
                foreach (var l in layers)
                {
                    var tid = StripOrder(l.name);
                    var task = area.tasks.FirstOrDefault(t => t.id == tid) ?? new AreaTask { id = tid, title = Humanize(tid), starCost = 1 };
                    task.layer = l.sprite;
                    var icon = icons.FirstOrDefault(i => i.name.Substring(6) == l.name);
                    if (icon.sprite != null) task.icon = icon.sprite;
                    tasks.Add(task);
                }
                // Known upgrades keep the order they already had (the file-name order only places new ones, last).
                var before = area.tasks;
                area.tasks = tasks.OrderBy(t => { var at = before.FindIndex(o => o.id == t.id); return at < 0 ? int.MaxValue : at; }).ToList();
                areaIndex = config.areas.IndexOf(area);
                activeTask = 0;
            });
            Debug.Log($"[Áreas] \"{id}\" importada: fundo {bg.name}, {layers.Count} camada(s), {icons.Count} ícone(s).");
        }

        // "03-poste-e-lampadas" -> "poste-e-lampadas"
        static string StripOrder(string name)
        {
            var i = 0;
            while (i < name.Length && char.IsDigit(name[i])) i++;
            i = i > 0 && i < name.Length && (name[i] == '-' || name[i] == '_') ? i + 1 : 0;
            return name.Substring(i).ToLowerInvariant();
        }

        static string Humanize(string id)
        {
            var s = id.Replace('-', ' ').Replace('_', ' ');
            return s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        static void Mutate(string undoName, System.Action repaint, System.Action change)
        {
            Undo.RecordObject(config, undoName);
            change();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            repaint?.Invoke();
        }
    }
}
