#if UNITY_EDITOR
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    public static class EnchantedStoneLaboratory
    {
        public const string EnabledKey = "MagicMail.EnchantedStoneLab";
        public const string LevelPath = "Assets/RoyalAves/Laboratory/EnchantedStoneLevel.asset";
        public const string TargetPath = "Assets/RoyalAves/Laboratory/EnchantedStoneTargets.asset";
        public const string PrefabPath = "Assets/RoyalAves/Laboratory/EnchantedStoneCell.prefab";
        public static bool IsActive { get; private set; }
        public static bool TryLoad(int number, out LevelData data)
        {
            IsActive = false;
            data = null;
            if (!SessionState.GetBool(EnabledKey, false)) return false;
            var source = AssetDatabase.LoadAssetAtPath<LevelContainer>(LevelPath);
            var targets = AssetDatabase.LoadAssetAtPath<TargetLevel>(TargetPath);
            if (source == null || targets == null) return false;
            data = source.levelData.DeepCopyForPlay(number);
            data.CheckLayers(); data.LoadTargetObject(); data.InitTargetObjects(true, targets);
            // SolidBlock normally forces the wooden-crate icon; override only this play copy.
            foreach (var counter in data.TargetCounters)
                if (counter.extraObjects != null && counter.extraObjects.Length > 0)
                    counter.extraObject = counter.extraObjects[0];
            EnchantedStone.RejectedHits = EnchantedStone.BrokenStones = 0;
            IsActive = true;
            return true;
        }
        public static GameObject GetPrefab(SquareTypes type) => IsActive && type == SquareTypes.SolidBlock
            ? AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) : null;
    }
}
#endif
