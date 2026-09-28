// Screen editor of Correio Mágico: every screen of the game in one list — the Unity scenes, the parts of the title and
// game scenes, every Sweet Sugar window in CanvasGlobal, the agency lobby (with the victory screen) and the loading
// overlay. Pick a screen to open it where it lives (its scene, or its prefab in Prefab Mode) and see every picture and
// text in it, each with a field to swap it. The lists of windows are read from the prefabs, so new ones show up by
// themselves.
// Menu: Royal Aves > Editor de telas.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RoyalAves.EditorTools
{
    public class RoyalAvesScreenEditor : EditorWindow
    {
        const string CanvasGlobalPath = "Assets/SweetSugar/Prefabs/CanvasGlobal.prefab";
        const string LobbyPath = "Assets/RoyalAves/Prefabs/RoyalAvesLobby.prefab";
        const string LoadingOverlayPath = "Assets/SweetSugar/Resources/Loading.prefab";
        const string MainScene = "Assets/SweetSugar/Scenes/main.unity";
        const string GameScene = "Assets/SweetSugar/Scenes/game.unity";
        const int MaxRows = 400;

        // Portuguese names for Sweet Sugar's windows (CanvasGlobal children); others show their object name.
        static readonly Dictionary<string, string> WindowNames = new Dictionary<string, string>
        {
            ["MenuPlay"] = "Início de nível (metas e reforços)",
            ["PrePlay"] = "Faixa \"Seu objetivo\" (antes do nível)",
            ["PreCompleteBanner"] = "Faixa \"Você venceu\"",
            ["MenuComplete"] = "Vitória do Sweet Sugar (não usada: o jogo mostra o envelope)",
            ["MenuFailed"] = "Derrota",
            ["PreFailed"] = "Mais movimentos? (continuar)",
            ["LiveShop"] = "Loja de vidas",
            ["GemsShop"] = "Loja de moedas",
            ["BoostShop"] = "Loja de reforços",
            ["BoostInfo"] = "Explicação de reforço",
            ["Settings"] = "Configurações do Sweet Sugar",
            ["SettingsButton"] = "Botão de configurações (Sweet Sugar)",
            ["Daily"] = "Prêmio diário",
            ["Reward"] = "Recompensa",
            ["BonusSpin"] = "Roleta de bônus",
            ["Tutorials"] = "Tutoriais",
            ["TutorialManager"] = "Tutorial (controle)",
            ["MenuGameOver"] = "Fim de jogo",
        };

        static readonly Dictionary<string, string> LobbyNames = new Dictionary<string, string>
        {
            ["Room"] = "Sala (fundo e melhorias)",
            ["HUD"] = "HUD (avatar, moedas, vidas, estrelas)",
            ["Actions"] = "Botões \"Nível\" e \"Área\"",
            ["Nav"] = "Menu inferior",
            ["Modal"] = "Janela (melhorias, baú, configurações)",
            ["RestoreEffect"] = "Efeito de restauração",
        };

        class ScreenEntry
        {
            public string Group, Title, AssetPath, RootPath, ObjectName;
            public bool IsScene => AssetPath.EndsWith(".unity");
        }

        struct Element
        {
            public Component Component;
            public string Path;
        }

        List<ScreenEntry> screens;
        ScreenEntry selected;
        readonly List<Element> elements = new List<Element>();
        bool elementsDirty = true;
        readonly HashSet<string> closedGroups = new HashSet<string>();
        Vector2 listScroll, elementScroll;
        string screenFilter = "", elementFilter = "";
        bool showPictures = true, showTexts = true;

        // "Mostrar esta tela": a hidden window turned on while it is edited, and put back afterwards.
        GameObject previewed;

        [MenuItem("Royal Aves/Editor de telas")]
        static void Open()
        {
            var window = GetWindow<RoyalAvesScreenEditor>("Editor de telas");
            window.minSize = new Vector2(820, 440);
        }

        void OnEnable()
        {
            screens = FindScreens();
            EditorApplication.hierarchyChanged += MarkDirty;
            PrefabStage.prefabStageOpened += OnStageChanged;
            PrefabStage.prefabStageClosing += OnStageChanged;
        }

        void OnDisable()
        {
            RestorePreview();
            EditorApplication.hierarchyChanged -= MarkDirty;
            PrefabStage.prefabStageOpened -= OnStageChanged;
            PrefabStage.prefabStageClosing -= OnStageChanged;
        }

        void MarkDirty()
        {
            elementsDirty = true;
            Repaint();
        }

        void OnStageChanged(PrefabStage stage) => MarkDirty();

        // ---------- screens ----------

        static List<ScreenEntry> FindScreens()
        {
            var list = new List<ScreenEntry>();
            void Add(string group, string title, string asset, string root, string objectName = null)
            {
                if (File.Exists(asset))
                    list.Add(new ScreenEntry { Group = group, Title = title, AssetPath = asset, RootPath = root, ObjectName = objectName ?? root });
            }

            var inBuild = EditorBuildSettings.scenes.ToDictionary(s => s.path, s => s.enabled);
            var scenes = EditorBuildSettings.scenes.Select(s => s.path)
                .Concat(AssetDatabase.FindAssets("t:Scene", new[] { "Assets/SweetSugar/Scenes", "Assets/RoyalAves" }).Select(AssetDatabase.GUIDToAssetPath))
                .Distinct();
            foreach (var scene in scenes)
            {
                var state = inBuild.TryGetValue(scene, out var enabled) ? (enabled ? "" : " (desligada no build)") : " (fora do build)";
                Add("Cenas inteiras", Path.GetFileNameWithoutExtension(scene) + state, scene, "", scene);
            }

            Add("Tela inicial (main)", "Fundo e pilha de cartas", MainScene, "CanvasBack");
            Add("Tela inicial (main)", "Logo e botão Play (em pé)", MainScene, "OrientationHandler/Vertical");
            Add("Tela inicial (main)", "Logo e botão Play (deitado)", MainScene, "OrientationHandler/Horrizontal");

            Add("Jogo (cena game)", "Fundo do jogo", GameScene, "Level/CanvasBack");
            Add("Jogo (cena game)", "HUD em pé (gerente, metas, movimentos, reforços)", GameScene, "Level/OrientationPanel/Vertical");
            Add("Jogo (cena game)", "HUD deitado", GameScene, "Level/OrientationPanel/Horizontal");
            Add("Jogo (cena game)", "HUD deitado (HD)", GameScene, "Level/OrientationPanel/HorizontalHD");

            var canvasGlobal = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasGlobalPath);
            if (canvasGlobal != null)
                foreach (Transform child in canvasGlobal.transform)
                    Add("Janelas do Sweet Sugar (CanvasGlobal)", WindowNames.TryGetValue(child.name, out var name) ? name : child.name, CanvasGlobalPath, child.name);

            var lobby = AssetDatabase.LoadAssetAtPath<GameObject>(LobbyPath);
            var content = lobby != null ? lobby.transform.Find("Content") : null;
            if (content != null)
                foreach (Transform child in content)
                    Add("Lobby da agência", LobbyNames.TryGetValue(child.name, out var name) ? name : child.name, LobbyPath, "Content/" + child.name, child.name);
            if (lobby != null && lobby.transform.Find("Victory") != null)
                Add("Lobby da agência", "Tela de vitória (envelope e estrela)", LobbyPath, "Victory");

            Add("Loading", "Loading entre cenas", LoadingOverlayPath, "", "Loading");
            return list;
        }

        static bool IsOpen(ScreenEntry screen)
        {
            if (screen.IsScene) return SceneManager.GetSceneByPath(screen.AssetPath).isLoaded;
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            return stage != null && stage.assetPath == screen.AssetPath;
        }

        // The screen's objects in what is open now (the whole scene when the entry has no root path).
        static List<GameObject> RootsOf(ScreenEntry screen)
        {
            var roots = new List<GameObject>();
            if (screen == null || !IsOpen(screen)) return roots;
            if (screen.IsScene)
            {
                var sceneRoots = SceneManager.GetSceneByPath(screen.AssetPath).GetRootGameObjects();
                if (string.IsNullOrEmpty(screen.RootPath)) return sceneRoots.ToList();
                var slash = screen.RootPath.IndexOf('/');
                var first = slash < 0 ? screen.RootPath : screen.RootPath.Substring(0, slash);
                var top = sceneRoots.FirstOrDefault(g => g.name == first);
                var found = top == null ? null : slash < 0 ? top.transform : top.transform.Find(screen.RootPath.Substring(slash + 1));
                if (found != null) roots.Add(found.gameObject);
                return roots;
            }
            var root = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
            var target = string.IsNullOrEmpty(screen.RootPath) ? root.transform : root.transform.Find(screen.RootPath);
            if (target != null) roots.Add(target.gameObject);
            return roots;
        }

        void OpenScreen(ScreenEntry screen)
        {
            RestorePreview();
            selected = screen;
            if (!IsOpen(screen))
            {
                if (screen.IsScene)
                {
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                    if (PrefabStageUtility.GetCurrentPrefabStage() != null) StageUtility.GoToMainStage();
                    EditorSceneManager.OpenScene(screen.AssetPath, OpenSceneMode.Single);
                }
                else PrefabStageUtility.OpenPrefab(screen.AssetPath);
            }
            else if (screen.IsScene && PrefabStageUtility.GetCurrentPrefabStage() != null)
                StageUtility.GoToMainStage();
            var roots = RootsOf(screen);
            if (roots.Count == 1)
            {
                Selection.activeGameObject = roots[0];
                EditorGUIUtility.PingObject(roots[0]);
                SceneView.FrameLastActiveSceneView();
            }
            elementsDirty = true;
        }

        // ---------- "Mostrar esta tela" ----------

        void Preview(GameObject root)
        {
            RestorePreview();
            if (root == null || root.activeSelf) return;
            Undo.RecordObject(root, "Mostrar tela");
            root.SetActive(true);
            previewed = root;
            MarkChanged(root);
        }

        void RestorePreview()
        {
            if (previewed == null) return;
            Undo.RecordObject(previewed, "Esconder tela");
            previewed.SetActive(false);
            MarkChanged(previewed);
            previewed = null;
        }

        // ---------- elements ----------

        void RefreshElements()
        {
            elementsDirty = false;
            elements.Clear();
            foreach (var root in RootsOf(selected))
            {
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    if (!(component is Image || component is RawImage || component is SpriteRenderer || component is TMP_Text)) continue;
                    elements.Add(new Element { Component = component, Path = RelativePath(component.transform, root.transform) });
                }
            }
        }

        static string RelativePath(Transform t, Transform root)
        {
            var parts = new List<string>();
            for (; t != null && t != root.parent; t = t.parent) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }

        static void MarkChanged(Object target)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
            var go = target is Component c ? c.gameObject : target as GameObject;
            if (go != null && go.scene.IsValid()) EditorSceneManager.MarkSceneDirty(go.scene);
        }

        // ---------- GUI ----------

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawScreenList();
            DrawDetails();
            EditorGUILayout.EndHorizontal();
        }

        void DrawScreenList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            EditorGUILayout.BeginHorizontal();
            screenFilter = EditorGUILayout.TextField(screenFilter, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("Atualizar", GUILayout.Width(70))) screens = FindScreens();
            EditorGUILayout.EndHorizontal();
            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            foreach (var group in screens.GroupBy(s => s.Group))
            {
                var shown = group.Where(s => Matches(s.Title + " " + s.ObjectName, screenFilter)).ToList();
                if (shown.Count == 0) continue;
                var open = !closedGroups.Contains(group.Key);
                var nowOpen = EditorGUILayout.Foldout(open, $"{group.Key} ({shown.Count})", true, EditorStyles.foldoutHeader);
                if (nowOpen != open)
                {
                    if (nowOpen) closedGroups.Remove(group.Key);
                    else closedGroups.Add(group.Key);
                }
                if (!nowOpen) continue;
                foreach (var screen in shown)
                {
                    var style = screen == selected ? EditorStyles.boldLabel : EditorStyles.label;
                    var label = new GUIContent((IsOpen(screen) ? "● " : "   ") + screen.Title, screen.AssetPath + (string.IsNullOrEmpty(screen.RootPath) ? "" : " › " + screen.RootPath));
                    if (GUILayout.Button(label, style)) OpenScreen(screen);
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.HelpBox("● = aberta agora. Clique numa tela para abri-la e editar.", MessageType.None);
            EditorGUILayout.EndVertical();
        }

        void DrawDetails()
        {
            EditorGUILayout.BeginVertical();
            if (selected == null)
            {
                EditorGUILayout.HelpBox("Escolha uma tela na lista à esquerda. Ela abre na cena ou no prefab onde fica, e aqui aparecem " +
                                        "todas as imagens e textos dela, cada um com um campo para trocar.\n\n" +
                                        "Arraste uma imagem do Project para o quadro ao lado do elemento para substituí-la.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField(selected.Title, EditorStyles.largeLabel);
            EditorGUILayout.LabelField(selected.AssetPath + (string.IsNullOrEmpty(selected.RootPath) ? "" : " › " + selected.RootPath), EditorStyles.miniLabel);
            if (!IsOpen(selected))
            {
                if (GUILayout.Button("Abrir esta tela", GUILayout.Height(28))) OpenScreen(selected);
                EditorGUILayout.EndVertical();
                return;
            }

            var roots = RootsOf(selected);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Selecionar e enquadrar", GUILayout.Height(24)) && roots.Count > 0)
            {
                Selection.objects = roots.Cast<Object>().ToArray();
                SceneView.FrameLastActiveSceneView();
            }
            var single = roots.Count == 1 ? roots[0] : null;
            if (single != null && !single.activeSelf && previewed != single && GUILayout.Button("Mostrar esta tela (está escondida)", GUILayout.Height(24)))
                Preview(single);
            if (previewed != null && GUILayout.Button("Esconder de novo", GUILayout.Height(24)))
                RestorePreview();
            if (selected.IsScene && GUILayout.Button("Salvar cena", GUILayout.Height(24)))
                EditorSceneManager.SaveScene(SceneManager.GetSceneByPath(selected.AssetPath));
            if (!selected.IsScene && GUILayout.Button("Voltar para a cena", GUILayout.Height(24)))
            {
                RestorePreview();
                StageUtility.GoToMainStage();
            }
            EditorGUILayout.EndHorizontal();
            if (!selected.IsScene)
                EditorGUILayout.HelpBox("Esta tela fica num prefab, aberto no modo de edição de prefab. Com o \"Auto Save\" ligado (padrão, no topo da " +
                                        "Scene) as trocas são salvas sozinhas; sem ele, use o botão Save de lá.", MessageType.None);
            if (previewed != null)
                EditorGUILayout.HelpBox("Tela ligada só para você ver. \"Esconder de novo\" (ou trocar de tela) a desliga como era.", MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            elementFilter = EditorGUILayout.TextField(elementFilter, EditorStyles.toolbarSearchField);
            showPictures = GUILayout.Toggle(showPictures, "Imagens", EditorStyles.toolbarButton, GUILayout.Width(70));
            showTexts = GUILayout.Toggle(showTexts, "Textos", EditorStyles.toolbarButton, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            if (elementsDirty) RefreshElements();
            var visible = elements.Where(e => e.Component != null && Matches(e.Path, elementFilter) &&
                                              (e.Component is TMP_Text ? showTexts : showPictures)).ToList();
            EditorGUILayout.LabelField($"{visible.Count} elementos" + (visible.Count > MaxRows ? $" (mostrando {MaxRows}; use a busca)" : ""), EditorStyles.miniBoldLabel);
            elementScroll = EditorGUILayout.BeginScrollView(elementScroll);
            foreach (var element in visible.Take(MaxRows)) DrawElement(element);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawElement(Element element)
        {
            var component = element.Component;
            var go = component.gameObject;
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            var active = EditorGUILayout.Toggle(go.activeSelf, GUILayout.Width(16));
            if (active != go.activeSelf)
            {
                Undo.RecordObject(go, "Ligar/desligar elemento");
                go.SetActive(active);
                MarkChanged(go);
            }

            EditorGUILayout.BeginVertical();
            var style = go.activeInHierarchy ? EditorStyles.label : EditorStyles.miniLabel;
            EditorGUILayout.LabelField(new GUIContent(go.name, element.Path), style);
            EditorGUILayout.LabelField(element.Path, EditorStyles.miniLabel);
            if (component is TMP_Text && component.GetComponents<MonoBehaviour>().Any(m => m != null && m.enabled && m.GetType().Name == "LocalizeText"))
                EditorGUILayout.LabelField("Traduzido pelo Sweet Sugar: ao rodar, o texto pode voltar ao da tradução.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            switch (component)
            {
                case Image image:
                    var sprite = (Sprite)EditorGUILayout.ObjectField(image.sprite, typeof(Sprite), false, GUILayout.Width(64), GUILayout.Height(64));
                    if (sprite != image.sprite) Change(image, "Trocar imagem", () => image.sprite = sprite);
                    break;
                case SpriteRenderer spriteRenderer:
                    var rendererSprite = (Sprite)EditorGUILayout.ObjectField(spriteRenderer.sprite, typeof(Sprite), false, GUILayout.Width(64), GUILayout.Height(64));
                    if (rendererSprite != spriteRenderer.sprite) Change(spriteRenderer, "Trocar imagem", () => spriteRenderer.sprite = rendererSprite);
                    break;
                case RawImage raw:
                    var texture = (Texture)EditorGUILayout.ObjectField(raw.texture, typeof(Texture), false, GUILayout.Width(64), GUILayout.Height(64));
                    if (texture != raw.texture) Change(raw, "Trocar imagem", () => raw.texture = texture);
                    break;
                case TMP_Text text:
                    var value = EditorGUILayout.TextArea(text.text, GUILayout.Width(260), GUILayout.MinHeight(34));
                    if (value != text.text) Change(text, "Trocar texto", () => text.text = value);
                    break;
            }

            if (GUILayout.Button("Ver", GUILayout.Width(40), GUILayout.Height(34)))
            {
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
                SceneView.FrameLastActiveSceneView();
            }
            EditorGUILayout.EndHorizontal();
        }

        static void Change(Object target, string undoName, System.Action apply)
        {
            Undo.RecordObject(target, undoName);
            apply();
            MarkChanged(target);
        }

        static bool Matches(string text, string filter) =>
            string.IsNullOrEmpty(filter) || text.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
