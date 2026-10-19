// Builds the reusable prefab of the Magic Mail progress bar from the art in Assets/Magic Mail/GUI:
//   Componentes/Barra de progresso/container.png + fill.png   the frame and the purple fill
//   Icons/postal card.png                                     the card on the front edge
//   Particles/Fill Magic/Sprite sheet de partículas mágicas   the star, the small orb and the big glow of the effect
// The sheet is cut into three clean, centred sprites (star.png, orb.png, glow.png) next to it, and a UI additive material is
// made for the light parts (so the golden sparkles turn pale pink over the purple). The prefab is
// Componentes/Barra de progresso/Barra de progresso.prefab and carries MagicProgressBar.
// Menu: Magic Mail > Componentes > Montar barra de progresso. Other tools call EnsurePrefab, which builds it only when missing.
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MagicMail.EditorTools
{
    public static class MagicProgressBarBuilder
    {
        public const string Folder = "Assets/Magic Mail/GUI/Componentes/Barra de progresso/";
        public const string PrefabPath = Folder + "Barra de progresso.prefab";
        const string ContainerPath = Folder + "container.png";
        const string FillPath = Folder + "fill.png";
        const string CardPath = "Assets/Magic Mail/GUI/Icons/postal card.png";
        const string SheetPath = "Assets/Magic Mail/GUI/Particles/Fill Magic/Sprite sheet de partículas mágicas.png";
        const string ParticleFolder = "Assets/Magic Mail/GUI/Particles/Fill Magic/";
        const string ShaderName = "Magic Mail/UI Additive";
        const string MaterialPath = "Assets/Magic Mail/GUI/Shaders/MagicUIAdditive.mat";
        const int PoolSize = 56;
        const float BarWidth = 900f;

        [MenuItem("Magic Mail/Componentes/Montar barra de progresso")]
        static void BuildFromMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null &&
                !EditorUtility.DisplayDialog("Barra de progresso",
                    "O prefab já existe. Montar de novo recria o prefab do zero e perde os ajustes feitos nele (cores, tamanhos, margens). Continuar?",
                    "Refazer", "Cancelar"))
                return;
            var error = Build(out var prefab);
            if (error != null) EditorUtility.DisplayDialog("Barra de progresso", error, "OK");
            else
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
        }

        /// The prefab, built first if it does not exist yet. Null (with a warning in the console) when it could not be built.
        public static GameObject EnsurePrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null) return prefab;
            var error = Build(out prefab);
            if (error != null) Debug.LogWarning("[Barra de progresso] " + error);
            return prefab;
        }

        /// Builds the prefab (replacing the one there). Returns an error message, or null when it worked.
        public static string Build(out GameObject prefab)
        {
            prefab = null;
            var container = MainSprite(ContainerPath);
            var fill = MainSprite(FillPath);
            var card = MainSprite(CardPath);
            if (container == null || fill == null || card == null)
                return "Faltam imagens: container.png, fill.png (em Componentes/Barra de progresso) e postal card.png (em Icons) precisam estar importadas como Sprite.";
            var particles = EnsureParticleSprites();
            if (particles == null) return "Não consegui recortar as partículas de " + SheetPath + ".";
            var additive = EnsureAdditiveMaterial();

            var root = new GameObject("Barra de progresso", typeof(RectTransform));
            try
            {
                var rect = (RectTransform)root.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(BarWidth, BarWidth * container.rect.height / container.rect.width);

                var frame = Part("Moldura", rect, container);
                Stretch(frame.rectTransform);

                var fillImage = Part("Preenchimento", rect, fill);
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                fillImage.fillAmount = 0.6f;

                // The streak and the dots use the normal blend so they keep their own yellow (added over the purple fill they
                // would saturate the blue and turn pale pink); the glow comes from the additive halos behind the dots.
                var trailImage = Part("Rastro", rect, EnsureStreakSprite());

                var halos = new Image[PoolSize];
                var haloParent = NewParent("Auréolas", rect);
                for (var i = 0; i < PoolSize; i++)
                {
                    halos[i] = Part("Auréola " + i.ToString("00"), haloParent, particles[2]);
                    halos[i].material = additive;
                    halos[i].enabled = false;
                }

                var pool = new Image[PoolSize];
                var sparkles = NewParent("Fagulhas", rect);
                for (var i = 0; i < PoolSize; i++)
                {
                    pool[i] = Part("Fagulha " + i.ToString("00"), sparkles, particles[i % 2]);
                    pool[i].enabled = false;
                }

                var glow = Part("Brilho", rect, particles[2]);
                glow.material = additive;
                var cardImage = Part("Carta", rect, card);

                var bar = root.AddComponent<MagicProgressBar>();
                var so = new SerializedObject(bar);
                so.FindProperty("fill").objectReferenceValue = fillImage;
                so.FindProperty("glow").objectReferenceValue = glow;
                so.FindProperty("trail").objectReferenceValue = trailImage;
                so.FindProperty("card").objectReferenceValue = cardImage;
                Assign(so.FindProperty("sparklePool"), pool);
                Assign(so.FindProperty("haloPool"), halos);
                Assign(so.FindProperty("sparkleSprites"), new Object[] { particles[0], particles[1] });
                so.ApplyModifiedPropertiesWithoutUndo();

                bar.ApplyPose(2.2f, 0.6f);   // a pleasant still frame for the prefab and for scenes that use it

                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
            return prefab != null ? null : "Não consegui gravar " + PrefabPath + ".";
        }

        // An empty group at the centre of the bar for a pool of parts (their positions are given in the bar's own coordinates).
        static RectTransform NewParent(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        static Image Part(string name, RectTransform parent, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static void Assign(SerializedProperty array, Object[] items)
        {
            array.arraySize = items.Length;
            for (var i = 0; i < items.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        // The sprite that holds the picture: the user's files were sliced by Unity, so there can be small leftover pieces too.
        static Sprite MainSprite(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderByDescending(s => s.rect.width * s.rect.height).FirstOrDefault();

        // ---- particles ----------------------------------------------------------------------------------------------

        // star, orb, glow: the three big pieces of the sheet, left to right, each cropped to what is really visible and
        // centred in a square so it spins and scales around its middle.
        // Bumped whenever the way the sprites are cut changes: RefreshAssets then cuts them again (the files keep their
        // GUIDs, so the prefab picks up the new pictures by itself).
        const string AssetVersion = "magic-bar-v5";

        // A soft horizontal streak (clear on the left, bright on the right, thin at the top and bottom) for the luminous thread
        // behind the front. Made by code, not cut from the sheet, and white so it can be tinted.
        public static Sprite EnsureStreakSprite()
        {
            const string path = ParticleFolder + "streak.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            const int w = 256, h = 32;
            var pixels = new Color32[w * h];
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var u = x / (w - 1f);                                                // 0 left .. 1 right
                var v = Mathf.Abs((y - (h - 1) / 2f) / (h / 2f));                      // 0 centre .. 1 edge
                var along = Mathf.Pow(u, 2.2f);
                var across = Mathf.Pow(1f - v, 2.2f);
                pixels[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(along * across * 255f));
            }
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(Disk(path), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// Cuts the particle sprites again if they were made by an older version of this builder, and updates the settings of
        /// the existing prefab that changed with it (only the ones still at the old default, so tuning is never lost).
        /// Returns true when something was changed.
        public static bool RefreshAssets()
        {
            var starPath = ParticleFolder + "star.png";
            if (!File.Exists(Disk(starPath))) return false;                       // nothing built yet: Build starts fresh
            if (AssetImporter.GetAtPath(starPath) is TextureImporter importer && importer.userData == AssetVersion) return false;
            EnsureParticleSprites(true);
            MigratePrefabDefaults();
            return true;
        }

        static void MigratePrefabDefaults()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) return;
            var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var bar = contents.GetComponent<MagicProgressBar>();
                if (bar == null) return;
                var so = new SerializedObject(bar);
                // The fill's margins were measured wrong (the soft shadow under the capsule was counted as part of it, which
                // squeezed the capsule). Only a prefab still at the old values is changed.
                var inset = so.FindProperty("fillInset");
                if (Mathf.Approximately(inset.vector4Value.z, 0.132f))
                    inset.vector4Value = new Vector4(0.0235f, 0.0190f, 0.0935f, 0.0374f);
                var size = so.FindProperty("glowSize");
                if (Mathf.Approximately(size.floatValue, 2.4f)) size.floatValue = 1.7f;   // the old halo was too big
                var color = so.FindProperty("glowColor");
                if (Mathf.Approximately(color.colorValue.a, 0.9f))
                    color.colorValue = new Color(color.colorValue.r, color.colorValue.g, color.colorValue.b, 0.7f);

                // The effect was reworked to look like the original: fine golden glitter close to the centre line, mostly round
                // dots, plus a luminous yellow streak behind the front. Each setting changes only if it is still at the old default.
                Retune(so, "sparkleInterval", 0.022f, 0.016f);
                Retune(so, "sparkleLife", new Vector2(0.45f, 1.0f), new Vector2(0.35f, 0.75f));
                Retune(so, "sparkleSize", new Vector2(0.10f, 0.34f), new Vector2(0.03f, 0.12f));
                Retune(so, "sparkleSpread", 0.14f, 0.05f);
                Retune(so, "sparkleRise", 0.2f, 0.06f);
                Retune(so, "trailLength", 0.55f, 0.6f);
                Retune(so, "sparkleColor", new Color(1f, 0.86f, 0.62f, 1f), new Color(1f, 0.9f, 0.5f, 1f));
                Retune(so, "glowColor", new Color(1f, 0.74f, 0.42f, 0.7f), new Color(1f, 0.82f, 0.4f, 0.7f));

                // Yellower and with more glow (the dots and the streak now use the normal blend, with halos behind the dots).
                Retune(so, "sparkleSize", new Vector2(0.03f, 0.12f), new Vector2(0.04f, 0.14f));
                Retune(so, "sparkleColor", new Color(1f, 0.9f, 0.5f, 1f), new Color(1f, 0.85f, 0.3f, 1f));
                Retune(so, "trailColor", new Color(1f, 0.88f, 0.45f, 0.5f), new Color(1f, 0.82f, 0.3f, 0.55f));
                Retune(so, "trailGlowHeight", 0.34f, 0.5f);
                Retune(so, "glowSize", 1.7f, 2.0f);
                Retune(so, "glowColor", new Color(1f, 0.82f, 0.4f, 0.7f), new Color(1f, 0.8f, 0.35f, 0.85f));
                so.ApplyModifiedPropertiesWithoutUndo();

                if (so.FindProperty("trail").objectReferenceValue == null) AddTrail(contents, so);
                // The dots and the streak go back to the normal material (they were additive in the first versions).
                var pool = so.FindProperty("sparklePool");
                for (var i = 0; i < pool.arraySize; i++)
                    if (pool.GetArrayElementAtIndex(i).objectReferenceValue is Image dot) dot.material = null;
                if (so.FindProperty("trail").objectReferenceValue is Image streak) streak.material = null;
                var haloPool = so.FindProperty("haloPool");
                if (haloPool != null && haloPool.arraySize == 0) AddHalos(contents, so, pool.arraySize);
                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static void Retune(SerializedObject so, string name, float old, float now)
        {
            var p = so.FindProperty(name);
            if (p != null && Mathf.Abs(p.floatValue - old) < 0.0005f) p.floatValue = now;
        }

        static void Retune(SerializedObject so, string name, Vector2 old, Vector2 now)
        {
            var p = so.FindProperty(name);
            if (p != null && (p.vector2Value - old).sqrMagnitude < 1e-6f) p.vector2Value = now;
        }

        static void Retune(SerializedObject so, string name, Color old, Color now)
        {
            var p = so.FindProperty(name);
            if (p == null) return;
            var c = p.colorValue;
            if (Mathf.Abs(c.r - old.r) + Mathf.Abs(c.g - old.g) + Mathf.Abs(c.b - old.b) + Mathf.Abs(c.a - old.a) < 0.002f) p.colorValue = now;
        }

        // Older prefabs have no halos behind the sparkles: one per sparkle slot is added under the sparkles, and wired in.
        static void AddHalos(GameObject root, SerializedObject so, int count)
        {
            var glow = EnsureParticleSprites()?[2];
            var additive = EnsureAdditiveMaterial();
            var parent = NewParent("Auréolas", (RectTransform)root.transform);
            var dots = root.transform.Find("Fagulhas");
            parent.SetSiblingIndex(dots != null ? dots.GetSiblingIndex() : parent.GetSiblingIndex());   // behind the dots
            var halos = new Image[count];
            for (var i = 0; i < count; i++)
            {
                halos[i] = Part("Auréola " + i.ToString("00"), parent, glow);
                halos[i].material = additive;
                halos[i].enabled = false;
            }
            Assign(so.FindProperty("haloPool"), halos);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Older prefabs have no luminous streak: it is added right after the fill, and wired into the component.
        static void AddTrail(GameObject root, SerializedObject so)
        {
            var go = new GameObject("Rastro", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = root.layer;
            go.transform.SetParent(root.transform, false);
            var image = go.GetComponent<Image>();
            image.sprite = EnsureStreakSprite();
            image.material = EnsureAdditiveMaterial();
            image.raycastTarget = false;
            var fill = root.transform.Find("Preenchimento");
            go.transform.SetSiblingIndex(fill != null ? fill.GetSiblingIndex() + 1 : 1);
            so.FindProperty("trail").objectReferenceValue = image;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Sprite[] EnsureParticleSprites(bool force = false)
        {
            string[] names = { "star", "orb", "glow" };
            var existing = names.Select(n => AssetDatabase.LoadAssetAtPath<Sprite>(ParticleFolder + n + ".png")).ToArray();
            if (!force && existing.All(s => s != null)) return existing;

            var pieces = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>()
                .OrderByDescending(s => s.rect.width * s.rect.height).Take(3).OrderBy(s => s.rect.x).ToArray();
            if (pieces.Length < 3) return null;

            var sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!sheet.LoadImage(File.ReadAllBytes(Disk(SheetPath)))) return null;
                var pixels = sheet.GetPixels32();
                int w = sheet.width, h = sheet.height;
                for (var k = 0; k < 3; k++)
                {
                    var r = pieces[k].rect;
                    int x0 = Mathf.Max(0, (int)r.xMin), x1 = Mathf.Min(w - 1, (int)r.xMax - 1);
                    int y0 = Mathf.Max(0, (int)r.yMin), y1 = Mathf.Min(h - 1, (int)r.yMax - 1);
                    // What is really visible inside the piece (faint haze around it does not count).
                    int minX = x1, maxX = x0, minY = y1, maxY = y0;
                    for (var y = y0; y <= y1; y++)
                    for (var x = x0; x <= x1; x++)
                        if (pixels[y * w + x].a > 40)
                        {
                            minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                            minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                        }
                    if (maxX < minX) return null;
                    var side = Mathf.Max(maxX - minX + 1, maxY - minY + 1) + 8;
                    var cx = (minX + maxX) / 2f;
                    var cy = (minY + maxY) / 2f;
                    var square = new Color32[side * side];
                    for (var y = 0; y < side; y++)
                    for (var x = 0; x < side; x++)
                    {
                        int sx = Mathf.RoundToInt(cx - side / 2f + x), sy = Mathf.RoundToInt(cy - side / 2f + y);
                        if (sx < x0 || sx > x1 || sy < y0 || sy > y1) continue;   // never borrow pixels of the neighbour
                        square[y * side + x] = pixels[sy * w + sx];
                    }

                    // The sheet has a faint haze of transparency around every piece; added with the additive blend it shows as a
                    // box with hard edges where the crop ends. Drop it, and feather the round pieces towards their rim (the big
                    // sphere is a nearly solid disc in the sheet, but it has to read as a soft glow).
                    for (var y = 0; y < side; y++)
                    for (var x = 0; x < side; x++)
                    {
                        var px = square[y * side + x];
                        var a = Mathf.Clamp01((px.a / 255f - 0.16f) / 0.84f);
                        var d = Mathf.Sqrt((x - side / 2f) * (x - side / 2f) + (y - side / 2f) * (y - side / 2f)) / (side / 2f);
                        if (k == 1) a *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, d));                       // orb
                        else if (k == 2) a *= Mathf.Pow(1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 1f, d)), 1.6f);  // glow
                        px.a = (byte)Mathf.RoundToInt(a * 255f);
                        square[y * side + x] = px;
                    }
                    var piece = new Texture2D(side, side, TextureFormat.RGBA32, false);
                    piece.SetPixels32(square);
                    File.WriteAllBytes(Disk(ParticleFolder + names[k] + ".png"), piece.EncodeToPNG());
                    Object.DestroyImmediate(piece);
                }
            }
            finally
            {
                Object.DestroyImmediate(sheet);
            }

            foreach (var n in names)
            {
                var path = ParticleFolder + n + ".png";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.userData = AssetVersion;
                    importer.SaveAndReimport();
                }
            }
            return names.Select(n => AssetDatabase.LoadAssetAtPath<Sprite>(ParticleFolder + n + ".png")).ToArray();
        }

        // ---- additive material --------------------------------------------------------------------------------------

        static Material EnsureAdditiveMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) return material;
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogWarning("[Barra de progresso] Shader \"" + ShaderName + "\" não encontrado (MagicUIAdditive.shader); as luzes usam o material padrão.");
                return null;
            }
            material = new Material(shader) { name = "MagicUIAdditive" };
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        static string Disk(string assetPath) =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
    }
}
