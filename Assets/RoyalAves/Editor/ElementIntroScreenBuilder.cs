using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoyalAves.Meta;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    /// <summary>
    /// Cria o prefab da tela "Novo item desbloqueado!" (Resources/UI/ElementIntroScreen.prefab) com a arte de
    /// Assets/Royal Postman Art/Screens/Gameplay/New item achieved, e o catálogo inicial das 18 introduções.
    /// Roda sozinho uma vez quando o prefab ou o catálogo não existem; depois disso o prefab é seu: edite à vontade.
    /// </summary>
    public static class ElementIntroScreenBuilder
    {
        const string Art = "Assets/Royal Postman Art/Screens/Gameplay/New item achieved/";
        public const string PrefabPath = "Assets/Royal Postman Art/Resources/UI/ElementIntroScreen.prefab";
        public const string CatalogPath = "Assets/Royal Postman Art/Resources/ElementIntros/ElementIntroCatalog.asset";
        const string AdditivePath = "Assets/Royal Postman Art/VFX/Materials/ImpactAdditive.mat";
        const string MaskFolder = "Assets/Royal Postman Art/VFX/ImpactMasks/";

        static readonly Color Gold = new Color(1f, 0.78f, 0.25f, 1f);
        static readonly Color GoldLight = new Color(1f, 0.92f, 0.55f, 1f);

        [InitializeOnLoadMethod]
        static void AutoBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                if (!File.Exists(Art + "frame_info.png")) return;
                if (File.Exists(PrefabPath) && File.Exists(CatalogPath)) return;
                try { BuildCatalog(); BuildPrefab(); }
                catch (System.Exception e) { Debug.LogError("Não consegui criar a tela de introdução: " + e); }
            };
        }

        [MenuItem("Royal Aves/Introdução de elemento/Recriar prefab da tela (apaga edições)")]
        static void RebuildMenu()
        {
            if (File.Exists(PrefabPath) && !EditorUtility.DisplayDialog("Recriar prefab",
                    "Isto sobrescreve ElementIntroScreen.prefab e perde as edições feitas nele. Continuar?", "Recriar", "Cancelar")) return;
            BuildPrefab();
        }

        [MenuItem("Royal Aves/Introdução de elemento/Abrir prefab da tela")]
        static void OpenPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) { BuildCatalog(); BuildPrefab(); prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath); }
            Selection.activeObject = prefab;
            AssetDatabase.OpenAsset(prefab);
        }

        // ------------------------------------------------------------------------------------------------ prefab

        public static void BuildPrefab()
        {
            EnsureFolder("Assets/Royal Postman Art/Resources/UI");

            var background = LoadFull(Art + "background_mask.png");
            var frame = LoadFull(Art + "frame_info.png");
            var subtitlePt = LoadFull(Art + "Novo item desbloqueado.png");
            var subtitleEn = LoadFull(Art + "New item unlocked.png");
            var nameMock = LoadFull(Art + "texto nome do objeto mock.png");
            var additive = AssetDatabase.LoadAssetAtPath<Material>(AdditivePath);
            var font = LobbyFeaturesConfig.Instance != null ? LobbyFeaturesConfig.Instance.displayFont : null;

            var root = new GameObject("ElementIntroScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(ElementIntroScreen));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var dim = Img(root.transform, "Fundo", background, Color.white, Vector2.zero, Vector2.zero);
            Stretch(dim.rectTransform);
            dim.color = new Color(1f, 1f, 1f, 0.94f);
            dim.raycastTarget = true;
            // Camada preta extra: a arte do fundo já vem ~92% opaca, então só ela nunca esconde o jogo por completo.
            var extraDim = Img(dim.transform, "Escurecimento extra", null, new Color(0f, 0f, 0f, 0.6f), Vector2.zero, Vector2.zero);
            Stretch(extraDim.rectTransform);

            var content = MakeRect(root.transform, "Conteudo", new Vector2(1080, 1920), Vector2.zero);

            var glow = CreateGlow(content, additive, new Vector2(0, 116), 0.75f);

            var icon = Img(content, "Elemento", null, Color.white, new Vector2(520, 520), new Vector2(0, 116));
            icon.preserveAspect = true;
            // Item de exemplo só para ver o layout ao editar; no jogo ele troca pelo ícone de cada introdução.
            icon.sprite = CampaignElementIntroductions.Icon("Blocks/MailCubby") ?? CampaignElementIntroductions.Icon("Blocks/SugarSquare");
            icon.enabled = icon.sprite != null;
            glow.AddRange(CreateSparkles(content, additive, new Vector2(0, 116)));

            var nameImage = Img(content, "Nome do elemento (imagem)", nameMock, Color.white, new Vector2(939, 147), new Vector2(0, 785));
            nameImage.preserveAspect = true;   // o nome de cada elemento tem uma proporção; não pode esticar
            var nameLocalized = nameImage.gameObject.AddComponent<LocalizedImage>();
            var nameFallback = Text(content, "Nome do elemento (texto reserva)", "Novo elemento!", 130, new Color(1f, 0.9f, 0.2f),
                new Vector2(1000, 200), new Vector2(0, 785), font);
            nameFallback.gameObject.SetActive(false);
            var nameFallbackText = nameFallback.gameObject.AddComponent<LocalizedTextTMP>();

            var subtitle = Img(content, "Subtítulo (Novo item desbloqueado)", subtitlePt, Color.white, new Vector2(918, 93), new Vector2(0, 474));
            var subtitleLocalized = subtitle.gameObject.AddComponent<LocalizedImage>();

            var plate = Img(content, "Placa de instrução", frame, Color.white, new Vector2(940, 362), new Vector2(0, -422));
            var instruction = Text(plate.transform, "Texto da instrução", "", 50, new Color(0.42f, 0.17f, 0.66f),
                new Vector2(820, 205), new Vector2(0, 6), font);
            instruction.enableAutoSizing = true;
            instruction.fontSizeMin = 36;
            instruction.fontSizeMax = 64;
            instruction.textWrappingMode = TextWrappingModes.Normal;
            var instructionText = instruction.gameObject.AddComponent<LocalizedTextTMP>();

            // --- serialização das referências e dos idiomas
            var screen = root.GetComponent<ElementIntroScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("group").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("content").objectReferenceValue = content;
            so.FindProperty("dim").objectReferenceValue = dim;
            so.FindProperty("nameImage").objectReferenceValue = nameImage;
            so.FindProperty("nameLocalized").objectReferenceValue = nameLocalized;
            so.FindProperty("nameFallback").objectReferenceValue = nameFallback;
            so.FindProperty("nameFallbackText").objectReferenceValue = nameFallbackText;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("instruction").objectReferenceValue = instruction;
            so.FindProperty("instructionText").objectReferenceValue = instructionText;
            WriteGlow(so, glow);
            so.ApplyModifiedPropertiesWithoutUndo();

            SetLocalizedSprites(nameLocalized, nameMock, nameMock, null);
            SetLocalizedSprites(subtitleLocalized, subtitlePt, subtitleEn, subtitleEn);
            SetLocalizedTexts(nameFallbackText, "Novo elemento!", "New element!");
            SetLocalizedTexts(instructionText, "Faça combinações ao lado de uma <hl>CAIXA DE CÁLICE</hl> para quebrá-la!",
                "Make matches next to a <hl>CHALICE BOX</hl> to break it!");

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("Tela de introdução criada: " + PrefabPath);
        }

        static List<(Image image, RectTransform rect, ElementIntroScreen.GlowMode mode, float spin, float pulse, float speed, float alpha, float phase)> CreateGlow(Transform content, Material additive, Vector2 position, float scale)
        {
            // Brilho dourado atrás do item, feito com as máscaras limpas de ImpactMasks (camadas animadas pelo ElementIntroScreen):
            // raios que giram, halo, anéis, núcleo, clarão de entrada e faíscas.
            var glowRoot = MakeRect(content, "Brilho dourado", new Vector2(10, 10), position);
            glowRoot.localScale = Vector3.one * scale;   // escala geral do efeito (ajustável no editor, aba Layout e textos)
            var glow = new List<(Image image, RectTransform rect, ElementIntroScreen.GlowMode mode, float spin, float pulse, float speed, float alpha, float phase)>();
            Sprite Mask(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(MaskFolder + name + ".png");
            void Glow(string name, string mask, float size, Color color, ElementIntroScreen.GlowMode mode, float spin,
                float pulse, float speed, float alpha, float phase = 0f, Vector2 offset = default)
            {
                var image = Img(glowRoot, name, Mask(mask), color, new Vector2(size, size), offset);
                image.material = additive;
                // No prefab aberto (sem Play) cada camada aparece com o brilho de repouso; o clarão de entrada só existe no jogo.
                image.color = new Color(color.r, color.g, color.b, mode == ElementIntroScreen.GlowMode.Burst ? 0f : alpha);
                glow.Add((image, null, mode, spin, pulse, speed, alpha, phase));
            }
            void Rays(string name, int count, float length, float thickness, string mask, Color color, float alpha, float spin, float speed)
            {
                var group = MakeRect(glowRoot, name, new Vector2(10, 10), Vector2.zero);
                for (var i = 0; i < count; i++)
                {
                    var ray = Img(group, "Raio " + (i + 1), Mask(mask), color,
                        new Vector2(length * (i % 2 == 0 ? 1f : 0.72f), thickness), Vector2.zero);
                    ray.rectTransform.pivot = new Vector2(0f, 0.5f);   // gira em torno do centro, pela ponta do raio
                    ray.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 360f * i / count);
                    ray.material = additive;
                    ray.color = new Color(color.r, color.g, color.b, alpha * (i % 2 == 0 ? 1f : 0.75f));
                }
                glow.Add((null, group, ElementIntroScreen.GlowMode.Steady, spin, 0.03f, speed, 1f, 0f));
            }
            Rays("Raios longos", 14, 330, 60, "streak_soft", Gold, 0.5f, 7f, 0.35f);
            Rays("Raios finos", 14, 280, 26, "streak_sharp", GoldLight, 0.55f, -12f, 0.45f);
            Glow("Halo grande", "flash_soft", 760, Gold, ElementIntroScreen.GlowMode.Steady, 0f, 0.06f, 0.45f, 0.6f);
            Glow("Disco de choque", "shock_disc", 620, Gold, ElementIntroScreen.GlowMode.Steady, 0f, 0.03f, 0.3f, 0.16f);
            Glow("Anel suave", "ring_soft", 600, Gold, ElementIntroScreen.GlowMode.Steady, 0f, 0.03f, 0.3f, 0.4f, 0.2f);
            Glow("Anel em expansão", "ring_thin", 600, GoldLight, ElementIntroScreen.GlowMode.Ring, 0f, 0f, 0.5f, 0.65f);
            Glow("Núcleo", "flash_core", 440, new Color(1f, 0.95f, 0.7f), ElementIntroScreen.GlowMode.Steady, 0f, 0.05f, 0.6f, 0.75f);
            Glow("Clarão de entrada", "flash_soft", 960, GoldLight, ElementIntroScreen.GlowMode.Burst, 0f, 0f, 1f, 0.9f);
            // Estrelas GRANDES, por fora do item e atrás dele, cada uma com um ponto de luz forte (spark_dot) no mesmo ritmo.
            var sparkOffsets = new[] { new Vector2(-250, 215), new Vector2(260, 185), new Vector2(-240, -195), new Vector2(250, -215) };
            for (var i = 0; i < sparkOffsets.Length; i++)
            {
                var speed = 0.8f + 0.13f * i;
                var phase = i * 0.21f;
                var size = 76 + 12 * (i % 2);
                Glow("Brilho da estrela grande " + (i + 1), "spark_dot", size * 2.4f, GoldLight, ElementIntroScreen.GlowMode.Twinkle, 0f, 0f, speed, 0.9f, phase, sparkOffsets[i]);
                Glow("Estrela grande " + (i + 1), "spark_cross", size, Color.white, ElementIntroScreen.GlowMode.Twinkle, 25f, 0f, speed, 1f, phase, sparkOffsets[i]);
            }
            return glow;
        }

        // Estrelinhas por cima do item, como se ele mesmo brilhasse: tamanhos diferentes, coladas na borda e SEM brilho forte
        // atrás (só um halo bem suave, quase invisível, para não aparecer um ponto de luz solto). Ficam num grupo próprio,
        // depois do ícone, para ficar na frente dele.
        static List<(Image image, RectTransform rect, ElementIntroScreen.GlowMode mode, float spin, float pulse, float speed, float alpha, float phase)> CreateSparkles(Transform content, Material additive, Vector2 position)
        {
            var group = MakeRect(content, "Brilhos do item", new Vector2(10, 10), position);
            var list = new List<(Image image, RectTransform rect, ElementIntroScreen.GlowMode mode, float spin, float pulse, float speed, float alpha, float phase)>();
            Sprite Mask(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(MaskFolder + name + ".png");
            // posição relativa ao centro do item, tamanho da estrela, ritmo e fase
            var stars = new[]
            {
                (new Vector2(-170, 175), 50f, 0.85f, 0.00f),
                (new Vector2(190, 120), 34f, 1.10f, 0.35f),
                (new Vector2(-205, -35), 24f, 1.30f, 0.62f),
                (new Vector2(155, -185), 42f, 0.95f, 0.18f),
                (new Vector2(30, 215), 28f, 1.20f, 0.80f),
                (new Vector2(-140, -195), 30f, 1.05f, 0.50f),
            };
            for (var i = 0; i < stars.Length; i++)
            {
                var (offset, size, speed, phase) = stars[i];
                var halo = Img(group, "Halo suave " + (i + 1), Mask("spark_dot"), GoldLight, new Vector2(size * 1.5f, size * 1.5f), offset);
                halo.material = additive;
                halo.color = new Color(GoldLight.r, GoldLight.g, GoldLight.b, 0.22f);
                list.Add((halo, null, ElementIntroScreen.GlowMode.Twinkle, 0f, 0f, speed, 0.22f, phase));
                var star = Img(group, "Faísca " + (i + 1), Mask("spark_cross"), Color.white, new Vector2(size, size), offset);
                star.material = additive;
                list.Add((star, null, ElementIntroScreen.GlowMode.Twinkle, 25f, 0f, speed, 1f, phase));
            }
            return list;
        }

        /// Garante o filho "Escurecimento extra" (preto) em "Fundo" num prefab criado antes dele existir.
        public static void EnsureExtraDim()
        {
            if (!File.Exists(PrefabPath)) return;
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var dim = root.transform.Find("Fundo");
                if (dim == null || dim.Find("Escurecimento extra") != null) return;
                var extra = Img(dim, "Escurecimento extra", null, new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.zero);
                Stretch(extra.rectTransform);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void WriteGlow(SerializedObject so, List<(Image image, RectTransform rect, ElementIntroScreen.GlowMode mode, float spin, float pulse, float speed, float alpha, float phase)> glow)
        {
            var layers = so.FindProperty("glow");
            layers.arraySize = glow.Count;
            for (var i = 0; i < glow.Count; i++)
            {
                var item = layers.GetArrayElementAtIndex(i);
                var g = glow[i];
                item.FindPropertyRelative("name").stringValue = g.image != null ? g.image.gameObject.name : g.rect.gameObject.name;
                item.FindPropertyRelative("image").objectReferenceValue = g.image;
                item.FindPropertyRelative("rect").objectReferenceValue = g.rect;
                item.FindPropertyRelative("mode").intValue = (int)g.mode;
                item.FindPropertyRelative("spinDegreesPerSecond").floatValue = g.spin;
                item.FindPropertyRelative("pulseScale").floatValue = g.pulse;
                item.FindPropertyRelative("speed").floatValue = g.speed;
                item.FindPropertyRelative("alpha").floatValue = g.alpha;
                item.FindPropertyRelative("phase").floatValue = g.phase;
            }
        }

        /// Refaz só as camadas do brilho dourado (raios, halo, anéis, estrelas com seu glow) dentro do prefab já existente,
        /// mantendo posição/escala do objeto "Brilho dourado" e tudo o mais que você editou.
        [MenuItem("Royal Aves/Introdução de elemento/Refazer só o efeito dourado (mantém o resto)")]
        public static void RebuildGlow()
        {
            if (!File.Exists(PrefabPath)) { BuildCatalog(); BuildPrefab(); return; }
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var screen = root.GetComponent<ElementIntroScreen>();
                var content = root.transform.Find("Conteudo");
                var old = content.Find("Brilho dourado");
                var position = new Vector2(0, 116);
                var scale = 0.75f;
                var index = 0;
                if (old != null)
                {
                    position = ((RectTransform)old).anchoredPosition;
                    scale = old.localScale.x;
                    index = old.GetSiblingIndex();
                    Object.DestroyImmediate(old.gameObject);
                }
                var additive = AssetDatabase.LoadAssetAtPath<Material>(AdditivePath);
                var glow = CreateGlow(content, additive, position, scale);
                content.Find("Brilho dourado").SetSiblingIndex(index);
                var oldSparkles = content.Find("Brilhos do item");
                if (oldSparkles != null) Object.DestroyImmediate(oldSparkles.gameObject);
                var sparkles = CreateSparkles(content, additive, position);
                var itemIcon = content.Find("Elemento");
                content.Find("Brilhos do item").SetSiblingIndex(itemIcon != null ? itemIcon.GetSiblingIndex() + 1 : content.childCount);
                glow.AddRange(sparkles);
                var so = new SerializedObject(screen);
                WriteGlow(so, glow);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("Efeito dourado refeito no prefab da introdução.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void SetLocalizedSprites(LocalizedImage target, Sprite pt, Sprite en, Sprite es)
        {
            var so = new SerializedObject(target);
            var list = so.FindProperty("images");
            var entries = new List<(int language, Sprite sprite)>();
            if (pt != null) entries.Add(((int)GameLanguage.Portuguese, pt));
            if (en != null) entries.Add(((int)GameLanguage.English, en));
            if (es != null) entries.Add(((int)GameLanguage.Spanish, es));
            list.arraySize = entries.Count;
            for (var i = 0; i < entries.Count; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("language").enumValueIndex = entries[i].language;
                item.FindPropertyRelative("sprite").objectReferenceValue = entries[i].sprite;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetLocalizedTexts(LocalizedTextTMP target, string pt, string en)
        {
            var so = new SerializedObject(target);
            var list = so.FindProperty("texts");
            list.arraySize = 2;
            var a = list.GetArrayElementAtIndex(0);
            a.FindPropertyRelative("language").enumValueIndex = (int)GameLanguage.Portuguese;
            a.FindPropertyRelative("text").stringValue = pt;
            var b = list.GetArrayElementAtIndex(1);
            b.FindPropertyRelative("language").enumValueIndex = (int)GameLanguage.English;
            b.FindPropertyRelative("text").stringValue = en;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------------------------------------ atlas

        // O atlas veio fatiado automaticamente em dezenas de pedacinhos: refaz com as 8 luzes (retângulos em pixels,
        // origem no canto inferior esquerdo; imagem 1448 x 1086).
        static void SliceAtlas()
        {
            var path = Art + "Atlas de Oito Efeitos Luminosos.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
#pragma warning disable 618
            if (importer.spritesheet.Any(s => s.name == "glow_disc_big")) return;
            SpriteMetaData Cell(string name, float x, float yTop, float w, float h) => new SpriteMetaData
            {
                name = name, rect = new Rect(x, 1086 - yTop - h, w, h), alignment = (int)SpriteAlignment.Center, pivot = new Vector2(0.5f, 0.5f)
            };
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritesheet = new[]
            {
                Cell("glow_disc_big", 10, 150, 360, 360),
                Cell("glow_disc_small", 415, 240, 250, 250),
                Cell("glow_ring_thin", 733, 162, 320, 320),
                Cell("glow_ring_thick", 1062, 130, 380, 380),
                Cell("glow_star_soft", 30, 595, 305, 335),
                Cell("glow_star_sharp", 355, 580, 380, 370),
                Cell("glow_sparkles", 765, 635, 245, 260),
                Cell("glow_burst", 1055, 580, 390, 375),
            };
#pragma warning restore 618
            importer.SaveAndReimport();
        }

        static Sprite AtlasSprite(string name) =>
            AssetDatabase.LoadAllAssetsAtPath(Art + "Atlas de Oito Efeitos Luminosos.png").OfType<Sprite>().FirstOrDefault(s => s.name == name);

        // A arte veio em modo Multiple e fatiada automaticamente (o subtítulo virava só "unlocked!"): vira Single, sem
        // compressão, para o sprite ser a imagem inteira e o texto ficar nítido.
        static Sprite LoadFull(string path)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                (importer.spriteImportMode != SpriteImportMode.Single || importer.textureCompression != TextureImporterCompression.Uncompressed))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ------------------------------------------------------------------------------------------------ ui helpers

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

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ------------------------------------------------------------------------------------------------ catálogo

        static readonly Dictionary<int, (string id, string titleEn, string textEn)> English = new Dictionary<int, (string, string, string)>
        {
            { 1, ("caixas", "Boxes!", "Make matches next to the <hl>BOXES</hl> or use specials to break them!") },
            { 2, ("foguete", "Rocket!", "Match <hl>4 in a row</hl> to create a rocket. It clears a whole row or column!") },
            { 3, ("dinamite", "Dynamite!", "Match in a <hl>T or L shape</hl> to create dynamite. Use it to hit many cells around it!") },
            { 4, ("grama", "Grass!", "Make matches <hl>on the GRASS</hl> or use specials to remove it!") },
            { 6, ("helice", "Propeller!", "Match <hl>4 in a square</hl> to create a propeller. It flies to a target and can carry another special!") },
            { 8, ("globo-de-luz", "Light ball!", "Match <hl>5 in a row</hl> to create a light ball. Combine it with a color or another special!") },
            { 11, ("escaninho", "Mail cubby!", "Make matches next to the <hl>CUBBY</hl> to open its doors. Keep hitting it to deliver all the letters!") },
            { 21, ("franqueadora", "Franking machine!", "Make matches next to the <hl>MACHINE</hl> or use specials to produce the goal letters!") },
            { 31, ("gelo", "Ice!", "Match the <hl>FROZEN</hl> piece or hit it with a special to break the ice!") },
            { 36, ("gelo-duplo", "Double ice!", "This ice has <hl>TWO LAYERS</hl>. Hit it twice to free the piece!") },
            { 46, ("ovos-magicos", "Magic eggs!", "Make matches next to the <hl>EGGS</hl> or use specials to crack them!") },
            { 61, ("pocoes", "Potions!", "Match next to each flask using <hl>ITS COLOR</hl>, or hit it with a special. Empty all four flasks!") },
            { 76, ("bolsa-do-carteiro", "Postman's bag!", "Make matches next to the <hl>BAG</hl> or use specials. It stays in place and delivers the goal scrolls!") },
            { 91, ("pedra-runica", "Runic stone!", "<hl>RUNIC STONES</hl> resist normal matches. Use specials or combine power-ups to break them!") },
            { 106, ("tinteiro", "Inkwell!", "Hit the <hl>INKWELL</hl> with nearby matches or specials. It spills ink and breaks after five hits!") },
            { 121, ("cofre", "Safe!", "The <hl>SAFE</hl> takes four cells and only takes damage from specials. Hit it five times to open it!") },
            { 136, ("sinalizador", "Signal!", "Make matches next to it with the <hl>LIT COLOR</hl>, or use specials. The color changes every move. Three hits remove it!") },
            { 151, ("fita-postal", "Postal tape!", "The <hl>TAPE</hl> holds a piece. Match on it or next to it, or use a special to peel it off. It is not a goal!") },
        };

        public static void BuildCatalog()
        {
            if (File.Exists(CatalogPath)) return;
            EnsureFolder("Assets/Royal Postman Art/Resources/ElementIntros");
            var catalog = ScriptableObject.CreateInstance<ElementIntroCatalog>();
            foreach (var old in CampaignElementIntroductions.Entries)
            {
                English.TryGetValue(old.level, out var en);
                catalog.entries.Add(new ElementIntroCatalog.Entry
                {
                    id = en.id ?? "fase-" + old.level,
                    level = old.level,
                    iconResource = old.resource,
                    titlePt = old.title,
                    titleEn = en.titleEn ?? old.title,
                    instructionPt = old.instruction.Replace("\n", " ").Replace("<color=#AE193F>", "<hl>").Replace("</color>", "</hl>"),
                    instructionEn = en.textEn ?? "",
                });
            }
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Catálogo de introduções criado: " + CatalogPath);
        }
    }
}
