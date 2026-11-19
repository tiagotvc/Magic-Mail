using System.IO;
using RoyalAves.Effects;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    /// Cria o prefab das moedas do fim de fase (Assets/Royal Postman Art/Resources/Effects/LevelEnd/LevelEndCoinEffect.prefab)
    /// com a moeda e as máscaras de Royal Postman Art/VFX/Power ups. Roda sozinho se o prefab não existir; depois ele é seu:
    /// ajuste cores, tamanhos, contagens e tempos direto no Inspector. Se o prefab já existe, só liga a arte que estiver vazia.
    public static class LevelEndCoinEffectBuilder
    {
        public const string PrefabPath = "Assets/Royal Postman Art/Resources/Effects/LevelEnd/LevelEndCoinEffect.prefab";
        const string Masks = "Assets/Royal Postman Art/VFX/Power ups/Mascaras_PNG/";
        const string CoinPath = "Assets/Royal Postman Art/Screens/Modal/coin.png";
        const string AdditivePath = "Assets/Royal Postman Art/VFX/Materials/ImpactAdditive.mat";

        [InitializeOnLoadMethod]
        static void AutoBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                if (!File.Exists(Masks + "00_Comuns/01_radial_glow.png")) return;
                try
                {
                    if (File.Exists(PrefabPath)) LinkMissingArt();
                    else Build();
                }
                catch (System.Exception e) { Debug.LogError("Não consegui criar o prefab das moedas do fim de fase: " + e); }
            };
        }

        [MenuItem("Royal Aves/VFX/Moedas do fim de fase/Recriar prefab (apaga edições)")]
        static void RebuildMenu()
        {
            if (File.Exists(PrefabPath) && !EditorUtility.DisplayDialog("Recriar prefab",
                    "Isto sobrescreve LevelEndCoinEffect.prefab e perde os ajustes feitos nele. Continuar?", "Recriar", "Cancelar")) return;
            Build();
        }

        [MenuItem("Royal Aves/VFX/Moedas do fim de fase/Selecionar prefab")]
        static void SelectPrefab()
        {
            if (!File.Exists(PrefabPath)) Build();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        // Teste visual: em Play, com uma fase aberta, mostra o efeito com um valor inventado e devolve os objetivos no fim.
        [MenuItem("Royal Aves/VFX/Moedas do fim de fase/Testar na fase aberta (Play): +350 moedas")]
        static void TestSmall() => Test(350);

        [MenuItem("Royal Aves/VFX/Moedas do fim de fase/Testar na fase aberta (Play): +2.400 moedas")]
        static void TestLarge() => Test(2400);

        [MenuItem("Royal Aves/VFX/Moedas do fim de fase/Testar na fase aberta (Play): +350 moedas", true)]
        [MenuItem("Royal Aves/VFX/Moedas do fim de fase/Testar na fase aberta (Play): +2.400 moedas", true)]
        static bool CanTest() => EditorApplication.isPlaying;

        static void Test(int coins)
        {
            if (LevelEndCoins.Simulate(coins) == null)
                Debug.LogWarning("Moedas do fim de fase: não achei o prefab ou o HUD dos objetivos (abra uma fase antes de testar).");
        }

        static void Assign(LevelEndCoinEffect effect, bool onlyMissing)
        {
            if (!onlyMissing || effect.coinSprite == null) effect.coinSprite = Load(CoinPath, false);
            if (!onlyMissing || effect.glow == null) effect.glow = Load(Masks + "00_Comuns/01_radial_glow.png", true);
            if (!onlyMissing || effect.sparkle4 == null) effect.sparkle4 = Load(Masks + "00_Comuns/02_sparkle_4_pontas.png", true);
            if (!onlyMissing || effect.sparkle6 == null) effect.sparkle6 = Load(Masks + "00_Comuns/03_sparkle_6_pontas.png", true);
            if (!onlyMissing || effect.dot == null) effect.dot = Load(Masks + "00_Comuns/07_particula_ponto.png", true);
            if (!onlyMissing || effect.ring == null) effect.ring = Load(Masks + "01_Match_Normal/01_anel_unico_suave.png", true);
            if (!onlyMissing || effect.additive == null) effect.additive = AssetDatabase.LoadAssetAtPath<Material>(AdditivePath);
        }

        /// Prefab que já existe: liga só o que estiver vazio, sem recriar (os ajustes feitos nele ficam).
        static void LinkMissingArt()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var current = asset != null ? asset.GetComponent<LevelEndCoinEffect>() : null;
            if (current == null) return;
            if (current.coinSprite != null && current.glow != null && current.sparkle4 != null && current.sparkle6 != null &&
                current.dot != null && current.ring != null && current.additive != null) return;
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var effect = root.GetComponent<LevelEndCoinEffect>();
                Assign(effect, true);
                // Só grava se alguma arte foi mesmo ligada (um arquivo ausente não regrava o prefab a cada recompilação).
                var linked = (effect.coinSprite != null) != (current.coinSprite != null) || (effect.glow != null) != (current.glow != null) ||
                             (effect.sparkle4 != null) != (current.sparkle4 != null) || (effect.sparkle6 != null) != (current.sparkle6 != null) ||
                             (effect.dot != null) != (current.dot != null) || (effect.ring != null) != (current.ring != null) ||
                             (effect.additive != null) != (current.additive != null);
                if (linked) PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        // A moeda só é carregada (sem mexer na importação, que outras telas usam); as máscaras de luz usam malha FullRect.
        static Sprite Load(string path, bool mask)
        {
            if (!File.Exists(path)) return null;
            if (!mask) return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var sprite = BoosterIntroBuilder.LoadFull(path);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteMeshType != SpriteMeshType.FullRect)
                {
                    settings.spriteMeshType = SpriteMeshType.FullRect;     // brilhos macios não podem ser recortados num polígono
                    importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }
            return sprite;
        }

        public static void Build()
        {
            EnsureFolder("Assets/Royal Postman Art/Resources/Effects/LevelEnd");
            var root = new GameObject("LevelEndCoinEffect");
            try
            {
                Assign(root.AddComponent<LevelEndCoinEffect>(), false);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Prefab das moedas do fim de fase criado em " + PrefabPath);
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
