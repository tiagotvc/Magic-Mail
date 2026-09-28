// Puts the red-haired manager, Correio Mágico's main character, in place of Sweet Sugar's confectioner everywhere she
// appears: the game HUD (OrientationPanel and the game scene), the fail and complete popups (CanvasGlobal), the scene
// transition (Resources/Loading) and the title scene (main). The confectioner, built from animated parts, is turned off
// and a "Gerente" picture with ManagerFace takes her place and size. It also creates Resources/GerenteExpressoes and puts
// the manager in the lobby instead of Avenaldo.
// Menu: Royal Aves > Aplicar gerente ruiva. Running it again only updates what it made.
using System.Collections.Generic;
using System.Linq;
using RoyalAves.Characters;
using RoyalAves.Meta;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesManagerBuilder
    {
        const string ArtFolder = RoyalAvesAssetImporter.ManagerFolder;
        const string SetPath = "Assets/RoyalAves/Resources/" + ManagerExpressionSet.ResourcePath + ".asset";
        const string LobbyPrefabPath = "Assets/RoyalAves/Prefabs/RoyalAvesLobby.prefab";
        const string OldCharacter = "character_main";
        const string NewName = "Gerente";

        // Confectioner pictures under other names, which the popups animate (the "Your Target:" banner stretches its
        // character in): the manager goes inside them, filling the same square, and only their pictures are hidden.
        static readonly string[] InPlaceCharacters = { "character_info", "character_info_complete" };

        static readonly string[] PrefabPaths =
        {
            "Assets/SweetSugar/Scripts/System/Orientation/OrientationPanel.prefab",
            "Assets/SweetSugar/Prefabs/CanvasGlobal.prefab",
            "Assets/SweetSugar/Resources/Loading.prefab",
        };

        static readonly string[] ScenePaths = { "Assets/SweetSugar/Scenes/main.unity", "Assets/SweetSugar/Scenes/game.unity" };

        [MenuItem("Royal Aves/Aplicar gerente ruiva")]
        static void ApplyFromMenu()
        {
            if (!RoyalAvesTools.CanRunTool()) return;
            RoyalAvesReskin.EnsureImportSettings();
            ReimportPictures();
            var set = EnsureExpressionSet();
            if (set.neutral == null)
            {
                EditorUtility.DisplayDialog("Gerente ruiva", "Faltam as imagens em " + ArtFolder + " (gerente-neutra.png e as outras).", "OK");
                return;
            }

            var log = new List<string>();
            foreach (var path in PrefabPaths)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (Replace(root.transform, System.IO.Path.GetFileNameWithoutExtension(path), log) > 0)
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            PatchLobby(log);

            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var path in ScenePaths)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var label = System.IO.Path.GetFileNameWithoutExtension(path);
                    if (scene.GetRootGameObjects().Sum(r => Replace(r.transform, label, log)) == 0) continue;
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
            finally
            {
                if (setup.Length > 0 && setup.All(s => !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            Selection.activeObject = set;
            EditorUtility.DisplayDialog("Gerente ruiva",
                (log.Count > 0 ? "Aplicado:\n- " + string.Join("\n- ", log) : "Nada para trocar: a gerente já estava em todos os lugares.") +
                "\n\nAs expressões ficam em " + SetPath + ". O jeito de cada tela (jogo, triste, vitória...) é o campo Mood " +
                "do componente ManagerFace no objeto Gerente.", "OK");
        }

        /// The shared expression list. Only empty slots are filled, so pictures changed in the Inspector stay.
        internal static ManagerExpressionSet EnsureExpressionSet()
        {
            var set = AssetDatabase.LoadAssetAtPath<ManagerExpressionSet>(SetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<ManagerExpressionSet>();
                AssetDatabase.CreateAsset(set, SetPath);
            }
            set.neutral = Fill(set.neutral, "gerente-neutra");
            set.blinkHalf = Fill(set.blinkHalf, "gerente-piscando-meio");
            set.blinkClosed = Fill(set.blinkClosed, "gerente-piscando-fechado");
            set.lookLeft = Fill(set.lookLeft, "gerente-olhando-esquerda");
            set.lookRight = Fill(set.lookRight, "gerente-olhando-direita");
            set.happy = Fill(set.happy, "gerente-feliz");
            if (set.nervous == null || set.nervous.Length < 3) System.Array.Resize(ref set.nervous, 3);
            for (var i = 0; i < set.nervous.Length; i++)
                set.nervous[i] = Fill(set.nervous[i], "gerente-nervosa-" + Mathf.Min(i + 1, 3));
            set.relieved = Fill(set.relieved, "gerente-aliviada");
            set.victory = Fill(set.victory, "gerente-vitoria");
            set.sad = Fill(set.sad, "gerente-triste");
            set.crying = Fill(set.crying, "gerente-chorando");
            set.thinking = Fill(set.thinking, "gerente-pensativa");
            set.confident = Fill(set.confident, "gerente-confiante");
            set.surprised = Fill(set.surprised, "gerente-surpresa");
            set.smile = Fill(set.smile, "gerente-sorriso");
            set.smileOpen = Fill(set.smileOpen, "gerente-sorriso-aberto");
            set.worryLight = Fill(set.worryLight, "gerente-preocupada-leve");
            set.worryMid = Fill(set.worryMid, "gerente-preocupada-media");
            set.worryHigh = Fill(set.worryHigh, "gerente-preocupada-forte");
            set.reliefStart = Fill(set.reliefStart, "gerente-aliviando");
            set.reliefSoft = Fill(set.reliefSoft, "gerente-aliviada-suave");
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            return set;
        }

        /// Picture of the manager plus ManagerFace on this rectangle (its Image is reused when it has one).
        internal static ManagerFace AddPortrait(RectTransform rect, ManagerMood mood)
        {
            var image = rect.GetComponent<Image>();
            if (image == null) image = rect.gameObject.AddComponent<Image>();
            image.sprite = Picture(mood);
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var face = rect.GetComponent<ManagerFace>();
            if (face == null) face = rect.gameObject.AddComponent<ManagerFace>();
            face.Mood = mood;
            return face;
        }

        /// Lobby avatar: the rectangle becomes a round mask and her head fills it.
        internal static ManagerFace AddAvatar(RectTransform circle)
        {
            var mask = circle.GetComponent<Image>();
            if (mask == null) mask = circle.gameObject.AddComponent<Image>();
            mask.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            mask.type = Image.Type.Simple;
            mask.color = Color.white;
            mask.preserveAspect = false;
            mask.raycastTarget = false;
            if (circle.GetComponent<Mask>() == null) circle.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var head = circle.Find(NewName) as RectTransform ?? NewRect(NewName, circle);
            var diameter = circle.sizeDelta.x;
            // Her head (chin to top of the hair) spans about 0.37-0.99 of the picture's height.
            head.anchorMin = head.anchorMax = new Vector2(0.5f, 0.5f);
            head.pivot = new Vector2(0.5f, 0.5f);
            head.sizeDelta = Vector2.one * diameter * 1.45f;
            head.anchoredPosition = new Vector2(0, -diameter * 0.26f);
            return AddPortrait(head, ManagerMood.Idle);
        }

        /// Victory screen: she stands at the bottom-left corner of the screen, peeking over the card.
        internal static ManagerFace AddVictoryPortrait(RectTransform panel, float size, float x)
        {
            var rect = panel.Find("Manager") as RectTransform ?? NewRect("Manager", panel);
            rect.SetAsLastSibling();
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(x, 0);
            rect.sizeDelta = new Vector2(size, size);
            return AddPortrait(rect, ManagerMood.Victory);
        }

        // Every outermost character_main under the root gets a Gerente next to it, covering the confectioner's drawn area;
        // then the confectioner is turned off.
        static int Replace(Transform root, string label, List<string> log)
        {
            var replaced = ReplaceInPlace(root, label, log);
            var confectioners = root.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name == OldCharacter && !HasAncestorNamed(t.parent, OldCharacter)).ToList();
            foreach (var old in confectioners)
            {
                var path = PathOf(old);
                if (!(old.parent is RectTransform parent))
                {
                    log.Add($"{label}: {path} não é interface; ignorado");
                    continue;
                }
                // The loading overlay shows the loading art (Aplicar visual da tela inicial), not the character.
                if (HasAncestorNamed(old.parent, "Loading"))
                {
                    if (old.gameObject.activeSelf)
                    {
                        old.gameObject.SetActive(false);
                        Record(old.gameObject);
                        replaced++;
                    }
                    continue;
                }
                var mood = MoodFor(path);
                var rect = parent.Find(NewName) as RectTransform;
                var created = rect == null;
                if (created)
                {
                    rect = NewRect(NewName, parent);
                    rect.gameObject.layer = old.gameObject.layer;
                    rect.SetSiblingIndex(old.GetSiblingIndex() + 1);
                }
                // Also fixes a Gerente left at size 0 by the first version of this tool.
                var resize = created || rect.sizeDelta.x < 1f;
                if (resize)
                {
                    var area = DrawnArea(old, parent);
                    var size = Mathf.Max(area.width, area.height);
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0); // she stands where the confectioner's feet (or frame bottom) were
                    rect.sizeDelta = new Vector2(size, size);
                    rect.anchoredPosition = new Vector2(area.center.x, area.yMin) - parent.rect.center;
                }
                AddPortrait(rect, mood);
                Record(rect);
                if (old.gameObject.activeSelf)
                {
                    old.gameObject.SetActive(false);
                    Record(old.gameObject);
                }
                if (created) log.Add($"{label}: {path} → Gerente ({mood})");
                else if (resize) log.Add($"{label}: {path} → tamanho da Gerente corrigido");
                replaced++;
            }
            return replaced;
        }

        static int ReplaceInPlace(Transform root, string label, List<string> log)
        {
            var replaced = 0;
            // Only the outermost ones: the confectioner's rig also has hidden parts with these names inside it.
            var pictures = root.GetComponentsInChildren<RectTransform>(true)
                .Where(t => InPlaceCharacters.Contains(t.name) &&
                            !HasAncestorNamed(t.parent, OldCharacter) &&
                            !InPlaceCharacters.Any(name => HasAncestorNamed(t.parent, name)))
                .ToList();
            foreach (var old in pictures)
            {
                var path = PathOf(old);
                var manager = old.Find(NewName) as RectTransform;
                var created = manager == null;
                if (created) manager = NewRect(NewName, old);
                manager.gameObject.layer = old.gameObject.layer;
                manager.anchorMin = Vector2.zero;
                manager.anchorMax = Vector2.one;
                manager.offsetMin = manager.offsetMax = Vector2.zero;
                manager.pivot = new Vector2(0.5f, 0.5f);
                manager.SetAsLastSibling();
                // The confectioner's pictures (hers and her parts') go dark; the object and its animation stay.
                foreach (var graphic in old.GetComponentsInChildren<Graphic>(true))
                    if (!graphic.transform.IsChildOf(manager) && graphic.enabled)
                    {
                        graphic.enabled = false;
                        Record(graphic);
                    }
                AddPortrait(manager, MoodFor(path));
                Record(manager);
                if (created) log.Add($"{label}: {path} → Gerente ({MoodFor(path)})");
                replaced++;
            }
            return replaced;
        }

        // Game HUD follows the board; the popups hold one expression; title and loading just blink, glance and smile.
        static ManagerMood MoodFor(string path)
        {
            if (path.Contains("PreCompleteBanner")) return ManagerMood.Victory;
            if (path.Contains("MenuFailed")) return ManagerMood.Sad;
            if (path.Contains("PreFailed")) return ManagerMood.Worried;
            if (path.Contains("MenuComplete")) return ManagerMood.Victory;
            if (path.Contains("OrientationPanel")) return ManagerMood.Game;
            return ManagerMood.Idle;
        }

        // The area covered by the confectioner's visible pieces, in the parent's space (her own rectangle when she has none).
        // Computed through the local transforms, not world corners: scenes keep inactive canvases at scale 0, which
        // collapses every world position to one point (that is how the game HUD got a Gerente of size 0).
        static Rect DrawnArea(Transform old, RectTransform parent)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            void Add(RectTransform rect)
            {
                var toParent = Matrix4x4.identity;
                for (Transform t = rect; t != null && t != parent; t = t.parent)
                    toParent = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale) * toParent;
                var r = rect.rect;
                foreach (var corner in new[] { new Vector3(r.xMin, r.yMin), new Vector3(r.xMin, r.yMax), new Vector3(r.xMax, r.yMin), new Vector3(r.xMax, r.yMax) })
                {
                    Vector2 point = toParent.MultiplyPoint3x4(corner);
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }
            }
            foreach (var graphic in old.GetComponentsInChildren<Graphic>(true))
                if (graphic.enabled && graphic.color.a > 0.01f && ShownUnder(graphic.transform, old) &&
                    !(graphic is Image image && image.sprite == null))
                    Add(graphic.rectTransform);
            if (min.x > max.x && old is RectTransform own) Add(own);
            return min.x > max.x ? new Rect(-100, -100, 200, 200) : Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static bool ShownUnder(Transform t, Transform top)
        {
            for (; t != null && t != top; t = t.parent)
                if (!t.gameObject.activeSelf) return false;
            return true;
        }

        // Avenaldo (manager.png) leaves the lobby's HUD avatar and window; the victory screen gets the manager too.
        static void PatchLobby(List<string> log)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(LobbyPrefabPath) == null) return;
            var root = PrefabUtility.LoadPrefabContents(LobbyPrefabPath);
            try
            {
                var changed = false;
                if (root.transform.Find("Content/HUD/Avatar/Face") is RectTransform avatar && avatar.GetComponent<Mask>() == null)
                {
                    AddAvatar(avatar);
                    log.Add("lobby: rosto no canto do HUD");
                    changed = true;
                }
                if (root.transform.Find("Content/Modal/Window/Art") is RectTransform art && art.GetComponent<ManagerFace>() == null)
                {
                    AddPortrait(art, ManagerMood.Idle);
                    log.Add("lobby: janela das melhorias");
                    changed = true;
                }
                var victory = root.GetComponentInChildren<VictoryScreen>(true);
                var serialized = victory != null ? new SerializedObject(victory) : null;
                var managerField = serialized?.FindProperty("manager");
                if (managerField != null && managerField.objectReferenceValue == null)
                {
                    // Same place as RoyalAvesLobbyBuilder: 30 x 16 prototype units of 10.8.
                    managerField.objectReferenceValue = AddVictoryPortrait((RectTransform)victory.transform, 324, 172.8f);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    log.Add("lobby: tela de vitória (aliviada ou comemorando)");
                    changed = true;
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, LobbyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Pictures imported before the importer knew the Gerente folder keep a 2048 limit.
        static void ReimportPictures()
        {
            foreach (var path in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder.TrimEnd('/') }).Select(AssetDatabase.GUIDToAssetPath))
                if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.maxTextureSize != RoyalAvesAssetImporter.ManagerMaxSize)
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        static Sprite Picture(ManagerMood mood)
        {
            switch (mood)
            {
                case ManagerMood.Sad: return Load("gerente-triste");
                case ManagerMood.Worried: return Load("gerente-nervosa-3");
                case ManagerMood.Relieved: return Load("gerente-aliviada");
                case ManagerMood.Victory: return Load("gerente-vitoria");
                default: return Load("gerente-neutra");
            }
        }

        static Sprite Fill(Sprite current, string file) => current != null ? current : Load(file);

        static Sprite Load(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + file + ".png");

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        static bool HasAncestorNamed(Transform t, string name)
        {
            for (; t != null; t = t.parent)
                if (t.name == name) return true;
            return false;
        }

        static void Record(Object target)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }

        static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (; t != null; t = t.parent) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }
    }
}
