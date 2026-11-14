using System.Collections;
using System.Linq;
using RoyalAves.Effects;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    public sealed class InkwellCell : MonoBehaviour
    {
        public Sprite[] stages;
        public Material tintMaterial;
        [Min(.1f)] public float tintDuration = .5f;
        public int Hits { get; private set; }
        public bool IsBreaking => Hits == 5;
        public Vector3 BodyCenterLocal => Soft3DElementStyle.Has(this)?stages[0].bounds.center+Vector3.down*.08f:new Vector3((288f-stages[0].pivot.x)/stages[0].pixelsPerUnit,
            (240.5f-stages[0].pivot.y)/stages[0].pixelsPerUnit,0);
        public Vector3 MouthPosition
        {
            get
            {
                var intact=stages[0];
                if(Soft3DElementStyle.Has(this))return transform.TransformPoint(intact.bounds.center+Vector3.up*intact.bounds.extents.y*.65f);
                // Authored mouth landmark in the intact frame, independent of damaged sprite bounds.
                return transform.TransformPoint(new Vector3((287.5f-intact.pivot.x)/intact.pixelsPerUnit,
                    (401f-intact.pivot.y)/intact.pixelsPerUnit,0));
            }
        }
        float nextHit;
        public static int CountCells(SweetSugar.Scripts.Level.LevelData data) => data.fields.Sum(field =>
            field.levelSquares.Count(cell => cell.blocks.Any(layer => layer.squareType == SquareTypes.Inkwell) ||
                cell.block == SquareTypes.Inkwell || cell.obstacle == SquareTypes.Inkwell));
        public void HandleHit(Square cell, Square origin)
        {
            if (cell == null || IsBreaking || Time.time < nextHit) return;
            if (origin != null && origin != cell && Mathf.Abs(cell.col-origin.col)+Mathf.Abs(cell.row-origin.row)!=1) return;
            nextHit = Time.time + .2f;
            Hits++;
            var visual = GetComponent<SpriteRenderer>();
            if (Hits < 5)
            {
                if(Soft3DElementStyle.Has(this))Soft3DElementEffect.Burst(visual,new Color(.53f,.19f,.77f),3,.55f);
                else StoneShatterEffect.PlayPorcelain(visual,chipOnly:true,scatter:.65f);
                if (stages != null && stages.Length > Hits) visual.sprite = stages[Hits];
                // One previously unpainted orthogonal neighbor per hit, no diagonal propagation.
                var neighbor = cell.GetAllNeghborsCross().FirstOrDefault(c => c != null && c.IsFree() && c.GetComponent<InkwellStainedCell>() == null);
                if (neighbor != null)
                {
                    InkwellSplashEffect.Play(visual, neighbor.transform.position);
                    InkwellStainedCell.Paint(neighbor, cell.transform.position, tintMaterial, tintDuration, InkwellSplashEffect.TravelTime);
                }
                return;
            }
            // Fracture the last standing state. Never swap to the old static rubble PNG.
            if(Soft3DElementStyle.Has(this))
            {
                Soft3DElementEffect.Burst(visual,new Color(.53f,.19f,.77f));
                Soft3DElementEffect.Burst(visual,new Color(.97f,.69f,.17f),7);
            }
            else
            {
                StoneShatterEffect.PlayPorcelain(visual,chipOnly:false);
                PorcelainBadgeDebris.Play(visual,stages[0]);
            }
            visual.enabled=false;
            InkwellSplashEffect.Play(visual, cell.transform.position);
            InkwellStainedCell.Paint(cell, cell.transform.position, tintMaterial, tintDuration, InkwellSplashEffect.TravelTime);
            StartCoroutine(Break(cell));
        }
        IEnumerator Break(Square cell)
        {
            var manager = LevelManager.THIS;
            var data = manager.levelData;
            var layer = GetComponent<Square>();
            var counters = data.TargetCounters.Where(t => t.collectingAction == CollectingTypes.Destroy &&
                t.extraObjects != null && stages.Length > 0 && t.extraObjects.Contains(stages[0])).ToArray();
            yield return new WaitForSeconds(.3f);
            if (manager == null || manager.levelData != data || cell == null || layer == null) yield break;
            if (!cell.CompleteLayerRemoval(layer)) yield break;
            foreach (var counter in counters) counter.changeCount(-1);
        }
    }
}
