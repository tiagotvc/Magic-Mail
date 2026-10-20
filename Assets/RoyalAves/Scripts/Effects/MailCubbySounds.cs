using SweetSugar.Scripts;
using UnityEngine;

namespace RoyalAves.Effects
{
    // Own cues for the mail cabinet. Same approach as ParcelSoundEffects: the game's own source and
    // mixer are reused, so the effects volume and the mute setting keep working.
    public static class MailCubbySounds
    {
        const string DoorsClip = "Audio/MailCubby/cubby_doors";
        const float DoorsVolume = 0.55f;
        static AudioClip doors;
        static float lastDoors = float.NegativeInfinity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => lastDoors = float.NegativeInfinity;

        // Wood breaking: the two doors tearing off on the first accepted impact.
        public static void PlayDoors()
        {
            var sound = SoundBase.Instance;
            if (sound == null) return;
            // Several cells of the same cabinet can be hit in one activation; that is a single cue.
            if (Time.unscaledTime - lastDoors < 0.06f) return;
            if (doors == null) doors = Resources.Load<AudioClip>(DoorsClip);
            if (doors == null) return;
            var source = sound.GetComponent<AudioSource>();
            if (source == null) return;
            lastDoors = Time.unscaledTime;
            source.PlayOneShot(doors, DoorsVolume * Mathf.Clamp01(ParcelSoundEffects.Volume));
        }
    }
}
