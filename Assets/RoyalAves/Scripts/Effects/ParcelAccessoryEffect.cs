using UnityEngine;

namespace RoyalAves.Effects
{
    // The whole plank detaches while loose splinters scatter around it.
    public sealed class ParcelAccessoryEffect : MonoBehaviour
    {
        public static void Play(SpriteRenderer source, int oldState)
        {
            FallingBoardDebris.ShatterWood(source, oldState);
            FallingBoardDebris.DropCratePlank(source, oldState);
            ParcelSoundEffects.Play(oldState);
            ParcelSparkEffect.Play(source, false);
        }
    }
}
