#if UNITY_EDITOR
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Items._Interfaces;

namespace RoyalAves.Gameplay
{
    public static class RouteCentralLaboratory
    {
        public const string EnabledKey = "MagicMail.RouteCentralLab";
        public const string LevelPath = "Assets/RoyalAves/Laboratory/RouteCentralLevel.asset";
        public const string TargetPath = "Assets/RoyalAves/Laboratory/RouteCentralTargets.asset";
        public const string CellPath = "Assets/RoyalAves/Laboratory/RouteCentralCell.prefab";
        public static bool IsActive { get; private set; }
        public static void ResetActive() => IsActive = false;
        public static bool TryLoad(int number, out LevelData data)
        {
            data = null;
            if (!SessionState.GetBool(EnabledKey, false)) return false;
            foreach (var key in new[] { FlyingLetterLaboratory.EnabledKey, FrankingMachineLaboratory.EnabledKey,
                MailCubbyLaboratory.EnabledKey, InkLaboratory.EnabledKey, ParcelStackLaboratory.EnabledKey })
                if (SessionState.GetBool(key, false)) { SessionState.SetBool(EnabledKey, false); return false; }
            var source = AssetDatabase.LoadAssetAtPath<LevelContainer>(LevelPath);
            var targets = AssetDatabase.LoadAssetAtPath<TargetLevel>(TargetPath);
            if (source == null || targets == null) return false;
            data = source.levelData.DeepCopyForPlay(number);
            // Reassert the lab palette at load time: older saved lab assets may
            // still contain the former first-four palette, which includes yellow.
            ApplyPalette(data);
            data.CheckLayers();
            data.LoadTargetObject();
            data.InitTargetObjects(true, targets);
            IsActive = true;
            return true;
        }
        public static GameObject GetPrefab(SquareTypes type) => IsActive && type == SquareTypes.SolidBlock
            ? AssetDatabase.LoadAssetAtPath<GameObject>(CellPath) : null;

        public static int[] ApplyPalette(LevelData data)
        {
            var sprites = Resources.Load<GameObject>("Items/Item").GetComponent<IColorableComponent>().GetSprites(data.levelNum);
            var colors = RouteCentralCell.RequestedPieces.Select(piece => Array.FindIndex(sprites,
                sprite => sprite != null && sprite.texture.name == piece)).ToArray();
            if (colors.Any(c => c < 0) || colors.Distinct().Count() != 4)
                throw new InvalidOperationException("A paleta do laboratório deve conter trevo, pena lilás, selo vermelho e envelope azul.");
            data.pieceColors = colors.ToList();
            data.colorLimit = 4;
            foreach (var field in data.fields)
                foreach (var square in field.levelSquares)
                    if (square.item != null && square.item.Item != null && !colors.Contains(square.item.Color))
                        square.item.Color = colors[0];
            return colors;
        }
    }
}
#endif
