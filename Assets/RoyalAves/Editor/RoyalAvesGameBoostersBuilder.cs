// Puts Correio Mágico's boosters in the game's booster bar (the buttons under the board, in every orientation of the
// game scene) instead of Sweet Sugar's candy art:
// - Martelo Postal on "Bomb" (breaks one piece), Canhão Postal on "Explode area", Boné do Mensageiro on "Free move"
//   (swap two pieces) and Arco Expresso on "Extra moves";
// - the yellow booster button, the grey locked button, the amount in a red badge and the "+" (none left) in a green
//   badge, both at the bottom right, as on the level start window;
// - the settings gear of the game (CanvasGlobal/SettingsButton) becomes the lobby's gear.
// Only the pictures change: each booster still does what Sweet Sugar's did.
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

        // What plays on the board when a booster fires. Both clips (Animation/random_color_boost.anim and
        // bomb_boost.controller) animate only m_LocalScale - a punch on one still picture - so swapping that picture
        // is all it takes: the hammer where Sweet Sugar drew its wand, the cannon where it drew its bomb.
        static readonly Dictionary<string, BoostType> Effects = new Dictionary<string, BoostType>
        {
            ["Assets/SweetSugar/Resources/Boosts/simple_explosion.prefab"] = BoostType.Bomb,
            ["Assets/SweetSugar/Resources/Boosts/area_explosion.prefab"] = BoostType.ExplodeArea,
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
            var gear = Load("UI/Buttons/configuracoes");
            if (new[] { button, locked, badge, plus, gear }.Any(s => s == null) || Pictures.Values.Any(p => Load(p.file) == null))
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
                    gearImage.sprite = gear;
                    gearImage.preserveAspect = true;
                    PrefabUtility.SaveAsPrefabAsset(root, CanvasGlobalPath);
                    log.Add("engrenagem de configurações do jogo");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            RestyleEffects(log);

            EditorUtility.DisplayDialog("Reforços do jogo", "Aplicado:\n- " + string.Join("\n- ", log.Distinct()) +
                "\n\nSó as imagens mudaram: cada reforço continua com o efeito do Sweet Sugar " +
                "(martelo quebra 1 peça, canhão explode uma área, boné troca 2 peças, arco dá +5 movimentos).", "OK");
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
