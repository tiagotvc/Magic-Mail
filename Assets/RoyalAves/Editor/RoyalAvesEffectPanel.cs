// Effect editor of Correio Mágico: one place to see and tweak every effect of the game — particle prefabs, animated
// prefabs, the materials/shaders they use and the ScriptableObjects that feed code-built effects.
// It lives in the "Effects" tab of the Sweet Sugar Level editor (LevelMakerEditor calls RoyalAvesEffectPanel.DrawTab) and
// also opens alone from the menu Royal Aves > Editor de efeitos.
//
//  - Partículas / Animados: pick a prefab, watch it in a live preview (play, scrub, step, slow motion, background colour,
//    zoom), edit its ParticleSystems and materials with Unity's own inspectors right under the preview, pick which
//    animation clip of its Animator plays with it, then "Salvar no prefab" (or "Reverter").
//  - Materiais: every material with its properties and a preview; "Ver shader" selects the shader file.
//  - Dados: ScriptableObjects of the game's code (namespace RoyalAves*), e.g. the data a code-built effect loads.
// The catalogue is built by folder (see PrefabRoots / MaterialRoots / DataRoots), so effects added later by anyone show up
// after "Reescanear" without touching this file.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RoyalAves.EditorTools
{
    public class RoyalAvesEffectsEditor : EditorWindow
    {
        Vector2 scroll;

        [MenuItem("Royal Aves/Editor de efeitos")]
        static void Open() => GetWindow<RoyalAvesEffectsEditor>("Efeitos").minSize = new Vector2(420, 480);

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            RoyalAvesEffectPanel.DrawTab(position.width - 24, Repaint);
            EditorGUILayout.EndScrollView();
        }

        void OnDisable() => RoyalAvesEffectPanel.Cleanup();
    }

    public static class RoyalAvesEffectPanel
    {
        enum Category { PowerUps, Particulas, Animados, Materiais, Dados }
        static readonly string[] CategoryNames = { "Power-ups", "Partículas", "Animados", "Materiais", "Dados" };

        // ---- power-ups: what plays together --------------------------------------------------------------------------
        // A power-up is the item that carries the animation plus the particle prefabs the game's code spawns when it fires
        // (they are pooled by name, so nothing in the item prefab says which ones). The composition is kept here, editable
        // in the tab, in a small JSON file next to this script.
        const string PowerUpFilePath = "Assets/RoyalAves/Editor/PowerUpPreviews.json";

        [Serializable] class PowerUpEffect { public string path; public float delay; }
        [Serializable] class PowerUpDef
        {
            public string name;
            public string itemPath;
            public string clip;          // name of the animation clip that plays with it ("" = pick one automatically)
            public List<PowerUpEffect> effects = new List<PowerUpEffect>();
        }
        [Serializable] class PowerUpFile { public List<PowerUpDef> powerUps = new List<PowerUpDef>(); }

        static PowerUpFile powerUpFile;
        const string PowerUpPrefix = "powerup:";

        // Where effects live. Add a folder here if a new kind of effect is kept somewhere else.
        static readonly string[] PrefabRoots = { "Assets/RoyalAves", "Assets/SweetSugar", "Assets/FXIFIED", "Assets/Lana Studio" };
        static readonly string[] MaterialRoots = { "Assets/RoyalAves", "Assets/SweetSugar/Materials", "Assets/FXIFIED", "Assets/Lana Studio" };
        static readonly string[] DataRoots = { "Assets/RoyalAves" };

        const int MaxRows = 300;
        const float PreviewHeight = 300f;

        class Entry { public string path, name, folder, note; }

        static readonly Dictionary<Category, List<Entry>> catalog = new Dictionary<Category, List<Entry>>();
        static bool scanned;
        static Category category;
        static string search = "";
        static string selectedPath;
        static Vector2 listScroll;
        static Action repaintCb;
        static double lastDrawn;

        // ---- the loaded prefab ---------------------------------------------------------------------------------------
        static string loadedPath;
        static PreviewRenderUtility pru;
        static GameObject instance;
        static ParticleSystem[] systems = new ParticleSystem[0];
        static (bool auto, uint seed)[] originalSeeds = new (bool, uint)[0];
        static Animator animator;
        static AnimationClip[] clips = new AnimationClip[0];
        static int clipIndex = -1;               // -1 = no animation clip
        static float length = 2f;
        static bool dirty;
        static readonly HashSet<Object> openSystems = new HashSet<Object>();
        static bool showMaterials = true;
        static readonly Dictionary<Object, UnityEditor.Editor> editors = new Dictionary<Object, UnityEditor.Editor>();

        // ---- the player ---------------------------------------------------------------------------------------------
        static float time;
        static bool playing = true, loop = true;
        static double lastTick;
        static int speedIndex = 4;
        static readonly float[] SpeedValues = { 0.05f, 0.1f, 0.25f, 0.5f, 1f };
        static readonly string[] SpeedLabels = { "0,05x", "0,1x", "0,25x", "0,5x", "1x" };
        const float FrameStep = 1f / 60f;
        static float zoom = 3f;
        static Vector2 pan;
        static int backgroundIndex;
        static readonly string[] BackgroundNames = { "Escuro", "Cinza", "Claro", "Verde (jogo)" };
        static readonly Color[] BackgroundColors =
        {
            new Color(0.10f, 0.10f, 0.12f), new Color(0.45f, 0.45f, 0.48f), new Color(0.85f, 0.85f, 0.88f), new Color(0.36f, 0.62f, 0.2f),
        };

        static RoyalAvesEffectPanel()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
        }

        // Call from inside an EditorWindow.OnGUI. `width` is the usable width, `repaint` redraws that window.
        public static void DrawTab(float width, Action repaint)
        {
            // Docked very wide, one stretched line per field is hard to read: keep a readable width, centred.
            var usable = Mathf.Min(width, 760f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical(GUILayout.Width(usable));
            DrawContent(usable, repaint);
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        static void DrawContent(float width, Action repaint)
        {
            repaintCb = repaint;
            lastDrawn = EditorApplication.timeSinceStartup;
            if (!scanned) Scan();

            EditorGUILayout.BeginHorizontal();
            var picked = (Category)GUILayout.Toolbar((int)category, CategoryNames);
            if (picked != category) { category = picked; selectedPath = null; Cleanup(); }
            if (GUILayout.Button(new GUIContent("↻", "Reescanear as pastas"), GUILayout.Width(28))) { Cleanup(); Scan(); }
            EditorGUILayout.EndHorizontal();

            search = EditorGUILayout.TextField("Buscar", search);
            DrawList();
            if (category == Category.PowerUps && GUILayout.Button("+ Novo power-up"))
                Later(() =>
                {
                    LoadPowerUps();
                    powerUpFile.powerUps.Add(new PowerUpDef { name = "Novo power-up" });
                    SavePowerUps();
                    selectedPath = PowerUpPrefix + (powerUpFile.powerUps.Count - 1);
                    Cleanup();
                });
            if (string.IsNullOrEmpty(selectedPath))
            {
                EditorGUILayout.HelpBox("Escolha um efeito na lista para ver e editar.", MessageType.Info);
                return;
            }
            EditorGUILayout.Space();
            switch (category)
            {
                case Category.PowerUps:
                case Category.Particulas:
                case Category.Animados: DrawPrefabEffect(width); break;
                case Category.Materiais: DrawMaterial(); break;
                default: DrawData(); break;
            }
        }

        // ---- catalogue ------------------------------------------------------------------------------------------------

        static void Scan()
        {
            scanned = true;
            foreach (Category c in Enum.GetValues(typeof(Category))) catalog[c] = new List<Entry>();
            LoadPowerUps();
            RebuildPowerUpEntries();

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", PrefabRoots))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null || go.GetComponentInChildren<Canvas>(true) != null) continue; // windows and screens are not effects
                var particleCount = go.GetComponentsInChildren<ParticleSystem>(true).Length;
                var animated = go.GetComponentInChildren<Animator>(true) != null;
                if (particleCount > 0)
                    catalog[Category.Particulas].Add(Make(path, $"{particleCount} sistema(s) de partículas{(animated ? " + animação" : "")}"));
                else if (animated)
                    catalog[Category.Animados].Add(Make(path, "animação"));
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Material", MaterialRoots))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null) catalog[Category.Materiais].Add(Make(path, mat.shader != null ? mat.shader.name : "sem shader"));
            }
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", DataRoots))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                var ns = so != null ? so.GetType().Namespace : null;
                if (ns != null && ns.StartsWith("RoyalAves")) catalog[Category.Dados].Add(Make(path, so.GetType().Name));
            }
            foreach (var list in catalog.Values) list.Sort((a, b) => string.Compare(a.folder + "/" + a.name, b.folder + "/" + b.name, StringComparison.OrdinalIgnoreCase));
        }

        // ---- power-up compositions: file, defaults, list ---------------------------------------------------------------

        static void LoadPowerUps()
        {
            if (powerUpFile != null) return;
            var disk = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, PowerUpFilePath);
            try
            {
                if (System.IO.File.Exists(disk)) powerUpFile = JsonUtility.FromJson<PowerUpFile>(System.IO.File.ReadAllText(disk));
            }
            catch (Exception e) { Debug.LogWarning("[Efeitos] PowerUpPreviews.json ilegível: " + e.Message); }
            if (powerUpFile == null || powerUpFile.powerUps == null)
            {
                powerUpFile = new PowerUpFile { powerUps = DefaultPowerUps() };
                SavePowerUps();
            }
        }

        static void SavePowerUps()
        {
            var disk = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, PowerUpFilePath);
            System.IO.File.WriteAllText(disk, JsonUtility.ToJson(powerUpFile, true));
        }

        // A first guess for the power-ups of the Sweet Sugar template (item prefab + the effect prefabs its code spawns by
        // name). Only the ones whose prefab exists in the project are created; adjust them in the tab.
        static List<PowerUpDef> DefaultPowerUps()
        {
            var guess = new (string title, string item, (string effect, float delay)[] effects)[]
            {
                ("Foguete horizontal (limpa a linha)", "HORIZONTAL_STRIPED", new[] { ("StripesEffect", 0f) }),
                ("Foguete vertical (limpa a coluna)", "VERTICAL_STRIPED", new[] { ("StripesEffect", 0f) }),
                ("Hélice", "MARMALADE", new[] { ("FireworkSplashSpiral", 0f) }),
                ("Bomba colorida", "MULTICOLOR", new[] { ("Multicolor_firework", 0f) }),
                ("Pacote (explosão em área)", "PACKAGE", new[] { ("PackageExpl", 0f), ("package_second_expl", 0.3f) }),
                ("Bomba-relógio", "TimeBomb", new[] { ("Expl", 0f) }),
            };
            var result = new List<PowerUpDef>();
            foreach (var g in guess)
            {
                var item = FindPrefab(g.item);
                if (item == null) continue;
                var def = new PowerUpDef { name = g.title, itemPath = item };
                foreach (var e in g.effects)
                {
                    var path = FindPrefab(e.effect);
                    if (path != null) def.effects.Add(new PowerUpEffect { path = path, delay = e.delay });
                }
                result.Add(def);
            }
            return result;
        }

        static string FindPrefab(string name)
        {
            foreach (var guid in AssetDatabase.FindAssets(name + " t:Prefab", new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == name) return path;
            }
            return null;
        }

        static void RebuildPowerUpEntries()
        {
            var list = catalog[Category.PowerUps];
            list.Clear();
            for (var i = 0; i < powerUpFile.powerUps.Count; i++)
            {
                var d = powerUpFile.powerUps[i];
                list.Add(new Entry
                {
                    path = PowerUpPrefix + i,
                    name = d.name,
                    folder = "",
                    note = $"{(string.IsNullOrEmpty(d.itemPath) ? "sem item" : System.IO.Path.GetFileNameWithoutExtension(d.itemPath))} + {d.effects.Count} efeito(s)",
                });
            }
        }

        static PowerUpDef SelectedPowerUp()
        {
            if (selectedPath == null || !selectedPath.StartsWith(PowerUpPrefix)) return null;
            return int.TryParse(selectedPath.Substring(PowerUpPrefix.Length), out var i) && powerUpFile != null && i >= 0 && i < powerUpFile.powerUps.Count
                ? powerUpFile.powerUps[i] : null;
        }

        static Entry Make(string path, string note) => new Entry
        {
            path = path,
            name = System.IO.Path.GetFileNameWithoutExtension(path),
            folder = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/').Replace("Assets/", ""),
            note = note,
        };

        static void DrawList()
        {
            var all = catalog[category];
            var q = search.Trim().ToLowerInvariant();
            var shown = all.Where(e => q.Length == 0 || e.name.ToLowerInvariant().Contains(q) || e.folder.ToLowerInvariant().Contains(q)).ToList();
            listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.Height(170));
            var rowStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, fontSize = 11 };
            foreach (var e in shown.Take(MaxRows))
            {
                var isSel = e.path == selectedPath;
                GUI.backgroundColor = isSel ? new Color(0.6f, 0.85f, 1f) : Color.white;
                if (GUILayout.Button(new GUIContent($"{e.name}   ·   {e.note}{(string.IsNullOrEmpty(e.folder) ? "" : "   ·   " + e.folder)}", e.path), rowStyle) && !isSel)
                {
                    selectedPath = e.path;
                    Cleanup(); // the preview of the old one goes away; the new one loads on this frame's draw
                }
                GUI.backgroundColor = Color.white;
            }
            if (shown.Count > MaxRows) GUILayout.Label($"… e mais {shown.Count - MaxRows}. Refine a busca.", EditorStyles.miniLabel);
            if (shown.Count == 0) GUILayout.Label(all.Count == 0 ? "Nada encontrado nas pastas de efeitos." : "Nada com essa busca.", EditorStyles.miniLabel);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.LabelField($"{shown.Count} de {all.Count}", EditorStyles.miniLabel);
        }

        // ---- prefab effects: preview + editors ------------------------------------------------------------------------

        static void DrawPrefabEffect(float width)
        {
            var powerUp = SelectedPowerUp();
            if (powerUp != null) DrawComposer(powerUp);
            if (loadedPath != selectedPath && !(powerUp != null ? LoadPowerUp(powerUp) : Load(selectedPath)))
            {
                EditorGUILayout.HelpBox(powerUp != null
                    ? "Escolha o prefab do item (a animação) na composição acima; os efeitos de partículas são opcionais."
                    : "Não consegui abrir este prefab na prévia.", MessageType.Info);
                return;
            }

            // View controls
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Fundo", GUILayout.Width(36));
            backgroundIndex = EditorGUILayout.Popup(backgroundIndex, BackgroundNames, GUILayout.Width(90));
            GUILayout.Label("Zoom", GUILayout.Width(34));
            zoom = EditorGUILayout.Slider(zoom, 0.3f, 20f);
            if (GUILayout.Button("Centralizar", GUILayout.Width(74))) { pan = Vector2.zero; zoom = 3f; }
            EditorGUILayout.EndHorizontal();

            var rect = GUILayoutUtility.GetRect(width, PreviewHeight, GUILayout.ExpandWidth(true));
            HandleViewInput(rect);
            DrawPreview(rect);
            DrawPlayer();

            if (loadedPath != null && instance != null)
            {
                DrawAnimationPicker();
                DrawSystems();
                DrawMaterials();
                DrawSaveBar();
            }
        }

        static bool Load(string path)
        {
            Cleanup();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return false;

            pru = new PreviewRenderUtility();
            var cam = pru.camera;
            cam.orthographic = true;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            instance = pru.InstantiatePrefabInScene(prefab);
            instance.transform.position = Vector3.zero;
            instance.SetActive(true);

            systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            // Scrubbing restarts the simulation every frame; with an automatic random seed the particles would reshuffle
            // each time. A fixed seed (only on this preview copy; Save puts the original back) keeps the scrub stable.
            originalSeeds = systems.Select(s => (s.useAutoRandomSeed, s.randomSeed)).ToArray();
            for (var i = 0; i < systems.Length; i++)
            {
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                systems[i].useAutoRandomSeed = false;
                systems[i].randomSeed = 1000u + (uint)i;
            }

            animator = instance.GetComponentInChildren<Animator>(true);
            clips = animator != null && animator.runtimeAnimatorController != null
                ? animator.runtimeAnimatorController.animationClips.Where(c => c != null).Distinct().ToArray()
                : new AnimationClip[0];
            clipIndex = clips.Length > 0 ? 0 : -1;

            length = ComputeLength();
            time = 0f;
            playing = true;
            dirty = false;
            loadedPath = path;
            primaryPath = path;
            openSystems.Clear();
            lastTick = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            return true;
        }

        // The extra effect prefabs of a power-up: they appear (all their particle systems start from zero) at their delay.
        class Companion
        {
            public GameObject go;
            public string path;
            public float delay;
            public ParticleSystem[] systems;
            public (bool auto, uint seed)[] seeds;
            public uint seedBase;
        }

        static readonly List<Companion> companions = new List<Companion>();
        static string primaryPath;

        // Loads the item of a power-up with its effect prefabs, and picks the animation clip that plays with it.
        static bool LoadPowerUp(PowerUpDef def)
        {
            if (string.IsNullOrEmpty(def.itemPath) || !Load(def.itemPath)) return false;
            foreach (var e in def.effects)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(e.path ?? "");
                if (prefab == null) continue;
                var go = pru.InstantiatePrefabInScene(prefab);
                go.transform.position = Vector3.zero;
                var ps = go.GetComponentsInChildren<ParticleSystem>(true);
                var seeds = ps.Select(s => (s.useAutoRandomSeed, s.randomSeed)).ToArray();
                var seedBase = 5000u + (uint)(companions.Count * 100);
                for (var i = 0; i < ps.Length; i++)
                {
                    ps[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps[i].useAutoRandomSeed = false;
                    ps[i].randomSeed = seedBase + (uint)i;
                }
                go.SetActive(false); // shown from its delay on
                companions.Add(new Companion { go = go, path = e.path, delay = Mathf.Max(0f, e.delay), systems = ps, seeds = seeds, seedBase = seedBase });
            }
            clipIndex = PreferredClip(def.clip);
            length = ComputeLength();
            loadedPath = selectedPath;
            return true;
        }

        // "-" = no animation; a name = that clip; empty = the one that looks like the activation (explode, fly, split…).
        static int PreferredClip(string wanted)
        {
            if (wanted == "-") return -1;
            if (!string.IsNullOrEmpty(wanted))
            {
                var named = Array.FindIndex(clips, c => c.name == wanted);
                if (named >= 0) return named;
            }
            var key = new[] { "expl", "activ", "ativ", "destroy", "boom", "split", "fly", "spin", "use" };
            var guess = Array.FindIndex(clips, c => key.Any(k => c.name.ToLowerInvariant().Contains(k)));
            return guess >= 0 ? guess : (clips.Length > 0 ? 0 : -1);
        }

        public static void Cleanup()
        {
            EditorApplication.update -= Tick;
            foreach (var e in editors.Values) if (e != null) Object.DestroyImmediate(e);
            editors.Clear();
            if (pru != null) { pru.Cleanup(); pru = null; }
            instance = null;
            companions.Clear();
            systems = new ParticleSystem[0];
            animator = null;
            clips = new AnimationClip[0];
            loadedPath = null;
        }

        // How long a set of particle systems lasts: the longest (delay + duration + lifetime).
        static float SystemsLength(ParticleSystem[] list)
        {
            var len = 0f;
            foreach (var s in list)
            {
                var m = s.main;
                var life = m.startLifetime;
                var lifetime = life.mode == ParticleSystemCurveMode.Constant ? life.constant
                    : life.mode == ParticleSystemCurveMode.TwoConstants ? life.constantMax : life.curveMultiplier;
                len = Mathf.Max(len, m.startDelayMultiplier + m.duration + lifetime);
            }
            return len;
        }

        // How long to play: the longest particle system or animation clip, effects counted from their own delay.
        static float ComputeLength()
        {
            var len = SystemsLength(systems);
            foreach (var c in companions) len = Mathf.Max(len, c.delay + SystemsLength(c.systems));
            if (clipIndex >= 0 && clipIndex < clips.Length) len = Mathf.Max(len, clips[clipIndex].length);
            return len < 0.2f ? 2f : len;
        }

        static void Tick()
        {
            var now = EditorApplication.timeSinceStartup;
            var dt = Mathf.Min(0.1f, (float)(now - lastTick));
            lastTick = now;
            if (now - lastDrawn > 1.0) { Cleanup(); return; } // the tab is not on screen: free the preview
            if (!playing || instance == null) return;
            time += dt * SpeedValues[speedIndex];
            if (time >= length)
            {
                if (loop) time %= length;
                else { time = length; playing = false; }
            }
            repaintCb?.Invoke();
        }

        static void Evaluate(float t)
        {
            foreach (var s in systems)
                if (s != null) s.Simulate(t, false, true, true);
            if (clipIndex >= 0 && clipIndex < clips.Length && instance != null)
                clips[clipIndex].SampleAnimation(instance, Mathf.Min(t, clips[clipIndex].length));
            foreach (var c in companions)
            {
                if (c.go == null) continue;
                var shown = t >= c.delay;
                if (c.go.activeSelf != shown) c.go.SetActive(shown);
                if (!shown) continue;
                foreach (var s in c.systems)
                    if (s != null) s.Simulate(t - c.delay, false, true, true);
            }
        }

        static void HandleViewInput(Rect rect)
        {
            var e = Event.current;
            if (!rect.Contains(e.mousePosition)) return;
            if (e.type == EventType.ScrollWheel)
            {
                zoom = Mathf.Clamp(zoom * (1f + e.delta.y * 0.05f), 0.3f, 20f);
                e.Use(); repaintCb?.Invoke();
            }
            else if (e.type == EventType.MouseDrag && (e.button == 0 || e.button == 2))
            {
                pan -= new Vector2(e.delta.x, -e.delta.y) * (zoom * 2f / rect.height);
                e.Use(); repaintCb?.Invoke();
            }
        }

        static void DrawPreview(Rect rect)
        {
            if (Event.current.type != EventType.Repaint || pru == null || instance == null) return;
            Evaluate(time);
            pru.BeginPreview(rect, GUIStyle.none);
            var cam = pru.camera;
            cam.backgroundColor = BackgroundColors[backgroundIndex];
            cam.orthographicSize = zoom;
            cam.transform.position = new Vector3(pan.x, pan.y, -10f);
            cam.transform.rotation = Quaternion.identity;
            pru.Render(true, false);
            var tex = pru.EndPreview();
            GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, false);
        }

        static void DrawPlayer()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("⏮", GUILayout.Width(30))) { time = 0f; playing = false; }
            if (GUILayout.Button("◀|", GUILayout.Width(34))) { playing = false; time = Mathf.Max(0f, time - FrameStep); }
            if (GUILayout.Button(playing ? "⏸ Pausar" : "▶ Tocar", GUILayout.Width(80)))
            {
                if (!playing && !loop && time >= length) time = 0f;
                playing = !playing;
                lastTick = EditorApplication.timeSinceStartup;
            }
            if (GUILayout.Button("|▶", GUILayout.Width(34))) { playing = false; time = Mathf.Min(length, time + FrameStep); }
            loop = GUILayout.Toggle(loop, "Repetir", GUILayout.Width(62));
            GUILayout.Label("Velocidade", GUILayout.Width(65));
            speedIndex = EditorGUILayout.Popup(speedIndex, SpeedLabels, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            var v = EditorGUILayout.Slider($"{time:0.000}s", time, 0f, length);
            if (EditorGUI.EndChangeCheck()) { time = v; playing = false; }
            EditorGUILayout.LabelField("Arraste a prévia para mover · roda do mouse = zoom · 1 passo = 1/60 s", EditorStyles.miniLabel);
        }

        static void DrawAnimationPicker()
        {
            if (clips.Length == 0) return;
            var names = new[] { "(sem animação)" }.Concat(clips.Select(c => $"{c.name}  ({c.length:0.00}s)")).ToArray();
            var picked = EditorGUILayout.Popup("Animação junto", clipIndex + 1, names) - 1;
            if (picked != clipIndex)
            {
                clipIndex = picked;
                length = ComputeLength();
                var def = SelectedPowerUp();
                if (def != null) { def.clip = clipIndex >= 0 ? clips[clipIndex].name : "-"; SavePowerUps(); }
            }
        }

        // The particle systems of the item and, for a power-up, of each effect prefab.
        static void DrawSystems()
        {
            DrawSystemList(systems, instance, "Partículas do item");
            foreach (var c in companions)
                DrawSystemList(c.systems, c.go, $"Efeito {System.IO.Path.GetFileNameWithoutExtension(c.path)} (entra em {c.delay:0.##}s)");
        }

        static void DrawSystemList(ParticleSystem[] list, GameObject root, string title)
        {
            if (list.Length == 0 || root == null) return;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"{title} ({list.Length})", EditorStyles.boldLabel);
            if (root.GetComponentsInChildren<Component>(true).Any(c => c != null && c.GetType().Name == "UIParticle"))
                EditorGUILayout.HelpBox("Este prefab usa UIParticle (partículas dentro de um Canvas). Aqui elas são mostradas fora do Canvas e podem ficar diferentes do jogo.", MessageType.Info);
            foreach (var s in list)
            {
                if (s == null) continue;
                Object id = s;
                var m = s.main;
                var summary = $"{RelativePath(s.transform, root.transform)}   ·   {(m.loop ? "loop" : m.duration.ToString("0.0#") + "s")}   ·   máx {m.maxParticles}";
                var open = EditorGUILayout.Foldout(openSystems.Contains(id), summary, true);
                if (open) openSystems.Add(id); else openSystems.Remove(id);
                if (!open) continue;
                EditorGUI.BeginChangeCheck();
                EditorGUI.indentLevel++;
                GetEditor(s).OnInspectorGUI();
                EditorGUI.indentLevel--;
                if (EditorGUI.EndChangeCheck()) { dirty = true; length = ComputeLength(); }
            }
        }

        static void DrawMaterials()
        {
            var materials = new[] { instance }.Concat(companions.Where(c => c.go != null).Select(c => c.go))
                .SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
                .SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().ToList();
            if (materials.Count == 0) return;
            EditorGUILayout.Space();
            showMaterials = EditorGUILayout.Foldout(showMaterials, $"Materiais e shaders usados ({materials.Count}) — editam o material do projeto", true);
            if (!showMaterials) return;
            foreach (var m in materials)
            {
                var open = openSystems.Contains(m);
                EditorGUILayout.BeginHorizontal();
                var now = EditorGUILayout.Foldout(open, $"{m.name}   ·   {(m.shader != null ? m.shader.name : "sem shader")}", true);
                if (GUILayout.Button("Ver shader", GUILayout.Width(80)) && m.shader != null) EditorGUIUtility.PingObject(m.shader);
                EditorGUILayout.EndHorizontal();
                if (now) openSystems.Add(m); else openSystems.Remove(m);
                if (!now) continue;
                EditorGUI.indentLevel++;
                DrawMaterialProperties(m);
                EditorGUI.indentLevel--;
            }
        }

        static void DrawSaveBar()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!dirty))
            {
                GUI.backgroundColor = dirty ? new Color(0.6f, 1f, 0.6f) : Color.white;
                if (GUILayout.Button(dirty ? "● Salvar no prefab" : "Salvar no prefab", GUILayout.Height(26))) Later(SaveToPrefab);
                GUI.backgroundColor = Color.white;
                if (GUILayout.Button("Reverter", GUILayout.Height(26), GUILayout.Width(80)))
                    Later(() => { var pu = SelectedPowerUp(); if (pu != null) LoadPowerUp(pu); else Load(selectedPath); });
            }
            if (GUILayout.Button("Selecionar no projeto", GUILayout.Height(26), GUILayout.Width(140)))
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(primaryPath));
            EditorGUILayout.EndHorizontal();
            if (dirty) EditorGUILayout.HelpBox("Há alterações só na prévia. \"Salvar no prefab\" grava no arquivo (os materiais já são gravados direto, use Ctrl+S).", MessageType.None);
        }

        // Writes the particle edits back to the prefab files (the item's and each effect's). Only the ParticleSystem and its
        // renderer are applied: the preview copy also carries the pose of the animation clip being sampled, which must
        // never end up in the prefab.
        static void SaveToPrefab()
        {
            if (instance == null || primaryPath == null) return;
            var saved = new List<string>();
            try
            {
                SaveSystems(systems, originalSeeds, 1000u, primaryPath, saved);
                foreach (var c in companions) SaveSystems(c.systems, c.seeds, c.seedBase, c.path, saved);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Efeitos] salvo: {string.Join(", ", saved)}");
                dirty = false;
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Salvar no prefab", "Não consegui salvar: " + e.Message, "OK");
            }
        }

        static void SaveSystems(ParticleSystem[] list, (bool auto, uint seed)[] seeds, uint seedBase, string path, List<string> saved)
        {
            // The preview's fixed seeds are only on this copy: put the originals back so they are not written to the prefab.
            for (var i = 0; i < list.Length; i++)
                if (list[i] != null) { list[i].useAutoRandomSeed = seeds[i].auto; list[i].randomSeed = seeds[i].seed; }
            foreach (var s in list)
            {
                if (s == null || !PrefabUtility.IsPartOfPrefabInstance(s)) continue;
                PrefabUtility.ApplyObjectOverride(s, path, InteractionMode.AutomatedAction);
                var renderer = s.GetComponent<ParticleSystemRenderer>();
                if (renderer != null && PrefabUtility.IsPartOfPrefabInstance(renderer))
                    PrefabUtility.ApplyObjectOverride(renderer, path, InteractionMode.AutomatedAction);
            }
            saved.Add(path);
            for (var i = 0; i < list.Length; i++)
                if (list[i] != null) { list[i].useAutoRandomSeed = false; list[i].randomSeed = seedBase + (uint)i; }
        }

        // ---- materials and data -----------------------------------------------------------------------------------------

        static void DrawMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(selectedPath);
            if (mat == null) { EditorGUILayout.HelpBox("Material não encontrado.", MessageType.Warning); return; }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(mat.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(mat.shader != null ? mat.shader.name : "sem shader", EditorStyles.miniLabel);
            if (GUILayout.Button("Ver shader", GUILayout.Width(80)) && mat.shader != null) EditorGUIUtility.PingObject(mat.shader);
            if (GUILayout.Button("Selecionar", GUILayout.Width(80))) EditorGUIUtility.PingObject(mat);
            EditorGUILayout.EndHorizontal();

            var editor = (MaterialEditor)GetEditor(mat);
            if (editor.HasPreviewGUI())
            {
                var r = GUILayoutUtility.GetRect(180, 180, GUILayout.ExpandWidth(false));
                editor.OnPreviewGUI(r, GUIStyle.none);
            }
            DrawMaterialProperties(mat);
            if (GUILayout.Button("Salvar (grava o asset)", GUILayout.Height(24))) AssetDatabase.SaveAssets();
        }

        static void DrawMaterialProperties(Material mat)
        {
            var editor = (MaterialEditor)GetEditor(mat);
            EditorGUI.BeginChangeCheck();
            editor.PropertiesGUI();
            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(mat);
        }

        static void DrawData()
        {
            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(selectedPath);
            if (so == null) { EditorGUILayout.HelpBox("Asset não encontrado.", MessageType.Warning); return; }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{so.name}  ({so.GetType().Name})", EditorStyles.boldLabel);
            if (GUILayout.Button("Selecionar", GUILayout.Width(80))) EditorGUIUtility.PingObject(so);
            EditorGUILayout.EndHorizontal();
            EditorGUI.BeginChangeCheck();
            GetEditor(so).OnInspectorGUI();
            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(so);
            if (GUILayout.Button("Salvar (grava o asset)", GUILayout.Height(24))) AssetDatabase.SaveAssets();
        }

        // ---- helpers ----------------------------------------------------------------------------------------------------

        static UnityEditor.Editor GetEditor(Object target)
        {
            var id = target;
            if (!editors.TryGetValue(id, out var editor) || editor == null)
            {
                editor = UnityEditor.Editor.CreateEditor(target);
                editors[id] = editor;
            }
            return editor;
        }

        static string RelativePath(Transform t, Transform root)
        {
            var parts = new List<string>();
            for (var x = t; x != null && x != root; x = x.parent) parts.Add(x.name);
            parts.Reverse();
            return parts.Count == 0 ? root.name.Replace("(Clone)", "") : string.Join("/", parts);
        }

        // The composition of a power-up: the item that carries the animation and the effect prefabs the game spawns when it
        // fires, each with the second it comes in. Saved as soon as it changes; the preview reloads with it.
        static void DrawComposer(PowerUpDef def)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Composição do power-up (o que toca junto)", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var name = EditorGUILayout.TextField("Nome", def.name);
            var nameChanged = EditorGUI.EndChangeCheck();
            if (nameChanged) def.name = name;

            var changed = false;
            var item = AssetDatabase.LoadAssetAtPath<GameObject>(def.itemPath ?? "");
            var newItem = (GameObject)EditorGUILayout.ObjectField("Item (com a animação)", item, typeof(GameObject), false);
            if (newItem != item)
            {
                def.itemPath = newItem != null ? AssetDatabase.GetAssetPath(newItem) : "";
                def.clip = "";
                changed = true;
            }

            var remove = -1;
            for (var i = 0; i < def.effects.Count; i++)
            {
                var e = def.effects[i];
                EditorGUILayout.BeginHorizontal();
                var fx = AssetDatabase.LoadAssetAtPath<GameObject>(e.path ?? "");
                var newFx = (GameObject)EditorGUILayout.ObjectField(fx, typeof(GameObject), false);
                if (newFx != fx) { e.path = newFx != null ? AssetDatabase.GetAssetPath(newFx) : ""; changed = true; }
                GUILayout.Label("entra em", GUILayout.Width(52));
                var delay = Mathf.Max(0f, EditorGUILayout.DelayedFloatField(e.delay, GUILayout.Width(48)));
                if (!Mathf.Approximately(delay, e.delay)) { e.delay = delay; changed = true; }
                GUILayout.Label("s", GUILayout.Width(10));
                if (GUILayout.Button("✕", GUILayout.Width(22))) remove = i;
                EditorGUILayout.EndHorizontal();
            }
            var added = (GameObject)EditorGUILayout.ObjectField("+ Efeito de partículas", null, typeof(GameObject), false);
            if (added != null) { def.effects.Add(new PowerUpEffect { path = AssetDatabase.GetAssetPath(added) }); changed = true; }

            if (GUILayout.Button("Excluir este power-up"))
                Later(() =>
                {
                    if (!EditorUtility.DisplayDialog("Excluir power-up", $"Excluir \"{def.name}\" da lista? (Os prefabs não são apagados.)", "Excluir", "Cancelar")) return;
                    powerUpFile.powerUps.Remove(def);
                    SavePowerUps();
                    RebuildPowerUpEntries();
                    selectedPath = null;
                    Cleanup();
                });
            EditorGUILayout.EndVertical();

            if (remove >= 0) { def.effects.RemoveAt(remove); changed = true; }
            if (nameChanged || changed)
            {
                SavePowerUps();
                RebuildPowerUpEntries();
                if (changed) Later(Cleanup); // the preview reloads with the new composition
            }
        }

        static void Later(Action action) => EditorApplication.delayCall += () => { action(); repaintCb?.Invoke(); };
    }
}
