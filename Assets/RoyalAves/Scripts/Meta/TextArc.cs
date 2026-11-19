// Curva um texto TextMeshPro (efeito de faixa/arco). Põe no mesmo objeto do texto.
// Height > 0: o meio sobe (sorriso invertido); Height < 0: o meio desce (faixa de baixo para cima, como "Mais vidas!").
// Rotate Letters: cada letra também gira acompanhando a curva.
using TMPro;
using UnityEngine;

namespace RoyalAves.Meta
{
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("Royal Aves/Text Arc")]
    public class TextArc : MonoBehaviour
    {
        [Tooltip("Forma da curva ao longo do texto (0 = início, 1 = fim). Padrão: sobe até o meio e desce.")]
        public AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));
        [Tooltip("Quanto o ponto mais alto da curva desloca o texto, em unidades do canvas. Negativo inverte.")]
        public float height = 40f;
        public bool rotateLetters = true;

        TMP_Text text;

        void OnEnable()
        {
            text = GetComponent<TMP_Text>();
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
            Apply();
        }

        void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            if (text != null) text.ForceMeshUpdate();
        }

        void OnValidate() { if (isActiveAndEnabled) { text = GetComponent<TMP_Text>(); Apply(); } }

        void OnRectTransformDimensionsChange() { if (isActiveAndEnabled && text != null) Apply(); }

        void OnTextChanged(Object changed)
        {
            if (changed == text) Apply();
        }

        bool applying;

        void Apply()
        {
            if (text == null || applying) return;
            applying = true;
            try
            {
                text.ForceMeshUpdate();
                var info = text.textInfo;
                if (info == null || info.characterCount == 0) return;

                var bounds = text.textBounds;
                var width = Mathf.Max(1f, bounds.size.x);

                for (var i = 0; i < info.characterCount; i++)
                {
                    var c = info.characterInfo[i];
                    if (!c.isVisible) continue;
                    var vertices = info.meshInfo[c.materialReferenceIndex].vertices;
                    var vi = c.vertexIndex;

                    var centerX = (c.bottomLeft.x + c.topRight.x) * 0.5f;
                    var centerY = (c.baseLine + c.ascender) * 0.5f;
                    var t = Mathf.Clamp01((centerX - bounds.min.x) / width);
                    var offset = curve.Evaluate(t) * height;

                    var angle = 0f;
                    if (rotateLetters)
                    {
                        var dt = 0.01f;
                        var slope = (curve.Evaluate(Mathf.Clamp01(t + dt)) - curve.Evaluate(Mathf.Clamp01(t - dt))) * height / (2f * dt * width);
                        angle = Mathf.Atan(slope) * Mathf.Rad2Deg;
                    }
                    var rotation = Quaternion.Euler(0f, 0f, angle);
                    var center = new Vector3(centerX, centerY, 0f);
                    for (var k = 0; k < 4; k++)
                    {
                        var v = vertices[vi + k];
                        v = rotation * (v - center) + center;
                        v.y += offset;
                        vertices[vi + k] = v;
                    }
                }

                for (var i = 0; i < info.meshInfo.Length; i++)
                {
                    info.meshInfo[i].mesh.vertices = info.meshInfo[i].vertices;
                    text.UpdateGeometry(info.meshInfo[i].mesh, i);
                }
            }
            finally { applying = false; }
        }
    }
}
