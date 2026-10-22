using System.Linq;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.System.Pool;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    // Legacy serialized identity retained. This is now the one-hit movable Magic Egg.
    public sealed class ItemFlyingLetter : Item, IItemInterface
    {
        bool broken;
        public bool IsBroken => broken;

        public static bool IsAdjacentMatch(int eggRow, int eggCol, int matchRow, int matchCol) =>
            System.Math.Abs(eggRow - matchRow) + System.Math.Abs(eggCol - matchCol) == 1;

        public static int CountLetters(LevelData data) => data?.fields?.Sum(field =>
            field.levelSquares.Count(cell => cell != null && cell.item != null &&
                cell.item.ItemType == ItemsTypes.FLYING_LETTER && !cell.startWithoutItem &&
                cell.block != SweetSugar.Scripts.Blocks.SquareTypes.NONE)) ?? 0;

        public override void InitItem()
        {
            broken = false;
            base.InitItem();
        }

        public bool TryBreak()
        {
            if (broken || !gameObject.activeInHierarchy || square == null || square.Item != this ||
                square.IsItemLocked || falling || needFall) return false;
            var manager = LevelManager.THIS;
            var data = manager != null ? manager.levelData : null;
            if (data == null || field != manager.field || ObjectPooler.Instance == null) return false;
            var sprite = GetSpriteRenderers().FirstOrDefault(r => r.sprite != null);
            if (sprite == null) return false;
            broken = true; // Commit before detaching: simultaneous hits can never break twice.
            destroying = true;
            var counters = data.TargetCounters.Where(t => t.targetLevel.name == "FlyingLetters").ToArray();
            RoyalAves.Effects.FallingBoardDebris.ShatterEgg(sprite);
            var sound = SweetSugar.Scripts.SoundBase.Instance;
            if (sound != null && sound.destroy != null && sound.destroy.Length > 0)
                sound.PlayOneShot(sound.destroy[Random.Range(0, sound.destroy.Length)]);
            square.Item = null;
            StopAllCoroutines();
            canBePooled = true;
            square = previousSquare = null;
            ObjectPooler.Instance.PutBack(gameObject);
            LevelManager.Score += scoreForItem;
            manager.CheckStars();
            // No HUD flight or deferred collection: the goal changes at the local break.
            foreach (var counter in counters) counter.changeCount(-1);
            return true;
        }

        public override void Check(Item item1, Item item2)
        {
            if (item2 != null && IsPlayerPowerUpType(item2.currentType)) item2.Check(item2, item1);
        }
        public void Destroy(Item item1, Item item2) => TryBreak();
        public bool IsExplodable() => true;
        public Item GetParentItem() => this;
        public void SetOrder(int order)
        {
            foreach (var renderer in GetSpriteRenderers()) renderer.sortingOrder = order;
        }
    }

}
