#if UNITY_EDITOR
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    // Session-only override: laboratory assets never enter the campaign or player save.
    public static class ParcelStackLaboratory
    {
        public const string EnabledKey = "MagicMail.ParcelStackLaboratory";
        public const string LevelPath = "Assets/RoyalAves/Laboratory/ParcelStackLevel.asset";
        public const string TargetPath = "Assets/RoyalAves/Laboratory/ParcelStackTargets.asset";
        public static bool IsActive { get; private set; }
        public static void ResetActive() => IsActive = false;

        public static bool TryLoad(int levelNumber, out LevelData data)
        {
            data = null;
            IsActive = false;
            if (!SessionState.GetBool(EnabledKey, false)) return false;
            var source = AssetDatabase.LoadAssetAtPath<LevelContainer>(LevelPath);
            var targets = AssetDatabase.LoadAssetAtPath<TargetLevel>(TargetPath);
            if (source == null || targets == null)
            {
                SessionState.SetBool(EnabledKey, false);
                Debug.LogError("[Correio Mágico] Laboratório ausente. Recrie pelo menu Pilha de Encomendas.");
                return false;
            }
            data = source.levelData.DeepCopyForPlay(levelNumber);
            data.CheckLayers();
            data.LoadTargetObject();
            data.InitTargetObjects(true, targets);
            IsActive = true;
            Debug.Log("[Correio Mágico] LABORATÓRIO: quatro pacotes, meta 4. Selo e fitas saem sem abrir o papel; somente a animação final contabiliza a destruição.");
            return true;
        }
    }
}
#endif
