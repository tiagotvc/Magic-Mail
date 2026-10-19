using RoyalAves.Gameplay;
using SweetSugar.Scripts.Level;

namespace SweetSugar.Scripts.TargetScripts
{
    // The legacy level loader resolves primary goals by this exact namespace and name.
    // The goal is read from the painted 2x2 groups and includes each letter in the four niches.
    public sealed class MailCubby : SugarSquare
    {
        public override void InitTarget(LevelData levelData)
        {
            for (int i = 0; i < subTargetContainers.Length; i++)
                subTargetContainers[i].count = i == 0 ? MailCubbyCell.CountLetters(levelData) : 0;
        }
    }
}
