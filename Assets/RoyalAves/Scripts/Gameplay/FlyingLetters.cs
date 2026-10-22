using System.Linq;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Level;

namespace SweetSugar.Scripts.TargetScripts
{
    // Legacy target identity; decremented exclusively by the Magic Egg's local break.
    public sealed class FlyingLetters : SolidBlock
    {
        public override void InitTarget(LevelData data)
        {
            foreach (var target in subTargetContainers)
                target.count = RoyalAves.Gameplay.ItemFlyingLetter.CountLetters(data);
        }
        public override void FulfillTarget<T>(T[] items) { }
        public override int GetDestinationCount() => LevelManager.THIS?.levelData?.TargetCounters?
            .Where(t => t.targetLevel.name == "FlyingLetters").Sum(t => t.count) ?? 0;
        public override int GetDestinationCountSublevel() => GetDestinationCount();
    }
}
