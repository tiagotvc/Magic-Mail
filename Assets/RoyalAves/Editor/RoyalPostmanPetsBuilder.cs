using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.UI.RoyalPostman2026;

namespace RoyalAves.EditorTools
{
    /// <summary>Explicit initial authoring recipe for Pets only. Never rebuilds Home or its navigation.</summary>
    public static class RoyalPostmanPetsBuilder
    {
        public const string PrefabPath = "Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab";
        static Font font;
        static readonly Color White = new Color32(255, 249, 239, 255);
        static readonly Color Navy = new Color32(9, 19, 45, 255);
        static readonly Color Gold = new Color32(255, 204, 49, 255);
        static Sprite panel;

        // Historical V1 recipe; the active menu and composition are in RoyalPostmanPetsMockBuilder.
        public static void Build()
        {
            foreach (var entry in new[] {
                new[]{"Menu","pets-workshop-v1"},new[]{"Menu","pets-incubator-empty-v1"},new[]{"Menu","pets-panel-v1"},
                new[]{"Icon","pets-egg-v1"},new[]{"Icon","pets-hourglass-v1"}})
                Import(entry[0], entry[1]);
            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/RoyalAves/Fonts/LilitaOne-Regular.ttf");
            panel = Sprite("pets-panel-v1");
            var scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("Pets", typeof(RectTransform));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                var r = (RectTransform)root.transform;
                r.sizeDelta = new Vector2(1080, 1920);
                var controller = root.AddComponent<RoyalPostmanPetsScreen>();
                var backdrop = Picture(r, "Background", "pets-workshop-v1", 0, 0, 1080, 1920);
                backdrop.raycastTarget = true;
                Stretch(backdrop.rectTransform);
                var fit = backdrop.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = (float)backdrop.sprite.texture.width / backdrop.sprite.texture.height;
                // Stable native logical viewport; all editable content is stored in this prefab.
                var body = Node(r, "Body", 0, 0, 1080, 1558);
                Stretch(body); body.offsetMin = new Vector2(0, 232); body.offsetMax = new Vector2(0, -130);
                var stage = Node(body, "Workshop", 420, 110, 840, 1060);
                stage.anchorMin = stage.anchorMax = new Vector2(0, .5f);
                controller.workshop = stage.gameObject;
                controller.incubators = new RoyalPostmanPetsScreen.IncubatorView[3];
                for (int i = 0; i < 3; i++) controller.incubators[i] = Incubator(stage, i);
                var detail = Panel(stage, "ItemDetail", 0, -542, 808, 162);
                controller.hintTitle = Label(detail.rectTransform, "Title", "Escolha um ovo", 0, 36, 740, 46, 36, Gold);
                controller.hintDescription = Label(detail.rectTransform, "Description", "Selecione um item no inventário ao lado.", 0, -26, 732, 72, 27, White);
                var current = Panel(body, "CurrentPet", 420, 100, 768, 700);
                current.rectTransform.anchorMin = current.rectTransform.anchorMax = new Vector2(0, .5f);
                Picture(current.rectTransform, "Paw", "paw-cream", 0, 140, 175, 175, true);
                Label(current.rectTransform, "Title", "Seu companheiro", 0, -15, 680, 70, 49, Gold);
                Label(current.rectTransform, "Description", "Nenhum mascote ativo.\nSeus companheiros aparecerão aqui.", 0, -130, 680, 150, 33, White);
                controller.currentPanel = current.gameObject; current.gameObject.SetActive(false);

                controller.currentTab = Tab(body, "CurrentTab", "Atual", "paw-cream", -102, -88, false);
                controller.incubatorTab = Tab(body, "IncubatorTab", "Incubadora", "pets-egg-v1", -102, -238, true);
                BuildInventory(body, controller);
                BuildHeader(r, controller);
                BuildHelp(r, controller);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                var kit = RoyalPostmanRuntimeKit.Load();
                if (kit.additionalScreens == null) kit.additionalScreens = new System.Collections.Generic.List<RoyalPostmanRuntimeKit.Screen>();
                var screen = kit.additionalScreens.Find(s => s.id == RoyalPostmanPetsScreen.ScreenId);
                if (screen == null) { screen = new RoyalPostmanRuntimeKit.Screen { id = RoyalPostmanPetsScreen.ScreenId }; kit.additionalScreens.Add(screen); }
                screen.title = "Mascotes"; screen.prefab = prefab;
                EditorUtility.SetDirty(kit); AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(scene); }
        }

