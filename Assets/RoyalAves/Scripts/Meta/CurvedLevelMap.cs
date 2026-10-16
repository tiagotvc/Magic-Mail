using System;
using RoyalAves.Meta.WorldMap;
using SweetSugar.Scripts.MapScripts;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public sealed class CurvedLevelMap : MonoBehaviour
    {
        sealed class Node
        {
            public RectTransform rect;
            public MaskableGraphic face;
            public Button button;
            public TextMeshProUGUI number;
            public Image star;
            public int level;
        }

        sealed class Prop
        {
            public RectTransform rect;
            public CurvedMapPropImage image;
        }

        LevelMapConfig config;
        WorldMapView world; // mapa 3D (camera -> RenderTexture); null = mapa 2D anterior como reserva
        public WorldMapView World => world;
        WorldMapAtmosphere atmosphere;
        WorldMapHudPresentation hudPresentation;
        WorldMapSantaBalloon santaBalloon;
        public event Action<bool> SantaEventCaught;
        public bool SantaBalloonVisible=>santaBalloon!=null&&santaBalloon.Visible;
        public bool NeedsPreviewFrames=>world!=null&&(world.SantaNeedsFrames||world.ChristmasNeedsFrames||world.NightNeedsFrames||world.IceNeedsFrames||world.MailboxNeedsFrames||world.CharacterNeedsFrames||world.AdornmentNeedsFrames||SantaBalloonVisible);
        LobbyFeaturesConfig art;
        RectTransform viewport;
        CurvedMapRoad road;
        CurvedMapTerrain terrain;
        Prop[] props;
        RectTransform nearScenery, farScenery;
        Material silhouetteMaterial;
        Image sky;
        Image foreground;
        Node[] nodes;
        RectTransform marker;
        TextMeshProUGUI title, currentLabel, playLabel;
        Button currentButton, playButton;
        Action<int> selected;
        Func<int> getNextLevel;
        int levelCount, nextLevel;
        float cursor, targetCursor, velocity;
        bool dragging;
        Vector2 lastSize;
        float lastClickBlocked;
        public bool Visible => gameObject.activeSelf;
        public int NextLevel => nextLevel;
        public float Cursor => cursor;
        public void BindHudButtons(Button avatar,Button coins,Button lives,Button settings,Button[] navigation)=>hudPresentation?.BindButtons(avatar,coins,lives,settings,navigation);
        public void SelectNavigationTab(int tab)=>hudPresentation?.SelectTab(tab);
        public void SetChromeVisible(bool visible)=>hudPresentation?.SetChromeVisible(visible);

        public static CurvedLevelMap Create(RectTransform parent, RectTransform nav, LevelMapConfig config,
            LobbyFeaturesConfig art, int levelCount, Func<int> nextLevel, Action<int> selected, Action openAgency, WorldMapKit kitOverride=null,WorldMapIslands islandsOverride=null)
        {
            var root = Rect("MapaDeNiveis", parent);
            Stretch(root);
            var map = root.gameObject.AddComponent<CurvedLevelMap>();
            map.config = config;
            map.art = art;
            map.levelCount = Mathf.Max(0, levelCount);
            map.getNextLevel = nextLevel;
            map.selected = selected;
            var skyPanel = Rect("CeuDistante", root);
            Stretch(skyPanel);
            var kit = config.useWorldMap ? (kitOverride??Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath)) : null;
            var worldRect = kit != null && kit.Ready && LayerMask.NameToLayer(WorldMapKit.LayerName) >= 0 ? Rect("Mundo3D", root) : null;
            skyPanel.anchorMin = worldRect != null ? Vector2.zero : new Vector2(0, 0.58f);
            var back = Rect("Paisagem", skyPanel);
            Stretch(back);
            var image = back.gameObject.AddComponent<Image>();
            map.sky = image;
            image.sprite = config.background;
            image.raycastTarget = false;
            if (config.background != null)
            {
                var fit = back.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = config.background.rect.width / config.background.rect.height;
            }
            var bottom = nav != null ? Mathf.Max(180, nav.rect.height) : 220;
            if (worldRect != null)
            {
                Stretch(worldRect);
                map.world = WorldMapView.Create(worldRect, art, kit, map.levelCount, 0,!Application.isPlaying,islandsOverride);
                if (map.world == null) Destroy(worldRect.gameObject);
                else
                {
                    map.viewport = worldRect;
                    map.world.LevelExists=WorldMapLevelCatalog.Exists;
                    worldRect.gameObject.AddComponent<CurvedMapInput>().Map = map;
                }
            }
            if (map.world == null) map.BuildFlatWorld(root, bottom);
            if (map.world != null) map.atmosphere = WorldMapAtmosphere.Create(root, skyPanel, map.sky, kit,map.world.Route);
            map.BuildHud(root, nav, bottom, openAgency);
            if(map.world!=null)map.santaBalloon=new WorldMapSantaBalloon(root,art);
            root.gameObject.SetActive(false);
            return map;
        }

        // Mapa 2D anterior (reserva quando o kit 3D / renderer / layer nao estao configurados).
        void BuildFlatWorld(RectTransform root, float bottom)
        {
            var map = this;
            map.viewport = Rect("Horizonte", root);
            Stretch(map.viewport);
            map.viewport.offsetMin = new Vector2(0, bottom + 155);
            map.viewport.offsetMax = new Vector2(0, -175);
            // A transparent raycast surface allows dragging on the grass, too.
            map.viewport.gameObject.AddComponent<Image>().color = Color.clear;
            map.viewport.gameObject.AddComponent<RectMask2D>();
            map.viewport.gameObject.AddComponent<CurvedMapInput>().Map = map;
            map.farScenery = Rect("ObjetosAtrasDaCurva", map.viewport);
            Stretch(map.farScenery);
            var shader = Shader.Find("RoyalAves/UI Map Silhouette");
            if (config.horizonMaterial != null) map.silhouetteMaterial = new Material(config.horizonMaterial);
            else if (shader != null) map.silhouetteMaterial = new Material(shader);
            var groundRect = Rect("TerrenoCurvo", map.viewport);
            Stretch(groundRect);
            map.terrain = groundRect.gameObject.AddComponent<CurvedMapTerrain>();
            map.terrain.Config = config;
            map.terrain.LevelCount = map.levelCount;
            map.terrain.raycastTarget = false;
            var roadRect = Rect("Estrada", map.viewport);
            Stretch(roadRect);
            map.road = roadRect.gameObject.AddComponent<CurvedMapRoad>();
            map.road.Config = config;
            map.road.LevelCount = map.levelCount;
            map.road.raycastTarget = false;

            map.nearScenery = Rect("ObjetosIndependentes", map.viewport);
            Stretch(map.nearScenery);
            map.props = new Prop[22];
            for (var i = map.props.Length - 1; i >= 0; i--)
            {
                var propRect = Rect("Objeto", map.nearScenery);
                propRect.pivot = new Vector2(0.5f, 0.12f);
                var shadowRect = Rect("Sombra", propRect);
                shadowRect.anchorMin = shadowRect.anchorMax = new Vector2(0.65f, 0.12f);
                shadowRect.sizeDelta = new Vector2(210, 30);
                var shadow = shadowRect.gameObject.AddComponent<MapPropShadow>();
                shadow.color = new Color(0.21f, 0.10f, 0.35f, 0.20f);
                shadow.raycastTarget = false;
                var faceRect = Rect("Sprite", propRect);
                Stretch(faceRect);
                var propImage = faceRect.gameObject.AddComponent<CurvedMapPropImage>();
                propImage.preserveAspect = true;
                propImage.raycastTarget = false;
                map.props[i] = new Prop { rect = propRect, image = propImage };
            }

            map.nodes = new Node[config.visibleLevels + 2];
            // Far nodes draw first; foreground nodes draw over the road and distant nodes.
            for (var i = map.nodes.Length - 1; i >= 0; i--) map.nodes[i] = map.CreateNode();
            map.marker = Rect("VoceEstaAqui", map.viewport);
            var portrait = map.marker.gameObject.AddComponent<Image>();
            portrait.sprite = art != null && art.avatars.Count > 0 ? art.avatars[0] : null;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            map.currentLabel = map.Text(map.marker, "VOCÊ", 24);
            map.currentLabel.rectTransform.anchoredPosition = new Vector2(0, 54);
            map.currentLabel.rectTransform.sizeDelta = new Vector2(160, 42);
            var foreground = Rect("TerrenoInferior", root);
            foreground.SetSiblingIndex(map.viewport.GetSiblingIndex());
            foreground.anchorMin = Vector2.zero;
            foreground.anchorMax = new Vector2(1, 0);
            foreground.pivot = new Vector2(0.5f, 0);
            foreground.sizeDelta = new Vector2(0, bottom + 155);
            map.foreground = foreground.gameObject.AddComponent<Image>();
            map.foreground.raycastTarget = false;
        }

        void BuildHud(RectTransform root, RectTransform nav, float bottom, Action openAgency)
        {
            var map = this;
            if(world!=null)
            {
                hudPresentation=new WorldMapHudPresentation(root.parent as RectTransform,nav,world.CameraModel.Kit,art!=null?art.displayFont:null);
                if(!WorldMapTopHud.Ready(world.CameraModel.Kit))
                {
                    var plate=Rect("PainelSuperiorDoMapa",root);plate.anchorMin=new Vector2(0,1);plate.anchorMax=Vector2.one;
                    plate.pivot=new Vector2(.5f,1);plate.sizeDelta=new Vector2(0,238);
                    plate.anchoredPosition=Vector2.zero;plate.gameObject.AddComponent<CanvasRenderer>();
                    var graphic=plate.gameObject.AddComponent<WorldMapPanelGraphic>();graphic.raycastTarget=false;
                }
            }
            var heading = Rect("Titulo", root);
            heading.anchorMin = heading.anchorMax = new Vector2(0.5f, 1);
            heading.anchoredPosition = new Vector2(0, -275);
            heading.sizeDelta = new Vector2(820, 64);
            var headingBack = heading.gameObject.AddComponent<Image>();
            headingBack.sprite = art != null ? art.pill : null;
            headingBack.color = new Color(0.55f, 0.36f, 0.70f, 0.90f);
            headingBack.raycastTarget = false;
            map.title = map.Text(heading, "ROTA DO CORREIO", 42);
            map.title.enableAutoSizing = true;
            map.title.fontSizeMin = 24;
            map.title.fontSizeMax = 42;
            map.title.textWrappingMode = TextWrappingModes.NoWrap;
            Stretch(map.title.rectTransform);
            heading.gameObject.SetActive(world==null);
            var controls = Rect("AcoesDoMapa", root);
            controls.anchorMin = new Vector2(0, 0);
            controls.anchorMax = new Vector2(1, 0);
            controls.pivot = new Vector2(0.5f, 0);
            controls.sizeDelta = new Vector2(0, 80);
            controls.anchoredPosition = new Vector2(0, bottom + 12);
            map.currentButton = map.MakeButton(controls, WorldMapMission100.English("Meu nível"), new Vector2(0.85f, 0.5f), new Vector2(220, 76),
                art != null ? art.pill : null, () => map.FocusCurrent());
            map.playButton = map.MakeButton(controls, "Jogar", new Vector2(0.82f, 0.5f), new Vector2(340, 126),
                art != null ? art.buttonGreen : null, () => map.Select(map.nextLevel));
            map.playLabel = map.playButton.GetComponentInChildren<TextMeshProUGUI>();
            if(world!=null)map.playButton.gameObject.SetActive(false);
        }

        Node CreateNode()
        {
            var node = new Node { rect = Rect("Nivel", viewport) };
            if (config.levelButton != null)
            {
                var image = node.rect.gameObject.AddComponent<Image>();
                image.sprite = config.levelButton;
                image.preserveAspect = true;
                node.face = image;
            }
            else node.face = node.rect.gameObject.AddComponent<MapLevelDisc>();
            node.button = node.rect.gameObject.AddComponent<Button>();
            node.button.targetGraphic = node.face;
            node.button.navigation = new Navigation { mode = Navigation.Mode.None };
            node.rect.gameObject.AddComponent<CurvedMapInput>().Map = this;
            node.button.onClick.AddListener(() => Select(node.level));
            node.number = Text(node.rect, "", 50);
            Stretch(node.number.rectTransform);
            var star = Rect("EstrelaGanha", node.rect);
            star.anchoredPosition = new Vector2(0, -40);
            star.sizeDelta = new Vector2(48, 48);
            node.star = star.gameObject.AddComponent<Image>();
            node.star.sprite = art != null ? art.star : null;
            node.star.preserveAspect = true;
            node.star.raycastTarget = false;
            return node;
        }

        // Sweet Sugar's stars and Magic Mail's highest won level use their existing saves.
        // Taking the maximum also supports old Sweet Sugar players who have no meta save yet.
        public static int ReachedLevel(int count)
        {
            var reached = MetaProgress.NextLevel;
            for (var i = 1; i <= count; i++)
                if (LevelsMap._mapProgressManager.LoadLevelStarsCount(i) > 0) reached = Mathf.Max(reached, i + 1);
            return Mathf.Clamp(reached, 1, Mathf.Max(1, count + 1));
        }

        public void Open(bool focus = true)
        {
            nextLevel = Mathf.Clamp(getNextLevel(), 1, Mathf.Max(1, levelCount + 1));
            nextLevel=WorldMapLevelCatalog.NextExisting(nextLevel);
            gameObject.SetActive(true);
            hudPresentation?.Show();
            playButton.interactable = nextLevel <= levelCount;
            playLabel.text = nextLevel <= levelCount ? $"Nível {nextLevel}" : "Concluído!";
            title.text = levelCount == 0 ? "NOVAS ROTAS EM BREVE" : "ROTA DO CORREIO";
            if (focus) FocusCurrent(true);
            Draw();
        }

        public void Close()
        {
            velocity = 0;
            dragging = false;
            gameObject.SetActive(false);
            hudPresentation?.Hide();
        }

        // O mundo 3D so renderiza (camera + RenderTexture) enquanto o mapa esta visivel.
        void OnEnable() => world?.SetActive(true);
        void OnDisable(){world?.SetActive(false);hudPresentation?.Hide();santaBalloon?.Hide();}

        void FocusCurrent(bool immediately = false)
        {
            velocity = 0;
            targetCursor = Clamp(world!=null?world.CursorForLevel(Mathf.Min(nextLevel,levelCount)):Mathf.Min(nextLevel,levelCount)-.75f);
            if (immediately) cursor = targetCursor;
        }

        // No mapa 3D da para rolar ate espiar as primeiras posicoes futuras (bloqueadas) alem das fases reais.
        float Clamp(float value) =>
            Mathf.Clamp(value, 1, Mathf.Max(1, world != null ? world.CursorForLevel(Mathf.Max(1,levelCount))+.15f : levelCount - 3));

        public bool CanSelect(int level) => level >= 1 && level <= levelCount && level <= nextLevel && WorldMapLevelCatalog.Exists(level);

        void Select(int level)
        {
            if (dragging || Time.unscaledTime < lastClickBlocked || !CanSelect(level)) return;
            velocity = 0;
            selected?.Invoke(level); // LobbyController uses the original Sweet Sugar pre-level loader.
        }

        void Update()
        {
            AdvanceMotion(Time.unscaledDeltaTime);
        }

        void AdvanceMotion(float deltaTime)
        {
            if (!dragging)
            {
                if (Mathf.Abs(velocity) > 0.01f)
                {
                    targetCursor = Clamp(targetCursor + velocity * deltaTime);
                    velocity *= Mathf.Exp(-6 * deltaTime);
                }
                cursor = Mathf.Lerp(cursor, targetCursor, 1 - Mathf.Exp(-12 * deltaTime));
            }
            Draw();
        }

        void Draw()
        {
            var size = viewport.rect.size;
            if (size.x < 1 || size.y < 1) return;
            if (world != null)
            {
                DrawWorld();
                return;
            }
            if (lastSize != size || Mathf.Abs(road.Cursor - cursor) > 0.0001f)
            {
                road.Cursor = cursor;
                road.SetVerticesDirty();
                terrain.Cursor = cursor;
                terrain.SetVerticesDirty();
                lastSize = size;
            }
            var region = config.RegionAt(cursor + 2);
            if (region != null)
            {
                sky.color = Color.Lerp(sky.color, region.skyTint, 0.06f);
                foreground.color = (config.RegionAt(cursor)?.ground ?? region.ground) * new Color(0.94f, 0.94f, 0.94f, 1);
                var chapter = Mathf.FloorToInt((cursor + 1) / config.levelsPerRegion);
                var start = chapter * config.levelsPerRegion + 1;
                title.text = $"{region.title}  •  {start}–{Mathf.Min(start + config.levelsPerRegion - 1, levelCount)}";
            }
            DrawProps(size);
            var first = Mathf.FloorToInt(cursor);
            for (var i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                var level = first + i;
                var distance = level - cursor;
                var shown = level <= levelCount && distance >= -0.5f && distance < config.curveDepth;
                node.rect.gameObject.SetActive(shown);
                if (!shown) continue;
                if (node.level != level)
                {
                    node.level = level;
                    node.rect.name = "Nivel_" + level;
                    node.number.text = level.ToString();
                }
                node.star.gameObject.SetActive(level < nextLevel && art != null && art.star != null);
                var scale = LevelMapProjection.Scale(distance, config);
                var facing = Mathf.Lerp(0.12f, 1, LevelMapProjection.SurfaceFacing(distance, config));
                node.rect.anchorMin = node.rect.anchorMax = Vector2.zero;
                node.rect.anchoredPosition = LevelMapProjection.Point(distance, cursor, size, config);
                node.rect.sizeDelta = Vector2.one * config.nodeSize;
                var active = level == nextLevel;
                var pulse = active && !dragging ? 1 + 0.045f * Mathf.Sin(Time.unscaledTime * 3) : 1;
                node.rect.localScale = new Vector3(1, facing, 1) * scale * (active ? 1.16f * pulse : 1);
                node.rect.localRotation = Quaternion.Euler(0, 0,
                    LevelMapProjection.SurfaceRoll(distance, node.rect.anchoredPosition.x, size, config));
                node.button.interactable = CanSelect(level) && config.nodeSize * scale * facing >= 28;
                node.face.color = level > nextLevel ? config.lockedTint : Color.white;
                node.number.color = level > nextLevel ? new Color32(232, 232, 240, 255) : Color.white;
            }
            var currentDistance = nextLevel - cursor;
            marker.gameObject.SetActive(nextLevel <= levelCount && currentDistance >= -0.3f && currentDistance < 10);
            if (marker.gameObject.activeSelf)
            {
                var scale = LevelMapProjection.Scale(currentDistance, config);
                marker.anchorMin = marker.anchorMax = Vector2.zero;
                marker.anchoredPosition = LevelMapProjection.Point(currentDistance, cursor, size, config)
                    + new Vector2(-98, 12) * scale;
                marker.sizeDelta = new Vector2(80, 100);
                marker.localScale = Vector3.one * scale;
            }
        }

        void DrawWorld()
        {
            hudPresentation?.Tick();
            world.Tick(cursor, nextLevel, dragging);
            santaBalloon?.Tick(world.Clock);
            if(currentButton!=null)
            {
                var visible=world.ProjectLevel(nextLevel,cursor,viewport.rect.width/viewport.rect.height,out var p)&&p.y>.14f&&p.y<.76f;
                currentButton.gameObject.SetActive(!visible);
            }
            if (atmosphere != null) atmosphere.Night = world.Night;
            atmosphere?.Tick(cursor, world.TimeOverride>=0?world.TimeOverride:Time.realtimeSinceStartup);
            var palette = world.Route.PaletteAt(world.Route.MetersAt(cursor) + 18);
            if (atmosphere == null) sky.color = Color.Lerp(sky.color, Color.Lerp(Color.white, palette.haze, 0.55f), 0.06f);
            var start = world.ChapterStart(cursor);
            title.text = $"{world.ChapterTitle(cursor)}  •  {start}–{start + WorldMapEditorial.LevelsPerChapter - 1}";
        }

        void OnDestroy()
        {
            if (world != null) world.Dispose();
            if (silhouetteMaterial != null)
            {
                if (Application.isPlaying) Destroy(silhouetteMaterial);
                else DestroyImmediate(silhouetteMaterial);
            }
        }

        void DrawProps(Vector2 size)
        {
            const float spacing = 1.6f;
            var first = Mathf.FloorToInt(cursor / spacing);
            for (var i = 0; i < props.Length; i++)
            {
                var index = first + i;
                var level = index * spacing;
                var d = level - cursor;
                var region = config.RegionAt(level);
                var prop = props[i];
                var hash = Mathf.Abs(Mathf.Sin(index * 127.1f) * 43758.5453f) % 1;
                var person = index % 13 == 0;
                var building = hash < (region != null ? region.buildings : 0);
                var sprite = region != null ? person && region.character != null ? region.character
                    : building ? region.building : region.tree : null;
                var shown = sprite != null && d >= -0.5f && d < config.curveDepth + 9 && level <= levelCount + 1;
                prop.rect.gameObject.SetActive(shown);
                if (!shown) continue;
                // The same objects emerge from behind the tangent as the world rolls forward.
                // Drawing far props before the terrain lets the curved ground hide their bases.
                var behindCurve = d > config.curveDepth;
                var parent = behindCurve ? farScenery : nearScenery;
                if (prop.rect.parent != parent) prop.rect.SetParent(parent, false);
                prop.image.sprite = sprite;
                var raise = Mathf.SmoothStep(0, 1,
                    Mathf.InverseLerp(config.curveDepth * 0.20f, config.curveDepth * 1.08f, d));
                prop.image.Tilt = config.horizonObjectTilt * raise;
                prop.image.material = behindCurve ? silhouetteMaterial : null;
                prop.image.color = behindCurve ? new Color(0.21f, 0.56f, 0.60f, 0.32f) : region.propTint;
                var scale = LevelMapProjection.Scale(d, config);
                var roadPoint = LevelMapProjection.Point(d, cursor, size, config);
                var side = index % 2 == 0 ? -1 : 1;
                var x = roadPoint.x + side * size.x * (0.34f + hash * 0.10f) * scale;
                prop.rect.anchorMin = prop.rect.anchorMax = Vector2.zero;
                prop.rect.anchoredPosition = new Vector2(x, LevelMapProjection.GroundY(d, x, size, config));
                var baseSize = person ? 150 : building ? 330 : 290;
                prop.rect.sizeDelta = new Vector2(baseSize, baseSize * sprite.rect.height / sprite.rect.width);
                var phase = Time.unscaledTime * (person ? 2.4f : 1.2f) + index;
                var factoryPulse = building && sprite.name.Contains("factory") ? 1 + Mathf.Sin(phase * 1.6f) * 0.016f : 1;
                prop.rect.localScale = Vector3.one * scale * factoryPulse;
                var roll = LevelMapProjection.SurfaceRoll(d, x, size, config) * 0.65f;
                prop.rect.localRotation = Quaternion.Euler(0, 0, roll + (building ? 0 : Mathf.Sin(phase) * (person ? 2 : 1.8f)));
                if (person) prop.rect.anchoredPosition += new Vector2(0, Mathf.Sin(phase) * 5 * scale);
                // Sorting follows distance: a foreground tree can cover a distant building.
                prop.rect.SetAsFirstSibling();
            }
        }

        public void BeginDrag(PointerEventData e)
        {
            dragging = true;
            velocity = 0;
            e.eligibleForClick = false;
        }

        public void Drag(PointerEventData e)
        {
            e.eligibleForClick = false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, e.position, e.pressEventCamera, out var now)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, e.position - e.delta, e.pressEventCamera, out var before)) return;
            var movement = (before.y - now.y) / (world != null ? Mathf.Max(60f, viewport.rect.height * 0.11f) : 95f);
            MoveDrag(movement,Time.unscaledDeltaTime);
        }

        // Shared by touch/mouse input in Play and the Simulator's disposable editor preview.
        public void DragByViewport(float verticalFraction,float deltaTime)
        {
            MoveDrag(-verticalFraction/0.11f,deltaTime);
        }

        void MoveDrag(float movement,float deltaTime)
        {
            if(!dragging)return;
            var previous=targetCursor;
            targetCursor = Clamp(targetCursor + movement);
            cursor = targetCursor;
            velocity = Mathf.Clamp((targetCursor-previous) / Mathf.Max(0.016f, deltaTime), -16, 16);
            Draw();
        }

        public void EndDrag(PointerEventData e)
        {
            dragging = false;
            e.eligibleForClick = false;
            lastClickBlocked = Time.unscaledTime + 0.15f;
        }

        // Clique no mundo 3D: raio da camera do mapa sobre o collider do medalhao.
        public void Click(PointerEventData e)
        {
            if(world!=null&&world.ScreenToViewport(e.position,e.pressEventCamera,out var viewport))ClickViewport(viewport);
        }
        public bool TryCatchSanta(Vector2 viewport)
        {
            if(world==null||dragging||Time.unscaledTime<lastClickBlocked||!world.CatchSanta(viewport,out var evil,out var anchor))return false;
            var kit=world.CameraModel.Kit;
            santaBalloon?.Show(evil?kit.santaVillainMessage:kit.santaFriendlyMessage,anchor,world.Clock);
            SantaEventCaught?.Invoke(evil);return true;
        }
        public void ClickViewport(Vector2 viewport)
        {
            if(world==null||dragging||Time.unscaledTime<lastClickBlocked)return;
            if(TryCatchSanta(viewport))return;
            var level = world.PickViewport(viewport);
            if (level > 0) Select(level);
        }

        public void Scroll(float delta)
        {
            targetCursor = Clamp(targetCursor - delta * 0.85f);
            velocity = 0;
        }

