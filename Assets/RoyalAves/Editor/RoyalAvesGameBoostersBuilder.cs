// Puts Correio Mágico's boosters in the game's booster bar (the buttons under the board, in every orientation of the
// game scene) instead of Sweet Sugar's candy art, left to right: Martelo Postal (destroys one piece), Arco Expresso
// (destroys the touched row), Canhão Postal (destroys the touched column), Boné do Mensageiro (shuffles the board's
// existing pieces). Each picture is still anchored to a stock Sweet Sugar slot ("Bomb", "ExtraMoves", "ExplodeArea",
// "FreeMove"), but that slot no longer does what it originally did in the template - see LevelManager.DestroyLine/
// ShuffleBoard and the ActivatedBoost setter for the actual gameplay logic.
// Also sets: the yellow booster button, the grey locked button, the amount in a red badge and the "+" (none left) in
// a green badge, both at the bottom right, as on the level start window; and the settings gear of the game
// (CanvasGlobal/SettingsButton) becomes the lobby's gear.
// Menu: Royal Aves > Aplicar reforços do jogo.
using System.Collections.Generic;
using System.Linq;
using SweetSugar.Scripts.GUI.Boost;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesGameBoostersBuilder
    {
        const string GameScene = "Assets/SweetSugar/Scenes/game.unity";
        const string CanvasGlobalPath = "Assets/SweetSugar/Prefabs/CanvasGlobal.prefab";
        const string Art = "Assets/RoyalAves/Art/";
        static readonly Vector2 BadgeSpot = new Vector2(62, -62);
        const float BadgeSize = 82;

        static readonly Dictionary<BoostType, (string file, string name)> Pictures = new Dictionary<BoostType, (string, string)>
        {
            [BoostType.Bomb] = ("Boosters/hammer", "Martelo Postal"),
            [BoostType.ExplodeArea] = ("Boosters/cannon", "Canhão Postal"),
            [BoostType.FreeMove] = ("Boosters/cap", "Boné do Mensageiro"),
            [BoostType.ExtraMoves] = ("Boosters/arrow", "Arco Expresso"),
        };

        // Da esquerda pra direita na barra: marreta, flecha, canhão, chapéu.
        static readonly BoostType[] DesiredOrder =
            { BoostType.Bomb, BoostType.ExtraMoves, BoostType.ExplodeArea, BoostType.FreeMove };

        // What plays on the board when the martelo fires. The clip (bomb_boost.controller) animates only
        // m_LocalScale - a punch on one still picture - so swapping that picture is all it takes: the hammer where
        // Sweet Sugar drew its wand. Flecha/canhão/chapéu não passam mais por essa animação (ver
        // LevelManager.DestroyLine/ShuffleBoard), só o martelo ainda usa esse prefab.
        static readonly Dictionary<string, BoostType> Effects = new Dictionary<string, BoostType>
        {
            ["Assets/SweetSugar/Resources/Boosts/simple_explosion.prefab"] = BoostType.Bomb,
        };

        [MenuItem("Royal Aves/Aplicar reforços do jogo")]
        static void ApplyFromMenu()
        {
            if (!RoyalAvesTools.CanRunTool()) return;
            RoyalAvesReskin.EnsureImportSettings();
            var button = Load("UI/LevelStart/botao-power-ups");
            var locked = Load("UI/Buttons/botao_bloqueado");
            var badge = Load("UI/LevelStart/botao-fechar-circulo");
            var plus = Load("UI/LevelStart/selo-verde");
            if (new[] { button, locked, badge, plus }.Any(s => s == null) || Pictures.Values.Any(p => Load(p.file) == null))
            {
                EditorUtility.DisplayDialog("Reforços do jogo", "Faltam imagens em Assets/RoyalAves/Art (Boosters, botao-power-ups, botao_bloqueado, selo-verde ou configuracoes).", "OK");
                return;
            }

            var log = new List<string>();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
                var boosts = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BoostIcon>(true))
                    .Where(b => PathOf(b.transform).Contains("OrientationPanel")).ToList();
                ReorderBoostTypes(boosts, log);
                foreach (var boost in boosts)
                {
                    Restyle(boost, button, locked, badge, plus);
                    log.Add($"{PathOf(boost.transform.parent.parent)} › {boost.name}" +
                            (Pictures.TryGetValue(boost.type, out var picture) ? $" → {picture.name}" : " (só o botão)"));
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (setup.Length > 0 && setup.All(s => !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            var root = PrefabUtility.LoadPrefabContents(CanvasGlobalPath);
            try
            {
                if (root.transform.Find("SettingsButton/Image")?.GetComponent<Image>() is Image gearImage)
                {
                    // The gear keeps the sprite it already has (the game's own icon); this builder only fixes its look.
                    gearImage.preserveAspect = true;
                    PrefabUtility.SaveAsPrefabAsset(root, CanvasGlobalPath);
                    log.Add("engrenagem de configurações do jogo (sprite mantido)");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            RestyleEffects(log);

            EditorUtility.DisplayDialog("Reforços do jogo", "Aplicado:\n- " + string.Join("\n- ", log.Distinct()) +
                "\n\nOrdem: martelo, flecha, canhão, chapéu. Martelo destrói 1 peça, flecha destrói a linha tocada, " +
                "canhão destrói a coluna tocada, chapéu embaralha as peças do tabuleiro.", "OK");
        }

        // Atribui martelo/flecha/canhão/chapéu (DesiredOrder) aos ícones de cada barra da esquerda pra direita,
        // pela posição x atual de cada um - funciona em qualquer variante de orientação sem depender de nomes.
        static void ReorderBoostTypes(List<BoostIcon> boosts, List<string> log)
        {
            var groups = boosts.Where(b => DesiredOrder.Contains(b.type)).GroupBy(b => b.transform.parent);
            foreach (var group in groups)
            {
                var ordered = group.OrderBy(b => ((RectTransform)b.transform).anchoredPosition.x).ToList();
                if (ordered.Count != DesiredOrder.Length)
                {
                    log.Add($"{PathOf(group.Key)}: esperava {DesiredOrder.Length} reforços, achei {ordered.Count} - ordem não mexida");
                    continue;
                }
                for (var i = 0; i < ordered.Count; i++)
                {
                    ordered[i].type = DesiredOrder[i];
                    Dirty(ordered[i]);
                }
            }
        }

        static void Restyle(BoostIcon boost, Sprite button, Sprite locked, Sprite badge, Sprite plus)
        {
            if (boost.GetComponent<Image>() is Image background)
            {
                background.sprite = button;
                background.preserveAspect = true;
                Dirty(background);
            }

            if (Pictures.TryGetValue(boost.type, out var picture))
            {
                var icon = boost.transform.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("Icon"));
                if (icon != null && icon.GetComponent<Image>() is Image iconImage)
                {
                    iconImage.sprite = Load(picture.file);
                    iconImage.preserveAspect = true;
                    Dirty(iconImage);
                }
                // The old "+5" drawn over Sweet Sugar's extra-moves picture.
                foreach (var label in boost.transform.Cast<Transform>().Where(t => t.GetComponent<TMP_Text>() != null))
                {
                    label.gameObject.SetActive(false);
                    Dirty(label.gameObject);
                }
            }

            if (boost.transform.Find("Lock")?.GetComponent<Image>() is Image lockImage)
            {
                lockImage.sprite = locked;
                lockImage.preserveAspect = true;
                Dirty(lockImage);
            }

            // Amount: red badge at the bottom right, white number in the game's font.
            if (boost.counter != null)
            {
                var counter = (RectTransform)boost.counter.transform;
                counter.anchoredPosition = BadgeSpot;
                counter.sizeDelta = Vector2.one * BadgeSize;
                if (counter.GetComponent<Image>() is Image counterImage)
                {
                    counterImage.sprite = badge;
                    counterImage.preserveAspect = true;
                    Dirty(counterImage);
                }
                Dirty(counter);
            }
            if (boost.boostCount != null)
            {
                var font = RoyalAvesFonts.Display;
                if (font != null)
                {
                    boost.boostCount.font = font;
                    boost.boostCount.fontSharedMaterial = font.material;
                }
                boost.boostCount.color = Color.white;
                boost.boostCount.alignment = TextAlignmentOptions.Center;
                Dirty(boost.boostCount);
            }

            // None left: a green badge with "+" on the same spot.
            if (boost.plus != null)
            {
                var plusRect = (RectTransform)boost.plus.transform;
                plusRect.anchoredPosition = BadgeSpot;
                plusRect.sizeDelta = Vector2.one * BadgeSize;
                if (plusRect.GetComponent<Image>() is Image plusImage)
                {
                    plusImage.sprite = plus;
                    plusImage.preserveAspect = true;
                    Dirty(plusImage);
                }
                var sign = plusRect.Find("Mais") as RectTransform;
                if (sign == null)
                {
                    sign = new GameObject("Mais", typeof(RectTransform)) { layer = plusRect.gameObject.layer }.GetComponent<RectTransform>();
                    sign.SetParent(plusRect, false);
                }
                sign.anchorMin = Vector2.zero;
                sign.anchorMax = Vector2.one;
                sign.offsetMin = sign.offsetMax = Vector2.zero;
                var text = sign.GetComponent<TextMeshProUGUI>();
                if (text == null) text = sign.gameObject.AddComponent<TextMeshProUGUI>();
                if (RoyalAvesFonts.Display != null) text.font = RoyalAvesFonts.Display;
                text.text = "+";
                text.fontSize = 64;
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;
                Dirty(text);
                Dirty(plusRect);
            }
        }

        static void Dirty(Object target)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }

        static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (; t != null; t = t.parent) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }

        static void RestyleEffects(List<string> log)
        {
            foreach (var pair in Effects)
            {
                if (!Pictures.TryGetValue(pair.Value, out var picture)) continue;
                var sprite = Load(picture.file);
                if (sprite == null) continue;
                var root = PrefabUtility.LoadPrefabContents(pair.Key);
                try
                {
                    // Never "GetComponent(...) is T x" nor "?? AddComponent": a missing component comes back as
                    // Unity's fake null, which passes both. Only "== null" asks Unity itself.
                    var renderer = root.GetComponent<SpriteRenderer>();
                    if (renderer == null)
                    {
                        log.Add($"animação de uso: {System.IO.Path.GetFileNameWithoutExtension(pair.Key)} sem SpriteRenderer");
                        continue;
                    }
                    renderer.sprite = sprite;
                    PrefabUtility.SaveAsPrefabAsset(root, pair.Key);
                    log.Add($"animação de uso do {picture.name}: {System.IO.Path.GetFileNameWithoutExtension(pair.Key)}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + path + ".png");
    }
}
