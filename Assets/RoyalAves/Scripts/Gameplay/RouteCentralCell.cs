using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RoyalAves.Effects;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    public sealed class RouteCentralCell : MonoBehaviour
    {
        public Sprite artwork;
        public Sprite standArtwork;
        public Sprite flaskArtwork;
        public Texture2D breakSheet;
        RouteCentralCell owner;
        Square cell;
        Square[] cells;
        RouteCentralPrototype visual;
        readonly bool[] broken = new bool[4];
        readonly HashSet<long> impacts = new HashSet<long>();
        public static readonly int[] RequestedColors = { 1, 0, 2, 6 };
        // Asset identities in flask order: green clover, lilac feather, red seal, blue envelope.
        public static readonly string[] RequestedPieces = { "gift", "envelope", "stamp", "bow" };
        float nextSpecialImpact;
        public int BrokenCount => owner != null && owner != this ? owner.BrokenCount : broken.Count(b => b);

        IEnumerator Start()
        {
            cell = transform.parent.GetComponent<Square>();
            GetComponent<SpriteRenderer>().enabled = false;
            yield return null;
            if (owner != null) yield break;
            var group = new[] { cell, cell.field.GetSquare(cell.col + 1, cell.row),
                cell.field.GetSquare(cell.col, cell.row + 1), cell.field.GetSquare(cell.col + 1, cell.row + 1) };
            if (group.Any(s => s == null || s.GetSubSquare().GetComponent<RouteCentralCell>() == null)) yield break;
            cells = group;
            foreach (var square in cells) square.GetSubSquare().GetComponent<RouteCentralCell>().owner = this;
            var root = new GameObject("RouteCentral_2x2");
            root.transform.SetParent(cell.field.transform, false);
            root.transform.position = (cells[0].transform.position + cells[3].transform.position) * .5f + Vector3.back * .6f;
            float width = Mathf.Abs(cells[3].transform.position.x - cells[0].transform.position.x) * 2;
            float scale = width / RouteCentralPrototype.CanvasSize;
            var parentScale = cell.field.transform.lossyScale;
            root.transform.localScale = new Vector3(scale / Mathf.Abs(parentScale.x), scale / Mathf.Abs(parentScale.y), 1);
            visual = root.AddComponent<RouteCentralPrototype>();
            visual.artwork = artwork;
            visual.standArtwork = standArtwork;
            visual.flaskArtwork = flaskArtwork;
            visual.breakSheet = breakSheet;
        }

        public static int SelectPotion(bool[] done, int color, bool special)
        {
            for (int i = 0; i < 4; i++)
                if (!done[i] && (special || RequestedColors[i] == color)) return i;
            return -1;
        }

        public static int SelectPotionForPiece(bool[] done, string piece, bool special)
        {
            for (int i = 0; i < 4; i++)
                if (!done[i] && (special || RequestedPieces[i] == piece)) return i;
            return -1;
        }

        public void HandleHit(Square origin)
        {
            if (owner != null) owner.Accept(origin);
        }

        void Accept(Square origin)
        {
            if (visual == null || origin == null || BrokenCount == 4) return;
            bool direct = cells.Contains(origin);
            Item item = origin.Item;
            if (!direct && (item == null || item.explodedItem != null || !cells.Any(s =>
                ItemFlyingLetter.IsAdjacentMatch(s.row, s.col, origin.row, origin.col)))) return;
            bool special = direct;
            if (special && Time.time < nextSpecialImpact) return;
            long key = item != null && item.combinationID != -1 ? (long)item.combinationID << 1
                : ((long)LevelManager.THIS.destLoopIterations << 32) | (uint)Time.frameCount;
            if (special) key |= 1;
            var matchedSprite = item != null ? item.GetSprite() : null;
            int index = SelectPotionForPiece(broken, matchedSprite != null ? matchedSprite.texture.name : null, special);
            if (index < 0 || impacts.Contains(key) || !visual.BreakFlask(index)) return;
            impacts.Add(key);
            if (special) nextSpecialImpact = Time.time + .25f;
            broken[index] = true;
            var manager = LevelManager.THIS;
            var barrier = visual.gameObject.AddComponent<SweetSugar.Scripts.System.AnimateItems>();
            barrier.target = true;
            manager.animateItems.Add(barrier);
            var level = manager.levelData;
            manager.delayedCall(Mathf.Max(2.4f, visual.SpillDuration(index) + .1f), () => {
                if (manager == null) return;
                manager.animateItems.Remove(barrier);
                if (barrier != null) Destroy(barrier);
                if (manager.levelData == level) manager.CheckWinLose();
            });
            foreach (var counter in manager.levelData.TargetCounters.Where(t => t.targetLevel.name == "RoutePotions"))
                counter.changeCount(-1);
            if (BrokenCount == 4) StartCoroutine(ReleaseCells());
        }

        IEnumerator ReleaseCells()
        {
            // Keep effects alive independently of the blocking cells being removed.
            yield return null;
            foreach (var square in cells)
                if (square != null && square.GetSubSquare().GetComponent<RouteCentralCell>() != null)
                    square.CompleteLayerRemoval(square.GetSubSquare());
        }
    }
}
