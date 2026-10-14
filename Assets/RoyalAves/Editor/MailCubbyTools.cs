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
            var cabinetSprite = Import("CM_armario_base_v1.png", 660);
            var doorSprite = Import("CM_armario_porta_v1.png", 660);
            const string letterPath = Art + "CM_escaninho_carta_cartoon_v2.png";
            var letterSprite = Import("CM_escaninho_carta_cartoon_v2.png", 2050);
            if (letterSprite == null) throw new System.InvalidOperationException("Envelope original do jogo não encontrado.");
            GetLetterFit(letterPath, letterSprite, out float letterScale, out Vector2 letterOffset);
            Directory.CreateDirectory(Path.GetDirectoryName(Prefab));
            var root = new GameObject("MailCubby_2x2");
            try
            {
                var animation = root.AddComponent<MailCubbyAnimation>();
                var cabinet = new GameObject("Cabinet");
                cabinet.transform.SetParent(root.transform, false);
                animation.cabinet = cabinet.AddComponent<SpriteRenderer>();
                animation.cabinet.sprite = cabinetSprite;
                animation.cabinetVisibleBounds = GetVisibleBounds(Art + "CM_armario_base_v1.png", cabinetSprite);
                animation.cabinet.sortingLayerName = RoyalAves.Effects.PowerUpEffectOrder.SortingLayer;
                animation.cabinet.sortingOrder = 30;
                animation.letters = new SpriteRenderer[4];
                var locations = new[] { new Vector2(-0.32f, 0.19f), new Vector2(0.32f, 0.19f), new Vector2(-0.32f, -0.34f), new Vector2(0.32f, -0.34f) };
                for (int i = 0; i < 4; i++)
                {
                    var letter = new GameObject("Letter_" + i);
                    letter.transform.SetParent(root.transform, false);
                    letter.transform.localPosition = locations[i] + letterOffset;
                    // Compensate the taller cupboard fit so the envelope and round seal keep their proportions.
                    letter.transform.localScale = new Vector3(letterScale, letterScale * 0.86f, letterScale);
                    letter.transform.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? 4 : -4);
                    animation.letters[i] = letter.AddComponent<SpriteRenderer>();
                    animation.letters[i].sprite = letterSprite;
                    animation.letters[i].sortingLayerName = animation.cabinet.sortingLayerName;
                    animation.letters[i].sortingOrder = 31;
                }
                animation.doors = new SpriteRenderer[2];
                var doorBounds = GetVisibleBounds(Art + "CM_armario_porta_v1.png", doorSprite);
                var doorScale = new Vector3(0.63f / doorBounds.width, 1.16f / doorBounds.height, 1f);
                for (int i = 0; i < 2; i++)
                {
                    var door = new GameObject(i == 0 ? "GlassDoor_Left" : "GlassDoor_Right");
                    door.transform.SetParent(root.transform, false);
                    float sign = i == 0 ? -1f : 1f;
                    float visualCenterX = i == 0 ? doorBounds.center.x : -doorBounds.center.x;
                    door.transform.localPosition = new Vector3(sign * 0.32f - visualCenterX * doorScale.x,
                        -0.12f - doorBounds.center.y * doorScale.y, -0.02f);
                    door.transform.localScale = doorScale;
                    var renderer = door.AddComponent<SpriteRenderer>();
                    renderer.sprite = doorSprite;
                    renderer.flipX = i == 1;
                    renderer.sortingLayerName = animation.cabinet.sortingLayerName;
                    renderer.sortingOrder = 32;
                    animation.doors[i] = renderer;
                }
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
            Debug.Log("Armário postal: duas portas removíveis, quatro cartas e topo à frente do grid.");
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
            EditorGUILayout.LabelField("Prévia visual — cheio → vazio", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Em Play, selecione o objeto criado na cena e enquadre-o com F. Cada impacto de teste emite uma carta. Esta prévia não configura células ou metas da fase.", MessageType.Info);
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
                    if (GUILayout.Button("Impacto: emitir uma carta")) preview.PlayAcceptedHit();
                    if (GUILayout.Button("Reiniciar: cheio")) preview.ResetStock();
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
