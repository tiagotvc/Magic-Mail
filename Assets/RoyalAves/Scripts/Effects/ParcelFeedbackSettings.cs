using UnityEngine;

namespace RoyalAves.Effects
{
    // Live controls exposed only by the editor test panel; production starts at the default values.
    public static class ParcelFeedbackSettings
    {
        static float speed = 1f;
        static float intensity = 1f;
        public static float Speed { get => speed; set => speed = Mathf.Clamp(value, 0.5f, 1.5f); }
        public static float Intensity { get => intensity; set => intensity = Mathf.Clamp(value, 0.5f, 1.25f); }
        public static void Reset() { speed = intensity = 1f; }
    }
}
