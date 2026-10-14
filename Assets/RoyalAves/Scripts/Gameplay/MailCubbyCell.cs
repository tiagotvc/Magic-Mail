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
            if (cell.col != 2 || cell.row != 2) yield break;
            var bottomRight = cell.field.GetSquare(3, 3);
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
            for (int row = 2; row <= 3; row++)
                for (int col = 2; col <= 3; col++)
                {
                    var boardCell = cell.field.GetSquare(col, row);
                    cells[index++] = boardCell;
                    var marker = boardCell.GetSubSquare().GetComponent<MailCubbyCell>();
                    if (marker != null) marker.Owner = Owner;
                }
            Owner.Initialize(animation, cells);
        }

        void LateUpdate() { GetComponent<SpriteRenderer>().enabled = false; }
        public void HandleHit() { if (Owner != null) Owner.AcceptImpact(); }
    }
}
