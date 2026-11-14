using System.IO;
using RoyalAves.Gameplay;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public sealed class MailCubbyTools : EditorWindow
    {
        const string Art = "Assets/RoyalAves/Art/Obstacles/MailCubby/";
        const string Prefab = "Assets/RoyalAves/Prefabs/Elements/MailCubby.prefab";
        MailCubbyAnimation preview;

        [MenuItem("Royal Aves/Elementos/Escaninho/Criar prefab da animação")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Prefab) != null)
            {
                InstallWoodV6();
                return;
            }
            var cabinetSprite = Import("MM_cubby_wood_body_v6.png", 660);
            var doorSprite = Import("MM_cubby_wood_door_v6.png", 660);
            const string letterPath = Art + "CM_escaninho_carta_cartoon_v2.png";
            var letterSprite = Import("CM_escaninho_carta_cartoon_v2.png", 2050);
            if (letterSprite == null) throw new System.InvalidOperationException("Envelope original do jogo não encontrado.");
            var letterBounds = GetVisibleBounds(letterPath, letterSprite);
            Directory.CreateDirectory(Path.GetDirectoryName(Prefab));
            var root = new GameObject("MailCubby_2x2");
            try
            {
                var animation = root.AddComponent<MailCubbyAnimation>();
                animation.playbackSpeed = 1f;
                animation.emissionDuration = .22f;
                animation.letterStagger = .01f;
                var cabinet = new GameObject("Cabinet");
                cabinet.transform.SetParent(root.transform, false);
                animation.cabinet = cabinet.AddComponent<SpriteRenderer>();
                animation.cabinet.sprite = cabinetSprite;
                var bodyBounds = GetVisibleBounds(Art + "MM_cubby_wood_body_v6.png", cabinetSprite);
                animation.cabinetVisibleBounds = bodyBounds;
                animation.cabinet.sortingLayerName = RoyalAves.Effects.PowerUpEffectOrder.SortingLayer;
                animation.cabinet.sortingOrder = 30;
                animation.lettersPerHit = new[] { MailCubbyCell.LettersPerHit, MailCubbyCell.LettersPerHit,
                    MailCubbyCell.LettersPerHit, MailCubbyCell.LettersPerHit };
                animation.letters = new SpriteRenderer[MailCubbyCell.LettersPerCabinet];
                int stackDepth = MailCubbyCell.LettersPerNiche / 3;
                float letterScale = bodyBounds.width * 0.235f / letterBounds.width;
                float letterHeight = letterBounds.height * letterScale;
                var shelfFloors = new[] { bodyBounds.yMin + bodyBounds.height * 0.52f, bodyBounds.yMin + bodyBounds.height * 0.10f };
                for (int i = 0; i < animation.letters.Length; i++)
                {
                    var letter = new GameObject("Letter_" + i);
                    letter.transform.SetParent(root.transform, false);
                    int niche = i / MailCubbyCell.LettersPerNiche;
                    int column = i % 3;
                    int depth = (i % MailCubbyCell.LettersPerNiche) / 3;
                    float depthScale = letterScale * (1f - depth * 0.035f);
                    var visibleCenter = new Vector2(bodyBounds.center.x + (column - 1) * bodyBounds.width * 0.255f
                        + depth * bodyBounds.width * 0.008f,
                        shelfFloors[niche] + letterHeight * (0.5f + depth * 0.12f));
                    letter.transform.localPosition = visibleCenter - letterBounds.center * depthScale;
                    letter.transform.localScale = Vector3.one * depthScale;
                    animation.letters[i] = letter.AddComponent<SpriteRenderer>();
                    animation.letters[i].sprite = letterSprite;
                    animation.letters[i].sortingLayerName = animation.cabinet.sortingLayerName;
                    animation.letters[i].sortingOrder = 31 + stackDepth - depth;
                    animation.letters[i].color = Color.Lerp(Color.white, new Color(0.72f, 0.55f, 0.34f),
                        depth / (float)Mathf.Max(1, stackDepth - 1) * 0.65f);
                }
                animation.doors = new SpriteRenderer[2];
                var doorBounds = GetVisibleBounds(Art + "MM_cubby_wood_door_v6.png", doorSprite);
                float doorScale = bodyBounds.width * 0.443f / doorBounds.width;
                float doorWidth = doorBounds.width * doorScale;
                for (int i = 0; i < 2; i++)
                {
                    var door = new GameObject(i == 0 ? "ClosedDoor_Left" : "ClosedDoor_Right");
                    door.transform.SetParent(root.transform, false);
                    float sign = i == 0 ? -1f : 1f;
                    // Slight overlap closes the seam between the rounded door edges.
                    float visibleCenterX = bodyBounds.center.x + sign * (doorWidth * 0.5f - bodyBounds.width * 0.005f);
                    door.transform.localPosition = new Vector3(visibleCenterX + sign * doorBounds.center.x * doorScale,
                        bodyBounds.yMin + bodyBounds.height * 0.48f - doorBounds.center.y * doorScale, -0.02f);
                    door.transform.localScale = Vector3.one * doorScale;
                    var renderer = door.AddComponent<SpriteRenderer>();
                    renderer.sprite = doorSprite;
                    renderer.flipX = i == 1;
                    renderer.sortingLayerName = animation.cabinet.sortingLayerName;
                    renderer.sortingOrder = 32 + stackDepth;
                    animation.doors[i] = renderer;
                }
                // Closed doors stay inside the frame. Open-door extents must not shrink
                // the body when the board fits this object into its four reserved cells.
                animation.cabinetVisibleBounds = bodyBounds;
                var plaque = new GameObject("StockCounter");
                plaque.transform.SetParent(root.transform, false);
                plaque.transform.localPosition = new Vector3(0, -0.69f, -0.04f);
                animation.counter = plaque.AddComponent<TextMesh>();
                animation.counter.text = "";
                plaque.SetActive(false);
                animation.counter.anchor = TextAnchor.MiddleCenter;
                animation.counter.alignment = TextAlignment.Center;
                animation.counter.fontSize = 64;
                animation.counter.characterSize = 0.018f;
                animation.counter.color = new Color(0.25f, 0.12f, 0.04f);
                plaque.GetComponent<MeshRenderer>().sortingLayerName = animation.cabinet.sortingLayerName;
                plaque.GetComponent<MeshRenderer>().sortingOrder = 33;
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { DestroyImmediate(root); }
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            Debug.Log("Armário postal: portas fechadas, corpo ajustado ao 2x2 e 36 cartas em seis pilhas.");
        }

        // Update existing objects in place: production and laboratory references retain their file IDs.
        public static void InstallWoodV6()
        {
            var body = Import("MM_cubby_wood_body_v6.png", 660);
            var door = Import("MM_cubby_wood_door_v6.png", 660);
            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var animation = root.GetComponent<MailCubbyAnimation>();
                if (animation.letters.Length != MailCubbyCell.LettersPerCabinet || animation.doors.Length != 2)
                    throw new System.InvalidOperationException("Unexpected cabinet hierarchy; refusing to replace it.");
                animation.cabinet.sprite = body;
                var bounds = GetVisibleBounds(Art + "MM_cubby_wood_body_v6.png", body);
                animation.cabinetVisibleBounds = bounds;
                var letter = animation.letters[0].sprite;
                var letterBounds = GetVisibleBounds(AssetDatabase.GetAssetPath(letter), letter);
                float letterScale = bounds.width * .235f / letterBounds.width;
                float letterHeight = letterBounds.height * letterScale;
                for (int i = 0; i < animation.letters.Length; i++)
                {
                    int niche = i / MailCubbyCell.LettersPerNiche;
                    int column = i % 3;
                    int depth = (i % MailCubbyCell.LettersPerNiche) / 3;
                    float scale = letterScale * (1f - depth * .035f);
                    var center = new Vector2(bounds.center.x + (column - 1) * bounds.width * .255f + depth * bounds.width * .008f,
                        bounds.yMin + bounds.height * (niche == 0 ? .52f : .10f) + letterHeight * (.5f + depth * .12f));
                    animation.letters[i].transform.localPosition = center - letterBounds.center * scale;
                    animation.letters[i].transform.localScale = Vector3.one * scale;
                }
                var doorBounds = GetVisibleBounds(Art + "MM_cubby_wood_door_v6.png", door);
                float doorScale = bounds.width * .443f / doorBounds.width;
                float doorWidth = doorBounds.width * doorScale;
                for (int i = 0; i < 2; i++)
                {
                    float sign = i == 0 ? -1f : 1f;
                    var renderer = animation.doors[i];
                    renderer.sprite = door;
                    renderer.flipX = i == 1;
                    renderer.transform.localPosition = new Vector3(bounds.center.x + sign * (doorWidth * .5f - bounds.width * .005f)
                        + sign * doorBounds.center.x * doorScale,
                        bounds.yMin + bounds.height * .48f - doorBounds.center.y * doorScale, -.02f);
                    renderer.transform.localScale = Vector3.one * doorScale;
                }
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void GetLetterFit(string path, Sprite sprite, out float scale, out Vector2 offset)
        {
            var pixels = new Texture2D(2, 2);
            try
            {
                pixels.LoadImage(File.ReadAllBytes(path));
                int left = pixels.width, right = 0, bottom = pixels.height, top = 0;
                var colors = pixels.GetPixels32();
                for (int y = 0; y < pixels.height; y++)
                    for (int x = 0; x < pixels.width; x++)
                        if (colors[y * pixels.width + x].a >= 128)
                        { left = Mathf.Min(left, x); right = Mathf.Max(right, x); bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
                scale = 0.57f * sprite.pixelsPerUnit / Mathf.Max(1, right - left + 1);
                offset = -new Vector2((left + right + 1) * 0.5f - sprite.rect.x - sprite.pivot.x, (bottom + top + 1) * 0.5f - sprite.rect.y - sprite.pivot.y) / sprite.pixelsPerUnit * scale;
            }
            finally { DestroyImmediate(pixels); }
        }

        static Rect GetVisibleBounds(string path, Sprite sprite)
        {
            var texture = new Texture2D(2, 2);
            try
            {
                texture.LoadImage(File.ReadAllBytes(path));
                var colors = texture.GetPixels32();
                int left = texture.width, right = -1, bottom = texture.height, top = -1;
                for (int y = 0; y < texture.height; y++)
                    for (int x = 0; x < texture.width; x++)
                        if (colors[y * texture.width + x].a >= 128)
                        { left = Mathf.Min(left, x); right = Mathf.Max(right, x); bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
                return new Rect((left - sprite.rect.x - sprite.pivot.x) / sprite.pixelsPerUnit,
                    (bottom - sprite.rect.y - sprite.pivot.y) / sprite.pixelsPerUnit,
                    (right - left + 1f) / sprite.pixelsPerUnit, (top - bottom + 1f) / sprite.pixelsPerUnit);
            }
            finally { DestroyImmediate(texture); }
        }

        static Sprite Import(string name, float ppu)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Art + name);
            if (importer == null) { AssetDatabase.ImportAsset(Art + name); importer = (TextureImporter)AssetImporter.GetAtPath(Art + name); }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(Art + name);
        }

        [MenuItem("Royal Aves/Elementos/Escaninho/Testar animação em Play")]
        static void Open() { GetWindow<MailCubbyTools>("Escaninho"); }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Preview visual - full to empty", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Primeiro impacto remove as portas. Cada nicho recebe dois impactos de nove cartas. Cinco impactos esvaziam as seis pilhas de seis cartas.", MessageType.Info);
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Criar prévia na cena"))
                {
                    if (preview != null) Destroy(preview.gameObject);
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
                    if (asset == null) { Build(); asset = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab); }
                    preview = Instantiate(asset).GetComponent<MailCubbyAnimation>();
                    Selection.activeGameObject = preview.gameObject;
                    SceneView.lastActiveSceneView?.FrameSelected();
                }
                using (new EditorGUI.DisabledScope(preview == null))
                {
                    if (preview != null)
                    {
                        if (GUILayout.Button(preview.DoorsAttached ? "Primeiro impacto: remover portas" : "Impacto: enviar nove cartas")) preview.PlayAcceptedHit();
                        if (GUILayout.Button("Reiniciar: cheio")) preview.ResetStock();
                    }
                }
            }
        }

        [InitializeOnLoadMethod]
        static void WatchRequest() { EditorApplication.update += ProcessRequest; }
        static void ProcessRequest()
        {
            const string request = "Logs/mail-cubby-build.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { Build(); File.WriteAllText("Logs/mail-cubby-build.result", "OK: prefab visual criado."); }
            catch (System.Exception e) { File.WriteAllText("Logs/mail-cubby-build.result", e.ToString()); Debug.LogException(e); }
        }
    }
}
