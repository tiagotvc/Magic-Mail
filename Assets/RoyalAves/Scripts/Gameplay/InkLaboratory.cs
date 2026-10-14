#if UNITY_EDITOR
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    public static class InkLaboratory
    {
        public const string EnabledKey = "MagicMail.InkLaboratory";
        public const string LevelPath = "Assets/RoyalAves/Laboratory/InkLevel.asset";
        public const string TargetPath = "Assets/RoyalAves/Laboratory/InkTargets.asset";
        public const string PrefabPath = "Assets/RoyalAves/Laboratory/InkFloor.prefab";
        public static bool IsActive { get; private set; }
        public static void ResetActive() => IsActive = false;

        public static bool TryLoad(int levelNumber, out LevelData data)
        {
            data = null;
            IsActive = false;
            if (!SessionState.GetBool(EnabledKey, false)) return false;
            var source = AssetDatabase.LoadAssetAtPath<LevelContainer>(LevelPath);
            var targets = AssetDatabase.LoadAssetAtPath<TargetLevel>(TargetPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (source == null || targets == null || prefab == null)
            {
                SessionState.SetBool(EnabledKey, false);
                Debug.LogError("[Correio Mágico] Prepare a grama pelo menu Elementos/Grama.");
                return false;
            }
            data = source.levelData.DeepCopyForPlay(levelNumber);
            data.CheckLayers();
            data.LoadTargetObject();
            data.InitTargetObjects(true, targets);
            IsActive = true;
            Debug.Log("[Correio Mágico] GRAMA: teste com 50% das células em grama clara, todas com duas camadas.");
            return true;
        }

        public static GameObject GetPrefab(SquareTypes type) => IsActive && type == SquareTypes.SugarSquare
            ? AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) : null;
    }
}
#endif
