using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Items._Interfaces;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.System;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class PowerUpRulesValidation
    {
        const string ActiveKey = "MagicMail.PowerUpSwapValidation";
        static int stage, caseIndex, initialLaunches;
        static double nextAt, deadline;
        static Item dragging;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
            deadline = EditorApplication.timeSinceStartup + 60d;
        }

        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            const string request = "Logs/power-up-rules.request";
            try
            {
                if (File.Exists(request))
                {
                    string mode = File.ReadAllText(request).Trim();
                    File.Delete(request);
                    if (EditorApplication.isPlaying) throw new Exception("Start the fixture outside Play Mode.");
                    ValidateGeometry();
                    if (mode == "play")
                    {
                        stage = caseIndex = 0;
                        SessionState.SetBool(ActiveKey, true);
                        deadline = EditorApplication.timeSinceStartup + 60d;
                        EditorApplication.isPlaying = true;
                    }
                }
                if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying) return;
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Swap validation timed out.");
                var manager = LevelManager.THIS;
                if (manager == null || AI.THIS == null) return;
                if (stage == 0)
                {
                    if (manager.gameStatus == GameState.Map) { stage = 1; manager.gameStatus = GameState.PrepareGame; }
                    return;
                }
                if (manager.field == null || EditorApplication.timeSinceStartup < nextAt) return;
                if (stage == 1)
                {
                    if (manager.gameStatus != GameState.WaitForPopup && manager.gameStatus != GameState.Playing) return;
                    manager.gameStatus = GameState.Playing;
                    manager.levelData.limit = 999;
                    foreach (var goal in manager.levelData.TargetCounters)
                    {
                        goal.targetLevel = goal.targetLevel.DeepCopy();
                        goal.targetLevel.setCount = SetCount.Manually;
                        goal.count = 999;
                    }
                    stage = 2;
                }
                if (stage == 2) { PrepareSwap(manager); stage = 3; nextAt = EditorApplication.timeSinceStartup + .4d; return; }
                if (stage == 3)
                {
                    initialLaunches = ItemMarmalade.LaunchCount;
                    var power = manager.field.GetSquare(caseIndex < 2 ? 3 : 4, 3).Item;
                    power.dontDestroyOnThisMove = true; // The explicit player move must bypass passive protection.
                    manager.DragBlocked = false;
                    manager.findMatchesStarted = true;
                    var pair = dragging.SwitchDirection(Vector3.left);
                    Expect(pair.currentItem != null && pair.neighborItem != null, "Swap was not accepted.");
                    stage = 4; nextAt = EditorApplication.timeSinceStartup + .7d; return;
                }
                if (stage == 4)
                {
                    Expect(ItemMarmalade.LaunchCount == initialLaunches + 1,
                        $"Helix did not launch once: case {caseIndex} (0/1 dragged helix; 2/3 dragged normal; odd cases with match).");
                    caseIndex++;
                    if (caseIndex == 4)
                    {
                        File.AppendAllText("Logs/power-up-rules.result", "\nPASS PLAY: four actual swaps launched the helix exactly once; with/without a match; dragging helix or ordinary piece; passive protection set.");
                        SessionState.SetBool(ActiveKey, false);
                        EditorApplication.isPlaying = false;
                        return;
                    }
                    stage = 2; nextAt = EditorApplication.timeSinceStartup + 2d;
                }
            }
            catch (Exception error)
            {
                File.WriteAllText("Logs/power-up-rules.result", "FAIL: " + error);
                bool running = SessionState.GetBool(ActiveKey, false);
                SessionState.SetBool(ActiveKey, false);
                if (running) EditorApplication.isPlaying = false;
                Debug.LogException(error);
            }
        }

        static void PrepareSwap(LevelManager manager)
        {
            manager.StopAllCoroutines();
            manager.findMatchesStarted = true;
            manager.DragBlocked = false;
            var field = manager.field;
            Expect(field.fieldData.maxCols >= 6 && field.fieldData.maxRows >= 5, "Fixture needs a board at least 6x5.");
            foreach (var cell in field.GetSquares())
            {
                if (cell == null) continue;
                if (cell.Item != null)
                {
                    cell.Item.StopAllCoroutines();
                    cell.Item.NextType = ItemsTypes.NONE;
                    cell.Item.dontDestroyOnThisMove = cell.Item.dontDestroyForThisCombine = false;
                    cell.Item.DestroyBehaviour();
                }
                foreach (var layer in cell.subSquares.ToArray()) UnityEngine.Object.Destroy(layer.gameObject);
                cell.subSquares.Clear();
                cell.type = SquareTypes.EmptySquare;
                cell.canMoveIn = cell.canMoveOut = true;
                cell.undestroyable = cell.cannotBeDestroy = false;
                cell.Item = cell.GenItem(false, ItemsTypes.NONE, (cell.col + cell.row * 2) % 5, noAnim: true);
            }
            bool powerDragged = caseIndex < 2;
            var powerCell = field.GetSquare(powerDragged ? 3 : 4, 3);
            var normalCell = field.GetSquare(powerDragged ? 4 : 3, 3);
            int destinationCol = powerDragged ? 3 : 4;
            normalCell.Item.GetComponent<IColorableComponent>().SetColor(0);
            if (caseIndex % 2 == 1)
            {
                field.GetSquare(destinationCol, 2).Item.GetComponent<IColorableComponent>().SetColor(0);
                field.GetSquare(destinationCol, 4).Item.GetComponent<IColorableComponent>().SetColor(0);
            }
            powerCell.Item.DestroyBehaviour();
            powerCell.Item = powerCell.GenItem(false, ItemsTypes.MARMALADE, noAnim: true);
            dragging = field.GetSquare(3, 3).Item;
        }

        [MenuItem("Royal Aves/Validação/Power-ups e alcance 7x7 da dinamite")]
        public static void ValidateGeometry()
        {
            var root = new GameObject("PowerUpRulesFixture") { hideFlags = HideFlags.HideAndDontSave };
            root.SetActive(false);
            try
            {
                var field = root.AddComponent<FieldBoard>();
                field.fieldData = new FieldData { maxRows = 9, maxCols = 9 };
                field.squaresArray = new Square[81];
                for (int row = 0; row < 9; row++)
                for (int col = 0; col < 9; col++)
                {
                    var node = new GameObject("Cell"); node.transform.SetParent(root.transform, false);
                    var square = node.AddComponent<Square>(); square.field = field; square.row = row; square.col = col;
                    square.type = SquareTypes.EmptySquare;
                    field.squaresArray[row * 9 + col] = square;
                }
                var clear = new ImpactBarrierSnapshot(Array.Empty<Vector2Int>());
                var center = field.GetSquare(4, 4);
                Expect(ImpactBarrierSnapshot.AreaAround(center, ItemDestroyAnimation.BlastRadius, clear).Count == 49, "Dynamite center must cover 49 cells.");
                Expect(ImpactBarrierSnapshot.AreaAround(field.GetSquare(0, 0), 3, clear).Count == 16, "Corner must clip to 16 unique cells.");
                Expect(ImpactBarrierSnapshot.AreaAround(field.GetSquare(0, 4), 3, clear).Count == 28, "Edge must clip to 28 cells.");
                Expect(ImpactBarrierSnapshot.AreaAround(center, 1, clear).Count == 9, "Other adjacency effects must remain 3x3.");
                var blocked = new ImpactBarrierSnapshot(new[] { new Vector2Int(5, 4) });
                var blast = ImpactBarrierSnapshot.AreaAround(center, 3, blocked);
                Expect(blast.Contains(field.GetSquare(5, 4)) && !blast.Contains(field.GetSquare(6, 4)), "A crate must receive but block the blast behind it.");
                foreach (var type in new[] { ItemsTypes.MARMALADE, ItemsTypes.PACKAGE, ItemsTypes.MULTICOLOR, ItemsTypes.HORIZONTAL_STRIPED, ItemsTypes.VERTICAL_STRIPED })
                    Expect(Item.IsPlayerPowerUpType(type), "Missing movable power-up: " + type);
                foreach (var type in new[] { ItemsTypes.NONE, ItemsTypes.SPIRAL, ItemsTypes.INGREDIENT, ItemsTypes.TimeBomb })
                    Expect(!Item.IsPlayerPowerUpType(type), "Ordinary/obstacle type treated as power-up: " + type);
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/power-up-rules.result", "PASS: native blast geometry (49 center, 16 corner, 28 edge); default adjacency 9; crate barriers; all five player power-up types.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
    }
}
