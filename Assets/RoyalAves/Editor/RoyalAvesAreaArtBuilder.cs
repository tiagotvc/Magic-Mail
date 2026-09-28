// Puts the agency artwork on the first area of the progression: the empty plaza as the background and one layer per
// upgrade, each already the size of the background with its furniture in place — which is what AreaTask.layer expects.
//
// The pictures come from Tools/montar-camadas-area, which cuts the user's sheets into their separate objects and
// composes the full-size layers; run that script first if the art changes.
//
// The order of the tasks is the order the layers are drawn in (LobbyController makes one Image per task, in order, and
// a later sibling draws on top), so they are listed back to front: the hedges are behind the benches, the rug under the
// sofa, the tea table on top of everything. That is also the order they are bought in.
// Menu: Royal Aves > Aplicar visual da agência. Running it again only refreshes the pictures.
using System.Collections.Generic;
using System.Linq;
using RoyalAves.Meta;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesAreaArtBuilder
    {
        const string Art = "Assets/RoyalAves/Art/Backgrounds/Agencia/";
        const string ConfigPath = "Assets/RoyalAves/Resources/" + AreaProgressionConfig.ResourcePath + ".asset";
        const string AreaId = "agencia";
        const string LobbyPrefab = "Assets/RoyalAves/Prefabs/RoyalAvesLobby.prefab";

        // Back to front, which is also the order they are bought in.
        static readonly (string id, string title, int stars, string file)[] Upgrades =
        {
            ("jardim", "Jardim e flores", 1, "04-vegetacao-e-flores"),
            ("bancos", "Bancos e tapete", 1, "06-bancos-e-tapete"),
            ("luz", "Poste e lampiões", 2, "03-poste-e-lampadas"),
            ("correio", "Encomendas e caixa de correio", 2, "05-encomendas-e-caixa-correio"),
            ("estofados", "Estofados da sala de espera", 3, "01-estofados-roxos"),
            ("mesinha", "Mesinha de chá", 3, "02-mesinha-e-cha"),
        };

        [MenuItem("Royal Aves/Aplicar visual da agência")]
        static void ApplyFromMenu()
        {
            if (!RoyalAvesTools.CanRunTool()) return;
            RoyalAvesReskin.EnsureImportSettings();

            var config = AssetDatabase.LoadAssetAtPath<AreaProgressionConfig>(ConfigPath);
            if (config == null)
            {
                EditorUtility.DisplayDialog("Visual da agência", "Falta " + ConfigPath +
                    " (menu Royal Aves > Criar lobby da agência).", "OK");
                return;
            }

            var background = Sprite("bg-agencia-sem-upgrades");
            var missing = Upgrades.Where(u => Sprite(u.file) == null).Select(u => u.file).ToList();
            if (background == null || missing.Count > 0)
            {
                EditorUtility.DisplayDialog("Visual da agência",
                    "Faltam imagens em " + Art + ":\n- " +
                    string.Join("\n- ", (background == null ? new[] { "bg-agencia-sem-upgrades" } : new string[0]).Concat(missing)) +
                    "\n\nGere-as com Tools/montar-camadas-area/montar.py.", "OK");
                return;
            }

            var area = config.areas.FirstOrDefault(a => a.id == AreaId);
            if (area == null)
            {
                // The first area is the agency; an older one made from other art is reused so its reward survives.
                area = config.areas.FirstOrDefault();
                if (area == null) { area = new AreaDefinition(); config.areas.Add(area); }
                area.id = AreaId;
            }
            area.title = "Agência dos Correios";
            area.background = background;

            var log = new List<string>();
            var tasks = new List<AreaTask>();
            foreach (var upgrade in Upgrades)
            {
                // An upgrade already there keeps the cost the user set in the Inspector.
                var task = area.tasks.FirstOrDefault(t => t.id == upgrade.id) ?? new AreaTask { starCost = upgrade.stars };
                task.id = upgrade.id;
                task.title = upgrade.title;
                task.layer = Sprite(upgrade.file);
                var icon = Sprite("icone-" + upgrade.file);
                if (icon != null) task.icon = icon;
                tasks.Add(task);
                log.Add($"{upgrade.title} ({task.starCost} ⭐)");
            }
            var dropped = area.tasks.Count(t => Upgrades.All(u => u.id != t.id));
            area.tasks = tasks;

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Selection.activeObject = config;

            PaintPrefab(background, log);

            EditorUtility.DisplayDialog("Visual da agência",
                "Área \"" + area.title + "\" com o pátio vazio de fundo e 6 melhorias, de trás para a frente:\n- " +
                string.Join("\n- ", log) +
                (dropped > 0 ? "\n\n" + dropped + " melhoria(s) da arte antiga foram substituídas." : "") +
                "\n\nAs posições vêm de Tools/montar-camadas-area/posicoes.json. Para acertar um móvel, mude cx/cy/w " +
                "nesse ficheiro e volte a correr montar.py — a prévia fica em previa-composta.png.", "OK");
        }

        // LobbyController swaps the background at run time (BuildArea), so in the editor the lobby keeps whatever
        // picture the prefab was built with. The same picture is written into the prefab as well, otherwise the Scene
        // view shows the old area and it looks as if nothing happened.
        static void PaintPrefab(Sprite background, List<string> log)
        {
            var root = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                var lobby = root.GetComponentInChildren<LobbyController>(true);
                if (lobby == null) { log.Add("prefab do lobby: LobbyController não encontrado"); return; }
                var serialized = new SerializedObject(lobby);
                var field = serialized.FindProperty("background");
                if (field == null || !(field.objectReferenceValue is Image image))
                {
                    log.Add("prefab do lobby: o campo Background do LobbyController está vazio");
                    return;
                }
                image.sprite = background;
                var fitter = root.GetComponentsInChildren<AspectRatioFitter>(true)
                    .FirstOrDefault(f => f.transform == image.transform || f.transform == image.transform.parent);
                if (fitter != null) fitter.aspectRatio = background.rect.width / background.rect.height;
                PrefabUtility.SaveAsPrefabAsset(root, LobbyPrefab);
                log.Add("fundo também gravado no prefab do lobby (para o Scene view bater com o jogo)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Sprite Sprite(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + file + ".png");
    }
}
