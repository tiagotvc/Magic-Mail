#if UNITY_EDITOR
using System.Linq;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using UnityEditor;
using UnityEngine;
namespace RoyalAves.Gameplay
{
    public static class PostalTapeLaboratory
    {
        public const string EnabledKey="MagicMail.PostalTapeLab";
        public const string LevelPath="Assets/RoyalAves/Laboratory/PostalTapeLevel.asset";
        public const string TargetPath="Assets/RoyalAves/Laboratory/PostalTapeTargets.asset";
        public const string SpritePath="Assets/RoyalAves/Art/Obstacles/PostalTape/TapeStrip.asset";
        public static readonly Vector2Int[] Positions={new Vector2Int(3,2),new Vector2Int(1,5),new Vector2Int(4,4),new Vector2Int(3,5),new Vector2Int(5,2)};
        public static bool IsActive{get;private set;}
        public static void Disable(){SessionState.SetBool(EnabledKey,false);IsActive=false;}
        public static bool TryLoad(int number,out LevelData data)
        {
            data=null;IsActive=false;if(!SessionState.GetBool(EnabledKey,false))return false;
            if(LightBallLaboratory.OtherKeys.Where(k=>k!=EnabledKey).Any(k=>SessionState.GetBool(k,false)) || SessionState.GetBool(LightBallLaboratory.EnabledKey,false) || SessionState.GetBool(InkwellLaboratory.EnabledKey,false)){Disable();return false;}
            var level=AssetDatabase.LoadAssetAtPath<LevelContainer>(LevelPath);var targets=AssetDatabase.LoadAssetAtPath<TargetLevel>(TargetPath);
            if(level==null || targets==null){Disable();return false;}
            data=level.levelData.DeepCopyForPlay(number);data.levelNum=102;data.CheckLayers();data.LoadTargetObject();data.InitTargetObjects(true,targets);IsActive=true;return true;
        }
    }
}
#endif
