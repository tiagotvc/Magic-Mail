// Bends a TextMeshPro text's own mesh along a circular arc, for titles that sit inside a curved ribbon
// (like the level-start banner). Works on vertices TMP already generated — doesn't care whether the text
// uses the normal font or the gold sprite font. ExecuteAlways + LateUpdate: the curve updates live in the
// Scene view as you tune Radius/Arc Up, no Play needed.
using TMPro;
using UnityEngine;

namespace RoyalAves.Meta
{
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    public class ArcText : MonoBehaviour
    {
        [Tooltip("Raio do arco em unidades de UI. Menor = curva mais fechada. Comece alto (ex.: 800) e vá reduzindo.")]
        [SerializeField] float radius = 800f;
        [Tooltip("Ligado: meio do texto sobe, pontas descem (cúpula, como a faixa). Desligado: inverte (vale).")]
        [SerializeField] bool archUp = true;
        [Tooltip("Gira cada letra pra acompanhar a tangente do arco — sem isso elas só sobem/descem, continuam 'retas'.")]
        [SerializeField] bool rotateCharacters = true;

        TMP_Text label;
        string lastText;
        float lastRadius;
        bool lastArchUp;

        void OnEnable()
        {
            label = GetComponent<TMP_Text>();
            Apply();
        }

        void LateUpdate()
        {
            if (label == null) label = GetComponent<TMP_Text>();
            // Only re-bend when something actually changed — rewriting the mesh every frame would fight
            // TMP's own regeneration for nothing once the curve already looks right.
            if (label.text == lastText && Mathf.Approximately(radius, lastRadius) && archUp == lastArchUp && !label.havePropertiesChanged) return;
            Apply();
        }

        void Apply()
        {
            label.ForceMeshUpdate();
            var textInfo = label.textInfo;
            var charCount = textInfo.characterCount;
            if (charCount == 0) return;

            var bounds = label.bounds;
            var midX = (bounds.min.x + bounds.max.x) / 2f;
            var r = Mathf.Max(radius, 1f);
            var direction = archUp ? 1f : -1f;

            for (var i = 0; i < charCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                var vertices = textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;
                var charMidX = (vertices[charInfo.vertexIndex + 0].x + vertices[charInfo.vertexIndex + 2].x) / 2f;
                var pivot = new Vector3(charMidX, charInfo.baseLine, 0);

                // Each character's distance from the line's centre becomes an angle (arc length / radius);
                // cos(angle)-1 is 0 at the centre and grows negative toward the edges, which is what pulls
                // the ends down (or up, flipped by "direction") relative to the middle.
                var angle = (charMidX - midX) / r;
                var arcOffset = new Vector3(0, r * (Mathf.Cos(angle) - 1f) * direction, 0);
                var rotation = rotateCharacters ? Quaternion.Euler(0, 0, -angle * Mathf.Rad2Deg * direction) : Quaternion.identity;
                var matrix = Matrix4x4.TRS(arcOffset, rotation, Vector3.one);

                for (var v = 0; v < 4; v++)
                {
                    var local = vertices[charInfo.vertexIndex + v] - pivot;
                    vertices[charInfo.vertexIndex + v] = matrix.MultiplyPoint3x4(local) + pivot;
                }
            }

            label.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            lastText = label.text;
            lastRadius = radius;
            lastArchUp = archUp;
        }
    }
}
