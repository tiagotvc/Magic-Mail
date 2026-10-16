using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.Blocks;

namespace RoyalAves.Meta
{
    public enum BoardBorderTheme { Gold, Christmas, Custom }

    public sealed class BoardBorderRenderer : ScriptableObject
    {
        public const string ResourcePath = "UI/Board/BoardBorderAppearance";
        public BoardBorderTheme theme = BoardBorderTheme.Christmas;
        public Sprite garland;
        public Sprite cornerOrnament;
        public float garlandVisibleWidth = 1f;
        public float ornamentVisibleWidth = 1f;
        public Sprite snowCap;
        public float snowVisibleWidth = 1f;
        [Min(0.1f)] public float snowHeightScale = 1f;
        [Min(0.1f)] public float snowWidthInCells = 0.60f;
        public Sprite christmasHudFrame;
        [Tooltip("Perfil usado quando o tema selecionado for Custom.")]
        public BoardSeasonTheme customSeason;
        static BoardSeasonTheme activeSeason;
        const string SortingLayerName = "Default";
        const int SortingOrder = 10;
        const float ThicknessScale = 0.28f;
        const int CornerSegments = 48;
        static Material borderMaterial;
        static BoardBorderRenderer appearance;

        static readonly List<GameObject> containers = new List<GameObject>();
        static bool subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoInit()
        {
            if (subscribed) return; // guards against a second domain-reload style re-entry
            subscribed = true;
            LevelManager.OnLevelLoaded += Rebuild;
        }

        public static void Refresh() => Rebuild();

        static void Rebuild()
        {
            Clear();
            appearance = Resources.Load<BoardBorderRenderer>(ResourcePath);
            activeSeason = appearance != null && appearance.theme == BoardBorderTheme.Custom
                ? appearance.customSeason : null;
            ApplyHudTheme();
            if (borderMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) return;
                borderMaterial = new Material(shader) { name = "Board frame warm leather" };
            }
            foreach (var board in Object.FindObjectsByType<FieldBoard>(FindObjectsSortMode.None)) BuildForBoard(board);
        }

