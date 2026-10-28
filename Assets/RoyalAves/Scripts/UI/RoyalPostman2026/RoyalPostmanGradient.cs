using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalPostmanGradient : MaskableGraphic
    {
        public Color top = Color.white, bottom = Color.black;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var r = rectTransform.rect;
            mesh.AddVert(new Vector2(r.xMin, r.yMin), bottom * color, Vector2.zero);
            mesh.AddVert(new Vector2(r.xMin, r.yMax), top * color, Vector2.zero);
            mesh.AddVert(new Vector2(r.xMax, r.yMax), top * color, Vector2.zero);
            mesh.AddVert(new Vector2(r.xMax, r.yMin), bottom * color, Vector2.zero);
            mesh.AddTriangle(0, 1, 2); mesh.AddTriangle(0, 2, 3);
        }
    }
}
