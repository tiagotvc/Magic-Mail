#if UNITY_EDITOR
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    public static class MailCubbyLaboratory
    {
        public const string EnabledKey = "MagicMail.MailCubbyLaboratory";
        public const string LevelPath = "Assets/RoyalAves/Laboratory/MailCubbyLevel.asset";
        public const string TargetPath = "Assets/RoyalAves/Laboratory/MailCubbyTargets.asset";
        public const string CellPrefabPath = "Assets/RoyalAves/Laboratory/MailCubbyCell.prefab";
        public static bool IsActive { get; private set; }
        public static void ResetActive() => IsActive = false;

        public static bool TryLoad(int number, out LevelData data)
        {
            data = null;
            if (!SessionState.GetBool(EnabledKey, false)) return false;
            var source = AssetDatabase.LoadAssetAtPath<LevelContainer>(LevelPath);
            var targets = AssetDatabase.LoadAssetAtPath<TargetLevel>(TargetPath);
            if (source == null || targets == null || GetCellAsset() == null)
            {
                SessionState.SetBool(EnabledKey, false);
                Debug.LogError("Prepare o laboratório pelo menu Elementos/Escaninho.");
                return false;
            }
            data = source.levelData.DeepCopyForPlay(number);
            data.CheckLayers();
            data.LoadTargetObject();
            data.InitTargetObjects(true, targets);
            IsActive = true;
            return true;
        }

        static GameObject GetCellAsset() => AssetDatabase.LoadAssetAtPath<GameObject>(CellPrefabPath);
        public static GameObject GetPrefab(SquareTypes type) => IsActive && type == SquareTypes.MailCubby ? GetCellAsset() : null;
    }
}
#endif
