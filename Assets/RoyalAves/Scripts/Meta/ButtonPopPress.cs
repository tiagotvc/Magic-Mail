// Feedback de clique para botões: o "miolo" do botão (Target) encolhe 15% por igual em todos os lados ao
// pressionar e volta ao tamanho normal ao soltar. Se o toque for rápido demais para ver, o encolhimento
// ainda dura o mínimo configurado, então todo clique "afunda e volta". Serve também para o botão de fechar.
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
        [Tooltip("Escala ao pressionar. 0.85 = encolhe 15%, igual em X e Y (o centro do botão fica parado).")]
        [SerializeField, Range(0.5f, 1f)] float pressedScale = 0.85f;
        [Tooltip("Segundos para encolher e para voltar.")]
        [SerializeField] float duration = 0.06f;
        [Tooltip("Tempo mínimo encolhido, para um toque rápido ainda ser visível.")]
        [SerializeField] float minimumHold = 0.05f;

        Vector3 normalScale = Vector3.one;
        bool captured, pressed;
        float pressedAt;
        Coroutine routine;

        Transform T => target != null ? target : transform;

        void Capture()
        {
            if (captured) return;
            normalScale = T.localScale;
            captured = true;
        }

        void Awake() => Capture();

        void OnDisable()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            pressed = false;
            if (captured) T.localScale = normalScale;
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
            var from = T.localScale;
            var to = down ? normalScale * pressedScale : normalScale;
            for (var t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                T.localScale = Vector3.Lerp(from, to, t / duration);
                yield return null;
            }
            T.localScale = to;
            routine = null;
        }
    }
}
