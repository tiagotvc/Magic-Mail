// Fita de luz desenhada na UI (Canvas): o rastro mágico atrás de cada power-up que sai do baú. Recebe uma lista de
// pontos (do mais novo, na cabeça, ao mais velho, na cauda) e monta uma tira que afina e some em direção à cauda.
// Usada pela ChestOpenScreen com o material aditivo do projeto.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Effects
{
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("")]
    public class ChestTrailGraphic : MaskableGraphic
    {
        readonly List<Vector2> points = new List<Vector2>();
        Texture profile;
        float width;
        Color head = Color.white, tail = Color.white;

        public override Texture mainTexture => profile != null ? profile : s_WhiteTexture;

        public void SetProfile(Texture texture)
        {
            profile = texture;
            SetMaterialDirty();
        }

        /// Pontos em coordenadas locais deste objeto, da cabeça para a cauda. A opacidade vem das cores.
        public void SetTrail(IList<Vector2> source, float trailWidth, Color headColor, Color tailColor)
        {
            points.Clear();
            if (source != null)
                for (var i = 0; i < source.Count; i++) points.Add(source[i]);
            width = trailWidth;
            head = headColor;
            tail = tailColor;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var n = points.Count;
            if (n < 2 || width <= 0f || head.a <= 0.001f && tail.a <= 0.001f) return;

            // Primeira direção válida: pontos repetidos (o power-up já parou) não têm direção própria.
            var normal = Vector2.up;
            for (var i = 0; i < n - 1; i++)
            {
                var first = points[i + 1] - points[i];
                if (first.sqrMagnitude < 0.0001f) continue;
                normal = new Vector2(-first.y, first.x).normalized;
                break;
            }

            for (var i = 0; i < n; i++)
            {
                var direction = points[Mathf.Min(i + 1, n - 1)] - points[Mathf.Max(i - 1, 0)];
                if (direction.sqrMagnitude > 0.0001f) normal = new Vector2(-direction.y, direction.x).normalized;
                var f = i / (float)(n - 1);
                var half = width * 0.5f * Mathf.Pow(1f - f, 0.75f) * Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(f * 5f));
                var color = Color.Lerp(head, tail, f);
                color.a *= 1f - f;
                vh.AddVert(points[i] + normal * half, color, new Vector2(f, 1f));
                vh.AddVert(points[i] - normal * half, color, new Vector2(f, 0f));
            }
            for (var i = 0; i < n - 1; i++)
            {
                var v = i * 2;
                vh.AddTriangle(v, v + 1, v + 2);
                vh.AddTriangle(v + 2, v + 1, v + 3);
            }
        }
    }
}
