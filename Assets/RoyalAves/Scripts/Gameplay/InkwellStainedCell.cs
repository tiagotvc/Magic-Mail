using RoyalAves.Effects;
using SweetSugar.Scripts.Blocks;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    // Lives on the board cell, independently from the moving piece and removed obstacle.
    public sealed class InkwellStainedCell : MonoBehaviour
    {
        Square cell;
        Material template;
        float duration;
        float activateAt;
        public InkTintVisual Floor { get; private set; }
        public static InkwellStainedCell Paint(Square cell, Vector3 from, Material material, float duration, float delay = 0)
        {
            var stain = cell.GetComponent<InkwellStainedCell>();
            if (stain != null) return stain;
            if (material == null) throw new System.ArgumentNullException(nameof(material));
            stain = cell.gameObject.AddComponent<InkwellStainedCell>();
            stain.cell = cell; stain.template = material; stain.duration = duration;
            stain.activateAt = Time.time + delay;
            Vector3 direction = cell.transform.InverseTransformPoint(from);
            Vector2 origin = new Vector2(.5f, .5f) + Vector2.ClampMagnitude(direction, 1f) * .48f;
            // Mirror the full existing cell sprite, including its corners, rather than drawing a puddle.
            stain.Floor = InkTintVisual.Attach(cell.mainRenderer, material, origin, duration, true, delay);
            stain.TintOccupant();
            return stain;
        }
        void LateUpdate()
        {
            TintOccupant();
            if (cell == null || Floor == null) return;
            bool Painted(int x, int y)
            {
                var neighbor = cell.field != null ? cell.field.GetSquare(x, y) : null;
                var stain = neighbor != null ? neighbor.GetComponent<InkwellStainedCell>() : null;
                return stain != null && stain.Floor != null && stain.Floor.Progress > 0;
            }
            bool left=Painted(cell.col-1,cell.row), right=Painted(cell.col+1,cell.row);
            bool bottom=Painted(cell.col,cell.row+1), top=Painted(cell.col,cell.row-1);
            Floor.SetFloorContour(new Vector4(!left&&!bottom?1:0, !right&&!bottom?1:0,
                !left&&!top?1:0, !right&&!top?1:0), new Vector4(left?0:1,right?0:1,bottom?0:1,top?0:1));
        }
        void TintOccupant()
        {
            if (Time.time < activateAt) return;
            var item = cell != null ? cell.Item : null;
            if (item == null || !item.gameObject.activeInHierarchy || item.destroying) return;
            var stain = item.GetComponent<InkwellStainedPiece>();
            if (stain == null) stain = item.gameObject.AddComponent<InkwellStainedPiece>();
            stain.Paint(template, duration);
        }
    }
}
