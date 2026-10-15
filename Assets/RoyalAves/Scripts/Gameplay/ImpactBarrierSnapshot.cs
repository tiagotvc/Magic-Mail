using System;
using System.Collections.Generic;
using System.Linq;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Level;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    // Capture once per activation: breaking the first crate must not extend the same impact.
    public sealed class ImpactBarrierSnapshot
    {
        readonly HashSet<Vector2Int> crates;

        public ImpactBarrierSnapshot(IEnumerable<Vector2Int> blockedCells)
            => crates = new HashSet<Vector2Int>(blockedCells);

        public static ImpactBarrierSnapshot Capture(FieldBoard field)
            => new ImpactBarrierSnapshot(field.squaresArray.Where(s => s != null &&
                s.subSquares.Any(layer => layer != null && layer.GetComponent<ParcelStackLayer>() != null))
                .Select(s => new Vector2Int(s.col, s.row)));

        public bool IsBlocking(int col, int row) => crates.Contains(new Vector2Int(col, row));

        public List<Square> Filter(Square origin, IEnumerable<Square> targets)
            => targets.Where(t => t != null && t.field == origin.field &&
                CanReach(origin.col, origin.row, t.col, t.row)).Distinct().ToList();

        public Item[] FilterItems(Square origin, IEnumerable<Item> targets)
            => targets.Where(t => t != null && t.square != null && t.square.field == origin.field &&
                CanReach(origin.col, origin.row, t.square.col, t.square.row)).Distinct().ToArray();

        public bool CanReach(int fromCol, int fromRow, int toCol, int toRow)
        {
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
                if (x == toCol && y == toRow) return true; // The first blocking crate itself can be hit.
                if (IsBlocking(x, y)) return false;
            }
            return true;
        }
    }
}
