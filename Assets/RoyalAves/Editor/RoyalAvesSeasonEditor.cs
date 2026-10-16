// Season editor of Correio Mágico: the seasons kept in Resources/RoyalAvesSeasons.asset.
//
// Every screen has a BASE theme (its normal look, edited once and shared by all seasons). A season only adds to it — its name,
// props placed on the screen — or replaces a piece on purpose: its own art, its own positions. Pick the screen, pick the
// season, see the screen in the preview and place things on it with the mouse; "Aplicar" writes the result into the screen's
// scene (the game reads the active season when the screen opens, see LoadingScreen / SeasonProps).
//
// Screens so far: Carregamento (the first screen of the app). The others use the same pieces (SeasonProps hosts the props on
// any screen) and are listed here to be wired next.
// It lives in the "Temporadas" tab of the Sweet Sugar Level editor (LevelMakerEditor calls RoyalAvesSeasonPanel.DrawTab) and
// also opens alone from the menu Royal Aves > Editor de temporadas.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoyalAves.Meta;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public class RoyalAvesSeasonEditor : EditorWindow
    {
        Vector2 scroll;

        [MenuItem("Royal Aves/Editor de temporadas")]
        static void Open() => GetWindow<RoyalAvesSeasonEditor>("Temporadas").minSize = new Vector2(420, 480);

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            RoyalAvesSeasonPanel.DrawTab(position.width - 24, position.height, Repaint);
            EditorGUILayout.EndScrollView();
        }
    }

    public static class RoyalAvesSeasonPanel
    {
        const string ArtFolder = "Assets/RoyalAves/Art/Backgrounds/seasons/";
        // What the Loading scene shows today: this picture (without the logo), zoomed and shifted by hand, with the logo over it.
        const string DefaultArt = "Assets/RoyalAves/Art/Backgrounds/title.png";
        const string ScenePath = "Assets/RoyalAves/Scenes/Loading.unity";
        const float ListWidth = 240f, PreviewWidth = 330f, MinWide = 900f;

        static SeasonConfig config;
        static int selected;
        static string status;
        static Action repaintCb;
        static Texture2D shadeTexture;
        static bool showBar = true;
        static bool showBase;
        static readonly HashSet<SeasonProp> openProps = new HashSet<SeasonProp>();

        // The screens a season can dress. Only the first is wired; the others need a SeasonProps host to show props.
        static int screenIndex;
        static readonly string[] ScreenNames = { "Carregamento", "Tela inicial (em breve)", "Mapa de níveis", "Jogo (em breve)" };

        public static void DrawTab(float width, float height, Action repaint)
        {
            repaintCb = repaint;
            config = LoadOrCreate();
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, config.seasons.Count - 1));

            var wide = width >= MinWide;
            if (wide)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical(GUILayout.Width(ListWidth));
                DrawList();
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical(GUILayout.Width(Mathf.Max(340f, width - ListWidth - PreviewWidth - 36f)));
                DrawForm();
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical(GUILayout.Width(PreviewWidth));
                DrawPreview(PreviewWidth - 8f);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                DrawList();
                DrawForm();
                DrawPreview(Mathf.Min(width, PreviewWidth));
            }

            // The loading screen's progress bar: the reusable Magic Mail component, rendered with its own code.
            EditorGUILayout.Space(10);
            showBar = EditorGUILayout.Foldout(showBar, "Barra de progresso da tela de carregamento (componente reutilizável)", true);
            if (showBar) MagicMail.EditorTools.MagicProgressBarPreview.Draw(Mathf.Min(width, 900f), repaint);
        }

        // ---- the asset ---------------------------------------------------------------------------------------------

        // The asset the game reads. Created the first time with the base theme of the loading screen as it is today and one
        // season that inherits all of it, so nothing changes until a name, an art or a prop is set.
        static SeasonConfig LoadOrCreate()
        {
            var asset = AssetDatabase.LoadAssetAtPath<SeasonConfig>(RoyalAvesLoadingBuilder.SeasonConfigPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SeasonConfig>();
                // Zoom and offset reproduce the placement of the scene's hand-made art (941x1672 at 1.4241 scale, 51 to the left).
                asset.baseLoading = new LoadingLook
                {
                    art = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultArt), zoom = 1.24f, offset = new Vector2(-51f, 0f),
                    showLogo = true, bottomShade = true,
                };
                asset.seasons.Add(new Season { id = "padrao" });
                asset.activeSeasonId = "padrao";
                asset.baseMigrated = true;
                AssetDatabase.CreateAsset(asset, RoyalAvesLoadingBuilder.SeasonConfigPath);
                AssetDatabase.SaveAssets();
            }
            else if (!asset.baseMigrated) Migrate(asset);
            return asset;
        }

        // The first version kept the whole look inside each season. The first season becomes the base theme; every season keeps
        // what it had, and what differs from the base is marked as its own (art, positions) so nothing moves.
        static void Migrate(SeasonConfig c)
        {
            if (c.seasons.Count > 0)
            {
                var first = c.seasons[0];
                c.baseLoading = new LoadingLook
                {
                    art = first.loadingArt != null ? first.loadingArt : AssetDatabase.LoadAssetAtPath<Sprite>(DefaultArt),
                    zoom = first.artZoom, offset = first.artOffset, showLogo = first.showLogo, bottomShade = first.bottomShade,
                    title = first.titleLayout.Copy(), subtitle = first.subtitleLayout.Copy(),
                    bar = first.barLayout.Copy(), label = first.labelLayout.Copy(),
                };
                foreach (var s in c.seasons)
                {
                    s.customArt = s != first && s.loadingArt != null && s.loadingArt != c.baseLoading.art;
                    s.ownLayout = s != first && !SameLayouts(s, c.baseLoading);
                }
            }
            c.baseMigrated = true;
            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
        }

        static bool SameLayouts(Season s, LoadingLook b) =>
            Same(s.titleLayout, b.title) && Same(s.subtitleLayout, b.subtitle) && Same(s.barLayout, b.bar) && Same(s.labelLayout, b.label);

        static bool Same(ElementLayout a, ElementLayout b) =>
            (a.position - b.position).sqrMagnitude < 0.01f && Mathf.Abs(a.scale - b.scale) < 0.001f;

        // ---- list --------------------------------------------------------------------------------------------------

        static void DrawList()
        {
            EditorGUILayout.LabelField("Temporadas", EditorStyles.boldLabel);
            var rowStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };
            for (var i = 0; i < config.seasons.Count; i++)
            {
                var s = config.seasons[i];
                var active = s.id == config.Active?.id;
                var label = (active ? "★ " : "    ") + (string.IsNullOrWhiteSpace(s.title) ? s.id : s.title);
                GUI.backgroundColor = i == selected ? new Color(0.6f, 0.85f, 1f) : Color.white;
                if (GUILayout.Button(new GUIContent(label, active ? "Temporada ativa: a que as telas mostram" : s.id), rowStyle, GUILayout.Height(26)))
                    selected = i;
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Nova")) Mutate("Nova temporada", () =>
            {
                config.seasons.Add(new Season { id = UniqueId("temporada"), title = "Nova temporada" });   // inherits the base theme
                selected = config.seasons.Count - 1;
            });
            using (new EditorGUI.DisabledScope(config.seasons.Count == 0))
            {
                if (GUILayout.Button("Duplicar")) Mutate("Duplicar temporada", () =>
                {
                    var from = config.seasons[selected];
                    config.seasons.Add(new Season
                    {
                        id = UniqueId(from.id), title = from.title, subtitle = from.subtitle,
                        customArt = from.customArt, loadingArt = from.loadingArt, bottomShade = from.bottomShade,
                        showLogo = from.showLogo, artZoom = from.artZoom, artOffset = from.artOffset,
                        ownLayout = from.ownLayout,
                        titleLayout = from.titleLayout.Copy(), subtitleLayout = from.subtitleLayout.Copy(),
                        barLayout = from.barLayout.Copy(), labelLayout = from.labelLayout.Copy(),
                        props = from.props.Select(p => p.Copy()).ToList(),
                        map = new MapSeasonLook {snow=from.map?.snow??false,snowCoverage=from.map?.snowCoverage??.72f,groundMaterials=from.map?.groundMaterials,
                            overlays=from.map?.overlays.Select(o=>new MapSeasonOverlay{category=o.category,sprite=o.sprite,position=o.position,relativeHeight=o.relativeHeight}).ToList()??new List<MapSeasonOverlay>()},
                    });
                    selected = config.seasons.Count - 1;
                });
                if (GUILayout.Button("Excluir") && config.seasons.Count > 0 &&
                    EditorUtility.DisplayDialog("Excluir temporada", $"Excluir \"{config.seasons[selected].title}\"? (As imagens continuam na pasta.)", "Excluir", "Cancelar"))
                    Mutate("Excluir temporada", () => config.seasons.RemoveAt(selected));
            }
            EditorGUILayout.EndHorizontal();
        }

        // ---- form --------------------------------------------------------------------------------------------------

        static void DrawForm()
        {
            EditorGUILayout.Space();
            if (config.seasons.Count == 0)
            {
                EditorGUILayout.HelpBox("Nenhuma temporada. Use \"+ Nova\".", MessageType.Info);
                return;
            }
            var s = config.seasons[selected];
            var isActive = s.id == config.Active?.id;

            EditorGUILayout.LabelField("Tela", EditorStyles.miniLabel);
            screenIndex = GUILayout.Toolbar(screenIndex, ScreenNames);
            if(screenIndex==(int)SeasonScreen.Lobby)
            {
                EditorGUILayout.HelpBox("Mapa: decorações sobrepostas em todos os biomas. O atlas de chão pode ser substituído pela season; os objetos base continuam os mesmos.",MessageType.Info);
                var serialized=new SerializedObject(config);serialized.Update();
                EditorGUILayout.PropertyField(serialized.FindProperty("seasons").GetArrayElementAtIndex(selected).FindPropertyRelative("map"),new GUIContent("Camadas da season no mapa"),true);
                if(serialized.ApplyModifiedProperties())EditorUtility.SetDirty(config);
                if(GUILayout.Button("Salvar season"))AssetDatabase.SaveAssets();
                return;
            }
            if (screenIndex != (int)SeasonScreen.Loading)
            {
                EditorGUILayout.HelpBox("Esta tela ainda não está ligada às temporadas. O mecanismo é o mesmo da tela de carregamento " +
                                        "(o componente SeasonProps mostra os adereços da temporada em qualquer tela); falta colocar essa tela " +
                                        "no editor e no jogo.", MessageType.Info);
                return;
            }

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(config, "Editar temporada");
            EditorGUILayout.LabelField(isActive ? "Temporada ativa (a que o jogo mostra agora)" : "Temporada (não é a ativa)", EditorStyles.boldLabel);

            var oldId = s.id;
            s.id = EditorGUILayout.TextField(new GUIContent("Id", "Nome interno, sem espaços (ex.: polo-norte)."), s.id);
            if (isActive && s.id != oldId) config.activeSeasonId = s.id;   // the active season keeps being the active one
            s.title = EditorGUILayout.TextField(new GUIContent("Nome", "Texto grande embaixo na tela de carregamento. Aparece em maiúsculas."), s.title);
            s.subtitle = EditorGUILayout.TextField(new GUIContent("Subtítulo", "Linha menor abaixo do nome."), s.subtitle);

            // ---- the base theme, shared by every season ----
            EditorGUILayout.Space(6);
            showBase = EditorGUILayout.Foldout(showBase, "Tema base da tela de carregamento (vale para TODAS as temporadas)", true);
            if (showBase)
            {
                EditorGUI.indentLevel++;
                var b = config.baseLoading;
                ArtBlock(ref b.art, ref b.bottomShade, ref b.showLogo, ref b.zoom, ref b.offset, "base");
                EditorGUILayout.LabelField("Posição e tamanho dos elementos (base)", EditorStyles.miniBoldLabel);
                LayoutRow("Nome", b.title, 0f, 325f);
                LayoutRow("Subtítulo", b.subtitle, 0f, 250f);
                LayoutRow("Barra", b.bar, 0f, 175f);
                LayoutRow("Carregando…", b.label, 0f, 85f);
                EditorGUI.indentLevel--;
            }

            // ---- what this season changes ----
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Esta temporada troca…", EditorStyles.boldLabel);
            var wantsArt = EditorGUILayout.ToggleLeft("a arte (usa uma imagem própria no lugar da do tema base)", s.customArt);
            if (wantsArt && !s.customArt)   // starts from the base's framing, so switching it on changes nothing until the art does
            {
                var b = config.baseLoading;
                s.loadingArt = b.art; s.artZoom = b.zoom; s.artOffset = b.offset; s.showLogo = b.showLogo; s.bottomShade = b.bottomShade;
            }
            s.customArt = wantsArt;
            if (s.customArt)
            {
                EditorGUI.indentLevel++;
                ArtBlock(ref s.loadingArt, ref s.bottomShade, ref s.showLogo, ref s.artZoom, ref s.artOffset, s.id);
                EditorGUI.indentLevel--;
            }

            var wantsLayout = EditorGUILayout.ToggleLeft("as posições e tamanhos (nome, subtítulo, barra, \"Carregando…\")", s.ownLayout);
            if (wantsLayout && !s.ownLayout)
            {
                var b = config.baseLoading;
                s.titleLayout = b.title.Copy(); s.subtitleLayout = b.subtitle.Copy(); s.barLayout = b.bar.Copy(); s.labelLayout = b.label.Copy();
            }
            s.ownLayout = wantsLayout;
            if (s.ownLayout)
            {
                EditorGUI.indentLevel++;
                LayoutRow("Nome", s.titleLayout, 0f, 325f);
                LayoutRow("Subtítulo", s.subtitleLayout, 0f, 250f);
                LayoutRow("Barra", s.barLayout, 0f, 175f);
                LayoutRow("Carregando…", s.labelLayout, 0f, 85f);
                EditorGUI.indentLevel--;
            }
            else EditorGUILayout.LabelField("Sem marcar, usa a arte e as posições do tema base. Mexer com o mouse nelas na prévia altera o tema base.", EditorStyles.miniLabel);

            // ---- props ----
            EditorGUILayout.Space(6);
            DrawProps(s);

            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(config);

            DrawWarnings(s);

            EditorGUILayout.Space();
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("Aplicar na tela de carregamento", GUILayout.Height(30))) Later(() => Apply(s));
            GUI.backgroundColor = Color.white;
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(isActive))
                if (GUILayout.Button("★ Só tornar ativa")) Mutate("Temporada ativa", () => config.activeSeasonId = s.id);
            if (GUILayout.Button("Abrir cena Loading")) Later(OpenLoadingScene);
            EditorGUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.None);
        }

        // Art, the two options that go with it, and its framing — for the base theme or for a season (same fields, so the same block).
        static void ArtBlock(ref Sprite art, ref bool shade, ref bool logo, ref float zoom, ref Vector2 offset, string importId)
        {
            // One line (an object field with a Sprite in a layout call draws a big square thumbnail).
            art = (Sprite)EditorGUI.ObjectField(EditorGUILayout.GetControlRect(), "Arte", art, typeof(Sprite), false);
            if (GUILayout.Button("Escolher arquivo de imagem…"))
            {
                var holder = art;
                Later(() => { var picked = ImportSprite(ArtFolder + importId, "loading"); if (picked != null) Assign(importId, picked); });
            }
            shade = EditorGUILayout.ToggleLeft("Escurecer a base da arte (para o nome e a barra aparecerem)", shade);
            logo = EditorGUILayout.ToggleLeft("Logo Magic Mail separado por cima (desligue se a arte já traz o logo)", logo);
            zoom = EditorGUILayout.Slider("Zoom da arte", zoom, 1f, 2.5f);
            offset.x = EditorGUILayout.Slider("Deslocar para a direita", offset.x, -540f, 540f);
            offset.y = EditorGUILayout.Slider("Deslocar para cima", offset.y, -960f, 960f);
            if (GUILayout.Button("Centralizar e sem zoom")) { zoom = 1f; offset = Vector2.zero; }
        }

        // The art picked by "Escolher arquivo" goes to the base theme or to the selected season, whichever block asked.
        static void Assign(string importId, Sprite sprite)
        {
            Undo.RecordObject(config, "Arte");
            if (importId == "base") config.baseLoading.art = sprite;
            else if (config.seasons.Count > 0) config.seasons[selected].loadingArt = sprite;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        // One element: X, Y and scale on a single line, with a button to put it back where it was by default.
        static void LayoutRow(string label, ElementLayout layout, float defaultX, float defaultY)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(86));
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 14f;
            layout.position.x = EditorGUILayout.FloatField("X", layout.position.x);
            layout.position.y = EditorGUILayout.FloatField("Y", layout.position.y);
            EditorGUIUtility.labelWidth = 44f;
            layout.scale = Mathf.Max(0.1f, EditorGUILayout.FloatField("Escala", layout.scale));
            EditorGUIUtility.labelWidth = oldWidth;
            if (GUILayout.Button(new GUIContent("↺", "Voltar ao padrão"), GUILayout.Width(24)))
            {
                layout.position = new Vector2(defaultX, defaultY);
                layout.scale = 1f;
            }
            EditorGUILayout.EndHorizontal();
        }

        // ---- props -------------------------------------------------------------------------------------------------

        // The decorations this season adds to the screen: pictures with a place, a size, a turn, a side, a tint and a movement.
        static void DrawProps(Season s)
        {
            EditorGUILayout.LabelField(new GUIContent("Adereços desta temporada",
                "Imagens postas por cima ou por trás da tela. Marque \"✥\" e arraste na prévia, ou digite os números."), EditorStyles.boldLabel);

            int remove = -1, moveFrom = -1, move = 0;
            for (var i = 0; i < s.props.Count; i++)
            {
                var p = s.props[i];
                if (p.screen != SeasonScreen.Loading) continue;
                EditorGUILayout.BeginVertical("helpBox");
                EditorGUILayout.BeginHorizontal();
                p.visible = EditorGUILayout.Toggle(p.visible, GUILayout.Width(16));
                var open = EditorGUILayout.Foldout(openProps.Contains(p), string.IsNullOrWhiteSpace(p.name) ? "Adereço" : p.name, true);
                if (open) openProps.Add(p); else openProps.Remove(p);
                var target = editTarget == FirstPropTarget + i;
                if (GUILayout.Toggle(target, new GUIContent("✥", "Mexer neste adereço com o mouse na prévia"), "Button", GUILayout.Width(28)) && !target)
                    editTarget = FirstPropTarget + i;
                if (GUILayout.Button("▲", GUILayout.Width(22))) { moveFrom = i; move = -1; }
                if (GUILayout.Button("▼", GUILayout.Width(22))) { moveFrom = i; move = 1; }
                if (GUILayout.Button("✕", GUILayout.Width(22))) remove = i;
                EditorGUILayout.EndHorizontal();

                if (open)
                {
                    p.name = EditorGUILayout.TextField("Nome", p.name);
                    p.sprite = (Sprite)EditorGUI.ObjectField(EditorGUILayout.GetControlRect(), "Imagem", p.sprite, typeof(Sprite), false);
                    EditorGUILayout.BeginHorizontal();
                    var oldWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 14f;
                    p.position.x = EditorGUILayout.FloatField("X", p.position.x);
                    p.position.y = EditorGUILayout.FloatField("Y", p.position.y);
                    EditorGUIUtility.labelWidth = 44f;
                    p.scale = Mathf.Max(0.05f, EditorGUILayout.FloatField("Escala", p.scale));
                    p.rotation = EditorGUILayout.FloatField("Giro", p.rotation);
                    EditorGUIUtility.labelWidth = oldWidth;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    p.inFront = EditorGUILayout.ToggleLeft(new GUIContent("Na frente", "Desligado: fica por trás do logo, do texto e da barra, logo acima da arte."), p.inFront, GUILayout.Width(90));
                    p.tint = EditorGUILayout.ColorField(GUIContent.none, p.tint, true, true, false, GUILayout.Width(60));
                    p.motion = (PropMotion)EditorGUILayout.EnumPopup(p.motion, GUILayout.Width(80));
                    using (new EditorGUI.DisabledScope(p.motion == PropMotion.None))
                    {
                        oldWidth = EditorGUIUtility.labelWidth;
                        EditorGUIUtility.labelWidth = 52f;
                        p.motionAmount = EditorGUILayout.Slider("Força", p.motionAmount, 0f, 5f);
                        p.motionSpeed = EditorGUILayout.Slider("Veloc.", p.motionSpeed, 0.1f, 5f);
                        EditorGUIUtility.labelWidth = oldWidth;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Adereço (escolher imagem…)")) Later(() => AddPropFromFile(s));
            if (GUILayout.Button("+ Adereço vazio")) Mutate("Novo adereço", () =>
            {
                var prop = new SeasonProp { name = "Adereço " + (s.props.Count + 1) };
                s.props.Add(prop);
                openProps.Add(prop);
                editTarget = FirstPropTarget + s.props.Count - 1;
            });
            EditorGUILayout.EndHorizontal();

            if (remove >= 0) { openProps.Remove(s.props[remove]); s.props.RemoveAt(remove); if (editTarget >= FirstPropTarget) editTarget = 0; }
            if (moveFrom >= 0 && moveFrom + move >= 0 && moveFrom + move < s.props.Count)
                (s.props[moveFrom], s.props[moveFrom + move]) = (s.props[moveFrom + move], s.props[moveFrom]);
        }

        static void AddPropFromFile(Season s)
        {
            var sprite = ImportSprite(ArtFolder + (string.IsNullOrWhiteSpace(s.id) ? "temporada" : s.id) + "/adereços", null);
            if (sprite == null) return;
            Undo.RecordObject(config, "Novo adereço");
            var prop = new SeasonProp { name = sprite.name, sprite = sprite };
            s.props.Add(prop);
            openProps.Add(prop);
            editTarget = FirstPropTarget + s.props.Count - 1;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        static void DrawWarnings(Season s)
        {
            var look = config.ResolveLoading(s);
            if (look.art == null)
            {
                EditorGUILayout.HelpBox("Sem arte (nem na temporada, nem no tema base): a tela de carregamento fica com a arte que já estava na cena.", MessageType.Warning);
                return;
            }
            var aspect = look.art.rect.width / look.art.rect.height;
            if (Mathf.Abs(aspect - 9f / 16f) > 0.04f)
                EditorGUILayout.HelpBox($"A arte tem {look.art.rect.width:0}x{look.art.rect.height:0} (proporção {aspect:0.00}); o ideal é 9:16 (0,56). Ela cobre a tela inteira e o excesso é cortado.", MessageType.Info);
            if (config.seasons.Count(x => x.id == s.id) > 1)
                EditorGUILayout.HelpBox("Há outra temporada com o mesmo id.", MessageType.Warning);
        }

        // ---- preview -----------------------------------------------------------------------------------------------

        // What the mouse moves in the preview: 0 the art, 1 the name, 2 the subtitle, 3 the bar, 4 "Carregando...", and from
        // FirstPropTarget on, the props of the season in order. The numbers are the same as in the form (and the game's).
        static int editTarget;
        const int FirstPropTarget = 5;
        static readonly string[] BaseTargetNames = { "Arte", "Nome", "Subtítulo", "Barra", "Carregando" };

        // The layout the game uses for an element of this season: the season's own, or the base theme's.
        static ElementLayout LayoutFor(Season s, int target)
        {
            var own = s.ownLayout;
            var b = config.baseLoading;
            switch (target)
            {
                case 1: return own ? s.titleLayout : b.title;
                case 2: return own ? s.subtitleLayout : b.subtitle;
                case 3: return own ? s.barLayout : b.bar;
                case 4: return own ? s.labelLayout : b.label;
                default: return null;
            }
        }

        // The loading screen as the game lays it out (1080 x 1920 reference): the art covering the screen, the shade on the
        // base, the props, name, subtitle, progress bar and "Carregando...". The lettering is only an approximation of the font.
        static void DrawPreview(float width)
        {
            if (config.seasons.Count == 0 || screenIndex != (int)SeasonScreen.Loading) return;
            var s = config.seasons[selected];
            var look = config.ResolveLoading(s);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Prévia da tela (aproximada)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Mexer com o mouse em:", EditorStyles.miniLabel);
            var names = BaseTargetNames.Concat(s.props.Select(p => "✥ " + p.name)).ToArray();
            editTarget = Mathf.Clamp(editTarget, 0, names.Length - 1);
            editTarget = GUILayout.SelectionGrid(editTarget, names, 3, EditorStyles.miniButton);
            var rect = GUILayoutUtility.GetRect(width, width * 1920f / 1080f, GUILayout.ExpandWidth(false));
            var k = rect.width / 1080f;

            // What is being edited, and whose numbers they are.
            var layout = LayoutFor(s, editTarget);
            var prop = editTarget >= FirstPropTarget ? s.props[editTarget - FirstPropTarget] : null;
            var inherited = (editTarget == 0 && !s.customArt) || (editTarget >= 1 && editTarget <= 4 && !s.ownLayout);
            EditorGUILayout.LabelField(
                (editTarget == 0 ? "Arraste = mover a arte · roda do mouse = zoom" : "Arraste = mover · roda do mouse = tamanho") +
                (inherited ? "  (altera o tema base)" : ""), EditorStyles.miniLabel);

            // Dragging moves what is selected and the wheel sizes it, like the fields of the form (saved when the mouse is let go).
            var e = Event.current;
            var canDrag = editTarget != 0 || look.art != null;
            if (rect.Contains(e.mousePosition) && canDrag)
            {
                if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    Undo.RecordObject(config, "Mover na tela");
                    var delta = new Vector2(e.delta.x, -e.delta.y) / k;     // the screen's y goes up
                    if (prop != null) prop.position += delta;
                    else if (layout != null) layout.position += delta;
                    else if (s.customArt) s.artOffset += delta;
                    else config.baseLoading.offset += delta;
                    EditorUtility.SetDirty(config);
                    e.Use(); repaintCb?.Invoke();
                }
                else if (e.type == EventType.ScrollWheel)
                {
                    Undo.RecordObject(config, "Tamanho na tela");
                    var factor = 1f - e.delta.y * 0.03f;
                    if (prop != null) prop.scale = Mathf.Clamp(prop.scale * factor, 0.05f, 8f);
                    else if (layout != null) layout.scale = Mathf.Clamp(layout.scale * factor, 0.1f, 4f);
                    else if (s.customArt) s.artZoom = Mathf.Clamp(s.artZoom * factor, 1f, 2.5f);
                    else config.baseLoading.zoom = Mathf.Clamp(config.baseLoading.zoom * factor, 1f, 2.5f);
                    EditorUtility.SetDirty(config);
                    e.Use(); repaintCb?.Invoke();
                }
            }
            if (e.type == EventType.MouseUp && e.button == 0 && rect.Contains(e.mousePosition)) AssetDatabase.SaveAssets();
            if (e.type != EventType.Repaint) return;

            // The art, the props, the shade and the real progress bar are drawn together by the bar's own renderer, in the
            // order the game stacks them, so the bar sits on the art exactly as in the game (and moves with the simulated
            // loading below); the lettering goes on top. If the bar cannot be shown, a plain approximation is drawn instead.
            var shown = s.props.Where(p => p.screen == SeasonScreen.Loading && p.visible && p.sprite != null).ToList();
            var rendered = MagicMail.EditorTools.MagicProgressBarPreview.DrawScreenPreview(rect, look, shown, repaintCb);
            if (!rendered) EditorGUI.DrawRect(rect, new Color(0.04f, 0.12f, 0.23f));
            GUI.BeginClip(rect);
            var local = new Rect(0, 0, rect.width, rect.height);

            if (!rendered && look.art != null)
            {
                // As LoadingScreen.FitArt: cover the 1080 x 1920 screen, then zoom and shift.
                var tex = look.art.texture;
                var r = look.art.textureRect;
                var uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);
                var aspect = r.width / r.height;
                var cover = 1080f / 1920f > aspect ? new Vector2(1080f, 1080f / aspect) : new Vector2(1920f * aspect, 1920f);
                var size = cover * Mathf.Max(0.1f, look.zoom) * k;
                var artRect = new Rect(local.center.x - size.x / 2f + look.offset.x * k, local.center.y - size.y / 2f - look.offset.y * k, size.x, size.y);
                GUI.DrawTextureWithTexCoords(artRect, tex, uv, false);
            }
            if (!rendered && look.bottomShade)
                GUI.DrawTexture(new Rect(0, local.height / 2f, local.width, local.height / 2f), ShadeTexture());

            var titleBox = Lettering(local, k, look.title, 130f, (s.title ?? "").ToUpperInvariant(), 80f, new Color(1f, 0.82f, 0.3f), new Color(0.25f, 0.05f, 0.4f));
            var subtitleBox = Lettering(local, k, look.subtitle, 64f, s.subtitle ?? "", 40f, new Color(1f, 0.95f, 0.82f), new Color(0.1f, 0.03f, 0.25f));

            var barWidth = MagicMail.EditorTools.MagicProgressBarPreview.ScreenBarWidth * look.bar.scale * k;
            var barHeight = barWidth * 107f / 1108f;
            var barBox = new Rect(local.center.x + look.bar.position.x * k - barWidth / 2f, local.height - look.bar.position.y * k - barHeight / 2f, barWidth, barHeight);
            if (!rendered)
            {
                EditorGUI.DrawRect(barBox, new Color(0.12f, 0.04f, 0.3f, 0.95f));
                EditorGUI.DrawRect(new Rect(barBox.x + 3 * k, barBox.y + 3 * k, (barBox.width - 6 * k) * 0.55f, barBox.height - 6 * k), new Color(0.75f, 0.35f, 1f));
            }
            var labelBox = Lettering(local, k, look.label, 70f, "Carregando...", 48f, new Color(1f, 0.95f, 0.82f), new Color(0.1f, 0.03f, 0.25f));

            // A thin frame around what the mouse is moving.
            if (editTarget == 1) Frame(titleBox);
            else if (editTarget == 2) Frame(subtitleBox);
            else if (editTarget == 3) Frame(barBox);
            else if (editTarget == 4) Frame(labelBox);
            else if (prop != null && prop.sprite != null)
            {
                var size = prop.sprite.rect.size * prop.scale * k;
                Frame(new Rect(local.center.x + prop.position.x * k - size.x / 2f, local.height - prop.position.y * k - size.y / 2f, size.x, size.y));
            }
            GUI.EndClip();
        }

        static void Frame(Rect r)
        {
            var c = new Color(1f, 0.92f, 0.2f, 0.9f);
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1.5f), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1.5f, r.width, 1.5f), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 1.5f, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - 1.5f, r.y, 1.5f, r.height), c);
        }

        // Draws a line of text where `layout` puts it (position and scale as the game uses them) and returns its box.
        static Rect Lettering(Rect screen, float k, ElementLayout layout, float height, string text, float size, Color color, Color outline)
        {
            var scale = Mathf.Max(0.1f, layout.scale);
            var centre = new Vector2(screen.center.x + layout.position.x * k, screen.height - layout.position.y * k);
            if (string.IsNullOrWhiteSpace(text))
                return new Rect(centre.x - 60f * k * scale, centre.y - 20f * k * scale, 120f * k * scale, 40f * k * scale);
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(6, Mathf.RoundToInt(size * scale * k)), clipping = TextClipping.Overflow,
            };
            var textSize = style.CalcSize(new GUIContent(text));
            var box = new Rect(centre.x - textSize.x / 2f - 4f, centre.y - height * scale * k / 2f, textSize.x + 8f, height * scale * k);
            var shift = Mathf.Max(1f, 2.5f * k * scale);
            style.normal.textColor = outline;
            foreach (var d in new[] { new Vector2(shift, 0), new Vector2(-shift, 0), new Vector2(0, shift), new Vector2(0, -shift) })
                GUI.Label(new Rect(box.x + d.x, box.y + d.y, box.width, box.height), text, style);
            style.normal.textColor = color;
            GUI.Label(box, text, style);
            return box;
        }

        static Texture2D ShadeTexture()
        {
            if (shadeTexture != null) return shadeTexture;
            const int h = 64;
            shadeTexture = new Texture2D(1, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            for (var y = 0; y < h; y++)
                shadeTexture.SetPixel(0, y, new Color(0.08f, 0.02f, 0.22f, Mathf.Pow(1f - y / (h - 1f), 1.7f) * 0.88f)); // row 0 = bottom
            shadeTexture.Apply();
            return shadeTexture;
        }

        // ---- actions -----------------------------------------------------------------------------------------------

        // Makes it the active season and writes it into the Loading scene.
        static void Apply(Season s)
        {
            config.activeSeasonId = s.id;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            var error = RoyalAvesLoadingBuilder.ApplySeasonToScene(s);
            status = error ?? $"\"{(string.IsNullOrWhiteSpace(s.title) ? s.id : s.title)}\" aplicada: é a temporada ativa e a cena Loading foi atualizada e salva. " +
                              "Para ver, abra a cena Loading e aperte Play (os adereços aparecem no Play).";
        }

        static void OpenLoadingScene()
        {
            if (!RoyalAvesTools.CanRunTool()) return;
            if (File.Exists(RoyalAvesAreaPlacer.ToDisk(ScenePath))) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            else status = "A cena " + ScenePath + " não existe (menu Royal Aves > Criar tela de loading).";
        }

        // Copies a picture from anywhere into `folder` (under Art/, where it becomes a sprite on its own) and returns the sprite.
        // fileName: the name to give it (keeps the extension), or null to keep the original name.
        static Sprite ImportSprite(string folder, string fileName)
        {
            var file = EditorUtility.OpenFilePanel("Imagem (png ou jpg)", "", "png,jpg,jpeg");
            if (string.IsNullOrEmpty(file)) return null;
            var data = Application.dataPath.Replace('\\', '/');
            var norm = file.Replace('\\', '/');
            string asset;
            if (norm.StartsWith(data)) asset = "Assets" + norm.Substring(data.Length);
            else
            {
                Directory.CreateDirectory(RoyalAvesAreaPlacer.ToDisk(folder));
                var name = (fileName ?? Path.GetFileNameWithoutExtension(file)) + Path.GetExtension(file).ToLowerInvariant();
                asset = folder + "/" + name;
                File.Copy(file, RoyalAvesAreaPlacer.ToDisk(asset), true);
            }
            AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceUpdate);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(asset);
            if (sprite == null)
                status = "A imagem foi copiada para " + asset + ", mas não virou sprite (a pasta precisa estar em Assets/RoyalAves/Art/).";
            return sprite;
        }

        // ---- helpers -----------------------------------------------------------------------------------------------

        static string UniqueId(string baseId)
        {
            var id = string.IsNullOrWhiteSpace(baseId) ? "temporada" : baseId;
            if (config.seasons.All(x => x.id != id)) return id;
            for (var n = 2; ; n++)
                if (config.seasons.All(x => x.id != id + "-" + n)) return id + "-" + n;
        }

        static void Mutate(string undoName, Action change)
        {
            Undo.RecordObject(config, undoName);
            change();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            repaintCb?.Invoke();
        }

        static void Later(Action action) => EditorApplication.delayCall += () => { action(); repaintCb?.Invoke(); };
    }
}
