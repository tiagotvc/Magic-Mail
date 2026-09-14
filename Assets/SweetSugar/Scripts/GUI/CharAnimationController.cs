using UnityEngine;

namespace SweetSugar.Scripts.GUI
{
    public class CharAnimationController : MonoBehaviour
    {
        public Animator animator;
        public string[] animationNames;
        public bool playOnStart;
        public int selectedAnimationIndex;

        private void Start()
        {
            if (playOnStart && animationNames.Length > 0 && selectedAnimationIndex < animationNames.Length)
            {
                PlaySelectedAnimation();
            }
        }

        public void PlaySelectedAnimation()
        {
            if (selectedAnimationIndex >= 0 && selectedAnimationIndex < animationNames.Length)
            {
                animator.Play(animationNames[selectedAnimationIndex]);
            }
        }

        public void PlayAnimationByIndex(int index)
        {
            if (index >= 0 && index < animationNames.Length)
            {
                animator.Play(animationNames[index]);
            }
        }

        public void PlayAnimation(string animationName)
        {
            animator.Play(animationName);
        }

        public void PlayAnimationByIndex(int layerIndex, int stateHash)
        {
            animator.Play(stateHash, layerIndex);
        }

        public void SetTrigger(string triggerName)
        {
            animator.SetTrigger(triggerName);
        }

        public void SetBool(string paramName, bool value)
        {
            animator.SetBool(paramName, value);
        }

        public void SetFloat(string paramName, float value)
        {
            animator.SetFloat(paramName, value);
        }

        public void SetInt(string paramName, int value)
        {
            animator.SetInteger(paramName, value);
        }
    }
}
