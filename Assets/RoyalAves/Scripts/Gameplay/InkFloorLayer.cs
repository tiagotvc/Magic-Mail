using System.Linq;
using RoyalAves.Effects;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    // Legacy component name retained for serialized assets. The floor now uses the original grass art.
    [RequireComponent(typeof(Square), typeof(SpriteRenderer))]
    public sealed class InkFloorLayer : MonoBehaviour
    {
        [Range(1, 2)] public int remainingLayers = 1;
        [HideInInspector] public Vector2 visualScale = Vector2.one;
        [HideInInspector] public Vector2 visualOffset;
        public bool CountsAsObjective => remainingLayers == 1;
        bool removed;
        public bool IsRemoving => removed;
        SpriteRenderer visual;
        GrassOrganicBorder organicBorder;
        Square cell;
        Vector3 baseVisualScale;
        Vector3 baseVisualPosition;
        bool fitInitialized;

        void Start()
        {
            visual = GetComponent<SpriteRenderer>();
            cell = transform.parent != null ? transform.parent.GetComponent<Square>() : null;
            if (cell != null)
            {
                organicBorder = GetComponent<GrassOrganicBorder>();
                if (organicBorder == null) organicBorder = gameObject.AddComponent<GrassOrganicBorder>();
                if (!fitInitialized)
                {
                    baseVisualScale = transform.localScale;
                    baseVisualPosition = transform.localPosition;
                    fitInitialized = true;
                }
                ApplyVisualFit(visualScale, visualOffset);
            }
            UpdateVisibility();
        }

        // Also used by the editor to update an existing game without compounding its scale.
        public void ApplyVisualFit(Vector2 scale, Vector2 offset)
        {
            if (!fitInitialized)
            {
                // A live component can have received its fit before this script was recompiled.
                // Recover the original transform in that case instead of applying the fit twice.
                baseVisualScale = didStart ? new Vector3(
                    transform.localScale.x / Mathf.Max(visualScale.x, 0.001f),
                    transform.localScale.y / Mathf.Max(visualScale.y, 0.001f), transform.localScale.z) : transform.localScale;
                baseVisualPosition = didStart ? transform.localPosition - new Vector3(visualOffset.x, visualOffset.y) : transform.localPosition;
                fitInitialized = true;
            }
            visualScale = scale;
            visualOffset = offset;
            transform.localScale = Vector3.Scale(baseVisualScale, new Vector3(scale.x, scale.y, 1f));
            transform.localPosition = baseVisualPosition + new Vector3(offset.x, offset.y);
        }

        void LateUpdate() => UpdateVisibility();

        void UpdateVisibility()
        {
            if (visual == null || cell == null) return;
            var exposedInk = cell.subSquares.LastOrDefault(layer => layer.GetComponent<InkFloorLayer>() != null);
            bool visible = !removed && exposedInk == GetComponent<Square>();
            if (organicBorder != null)
            {
                organicBorder.Show(visual, cell, visible);
                visual.enabled = false;
            }
            else visual.enabled = visible;
        }

        public void HandleHit(Square boardCell)
        {
            if (removed) return;
            removed = true;
            var manager = LevelManager.THIS;
            var data = manager.levelData;
            var layer = GetComponent<Square>();
            var source = GetComponent<SpriteRenderer>();
            var sprite = source.sprite;
            if (!CountsAsObjective)
            {
                GrassDefoliateEffect.Play(source, manager, false);
                boardCell.CompleteLayerRemoval(layer);
                return;
            }
            var counters = data.TargetCounters.Where(counter =>
                counter.collectingAction == CollectingTypes.Destroy &&
                counter.extraObjects != null && counter.extraObjects.Contains(sprite)).ToArray();
            GrassDefoliateEffect.Play(source, manager, true, () =>
            {
                if (boardCell == null || layer == null || manager == null || manager.levelData != data) return;
                if (!boardCell.CompleteLayerRemoval(layer)) return;
                foreach (var counter in counters) counter.changeCount(-1);
            });
            source.enabled = false;
        }
    }
}
