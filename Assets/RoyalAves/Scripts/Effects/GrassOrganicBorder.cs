using RoyalAves.Gameplay;
using SweetSugar.Scripts.Blocks;
using UnityEngine;

namespace RoyalAves.Effects
{
    // Capture the initial contour; clearing adjacent cells must never grow surviving foliage.
    public sealed class GrassOrganicBorder : MonoBehaviour
    {
        const int Segments = 24;
        SpriteRenderer source;
        Sprite sprite;
        Mesh mesh;
        Material material;
        MeshRenderer visual;
        Square cell;
        int edgeMask = -1;
        Vector3[] vertices;

        public void Show(SpriteRenderer renderer, Square boardCell, bool visible)
        {
            source = renderer;
            cell = boardCell;
            // Capture hidden layers too, before damage can open a neighboring cell.
            // Both tones retain the footprint established when the board was created.
            if (edgeMask < 0) edgeMask = ExposedEdges();
            if (!visible)
            {
                if (visual != null) visual.enabled = false;
                return;
            }
            if (visual == null) CreateVisual();
            if (sprite != source.sprite)
            {
                sprite = source.sprite;
                BuildMesh();
                ShapeEdges();
            }
            visual.sortingLayerID = source.sortingLayerID;
            visual.sortingOrder = source.sortingOrder;
            material.SetColor("_Color", source.color);
            visual.enabled = true;
        }

        void CreateVisual()
        {
            var root = new GameObject("OrganicGrassVisual");
            root.transform.SetParent(transform, false);
            root.AddComponent<MeshFilter>();
            visual = root.AddComponent<MeshRenderer>();
            material = new Material(source.sharedMaterial);
            material.SetColor("_Color", Color.white);
            visual.sharedMaterial = material;
        }

        void BuildMesh()
        {
            if (mesh != null) Destroy(mesh);
            int stride = Segments + 1;
            vertices = new Vector3[stride * stride];
            var uv = new Vector2[vertices.Length];
            var colors = new Color32[vertices.Length];
            var triangles = new int[Segments * Segments * 6];
            var rect = sprite.textureRect;
            for (int y = 0; y <= Segments; y++)
            for (int x = 0; x <= Segments; x++)
            {
                int i = y * stride + x;
                uv[i] = new Vector2((rect.x + rect.width * x / Segments) / sprite.texture.width,
                    (rect.y + rect.height * y / Segments) / sprite.texture.height);
                colors[i] = Color.white;
                if (x == Segments || y == Segments) continue;
                int t = (y * Segments + x) * 6;
                triangles[t] = i; triangles[t + 1] = i + stride; triangles[t + 2] = i + 1;
                triangles[t + 3] = i + 1; triangles[t + 4] = i + stride; triangles[t + 5] = i + stride + 1;
            }
            mesh = new Mesh { name = "OrganicGrassSurface", vertices = vertices, uv = uv,
                colors32 = colors, triangles = triangles };
            visual.GetComponent<MeshFilter>().sharedMesh = mesh;
            material.SetTexture("_MainTex", sprite.texture);
        }

        int ExposedEdges()
        {
            if (cell == null || cell.field == null) return 15;
            return (HasGrass(cell.col - 1, cell.row) ? 0 : 1) |
                (HasGrass(cell.col + 1, cell.row) ? 0 : 2) |
                (HasGrass(cell.col, cell.row - 1) ? 0 : 4) |
                (HasGrass(cell.col, cell.row + 1) ? 0 : 8);
        }

        bool HasGrass(int col, int row)
        {
            // The safe overload clamps out-of-board coordinates back onto an occupied cell.
            var neighbor = cell.field.GetSquare(col, row);
            if (neighbor == null) return false;
            foreach (var layer in neighbor.subSquares)
                if (layer != null && layer.GetComponent<InkFloorLayer>() != null) return true;
            return false;
        }

