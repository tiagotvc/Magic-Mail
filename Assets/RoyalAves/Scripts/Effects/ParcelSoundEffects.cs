using SweetSugar.Scripts;
using UnityEngine;

namespace RoyalAves.Effects
{
    public static class ParcelSoundEffects
    {
        static readonly string[] Names = { "parcel_complete", "tape_horizontal", "tape_vertical", "seal_pop" };
        static readonly float[] Volumes = { 0.55f, 0.42f, 0.42f, 0.50f };
        static readonly AudioClip[] Clips = new AudioClip[4];
        static readonly float[] LastPlayed = { float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity };
        public static float Volume { get; set; } = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Volume = 1f;
            for (int i = 0; i < LastPlayed.Length; i++) LastPlayed[i] = float.NegativeInfinity;
        }

        public static void Play(int state)
        {
            var sound = SoundBase.Instance;
            if (sound == null) return;
            int index = Mathf.Clamp(state - 1, 0, 3);
            // A simultaneous group shares a cue; sequential hits and different cues still play.
            if (Time.unscaledTime - LastPlayed[index] < 0.06f) return;
            if (Clips[index] == null) Clips[index] = Resources.Load<AudioClip>("Audio/Parcels/" + Names[index]);
            if (Clips[index] == null) return;
            var source = sound.GetComponent<AudioSource>();
            if (source == null) return;
            LastPlayed[index] = Time.unscaledTime;
            // Reuse the game's source and mixer so its effects-volume and mute settings apply.
            source.PlayOneShot(Clips[index], Volumes[index] * Mathf.Clamp01(Volume));
        }
    }
}
