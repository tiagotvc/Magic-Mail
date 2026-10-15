using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    // Small, flat cartoon discs match the new world without importing a glossy 3D button.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MapLevelDisc : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = rectTransform.rect;
            var center = rect.center;
            var radius = Mathf.Min(rect.width, rect.height) * 0.44f;
            Circle(mesh, center + new Vector2(0, -radius * 0.12f), radius, new Color32(119, 75, 139, 255));
            Circle(mesh, center, radius, new Color32(255, 239, 181, 255));
            Circle(mesh, center + new Vector2(0, radius * 0.02f), radius * 0.84f, new Color32(187, 100, 218, 255));
            Circle(mesh, center + new Vector2(-radius * 0.22f, radius * 0.35f), radius * 0.14f, new Color32(222, 166, 239, 255));
        }

        void Circle(VertexHelper mesh, Vector2 center, float radius, Color tint)
        {
            const int segments = 40;
            var start = mesh.currentVertCount;
            tint *= color;
            mesh.AddVert(center, tint, Vector2.zero);
            for (var i = 0; i <= segments; i++)
            {
                var angle = i * Mathf.PI * 2 / segments;
                mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.83f) * radius, tint, Vector2.zero);
                if (i > 0) mesh.AddTriangle(start, start + i, start + i + 1);
            }
        }
    }
}
