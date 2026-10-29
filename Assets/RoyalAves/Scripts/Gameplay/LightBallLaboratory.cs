#if UNITY_EDITOR
using System.Linq;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    public static class LightBallLaboratory
    {
        public const string EnabledKey="MagicMail.LightBallLab";
        public const string ModeKey="MagicMail.LightBallLab.Mode";
        public const string LevelPath="Assets/RoyalAves/Laboratory/LightBallLevel.asset";
        public const string TargetPath="Assets/RoyalAves/Laboratory/LightBallTargets.asset";
        public static bool IsActive { get; private set; }
        public static readonly string[] Modes={"Cor comum","Com dinamite","Com foguete","Com hélice","Duas Light Balls"};
        public static readonly string[] OtherKeys={EnchantedStoneLaboratory.EnabledKey,MailbagLaboratory.EnabledKey,
            RouteCentralLaboratory.EnabledKey,FlyingLetterLaboratory.EnabledKey,FrankingMachineLaboratory.EnabledKey,
            MailCubbyLaboratory.EnabledKey,InkLaboratory.EnabledKey,ParcelStackLaboratory.EnabledKey};
        public static ItemsTypes PartnerType(int mode) => mode==1?ItemsTypes.PACKAGE:mode==2?ItemsTypes.HORIZONTAL_STRIPED:
            mode==3?ItemsTypes.MARMALADE:mode==4?ItemsTypes.MULTICOLOR:ItemsTypes.NONE;
        public static void Disable() {SessionState.SetBool(EnabledKey,false);IsActive=false;}
        public static bool TryLoad(int number,out LevelData data)
        {
            data=null;IsActive=false;
            if(!SessionState.GetBool(EnabledKey,false))return false;
            // Existing lab menus do not know this new key; yield when another is explicitly enabled.
            if(OtherKeys.Any(key=>SessionState.GetBool(key,false))) {Disable();return false;}
            var source=AssetDatabase.LoadAssetAtPath<LevelContainer>(LevelPath);
            var targets=AssetDatabase.LoadAssetAtPath<TargetLevel>(TargetPath);
            if(source==null || targets==null){Disable();Debug.LogError("Prepare o laboratório da Light Ball pelo menu Efeitos.");return false;}
            data=source.levelData.DeepCopyForPlay(number);
            int mode=Mathf.Clamp(SessionState.GetInt(ModeKey,0),0,4);
            var type=PartnerType(mode);
            data.fields[0].levelSquares[3*7+4].item=new ItemForEditor
                {Item=Resources.Load<GameObject>(type==ItemsTypes.NONE?"Items/Item":"Items/"+type),ItemType=type,Color=0};
            data.CheckLayers();data.LoadTargetObject();data.InitTargetObjects(true,targets);
            IsActive=true;return true;
        }
    }
}
#endif
