// Escreve um texto TextMeshPro sobre um arco de círculo (faixa de título). Põe no mesmo objeto do texto.
// Height > 0: o meio sobe e as pontas descem (sorriso invertido); Height < 0: o contrário ("Mais vidas!").
// Height é a diferença de altura entre o meio e as pontas, em unidades do canvas.
//
// Cada letra fica em pé sobre o arco: a linha de base de todas as letras cai exatamente no círculo e a letra gira
// para apontar para fora dele (Rotate Letters). Letras altas ("L") ou com cauda ("g") não mudam de altura entre si,
// porque o encaixe usa a linha de base, não o centro de cada letra.
using TMPro;
using UnityEngine;

namespace RoyalAves.Meta
{
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("Royal Aves/Text Arc")]
    public class TextArc : MonoBehaviour
    {
        [Tooltip("Não é mais usado: o arco agora é um círculo de verdade. Fica só para não quebrar prefabs antigos.")]
        public AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));
        [Tooltip("Quanto o meio do texto fica mais alto que as pontas, em unidades do canvas. Negativo inverte.")]
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
                var middle = bounds.center.x;
                var sagitta = Mathf.Abs(height);
                if (sagitta < 0.01f) return;

                // Círculo que passa pelas duas pontas do texto e sobe "sagitta" no meio.
                var half = width * 0.5f;
                var radius = (half * half + sagitta * sagitta) / (2f * sagitta);
                var sign = Mathf.Sign(height);
                var endAngle = Mathf.Asin(Mathf.Clamp(half / radius, -1f, 1f));
                var endDrop = Mathf.Cos(endAngle);

                for (var i = 0; i < info.characterCount; i++)
                {
                    var c = info.characterInfo[i];
                    if (!c.isVisible) continue;
                    var vertices = info.meshInfo[c.materialReferenceIndex].vertices;
                    var vi = c.vertexIndex;

                    var centerX = (c.bottomLeft.x + c.topRight.x) * 0.5f;
                    var baseline = c.baseLine;
                    var phi = Mathf.Clamp((centerX - middle) / radius, -1.5f, 1.5f);   // ângulo no círculo

                    // Onde o pé da letra (centro da base) vai parar no arco.
                    var targetX = middle + radius * Mathf.Sin(phi);
                    var targetY = baseline + sign * radius * (Mathf.Cos(phi) - endDrop);
                    var rotation = rotateLetters ? Quaternion.Euler(0f, 0f, -sign * phi * Mathf.Rad2Deg) : Quaternion.identity;
                    var foot = new Vector3(centerX, baseline, 0f);
                    var target = new Vector3(targetX, targetY, 0f);

                    for (var k = 0; k < 4; k++)
                        vertices[vi + k] = target + rotation * (vertices[vi + k] - foot);
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
