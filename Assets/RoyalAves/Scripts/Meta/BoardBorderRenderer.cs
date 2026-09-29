// Draws a decorative border around whatever shape a level's board actually has, instead of assuming
// a rectangle. It hooks itself into the board-building pipeline directly — no GameObject to add, no
// sprite to drag in the Inspector — via [RuntimeInitializeOnLoadMethod], so it runs for every level
// automatically as soon as the game starts. It reads SweetSugar's grid at runtime (a cell is "in" the
// board unless its SquareTypes is NONE), traces the outline as a sequence of straight runs and turns,
// and places a tiled edge sprite along each run plus a rotated corner sprite at each outer (convex)
// turn. Inner (concave) turns are left as a plain butt-join between two edge runs, since the source
// art only has one outer-corner style; that is the one shape this can't decorate with a matching cap.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.Blocks;

namespace RoyalAves.Meta
{
    public static class BoardBorderRenderer
    {
        // Files live under a Resources/ folder specifically so they can be loaded here without any
        // scene reference: Assets/RoyalAves/Resources/UI/Board/board-border-*.png.
        const string CornerSpritePath = "UI/Board/board-border-corner";
        const string EdgeSpritePath = "UI/Board/board-border-edge";

        // Tune these three directly if the border comes out too thick/thin or overlaps the pieces.
        const string SortingLayerName = "Default";
        const int SortingOrder = 10;
        const float ExtraOutwardOffset = 0f;

        static Sprite cornerSprite;
        static Sprite edgeSprite;
        static readonly List<GameObject> containers = new List<GameObject>();
        static bool subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoInit()
        {
            if (subscribed) return; // guards against a second domain-reload style re-entry
            subscribed = true;
            LevelManager.OnLevelLoaded += Rebuild;
        }

        static float Thickness => edgeSprite.rect.height / edgeSprite.pixelsPerUnit;

        static void Rebuild()
        {
            if (cornerSprite == null) cornerSprite = Resources.Load<Sprite>(CornerSpritePath);
            if (edgeSprite == null) edgeSprite = Resources.Load<Sprite>(EdgeSpritePath);
            Clear();
            if (cornerSprite == null || edgeSprite == null)
            {
                Debug.LogWarning($"BoardBorderRenderer: couldn't load sprites from Resources/{CornerSpritePath} or Resources/{EdgeSpritePath}.");
                return;
            }
            foreach (var board in Object.FindObjectsByType<FieldBoard>(FindObjectsSortMode.None)) BuildForBoard(board);
        }

