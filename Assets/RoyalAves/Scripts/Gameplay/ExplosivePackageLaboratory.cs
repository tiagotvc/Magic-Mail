#if UNITY_EDITOR
using System.Linq;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using UnityEditor;
using UnityEngine;
namespace RoyalAves.Gameplay
{
    public static class ExplosivePackageLaboratory
    {
        public const string EnabledKey="MagicMail.ExplosivePackageLab";
        public const string ModeKey=EnabledKey+".Mode";
        public static readonly string[] Modes={"Dinamite simples","Duas dinamites","Dinamite + foguete horizontal","Dinamite + helice","Dinamite + bola de luz","Dinamite + foguete vertical","Foguete horizontal + helice","Foguete vertical + helice","Duas helices"};
        public static ItemsTypes PartnerType(int mode)=>mode>=6?ItemsTypes.MARMALADE:mode==2?ItemsTypes.HORIZONTAL_STRIPED:mode==3?ItemsTypes.MARMALADE:
            mode==4?ItemsTypes.MULTICOLOR:mode==5?ItemsTypes.VERTICAL_STRIPED:ItemsTypes.PACKAGE;
        public static bool IsActive {get;private set;}
        public static void ResetActive()=>IsActive=false;
        public static void Disable(){SessionState.SetBool(EnabledKey,false);ResetActive();}
        public static bool TryLoad(int number,out LevelData data)
        {
            data=null;IsActive=false;
            if(!SessionState.GetBool(EnabledKey,false))return false;
            if(SessionState.GetBool(LightBallLaboratory.EnabledKey,false) || LightBallLaboratory.OtherKeys.Where(k=>k!=EnabledKey).Any(k=>SessionState.GetBool(k,false)))
            {Disable();return false;}
            // Reuse the bench assets as templates; construct the larger board in the play copy only.
            var source=AssetDatabase.LoadAssetAtPath<LevelContainer>(LightBallLaboratory.LevelPath);
            var targets=AssetDatabase.LoadAssetAtPath<TargetLevel>(LightBallLaboratory.TargetPath);
            if(source==null||targets==null){Disable();Debug.LogError("Modelo do laboratório de efeitos ausente.");return false;}
            data=source.levelData.DeepCopyForPlay(1);data.limit=99;data.selectedTutorial=0;
            var field=data.fields[0];var template=field.levelSquares[0];
            field.maxRows=field.maxCols=9;field.levelSquares=new SquareBlocks[81];
            var normal=Resources.Load<GameObject>("Items/Item");var package=Resources.Load<GameObject>("Items/PACKAGE");
            for(int row=0;row<9;row++)for(int col=0;col<9;col++)
            {
                var cell=template.DeepCopy();cell.position=new Vector2Int(col,row);cell.enterSquare=row==0;
                cell.item=new ItemForEditor{Item=normal,ItemType=ItemsTypes.NONE,Color=(row+2*col)%4};
                field.levelSquares[row*9+col]=cell;
            }
            int mode=Mathf.Clamp(SessionState.GetInt(ModeKey,0),0,Modes.Length-1);
            void Place(int col,ItemsTypes type)=>field.levelSquares[4*9+col].item=new ItemForEditor{
                Item=Resources.Load<GameObject>("Items/"+type),ItemType=type,Color=0};
            Place(4,mode==6?ItemsTypes.HORIZONTAL_STRIPED:mode==7?ItemsTypes.VERTICAL_STRIPED:mode==8?ItemsTypes.MARMALADE:ItemsTypes.PACKAGE);
            if(mode>0)Place(5,PartnerType(mode));
            data.CheckLayers();data.LoadTargetObject();data.InitTargetObjects(true,targets);
            IsActive=true;return true;
        }
    }
}
#endif
