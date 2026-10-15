using System;
using System.Collections;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.System;
using UnityEngine;

namespace RoyalAves.Effects
{
    public sealed class ParcelTearEffect : MonoBehaviour
    {
        public const float Duration = 0.42f;
        const float TensionDuration = 0.08f;
        LevelManager manager;
        AnimateItems barrier;
        Action onComplete;
        SpriteRenderer visual;
        Quaternion restRotation;

        public static void Play(SpriteRenderer source, LevelManager manager, Action onComplete)
        {
            var root = new GameObject("ParcelCompletionFX");
            root.transform.SetParent(source.transform.parent, false);
            root.transform.localPosition = source.transform.localPosition;
            root.transform.localRotation = source.transform.localRotation;
            root.transform.localScale = source.transform.localScale;
            root.AddComponent<ParcelTearEffect>().Initialize(source, manager, onComplete);
        }

        void Initialize(SpriteRenderer source, LevelManager owner, Action callback)
        {
            manager = owner;
            onComplete = callback;
            restRotation = transform.localRotation;
            // Keep the short completion barrier, independent of decorative falling wood.
            barrier = gameObject.AddComponent<AnimateItems>();
            barrier.target = true;
            barrier.linkObjectHash = source.transform.parent.gameObject.GetHashCode();
            manager.animateItems.Add(barrier);
            visual = gameObject.AddComponent<SpriteRenderer>();
            visual.sprite = source.sprite;
            visual.sharedMaterial = source.sharedMaterial;
            visual.color = source.color;
            visual.flipX = source.flipX;
            visual.flipY = source.flipY;
            visual.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            visual.sortingOrder = PowerUpEffectOrder.Value;
            StartCoroutine(Animate());
        }

        IEnumerator Animate()
        {
            float elapsed = 0f;
            bool burstStarted = false;
            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime * ParcelFeedbackSettings.Speed;
                if (elapsed < TensionDuration)
                {
                    float shake = Mathf.Sin(elapsed / TensionDuration * Mathf.PI * 4f);
                    transform.localRotation = restRotation * Quaternion.Euler(0f, 0f,
                        shake * 1.6f * ParcelFeedbackSettings.Intensity);
                }
                else if (!burstStarted)
                {
                    burstStarted = true;
                    transform.localRotation = restRotation;
                    FallingBoardDebris.ShatterWood(visual, 1);
                    ParcelSoundEffects.Play(1);
                    ParcelSparkEffect.Play(visual, true);
                    visual.enabled = false;
                }
                yield return null;
            }
            // Objective completion does not wait for chips to reach the bottom of the screen.
            try { onComplete?.Invoke(); }
            finally
            {
                ReleaseBarrier();
                Destroy(gameObject);
            }
        }

        void ReleaseBarrier()
        {
            if (manager != null && barrier != null) manager.animateItems.Remove(barrier);
            barrier = null;
        }

        void OnDestroy()
        {
            // Restarting the field cancels collection and destroys its attached debris.
            ReleaseBarrier();
            onComplete = null;
        }
    }
}
