using System.Collections;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    public sealed class FrankingMachineLever : MonoBehaviour
    {
        public Transform lever;
        public bool IsLowered { get; private set; }
        public IEnumerator Pull()
        {
            yield return Turn(-28f, -100f, 0.12f);
            IsLowered = true;
            yield return new WaitForSeconds(0.025f);
        }
        public IEnumerator Return()
        {
            yield return Turn(-100f, -28f, 0.13f);
            IsLowered = false;
        }
        IEnumerator Turn(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                lever.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(from, to, t));
                yield return null;
            }
            lever.localRotation = Quaternion.Euler(0, 0, to);
        }
    }
}
