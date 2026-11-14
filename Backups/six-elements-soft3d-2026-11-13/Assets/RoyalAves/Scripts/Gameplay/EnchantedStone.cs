using System.Collections;
using System.Linq;
using RoyalAves.Effects;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    // One-cell rune stone: ordinary matches have no visual or damage response.
    public sealed class EnchantedStone : MonoBehaviour
    {
        public bool IsBreaking { get; private set; }
        public static float EffectSpeed = 1f;
        public static float Scatter = 1f;
        public static int RejectedHits, BrokenStones;
#if UNITY_EDITOR
        public static event System.Action<Square> HitApplied;
#endif

        static AudioClip breakSound;
        // Stones usually break in bursts (a rocket crossing a row). Only the first of a burst plays the
        // whole clip; the ones right after it skip the attack, so the burst is not a machine gun.
        // The offset is the 2-frame mark (30 fps) picked in the sound editor.
        public static float ChainStartOffset = 2f / 30f, ChainWindow = .5f;
        static float lastBreakTime = -99;

        static void PlayBreakSound()
        {
            if (breakSound == null) breakSound = Resources.Load<AudioClip>("Audio/pedra-quebrando");
            var sound = SweetSugar.Scripts.SoundBase.Instance;
            if (breakSound == null || sound == null) return;
            bool chained = Time.unscaledTime - lastBreakTime <= ChainWindow;
            lastBreakTime = Time.unscaledTime;
            if (!chained) { sound.PlayOneShot(breakSound); return; }
            // PlayOneShot always starts at zero, so a trimmed start needs its own short-lived voice.
            float offset = Mathf.Clamp(ChainStartOffset, 0, breakSound.length - .05f);
            var reference = sound.GetComponent<AudioSource>();
            var voice = new GameObject("StoneBreakVoice").AddComponent<AudioSource>();
            voice.transform.SetParent(sound.transform, false);
            voice.clip = breakSound;
            voice.playOnAwake = false;
            if (reference != null)
            {
                voice.volume = reference.volume;
                voice.spatialBlend = reference.spatialBlend;
                voice.outputAudioMixerGroup = reference.outputAudioMixerGroup;
            }
            voice.time = offset;
            voice.Play();
            Destroy(voice.gameObject, breakSound.length - offset + .1f);
        }

        public static int CountCells(SweetSugar.Scripts.Level.LevelData data) => data.fields.Sum(field =>
            field.levelSquares.Count(cell => cell.blocks.Any(layer => layer.squareType == SquareTypes.EnchantedStone) ||
                cell.block == SquareTypes.EnchantedStone || cell.obstacle == SquareTypes.EnchantedStone));

        public void HandleHit(Square cell, bool special)
        {
            if (IsBreaking || cell == null) return;
            if (!special)
            {
                RejectedHits++;
                return;
            }
            IsBreaking = true;
#if UNITY_EDITOR
            HitApplied?.Invoke(cell);
#endif
            var manager = LevelManager.THIS;
            var data = manager.levelData;
            var layer = GetComponent<Square>();
            var source = GetComponent<SpriteRenderer>();
            var counters = data.TargetCounters.Where(t => t.collectingAction == CollectingTypes.Destroy &&
                t.extraObjects != null && t.extraObjects.Contains(source.sprite)).ToArray();
            StoneShatterEffect.Play(source, EffectSpeed, Scatter);
            PlayBreakSound();
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

    }
}

namespace SweetSugar.Scripts.TargetScripts
{
    // Compatibility target handler for saved levels; the stone counts on its local break.
    public sealed class EnchantedStone : SolidBlock
    {
        public override int GetDestinationCount()
        {
            var manager = LevelManager.THIS;
            return manager != null ? manager.fieldBoards.Sum(field => field.CountSquaresByType(nameof(EnchantedStone))) : 0;
        }

        public override void InitTarget(SweetSugar.Scripts.Level.LevelData levelData)
        {
            subTargetContainers[0].count = RoyalAves.Gameplay.EnchantedStone.CountCells(levelData);
        }
    }
}
