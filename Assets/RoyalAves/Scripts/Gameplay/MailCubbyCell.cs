using System.Collections;
using SweetSugar.Scripts.Blocks;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    // Four blocking cells forward their damage to one shared visual/stock controller.
    [RequireComponent(typeof(Square), typeof(SpriteRenderer))]
    public sealed class MailCubbyCell : MonoBehaviour
    {
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
            if (!IsGroupAnchor()) yield break;
            var bottomRight = cell.field.GetSquare(cell.col + 1, cell.row + 1);
            if (bottomRight == null) yield break;
            var root = Instantiate(visualPrefab, cell.field.transform);
            root.transform.position = (cell.transform.position + bottomRight.transform.position) * 0.5f;
            float width = Mathf.Abs(bottomRight.transform.position.x - cell.transform.position.x) * 2f;
            var animation = root.GetComponent<MailCubbyAnimation>();
            var bounds = animation.cabinetVisibleBounds;
            if (bounds.width <= 0f || bounds.height <= 0f)
                bounds = new Rect(animation.cabinet.sprite.bounds.min, animation.cabinet.sprite.bounds.size);
            float height = Mathf.Abs(bottomRight.transform.position.y - cell.transform.position.y) * 2f;
            float sx = width / bounds.width;
            float sy = height * 1.1f / bounds.height;
            root.transform.localScale = new Vector3(sx / Mathf.Abs(cell.field.transform.lossyScale.x), sy / Mathf.Abs(cell.field.transform.lossyScale.y), 1f);
            // Fit the visible wood, not the transparent canvas. Feet meet the bottom cell edge.
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

        void LateUpdate() { GetComponent<SpriteRenderer>().enabled = false; }
        public void HandleHit() { if (Owner != null) Owner.AcceptImpact(); }
    }
}
