using System.IO;
using RoyalAves.Meta;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class ChristmasBorderTools
    {
        const string RequestPath = "Logs/board-theme.request";
        const string Folder = "Assets/RoyalAves/Resources/UI/Board/";
        const string SettingsPath = Folder + "BoardBorderAppearance.asset";
        const string Menu = "Royal Aves/Tabuleiro/Tema/";
        static double nextPoll;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            nextPoll = EditorApplication.timeSinceStartup + 0.5d;
            if (!File.Exists(RequestPath)) return;
            try
            {
                var request = File.ReadAllText(RequestPath).Trim();
                if (request == "lights-status")
                {
                    var shader = Resources.Load<Shader>("UI/Board/SeasonalHudLights");
                    bool errors = shader == null;
                    if (shader != null)
                    {
                        var material = new Material(shader);
                        try
                        {
                            ShaderUtil.CompilePass(material, 0, true);
                            errors = ShaderUtil.ShaderHasError(shader);
                        }
                        finally { Object.DestroyImmediate(material); }
                    }
                    var lights = Object.FindObjectsByType<SeasonalHudLights>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    var status = "Shader errors=" + errors + "; Light components=" + lights.Length;
                    foreach (var light in lights)
                        status += "; enabled=" + light.enabled + ", pulse=" + light.CurrentPulse.ToString("F4");
                    File.WriteAllText("Logs/board-theme.result", status);
                    File.Delete(RequestPath);
                    return;
                }
                if (request == "status")
                {
                    File.WriteAllText("Logs/board-theme.result", "Play=" + EditorApplication.isPlaying +
                        "; Scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                    File.Delete(RequestPath);
                    return;
                }
                if (request == "preview")
                {
                    if (!EditorApplication.isPlaying)
                    {
                        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                        if (scene != "game" && scene != "gameStatic")
                        {
                            File.WriteAllText("Logs/board-theme.result", "Preview requer a cena game ou gameStatic aberta.");
                            File.Delete(RequestPath);
                            return;
                        }
                        SessionState.SetBool("MagicMail.ChristmasPreviewStartedPlay", true);
                        EditorApplication.isPlaying = true;
                        return;
                    }
                    if (SweetSugar.Scripts.Core.LevelManager.THIS?.field?.squaresArray == null) return;
                    BoardBorderRenderer.Refresh();
                    ScreenCapture.CaptureScreenshot("Logs/christmas-game-preview.png");
                    File.Delete(RequestPath);
                    File.WriteAllText("Logs/board-theme.result", "Captura solicitada: Logs/christmas-game-preview.png");
                    return;
                }
                if (request == "stop-preview")
                {
                    if (SessionState.GetBool("MagicMail.ChristmasPreviewStartedPlay", false))
                        EditorApplication.isPlaying = false;
                    SessionState.SetBool("MagicMail.ChristmasPreviewStartedPlay", false);
                    File.Delete(RequestPath);
                    return;
                }
                if (request == "capture")
                {
                    if (!EditorApplication.isPlaying) return;
                    BoardBorderRenderer.Refresh();
                    ScreenCapture.CaptureScreenshot("Logs/christmas-game-preview.png");
                    File.Delete(RequestPath);
                    File.WriteAllText("Logs/board-theme.result", "Captura solicitada: Logs/christmas-game-preview.png");
                    return;
                }
                var theme = File.ReadAllText(RequestPath).Trim() == "gold" ? BoardBorderTheme.Gold : BoardBorderTheme.Christmas;
                SetTheme(theme);
                File.Delete(RequestPath);
                File.WriteAllText("Logs/board-theme.result", "OK: borda " + theme + " salva e aplicada; imagens importadas sem compressão.");
            }
            catch (System.Exception exception)
            {
                File.Delete(RequestPath);
                File.WriteAllText("Logs/board-theme.result", exception.ToString());
                Debug.LogException(exception);
            }
        }

        [MenuItem(Menu + "Borda natalina")]
        public static void Christmas() => SetTheme(BoardBorderTheme.Christmas);

        [MenuItem(Menu + "Borda dourada fina")]
        public static void Gold() => SetTheme(BoardBorderTheme.Gold);

        public static void Prepare() => SetTheme(BoardBorderTheme.Christmas);

        [MenuItem("Royal Aves/Temporadas/Natal")]
        public static void ActivateChristmas() => SetTheme(BoardBorderTheme.Christmas);

        [MenuItem("Royal Aves/Temporadas/Padrão")]
        public static void ActivateDefault() => SetTheme(BoardBorderTheme.Gold);

        [MenuItem("Royal Aves/Temporadas/Ativar perfil selecionado", true)]
        static bool CanActivateProfile() => Selection.activeObject is BoardSeasonTheme;

        [MenuItem("Royal Aves/Temporadas/Ativar perfil selecionado")]
        static void ActivateProfile()
        {
            var profile = Selection.activeObject as BoardSeasonTheme;
            if (profile == null) return;
            var settings = AssetDatabase.LoadAssetAtPath<BoardBorderRenderer>(SettingsPath);
            if (settings == null) return;
            Undo.RecordObject(settings, "Trocar temporada");
            settings.customSeason = profile;
            settings.theme = BoardBorderTheme.Custom;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            if (EditorApplication.isPlaying) BoardBorderRenderer.Refresh();
        }

        static void SetTheme(BoardBorderTheme theme)
        {
            var settings = AssetDatabase.LoadAssetAtPath<BoardBorderRenderer>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<BoardBorderRenderer>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            Undo.RecordObject(settings, "Trocar temporada");
            if (theme == BoardBorderTheme.Christmas)
            {
                settings.garland = null;
                settings.cornerOrnament = null;
                settings.christmasHudFrame = Import(Folder + "Christmas/hud-frame-v4.png", out _);
                settings.snowCap = Import(Folder + "Christmas/snow-corner-v5.png", out float snowWidth,
                    new Vector2(366f / 1484f, 664f / 1060f));
                settings.snowVisibleWidth = snowWidth;
                settings.snowHeightScale = 509f / 397f;
                // Enlarge the cap to hide every bevel band along the rounded corner.
                settings.snowWidthInCells = snowWidth * 200f * 0.60f / 1168f * 1.15f;
            }
            settings.theme = theme;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            if (EditorApplication.isPlaying) BoardBorderRenderer.Refresh();
            Debug.Log("[Correio Mágico] Tema da borda: " + theme);
        }

        static Sprite Import(string path, out float visibleWidth, Vector2? supportPivot = null)
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(path));
            var pixels = texture.GetPixels32();
            int left = texture.width, right = 0, bottom = texture.height, top = 0;
            for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
            {
                if (pixels[y * texture.width + x].a < 64) continue;
                left = Mathf.Min(left, x); right = Mathf.Max(right, x);
                bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
            }
            var pivot = new Vector2((left + right + 1f) / (2f * texture.width),
                (bottom + top + 1f) / (2f * texture.height));
            if (supportPivot.HasValue) pivot = supportPivot.Value;
            visibleWidth = (right - left + 1f) / 200f;
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 200f;
            importer.spritePivot = pivot;
            var spriteSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(spriteSettings);
            spriteSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            spriteSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(spriteSettings);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
