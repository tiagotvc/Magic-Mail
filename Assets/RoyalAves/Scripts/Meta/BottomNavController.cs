// Bottom nav bar: the active-tab indicator (the yellow platform) slides sideways to sit under
// whichever tab was clicked, that tab's icon pops up and grows while its label shifts up with it,
// and the previously active tab eases back down to normal. Positions/scales are captured from each
// tab's own resting layout in Awake, so it works with however the tabs are already placed — no need
// to hardcode coordinates. The indicator must be a sibling of the tab buttons (same parent, e.g. all
// direct children of "Nav") since it slides by matching the target tab's own X position.
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public class BottomNavController : MonoBehaviour
    {
        [System.Serializable]
        public class Tab
        {
            public Button button;
            [Tooltip("O ícone que sobe e aumenta quando essa aba fica ativa.")]
            public RectTransform icon;
            [Tooltip("O texto que sobe junto (opcional).")]
            public RectTransform label;
        }

        [Tooltip("A plataforma amarela que fica embaixo da aba ativa. Precisa ser irmã dos botões das abas.")]
        [SerializeField] RectTransform indicator;
        [Tooltip("Ajuste fino: desloca o indicator pra direita (+) ou esquerda (-), em pixels de tela.")]
        [SerializeField] float indicatorOffsetX = 0f;
        [SerializeField] Tab[] tabs;
        [SerializeField] int startIndex;

        [Header("Aba ativa")]
        [Tooltip("Quanto o ícone sobe, em pixels.")]
        [SerializeField] float iconLift = 20f;
        [Tooltip("Quanto o ícone aumenta (1 = tamanho normal).")]
        [SerializeField] float iconScale = 1.25f;
        [Tooltip("Quanto o texto sobe, em pixels.")]
        [SerializeField] float labelLift = 8f;
        [SerializeField] float duration = 0.25f;

        Vector2[] iconRest, labelRest;
        Vector3[] iconRestScale;
        int active = -1;
        Coroutine indicatorRoutine;
        Coroutine[] tabRoutines;

        void Awake()
        {
            var n = tabs.Length;
            iconRest = new Vector2[n];
            labelRest = new Vector2[n];
            iconRestScale = new Vector3[n];
            tabRoutines = new Coroutine[n];
            for (var i = 0; i < n; i++)
            {
                if (tabs[i].icon != null) { iconRest[i] = tabs[i].icon.anchoredPosition; iconRestScale[i] = tabs[i].icon.localScale; }
                if (tabs[i].label != null) labelRest[i] = tabs[i].label.anchoredPosition;
                var captured = i;
                if (tabs[i].button != null)
                {
                    // O Color Tint padrão do Button sobrescreve o gráfico (sprite/alpha) pela Normal Color
                    // branca opaca assim que o jogo roda — o feedback visual já é todo feito por este script.
                    tabs[i].button.transition = Selectable.Transition.None;
                    // Esconde qualquer fundo estático do botão (inclusive o sprite "nav-active" sobrando do
                    // sistema antigo, que aparece branco se o sprite não carrega) — o indicator + ícone/label
                    // já fazem 100% do destaque visual da aba ativa.
                    if (tabs[i].button.targetGraphic != null) tabs[i].button.targetGraphic.color = new Color(1, 1, 1, 0);
                    tabs[i].button.onClick.AddListener(() => Select(captured));
                }
            }
            Select(startIndex, true);
        }

        public void Select(int index) => Select(index, false);

        void Select(int index, bool instant)
        {
            // Evita o highlight branco de "Selected" do Button ficando preso no botão clicado.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            if (index < 0 || index >= tabs.Length || (index == active && !instant)) return;
            var previous = active;
            active = index;
            if (previous >= 0 && previous != index) AnimateTab(previous, false, instant);
            AnimateTab(index, true, instant);
            AnimateIndicator(index, instant);
        }

        void AnimateIndicator(int index, bool instant)
        {
            if (indicator == null || tabs[index].button == null) return;
            // Posição mundial em vez de anchoredPosition: robusto mesmo se cada aba tiver seu próprio anchor
            // (não dá pra assumir que anchoredPosition.x reflete a posição visual real na tela).
            var targetX = tabs[index].button.transform.position.x + indicatorOffsetX;
            if (indicatorRoutine != null) StopCoroutine(indicatorRoutine);
            if (instant)
            {
                var p = indicator.position;
                p.x = targetX;
                indicator.position = p;
            }
            else indicatorRoutine = StartCoroutine(MoveX(indicator, targetX, duration));
        }

        void AnimateTab(int index, bool toActive, bool instant)
        {
            var tab = tabs[index];
            if (tabRoutines[index] != null) StopCoroutine(tabRoutines[index]);
            var iconTargetPos = toActive ? iconRest[index] + new Vector2(0f, iconLift) : iconRest[index];
            var iconTargetScale = toActive ? iconRestScale[index] * iconScale : iconRestScale[index];
            var labelTargetPos = toActive ? labelRest[index] + new Vector2(0f, labelLift) : labelRest[index];
            if (instant)
            {
                if (tab.icon != null) { tab.icon.anchoredPosition = iconTargetPos; tab.icon.localScale = iconTargetScale; }
                if (tab.label != null) tab.label.anchoredPosition = labelTargetPos;
                return;
            }
            tabRoutines[index] = StartCoroutine(AnimateTabRoutine(tab, iconTargetPos, iconTargetScale, labelTargetPos));
        }

        IEnumerator AnimateTabRoutine(Tab tab, Vector2 iconPos, Vector3 iconScl, Vector2 labelPos)
        {
            var iconFromPos = tab.icon != null ? tab.icon.anchoredPosition : Vector2.zero;
            var iconFromScale = tab.icon != null ? tab.icon.localScale : Vector3.one;
            var labelFromPos = tab.label != null ? tab.label.anchoredPosition : Vector2.zero;
            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var f = EaseOut(Mathf.Clamp01(t / duration));
                if (tab.icon != null)
                {
                    tab.icon.anchoredPosition = Vector2.Lerp(iconFromPos, iconPos, f);
                    tab.icon.localScale = Vector3.Lerp(iconFromScale, iconScl, f);
                }
                if (tab.label != null) tab.label.anchoredPosition = Vector2.Lerp(labelFromPos, labelPos, f);
                yield return null;
            }
            if (tab.icon != null) { tab.icon.anchoredPosition = iconPos; tab.icon.localScale = iconScl; }
            if (tab.label != null) tab.label.anchoredPosition = labelPos;
        }

        IEnumerator MoveX(RectTransform rt, float targetX, float dur)
        {
            var from = rt.position;
            var to = new Vector3(targetX, from.y, from.z);
            var t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                var f = EaseOut(Mathf.Clamp01(t / dur));
                rt.position = Vector3.Lerp(from, to, f);
                yield return null;
            }
            rt.position = to;
        }

        static float EaseOut(float f) => 1f - (1f - f) * (1f - f);
    }
}
