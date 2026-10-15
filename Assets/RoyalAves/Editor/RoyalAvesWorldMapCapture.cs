using System;
using System.IO;
using System.Linq;
using System.Text;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    // Captura offline do mapa 3D (sem play mode e sem tocar a cena aberta): renderiza a camera do mapa para PNG,
    // projeta medalhoes na tela e audita oclusao de props. Aciona por menu ou por um arquivo de pedido em
    // Temp/worldmap-capture.json (lido a cada ~0.7 s pelo editor aberto). Resultado em Temp/worldmap-capture.done.
    [InitializeOnLoad]
    public static class RoyalAvesWorldMapCapture
    {
        [Serializable]
        public class Request
        {
            public string outDir = "Tools/level-map/previews/world3d";
            public string prefix = "shot";
            public float[] cursors = { 14.55f };
            public int width = 720;
            public int height = 1280;
            public int levelCount = 100;
            public int nextLevel = 15;
            public int futureShown = 5;
            public float time = 1f;
            public bool audit = true;
            public float renderScale = 0; // 0 = usa o valor do kit
            public float cameraHeight = 0, cameraBack = 0, pitch = 0, fov = 0, radius = 0, tokenScale = 0; // 0 = usa o valor do kit (para depurar a geometria)
            public float night = -1;       // 0 dia .. 1 noite (-1 = valor do kit)
            public bool testLight;        // injeta dois raios de sol de teste (memoria, nao salva)
            public bool ui;               // captura pela janela de preview do lobby (HUD + RawImage + CurvedLevelMap)
            public bool lava;
            public WorldMapIslands islands;
            public bool diagnose;         // anexa lista de renderers e do pipeline ao relatorio
            public bool sheet;            // alem dos PNGs, gera uma folha de contato com todos os quadros
            public int minVersion = 0;    // o pedido so roda quando o editor ja compilou esta versao da ferramenta
        }

        public const int Version = 44;
        static readonly string Root = Path.GetDirectoryName(Application.dataPath);
        static readonly string RequestPath = Path.Combine(Root, "Temp", "worldmap-capture.json");
        static readonly string DonePath = Path.Combine(Root, "Temp", "worldmap-capture.done");
        static double next;

        static RoyalAvesWorldMapCapture() => EditorApplication.update += Poll;

        [MenuItem("Royal Aves/Mapa 3D/Capturar trecho atual (PNG)")]
        static void Menu() => Execute(new Request());

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < next || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            next = EditorApplication.timeSinceStartup + 0.7;
            if (!File.Exists(RequestPath)) return;
            Request request;
            try
            {
                request = JsonUtility.FromJson<Request>(File.ReadAllText(RequestPath));
                if (request.minVersion > Version) return; // codigo antigo: espera a recompilacao
                File.Delete(RequestPath);
            }
            catch { return; }
            Execute(request);
        }

        static void Execute(Request request)
        {
            var report = new StringBuilder();
            try { Run(request, report); report.Insert(0, "OK\n"); }
            catch (Exception e) { report.Insert(0, "ERRO: " + e + "\n"); }
            Directory.CreateDirectory(Path.GetDirectoryName(DonePath));
            File.WriteAllText(DonePath, report.ToString());
        }

        static void Run(Request r, StringBuilder report)
        {
            var kit = RoyalAvesWorldMapSetup.Ensure(true);
            if (!kit.Ready) throw new Exception($"WorldMapKit incompleto (renderer {kit.rendererIndex}). Layer/renderer ainda nao aplicados? Tente de novo.");
            if (LayerMask.NameToLayer(WorldMapKit.LayerName) < 0) throw new Exception("Layer WorldMap ausente.");
            var art = AssetDatabase.LoadAssetAtPath<LobbyFeaturesConfig>("Assets/RoyalAves/Resources/RoyalAvesFeatures.asset");
            var savedNight = kit.night;
            if (r.night >= 0) kit.night = r.night;
            try { if (r.ui) { RunUi(r, report); return; } RunOffline(r, kit, art, report); }
            finally { kit.night = savedNight; }
        }

        static void RunOffline(Request r, WorldMapKit kit, LobbyFeaturesConfig art, StringBuilder report)
        {
            var saved = (kit.renderScale, kit.cameraHeight, kit.cameraBack, kit.pitch, kit.fov, kit.radius, kit.tokenScale);
            if (r.renderScale > 0) kit.renderScale = r.renderScale;
            if (r.cameraHeight > 0) kit.cameraHeight = r.cameraHeight;
            if (r.cameraBack > 0) kit.cameraBack = r.cameraBack;
            if (r.pitch > 0) kit.pitch = r.pitch;
            if (r.fov > 0) kit.fov = r.fov;
            if (r.radius > 0) kit.radius = r.radius;
            if (r.tokenScale > 0) kit.tokenScale = r.tokenScale;
            // Restos de capturas anteriores (camera/geometria na mesma layer) apareceriam como medalhoes fantasma.
            var leftovers = Resources.FindObjectsOfTypeAll<WorldMapView>().Where(v => v != null && (v.hideFlags & HideFlags.HideAndDontSave) != 0).ToArray();
            foreach (var old in leftovers) old.Dispose();
            report.AppendLine($"views remanescentes removidas: {leftovers.Length}");
            var files = new System.Collections.Generic.List<string>();
            var view = WorldMapView.Create(null, art, kit, r.levelCount, r.futureShown, true,r.islands);
            if (view == null) throw new Exception("WorldMapView.Create retornou null.");
            try
            {
                view.TimeOverride = r.time;
                view.SetFixedSize(r.width, r.height);
                var dir = Path.IsPathRooted(r.outDir) ? r.outDir : Path.Combine(Root, r.outDir);
                Directory.CreateDirectory(dir);
                var aspect = (float)r.width / r.height;
                foreach (var cursor in r.cursors)
                {
                    if (r.testLight && view.Props is System.Collections.Generic.List<PropPlacement> tl && !tl.Exists(x => x.kind == PropKind.LightShaft))
                    {
                        foreach (var (lv, off, rot) in new[] { (3.0f, -2.2f, 50f), (3.6f, 2.2f, -50f) })
                            tl.Add(new PropPlacement { id = "teste" + lv, s = view.Route.MetersAt(lv), offset = off, height = 1, kind = PropKind.LightShaft, sprite = kit.meteorSprite, rotation = rot, intensity = .35f, seed = (int)(lv * 10), fixedPlacement = true });
                        tl.Sort((a, b) => a.s.CompareTo(b.s));
                    }
                    view.Tick(cursor, r.nextLevel, false);
                    view.Tick(cursor, r.nextLevel, false); // segunda passada: o primeiro quadro cria o RenderTexture
                    view.RenderNow();
                    var file = Path.Combine(dir, $"{r.prefix}_{cursor:0.00}.png".Replace(',', '.'));
                    Save(view, file, view.Route.PaletteAt(view.Route.MetersAt(cursor) + 18).haze);
                    files.Add(file);
                    report.AppendLine($"cursor {cursor:0.00} -> {file}");
                    var tokens = new StringBuilder();
                    var legible = 0;
                    for (var level = Mathf.Max(1, Mathf.FloorToInt(cursor)); level <= Mathf.Min(WorldMapRoute.Positions, Mathf.CeilToInt(cursor) + 16); level++)
                    {
                        if (!view.ProjectLevel(level, cursor, aspect, out var p) || p.y < 0 || p.y > 1 || p.x < 0 || p.x > 1) continue;
                        var width = TokenWidthFraction(view, level, cursor, aspect);
                        if (width >= 0.07f) legible++;
                        tokens.Append($" L{level}(y={p.y:0.00},w={width:0.00})");
                    }
                    report.AppendLine($"  medalhoes na tela, largura>=7%: {legible} |{tokens}");
                    { var ls = Resources.Load<WorldMapLightSet>(WorldMapLightSet.ResourcePath); report.AppendLine("  luzes: " + (ls == null ? "ASSET NULL" : ls.shafts.Count + " itens; 1a: nivel " + (ls.shafts.Count > 0 ? ls.shafts[0].level + " lat " + ls.shafts[0].lateral + " ang " + ls.shafts[0].angle + " int " + ls.shafts[0].intensity + " en " + ls.shafts[0].enabled : "-"))); }
                    report.AppendLine("  " + PickTest(view, cursor, aspect));
                    foreach (var pr in view.Props) if (pr.kind == PropKind.Mailbox && view.Route.LevelAt(pr.s) < cursor + 8) report.AppendLine($"  caixa: nivel {view.Route.LevelAt(pr.s):0.00} lateral {pr.offset:0.00} altura {pr.height:0.00}");
                }
                if (r.sheet) MakeSheet(files, Path.Combine(dir, r.prefix + "_sheet.png"));
                if (r.diagnose) report.AppendLine(view.Diagnose());
                if (r.diagnose) report.AppendLine(DescribePipeline(kit));
                if (r.audit) Audit(view, report);
            }
            finally
            {
                (kit.renderScale, kit.cameraHeight, kit.cameraBack, kit.pitch, kit.fov, kit.radius, kit.tokenScale) = saved;
                view.Dispose();
                WorldMapCards.Clear();
            }
        }

        // Integracao: usa a janela de preview do lobby (preview scene), que cria CurvedLevelMap com HUD e RawImage.
        static void RunUi(Request r, StringBuilder report)
        {
            var type = typeof(RoyalAvesLevelMapPreview);
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var window = ScriptableObject.CreateInstance<RoyalAvesLevelMapPreview>();
            WorldMapIslands overrideIslands=r.islands;
            var temporary=false;
            if(r.lava&&overrideIslands==null)
            {
                overrideIslands=UnityEngine.Object.Instantiate(Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath));temporary=true;
                foreach(var island in overrideIslands.islands)
                {island.terrain=WorldMapTerrainTheme.LavaLand;island.volcanoes=true;island.weather=WorldMapWeather.Embers;island.skyTop=new Color(1,.65f,.32f);island.skyBottom=new Color(1,.88f,.53f);}
            }
            var files = new System.Collections.Generic.List<string>();
            try
            {
                var dir = Path.IsPathRooted(r.outDir) ? r.outDir : Path.Combine(Root, r.outDir);
                Directory.CreateDirectory(dir);
                foreach (var cursor in r.cursors)
                {
                    type.GetField("level", flags).SetValue(window, cursor);
                    type.GetField("captureIslands",flags).SetValue(window,overrideIslands);
                    type.GetMethod("Build", flags).Invoke(window, new object[] { r.width, r.height, r.nextLevel });
                    var map=(CurvedLevelMap)type.GetField("map",flags).GetValue(window);map.World.TimeOverride=r.time;
                    var file = Path.Combine(dir, $"{r.prefix}_{cursor:0.00}.png".Replace(',', '.'));
                    type.GetMethod("Save", flags).Invoke(window, new object[] { file, false });
                    files.Add(file);
                    report.AppendLine($"ui cursor {cursor:0.00} -> {file}");
                }
                if (r.sheet) MakeSheet(files, Path.Combine(dir, r.prefix + "_sheet.png"));
            }
            finally
            {
                type.GetMethod("Cleanup", flags).Invoke(window, null);
                UnityEngine.Object.DestroyImmediate(window);
                if(temporary)UnityEngine.Object.DestroyImmediate(overrideIslands);
                WorldMapCards.Clear();
            }
        }

        static void MakeSheet(System.Collections.Generic.List<string> files, string path)
        {
            const int h = 640;
            var tiles = new System.Collections.Generic.List<Texture2D>();
            foreach (var f in files)
            {
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                t.LoadImage(File.ReadAllBytes(f));
                tiles.Add(t);
            }
            var width = 0;
            foreach (var t in tiles) width += Mathf.RoundToInt(t.width * (h / (float)t.height)) + 6;
            var sheet = new Texture2D(width, h, TextureFormat.RGBA32, false);
            var clear = new Color32[width * h];
            for (var i = 0; i < clear.Length; i++) clear[i] = new Color32(40, 40, 48, 255);
            sheet.SetPixels32(clear);
            var x0 = 0;
            foreach (var t in tiles)
            {
                var w = Mathf.RoundToInt(t.width * (h / (float)t.height));
                for (var y = 0; y < h; y++)
                    for (var x = 0; x < w; x++)
                        sheet.SetPixel(x0 + x, y, t.GetPixelBilinear((x + 0.5f) / w, (y + 0.5f) / h));
                x0 += w + 6;
                UnityEngine.Object.DestroyImmediate(t);
            }
            sheet.Apply();
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
        }

        static string DescribePipeline(WorldMapKit kit)
        {
            var asset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (asset == null) return "pipeline: null";
            var so = new SerializedObject(asset);
            var list = so.FindProperty("m_RendererDataList");
            var sb = new StringBuilder($"pipeline {asset.name} ({asset.GetType().Name}) renderers={list.arraySize} kit.rendererIndex={kit.rendererIndex}:");
            for (var i = 0; i < list.arraySize; i++)
            {
                var o = list.GetArrayElementAtIndex(i).objectReferenceValue;
                sb.Append($" [{i}] {(o != null ? o.name + "/" + o.GetType().Name : "NULL")}");
            }
            return sb.ToString();
        }

        // Cada medalhao visivel, clicado no centro projetado, deve devolver a propria fase (colliders seguem a curva).
        static string PickTest(WorldMapView view, float cursor, float aspect)
        {
            int tested = 0, hit = 0, blocked = 0;
            var misses = new StringBuilder();
            for (var level = Mathf.Max(1, Mathf.FloorToInt(cursor)); level <= Mathf.Min(WorldMapRoute.Positions, Mathf.CeilToInt(cursor) + 8); level++)
            {
                if (!view.ProjectLevel(level, cursor, aspect, out var p) || p.y < 0.05f || p.y > 0.95f || p.x < 0.02f || p.x > 0.98f) continue;
                tested++;
                var got = view.PickViewport(p);
                if (got == level) hit++;
                else { blocked++; misses.Append($" L{level}->{got}"); }
            }
            return $"clique: {hit}/{tested} acertos{(blocked > 0 ? " | divergencias:" + misses : "")}";
        }

        static float TokenWidthFraction(WorldMapView view, int level, float cursor, float aspect)
        {
            var model = view.CameraModel;
            var cursorM = view.Route.MetersAt(cursor);
            var s = view.Route.Pos(level);
            var x = view.Route.CenterX(s);
            if (!model.Project(cursorM, s, x - view.TokenHalfWidth, 0.05f, aspect, out var a, out _)) return 0;
            model.Project(cursorM, s, x + view.TokenHalfWidth, 0.05f, aspect, out var b, out _);
            return Mathf.Abs(b.x - a.x);
        }

        static void Save(WorldMapView view, string file, Color haze)
        {
            var rt = (RenderTexture)view.Target;
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            var pixels = tex.GetPixels32();
            var top = new Color(0.72f, 0.84f, 0.98f);
            var bottom = Color.Lerp(haze, Color.white, 0.4f);
            for (var y = 0; y < rt.height; y++)
            {
                var sky = (Color32)Color.Lerp(bottom, top, Mathf.Clamp01((y / (float)rt.height - 0.45f) * 2.2f));
                for (var x = 0; x < rt.width; x++)
                {
                    var i = y * rt.width + x;
                    var p = pixels[i];
                    var a = p.a / 255f;
                    pixels[i] = new Color32(
                        (byte)(p.r * a + sky.r * (1 - a)), (byte)(p.g * a + sky.g * (1 - a)), (byte)(p.b * a + sky.b * (1 - a)), 255);
                }
            }
            tex.SetPixels32(pixels);
            File.WriteAllBytes(file, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        // Para cada avanco do cursor: nenhum prop mais perto que um medalhao pode cobrir o retangulo dele.
        static void Audit(WorldMapView view, StringBuilder report)
        {
            var route = view.Route;
            var model = view.CameraModel;
            var props = view.Props;
            foreach (var aspect in new[] { 9f / 16f, 9f / 19.5f, 3f / 4f })
            {
                var violations = 0;
                var samples = 0;
                var examples = new StringBuilder();
                for (var cursor = 1f; cursor <= WorldMapRoute.Positions; cursor += 0.5f)
                {
                    var cm = route.MetersAt(cursor);
                    foreach (var p in props)
                    {
                        if (p.s < cm - 1 || p.s > cm + 40) continue;
                        var width = WorldMapCards.Width(p.sprite, p.height);
                        if (p.kind == PropKind.FloatingMail) continue;
                        var cx = route.CenterX(p.s) + WorldMapTerrain.Lateral(p.offset, p.s - cm);
                        var foot = WorldMapTerrain.SurfaceHeight(route,p.s,p.offset);
                        if (!model.Project(cm, p.s, cx - width * 0.5f, foot, aspect, out var a, out var da)) continue;
                        model.Project(cm, p.s, cx + width * 0.5f, foot + p.height, aspect, out var b, out _);
                        var rect = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
                        var from = Mathf.CeilToInt(route.LevelAt(p.s + 0.2f));
                        var to = Mathf.Min(WorldMapRoute.Positions, Mathf.FloorToInt(route.LevelAt(cm + 46)));
                        for (var level = from; level <= to; level++)
                        {
                            var s = route.Pos(level);
                            if (!model.SurfaceVisible(cm, s)) continue;
                            var tx = route.CenterX(s);
                            if (!model.Project(cm, s, tx - view.TokenHalfWidth, 0, aspect, out var t0, out var dt) || dt <= da) continue;
                            model.Project(cm, s, tx + view.TokenHalfWidth, 0.10f, aspect, out var t1, out _);
                            var tokenRect = Rect.MinMaxRect(Mathf.Min(t0.x, t1.x), Mathf.Min(t0.y, t1.y), Mathf.Max(t0.x, t1.x), Mathf.Max(t0.y, t1.y));
                            samples++;
                            if (!rect.Overlaps(tokenRect)) continue;
                            violations++;
                            if (examples.Length < 400) examples.Append($" [cursor {cursor:0.0}: {p.kind}@{p.s:0.0}m cobre L{level}]");
                        }
                    }
                }
                report.AppendLine($"auditoria oclusao aspecto {aspect:0.000}: {violations} violacoes em {samples} testes.{examples}");
            }
        }
    }
}
