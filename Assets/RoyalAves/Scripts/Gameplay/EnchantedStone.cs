using System.Collections;
using System.Linq;
using RoyalAves.Effects;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    // Laboratory-only prefab: no serialized enum or campaign prefab is replaced.
    public sealed class EnchantedStone : MonoBehaviour
    {
        public bool IsBreaking { get; private set; }
        public static float EffectSpeed = 1f;
        public static float Scatter = 1f;
        public static int RejectedHits, BrokenStones;
        Coroutine reaction;

        public void HandleHit(Square cell, bool special)
        {
            if (IsBreaking || cell == null) return;
            if (!special)
            {
                RejectedHits++;
                if (reaction == null) reaction = StartCoroutine(Resist());
                return;
            }
            IsBreaking = true;
            if (reaction != null) StopCoroutine(reaction);
            var manager = LevelManager.THIS;
            var data = manager.levelData;
            var layer = GetComponent<Square>();
            var source = GetComponent<SpriteRenderer>();
            var counters = data.TargetCounters.Where(t => t.collectingAction == CollectingTypes.Destroy &&
                t.extraObjects != null && t.extraObjects.Contains(source.sprite)).ToArray();
            StoneShatterEffect.Play(source, EffectSpeed, Scatter);
            source.enabled = false;
            StartCoroutine(Complete());

            IEnumerator Complete()
            {
                yield return new WaitForSeconds(.12f / Mathf.Max(.1f, EffectSpeed));
                if (manager == null || manager.levelData != data || cell == null || layer == null) yield break;
                if (!cell.CompleteLayerRemoval(layer)) yield break;
                BrokenStones++;
                foreach (var counter in counters) counter.changeCount(-1);
            }
        }

        IEnumerator Resist()
        {
            var source = GetComponent<SpriteRenderer>();
            var original = source.color;
            for (float time = 0; time < .14f; time += Time.deltaTime)
            {
                source.color = Color.Lerp(original, new Color(.7f,.68f,.78f,1), Mathf.Sin(time / .14f * Mathf.PI));
                yield return null;
            }
            source.color = original;
            reaction = null;
        }
    }
}
