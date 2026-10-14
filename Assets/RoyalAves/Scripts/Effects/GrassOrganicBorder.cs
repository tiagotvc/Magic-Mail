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
            float col = cell != null ? cell.col : 0f;
            float row = cell != null ? cell.row : 0f;
            // The thinner frame cannot hide foliage outside the playable board.
            // Keep organic overlap over playable cells, but contain it at the board perimeter.
            var minimum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            var maximum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            if (cell != null && cell.field != null)
            {
                var halfCell = new Vector3(cell.field.squareWidth, cell.field.squareHeight) * 0.5f;
                var lower = transform.InverseTransformPoint(cell.transform.position - halfCell);
                var upper = transform.InverseTransformPoint(cell.transform.position + halfCell);
                if (!Playable(cell.col - 1, cell.row)) minimum.x = lower.x;
                if (!Playable(cell.col + 1, cell.row)) maximum.x = upper.x;
                if (!Playable(cell.col, cell.row + 1)) minimum.y = lower.y;
                if (!Playable(cell.col, cell.row - 1)) maximum.y = upper.y;
            }
            for (int y = 0; y <= Segments; y++)
            for (int x = 0; x <= Segments; x++)
            {
                float u = (float)x / Segments, v = (float)y / Segments;
                var point = new Vector3((u * sprite.rect.width - sprite.pivot.x) / sprite.pixelsPerUnit,
                    (v * sprite.rect.height - sprite.pivot.y) / sprite.pixelsPerUnit);
                // Coordinates continue across adjacent cells, avoiding a repeated scallop per tile.
                float horizontal = col + (u - 0.5f) * 1.08f;
                float vertical = row - (v - 0.5f) * 1.08f;
                if ((edgeMask & 1) != 0) point.x -= width * EdgeWeight(1f - u) * Bulge(vertical, col + 0.7f);
                if ((edgeMask & 2) != 0) point.x += width * EdgeWeight(u) * Bulge(vertical, col + 1.7f);
                if ((edgeMask & 4) != 0) point.y += height * EdgeWeight(v) * Bulge(horizontal, row + 2.3f);
                if ((edgeMask & 8) != 0) point.y -= height * EdgeWeight(1f - v) * Bulge(horizontal, row + 3.3f);
                if (source.flipX) point.x = -point.x;
                if (source.flipY) point.y = -point.y;
                point.x = Mathf.Clamp(point.x, minimum.x, maximum.x);
                point.y = Mathf.Clamp(point.y, minimum.y, maximum.y);
                vertices[y * (Segments + 1) + x] = point;
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        bool Playable(int col, int row)
        {
            var neighbor = cell.field.GetSquare(col, row);
            return neighbor != null && !neighbor.IsNone();
        }

        static float EdgeWeight(float value) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 0.95f, value));

        static float Bulge(float along, float phase)
        {
            float broad = 0.5f + 0.5f * Mathf.Sin(along * 7.1f + phase * 1.9f);
            float small = 0.5f + 0.5f * Mathf.Sin(along * 17.3f - phase * 2.7f);
            return 0.006f + broad * 0.062f + small * 0.020f;
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
