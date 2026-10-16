using UnityEngine;

namespace RoyalAves.Effects
{
    public static class GrassFeedbackSettings
    {
        static float speed = 1f;
        static float dispersion = 1f;
        public static float Speed { get => speed; set => speed = Mathf.Clamp(value, 0.5f, 1.5f); }
        public static float Dispersion { get => dispersion; set => dispersion = Mathf.Clamp(value, 0.5f, 1.25f); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { speed = dispersion = 1f; }
    }
}
