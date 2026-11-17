using System.Linq;
using RoyalAves.Effects;
using RoyalAves.Meta;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.GUI.Boost;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    /// Menu Royal Aves/Introdução de power-up/Editor. Edita a tela que aparece ao escolher um power-up e a testa no jogo
    /// de verdade (tabuleiro, peças e barra reais): "Abrir o jogo" entra no Play numa fase, e os botões de teste escolhem
    /// o power-up como se você tivesse tocado na barra. O que você muda aqui é gravado no catálogo/prefab e vale no jogo.
    public sealed class BoosterIntroEditorWindow : EditorWindow
    {
        enum Tab { Teste, Conteudo, Efeito }
        static readonly string[] TabNames = { "Testar no jogo", "Conteúdo", "Efeito e layout" };

        Tab tab;
        Vector2 scroll;
        int testLevel = 1;
        SerializedObject catalogSo;
        BoosterIntroCatalog catalog;
        GameObject prefab;
        UnityEditor.Editor screenEditor;
        UnityEditor.Editor[] glowEditors = new UnityEditor.Editor[0];
        string[] glowNames = new string[0];

        [MenuItem("Royal Aves/Introdução de power-up/Editor")]
        static void Open()
        {
            var window = GetWindow<BoosterIntroEditorWindow>("Power-up intro");
            window.minSize = new Vector2(440, 520);
        }

        void OnEnable()
        {
            testLevel = Mathf.Max(1, EditorPrefs.GetInt("RoyalAves.BoosterIntro.TestLevel", 1));
            EditorApplication.update += Repaint;
        }

        void OnDisable()
        {
            EditorApplication.update -= Repaint;
            if (screenEditor != null) DestroyImmediate(screenEditor);
            foreach (var e in glowEditors) if (e != null) DestroyImmediate(e);
        }

        bool Load()
        {
            if (!System.IO.File.Exists(BoosterIntroBuilder.CatalogPath)) BoosterIntroBuilder.BuildCatalog();
            BoosterIntroBuilder.EnsureCurrent();
            if (!System.IO.File.Exists(BoosterIntroBuilder.PrefabPath)) BoosterIntroBuilder.BuildPrefab();
            if (catalog == null) { catalog = AssetDatabase.LoadAssetAtPath<BoosterIntroCatalog>(BoosterIntroBuilder.CatalogPath); catalogSo = null; }
            if (prefab == null) prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoosterIntroBuilder.PrefabPath);
            if (catalog == null || prefab == null) return false;
            if (catalogSo == null) catalogSo = new SerializedObject(catalog);
            return true;
        }

        void OnGUI()
        {
            if (!Load())
            {
                EditorGUILayout.HelpBox("Não achei o catálogo ou o prefab da tela. Use o menu Royal Aves/Introdução de power-up/Recriar prefab.", MessageType.Warning);
                return;
            }
            tab = (Tab)GUILayout.Toolbar((int)tab, TabNames, GUILayout.Height(26));
            scroll = EditorGUILayout.BeginScrollView(scroll);
            switch (tab)
            {
                case Tab.Teste: DrawTest(); break;
                case Tab.Conteudo: DrawContent(); break;
                default: DrawEffect(); break;
            }
            EditorGUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------------------------------ teste no jogo

        void DrawTest()
        {
            EditorGUILayout.HelpBox("Aqui você usa o power-up no jogo real. 1) Abra o jogo numa fase. 2) Escolha o power-up pelos botões abaixo " +
                                    "(ou toque na barra do jogo). 3) Toque no tabuleiro para usar. Mexa nas outras abas durante o Play e use " +
                                    "\"Reabrir a tela\" para ver o resultado na hora.", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            testLevel = Mathf.Max(1, EditorGUILayout.IntField("Fase de teste", testLevel));
            if (EditorGUI.EndChangeCheck()) EditorPrefs.SetInt("RoyalAves.BoosterIntro.TestLevel", testLevel);

            if (!Application.isPlaying)
            {
                if (GUILayout.Button("Abrir o jogo nesta fase (entra no Play)", GUILayout.Height(34)))
                    BoosterIntroTestDriver.Start(testLevel);
                return;
            }

            if (!BoosterIntroBuilder.IsCurrent)
                EditorGUILayout.HelpBox("O prefab da tela está na versão antiga (sem o escurecimento com buraco no tabuleiro). Clique em \"Parar o Play\": " +
                                        "ao voltar ao editor, abra esta janela de novo e ele é atualizado sozinho.", MessageType.Warning);
            var lm = LevelManager.THIS;
            EditorGUILayout.LabelField("Estado do jogo", lm != null ? lm.gameStatus.ToString() : "(sem LevelManager)");
            if (lm != null && lm.gameStatus == GameState.Map && GUILayout.Button("Começar a fase " + testLevel))
                BoosterIntroTestDriver.BeginLevel(testLevel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Escolher power-up", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
                foreach (var type in new[] { BoostType.Arrow, BoostType.Hammer, BoostType.Cannon })
                    if (GUILayout.Button(type.ToString(), GUILayout.Height(28))) BoosterIntroTestDriver.Select(type);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Cancelar seleção")) BoosterIntroTestDriver.Cancel();
                if (GUILayout.Button("Reabrir a tela")) BoosterIntroTestDriver.Reopen();
                if (GUILayout.Button("Dar 9 de cada")) BoosterIntroTestDriver.GiveBoosts();
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Efeito da flecha", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Disparar na linha do meio")) BoosterIntroTestDriver.FireArrow(-1);
                if (GUILayout.Button("Disparar na linha de cima")) BoosterIntroTestDriver.FireArrow(0);
            }
            if (GUILayout.Button("Selecionar prefab do efeito da flecha")) Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowFlightBuilder.PrefabPath);
            EditorGUILayout.LabelField("Tela aberta", BoosterIntroScreen.IsOpen ? "sim" : "não");
            if (GUILayout.Button("Parar o Play")) EditorApplication.isPlaying = false;
        }

        // ------------------------------------------------------------------------------------------ conteúdo

        void DrawContent()
        {
            EditorGUILayout.HelpBox("Uma entrada por power-up: figura, nome (imagem em português e inglês) e texto de como usar. " +
                                    "Salva sozinho. Se um power-up não tiver entrada (ou estiver desligado), a tela não aparece para ele.", MessageType.None);
            catalogSo.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(catalogSo.FindProperty("entries"), true);
            if (EditorGUI.EndChangeCheck())
            {
                catalogSo.ApplyModifiedProperties();
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssetIfDirty(catalog);
                BoosterIntroTestDriver.ReopenIfPlaying();
            }
            WarnIfSliced();
        }

        // As imagens do projeto às vezes vêm como "Multiple" fatiadas automaticamente: o sprite aparece cortado.
        void WarnIfSliced()
        {
            var bad = catalog.entries.Where(e => e != null)
                .SelectMany(e => new[] { e.icon, e.nameSpritePt, e.nameSpriteEn })
                .Where(s => s != null && AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(s)) is TextureImporter ti && ti.spriteImportMode == SpriteImportMode.Multiple)
                .Select(AssetDatabase.GetAssetPath).Distinct().ToList();
            if (bad.Count == 0) return;
            EditorGUILayout.HelpBox("Estas imagens estão como Sprite Mode Multiple e podem aparecer cortadas:\n" + string.Join("\n", bad), MessageType.Warning);
            if (GUILayout.Button("Corrigir (Single, sem compressão)"))
                foreach (var path in bad) BoosterIntroBuilder.LoadFull(path);
        }

        // ------------------------------------------------------------------------------------------ efeito e layout

        void EnsureEditors()
        {
            var screen = prefab.GetComponent<BoosterIntroScreen>();
            if (screenEditor == null || screenEditor.target != screen)
            {
                if (screenEditor != null) DestroyImmediate(screenEditor);
                screenEditor = UnityEditor.Editor.CreateEditor(screen);
            }
            var glows = prefab.GetComponentsInChildren<UIMagicGlow>(true);
            if (glowEditors.Length != glows.Length || glows.Where((g, i) => glowEditors[i] == null || glowEditors[i].target != g).Any())
            {
                foreach (var e in glowEditors) if (e != null) DestroyImmediate(e);
                glowEditors = glows.Select(g => UnityEditor.Editor.CreateEditor(g)).ToArray();
                glowNames = glows.Select(g => g.name).ToArray();
            }
        }

        void DrawEffect()
        {
            EnsureEditors();
            EditorGUILayout.HelpBox("Estes campos são do prefab BoosterIntroScreen: valem no jogo. Para mexer em posição e tamanho dos " +
                                    "elementos, abra o prefab (botão abaixo) e arraste no Scene view.", MessageType.None);
            if (GUILayout.Button("Abrir prefab para editar posições e tamanhos"))
            {
                Selection.activeObject = prefab;
                AssetDatabase.OpenAsset(prefab);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Tela (escurecimento, tempos, botão)", EditorStyles.boldLabel);
            screenEditor.OnInspectorGUI();
            for (var i = 0; i < glowEditors.Length; i++)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Efeito: " + glowNames[i], EditorStyles.boldLabel);
                glowEditors[i].OnInspectorGUI();
            }
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssetIfDirty(prefab);
                BoosterIntroTestDriver.ReopenIfPlaying();
            }
        }
    }

    /// Controla o jogo real durante o teste: abre a fase, dá power-ups e escolhe o power-up como o toque na barra faria.
    [InitializeOnLoad]
    static class BoosterIntroTestDriver
    {
        const string Flag = "RoyalAves.BoosterIntro.Test";
        const string LevelKey = "RoyalAves.BoosterIntro.TestLevelRun";
        const string GameScene = "Assets/SweetSugar/Scenes/game.unity";
        const string MainScene = "Assets/SweetSugar/Scenes/main.unity";
        static bool started;
        static double giveAt;

        static BoosterIntroTestDriver()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayState;
        }

        public static void Start(int level)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool(Flag, true);
            SessionState.SetInt(LevelKey, level);
            started = false;
            EditorSceneManager.playModeStartScene = null;   // o Play começa na cena do jogo, não na tela inicial
            EditorSceneManager.OpenScene(GameScene);
            EditorApplication.isPlaying = true;
        }

        static void OnPlayState(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            started = false;
            SessionState.SetBool(Flag, false);
            // Devolve a regra de "Play começa pela tela inicial" (menu Royal Aves), se estiver ligada.
            if (EditorPrefs.GetBool("RoyalAves.PlayModeStartScene", true))
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScene);
        }

        static void Update()
        {
            if (!Application.isPlaying || !SessionState.GetBool(Flag, false) || started) return;
            var lm = LevelManager.THIS;
            if (lm == null || AI.THIS == null || lm.gameStatus != GameState.Map) return;
            if (EditorApplication.timeSinceStartup < giveAt) return;
            if (giveAt == 0) { giveAt = EditorApplication.timeSinceStartup + 1.5; return; }   // espera o lobby montar
            started = true; giveAt = 0;
            BeginLevel(SessionState.GetInt(LevelKey, 1));
        }

        public static void BeginLevel(int level)
        {
            var lm = LevelManager.THIS;
            if (lm == null) return;
            Application.runInBackground = true;
            GiveBoosts();
            PlayerPrefs.SetInt("OpenLevel", level);
            lm.currentLevel = level;
            lm.BoostPackage = lm.BoostColorfullBomb = lm.BoostStriped = 0;
            lm.gameStatus = GameState.PrepareGame;
        }

        public static void GiveBoosts()
        {
            foreach (var type in new[] { BoostType.Arrow, BoostType.Hammer, BoostType.Cannon, BoostType.Cap })
                PlayerPrefs.SetInt(type.ToString(), 9);
            foreach (var icon in Object.FindObjectsByType<BoostIcon>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (icon.boostCount != null) icon.boostCount.text = "9";
        }

        static BoostIcon FindIcon(BoostType type) =>
            Object.FindObjectsByType<BoostIcon>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(i => i.type == type && i.isActiveAndEnabled && i.GetComponentInParent<Canvas>() != null)
                .OrderByDescending(i => i.transform.parent != null && i.transform.parent.name == "BarraReforcos")
                .FirstOrDefault();

        public static void Select(BoostType type)
        {
            var lm = LevelManager.THIS;
            if (lm == null) return;
            if (lm.gameStatus != GameState.Playing && lm.gameStatus != GameState.Tutorial)
            {
                Debug.LogWarning("Power-up intro: comece a fase antes de escolher o power-up (estado atual: " + lm.gameStatus + ").");
                return;
            }
            var icon = FindIcon(type);
            if (icon == null) { Debug.LogWarning("Power-up intro: não achei o botão " + type + " na barra do jogo."); return; }
            if (lm.activatedBoost == icon) return;
            if (lm.activatedBoost != null) lm.activatedBoost = null;
            GiveBoosts();
            icon.ActivateBoost();
        }

        /// Dispara a flecha numa linha (-1 = a do meio) sem gastar o power-up: usa o mesmo caminho do jogo (LevelManager.DestroyLine).
        public static void FireArrow(int row)
        {
            var lm = LevelManager.THIS;
            if (lm == null || lm.field == null) return;
            if (lm.gameStatus != GameState.Playing && lm.gameStatus != GameState.Tutorial) { Debug.LogWarning("Comece a fase antes de disparar a flecha."); return; }
            var rows = lm.levelData.maxRows;
            var target = row < 0 ? rows / 2 : Mathf.Clamp(row, 0, rows - 1);
            var item = lm.field.GetSquares().Where(s => s != null && s.row == target && s.Item != null)
                .OrderBy(s => Mathf.Abs(s.col - lm.levelData.maxCols / 2)).Select(s => s.Item).FirstOrDefault();
            if (item == null) { Debug.LogWarning("Não achei uma peça nessa linha."); return; }
            lm.DestroyLine(item, true);
        }

        public static void Cancel()
        {
            var lm = LevelManager.THIS;
            if (lm == null || lm.activatedBoost == null) return;
            lm.activatedBoost.ActivateBoost();
        }

        public static void Reopen()
        {
            var lm = LevelManager.THIS;
            BoosterIntroScreen.Hide(true);
            if (lm != null && lm.activatedBoost != null) BoosterIntroScreen.Show(lm.activatedBoost);
        }

        public static void ReopenIfPlaying()
        {
            if (Application.isPlaying) Reopen();
        }
    }
}
