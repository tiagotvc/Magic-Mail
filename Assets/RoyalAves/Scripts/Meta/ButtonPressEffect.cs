// Classic "chunky 3D button" press feedback: nudges a sprite down and shrinks it slightly while
// held, springing back on release — for buttons like "Nível 1" that already have their own baked-in
// bottom shadow/lip drawn into the sprite, so sinking into it on press reads as a real press.
// The click itself must be detected on the object that actually has the Button/Raycast Target (so
// this can sit on that object), but the piece that visually moves is often a child (the face sprite,
// not the whole button incl. its fixed frame) — set "Target" to that child; leave it empty to just
// animate this object itself.
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RoyalAves.Meta
{
    public class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Tooltip("O que visualmente desce/encolhe. Vazio = este próprio objeto.")]
        [SerializeField] RectTransform target;
        [Tooltip("Quanto desce quando pressionado, em pixels — mais ou menos a altura do relevo/sombra do botão.")]
        [SerializeField] float pressOffset = 8f;
        [Tooltip("Quanto encolhe quando pressionado (1 = tamanho normal, sem encolher).")]
        [SerializeField, Range(0.8f, 1f)] float pressScale = 0.97f;
        [Tooltip("Segundos pra descer/subir.")]
        [SerializeField] float duration = 0.06f;

        Vector2 upPos;
        Vector3 upScale;
        bool pressed;
        Coroutine anim;

        void Awake()
        {
            if (target == null) target = (RectTransform)transform;
            upPos = target.anchoredPosition;
            upScale = target.localScale;
        }

        public void OnPointerDown(PointerEventData eventData) => SetPressed(true);
        public void OnPointerUp(PointerEventData eventData) => SetPressed(false);
        public void OnPointerExit(PointerEventData eventData) { if (pressed) SetPressed(false); }

        void SetPressed(bool value)
        {
            if (pressed == value) return;
            pressed = value;
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(Animate(value));
        }

        IEnumerator Animate(bool down)
        {
            var fromPos = target.anchoredPosition;
            var fromScale = target.localScale;
            var toPos = down ? upPos + new Vector2(0f, -pressOffset) : upPos;
            var toScale = down ? upScale * pressScale : upScale;
            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var f = Mathf.Clamp01(t / duration);
                target.anchoredPosition = Vector2.Lerp(fromPos, toPos, f);
                target.localScale = Vector3.Lerp(fromScale, toScale, f);
                yield return null;
            }
            target.anchoredPosition = toPos;
            target.localScale = toScale;
        }
    }
}
