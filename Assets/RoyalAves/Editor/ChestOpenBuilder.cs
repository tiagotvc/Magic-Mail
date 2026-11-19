using System.Collections.Generic;
using System.IO;
using RoyalAves.Effects;
using RoyalAves.Meta;
using SweetSugar.Scripts.GUI.Boost;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    /// Cria o prefab da tela de abertura de baú (Resources/Effects/Chest/ChestOpenScreen.prefab): os ajustes da animação,
    /// as máscaras de luz do projeto, o material aditivo, as figuras dos power-ups e o modelo do brilho que fica atrás de
    /// cada um. Roda sozinho se o prefab não existir; depois ele é seu (ajuste tudo no Inspector).
    /// O menu Royal Aves > Efeitos > Baú também abre a tela no Play, para testar sem existir um sistema de baús.
    public static class ChestOpenBuilder
    {
        public const string PrefabPath = "Assets/Royal Postman Art/Resources/Effects/Chest/ChestOpenScreen.prefab";
        const string Masks = "Assets/Royal Postman Art/VFX/ImpactMasks/";
        const string AdditivePath = "Assets/Royal Postman Art/VFX/Materials/ImpactAdditive.mat";
        const string VersionKey = "RoyalAves.ChestOpen.Version";
        const int Version = 1;

        // Figuras que já existem no projeto para cada power-up.
        static readonly (BoostType type, string path)[] IconPaths =
        {
            (BoostType.Arrow, "Assets/Royal Postman Art/Screens/Gameplay/Power ups/Arrow.png"),
            (BoostType.Hammer, "Assets/RoyalAves/Art/Boosters/hammer.png"),
            (BoostType.Cannon, "Assets/RoyalAves/Art/Boosters/cannon.png"),
            (BoostType.Cap, "Assets/RoyalAves/Art/Boosters/cap.png"),
        };

        [InitializeOnLoadMethod]
        static void AutoBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                try
                {
                    var known = EditorPrefs.GetInt(VersionKey, 0);
                    if (!File.Exists(PrefabPath)) Build();
                    else if (known == 0) EditorPrefs.SetInt(VersionKey, Version);   // prefab veio do repositório: não mexe nele
                    else if (known < Version) Build();                              // o desenho padrão mudou: refaz uma vez
                }
                catch (System.Exception e) { Debug.LogError("Não consegui criar o prefab da abertura de baú: " + e); }
            };
        }

        [MenuItem("Royal Aves/Efeitos/Baú/Testar com 1 prêmio (no Play)", false, 10)]
        static void Test1() => Test(1);

        [MenuItem("Royal Aves/Efeitos/Baú/Testar com 3 prêmios (no Play)", false, 11)]
        static void Test3() => Test(3);

        [MenuItem("Royal Aves/Efeitos/Baú/Testar com 5 prêmios (no Play)", false, 12)]
        static void Test5() => Test(5);

        [MenuItem("Royal Aves/Efeitos/Baú/Testar com 8 prêmios - grade (no Play)", false, 13)]
        static void Test8() => Test(8);

        [MenuItem("Royal Aves/Efeitos/Baú/Selecionar prefab", false, 30)]
        static void SelectPrefab()
        {
            if (!File.Exists(PrefabPath)) Build();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        [MenuItem("Royal Aves/Efeitos/Baú/Recriar prefab (apaga edições)", false, 31)]
        static void RebuildMenu()
        {
            if (File.Exists(PrefabPath) && !EditorUtility.DisplayDialog("Recriar prefab",
                    "Isto sobrescreve ChestOpenScreen.prefab e perde os ajustes (e a arte do baú) feitos nele. Continuar?", "Recriar", "Cancelar")) return;
            Build();
        }

        /// Abre a tela com prêmios de exemplo. Só mostra: não credita nada ao jogador.
        static void Test(int count)
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Abertura de baú", "Entre no Play primeiro (em qualquer cena) e escolha o teste de novo.", "OK");
                return;
            }
            var types = new[] { BoostType.Hammer, BoostType.Arrow, BoostType.Cannon, BoostType.Cap };
            var amounts = new[] { 2, 1, 3, 1, 5, 2, 10, 1 };
            var rewards = new List<ChestReward>();
            for (var i = 0; i < count; i++) rewards.Add(new ChestReward(types[i % types.Length], amounts[i % amounts.Length]));
            ChestOpenScreen.Show(rewards, () => Debug.Log("Abertura de baú (teste): tela fechada, " + count + " prêmio(s) mostrado(s)."));
        }

        static Sprite LoadSprite(string path)
        {
            if (!File.Exists(path)) return null;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite found) return found;
            return null;
        }

        public static void Build()
        {
            EnsureFolder("Assets/Royal Postman Art/Resources/Effects/Chest");
            var additive = AssetDatabase.LoadAssetAtPath<Material>(AdditivePath);
            var root = new GameObject("ChestOpenScreen");
            try
            {
                var screen = root.AddComponent<ChestOpenScreen>();
                screen.additive = additive;
                screen.glowSprite = LoadSprite(Masks + "flash_soft.png");
                screen.sparkleSprite = LoadSprite(Masks + "spark_cross.png");
                screen.dotSprite = LoadSprite(Masks + "spark_dot.png");
                screen.ringSprite = LoadSprite(Masks + "ring_soft.png");
                screen.font = LobbyFeaturesConfig.Instance != null ? LobbyFeaturesConfig.Instance.displayFont : null;
                foreach (var (type, path) in IconPaths)
                {
                    var icon = LoadSprite(path);
                    if (icon != null) screen.icons.Add(new ChestOpenScreen.IconEntry { type = type, icon = icon });
                }

                // Modelo do brilho atrás de cada power-up: fica desligado; a tela faz uma cópia para cada prêmio.
                var template = new GameObject("Brilho do power-up (modelo)", typeof(RectTransform));
                template.transform.SetParent(root.transform, false);
                var rect = (RectTransform)template.transform;
                rect.sizeDelta = new Vector2(475f, 475f);
                var glow = template.AddComponent<UIMagicGlow>();
                glow.additive = additive;
                glow.glowSprite = screen.glowSprite;
                glow.raySprite = LoadSprite(Masks + "streak_soft.png");
                glow.sparkleStar = screen.sparkleSprite;
                glow.sparkleDot = screen.dotSprite;
                glow.ringSprite = screen.ringSprite;
                ChestOpenScreen.ApplyDefaultGlow(glow);
                template.SetActive(false);
                screen.glowTemplate = glow;

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
            EditorPrefs.SetInt(VersionKey, Version);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Prefab da abertura de baú criado em " + PrefabPath + ". Teste pelo menu Royal Aves > Efeitos > Baú (no Play).");
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
