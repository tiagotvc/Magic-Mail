using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MapPropShadow : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var bounds = rectTransform.rect;
            mesh.AddVert(bounds.center, color, Vector2.zero);
            const int segments = 20;
            for (var i = 0; i <= segments; i++)
            {
                var angle = i * Mathf.PI * 2 / segments;
                mesh.AddVert(bounds.center + new Vector2(Mathf.Cos(angle) * bounds.width * 0.5f,
                    Mathf.Sin(angle) * bounds.height * 0.5f), color, Vector2.zero);
                if (i > 0) mesh.AddTriangle(0, i, i + 1);
            }
        }
    }
}
