using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.Core;
using System.Linq;

namespace SweetSugar.Scripts.TargetScripts
{
    // Manual delivery goal. Only FrankingMachineCell.Collect changes its runtime counter.
    public sealed class MachineLetters : SolidBlock
    {
        public override void InitTarget(LevelData levelData)
        {
            foreach (var target in subTargetContainers) target.count = 8;
        }

        public override int GetDestinationCount() => LevelManager.THIS?.levelData?.TargetCounters?
            .Where(t => t.targetLevel.name == "MachineLetters").Sum(t => t.count) ?? 0;
        public override int GetDestinationCountSublevel() => GetDestinationCount();
    }
}
