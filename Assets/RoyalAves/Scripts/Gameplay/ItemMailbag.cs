using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.System.Pool;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    public sealed class MailbagDamageState
    {
        readonly HashSet<int> matches = new HashSet<int>();
        float nextHit = float.NegativeInfinity;
        public int Remaining { get; private set; } = 2;
        public void Reset() {Remaining=2;nextHit=float.NegativeInfinity;matches.Clear();}
        public bool TryHit(float time, int matchId)
        {
            if (Remaining <= 0 || time < nextHit || (matchId != -1 && matches.Contains(matchId))) return false;
            Remaining--;nextHit=time+.25f;
            if (matchId != -1) matches.Add(matchId);
            return true;
        }
    }

    public sealed class ItemMailbag : Item, IItemInterface
    {
        public Sprite intactSprite;
        public Sprite damagedSprite;
        readonly MailbagDamageState damage = new MailbagDamageState();
        public int RemainingHits => damage.Remaining;
        SpriteRenderer body;
        Vector3 bodyScale;
        SweetSugar.Scripts.System.AnimateItems collectionBarrier;
        LevelManager collectionManager;

        public static int CountBags(LevelData data) => data?.fields?.Sum(f => f.levelSquares.Count(c =>
            c != null && c.item != null && c.item.ItemType == ItemsTypes.MAILBAG && !c.startWithoutItem
            && c.block != SweetSugar.Scripts.Blocks.SquareTypes.NONE)) ?? 0;

        public override void InitItem()
        {
            if (body != null) body.transform.localScale = bodyScale;
            damage.Reset();
            base.InitItem();
            body = GetSpriteRenderers().FirstOrDefault();
            if (body != null)
            {
                body.sprite = intactSprite;
                bodyScale = body.transform.localScale;
            }
        }

        public void HandleHit(SweetSugar.Scripts.Blocks.Square origin)
        {
            if (origin == null || square == null) return;
            if (origin != square)
            {
                if (!ItemFlyingLetter.IsAdjacentMatch(square.row,square.col,origin.row,origin.col)
                    || origin.Item == null || origin.Item.explodedItem != null) return;
                int id = origin.Item.combinationID;
                TryHit(id);
                return;
            }
            TryHit();
        }

        public bool TryHit(int matchId = -1)
        {
            if (RemainingHits <= 0 || !gameObject.activeInHierarchy || square == null || square.Item != this
                || square.IsItemLocked || falling || needFall) return false;
            var manager = LevelManager.THIS;
            if (manager == null || manager.levelData == null || field != manager.field || ObjectPooler.Instance == null) return false;
            if (!damage.TryHit(Time.time,matchId)) return false;
            if (RemainingHits == 1)
            {
                if (body != null) body.sprite = damagedSprite;
                RoyalAves.Effects.MailCubbySounds.PlayDoors();
                StartCoroutine(Pulse());
                return true;
            }
            destroying = true;
            collectionManager = manager;
            collectionBarrier = gameObject.AddComponent<SweetSugar.Scripts.System.AnimateItems>();
            collectionBarrier.target = true;
            collectionBarrier.linkObjectHash = gameObject.GetHashCode();
            manager.animateItems.Add(collectionBarrier);
            // Short cloth withdrawal rather than wooden debris or an atlas dragged down-screen.
            if (body != null) StartCoroutine(Collect(manager));
            else FinishCollection(manager);
            return true;
        }

        IEnumerator Pulse()
        {
            float elapsed = 0;
            while (elapsed < .16f)
            {
                elapsed += Time.deltaTime;
                float pulse = Mathf.Sin(Mathf.Clamp01(elapsed / .16f) * Mathf.PI) * .08f;
                if (body != null) body.transform.localScale = Vector3.Scale(bodyScale,new Vector3(1+pulse,1-pulse,1));
                yield return null;
            }
            if (body != null) body.transform.localScale = bodyScale;
        }

        IEnumerator Collect(LevelManager manager)
        {
            float elapsed = 0;
            while (elapsed < .16f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / .16f);
                body.transform.localScale = Vector3.Scale(bodyScale,new Vector3(1+.15f*t,1-t,1));
                yield return null;
            }
            FinishCollection(manager);
        }

        void FinishCollection(LevelManager manager)
        {
            var counters = manager.levelData.TargetCounters.Where(t => t.targetLevel.name == "Mailbags").ToArray();
            square.Item = null;
            canBePooled = true;
            square = previousSquare = null;
            if (body != null) body.transform.localScale = bodyScale;
            ObjectPooler.Instance.PutBack(gameObject);
            LevelManager.Score += scoreForItem;
            manager.CheckStars();
            foreach (var counter in counters) counter.changeCount(-1);
            manager.CheckWinLose();
        }

        public override void Check(Item a, Item b)
        {
            if (b != null && IsPlayerPowerUpType(b.currentType)) b.Check(b,a);
        }
        public void Destroy(Item a, Item b) => TryHit();
        public bool IsExplodable() => true;
        public Item GetParentItem() => this;
        public void SetOrder(int order) { foreach (var r in GetSpriteRenderers()) r.sortingOrder = order; }

        protected override void OnDisable()
        {
            base.OnDisable();
            StopAllCoroutines();
            if (body != null) body.transform.localScale = bodyScale;
            if (collectionBarrier != null)
            {
                if (collectionManager != null) collectionManager.animateItems.Remove(collectionBarrier);
                Destroy(collectionBarrier);
                collectionBarrier = null;
            }
            collectionManager = null;
        }
    }
}
