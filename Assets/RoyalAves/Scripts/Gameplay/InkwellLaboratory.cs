#if UNITY_EDITOR
using System.Linq;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    public static class InkwellLaboratory
    {
        public const string EnabledKey = "MagicMail.InkwellLab";
        public const string LevelPath = "Assets/RoyalAves/Laboratory/InkwellLevel.asset";
        public const string TargetPath = "Assets/RoyalAves/Laboratory/InkwellTargets.asset";
        public const string PrefabPath = "Assets/RoyalAves/Laboratory/InkwellCell.prefab";
        public static bool IsActive { get; private set; }
        public static void Disable() { SessionState.SetBool(EnabledKey, false); IsActive = false; }
        public static bool TryLoad(int number, out LevelData data)
        {
            data = null; IsActive = false;
            if (!SessionState.GetBool(EnabledKey, false)) return false;
            if (LightBallLaboratory.OtherKeys.Any(k => SessionState.GetBool(k, false)) ||
                SessionState.GetBool(LightBallLaboratory.EnabledKey, false)) { Disable(); return false; }
            var source = AssetDatabase.LoadAssetAtPath<LevelContainer>(LevelPath);
            var targets = AssetDatabase.LoadAssetAtPath<TargetLevel>(TargetPath);
            if (source == null || targets == null) { Disable(); return false; }
            data = source.levelData.DeepCopyForPlay(number);
            data.CheckLayers(); data.LoadTargetObject(); data.InitTargetObjects(true, targets);
            foreach (var goal in data.TargetCounters)
            {
                if (goal.extraObjects?.Length > 0) goal.extraObject = goal.extraObjects[0];
            }
            IsActive = true; return true;
        }
        public static GameObject GetPrefab(SquareTypes type) => IsActive && type == SquareTypes.Inkwell
            ? AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) : null;
    }
}
#endif
