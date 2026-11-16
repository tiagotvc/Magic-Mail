using System;
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
    /// Editor das introduções de elemento (tela "Novo item desbloqueado!").
    /// Em cima: a tela rodando ao vivo, como no jogo (fundo de referência + brilho animado + textos no idioma escolhido).
    /// Abas: Conteúdo (fase, nome, ícone e textos PT/EN de cada introdução, salvos no catálogo), Layout e textos
    /// (posição, tamanho e fonte de cada parte, salvos no prefab) e Efeito dourado (camadas do brilho, salvas no prefab).
    /// Menu: Royal Aves > Introdução de elemento > Editor.
    /// </summary>
    public sealed class ElementIntroEditorWindow : EditorWindow
    {
        const string BackdropPath = "Assets/Royal Postman Art/Screens/Gameplay/mockup.png";
        // Partes do prefab editáveis na aba Layout (nomes dos objetos criados pelo ElementIntroScreenBuilder).
        static readonly string[] PartNames =
        {
            "Nome do elemento (imagem)", "Nome do elemento (texto reserva)", "Subtítulo (Novo item desbloqueado)",
            "Elemento", "Placa de instrução", "Texto da instrução", "Brilho dourado",
        };

        ElementIntroCatalog catalog;
        SerializedObject catalogObject;
        int selected, tab;
        Vector2 listScroll, formScroll;
        bool previewEnglish, animate = true, showBackdrop = true;
        float loopSeconds = 5f;
        GUIStyle listItem;

        PreviewRenderUtility preview;
        GameObject clone;
        ElementIntroScreen cloneScreen;
        bool rebuildClone = true;
        double startTime;
        Texture2D backdrop;

        [MenuItem("Royal Aves/Introdução de elemento/Editor")]
        public static void OpenWindow() => GetWindow<ElementIntroEditorWindow>("Introduções");

        /// Abre o editor já na introdução indicada.
        public static void Open(ElementIntroCatalog.Entry entry)
        {
            var window = GetWindow<ElementIntroEditorWindow>("Introduções");
            window.Load();
            if (window.catalog != null) window.selected = Mathf.Max(0, window.catalog.entries.IndexOf(entry));
            window.rebuildClone = true;
            window.Focus();
        }

        public static ElementIntroCatalog LoadOrCreateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ElementIntroCatalog>(ElementIntroScreenBuilder.CatalogPath);
            if (catalog == null)
            {
                ElementIntroScreenBuilder.BuildCatalog();
                catalog = AssetDatabase.LoadAssetAtPath<ElementIntroCatalog>(ElementIntroScreenBuilder.CatalogPath);
            }
            return catalog;
        }

        /// Nova entrada (fase, recurso do ícone) já salva no catálogo.
        public static ElementIntroCatalog.Entry CreateEntry(int level, string iconResource, string id)
        {
            var catalog = LoadOrCreateCatalog();
            if (catalog == null) return null;
            Undo.RecordObject(catalog, "Nova introdução");
            var entry = new ElementIntroCatalog.Entry
            {
                id = id, level = level, iconResource = iconResource,
                titlePt = "Novo elemento!", titleEn = "New element!",
                instructionPt = "Faça combinações ao lado do <hl>ELEMENTO</hl> para quebrá-lo!",
                instructionEn = "Make matches next to the <hl>ELEMENT</hl> to break it!",
            };
            catalog.entries.Add(entry);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return entry;
        }

        // ------------------------------------------------------------------------------------------ ciclo de vida

        const string PreviewName = "ElementIntroPreview (temporário)";

        /// Põe a tela, parada e já com o conteúdo desta introdução, na Game view (fora do Play; não é salva na cena).
        public static void PreviewInGameView(ElementIntroCatalog.Entry entry)
        {
            ClosePreview();
            var prefab = AssetDatabase.LoadAssetAtPath<ElementIntroScreen>(ElementIntroScreenBuilder.PrefabPath);
            if (prefab == null)
            {
                ElementIntroScreenBuilder.BuildPrefab();
                prefab = AssetDatabase.LoadAssetAtPath<ElementIntroScreen>(ElementIntroScreenBuilder.PrefabPath);
            }
            var view = (ElementIntroScreen)PrefabUtility.InstantiatePrefab(prefab);
            view.gameObject.name = PreviewName;
            view.gameObject.hideFlags = HideFlags.DontSave;
            var icon = entry.iconOverride != null ? entry.iconOverride : CampaignElementIntroductions.Icon(entry.iconResource);
            view.Fill(entry, icon);
            view.PreviewPose(1.5f);
            Selection.activeGameObject = view.gameObject;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        public static void ClosePreview()
        {
            foreach (var view in UnityEngine.Object.FindObjectsByType<ElementIntroScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (view.gameObject.name == PreviewName) UnityEngine.Object.DestroyImmediate(view.gameObject);
        }

        static void OnPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode) ClosePreview();
        }

        void OnEnable()
        {
            Load();
            backdrop = AssetDatabase.LoadAssetAtPath<Texture2D>(BackdropPath);
            startTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
            Undo.undoRedoPerformed += OnUndo;
        }

        void OnDisable()
        {
            EditorApplication.update -= Tick;
            Undo.undoRedoPerformed -= OnUndo;
            LocalizedImage.OverrideLanguage = null;
            DestroyClone();
            if (preview != null) { preview.Cleanup(); preview = null; }
        }

        void OnUndo() { rebuildClone = true; Repaint(); }

        void Load()
        {
            catalog = LoadOrCreateCatalog();
            catalogObject = catalog != null ? new SerializedObject(catalog) : null;
        }

        void Tick()
        {
            if (clone == null && !rebuildClone) return;
            if (animate || rebuildClone) Repaint();
        }

        // ------------------------------------------------------------------------------------------ preview ao vivo

        void DestroyClone()
        {
            if (clone != null) DestroyImmediate(clone);
            clone = null;
            cloneScreen = null;
        }

        void BuildClone()
        {
            rebuildClone = false;
            DestroyClone();
            if (catalog == null || catalog.entries.Count == 0) return;
            var prefab = AssetDatabase.LoadAssetAtPath<ElementIntroScreen>(ElementIntroScreenBuilder.PrefabPath);
            if (prefab == null) return;
            if (preview == null)
            {
                preview = new PreviewRenderUtility();
                var camera = preview.camera;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0, 0, 0, 0);
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 50f;
                camera.transform.position = new Vector3(0, 0, -10f);
            }
            clone = Instantiate(prefab.gameObject);
            clone.name = "ElementIntroScreen (preview)";
            preview.AddSingleGO(clone);
            var canvas = clone.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = preview.camera;
            canvas.planeDistance = 5f;
            cloneScreen = clone.GetComponent<ElementIntroScreen>();
            var entry = catalog.entries[Mathf.Clamp(selected, 0, catalog.entries.Count - 1)];
            LocalizedImage.OverrideLanguage = previewEnglish ? GameLanguage.English : GameLanguage.Portuguese;
            var icon = entry.iconOverride != null ? entry.iconOverride : CampaignElementIntroductions.Icon(entry.iconResource);
            cloneScreen.Fill(entry, icon);
            RefreshLanguage();
            startTime = EditorApplication.timeSinceStartup;
            pendingFit = true;
        }

        bool pendingFit;

        void RefreshLanguage()
        {
            if (clone == null) return;
            LocalizedImage.OverrideLanguage = previewEnglish ? GameLanguage.English : GameLanguage.Portuguese;
            foreach (var item in clone.GetComponentsInChildren<LocalizedImage>(true)) item.Refresh();
            foreach (var item in clone.GetComponentsInChildren<LocalizedTextTMP>(true)) item.Refresh();
        }

        void DrawLivePreview(float width, float height)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var wantEnglish = GUILayout.Toggle(previewEnglish, "EN", EditorStyles.miniButtonLeft, GUILayout.Width(34));
                var wantPortuguese = GUILayout.Toggle(!previewEnglish, "PT", EditorStyles.miniButtonRight, GUILayout.Width(34));
                if (wantEnglish != previewEnglish) { previewEnglish = wantEnglish; rebuildClone = true; }
                else if (wantPortuguese == previewEnglish) { previewEnglish = false; rebuildClone = true; }
                animate = GUILayout.Toggle(animate, "Animar", EditorStyles.miniButton, GUILayout.Width(52));
                showBackdrop = GUILayout.Toggle(showBackdrop, "Fundo", EditorStyles.miniButton, GUILayout.Width(48));
                if (GUILayout.Button("Reiniciar", EditorStyles.miniButton, GUILayout.Width(62))) startTime = EditorApplication.timeSinceStartup;
            }
            loopSeconds = EditorGUILayout.Slider("Loop (s)", loopSeconds, 2f, 12f);

            if (rebuildClone) BuildClone();
            var rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.12f));
            if (showBackdrop && backdrop != null) GUI.DrawTexture(rect, backdrop, ScaleMode.ScaleAndCrop);
            if (clone == null || preview == null)
            {
                EditorGUI.LabelField(rect, "Prefab da tela não encontrado.\nUse \"Abrir prefab da tela\".", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            HandleSelection(rect);
            if (Event.current.type != EventType.Repaint) return;

            Canvas.ForceUpdateCanvases();
            if (pendingFit)
            {
                preview.BeginPreview(rect, GUIStyle.none);
                preview.camera.Render();
                preview.EndPreview();
                cloneScreen.FitToScreen();
                cloneScreen.CaptureBase();
                pendingFit = false;
            }
            var t = animate ? (float)((EditorApplication.timeSinceStartup - startTime) % loopSeconds) : 1.5f;
            cloneScreen.Animate(t);
            Canvas.ForceUpdateCanvases();
            preview.BeginPreview(rect, GUIStyle.none);
            preview.camera.Render();
            var texture = preview.EndPreview();
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            DrawSelection(rect);
        }

        // ------------------------------------------------------------------------------------------ seleção direta no preview

        // Partes que dá para clicar no preview (de trás para a frente) e mexer com a caixa de seleção.
        static readonly string[] Pickable =
        {
            "Placa de instrução", "Texto da instrução", "Subtítulo (Novo item desbloqueado)", "Elemento",
            "Nome do elemento (imagem)", "Nome do elemento (texto reserva)",
        };

        string pickedPart;
        bool dragging;
        int dragHandleX, dragHandleY;   // -1/0/1: 0,0 = mover; senão, qual lado/canto está sendo puxado
        Vector2 dragLast;
        const float HandleSize = 9f;

        RectTransform ClonePart(string name)
        {
            if (clone == null) return null;
            var found = clone.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name) as RectTransform;
            return found != null && found.gameObject.activeInHierarchy ? found : null;
        }

        // Retângulo da parte em pixels da janela, dentro do retângulo do preview.
        Rect PartPixels(RectTransform part, Rect view)
        {
            var canvas = (RectTransform)clone.transform;
            var corners = new Vector3[4];
            part.GetWorldCorners(corners);
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var corner in corners)
            {
                var local = canvas.InverseTransformPoint(corner);
                var u = (local.x - canvas.rect.xMin) / canvas.rect.width;
                var v = (local.y - canvas.rect.yMin) / canvas.rect.height;
                var pixel = new Vector2(view.x + u * view.width, view.y + (1f - v) * view.height);
                min = Vector2.Min(min, pixel);
                max = Vector2.Max(max, pixel);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static Vector2[] HandlePoints(Rect box) => new[]
        {
            new Vector2(box.xMin, box.yMin), new Vector2(box.center.x, box.yMin), new Vector2(box.xMax, box.yMin),
            new Vector2(box.xMin, box.center.y), new Vector2(box.xMax, box.center.y),
            new Vector2(box.xMin, box.yMax), new Vector2(box.center.x, box.yMax), new Vector2(box.xMax, box.yMax),
        };

        // Sinais de cada ponto acima (x: esquerda -1/direita 1; y: em cima 1/embaixo -1, como no Unity).
        static readonly Vector2Int[] HandleSigns =
        {
            new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1),
            new Vector2Int(-1, 0), new Vector2Int(1, 0),
            new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
        };

        void HandleSelection(Rect view)
        {
            if (clone == null) return;
            var e = Event.current;
            var id = GUIUtility.GetControlID(FocusType.Passive);
            var part = pickedPart != null ? ClonePart(pickedPart) : null;

            if (e.type == EventType.MouseDown && e.button == 0 && view.Contains(e.mousePosition))
            {
                // 1) alças da parte já selecionada
                if (part != null)
                {
                    var box = PartPixels(part, view);
                    var points = HandlePoints(box);
                    for (var i = 0; i < points.Length; i++)
                        if (Vector2.Distance(points[i], e.mousePosition) <= HandleSize)
                        {
                            dragging = true; dragHandleX = HandleSigns[i].x; dragHandleY = HandleSigns[i].y;
                            dragLast = e.mousePosition; GUIUtility.hotControl = id; e.Use(); return;
                        }
                }
                // 2) clicar numa parte (a da frente primeiro)
                string hit = null;
                for (var i = Pickable.Length - 1; i >= 0 && hit == null; i--)
                {
                    var candidate = ClonePart(Pickable[i]);
                    if (candidate != null && PartPixels(candidate, view).Contains(e.mousePosition)) hit = Pickable[i];
                }
                pickedPart = hit;
                if (hit != null)
                {
                    dragging = true; dragHandleX = 0; dragHandleY = 0;
                    dragLast = e.mousePosition; GUIUtility.hotControl = id;
                }
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseDrag && dragging && part != null && GUIUtility.hotControl == id)
            {
                var canvas = (RectTransform)clone.transform;
                var pixels = e.mousePosition - dragLast;
                dragLast = e.mousePosition;
                // pixels do preview -> unidades do canvas -> espaço local do pai da parte (que tem escala própria)
                var local = new Vector3(pixels.x / view.width * canvas.rect.width, -pixels.y / view.height * canvas.rect.height, 0f);
                var delta = (Vector2)part.parent.InverseTransformVector(canvas.TransformVector(local));
                if (dragHandleX == 0 && dragHandleY == 0) part.anchoredPosition += delta;
                else
                {
                    // Pivô no centro: puxar um lado muda o tamanho e desloca o centro pela metade.
                    var size = part.sizeDelta;
                    var grow = new Vector2(dragHandleX * delta.x, dragHandleY * delta.y);
                    size += grow;
                    size = new Vector2(Mathf.Max(10f, size.x), Mathf.Max(10f, size.y));
                    var applied = size - part.sizeDelta;
                    part.sizeDelta = size;
                    part.anchoredPosition += new Vector2(dragHandleX * applied.x * 0.5f, dragHandleY * applied.y * 0.5f);
                }
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseUp && dragging && GUIUtility.hotControl == id)
            {
                dragging = false;
                GUIUtility.hotControl = 0;
                if (part != null) SaveDraggedPart(part);
                e.Use();
            }
        }

        // Grava no prefab a posição e o tamanho que a parte tem no preview.
        void SaveDraggedPart(RectTransform part)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<ElementIntroScreen>(ElementIntroScreenBuilder.PrefabPath);
            if (prefab == null) return;
            var target = FindPart(prefab, part.name) as RectTransform;
            if (target == null) return;
            Undo.RecordObject(target, "Mover/redimensionar na introdução");
            target.anchoredPosition = part.anchoredPosition;
            target.sizeDelta = part.sizeDelta;
            EditorUtility.SetDirty(target);
            var keep = pickedPart;
            SavePrefab(prefab);          // salva e pede para refazer o preview
            pickedPart = keep;
        }

        void DrawSelection(Rect view)
        {
            if (clone == null || pickedPart == null) return;
            var part = ClonePart(pickedPart);
            if (part == null) return;
            var box = PartPixels(part, view);
            Handles.BeginGUI();
            Handles.DrawSolidRectangleWithOutline(box, new Color(0.3f, 0.7f, 1f, 0.07f), new Color(0.3f, 0.7f, 1f, 1f));
            foreach (var point in HandlePoints(box))
                Handles.DrawSolidRectangleWithOutline(new Rect(point.x - HandleSize * 0.5f, point.y - HandleSize * 0.5f, HandleSize, HandleSize),
                    Color.white, new Color(0.1f, 0.4f, 0.8f, 1f));
            Handles.EndGUI();
            var label = pickedPart + "  " + Mathf.RoundToInt(part.sizeDelta.x) + " × " + Mathf.RoundToInt(part.sizeDelta.y);
            var style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.white } };
            var labelRect = new Rect(box.x, Mathf.Max(view.y, box.y - 16f), 330f, 16f);
            EditorGUI.DrawRect(labelRect, new Color(0.1f, 0.4f, 0.8f, 0.85f));
            GUI.Label(labelRect, " " + label, style);
        }


        // ------------------------------------------------------------------------------------------ janela

        void OnGUI()
        {
            if (catalog == null || catalogObject == null)
            {
                EditorGUILayout.HelpBox("Catálogo não encontrado: " + ElementIntroScreenBuilder.CatalogPath, MessageType.Warning);
                if (GUILayout.Button("Criar catálogo")) Load();
                return;
            }
            listItem ??= new GUIStyle(EditorStyles.miniButtonLeft) { alignment = TextAnchor.MiddleLeft, richText = true };
            catalogObject.Update();
            var entries = catalogObject.FindProperty("entries");
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, entries.arraySize - 1));

            DrawToolbar(entries);
            // Esquerda: lista; centro: a tela ao vivo, o maior possível; direita: painel de edição.
            var previewHeight = Mathf.Clamp(position.height - 80f, 300f, 1000f);
            var previewWidth = previewHeight * 9f / 16f;
            var maxPreviewWidth = Mathf.Max(220f, (position.width - 190f) * 0.5f);
            if (previewWidth > maxPreviewWidth) { previewWidth = maxPreviewWidth; previewHeight = previewWidth * 16f / 9f; }
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawList(entries);
                if (entries.arraySize > 0)
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(previewWidth + 8f)))
                        DrawLivePreview(previewWidth, previewHeight);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        tab = GUILayout.Toolbar(tab, new[] { "Conteúdo", "Layout e textos", "Efeito dourado" });
                        EditorGUILayout.Space();
                        formScroll = EditorGUILayout.BeginScrollView(formScroll);
                        if (tab == 0) DrawContent(entries.GetArrayElementAtIndex(selected));
                        else if (tab == 1) DrawLayout();
                        else DrawGlow();
                        EditorGUILayout.EndScrollView();
                    }
                }
            }
            if (catalogObject.ApplyModifiedProperties())
            {
                rebuildClone = true;
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssetIfDirty(catalog);   // a aba Conteúdo também salva na hora
            }
        }

        void DrawList(SerializedProperty entries)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(190)))
            {
                EditorGUILayout.LabelField("Introduções", EditorStyles.boldLabel);
                listScroll = EditorGUILayout.BeginScrollView(listScroll);
                var order = Enumerable.Range(0, entries.arraySize)
                    .OrderBy(i => entries.GetArrayElementAtIndex(i).FindPropertyRelative("level").intValue).ToList();
                foreach (var i in order)
                {
                    var item = entries.GetArrayElementAtIndex(i);
                    var level = item.FindPropertyRelative("level").intValue;
                    var title = item.FindPropertyRelative("titlePt").stringValue;
                    var label = (level > 0 ? "Fase " + level : "(desligada)") + "  " + title;
                    var previous = GUI.backgroundColor;
                    if (i == selected) GUI.backgroundColor = new Color(0.55f, 0.8f, 1f);
                    if (GUILayout.Button(label, listItem)) { selected = i; rebuildClone = true; GUI.FocusControl(null); }
                    GUI.backgroundColor = previous;
                }
                EditorGUILayout.EndScrollView();
            }
        }

        void DrawToolbar(SerializedProperty entries)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Nova", EditorStyles.toolbarButton))
                {
                    var entry = CreateEntry(0, "", "novo-elemento");
                    catalogObject.Update();
                    selected = catalog.entries.IndexOf(entry);
                    rebuildClone = true;
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("Duplicar", EditorStyles.toolbarButton) && entries.arraySize > 0)
                {
                    entries.GetArrayElementAtIndex(selected).DuplicateCommand();
                    selected++;
                }
                if (GUILayout.Button("Remover", EditorStyles.toolbarButton) && entries.arraySize > 0 &&
                    EditorUtility.DisplayDialog("Remover introdução", "Remover esta introdução do catálogo?", "Remover", "Cancelar"))
                {
                    entries.DeleteArrayElementAtIndex(selected);
                    selected = Mathf.Max(0, selected - 1);
                    rebuildClone = true;
                    GUIUtility.ExitGUI();
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Abrir prefab da tela", EditorStyles.toolbarButton))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ElementIntroScreenBuilder.PrefabPath);
                    if (prefab == null) { ElementIntroScreenBuilder.BuildPrefab(); prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ElementIntroScreenBuilder.PrefabPath); }
                    Selection.activeObject = prefab;
                    AssetDatabase.OpenAsset(prefab);
                }
                if (Application.isPlaying && GUILayout.Button("Abrir no jogo", EditorStyles.toolbarButton))
                {
                    var data = catalog.entries[selected];
                    var icon = data.iconOverride != null ? data.iconOverride : CampaignElementIntroductions.Icon(data.iconResource);
                    ElementIntroScreen.Show(data, icon);
                }
                if (GUILayout.Button("Exportar JSON", EditorStyles.toolbarButton)) ExportJson();
                if (GUILayout.Button("Importar JSON", EditorStyles.toolbarButton)) ImportJson();
            }
        }

        // ------------------------------------------------------------------------------------------ aba: conteúdo

        void DrawContent(SerializedProperty entry)
        {
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("id"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("level"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("iconResource"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("iconOverride"));
            WarnIfSliced(entry.FindPropertyRelative("iconOverride"));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Nome do elemento", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("nameSpritePt"), new GUIContent("Imagem do nome (Português)"));
            WarnIfSliced(entry.FindPropertyRelative("nameSpritePt"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("nameSpriteEn"), new GUIContent("Imagem do nome (English)"));
            WarnIfSliced(entry.FindPropertyRelative("nameSpriteEn"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("titlePt"), new GUIContent("Texto reserva (Português)"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("titleEn"), new GUIContent("Texto reserva (English)"));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Texto do retângulo  (<hl>palavra</hl> = destaque)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("instructionPt"), new GUIContent("Português"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("instructionEn"), new GUIContent("English"));
        }

        static Sprite GetLocalized(SerializedProperty list, GameLanguage language)
        {
            for (var i = 0; i < list.arraySize; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                if (item.FindPropertyRelative("language").enumValueIndex == (int)language)
                    return item.FindPropertyRelative("sprite").objectReferenceValue as Sprite;
            }
            return null;
        }

        static void SetLocalized(SerializedProperty list, GameLanguage language, Sprite sprite)
        {
            for (var i = 0; i < list.arraySize; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                if (item.FindPropertyRelative("language").enumValueIndex != (int)language) continue;
                if (sprite == null) list.DeleteArrayElementAtIndex(i);
                else item.FindPropertyRelative("sprite").objectReferenceValue = sprite;
                return;
            }
            if (sprite == null) return;
            list.arraySize++;
            var added = list.GetArrayElementAtIndex(list.arraySize - 1);
            added.FindPropertyRelative("language").enumValueIndex = (int)language;
            added.FindPropertyRelative("sprite").objectReferenceValue = sprite;
        }

        // Uma imagem importada como "Multiple" e fatiada automaticamente vira vários pedaços (ex.: só "Globo" de "Globo de luz").
        // Se o sprite escolhido é só um pedaço da textura, avisa e oferece converter a imagem para Single (inteira).
        static void WarnIfSliced(SerializedProperty property)
        {
            var sprite = property.objectReferenceValue as Sprite;
            if (sprite == null || sprite.texture == null) return;
            var partial = sprite.rect.width < sprite.texture.width - 0.5f || sprite.rect.height < sprite.texture.height - 0.5f;
            if (!partial) return;
            EditorGUILayout.HelpBox("Esta imagem está fatiada (Sprite Mode = Multiple): o sprite escolhido (" + sprite.name +
                ") é só um pedaço dela e o resto fica cortado no jogo.", MessageType.Warning);
            if (!GUILayout.Button("Corrigir: usar a imagem inteira (Single)")) return;
            var path = AssetDatabase.GetAssetPath(sprite);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                property.serializedObject.ApplyModifiedProperties();
            }
            GUIUtility.ExitGUI();
        }

        // ------------------------------------------------------------------------------------------ aba: layout e textos

        static Transform FindPart(ElementIntroScreen prefab, string name) =>
            prefab.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        void DrawLayout()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<ElementIntroScreen>(ElementIntroScreenBuilder.PrefabPath);
            if (prefab == null) { EditorGUILayout.HelpBox("Prefab não encontrado.", MessageType.Warning); return; }
            EditorGUILayout.HelpBox("As mudanças desta aba valem para TODAS as introduções e são salvas no prefab na hora.", MessageType.Info);
            var changed = false;
            foreach (var name in PartNames)
            {
                var part = FindPart(prefab, name);
                if (part == null) continue;
                var rect = part as RectTransform;
                var label = part.GetComponent<TMP_Text>();
                var image = part.GetComponent<Image>();
                EditorGUILayout.LabelField(name, EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                Undo.RecordObject(rect, "Layout da introdução");
                var position = EditorGUILayout.Vector2Field("Posição", rect.anchoredPosition);
                var size = EditorGUILayout.Vector2Field("Tamanho", rect.sizeDelta);
                // O efeito dourado inteiro cresce/encolhe junto por esta escala (as camadas animam só a escala delas).
                var master = name == "Brilho dourado" ? EditorGUILayout.Slider("Escala geral do efeito", rect.localScale.x, 0.2f, 2f) : 1f;
                if (EditorGUI.EndChangeCheck())
                {
                    rect.anchoredPosition = position;
                    rect.sizeDelta = size;
                    if (name == "Brilho dourado") rect.localScale = new Vector3(master, master, 1f);
                    EditorUtility.SetDirty(rect);
                    changed = true;
                }
                if (label != null)
                {
                    EditorGUI.BeginChangeCheck();
                    Undo.RecordObject(label, "Texto da introdução");
                    var auto = EditorGUILayout.Toggle("Ajustar tamanho ao espaço", label.enableAutoSizing);
                    var min = label.fontSizeMin; var max = label.fontSizeMax; var fixedSize = label.fontSize;
                    if (auto)
                    {
                        min = EditorGUILayout.FloatField("Fonte mínima", label.fontSizeMin);
                        max = EditorGUILayout.FloatField("Fonte máxima  (aumente aqui)", label.fontSizeMax);
                    }
                    else fixedSize = EditorGUILayout.FloatField("Tamanho da fonte", label.fontSize);
                    var color = EditorGUILayout.ColorField("Cor do texto", label.color);
                    var font = (TMP_FontAsset)EditorGUILayout.ObjectField("Fonte", label.font, typeof(TMP_FontAsset), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        label.enableAutoSizing = auto;
                        label.fontSizeMin = min; label.fontSizeMax = max; label.fontSize = fixedSize;
                        label.color = color;
                        if (font != null) label.font = font;
                        EditorUtility.SetDirty(label);
                        changed = true;
                    }
                    var localized = part.GetComponent<LocalizedTextTMP>();
                    if (localized != null && part.name != "Texto da instrução")
                        EditorGUILayout.LabelField("(o conteúdo vem da aba Conteúdo)", EditorStyles.miniLabel);
                }
                else if (image != null && part.GetComponent<LocalizedImage>() != null && name != "Nome do elemento (imagem)")
                {
                    // Imagem com uma versão por idioma (ex.: "Novo item desbloqueado!" / "New item unlocked!").
                    var localized = part.GetComponent<LocalizedImage>();
                    var localizedObject = new SerializedObject(localized);
                    localizedObject.Update();
                    var list = localizedObject.FindProperty("images");
                    EditorGUI.BeginChangeCheck();
                    var pt = (Sprite)EditorGUILayout.ObjectField("Imagem (Português)", GetLocalized(list, GameLanguage.Portuguese), typeof(Sprite), false);
                    var en = (Sprite)EditorGUILayout.ObjectField("Imagem (English)", GetLocalized(list, GameLanguage.English), typeof(Sprite), false);
                    var es = (Sprite)EditorGUILayout.ObjectField("Imagem (Español, opcional)", GetLocalized(list, GameLanguage.Spanish), typeof(Sprite), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        SetLocalized(list, GameLanguage.Portuguese, pt);
                        SetLocalized(list, GameLanguage.English, en);
                        SetLocalized(list, GameLanguage.Spanish, es);
                        localizedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(localized);
                        changed = true;
                    }
                }
                else if (image != null && name != "Brilho dourado")
                {
                    EditorGUI.BeginChangeCheck();
                    Undo.RecordObject(image, "Imagem da introdução");
                    var sprite = (Sprite)EditorGUILayout.ObjectField("Imagem", image.sprite, typeof(Sprite), false);
                    var tint = EditorGUILayout.ColorField("Cor", image.color);
                    if (EditorGUI.EndChangeCheck())
                    {
                        image.sprite = sprite; image.color = tint;
                        EditorUtility.SetDirty(image);
                        changed = true;
                    }
                }
                EditorGUILayout.Space();
            }

            EditorGUILayout.LabelField("Fundo escuro", EditorStyles.boldLabel);
            var dim = FindPart(prefab, "Fundo")?.GetComponent<Image>();
            if (dim != null)
            {
                EditorGUI.BeginChangeCheck();
                Undo.RecordObject(dim, "Fundo da introdução");
                var alpha = EditorGUILayout.Slider(new GUIContent("Opacidade da arte do fundo",
                    "A imagem background_mask.png já é ~92% opaca; por isso 1 aqui não escurece tudo."), dim.color.a, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    var c = dim.color; c.a = alpha; dim.color = c;
                    EditorUtility.SetDirty(dim);
                    changed = true;
                }
                var extra = dim.transform.Find("Escurecimento extra")?.GetComponent<Image>();
                EditorGUI.BeginChangeCheck();
                var extraAlpha = EditorGUILayout.Slider(new GUIContent("Escurecimento extra (preto)",
                    "Camada preta por cima da arte, para o fundo do jogo sumir de vez."), extra != null ? extra.color.a : 0f, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    if (extra == null)
                    {
                        ElementIntroScreenBuilder.EnsureExtraDim();
                        prefab = AssetDatabase.LoadAssetAtPath<ElementIntroScreen>(ElementIntroScreenBuilder.PrefabPath);
                        extra = FindPart(prefab, "Fundo")?.Find("Escurecimento extra")?.GetComponent<Image>();
                    }
                    if (extra != null)
                    {
                        Undo.RecordObject(extra, "Escurecimento extra");
                        var c = extra.color; c.a = extraAlpha; extra.color = c;
                        EditorUtility.SetDirty(extra);
                        changed = true;
                    }
                }
            }
            var screenObject = new SerializedObject(prefab);
            screenObject.Update();
            EditorGUI.BeginChangeCheck();
            foreach (var field in new[] { "autoCloseSeconds", "tapDelay", "fadeIn", "fadeOut", "iconPopSeconds" })
                EditorGUILayout.PropertyField(screenObject.FindProperty(field));
            if (EditorGUI.EndChangeCheck()) { screenObject.ApplyModifiedProperties(); changed = true; }
            if (changed) SavePrefab(prefab);
        }

        // ------------------------------------------------------------------------------------------ aba: efeito dourado

        void DrawGlow()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<ElementIntroScreen>(ElementIntroScreenBuilder.PrefabPath);
            if (prefab == null) { EditorGUILayout.HelpBox("Prefab não encontrado.", MessageType.Warning); return; }
            EditorGUILayout.HelpBox("Cada camada do brilho atrás do item. Mexa e veja no preview acima; tudo é salvo no prefab. " +
                "Cada faísca tem duas camadas: \"Faísca N\" (a estrela) e \"Brilho da faísca N\" (o glow atrás dela).", MessageType.Info);
            if (GUILayout.Button("Refazer o efeito dourado com o padrão novo (mantém o resto do prefab)"))
            {
                if (EditorUtility.DisplayDialog("Refazer efeito dourado", "Isto recria só as camadas do brilho, perdendo ajustes feitos nelas. O resto do prefab fica igual. Continuar?", "Refazer", "Cancelar"))
                {
                    ElementIntroScreenBuilder.RebuildGlow();
                    rebuildClone = true;
                    GUIUtility.ExitGUI();
                }
            }
            var so = new SerializedObject(prefab);
            so.Update();
            var layers = so.FindProperty("glow");
            var changed = false;
            for (var i = 0; i < layers.arraySize; i++)
            {
                var layer = layers.GetArrayElementAtIndex(i);
                var name = layer.FindPropertyRelative("name").stringValue;
                var image = layer.FindPropertyRelative("image").objectReferenceValue as Image;
                var group = layer.FindPropertyRelative("rect").objectReferenceValue as RectTransform;
                layer.isExpanded = EditorGUILayout.Foldout(layer.isExpanded, name, true);
                if (!layer.isExpanded) continue;
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(layer.FindPropertyRelative("mode"));
                EditorGUILayout.PropertyField(layer.FindPropertyRelative("alpha"));
                EditorGUILayout.PropertyField(layer.FindPropertyRelative("speed"));
                EditorGUILayout.PropertyField(layer.FindPropertyRelative("spinDegreesPerSecond"));
                EditorGUILayout.PropertyField(layer.FindPropertyRelative("pulseScale"));
                EditorGUILayout.PropertyField(layer.FindPropertyRelative("alphaPulse"));
                EditorGUILayout.PropertyField(layer.FindPropertyRelative("phase"));
                if (EditorGUI.EndChangeCheck()) changed = true;

                if (image != null)
                {
                    EditorGUI.BeginChangeCheck();
                    Undo.RecordObject(image, "Brilho da introdução");
                    var color = EditorGUILayout.ColorField(new GUIContent("Cor"), image.color, true, false, false);
                    var size = EditorGUILayout.FloatField("Tamanho", image.rectTransform.sizeDelta.x);
                    if (EditorGUI.EndChangeCheck())
                    {
                        color.a = image.color.a;
                        image.color = color;
                        image.rectTransform.sizeDelta = new Vector2(size, size);
                        EditorUtility.SetDirty(image);
                        EditorUtility.SetDirty(image.rectTransform);
                        changed = true;
                    }
                }
                else if (group != null)
                {
                    var children = group.GetComponentsInChildren<Image>(true);
                    if (children.Length > 0)
                    {
                        EditorGUI.BeginChangeCheck();
                        var color = EditorGUILayout.ColorField(new GUIContent("Cor dos raios"), children[0].color, true, false, false);
                        var length = EditorGUILayout.FloatField("Comprimento do raio longo", children[0].rectTransform.sizeDelta.x);
                        var thickness = EditorGUILayout.FloatField("Espessura", children[0].rectTransform.sizeDelta.y);
                        if (EditorGUI.EndChangeCheck())
                        {
                            for (var k = 0; k < children.Length; k++)
                            {
                                Undo.RecordObject(children[k], "Raios da introdução");
                                Undo.RecordObject(children[k].rectTransform, "Raios da introdução");
                                var c = color; c.a = children[k].color.a;
                                children[k].color = c;
                                children[k].rectTransform.sizeDelta = new Vector2(length * (k % 2 == 0 ? 1f : 0.72f), thickness);
                                EditorUtility.SetDirty(children[k]);
                                EditorUtility.SetDirty(children[k].rectTransform);
                            }
                            changed = true;
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }
            if (changed) { so.ApplyModifiedProperties(); SavePrefab(prefab); }
        }

        void SavePrefab(ElementIntroScreen prefab)
        {
            EditorUtility.SetDirty(prefab);
            EditorUtility.SetDirty(prefab.gameObject);
            AssetDatabase.SaveAssetIfDirty(prefab);
            rebuildClone = true;
        }

        // ------------------------------------------------------------------------------------------ JSON

        [Serializable]
        class JsonEntry
        {
            public string id; public int level; public string iconResource; public string iconOverride;
            public string nameSpritePt, nameSpriteEn, titlePt, titleEn, instructionPt, instructionEn;
        }

        [Serializable]
        class JsonFile { public List<JsonEntry> entries = new List<JsonEntry>(); }

        // Sprite de arquivo "Multiple": guarda "caminho#nome" para achar o recorte certo na importação.
        static string SpriteRef(Sprite sprite) => sprite == null ? "" : AssetDatabase.GetAssetPath(sprite) + "#" + sprite.name;

        static Sprite SpriteFromRef(string reference)
        {
            if (string.IsNullOrEmpty(reference)) return null;
            var parts = reference.Split('#');
            var all = AssetDatabase.LoadAllAssetsAtPath(parts[0]).OfType<Sprite>().ToList();
            return all.FirstOrDefault(s => parts.Length > 1 && s.name == parts[1]) ?? all.FirstOrDefault();
        }

        void ExportJson()
        {
            var path = EditorUtility.SaveFilePanel("Exportar introduções", "Assets/RoyalAves", "ElementIntros.json", "json");
            if (string.IsNullOrEmpty(path)) return;
            var file = new JsonFile();
            foreach (var e in catalog.entries)
                file.entries.Add(new JsonEntry
                {
                    id = e.id, level = e.level, iconResource = e.iconResource, iconOverride = SpriteRef(e.iconOverride),
                    nameSpritePt = SpriteRef(e.nameSpritePt), nameSpriteEn = SpriteRef(e.nameSpriteEn),
                    titlePt = e.titlePt, titleEn = e.titleEn, instructionPt = e.instructionPt, instructionEn = e.instructionEn,
                });
            File.WriteAllText(path, JsonUtility.ToJson(file, true));
            Debug.Log("Introduções exportadas: " + path);
        }

        void ImportJson()
        {
            var path = EditorUtility.OpenFilePanel("Importar introduções", "Assets/RoyalAves", "json");
            if (string.IsNullOrEmpty(path)) return;
            var file = JsonUtility.FromJson<JsonFile>(File.ReadAllText(path));
            if (file == null || file.entries == null) return;
            Undo.RecordObject(catalog, "Importar introduções");
            catalog.entries = file.entries.Select(j => new ElementIntroCatalog.Entry
            {
                id = j.id, level = j.level, iconResource = j.iconResource, iconOverride = SpriteFromRef(j.iconOverride),
                nameSpritePt = SpriteFromRef(j.nameSpritePt), nameSpriteEn = SpriteFromRef(j.nameSpriteEn),
                titlePt = j.titlePt, titleEn = j.titleEn, instructionPt = j.instructionPt, instructionEn = j.instructionEn,
            }).ToList();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Load();
            rebuildClone = true;
        }
    }
}
