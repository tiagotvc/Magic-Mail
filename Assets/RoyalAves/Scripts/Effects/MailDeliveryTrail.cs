using System.Collections;
using UnityEngine;

namespace RoyalAves.Effects
{
    // A short warm ribbon follows the physical flight; only the wake fades, not the letter.
    public sealed class MailDeliveryTrail : MonoBehaviour
    {
        TrailRenderer trail;
        Material material;
        public static MailDeliveryTrail Create(SpriteRenderer letter)
        {
            var root = new GameObject("MailDeliveryWake");
            root.transform.SetParent(letter.transform.parent, true);
            root.transform.position = letter.transform.position;
            var effect = root.AddComponent<MailDeliveryTrail>();
            effect.trail = root.AddComponent<TrailRenderer>();
            effect.material = new Material(letter.sharedMaterial);
            effect.material.SetTexture("_MainTex", Texture2D.whiteTexture);
            effect.trail.sharedMaterial = effect.material;
            effect.trail.time = 0.24f;
            effect.trail.minVertexDistance = 0.025f;
            effect.trail.widthMultiplier = 0.09f;
            effect.trail.widthCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            effect.trail.colorGradient = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(new Color(1f, 0.96f, 0.75f), 0f), new GradientColorKey(new Color(1f, 0.69f, 0.2f), 1f) },
                alphaKeys = new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) }
            };
            effect.trail.numCapVertices = 4;
            effect.trail.numCornerVertices = 3;
            effect.trail.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            effect.trail.sortingOrder = PowerUpEffectOrder.Value + 7;
            effect.trail.Clear();
            return effect;
        }

        public void Follow(Vector3 position) => transform.position = position;
        public void Finish() { trail.emitting = false; StartCoroutine(FadeWake()); }
        IEnumerator FadeWake() { yield return new WaitForSeconds(0.26f); Destroy(gameObject); }
        void OnDestroy() { if (material != null) Destroy(material); }
    }
}
