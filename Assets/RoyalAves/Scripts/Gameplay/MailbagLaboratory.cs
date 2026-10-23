#if UNITY_EDITOR
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using UnityEditor;
using System;
using System.Linq;
using UnityEngine;
using SweetSugar.Scripts.Items._Interfaces;

namespace RoyalAves.Gameplay
{
    public static class MailbagLaboratory
    {
        public const string EnabledKey = "MagicMail.MailbagLab";
        public const string LevelPath = "Assets/RoyalAves/Laboratory/MailbagLevel.asset";
        public const string TargetPath = "Assets/RoyalAves/Laboratory/MailbagTargets.asset";
        public const string CellPath = "Assets/RoyalAves/Laboratory/BagOpeningCell.prefab";
        public static UnityEngine.GameObject GetPrefab(SweetSugar.Scripts.Blocks.SquareTypes type) =>
            IsActive && type == SweetSugar.Scripts.Blocks.SquareTypes.SolidBlock
                ? AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(CellPath) : null;
        public static bool IsActive { get; private set; }
        public static void ResetActive() => IsActive = false;
        public static bool TryLoad(int number, out LevelData data)
        {
            data = null;
            if (!SessionState.GetBool(EnabledKey,false)) return false;
            foreach (string key in new[] {RouteCentralLaboratory.EnabledKey,FlyingLetterLaboratory.EnabledKey,
                MailCubbyLaboratory.EnabledKey,FrankingMachineLaboratory.EnabledKey,InkLaboratory.EnabledKey,ParcelStackLaboratory.EnabledKey})
                if (SessionState.GetBool(key,false)) { SessionState.SetBool(EnabledKey,false); return false; }
            var source = AssetDatabase.LoadAssetAtPath<LevelContainer>(LevelPath);
            var targets = AssetDatabase.LoadAssetAtPath<TargetLevel>(TargetPath);
            if (source == null || targets == null) return false;
            data = source.levelData.DeepCopyForPlay(number);
            UseBlueEnvelope(data);
            data.CheckLayers(); data.LoadTargetObject(); data.InitTargetObjects(true,targets);
            IsActive = true;
            return true;
        }

        public static void UseBlueEnvelope(LevelData data)
        {
            var sprites=Resources.Load<GameObject>("Items/Item").GetComponent<IColorableComponent>().GetSprites(data.levelNum);
            int ink=Array.FindIndex(sprites,s=>s!=null && s.texture.name=="ink");
            int blue=Array.FindIndex(sprites,s=>s!=null && s.texture.name=="bow");
            if (blue<0) throw new InvalidOperationException("Envelope azul não encontrado na paleta do laboratório.");
            data.pieceColors=data.pieceColors.Select(c=>c==ink?blue:c).Distinct().ToList();
            if (!data.pieceColors.Contains(blue)) data.pieceColors.Add(blue);
            data.colorLimit=data.pieceColors.Count;
            foreach (var field in data.fields)
                foreach (var cell in field.levelSquares)
                    if (cell.item!=null && cell.item.Color==ink) cell.item.Color=blue;
        }
    }
}
#endif
