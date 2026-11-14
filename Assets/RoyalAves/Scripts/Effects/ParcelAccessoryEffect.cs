using UnityEngine;

namespace RoyalAves.Effects
{
    // The whole plank detaches while loose splinters scatter around it.
    public sealed class ParcelAccessoryEffect : MonoBehaviour
    {
        public static void Play(SpriteRenderer source, int oldState)
        {
            if(Soft3DElementStyle.Has(source))
            {
                Soft3DElementEffect.Plank(source,oldState);
                Soft3DElementEffect.Burst(source,new Color(.78f,.46f,.22f),5,.5f);
                return;
            }
            FallingBoardDebris.ShatterWood(source, oldState);
            FallingBoardDebris.DropCratePlank(source, oldState);
            ParcelSparkEffect.Play(source, false);
        }
    }
}