        void ShapeEdges()
        {
            float width = sprite.rect.width / sprite.pixelsPerUnit;
            float height = sprite.rect.height / sprite.pixelsPerUnit;
            float left = -sprite.pivot.x / sprite.pixelsPerUnit;
            float bottom = -sprite.pivot.y / sprite.pixelsPerUnit;
            float right = left + width;
            float top = bottom + height;
            bool exposedLeft = (edgeMask & 1) != 0;
            bool exposedRight = (edgeMask & 2) != 0;
            bool exposedTop = (edgeMask & 4) != 0;
            bool exposedBottom = (edgeMask & 8) != 0;
            float cellWidth = width / 1.08f, cellHeight = height / 1.08f;
            if (cell != null && cell.field != null)
            {
                var half = new Vector3(cell.field.squareWidth, cell.field.squareHeight) * 0.5f;
                var lower = transform.InverseTransformPoint(cell.field.transform.TransformPoint(cell.transform.localPosition - half));
                var upper = transform.InverseTransformPoint(cell.field.transform.TransformPoint(cell.transform.localPosition + half));
                cellWidth = upper.x - lower.x;
                cellHeight = upper.y - lower.y;
                // Retain overlap only between grass cells. Exposed sides follow the grid footprint.
                if (exposedLeft) left = lower.x;
                if (exposedRight) right = upper.x;
                if (exposedBottom) bottom = lower.y;
                if (exposedTop) top = upper.y;
            }
            float radius = Mathf.Min(cellWidth, cellHeight) * 0.13f;
            for (int y = 0; y <= Segments; y++)
            for (int x = 0; x <= Segments; x++)
            {
                float u = (float)x / Segments, v = (float)y / Segments;
                // Small inward variation softens the silhouette without protruding lobes.
                float horizontal = (cell != null ? cell.col : 0) + u;
                float vertical = (cell != null ? cell.row : 0) - v;
                var point = new Vector2(Mathf.Lerp(left, right, u), Mathf.Lerp(bottom, top, v));
                if (exposedLeft) point.x += cellWidth * EdgeWeight(1f - u) * EdgeInset(vertical, 0.7f);
                if (exposedRight) point.x -= cellWidth * EdgeWeight(u) * EdgeInset(vertical, 1.7f);
                if (exposedTop) point.y -= cellHeight * EdgeWeight(v) * EdgeInset(horizontal, 2.3f);
                if (exposedBottom) point.y += cellHeight * EdgeWeight(1f - v) * EdgeInset(horizontal, 3.3f);
                if (exposedLeft && exposedTop) RoundCorner(ref point, new Vector2(left + radius, top - radius), -1f, 1f, radius);
                if (exposedRight && exposedTop) RoundCorner(ref point, new Vector2(right - radius, top - radius), 1f, 1f, radius);
                if (exposedLeft && exposedBottom) RoundCorner(ref point, new Vector2(left + radius, bottom + radius), -1f, -1f, radius);
                if (exposedRight && exposedBottom) RoundCorner(ref point, new Vector2(right - radius, bottom + radius), 1f, -1f, radius);
                vertices[y * (Segments + 1) + x] = new Vector3(point.x, point.y);
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        static void RoundCorner(ref Vector2 point, Vector2 center, float signX, float signY, float radius)
        {
            var delta = point - center;
            if (delta.x * signX > 0f && delta.y * signY > 0f && delta.sqrMagnitude > radius * radius)
                point = center + delta.normalized * radius;
        }

        static float EdgeWeight(float value) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.76f, 1f, value));

        static float EdgeInset(float along, float phase)
        {
            return 0.002f + (0.5f + 0.5f * Mathf.Sin(along * 5.2f + phase)) * 0.004f;
        }

        // The removal effect owns these copies, so destroying a layer cannot cut its fade short.
        public MeshRenderer CopyForFade(GameObject root, out Mesh meshCopy, out Material materialCopy)
        {
            meshCopy = null;
            materialCopy = null;
            if (mesh == null || visual == null) return null;
            meshCopy = Instantiate(mesh);
            materialCopy = new Material(material);
            // A SpriteRenderer cannot share its object with a MeshFilter.
            var fadingSurface = new GameObject("FadingOrganicGrass");
            fadingSurface.transform.SetParent(root.transform, false);
            fadingSurface.AddComponent<MeshFilter>().sharedMesh = meshCopy;
            var copy = fadingSurface.AddComponent<MeshRenderer>();
            copy.sharedMaterial = materialCopy;
            copy.sortingLayerID = source.sortingLayerID;
            copy.sortingOrder = source.sortingOrder;
            return copy;
        }

        void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
        }
    }
}
