using UnityEngine;

namespace RoyalAves.Effects
{
    // Art + material the rocket's split-and-fly effect needs at runtime, loaded once via
    // Resources.Load("Effects/RocketSplitEffectData") since the effect is built entirely in code (see RocketSplitEffect).
    [CreateAssetMenu(menuName = "Royal Aves/Rocket Split Effect Data")]
    public class RocketSplitEffectData : ScriptableObject
    {
        // Textures, not Sprites: a texture's main asset fileID is always the fixed 2800000, while the generated
        // Sprite sub-asset's fileID is a hash assigned at import time - hand-writing that hash into this .asset
        // without an actual Unity import has broken this reference twice already. RocketSplitEffect builds the
        // Sprite it needs from these at runtime instead.
        public Texture2D HalfLeft;
        public Texture2D HalfRight;
        public Texture2D EngineNozzle;
        public Material FlameMaterial;
    }
}
