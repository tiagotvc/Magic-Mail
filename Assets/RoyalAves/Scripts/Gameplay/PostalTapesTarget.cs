using System.Linq;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Level;
namespace SweetSugar.Scripts.TargetScripts
{
    public sealed class PostalTapes : SolidBlock
    {
        public override void InitTarget(LevelData data) { }
        public override void FulfillTarget<T>(T[] items) { }
        public override int GetDestinationCount()=>LevelManager.THIS?.levelData?.TargetCounters?.Where(t=>t.targetLevel.name=="PostalTapes").Sum(t=>t.count) ?? 0;
        public override int GetDestinationCountSublevel()=>GetDestinationCount();
    }
}
