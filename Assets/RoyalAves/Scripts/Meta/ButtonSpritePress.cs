// Press feedback for a button: shrinks a chosen set of objects (and whatever children each one has) a touch
// on press and springs them back on release/exit — no separate "pressed" art needed. Lists its targets
// explicitly instead of just scaling the whole button, because things like a border/frame sibling often
// need to stay put while only the coloured face + its label/icon recede.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RoyalAves.Meta
{
    public class ButtonSpritePress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Tooltip("O que encolhe ao pressionar (com os filhos de cada um — ex.: o \"bg\" verde já carrega texto/ícone se eles forem filhos dele). Deixe vazio pra encolher este próprio objeto.")]
        [SerializeField] Transform[] targets;
        [Tooltip("Escala ao pressionar, relativa ao tamanho normal (1 = sem efeito).")]
        [SerializeField] float pressedScale = 0.94f;
        [Tooltip("Duração da animação de encolher/voltar, em segundos.")]
        [SerializeField] float duration = 0.08f;

        readonly List<Vector3> normalScales = new List<Vector3>();
        float pressAmount; // 0 = tamanho normal, 1 = totalmente pressionado
        Coroutine animation;

        void Awake()
        {
            if (targets == null || targets.Length == 0) targets = new[] { transform };
            normalScales.Clear();
            foreach (var t in targets) normalScales.Add(t != null ? t.localScale : Vector3.one);
        }

        void OnDisable()
        {
            if (animation != null) StopCoroutine(animation);
            animation = null;
            pressAmount = 0f;
            Apply();
        }

        public void OnPointerDown(PointerEventData eventData) => AnimateTo(1f);
        public void OnPointerUp(PointerEventData eventData) => AnimateTo(0f);
        public void OnPointerExit(PointerEventData eventData) => AnimateTo(0f);

        void AnimateTo(float target)
        {
            if (!isActiveAndEnabled) return;
            if (animation != null) StopCoroutine(animation);
            animation = StartCoroutine(Animate(target));
        }

        IEnumerator Animate(float target)
        {
            var start = pressAmount;
            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                pressAmount = Mathf.Lerp(start, target, duration > 0f ? t / duration : 1f);
                Apply();
                yield return null;
            }
            pressAmount = target;
            Apply();
            animation = null;
        }

        void Apply()
        {
            for (var i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;
                targets[i].localScale = Vector3.Lerp(normalScales[i], normalScales[i] * pressedScale, pressAmount);
            }
        }
    }
}
