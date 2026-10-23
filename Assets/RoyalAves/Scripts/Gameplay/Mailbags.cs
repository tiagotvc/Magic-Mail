using System.Linq;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Level;

namespace SweetSugar.Scripts.TargetScripts
{
    public sealed class Mailbags : SolidBlock
    {
        public override void InitTarget(LevelData data)
        {
            foreach (var target in subTargetContainers) target.count = RoyalAves.Gameplay.ItemMailbag.CountBags(data);
        }
        public override void FulfillTarget<T>(T[] items) { }
        public override int GetDestinationCount() => LevelManager.THIS?.levelData?.TargetCounters?
            .Where(t => t.targetLevel.name == "Mailbags").Sum(t => t.count) ?? 0;
        public override int GetDestinationCountSublevel() => GetDestinationCount();
    }
}
