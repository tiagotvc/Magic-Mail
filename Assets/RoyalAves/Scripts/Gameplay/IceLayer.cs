using System.Linq;
using RoyalAves.Effects;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    [RequireComponent(typeof(Square), typeof(SpriteRenderer))]
    public sealed class IceLayer : MonoBehaviour
    {
        [Range(1, 2)] public int remainingLayers = 1;
        public bool CountsAsObjective => remainingLayers == 1;
        public static float OpacityForLevel(int level) => level == 1 ? .35f : .60f;
        public static int CountCells(LevelData data) => data.fields.Sum(field => field.levelSquares.Count(cell =>
            cell.blocks != null && cell.blocks.Count > 0
                ? cell.blocks.Any(layer => layer.squareType == SquareTypes.IceBlock)
                : cell.block == SquareTypes.IceBlock || cell.obstacle == SquareTypes.IceBlock));
        bool removed;
        SpriteRenderer visual;
        Square cell;

        void Start()
        {
            visual = GetComponent<SpriteRenderer>();
            visual.color = new Color(1f, 1f, 1f, OpacityForLevel(remainingLayers));
            cell = transform.parent != null ? transform.parent.GetComponent<Square>() : null;
            UpdateVisibility();
        }

        void LateUpdate() => UpdateVisibility();

        void UpdateVisibility()
        {
            if (visual == null || cell == null) return;
            // Each layer is a complete silhouette; drawing both would hide the piece below thin ice.
            visual.enabled = !removed && cell.subSquares.LastOrDefault(s => s.GetComponent<IceLayer>() != null) == GetComponent<Square>();
        }

        public void HandleHit(Square boardCell)
        {
            if (removed) return;
            removed = true;
            boardCell.AbsorbIceImpact();
            var manager = LevelManager.THIS;
            var layer = GetComponent<Square>();
            var source = GetComponent<SpriteRenderer>();
            var counters = manager.levelData.TargetCounters.Where(counter => CountsAsObjective &&
                counter.collectingAction == CollectingTypes.Destroy && counter.extraObjects != null &&
                counter.extraObjects.Contains(source.sprite)).ToArray();
            IceBreakEffect.Play(source, remainingLayers > 1);
            source.enabled = false;
            // Use the shared removal path to restore cell permissions without touching its item.
            if (!boardCell.CompleteLayerRemoval(layer)) return;
            foreach (var counter in counters) counter.changeCount(-1);
        }
    }
}
