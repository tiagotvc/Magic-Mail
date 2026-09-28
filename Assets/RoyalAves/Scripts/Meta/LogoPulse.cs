// A slow, light pulse for the title logo.
using UnityEngine;

namespace RoyalAves.Meta
{
    public class LogoPulse : MonoBehaviour
    {
        [Tooltip("Quanto o logo cresce no pulso (0,04 = 4%).")]
        [SerializeField, Range(0f, 0.2f)] float amount = 0.04f;
        [Tooltip("Duração de um pulso, em segundos.")]
        [SerializeField, Min(0.1f)] float seconds = 1.8f;

        Vector3 baseScale;

        void Awake() => baseScale = transform.localScale;

        void OnDisable() => transform.localScale = baseScale;

        void Update()
        {
            var wave = 0.5f - 0.5f * Mathf.Cos(Time.unscaledTime * 2 * Mathf.PI / seconds);
            transform.localScale = baseScale * (1 + amount * wave);
        }
    }
}
