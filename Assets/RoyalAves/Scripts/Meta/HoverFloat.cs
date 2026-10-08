// Idle "hover" for a UI element that should feel alive even without being touched (the Play button, the
// Tasks badge, ...): a gentle continuous up/down bob, optionally with a slight rotation sway. Loops
// forever on its own — no trigger needed, unlike the press/celebration effects.
using UnityEngine;

namespace RoyalAves.Meta
{
    public class HoverFloat : MonoBehaviour
    {
        [Tooltip("Altura do balanço, em pixels.")]
        [SerializeField] float amplitude = 10f;
        [Tooltip("Quantos ciclos completos por segundo.")]
        [SerializeField] float speed = 1f;
        [Tooltip("Graus de rotação de um lado a outro (0 = só sobe/desce, sem girar).")]
        [SerializeField] float rotationAmplitude = 0f;
        [Tooltip("Defasagem do ciclo (0-1) — usa valores diferentes em objetos vizinhos pra não balançarem igualzinho ao mesmo tempo.")]
        [SerializeField] float phase = 0f;

        RectTransform rect;
        Vector2 restPosition;
        float restRotationZ;

        void Awake()
        {
            rect = (RectTransform)transform;
            restPosition = rect.anchoredPosition;
            restRotationZ = rect.localEulerAngles.z;
        }

        void OnEnable() => rect.anchoredPosition = restPosition;

        void Update()
        {
            var wave = Mathf.Sin((Time.unscaledTime * speed + phase) * Mathf.PI * 2f);
            rect.anchoredPosition = restPosition + new Vector2(0f, wave * amplitude);
            if (rotationAmplitude != 0f) rect.localRotation = Quaternion.Euler(0, 0, restRotationZ + wave * rotationAmplitude);
        }

        void OnDisable()
        {
            if (rect == null) return;
            rect.anchoredPosition = restPosition;
            if (rotationAmplitude != 0f) rect.localRotation = Quaternion.Euler(0, 0, restRotationZ);
        }
    }
}
