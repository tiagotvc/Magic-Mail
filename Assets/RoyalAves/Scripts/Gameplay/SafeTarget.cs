using System.Linq;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Level;

namespace SweetSugar.Scripts.TargetScripts
{
    public sealed class Safe : SolidBlock
    {
        public override void InitTarget(LevelData data) => subTargetContainers[0].count=RoyalAves.Gameplay.SafeFootprint.Count(data);
        public override int GetDestinationCount() => LevelManager.THIS?.fieldBoards?
            .Sum(field=>field.GetComponentsInChildren<RoyalAves.Gameplay.SafeCell>().Length) ?? 0;
        public override int GetDestinationCountSublevel() => LevelManager.THIS?.field?
            .GetComponentsInChildren<RoyalAves.Gameplay.SafeCell>().Length ?? 0;
    }
}
