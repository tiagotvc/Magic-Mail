using UnityEngine;

namespace RoyalAves.Effects
{
    // Art + material the rocket's split-and-fly effect needs at runtime, loaded once via
    // Resources.Load("Effects/RocketSplitEffectData") since the effect is built entirely in code (see RocketSplitEffect).
    [CreateAssetMenu(menuName = "Royal Aves/Rocket Split Effect Data")]
    public class RocketSplitEffectData : ScriptableObject
    {
        public Sprite HalfLeft;
        public Sprite HalfRight;
        public Material FlameMaterial;
    }
}
