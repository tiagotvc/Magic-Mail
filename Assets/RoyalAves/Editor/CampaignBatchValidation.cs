using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    // Read-only validation against assets deserialized by Unity, without modifying the open scene or saves.
    public static class CampaignBatchValidation
    {
        const string Request = "Logs/campaign-11-30-native.request";
        const string Result = "Logs/campaign-11-30-native.result";

        [Serializable]
        class RecipeBatch { public Recipe[] levels; }
        [Serializable]
        class Recipe
        {
            public int level;
            public int moves;
            public int palette;
            public string[] layout;
        }

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            Run();
        }

        [MenuItem("Royal Aves/Campanha/Validar fases 11 a 30")]
        public static void Run()
        {
            Directory.CreateDirectory("Logs");
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var recipes = JsonUtility.FromJson<RecipeBatch>("{\"levels\":" +
                    File.ReadAllText("Tools/level-map/campaign-11-30-recipes.json") + "}").levels;
                var report = new List<string>();
                for (int level = 11; level <= 30; level++)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<LevelContainer>($"Assets/SweetSugar/Resources/Levels/Level_{level}.asset");
                    var goals = AssetDatabase.LoadAssetAtPath<TargetLevel>($"Assets/SweetSugar/Resources/Levels/Targets/TargetLevel{level}.asset");
                    Require(asset != null && goals != null, $"Level {level}: missing assets");
                    var data = asset.levelData;
                    var expectedRecipe = recipes.Single(r => r.level == level);
                    Require(data.levelNum == level && data.fields.Count == 1, $"Level {level}: identity/fields");
                    Require(data.limit > 0 && data.star1 > 0 && data.star1 < data.star2 && data.star2 < data.star3,
                        $"Level {level}: moves/stars");
                    Require(data.GetPieceColors().Length == data.colorLimit && data.pieceColors.Count == data.colorLimit,
                        $"Level {level}: serialized palette");
                    Require(typeof(LevelData).Assembly.GetType("SweetSugar.Scripts.TargetScripts." + data.target.name) != null,
                        $"Level {level}: missing compatibility target handler");
                    var field = data.fields[0];
                    Require(data.limit == expectedRecipe.moves && data.colorLimit == expectedRecipe.palette &&
                        field.maxRows == expectedRecipe.layout.Length && field.maxCols == expectedRecipe.layout[0].Length,
                        $"Level {level}: differs from authored recipe (possible stale editor overwrite)");
                    Require(field.levelSquares.Length == field.maxRows * field.maxCols, $"Level {level}: grid dimensions");
                    Func<int, int, bool> cabinet = (x, y) => x >= 0 && y >= 0 && x < field.maxCols && y < field.maxRows &&
                        field.levelSquares[y * field.maxCols + x].blocks.Any(b => b.squareType == SquareTypes.MailCubby);
                    var covered = new HashSet<int>();
                    for (int y = 0; y < field.maxRows; y++)
                        for (int x = 0; x < field.maxCols; x++)
                        {
                            int index = y * field.maxCols + x;
                            var square = field.levelSquares[index];
                            Require(square != null && (square.blocks.Count > 0 || square.block == SquareTypes.NONE), $"Level {level}: empty cell definition");
                            Require(square.position == new Vector2Int(x, y), $"Level {level}: cell position");
                            if (!cabinet(x, y) || covered.Contains(index)) continue;
                            Require(cabinet(x+1,y) && cabinet(x,y+1) && cabinet(x+1,y+1), $"Level {level}: orphan cabinet");
                            covered.Add(index); covered.Add(index+1);
                            covered.Add(index+field.maxCols); covered.Add(index+field.maxCols+1);
                        }
                    int machines = field.levelSquares.Count(s => s.blocks.Any(b => b.squareType == SquareTypes.FrankingMachine));
                    if (machines > 0)
                    {
                        Require(machines >= 2, $"Level {level}: machine formation needs multiple units");
                        Require(machines == expectedRecipe.layout.Sum(line => line.Count(symbol => symbol == 'M')),
                            $"Level {level}: machine count differs from recipe");
                        for (int y = 0; y < field.maxRows; y++)
                            for (int x = 0; x < field.maxCols; x++)
                            {
                                var square = field.levelSquares[y * field.maxCols + x];
                                if (!square.blocks.Any(b => b.squareType == SquareTypes.FrankingMachine)) continue;
                                bool atEdge = x == 0 || y == 0 || x == field.maxCols-1 || y == field.maxRows-1 ||
                                    field.levelSquares[y*field.maxCols+x-1].block == SquareTypes.NONE ||
                                    field.levelSquares[y*field.maxCols+x+1].block == SquareTypes.NONE ||
                                    field.levelSquares[(y-1)*field.maxCols+x].block == SquareTypes.NONE ||
                                    field.levelSquares[(y+1)*field.maxCols+x].block == SquareTypes.NONE;
                                Require(atEdge && !square.enterSquare, $"Level {level}: interior machine or entry on machine");
                            }
                    }
                    var names = goals.targets.Select(t => t.targetType.GetTarget().name).ToArray();
                    Require(names.Distinct().Count() == names.Length, $"Level {level}: duplicated goal");
                    Require(names.Contains("MailCubby") == (covered.Count > 0), $"Level {level}: cabinet goal");
                    Require(names.Contains("MachineLetters") == (machines > 0), $"Level {level}: machine goal");
                    foreach (var goal in goals.targets)
                    {
                        string name = goal.targetType.GetTarget().name;
                        Require(goal.CountDrawer.count > 0 && goal.sprites.Any() && goal.sprites.All(s => s.icon != null),
                            $"Level {level}: invalid count or missing goal sprite ({name})");
                        int expected = name == "MailCubby" ? MailCubbyCell.CountLetters(data)
                            : name == "SolidBlock" ? field.levelSquares.Count(s => s.blocks.Any(b => b.squareType == SquareTypes.SolidBlock))
                            : name == "SugarSquare" ? field.levelSquares.Count(s => s.blocks.Any(b => b.squareType == SquareTypes.SugarSquare))
                            : goal.CountDrawer.count;
                        Require(goal.CountDrawer.count == expected, $"Level {level}: target count ({name})");
                    }
                    report.Add($"PASS {level}: {field.maxRows}x{field.maxCols}, {data.limit} moves, " +
                        $"{covered.Count/4} cabinets, {machines} machines, goals={string.Join(",", names)}");
                }
                CheckPresentationPrefab<MailCubbyCell>("MailCubby", cell => cell.visualPrefab);
                CheckPresentationPrefab<FrankingMachineCell>("FrankingMachine", cell => cell.visualPrefab);
                File.WriteAllText(Result, "PASS: native asset validation, levels 11–30. Gameplay not simulated.\n" + string.Join("\n", report));
                Debug.Log("[Campanha] Fases 11–30: assets, metas e artes validados. Playtest ainda necessário.");
            }
            catch (Exception error)
            {
                File.WriteAllText(Result, "FAIL: " + error);
                Debug.LogException(error);
            }
        }

        static void CheckPresentationPrefab<T>(string name, Func<T, GameObject> getVisual) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/SweetSugar/Resources/Blocks/{name}.prefab");
            Require(prefab != null && prefab.GetComponent<T>() != null, name + ": missing campaign component");
            var visual = getVisual(prefab.GetComponent<T>());
            var art = visual != null ? visual.GetComponent<MailCubbyAnimation>() : null;
            Require(art != null && art.cabinet != null && art.cabinet.sprite != null, name + ": missing intro artwork");
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
