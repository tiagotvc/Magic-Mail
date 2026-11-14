using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Level;

namespace SweetSugar.Scripts.TargetScripts
{
    public sealed class Inkwell : SolidBlock
    {
        public override void InitTarget(LevelData data) => subTargetContainers[0].count = InkwellCell.CountCells(data);
        public override int GetDestinationCount() => LevelManager.THIS.fieldBoards.Sum(field=>field.GetComponentsInChildren<InkwellCell>().Length);
        public override int GetDestinationCountSublevel() => LevelManager.THIS.field.GetComponentsInChildren<InkwellCell>().Length;
    }
}
