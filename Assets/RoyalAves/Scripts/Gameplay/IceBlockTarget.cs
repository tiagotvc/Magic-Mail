using RoyalAves.Gameplay;
using SweetSugar.Scripts.Level;

namespace SweetSugar.Scripts.TargetScripts
{
    // The legacy level loader resolves primary goals by this exact namespace and name.
    public sealed class IceBlock : SugarSquare
    {
        public override void InitTarget(LevelData levelData)
        {
            for (int i = 0; i < subTargetContainers.Length; i++)
                subTargetContainers[i].count = i == 0 ? IceLayer.CountCells(levelData) : 0;
        }
    }
}
