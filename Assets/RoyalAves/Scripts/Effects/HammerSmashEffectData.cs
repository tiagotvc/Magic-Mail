using UnityEngine;

namespace RoyalAves.Effects
{
    // Art the martelo boost's smash effect needs at runtime, loaded once via
    // Resources.Load("Effects/HammerSmashEffectData") since the effect is built entirely in code (see HammerSmashEffect).
    // Textures, not Sprites - see the same note on RocketSplitEffectData/CannonFireEffectData: a texture's main asset
    // fileID is always the fixed 2800000, while a Sprite sub-asset's fileID is a hash only trustworthy after a real
    // Unity import.
    [CreateAssetMenu(menuName = "Royal Aves/Hammer Smash Effect Data")]
    public class HammerSmashEffectData : ScriptableObject
    {
        // Top row of the source sheet: hammer held up, 3 near-identical frames (idle sway before it swings).
        public Texture2D[] IdleFrames;
        // Bottom row of the source sheet, in play order: wind-up spin x2, impact flash, spark aftermath, settle
        // back to rest.
        public Texture2D[] SwingFrames;
    }
}
