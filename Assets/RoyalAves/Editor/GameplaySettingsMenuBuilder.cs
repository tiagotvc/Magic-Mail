using System.IO;
using RoyalAves.Meta;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    /// Cria o asset de configuração do menu de ajustes da fase (Resources/Settings/GameplaySettingsMenu.asset) com o quadro
    /// dos power-ups como botão redondo, os ícones atuais de música/som/vibração e um risco placeholder (PNG gerado), para
    /// você trocar pelas artes finais no Inspector. Roda sozinho uma vez se o asset não existir.
    public static class GameplaySettingsMenuBuilder
    {
        public const string AssetPath = "Assets/Royal Postman Art/Resources/Settings/GameplaySettingsMenu.asset";
        const string FramePath = "Assets/Royal Postman Art/Screens/Gameplay/Frames/Power ups/base_frame.png";
        const string SlashFolder = "Assets/Royal Postman Art/Screens/Gameplay/Settings";
        const string SlashPath = SlashFolder + "/risco_placeholder.png";

        [InitializeOnLoadMethod]
        static void AutoBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                if (File.Exists(AssetPath)) return;
                try { Build(); }
                catch (System.Exception e) { Debug.LogError("Não consegui criar a configuração do menu de ajustes: " + e); }
            };
        }

        [MenuItem("Royal Aves/Menu de ajustes da fase/Abrir configuração")]
        static void Open()
        {
            if (!File.Exists(AssetPath)) Build();
            var asset = AssetDatabase.LoadAssetAtPath<GameplaySettingsMenuConfig>(AssetPath);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        [MenuItem("Royal Aves/Menu de ajustes da fase/Refazer configuração (apaga edições)")]
        static void Rebuild()
        {
            if (File.Exists(AssetPath) && !EditorUtility.DisplayDialog("Refazer", "Isto volta a configuração do menu ao padrão. Continuar?", "Refazer", "Cancelar")) return;
            AssetDatabase.DeleteAsset(AssetPath);
            Build();
        }

        public static void Build()
        {
            EnsureFolder("Assets/Royal Postman Art/Resources/Settings");
            var config = AssetDatabase.LoadAssetAtPath<GameplaySettingsMenuConfig>(AssetPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameplaySettingsMenuConfig>();
                AssetDatabase.CreateAsset(config, AssetPath);
            }
            config.buttonFrame = BoosterIntroBuilder.LoadFull(FramePath);
            var art = LobbyFeaturesConfig.Instance;
            if (art != null)
            {
                config.musicIcon = art.gameMenuMusicOn;
                config.soundIcon = art.gameMenuSoundOn;
                config.vibrationIcon = art.gameMenuVibrationOn;
                config.exitIcon = art.gameMenuExit;
            }
            config.slash = CreateSlashPlaceholder();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("Configuração do menu de ajustes criada em " + AssetPath + ". Troque ícones e risco no Inspector.");
        }

        /// PNG do risco: diagonal branca com contorno escuro (será pintada com a cor do campo "Slash Color"). Placeholder para você substituir.
        static Sprite CreateSlashPlaceholder()
        {
            EnsureFolder(SlashFolder);
            if (!File.Exists(SlashPath))
            {
                const int n = 256;
                var texture = new Texture2D(n, n, TextureFormat.RGBA32, false);
                for (var y = 0; y < n; y++)
                    for (var x = 0; x < n; x++)
                    {
                        var d = Mathf.Abs((x - (n - 1 - y)) / 1.4142f);   // diagonal do canto superior esquerdo ao inferior direito
                        var along = ((x) + (n - 1 - y)) / 2f / n;
                        var inside = along > 0.06f && along < 0.94f;
                        var core = inside ? Mathf.Clamp01((12f - d) / 3f) : 0f;
                        var edge = inside ? Mathf.Clamp01((20f - d) / 3f) : 0f;
                        var c = Color.Lerp(new Color(0.12f, 0.04f, 0.06f, 1f), Color.white, core);
                        texture.SetPixel(x, y, new Color(c.r, c.g, c.b, edge));
                    }
                texture.Apply();
                File.WriteAllBytes(SlashPath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(SlashPath);
            }
            return BoosterIntroBuilder.LoadFull(SlashPath);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace("\\", "/");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
