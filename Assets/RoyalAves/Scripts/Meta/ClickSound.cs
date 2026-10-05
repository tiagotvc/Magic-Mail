// The standard UI click sound (SoundBase.click), for a Button's OnClick alongside its real action.
// Sweet Sugar normally plays this from an Animator's "Pressed" state (StateMachineBehaviour ButtonClickSound), but a
// button whose Animator was removed (because it kept fighting a custom size the designer set on it) loses that sound
// along with it. This gives such a button the same sound back through its OnClick instead.
using SweetSugar.Scripts;
using UnityEngine;

namespace RoyalAves.Meta
{
    public class ClickSound : MonoBehaviour
    {
        public void Play()
        {
            if (SoundBase.Instance != null) SoundBase.Instance.PlayOneShot(SoundBase.Instance.click);
        }
    }
}
