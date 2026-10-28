using UnityEngine;

namespace RoyalAves.Characters
{
    // Deliberately has no Update: animation is controlled by clips or story events.
    public sealed class RibbonRookCutout : MonoBehaviour
    {
        public Transform leftArm, rightArm;
        public SpriteRenderer mouth;
        public Sprite closedMouth, openMouth;

        public void SetSpeaking(bool speaking)
        {
            if(mouth!=null)mouth.sprite=speaking?openMouth:closedMouth;
        }

        [ContextMenu("Preview/Open mouth")] void OpenMouth()=>SetSpeaking(true);
        [ContextMenu("Preview/Close mouth")] void CloseMouth()=>SetSpeaking(false);
    }
}
