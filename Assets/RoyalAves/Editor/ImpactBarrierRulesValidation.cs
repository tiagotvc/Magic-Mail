using System;
using System.Collections.Generic;
using RoyalAves.Gameplay;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class ImpactBarrierRulesValidation
    {
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
            Debug.Log("PASS: crate impact barriers, diagonal corners, activation snapshots and reciprocal paths.");
        }
    }
}