        static void Clear()
        {
            for (int i = containers.Count - 1; i >= 0; i--) if (containers[i] != null) Object.Destroy(containers[i]);
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
            // active cell — an edge only exists where an active cell borders a hole or the board edge.
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

            var container = new GameObject($"BoardBorder (auto) — {board.name}");
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

        // Run-length-encodes the closed vertex loop into straight segments, emits an edge tile per
        // run, then — once the loop's overall winding is known — a corner tile at every outer turn.
        // Winding matters because a board with a hole in the middle produces a second, inner loop
        // that winds the opposite way round; without checking it, inner and outer corners would swap.
        static void EmitLoop(List<Vector2Int> verts, FieldBoard board, GameObject container, System.Func<Vector2Int, Vector3> vertexWorld)
        {
            int n = verts.Count - 1; // verts[n] == verts[0]
            if (n < 4) return;

            var dirs = new Vector2Int[n];
            for (int i = 0; i < n; i++) dirs[i] = verts[i + 1] - verts[i];

            int start = 0;
            for (int i = 0; i < n; i++)
                if (dirs[i] != dirs[(i - 1 + n) % n]) { start = i; break; }

            // Doubled buffer so a run can never need to wrap its own index range.
            var v2 = new List<Vector2Int>(n * 2 + 1);
            for (int i = 0; i <= n; i++) v2.Add(verts[(start + i) % n]);
            for (int i = 1; i <= n; i++) v2.Add(verts[(start + i) % n]);

            var runs = new List<(Vector2Int startVert, Vector2Int dir)>();
            int pos = 0, consumed = 0;
            while (consumed < n)
            {
                Vector2Int runDir = v2[pos + 1] - v2[pos];
                int runStartPos = pos;
                while (consumed < n && v2[pos + 1] - v2[pos] == runDir) { pos++; consumed++; }
                EmitRun(v2[runStartPos], v2[pos], board, container, vertexWorld);
                runs.Add((v2[runStartPos], runDir));
            }
            if (runs.Count == 0) return;

            // Sum of the turn cross-products: a plain outer boundary sums negative (clockwise in
            // world space); a loop around a hole winds the other way and sums positive.
            float turnSum = 0f;
            for (int i = 0; i < runs.Count; i++)
            {
                var inW = WorldDir(runs[i].dir);
                var outW = WorldDir(runs[(i + 1) % runs.Count].dir);
                turnSum += inW.x * outW.y - inW.y * outW.x;
            }
            bool invert = turnSum > 0f;

            for (int i = 0; i < runs.Count; i++)
            {
                var vertex = runs[(i + 1) % runs.Count].startVert; // shared corner between run i and i+1
                EmitCornerIfConvex(vertex, runs[i].dir, runs[(i + 1) % runs.Count].dir, container, vertexWorld, invert);
            }
        }

        static Vector2 WorldDir(Vector2Int gridDir) => new Vector2(gridDir.x, -gridDir.y);

        static void EmitRun(Vector2Int fromV, Vector2Int toV, FieldBoard board, GameObject container, System.Func<Vector2Int, Vector3> vertexWorld)
        {
            var steps = toV - fromV;
            var worldDelta = WorldDir(steps);
            float gridLen = Mathf.Max(Mathf.Abs(steps.x), Mathf.Abs(steps.y));
            if (gridLen < 1) return;
            var dirNorm = worldDelta / worldDelta.magnitude;
            bool horizontal = Mathf.Abs(dirNorm.x) > Mathf.Abs(dirNorm.y);
            float cellSize = horizontal ? board.squareWidth : board.squareHeight;
            float runWorldLen = gridLen * cellSize;

            var fromWorld = vertexWorld(fromV);
            var toWorld = vertexWorld(toV);
            var mid = (fromWorld + toWorld) / 2f;
            var outwardNormal = new Vector2(-dirNorm.y, dirNorm.x); // +90° from travel direction
            float th = Thickness;
            var pos = (Vector2)mid + outwardNormal * (th / 2f + ExtraOutwardOffset);

            var go = new GameObject("edge");
            go.transform.SetParent(container.transform, false);
            go.transform.position = new Vector3(pos.x, pos.y, mid.z);
            go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dirNorm.y, dirNorm.x) * Mathf.Rad2Deg);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = edgeSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(runWorldLen, th);
            sr.sortingLayerName = SortingLayerName;
            sr.sortingOrder = SortingOrder;
        }

        static void EmitCornerIfConvex(Vector2Int vertex, Vector2Int inGridDir, Vector2Int outGridDir, GameObject container, System.Func<Vector2Int, Vector3> vertexWorld, bool invert)
        {
            var inW = WorldDir(inGridDir);
            var outW = WorldDir(outGridDir);
            float cross = inW.x * outW.y - inW.y * outW.x;
            bool isConvex = invert ? cross > 0.001f : cross < -0.001f;
            if (!isConvex) return; // straight or concave: no matching art for those

            var go = new GameObject("corner");
            go.transform.SetParent(container.transform, false);
            go.transform.position = vertexWorld(vertex);
            go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(outW.y, outW.x) * Mathf.Rad2Deg);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = cornerSprite;
            sr.sortingLayerName = SortingLayerName;
            sr.sortingOrder = SortingOrder;
        }
    }
}
