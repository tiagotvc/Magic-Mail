using System.Linq;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Level;

namespace SweetSugar.Scripts.TargetScripts
{
    public sealed class RoutePotions : SolidBlock
    {
        public override void InitTarget(LevelData data)
        {
            foreach (var target in subTargetContainers) target.count = 4;
        }
        public override void FulfillTarget<T>(T[] items) { }
        public override int GetDestinationCount() => LevelManager.THIS?.levelData?.TargetCounters?
            .Where(t => t.targetLevel.name == "RoutePotions").Sum(t => t.count) ?? 0;
        public override int GetDestinationCountSublevel() => GetDestinationCount();
    }
}