        static void ApplyHudTheme()
        {
            var hudImages = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var image in hudImages)
                if (image.name == "MolduraFase")
                {
                    // The production HUD is assigned manually; do not replace its artwork with legacy seasonal frames.
                    if (image.sprite != null && image.sprite.name.StartsWith("hud-base-purple-gold")) continue;
                    // The authored sprite is the default, including after a Play-mode script reload.
                    image.overrideSprite = null;
                    SeasonalHudLights.SetChristmas(image, false);
                }
            Sprite frame = activeSeason != null ? activeSeason.hudFrame :
                appearance != null && appearance.theme == BoardBorderTheme.Christmas ? appearance.christmasHudFrame : null;
            if (frame == null) return;
            foreach (var image in hudImages)
            {
                if (image.name != "MolduraFase") continue;
                if (image.sprite != null && image.sprite.name.StartsWith("hud-base-purple-gold")) continue;
                image.overrideSprite = frame;
                SeasonalHudLights.SetChristmas(image, appearance.theme == BoardBorderTheme.Christmas);
            }
        }

        static void Clear()
        {
            // Generated objects can survive a script reload during Play while static lists reset.
            foreach (var board in Object.FindObjectsByType<FieldBoard>(FindObjectsSortMode.None))
                foreach (Transform child in board.transform)
                    if (child.name.StartsWith("BoardBorder (auto)") && !containers.Contains(child.gameObject))
                        containers.Add(child.gameObject);
            for (int i = containers.Count - 1; i >= 0; i--)
            {
                if (containers[i] == null) continue;
                foreach (var filter in containers[i].GetComponentsInChildren<MeshFilter>())
                    if (filter.sharedMesh != null) Object.Destroy(filter.sharedMesh);
                Object.Destroy(containers[i]);
            }
            containers.Clear();
        }

        static void BuildForBoard(FieldBoard board)
        {
            if (board == null || board.squaresArray == null || board.fieldData == null) return;
            int rows = board.fieldData.maxRows, cols = board.fieldData.maxCols;
            if (rows <= 0 || cols <= 0) return;

            var grid = new Square[rows, cols];
            foreach (var sq in board.squaresArray)
            {
                if (sq == null) continue;
                if (sq.row < 0 || sq.row >= rows || sq.col < 0 || sq.col >= cols) continue;
                grid[sq.row, sq.col] = sq;
            }

            bool Active(int r, int c) => r >= 0 && r < rows && c >= 0 && c < cols
                && grid[r, c] != null && !grid[r, c].IsNone();

            Vector3 VertexWorld(Vector2Int v)
            {
                int r = v.y, c = v.x;
                // (row offset, col offset, which corner of that cell this vertex is: sign*(halfWidth,halfHeight))
                var candidates = new (int dr, int dc, float sx, float sy)[]
                {
                    (-1, -1, +1f, -1f), // bottom-right of the cell up-and-left of this vertex
                    (-1,  0, -1f, -1f), // bottom-left of the cell straight above
                    ( 0, -1, +1f, +1f), // top-right of the cell straight left
                    ( 0,  0, -1f, +1f), // top-left of the cell this vertex starts
                };
                foreach (var (dr, dc, sx, sy) in candidates)
                {
                    int rr = r + dr, cc = c + dc;
                    if (!Active(rr, cc)) continue;
                    var center = grid[rr, cc].transform.position;
                    return center + new Vector3(sx * board.squareWidth / 2f, sy * board.squareHeight / 2f, 0f);
                }
                // Shouldn't happen (every boundary vertex touches at least one active cell), but fall
                // back to the board's own authored grid formula rather than throw.
                var local = board.firstSquarePosition + new Vector2(c * board.squareWidth, -r * board.squareHeight);
                return board.transform.TransformPoint(local);
            }

            // Directed unit boundary edges, one per grid-vertex step, walked clockwise around every
            // active cell â€” an edge only exists where an active cell borders a hole or the board edge.
            var outgoing = new Dictionary<Vector2Int, List<Vector2Int>>();
            void AddEdge(Vector2Int from, Vector2Int to)
            {
                if (!outgoing.TryGetValue(from, out var list)) { list = new List<Vector2Int>(); outgoing[from] = list; }
                list.Add(to);
            }
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (!Active(r, c)) continue;
                    if (!Active(r - 1, c)) AddEdge(new Vector2Int(c, r), new Vector2Int(c + 1, r));         // top
                    if (!Active(r, c + 1)) AddEdge(new Vector2Int(c + 1, r), new Vector2Int(c + 1, r + 1)); // right
                    if (!Active(r + 1, c)) AddEdge(new Vector2Int(c + 1, r + 1), new Vector2Int(c, r + 1)); // bottom
                    if (!Active(r, c - 1)) AddEdge(new Vector2Int(c, r + 1), new Vector2Int(c, r));         // left
                }
            }
            if (outgoing.Count == 0) return;

            var container = new GameObject($"BoardBorder (auto) â€” {board.name}");
            container.transform.SetParent(board.transform, false);
            containers.Add(container);

            var used = new HashSet<(Vector2Int, Vector2Int)>();
            foreach (var from in outgoing.Keys.ToList())
                foreach (var to in outgoing[from].ToList())
                    if (!used.Contains((from, to)))
                        TraceLoop(from, to, outgoing, used, board, container, VertexWorld);
        }

        static void TraceLoop(Vector2Int startFrom, Vector2Int startTo, Dictionary<Vector2Int, List<Vector2Int>> outgoing,
            HashSet<(Vector2Int, Vector2Int)> used, FieldBoard board, GameObject container, System.Func<Vector2Int, Vector3> vertexWorld)
        {
            var verts = new List<Vector2Int> { startFrom, startTo };
            used.Add((startFrom, startTo));
            var cur = startTo;
            int guard = 0;
            while (cur != startFrom && guard++ < 100000)
            {
                if (!outgoing.TryGetValue(cur, out var options) || options.Count == 0) break;
                Vector2Int chosen = default; bool found = false;
                foreach (var opt in options) { if (!used.Contains((cur, opt))) { chosen = opt; found = true; break; } }
                if (!found) break;
                used.Add((cur, chosen));
                verts.Add(chosen);
                cur = chosen;
            }
            if (verts.Count >= 2 && verts[verts.Count - 1] == verts[0])
                EmitLoop(verts, board, container, vertexWorld);
        }

        // Round the actual boundary, including notches and holes, then build seamless bevel bands.
        static void EmitLoop(List<Vector2Int> verts, FieldBoard board, GameObject container,
            System.Func<Vector2Int, Vector3> vertexWorld)
        {
            int n = verts.Count - 1;
            if (n < 4) return;
            float cell = Mathf.Min(board.squareWidth, board.squareHeight);
            float radius = cell * 0.20f;
            var path = new List<Vector3>();
            for (int i = 0; i < n; i++)
            {
                var previous = vertexWorld(verts[(i - 1 + n) % n]);
                var current = vertexWorld(verts[i]);
                var next = vertexWorld(verts[(i + 1) % n]);
                var incoming = (current - previous).normalized;
                var outgoing = (next - current).normalized;
                if (Vector3.Dot(incoming, outgoing) > 0.999f) continue;
                var entry = current - incoming * radius;
                var exit = current + outgoing * radius;
                for (int step = 0; step <= CornerSegments; step++)
                {
                    float t = (float)step / CornerSegments;
                    path.Add((1f - t) * (1f - t) * entry + 2f * (1f - t) * t * current + t * t * exit);
                }
            }
            if (path.Count < 3) return;

            float bandUnit = cell * ThicknessScale;
            float feather = EdgeFeather(path[0], cell);
            // Offsets are measured outward from the grid; the pieces keep their existing space.
            EmitBand("Brown inner rim", path, -0.015f * bandUnit, 0.045f * bandUnit,
                new Color32(147, 81, 26, 255), container, 0, innerFeather: feather);
            EmitBand("Cream inner highlight", path, 0.045f * bandUnit, 0.065f * bandUnit,
                new Color32(255, 225, 165, 255), container, 1);
            EmitBand("Warm leather bevel", path, 0.065f * bandUnit, 0.145f * bandUnit,
                new Color32(246, 178, 54, 255), container, 2);
            EmitBand("Brown outer bevel", path, 0.145f * bandUnit, 0.185f * bandUnit,
                new Color32(192, 114, 30, 255), container, 3);
            EmitBand("Outer fine highlight", path, 0.185f * bandUnit, 0.198f * bandUnit,
                new Color32(237, 177, 89, 255), container, 4, outerFeather: feather);
            // The board keeps a clean thin frame; seasonal decorations remain in the surrounding HUD.
        }

        static void DecorateChristmas(List<Vector2Int> vertices, GameObject container,
            System.Func<Vector2Int, Vector3> vertexWorld, float cell)
        {
            // Christmas ornaments belong to the HUD. Only snow rests on the board frame.
            float area = 0f;
            for (int i = 0; i < vertices.Count - 1; i++)
            {
                var a = vertexWorld(vertices[i]);
                var b = vertexWorld(vertices[i + 1]);
                area += a.x * b.y - b.x * a.y;
            }
            if (area >= 0f) return; // Interior holes have the opposite winding.
            bool snow = activeSeason == null || activeSeason.snow;
            var corners = new List<Vector3>();
            int count = vertices.Count - 1;
            for (int i = 0; i < count; i++)
            {
                var point = vertexWorld(vertices[i]);
                var before = (point - vertexWorld(vertices[(i - 1 + count) % count])).normalized;
                var after = (vertexWorld(vertices[(i + 1) % count]) - point).normalized;
                if (Vector3.Dot(before, after) < 0.999f) corners.Add(point);
            }
            // Decorate the highest exterior ledge only, including irregular boards.
            float highest = corners.Max(point => point.y);
            for (int i = 0; i < corners.Count; i++)
            {
                var current = corners[i];
                var next = corners[(i + 1) % corners.Count];
                var direction = (next - current).normalized;
                if (direction.x < 0.5f || current.y < highest - cell * 0.01f) continue;
                float length = Vector3.Distance(current, next);
                var snowSprite = activeSeason != null ? activeSeason.snowCap : appearance.snowCap;
                float snowWidth = activeSeason != null ? activeSeason.snowVisibleWidth : appearance.snowVisibleWidth;
                float snowHeight = activeSeason != null ? activeSeason.snowHeightScale : appearance.snowHeightScale;
                float snowCellWidth = activeSeason != null ? activeSeason.snowWidthInCells : appearance.snowWidthInCells;
                if (snow && snowSprite != null)
                {
                    float width = Mathf.Min(cell * snowCellWidth, length * 0.35f);
                    // The pivot is the intersection of the cutout's horizontal and vertical
                    // support tangents. Cover the entire bevel around the corner, including
                    // its inner highlight, rather than only resting on the outside edge.
                    float seat = cell * (ThicknessScale * 0.198f - 0.090f);
                    float topSeat = seat - cell * 0.025f;
                    Decoration("Left snow cap", snowSprite, snowWidth, width,
                        current + new Vector3(-seat, topSeat), 0f, container, SortingOrder + 7, snowHeight);
                    var right = Decoration("Right snow cap", snowSprite, snowWidth, width,
                        next + new Vector3(seat, topSeat), 0f, container, SortingOrder + 7, snowHeight);
                    right.GetComponent<SpriteRenderer>().flipX = true;
                }
            }
        }

        static GameObject Decoration(string name, Sprite sprite, float visibleWidth, float width,
            Vector3 position, float angle, GameObject container, int order, float heightScale = 1f)
        {
            var root = new GameObject(name);
            root.transform.SetParent(container.transform, false);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            float scale = width / Mathf.Max(visibleWidth, 0.001f);
            var parentScale = container.transform.lossyScale;
            root.transform.localScale = new Vector3(scale / Mathf.Abs(parentScale.x), scale * heightScale / Mathf.Abs(parentScale.y), 1f);
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = SortingLayerName;
            renderer.sortingOrder = order;
            return root;
        }

        static float EdgeFeather(Vector3 point, float cell)
        {
            var camera = Camera.main;
            if (camera == null) return cell / 256f;
            float pixelsPerCell = Vector2.Distance(camera.WorldToScreenPoint(point),
                camera.WorldToScreenPoint(point + Vector3.right * cell));
            // Subpixel coverage softens the silhouette without blurring the gold bands.
            return Mathf.Min(cell * 0.015f, cell * 0.75f / Mathf.Max(1f, pixelsPerCell));
        }

        static void EmitBand(string name, List<Vector3> path, float inner, float outer,
            Color baseColor, GameObject container, int order, float innerFeather = 0f, float outerFeather = 0f)
        {
            int count = path.Count;
            var offsets = new List<float>();
            var opacity = new List<float>();
            if (innerFeather > 0f) { offsets.Add(inner - innerFeather); opacity.Add(0f); }
            offsets.Add(inner); opacity.Add(1f);
            offsets.Add(outer); opacity.Add(1f);
            if (outerFeather > 0f) { offsets.Add(outer + outerFeather); opacity.Add(0f); }
            int rows = offsets.Count;
            var vertices = new Vector3[count * rows];
            var colors = new Color[count * rows];
            var triangles = new int[count * (rows - 1) * 6];
            for (int i = 0; i < count; i++)
            {
                var before = (path[i] - path[(i - 1 + count) % count]).normalized;
                var after = (path[(i + 1) % count] - path[i]).normalized;
                var tangent = (before + after).normalized;
                var normal = new Vector3(-tangent.y, tangent.x, 0f);
                float miter = 1f / Mathf.Max(0.7f, Vector3.Dot(tangent, after));
                // A fixed upper-left light gives the frame a soft, polished relief.
                float light = Mathf.Clamp01(0.5f + 0.35f * normal.y - 0.15f * normal.x);
                Color shade = Color.Lerp(baseColor * 0.78f, Color.Lerp(baseColor, Color.white, 0.22f), light);
                for (int row = 0; row < rows; row++)
                {
                    int vertex = rows * i + row;
                    vertices[vertex] = container.transform.InverseTransformPoint(path[i] + normal * offsets[row] * miter);
                    shade.a = opacity[row];
                    colors[vertex] = shade;
                }
                int next = (i + 1) % count;
                for (int row = 0; row < rows - 1; row++)
                {
                    int at = (i * (rows - 1) + row) * 6;
                    int a = rows * i + row, b = rows * next + row;
                    triangles[at] = a;
                    triangles[at + 1] = b;
                    triangles[at + 2] = a + 1;
                    triangles[at + 3] = a + 1;
                    triangles[at + 4] = b;
                    triangles[at + 5] = b + 1;
                }
            }
            var mesh = new Mesh { name = name, vertices = vertices, colors = colors, triangles = triangles };
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(container.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = borderMaterial;
            renderer.sortingLayerName = SortingLayerName;
            renderer.sortingOrder = SortingOrder + order;
        }
    }
}
