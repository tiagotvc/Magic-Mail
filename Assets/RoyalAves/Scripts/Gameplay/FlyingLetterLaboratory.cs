#if UNITY_EDITOR
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using UnityEditor;

namespace RoyalAves.Gameplay
{
    public static class FlyingLetterLaboratory
    {
        public const string EnabledKey = "MagicMail.FlyingLetterLaboratory";
        public const string LevelPath = "Assets/RoyalAves/Laboratory/FlyingLetterLevel.asset";
        public const string TargetPath = "Assets/RoyalAves/Laboratory/FlyingLetterTargets.asset";
        public static bool IsActive { get; private set; }
        public static void ResetActive() => IsActive = false;
        public static bool TryLoad(int number, out LevelData data)
        {
            data = null;
            if (!SessionState.GetBool(EnabledKey, false)) return false;
            if (SessionState.GetBool(ParcelStackLaboratory.EnabledKey, false) ||
                SessionState.GetBool(InkLaboratory.EnabledKey, false) ||
                SessionState.GetBool(MailCubbyLaboratory.EnabledKey, false) ||
                SessionState.GetBool(FrankingMachineLaboratory.EnabledKey, false))
            {
                SessionState.SetBool(EnabledKey, false);
                return false;
            }
            var source = AssetDatabase.LoadAssetAtPath<LevelContainer>(LevelPath);
            var targets = AssetDatabase.LoadAssetAtPath<TargetLevel>(TargetPath);
            if (source == null || targets == null) { SessionState.SetBool(EnabledKey, false); return false; }
            data = source.levelData.DeepCopyForPlay(number);
            data.CheckLayers();
            data.LoadTargetObject();
            data.InitTargetObjects(true, targets);
            IsActive = true;
            return true;
        }
    }
}
#endif
