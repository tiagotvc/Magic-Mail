// A pressed look built from separate "normal" and "pressed" GameObjects (same pattern as the broken/fixed
// clock): each pair toggles which one is active, instead of moving/scaling (ButtonPressEffect) or swapping
// a sprite on one Image. Add one pair per thing that changes look — the icon, a label, etc. Swaps back the
// moment the pointer lifts or leaves.
using UnityEngine;
using UnityEngine.EventSystems;

namespace RoyalAves.Meta
{
    public class ButtonSpritePress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [System.Serializable]
        public class Pair
        {
            public GameObject normal;
            public GameObject pressed;
        }

        [Tooltip("Um par por elemento que troca de cara (o ícone, o texto, ...).")]
        [SerializeField] Pair[] pairs;

        void Awake() => SetPressed(false);

        public void OnPointerDown(PointerEventData eventData) => SetPressed(true);
        public void OnPointerUp(PointerEventData eventData) => SetPressed(false);
        public void OnPointerExit(PointerEventData eventData) => SetPressed(false);

        void SetPressed(bool pressed)
        {
            foreach (var pair in pairs)
            {
                if (pair.normal != null) pair.normal.SetActive(!pressed);
                if (pair.pressed != null) pair.pressed.SetActive(pressed);
            }
        }
    }
}
