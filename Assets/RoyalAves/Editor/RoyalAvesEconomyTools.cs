// Puts Correio Mágico's prices, from RoyalAvesProgression (Preços na loja), in Sweet Sugar's shop: every booster pack
// (CanvasGlobal's BoostShop) and the "continue with 5 more moves" after losing (LevelManager.FailedCost in the game
// scenes). The coins a level gives come from the same asset at run time (LobbyController.ShowVictory).
// Menu: Royal Aves > Ajustar preços da loja. Run it again after changing the prices in the asset.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoyalAves.Meta;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.GUI.Boost;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesEconomyTools
    {
        const string ConfigPath = "Assets/RoyalAves/Resources/" + AreaProgressionConfig.ResourcePath + ".asset";
        const string CanvasGlobalPath = "Assets/SweetSugar/Prefabs/CanvasGlobal.prefab";
        static readonly string[] GameScenes = { "Assets/SweetSugar/Scenes/game.unity", "Assets/SweetSugar/Scenes/gameStatic.unity" };

        [MenuItem("Royal Aves/Ajustar preços da loja")]
        static void Apply()
        {
            if (!RoyalAvesTools.CanRunTool()) return;
            var config = AssetDatabase.LoadAssetAtPath<AreaProgressionConfig>(ConfigPath);
            if (config == null)
            {
                EditorUtility.DisplayDialog("Preços da loja", "Falta " + ConfigPath + " (menu Royal Aves > Criar lobby da agência).", "OK");
                return;
            }

            var log = new List<string>();
            var root = PrefabUtility.LoadPrefabContents(CanvasGlobalPath);
            try
            {
                var packs = SetBoosterPrices(root.GetComponentsInChildren<BoostShop>(true), config.boosterPackPrice);
                log.Add($"{packs} pacotes de reforços: {config.boosterPackPrice} moedas cada");
                // Recovering all lives costs the same in Sweet Sugar's life shop as in the Inventário (prototype: 500).
                var features = AssetDatabase.LoadAssetAtPath<LobbyFeaturesConfig>("Assets/RoyalAves/Resources/" + LobbyFeaturesConfig.ResourcePath + ".asset");
                if (features != null)
                    foreach (var lifeShop in root.GetComponentsInChildren<SweetSugar.Scripts.GUI.LifeShop>(true))
                    {
                        lifeShop.CostIfRefill = features.livesRefillCost;
                        log.Add($"recuperar vidas: {features.livesRefillCost} moedas");
                    }
                PrefabUtility.SaveAsPrefabAsset(root, CanvasGlobalPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var path in GameScenes.Where(File.Exists))
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var roots = scene.GetRootGameObjects();
                    foreach (var levelManager in roots.SelectMany(r => r.GetComponentsInChildren<LevelManager>(true)))
                    {
                        levelManager.FailedCost = config.continuePrice;
                        Record(levelManager);
                    }
                    // Only used on the first launch on a device (Sweet Sugar's "Lauched" key).
                    foreach (var init in roots.SelectMany(r => r.GetComponentsInChildren<InitScript>(true)))
                    {
                        init.FirstGems = config.startingCoins;
                        Record(init);
                    }
                    // The scene's CanvasGlobal copy follows the prefab unless it overrides the prices; set them here too.
                    SetBoosterPrices(roots.SelectMany(r => r.GetComponentsInChildren<BoostShop>(true)), config.boosterPackPrice);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    log.Add($"{Path.GetFileNameWithoutExtension(path)}: continuar (+5 movimentos) por {config.continuePrice} moedas; " +
                            $"jogador novo começa com {config.startingCoins} moedas");
                }
            }
            finally
            {
                if (setup.Length > 0 && setup.All(s => !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            Selection.activeObject = config;
            EditorUtility.DisplayDialog("Preços da loja", "Aplicado:\n- " + string.Join("\n- ", log) +
                $"\n\nMoedas por nível vencido: pontos × {config.coinsPerPoint:0.##}" +
                (config.replayCoinShare < 1 ? $" (repetindo um nível: {config.replayCoinShare:P0} disso)." : "."), "OK");
        }

        // Testing aid: coins for this computer's player. With the game running they go straight to Sweet Sugar (and the
        // lobby counter); stopped, they go to the saved progress (PlayerPrefs "Gems").
        [MenuItem("Royal Aves/Teste: +5.000 moedas")]
        static void GiveTestCoins()
        {
            const int amount = 5000;
            if (EditorApplication.isPlaying && InitScript.Instance != null)
            {
                InitScript.Instance.AddGems(amount);
                if (LobbyController.Instance != null) LobbyController.Instance.RefreshNow();
            }
            else
            {
                // Sweet Sugar's first launch would replace the coins with the starting coins; mark it done, with what
                // that first launch sets (full lives, sound and music on).
                if (PlayerPrefs.GetInt("Lauched") == 0)
                {
                    PlayerPrefs.SetInt("Lifes", 5);
                    PlayerPrefs.SetInt("Music", 1);
                    PlayerPrefs.SetInt("Sound", 1);
                    PlayerPrefs.SetInt("Lauched", 1);
                }
                PlayerPrefs.SetInt("Gems", PlayerPrefs.GetInt("Gems") + amount);
                PlayerPrefs.Save();
            }
            var total = EditorApplication.isPlaying ? InitScript.Gems : PlayerPrefs.GetInt("Gems");
            Debug.Log($"[Correio Mágico] +{amount} moedas de teste; agora: {total}");
            EditorUtility.DisplayDialog("Moedas de teste", $"+{amount} moedas. Total agora: {total}.", "OK");
        }

        // Testing aid: five lives. Sweet Sugar keeps at most CapOfLife (5), so this fills the lives up. With the game
        // running they go straight to Sweet Sugar; stopped, they go to the saved progress (PlayerPrefs "Lifes").
        [MenuItem("Royal Aves/Teste: +5 vidas")]
        static void GiveTestLives()
        {
            const int amount = 5;
            if (EditorApplication.isPlaying && InitScript.Instance != null)
            {
                InitScript.Instance.AddLife(amount);
                if (LobbyController.Instance != null) LobbyController.Instance.RefreshNow();
            }
            else
            {
                // As with the coins: a first launch would reset the lives, so it is marked done.
                if (PlayerPrefs.GetInt("Lauched") == 0)
                {
                    PlayerPrefs.SetInt("Music", 1);
                    PlayerPrefs.SetInt("Sound", 1);
                    PlayerPrefs.SetInt("Lauched", 1);
                }
                PlayerPrefs.SetInt("Lifes", Mathf.Min(PlayerPrefs.GetInt("Lifes") + amount, amount));
                PlayerPrefs.Save();
            }
            var total = EditorApplication.isPlaying ? InitScript.lifes : PlayerPrefs.GetInt("Lifes");
            Debug.Log($"[Correio Mágico] +{amount} vidas de teste; agora: {total}");
            EditorUtility.DisplayDialog("Vidas de teste", $"+{amount} vidas. Total agora: {total}.", "OK");
        }

        // Testing aid: ten more stars to spend on the area's upgrades. Saved at once; with the game running the lobby's star
        // counter updates too (MetaProgress.Changed).
        [MenuItem("Royal Aves/Teste: +10 estrelas")]
        static void GiveTestStars()
        {
            const int amount = 10;
            MetaProgress.AddStars(amount);
            Debug.Log($"[Correio Mágico] +{amount} estrelas de teste; agora: {MetaProgress.Stars}");
            EditorUtility.DisplayDialog("Estrelas de teste", $"+{amount} estrelas. Total agora: {MetaProgress.Stars}.", "OK");
        }

        // Testing aid: five Pacote explosivo boosters in the inventory, to pick on the pre-level screen (it turns two pieces
        // into dinamites when the level starts). Sweet Sugar keeps the count in PlayerPrefs under the BoostType name.
        [MenuItem("Royal Aves/Teste: +5 pacotes explosivos")]
        static void GiveTestPackages()
        {
            const int amount = 5;
            var key = "" + BoostType.Packages;
            PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key) + amount);
            PlayerPrefs.Save();
            var total = PlayerPrefs.GetInt(key);
            Debug.Log($"[Correio Mágico] +{amount} pacotes explosivos de teste; agora: {total}");
            EditorUtility.DisplayDialog("Pacotes explosivos de teste", $"+{amount} pacotes explosivos. Total agora: {total}.", "OK");
        }

        static int SetBoosterPrices(IEnumerable<BoostShop> shops, int price)
        {
            var packs = 0;
            foreach (var shop in shops)
            {
                foreach (var product in shop.boostProducts)
                {
                    product.GemPrices = price;
                    packs++;
                }
                Record(shop);
            }
            return packs;
        }

        static void Record(Object target)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }
    }
}
