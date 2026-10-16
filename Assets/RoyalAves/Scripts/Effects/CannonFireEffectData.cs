using UnityEngine;

namespace RoyalAves.Effects
{
    // Art the cannon boost's fire effect needs at runtime, loaded once via
    // Resources.Load("Effects/CannonFireEffectData") since the effect is built entirely in code (see CannonFireEffect).
    // Textures, not Sprites - see the same note on RocketSplitEffectData: a texture's main asset fileID is always the
    // fixed 2800000, while a Sprite sub-asset's fileID is a hash only trustworthy after a real Unity import.
    [CreateAssetMenu(menuName = "Royal Aves/Cannon Fire Effect Data")]
    public class CannonFireEffectData : ScriptableObject
    {
        // Top row of the source sheet: cannon at rest, 4 near-identical frames (subtle idle sway).
        public Texture2D[] IdleFrames;
        // Bottom row of the source sheet: muzzle flash + smoke, in firing order.
        public Texture2D[] FireFrames;
    }
}
