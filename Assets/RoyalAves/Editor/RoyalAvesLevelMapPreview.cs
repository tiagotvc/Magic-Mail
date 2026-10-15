using System;
using System.IO;
using System.Linq;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    // A preview scene keeps the open scene, prefab overrides and player saves untouched.
    public sealed class RoyalAvesLevelMapPreview : EditorWindow
    {
        Scene scene;
        Camera camera;
        RenderTexture capture;
        CurvedLevelMap map;
        float level = 1;
        int count;
        int selections;
        int previewReached;
        WorldMapKit previewKit;
        bool needsRebuild;
        WorldMapIslands captureIslands;
        Vector2 settingsScroll,previewPan;
        float sidebarWidth=420,previewZoom=1;
        bool resizing,panning;
        int seasonPreview;
        readonly RoyalAvesMapIslandControls islandControls=new RoyalAvesMapIslandControls();
        readonly RoyalAvesMapLayoutControls layoutControls=new RoyalAvesMapLayoutControls();
        static bool creatingScenePreview;
        bool scenePreview;
        bool santaSettings;
        PointerEventData scenePointer;

        // The Simulator can show the same saved map before Play without editing game.unity.
        internal static RoyalAvesLevelMapPreview CreateScenePreview(int width,int height)
        {
            RoyalAvesLevelMapPreview preview=null;
            creatingScenePreview=true;
            try
            {
                preview=CreateInstance<RoyalAvesLevelMapPreview>();
                preview.hideFlags=HideFlags.HideAndDontSave;
                preview.scenePreview=true;
                preview.Build(width,height,1);
                return preview;
            }
            catch {if(preview!=null)DestroyImmediate(preview);throw;}
            finally {creatingScenePreview=false;}
        }

        internal Texture SceneTexture => capture;
        internal bool NeedsSceneAnimation=>map!=null&&map.NeedsPreviewFrames;
        internal void ClickScenePreview(Vector2 viewport){map.ClickViewport(viewport);RenderCanvas();}
        internal void RenderScenePreview() => RenderScenePreview(0);
        internal bool RenderScenePreview(float deltaTime)
        {
            var moving=map.PreviewMotion(deltaTime);
            level=map.Cursor;
            RenderCanvas();
            return moving;
        }
        internal void BeginSceneDrag(){scenePointer=new PointerEventData(null);map.BeginDrag(scenePointer);}
        internal void DragScenePreview(float fraction,float deltaTime)=>map.DragByViewport(fraction,deltaTime);
        internal void EndSceneDrag(){if(scenePointer!=null)map.EndDrag(scenePointer);}
        internal void ScrollScenePreview(float delta)=>map.Scroll(delta);

        [MenuItem("Royal Aves/Mapa de níveis/Pré-visualizar mundo curvo")]
        static void Open() => GetWindow<RoyalAvesLevelMapPreview>("Mapa de níveis").Show();

        [MenuItem("Royal Aves/Mapa de níveis/Selecionar configuração")]
        static void SelectConfig() => Selection.activeObject = RoyalAvesMapIslandControls.GetOrCreate();

        void OnEnable()
        {
            if(creatingScenePreview)return;
            minSize=new Vector2(720,480);
            Undo.undoRedoPerformed+=OnLayoutUndo;
            try { Build(1080, 1920, 1); }
            catch (Exception error) { Debug.LogException(error); }
        }

        void Build(int width, int height, int reached)
        {
            Cleanup();
            if(!scenePreview)RoyalAvesMapLibrary.Ensure(RoyalAvesWorldMapSetup.Ensure(false));
            previewKit=Instantiate(Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath));
            previewKit.hideFlags=HideFlags.HideAndDontSave;
            previewKit.seasonPreview=seasonPreview;
            previewReached = reached;
            var config = AssetDatabase.LoadAssetAtPath<LevelMapConfig>("Assets/RoyalAves/Resources/RoyalAvesLevelMap.asset");
            var art = AssetDatabase.LoadAssetAtPath<LobbyFeaturesConfig>("Assets/RoyalAves/Resources/RoyalAvesFeatures.asset");
            if (config == null || previewKit==null || !previewKit.Ready)
                throw new InvalidOperationException("Configuração ou sprites do mapa não importados.");
            WorldMapLevelCatalog.Refresh();count=WorldMapLevelCatalog.LastLevel;
            scene = EditorSceneManager.NewPreviewScene();
            var cameraObject = new GameObject("Camera do preview");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene;
            camera.enabled = false;
            camera.orthographic = true;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            capture = new RenderTexture(width, height, 24);
            capture.Create();
            camera.targetTexture = capture;
            camera.aspect = width / (float)height;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RoyalAves/Prefabs/RoyalAvesLobby.prefab");
            var root = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)
                : new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            if (root.scene != scene) SceneManager.MoveGameObjectToScene(root, scene);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 5;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0;
            var content = root.transform.Find("Content") as RectTransform;
            if (content == null) content = (RectTransform)root.transform;
            content.gameObject.SetActive(true);
            foreach (var child in content.GetComponentsInChildren<Transform>(true))
                if (child.name == "Room" || child.name == "areas" || child.name == "PlayButton" || child.name == "AreaButton"
                    || child.name == "Modal" || child.name == "RestoreEffect" || (child.name == "Settings"&&child.parent.name!="HUD"))
                    child.gameObject.SetActive(false);
            Canvas.ForceUpdateCanvases();
            var nav = content.Find("Nav") as RectTransform;
            map = CurvedLevelMap.Create(content, nav, config, art, count, () => previewReached, _ => selections++, () => { },previewKit,captureIslands);
            map.transform.SetAsFirstSibling();
            map.Open();
            Canvas.ForceUpdateCanvases();
            map.PreviewAt(level);
            Render();
        }

        void Render()
        {
            if (camera == null || map == null) return;
            map.PreviewAt(level);
            RenderCanvas();
        }

        void RenderCanvas()
        {
            if(camera==null||capture==null)return;
            Canvas.ForceUpdateCanvases();
            var request = new RenderPipeline.StandardRequest { destination = capture };
            if (RenderPipeline.SupportsRenderRequest(camera, request)) RenderPipeline.SubmitRenderRequest(camera, request);
            else
            {
                camera.targetTexture = capture;
                camera.Render();
            }
        }

        void OnGUI()
        {
            if(needsRebuild){needsRebuild=false;Build(capture!=null?capture.width:1080,capture!=null?capture.height:1920,previewReached);}
            var divider=new Rect(Mathf.Clamp(sidebarWidth,300,Mathf.Max(300,position.width-220)),0,6,position.height);
            EditorGUIUtility.AddCursorRect(divider,MouseCursor.ResizeHorizontal);
            var e=Event.current;
            if(e.type==EventType.MouseDown&&divider.Contains(e.mousePosition)){resizing=true;e.Use();}
            if(resizing&&e.type==EventType.MouseDrag){sidebarWidth=e.mousePosition.x;Repaint();e.Use();}
            if(e.type==EventType.MouseUp)resizing=false;
            GUILayout.BeginArea(new Rect(0,0,divider.x,position.height));
            settingsScroll=EditorGUILayout.BeginScrollView(settingsScroll);
            EditorGUILayout.LabelField("Mapa de níveis · Base ilustrada v6",EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Configurações salvas são usadas no jogo.",EditorStyles.miniLabel);
            if(map!=null&&islandControls.Draw(ref level,map.World,WorldMapLevelCatalog.Count))
            {layoutControls.ClearSelection();Build(capture.width,capture.height,Mathf.Clamp(Mathf.CeilToInt(level),1,Mathf.Max(1,count)));}
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            seasonPreview=EditorGUILayout.Popup("Preview de season",seasonPreview,new[]{"Season ativa no jogo","Sem decoração","Natal / neve sobreposta"});
            if(EditorGUI.EndChangeCheck()){previewKit.seasonPreview=seasonPreview;Build(capture.width,capture.height,previewReached);}
            EditorGUILayout.HelpBox("O preview de season não altera a season ativa. Natal coloca neve sobre os objetos de qualquer área e aplica o chão nevado.",MessageType.None);
            if(GUILayout.Button("Editar seasons e camadas do mapa"))EditorApplication.ExecuteMenuItem("Royal Aves/Editor de temporadas");
            DrawSantaSettings();
            EditorGUI.BeginChangeCheck();
            level = EditorGUILayout.Slider("Avançar no mundo", level, 1, Mathf.Max(1, map?.World!=null?map.World.Route.LevelSlots+1.5f:count));
            if (EditorGUI.EndChangeCheck() && map != null)
            {
                previewReached = Mathf.Clamp(Mathf.CeilToInt(level), 1, count);
                map.Open(false);
            }
            if (GUILayout.Button("Exportar PNG"))
            {
                var path = EditorUtility.SaveFilePanel("Exportar preview", "", "mapa-de-niveis.png", "png");
                if (!string.IsNullOrEmpty(path)) Save(path);
            }
            if (capture != null)
            {
                layoutControls.Toolbar(map.World,ref level);
            }
            if(GUILayout.Button("Atualizar dados e reconstruir preview")){AssetDatabase.Refresh();needsRebuild=true;}
            EditorGUILayout.EndScrollView();GUILayout.EndArea();
            EditorGUI.DrawRect(divider,new Color(.13f,.13f,.13f));
            var pane=new Rect(divider.xMax,0,Mathf.Max(1,position.width-divider.xMax),position.height);
            GUILayout.BeginArea(new Rect(pane.x,0,pane.width,28),EditorStyles.toolbar);
            using(new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Preview · arraste objetos · botão do meio move a visão",EditorStyles.miniLabel);
                previewZoom=GUILayout.HorizontalSlider(previewZoom,1,4,GUILayout.Width(110));
                if(GUILayout.Button("Enquadrar",EditorStyles.toolbarButton,GUILayout.Width(75))){previewZoom=1;previewPan=Vector2.zero;}
            }
            GUILayout.EndArea();
            pane.yMin=28;
            if(capture!=null&&map!=null)
            {
                Render();
                GUI.BeginGroup(pane);
                var area=new Rect(0,0,pane.width,pane.height);
                EditorGUI.DrawRect(area,new Color(.12f,.12f,.12f));
                e=Event.current;
                if(e.type==EventType.MouseDown&&e.button==2&&area.Contains(e.mousePosition)){panning=true;e.Use();}
                if(panning&&e.type==EventType.MouseDrag){previewPan+=e.delta;e.Use();}
                if(e.type==EventType.MouseUp&&e.button==2){panning=false;e.Use();}
                if(e.type==EventType.ScrollWheel&&area.Contains(e.mousePosition))
                {
                    var old=previewZoom;previewZoom=Mathf.Clamp(previewZoom*Mathf.Exp(-e.delta.y*.08f),1,4);
                    previewPan=(previewPan-(e.mousePosition-area.center))*(previewZoom/old)+(e.mousePosition-area.center);e.Use();
                }
                var fit=Mathf.Min(area.width/capture.width,area.height/capture.height)*previewZoom;
                var imageRect=new Rect(area.center.x+previewPan.x-capture.width*fit*.5f,area.center.y+previewPan.y-capture.height*fit*.5f,capture.width*fit,capture.height*fit);
                GUI.DrawTexture(imageRect,capture,ScaleMode.StretchToFill);
                if(!layoutControls.Editing&&e.type==EventType.MouseUp&&e.button==0&&imageRect.Contains(e.mousePosition))
                {
                    var uv=new Vector2((e.mousePosition.x-imageRect.x)/imageRect.width,1-(e.mousePosition.y-imageRect.y)/imageRect.height);
                    if(map.TryCatchSanta(uv))e.Use();
                }
                layoutControls.Overlay(imageRect,map.World,level);
                GUI.EndGroup();Repaint();
            }
        }

        void DrawSantaSettings()
        {
            santaSettings=EditorGUILayout.Foldout(santaSettings,"Evento surpresa de Natal",true);
            if(!santaSettings)return;
            var settings=Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath);if(settings==null)return;
            var data=new SerializedObject(settings);data.Update();
            void Field(string name,string label)=>EditorGUILayout.PropertyField(data.FindProperty(name),new GUIContent(label));
            Field("santaEventEnabled","Ativar aparições");Field("santaFirstDelay","Primeira aparição (s)");
            Field("santaVisibleSeconds","Tempo para clicar (s)");Field("santaInterval","Intervalo entre aparições (s)");
            Field("santaFriendlyMessage","Balão do Papai Noel");Field("santaVillainMessage","Balão do Noel do mal");
            if(data.ApplyModifiedProperties())needsRebuild=true;
            if(GUILayout.Button("Salvar evento"))AssetDatabase.SaveAssetIfDirty(settings);
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("Testar Noel simpático"))map?.World?.ShowSantaNow(false);
                if(GUILayout.Button("Testar Noel do mal"))map?.World?.ShowSantaNow(true);
            }
            EditorGUILayout.HelpBox("Os dois aparecem somente atrás de arbustos visíveis, mostrando a cabeça e parte do tronco. Para clicar no personagem neste preview, desligue Editar objetos.",MessageType.None);
        }

        void Save(string path, bool raw = false)
        {
            Render();
            var previous = RenderTexture.active;
            var image = new Texture2D(capture.width, capture.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = capture;
                image.ReadPixels(new Rect(0, 0, capture.width, capture.height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, raw ? image.GetRawTextureData() : image.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; DestroyImmediate(image); }
        }

        void Cleanup()
        {
            // Edit-mode MonoBehaviours are not guaranteed an OnDestroy callback on preview-scene close.
            if(map!=null&&map.World!=null)map.World.Dispose();
            map = null;
            camera = null;
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            if (capture != null) { capture.Release(); DestroyImmediate(capture); capture = null; }
            if(previewKit!=null){DestroyImmediate(previewKit);previewKit=null;}
        }

        void OnLayoutUndo(){layoutControls.ClearSelection();needsRebuild=true;Repaint();}
        void OnProjectChange(){needsRebuild=true;Repaint();}
        void OnDisable(){Undo.undoRedoPerformed-=OnLayoutUndo;Cleanup();}

        // Executed in an isolated preview project when the user's Unity already owns the main project.
        public static void ValidateAndRender()
        {
            var preview = CreateInstance<RoyalAvesLevelMapPreview>();
            var folder = "Library/MapValidation/Previews";
            Directory.CreateDirectory(folder);
            try
            {
                preview.Build(1080, 1920, 1);
                var mapConfig = AssetDatabase.LoadAssetAtPath<LevelMapConfig>("Assets/RoyalAves/Resources/RoyalAvesLevelMap.asset");
                var crest = mapConfig.curveDepth;
                var size = new Vector2(1080, 1400);
                var front = LevelMapProjection.GroundY(crest - 1, size.x * 0.5f, size, mapConfig);
                var top = LevelMapProjection.HorizonY(size.x * 0.5f, size, mapConfig);
                var hidden = LevelMapProjection.GroundY(crest + 1, size.x * 0.5f, size, mapConfig);
                var edge = LevelMapProjection.HorizonY(0, size, mapConfig);
                if (top <= front || top <= hidden || Mathf.Abs(top - edge - size.y * mapConfig.crestCurvature) > 1
                    || LevelMapProjection.SurfaceFacing(crest, mapConfig) > 0.001f)
                    throw new InvalidOperationException("A projeção não dobra para trás do horizonte.");
                var prop = preview.map.GetComponentsInChildren<CurvedMapPropImage>().First();
                var populate = typeof(CurvedMapPropImage).GetMethod("OnPopulateMesh",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly);
                float PropHeight(float tilt)
                {
                    prop.Tilt = tilt;
                    using (var vertices = new VertexHelper())
                    {
                        populate.Invoke(prop, new object[] { vertices });
                        var vertex = new UIVertex();
                        var minimum = float.PositiveInfinity;
                        var maximum = float.NegativeInfinity;
                        for (var i = 0; i < vertices.currentVertCount; i++)
                        {
                            vertices.PopulateUIVertex(ref vertex, i);
                            minimum = Mathf.Min(minimum, vertex.position.y);
                            maximum = Mathf.Max(maximum, vertex.position.y);
                        }
                        return maximum - minimum;
                    }
                }
                if (PropHeight(mapConfig.horizonObjectTilt) >= PropHeight(0) * 0.65f)
                    throw new InvalidOperationException("O sprite distante não deita sobre a curva.");
                if (!preview.map.CanSelect(1) || preview.map.CanSelect(2) || preview.map.CanSelect(0)
                    || preview.map.CanSelect(preview.count + 1))
                    throw new InvalidOperationException("Bloqueio de níveis inválido.");
                var firstButton = preview.map.GetComponentsInChildren<Button>().First(button => button.name == "Nivel_1");
                firstButton.onClick.Invoke();
                if (preview.selections != 1) throw new InvalidOperationException("Seleção não entregue ao carregador.");
                var pointer = new PointerEventData(null);
                preview.map.BeginDrag(pointer);
                firstButton.onClick.Invoke();
                preview.map.EndDrag(pointer);
                firstButton.onClick.Invoke();
                if (preview.selections != 1 || pointer.eligibleForClick)
                    throw new InvalidOperationException("Arrastar acionou uma fase.");
                foreach (var start in new[] { 1, 23, 26, 51, 76, 97 })
                {
                    preview.level = start;
                    preview.Build(1080, 1920, Mathf.Min(preview.count, start == 1 ? 1 : start + 1));
                    preview.Save(Path.Combine(folder, $"mapa-{start:000}.png"));
                }
                preview.level = 26;
                preview.Build(1080, 2340, 28);
                preview.Save(Path.Combine(folder, "mapa-tela-alta.png"));
                preview.Build(1080, 1920, preview.count + 1);
                if (!preview.map.CanSelect(preview.count) || preview.map.CanSelect(preview.count + 1))
                    throw new InvalidOperationException("Último nível inválido.");
                var frames = Path.Combine(folder, "rolling-frames");
                Directory.CreateDirectory(frames);
                preview.level = 20;
                preview.Build(360, 640, 26);
                for (var frame = 0; frame < 36; frame++)
                {
                    preview.level = 20 + frame * 0.2f;
                    preview.Save(Path.Combine(frames, $"frame-{frame:000}.rgba"), true);
                }
                File.WriteAllText(Path.Combine(folder, "validation.txt"),
                    "PASS: finite curved horizon; ground descends behind crest; lateral arc; tangent ground facing; sprite stands up from crest; first/locked/invalid/completed level selection; click callback; drag cancels click; six world positions; region transition; 9:16 and tall-screen renders.\n");
                Debug.Log("Royal Aves level map validation PASS: " + Path.GetFullPath(folder));
            }
            finally { preview.Cleanup(); DestroyImmediate(preview); }
        }
    }
}
