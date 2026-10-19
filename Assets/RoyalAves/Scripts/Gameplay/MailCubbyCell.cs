using System.Collections;
using System.Linq;
using SweetSugar.Scripts.Blocks;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    // Four blocking cells forward their damage to one shared visual/stock controller.
    [RequireComponent(typeof(Square), typeof(SpriteRenderer))]
    public sealed class MailCubbyCell : MonoBehaviour
    {
        public const int NichesPerCabinet = 2;
        public const int LettersPerNiche = 18;
        public const int LettersPerHit = 9;
        public const int LettersPerCabinet = NichesPerCabinet * LettersPerNiche;

        // Each cabinet occupies four cells and carries six stacks of six letters.
        // Read before the board exists, so the level popup already shows the right number.
        public static int CountLetters(SweetSugar.Scripts.Level.LevelData data) =>
            data == null ? 0 : data.fields.Sum(field => field.levelSquares.Count(cell =>
                cell.blocks != null && cell.blocks.Count > 0
                    ? cell.blocks.Any(layer => layer.squareType == SquareTypes.MailCubby)
                    : cell.block == SquareTypes.MailCubby || cell.obstacle == SquareTypes.MailCubby)) / 4 * LettersPerCabinet;

        public GameObject visualPrefab;
        public MailCubbyBoardController Owner { get; private set; }
        public bool CountsAsObjective { get; set; } = true;
        Square cell;

        IEnumerator Start()
        {
            cell = transform.parent != null ? transform.parent.GetComponent<Square>() : null;
            if (cell == null) yield break;
            yield return null;
            // A cabinet is painted as a 2x2 group of cells; the top-left one builds the shared visual.
            if (!IsGroupAnchor())
            {
                // Anchors build on this frame. Give them one more frame before calling this cell orphaned.
                yield return null;
                if (Owner == null) ReleaseOrphanCell();
                yield break;
            }
            var bottomRight = cell.field.GetSquare(cell.col + 1, cell.row + 1);
            if (bottomRight == null)
            {
                ReleaseOrphanCell();
                yield break;
            }
            var root = Instantiate(visualPrefab, cell.field.transform);
            root.transform.position = (cell.transform.position + bottomRight.transform.position) * 0.5f;
            float width = Mathf.Abs(bottomRight.transform.position.x - cell.transform.position.x) * 2f;
            var animation = root.GetComponent<MailCubbyAnimation>();
            var bounds = animation.cabinetVisibleBounds;
            if (bounds.width <= 0f || bounds.height <= 0f)
                bounds = new Rect(animation.cabinet.sprite.bounds.min, animation.cabinet.sprite.bounds.size);
            float height = Mathf.Abs(bottomRight.transform.position.y - cell.transform.position.y) * 2f;
            float sx = width / bounds.width;
            float sy = sx;
            root.transform.localScale = new Vector3(sx / Mathf.Abs(cell.field.transform.lossyScale.x), sy / Mathf.Abs(cell.field.transform.lossyScale.y), 1f);
            // Fill both columns with the visible frame, preserving its aspect ratio.
            // The bottom aligns with the reserved cells; only the crest may rise above them.
            var center = (cell.transform.position + bottomRight.transform.position) * 0.5f;
            root.transform.position = new Vector3(center.x - bounds.center.x * sx,
                center.y - height * 0.5f - bounds.yMin * sy, center.z - 0.6f);
            Owner = root.AddComponent<MailCubbyBoardController>();
            var cells = new Square[4];
            int index = 0;
            for (int row = cell.row; row <= cell.row + 1; row++)
                for (int col = cell.col; col <= cell.col + 1; col++)
                {
                    var boardCell = cell.field.GetSquare(col, row);
                    cells[index++] = boardCell;
                    var marker = boardCell.GetSubSquare().GetComponent<MailCubbyCell>();
                    if (marker != null) marker.Owner = Owner;
                }
            Owner.Initialize(animation, cells);
        }

        bool HasCubby(int col, int row)
        {
            var square = cell.field.GetSquare(col, row);
            if (square == null || square.subSquares == null || square.subSquares.Count == 0) return false;
            return square.GetSubSquare().GetComponent<MailCubbyCell>() != null;
        }

        // Anchor = top-left of its 2x2: right, below and diagonal are cabinet cells, and an even
        // number of cabinet cells lies to the left and above (so side-by-side cabinets pair up).
        bool IsGroupAnchor()
        {
            if (!HasCubby(cell.col + 1, cell.row) || !HasCubby(cell.col, cell.row + 1) || !HasCubby(cell.col + 1, cell.row + 1))
                return false;
            int left = 0, up = 0;
            while (HasCubby(cell.col - 1 - left, cell.row)) left++;
            while (HasCubby(cell.col, cell.row - 1 - up)) up++;
            return left % 2 == 0 && up % 2 == 0;
        }

        // A cabinet cell without a group would be an invisible wall: the renderer is off and no
        // controller takes its hits. Free the cell instead of shipping an unplayable board.
        void ReleaseOrphanCell()
        {
            Debug.LogWarning($"Armario postal incompleto em ({cell.col},{cell.row}): o movel ocupa um 2x2 inteiro. " +
                             "Celula liberada. Repinte o armario no editor de fases.");
            cell.CompleteLayerRemoval(GetComponent<Square>());
        }

        void LateUpdate() { GetComponent<SpriteRenderer>().enabled = false; }
        public void HandleHit() { if (Owner != null) Owner.AcceptImpact(); }
    }
}
