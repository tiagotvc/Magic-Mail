using System.Collections.Generic;
using RoyalAves.Effects;
using SweetSugar.Scripts.Items;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    // Attached to the piece, so the stain travels with swaps and falls.
    public sealed class InkwellStainedPiece : MonoBehaviour
    {
        readonly List<InkTintVisual> visuals = new List<InkTintVisual>();
        bool painted;
        public void Paint(Material template, float duration)
        {
            if (painted || template == null) return;
            painted = true;
            var item = GetComponent<Item>();
            foreach (var source in item.GetSpriteRenderers())
                if (source != null && source.sprite != null)
                    visuals.Add(InkTintVisual.Attach(source, template, new Vector2(.5f, .9f), duration));
        }
        // Called by the pool, not OnDisable: temporarily hiding a piece must preserve its ink.
        public void ClearInk()
        {
            foreach (var visual in visuals) if (visual != null)
            {
                if (Application.isPlaying) Destroy(visual.gameObject); else DestroyImmediate(visual.gameObject);
            }
            visuals.Clear(); painted = false;
        }
    }
}
