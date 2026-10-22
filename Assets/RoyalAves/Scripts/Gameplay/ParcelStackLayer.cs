using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using SweetSugar.Scripts;
using RoyalAves.Effects;
using System.Linq;
using System.Collections;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    // Each layer has a complete silhouette. Only the currently exposed state is drawn.
    [RequireComponent(typeof(Square), typeof(SpriteRenderer))]
    public sealed class ParcelStackLayer : MonoBehaviour
    {
        public const int MaxLayers = 3;
        [Range(1, MaxLayers)] public int remainingLayers = 1;
        [HideInInspector] public Vector2 visualScale = Vector2.one;
        [HideInInspector] public Vector2 visualOffset;
        SpriteRenderer spriteRenderer;
        Square square;
        Square cell;
        Vector3 restScale;
        Quaternion restRotation;
        Coroutine hitReaction;
        bool initialized;
        bool pendingHit;
        public bool CountsAsObjective => remainingLayers == 1;
        public bool IsFinishing { get; private set; }

        public void PlayHitReaction()
        {
            if (!initialized) { pendingHit = true; return; }
            if (hitReaction != null) StopCoroutine(hitReaction);
            transform.localScale = restScale;
            transform.localRotation = restRotation;
            hitReaction = StartCoroutine(HitReaction());
        }

        IEnumerator HitReaction()
        {
            float elapsed = 0f;
            while (elapsed < 0.20f)
            {
                elapsed += Time.deltaTime * ParcelFeedbackSettings.Speed;
                float t = Mathf.Clamp01(elapsed / 0.20f);
                float pulse = Mathf.Sin(t * Mathf.PI * 2f) * Mathf.Exp(-t * 3f) * ParcelFeedbackSettings.Intensity;
                transform.localScale = Vector3.Scale(restScale, new Vector3(1f + pulse * 0.045f, 1f - pulse * 0.055f, 1f));
                transform.localRotation = restRotation * Quaternion.Euler(0f, 0f, pulse * 1.8f);
                yield return null;
            }
            transform.localScale = restScale;
            transform.localRotation = restRotation;
            hitReaction = null;
        }

        public void PlayAccessoryRemoval()
        {
            if (!CountsAsObjective)
                ParcelAccessoryEffect.Play(GetComponent<SpriteRenderer>(), remainingLayers);
        }

        public bool TryHandleFinalHit(Square boardCell)
        {
            if (!CountsAsObjective) return false;
            if (IsFinishing) return true;
            IsFinishing = true;
            if (hitReaction != null) StopCoroutine(hitReaction);
            if (initialized)
            {
                transform.localScale = restScale;
                transform.localRotation = restRotation;
            }
            var manager = LevelManager.THIS;
            var data = manager.levelData;
            var layer = GetComponent<Square>();
            var source = GetComponent<SpriteRenderer>();
            var counters = data.TargetCounters.Where(counter =>
                counter.collectingAction == CollectingTypes.Destroy &&
                counter.extraObjects != null && counter.extraObjects.Contains(source.sprite)).ToArray();
            ParcelTearEffect.Play(source, manager, () =>
            {
                // Restarting or leaving the level cancels collection of the old parcel.
                if (boardCell == null || layer == null || manager == null || manager.levelData != data) return;
                if (!boardCell.CompleteLayerRemoval(layer)) return;
                foreach (var counter in counters) counter.changeCount(-1);
            });
            source.enabled = false;
            return true;
        }

        void Start()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            square = GetComponent<Square>();
            cell = transform.parent != null ? transform.parent.GetComponent<Square>() : null;
            if (cell != null)
            {
                transform.localScale = Vector3.Scale(transform.localScale, new Vector3(visualScale.x, visualScale.y, 1f));
                transform.localPosition += new Vector3(visualOffset.x, visualOffset.y, 0f);
            }
            restScale = transform.localScale;
            restRotation = transform.localRotation;
            initialized = true;
            if (pendingHit) PlayHitReaction();
            UpdateVisibility();
        }

        void LateUpdate() => UpdateVisibility();

        void UpdateVisibility()
        {
            // Collection animation clones have no board cell and retain their visible sprite.
            if (spriteRenderer == null || cell == null) return;
            spriteRenderer.enabled = !IsFinishing && cell.GetSubSquare() == square;
        }
    }
}
