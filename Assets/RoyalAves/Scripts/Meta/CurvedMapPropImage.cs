using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    // A sprite card hinged at its feet. At the crest its top recedes and its height compresses;
    // rolling towards the camera brings it upright. The shadow remains on the ground.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CurvedMapPropImage : Image
    {
        float tilt;
        public float Tilt
        {
            get => tilt;
            set
            {
                if (Mathf.Abs(tilt - value) < 0.01f) return;
                tilt = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            base.OnPopulateMesh(mesh);
            var bounds = rectTransform.rect;
            if (bounds.height < 1) return;
            var hinge = bounds.yMin + bounds.height * 0.12f;
            var upright = Mathf.Cos(tilt * Mathf.Deg2Rad);
            var vertex = new UIVertex();
            for (var i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                var height = Mathf.Clamp01((vertex.position.y - hinge) / (bounds.height * 0.88f));
                var receding = 1 - height * (1 - upright) * 0.32f;
                vertex.position.x = bounds.center.x + (vertex.position.x - bounds.center.x) * receding;
                vertex.position.y = hinge + (vertex.position.y - hinge) * upright;
                mesh.SetUIVertex(vertex, i);
            }
        }
    }
}
