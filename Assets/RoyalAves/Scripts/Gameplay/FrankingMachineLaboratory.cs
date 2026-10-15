#if UNITY_EDITOR
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    public static class FrankingMachineLaboratory
    {
        public const string EnabledKey = "MagicMail.FrankingMachineLaboratory";
        public const string LevelPath = "Assets/RoyalAves/Laboratory/FrankingMachineLevel.asset";
        public const string TargetPath = "Assets/RoyalAves/Laboratory/FrankingMachineTargets.asset";
        public const string CellPrefabPath = "Assets/RoyalAves/Laboratory/FrankingMachineCell.prefab";
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
            foreach (var field in data.fields) FrankingMachineCell.RestoreGridPlacements(field);
            data.LoadTargetObject();
            data.InitTargetObjects(true, targets);
            foreach (var counter in data.TargetCounters)
            {
                counter.targetLevel = JsonUtility.FromJson<SweetSugar.Scripts.TargetScripts.TargetSystem.TargetContainer>(
                    JsonUtility.ToJson(data.GetTargetByNameEditor("MachineLetters")));
                counter.targetLevel.setCount = SweetSugar.Scripts.TargetScripts.TargetSystem.SetCount.Manually;
                counter.count = 8;
            }
            IsActive = true;
            return true;
        }

        static GameObject GetCellAsset() => AssetDatabase.LoadAssetAtPath<GameObject>(CellPrefabPath);
        public static GameObject GetPrefab(SquareTypes type) => IsActive && type == SquareTypes.SolidBlock ? GetCellAsset() : null;
    }
}
#endif

