using System;
using System.Collections.Generic;
using System.Linq;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Level;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    // All crates in range take the hit. Only crates surviving it shield ordinary pieces.
    public sealed class ImpactBarrierSnapshot
    {
        readonly HashSet<Vector2Int> crates;
        readonly HashSet<Vector2Int> survivingCrates;

        public ImpactBarrierSnapshot(IEnumerable<Vector2Int> blockedCells, IEnumerable<Vector2Int> brokenByHit = null)
        {
            crates = new HashSet<Vector2Int>(blockedCells);
            survivingCrates = new HashSet<Vector2Int>(crates);
            if (brokenByHit != null) survivingCrates.ExceptWith(brokenByHit);
        }

        public static ImpactBarrierSnapshot Capture(FieldBoard field)
        {
            var cells = field.squaresArray.Where(s => s != null)
                .Select(s => new { Cell = s, Crate = s.subSquares.Where(layer => layer != null)
                    .Select(layer => layer.GetComponent<ParcelStackLayer>()).LastOrDefault(layer => layer != null) })
                .Where(s => s.Crate != null).ToArray();
            return new ImpactBarrierSnapshot(cells.Select(s => new Vector2Int(s.Cell.col, s.Cell.row)),
                cells.Where(s => s.Crate.remainingLayers == 1)
                    .Select(s => new Vector2Int(s.Cell.col, s.Cell.row)));
        }

        public bool IsBlocking(int col, int row) => survivingCrates.Contains(new Vector2Int(col, row));

        public static List<Square> AreaAround(Square origin, int radius, ImpactBarrierSnapshot barriers = null)
        {
            var cells = new List<Square>();
            for (int row = origin.row - radius; row <= origin.row + radius; row++)
            for (int col = origin.col - radius; col <= origin.col + radius; col++)
                cells.Add(origin.field.GetSquare(col, row));
            return (barriers ?? Capture(origin.field)).Filter(origin, cells);
        }

        public List<Square> Filter(Square origin, IEnumerable<Square> targets)
            => targets.Where(t => t != null && t.field == origin.field &&
                CanReach(origin.col, origin.row, t.col, t.row)).Distinct().ToList();

        public Item[] FilterItems(Square origin, IEnumerable<Item> targets)
            => targets.Where(t => t != null && t.square != null && t.square.field == origin.field &&
                CanReach(origin.col, origin.row, t.square.col, t.square.row)).Distinct().ToArray();

        public bool CanReach(int fromCol, int fromRow, int toCol, int toRow)
        {
            // Obstacles receive damage through other crates; the path test protects pieces only.
            if (crates.Contains(new Vector2Int(toCol, toRow))) return true;
            int x = fromCol, y = fromRow;
            int nx = Math.Abs(toCol - x), ny = Math.Abs(toRow - y);
            int sx = Math.Sign(toCol - x), sy = Math.Sign(toRow - y);
            int ix = 0, iy = 0;
            while (ix < nx || iy < ny)
            {
                long decision = (1L + 2L * ix) * ny - (1L + 2L * iy) * nx;
                if (decision == 0)
                {
                    // Supercover: a blast cannot cut through a crate's corner diagonally.
                    if (IsBlocking(x + sx, y) || IsBlocking(x, y + sy)) return false;
                    x += sx; y += sy; ix++; iy++;
                }
                else if (decision < 0) { x += sx; ix++; }
                else { y += sy; iy++; }
                if (x == toCol && y == toRow) return true;
                if (IsBlocking(x, y)) return false;
            }
            return true;
        }
    }
}
