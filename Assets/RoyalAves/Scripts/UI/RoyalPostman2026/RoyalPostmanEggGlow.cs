using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>The same separate light sprite and pulse are used for every rarity.</summary>
    public sealed class RoyalPostmanEggGlow : MonoBehaviour
    {
        public Image[] lights;
        public Color[] palette;
        [Range(0, 1)] public float intensity = .6f;
        [Min(0)] public float pulseSpeed = 1.4f;
        void OnEnable() => Apply(0);
        void Update() { if(Application.isPlaying) Apply(Time.unscaledTime); }
        public void Apply(float time)
        {
            float alpha=intensity*(.92f+.08f*Mathf.Sin(time*pulseSpeed));
            for(int i=0;lights!=null&&i<lights.Length;i++)if(lights[i]!=null)
            {
                var c=palette!=null&&i<palette.Length?palette[i]:Color.white;
                c.a*=alpha;lights[i].color=c;
            }
        }
    }
}
