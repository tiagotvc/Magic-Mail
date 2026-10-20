using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Level;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class ImpactBarrierRulesValidation
    {
        [InitializeOnLoadMethod]
        static void Initialize() { EditorApplication.update -= Poll; EditorApplication.update += Poll; }

        static void Poll()
        {
            const string request = "Logs/crate-impact.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request);
            try { Validate(); File.WriteAllText("Logs/crate-impact.result", "PASS: all crates in range; surviving crates shield pieces; final-hit crates pass damage; horizontal, vertical, diagonal, range and captured resistance."); }
            catch (Exception error) { File.WriteAllText("Logs/crate-impact.result", error.ToString()); Debug.LogException(error); }
        }
        static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        [MenuItem("Tools/Magic Mail/Validate Crate Impact Barriers")]
        public static void Validate()
        {
        var cells = new List<UnityEngine.Vector2Int> { new UnityEngine.Vector2Int(2, 2) };
        var barrier = new RoyalAves.Gameplay.ImpactBarrierSnapshot(cells);
        Check(barrier.CanReach(2, 0, 2, 2), "The crate must receive the hit");
        Check(!barrier.CanReach(2, 0, 2, 4), "Vertical hit crossed the crate");
        Check(!barrier.CanReach(0, 2, 4, 2), "Horizontal hit crossed the crate");
        Check(!barrier.CanReach(4, 2, 0, 2), "Reverse horizontal hit crossed the crate");
        Check(!barrier.CanReach(2, 4, 2, 0), "Reverse vertical hit crossed the crate");
        Check(!barrier.CanReach(0, 0, 4, 4), "Diagonal hit crossed the crate");
        Check(barrier.CanReach(0, 0, 2, 2), "Diagonal hit did not reach the crate");
        Check(!barrier.CanReach(1, 2, 2, 3), "Hit cut through crate corner");
        Check(barrier.CanReach(0, 0, 0, 4), "Clear path was blocked");
        Check(barrier.CanReach(0, 0, 0, 0), "Source cell was blocked");
        cells.Clear();
        Check(!barrier.CanReach(2, 0, 2, 4), "Breaking crate extended the same activation");
        Check(new RoyalAves.Gameplay.ImpactBarrierSnapshot(cells).CanReach(2, 0, 2, 4), "Later activation stayed blocked");
        for (int x = -4; x <= 6; x++)
            for (int y = -4; y <= 6; y++)
                if (!barrier.IsBlocking(x, y))
                    Check(barrier.CanReach(0, 0, x, y) == barrier.CanReach(x, y, 0, 0), "Path is not symmetric");
            ValidateCrateChains();
            ValidateCapturedResistance();
            Debug.Log("PASS: crate impact barriers, crate chains, final-hit penetration, range, diagonal corners and captured resistance.");
        }

        static void ValidateCrateChains()
        {
            var positions = new[] { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0) };
            var durable = new ImpactBarrierSnapshot(positions);
            foreach (var crate in positions)
                Check(durable.CanReach(0, 0, crate.x, crate.y), "A crate behind another crate missed the hit.");
            Check(!durable.CanReach(0, 0, 4, 0), "Piece behind surviving crates received damage.");
            var breaking = new ImpactBarrierSnapshot(positions, positions);
            Check(breaking.CanReach(0, 0, 4, 0), "Final-hit crates shielded a piece in the same impact.");
            Check(breaking.CanReach(4, 0, 0, 0), "Reverse final-hit path stayed blocked.");
            var mixed = new ImpactBarrierSnapshot(positions, positions.Take(2));
            Check(!mixed.CanReach(0, 0, 4, 0), "A surviving rear crate failed to shield its piece.");
            Check(mixed.CanReach(0, 0, 3, 0), "A rear surviving crate did not receive damage.");
            foreach (var step in new[] { new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(1, 1) })
            {
                var chain = new[] { step, step * 2, step * 3 };
                var blocked = new ImpactBarrierSnapshot(chain);
                Check(blocked.CanReach(0, 0, step.x * 3, step.y * 3), "Directional crate chain missed the last crate.");
                Check(!blocked.CanReach(0, 0, step.x * 4, step.y * 4), "Directional path crossed a surviving crate.");
                Check(new ImpactBarrierSnapshot(chain, chain).CanReach(0, 0, step.x * 4, step.y * 4), "Directional final-hit path stayed blocked.");
            }
        }

        static void ValidateCapturedResistance()
        {
            var root = new GameObject("CrateImpactFixture");
            root.SetActive(false);
            try
            {
                var field = root.AddComponent<FieldBoard>();
                field.fieldData = new FieldData { maxRows = 1, maxCols = 7 };
                field.squaresArray = new Square[7];
                for (int col = 0; col < 7; col++)
                {
                    var node = new GameObject("Cell"); node.transform.SetParent(root.transform, false);
                    var cell = node.AddComponent<Square>(); cell.field = field; cell.col = col; cell.row = 0;
                    cell.type = SquareTypes.EmptySquare;
                    field.squaresArray[col] = cell;
                }
                var origin = field.GetSquare(0, 0);
                for (int col = 1; col <= 3; col++)
                {
                    var node = new GameObject("Crate"); node.transform.SetParent(field.GetSquare(col, 0).transform, false);
                    var layer = node.AddComponent<ParcelStackLayer>(); layer.remainingLayers = 4 - col;
                    field.GetSquare(col, 0).subSquares.Add(node.GetComponent<Square>());
                }
                var snapshot = ImpactBarrierSnapshot.Capture(field);
                var line = snapshot.Filter(origin, field.squaresArray);
                Check(line.Contains(field.GetSquare(3, 0)), "Captured line missed a rear crate.");
                Check(!line.Contains(field.GetSquare(4, 0)), "Captured line damaged a shielded piece.");
                Check(!snapshot.IsBlocking(3, 0) && snapshot.IsBlocking(1, 0), "Resistance prediction used wrong layer.");
                foreach (var cell in field.squaresArray)
                    foreach (var layer in cell.subSquares) layer.GetComponent<ParcelStackLayer>().remainingLayers = 1;
                Check(!snapshot.CanReach(0, 0, 4, 0), "Snapshot resistance changed during an activation.");
                var finalHit = ImpactBarrierSnapshot.Capture(field);
                Check(finalHit.Filter(origin, field.squaresArray).Count == 7, "Final hit failed to reach ordinary pieces.");
                var area = ImpactBarrierSnapshot.AreaAround(origin, 2, finalHit);
                Check(area.Contains(field.GetSquare(2, 0)) && !area.Contains(field.GetSquare(3, 0)), "Crate penetration extended blast range.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
