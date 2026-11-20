// Feedback de clique para botões: o "miolo" do botão (Target) afunda ao pressionar e volta ao soltar. Se o toque for
// rápido demais para ver, o encolhimento ainda dura o mínimo configurado, então todo clique "afunda e volta".
// Serve também para o botão de fechar.
//
// O miolo perde a MESMA folga em cima, embaixo e dos lados (em pixels), não a mesma porcentagem: num botão comprido,
// 15% de cada eixo deixaria as laterais bem mais largas que o topo. A folga é a de "15%" do lado menor, dividida
// pelos dois lados. Os filhos (ícone, texto) recebem a escala inversa da deformação, então não esticam: só
// encolhem por igual, como no miolo quadrado.
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RoyalAves.Meta
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Royal Aves/Button Pop Press")]
    public class ButtonPopPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Tooltip("O que encolhe (o centro do botão, com os filhos). Vazio = este próprio objeto. A moldura/borda que deve ficar parada NÃO entra aqui.")]
        [SerializeField] RectTransform target;
        [Tooltip("Quanto o lado menor encolhe ao pressionar. 0.85 = 15%. A folga resultante é igual nos quatro lados.")]
        [SerializeField, Range(0.5f, 1f)] float pressedScale = 0.85f;
        [Tooltip("Segundos para encolher e para voltar.")]
        [SerializeField] float duration = 0.06f;
        [Tooltip("Tempo mínimo encolhido, para um toque rápido ainda ser visível.")]
        [SerializeField] float minimumHold = 0.05f;

        Vector3 normalScale = Vector3.one;
        Vector3[] childScales;
        bool captured, pressed;
        float pressedAt;
        float amount;   // 0 = normal, 1 = totalmente apertado
        Coroutine routine;

        Transform T => target != null ? target : transform;

        void Capture()
        {
            if (captured) return;
            normalScale = T.localScale;
            childScales = new Vector3[T.childCount];
            for (var i = 0; i < childScales.Length; i++) childScales[i] = T.GetChild(i).localScale;
            captured = true;
        }

        void Awake() => Capture();

        void OnDisable()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            pressed = false;
            if (captured) { amount = 0f; Apply(0f); }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Capture();
            pressed = true;
            pressedAt = Time.unscaledTime;
            Go(true);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        void Release()
        {
            if (!pressed) return;
            pressed = false;
            Go(false);
        }

        void Go(bool down)
        {
            if (!isActiveAndEnabled) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Animate(down));
        }

        IEnumerator Animate(bool down)
        {
            if (!down)
            {
                var wait = minimumHold - (Time.unscaledTime - pressedAt);
                while (wait > 0f) { wait -= Time.unscaledDeltaTime; yield return null; }
            }
            var from = amount;
            var to = down ? 1f : 0f;
            for (var t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                amount = Mathf.Lerp(from, to, t / duration);
                Apply(amount);
                yield return null;
            }
            amount = to;
            Apply(to);
            routine = null;
        }

        void Apply(float k)
        {
            var scaleX = 1f; var scaleY = 1f;
            if (T is RectTransform rect && rect.rect.width > 1f && rect.rect.height > 1f)
            {
                var w = rect.rect.width; var h = rect.rect.height;
                var inset = (1f - pressedScale) * 0.5f * Mathf.Min(w, h);   // folga igual nos quatro lados
                scaleX = Mathf.Lerp(1f, (w - 2f * inset) / w, k);
                scaleY = Mathf.Lerp(1f, (h - 2f * inset) / h, k);
            }
            else scaleX = scaleY = Mathf.Lerp(1f, pressedScale, k);

            T.localScale = new Vector3(normalScale.x * scaleX, normalScale.y * scaleY, normalScale.z);

            // Os filhos não esticam junto: ficam com a escala uniforme do eixo que mais encolheu.
            var uniform = Mathf.Min(scaleX, scaleY);
            for (var i = 0; i < childScales.Length && i < T.childCount; i++)
            {
                var c = childScales[i];
                T.GetChild(i).localScale = new Vector3(c.x * uniform / scaleX, c.y * uniform / scaleY, c.z);
            }
        }
    }
}
