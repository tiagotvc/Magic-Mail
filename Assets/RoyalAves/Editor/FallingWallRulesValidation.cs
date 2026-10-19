using System;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Level;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class FallingWallRulesValidation
    {
        [MenuItem("Royal Aves/Validação/Queda sem atravessar paredes")]
        public static void Run()
        {
            var root = new GameObject("FallingWallRulesValidation") { hideFlags = HideFlags.HideAndDontSave };
            root.SetActive(false);
            try
            {
                var field = root.AddComponent<FieldBoard>();
                field.fieldData = new FieldData { maxCols = 3, maxRows = 3 };
                field.squaresArray = new Square[9];
                for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                {
                    var node = new GameObject("ValidationCell");
                    node.transform.SetParent(root.transform, false);
                    var square = node.AddComponent<Square>();
                    square.field = field; square.col = col; square.row = row;
                    square.type = SquareTypes.EmptySquare;
                    square.canMoveIn = square.canMoveOut = true;
                    field.squaresArray[row * 3 + col] = square;
                }
                var source = field.GetSquare(1, 1);
                foreach (int dx in new[] { -1, 1 })
                foreach (int dy in new[] { -1, 1 })
                {
                    var target = field.GetSquare(1 + dx, 1 + dy);
                    var side = field.GetSquare(1 + dx, 1);
                    var forward = field.GetSquare(1, 1 + dy);
                    Expect(source.CanFallDiagonallyTo(target), "Open diagonal must remain available");
                    // One blocked corner is the normal case: the piece slides around the blocker.
                    foreach (var corner in new[] { side, forward })
                    {
                        corner.type = SquareTypes.NONE;
                        Expect(source.CanFallDiagonallyTo(target), "A single missing corner must not block the fall");
                        corner.type = SquareTypes.EmptySquare;
                        corner.canMoveIn = false;
                        Expect(source.CanFallDiagonallyTo(target), "A single crate corner must not block the fall");
                        corner.canMoveIn = true;
                        corner.canMoveOut = false;
                        Expect(source.CanFallDiagonallyTo(target), "A single one-way corner must not block the fall");
                        corner.canMoveOut = true;
                    }
                    // Both corners sealed: the destination would only be reachable through the corner.
                    side.type = forward.type = SquareTypes.NONE;
                    Expect(!source.CanFallDiagonallyTo(target), "Two missing corners must block the fall");
                    side.type = forward.type = SquareTypes.EmptySquare;
                    side.canMoveIn = forward.canMoveIn = false;
                    Expect(!source.CanFallDiagonallyTo(target), "Two crate corners must block the fall");
                    side.canMoveIn = forward.canMoveIn = true;
                    side.type = SquareTypes.NONE;
                    forward.canMoveOut = false;
                    Expect(!source.CanFallDiagonallyTo(target), "A hole plus a one-way corner must block the fall");
                    side.type = SquareTypes.EmptySquare;
                    forward.canMoveOut = true;

                    var x = new Vector2(dx, 0); var y = new Vector2(0, -dy);
                    // A wall on one route leaves the other one open; both walled closes the diagonal.
                    WallKeepsOneRoute(source, target, source, x, source, y);
                    WallKeepsOneRoute(source, target, side, -x, forward, -y);
                    WallKeepsOneRoute(source, target, side, y, forward, x);
                    WallKeepsOneRoute(source, target, target, -y, target, -x);
                    Expect(source.CanFallDiagonallyTo(target), "Removing the wall must reopen the diagonal");
                }
                Expect(!source.CanFallDiagonallyTo(source), "A stationary destination is not diagonal");
                Expect(!source.CanFallDiagonallyTo(field.GetSquare(1, 2)), "A straight move is not diagonal");
                Debug.Log("Queda validada: diagonal passa com um canto bloqueado, trava com os dois cantos fechados, respeita paredes nas duas rotas e reabre após liberação, nos quatro sentidos.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        // Walling one route keeps the diagonal open; walling both closes it.
        static void WallKeepsOneRoute(Square source, Square target, Square firstWall, Vector2 firstDirection,
            Square secondWall, Vector2 secondDirection)
        {
            firstWall.directionRestriction.Add(firstDirection);
            Expect(source.CanFallDiagonallyTo(target), "A separator on one route must not block the diagonal");
            secondWall.directionRestriction.Add(secondDirection);
            Expect(!source.CanFallDiagonallyTo(target), "Separators on both routes must block the diagonal");
            firstWall.directionRestriction.Remove(firstDirection);
            secondWall.directionRestriction.Remove(secondDirection);
        }
        static void Expect(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
