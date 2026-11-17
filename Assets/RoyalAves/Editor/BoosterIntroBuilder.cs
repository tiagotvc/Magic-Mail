using System.IO;
using RoyalAves.Effects;
using RoyalAves.Meta;
using SweetSugar.Scripts.GUI.Boost;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    /// Cria o prefab da tela de uso de power-up (Resources/UI/BoosterIntroScreen.prefab) e o catálogo inicial
    /// (flecha completa; martelo e canhão com arte provisória). Roda sozinho uma vez se faltar; depois é seu.
    public static class BoosterIntroBuilder
    {
        const string Art = "Assets/Royal Postman Art/Screens/Gameplay/Power ups/";
        const string Masks = "Assets/Royal Postman Art/VFX/ImpactMasks/";
        const string BoosterArt = "Assets/RoyalAves/Art/Boosters/";
        public const string PrefabPath = "Assets/Royal Postman Art/Resources/UI/BoosterIntroScreen.prefab";
        public const string CatalogPath = "Assets/Royal Postman Art/Resources/BoosterIntros/BoosterIntroCatalog.asset";
        const string AdditivePath = "Assets/Royal Postman Art/VFX/Materials/ImpactAdditive.mat";
        const string DimMaterialPath = "Assets/Royal Postman Art/VFX/Materials/BoosterDimHole.mat";
        const string LayoutKey = "RoyalAves.BoosterIntro.LayoutVersion";
        const int LayoutVersion = 11;

        [InitializeOnLoadMethod]
        static void AutoBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                if (!File.Exists(Masks + "flash_soft.png")) return;
                // Versão do layout: ao mudar o desenho padrão (posições, escurecimento com buraco, efeitos) refaz o prefab uma vez.
                var outdated = EditorPrefs.GetInt(LayoutKey, 0) < LayoutVersion;
                if (File.Exists(PrefabPath) && File.Exists(CatalogPath) && !outdated) return;
                try { if (!File.Exists(CatalogPath)) BuildCatalog(); if (!File.Exists(PrefabPath) || outdated) BuildPrefab(); }
                catch (System.Exception e) { Debug.LogError("Não consegui criar a tela de power-up: " + e); }
            };
        }

        /// Refaz o prefab uma vez quando o desenho padrão mudou (versão). Não roda durante o Play.
        public static bool EnsureCurrent()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return false;
            if (File.Exists(PrefabPath) && EditorPrefs.GetInt(LayoutKey, 0) >= LayoutVersion) return true;
            try { if (!File.Exists(CatalogPath)) BuildCatalog(); BuildPrefab(); return true; }
            catch (System.Exception e) { Debug.LogError("Não consegui criar a tela de power-up: " + e); return false; }
        }

        public static bool IsCurrent => File.Exists(PrefabPath) && EditorPrefs.GetInt(LayoutKey, 0) >= LayoutVersion;

        [MenuItem("Royal Aves/Introdução de power-up/Recriar prefab da tela (apaga edições)")]
        static void RebuildMenu()
        {
            if (File.Exists(PrefabPath) && !EditorUtility.DisplayDialog("Recriar prefab",
                    "Isto sobrescreve BoosterIntroScreen.prefab e perde as edições feitas nele. Continuar?", "Recriar", "Cancelar")) return;
            BuildPrefab();
        }

        [MenuItem("Royal Aves/Introdução de power-up/Abrir prefab da tela")]
        static void OpenPrefab()
        {
            if (!File.Exists(CatalogPath)) BuildCatalog();
            if (!File.Exists(PrefabPath)) BuildPrefab();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Selection.activeObject = prefab;
            AssetDatabase.OpenAsset(prefab);
        }

        // ------------------------------------------------------------------------------------------------ helpers

        public static Sprite LoadFull(string path)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                (importer.spriteImportMode != SpriteImportMode.Single || importer.textureType != TextureImporterType.Sprite ||
                 importer.textureCompression != TextureImporterCompression.Uncompressed))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static RectTransform MakeRect(Transform parent, string name, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        static Image Img(Transform parent, string name, Sprite sprite, Color color, Vector2 size, Vector2 position)
        {
            var rect = MakeRect(parent, name, size, position);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color, Vector2 bounds, Vector2 position, TMP_FontAsset font)
        {
            var rect = MakeRect(parent, name, bounds, position);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            if (font != null) label.font = font;
            return label;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static UIMagicGlow AddGlow(RectTransform rect, Material additive, bool forButton)
        {
            var glow = rect.gameObject.AddComponent<UIMagicGlow>();
            glow.additive = additive;
            glow.glowSprite = LoadFull(Masks + "flash_soft.png");
            glow.raySprite = LoadFull(Masks + "streak_soft.png");
            glow.sparkleStar = LoadFull(Masks + "spark_cross.png");
            glow.sparkleDot = LoadFull(Masks + "spark_dot.png");
            glow.ringSprite = LoadFull(Masks + "ring_soft.png");
            if (forButton)
            {
                // Atrás do botão: só um halo curto e faíscas na altura da borda (o contorno fino é feito pela tela).
                // Atrás do botão: halo bem forte; dos espinhos só as pontinhas aparecem (saindo de trás); faíscas são só pontos.
                glow.glowAlpha = 1f; glow.glowSize = 1.2f; glow.glowPulse = 0.06f;
                glow.innerGlowAlpha = 0.85f; glow.innerGlowSize = 0.62f;
                glow.rayCount = 18; glow.rayAlpha = 0.42f; glow.rayLength = 0.83f; glow.rayWidth = 0.075f; glow.raySpin = 2f; glow.rayLengthVariation = 0.3f;
                glow.rayHaloWidth = 3f; glow.rayHaloAlpha = 0.4f;
                glow.sparkleCount = 44; glow.sparkleSizeMin = 0.012f; glow.sparkleSizeMax = 0.032f; glow.sparkleStarShare = 0f; glow.sparkleHaloSize = 3.2f; glow.sparkleRadius = new Vector2(0.3f, 1.0f);
                glow.sparklePeriod = 2.2f; glow.sparkleRise = 0f; glow.sparkleOutward = true;
                glow.dotCount = 0;
            }
            else
            {
                // Atrás da figura (referência: Royal Match): glow quente, raios finos e muitas faíscas espalhadas.
                // Referência: halo forte e curto + MUITOS raios finos e fracos + faíscas que explodem do centro para fora.
                glow.glowAlpha = 1f; glow.glowSize = 0.95f; glow.glowPulse = 0.06f;
                glow.innerGlowAlpha = 0.55f; glow.innerGlowSize = 0.5f;
                glow.rayCount = 56; glow.rayAlpha = 0.32f; glow.rayLength = 0.95f; glow.rayWidth = 0.075f; glow.raySpin = 2f; glow.rayLengthVariation = 0.55f;
                glow.rayHaloWidth = 3.5f; glow.rayHaloAlpha = 0.4f;
                glow.sparkleCount = 20; glow.sparkleSizeMin = 0.03f; glow.sparkleSizeMax = 0.085f; glow.sparkleRadius = new Vector2(0.2f, 1.0f);
                glow.sparklePeriod = 2.4f; glow.sparkleRise = 0f; glow.sparkleOutward = true; glow.sparkleStarShare = 0.85f;
            }
            return glow;
        }

        // ------------------------------------------------------------------------------------------------ prefab

        public static void BuildPrefab()
        {
            EnsureFolder("Assets/Royal Postman Art/Resources/UI");
            var additive = AssetDatabase.LoadAssetAtPath<Material>(AdditivePath);
            var font = LobbyFeaturesConfig.Instance != null ? LobbyFeaturesConfig.Instance.displayFont : null;
            var arrow = LoadFull(Art + "Arrow.png");
            var nameMock = LoadFull(Art + "Flexa Postal.png");

            var root = new GameObject("BoosterIntroScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(BoosterIntroScreen));
            try
            {
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 30000;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;

                var dim = Img(root.transform, "Escurecimento (com buraco no tabuleiro)", null, new Color(0, 0, 0, 0.93f), Vector2.zero, Vector2.zero);
                dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one;
                dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
                var dimMaterial = EnsureDimMaterial();

                var buttonGlowRect = MakeRect(root.transform, "Brilho do botão", new Vector2(300, 300), Vector2.zero);
                var buttonGlow = AddGlow(buttonGlowRect, additive, true);
                var buttonLayer = MakeRect(root.transform, "Botão selecionado", Vector2.zero, Vector2.zero);
                buttonLayer.anchorMin = Vector2.zero; buttonLayer.anchorMax = Vector2.one;
                buttonLayer.offsetMin = buttonLayer.offsetMax = Vector2.zero;

                var header = MakeRect(root.transform, "Cabeçalho", new Vector2(1080, 0), Vector2.zero);   // altura 0: as posições dos filhos contam a partir do topo da tela
                header.anchorMin = header.anchorMax = new Vector2(0.5f, 1f);
                header.pivot = new Vector2(0.5f, 1f);
                header.anchoredPosition = Vector2.zero;

                var iconCenter = new Vector2(-250f, -275f);
                var glowRect = MakeRect(header, "Brilho do ícone", new Vector2(460, 460), iconCenter);
                var headerGlow = AddGlow(glowRect, additive, false);
                var icon = Img(header, "Ícone do power-up", arrow, Color.white, new Vector2(400, 400), iconCenter);
                icon.preserveAspect = true;

                var nameImage = Img(header, "Nome (imagem)", nameMock, Color.white, new Vector2(520, 110), new Vector2(190f, -195f));
                nameImage.preserveAspect = true;
                var nameLocalized = nameImage.gameObject.AddComponent<LocalizedImage>();
                var nameFallback = Text(header, "Nome (texto reserva)", "Power-up", 80, new Color(1f, 0.9f, 0.2f), new Vector2(520, 110), new Vector2(190f, -195f), font);
                nameFallback.gameObject.SetActive(false);
                var nameFallbackText = nameFallback.gameObject.AddComponent<LocalizedTextTMP>();

                var instruction = Text(header, "Como usar", "", 40, Color.white, new Vector2(520, 150), new Vector2(190f, -295f), font);
                instruction.enableAutoSizing = true;
                instruction.fontSizeMin = 24;
                instruction.fontSizeMax = 34;
                instruction.textWrappingMode = TextWrappingModes.Normal;
                instruction.outlineWidth = 0.18f;
                instruction.outlineColor = new Color32(40, 14, 70, 255);
                var instructionText = instruction.gameObject.AddComponent<LocalizedTextTMP>();

                var screen = root.GetComponent<BoosterIntroScreen>();
                var so = new SerializedObject(screen);
                so.FindProperty("group").objectReferenceValue = root.GetComponent<CanvasGroup>();
                so.FindProperty("dim").objectReferenceValue = dim;
                so.FindProperty("dimMaterial").objectReferenceValue = dimMaterial;
                so.FindProperty("header").objectReferenceValue = header;
                so.FindProperty("icon").objectReferenceValue = icon;
                so.FindProperty("headerGlow").objectReferenceValue = headerGlow;
                so.FindProperty("nameImage").objectReferenceValue = nameImage;
                so.FindProperty("nameLocalized").objectReferenceValue = nameLocalized;
                so.FindProperty("nameFallback").objectReferenceValue = nameFallback;
                so.FindProperty("nameFallbackText").objectReferenceValue = nameFallbackText;
                so.FindProperty("instruction").objectReferenceValue = instruction;
                so.FindProperty("instructionText").objectReferenceValue = instructionText;
                so.FindProperty("buttonLayer").objectReferenceValue = buttonLayer;
                so.FindProperty("buttonGlow").objectReferenceValue = buttonGlow;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
            EditorPrefs.SetInt(LayoutKey, LayoutVersion);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Prefab da tela de power-up criado em " + PrefabPath);
        }

        static Material EnsureDimMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(DimMaterialPath);
            if (material != null) return material;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Royal Postman Art/VFX/Materials/BoosterDimHole.shader") ?? Shader.Find("RoyalAves/BoosterDimHole");
            if (shader == null) { Debug.LogError("Shader RoyalAves/BoosterDimHole não encontrado."); return null; }
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, DimMaterialPath);
            return material;
        }

        // ------------------------------------------------------------------------------------------------ catálogo

        public static void BuildCatalog()
        {
            EnsureFolder("Assets/Royal Postman Art/Resources/BoosterIntros");
            var catalog = AssetDatabase.LoadAssetAtPath<BoosterIntroCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<BoosterIntroCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.entries.Clear();
            catalog.entries.Add(new BoosterIntroCatalog.Entry
            {
                type = BoostType.Arrow,
                icon = LoadFull(Art + "Arrow.png"),
                nameSpritePt = LoadFull(Art + "Flexa Postal.png"),
                nameSpriteEn = LoadFull(Art + "Flexa Postal EN.png"),
                titlePt = "Flexa Postal", titleEn = "Postal Arrow",
                instructionPt = "Toque em qualquer item para eliminar a fileira inteira!",
                instructionEn = "Tap any item to remove the entire row!",
            });
            catalog.entries.Add(new BoosterIntroCatalog.Entry
            {
                type = BoostType.Hammer,
                icon = LoadFull(BoosterArt + "hammer.png"),
                titlePt = "Martelo Postal", titleEn = "Postal Hammer",
                instructionPt = "Toque em qualquer item para quebrá-lo!",
                instructionEn = "Tap any item to smash it!",
            });
            catalog.entries.Add(new BoosterIntroCatalog.Entry
            {
                type = BoostType.Cannon,
                icon = LoadFull(BoosterArt + "cannon.png"),
                titlePt = "Canhão Postal", titleEn = "Postal Cannon",
                instructionPt = "Toque em qualquer item para eliminar a coluna inteira!",
                instructionEn = "Tap any item to remove the entire column!",
            });
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }
    }
}
