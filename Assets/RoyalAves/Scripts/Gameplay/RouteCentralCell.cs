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
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("MagicMail.RouteCentralLab.DrawnLiquid", false)
                && RouteCentralLaboratory.IsActive)
            {
                visual.drawnLiquidSheet = Resources.Load<Texture2D>("RouteCentral/CM_pocao_escoamento_desenhado_v1");
                visual.drawnLiquidPreview = visual.drawnLiquidSheet != null;
                if (!visual.drawnLiquidPreview) Debug.LogError("Poção desenhada: spritesheet ausente.", visual);
            }
            // Configure each new central before it can receive its first hit.
            // The normal game and ordinary laboratory keep the existing effect.
            if (UnityEditor.SessionState.GetBool("MagicMail.RouteCentralLab.Metaballs", false)
                && RouteCentralLaboratory.IsActive)
            {
                visual.metaballsShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(
                    "Assets/RoyalAves/Shaders/PotionMetaballsPreview.shader");
                visual.metaballsPreview = visual.metaballsShader != null;
            }
#endif
        }

        public static int SelectPotion(bool[] done, int color, bool special)
        {
            if (special) return -1; // Direct hits require board coordinates.
            for (int i = 0; i < 4; i++)
                if (!done[i] && RequestedColors[i] == color) return i;
            return -1;
        }

        public static int SelectPotionForPiece(bool[] done, string piece, bool special)
        {
            if (special) return -1;
            for (int i = 0; i < 4; i++)
                if (!done[i] && RequestedPieces[i] == piece) return i;
            return -1;
        }

        public static int SelectPotionAtCell(bool[] done, int columnOffset, int rowOffset)
        {
            if (done == null || done.Length < 4 || columnOffset < 0 || columnOffset > 1
                || rowOffset < 0 || rowOffset > 1) return -1;
            int index = rowOffset * 2 + columnOffset;
            return done[index] ? -1 : index;
        }

        public void HandleHit(Square origin)
        {
            if (owner != null) owner.Accept(origin, cell);
        }

        void Accept(Square origin, Square hitCell)
        {
            if (visual == null || origin == null || hitCell == null || BrokenCount == 4) return;
            bool direct = cells.Contains(origin);
            Item item = origin.Item;
            if (!direct && (item == null || item.explodedItem != null || !cells.Any(s =>
                ItemFlyingLetter.IsAdjacentMatch(s.row, s.col, origin.row, origin.col)))) return;
            long key = item != null && item.combinationID != -1 ? (long)item.combinationID << 1
                : ((long)LevelManager.THIS.destLoopIterations << 32) | (uint)Time.frameCount;
            var matchedSprite = item != null ? item.GetSprite() : null;
            // A line/area special calls DestroyBlock on each affected board cell.
            // Preserve that footprint: no central-wide cooldown or first-intact
            // fallback. Per-flask state makes repeated hits idempotent.
            int index = direct ? SelectPotionAtCell(broken, hitCell.col - cells[0].col, hitCell.row - cells[0].row)
                : SelectPotionForPiece(broken, matchedSprite != null ? matchedSprite.texture.name : null, false);
            if (index < 0 || (!direct && impacts.Contains(key)) || !visual.BreakFlask(index)) return;
            if (!direct) impacts.Add(key);
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
