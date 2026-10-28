using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>Portrait and frame can be replaced independently by the profile system.</summary>
    public sealed class RoyalPostmanAvatar : MonoBehaviour
    {
        public Image portrait;
        public Image frame;
        public void SetPortrait(Sprite sprite) { if (portrait != null) portrait.sprite = sprite; }
        public void SetFrame(Sprite sprite) { if (frame != null) frame.sprite = sprite; }
    }
}
