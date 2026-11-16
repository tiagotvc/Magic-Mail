// Geometria do contorno das dicas: dado um conjunto de casas do tabuleiro, calcula o PERÍMETRO EXTERNO da união
// delas (sem bordas internas entre casas vizinhas). Funciona para 3/4/5 em linha, L, T, cruz e conjuntos separados.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoyalAves.Effects
{
    /// Mapeia (coluna, linha) do tabuleiro para o mundo: o centro da casa (c, r) é origin + ex*c + ey*r.
    public struct HintGrid
    {
        public Vector3 origin, ex, ey;
        public Vector3 Center(int c, int r) => origin + ex * c + ey * r;
        /// Ponto de malha: (x, y) = cantos entre as casas; a casa (c, r) ocupa x de c até c+1.
        public Vector3 Lattice(float x, float y) => origin + ex * (x - 0.5f) + ey * (y - 0.5f);
        public float Cell => 0.5f * (ex.magnitude + ey.magnitude);
        /// true quando ex→ey gira no sentido horário (eixo Y do tabuleiro apontando para baixo).
        public bool Mirrored => (ex.x * ey.y - ex.y * ey.x) < 0f;
    }

    public static class HintShape
    {
        /// Laços fechados (em coordenadas de malha) com as casas sempre à esquerda de cada aresta.
        public static List<List<Vector2Int>> Loops(IEnumerable<Vector2Int> cells)
        {
            var set = new HashSet<Vector2Int>(cells);
            var edges = new Dictionary<Vector2Int, List<Vector2Int>>();
            void Add(Vector2Int a, Vector2Int b)
            {
                if (!edges.TryGetValue(a, out var list)) edges[a] = list = new List<Vector2Int>();
                list.Add(b);
            }

            foreach (var c in set)
            {
                var x = c.x; var y = c.y;
                if (!set.Contains(new Vector2Int(x, y - 1))) Add(new Vector2Int(x, y), new Vector2Int(x + 1, y));
                if (!set.Contains(new Vector2Int(x + 1, y))) Add(new Vector2Int(x + 1, y), new Vector2Int(x + 1, y + 1));
                if (!set.Contains(new Vector2Int(x, y + 1))) Add(new Vector2Int(x + 1, y + 1), new Vector2Int(x, y + 1));
                if (!set.Contains(new Vector2Int(x - 1, y))) Add(new Vector2Int(x, y + 1), new Vector2Int(x, y));
            }

            var loops = new List<List<Vector2Int>>();
            while (true)
            {
                var start = edges.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key).Cast<Vector2Int?>().FirstOrDefault();
                if (start == null) break;
                var s = start.Value;
                var pts = new List<Vector2Int>();
                var cur = s;
                do
                {
                    pts.Add(cur);
                    var list = edges[cur];
                    if (list.Count == 0) break;
                    var next = list[0];
                    list.RemoveAt(0);
                    cur = next;
                } while (cur != s && pts.Count < 4096);

                var simple = RemoveCollinear(pts);
                if (simple.Count >= 4) loops.Add(simple);
            }
            return loops;
        }

        static List<Vector2Int> RemoveCollinear(List<Vector2Int> pts)
        {
            var result = new List<Vector2Int>();
            for (var i = 0; i < pts.Count; i++)
            {
                var prev = pts[(i + pts.Count - 1) % pts.Count];
                var cur = pts[i];
                var next = pts[(i + 1) % pts.Count];
                var a = cur - prev; var b = next - cur;
                if (a.x * b.y - a.y * b.x == 0 && a.x * b.x + a.y * b.y > 0) continue;
                result.Add(cur);
            }
            return result;
        }

        /// Caminho fechado no mundo com cantos arredondados e amostrado em passos regulares (distância `spacing`).
        public static List<Vector3> Smooth(List<Vector2Int> loop, HintGrid grid, float radius, int arcSteps, float spacing)
        {
            var corners = loop.Select(p => grid.Lattice(p.x, p.y)).ToList();
            var rounded = new List<Vector3>();
            for (var i = 0; i < corners.Count; i++)
            {
                var prev = corners[(i + corners.Count - 1) % corners.Count];
                var cur = corners[i];
                var next = corners[(i + 1) % corners.Count];
                var d1 = prev - cur; var d2 = next - cur;
                var r = Mathf.Min(radius, d1.magnitude * 0.5f, d2.magnitude * 0.5f);
                var a = cur + d1.normalized * r;
                var b = cur + d2.normalized * r;
                for (var k = 0; k <= arcSteps; k++)
                {
                    var t = k / (float)arcSteps;
                    rounded.Add(Vector3.Lerp(Vector3.Lerp(a, cur, t), Vector3.Lerp(cur, b, t), t));
                }
            }

            var result = new List<Vector3>();
            for (var i = 0; i < rounded.Count; i++)
            {
                var p = rounded[i];
                var q = rounded[(i + 1) % rounded.Count];
                var length = Vector3.Distance(p, q);
                if (length < 1e-5f) continue;
                var n = Mathf.Max(1, Mathf.CeilToInt(length / spacing));
                for (var k = 0; k < n; k++) result.Add(Vector3.Lerp(p, q, k / (float)n));
            }
            return result;
        }
    }
}