#if UNITY_EDITOR
        public bool PreviewMotion(float deltaTime)
        {
            AdvanceMotion(Mathf.Clamp(deltaTime,0,.05f));
            world?.RenderNow();
            return NeedsPreviewFrames||!dragging&&(Mathf.Abs(velocity)>.01f||Mathf.Abs(cursor-targetCursor)>.001f);
        }

        public void PreviewAt(float levelCursor)
        {
            cursor = targetCursor = world!=null?Mathf.Clamp(levelCursor,1,world.Route.LevelSlots+1.5f):Clamp(levelCursor);
            velocity = 0;
            Draw();
            if (world != null) world.RenderNow(); // fora do play mode a camera do mundo nao renderiza sozinha
        }
#endif

        Button MakeButton(RectTransform parent, string caption, Vector2 anchor, Vector2 size, Sprite sprite, Action click)
        {
            var rect = Rect(caption, parent);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            var face = rect.gameObject.AddComponent<Image>();
            face.sprite = sprite;
            face.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.onClick.AddListener(() => click?.Invoke());
            var label = Text(rect, caption, 38);
            Stretch(label.rectTransform);
            return button;
        }

        TextMeshProUGUI Text(RectTransform parent, string value, float size)
        {
            var rect = Rect("Texto", parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (art != null && art.displayFont != null) text.font = art.displayFont;
            text.text = value;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.fontStyle = FontStyles.Bold;
            var shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.20f, 0.08f, 0.31f, 0.8f);
            shadow.effectDistance = new Vector2(0, -3);
            return text;
        }

        static RectTransform Rect(string name, RectTransform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)) { layer = parent.gameObject.layer }.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
