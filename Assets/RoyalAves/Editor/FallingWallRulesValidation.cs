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
                    foreach (var corner in new[] { side, forward })
                    {
                        corner.type = SquareTypes.NONE;
                        Expect(!source.CanFallDiagonallyTo(target), "A missing corner must block the fall");
                        corner.type = SquareTypes.EmptySquare;
                        corner.canMoveIn = false;
                        Expect(!source.CanFallDiagonallyTo(target), "A crate corner must block the fall");
                        corner.canMoveIn = true;
                        corner.canMoveOut = false;
                        Expect(!source.CanFallDiagonallyTo(target), "A one-way blocked corner must block the fall");
                        corner.canMoveOut = true;
                    }
                    var x = new Vector2(dx, 0); var y = new Vector2(0, -dy);
                    WallBlocks(source, target, source, x); WallBlocks(source, target, side, -x);
                    WallBlocks(source, target, side, y); WallBlocks(source, target, target, -y);
                    WallBlocks(source, target, source, y); WallBlocks(source, target, forward, -y);
                    WallBlocks(source, target, forward, x); WallBlocks(source, target, target, -x);
                    Expect(source.CanFallDiagonallyTo(target), "Removing the wall must reopen the diagonal");
                }
                Expect(!source.CanFallDiagonallyTo(source), "A stationary destination is not diagonal");
                Expect(!source.CanFallDiagonallyTo(field.GetSquare(1, 2)), "A straight move is not diagonal");
                Debug.Log("Queda validada: diagonais abertas, buracos, caixas, paredes dos dois lados e passagem após liberação, nos quatro sentidos.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void WallBlocks(Square source, Square target, Square wall, Vector2 direction)
        {
            wall.directionRestriction.Add(direction);
            Expect(!source.CanFallDiagonallyTo(target), "A separator must block the diagonal on either side");
            wall.directionRestriction.Remove(direction);
        }
        static void Expect(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