        static RoyalPostmanPetsScreen.IncubatorView Incubator(RectTransform stage, int index)
        {
            var slot = Node(stage, "Incubator" + index, (index - 1) * 278, 0, 274, 900);
            var card = Panel(slot, "Panel", 0, -222, 274, 370);
            var egg = Picture(slot, "Egg", "pets-egg-v1", 0, 156, 152, 218, true);
            egg.gameObject.SetActive(false);
            Picture(slot, "Apparatus", "pets-incubator-empty-v1", 0, 170, 290, 466, true);
            var unlocked = Node(card.rectTransform, "Available", 0, 0, 274, 370);
            var track = Panel(unlocked, "ProgressTrack", 0, 137, 222, 37);
            track.pixelsPerUnitMultiplier = 12;
            var fill = Picture(track.rectTransform, "Fill", "pets-panel-v1", 0, 0, 214, 29);
            fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fill.color = new Color(.15f, 1, .22f, 1); fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 0;
            var progress = Label(track.rectTransform, "Value", "0 / 100", 0, 0, 218, 39, 30, White);
            var empty = Node(unlocked, "Empty", 0, 78, 248, 52);
            Label(empty, "Status", "Vazia", 0, 0, 230, 45, 31, White);
            var running = Node(unlocked, "Running", 0, 78, 248, 52);
            var remaining = Label(running, "Time", "", 0, 0, 230, 45, 30, White);
            running.gameObject.SetActive(false);
            var accelerate = ActionButton(unlocked, "Accelerate", "Acelerar", 0, -10, 246, 91, "teal");
            var center = (RectTransform)accelerate.transform.Find("MovingCenter");
            center.Find("Label").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 15);
            center.Find("Label").GetComponent<Text>().fontSize = 32;
            Picture(center, "Hourglass", "pets-hourglass-v1", -29, -22, 29, 39, true);
            Label(center, "Amount", "+10", 23, -23, 90, 34, 27, White);
            accelerate.interactable = false;
            var use = ActionButton(unlocked, "UseItem", "Usar item", 0, -122, 246, 90, "blue");
            var locked = Node(card.rectTransform, "Locked", 0, 0, 274, 370);
            Picture(locked, "Lock", "lock-gray", 0, 74, 74, 80, true);
            Label(locked, "Level", "Nv. " + (index == 1 ? 30 : 100), 0, -7, 250, 64, 43, White);
            Label(locked, "Unlock", "Desbloqueia", 0, -74, 250, 49, 29, White);
            Label(locked, "Rarity", index == 1 ? "Pets Raros" : "Pets Épicos", 0, -118, 250, 49, 29, index == 1 ? Color.cyan : new Color(1, .08f, 1));
            locked.gameObject.SetActive(index > 0); unlocked.gameObject.SetActive(index == 0);
            return new RoyalPostmanPetsScreen.IncubatorView { unlockLevel = index == 0 ? 0 : index == 1 ? 30 : 100,
                egg = egg, available = unlocked.gameObject, empty = empty.gameObject, running = running.gameObject, locked = locked.gameObject,
                progress = progress, remaining = remaining, progressFill = fill, useItem = use, accelerate = accelerate };
        }

        static void BuildInventory(RectTransform body, RoyalPostmanPetsScreen controller)
        {
            var column = Panel(body, "Inventory", 0, 0, 204, 0);
            var r = column.rectTransform; r.anchorMin = new Vector2(1, 0); r.anchorMax = Vector2.one; r.pivot = new Vector2(1, .5f);
            r.offsetMin = new Vector2(-214, 18); r.offsetMax = new Vector2(-10, -330);
            Label(r, "Title", "Inventário", 0, -36, 190, 48, 30, White).rectTransform.anchorMin = new Vector2(.5f, 1);
            var title = r.Find("Title").GetComponent<RectTransform>(); title.anchorMax = new Vector2(.5f, 1); title.anchoredPosition = new Vector2(0, -36);
            var scrollRoot = Node(r, "Scroll", 0, 0, 0, 0); Stretch(scrollRoot); scrollRoot.offsetMin = new Vector2(12, 18); scrollRoot.offsetMax = new Vector2(-20, -74);
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 42; scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = Node(scrollRoot, "Viewport", 0, 0, 0, 0); Stretch(viewport);
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .01f); viewport.gameObject.AddComponent<RectMask2D>();
            var content = Node(viewport, "Content", 0, 0, 0, 0); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 12; layout.padding = new RectOffset(4, 4, 5, 8); layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            string[] sprites = { "pets-egg-v1", "pets-hourglass-v1", "goal-potion", "booster-orb", "pets-hourglass-v1", "goal-potion", "pets-egg-v1", "booster-orb", "pets-hourglass-v1" };
            string[] titles = { "Ovo mágico", "Areia do tempo", "Poção estelar", "Essência mágica", "Areia do tempo", "Poção estelar", "Ovo mágico", "Essência mágica", "Areia do tempo" };
            int[] quantities = { 3, 8, 5, 4, 3, 2, 1, 7, 3 };
            controller.items = new RoyalPostmanPetsScreen.InventoryItem[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                var cell = Panel(content, "Item" + i, 0, 0, 168, 190);
                cell.gameObject.AddComponent<LayoutElement>().preferredHeight = 190;
                var chosen = Panel(cell.rectTransform, "Selected", 0, 0, 168, 190);
                Stretch(chosen.rectTransform); chosen.color = new Color(1, .77f, .1f, .35f); chosen.gameObject.SetActive(false);
                Picture(cell.rectTransform, "Icon", sprites[i], 0, 22, 120, 123, true);
                Label(cell.rectTransform, "Quantity", "x" + quantities[i], 0, -64, 150, 45, 31, White);
                cell.raycastTarget = true;
                var button = cell.gameObject.AddComponent<Button>(); button.targetGraphic = cell; button.transition = Selectable.Transition.ColorTint;
                controller.items[i] = new RoyalPostmanPetsScreen.InventoryItem { id = "sample-" + i, title = titles[i], description = i == 0 || i == 6 ? "Um novo companheiro começa dentro de um ovo." : "Item de apoio para os cuidados com seu mascote.", button = button, selection = chosen.gameObject };
            }
            scroll.viewport = viewport; scroll.content = content; controller.inventory = scroll;
            var barRoot = Node(r, "Scrollbar", -10, -30, 8, 0); barRoot.anchorMin = new Vector2(1, 0); barRoot.anchorMax = Vector2.one; barRoot.offsetMin = new Vector2(-11, 24); barRoot.offsetMax = new Vector2(-5, -82);
            barRoot.gameObject.AddComponent<Image>().color = new Color(.1f, .2f, .4f, .7f);
            var handle = Panel(barRoot, "Handle", 0, 0, 0, 0); Stretch(handle.rectTransform); handle.color = new Color(.4f, .7f, 1);
            var bar = barRoot.gameObject.AddComponent<Scrollbar>(); bar.direction = Scrollbar.Direction.BottomToTop; bar.handleRect = handle.rectTransform; bar.targetGraphic = handle;
            scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }

        static Button Tab(RectTransform body, string name, string text, string icon, float x, float y, bool selected)
        {
            var image = Panel(body, name, x, y, 190, 128); image.rectTransform.anchorMin = image.rectTransform.anchorMax = Vector2.one;
            var glow = Panel(image.rectTransform, "Selected", 0, 0, 190, 128); glow.color = new Color(1, .55f, .03f, .42f); glow.gameObject.SetActive(selected);
            Picture(image.rectTransform, "Icon", icon, 0, 22, 62, 66, true);
            Label(image.rectTransform, "Label", text, 0, -37, 178, 39, 27, White);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.transition = Selectable.Transition.ColorTint; return button;
        }
        static void BuildHeader(RectTransform root, RoyalPostmanPetsScreen controller)
        {
            var header = Node(root, "Header", 0, -65, 1080, 130); header.anchorMin = new Vector2(0, 1); header.anchorMax = Vector2.one; header.sizeDelta = new Vector2(0, 130);
            var shared = RoyalPostmanRuntimeKit.Load().navigation.transform.Find("ContinuousBar").GetComponent<Image>();
            var back = Node(header, "Background", 0, 0, 1080, 130).gameObject.AddComponent<Image>(); Stretch(back.rectTransform); back.sprite = shared.sprite; back.material = shared.material; back.type = Image.Type.Sliced; back.pixelsPerUnitMultiplier = shared.pixelsPerUnitMultiplier; back.rectTransform.localRotation = Quaternion.Euler(0, 0, 180); back.raycastTarget = true;
            Label(header, "Title", "Mascotes", 0, 0, 770, 105, 70, White);
            controller.backButton = Round(header, "Back", -475, "", new Color(.91f, .025f, .08f));
            var glyph = Node((RectTransform)controller.backButton.transform, "Arrow", 0, 0, 62, 62).gameObject.AddComponent<RoyalPostmanGlyph>(); glyph.symbol = RoyalPostmanGlyph.Symbol.BackArrow; glyph.color = White; glyph.raycastTarget = false;
            controller.helpButton = Round(header, "Help", 475, "?", new Color(0, .55f, 1));
        }
        static void BuildHelp(RectTransform root, RoyalPostmanPetsScreen controller)
        {
            var shade = Node(root, "HelpPanel", 0, 0, 0, 0); Stretch(shade); shade.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .75f);
            var card = Panel(shade, "Card", 0, 10, 820, 560);
            Label(card.rectTransform, "Title", "Incubadoras", 0, 190, 720, 75, 59, Gold);
            Label(card.rectTransform, "Copy", "Os ovos aparecem nas cápsulas somente\ndurante uma incubação ativa.\n\nEscolha os itens no inventário à direita.\nAs incubadoras extras são liberadas\nconforme você avança nas fases.", 0, 10, 712, 292, 33, White);
            controller.dismissHelp = ActionButton(card.rectTransform, "Close", "Entendi", 0, -200, 330, 95, "blue");
            controller.helpPanel = shade.gameObject; shade.gameObject.SetActive(false);
        }
        static Button Round(RectTransform parent, string name, float x, string text, Color target)
        {
            var image = Picture(parent, name, "top-button-body-v15", x, 0, 104, 104, true);
            string folder = "Assets/RoyalAves/Art/UI/Button/RoyalPostman/Pets/Materials"; Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            string path = folder + "/pets-" + name.ToLowerInvariant() + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("RoyalPostman/UI/PaintedPalette")); AssetDatabase.CreateAsset(material, path); }
            material.SetFloat("_UsePalette", 0); material.SetFloat("_ButtonPart", 0); material.SetFloat("_RecolorPurple", 1); material.SetColor("_TargetColor", target); EditorUtility.SetDirty(material); image.material = material;
            if (text.Length > 0) Label(image.rectTransform, "Label", text, 0, 1, 90, 98, 75, White);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.transition = Selectable.Transition.ColorTint; return button;
        }
        static Button ActionButton(RectTransform parent, string name, string text, float x, float y, float w, float h, string paint)
        {
            var root = Node(parent, name, x, y, w, h);
            var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var gold = Picture(root, "FixedGold", "level-base-v6", 0, 0, w, h);
            gold.type = Image.Type.Sliced; gold.pixelsPerUnitMultiplier = 5;
            gold.material = AssetDatabase.LoadAssetAtPath<Material>(RoyalPostmanArtPaths.Material("home-fixed-gold-v6"));
            var center = Node(root, "MovingCenter", 0, 0, w, h);
            var face = Picture(center, "Face", "level-base-v6", 0, 0, w, h);
            face.type = Image.Type.Sliced; face.pixelsPerUnitMultiplier = 5;
            face.material = AssetDatabase.LoadAssetAtPath<Material>(RoyalPostmanArtPaths.Material("home-moving-center-v6"));
            face.color = paint == "blue" ? new Color(.25f, .75f, 1) : new Color(.35f, 1, .12f);
            Label(center, "Label", text, 0, 0, w - 25, h - 12, 34, White);
            var button = root.gameObject.AddComponent<Button>(); button.targetGraphic = face; button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors; colors.disabledColor = new Color(.65f, .69f, .76f, 1); button.colors = colors;
            var press = root.gameObject.AddComponent<RoyalPostmanPaintedButton>(); press.movingCenter = center; press.pressTravel = 3;
            return button;
        }
        internal static void Import(string group, string id)
        {
            string path = "Assets/RoyalAves/Art/UI/" + group + "/RoyalPostman/Pets/" + id + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) throw new InvalidOperationException("Texture did not load: " + path);
            var read = new Texture2D(2, 2, TextureFormat.RGBA32, false); ImageConversion.LoadImage(read, File.ReadAllBytes(path));
            Rect rect = new Rect(0, 0, read.width, read.height);
            if (!id.StartsWith("pets-workshop-"))
            {
                var pixels = read.GetPixels32(); int left = read.width, right = 0, bottom = read.height, top = 0;
                for (int y = 0; y < read.height; y++) for (int x = 0; x < read.width; x++) if (pixels[y * read.width + x].a > 8) { left = Math.Min(left, x); right = Math.Max(right, x); bottom = Math.Min(bottom, y); top = Math.Max(top, y); }
                rect = Rect.MinMaxRect(left, bottom, right + 1, top + 1);
            }
            UnityEngine.Object.DestroyImmediate(read);
            var border = id.StartsWith("pets-panel-") ? Vector4.one * 142 : Vector4.zero;
            var sprite = UnityEngine.Sprite.Create(texture, rect, Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect, border);
            if (sprite == null) throw new InvalidOperationException("Sprite creation failed: " + id + " source=" + texture.width + "x" + texture.height + " rect=" + rect);
            sprite.name = id;
            string folder = Path.GetDirectoryName(path).Replace('\\', '/') + "/Sprites"; Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            var spritePath = folder + "/" + id + ".asset"; var previous = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (previous == null) AssetDatabase.CreateAsset(sprite, spritePath);
            else { EditorUtility.CopySerialized(sprite, previous); EditorUtility.SetDirty(previous); UnityEngine.Object.DestroyImmediate(sprite); }
        }
        static Sprite Sprite(string id) => AssetDatabase.LoadAssetAtPath<Sprite>(RoyalPostmanArtPaths.Sprite(id)) ?? throw new InvalidOperationException("Missing sprite: " + id);
        static RectTransform Node(RectTransform parent, string name, float x, float y, float w, float h)
        {
            var r = (RectTransform)new GameObject(name, typeof(RectTransform)).transform; r.SetParent(parent, false); r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.pivot = Vector2.one * .5f; r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h); return r;
        }
        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        static Image Picture(RectTransform parent, string name, string sprite, float x, float y, float w, float h, bool aspect = false)
        {
            var image = Node(parent, name, x, y, w, h).gameObject.AddComponent<Image>(); image.sprite = Sprite(sprite); image.preserveAspect = aspect; image.raycastTarget = false; return image;
        }
        static Image Panel(RectTransform parent, string name, float x, float y, float w, float h)
        {
            var image = Node(parent, name, x, y, w, h).gameObject.AddComponent<Image>(); image.sprite = panel; image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 4; image.raycastTarget = false; return image;
        }
        static Text Label(RectTransform parent, string name, string text, float x, float y, float w, float h, int size, Color color)
        {
            var label = Node(parent, name, x, y, w, h).gameObject.AddComponent<Text>(); label.font = font; label.text = text; label.fontSize = size; label.color = color; label.alignment = TextAnchor.MiddleCenter; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Overflow; label.raycastTarget = false;
            var shadow = label.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(.02f, .01f, .06f, .8f); shadow.effectDistance = new Vector2(1.5f, -2.5f); return label;
        }
    }
}
